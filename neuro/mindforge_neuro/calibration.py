from __future__ import annotations

from dataclasses import dataclass, replace
from itertools import combinations
from typing import Iterable
import numpy as np

from .config import AuraTarget, SsvepConfig
from .ssvep import SsvepDecoder


@dataclass(frozen=True)
class CalibrationProfile:
    model_id: str
    min_score: float
    min_margin: float
    training_accuracy: float
    accepted_fraction: float
    trials_per_target: dict[AuraTarget, int]


@dataclass(frozen=True)
class FrequencyPairEvaluation:
    """Derived participant evidence for one candidate SSVEP frequency pair.

    Scores are decoder-derived summary values only. Raw EEG is intentionally absent so the
    object is safe to serialize across the Unity boundary if desired.
    """

    sight_hz: float
    guard_hz: float
    usable_trials: int
    trials_per_frequency: dict[float, int]
    balanced_accuracy: float
    median_true_score: float
    median_margin: float
    mean_quality: float
    objective: float


@dataclass(frozen=True)
class ParticipantFrequencyProfile:
    """Participant-specific two-target frequency recommendation.

    This is a calibration recommendation, not a promise that every display can render the
    selected frequencies faithfully. Presentation timing still requires refresh/photodiode
    qualification on the actual device used for play.
    """

    selected_sight_hz: float
    selected_guard_hz: float
    evaluations: tuple[FrequencyPairEvaluation, ...]
    candidate_frequencies_hz: tuple[float, ...]
    model_id: str

    @property
    def best(self) -> FrequencyPairEvaluation:
        if not self.evaluations:
            raise ValueError("frequency profile has no pair evaluations")
        return self.evaluations[0]


def calibrate_decoder(decoder: SsvepDecoder, trials: Iterable[tuple[AuraTarget, np.ndarray]], *, model_id: str) -> CalibrationProfile:
    """Fit conservative score/margin gates from labeled session trials."""
    trials = list(trials)
    records: list[tuple[AuraTarget, AuraTarget, float, float]] = []
    counts = {AuraTarget.SIGHT: 0, AuraTarget.GUARD: 0}
    for truth, eeg in trials:
        counts[truth] += 1
        quality = decoder.decide(eeg).quality
        if quality.artifact:
            continue
        scores = decoder.score(eeg)
        ranked = sorted(scores.items(), key=lambda kv: kv[1], reverse=True)
        pred, top = ranked[0]
        margin = float(top - ranked[1][1])
        records.append((truth, pred, float(top), margin))

    if len(records) < 6 or min(counts.values()) < 3:
        raise ValueError("need at least 3 labeled trials per aura and 6 usable trials total")
    correct = [r for r in records if r[0] == r[1]]
    accuracy = len(correct) / len(records)
    if len(correct) < 4:
        raise ValueError("calibration produced too few correct trials for stable thresholds")

    correct_scores = np.asarray([r[2] for r in correct])
    correct_margins = np.asarray([r[3] for r in correct])
    min_score = max(decoder.config.min_score, float(np.quantile(correct_scores, 0.15)) * 0.90)
    min_margin = max(decoder.config.min_margin, float(np.quantile(correct_margins, 0.15)) * 0.80)
    decoder.set_thresholds(min_score=min_score, min_margin=min_margin)

    accepted = sum(int(decoder.decide(eeg).accepted and decoder.decide(eeg).target == truth)
                   for truth, eeg in trials)
    return CalibrationProfile(model_id, min_score, min_margin, accuracy,
                              accepted / max(1, sum(counts.values())), counts)


def rank_participant_frequency_pairs(
    trials: Iterable[tuple[float, np.ndarray]],
    *,
    base_config: SsvepConfig | None = None,
    model_id: str = "participant-frequency-ranking-v1",
    minimum_trials_per_frequency: int = 3,
    minimum_frequency_separation_hz: float = 1.5,
) -> ParticipantFrequencyProfile:
    """Rank candidate two-target SSVEP frequency pairs for one participant.

    ``trials`` contains the *actual displayed* nominal stimulus frequency and the EEG window
    captured for that trial. Every candidate pair is evaluated using the same filter-bank CCA
    implementation as gameplay. The objective prioritizes balanced classification accuracy,
    then separation margin, true-frequency response strength and usable signal quality.

    This function does not choose renderer timing, alter stimulus luminance, or export EEG.
    The selected pair must still pass device-specific refresh/photodiode qualification.
    """
    base = base_config or SsvepConfig()
    minimum_trials_per_frequency = max(2, int(minimum_trials_per_frequency))
    minimum_frequency_separation_hz = max(0.1, float(minimum_frequency_separation_hz))

    grouped: dict[float, list[np.ndarray]] = {}
    for raw_hz, eeg in trials:
        hz = round(float(raw_hz), 4)
        if hz <= 0.0:
            raise ValueError("stimulus frequencies must be positive")
        grouped.setdefault(hz, []).append(np.asarray(eeg, dtype=float))

    candidates = tuple(sorted(grouped))
    if len(candidates) < 2:
        raise ValueError("need trials from at least two candidate stimulus frequencies")

    evaluations: list[FrequencyPairEvaluation] = []
    for sight_hz, guard_hz in combinations(candidates, 2):
        if guard_hz - sight_hz < minimum_frequency_separation_hz:
            continue
        if min(len(grouped[sight_hz]), len(grouped[guard_hz])) < minimum_trials_per_frequency:
            continue

        config = replace(base, blue_frequency_hz=sight_hz, green_frequency_hz=guard_hz)
        config.validate()
        decoder = SsvepDecoder(config)

        correct = {AuraTarget.SIGHT: 0, AuraTarget.GUARD: 0}
        usable = {AuraTarget.SIGHT: 0, AuraTarget.GUARD: 0}
        true_scores: list[float] = []
        margins: list[float] = []
        qualities: list[float] = []

        for target, hz in ((AuraTarget.SIGHT, sight_hz), (AuraTarget.GUARD, guard_hz)):
            for eeg in grouped[hz]:
                decision = decoder.decide(eeg)
                if decision.quality.artifact or decision.quality.score < config.min_quality:
                    continue
                scores = decoder.score(eeg)
                ranked = sorted(scores.items(), key=lambda kv: kv[1], reverse=True)
                predicted, top_score = ranked[0]
                usable[target] += 1
                correct[target] += int(predicted == target)
                true_scores.append(float(scores[target]))
                margins.append(float(max(0.0, top_score - ranked[1][1])))
                qualities.append(float(decision.quality.score))

        if min(usable.values()) < minimum_trials_per_frequency:
            continue

        per_target_accuracy = [correct[t] / usable[t] for t in (AuraTarget.SIGHT, AuraTarget.GUARD)]
        balanced_accuracy = float(np.mean(per_target_accuracy))
        median_true_score = float(np.median(true_scores)) if true_scores else 0.0
        median_margin = float(np.median(margins)) if margins else 0.0
        mean_quality = float(np.mean(qualities)) if qualities else 0.0

        # Accuracy carries most of the decision. Margins/scores are bounded so one unusually
        # strong CCA trial cannot swamp repeatable classification performance.
        objective = (
            0.68 * balanced_accuracy
            + 0.14 * min(1.0, median_margin / 0.12)
            + 0.10 * min(1.0, median_true_score / 0.45)
            + 0.08 * mean_quality
        )
        evaluations.append(FrequencyPairEvaluation(
            sight_hz=sight_hz,
            guard_hz=guard_hz,
            usable_trials=sum(usable.values()),
            trials_per_frequency={sight_hz: usable[AuraTarget.SIGHT], guard_hz: usable[AuraTarget.GUARD]},
            balanced_accuracy=balanced_accuracy,
            median_true_score=median_true_score,
            median_margin=median_margin,
            mean_quality=mean_quality,
            objective=float(objective),
        ))

    if not evaluations:
        raise ValueError(
            "no candidate frequency pair had enough clean trials and required frequency separation")

    evaluations.sort(key=lambda item: (
        -item.objective,
        -item.balanced_accuracy,
        -item.median_margin,
        item.sight_hz,
        item.guard_hz,
    ))
    best = evaluations[0]
    return ParticipantFrequencyProfile(
        selected_sight_hz=best.sight_hz,
        selected_guard_hz=best.guard_hz,
        evaluations=tuple(evaluations),
        candidate_frequencies_hz=candidates,
        model_id=str(model_id),
    )


def personalized_ssvep_config(
    profile: ParticipantFrequencyProfile,
    *,
    base_config: SsvepConfig | None = None,
) -> SsvepConfig:
    """Create the gameplay decoder config chosen by participant calibration.

    This is intentionally a pure configuration transform. Calling it does not imply that the
    display has passed timing qualification and does not mutate a running decoder in place.
    """
    base = base_config or SsvepConfig()
    config = replace(
        base,
        blue_frequency_hz=float(profile.selected_sight_hz),
        green_frequency_hz=float(profile.selected_guard_hz),
    )
    config.validate()
    return config
