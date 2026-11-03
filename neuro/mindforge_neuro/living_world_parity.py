"""Offline comparison of Unity gameplay marker semantics, passive OFF versus ON.

This is a regression *auditor*, not evidence of identical Unity physics/timing.
Inputs must be captured with identical deterministic input and neural replay.
"""
from __future__ import annotations

import argparse
from dataclasses import dataclass, asdict
import hashlib
import json
from pathlib import Path
from typing import Iterable

from .markers import GameMarker

MAX_MARKERS = 250_000
FIELDS = (
    "event", "category", "stage", "action", "target", "reason",
    "value", "boss_phase", "stimulus_epoch", "planned_duration_s",
)


def canonical_marker(marker: GameMarker) -> tuple:
    """Drop run-specific identity, realtime/frame/seq and preserve event meaning."""
    return (
        marker.event, marker.category, marker.stage, marker.action,
        marker.target, marker.reason,
        round(marker.value, 3), marker.boss_phase,
        marker.stimulus_epoch, round(marker.planned_duration_s, 3),
    )


def load_marker_trace(path: Path) -> list[tuple]:
    """Read bounded, serialized GameMarker v1 JSONL, never arbitrary Python objects."""
    trace = []
    with path.open("r", encoding="utf-8") as stream:
        for number, line in enumerate(stream, 1):
            if not line.strip():
                continue
            if len(trace) >= MAX_MARKERS:
                raise ValueError("marker trace exceeds maximum length")
            try:
                marker = GameMarker.from_json(line)
                if marker.schema != "mindforge.game_marker.v1":
                    raise ValueError("legacy marker does not qualify for v3.1 parity")
                trace.append(canonical_marker(marker))
            except (ValueError, KeyError, TypeError) as exc:
                raise ValueError(f"bad game marker at line {number}") from exc
    if not trace:
        raise ValueError("marker trace is empty")
    return trace


def _digest(trace: Iterable[tuple]) -> str:
    payload = json.dumps(list(trace), separators=(",", ":"), ensure_ascii=True, allow_nan=False)
    return hashlib.sha256(payload.encode("utf-8")).hexdigest()


def compare_semantics(off: list[tuple], on: list[tuple]) -> dict:
    """Strict order-sensitive semantic parity, no invented human/Unity evidence."""
    if not off or not on:
        raise ValueError("both runs must contain game markers")
    first = next((i for i, (a, b) in enumerate(zip(off, on)) if a != b), None)
    if first is None and len(off) != len(on):
        first = min(len(off), len(on))
    mismatched_fields = []
    if first is not None and first < min(len(off), len(on)):
        mismatched_fields = [
            name for name, a, b in zip(FIELDS, off[first], on[first]) if a != b
        ]
    return {
        "schema": "mindforge.living_world_semantic_parity.v1",
        "pass": first is None,
        "off_marker_count": len(off),
        "on_marker_count": len(on),
        "off_digest": _digest(off),
        "on_digest": _digest(on),
        "first_mismatch_index": first,
        "mismatched_fields": mismatched_fields,
        "physical_time_parity_proven": False,
        "render_parity_proven": False,
        "raw_eeg_used": False,
    }


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--off", type=Path, required=True, help="Passive-disabled GameMarker JSONL")
    parser.add_argument("--on", type=Path, required=True, help="Passive-enabled GameMarker JSONL")
    parser.add_argument("--output", type=Path, help="Derived-only summary JSON")
    args = parser.parse_args(argv)
    report = compare_semantics(load_marker_trace(args.off), load_marker_trace(args.on))
    encoded = json.dumps(report, sort_keys=True, indent=2) + "\n"
    if args.output:
        args.output.parent.mkdir(parents=True, exist_ok=True)
        args.output.write_text(encoded, encoding="utf-8")
    print(encoded, end="")
    return 0 if report["pass"] else 2


if __name__ == "__main__":
    raise SystemExit(main())
