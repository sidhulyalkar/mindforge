#!/usr/bin/env python3
"""Read-only Cuphead stage observation auditor; no combat/SSVEP authority.

Native mode listens exclusively on 127.0.0.1:19757 for the OFF-by-default
Cuphead BepInEx stage observer. A version pin is mandatory in native mode.
"""
from __future__ import annotations

import argparse
import json
from pathlib import Path
import socket
import time

from mindforge_neuro.cuphead_stage import CupheadStageMonitor, CupheadStageObservation, SOURCE

DEMO = (
    (0.00, "unknown", "sample"),
    (0.45, "map", "sample"),
    (0.95, "unknown", "scene_changed"),
    (1.35, "level", "sample"),
    (1.95, "level", "sample"),
    (2.45, "unknown", "scene_changed"),
    (2.95, "map", "sample"),
)


def main() -> None:
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument("--demo", action="store_true",
                   help="Explicit synthetic records, not a running game")
    p.add_argument("--seconds", type=float, default=120.)
    p.add_argument("--listen-port", type=int, default=19757)
    p.add_argument("--expected-build", help="Exact local Cuphead Application.version")
    p.add_argument("--output", type=Path, help="Opt-in accepted derived JSONL capture")
    p.add_argument("--report", type=Path, help="Optional JSON audit report")
    args = p.parse_args()
    if not 1024 <= args.listen_port <= 65535 or not 0 < args.seconds <= 1800:
        p.error("invalid local UDP port or duration")
    if args.listen_port in (19742, 19747, 19752, 19753, 19754, 19755, 19756):
        p.error("port collides with existing Mindforge protocol")
    if not args.demo and not args.expected_build:
        p.error("native Cuphead source requires --expected-build")
    audit = CupheadStageMonitor()
    recorded: list[str] = []
    if args.demo:
        print("SYNTHETIC CUPHEAD STAGE TAPE; not measured gameplay or neural control",
              flush=True)
        for i, (_, stage, event) in enumerate(DEMO, 1):
            obs = CupheadStageObservation.synthetic(
                stage, i, event=event, source_time_s=DEMO[i - 1][0])
            assert audit.accept(obs, DEMO[i - 1][0])
            recorded.append(obs.to_json())
        now = DEMO[-1][0]
    else:
        listener = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        try:
            listener.bind(("127.0.0.1", args.listen_port))
            listener.settimeout(0.25)
            print(f"Unqualified Cuphead stage telemetry 127.0.0.1:{args.listen_port}",
                  flush=True)
            start = time.monotonic()
            while time.monotonic() - start <= args.seconds:
                try:
                    data, peer = listener.recvfrom(8192)
                except socket.timeout:
                    continue
                if peer[0] != "127.0.0.1" or len(data) > 4096:
                    audit.rejections["invalid_peer_or_size"] += 1
                    continue
                try:
                    obs = CupheadStageObservation.from_json(data)
                    if obs.source != SOURCE:
                        audit.rejections["non_native_source"] += 1
                        continue
                    if obs.game_version != args.expected_build:
                        audit.rejections["wrong_game_build"] += 1
                        continue
                    if audit.accept(obs, time.monotonic()):
                        recorded.append(obs.to_json())
                        if obs.event == "scene_changed":
                            print("scene_changed -> unknown", flush=True)
                        else:
                            print(f"#{obs.seq} stage={obs.stage} (not combat evidence)",
                                  flush=True)
                except (TypeError, ValueError, UnicodeError):
                    audit.rejections["invalid_payload"] += 1
        except KeyboardInterrupt:
            pass
        finally:
            listener.close()
        now = time.monotonic()
    report = audit.report(now)
    if args.output:
        args.output.parent.mkdir(parents=True, exist_ok=True)
        args.output.write_text("\n".join(recorded) + ("\n" if recorded else ""),
                               encoding="utf-8")
    if args.report:
        args.report.parent.mkdir(parents=True, exist_ok=True)
        args.report.write_text(json.dumps(report, indent=2, sort_keys=True) + "\n",
                               encoding="utf-8")
    print(json.dumps(report, indent=2, sort_keys=True), flush=True)


if __name__ == "__main__":
    main()
