#!/usr/bin/env python3
"""Evaluate EEG electrode groups from an intentionally explicit offline .npz archive.

This tool does not stream neural commands to Unity and must not be used for live
BCI authority. The input archive is local, and the report excludes raw EEG.
"""
from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

import numpy as np

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "neuro"))
from mindforge_neuro.adaptive_spatial import evaluate_electrode_groups  # noqa: E402


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("archive", type=Path, help="NPZ with eeg_uv, labels, session_ids, channel_names, sample_rate_hz")
    parser.add_argument("--task", default="imagined_speech")
    parser.add_argument("--output", type=Path, help="Write derived JSON metrics; never writes raw samples")
    args = parser.parse_args()
    with np.load(args.archive, allow_pickle=False) as archive:
        required = ("eeg_uv", "labels", "session_ids", "channel_names", "sample_rate_hz")
        missing = [field for field in required if field not in archive]
        if missing:
            parser.error(f"archive missing fields: {', '.join(missing)}")
        fs = np.asarray(archive["sample_rate_hz"])
        if fs.size != 1:
            parser.error("sample_rate_hz must contain exactly one scalar")
        result = evaluate_electrode_groups(
            archive["eeg_uv"], archive["labels"].astype(str).tolist(),
            archive["session_ids"].astype(str).tolist(),
            archive["channel_names"].astype(str).tolist(), float(fs.item()),
            task=args.task,
        )
    text = json.dumps(result.to_dict(), indent=2, sort_keys=True) + "\n"
    if args.output:
        args.output.parent.mkdir(parents=True, exist_ok=True)
        args.output.write_text(text, encoding="utf-8")
    else:
        print(text, end="")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
