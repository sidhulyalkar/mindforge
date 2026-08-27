from __future__ import annotations

import time
from dataclasses import dataclass, field

from .events import EventType, NeuralEvent, SourceMode
from .markers import GameMarker


_STAGE_SEQUENCE = (
    ("baseline", "begin"),
    ("baseline", "end"),
    ("sight", "begin"),
    ("sight", "end"),
    ("guard", "begin"),
    ("guard", "end"),
)


@dataclass
class DevelopmentCalibrationFixture:
    """Truthfully substitutes calibration for no-headset development.

    It speaks the same Unity marker/status contract as the real calibrated decoder,
    but it never claims to have measured EEG. ``source_mode`` must therefore be a
    development provenance such as ``manual`` or ``simulated_decision``.

    The state machine fails closed on an out-of-order protocol. A development run is
    promoted only after Unity emits the complete baseline -> sight -> guard sequence.
    """

    source_mode: str
    model_id: str = "development-calibration-fixture-v1"
    heartbeat_seconds: float = 0.5
    seq: int = 0
    session_id: str | None = None
    calibration_id: str | None = None
    _next_expected: int = 0
    _last_emit: float = field(default_factory=lambda: 0.0)
    _completed: bool = False

    def __post_init__(self) -> None:
        allowed = {SourceMode.MANUAL.value, SourceMode.SIMULATED_DECISION.value,
                   SourceMode.DECISION_REPLAY.value}
        if self.source_mode not in allowed:
            raise ValueError(
                f"development calibration fixture requires development provenance, got {self.source_mode!r}")
        self.heartbeat_seconds = max(0.1, float(self.heartbeat_seconds))

    @property
    def completed(self) -> bool:
        return self._completed

    def periodic(self, now: float | None = None) -> NeuralEvent | None:
        now = time.monotonic() if now is None else float(now)
        if now - self._last_emit < self.heartbeat_seconds:
            return None
        self._last_emit = now
        kind = EventType.CALIBRATION_HEARTBEAT if self.calibration_id else EventType.CALIBRATION_SERVICE_READY
        reason = (
            "DEVELOPMENT_FIXTURE_NO_EEG_CALIBRATION"
            if self.calibration_id
            else "DEVELOPMENT_FIXTURE_SERVICE_READY_NO_EEG"
        )
        return self._event(kind, reason=reason)

    def consume(self, marker: GameMarker) -> NeuralEvent | None:
        if marker.category != "calibration" or marker.event != "CALIBRATION_STAGE":
            return None
        stage = (marker.stage or "").lower()
        action = (marker.action or "").lower()
        calibration_id = marker.calibration_id or marker.session_id or None

        # A new baseline begin is an explicit retry and starts a fresh state machine.
        if (stage, action) == ("baseline", "begin") and calibration_id != self.calibration_id:
            self.session_id = marker.session_id or None
            self.calibration_id = calibration_id
            self._next_expected = 0
            self._completed = False

        expected = _STAGE_SEQUENCE[self._next_expected] if self._next_expected < len(_STAGE_SEQUENCE) else None
        if expected != (stage, action):
            self._completed = False
            self._next_expected = 0
            return self._event(
                EventType.CALIBRATION_FAILED,
                reason=f"DEV_FIXTURE_PROTOCOL_ORDER expected={expected} observed={(stage, action)}",
            )

        self.session_id = marker.session_id or self.session_id
        self.calibration_id = calibration_id or self.calibration_id
        self._next_expected += 1
        self._last_emit = time.monotonic()

        if self._next_expected == len(_STAGE_SEQUENCE):
            self._completed = True
            return self._event(
                EventType.CALIBRATION_READY,
                confidence=1.0,
                quality=1.0,
                reason="DEVELOPMENT_FIXTURE_ACCEPTED_NO_EEG_CALIBRATION",
            )
        return self._event(
            EventType.CALIBRATION_HEARTBEAT,
            reason=f"DEVELOPMENT_FIXTURE_STAGE_{stage.upper()}_{action.upper()}",
        )

    def _event(self, kind: EventType, *, confidence: float = 0.0, quality: float = 0.0,
               reason: str | None = None) -> NeuralEvent:
        self.seq += 1
        return NeuralEvent.create(
            seq=self.seq,
            event=kind,
            target=None,
            confidence=confidence,
            quality=quality,
            model_id=self.model_id,
            reason=reason,
            source_mode=self.source_mode,
            session_id=self.session_id,
            calibration_id=self.calibration_id,
            authority_ttl_ms=0,
        )
