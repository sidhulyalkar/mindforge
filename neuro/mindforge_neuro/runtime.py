from __future__ import annotations

import socket
import time
from dataclasses import dataclass
import numpy as np

from .calibration import CalibrationProfile
from .config import AuraTarget
from .events import EventType, NeuralEvent
from .ssvep import SsvepDecoder


@dataclass
class RuntimeState:
    seq: int = 0
    candidate: AuraTarget | None = None
    candidate_windows: int = 0
    last_emitted: AuraTarget | None = None
    last_emit_time: float = -1e9


class AuraSelectionRuntime:
    """Turn per-window SSVEP decisions into stable, rate-limited game events."""

    def __init__(self, decoder: SsvepDecoder, profile: CalibrationProfile):
        self.decoder = decoder
        self.profile = profile
        self.state = RuntimeState()

    def process(self, eeg_uv: np.ndarray, now: float | None = None) -> NeuralEvent:
        now = time.monotonic() if now is None else now
        decision = self.decoder.decide(eeg_uv)
        self.state.seq += 1
        if not decision.accepted or decision.target is None:
            self.state.candidate = None
            self.state.candidate_windows = 0
            return NeuralEvent.create(seq=self.state.seq, event=EventType.ABSTAIN, target=None,
                                      confidence=decision.confidence, quality=decision.quality.score,
                                      model_id=self.profile.model_id, reason=decision.reason,
                                      artifact=decision.quality.artifact)

        if decision.target == self.state.candidate:
            self.state.candidate_windows += 1
        else:
            self.state.candidate = decision.target
            self.state.candidate_windows = 1

        if self.state.candidate_windows < self.decoder.config.dwell_windows:
            return NeuralEvent.create(seq=self.state.seq, event=EventType.ABSTAIN, target=None,
                                      confidence=decision.confidence, quality=decision.quality.score,
                                      model_id=self.profile.model_id, reason="DWELL")

        since_last = now - self.state.last_emit_time
        changed = decision.target != self.state.last_emitted
        if not changed and since_last < self.decoder.config.refresh_seconds:
            return NeuralEvent.create(seq=self.state.seq, event=EventType.ABSTAIN, target=None,
                                      confidence=decision.confidence, quality=decision.quality.score,
                                      model_id=self.profile.model_id, reason="HELD")
        if since_last < self.decoder.config.refractory_seconds:
            return NeuralEvent.create(seq=self.state.seq, event=EventType.ABSTAIN, target=None,
                                      confidence=decision.confidence, quality=decision.quality.score,
                                      model_id=self.profile.model_id, reason="REFRACTORY")

        self.state.last_emitted = decision.target
        self.state.last_emit_time = now
        return NeuralEvent.create(seq=self.state.seq, event=EventType.AURA_SELECTED,
                                  target=decision.target, confidence=decision.confidence,
                                  quality=decision.quality.score, model_id=self.profile.model_id)


class UdpEventSink:
    def __init__(self, host: str = "127.0.0.1", port: int = 19742):
        self.address = (host, port)
        self.socket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

    def send(self, event: NeuralEvent) -> None:
        self.socket.sendto(event.to_json().encode("utf-8"), self.address)

    def close(self) -> None:
        self.socket.close()
