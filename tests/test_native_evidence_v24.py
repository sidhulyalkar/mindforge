import json
from pathlib import Path
import pytest
from mindforge_neuro.markers import GameMarker
from mindforge_neuro.native_evidence_v24 import audit_selection_markers, load_markers


def marker(seq, event, *, trial="selection-v24:game-a:7:44", target="sight",
           epoch=7, session="game-a", time=None):
    return GameMarker.from_dict({
        "schema": "mindforge.game_marker.v1", "seq": seq,
        "session_id": session, "event": event, "category": "neural_evidence",
        "unity_realtime_s": float(seq) if time is None else time,
        "game_time_s": float(seq), "frame": seq, "fixed_tick": seq,
        "trial_id": trial, "target": target, "stimulus_epoch": epoch,
    })


def good():
    return [marker(1, "NEURAL_SELECTION_OBSERVED"),
            marker(2, "NEURAL_SELECTION_ACCEPTED"),
            marker(3, "NEURAL_EFFECT_OBSERVED")]


def codes(rows):
    return {x["code"] for x in audit_selection_markers(rows)["issues"]}


def test_clean_chain_is_only_software_consistent():
    result = audit_selection_markers(good())
    assert result["software_consistent"] is True
    assert result["scientifically_qualified"] is False
    assert result["source_stream_complete"] is False
    assert result["independent_effect_verified"] is False


def test_denied_selection_does_not_become_effect():
    rows = [marker(1, "NEURAL_SELECTION_OBSERVED"), marker(2, "NEURAL_SELECTION_DENIED")]
    assert not codes(rows)


def test_orphan_effect_fails():
    assert "UNSUPPORTED_OR_DUPLICATE_EFFECT" in codes([marker(1, "NEURAL_EFFECT_OBSERVED")])


def test_missing_effect_fails():
    assert "MISSING_EFFECT_OBSERVATION" in codes(good()[:2])


def test_duplicate_effect_fails():
    assert "UNSUPPORTED_OR_DUPLICATE_EFFECT" in codes(good() + [marker(4, "NEURAL_EFFECT_OBSERVED")])


def test_denied_then_accepted_fails():
    rows = [marker(1, "NEURAL_SELECTION_OBSERVED"),
            marker(2, "NEURAL_SELECTION_DENIED"),
            marker(3, "NEURAL_SELECTION_ACCEPTED")]
    assert "INVALID_ACCEPTANCE_TRANSITION" in codes(rows)


def test_identity_mutation_fails():
    rows = good()
    rows[2] = marker(3, "NEURAL_EFFECT_OBSERVED", target="guard")
    assert "MUTATED_TRIAL_IDENTITY" in codes(rows)


def test_wrong_epoch_fails():
    rows = good()
    rows[2] = marker(3, "NEURAL_EFFECT_OBSERVED", epoch=6)
    assert "MUTATED_TRIAL_IDENTITY" in codes(rows)


def test_sequence_gap_fails():
    rows = good()
    rows[2] = marker(5, "NEURAL_EFFECT_OBSERVED")
    assert "SOURCE_SEQUENCE_GAP" in codes(rows)


def test_reorder_fails():
    rows = good()
    rows[2] = marker(2, "NEURAL_EFFECT_OBSERVED")
    assert "SEQUENCE_REUSED_OR_REORDERED" in codes(rows)


def test_clock_regression_fails():
    rows = good()
    rows[2] = marker(3, "NEURAL_EFFECT_OBSERVED", time=0.1)
    assert "UNITY_CLOCK_REGRESSION" in codes(rows)


def test_no_trials_cannot_pass():
    assert "NO_NEURAL_SELECTION_TRIALS" in codes([])


def test_unverified_observation_stays_unqualified():
    rows = good()
    rows[2] = marker(3, "NEURAL_EFFECT_UNVERIFIED")
    r = audit_selection_markers(rows)
    assert r["software_consistent"] is True
    assert r["scientifically_qualified"] is False
    assert "NEURAL_EFFECT_OBSERVED" not in r["event_counts"]


def test_legacy_unattributed_buff_not_promoted():
    rows = good() + [marker(4, "NEURAL_BUFF_APPLIED", trial="")]
    r = audit_selection_markers(rows)
    assert r["unattributed_legacy_buff_markers"] == 1
    assert r["scientifically_qualified"] is False


def test_loader_rejects_extra_biometric_data(tmp_path):
    p = tmp_path / "bad.jsonl"
    row = {"schema": "mindforge.game_marker.v1", "seq": 1,
           "session_id": "a", "event": "NEURAL_EFFECT_OBSERVED", "raw_eeg": [3]}
    p.write_text(json.dumps(row) + "\n")
    with pytest.raises(ValueError, match="unknown marker"):
        load_markers(p)


def test_loader_rejects_truncated_and_duplicate_json(tmp_path):
    p = tmp_path / "bad.jsonl"
    p.write_bytes(b'{"schema":"x"}')
    with pytest.raises(ValueError, match="terminal LF"):
        load_markers(p)
    p.write_bytes(b'{"schema":"a","schema":"b"}\n')
    with pytest.raises(ValueError, match="duplicate JSON key"):
        load_markers(p)


def test_loader_accepts_valid_record(tmp_path):
    p = tmp_path / "good.jsonl"
    p.write_text(json.dumps(marker(1, "NEURAL_SELECTION_OBSERVED").to_dict()) + "\n")
    assert len(load_markers(p)) == 1


def test_unity_authorization_source_does_not_trust_udp_editor_mode():
    from pathlib import Path
    root = Path(__file__).resolve().parents[1]
    window = (root / "unity/Assets/Mindforge/SoulWisp/WispResonanceWindow.cs").read_text()
    director = (root / "unity/Assets/Mindforge/Combat/DualAuraCombatDirector.cs").read_text()
    assert "return CanAcceptSelectionInternal(evt, false)" in window
    assert "CanAcceptSelectionInternal(simulated, true)" in window
    assert "NeuralSelectionIdentityV24.IsBoundToGameSession" in window
    assert "NEURAL_EFFECT_OBSERVED" in director
    assert "trialId: trialId" in director


def test_foreign_session_correlation_id_is_not_accepted_as_grounded():
    rows = good()
    rows[0] = marker(1, "NEURAL_SELECTION_OBSERVED", trial="selection-v24:foreign:7:44")
    assert "INVALID_SELECTION_CORRELATION_ID" in codes(rows)


def test_cross_epoch_correlation_id_is_rejected():
    rows = good()
    rows[0] = marker(1, "NEURAL_SELECTION_OBSERVED", trial="selection-v24:game-a:8:44")
    assert "INVALID_SELECTION_CORRELATION_ID" in codes(rows)


def test_loader_rejects_numeric_string_sequence_and_boolean_timestamp(tmp_path):
    p = tmp_path / "coercion.jsonl"
    valid = marker(1, "NEURAL_SELECTION_OBSERVED").to_dict()
    valid["seq"] = "1"
    p.write_text(json.dumps(valid) + "\n")
    with pytest.raises(ValueError, match="invalid strict marker wire types"):
        load_markers(p)

    valid["seq"] = 1
    valid["unity_realtime_s"] = True
    p.write_text(json.dumps(valid) + "\n")
    with pytest.raises(ValueError, match="invalid strict marker wire types"):
        load_markers(p)


def test_loader_rejects_nested_trial_identity(tmp_path):
    p = tmp_path / "bad.jsonl"
    valid = marker(1, "NEURAL_SELECTION_OBSERVED").to_dict()
    valid["trial_id"] = {"untrusted": True}
    p.write_text(json.dumps(valid) + "\n")
    with pytest.raises(ValueError, match="invalid strict marker wire field"):
        load_markers(p)
