from pathlib import Path
import json
import sys

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "neuro"))

from mindforge_neuro.gaze_confound import (  # noqa: E402
    EvidenceWindow,
    SelectionPolicy,
    decide,
    evaluate_policy,
    gameplay_loss,
    recommend_game_architecture,
    tune_policy,
)


def w(truth, sight, guard, *, condition="overt", se=None, ge=None, seconds=1.0):
    return EvidenceWindow(
        subject_id="S01",
        truth=truth,
        condition=condition,
        sight_score=sight,
        guard_score=guard,
        quality=0.9,
        window_seconds=seconds,
        sight_eccentricity_deg=se,
        guard_eccentricity_deg=ge,
    )


def test_policy_abstains_on_small_margin_and_idle():
    policy = SelectionPolicy(min_score=0.2, min_margin=0.08, min_quality=0.5)
    assert decide(w("sight", 0.55, 0.18), policy) == "sight"
    assert decide(w("guard", 0.32, 0.37), policy) is None
    assert decide(w("none", 0.12, 0.10), policy) is None


def test_gaze_gate_never_creates_a_selection():
    policy = SelectionPolicy(
        min_score=0.2,
        min_margin=0.05,
        min_quality=0.5,
        require_gaze_geometry=True,
        max_attended_eccentricity_deg=6.0,
    )
    # Strong Sight EEG, but gaze geometry says Guard is closer: abstain rather than override EEG.
    assert decide(w("sight", 0.60, 0.20, se=8.0, ge=1.0), policy) is None
    # Gaze agrees and is within the calibrated geometry: EEG remains the selecting evidence.
    assert decide(w("sight", 0.60, 0.20, se=1.0, ge=8.0), policy) == "sight"
    # Gaze cannot rescue weak EEG.
    assert decide(w("sight", 0.20, 0.18, se=1.0, ge=8.0), policy) is None


def test_metrics_separate_protocol_leakage_from_gaze_verified_leakage():
    rows = [
        w("sight", 0.60, 0.18, se=1.0, ge=9.0),
        w("guard", 0.20, 0.65, se=9.0, ge=1.0),
        # Deliberate dissociation: this row contributes to protocol leakage, but must not be
        # called gaze-verified peripheral leakage because gaze is nearer the wrong target.
        w("guard", 0.22, 0.58, condition="dissociation", se=1.0, ge=9.0),
        w("none", 0.10, 0.08, condition="idle", se=5.0, ge=5.0, seconds=2.0),
    ]
    metrics = evaluate_policy(rows, SelectionPolicy(min_score=0.15, min_margin=0.05, min_quality=0.5))
    assert metrics.forced_choice_accuracy == 1.0
    assert metrics.accepted_accuracy == 1.0
    assert metrics.gaze_only_accuracy == 2 / 3
    assert metrics.gaze_disagreement_windows == 1
    assert metrics.eeg_accuracy_when_gaze_disagrees == 1.0
    assert metrics.dissociation_accuracy == 1.0
    assert 0.0 < metrics.median_non_target_score_ratio < 1.0
    assert 0.0 < metrics.median_peripheral_leakage_ratio < 1.0
    assert metrics.idle_false_activations_per_minute == 0.0


def test_protocol_leakage_does_not_require_eye_tracking():
    metrics = evaluate_policy(
        [
            w("sight", 0.60, 0.18),
            w("guard", 0.20, 0.65),
        ],
        SelectionPolicy(min_score=0.15, min_margin=0.05, min_quality=0.5),
    )
    assert metrics.median_non_target_score_ratio is not None
    assert metrics.median_peripheral_leakage_ratio is None


def test_gameplay_loss_penalizes_wrong_commands_more_than_abstention():
    wrong = evaluate_policy(
        [w("sight", 0.20, 0.60)],
        SelectionPolicy(min_score=0.1, min_margin=0.01, min_quality=0.5),
    )
    abstain = evaluate_policy(
        [w("sight", 0.20, 0.19)],
        SelectionPolicy(min_score=0.1, min_margin=0.05, min_quality=0.5),
    )
    assert gameplay_loss(wrong) > gameplay_loss(abstain)


def test_tuning_prefers_safe_thresholds_when_idle_is_ambiguous():
    rows = [
        w("sight", 0.62, 0.18),
        w("sight", 0.58, 0.20),
        w("guard", 0.20, 0.64),
        w("guard", 0.22, 0.59),
        # Idle evidence is strong enough to cross a permissive 0.02 margin, but not 0.05.
        w("none", 0.27, 0.23, condition="idle", seconds=1.0),
        w("none", 0.26, 0.22, condition="idle", seconds=1.0),
    ]
    policy, metrics = tune_policy(
        rows,
        score_grid=(0.10, 0.20),
        margin_grid=(0.02, 0.05),
        quality_grid=(0.5,),
    )
    assert policy.min_margin == 0.05
    assert metrics.idle_false_activations_per_minute == 0.0
    assert metrics.accepted_accuracy == 1.0


def test_recommendation_prefers_triggered_not_always_listening_control():
    rows = []
    for _ in range(12):
        rows.append(w("sight", 0.60, 0.15, se=1.0, ge=9.0))
        rows.append(w("guard", 0.15, 0.60, se=9.0, ge=1.0))
    rows.extend(w("none", 0.08, 0.07, condition="idle", seconds=1.0) for _ in range(120))
    metrics = evaluate_policy(rows, SelectionPolicy(min_score=0.15, min_margin=0.05, min_quality=0.5))
    recommendation = recommend_game_architecture(metrics)
    assert recommendation.promote_bci_authority
    assert recommendation.architecture == "TRIGGERED_OVERT_SSVEP"
    assert any("player-armed" in reason for reason in recommendation.rationale)


def test_evidence_contract_excludes_raw_biometric_payloads():
    schema = json.loads((ROOT / "contracts" / "ssvep_evidence_window.v1.schema.json").read_text())
    props = set(schema["properties"])
    assert schema["additionalProperties"] is False
    assert {"sight_score", "guard_score", "sight_eccentricity_deg", "guard_eccentricity_deg"} <= props
    forbidden = {"eeg", "samples", "channels", "raw_gaze", "eye_image", "pupil_video"}
    assert not (forbidden & props)
