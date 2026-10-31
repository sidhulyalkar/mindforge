from __future__ import annotations

import json
from pathlib import Path
import subprocess
import sys

import numpy as np
import pytest

from mindforge_neuro.calibration_lab import (
    build_lab_report, render_lab_html, report_json,
    synthetic_research_epochs, write_lab_artifacts,
)


def make_report():
    return build_lab_report(*synthetic_research_epochs(), source_kind="synthetic_eeg")


def test_demo_selects_central_sensors_and_reports_independent_sessions():
    report = make_report()
    assert report["schema"] == "mindforge.calibration_lab.v2"
    assert report["selection"]["chosen_group"] == "central"
    assert "C3" in report["selection"]["chosen_electrodes"]
    assert report["audit"]["training"]["trials"] == 40
    assert report["audit"]["validation"]["trials"] == 40
    assert report["audit"]["holdout"]["trials"] == 40
    assert report["audit"]["holdout"]["class_counts"] == {"bridge": 20, "wall": 20}
    assert not report["gates"]["live_gameplay_authorized"]
    assert not report["gates"]["human_speech_decoding_demonstrated"]
    assert "SYNTHETIC_DEMO_NOT_HUMAN_EVIDENCE" in report["warnings"]


def test_report_redacts_original_session_ids_and_never_serializes_raw_samples():
    eeg, labels, ids, channels, fs = synthetic_research_epochs()
    ids = ["PERSON_PRIVATE_LAB_" + s for s in ids]
    report = build_lab_report(eeg, labels, ids, channels, fs)
    serialized = report_json(report)
    assert "PERSON_PRIVATE_LAB" not in serialized
    assert "eeg_uv" not in serialized
    assert "raw_eeg" not in serialized
    assert "PROVENANCE_UNVERIFIED" in serialized
    assert report["selection"]["train_sessions"] == ["train-1"]
    assert report["selection"]["validation_session"] == "validation-redacted"


def test_html_is_self_contained_and_escapes_untrusted_labels():
    report = make_report()
    report["task"] = '<img src=x onerror="alert(7)">'
    report["selection"]["candidate_validation_balanced_accuracy"]['<script>alert(1)</script>'] = .25
    report["warnings"].append('<svg onload="alert(1)">')
    page = render_lab_html(report)
    assert '<svg viewBox=' in page
    assert "<details>" in page
    assert "<script>" not in page
    assert "<img src=x" not in page
    assert "&lt;img src=x" in page
    assert "&lt;script&gt;" in page
    assert "default-src 'none'" in page
    assert "https://" not in page


def test_deterministic_report_and_files_are_derived_only(tmp_path):
    a, b = make_report(), make_report()
    assert a == b
    jp, hp = write_lab_artifacts(a, tmp_path / "lab")
    assert json.loads(jp.read_text())["report_id"] == a["report_id"]
    assert "Neural Calibration Lab" in hp.read_text()
    assert not list((tmp_path / "lab").glob("*.npz"))


def test_invalid_source_kind_and_wrong_schema_fail_closed():
    with pytest.raises(ValueError, match="source_kind"):
        build_lab_report(*synthetic_research_epochs(), source_kind="human_verified")
    with pytest.raises(ValueError, match="schema"):
        render_lab_html({"schema": "invalid"})
    with pytest.raises(ValueError, match="schema"):
        report_json({"schema": "invalid"})


def test_montage_marks_unrecorded_lateral_sites():
    report = make_report()
    lateral = report["montage"]["group_coverage"]["lateral_frontotemporal"]
    assert lateral["present"] == []
    assert "FT7" in lateral["missing"]
    assert "PO7" in report["montage"]["group_coverage"]["posterior_control"]["present"]


def test_unknown_sensor_is_not_fabricated_on_head_map():
    eeg, labels, ids, channels, fs = synthetic_research_epochs()
    channels = ("MYSTERY-1",) + channels[1:]
    report = build_lab_report(eeg, labels, ids, channels, fs)
    assert report["montage"]["unmapped"] == ["MYSTERY-1"]
    assert "UNMAPPED_ELECTRODES_RENDERED_IN_TEXT_ONLY" in report["warnings"]
    assert "MYSTERY-1" in render_lab_html(report)


def test_cli_synthetic_demo_writes_private_standalone_report(tmp_path):
    script = Path(__file__).resolve().parents[1] / "tools" / "mindforge_calibration_lab.py"
    target = tmp_path / "demo"
    result = subprocess.run(
        [sys.executable, str(script), "--demo", "--output", str(target)],
        check=True, capture_output=True, text=True,
    )
    assert "Live gameplay remains disabled" in result.stdout
    assert json.loads((target / "calibration.json").read_text())["source_kind"] == "synthetic_eeg"
    assert (target / "index.html").read_text().startswith("<!doctype html>")


def test_html_does_not_disclose_original_recording_sessions():
    eeg, labels, ids, channels, fs = synthetic_research_epochs()
    secret = "private_participant_id"
    report = build_lab_report(eeg, labels, [secret + s for s in ids], channels, fs)
    assert secret not in render_lab_html(report)
