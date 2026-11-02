#!/usr/bin/env python3
"""Local-only portable mod bridge; all output is cosmetic and derived-only.

Synthetic no-headset demo:
    python tools/mindforge_mod_bridge.py --game silksong --demo --seconds 20

Separate terminal (non-demo mode):
    python tools/mindforge_mod_bridge.py --game cuphead
    python tools/mindforge_dev.py decision --port 19752 --script sight:3,guard:3,abstain:2
"""
from __future__ import annotations

import argparse
import json
import socket
import time

from mindforge_neuro.dev_sources import DecisionSimulationConfig, DecisionSimulator
from mindforge_neuro.events import NeuralEvent
from mindforge_neuro.mod_bridge import ModBridgeState, SUPPORTED_GAMES

DEFAULT_INPUT_PORT = 19752  # distinct from Unity NeuralEvent port 19742
DEFAULT_OUTPUT_PORT = 19753  # cosmetic state, never raw EEG or game authority


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--game", choices=sorted(SUPPORTED_GAMES), required=True)
    parser.add_argument("--input-port", type=int, default=DEFAULT_INPUT_PORT)
    parser.add_argument("--output-port", type=int, default=DEFAULT_OUTPUT_PORT)
    parser.add_argument("--demo", action="store_true", help="simulated decisions, NOT measured EEG")
    parser.add_argument("--seconds", type=float, default=20.0, help="demo duration")
    parser.add_argument("--hz", type=float, default=10.0, help="cosmetic frame rate")
    args = parser.parse_args()
    if not (1024 <= args.input_port <= 65535 and 1024 <= args.output_port <= 65535):
        parser.error("ports must be in 1024..65535")
    if args.input_port == args.output_port:
        parser.error("input and output ports must differ")
    if args.hz < 1 or args.hz > 30 or args.seconds <= 0:
        parser.error("1 <= hz <= 30 and seconds > 0 required")

    state = ModBridgeState(args.game)
    demo = DecisionSimulator(DecisionSimulationConfig(seed=17), session_id="mod-demo") if args.demo else None
    script = ("sight", "abstain", "guard", "abstain", "lost", "recovered")
    source = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    output = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    try:
        if demo is None:
            source.bind(("127.0.0.1", args.input_port))
            source.setblocking(False)
        print(f"Mindforge mod bridge [{args.game}] => 127.0.0.1:{args.output_port}")
        print("Mode: synthetic decision fixture (NOT EEG)" if demo else
              f"NeuralEvent v2 input: 127.0.0.1:{args.input_port}")
        start = time.monotonic()
        last_script_step = -1
        old_link = None
        period = 1.0 / args.hz
        while True:
            frame_start = time.monotonic()
            if demo is not None:
                elapsed = frame_start - start
                if elapsed >= args.seconds:
                    break
                step = int(elapsed // 2.0)
                if step != last_script_step:
                    last_script_step = step
                    state.accept(demo.next(script[step % len(script)]), frame_start)
            else:
                # Bounded drain on the bridge process, never a backlog replay.
                for _ in range(64):
                    try:
                        payload, peer = source.recvfrom(8192)
                    except BlockingIOError:
                        break
                    if peer[0] != "127.0.0.1" or len(payload) > 4096:
                        continue
                    try:
                        event = NeuralEvent.from_json(payload)
                        state.accept(event, time.monotonic())
                    except (ValueError, KeyError, UnicodeError, TypeError):
                        continue

            snapshot = state.snapshot(time.monotonic())
            output.sendto(json.dumps(snapshot, separators=(",", ":")).encode("utf-8"),
                          ("127.0.0.1", args.output_port))
            if snapshot["link"] != old_link:
                old_link = snapshot["link"]
                print(f"BCI link: {old_link}, source={snapshot['source_mode']}")
            sleep = period - (time.monotonic() - frame_start)
            if sleep > 0:
                time.sleep(sleep)
    except KeyboardInterrupt:
        pass
    finally:
        # With UDP loss this stop frame is best effort; consumer has its own TTL.
        source.close()
        output.close()


if __name__ == "__main__":
    main()
