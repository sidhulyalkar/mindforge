from pathlib import Path
import runpy


ROOT = Path(__file__).resolve().parents[1]
VALIDATOR = ROOT / "tools" / "validate_v34_calibration_report.py"


def load_validator():
    return runpy.run_path(str(VALIDATOR))["validate"]


def valid_payload():
    return {
        "schema": "mindforge.calibration_report.v1",
        "session_id": "session-1",
        "calibration_id": "cal-1",
        "stimulus_layout_id": "v34-s210-a060-l350",
        "protocol": "adaptive_repeated_blocks_v34",
        "model_id": "synthetic_eeg-1",
        "source_mode": "synthetic_eeg",
        "training_accuracy": 0.91,
        "accepted_fraction": 0.80,
        "training_window_count": 12,
        "heldout_window_count": 4,
        "heldout_validation": {
            "usable_windows": 4,
            "usable_sight_windows": 2,
            "usable_guard_windows": 2,
            "sight_accuracy": 1.0,
            "guard_accuracy": 0.5,
            "balanced_accuracy": 0.75,
            "accepted_correct_fraction": 0.50,
            "mean_quality": 0.88,
            "median_margin": 0.12,
        },
        "promotion_accuracy": 0.75,
        "promotion_accepted_fraction": 0.50,
        "min_score": 0.2,
        "min_margin": 0.05,
    }


def test_validator_accepts_heldout_bound_adaptive_report():
    validate = load_validator()
    payload = valid_payload()
    assert validate(
        payload,
        expected_layout="v34-s210-a060-l350",
        expected_calibration="cal-1",
    ) == []


def test_validator_rejects_training_metrics_masquerading_as_promotion():
    validate = load_validator()
    payload = valid_payload()
    payload["promotion_accuracy"] = payload["training_accuracy"]
    payload["promotion_accepted_fraction"] = payload["accepted_fraction"]
    errors = validate(payload)
    assert "promotion_accuracy_not_heldout" in errors
    assert "promotion_accepted_not_heldout" in errors


def test_validator_requires_balanced_heldout_evidence_per_target():
    validate = load_validator()
    payload = valid_payload()
    payload["heldout_validation"]["usable_guard_windows"] = 1
    assert "heldout_guard_windows" in validate(payload)


def test_validator_rejects_layout_or_calibration_identity_drift():
    validate = load_validator()
    payload = valid_payload()
    errors = validate(
        payload,
        expected_layout="different-layout",
        expected_calibration="different-calibration",
    )
    assert "stimulus_layout_id_mismatch" in errors
    assert "calibration_id_mismatch" in errors


def test_validator_rejects_raw_eeg_fields_anywhere_in_report():
    validate = load_validator()
    payload = valid_payload()
    payload["diagnostics"] = {"samples_uv": [[0.1, 0.2]]}
    assert any(error.startswith("raw_eeg_boundary:") for error in validate(payload))


def test_validator_rejects_legacy_protocol_for_v34_promotion():
    validate = load_validator()
    payload = valid_payload()
    payload["protocol"] = "legacy_continuous_v33"
    assert "adaptive_protocol_required" in validate(payload)
