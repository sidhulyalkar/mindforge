from __future__ import annotations

from dataclasses import dataclass
from typing import Iterable
import numpy as np

from .config import AuraTarget
from .ssvep import SsvepDecoder


@dataclass(frozen=True)
class CalibrationProfile:
    model_id: str
    min_score: float
    min_margin: float
    training_accuracy: float
    accepted_fraction: float
    trials_per_target: dict[AuraTarget, int]


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
