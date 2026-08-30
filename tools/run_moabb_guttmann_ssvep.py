#!/usr/bin/env python3
"""Run the current Mindforge 10/12-Hz decoder on public Guttmann-Flury SSVEP EEG.

This is phase A of the public-data qualification: frequency separability on an eight-channel
Unicorn-like montage. It intentionally does not claim gaze-confound control because MOABB's standard
SSVEP paradigm output is EEG-focused. The full Tobii sidecar analysis is a separate phase B adapter.

Install the optional public-data stack on Python >=3.11:

    pip install -e '.[public-data]'

Example:

    python tools/run_moabb_guttmann_ssvep.py --subjects 1 2 3 --window-seconds 1.25 \
      --output experiments/public/guttmann-10-12.jsonl

The output contains derived decoder evidence only. Raw EEG is never written by this tool.
"""

from __future__ import annotations

import argparse
from dataclasses import replace
import json
from pathlib import Path
import sys

import numpy as np

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "neuro"))

from mindforge_neuro.config import AuraTarget, SsvepConfig  # noqa: E402
from mindforge_neuro.gaze_confound import EvidenceWindow  # noqa: E402
from mindforge_neuro.ssvep import SsvepDecoder  # noqa: E402


UNICORN8 = ["FZ", "C3", "CZ", "C4", "PZ", "PO7", "OZ", "PO8"]
POSTERIOR_INDICES = (4, 5, 6, 7)
LABEL_TO_TARGET = {"10.0": AuraTarget.SIGHT, "12.0": AuraTarget.GUARD}


def _require_moabb():
    try:
        from moabb.datasets import GuttmannFlury2025_SSVEP
        from moabb.paradigms import SSVEP
    except ImportError as exc:
        raise SystemExit(
            "MOABB public-data dependencies are not installed. Use Python >=3.11 and run "
            "`pip install -e '.[public-data]'`."
        ) from exc
    return GuttmannFlury2025_SSVEP, SSVEP


def _canonical_label(value: object) -> str:
    try:
        return f"{float(value):.1f}"
    except (TypeError, ValueError):
        return str(value).strip()


def run(subjects: list[int], *, window_seconds: float) -> list[EvidenceWindow]:
    if not subjects:
        raise ValueError("at least one subject is required")
    if not (0.35 <= window_seconds <= 2.5):
        raise ValueError("window_seconds must be between 0.35 and 2.5 seconds")

    GuttmannFlury2025_SSVEP, SSVEP = _require_moabb()
    dataset = GuttmannFlury2025_SSVEP(subjects=subjects)
    paradigm = SSVEP(
        fmin=6.0,
        fmax=35.0,
        events=["10.0", "12.0"],
        tmin=0.0,
        tmax=float(window_seconds),
        channels=UNICORN8,
        resample=250.0,
    )
    X, labels, metadata = paradigm.get_data(dataset=dataset, subjects=subjects)

    config = replace(
        SsvepConfig(),
        window_seconds=float(window_seconds),
        decode_channel_indices=POSTERIOR_INDICES,
    )
    config.validate()
    decoder = SsvepDecoder(config)
    expected_samples = config.window_samples

    rows: list[EvidenceWindow] = []
    for index, (epoch_v, raw_label) in enumerate(zip(X, labels)):
        label = _canonical_label(raw_label)
        target = LABEL_TO_TARGET.get(label)
        if target is None:
            continue
        epoch = np.asarray(epoch_v, dtype=float)
        if epoch.ndim != 2 or epoch.shape[0] != len(UNICORN8):
            raise ValueError(f"unexpected MOABB epoch shape: {epoch.shape}")
        if epoch.shape[1] < expected_samples:
            raise ValueError(
                f"MOABB epoch has {epoch.shape[1]} samples, need {expected_samples}; "
                "check paradigm resampling/window semantics"
            )
        # MNE/MOABB arrays are volts. Mindforge quality/decoder contracts are microvolts.
        eeg_uv = epoch[:, :expected_samples] * 1e6
        decision = decoder.decide(eeg_uv)
        scores = decoder.score(eeg_uv) if not decision.quality.artifact else {
            AuraTarget.SIGHT: 0.0,
            AuraTarget.GUARD: 0.0,
        }

        subject_id = str(subjects[0])
        if metadata is not None and hasattr(metadata, "iloc") and index < len(metadata):
            record = metadata.iloc[index]
            if "subject" in record:
                subject_id = str(record["subject"])

        rows.append(EvidenceWindow(
            subject_id=subject_id,
            truth=target.value,
            condition="overt",
            sight_score=float(scores[AuraTarget.SIGHT]),
            guard_score=float(scores[AuraTarget.GUARD]),
            quality=float(decision.quality.score),
            window_seconds=float(window_seconds),
        ))

    if not rows:
        raise RuntimeError("no 10/12-Hz evidence windows were produced")
    return rows


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--subjects", type=int, nargs="+", required=True)
    parser.add_argument("--window-seconds", type=float, default=1.25)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()

    rows = run(args.subjects, window_seconds=float(args.window_seconds))
    args.output.parent.mkdir(parents=True, exist_ok=True)
    with args.output.open("w", encoding="utf-8") as handle:
        for row in rows:
            handle.write(json.dumps({
                "subject_id": row.subject_id,
                "truth": row.truth,
                "condition": row.condition,
                "sight_score": row.sight_score,
                "guard_score": row.guard_score,
                "quality": row.quality,
                "window_seconds": row.window_seconds,
                "sight_eccentricity_deg": row.sight_eccentricity_deg,
                "guard_eccentricity_deg": row.guard_eccentricity_deg,
            }, sort_keys=True) + "\n")

    print(json.dumps({
        "schema": "mindforge.public_ssvep_export.v1",
        "dataset": "GuttmannFlury2025_SSVEP",
        "subjects_requested": args.subjects,
        "windows": len(rows),
        "window_seconds": args.window_seconds,
        "channels": UNICORN8,
        "decode_channels": [UNICORN8[i] for i in POSTERIOR_INDICES],
        "frequencies_hz": {"sight": 10.0, "guard": 12.0},
        "output": str(args.output),
        "claim_boundary": "EEG-only frequency-separability evidence; gaze/eccentricity not yet joined.",
    }, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
