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
    """Fixed-size EEG ring buffer with deterministic hop scheduling.

    The first window is emitted as soon as it becomes full. Subsequent windows
    are emitted every ``hop_samples`` new samples.
    """

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
    """Optional live source for a Unicorn Suite LSL EEG stream.

    ``pylsl`` is optional so replay/simulation tests remain hardware-free. The
    adapter intentionally requires an explicit scale-to-microvolts value. LSL
    stream units and channel ordering must be verified on the actual competition
    machine before a run is classified as observed hardware evidence.
    """

    EXPECTED_SAMPLE_RATE_HZ = 250.0
    EXPECTED_CHANNELS = 8

    def __init__(
        self,
        *,
        stream_name: str | None = None,
        timeout_s: float = 5.0,
        scale_to_uv: float = 1.0,
        eeg_channel_indices: tuple[int, ...] = tuple(range(8)),
    ):
        if len(eeg_channel_indices) != self.EXPECTED_CHANNELS:
            raise ValueError("exactly 8 EEG channel indices are required")
        if scale_to_uv <= 0:
            raise ValueError("scale_to_uv must be positive")
        self.stream_name = stream_name
        self.timeout_s = timeout_s
        self.scale_to_uv = float(scale_to_uv)
        self.eeg_channel_indices = eeg_channel_indices
        self._inlet = None
        self._info = None

    def connect(self) -> None:
        try:
            from pylsl import StreamInlet, resolve_byprop
        except ImportError as exc:
            raise RuntimeError("pylsl is required for live Unicorn LSL acquisition") from exc

        if self.stream_name:
            streams = resolve_byprop("name", self.stream_name, timeout=self.timeout_s)
        else:
            streams = resolve_byprop("type", "EEG", timeout=self.timeout_s)
        if not streams:
            raise RuntimeError("no matching EEG LSL stream found")

        info = streams[0]
        nominal = float(info.nominal_srate())
        channels = int(info.channel_count())
        required = max(self.eeg_channel_indices) + 1
        if channels < required:
            raise RuntimeError(f"EEG stream has {channels} channels; channel index {required - 1} is required")
        if nominal and abs(nominal - self.EXPECTED_SAMPLE_RATE_HZ) > 1.0:
            raise RuntimeError(f"EEG stream nominal rate {nominal} Hz; expected approximately 250 Hz")

        self._info = info
        self._inlet = StreamInlet(info, max_buflen=5, recover=True)

    @property
    def connected(self) -> bool:
        return self._inlet is not None

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
        return EegChunk(x, ts, time.monotonic())

    def close(self) -> None:
        if self._inlet is not None:
            try:
                self._inlet.close_stream()
            finally:
                self._inlet = None
                self._info = None
