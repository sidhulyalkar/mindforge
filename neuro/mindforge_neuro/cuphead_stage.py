"""Stage observations for legacy Cuphead only, deliberately not combat.

The native producer *reads* Level.Current and Map.Current from its own
installed build. A level being loaded does not imply a boss, danger,
parry window, or opportunity for neural selection.
"""
from __future__ import annotations

from dataclasses import asdict, dataclass
from collections import Counter
import json
import math
from typing import Any, Mapping

SCHEMA = "mindforge.cuphead_stage_observation.v1"
SOURCE = "observed_stage_reflection_unqualified"
SYNTHETIC = "synthetic_stage_observation"
STAGES = frozenset({"unknown", "map", "level"})
EVENTS = frozenset({"sample", "scene_changed"})


@dataclass(frozen=True)
class CupheadStageObservation:
    schema: str
    game_id: str
    session_id: str
    seq: int
    source: str
    game_version: str
    stage: str
    event: str
    scene_name: str
    app_focused: bool
    paused: bool
    source_time_s: float

    @classmethod
    def from_dict(cls, payload: Mapping[str, Any]) -> "CupheadStageObservation":
        if not isinstance(payload, Mapping) or set(payload) != set(cls.__dataclass_fields__):
            raise ValueError("incorrect Cuphead stage observation fields")
        if (payload["schema"] != SCHEMA or payload["game_id"] != "cuphead"
            or payload["source"] not in (SOURCE, SYNTHETIC)
            or payload["stage"] not in STAGES or payload["event"] not in EVENTS
            or type(payload["seq"]) is not int or payload["seq"] <= 0
            or type(payload["session_id"]) is not str
            or not 1 <= len(payload["session_id"]) <= 128
            or type(payload["game_version"]) is not str
            or not 1 <= len(payload["game_version"]) <= 128
            or type(payload["scene_name"]) is not str
            or len(payload["scene_name"]) > 128
            or type(payload["app_focused"]) is not bool
            or type(payload["paused"]) is not bool):
            raise ValueError("invalid Cuphead stage observation source/identity")
        t = payload["source_time_s"]
        if type(t) not in (float, int) or not math.isfinite(t) or t < 0:
            raise ValueError("invalid source time")
        if payload["event"] == "scene_changed" and payload["stage"] != "unknown":
            raise ValueError("scene transition cannot claim a known active stage")
        return cls(**payload)

    @classmethod
    def from_json(cls, raw: str | bytes) -> "CupheadStageObservation":
        try:
            return cls.from_dict(json.loads(raw))
        except (KeyError, TypeError, UnicodeError) as exc:
            raise ValueError("malformed Cuphead stage observation") from exc

    def to_json(self) -> str:
        data = asdict(self)
        self.from_dict(data)
        return json.dumps(data, allow_nan=False, sort_keys=True, separators=(",", ":"))

    @classmethod
    def synthetic(cls, stage: str, seq: int, *, event: str = "sample",
                  session_id: str = "synthetic-cuphead-stage-session",
                  source_time_s: float = 0.) -> "CupheadStageObservation":
        return cls.from_dict({
            "schema": SCHEMA, "game_id": "cuphead", "session_id": session_id,
            "seq": seq, "source": SYNTHETIC,
            "game_version": "synthetic-not-a-game-build", "stage": stage,
            "event": event, "scene_name": "synthetic",
            "app_focused": True, "paused": False, "source_time_s": source_time_s,
        })


class CupheadStageMonitor:
    """Audit stage transitions, never produce orb or EEG authority."""

    def __init__(self, *, ttl_s: float = 1.25):
        if not math.isfinite(ttl_s) or not 0 < ttl_s <= 2:
            raise ValueError("invalid TTL")
        self.ttl_s = ttl_s
        self.session_id: str | None = None
        self.last_seq = -1
        self.last_at: float | None = None
        self.last_source_time_s: float | None = None
        self.latest: CupheadStageObservation | None = None
        self.counts: Counter[str] = Counter()
        self.rejections: Counter[str] = Counter()
        self.missing_seq = 0

    def accept(self, packet: CupheadStageObservation, now_s: float) -> bool:
        if not math.isfinite(now_s):
            self.rejections["invalid_clock"] += 1
            return False
        if packet.session_id != self.session_id:
            # Reject interleaved producer processes until previous one expires.
            if self.last_at is not None and now_s - self.last_at <= self.ttl_s:
                self.rejections["concurrent_session"] += 1
                return False
            self.session_id = packet.session_id
            self.last_seq = -1
            self.last_source_time_s = None
        if packet.seq <= self.last_seq:
            self.rejections["replay_or_reorder"] += 1
            return False
        if (self.last_source_time_s is not None
                and packet.source_time_s < self.last_source_time_s):
            self.rejections["source_clock_reversed"] += 1
            return False
        if self.last_seq >= 0:
            self.missing_seq += max(0, packet.seq - self.last_seq - 1)
        self.last_seq = packet.seq
        self.last_at = now_s
        self.last_source_time_s = packet.source_time_s
        self.latest = packet
        self.counts[packet.stage] += 1
        return True

    def stage_at(self, now_s: float) -> str:
        if not math.isfinite(now_s) or self.last_at is None or self.latest is None:
            return "unknown"
        if not 0 <= now_s - self.last_at < self.ttl_s:
            return "unknown"
        if self.latest.paused or not self.latest.app_focused:
            return "unknown"
        return self.latest.stage

    def report(self, now_s: float) -> dict[str, Any]:
        return {
            "schema": "mindforge.cuphead_stage_audit.v1",
            "game_id": "cuphead",
            "session_id": self.session_id,
            "stage": self.stage_at(now_s),
            "stage_samples": dict(self.counts),
            "rejections": dict(self.rejections),
            "missing_seq": self.missing_seq,
            "stage_reflection_is_unqualified": True,
            "combat_engagement_verified": False,
            "boss_phase_verified": False,
            "parry_window_verified": False,
            "ssvep_stimulus_enabled": False,
            "native_runtime_qualified": False,
        }
