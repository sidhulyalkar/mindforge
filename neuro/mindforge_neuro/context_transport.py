"""Strict local transport for contextual orb presentation, without game authority.

A game-specific, opt-in semantic producer may send derived context observations
to a separate Python sidecar. This module does not inspect or alter game memory.
No single field authorizes an SSVEP stimulus or any combat effect.
"""
from __future__ import annotations

from dataclasses import dataclass, fields
import json
import math
from typing import Any, Mapping

from .combat_context import CombatContext, ContextOrbPolicy, OrbMode
from .mod_bridge import SUPPORTED_GAMES

CONTEXT_INPUT_V1 = "mindforge.game_context.v1"
CONTEXT_OUTPUT_V1 = "mindforge.orb_context_state.v1"
SOURCES = frozenset({"synthetic_game_context", "verified_game_hook"})
CONTEXT_FLAGS = (
    "in_game", "app_focused", "paused", "menu_open", "cutscene",
    "scene_transition", "player_alive", "enemy_nearby", "enemy_engaged",
    "boss_encounter", "incoming_attack", "precision_window",
    "platforming_hazard", "recovery_opening",
)
INPUT_KEYS = frozenset({
    "schema", "game_id", "session_id", "seq", "source", "ttl_ms",
    "context", "anchor_x", "anchor_y",
})


@dataclass(frozen=True)
class GameContextPacket:
    game_id: str
    session_id: str
    seq: int
    source: str
    ttl_ms: int
    context: CombatContext
    anchor_x: float | None = None
    anchor_y: float | None = None

    @classmethod
    def from_dict(cls, obj: Mapping[str, Any]) -> "GameContextPacket":
        if not isinstance(obj, Mapping) or set(obj) != INPUT_KEYS:
            raise ValueError("invalid game context envelope")
        if (obj["schema"] != CONTEXT_INPUT_V1 or obj["game_id"] not in SUPPORTED_GAMES
                or obj["source"] not in SOURCES or type(obj["seq"]) is not int
                or obj["seq"] < 0 or type(obj["ttl_ms"]) is not int
                or not 100 <= obj["ttl_ms"] <= 750 or
                type(obj["session_id"]) is not str or
                not 1 <= len(obj["session_id"]) <= 128):
            raise ValueError("invalid game context identity/provenance")
        ctx = obj["context"]
        if not isinstance(ctx, Mapping) or set(ctx) != (set(CONTEXT_FLAGS) | {"opening_duration_s"}):
            raise ValueError("missing/extra semantic context fields")
        if any(type(ctx[k]) is not bool for k in CONTEXT_FLAGS):
            raise ValueError("context flags must be booleans")
        seconds = ctx["opening_duration_s"]
        if type(seconds) not in (float, int) or not math.isfinite(seconds) or not 0 <= seconds <= 60:
            raise ValueError("invalid recovery opening duration")
        x, y = obj["anchor_x"], obj["anchor_y"]
        if (x is None) != (y is None):
            raise ValueError("player anchor requires two coordinates")
        if x is not None and (type(x) not in (float, int) or type(y) not in (float, int)
                              or not math.isfinite(x) or not math.isfinite(y)
                              or not 0.0 <= x <= 1.0 or not 0.0 <= y <= 1.0):
            raise ValueError("player anchor must be normalized and finite")
        context = CombatContext(
            game_id=obj["game_id"], session_id=obj["session_id"],
            seq=obj["seq"], source=obj["source"],
            **dict(ctx),
        )
        return cls(obj["game_id"], obj["session_id"], obj["seq"], obj["source"],
                   obj["ttl_ms"], context, x, y)

    @classmethod
    def from_json(cls, raw: str | bytes) -> "GameContextPacket":
        return cls.from_dict(json.loads(raw))

    def to_dict(self) -> dict[str, Any]:
        context = {name: getattr(self.context, name)
                   for name in (*CONTEXT_FLAGS, "opening_duration_s")}
        return {
            "schema": CONTEXT_INPUT_V1,
            "game_id": self.game_id,
            "session_id": self.session_id,
            "seq": self.seq,
            "source": self.source,
            "ttl_ms": self.ttl_ms,
            "context": context,
            "anchor_x": self.anchor_x,
            "anchor_y": self.anchor_y,
        }

    def to_json(self) -> str:
        # Validate every generated packet against the same strict input gate.
        payload = self.to_dict()
        self.from_dict(payload)
        return json.dumps(payload, sort_keys=True, separators=(",", ":"), allow_nan=False)


def make_context_packet(game_id: str, session_id: str, seq: int, *,
                        source: str = "synthetic_game_context", ttl_ms: int = 400,
                        anchor: tuple[float, float] | None = None,
                        **flags: Any) -> GameContextPacket:
    """Convenience for testing; never labels a packet verified by default."""
    allowed = set(CONTEXT_FLAGS) | {"opening_duration_s"}
    if not set(flags).issubset(allowed):
        raise ValueError("unknown game-context flags")
    x, y = anchor if anchor is not None else (None, None)
    ctx = CombatContext(game_id=game_id, session_id=session_id,
                        seq=seq, source=source, **flags)
    packet = GameContextPacket(game_id, session_id, seq, source,
                               ttl_ms, ctx, x, y)
    GameContextPacket.from_dict(packet.to_dict())
    return packet


class OrbContextBridge:
    """Input validation + conservative output frames for an external cosmetic mod."""

    def __init__(self, game_id: str, *, output_ttl_ms: int = 250):
        if game_id not in SUPPORTED_GAMES or not 100 <= output_ttl_ms <= 500:
            raise ValueError("invalid game or outgoing TTL")
        self.game_id = game_id
        self.policy = ContextOrbPolicy(game_id)
        self.latest: GameContextPacket | None = None
        self.last_at: float | None = None
        self.seq = 0
        self.output_ttl_ms = output_ttl_ms

    def accept(self, packet: GameContextPacket, now_s: float) -> bool:
        if not math.isfinite(now_s) or packet.game_id != self.game_id:
            return False
        # Never let a failed context update replace a valid packet.
        if not self.policy.update(packet.context, now_s):
            return False
        self.latest = packet
        self.last_at = now_s
        return True

    def snapshot(self, now_s: float) -> dict[str, Any]:
        if not math.isfinite(now_s):
            raise ValueError("snapshot clock must be finite")
        self.seq += 1
        pkt = self.latest
        valid = (pkt is not None and self.last_at is not None
                 and 0 <= now_s - self.last_at < pkt.ttl_ms / 1000.)
        decision = self.policy.decide(now_s) if valid else None
        visible = bool(decision is not None and decision.mode != OrbMode.HIDDEN)
        follow = bool(visible and decision is not None
                      and decision.placement_hint == "player_follow_hint"
                      and pkt is not None and pkt.anchor_x is not None
                      and pkt.source == "verified_game_hook")
        return {
            "schema": CONTEXT_OUTPUT_V1,
            "game_id": self.game_id,
            "session_id": pkt.session_id if valid and pkt is not None else "",
            "seq": self.seq,
            "source": pkt.source if valid and pkt is not None else "none",
            "mode": decision.mode.value if decision else "hidden",
            "placement": ("player_anchor" if follow else "edge_compact"
                          if visible else "none"),
            "anchor_x": pkt.anchor_x if follow else None,
            "anchor_y": pkt.anchor_y if follow else None,
            "selection_opportunity_only": bool(
                decision is not None and decision.can_offer_selection),
            "stimulus_enabled": False,
            "presentation_only": True,
            "reason": decision.reason if decision else "missing_or_stale_context",
            "ttl_ms": self.output_ttl_ms,
        }
