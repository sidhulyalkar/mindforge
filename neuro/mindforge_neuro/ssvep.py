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
    """Two-target FBCCA decoder with cached filters and cumulative-window scoring.

    The original fixed-window API remains unchanged for calibration/replay. V0.14 adds
    ``score_window``/``decide_window`` so a single Unity resonance epoch can be checked
    at progressively longer evidence durations without inventing overlapping dwell trials.
    """

    def __init__(self, config: SsvepConfig | None = None):
        self.config = config or SsvepConfig()
        self.config.validate()
        self.min_score = self.config.min_score
        self.min_margin = self.config.min_margin
        self._reference_cache: dict[int, dict[AuraTarget, np.ndarray]] = {}
        self._filter_sos: list[np.ndarray] = []
        nyquist = self.config.sample_rate_hz / 2.0
        for low, high in self.config.filter_bands_hz:
            high = min(high, nyquist * 0.95)
            self._filter_sos.append(
                butter(4, [low / nyquist, high / nyquist], btype="bandpass", output="sos")
            )
        self._references_for(self.config.window_samples)

    def set_thresholds(self, *, min_score: float, min_margin: float) -> None:
        self.min_score = float(min_score)
        self.min_margin = float(min_margin)

    def _references_for(self, samples: int) -> dict[AuraTarget, np.ndarray]:
        samples = int(samples)
        refs = self._reference_cache.get(samples)
        if refs is None:
            refs = {
                target: reference_bank(freq, self.config.sample_rate_hz, samples, self.config.harmonics)
                for target, freq in self.config.target_frequencies.items()
            }
            self._reference_cache[samples] = refs
        return refs

    def _filter(self, eeg_uv: np.ndarray, bank_index: int) -> np.ndarray:
        return sosfiltfilt(self._filter_sos[bank_index], eeg_uv, axis=1)

    def score(self, eeg_uv: np.ndarray) -> dict[AuraTarget, float]:
        x = np.asarray(eeg_uv, dtype=float)
        if x.ndim != 2 or x.shape[1] != self.config.window_samples:
            raise ValueError(f"expected (channels, {self.config.window_samples}) EEG window, got {x.shape}")
        return self.score_window(x)

    def score_window(self, eeg_uv: np.ndarray) -> dict[AuraTarget, float]:
        x = np.asarray(eeg_uv, dtype=float)
        if x.ndim != 2 or x.shape[1] < 64:
            raise ValueError(f"expected (channels, >=64) EEG window, got {x.shape}")
        indices = np.asarray(self.config.decode_channel_indices, dtype=int)
        if np.any(indices >= x.shape[0]):
            raise ValueError(f"decode channel index exceeds EEG channel count {x.shape[0]}")
        x_decode = x[indices]
        refs_for_length = self._references_for(x.shape[1])
        aggregate = {target: 0.0 for target in self.config.target_frequencies}
        total_weight = float(sum(self.config.filter_bank_weights))
        for bank_index, weight in enumerate(self.config.filter_bank_weights):
            filtered = self._filter(x_decode, bank_index)
            for target, refs in refs_for_length.items():
                rho = canonical_correlation(filtered, refs)
                aggregate[target] += weight * rho * rho
        return {target: value / total_weight for target, value in aggregate.items()}

    def decide(self, eeg_uv: np.ndarray) -> SsvepDecision:
        x = np.asarray(eeg_uv, dtype=float)
        if x.ndim != 2 or x.shape[1] != self.config.window_samples:
            raise ValueError(f"expected (channels, {self.config.window_samples}) EEG window, got {x.shape}")
        return self.decide_window(x)

    def decide_window(
        self,
        eeg_uv: np.ndarray,
        *,
        min_score: float | None = None,
        min_margin: float | None = None,
    ) -> SsvepDecision:
        x = np.asarray(eeg_uv, dtype=float)
        quality = assess_window_quality(x, self.config.sample_rate_hz)
        if quality.artifact or quality.score < self.config.min_quality:
            return SsvepDecision(None, {t: 0.0 for t in self.config.target_frequencies}, 0.0, 0.0,
                                 quality, False, quality.reason or "LOW_QUALITY")

        scores = self.score_window(x)
        ranked = sorted(scores.items(), key=lambda kv: kv[1], reverse=True)
        winner, top = ranked[0]
        second = ranked[1][1]
        margin = float(top - second)
        confidence = float(np.clip(0.5 + 0.5 * margin / max(top, 1e-9), 0.0, 1.0))
        score_gate = self.min_score if min_score is None else float(min_score)
        margin_gate = self.min_margin if min_margin is None else float(min_margin)
        if top < score_gate:
            return SsvepDecision(winner, scores, confidence, margin, quality, False, "LOW_SCORE")
        if margin < margin_gate:
            return SsvepDecision(winner, scores, confidence, margin, quality, False, "LOW_MARGIN")
        return SsvepDecision(winner, scores, confidence, margin, quality, True, None)
