from __future__ import annotations

import time
from dataclasses import dataclass

import numpy as np


@dataclass(frozen=True)
class EegChunk:
    samples_uv: np.ndarray  # channels x samples
    timestamps_s: np.ndarray
    received_monotonic_s: float


class SlidingWindowBuffer:
    """Fixed-size EEG ring buffer with deterministic hop scheduling."""

    def __init__(self, channels: int, window_samples: int, hop_samples: int):
        if channels < 1 or window_samples < 2 or not 1 <= hop_samples <= window_samples:
            raise ValueError("invalid buffer dimensions")
        self.channels = channels
        self.window_samples = window_samples
        self.hop_samples = hop_samples
        self._data = np.zeros((channels, window_samples), dtype=float)
        self._timestamps = np.zeros(window_samples, dtype=float)
        self._filled = 0
        self._since_emit = 0
        self._has_emitted = False

    def push(self, samples_uv: np.ndarray, timestamps_s: np.ndarray) -> list[tuple[np.ndarray, np.ndarray]]:
        x = np.asarray(samples_uv, dtype=float)
        ts = np.asarray(timestamps_s, dtype=float)
        if x.ndim != 2 or x.shape[0] != self.channels:
            raise ValueError(f"expected ({self.channels}, n) samples, got {x.shape}")
        if ts.ndim != 1 or ts.shape[0] != x.shape[1]:
            raise ValueError("timestamps must match sample count")
        if ts.size and not np.all(np.diff(ts) >= 0):
            raise ValueError("timestamps must be monotonic non-decreasing")

        outputs: list[tuple[np.ndarray, np.ndarray]] = []
        for i in range(x.shape[1]):
            if self._filled < self.window_samples:
                self._data[:, self._filled] = x[:, i]
                self._timestamps[self._filled] = ts[i]
                self._filled += 1
            else:
                self._data[:, :-1] = self._data[:, 1:]
                self._data[:, -1] = x[:, i]
                self._timestamps[:-1] = self._timestamps[1:]
                self._timestamps[-1] = ts[i]

            if self._filled < self.window_samples:
                continue
            if not self._has_emitted:
                outputs.append((self._data.copy(), self._timestamps.copy()))
                self._has_emitted = True
                self._since_emit = 0
                continue
            self._since_emit += 1
            if self._since_emit >= self.hop_samples:
                outputs.append((self._data.copy(), self._timestamps.copy()))
                self._since_emit = 0
        return outputs


class UnicornLslSource:
    """Fail-closed LSL source shared by Phantom Unicorn and physical Unicorn runs.

    ``scale_to_uv`` and EEG channel indices remain explicit because LSL metadata
    from hardware tools is not assumed to have universal units/order.
    """

    EXPECTED_SAMPLE_RATE_HZ = 250.0
    EXPECTED_CHANNELS = 8

    def __init__(
        self,
        *,
        stream_name: str | None = None,
        source_id: str | None = None,
        timeout_s: float = 5.0,
        scale_to_uv: float = 1.0,
        eeg_channel_indices: tuple[int, ...] = tuple(range(8)),
    ):
        if len(eeg_channel_indices) != self.EXPECTED_CHANNELS:
            raise ValueError("exactly 8 EEG channel indices are required")
        if scale_to_uv <= 0:
            raise ValueError("scale_to_uv must be positive")
        if timeout_s <= 0:
            raise ValueError("timeout_s must be positive")
        self.stream_name = stream_name
        self.source_id = source_id
        self.timeout_s = float(timeout_s)
        self.scale_to_uv = float(scale_to_uv)
        self.eeg_channel_indices = tuple(int(i) for i in eeg_channel_indices)
        self._inlet = None
        self._info = None

    def connect(self) -> None:
        try:
            from pylsl import StreamInlet, resolve_byprop
        except (ImportError, OSError, RuntimeError) as exc:
            raise RuntimeError("pylsl and a loadable liblsl runtime are required for LSL acquisition") from exc

        if self.source_id:
            prop, value = "source_id", self.source_id
        elif self.stream_name:
            prop, value = "name", self.stream_name
        else:
            prop, value = "type", "EEG"

        streams = list(resolve_byprop(prop, value, minimum=2, timeout=self.timeout_s))
        if self.stream_name:
            streams = [s for s in streams if str(s.name()) == self.stream_name]
        if self.source_id:
            streams = [s for s in streams if str(s.source_id()) == self.source_id]
        if not streams:
            raise RuntimeError(f"no LSL stream matched {prop}={value!r}")
        if len(streams) > 1:
            identities = ", ".join(f"{s.name()}[{s.source_id()}]" for s in streams[:5])
            raise RuntimeError(f"ambiguous LSL source; refine stream_name/source_id. Matches: {identities}")

        info = streams[0]
        nominal = float(info.nominal_srate())
        channels = int(info.channel_count())
        required = max(self.eeg_channel_indices) + 1
        if channels < required:
            raise RuntimeError(f"EEG stream has {channels} channels; channel index {required - 1} is required")
        if nominal and abs(nominal - self.EXPECTED_SAMPLE_RATE_HZ) > 1.0:
            raise RuntimeError(f"EEG stream nominal rate {nominal} Hz; expected approximately 250 Hz")

        self._info = info
        self._inlet = StreamInlet(info, max_buflen=5, recover=bool(str(info.source_id() or "")), processing_flags=0)

    @property
    def connected(self) -> bool:
        return self._inlet is not None

    @property
    def stream_identity(self) -> str | None:
        if self._info is None:
            return None
        return f"{self._info.name()}[{self._info.source_id()}]"

    def pull_chunk(self, *, max_samples: int = 128, timeout_s: float = 0.25) -> EegChunk | None:
        if self._inlet is None:
            raise RuntimeError("source is not connected")
        samples, timestamps = self._inlet.pull_chunk(timeout=timeout_s, max_samples=max_samples)
        if not samples:
            return None
        raw = np.asarray(samples, dtype=float)
        if raw.ndim != 2:
            raise RuntimeError(f"unexpected LSL sample shape {raw.shape}")
        x = raw[:, self.eeg_channel_indices].T * self.scale_to_uv
        ts = np.asarray(timestamps, dtype=float)
        if ts.size != x.shape[1] or not np.isfinite(x).all() or not np.isfinite(ts).all():
            raise RuntimeError("invalid samples or timestamps from LSL stream")
        if ts.size > 1 and np.any(np.diff(ts) < 0):
            raise RuntimeError("LSL timestamps moved backwards")
        return EegChunk(x, ts, time.monotonic())

    def close(self) -> None:
        if self._inlet is not None:
            try:
                self._inlet.close_stream()
            finally:
                self._inlet = None
                self._info = None
