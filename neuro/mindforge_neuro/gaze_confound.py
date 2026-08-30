from __future__ import annotations

from dataclasses import dataclass, asdict
from math import isfinite
from typing import Iterable, Sequence

import numpy as np


@dataclass(frozen=True)
class EvidenceWindow:
    """One derived SSVEP decision window used for public or physical qualification.

    Raw EEG and raw eye images deliberately do not belong here. Dataset adapters should
    compute decoder scores and gaze geometry locally, then emit this bounded record.
    ``truth`` is ``sight``, ``guard`` or ``none``. ``condition`` is a free but stable
    experimental label such as ``overt``, ``covert``, ``dissociation`` or ``idle``.

    ``window_seconds`` is also the exposure represented by the row when computing idle
    false-activation rate. Adapters must therefore use non-overlapping idle windows or
    otherwise emit the actual non-duplicated exposure duration rather than sliding-window
    duration summed repeatedly.
    """

    subject_id: str
    truth: str
    condition: str
    sight_score: float
    guard_score: float
    quality: float = 1.0
    window_seconds: float = 1.0
    sight_eccentricity_deg: float | None = None
    guard_eccentricity_deg: float | None = None

    def validate(self) -> None:
        if self.truth not in {"sight", "guard", "none"}:
            raise ValueError(f"unsupported truth label: {self.truth}")
        if not self.subject_id:
            raise ValueError("subject_id is required")
        for value in (self.sight_score, self.guard_score, self.quality, self.window_seconds):
            if not isfinite(float(value)):
                raise ValueError("evidence values must be finite")
        if self.window_seconds <= 0:
            raise ValueError("window_seconds must be positive")
        for value in (self.sight_eccentricity_deg, self.guard_eccentricity_deg):
            if value is not None and (not isfinite(float(value)) or value < 0):
                raise ValueError("eccentricity must be finite and non-negative")


@dataclass(frozen=True)
class SelectionPolicy:
    """Gameplay-facing abstention policy for a two-target SSVEP decision window."""

    min_score: float = 0.15
    min_margin: float = 0.035
    min_quality: float = 0.55
    require_gaze_geometry: bool = False
    max_attended_eccentricity_deg: float = 6.0


@dataclass(frozen=True)
class PolicyMetrics:
    windows: int
    command_windows: int
    idle_windows: int
    accepted_commands: int
    correct_commands: int
    wrong_commands: int
    abstained_commands: int
    accepted_idle: int
    forced_choice_accuracy: float
    accepted_accuracy: float
    command_coverage: float
    idle_false_activations_per_minute: float
    mean_decision_seconds: float
    gaze_only_accuracy: float | None
    eeg_accuracy_when_gaze_disagrees: float | None
    gaze_disagreement_windows: int
    overt_accuracy: float | None
    covert_accuracy: float | None
    dissociation_accuracy: float | None
    median_peripheral_leakage_ratio: float | None

    def to_dict(self) -> dict[str, object]:
        return asdict(self)


@dataclass(frozen=True)
class GameDesignRecommendation:
    architecture: str
    promote_bci_authority: bool
    rationale: tuple[str, ...]

    def to_dict(self) -> dict[str, object]:
        return asdict(self)


def _winner(window: EvidenceWindow) -> tuple[str, float, float]:
    if window.sight_score >= window.guard_score:
        return "sight", float(window.sight_score), float(window.sight_score - window.guard_score)
    return "guard", float(window.guard_score), float(window.guard_score - window.sight_score)


def _gaze_winner(window: EvidenceWindow) -> str | None:
    if window.sight_eccentricity_deg is None or window.guard_eccentricity_deg is None:
        return None
    if window.sight_eccentricity_deg == window.guard_eccentricity_deg:
        return None
    return "sight" if window.sight_eccentricity_deg < window.guard_eccentricity_deg else "guard"


def decide(window: EvidenceWindow, policy: SelectionPolicy) -> str | None:
    window.validate()
    winner, score, margin = _winner(window)
    if window.quality < policy.min_quality or score < policy.min_score or margin < policy.min_margin:
        return None
    if policy.require_gaze_geometry:
        gaze_target = _gaze_winner(window)
        if gaze_target != winner:
            return None
        eccentricity = (
            window.sight_eccentricity_deg if winner == "sight" else window.guard_eccentricity_deg
        )
        if eccentricity is None or eccentricity > policy.max_attended_eccentricity_deg:
            return None
    return winner


def _accuracy(rows: Sequence[EvidenceWindow], *, accepted_only: bool, policy: SelectionPolicy) -> float | None:
    labeled = [w for w in rows if w.truth in {"sight", "guard"}]
    if not labeled:
        return None
    correct = 0
    denominator = 0
    for w in labeled:
        pred = decide(w, policy) if accepted_only else _winner(w)[0]
        if pred is None and accepted_only:
            continue
        denominator += 1
        correct += int(pred == w.truth)
    return correct / denominator if denominator else None


def evaluate_policy(windows: Iterable[EvidenceWindow], policy: SelectionPolicy) -> PolicyMetrics:
    rows = list(windows)
    if not rows:
        raise ValueError("at least one evidence window is required")
    for row in rows:
        row.validate()

    commands = [w for w in rows if w.truth in {"sight", "guard"}]
    idle = [w for w in rows if w.truth == "none"]

    forced_correct = sum(_winner(w)[0] == w.truth for w in commands)
    accepted_commands = 0
    correct_commands = 0
    wrong_commands = 0
    abstained_commands = 0
    accepted_idle = 0
    accepted_latencies: list[float] = []

    for w in commands:
        pred = decide(w, policy)
        if pred is None:
            abstained_commands += 1
            continue
        accepted_commands += 1
        accepted_latencies.append(w.window_seconds)
        if pred == w.truth:
            correct_commands += 1
        else:
            wrong_commands += 1

    for w in idle:
        if decide(w, policy) is not None:
            accepted_idle += 1

    idle_minutes = sum(w.window_seconds for w in idle) / 60.0
    false_per_minute = accepted_idle / idle_minutes if idle_minutes > 0 else 0.0

    gaze_labeled = [(w, _gaze_winner(w)) for w in commands]
    gaze_labeled = [(w, g) for w, g in gaze_labeled if g is not None]
    gaze_only_accuracy = (
        sum(g == w.truth for w, g in gaze_labeled) / len(gaze_labeled)
        if gaze_labeled else None
    )
    disagreements = [(w, g) for w, g in gaze_labeled if g != w.truth]
    eeg_when_gaze_wrong = (
        sum(_winner(w)[0] == w.truth for w, _ in disagreements) / len(disagreements)
        if disagreements else None
    )

    leakage: list[float] = []
    for w in commands:
        gaze = _gaze_winner(w)
        if gaze is None or gaze != w.truth:
            continue
        target = w.sight_score if w.truth == "sight" else w.guard_score
        non_target = w.guard_score if w.truth == "sight" else w.sight_score
        if target > 1e-9:
            leakage.append(max(0.0, float(non_target) / float(target)))

    def condition_accuracy(name: str) -> float | None:
        subset = [w for w in commands if w.condition.lower() == name]
        return _accuracy(subset, accepted_only=False, policy=policy)

    return PolicyMetrics(
        windows=len(rows),
        command_windows=len(commands),
        idle_windows=len(idle),
        accepted_commands=accepted_commands,
        correct_commands=correct_commands,
        wrong_commands=wrong_commands,
        abstained_commands=abstained_commands,
        accepted_idle=accepted_idle,
        forced_choice_accuracy=forced_correct / len(commands) if commands else 0.0,
        accepted_accuracy=correct_commands / accepted_commands if accepted_commands else 0.0,
        command_coverage=accepted_commands / len(commands) if commands else 0.0,
        idle_false_activations_per_minute=float(false_per_minute),
        mean_decision_seconds=float(np.mean(accepted_latencies)) if accepted_latencies else 0.0,
        gaze_only_accuracy=gaze_only_accuracy,
        eeg_accuracy_when_gaze_disagrees=eeg_when_gaze_wrong,
        gaze_disagreement_windows=len(disagreements),
        overt_accuracy=condition_accuracy("overt"),
        covert_accuracy=condition_accuracy("covert"),
        dissociation_accuracy=condition_accuracy("dissociation"),
        median_peripheral_leakage_ratio=float(np.median(leakage)) if leakage else None,
    )


def gameplay_loss(metrics: PolicyMetrics, *, wrong_cost: float = 8.0, abstain_cost: float = 0.65,
                  false_activation_cost: float = 12.0, latency_cost_per_second: float = 0.25) -> float:
    """Risk score for game control. Lower is better.

    Wrong/idle activations are intentionally much more expensive than abstention. A game can
    gracefully say "signal unclear"; it should not randomly spend a neural ability.
    """
    command_n = max(1, metrics.command_windows)
    wrong_rate = metrics.wrong_commands / command_n
    abstain_rate = metrics.abstained_commands / command_n
    return float(
        wrong_cost * wrong_rate
        + abstain_cost * abstain_rate
        + false_activation_cost * metrics.idle_false_activations_per_minute
        + latency_cost_per_second * metrics.mean_decision_seconds
    )


def tune_policy(
    train_windows: Iterable[EvidenceWindow],
    *,
    score_grid: Sequence[float] = (0.10, 0.15, 0.20, 0.25, 0.30),
    margin_grid: Sequence[float] = (0.02, 0.035, 0.05, 0.075, 0.10),
    quality_grid: Sequence[float] = (0.45, 0.55, 0.65),
    require_gaze_geometry: bool = False,
) -> tuple[SelectionPolicy, PolicyMetrics]:
    rows = list(train_windows)
    if not rows:
        raise ValueError("training windows are required")
    ranked: list[tuple[float, float, SelectionPolicy, PolicyMetrics]] = []
    for score in score_grid:
        for margin in margin_grid:
            for quality in quality_grid:
                policy = SelectionPolicy(float(score), float(margin), float(quality), require_gaze_geometry)
                metrics = evaluate_policy(rows, policy)
                # Tie-break toward higher accepted accuracy and then more coverage.
                ranked.append((gameplay_loss(metrics), -metrics.accepted_accuracy, policy, metrics))
    ranked.sort(key=lambda item: (item[0], item[1], -item[3].command_coverage))
    _, _, policy, metrics = ranked[0]
    return policy, metrics


def recommend_game_architecture(metrics: PolicyMetrics) -> GameDesignRecommendation:
    """Translate evidence into a conservative product architecture.

    These are engineering promotion gates, not universal neurophysiology constants. They are
    deliberately strict because an erroneous neural action is more damaging to action-game feel
    than a graceful abstention. A dataset without idle/no-attention windows cannot establish the
    asynchronous safety gate and therefore cannot promote production BCI authority by itself.
    """
    reasons: list[str] = []
    reliable = metrics.accepted_accuracy >= 0.90 and metrics.command_coverage >= 0.55
    quiet = metrics.idle_windows > 0 and metrics.idle_false_activations_per_minute <= 0.25
    if not reliable:
        reasons.append("accepted command accuracy/coverage does not yet support gameplay authority")
    if metrics.idle_windows <= 0:
        reasons.append("no idle/no-attention exposure was measured, so false-activation safety is unqualified")
    elif not quiet:
        reasons.append("idle false-activation rate is too high for gameplay authority")

    covert = metrics.covert_accuracy
    dissociation = metrics.dissociation_accuracy
    gaze_increment = metrics.eeg_accuracy_when_gaze_disagrees

    if reliable and quiet and covert is not None and covert >= 0.80 and (
        dissociation is None or dissociation >= 0.75
    ):
        reasons.append("covert/dissociated attention remains sufficiently decodable")
        return GameDesignRecommendation("TRIGGERED_COVERT_SSVEP", True, tuple(reasons))

    if reliable and quiet:
        if gaze_increment is not None and metrics.gaze_disagreement_windows >= 10 and gaze_increment >= 0.70:
            reasons.append("EEG retains useful information on trials where gaze points toward the wrong target")
        reasons.append("use a player-armed short resonance window; the arm action never chooses Sight or Guard")
        reasons.append("keep both coded targets at controlled retinal geometry and dynamically stop or abstain")
        return GameDesignRecommendation("TRIGGERED_OVERT_SSVEP", True, tuple(reasons))

    reasons.append("keep the BCI experimental and preserve conventional gameplay fallback")
    return GameDesignRecommendation("EXPERIMENT_ONLY", False, tuple(reasons))
