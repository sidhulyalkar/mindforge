#!/usr/bin/env python3
"""Unity-driven calibration followed by the production Mindforge SSVEP decoder.

Unity owns presentation and sends labeled begin/end markers on UDP 19743. Python
continuously acquires LSL EEG, fits session-specific thresholds, emits calibration
status heartbeats, then continues with the exact NeuralEvent stream used by combat.
No raw EEG is written to disk.

When and only when source_mode is an explicit synthetic source, this tool may drive
the neurOS Phantom Unicorn localhost control port so REST/SIGHT/GUARD calibration
labels and synthetic EEG state cannot drift apart during a golden-path rehearsal.

V0.34 adds two fail-closed contracts while remaining compatible with V0.33 markers:

* every adaptive calibration stage and neural window is bound to one frozen Unity
  stimulus layout through ``GameMarker.trial_id``;
* the repeated-block protocol reserves the final Sight and Guard block for held-out
  validation, so promotion is not based only on overlapping windows used to fit the
  participant thresholds.
"""
from __future__ import annotations

import argparse
import json
import socket
import time
from pathlib import Path
from typing import Any

import numpy as np

from mindforge_neuro import AuraTarget, SsvepConfig, SsvepDecoder
from mindforge_neuro.acquisition import UnicornLslSource
from mindforge_neuro.calibration import calibrate_decoder
from mindforge_neuro.events import EventType, NeuralEvent
from mindforge_neuro.markers import GameMarker
from mindforge_neuro.resonance import ResonanceEpochRuntime
from mindforge_neuro.runtime import UdpEventSink

STAGES = ("baseline", "sight", "guard")
POSTERIOR = (4, 5, 6, 7)
PHANTOM_STAGE_COMMAND = {"baseline": "0", "sight": "1", "guard": "2"}


def split_windows(eeg: np.ndarray, samples: int, hop: int) -> list[np.ndarray]:
    if eeg.shape[1] < samples:
        return []
    return [
        eeg[:, start:start + samples]
        for start in range(0, eeg.shape[1] - samples + 1, max(1, hop))
    ]


def heldout_validation(
    decoder: SsvepDecoder,
    trials: list[tuple[AuraTarget, np.ndarray]],
    *,
    minimum_per_target: int = 2,
) -> dict[str, Any]:
    """Evaluate calibrated thresholds on non-overlapping windows from unseen blocks."""
    usable = {AuraTarget.SIGHT: 0, AuraTarget.GUARD: 0}
    correct = {AuraTarget.SIGHT: 0, AuraTarget.GUARD: 0}
    accepted_correct = 0
    qualities: list[float] = []
    margins: list[float] = []

    for truth, eeg in trials:
        decision = decoder.decide(eeg)
        if decision.quality.artifact or decision.quality.score < decoder.config.min_quality:
            continue
        scores = decoder.score(eeg)
        ranked = sorted(scores.items(), key=lambda kv: kv[1], reverse=True)
        pred, top = ranked[0]
        usable[truth] += 1
        correct[truth] += int(pred == truth)
        accepted_correct += int(decision.accepted and decision.target == truth)
        qualities.append(float(decision.quality.score))
        margins.append(float(top - ranked[1][1]))

    if min(usable.values()) < max(1, int(minimum_per_target)):
        raise ValueError(
            "held-out validation has too few clean windows: "
            f"sight={usable[AuraTarget.SIGHT]} guard={usable[AuraTarget.GUARD]}"
        )

    sight_accuracy = correct[AuraTarget.SIGHT] / usable[AuraTarget.SIGHT]
    guard_accuracy = correct[AuraTarget.GUARD] / usable[AuraTarget.GUARD]
    total = usable[AuraTarget.SIGHT] + usable[AuraTarget.GUARD]
    return {
        "usable_windows": total,
        "usable_sight_windows": usable[AuraTarget.SIGHT],
        "usable_guard_windows": usable[AuraTarget.GUARD],
        "sight_accuracy": sight_accuracy,
        "guard_accuracy": guard_accuracy,
        "balanced_accuracy": 0.5 * (sight_accuracy + guard_accuracy),
        "accepted_correct_fraction": accepted_correct / max(1, total),
        "mean_quality": float(np.mean(qualities)) if qualities else 0.0,
        "median_margin": float(np.median(margins)) if margins else 0.0,
    }


def resting_alpha_diagnostics(eeg: np.ndarray, sample_rate_hz: float) -> dict[str, float]:
    x = np.mean(eeg[list(POSTERIOR)], axis=0).astype(float)
    x -= np.mean(x)
    if x.size < 16:
        return {"alpha_peak_hz": 0.0, "alpha_fraction": 0.0}
    spec = np.abs(np.fft.rfft(x)) ** 2
    freq = np.fft.rfftfreq(x.size, d=1.0 / sample_rate_hz)
    alpha = (freq >= 8.0) & (freq <= 13.0)
    broadband = (freq >= 4.0) & (freq <= 35.0)
    if not np.any(alpha) or float(np.sum(spec[broadband])) <= 0.0:
        return {"alpha_peak_hz": 0.0, "alpha_fraction": 0.0}
    alpha_idx = np.flatnonzero(alpha)
    peak_idx = alpha_idx[int(np.argmax(spec[alpha]))]
    return {
        "alpha_peak_hz": float(freq[peak_idx]),
        "alpha_fraction": float(np.sum(spec[alpha]) / np.sum(spec[broadband])),
    }


def status_event(
    seq: int,
    kind: EventType,
    model_id: str,
    source_mode: str,
    confidence: float = 0.0,
    quality: float = 0.0,
    reason: str | None = None,
    session_id: str | None = None,
    calibration_id: str | None = None,
) -> NeuralEvent:
    return NeuralEvent.create(
        seq=seq,
        event=kind,
        target=None,
        confidence=confidence,
        quality=quality,
        model_id=model_id,
        reason=reason,
        source_mode=source_mode,
        session_id=session_id,
        calibration_id=calibration_id,
        authority_ttl_ms=0,
    )


class PhantomController:
    """Best-effort localhost simulator control. Never enabled for live/replay."""

    def __init__(self, enabled: bool, host: str, port: int):
        self.enabled = bool(enabled)
        self.address = (host, port)
        self.socket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM) if self.enabled else None

    def send(self, command: str) -> None:
        if self.socket is None:
            return
        self.socket.sendto(command.encode("utf-8"), self.address)

    def close(self) -> None:
        if self.socket is not None:
            self.socket.close()
            self.socket = None


def marker_layout_id(marker: GameMarker) -> str | None:
    value = marker.trial_id
    if value is None:
        return None
    value = value.strip()
    return value or None


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--stream-name", default="UnicornMock")
    parser.add_argument("--source-id", default=None)
    parser.add_argument("--scale-to-uv", type=float, default=1.0)
    parser.add_argument("--marker-host", default="127.0.0.1")
    parser.add_argument("--marker-port", type=int, default=19743)
    parser.add_argument("--udp-host", default="127.0.0.1")
    parser.add_argument("--udp-port", type=int, default=19742)
    parser.add_argument(
        "--source-mode",
        choices=("simulation", "live", "replay", "synthetic_eeg", "eeg_replay"),
        default="simulation",
    )
    parser.add_argument("--calibration-hop-seconds", type=float, default=0.50)
    parser.add_argument("--report-dir", default="experiments/reports")
    parser.add_argument("--phantom-control-host", default="127.0.0.1")
    parser.add_argument("--phantom-control-port", type=int, default=19744)
    parser.add_argument(
        "--disable-phantom-control",
        action="store_true",
        help="do not drive neurOS Phantom from Unity calibration markers",
    )
    parser.add_argument(
        "--require-layout-id",
        action=argparse.BooleanOptionalAction,
        default=True,
        help="require one immutable GameMarker.trial_id across calibration and neural windows",
    )
    parser.add_argument("--heldout-min-balanced-accuracy", type=float, default=0.75)
    parser.add_argument("--heldout-min-accepted-fraction", type=float, default=0.50)
    args = parser.parse_args()

    cfg = SsvepConfig()
    decoder = SsvepDecoder(cfg)
    source = UnicornLslSource(
        stream_name=args.stream_name,
        source_id=args.source_id,
        scale_to_uv=args.scale_to_uv,
    )
    source.connect()
    model_id = f"{args.source_mode}-{int(time.time())}"
    sink = UdpEventSink(args.udp_host, args.udp_port)
    markers = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    markers.bind((args.marker_host, args.marker_port))
    markers.setblocking(False)

    # Keep synthetic authority opt-in painfully explicit. LIVE never inherits simulator
    # control merely because a stream happens to be named UnicornMock.
    legacy_simulation = args.source_mode == "simulation"
    explicit_synthetic_eeg = args.source_mode == "synthetic_eeg"
    phantom_enabled = (
        (legacy_simulation or explicit_synthetic_eeg)
        and not args.disable_phantom_control
        and args.stream_name == "UnicornMock"
    )
    phantom = PhantomController(
        phantom_enabled,
        args.phantom_control_host,
        args.phantom_control_port,
    )
    if phantom_enabled:
        phantom.send("0")
        print(
            "Phantom calibration control enabled at "
            f"udp://{args.phantom_control_host}:{args.phantom_control_port}"
        )

    seq = 1
    sink.send(
        status_event(
            seq,
            EventType.CALIBRATION_SERVICE_READY,
            model_id,
            args.source_mode,
            reason=str(source.stream_identity),
        )
    )
    heartbeat_at = time.monotonic() + 0.5
    print(
        f"Connected to {source.stream_identity}; waiting for Unity Awakening markers "
        f"on {args.marker_port}"
    )

    active_stage: str | None = None
    active_game_session: str | None = None
    active_calibration: str | None = None
    active_layout_id: str | None = None
    active_chunks: list[np.ndarray] = []
    epochs: dict[str, list[np.ndarray]] = {}
    adaptive_protocol = False
    protocol_complete = False

    def reject_calibration(reason: str) -> None:
        nonlocal seq, active_stage, active_chunks, epochs, protocol_complete
        if phantom_enabled:
            phantom.send("0")
        seq += 1
        sink.send(
            status_event(
                seq,
                EventType.CALIBRATION_FAILED,
                model_id,
                args.source_mode,
                reason=reason[:240],
                session_id=active_game_session,
                calibration_id=active_calibration,
            )
        )
        print(f"Calibration rejected: {reason}. Waiting for Unity retry.")
        active_stage = None
        active_chunks = []
        epochs = {}
        protocol_complete = False

    try:
        while True:
            while True:
                try:
                    raw, _ = markers.recvfrom(65535)
                except BlockingIOError:
                    break
                try:
                    marker = GameMarker.from_json(raw)
                except Exception:
                    continue
                if marker.category != "calibration":
                    continue

                game_session = marker.session_id or None
                calibration_session = marker.calibration_id or marker.session_id
                layout_id = marker_layout_id(marker)
                stage = str(marker.stage or "")
                action = str(marker.action or "")

                if args.require_layout_id and layout_id is None:
                    active_game_session = game_session
                    active_calibration = calibration_session
                    reject_calibration("stimulus_layout_id_missing")
                    continue

                if stage == "protocol":
                    if action == "begin":
                        active_game_session = game_session
                        active_calibration = calibration_session
                        active_layout_id = layout_id
                        active_stage = None
                        active_chunks = []
                        epochs = {}
                        adaptive_protocol = True
                        protocol_complete = False
                        if phantom_enabled:
                            phantom.send("0")
                        print(
                            "Calibration PROTOCOL BEGIN "
                            f"game={active_game_session or '-'} "
                            f"calibration={(active_calibration or '-')[:12]} "
                            f"layout={active_layout_id or '-'}"
                        )
                    elif (
                        action == "complete"
                        and active_calibration == calibration_session
                    ):
                        if active_layout_id != layout_id:
                            reject_calibration(
                                "stimulus_layout_changed_before_protocol_complete:"
                                f"{active_layout_id or '-'}->{layout_id or '-'}"
                            )
                            active_layout_id = layout_id
                            continue
                        protocol_complete = True
                        if phantom_enabled:
                            phantom.send("0")
                        print(
                            "Calibration PROTOCOL COMPLETE "
                            f"calibration={(active_calibration or '-')[:12]} "
                            f"layout={active_layout_id or '-'}"
                        )
                    continue

                if stage not in STAGES:
                    continue

                if action == "begin":
                    if active_calibration != calibration_session:
                        epochs = {}
                        active_layout_id = layout_id
                        adaptive_protocol = False
                        protocol_complete = True
                    elif active_layout_id is None:
                        active_layout_id = layout_id

                    if active_layout_id != layout_id:
                        active_game_session = game_session
                        active_calibration = calibration_session
                        reject_calibration(
                            f"stimulus_layout_changed:{active_layout_id or '-'}->{layout_id or '-'}"
                        )
                        active_layout_id = layout_id
                        continue

                    active_game_session = game_session
                    active_calibration = calibration_session
                    active_stage = stage
                    active_chunks = []
                    source.flush()
                    if phantom_enabled:
                        phantom.send(PHANTOM_STAGE_COMMAND[stage])
                    print(
                        f"Calibration BEGIN {stage} game={active_game_session or '-'} "
                        f"calibration={(active_calibration or '-')[:12]} "
                        f"layout={active_layout_id or '-'}"
                    )
                elif (
                    action == "end"
                    and active_stage == stage
                    and active_calibration == calibration_session
                ):
                    if active_layout_id != layout_id:
                        reject_calibration(
                            "stimulus_layout_changed_before_end:"
                            f"{active_layout_id or '-'}->{layout_id or '-'}"
                        )
                        active_layout_id = layout_id
                        continue
                    segment = (
                        np.concatenate(active_chunks, axis=1)
                        if active_chunks
                        else np.empty((8, 0), dtype=float)
                    )
                    epochs.setdefault(stage, []).append(segment)
                    print(
                        f"Calibration END {stage}: segment={len(epochs[stage])} "
                        f"samples={segment.shape[1]} layout={active_layout_id or '-'}"
                    )
                    active_stage = None
                    active_chunks = []
                    if phantom_enabled and stage == "guard":
                        phantom.send("0")

            chunk = source.pull_chunk(max_samples=128, timeout_s=0.05)
            if chunk is not None and active_stage is not None:
                active_chunks.append(chunk.samples_uv.copy())

            now = time.monotonic()
            if now >= heartbeat_at:
                seq += 1
                sink.send(
                    status_event(
                        seq,
                        EventType.CALIBRATION_HEARTBEAT,
                        model_id,
                        args.source_mode,
                        reason=(
                            f"{active_stage or 'waiting'};layout={active_layout_id or '-'};"
                            f"protocol={'adaptive' if adaptive_protocol else 'legacy'}"
                        ),
                        session_id=active_game_session,
                        calibration_id=active_calibration,
                    )
                )
                heartbeat_at = now + 0.5

            enough_stage_data = all(stage in epochs and epochs[stage] for stage in STAGES)
            ready_to_fit = enough_stage_data and (not adaptive_protocol or protocol_complete)
            if ready_to_fit:
                try:
                    if args.require_layout_id and not active_layout_id:
                        raise ValueError("stimulus_layout_id_missing_at_fit")

                    hop = max(1, int(round(args.calibration_hop_seconds * cfg.sample_rate_hz)))
                    training_trials: list[tuple[AuraTarget, np.ndarray]] = []
                    validation_trials: list[tuple[AuraTarget, np.ndarray]] = []

                    for target, stage_name in (
                        (AuraTarget.SIGHT, "sight"),
                        (AuraTarget.GUARD, "guard"),
                    ):
                        segments = epochs[stage_name]
                        if adaptive_protocol:
                            if len(segments) < 3:
                                raise ValueError(
                                    f"adaptive protocol requires 3 {stage_name} blocks; got {len(segments)}"
                                )
                            train_segments = segments[:-1]
                            heldout_segments = segments[-1:]
                        else:
                            train_segments = segments
                            heldout_segments = []

                        for segment in train_segments:
                            training_trials.extend(
                                (target, window)
                                for window in split_windows(segment, cfg.window_samples, hop)
                            )
                        for segment in heldout_segments:
                            # Non-overlapping held-out windows avoid sharing samples with
                            # one another and come from an entire unseen presentation block.
                            validation_trials.extend(
                                (target, window)
                                for window in split_windows(
                                    segment,
                                    cfg.window_samples,
                                    cfg.window_samples,
                                )
                            )

                    profile = calibrate_decoder(
                        decoder,
                        training_trials,
                        model_id=model_id,
                    )
                    baseline_epoch = np.concatenate(epochs["baseline"], axis=1)
                    baseline = resting_alpha_diagnostics(
                        baseline_epoch,
                        cfg.sample_rate_hz,
                    )

                    if profile.training_accuracy < 0.70 or profile.accepted_fraction < 0.50:
                        raise ValueError(
                            "training separability below gate: "
                            f"accuracy={profile.training_accuracy:.3f}, "
                            f"accepted={profile.accepted_fraction:.3f}"
                        )

                    validation: dict[str, Any] | None = None
                    promotion_accuracy = profile.training_accuracy
                    promotion_accepted = profile.accepted_fraction
                    if adaptive_protocol:
                        validation = heldout_validation(decoder, validation_trials)
                        promotion_accuracy = float(validation["balanced_accuracy"])
                        promotion_accepted = float(validation["accepted_correct_fraction"])
                        if promotion_accuracy < args.heldout_min_balanced_accuracy:
                            raise ValueError(
                                "held-out balanced accuracy below gate: "
                                f"{promotion_accuracy:.3f} < {args.heldout_min_balanced_accuracy:.3f}"
                            )
                        if promotion_accepted < args.heldout_min_accepted_fraction:
                            raise ValueError(
                                "held-out accepted fraction below gate: "
                                f"{promotion_accepted:.3f} < {args.heldout_min_accepted_fraction:.3f}"
                            )

                    report = {
                        "schema": "mindforge.calibration_report.v1",
                        "session_id": active_game_session,
                        "calibration_id": active_calibration,
                        "stimulus_layout_id": active_layout_id,
                        "protocol": (
                            "adaptive_repeated_blocks_v34"
                            if adaptive_protocol
                            else "legacy_continuous_v33"
                        ),
                        "model_id": profile.model_id,
                        "source_mode": args.source_mode,
                        "training_accuracy": profile.training_accuracy,
                        "accepted_fraction": profile.accepted_fraction,
                        "training_window_count": len(training_trials),
                        "heldout_window_count": len(validation_trials),
                        "heldout_validation": validation,
                        "promotion_accuracy": promotion_accuracy,
                        "promotion_accepted_fraction": promotion_accepted,
                        "min_score": profile.min_score,
                        "min_margin": profile.min_margin,
                        "sight_off_center": profile.sight_off_center,
                        "sight_off_scale": profile.sight_off_scale,
                        "guard_off_center": profile.guard_off_center,
                        "guard_off_scale": profile.guard_off_scale,
                        "normalization_ready": profile.normalization_ready,
                        **baseline,
                    }
                    report_dir = Path(args.report_dir)
                    report_dir.mkdir(parents=True, exist_ok=True)
                    calibration_name = (
                        active_calibration
                        or active_game_session
                        or str(int(time.time()))
                    )
                    (report_dir / f"calibration-{calibration_name}.json").write_text(
                        json.dumps(report, indent=2, sort_keys=True),
                        encoding="utf-8",
                    )
                    seq += 1
                    sink.send(
                        status_event(
                            seq,
                            EventType.CALIBRATION_READY,
                            model_id,
                            args.source_mode,
                            confidence=promotion_accuracy,
                            quality=promotion_accepted,
                            reason=(
                                f"alpha_peak_hz={baseline['alpha_peak_hz']:.2f};"
                                f"alpha_fraction={baseline['alpha_fraction']:.3f};"
                                f"layout={active_layout_id or '-'};"
                                f"protocol={'adaptive' if adaptive_protocol else 'legacy'}"
                            ),
                            session_id=active_game_session,
                            calibration_id=active_calibration,
                        )
                    )
                    print("Calibration accepted:", json.dumps(report, indent=2))
                    break
                except Exception as exc:
                    reject_calibration(str(exc))

        runtime = ResonanceEpochRuntime(
            decoder,
            profile,
            source_mode=args.source_mode,
            initial_seq=seq,
            session_id=active_game_session,
            calibration_id=active_calibration,
        )
        print(
            f"Calibrated for layout={active_layout_id or '-'}. "
            "Waiting for Unity NEURAL_WINDOW_LISTENING epochs. Ctrl-C to stop."
        )
        if phantom_enabled:
            print(
                "For simulated combat, drive attention/faults with "
                "tools/phantom_control.py."
            )

        terminal_markers = {
            "NEURAL_WINDOW_ENDED",
            "NEURAL_WINDOW_ABSTAINED",
            "NEURAL_WINDOW_RESOLVED",
        }
        while True:
            # Markers are polled before EEG. When LISTENING arrives, flush any queued
            # LSL backlog and reset the cumulative buffer so every sample is post-onset.
            while True:
                try:
                    raw, _ = markers.recvfrom(65535)
                except BlockingIOError:
                    break
                try:
                    marker = GameMarker.from_json(raw)
                except Exception:
                    continue
                if marker.category != "neural_window" or marker.stimulus_epoch < 0:
                    continue

                layout_id = marker_layout_id(marker)
                if args.require_layout_id and layout_id != active_layout_id:
                    runtime.cancel_epoch(marker.stimulus_epoch)
                    print(
                        f"Epoch {marker.stimulus_epoch}: rejected layout mismatch "
                        f"calibrated={active_layout_id or '-'} marker={layout_id or '-'}"
                    )
                    continue

                if marker.event == "NEURAL_WINDOW_LISTENING":
                    source.flush()
                    runtime.begin_epoch(
                        marker.stimulus_epoch,
                        session_id=marker.session_id or active_game_session,
                    )
                    print(
                        f"Epoch {marker.stimulus_epoch}: coded onset; EEG queue flushed; "
                        f"layout={active_layout_id or '-'}"
                    )
                elif marker.event in terminal_markers:
                    runtime.cancel_epoch(marker.stimulus_epoch)

            chunk = source.pull_chunk(max_samples=32, timeout_s=0.02)
            if chunk is None or not runtime.active:
                continue
            event = runtime.push(chunk.samples_uv, chunk.timestamps_s)
            if event is None:
                continue
            sink.send(event)
            print(
                f"epoch={event.stimulus_epoch} {event.event.value:13s} "
                f"target={(event.target.value if event.target else '-'):5s} "
                f"evidence={event.evidence_ms:4d}ms S={event.sight_score:.3f} "
                f"G={event.guard_score:.3f} margin={event.margin:.3f} "
                f"q={event.quality:.2f} reason={event.reason or '-'}"
            )
    except KeyboardInterrupt:
        print("\nStopping calibrated decoder.")
    finally:
        if phantom_enabled:
            phantom.send("0")
        phantom.close()
        markers.close()
        sink.close()
        source.close()


if __name__ == "__main__":
    main()
