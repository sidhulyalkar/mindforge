#!/usr/bin/env python3
"""Generate the standalone offline Neural Calibration Lab from NPZ or a synthetic demo."""
from __future__ import annotations

import argparse
from pathlib import Path
import sys

import numpy as np

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "neuro"))
from mindforge_neuro.calibration_lab import (  # noqa: E402
    build_lab_report, synthetic_research_epochs, write_lab_artifacts,
)


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    source = parser.add_mutually_exclusive_group(required=True)
    source.add_argument("--demo", action="store_true", help="Seeded, entirely synthetic example, NOT human EEG")
    source.add_argument("--archive", type=Path, help="NPZ archive with labeled local EEG epochs")
    parser.add_argument("--task", default="imagined_speech")
    parser.add_argument("--source-kind", choices=("recorded_eeg", "unverified"), default="unverified",
                        help="Self-declared provenance only; demo always uses synthetic_eeg")
    parser.add_argument("--output", type=Path, default=Path("experiments/reports/neural-calibration-lab"))
    args = parser.parse_args(argv)

    if args.demo:
        data = synthetic_research_epochs()
        kind = "synthetic_eeg"
    else:
        with np.load(args.archive, allow_pickle=False) as archive:
            required = ("eeg_uv", "labels", "session_ids", "channel_names", "sample_rate_hz")
            if any(x not in archive for x in required):
                parser.error("archive must contain eeg_uv, labels, session_ids, channel_names, sample_rate_hz")
            fs = np.asarray(archive["sample_rate_hz"])
            if fs.size != 1:
                parser.error("sample_rate_hz must be a scalar")
            data = (archive["eeg_uv"], archive["labels"].astype(str).tolist(),
                    archive["session_ids"].astype(str).tolist(),
                    archive["channel_names"].astype(str).tolist(), float(fs.item()))
        kind = args.source_kind
    report = build_lab_report(*data, task=args.task, source_kind=kind)
    json_path, html_path = write_lab_artifacts(report, args.output)
    print(f"Calibration Lab: {html_path}\nDerived JSON: {json_path}")
    print(f"Selected group: {report['selection']['chosen_group']}, "
          f"holdout balanced accuracy: {report['selection']['holdout_balanced_accuracy']:.3f}")
    print("NOT a demonstrated imagined-speech decoder. Live gameplay remains disabled.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
