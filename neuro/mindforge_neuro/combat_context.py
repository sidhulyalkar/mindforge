"""Pure, game-agnostic context policy for cosmetic BCI orb presentation.

No game hooks or SSVEP generator are installed here. The host game must provide
verified semantic context. Inferred enemy proximity alone is never sufficient
to begin a timed visual-selection stimulus.
"""
from __future__ import annotations

from dataclasses import dataclass
from enum import Enum
import math

from .mod_bridge import SUPPORTED_GAMES

class OrbMode(str, Enum):
    HIDDEN = "hidden"
    PRIMED = "primed"
    PRESENT = "present"
    DOCKED = "docked"
    RESTING = "resting"


@dataclass(frozen=True)
class CombatContext:
    game_id: str
    session_id: str
    seq: int
    in_game: bool = True
    app_focused: bool = True
    paused: bool = False
    menu_open: bool = False
    cutscene: bool = False
    scene_transition: bool = False
    player_alive: bool = True
    enemy_nearby: bool = False
    enemy_engaged: bool = False
    boss_encounter: bool = False
    incoming_attack: bool = False
    precision_window: bool = False
    platforming_hazard: bool = False
    recovery_opening: bool = False
    opening_duration_s: float = 0.0
    # The hook is advisory and NEVER asserts physical stimulus validation.
    source: str = "synthetic_game_context"


@dataclass(frozen=True)
class OrbDecision:
    mode: OrbMode
    placement_hint: str
    reason: str
    can_offer_selection: bool
    stimulus_enabled: bool = False  # deliberately forbidden in this version


class ContextOrbPolicy:
    """Presentation planner with input TTL, session isolation, and postcombat hold.

    Inputs must come from a game-verified semantic hook, or explicitly labeled
    synthetic context. The local receive time is the only TTL clock used.
    'can_offer_selection' is a *design opportunity*, NOT BCI authorization.
    """
    def __init__(self, game_id: str, *, input_ttl_s: float = 0.75,
                 postcombat_hold_s: float = 2.25, opening_min_s: float = 2.0):
        if game_id not in SUPPORTED_GAMES:
            raise ValueError(f"unsupported game: {game_id}")
        if not all(math.isfinite(x) and x > 0 for x in
                   (input_ttl_s, postcombat_hold_s, opening_min_s)):
            raise ValueError("timing must be positive and finite")
        self.game_id = game_id
        self.input_ttl_s = input_ttl_s
        self.postcombat_hold_s = postcombat_hold_s
        self.opening_min_s = opening_min_s
        self.context: CombatContext | None = None
        self.received_at = float("-inf")
        self.last_fight_at = float("-inf")

    def update(self, context: CombatContext, now_s: float) -> bool:
        if (context.game_id != self.game_id or not context.session_id
                or context.seq < 0 or not math.isfinite(now_s)
                or not math.isfinite(context.opening_duration_s)
                or context.opening_duration_s < 0
                or context.source not in ("synthetic_game_context", "verified_game_hook")):
            return False
        if self.context is not None:
            if context.session_id != self.context.session_id:
                if now_s - self.received_at <= self.input_ttl_s:
                    return False
                self.last_fight_at = float("-inf")
            elif context.seq <= self.context.seq:
                return False
        self.context = context
        self.received_at = now_s
        # Preserve postcombat hold only for genuine fight-state context.
        if context.enemy_engaged or context.boss_encounter:
            self.last_fight_at = now_s
        return True

    def decide(self, now_s: float) -> OrbDecision:
        ctx = self.context
        hidden = OrbDecision(OrbMode.HIDDEN, "none", "no_valid_game_context", False)
        if ctx is None or not math.isfinite(now_s) or now_s - self.received_at > self.input_ttl_s:
            return hidden
        if (not ctx.in_game or not ctx.app_focused or ctx.paused or ctx.menu_open
                or ctx.cutscene or ctx.scene_transition or not ctx.player_alive):
            return OrbDecision(OrbMode.HIDDEN, "none", "hard_visual_veto", False)
        fight = ctx.enemy_engaged or ctx.boss_encounter
        if fight and (ctx.incoming_attack or ctx.precision_window or ctx.platforming_hazard):
            # A small static indicator can remain available without asking gaze
            # to leave a boss telegraph, parry, platform landing, or projectile.
            return OrbDecision(OrbMode.DOCKED, "edge_compact", "high_precision_threat", False)
        if fight:
            placement = "edge_compact" if self.game_id == "cuphead" else "player_follow_hint"
            # Candidate only: need externally verified temporal safety and a
            # qualified real display before any coded visual stimulus.
            candidate = (ctx.recovery_opening
                         and ctx.opening_duration_s >= self.opening_min_s
                         and not ctx.incoming_attack
                         and not ctx.precision_window
                         and not ctx.platforming_hazard)
            return OrbDecision(OrbMode.PRESENT, placement, "engaged", candidate)
        if ctx.enemy_nearby:
            return OrbDecision(OrbMode.PRIMED, "edge_compact", "nearby_unengaged", False)
        if now_s - self.last_fight_at <= self.postcombat_hold_s:
            return OrbDecision(OrbMode.RESTING, "edge_compact", "postcombat", False)
        return OrbDecision(OrbMode.HIDDEN, "none", "exploration", False)
