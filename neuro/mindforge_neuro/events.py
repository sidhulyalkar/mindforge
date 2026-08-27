from __future__ import annotations

import json
import time
from dataclasses import asdict, dataclass
from enum import Enum
from typing import Any, Mapping

from .config import AuraTarget


NEURAL_EVENT_V1 = "mindforge.neural_event.v1"
NEURAL_EVENT_V2 = "mindforge.neural_event.v2"
SUPPORTED_NEURAL_EVENT_SCHEMAS = frozenset({NEURAL_EVENT_V1, NEURAL_EVENT_V2})


class EventType(str, Enum):
    AURA_SELECTED = "AURA_SELECTED"
    ABSTAIN = "ABSTAIN"
    BCI_HEARTBEAT = "BCI_HEARTBEAT"
    BCI_LOST = "BCI_LOST"
    BCI_RECOVERED = "BCI_RECOVERED"
    PARTICIPANT_STOP = "PARTICIPANT_STOP"
    CALIBRATION_SERVICE_READY = "CALIBRATION_SERVICE_READY"
    CALIBRATION_HEARTBEAT = "CALIBRATION_HEARTBEAT"
    CALIBRATION_READY = "CALIBRATION_READY"
    CALIBRATION_FAILED = "CALIBRATION_FAILED"


class SourceMode(str, Enum):
    """Provenance of a derived neural event.

    The legacy ``simulation`` and ``replay`` values remain valid because existing
    Mindforge qualification artifacts use them. New development tools should prefer
    the more specific modes so a recording says exactly which part of the causal
    chain was substituted.
    """

    MANUAL = "manual"
    SIMULATED_DECISION = "simulated_decision"
    DECISION_REPLAY = "decision_replay"
    EEG_REPLAY = "eeg_replay"
    SYNTHETIC_EEG = "synthetic_eeg"
    LIVE = "live"
    LEGACY_SIMULATION = "simulation"
    LEGACY_REPLAY = "replay"


KNOWN_SOURCE_MODES = frozenset(mode.value for mode in SourceMode)


@dataclass(frozen=True)
class NeuralEvent:
    schema: str
    seq: int
    monotonic_ns: int
    event: EventType
    target: AuraTarget | None
    confidence: float
    quality: float
    paradigm: str
    model_id: str
    artifact: bool = False
    reason: str | None = None
    has_evidence: bool = False
    sight_score: float = 0.0
    guard_score: float = 0.0
    margin: float = 0.0
    source_mode: str = SourceMode.LIVE.value

    # v2 provenance. Values are optional/sentinel-friendly so v1 recordings remain
    # replayable without inventing facts that were never recorded.
    session_id: str | None = None
    calibration_id: str | None = None
    source_sample_start: int = -1
    source_sample_end: int = -1
    decoder_time_ns: int = 0
    authority_ttl_ms: int = 900

    @classmethod
    def create(
        cls,
        *,
        seq: int,
        event: EventType,
        target: AuraTarget | None,
        confidence: float,
        quality: float,
        model_id: str,
        reason: str | None = None,
        artifact: bool = False,
        monotonic_ns: int | None = None,
        sight_score: float = 0.0,
        guard_score: float = 0.0,
        margin: float = 0.0,
        has_evidence: bool | None = None,
        source_mode: str = SourceMode.LIVE.value,
        session_id: str | None = None,
        calibration_id: str | None = None,
        source_sample_start: int = -1,
        source_sample_end: int = -1,
        decoder_time_ns: int | None = None,
        authority_ttl_ms: int = 900,
        schema: str = NEURAL_EVENT_V2,
    ) -> "NeuralEvent":
        if schema not in SUPPORTED_NEURAL_EVENT_SCHEMAS:
            raise ValueError(f"unsupported neural event schema: {schema}")
        if not isinstance(event, EventType):
            event = EventType(str(event))
        if target is not None and not isinstance(target, AuraTarget):
            target = AuraTarget(str(target).lower())
        sight_score = float(max(0.0, sight_score))
        guard_score = float(max(0.0, guard_score))
        if has_evidence is None:
            has_evidence = sight_score > 0.0 or guard_score > 0.0
        now_ns = time.monotonic_ns() if monotonic_ns is None else int(monotonic_ns)
        decoder_ns = now_ns if decoder_time_ns is None else int(decoder_time_ns)
        sample_start = int(source_sample_start)
        sample_end = int(source_sample_end)
        if sample_start >= 0 and sample_end >= 0 and sample_end < sample_start:
            raise ValueError("source_sample_end must be >= source_sample_start")
        return cls(
            schema=schema,
            seq=max(0, int(seq)),
            monotonic_ns=now_ns,
            event=event,
            target=target,
            confidence=float(max(0.0, min(1.0, confidence))),
            quality=float(max(0.0, min(1.0, quality))),
            paradigm="ssvep_fbcca",
            model_id=str(model_id),
            artifact=bool(artifact),
            reason=reason,
            has_evidence=bool(has_evidence),
            sight_score=sight_score,
            guard_score=guard_score,
            margin=float(max(0.0, margin)),
            source_mode=str(source_mode),
            session_id=session_id or None,
            calibration_id=calibration_id or None,
            source_sample_start=sample_start,
            source_sample_end=sample_end,
            decoder_time_ns=decoder_ns,
            authority_ttl_ms=max(0, int(authority_ttl_ms)),
        )

    @classmethod
    def from_dict(cls, payload: Mapping[str, Any]) -> "NeuralEvent":
        schema = str(payload.get("schema") or NEURAL_EVENT_V1)
        if schema not in SUPPORTED_NEURAL_EVENT_SCHEMAS:
            raise ValueError(f"unsupported neural event schema: {schema}")
        event = EventType(str(payload["event"]))
        raw_target = payload.get("target")
        target = AuraTarget(str(raw_target).lower()) if raw_target not in (None, "", "none") else None
        return cls.create(
            schema=schema,
            seq=int(payload.get("seq", 0)),
            monotonic_ns=int(payload.get("monotonic_ns", 0)),
            event=event,
            target=target,
            confidence=float(payload.get("confidence", 0.0)),
            quality=float(payload.get("quality", 0.0)),
            model_id=str(payload.get("model_id") or "unknown"),
            reason=payload.get("reason"),
            artifact=bool(payload.get("artifact", False)),
            sight_score=float(payload.get("sight_score", 0.0)),
            guard_score=float(payload.get("guard_score", 0.0)),
            margin=float(payload.get("margin", 0.0)),
            has_evidence=bool(payload.get("has_evidence", False)),
            source_mode=str(payload.get("source_mode") or SourceMode.LIVE.value),
            session_id=payload.get("session_id"),
            calibration_id=payload.get("calibration_id"),
            source_sample_start=int(payload.get("source_sample_start", -1)),
            source_sample_end=int(payload.get("source_sample_end", -1)),
            decoder_time_ns=int(payload.get("decoder_time_ns", payload.get("monotonic_ns", 0))),
            authority_ttl_ms=int(payload.get("authority_ttl_ms", 0 if schema == NEURAL_EVENT_V1 else 900)),
        )

    @classmethod
    def from_json(cls, raw: str | bytes) -> "NeuralEvent":
        if isinstance(raw, bytes):
            raw = raw.decode("utf-8")
        payload = json.loads(raw)
        if not isinstance(payload, dict):
            raise ValueError("neural event JSON must be an object")
        return cls.from_dict(payload)

    def to_dict(self) -> dict[str, Any]:
        payload = asdict(self)
        payload["event"] = self.event.value
        payload["target"] = self.target.value if self.target else None
        return payload

    def to_json(self) -> str:
        return json.dumps(self.to_dict(), separators=(",", ":"), sort_keys=True)
