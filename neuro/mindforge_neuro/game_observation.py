"""Strict, derived-only observations from optional native game instrumentation.

An observed button/action hook does NOT establish enemy engagement, danger,
a safe recovery interval, or neural control authority. Producers use the
'unqualified' label until tested against an exact installed game build.
"""
from __future__ import annotations

from dataclasses import dataclass, asdict
from collections import Counter
import json
import math
from typing import Any, Mapping

SCHEMA = "mindforge.game_observation.v1"
GAMES = frozenset(("silksong", "hollow_knight"))  # Cuphead has no qualified native observer yet
SOURCES = frozenset(("observed_game_hook_unqualified", "synthetic_game_observation"))
EVENTS = frozenset((
    "hero_ready", "hero_attack", "hero_dash", "hero_jump",
    "hero_damaged", "hero_die", "scene_changed", "heartbeat",
))
FIELDS = frozenset((
    "schema", "game_id", "source_mode", "session_id", "seq",
    "event", "game_version", "hook_target", "in_game",
    "app_focused", "paused", "hero_x", "hero_y", "source_time_s",
))
SILKSONG_EVENT_HOOKS = {
    "hero_ready": "HeroController.Start",
    "hero_attack": "HeroController.Attack",
    "hero_dash": "HeroController.HeroDash",
    "hero_jump": "HeroController.OnHeroJumped",
    "hero_damaged": "HeroController.HeroDamaged",
    "hero_die": "HeroController.Die",
    "scene_changed": "SceneManager.activeSceneChanged",
    "heartbeat": "GameTelemetryPlugin.Update",
}

# Read-only event provenance must identify the *actual* game-specific API.
# Do not disguise Hollow Knight's ModHooks callback as Silksong Harmony.
HOLLOW_KNIGHT_EVENT_HOOKS = {
    "hero_ready": "ModHooks.HeroUpdateHook",
    "hero_attack": "ModHooks.AttackHook",
    "scene_changed": "SceneManager.activeSceneChanged",
    "heartbeat": "ModHooks.HeroUpdateHook",
}
GAME_EVENT_HOOKS = {
    "silksong": SILKSONG_EVENT_HOOKS,
    "hollow_knight": HOLLOW_KNIGHT_EVENT_HOOKS,
}
EVENT_HOOKS = SILKSONG_EVENT_HOOKS  # Existing Silksong API compatibility


@dataclass(frozen=True)
class GameObservation:
    schema: str
    game_id: str
    source_mode: str
    session_id: str
    seq: int
    event: str
    game_version: str
    hook_target: str
    in_game: bool
    app_focused: bool
    paused: bool
    hero_x: float | None
    hero_y: float | None
    source_time_s: float

    @classmethod
    def from_dict(cls, value: Mapping[str, Any]) -> "GameObservation":
        if not isinstance(value, Mapping) or set(value) != FIELDS:
            raise ValueError("observation fields differ from v1 contract")
        if (value["schema"] != SCHEMA or value["game_id"] not in GAMES
            or value["source_mode"] not in SOURCES
            or value["event"] not in EVENTS
            or type(value["session_id"]) is not str
            or not 1 <= len(value["session_id"]) <= 128
            or type(value["game_version"]) is not str
            or not 1 <= len(value["game_version"]) <= 128
            or type(value["seq"]) is not int or value["seq"] < 1
            or any(type(value[f]) is not bool
                   for f in ("in_game", "app_focused", "paused"))):
            raise ValueError("invalid observation identity, provenance or flags")
        expected_hook = GAME_EVENT_HOOKS[value["game_id"]].get(value["event"])
        if expected_hook is None or value["hook_target"] != expected_hook:
            raise ValueError("event name and game-specific hook target disagree")
        t = value["source_time_s"]
        if type(t) not in (int, float) or not math.isfinite(t) or t < 0:
            raise ValueError("invalid local source timestamp")
        x, y = value["hero_x"], value["hero_y"]
        if (x is None) != (y is None):
            raise ValueError("player screen coordinates require both axes")
        if x is not None and (
            type(x) not in (int, float) or type(y) not in (int, float)
            or not math.isfinite(x) or not math.isfinite(y)
            or not 0 <= x <= 1 or not 0 <= y <= 1
        ):
            raise ValueError("invalid normalized screen coordinates")
        if value["event"] in ("scene_changed", "hero_die") and x is not None:
            raise ValueError("scene change or death cannot assert a live anchor")
        return cls(**value)

    @classmethod
    def from_json(cls, text: str | bytes) -> "GameObservation":
        try:
            return cls.from_dict(json.loads(text))
        except (TypeError, KeyError) as exc:
            raise ValueError("malformed game observation") from exc

    def to_json(self) -> str:
        payload = asdict(self)
        self.from_dict(payload)
        return json.dumps(payload, separators=(",", ":"), sort_keys=True, allow_nan=False)

    @classmethod
    def make(cls, game_id: str, event: str, seq: int, *,
             session_id: str = "fixture-session",
             source_mode: str = "synthetic_game_observation",
             game_version: str = "synthetic-not-a-game-build",
             in_game: bool = True, app_focused: bool = True,
             paused: bool = False, anchor: tuple[float, float] | None = None,
             source_time_s: float = 0.) -> "GameObservation":
        hook = GAME_EVENT_HOOKS.get(game_id, {}).get(event)
        if hook is None:
            raise ValueError("unsupported event for game-specific observer")
        x, y = anchor if anchor else (None, None)
        instance = cls(
            SCHEMA, game_id, source_mode, session_id, seq, event,
            game_version, hook,
            in_game, app_focused,
            paused, x, y, source_time_s,
        )
        return cls.from_dict(asdict(instance))


class ObservationMonitor:
    """Audit/record-only gate; outputs counts, never inferred boss context."""

    def __init__(self, game_id: str, max_gap_s: float = 2.0):
        if game_id not in GAMES or max_gap_s <= 0:
            raise ValueError("unsupported game or gap")
        self.game_id = game_id
        self.max_gap_s = max_gap_s
        self.session_id: str | None = None
        self.last_seq = -1
        self.last_at: float | None = None
        self.counts: Counter[str] = Counter()
        self.rejected = Counter()
        self.missing_seq = 0
        self.last_event: str | None = None

    def accept(self, frame: GameObservation, received_s: float) -> bool:
        if not math.isfinite(received_s) or frame.game_id != self.game_id:
            self.rejected["invalid_game_or_time"] += 1
            return False
        if frame.session_id != self.session_id:
            # Once the current stream expires, a new session can become owner.
            if self.last_at is not None and received_s - self.last_at <= self.max_gap_s:
                self.rejected["concurrent_session"] += 1
                return False
            self.session_id = frame.session_id
            self.last_seq = -1
        if frame.seq <= self.last_seq:
            self.rejected["replay_or_reorder"] += 1
            return False
        if self.last_seq >= 0:
            self.missing_seq += max(0, frame.seq - self.last_seq - 1)
        self.last_seq = frame.seq
        self.last_at = received_s
        self.counts[frame.event] += 1
        self.last_event = frame.event
        return True

    def report(self) -> dict[str, Any]:
        return {
            "schema": "mindforge.game_observation_audit.v1",
            "game_id": self.game_id, "session_id": self.session_id,
            "last_seq": self.last_seq, "counts": dict(self.counts),
            "rejections": dict(self.rejected), "missing_seq": self.missing_seq,
            "boss_phase_verified": False,
            "combat_engagement_verified": False,
            "safe_ssvep_window_verified": False,
        }
