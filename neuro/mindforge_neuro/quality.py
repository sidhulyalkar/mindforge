from __future__ import annotations

from dataclasses import dataclass
import numpy as np


@dataclass(frozen=True)
class SignalQuality:
    score: float
    artifact: bool
    reason: str | None
    usable_channels: int
    central_hf_rms_uv: float = 0.0
    common_mode_peak_uv: float = 0.0
    max_derivative_uv_per_s: float = 0.0


def _band_rms(x: np.ndarray, sample_rate_hz: float, low_hz: float, high_hz: float) -> np.ndarray:
    """Per-channel FFT-band RMS used only as a conservative artifact proxy."""
    n = x.shape[1]
    centered = x - np.mean(x, axis=1, keepdims=True)
    spectrum = np.fft.rfft(centered, axis=1)
    frequencies = np.fft.rfftfreq(n, d=1.0 / sample_rate_hz)
    mask = (frequencies >= low_hz) & (frequencies <= high_hz)
    if not np.any(mask):
        return np.zeros(x.shape[0], dtype=float)
    # One-sided Parseval scaling. Absolute calibration is less important here
    # than deterministic relative sensitivity to broad high-frequency energy.
    power = 2.0 * np.sum(np.abs(spectrum[:, mask]) ** 2, axis=1) / float(n * n)
    return np.sqrt(np.maximum(power, 0.0))


def assess_window_quality(eeg_uv: np.ndarray, sample_rate_hz: float = 250.0) -> SignalQuality:
    """Fail closed on obviously unreliable EEG windows.

    Input is expected in microvolts with shape ``(channels, samples)``. These
    checks do not claim to remove or identify artifacts physiologically. They
    only decide whether a window should retain authority to affect gameplay.

    Thresholds are provisional engineering defaults selected against synthetic
    stress cases. Physical Unicorn sessions must retune and validate them.
    """
    x = np.asarray(eeg_uv, dtype=float)
    if x.ndim != 2 or x.shape[1] < 8 or sample_rate_hz <= 0:
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

    centered = x - np.median(x, axis=1, keepdims=True)
    common_mode = np.median(centered, axis=0)
    common_mode_peak = float(np.max(np.abs(common_mode)))
    derivative = np.diff(x, axis=1) * sample_rate_hz
    max_derivative = float(np.median(np.max(np.abs(derivative), axis=1))) if derivative.size else 0.0

    if x.shape[0] >= 4:
        central_indices = np.asarray([1, 2, 3], dtype=int)  # C3/Cz/C4 in the Unicorn montage
    else:
        central_indices = np.arange(x.shape[0])
    hf = _band_rms(x, sample_rate_hz, 35.0, min(90.0, sample_rate_hz * 0.45))
    central_hf = float(np.median(hf[central_indices])) if central_indices.size else 0.0

    if usable_count == 0:
        return SignalQuality(0.0, True, "NO_USABLE_CHANNELS", 0, central_hf, common_mode_peak, max_derivative)

    fraction = usable_count / x.shape[0]
    robust_peak = float(np.median(peak[usable]))
    peak_penalty = np.clip((robust_peak - 80.0) / 220.0, 0.0, 0.45)
    hf_penalty = np.clip((central_hf - 3.5) / 18.0, 0.0, 0.25)
    score = float(np.clip(fraction - peak_penalty - hf_penalty, 0.0, 1.0))

    if usable_count < max(2, x.shape[0] // 2):
        return SignalQuality(score, True, "TOO_FEW_CHANNELS", usable_count, central_hf, common_mode_peak, max_derivative)
    if saturated.any():
        return SignalQuality(score, True, "SATURATION", usable_count, central_hf, common_mode_peak, max_derivative)
    if common_mode_peak > 28.0:
        return SignalQuality(score, True, "COMMON_MODE_TRANSIENT", usable_count, central_hf, common_mode_peak, max_derivative)
    if max_derivative > 8500.0:
        return SignalQuality(score, True, "FAST_TRANSIENT", usable_count, central_hf, common_mode_peak, max_derivative)
    if central_hf > 6.0:
        return SignalQuality(score, True, "EMG_SUSPECTED", usable_count, central_hf, common_mode_peak, max_derivative)

    return SignalQuality(score, False, None, usable_count, central_hf, common_mode_peak, max_derivative)
