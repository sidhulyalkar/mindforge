#!/usr/bin/env python3
"""Mindforge BCI game-development harness.

Examples
--------
Decision-level source, no EEG required::

    python tools/mindforge_dev.py decision --script sight:3,guard:3,abstain:1

Replay an exact decision tape::

    python tools/mindforge_dev.py replay experiments/tapes/demo.jsonl --speed 1.0

Record Unity-originated game markers::

    python tools/mindforge_dev.py marker-log --output experiments/markers/run.jsonl

The harness only substitutes declared layers. ``decision`` never claims to be EEG,
and ``replay`` identifies itself as decision replay in every emitted NeuralEvent.
"""
from __future__ import annotations

import argparse
import json
import time
from dataclasses import replace
from pathlib import Path

from mindforge_neuro.dev_sources import DecisionSimulationConfig, DecisionSimulator, NeuralEventTape, TapeEntry
from mindforge_neuro.events import NeuralEvent
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


def run_decision(args: argparse.Namespace) -> None:
    script = _parse_script(args.script)
    config = DecisionSimulationConfig(
        seed=args.seed,
        confidence_mean=args.confidence,
        quality_mean=args.quality,
        jitter=args.jitter,
        authority_ttl_ms=args.authority_ttl_ms,
    )
    simulator = DecisionSimulator(config, session_id=args.session_id)
    sink = UdpEventSink(args.host, args.port)
    period = 1.0 / args.hz
    started = time.monotonic()
    tape: list[TapeEntry] = []
    print(f"Decision simulator -> udp://{args.host}:{args.port} session={simulator.session_id}")
    try:
        for repeat in range(args.repeat):
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
        if args.output_tape:
            NeuralEventTape(tape).save(args.output_tape)
            print(f"Tape written: {args.output_tape}")


def run_replay(args: argparse.Namespace) -> None:
    tape = NeuralEventTape.load(args.tape)
    if not tape.entries:
        raise SystemExit("Replay tape is empty")
    sink = UdpEventSink(args.host, args.port)
    previous_offset = 0.0
    print(f"Decision replay -> udp://{args.host}:{args.port} entries={len(tape.entries)} speed={args.speed:g}x")
    try:
        for entry in tape.replay_events(session_id=args.session_id):
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


def run_marker_log(args: argparse.Namespace) -> None:
    output = Path(args.output) if args.output else None
    if output is not None:
        output.parent.mkdir(parents=True, exist_ok=True)
    deadline = time.monotonic() + args.seconds if args.seconds > 0 else None
    print(f"Listening for Unity GameMarker on udp://{args.host}:{args.port}")
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
    decision.set_defaults(func=run_decision)

    replay = sub.add_parser("replay", help="replay a NeuralEvent tape through the production UDP boundary")
    replay.add_argument("tape")
    replay.add_argument("--speed", type=float, default=1.0)
    replay.add_argument("--session-id", default=None)
    replay.set_defaults(func=run_replay)

    marker = sub.add_parser("marker-log", help="listen to the Unity -> Python GameMarker channel")
    marker.add_argument("--host", default="127.0.0.1")
    marker.add_argument("--port", type=int, default=19743)
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
    args.func(args)


if __name__ == "__main__":
    main()
