#!/usr/bin/env python3
"""Unity-driven calibration followed by the production Mindforge SSVEP decoder.

Unity owns presentation and sends labeled begin/end markers on UDP 19743. Python
continuously acquires LSL EEG, fits session-specific thresholds, emits calibration
status heartbeats, then continues with the exact NeuralEvent stream used by combat.
No raw EEG is written to disk.

When and only when source_mode is an explicit synthetic source, this tool may drive
the neurOS Phantom Unicorn localhost control port so REST/SIGHT/GUARD calibration
labels and synthetic EEG state cannot drift apart during a golden-path rehearsal.
"""
from __future__ import annotations

import argparse
import json
import socket
import time
from pathlib import Path

import numpy as np

from mindforge_neuro import AuraTarget, SsvepConfig, SsvepDecoder
from mindforge_neuro.acquisition import SlidingWindowBuffer, UnicornLslSource
from mindforge_neuro.calibration import calibrate_decoder
from mindforge_neuro.events import EventType, NeuralEvent
from mindforge_neuro.markers import GameMarker
from mindforge_neuro.runtime import AuraSelectionRuntime, UdpEventSink

STAGES = ("baseline", "sight", "guard")
POSTERIOR = (4, 5, 6, 7)
PHANTOM_STAGE_COMMAND = {"baseline": "0", "sight": "1", "guard": "2"}


def split_windows(eeg: np.ndarray, samples: int, hop: int) -> list[np.ndarray]:
    if eeg.shape[1] < samples:
        return []
    return [eeg[:, start:start + samples]
            for start in range(0, eeg.shape[1] - samples + 1, max(1, hop))]


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
    return {"alpha_peak_hz": float(freq[peak_idx]),
            "alpha_fraction": float(np.sum(spec[alpha]) / np.sum(spec[broadband]))}


def status_event(seq: int, kind: EventType, model_id: str, source_mode: str,
                 confidence: float = 0.0, quality: float = 0.0,
                 reason: str | None = None, session_id: str | None = None) -> NeuralEvent:
    return NeuralEvent.create(seq=seq, event=kind, target=None, confidence=confidence,
                              quality=quality, model_id=model_id, reason=reason,
                              source_mode=source_mode, session_id=session_id,
                              calibration_id=session_id, authority_ttl_ms=0)


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


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--stream-name", default="UnicornMock")
    parser.add_argument("--source-id", default=None)
    parser.add_argument("--scale-to-uv", type=float, default=1.0)
    parser.add_argument("--marker-host", default="127.0.0.1")
    parser.add_argument("--marker-port", type=int, default=19743)
    parser.add_argument("--udp-host", default="127.0.0.1")
    parser.add_argument("--udp-port", type=int, default=19742)
    parser.add_argument("--source-mode", choices=("simulation", "live", "replay", "synthetic_eeg", "eeg_replay"), default="simulation")
    parser.add_argument("--hop-seconds", type=float, default=0.25)
    parser.add_argument("--calibration-hop-seconds", type=float, default=0.50)
    parser.add_argument("--report-dir", default="experiments/reports")
    parser.add_argument("--phantom-control-host", default="127.0.0.1")
    parser.add_argument("--phantom-control-port", type=int, default=19744)
    parser.add_argument("--disable-phantom-control", action="store_true",
                        help="do not drive neurOS Phantom from Unity calibration markers")
    args = parser.parse_args()

    cfg = SsvepConfig()
    decoder = SsvepDecoder(cfg)
    source = UnicornLslSource(stream_name=args.stream_name, source_id=args.source_id,
                              scale_to_uv=args.scale_to_uv)
    source.connect()
    model_id = f"{args.source_mode}-{int(time.time())}"
    sink = UdpEventSink(args.udp_host, args.udp_port)
    markers = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    markers.bind((args.marker_host, args.marker_port))
    markers.setblocking(False)

    # Keep synthetic authority opt-in painfully explicit. In particular, LIVE must
    # never inherit simulator control merely because a stream happens to be named
    # UnicornMock. The legacy simulation label remains accepted for old workflows.
    legacy_simulation = args.source_mode == "simulation"
    explicit_synthetic_eeg = args.source_mode == "synthetic_eeg"
    phantom_enabled = (
        (legacy_simulation or explicit_synthetic_eeg)
        and not args.disable_phantom_control
        and args.stream_name == "UnicornMock"
    )
    phantom = PhantomController(phantom_enabled, args.phantom_control_host,
                                args.phantom_control_port)
    if phantom_enabled:
        phantom.send("0")
        print(f"Phantom calibration control enabled at udp://{args.phantom_control_host}:{args.phantom_control_port}")

    seq = 1
    sink.send(status_event(seq, EventType.CALIBRATION_SERVICE_READY, model_id, args.source_mode,
                           reason=str(source.stream_identity)))
    heartbeat_at = time.monotonic() + 0.5
    print(f"Connected to {source.stream_identity}; waiting for Unity Awakening markers on {args.marker_port}")

    active_stage: str | None = None
    active_session: str | None = None
    active_chunks: list[np.ndarray] = []
    epochs: dict[str, np.ndarray] = {}

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
                if marker.category != "calibration" or marker.stage not in STAGES:
                    continue
                session = marker.session_id
                stage = str(marker.stage)
                action = str(marker.action or "")
                if action == "begin":
                    if active_session != session:
                        epochs = {}
                    active_session = session
                    active_stage = stage
                    active_chunks = []
                    if phantom_enabled:
                        phantom.send(PHANTOM_STAGE_COMMAND[stage])
                    print(f"Calibration BEGIN {stage} session={session[:8]}")
                elif action == "end" and active_stage == stage and active_session == session:
                    epochs[stage] = (np.concatenate(active_chunks, axis=1)
                                     if active_chunks else np.empty((8, 0), dtype=float))
                    print(f"Calibration END {stage}: {epochs[stage].shape[1]} samples")
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
                sink.send(status_event(seq, EventType.CALIBRATION_HEARTBEAT, model_id, args.source_mode,
                                       reason=active_stage or "waiting", session_id=active_session))
                heartbeat_at = now + 0.5

            if all(stage in epochs for stage in STAGES):
                try:
                    hop = max(1, int(round(args.calibration_hop_seconds * cfg.sample_rate_hz)))
                    trials: list[tuple[AuraTarget, np.ndarray]] = []
                    for target, stage in ((AuraTarget.SIGHT, "sight"), (AuraTarget.GUARD, "guard")):
                        trials.extend((target, window) for window in split_windows(
                            epochs[stage], cfg.window_samples, hop))
                    profile = calibrate_decoder(decoder, trials, model_id=model_id)
                    baseline = resting_alpha_diagnostics(epochs["baseline"], cfg.sample_rate_hz)
                    if profile.training_accuracy < 0.70 or profile.accepted_fraction < 0.50:
                        raise ValueError(
                            f"separability below promotion gate: accuracy={profile.training_accuracy:.3f}, "
                            f"accepted={profile.accepted_fraction:.3f}")

                    report = {
                        "schema": "mindforge.calibration_report.v1",
                        "session_id": active_session,
                        "model_id": profile.model_id,
                        "source_mode": args.source_mode,
                        "training_accuracy": profile.training_accuracy,
                        "accepted_fraction": profile.accepted_fraction,
                        "min_score": profile.min_score,
                        "min_margin": profile.min_margin,
                        **baseline,
                    }
                    report_dir = Path(args.report_dir)
                    report_dir.mkdir(parents=True, exist_ok=True)
                    (report_dir / f"calibration-{active_session}.json").write_text(
                        json.dumps(report, indent=2, sort_keys=True), encoding="utf-8")
                    seq += 1
                    sink.send(status_event(
                        seq, EventType.CALIBRATION_READY, model_id, args.source_mode,
                        confidence=profile.training_accuracy,
                        quality=profile.accepted_fraction,
                        reason=(f"alpha_peak_hz={baseline['alpha_peak_hz']:.2f};"
                                f"alpha_fraction={baseline['alpha_fraction']:.3f}"),
                        session_id=active_session))
                    print("Calibration accepted:", json.dumps(report, indent=2))
                    break
                except Exception as exc:
                    if phantom_enabled:
                        phantom.send("0")
                    seq += 1
                    sink.send(status_event(seq, EventType.CALIBRATION_FAILED, model_id,
                                           args.source_mode, reason=str(exc)[:240],
                                           session_id=active_session))
                    print(f"Calibration rejected: {exc}. Waiting for Unity retry.")
                    epochs = {}

        runtime = AuraSelectionRuntime(
            decoder,
            profile,
            source_mode=args.source_mode,
            initial_seq=seq,
            session_id=active_session,
            calibration_id=active_session,
        )
        buffer = SlidingWindowBuffer(8, cfg.window_samples,
                                     max(1, int(round(args.hop_seconds * cfg.sample_rate_hz))))
        print("Streaming calibrated derived events to Unity. Ctrl-C to stop.")
        if phantom_enabled:
            print("For simulated combat, drive attention/faults with tools/phantom_control.py.")
        while True:
            chunk = source.pull_chunk(max_samples=128, timeout_s=0.35)
            if chunk is None:
                continue
            for window, _timestamps in buffer.push(chunk.samples_uv, chunk.timestamps_s):
                event = runtime.process(window)
                sink.send(event)
                print(
                    f"{event.event.value:13s} target={(event.target.value if event.target else '-'):5s} "
                    f"S={event.sight_score:.3f} G={event.guard_score:.3f} "
                    f"margin={event.margin:.3f} q={event.quality:.2f} reason={event.reason or '-'}")
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
