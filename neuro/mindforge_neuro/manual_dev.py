from __future__ import annotations

import json
import socket
import time
from dataclasses import dataclass
from typing import Any, Mapping

from .config import AuraTarget
from .events import EventType, NeuralEvent, SourceMode


MANUAL_INTENT_V1 = "mindforge.manual_intent.v1"


@dataclass(frozen=True)
class ManualIntent:
    schema: str
    session_id: str
    calibration_id: str | None
    target: AuraTarget
    unity_realtime_s: float

    @classmethod
    def from_dict(cls, payload: Mapping[str, Any]) -> "ManualIntent":
        schema = str(payload.get("schema") or "")
        if schema != MANUAL_INTENT_V1:
            raise ValueError(f"unsupported manual intent schema: {schema}")
        target = AuraTarget(str(payload.get("target") or "").lower())
        return cls(
            schema=schema,
            session_id=str(payload.get("session_id") or ""),
            calibration_id=(str(payload.get("calibration_id")) if payload.get("calibration_id") else None),
            target=target,
            unity_realtime_s=float(payload.get("unity_realtime_s", 0.0)),
        )

    @classmethod
    def from_json(cls, raw: str | bytes) -> "ManualIntent":
        if isinstance(raw, bytes):
            raw = raw.decode("utf-8")
        payload = json.loads(raw)
        if not isinstance(payload, dict):
            raise ValueError("manual intent JSON must be an object")
        return cls.from_dict(payload)


class UdpManualIntentSource:
    def __init__(self, host: str = "127.0.0.1", port: int = 19746, *, timeout_s: float = 0.10):
        self.address = (host, int(port))
        self.socket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        self.socket.bind(self.address)
        self.socket.settimeout(max(0.001, float(timeout_s)))

    def receive(self) -> ManualIntent | None:
        try:
            raw, _remote = self.socket.recvfrom(8192)
        except socket.timeout:
            return None
        try:
            return ManualIntent.from_json(raw)
        except (UnicodeDecodeError, json.JSONDecodeError, ValueError, TypeError):
            return None

    def close(self) -> None:
        self.socket.close()

    def __enter__(self) -> "UdpManualIntentSource":
        return self

    def __exit__(self, exc_type, exc, tb) -> None:
        self.close()


def manual_selection_event(*, seq: int, session_id: str, calibration_id: str | None,
                           intent: ManualIntent) -> NeuralEvent:
    sight = intent.target == AuraTarget.SIGHT
    return NeuralEvent.create(
        seq=seq,
        event=EventType.AURA_SELECTED,
        target=intent.target,
        confidence=0.90,
        quality=0.92,
        model_id="manual-intent-adapter-v1",
        reason="MANUAL_DEV_SELECTION",
        sight_score=0.75 if sight else 0.10,
        guard_score=0.10 if sight else 0.75,
        margin=0.65,
        source_mode=SourceMode.MANUAL.value,
        session_id=session_id,
        calibration_id=calibration_id,
        authority_ttl_ms=900,
    )


def manual_idle_event(*, seq: int, session_id: str, calibration_id: str | None) -> NeuralEvent:
    """Safe liveness packet for a healthy manual development source."""
    return NeuralEvent.create(
        seq=seq,
        event=EventType.BCI_HEARTBEAT,
        target=None,
        confidence=0.0,
        quality=1.0,
        model_id="manual-intent-adapter-v1",
        reason="MANUAL_DEV_IDLE",
        has_evidence=False,
        source_mode=SourceMode.MANUAL.value,
        session_id=session_id,
        calibration_id=calibration_id,
        authority_ttl_ms=0,
        monotonic_ns=time.monotonic_ns(),
    )
