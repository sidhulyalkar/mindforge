#!/usr/bin/env python3
"""Mindforge BCI game-development harness.

Examples
--------
Run the full no-headset Awakening handshake, then emit decision-level authority::

    python tools/mindforge_dev.py decision --calibrate \
      --script sight:3,guard:3,abstain:1

Run S0 manual BCI. Start this service, then launch Unity with
``-mindforgeManualBCI`` and hold Q/E to select Sight/Guard::

    python tools/mindforge_dev.py manual-service

Replay an exact decision tape through a fresh development handshake::

    python tools/mindforge_dev.py replay experiments/tapes/demo.jsonl --calibrate

Record Unity-originated game markers from the passive mirror lane::

    python tools/mindforge_dev.py marker-log --output experiments/markers/run.jsonl

The harness only substitutes declared layers. ``decision`` never claims to be EEG,
and ``replay`` identifies itself as decision replay in every emitted NeuralEvent.
Development calibration follows the production marker/status handshake but explicitly
declares that no EEG calibration occurred.
"""
from __future__ import annotations

import argparse
import time
from dataclasses import replace
from pathlib import Path

from mindforge_neuro.dev_calibration import DevelopmentCalibrationFixture
from mindforge_neuro.dev_sources import DecisionSimulationConfig, DecisionSimulator, NeuralEventTape, TapeEntry
from mindforge_neuro.events import SourceMode
from mindforge_neuro.manual_dev import UdpManualIntentSource, manual_idle_event, manual_selection_event
from mindforge_neuro.markers import UdpGameMarkerSource
from mindforge_neuro.runtime import UdpEventSink


def _parse_script(raw: str) -> list[tuple[str, float]]:
    result: list[tuple[str, float]] = []
    for token in raw.split(","):
        token = token.strip()
        if not token:
            continue
        if ":" in token:
            state, seconds = token.rsplit(":", 1)
            duration = float(seconds)
        else:
            state, duration = token, 1.0
        state = state.strip().lower()
        if state not in {"sight", "guard", "abstain", "none", "rest", "lost", "recovered", "stop"}:
            raise ValueError(f"unsupported decision state: {state}")
        if duration <= 0:
            raise ValueError("script durations must be > 0")
        result.append((state, duration))
    if not result:
        raise ValueError("decision script is empty")
    return result


def _print_status(event) -> None:
    print(
        f"fixture #{event.seq:03d} {event.event.value:27s} "
        f"session={event.session_id or '-'} calibration={event.calibration_id or '-'} "
        f"src={event.source_mode} reason={event.reason or '-'}"
    )


def _await_development_calibration(
    *,
    sink: UdpEventSink,
    source_mode: str,
    marker_host: str,
    marker_port: int,
    timeout_s: float,
) -> DevelopmentCalibrationFixture:
    fixture = DevelopmentCalibrationFixture(source_mode=source_mode)
    deadline = time.monotonic() + timeout_s if timeout_s > 0 else None
    print(
        f"Development calibration fixture listening on udp://{marker_host}:{marker_port}; "
        f"NeuralEvent source_mode={source_mode}"
    )
    print("This substitutes calibration protocol authority. It does NOT claim EEG was measured.")

    with UdpGameMarkerSource(marker_host, marker_port, timeout_s=0.10) as marker_source:
        while not fixture.completed:
            if deadline is not None and time.monotonic() >= deadline:
                raise TimeoutError("development calibration fixture timed out before complete baseline/sight/guard protocol")

            periodic = fixture.periodic()
            if periodic is not None:
                sink.send(periodic)
                _print_status(periodic)

            marker = marker_source.receive()
            if marker is None:
                continue
            response = fixture.consume(marker)
            if response is not None:
                sink.send(response)
                _print_status(response)

    print(
        f"Development calibration complete: game={fixture.session_id or '-'} "
        f"calibration={fixture.calibration_id or '-'}"
    )
    return fixture


def _decision_config(args: argparse.Namespace) -> DecisionSimulationConfig:
    return DecisionSimulationConfig(
        seed=args.seed,
        confidence_mean=args.confidence,
        quality_mean=args.quality,
        jitter=args.jitter,
        authority_ttl_ms=args.authority_ttl_ms,
    )


def run_decision(args: argparse.Namespace) -> None:
    script = _parse_script(args.script)
    sink = UdpEventSink(args.host, args.port)
    fixture = None
    try:
        if args.calibrate:
            fixture = _await_development_calibration(
                sink=sink,
                source_mode=SourceMode.SIMULATED_DECISION.value,
                marker_host=args.marker_host,
                marker_port=args.marker_port,
                timeout_s=args.calibration_timeout,
            )

        simulator = DecisionSimulator(
            _decision_config(args),
            session_id=fixture.session_id if fixture else args.session_id,
            calibration_id=fixture.calibration_id if fixture else None,
            initial_seq=fixture.seq if fixture else 0,
        )
        period = 1.0 / args.hz
        started = time.monotonic()
        tape: list[TapeEntry] = []
        print(f"Decision simulator -> udp://{args.host}:{args.port} session={simulator.session_id}")
        for _repeat in range(args.repeat):
            for state, duration in script:
                ticks = max(1, int(round(duration * args.hz)))
                for _ in range(ticks):
                    loop_start = time.monotonic()
                    event = simulator.next(state)
                    sink.send(event)
                    offset = time.monotonic() - started
                    tape.append(TapeEntry(offset, event))
                    print(
                        f"{offset:7.3f}s {event.event.value:17s} "
                        f"target={(event.target.value if event.target else '-'):5s} "
                        f"c={event.confidence:.2f} q={event.quality:.2f} src={event.source_mode}"
                    )
                    sleep_for = period - (time.monotonic() - loop_start)
                    if sleep_for > 0:
                        time.sleep(sleep_for)
    except KeyboardInterrupt:
        print("\nDecision simulation interrupted.")
    finally:
        sink.close()
        if "tape" in locals() and args.output_tape:
            NeuralEventTape(tape).save(args.output_tape)
            print(f"Tape written: {args.output_tape}")


def run_replay(args: argparse.Namespace) -> None:
    tape = NeuralEventTape.load(args.tape)
    if not tape.entries:
        raise SystemExit("Replay tape is empty")
    sink = UdpEventSink(args.host, args.port)
    fixture = None
    try:
        if args.calibrate:
            fixture = _await_development_calibration(
                sink=sink,
                source_mode=SourceMode.DECISION_REPLAY.value,
                marker_host=args.marker_host,
                marker_port=args.marker_port,
                timeout_s=args.calibration_timeout,
            )

        previous_offset = 0.0
        replay_session = fixture.session_id if fixture else args.session_id
        print(f"Decision replay -> udp://{args.host}:{args.port} entries={len(tape.entries)} speed={args.speed:g}x")
        for entry in tape.replay_events(
            initial_seq=fixture.seq if fixture else 0,
            session_id=replay_session,
            calibration_id=fixture.calibration_id if fixture else None,
        ):
            wait = max(0.0, entry.offset_s - previous_offset) / args.speed
            if wait:
                time.sleep(wait)
            now_ns = time.monotonic_ns()
            event = replace(entry.event, monotonic_ns=now_ns, decoder_time_ns=now_ns)
            sink.send(event)
            previous_offset = entry.offset_s
            print(
                f"{entry.offset_s:7.3f}s {event.event.value:17s} "
                f"target={(event.target.value if event.target else '-'):5s} src={event.source_mode}"
            )
    except KeyboardInterrupt:
        print("\nReplay interrupted.")
    finally:
        sink.close()


def run_calibration_fixture(args: argparse.Namespace) -> None:
    sink = UdpEventSink(args.host, args.port)
    try:
        _await_development_calibration(
            sink=sink,
            source_mode=args.source_mode,
            marker_host=args.marker_host,
            marker_port=args.marker_port,
            timeout_s=args.calibration_timeout,
        )
    except KeyboardInterrupt:
        print("\nDevelopment calibration fixture interrupted.")
    finally:
        sink.close()


def run_manual_service(args: argparse.Namespace) -> None:
    sink = UdpEventSink(args.host, args.port)
    try:
        fixture = _await_development_calibration(
            sink=sink,
            source_mode=SourceMode.MANUAL.value,
            marker_host=args.marker_host,
            marker_port=args.marker_port,
            timeout_s=args.calibration_timeout,
        )
        seq = fixture.seq
        deadline = time.monotonic() + args.seconds if args.seconds > 0 else None
        next_idle = 0.0
        print(
            f"Manual intent adapter listening on udp://{args.intent_host}:{args.intent_port}. "
            "Unity must be launched with -mindforgeManualBCI."
        )
        with UdpManualIntentSource(args.intent_host, args.intent_port, timeout_s=0.10) as source:
            while deadline is None or time.monotonic() < deadline:
                now = time.monotonic()
                intent = source.receive()
                if intent is not None:
                    if intent.session_id != fixture.session_id:
                        print(f"Ignoring manual intent from another game session: {intent.session_id}")
                        continue
                    if intent.calibration_id and fixture.calibration_id and intent.calibration_id != fixture.calibration_id:
                        print(f"Ignoring manual intent from another calibration: {intent.calibration_id}")
                        continue
                    seq += 1
                    event = manual_selection_event(
                        seq=seq,
                        session_id=fixture.session_id or intent.session_id,
                        calibration_id=fixture.calibration_id,
                        intent=intent,
                    )
                    sink.send(event)
                    print(f"manual #{seq:03d} ACCEPT {intent.target.value.upper()}")
                    next_idle = now + args.heartbeat_seconds

                now = time.monotonic()
                if now >= next_idle:
                    seq += 1
                    idle = manual_idle_event(
                        seq=seq,
                        session_id=fixture.session_id or "manual",
                        calibration_id=fixture.calibration_id,
                    )
                    sink.send(idle)
                    next_idle = now + args.heartbeat_seconds
    except KeyboardInterrupt:
        print("\nManual development service interrupted.")
    finally:
        sink.close()


def run_marker_log(args: argparse.Namespace) -> None:
    output = Path(args.output) if args.output else None
    if output is not None:
        output.parent.mkdir(parents=True, exist_ok=True)
    deadline = time.monotonic() + args.seconds if args.seconds > 0 else None
    print(f"Listening for Unity GameMarker mirror on udp://{args.host}:{args.port}")
    with UdpGameMarkerSource(args.host, args.port, timeout_s=0.25) as source:
        try:
            while deadline is None or time.monotonic() < deadline:
                marker = source.receive()
                if marker is None:
                    continue
                line = marker.to_json()
                print(line)
                if output is not None:
                    with output.open("a", encoding="utf-8") as handle:
                        handle.write(line + "\n")
        except KeyboardInterrupt:
            print("\nMarker logging interrupted.")


def _add_calibration_options(parser: argparse.ArgumentParser) -> None:
    parser.add_argument("--calibrate", action="store_true",
                        help="complete the real Unity Awakening handshake with declared development provenance")
    parser.add_argument("--marker-host", default="127.0.0.1")
    parser.add_argument("--marker-port", type=int, default=19743,
                        help="primary Unity GameMarker processing lane")
    parser.add_argument("--calibration-timeout", type=float, default=45.0,
                        help="seconds to wait for the complete Awakening protocol; 0 means indefinitely")


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--host", default="127.0.0.1")
    parser.add_argument("--port", type=int, default=19742, help="NeuralEvent UDP port")
    sub = parser.add_subparsers(dest="command", required=True)

    decision = sub.add_parser("decision", help="emit deterministic decision-level BCI events")
    decision.add_argument("--script", default="sight:3,guard:3,abstain:1",
                          help="comma-separated state:seconds sequence")
    decision.add_argument("--hz", type=float, default=4.0)
    decision.add_argument("--repeat", type=int, default=1)
    decision.add_argument("--seed", type=int, default=17)
    decision.add_argument("--confidence", type=float, default=0.86)
    decision.add_argument("--quality", type=float, default=0.91)
    decision.add_argument("--jitter", type=float, default=0.035)
    decision.add_argument("--authority-ttl-ms", type=int, default=900)
    decision.add_argument("--session-id", default=None)
    decision.add_argument("--output-tape", default=None)
    _add_calibration_options(decision)
    decision.set_defaults(func=run_decision)

    replay = sub.add_parser("replay", help="replay a NeuralEvent tape through the production UDP boundary")
    replay.add_argument("tape")
    replay.add_argument("--speed", type=float, default=1.0)
    replay.add_argument("--session-id", default=None)
    _add_calibration_options(replay)
    replay.set_defaults(func=run_replay)

    fixture = sub.add_parser(
        "calibration-fixture",
        help="exercise Awakening with declared development calibration provenance",
    )
    fixture.add_argument(
        "--source-mode",
        choices=(SourceMode.MANUAL.value, SourceMode.SIMULATED_DECISION.value, SourceMode.DECISION_REPLAY.value),
        default=SourceMode.SIMULATED_DECISION.value,
    )
    fixture.add_argument("--marker-host", default="127.0.0.1")
    fixture.add_argument("--marker-port", type=int, default=19743)
    fixture.add_argument("--calibration-timeout", type=float, default=45.0)
    fixture.set_defaults(func=run_calibration_fixture)

    manual = sub.add_parser(
        "manual-service",
        help="S0: Awakening fixture + sequenced Q/E manual-intent adapter + safe idle liveness",
    )
    manual.add_argument("--marker-host", default="127.0.0.1")
    manual.add_argument("--marker-port", type=int, default=19743)
    manual.add_argument("--intent-host", default="127.0.0.1")
    manual.add_argument("--intent-port", type=int, default=19746)
    manual.add_argument("--calibration-timeout", type=float, default=45.0)
    manual.add_argument("--heartbeat-seconds", type=float, default=0.5)
    manual.add_argument("--seconds", type=float, default=0.0, help="0 means until Ctrl-C")
    manual.set_defaults(func=run_manual_service)

    marker = sub.add_parser("marker-log", help="listen to the passive Unity GameMarker mirror")
    marker.add_argument("--host", default="127.0.0.1")
    marker.add_argument("--port", type=int, default=19745,
                        help="passive GameMarker observer port; 19743 is reserved for the active processing consumer")
    marker.add_argument("--output", default=None)
    marker.add_argument("--seconds", type=float, default=0.0, help="0 means until Ctrl-C")
    marker.set_defaults(func=run_marker_log)
    return parser


def main() -> None:
    parser = build_parser()
    args = parser.parse_args()
    if getattr(args, "hz", 1.0) <= 0:
        parser.error("--hz must be > 0")
    if getattr(args, "speed", 1.0) <= 0:
        parser.error("--speed must be > 0")
    if getattr(args, "repeat", 1) <= 0:
        parser.error("--repeat must be > 0")
    if getattr(args, "calibration_timeout", 1.0) < 0:
        parser.error("--calibration-timeout must be >= 0")
    if getattr(args, "heartbeat_seconds", 1.0) <= 0:
        parser.error("--heartbeat-seconds must be > 0")
    args.func(args)


if __name__ == "__main__":
    main()
