#!/usr/bin/env python3
"""Validate a native Mindforge V0.34 adaptive-tutorial receipt.

The validator is intentionally independent of Unity. It checks provenance, the frozen
stimulus-layout contract, gaze-profile shape, neural-practice evidence and scientific
claim boundaries. A partial tutorial may be inspected, but only ``complete`` satisfies
the default promotion contract.
"""

from __future__ import annotations

import argparse
import json
from pathlib import Path
from typing import Any

SCHEMA = "mindforge.tutorial_receipt.v1"
GAZE_SCHEMA = "mindforge.gaze_profile.v1"


def _load(path: Path) -> dict[str, Any]:
    try:
        value = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as exc:
        raise SystemExit(f"could not read tutorial receipt {path}: {exc}") from exc
    if not isinstance(value, dict):
        raise SystemExit("tutorial receipt must be a JSON object")
    return value


def validate(
    payload: dict[str, Any],
    *,
    expected_commit: str | None = None,
    allow_partial: bool = False,
    require_clean: bool = False,
) -> list[str]:
    errors: list[str] = []
    if payload.get("schema") != SCHEMA:
        errors.append("schema")

    status = str(payload.get("tutorial_status") or "")
    if status not in {"complete", "partial"}:
        errors.append("tutorial_status")
    elif status == "partial" and not allow_partial:
        errors.append("tutorial_not_complete")

    source_commit = str(payload.get("source_commit") or "")
    if not source_commit:
        errors.append("source_commit")
    if expected_commit and source_commit != expected_commit:
        errors.append("source_commit_mismatch")
    if require_clean and not bool(payload.get("clean_source")):
        errors.append("clean_source")

    if payload.get("raw_eeg_in_unity") is not False:
        errors.append("raw_eeg_boundary")
    if payload.get("physical_display_timing_observed") is not False:
        errors.append("physical_timing_claim_boundary")

    layout_id = str(payload.get("stimulus_layout_id") or "")
    if not layout_id:
        errors.append("stimulus_layout_id")
    if not bool(payload.get("stimulus_layout_frozen")):
        errors.append("stimulus_layout_not_frozen")

    gaze = payload.get("gaze_profile")
    if not isinstance(gaze, dict) or gaze.get("schema") != GAZE_SCHEMA:
        errors.append("gaze_profile")
    else:
        usable_fraction = float(gaze.get("usable_fraction", 0.0) or 0.0)
        if not 0.0 <= usable_fraction <= 1.0:
            errors.append("gaze_usable_fraction")
        radius = float(gaze.get("recommended_aoi_radius", 0.0) or 0.0)
        if not 0.04 <= radius <= 0.12:
            errors.append("gaze_aoi_radius")
        lead_ms = int(gaze.get("recommended_acquisition_lead_ms", 0) or 0)
        if not 200 <= lead_ms <= 1000:
            errors.append("gaze_acquisition_lead")

    if status == "complete":
        if str(payload.get("calibration_state") or "") != "Calibrated":
            errors.append("calibration_state")
        if not bool(payload.get("sight_practice_pass")):
            errors.append("sight_practice")
        if not bool(payload.get("guard_practice_pass")):
            errors.append("guard_practice")
        calibration_id = str(payload.get("calibration_id") or "")
        if not calibration_id:
            errors.append("calibration_id")

    return errors


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description="Validate Mindforge V0.34 tutorial receipt")
    parser.add_argument("receipt", type=Path)
    parser.add_argument("--expected-commit")
    parser.add_argument("--allow-partial", action="store_true")
    parser.add_argument("--require-clean", action="store_true")
    return parser


def main() -> int:
    args = build_parser().parse_args()
    payload = _load(args.receipt)
    errors = validate(
        payload,
        expected_commit=args.expected_commit,
        allow_partial=args.allow_partial,
        require_clean=args.require_clean,
    )
    if errors:
        print(json.dumps({"status": "FAIL", "errors": errors}, sort_keys=True))
        return 2

    print(
        json.dumps(
            {
                "status": "PASS",
                "schema": payload.get("schema"),
                "tutorial_status": payload.get("tutorial_status"),
                "source_commit": payload.get("source_commit"),
                "layout_id": payload.get("stimulus_layout_id"),
                "gaze_source_mode": payload.get("gaze_source_mode"),
                "neural_source_mode": payload.get("neural_source_mode"),
            },
            sort_keys=True,
        )
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
