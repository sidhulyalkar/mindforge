from __future__ import annotations

import json
import time
from dataclasses import asdict, dataclass
from enum import Enum
from typing import Any

from .config import AuraTarget


class EventType(str, Enum):
    AURA_SELECTED = "AURA_SELECTED"
    ABSTAIN = "ABSTAIN"
    BCI_LOST = "BCI_LOST"
    BCI_RECOVERED = "BCI_RECOVERED"
    PARTICIPANT_STOP = "PARTICIPANT_STOP"
    CALIBRATION_SERVICE_READY = "CALIBRATION_SERVICE_READY"
    CALIBRATION_HEARTBEAT = "CALIBRATION_HEARTBEAT"
    CALIBRATION_READY = "CALIBRATION_READY"
    CALIBRATION_FAILED = "CALIBRATION_FAILED"


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
    source_mode: str = "live"

    @classmethod
    def create(cls, *, seq: int, event: EventType, target: AuraTarget | None,
               confidence: float, quality: float, model_id: str,
               reason: str | None = None, artifact: bool = False,
               monotonic_ns: int | None = None,
               sight_score: float = 0.0, guard_score: float = 0.0,
               margin: float = 0.0, has_evidence: bool | None = None,
               source_mode: str = "live") -> "NeuralEvent":
        sight_score = float(max(0.0, sight_score))
        guard_score = float(max(0.0, guard_score))
        if has_evidence is None:
            has_evidence = sight_score > 0.0 or guard_score > 0.0
        return cls(
            schema="mindforge.neural_event.v1",
            seq=seq,
            monotonic_ns=time.monotonic_ns() if monotonic_ns is None else monotonic_ns,
            event=event,
            target=target,
            confidence=float(max(0.0, min(1.0, confidence))),
            quality=float(max(0.0, min(1.0, quality))),
            paradigm="ssvep_fbcca",
            model_id=model_id,
            artifact=artifact,
            reason=reason,
            has_evidence=bool(has_evidence),
            sight_score=sight_score,
            guard_score=guard_score,
            margin=float(max(0.0, margin)),
            source_mode=source_mode,
        )

    def to_dict(self) -> dict[str, Any]:
        payload = asdict(self)
        payload["event"] = self.event.value
        payload["target"] = self.target.value if self.target else None
        return payload

    def to_json(self) -> str:
        return json.dumps(self.to_dict(), separators=(",", ":"), sort_keys=True)
