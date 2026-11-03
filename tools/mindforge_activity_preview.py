#!/usr/bin/env python3
"""Opt-in read-only Silksong activity -> cosmetic context preview.

Native game BepInEx observation (:19756) -> qualified-as-UNQUALIFIED activity
policy -> context packet (:19754). The existing context sidecar then publishes
presentation-only orb mode to the installed cosmetic overlay (:19755).

No source in this pipeline is allowed to claim enemy engagement, incoming boss
attacks, safe recovery windows, or EEG selections. Requires explicit build pin
for the native path; all modes are simulation or unqualified instrumentation.
"""
from __future__ import annotations

import argparse
import json
from pathlib import Path
import socket
import time

from mindforge_neuro.activity_preview import ActivityPreviewBridge
from mindforge_neuro.game_observation import GameObservation


DEMO_EVENTS = (
    (0.00, "hero_ready"),
    (0.25, "hero_attack"),
    (0.50, "hero_attack"),
    (0.80, "heartbeat"),
    (1.10, "hero_damaged"),
    (1.55, "hero_attack"),
    (1.80, "hero_attack"),
    (2.15, "scene_changed"),
    (2.60, "hero_ready"),
    (2.85, "hero_attack"),
    (3.10, "hero_attack"),
    (3.40, "hero_die"),
)


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--game", choices=("silksong",), required=True)
    parser.add_argument("--input-port", type=int, default=19756)
    parser.add_argument("--context-port", type=int, default=19754)
    parser.add_argument("--expected-build", help="Exact locally observed Application.version for native mode")
    parser.add_argument("--demo", action="store_true", help="Explicitly synthetic actions, no game required")
    parser.add_argument("--dry-run", action="store_true", help="Print rather than send context")
    parser.add_argument("--seconds", type=float, default=8.0)
    parser.add_argument("--hz", type=float, default=15.0)
    parser.add_argument("--tape", help="Optional derived observation JSONL for local audit")
    parser.add_argument("--report", help="Optional telemetry summary JSON path")
    args = parser.parse_args()
    reserved = {19742, 19747, 19752, 19753, 19754, 19755, 19756}
    if not 1024 <= args.input_port <= 65535 or not 1024 <= args.context_port <= 65535:
        parser.error("invalid UDP port")
    if args.input_port == args.context_port:
        parser.error("source and context destinations must differ")
    if args.input_port in reserved - {19756} or args.context_port in reserved - {19754}:
        parser.error("port would conflict with another Mindforge pipeline")
    if not 1 <= args.hz <= 30 or not 0 < args.seconds <= 600:
        parser.error("invalid duration / output rate")
    if not args.demo and not args.expected_build:
        parser.error("native preview requires --expected-build (pin your installed game)")
    preview = ActivityPreviewBridge(args.game)
    listener = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    emitter = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    accepted: list[str] = []
    try:
        if not args.demo:
            listener.bind(("127.0.0.1", args.input_port))
            listener.setblocking(False)
        print("SYNTHETIC GAME ACTIVITY: no game, EEG or combat-state inference"
              if args.demo else
              f"UNQUALIFIED Silksong hook observations on 127.0.0.1:{args.input_port}",
              flush=True)
        start = time.monotonic()
        demo_seq = 0
        demo_index = 0
        previous = None
        while time.monotonic() - start <= args.seconds:
            tick = time.monotonic()
            elapsed = tick - start
            if args.demo:
                while demo_index < len(DEMO_EVENTS) and elapsed >= DEMO_EVENTS[demo_index][0]:
                    time_offset, name = DEMO_EVENTS[demo_index]
                    demo_index += 1
                    demo_seq += 1
                    sample = GameObservation.make(
                        args.game, name, demo_seq,
                        session_id="synthetic-activity-session",
                        source_time_s=time_offset,
                        in_game=name not in ("hero_die", "scene_changed"),
                    )
                    if preview.accept(sample, tick):
                        accepted.append(sample.to_json())
            else:
                for _ in range(32):
                    try:
                        raw, peer = listener.recvfrom(8192)
                    except BlockingIOError:
                        break
                    if peer[0] != "127.0.0.1" or len(raw) > 4096:
                        continue
                    try:
                        sample = GameObservation.from_json(raw)
                        if sample.game_version != args.expected_build:
                            preview.audit.rejected["wrong_game_build"] += 1
                            continue
                        if sample.source_mode != "observed_game_hook_unqualified":
                            preview.audit.rejected["non_native_source"] += 1
                            continue
                        if preview.accept(sample, time.monotonic()):
                            accepted.append(sample.to_json())
                    except (ValueError, UnicodeError, TypeError):
                        preview.audit.rejected["invalid_wire_packet"] += 1
            frame = preview.snapshot(time.monotonic())
            if frame is not None:
                state = "primed" if frame.context.player_activity_recent else "hidden"
                if state != previous:
                    print(f"{state:7s} [{frame.source}] no combat inference",
                          flush=True)
                    previous = state
                if args.dry_run:
                    print(frame.to_json(), flush=True)
                else:
                    emitter.sendto(frame.to_json().encode("utf-8"),
                                   ("127.0.0.1", args.context_port))
            rest = (1. / args.hz) - (time.monotonic() - tick)
            if rest > 0:
                time.sleep(rest)
    except KeyboardInterrupt:
        pass
    finally:
        listener.close()
        emitter.close()
        if args.tape:
            path = Path(args.tape)
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text("\n".join(accepted) + ("\n" if accepted else ""), encoding="utf-8")
        report = preview.audit_report()
        if args.report:
            path = Path(args.report)
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text(json.dumps(report, indent=2, sort_keys=True) + "\n",
                            encoding="utf-8")
        print(json.dumps(report, indent=2, sort_keys=True), flush=True)


if __name__ == "__main__":
    main()
