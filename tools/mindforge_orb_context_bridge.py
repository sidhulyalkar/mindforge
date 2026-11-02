#!/usr/bin/env python3
"""Standalone loopback-only contextual orb state sidecar.

No game hooks or neural decoder are installed by this command.
Demo: python tools/mindforge_orb_context_bridge.py --game silksong --demo
Live source: game context v1 -> UDP 19754 -> bridge -> UDP 19755 -> BepInEx.
"""
from __future__ import annotations

import argparse
import json
import socket
import time

from mindforge_neuro.context_transport import (
    GameContextPacket, OrbContextBridge, make_context_packet,
)
from mindforge_neuro.mod_bridge import SUPPORTED_GAMES

SCENARIOS = (
    ("exploration", {}),
    ("approach", {"enemy_nearby": True}),
    ("engage", {"enemy_engaged": True}),
    ("incoming", {"enemy_engaged": True, "incoming_attack": True}),
    ("stagger", {"boss_encounter": True, "recovery_opening": True,
                 "opening_duration_s": 3.0}),
    ("precision", {"boss_encounter": True, "precision_window": True}),
    ("finish", {}),
    ("paused", {"paused": True}),
)


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--game", required=True, choices=sorted(SUPPORTED_GAMES))
    parser.add_argument("--input-port", type=int, default=19754)
    parser.add_argument("--output-port", type=int, default=19755)
    parser.add_argument("--demo", action="store_true")
    parser.add_argument("--seconds", type=float, default=24.0)
    parser.add_argument("--hz", type=float, default=15.0)
    args = parser.parse_args()
    if (not 1024 <= args.input_port <= 65535
            or not 1024 <= args.output_port <= 65535
            or args.input_port == args.output_port or args.input_port in (19742, 19747, 19752, 19753)
            or args.output_port in (19742, 19747, 19752, 19753)
            or not 1 <= args.hz <= 30 or not 0 < args.seconds <= 600):
        parser.error("invalid ports, frame rate or demo duration")
    bridge = OrbContextBridge(args.game)
    receiver = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    sender = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    try:
        if not args.demo:
            receiver.bind(("127.0.0.1", args.input_port))
            receiver.setblocking(False)
        print(f"Mindforge context output -> 127.0.0.1:{args.output_port}", flush=True)
        print("SYNTHETIC GAME CONTEXT, not observed gameplay" if args.demo else
              f"Game-context input <- 127.0.0.1:{args.input_port}", flush=True)
        started = time.monotonic()
        prev_mode = None
        sequence = 0
        while True:
            frame_start = time.monotonic()
            if args.demo:
                elapsed = frame_start - started
                if elapsed > args.seconds:
                    break
                name, flags = SCENARIOS[int(elapsed / 2.5) % len(SCENARIOS)]
                sequence += 1
                packet = make_context_packet(
                    args.game, "synthetic-combat-demo", sequence,
                    source="synthetic_game_context", ttl_ms=400, **flags)
                bridge.accept(packet, frame_start)
            else:
                for _ in range(32):
                    try:
                        data, peer = receiver.recvfrom(8192)
                    except BlockingIOError:
                        break
                    if peer[0] != "127.0.0.1" or len(data) > 4096:
                        continue
                    try:
                        packet = GameContextPacket.from_json(data)
                        bridge.accept(packet, time.monotonic())
                    except (ValueError, TypeError, KeyError, UnicodeError):
                        continue
            state = bridge.snapshot(time.monotonic())
            sender.sendto(json.dumps(state, sort_keys=True,
                                     separators=(",", ":"), allow_nan=False).encode("utf-8"),
                          ("127.0.0.1", args.output_port))
            if state["mode"] != prev_mode:
                prev_mode = state["mode"]
                print(f"{state['mode']:8s} {state['placement']:13s} "
                      f"source={state['source']} reason={state['reason']}", flush=True)
            remain = (1.0 / args.hz) - (time.monotonic() - frame_start)
            if remain > 0:
                time.sleep(remain)
    except KeyboardInterrupt:
        pass
    finally:
        receiver.close()
        sender.close()


if __name__ == "__main__":
    main()
