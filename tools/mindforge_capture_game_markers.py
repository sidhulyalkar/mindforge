#!/usr/bin/env python3
"""Capture Unity's observer GameMarker lane for Living World v3.1 parity studies.

Captures GameMarker v1 only, never EEG. Start immediately before a controller
input/decision replay and compare a baseline run with a passive-enabled run.
"""
from __future__ import annotations

import argparse
import json
from pathlib import Path
import sys
import time
from typing import Protocol

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "neuro"))

from mindforge_neuro.markers import GAME_MARKER_V1, GameMarker, UdpGameMarkerSource  # noqa: E402


class MarkerReceiver(Protocol):
    def receive(self) -> GameMarker | None: ...


def collect_markers(
    source: MarkerReceiver, *, seconds: float = 60.0,
    limit: int = 100000,
) -> list[GameMarker]:
    """Bounded capture from one Unity game session. Never merges unrelated sessions."""
    if not 0 < seconds <= 3600 or not 1 <= limit <= 250000:
        raise ValueError("invalid capture time or record limit")
    cutoff = time.monotonic() + seconds
    records: list[GameMarker] = []
    session: str | None = None
    last_seq = -1
    while time.monotonic() < cutoff and len(records) < limit:
        marker = source.receive()
        if marker is None or marker.schema != GAME_MARKER_V1 or not marker.session_id:
            continue
        if session is None:
            session = marker.session_id
        if marker.session_id != session or marker.seq <= last_seq:
            continue
        records.append(marker)
        last_seq = marker.seq
    return records


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", type=Path, required=True, help="Write local GameMarker JSONL")
    parser.add_argument("--seconds", type=float, default=60.)
    parser.add_argument("--limit", type=int, default=100000)
    parser.add_argument("--port", type=int, default=19745, help="Mindforge observer port, default 19745")
    args = parser.parse_args(argv)
    if not (0 < args.seconds <= 3600 and 1 <= args.limit <= 250000 and 1024 <= args.port <= 65535):
        parser.error("invalid time, limit or port")

    # An observation-port receiver never owns the gameplay command UDP port.
    with UdpGameMarkerSource(host="127.0.0.1", port=args.port, timeout_s=0.15) as source:
        records = collect_markers(source, seconds=args.seconds, limit=args.limit)
    if not records:
        print("No Unity GameMarker v1 packets captured. Check Unity play mode and observer port.",
              file=sys.stderr)
        return 2
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(
        "".join(marker.to_json() + "\n" for marker in records),
        encoding="utf-8",
    )
    print(json.dumps({
        "schema": "mindforge.game_marker_capture.v1",
        "records": len(records),
        "session_count": 1,
        "output": str(args.output),
        "no_raw_eeg": True,
        "passive_transport_was_not_modified": True,
    }, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
