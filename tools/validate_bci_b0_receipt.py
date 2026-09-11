#!/usr/bin/env python3
"""Validate a Mindforge V0.33 controller-only native B0 receipt.

This validator intentionally treats controller simulation as software/native evidence
only. A valid B0 receipt must explicitly say that neither EEG nor physical display
timing was observed, and promotion requires exact clean-source provenance.
"""

from __future__ import annotations

import argparse
import json
from pathlib import Path
from typing import Any


SCHEMA = "mindforge.bci_b0_receipt.v1"
REQUIRED_TRUE = (
    "sight_selection_resolved",
    "sight_receptor_observed",
    "guard_selection_resolved",
    "guard_receptor_observed",
    "timeout_abstention_observed",
    "participant_pause_abort_observed",
    "functional_pass",
    "provenance_available",
    "source_clean",
    "overlay_applied",
    "passed",
)


def validate(payload: dict[str, Any], expected_commit: str | None = None) -> list[str]:
    errors: list[str] = []

    if payload.get("schema") != SCHEMA:
        errors.append("schema")
    if payload.get("mode") != "controller_only":
        errors.append("mode")
    if payload.get("source_mode") != "simulated_decision":
        errors.append("source_mode")

    for key in REQUIRED_TRUE:
        if payload.get(key) is not True:
            errors.append(key)

    if payload.get("worktree_dirty") is not False:
        errors.append("worktree_dirty")
    if payload.get("eeg_observed") is not False:
        errors.append("eeg_observed_must_be_false")
    if payload.get("physical_display_timing_observed") is not False:
        errors.append("physical_display_timing_observed_must_be_false")

    source_commit = str(payload.get("source_commit") or "")
    if len(source_commit) < 7:
        errors.append("source_commit")
    if expected_commit and source_commit != expected_commit:
        errors.append("source_commit_mismatch")

    if not payload.get("unity_version"):
        errors.append("unity_version")
    if not payload.get("runtime_platform"):
        errors.append("runtime_platform")
    if not payload.get("upstream_commit"):
        errors.append("upstream_commit")

    return errors


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("receipt", type=Path)
    parser.add_argument("--expected-commit", default=None)
    args = parser.parse_args()

    try:
        payload = json.loads(args.receipt.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as exc:
        print(json.dumps({"schema": SCHEMA, "passed": False, "errors": [f"read:{type(exc).__name__}"]}))
        return 2

    if not isinstance(payload, dict):
        print(json.dumps({"schema": SCHEMA, "passed": False, "errors": ["receipt_not_object"]}))
        return 2

    errors = validate(payload, expected_commit=args.expected_commit)
    report = {
        "schema": SCHEMA,
        "passed": not errors,
        "source_commit": payload.get("source_commit"),
        "unity_version": payload.get("unity_version"),
        "runtime_platform": payload.get("runtime_platform"),
        "errors": errors,
    }
    print(json.dumps(report, indent=2, sort_keys=True))
    return 0 if not errors else 1


if __name__ == "__main__":
    raise SystemExit(main())
