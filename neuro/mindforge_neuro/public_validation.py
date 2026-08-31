from __future__ import annotations

from dataclasses import asdict, dataclass
from typing import Iterable

import numpy as np

from .gaze_confound import (
    EvidenceWindow,
    PolicyMetrics,
    SelectionPolicy,
    evaluate_policy,
    gameplay_loss,
    tune_policy,
)


@dataclass(frozen=True)
class HeldOutSubjectResult:
    """One leave-one-subject-out policy-selection fold."""

    subject_id: str
    train_subjects: int
    policy: SelectionPolicy
    metrics: PolicyMetrics
    gameplay_loss: float

    def to_dict(self) -> dict[str, object]:
        return asdict(self)


@dataclass(frozen=True)
class CrossValidatedCohort:
    """Subject-balanced public-data qualification summary.

    Thresholds are selected on every subject except the held-out subject. This prevents a
    permissive subject-specific threshold from being evaluated on the same data that selected it.
    The result is deliberately stricter than the participant-calibrated production path, but is a
    useful first test of whether the game architecture generalizes at all.
    """

    subjects: int
    folds: tuple[HeldOutSubjectResult, ...]
    median_accepted_accuracy: float
    p10_accepted_accuracy: float
    median_command_coverage: float
    p10_command_coverage: float
    median_idle_false_activations_per_minute: float | None
    p90_idle_false_activations_per_minute: float | None
    median_gameplay_loss: float
    responder_fraction: float
    idle_qualified_subject_fraction: float

    def to_dict(self) -> dict[str, object]:
        return asdict(self)


def leave_one_subject_out_validation(
    windows: Iterable[EvidenceWindow],
    *,
    require_gaze_geometry: bool = False,
) -> CrossValidatedCohort:
    """Tune policy on N-1 subjects and evaluate exactly once on the held-out subject.

    This routine evaluates policy/threshold transfer, not a trained EEG spatial model. Dataset
    adapters must fit any data-driven EEG model (for example TRCA templates) strictly inside the
    corresponding training/calibration split before emitting held-out EvidenceWindow scores.
    """
    rows = list(windows)
    if not rows:
        raise ValueError("evidence windows are required")
    for row in rows:
        row.validate()

    subjects = sorted({row.subject_id for row in rows})
    if len(subjects) < 2:
        raise ValueError("leave-one-subject-out validation requires at least two subjects")

    folds: list[HeldOutSubjectResult] = []
    for subject in subjects:
        train = [row for row in rows if row.subject_id != subject]
        test = [row for row in rows if row.subject_id == subject]
        if not any(row.truth in {"sight", "guard"} for row in train):
            raise ValueError(f"training fold for {subject} contains no command windows")
        if not any(row.truth in {"sight", "guard"} for row in test):
            raise ValueError(f"held-out subject {subject} contains no command windows")

        policy, _ = tune_policy(train, require_gaze_geometry=require_gaze_geometry)
        metrics = evaluate_policy(test, policy)
        folds.append(HeldOutSubjectResult(
            subject_id=subject,
            train_subjects=len(subjects) - 1,
            policy=policy,
            metrics=metrics,
            gameplay_loss=gameplay_loss(metrics),
        ))

    accuracies = np.asarray([fold.metrics.accepted_accuracy for fold in folds], dtype=float)
    coverage = np.asarray([fold.metrics.command_coverage for fold in folds], dtype=float)
    losses = np.asarray([fold.gameplay_loss for fold in folds], dtype=float)
    idle_folds = [fold for fold in folds if fold.metrics.idle_windows > 0]
    idle_far = np.asarray(
        [fold.metrics.idle_false_activations_per_minute for fold in idle_folds], dtype=float
    )

    def is_responder(fold: HeldOutSubjectResult) -> bool:
        metrics = fold.metrics
        return (
            metrics.accepted_accuracy >= 0.90
            and metrics.command_coverage >= 0.55
            and metrics.idle_windows > 0
            and metrics.idle_false_activations_per_minute <= 0.25
        )

    return CrossValidatedCohort(
        subjects=len(subjects),
        folds=tuple(folds),
        median_accepted_accuracy=float(np.median(accuracies)),
        p10_accepted_accuracy=float(np.quantile(accuracies, 0.10)),
        median_command_coverage=float(np.median(coverage)),
        p10_command_coverage=float(np.quantile(coverage, 0.10)),
        median_idle_false_activations_per_minute=(
            float(np.median(idle_far)) if idle_far.size else None
        ),
        p90_idle_false_activations_per_minute=(
            float(np.quantile(idle_far, 0.90)) if idle_far.size else None
        ),
        median_gameplay_loss=float(np.median(losses)),
        responder_fraction=float(np.mean([is_responder(fold) for fold in folds])),
        idle_qualified_subject_fraction=len(idle_folds) / len(folds),
    )
