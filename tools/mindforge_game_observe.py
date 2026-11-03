#!/usr/bin/env python3
"""Read-only Silksong native-hook audit. No combat or SSVEP authority.

Local simulation:
    python tools/mindforge_game_observe.py --game silksong --demo

Actual opted-in native plugin (same Windows PC):
    python tools/mindforge_game_observe.py --game silksong --seconds 90 \
        --output experiments/game-observation/silksong-local.jsonl

This monitor cannot validate that a hook fires correctly without a game capture.
"""
from __future__ import annotations

import argparse
import json
from pathlib import Path
import socket
import time

from mindforge_neuro.game_observation import GameObservation, ObservationMonitor


def example_events(game: str) -> list[GameObservation]:
    seq = (("hero_ready", "heartbeat", "hero_jump", "hero_attack",
            "hero_dash", "hero_damaged", "hero_die", "scene_changed")
           if game == "silksong" else
           ("hero_ready", "heartbeat", "hero_attack", "hero_attack",
            "scene_changed"))
    return [
        GameObservation.make(game, event, i + 1, source_time_s=i * 0.25,
                             in_game=event not in ("hero_die", "scene_changed"),
                             anchor=(0.5, 0.5) if event not in ("hero_die", "scene_changed") else None)
        for i, event in enumerate(seq)
    ]


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--game", choices=("silksong", "hollow_knight"), required=True)
    parser.add_argument("--listen-port", type=int, default=19756)
    parser.add_argument("--seconds", type=float, default=90.0)
    parser.add_argument("--demo", action="store_true")
    parser.add_argument("--output", help="Opt-in JSONL recording of derived game observations")
    parser.add_argument("--report", help="Optional JSON audit summary path")
    parser.add_argument("--expected-build", help="Reject events whose game_version does not match")
    args = parser.parse_args()
    if not 1024 <= args.listen_port <= 65535 or not 0 < args.seconds <= 3600:
        parser.error("bad listen port or duration")
    if args.listen_port in (19742, 19743, 19745, 19747, 19752, 19753, 19754, 19755):
        parser.error("observation port must remain separate from game and EEG ports")

    monitor = ObservationMonitor(args.game)
    observed: list[str] = []
    if args.demo:
        print("SYNTHETIC observation records; no native game hooks or EEG", flush=True)
        for i, frame in enumerate(example_events(args.game)):
            assert monitor.accept(frame, float(i))
            observed.append(frame.to_json())
    else:
        print(f"Listening for read-only game observations on 127.0.0.1:{args.listen_port}",
              flush=True)
        print("Unqualified instrumentation, NOT verified boss engagement or safe SSVEP.", flush=True)
        listener = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        try:
            listener.bind(("127.0.0.1", args.listen_port))
            listener.settimeout(0.2)
            deadline = time.monotonic() + args.seconds
            while time.monotonic() < deadline:
                try:
                    payload, peer = listener.recvfrom(8192)
                except socket.timeout:
                    continue
                if peer[0] != "127.0.0.1" or len(payload) > 4096:
                    continue
                try:
                    frame = GameObservation.from_json(payload)
                    if args.expected_build and frame.game_version != args.expected_build:
                        monitor.rejected["wrong_game_build"] += 1
                        continue
                    if monitor.accept(frame, time.monotonic()):
                        observed.append(frame.to_json())
                        print(f"#{frame.seq}: {frame.event} ({frame.source_mode})", flush=True)
                except (ValueError, UnicodeError, TypeError):
                    monitor.rejected["invalid_wire_packet"] += 1
        except KeyboardInterrupt:
            print("Observation interrupted.", flush=True)
        finally:
            listener.close()

    summary = monitor.report()
    if args.output:
        output = Path(args.output)
        output.parent.mkdir(parents=True, exist_ok=True)
        output.write_text("\n".join(observed) + ("\n" if observed else ""), encoding="utf-8")
    if args.report:
        report = Path(args.report)
        report.parent.mkdir(parents=True, exist_ok=True)
        report.write_text(json.dumps(summary, indent=2, sort_keys=True) + "\n",
                          encoding="utf-8")
    print(json.dumps(summary, sort_keys=True, indent=2))


if __name__ == "__main__":
    main()
