#!/usr/bin/env python3
"""Validate a complete Mindforge V0.34 cross-process evidence bundle.

The tutorial receipt, decoder calibration report and optional aggregate gaze/BCI JSONL
are produced by different runtime components. This validator joins them on session,
calibration and frozen-layout identity, reuses the independent artifact validators, and
can emit a compact SHA-256-bound bundle receipt. It never needs raw EEG or raw gaze.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import runpy
from pathlib import Path
from typing import Any

SCHEMA = "mindforge.v34_session_bundle_validation.v1"
GAZE_EVIDENCE_SCHEMA = "mindforge.gaze_bci_evidence.v1"
FORBIDDEN_GAZE_KEYS = {
    "gaze_x",
    "gaze_y",
    "screen_x",
    "screen_y",
    "normalized_x",
    "normalized_y",
    "raw_x",
    "raw_y",
    "eye_image",
    "scene_video",
}


def _load_object(path: Path, label: str) -> dict[str, Any]:
    try:
        value = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as exc:
        raise ValueError(f"could_not_read_{label}:{exc}") from exc
    if not isinstance(value, dict):
        raise ValueError(f"{label}_must_be_object")
    return value


def _load_jsonl(path: Path) -> list[dict[str, Any]]:
    records: list[dict[str, Any]] = []
    try:
        lines = path.read_text(encoding="utf-8").splitlines()
    except OSError as exc:
        raise ValueError(f"could_not_read_gaze_evidence:{exc}") from exc
    for index, line in enumerate(lines, start=1):
        if not line.strip():
            continue
        try:
            value = json.loads(line)
        except json.JSONDecodeError as exc:
            raise ValueError(f"gaze_evidence_json_line_{index}:{exc.msg}") from exc
        if not isinstance(value, dict):
            raise ValueError(f"gaze_evidence_line_{index}_must_be_object")
        records.append(value)
    return records


def _sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


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


def _validator(path: Path, name: str):
    namespace = runpy.run_path(str(path))
    return namespace[name]


def validate_bundle(
    tutorial: dict[str, Any],
    calibration: dict[str, Any],
    *,
    gaze_records: list[dict[str, Any]] | None = None,
    expected_commit: str | None = None,
    require_clean: bool = False,
    require_gaze_evidence: bool = False,
    tools_dir: Path | None = None,
) -> list[str]:
    errors: list[str] = []
    tools = tools_dir or Path(__file__).resolve().parent

    validate_tutorial = _validator(tools / "validate_tutorial_receipt_v34.py", "validate")
    validate_calibration = _validator(tools / "validate_v34_calibration_report.py", "validate")

    errors.extend(
        "tutorial:" + error
        for error in validate_tutorial(
            tutorial,
            expected_commit=expected_commit,
            require_clean=require_clean,
        )
    )

    layout_id = str(tutorial.get("stimulus_layout_id") or "")
    calibration_id = str(tutorial.get("calibration_id") or "")
    errors.extend(
        "calibration:" + error
        for error in validate_calibration(
            calibration,
            expected_layout=layout_id or None,
            expected_calibration=calibration_id or None,
        )
    )

    tutorial_session = str(tutorial.get("session_id") or "")
    calibration_session = str(calibration.get("session_id") or "")
    if not tutorial_session:
        errors.append("tutorial_session_id_missing")
    if not calibration_session:
        errors.append("calibration_session_id_missing")
    elif tutorial_session and tutorial_session != calibration_session:
        errors.append("session_id_mismatch")

    tutorial_source = str(tutorial.get("neural_source_mode") or "")
    calibration_source = str(calibration.get("source_mode") or "")
    if tutorial.get("tutorial_status") == "complete":
        if not tutorial_source or tutorial_source == "unobserved":
            errors.append("neural_source_unobserved")
        if not calibration_source:
            errors.append("calibration_source_mode_missing")
        elif tutorial_source and tutorial_source != "unobserved" and tutorial_source != calibration_source:
            errors.append("neural_source_mode_mismatch")

    records = gaze_records or []
    if require_gaze_evidence and not records:
        errors.append("gaze_evidence_required")

    neural_window_records = 0
    for index, record in enumerate(records):
        prefix = f"gaze[{index}]"
        if record.get("schema") != GAZE_EVIDENCE_SCHEMA:
            errors.append(prefix + ":schema")
            continue

        record_session = str(record.get("session_id") or "")
        record_layout = str(record.get("layout_id") or "")
        record_calibration = str(record.get("calibration_id") or "")
        if not record_session:
            errors.append(prefix + ":session_id_missing")
        elif tutorial_session and record_session != tutorial_session:
            errors.append(prefix + ":session_id_mismatch")
        if not record_layout:
            errors.append(prefix + ":layout_id_missing")
        elif layout_id and record_layout != layout_id:
            errors.append(prefix + ":layout_id_mismatch")
        if not record_calibration:
            errors.append(prefix + ":calibration_id_missing")
        elif calibration_id and record_calibration != calibration_id:
            errors.append(prefix + ":calibration_id_mismatch")

        phase = str(record.get("phase") or "")
        if phase == "neural_window":
            neural_window_records += 1
            record_neural_source = str(record.get("neural_source_mode") or "")
            outcome = str(record.get("outcome") or "")
            if outcome == "accepted":
                if not record_neural_source or record_neural_source == "unobserved":
                    errors.append(prefix + ":neural_source_unobserved")
                elif calibration_source and record_neural_source != calibration_source:
                    errors.append(prefix + ":neural_source_mode_mismatch")

        leaked = sorted(FORBIDDEN_GAZE_KEYS.intersection(_walk_keys(record)))
        if leaked:
            errors.append(prefix + ":raw_gaze_boundary:" + ",".join(leaked))

    if require_gaze_evidence and neural_window_records < 2:
        errors.append("gaze_neural_window_evidence_insufficient")

    return errors


def build_receipt(
    tutorial_path: Path,
    calibration_path: Path,
    gaze_path: Path | None,
    tutorial: dict[str, Any],
    calibration: dict[str, Any],
) -> dict[str, Any]:
    return {
        "schema": SCHEMA,
        "status": "PASS",
        "source_commit": tutorial.get("source_commit"),
        "session_id": tutorial.get("session_id"),
        "calibration_id": tutorial.get("calibration_id"),
        "stimulus_layout_id": tutorial.get("stimulus_layout_id"),
        "gaze_source_mode": tutorial.get("gaze_source_mode"),
        "neural_source_mode": tutorial.get("neural_source_mode"),
        "protocol": calibration.get("protocol"),
        "promotion_accuracy": calibration.get("promotion_accuracy"),
        "promotion_accepted_fraction": calibration.get("promotion_accepted_fraction"),
        "tutorial_receipt_sha256": _sha256(tutorial_path),
        "calibration_report_sha256": _sha256(calibration_path),
        "gaze_evidence_sha256": _sha256(gaze_path) if gaze_path is not None else None,
        "raw_eeg_in_unity": False,
        "physical_display_timing_observed": False,
    }


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("tutorial_receipt", type=Path)
    parser.add_argument("calibration_report", type=Path)
    parser.add_argument("--gaze-evidence", type=Path)
    parser.add_argument("--expected-commit")
    parser.add_argument("--require-clean", action="store_true")
    parser.add_argument("--require-gaze-evidence", action="store_true")
    parser.add_argument("--output", type=Path)
    args = parser.parse_args()

    try:
        tutorial = _load_object(args.tutorial_receipt, "tutorial_receipt")
        calibration = _load_object(args.calibration_report, "calibration_report")
        gaze_records = _load_jsonl(args.gaze_evidence) if args.gaze_evidence else None
    except ValueError as exc:
        print(json.dumps({"status": "FAIL", "errors": [str(exc)]}, sort_keys=True))
        return 2

    errors = validate_bundle(
        tutorial,
        calibration,
        gaze_records=gaze_records,
        expected_commit=args.expected_commit,
        require_clean=args.require_clean,
        require_gaze_evidence=args.require_gaze_evidence,
    )
    if errors:
        print(json.dumps({"status": "FAIL", "errors": errors}, sort_keys=True))
        return 2

    receipt = build_receipt(
        args.tutorial_receipt,
        args.calibration_report,
        args.gaze_evidence,
        tutorial,
        calibration,
    )
    if args.output:
        args.output.parent.mkdir(parents=True, exist_ok=True)
        args.output.write_text(json.dumps(receipt, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    print(json.dumps(receipt, sort_keys=True))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
