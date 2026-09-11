#!/usr/bin/env python3
"""Independently validate a V0.34 adaptive BCI calibration report.

This validator is intentionally separate from the live decoder runner. It checks that
promotion was based on held-out repeated blocks, that the calibration is bound to a
frozen stimulus layout identity, and that no raw EEG payload leaked into the evidence
artifact.
"""
from __future__ import annotations

import argparse
import json
from pathlib import Path
from typing import Any

SCHEMA = "mindforge.calibration_report.v1"
ADAPTIVE_PROTOCOL = "adaptive_repeated_blocks_v34"
FORBIDDEN_KEYS = {
    "raw_eeg",
    "eeg_samples",
    "samples_uv",
    "channel_data",
    "channel_samples",
    "eeg_array",
}


def _walk_keys(value: Any) -> set[str]:
    keys: set[str] = set()
    if isinstance(value, dict):
        for key, child in value.items():
            keys.add(str(key).lower())
            keys.update(_walk_keys(child))
    elif isinstance(value, list):
        for child in value:
            keys.update(_walk_keys(child))
    return keys


def _float(payload: dict[str, Any], key: str) -> float | None:
    try:
        return float(payload[key])
    except (KeyError, TypeError, ValueError):
        return None


def _int(payload: dict[str, Any], key: str) -> int | None:
    try:
        return int(payload[key])
    except (KeyError, TypeError, ValueError):
        return None


def validate(
    payload: dict[str, Any],
    *,
    expected_layout: str | None = None,
    expected_calibration: str | None = None,
    minimum_balanced_accuracy: float = 0.75,
    minimum_accepted_fraction: float = 0.50,
) -> list[str]:
    errors: list[str] = []

    if payload.get("schema") != SCHEMA:
        errors.append("schema")
    if payload.get("protocol") != ADAPTIVE_PROTOCOL:
        errors.append("adaptive_protocol_required")

    layout_id = str(payload.get("stimulus_layout_id") or "").strip()
    calibration_id = str(payload.get("calibration_id") or "").strip()
    if not layout_id:
        errors.append("stimulus_layout_id_missing")
    if not calibration_id:
        errors.append("calibration_id_missing")
    if expected_layout is not None and layout_id != expected_layout:
        errors.append("stimulus_layout_id_mismatch")
    if expected_calibration is not None and calibration_id != expected_calibration:
        errors.append("calibration_id_mismatch")

    training_accuracy = _float(payload, "training_accuracy")
    training_accepted = _float(payload, "accepted_fraction")
    if training_accuracy is None or training_accuracy < 0.70:
        errors.append("training_accuracy_gate")
    if training_accepted is None or training_accepted < 0.50:
        errors.append("training_accepted_gate")

    training_windows = _int(payload, "training_window_count")
    heldout_windows = _int(payload, "heldout_window_count")
    if training_windows is None or training_windows < 6:
        errors.append("training_window_count")
    if heldout_windows is None or heldout_windows < 4:
        errors.append("heldout_window_count")

    heldout = payload.get("heldout_validation")
    if not isinstance(heldout, dict):
        errors.append("heldout_validation_missing")
    else:
        sight_windows = _int(heldout, "usable_sight_windows")
        guard_windows = _int(heldout, "usable_guard_windows")
        balanced_accuracy = _float(heldout, "balanced_accuracy")
        accepted_fraction = _float(heldout, "accepted_correct_fraction")

        if sight_windows is None or sight_windows < 2:
            errors.append("heldout_sight_windows")
        if guard_windows is None or guard_windows < 2:
            errors.append("heldout_guard_windows")
        if balanced_accuracy is None or balanced_accuracy < minimum_balanced_accuracy:
            errors.append("heldout_balanced_accuracy_gate")
        if accepted_fraction is None or accepted_fraction < minimum_accepted_fraction:
            errors.append("heldout_accepted_fraction_gate")

        promotion_accuracy = _float(payload, "promotion_accuracy")
        promotion_accepted = _float(payload, "promotion_accepted_fraction")
        if balanced_accuracy is not None and (
            promotion_accuracy is None or abs(promotion_accuracy - balanced_accuracy) > 1e-6
        ):
            errors.append("promotion_accuracy_not_heldout")
        if accepted_fraction is not None and (
            promotion_accepted is None or abs(promotion_accepted - accepted_fraction) > 1e-6
        ):
            errors.append("promotion_accepted_not_heldout")

    leaked = sorted(FORBIDDEN_KEYS.intersection(_walk_keys(payload)))
    if leaked:
        errors.append("raw_eeg_boundary:" + ",".join(leaked))

    return errors


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("report", type=Path)
    parser.add_argument("--expected-layout")
    parser.add_argument("--expected-calibration")
    parser.add_argument("--minimum-balanced-accuracy", type=float, default=0.75)
    parser.add_argument("--minimum-accepted-fraction", type=float, default=0.50)
    args = parser.parse_args()

    payload = json.loads(args.report.read_text(encoding="utf-8"))
    if not isinstance(payload, dict):
        print("FAIL: report root must be an object")
        return 2

    errors = validate(
        payload,
        expected_layout=args.expected_layout,
        expected_calibration=args.expected_calibration,
        minimum_balanced_accuracy=args.minimum_balanced_accuracy,
        minimum_accepted_fraction=args.minimum_accepted_fraction,
    )
    if errors:
        print("FAIL: " + ", ".join(errors))
        return 1

    print(
        "PASS: adaptive held-out calibration evidence is internally consistent "
        f"layout={payload.get('stimulus_layout_id')} calibration={payload.get('calibration_id')}"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
