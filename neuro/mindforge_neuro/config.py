from __future__ import annotations

from dataclasses import dataclass
from enum import Enum


class AuraTarget(str, Enum):
    """Operational gameplay target identity, not a psychological EEG claim."""

    SIGHT = "sight"
    GUARD = "guard"


@dataclass(frozen=True)
class SsvepConfig:
    sample_rate_hz: float = 250.0
    window_seconds: float = 1.25
    blue_frequency_hz: float = 10.0
    green_frequency_hz: float = 12.0
    harmonics: int = 3
    filter_bands_hz: tuple[tuple[float, float], ...] = ((6.0, 35.0), (14.0, 35.0))
    filter_bank_weights: tuple[float, ...] = (1.0, 0.55)
    mains_hz: float = 60.0
    min_quality: float = 0.55
    min_score: float = 0.15
    min_margin: float = 0.035
    dwell_windows: int = 2
    refresh_seconds: float = 2.25
    refractory_seconds: float = 0.35

    @property
    def window_samples(self) -> int:
        return int(round(self.sample_rate_hz * self.window_seconds))

    @property
    def target_frequencies(self) -> dict[AuraTarget, float]:
        return {AuraTarget.SIGHT: self.blue_frequency_hz, AuraTarget.GUARD: self.green_frequency_hz}

    def validate(self) -> None:
        nyquist = self.sample_rate_hz / 2.0
        if self.window_seconds <= 0:
            raise ValueError("window_seconds must be positive")
        if self.harmonics < 1:
            raise ValueError("harmonics must be >= 1")
        for target, freq in self.target_frequencies.items():
            if not (0.0 < freq < nyquist):
                raise ValueError(f"{target.value} frequency {freq} outside Nyquist")
            if freq * self.harmonics >= nyquist:
                raise ValueError(f"{target.value} harmonics exceed Nyquist")
        if len(self.filter_bands_hz) != len(self.filter_bank_weights):
            raise ValueError("filter bands and weights must have equal length")
