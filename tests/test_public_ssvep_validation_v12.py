from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "neuro"))

from mindforge_neuro.gaze_confound import (  # noqa: E402
    EvidenceWindow,
    SelectionPolicy,
    evaluate_policy,
    recommend_game_architecture,
)
from mindforge_neuro.public_validation import leave_one_subject_out_validation  # noqa: E402


def row(subject, truth, sight, guard, *, condition="overt", seconds=1.0):
    return EvidenceWindow(
        subject_id=subject,
        truth=truth,
        condition=condition,
        sight_score=sight,
        guard_score=guard,
        quality=0.9,
        window_seconds=seconds,
    )


def good_subject(subject):
    rows = []
    for _ in range(6):
        rows.append(row(subject, "sight", 0.62, 0.18))
        rows.append(row(subject, "guard", 0.18, 0.62))
    rows.extend(row(subject, "none", 0.08, 0.07, condition="idle") for _ in range(60))
    return rows


def test_loso_tunes_without_heldout_subject_leakage():
    rows = good_subject("S01") + good_subject("S02") + good_subject("S03")
    cohort = leave_one_subject_out_validation(rows)
    assert cohort.subjects == 3
    assert len(cohort.folds) == 3
    assert {fold.subject_id for fold in cohort.folds} == {"S01", "S02", "S03"}
    assert all(fold.train_subjects == 2 for fold in cohort.folds)
    assert cohort.median_accepted_accuracy == 1.0
    assert cohort.median_command_coverage == 1.0
    assert cohort.responder_fraction == 1.0
    assert cohort.idle_qualified_subject_fraction == 1.0


def test_no_idle_dataset_cannot_promote_production_authority():
    rows = [
        row("S01", "sight", 0.62, 0.18),
        row("S01", "guard", 0.18, 0.62),
    ]
    metrics = evaluate_policy(rows, SelectionPolicy(min_score=0.15, min_margin=0.05, min_quality=0.5))
    recommendation = recommend_game_architecture(metrics)
    assert recommendation.architecture == "EXPERIMENT_ONLY"
    assert not recommendation.promote_bci_authority
    assert any("idle/no-attention" in reason for reason in recommendation.rationale)


def test_loso_requires_more_than_one_subject():
    try:
        leave_one_subject_out_validation(good_subject("S01"))
    except ValueError as exc:
        assert "at least two subjects" in str(exc)
    else:
        raise AssertionError("expected a subject-count validation error")
