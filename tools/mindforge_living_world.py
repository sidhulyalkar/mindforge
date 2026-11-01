#!/usr/bin/env python3
"""Mindforge Living World: derived-only, optional UDP demo and deterministic replay."""
from __future__ import annotations

import argparse
import json
from pathlib import Path
import sys
import time
import uuid

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "neuro"))

from mindforge_neuro.passive_features import (  # noqa: E402
    DEFAULT_PORT, NeuralFeatureFrame, PassiveUdpEmitter, demo_frames,
)


def read_tape(path: Path) -> list[tuple[float, NeuralFeatureFrame]]:
    out = []
    with path.open(encoding="utf-8") as stream:
        for line in stream:
            if not line.strip():
                continue
            row = json.loads(line)
            if not isinstance(row, dict) or set(row) != {"offset_s", "frame"}:
                raise ValueError("invalid tape record structure")
            offset = float(row["offset_s"])
            if not 0 <= offset <= 86400 or (out and offset < out[-1][0]):
                raise ValueError("tape offsets must be sorted and bounded")
            frame = NeuralFeatureFrame.from_dict(row["frame"])
            out.append((offset, frame))
            if len(out) > 100000:
                raise ValueError("tape exceeds maximum record count")
    if not out:
        raise ValueError("tape is empty")
    return out


def write_tape(path: Path, records: list[tuple[float, NeuralFeatureFrame]]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", encoding="utf-8") as f:
        for offset, frame in records:
            f.write(json.dumps({"offset_s": offset, "frame": json.loads(frame.to_json())},
                               sort_keys=True, allow_nan=False) + "\n")


def run(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    source = parser.add_mutually_exclusive_group(required=True)
    source.add_argument("--demo", action="store_true", help="Synthetic derived features, not EEG")
    source.add_argument("--replay", type=Path, help="Replay a previous derived-only JSONL tape")
    parser.add_argument("--seconds", type=float, default=20.)
    parser.add_argument("--hz", type=float, default=5.)
    parser.add_argument("--seed", type=int, default=17)
    parser.add_argument("--port", type=int, default=DEFAULT_PORT)
    parser.add_argument("--speed", type=float, default=1., help="Replay time multiplier")
    parser.add_argument("--dry-run", action="store_true", help="Save/inspect without UDP or delays")
    parser.add_argument("--output-tape", type=Path)
    args = parser.parse_args(argv)
    if not 0 < args.seconds <= 3600 or not 0 < args.hz <= 20 or not 0.1 <= args.speed <= 20:
        parser.error("seconds, hz or speed outside supported bounds")
    if args.demo:
        steps = max(1, int(args.seconds * args.hz))
        generated = demo_frames(steps=steps, hz=args.hz, seed=args.seed)
        records = [(round(i / args.hz, 6), frame) for i, frame in enumerate(generated)]
    else:
        loaded = read_tape(args.replay)
        new_session = "replay-" + uuid.uuid4().hex
        records = [(offset, NeuralFeatureFrame.create(
            seq=i, value=f.value, quality=f.quality, artifact=f.artifact,
            source_mode="feature_replay", source_session_id=new_session,
            calibration_id=None, ttl_ms=f.ttl_ms,
            sample_start=f.sample_start, sample_end=f.sample_end,
        )) for i, (offset, f) in enumerate(loaded)]
    if args.output_tape:
        write_tape(args.output_tape, records)
    print(f"PASSIVE ONLY · {len(records)} frames · {'dry run' if args.dry_run else 'UDP 127.0.0.1:'+str(args.port)}")
    print("No EEG recordings, gameplay actions, imagined-speech labels, or emotional states are transmitted.")
    if not args.dry_run:
        with PassiveUdpEmitter(port=args.port) as emitter:
            start = time.monotonic()
            for offset, frame in records:
                delay = start + offset / args.speed - time.monotonic()
                if delay > 0:
                    time.sleep(delay)
                emitter.send(frame)
    return 0


if __name__ == "__main__":
    raise SystemExit(run())
