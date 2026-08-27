from __future__ import annotations

import json
import socket
from dataclasses import asdict, dataclass
from enum import Enum
from typing import Any, Mapping


GAME_MARKER_V1 = "mindforge.game_marker.v1"
LEGACY_CALIBRATION_MARKER_V1 = "mindforge.calibration_marker.v1"
SUPPORTED_GAME_MARKER_SCHEMAS = frozenset({GAME_MARKER_V1, LEGACY_CALIBRATION_MARKER_V1})


class GameMarkerType(str, Enum):
    CALIBRATION_STAGE = "CALIBRATION_STAGE"
    STIMULUS_PRESENTATION = "STIMULUS_PRESENTATION"
    COMBAT_ACTION = "COMBAT_ACTION"
    COMBAT_OUTCOME = "COMBAT_OUTCOME"
    BOSS_PHASE = "BOSS_PHASE"
    SIGNAL_BREAK = "SIGNAL_BREAK"
    FLUX_CHANGED = "FLUX_CHANGED"
    NEURAL_LINK = "NEURAL_LINK"
    SESSION = "SESSION"
    CUSTOM = "CUSTOM"


@dataclass(frozen=True)
class GameMarker:
    """A Unity-originated event describing what the game actually did.

    ``session_id`` identifies the Unity game session. ``calibration_id`` is a
    separate optional join key for calibration epochs. This prevents a calibration
    marker from silently changing the meaning of session identity.

    This channel intentionally carries no raw EEG and no decoder implementation
    state. It is the inverse of ``NeuralEvent``: Unity publishes presentation and
    gameplay facts so acquisition, replay and qualification tools can align the
    closed loop without reaching into the game process.
    """

    schema: str
    seq: int
    session_id: str
    event: str
    category: str
    unity_realtime_s: float
    game_time_s: float
    frame: int
    fixed_tick: int
    stage: str | None = None
    action: str | None = None
    target: str | None = None
    reason: str | None = None
    value: float = 0.0
    boss_phase: int = 0
    stimulus_epoch: int = -1
    trial_id: str | None = None
    planned_duration_s: float = 0.0
    calibration_id: str | None = None

    @classmethod
    def from_dict(cls, payload: Mapping[str, Any]) -> "GameMarker":
        schema = str(payload.get("schema") or "")
        if schema not in SUPPORTED_GAME_MARKER_SCHEMAS:
            raise ValueError(f"unsupported game marker schema: {schema}")

        # Old Awakening markers used session_id as the calibration identifier.
        # Preserve that information while promoting it to the explicit field.
        if schema == LEGACY_CALIBRATION_MARKER_V1:
            legacy_id = str(payload.get("session_id") or "")
            return cls(
                schema=schema,
                seq=int(payload.get("seq", 0)),
                session_id=legacy_id,
                calibration_id=legacy_id or None,
                event=GameMarkerType.CALIBRATION_STAGE.value,
                category="calibration",
                unity_realtime_s=float(payload.get("unity_realtime_s", 0.0)),
                game_time_s=float(payload.get("game_time_s", 0.0)),
                frame=int(payload.get("frame", -1)),
                fixed_tick=int(payload.get("fixed_tick", -1)),
                stage=_optional_str(payload.get("stage")),
                action=_optional_str(payload.get("action")),
                planned_duration_s=float(payload.get("planned_duration_s", 0.0)),
            )

        return cls(
            schema=schema,
            seq=max(0, int(payload.get("seq", 0))),
            session_id=str(payload.get("session_id") or ""),
            calibration_id=_optional_str(payload.get("calibration_id")),
            event=str(payload.get("event") or GameMarkerType.CUSTOM.value),
            category=str(payload.get("category") or "game"),
            unity_realtime_s=float(payload.get("unity_realtime_s", 0.0)),
            game_time_s=float(payload.get("game_time_s", 0.0)),
            frame=int(payload.get("frame", -1)),
            fixed_tick=int(payload.get("fixed_tick", -1)),
            stage=_optional_str(payload.get("stage")),
            action=_optional_str(payload.get("action")),
            target=_optional_str(payload.get("target")),
            reason=_optional_str(payload.get("reason")),
            value=float(payload.get("value", 0.0)),
            boss_phase=int(payload.get("boss_phase", 0)),
            stimulus_epoch=int(payload.get("stimulus_epoch", -1)),
            trial_id=_optional_str(payload.get("trial_id")),
            planned_duration_s=float(payload.get("planned_duration_s", 0.0)),
        )

    @classmethod
    def from_json(cls, raw: str | bytes) -> "GameMarker":
        if isinstance(raw, bytes):
            raw = raw.decode("utf-8")
        payload = json.loads(raw)
        if not isinstance(payload, dict):
            raise ValueError("game marker JSON must be an object")
        return cls.from_dict(payload)

    def to_dict(self) -> dict[str, Any]:
        return asdict(self)

    def to_json(self) -> str:
        return json.dumps(self.to_dict(), separators=(",", ":"), sort_keys=True)


class UdpGameMarkerSource:
    """Small reusable UDP inlet for Unity game markers.

    Port 19743 is the primary processing lane. Port 19745 is the default passive
    observation mirror used by developer logging so a recorder does not contend with
    the calibration/decoder process.
    """

    def __init__(self, host: str = "127.0.0.1", port: int = 19745, *, timeout_s: float = 0.25):
        self.address = (host, int(port))
        self.socket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        self.socket.bind(self.address)
        self.socket.settimeout(max(0.001, float(timeout_s)))

    def receive(self) -> GameMarker | None:
        try:
            raw, _remote = self.socket.recvfrom(65535)
        except socket.timeout:
            return None
        try:
            return GameMarker.from_json(raw)
        except (UnicodeDecodeError, json.JSONDecodeError, ValueError, TypeError):
            return None

    def close(self) -> None:
        self.socket.close()

    def __enter__(self) -> "UdpGameMarkerSource":
        return self

    def __exit__(self, exc_type, exc, tb) -> None:
        self.close()


def _optional_str(value: Any) -> str | None:
    if value is None:
        return None
    text = str(value)
    return text if text else None
