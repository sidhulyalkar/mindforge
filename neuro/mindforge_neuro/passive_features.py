"""Passive neural feature channel for Mindforge's optional Living World.

This file NEVER creates a NeuralEvent, actuates gameplay, or sends raw EEG to
Unity. Spectral features are measurements, not emotional/attentional diagnoses.
"""
from __future__ import annotations

from dataclasses import asdict, dataclass
import json
import math
import socket
import time
from typing import Mapping, Sequence

import numpy as np

from .quality import assess_window_quality

SCHEMA = "mindforge.neural_feature_frame.v1"
FEATURE = "posterior_alpha_relative_power"
MODES = frozenset(("simulated_feature", "feature_replay", "eeg_replay", "live"))
FIELDS = frozenset((
    "schema", "seq", "source_session_id", "source_mode", "calibration_id",
    "feature", "unit", "value", "quality", "artifact", "passive_only",
    "ttl_ms", "sample_start", "sample_end", "producer_monotonic_ns",
))
POSTERIOR = frozenset(("PZ", "PO7", "OZ", "PO8", "PO3", "PO4", "O1", "O2"))
DEFAULT_PORT = 19747


@dataclass(frozen=True)
class NeuralFeatureFrame:
    schema: str
    seq: int
    source_session_id: str
    source_mode: str
    calibration_id: str | None
    feature: str
    unit: str
    value: float
    quality: float
    artifact: bool
    passive_only: bool
    ttl_ms: int
    sample_start: int
    sample_end: int
    producer_monotonic_ns: int

    def validate(self) -> None:
        if self.schema != SCHEMA or self.feature != FEATURE or self.unit != "ratio":
            raise ValueError("unsupported passive feature schema / name / unit")
        if self.source_mode not in MODES:
            raise ValueError("unrecognized feature source provenance")
        if not isinstance(self.passive_only, bool) or not self.passive_only:
            raise ValueError("passive feature cannot grant gameplay authority")
        if type(self.artifact) is not bool:
            raise ValueError("artifact must be boolean")
        if not isinstance(self.source_session_id, str) or not 1 <= len(self.source_session_id) <= 128:
            raise ValueError("source_session_id is required and bounded")
        if self.calibration_id is not None and not isinstance(self.calibration_id, str):
            raise ValueError("calibration_id must be null or a string")
        for field in ("seq", "ttl_ms", "sample_start", "sample_end", "producer_monotonic_ns"):
            if type(getattr(self, field)) is not int:
                raise ValueError(f"{field} must be an integer")
        if self.seq < 0 or not 100 <= self.ttl_ms <= 5000 or self.producer_monotonic_ns < 0:
            raise ValueError("invalid sequence, TTL, or provenance time")
        if self.sample_start < -1 or self.sample_end < -1 or (self.sample_start >= 0 and
                                                              self.sample_end >= 0 and self.sample_end < self.sample_start):
            raise ValueError("invalid source sample range")
        for name in ("value", "quality"):
            number = getattr(self, name)
            if isinstance(number, bool) or not math.isfinite(float(number)) or not 0 <= number <= 1:
                raise ValueError(f"{name} must be finite and within [0,1]")

    def to_json(self) -> str:
        self.validate()
        return json.dumps(asdict(self), separators=(",", ":"), sort_keys=True, allow_nan=False)

    @classmethod
    def from_dict(cls, record: Mapping) -> "NeuralFeatureFrame":
        if not isinstance(record, Mapping) or set(record) != FIELDS:
            raise ValueError("missing or unexpected passive feature fields")
        f = cls(**record)
        f.validate()
        return f

    @classmethod
    def from_json(cls, encoded: str | bytes) -> "NeuralFeatureFrame":
        data = json.loads(encoded)
        return cls.from_dict(data)

    @classmethod
    def create(cls, seq: int, value: float, *, quality: float = 1.0,
               source_mode: str = "simulated_feature", source_session_id: str = "synthetic-session",
               calibration_id: str | None = None, artifact: bool = False,
               ttl_ms: int = 1200, sample_start: int = -1,
               sample_end: int = -1, producer_monotonic_ns: int | None = None) -> "NeuralFeatureFrame":
        frame = cls(
            SCHEMA, seq, source_session_id, source_mode, calibration_id, FEATURE,
            "ratio", value, quality, artifact, True, ttl_ms, sample_start, sample_end,
            time.monotonic_ns() if producer_monotonic_ns is None else producer_monotonic_ns,
        )
        frame.validate()
        return frame


def posterior_alpha_relative_power(
    eeg_uv: np.ndarray, channel_names: Sequence[str], sample_rate_hz: float,
    *, quality_floor: float = 0.65,
) -> tuple[float, float] | None:
    """Return (alpha/(theta+alpha+beta), engineering-quality) or abstain.

    Frequency bands: 4–30 Hz denominator and 8–13 Hz numerator. This is
    a measured spectral ratio, **not** a universal calm/focus metric.
    Channel and EEG units must already have been verified by acquisition.
    """
    x = np.asarray(eeg_uv, dtype=float)
    if (x.ndim != 2 or len(channel_names) != x.shape[0] or sample_rate_hz < 100 or
            x.shape[1] < int(sample_rate_hz) or not np.isfinite(x).all()):
        raise ValueError("expected finite EEG (channels,samples), >=1 second, >=100 Hz")
    names = [str(n).strip().upper() for n in channel_names]
    if len(names) != len(set(names)):
        raise ValueError("duplicate channel names")
    indices = [i for i, n in enumerate(names) if n in POSTERIOR]
    if len(indices) < 2:
        raise ValueError("requires at least two verified posterior EEG electrodes")
    quality = assess_window_quality(x, sample_rate_hz)
    if quality.artifact or quality.score < quality_floor:
        return None
    posterior = x[indices]
    centered = posterior - posterior.mean(axis=1, keepdims=True)
    power = np.abs(np.fft.rfft(centered * np.hanning(posterior.shape[1]), axis=1)) ** 2
    freq = np.fft.rfftfreq(posterior.shape[1], d=1. / sample_rate_hz)
    base = (freq >= 4.) & (freq < 30.)
    alpha = (freq >= 8.) & (freq < 13.)
    denominator = float(power[:, base].sum())
    if denominator <= 1e-12:
        return None
    ratio = float(np.clip(power[:, alpha].sum() / denominator, 0., 1.))
    return ratio, float(quality.score)


def demo_frames(*, steps: int = 80, hz: float = 5., seed: int = 17,
                source_session_id: str = "synthetic-living-world") -> list[NeuralFeatureFrame]:
    """Deterministic synthetic derived-feature source, *not* synthetic human EEG."""
    if not 1 <= steps <= 100000 or not 0 < hz <= 20:
        raise ValueError("invalid demo length or update frequency")
    rng = np.random.default_rng(seed)
    values = []
    for i in range(steps):
        t = i / hz
        value = float(np.clip(0.5 + 0.23 * np.sin(2. * np.pi * t / 14.) +
                              0.08 * np.sin(2. * np.pi * t / 4.3) +
                              rng.normal(0., 0.012), 0., 1.))
        values.append(NeuralFeatureFrame.create(
            i, value, source_session_id=source_session_id,
            producer_monotonic_ns=i * int(1e9 / hz),
        ))
    return values


def offline_eeg_feature_frames(
    epochs_uv: np.ndarray, channel_names: Sequence[str], sample_rate_hz: float,
    *, source_session_id: str = "offline-eeg-archive", quality_floor: float = 0.65,
) -> list[NeuralFeatureFrame]:
    """Extract only derived ratios from prerecorded EEG epochs.

    Epochs may be discontinuous. Their order is preserved but their real capture
    timing and source sample indices are UNKNOWN and must not be invented.
    Caller supplies *presentation pacing*, not claimed acquisition timing.
    A rejected epoch emits an artifact/neutral frame to withdraw old colors.
    Archive identity makes no assertion about human vs synthetic EEG.
    """
    epochs = np.asarray(epochs_uv, dtype=float)
    if epochs.ndim != 3 or not 1 <= len(epochs) <= 100000:
        raise ValueError("expected 1–100000 prerecorded EEG epochs")
    frames: list[NeuralFeatureFrame] = []
    for index, epoch in enumerate(epochs):
        result = posterior_alpha_relative_power(
            epoch, channel_names, sample_rate_hz, quality_floor=quality_floor)
        ratio, quality = result if result is not None else (0.5, 0.)
        frames.append(NeuralFeatureFrame.create(
            seq=index, value=ratio, quality=quality,
            artifact=result is None, source_mode="eeg_replay",
            source_session_id=source_session_id, ttl_ms=1200,
            sample_start=-1, sample_end=-1,
            producer_monotonic_ns=index,
        ))
    return frames


class PassiveUdpEmitter:
    """Explicit loopback-only UDP transport; no game action port is ever opened."""

    def __init__(self, port: int = DEFAULT_PORT, *, host: str = "127.0.0.1"):
        if host != "127.0.0.1" or not 1024 <= port <= 65535:
            raise ValueError("Living World emitter is restricted to IPv4 loopback and a valid port")
        self.address = (host, port)
        self.socket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

    def send(self, frame: NeuralFeatureFrame) -> None:
        self.socket.sendto(frame.to_json().encode("utf-8"), self.address)

    def close(self) -> None:
        self.socket.close()

    def __enter__(self) -> "PassiveUdpEmitter":
        return self

    def __exit__(self, *_: object) -> None:
        self.close()
