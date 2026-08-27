from __future__ import annotations

import socket
import time
from dataclasses import dataclass
import numpy as np

from .calibration import CalibrationProfile
from .config import AuraTarget
from .events import EventType, NeuralEvent
from .ssvep import SsvepDecision, SsvepDecoder


@dataclass
class RuntimeState:
    seq: int = 0
    candidate: AuraTarget | None = None
    candidate_windows: int = 0
    last_emitted: AuraTarget | None = None
    last_emit_time: float = -1e9


class AuraSelectionRuntime:
    """Turn per-window SSVEP decisions into stable, rate-limited game events.

    The runtime owns decoder-to-authority semantics but knows nothing about Unity.
    Live EEG, recorded EEG, or neurOS synthetic EEG therefore execute this exact
    implementation and differ only in declared provenance.
    """

    def __init__(
        self,
        decoder: SsvepDecoder,
        profile: CalibrationProfile,
        *,
        source_mode: str = "live",
        initial_seq: int = 0,
        session_id: str | None = None,
        calibration_id: str | None = None,
        authority_ttl_ms: int = 900,
    ):
        self.decoder = decoder
        self.profile = profile
        self.source_mode = source_mode
        self.session_id = session_id
        self.calibration_id = calibration_id
        self.authority_ttl_ms = max(0, int(authority_ttl_ms))
        self.state = RuntimeState(seq=int(initial_seq))

    def _event(
        self,
        decision: SsvepDecision,
        *,
        event: EventType,
        target: AuraTarget | None,
        reason: str | None = None,
    ) -> NeuralEvent:
        return NeuralEvent.create(
            seq=self.state.seq,
            event=event,
            target=target,
            confidence=decision.confidence,
            quality=decision.quality.score,
            model_id=self.profile.model_id,
            reason=reason,
            artifact=decision.quality.artifact,
            sight_score=decision.scores.get(AuraTarget.SIGHT, 0.0),
            guard_score=decision.scores.get(AuraTarget.GUARD, 0.0),
            margin=decision.margin,
            source_mode=self.source_mode,
            session_id=self.session_id,
            calibration_id=self.calibration_id,
            authority_ttl_ms=self.authority_ttl_ms,
        )

    def process(self, eeg_uv: np.ndarray, now: float | None = None) -> NeuralEvent:
        now = time.monotonic() if now is None else now
        decision = self.decoder.decide(eeg_uv)
        self.state.seq += 1
        if not decision.accepted or decision.target is None:
            self.state.candidate = None
            self.state.candidate_windows = 0
            return self._event(
                decision,
                event=EventType.ABSTAIN,
                target=None,
                reason=decision.reason or "LOW_QUALITY",
            )

        if decision.target == self.state.candidate:
            self.state.candidate_windows += 1
        else:
            self.state.candidate = decision.target
            self.state.candidate_windows = 1

        if self.state.candidate_windows < self.decoder.config.dwell_windows:
            return self._event(decision, event=EventType.ABSTAIN, target=None, reason="DWELL")

        since_last = now - self.state.last_emit_time
        changed = decision.target != self.state.last_emitted
        if not changed and since_last < self.decoder.config.refresh_seconds:
            return self._event(decision, event=EventType.ABSTAIN, target=None, reason="HELD")
        if since_last < self.decoder.config.refractory_seconds:
            return self._event(decision, event=EventType.ABSTAIN, target=None, reason="REFRACTORY")

        self.state.last_emitted = decision.target
        self.state.last_emit_time = now
        return self._event(decision, event=EventType.AURA_SELECTED, target=decision.target)


class UdpEventSink:
    def __init__(self, host: str = "127.0.0.1", port: int = 19742):
        self.address = (host, int(port))
        self.socket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

    def send(self, event: NeuralEvent) -> None:
        self.socket.sendto(event.to_json().encode("utf-8"), self.address)

    def close(self) -> None:
        self.socket.close()

    def __enter__(self) -> "UdpEventSink":
        return self

    def __exit__(self, exc_type, exc, tb) -> None:
        self.close()
