#!/usr/bin/env python3
"""Evaluate derived public-dataset SSVEP evidence against Mindforge gameplay policy.

The tool intentionally consumes derived JSONL rather than raw EEG. Dataset-specific notebooks or
adapters may use MNE/MOABB/Pandas locally, then emit bounded evidence records that are easy to audit,
version and replay. This keeps heavyweight public datasets out of the repository and prevents raw
biometric data from crossing into Unity-facing artifacts.
"""

from __future__ import annotations

import argparse
import json
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "neuro"))

from mindforge_neuro.gaze_confound import (  # noqa: E402
    EvidenceWindow,
    SelectionPolicy,
    evaluate_policy,
    gameplay_loss,
    recommend_game_architecture,
    tune_policy,
)
from mindforge_neuro.public_validation import leave_one_subject_out_validation  # noqa: E402


DATASETS = {
    "guttmann-flury-2025-ssvep": {
        "doi": "10.1038/s41597-025-04861-9",
        "data_doi": "10.7303/syn64005218",
        "participants": 31,
        "frequencies_hz": [10.0, 11.0, 12.0, 13.0],
        "modalities": ["64-channel EEG", "Tobii eye tracking", "high-speed eye video"],
        "mindforge_role": "Primary peripheral-flicker/gaze-confound benchmark; use the 10/12-Hz subset first.",
        "important_note": "The original publication/MOABB annotations are authoritative for frequencies. Validate source labels rather than trusting re-host catalog prose.",
    },
    "iscan-2026-overt-covert": {
        "doi": "10.1371/journal.pone.0345793",
        "data_doi": "10.5281/zenodo.19081765",
        "participants": 20,
        "frequencies_hz": [4.6, 6.43, 8.03, 10.7],
        "modalities": ["16-channel EEG"],
        "mindforge_role": "Overt-versus-covert attention stress test and no-attention/zero-class evidence.",
    },
    "li-2021-retinal-eccentricity": {
        "doi": "10.3389/fnins.2021.746146",
        "participants": 25,
        "modalities": ["64-channel EEG"],
        "mindforge_role": "Quantifies overt/covert/no-attention changes across retinal eccentricity; use as design prior unless raw data are obtained.",
    },
    "eegeyenet": {
        "doi": "10.48550/arXiv.2111.05100",
        "participants": 356,
        "modalities": ["128-channel EEG", "eye tracking"],
        "mindforge_role": "Ocular nuisance benchmark: test how much gaze/eye movement is inferable from a Unicorn-like EEG montage.",
    },
    "han-2024-fatigue": {
        "doi": "10.1109/TNSRE.2024.3380635",
        "data_doi": "10.5281/zenodo.10507229",
        "modalities": ["EEG"],
        "mindforge_role": "Frequency-band and fatigue robustness benchmark; compare low/medium bands before final stimulus selection.",
    },
}


def load_jsonl(path: Path) -> list[EvidenceWindow]:
    rows: list[EvidenceWindow] = []
    with path.open("r", encoding="utf-8") as handle:
        for line_number, raw in enumerate(handle, 1):
            raw = raw.strip()
            if not raw or raw.startswith("#"):
                continue
            payload = json.loads(raw)
            try:
                row = EvidenceWindow(**payload)
                row.validate()
            except Exception as exc:
                raise ValueError(f"{path}:{line_number}: {exc}") from exc
            rows.append(row)
    if not rows:
        raise ValueError(f"{path} contains no evidence windows")
    return rows


def report(
    rows: list[EvidenceWindow],
    policy: SelectionPolicy,
    *,
    include_loso: bool,
) -> dict[str, object]:
    metrics = evaluate_policy(rows, policy)
    recommendation = recommend_game_architecture(metrics)
    payload: dict[str, object] = {
        "schema": "mindforge.ssvep_game_design_report.v1",
        "policy": {
            "min_score": policy.min_score,
            "min_margin": policy.min_margin,
            "min_quality": policy.min_quality,
            "require_gaze_geometry": policy.require_gaze_geometry,
            "max_attended_eccentricity_deg": policy.max_attended_eccentricity_deg,
        },
        "metrics": metrics.to_dict(),
        "gameplay_loss": gameplay_loss(metrics),
        "recommendation": recommendation.to_dict(),
        "claim_boundary": (
            "Public-data results can reject weak designs and rank architectures, but cannot qualify "
            "the final Unity renderer, display timing, Unicorn hardware, or a specific player."
        ),
    }
    if include_loso:
        payload["leave_one_subject_out"] = leave_one_subject_out_validation(
            rows,
            require_gaze_geometry=policy.require_gaze_geometry,
        ).to_dict()
        payload["validation_note"] = (
            "LOSO threshold selection is the minimum cohort-level evidence. Data-driven EEG models "
            "such as TRCA must also be fitted strictly inside each training/calibration split."
        )
    return payload


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    sub = parser.add_subparsers(dest="command", required=True)

    sub.add_parser("catalog", help="Print the public-dataset qualification catalog")

    evaluate = sub.add_parser("evaluate", help="Evaluate standardized derived JSONL evidence")
    evaluate.add_argument("input", type=Path)
    evaluate.add_argument("--output", type=Path)
    evaluate.add_argument("--min-score", type=float, default=0.15)
    evaluate.add_argument("--min-margin", type=float, default=0.035)
    evaluate.add_argument("--min-quality", type=float, default=0.55)
    evaluate.add_argument("--gaze-gate", action="store_true")
    evaluate.add_argument("--max-eccentricity-deg", type=float, default=6.0)
    evaluate.add_argument(
        "--tune",
        action="store_true",
        help="Tune a descriptive pooled policy by gameplay risk; do not use this pooled score as promotion evidence.",
    )
    evaluate.add_argument(
        "--loso",
        action="store_true",
        help="Also tune thresholds on N-1 subjects and evaluate once on each held-out subject.",
    )

    args = parser.parse_args()
    if args.command == "catalog":
        print(json.dumps(DATASETS, indent=2, sort_keys=True))
        return 0

    rows = load_jsonl(args.input)
    if args.tune:
        policy, _ = tune_policy(rows, require_gaze_geometry=bool(args.gaze_gate))
    else:
        policy = SelectionPolicy(
            min_score=float(args.min_score),
            min_margin=float(args.min_margin),
            min_quality=float(args.min_quality),
            require_gaze_geometry=bool(args.gaze_gate),
            max_attended_eccentricity_deg=float(args.max_eccentricity_deg),
        )
    payload = report(rows, policy, include_loso=bool(args.loso))
    rendered = json.dumps(payload, indent=2, sort_keys=True)
    print(rendered)
    if args.output:
        args.output.parent.mkdir(parents=True, exist_ok=True)
        args.output.write_text(rendered + "\n", encoding="utf-8")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
