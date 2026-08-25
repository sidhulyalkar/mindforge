from __future__ import annotations

from dataclasses import dataclass
import numpy as np


@dataclass(frozen=True)
class SignalQuality:
    score: float
    artifact: bool
    reason: str | None
    usable_channels: int


def assess_window_quality(eeg_uv: np.ndarray) -> SignalQuality:
    """Conservative gate for obvious unusable EEG windows.

    Input is expected as microvolts, shape (channels, samples). This is a gate,
    not a claim that artifacts are fully removed.
    """
    x = np.asarray(eeg_uv, dtype=float)
    if x.ndim != 2 or x.shape[1] < 8:
        return SignalQuality(0.0, True, "BAD_SHAPE", 0)
    if not np.isfinite(x).all():
        return SignalQuality(0.0, True, "NONFINITE", 0)

    std = np.std(x, axis=1)
    peak = np.max(np.abs(x), axis=1)
    flat = std < 0.20
    saturated = peak > 300.0
    extreme_variance = std > 120.0
    usable = ~(flat | saturated | extreme_variance)
    usable_count = int(np.sum(usable))
    if usable_count == 0:
        return SignalQuality(0.0, True, "NO_USABLE_CHANNELS", 0)

    fraction = usable_count / x.shape[0]
    robust_peak = float(np.median(peak[usable]))
    peak_penalty = np.clip((robust_peak - 80.0) / 220.0, 0.0, 0.45)
    score = float(np.clip(fraction - peak_penalty, 0.0, 1.0))
    if usable_count < max(2, x.shape[0] // 2):
        return SignalQuality(score, True, "TOO_FEW_CHANNELS", usable_count)
    if saturated.any():
        return SignalQuality(score, True, "SATURATION", usable_count)
    return SignalQuality(score, False, None, usable_count)
