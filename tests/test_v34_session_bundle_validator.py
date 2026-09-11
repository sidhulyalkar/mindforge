from pathlib import Path
import runpy


ROOT = Path(__file__).resolve().parents[1]
TOOLS = ROOT / "tools"
VALIDATOR = TOOLS / "validate_v34_session_bundle.py"


def validate_bundle():
    return runpy.run_path(str(VALIDATOR))["validate_bundle"]


def tutorial_payload():
    return {
        "schema": "mindforge.tutorial_receipt.v1",
        "source_commit": "a" * 40,
        "clean_source": True,
        "unity_version": "2021.3.20f1",
        "session_id": "session-1",
        "tutorial_status": "complete",
        "gaze_source_mode": "mouse",
        "gaze_profile": {
            "schema": "mindforge.gaze_profile.v1",
            "usable_fraction": 0.9,
            "recommended_aoi_radius": 0.06,
            "recommended_acquisition_lead_ms": 350,
        },
        "stimulus_layout_id": "layout-1",
        "stimulus_layout_frozen": True,
        "calibration_id": "cal-1",
        "calibration_state": "Calibrated",
        "sight_practice_pass": True,
        "guard_practice_pass": True,
        "neural_source_mode": "synthetic_eeg",
        "raw_eeg_in_unity": False,
        "physical_display_timing_observed": False,
    }


def calibration_payload():
    return {
        "schema": "mindforge.calibration_report.v1",
        "session_id": "session-1",
        "calibration_id": "cal-1",
        "stimulus_layout_id": "layout-1",
        "protocol": "adaptive_repeated_blocks_v34",
        "source_mode": "synthetic_eeg",
        "training_accuracy": 0.9,
        "accepted_fraction": 0.8,
        "training_window_count": 12,
        "heldout_window_count": 4,
        "heldout_validation": {
            "usable_windows": 4,
            "usable_sight_windows": 2,
            "usable_guard_windows": 2,
            "balanced_accuracy": 0.75,
            "accepted_correct_fraction": 0.50,
        },
        "promotion_accuracy": 0.75,
        "promotion_accepted_fraction": 0.50,
    }


def gaze_records():
    base = {
        "schema": "mindforge.gaze_bci_evidence.v1",
        "session_id": "session-1",
        "calibration_id": "cal-1",
        "layout_id": "layout-1",
        "phase": "neural_window",
        "usable_samples": 25,
        "sight_occupancy": 0.8,
        "guard_occupancy": 0.1,
        "off_target_fraction": 0.1,
    }
    return [dict(base, stimulus_epoch=1), dict(base, stimulus_epoch=2)]


def test_bundle_accepts_cross_process_identity_consistent_evidence():
    validate = validate_bundle()
    assert validate(
        tutorial_payload(),
        calibration_payload(),
        gaze_records=gaze_records(),
        expected_commit="a" * 40,
        require_clean=True,
        require_gaze_evidence=True,
        tools_dir=TOOLS,
    ) == []


def test_bundle_rejects_session_layout_and_calibration_drift():
    validate = validate_bundle()
    tutorial = tutorial_payload()
    calibration = calibration_payload()
    calibration["session_id"] = "other-session"
    calibration["stimulus_layout_id"] = "other-layout"
    calibration["calibration_id"] = "other-calibration"
    errors = validate(tutorial, calibration, tools_dir=TOOLS)
    assert "session_id_mismatch" in errors
    assert "calibration:stimulus_layout_id_mismatch" in errors
    assert "calibration:calibration_id_mismatch" in errors


def test_bundle_rejects_neural_source_mode_drift():
    validate = validate_bundle()
    calibration = calibration_payload()
    calibration["source_mode"] = "live"
    assert "neural_source_mode_mismatch" in validate(
        tutorial_payload(), calibration, tools_dir=TOOLS
    )


def test_bundle_requires_multiple_neural_window_gaze_records_when_requested():
    validate = validate_bundle()
    errors = validate(
        tutorial_payload(),
        calibration_payload(),
        gaze_records=gaze_records()[:1],
        require_gaze_evidence=True,
        tools_dir=TOOLS,
    )
    assert "gaze_neural_window_evidence_insufficient" in errors


def test_bundle_rejects_raw_gaze_coordinates_in_aggregate_evidence():
    validate = validate_bundle()
    records = gaze_records()
    records[0]["gaze_x"] = 0.5
    assert any(
        "raw_gaze_boundary:gaze_x" in error
        for error in validate(
            tutorial_payload(),
            calibration_payload(),
            gaze_records=records,
            tools_dir=TOOLS,
        )
    )


def test_bundle_requires_join_keys_on_every_gaze_record():
    validate = validate_bundle()
    records = gaze_records()
    records[0].pop("session_id")
    records[0].pop("layout_id")
    records[0].pop("calibration_id")
    errors = validate(
        tutorial_payload(),
        calibration_payload(),
        gaze_records=records,
        tools_dir=TOOLS,
    )
    assert "gaze[0]:session_id_missing" in errors
    assert "gaze[0]:layout_id_missing" in errors
    assert "gaze[0]:calibration_id_missing" in errors


def test_bundle_binds_accepted_gaze_neural_evidence_to_decoder_source():
    validate = validate_bundle()
    records = gaze_records()
    records[0]["outcome"] = "accepted"
    records[0]["neural_source_mode"] = "live"
    errors = validate(
        tutorial_payload(),
        calibration_payload(),
        gaze_records=records,
        tools_dir=TOOLS,
    )
    assert "gaze[0]:neural_source_mode_mismatch" in errors

    records[0]["neural_source_mode"] = "synthetic_eeg"
    assert "gaze[0]:neural_source_mode_mismatch" not in validate(
        tutorial_payload(),
        calibration_payload(),
        gaze_records=records,
        tools_dir=TOOLS,
    )
