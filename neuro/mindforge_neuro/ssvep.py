from __future__ import annotations

from dataclasses import dataclass
import numpy as np
from scipy.signal import butter, sosfiltfilt

from .config import AuraTarget, SsvepConfig
from .quality import SignalQuality, assess_window_quality


@dataclass(frozen=True)
class SsvepDecision:
    target: AuraTarget | None
    scores: dict[AuraTarget, float]
    confidence: float
    margin: float
    quality: SignalQuality
    accepted: bool
    reason: str | None


def _invsqrt_psd(matrix: np.ndarray, floor: float = 1e-8) -> np.ndarray:
    vals, vecs = np.linalg.eigh(matrix)
    vals = np.maximum(vals, floor)
    return (vecs * (1.0 / np.sqrt(vals))) @ vecs.T


def canonical_correlation(x: np.ndarray, y: np.ndarray, regularization: float = 1e-4) -> float:
    x = np.asarray(x, dtype=float)
    y = np.asarray(y, dtype=float)
    if x.ndim != 2 or y.ndim != 2 or x.shape[1] != y.shape[1]:
        raise ValueError("x and y must be 2D with equal sample count")
    x = x - np.mean(x, axis=1, keepdims=True)
    y = y - np.mean(y, axis=1, keepdims=True)
    n = max(1, x.shape[1] - 1)
    cxx = (x @ x.T) / n + regularization * np.eye(x.shape[0])
    cyy = (y @ y.T) / n + regularization * np.eye(y.shape[0])
    cxy = (x @ y.T) / n
    whitened = _invsqrt_psd(cxx) @ cxy @ _invsqrt_psd(cyy)
    singular = np.linalg.svd(whitened, compute_uv=False)
    return float(np.clip(singular[0] if singular.size else 0.0, 0.0, 1.0))


def reference_bank(frequency_hz: float, sample_rate_hz: float, samples: int, harmonics: int) -> np.ndarray:
    t = np.arange(samples, dtype=float) / sample_rate_hz
    rows: list[np.ndarray] = []
    for harmonic in range(1, harmonics + 1):
        phase = 2.0 * np.pi * frequency_hz * harmonic * t
        rows.extend((np.sin(phase), np.cos(phase)))
    return np.stack(rows, axis=0)


class SsvepDecoder:
    """Two-target filter-bank CCA decoder for the Mindforge soul auras."""

    def __init__(self, config: SsvepConfig | None = None):
        self.config = config or SsvepConfig()
        self.config.validate()
        self._refs = {
            target: reference_bank(freq, self.config.sample_rate_hz,
                                   self.config.window_samples, self.config.harmonics)
            for target, freq in self.config.target_frequencies.items()
        }
        self.min_score = self.config.min_score
        self.min_margin = self.config.min_margin

    def set_thresholds(self, *, min_score: float, min_margin: float) -> None:
        self.min_score = float(min_score)
        self.min_margin = float(min_margin)

    def _filter(self, eeg_uv: np.ndarray, low: float, high: float) -> np.ndarray:
        nyquist = self.config.sample_rate_hz / 2.0
        high = min(high, nyquist * 0.95)
        sos = butter(4, [low / nyquist, high / nyquist], btype="bandpass", output="sos")
        return sosfiltfilt(sos, eeg_uv, axis=1)

    def score(self, eeg_uv: np.ndarray) -> dict[AuraTarget, float]:
        x = np.asarray(eeg_uv, dtype=float)
        if x.ndim != 2 or x.shape[1] != self.config.window_samples:
            raise ValueError(f"expected (channels, {self.config.window_samples}) EEG window, got {x.shape}")
        aggregate = {target: 0.0 for target in self.config.target_frequencies}
        total_weight = float(sum(self.config.filter_bank_weights))
        for band, weight in zip(self.config.filter_bands_hz, self.config.filter_bank_weights):
            filtered = self._filter(x, *band)
            for target, refs in self._refs.items():
                rho = canonical_correlation(filtered, refs)
                aggregate[target] += weight * rho * rho
        return {target: value / total_weight for target, value in aggregate.items()}

    def decide(self, eeg_uv: np.ndarray) -> SsvepDecision:
        quality = assess_window_quality(eeg_uv)
        if quality.artifact or quality.score < self.config.min_quality:
            return SsvepDecision(None, {t: 0.0 for t in self.config.target_frequencies}, 0.0, 0.0,
                                 quality, False, quality.reason or "LOW_QUALITY")

        scores = self.score(eeg_uv)
        ranked = sorted(scores.items(), key=lambda kv: kv[1], reverse=True)
        winner, top = ranked[0]
        second = ranked[1][1]
        margin = float(top - second)
        # Monotonic control/display score, not a calibrated posterior probability.
        confidence = float(np.clip(0.5 + 0.5 * margin / max(top, 1e-9), 0.0, 1.0))
        if top < self.min_score:
            return SsvepDecision(winner, scores, confidence, margin, quality, False, "LOW_SCORE")
        if margin < self.min_margin:
            return SsvepDecision(winner, scores, confidence, margin, quality, False, "LOW_MARGIN")
        return SsvepDecision(winner, scores, confidence, margin, quality, True, None)
