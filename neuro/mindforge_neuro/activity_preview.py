"""Conservative opt-in preview from observed *player actions*, never combat.

Converts unqualified native observations into the existing context protocol.
This cannot discover enemy engagement, boss staggers, incoming attacks, safe
selection windows, or player anchor fidelity. All such fields remain false.
No EEG or gameplay authority is generated here.
"""
from __future__ import annotations

import math

from .context_transport import GameContextPacket, make_context_packet
from .game_observation import GameObservation, ObservationMonitor, GAMES


class ActivityPreviewBridge:
    """Only two recent attacks can briefly prime a cosmetic edge indicator.

    Jump, dash and ordinary heartbeat never prime the orb. Death, damage,
    scene transition, pause, source loss and focus loss immediately withdraw.
    All times are LOCAL receiver monotonic time, never source timestamps.
    """

    def __init__(self, game_id: str, *, attack_window_s: float = 1.2,
                 activity_hold_s: float = 0.65, source_timeout_s: float = 0.72,
                 output_ttl_ms: int = 250):
        if game_id not in GAMES:
            raise ValueError("unsupported observation game")
        if not (0 < activity_hold_s <= attack_window_s <= 3.
                and 0 < source_timeout_s <= 0.75
                and 100 <= output_ttl_ms <= 500):
            raise ValueError("invalid activity preview bounds")
        self.game_id = game_id
        self.attack_window_s = attack_window_s
        self.activity_hold_s = activity_hold_s
        self.source_timeout_s = source_timeout_s
        self.output_ttl_ms = output_ttl_ms
        self.audit = ObservationMonitor(game_id)
        self.session_id: str | None = None
        self.source_mode = "synthetic_game_observation"
        self.last_event: GameObservation | None = None
        self.last_received_s: float | None = None
        self.ready = False
        self.attacks: list[float] = []
        self.last_attack_s: float | None = None
        self.seq = 0

    def accept(self, event: GameObservation, now_s: float) -> bool:
        if not self.audit.accept(event, now_s):
            return False
        if event.session_id != self.session_id:
            self.session_id = event.session_id
            self.ready = False
            self.attacks = []
            self.last_attack_s = None
        self.source_mode = event.source_mode
        self.last_event = event
        self.last_received_s = now_s
        self.attacks = [t for t in self.attacks if now_s - t <= self.attack_window_s]

        if event.event in ("hero_die", "scene_changed"):
            self.ready = False
            self.attacks = []
            self.last_attack_s = None
        elif event.event == "hero_ready":
            self.ready = bool(event.in_game and event.app_focused and not event.paused)
            self.attacks = []
            self.last_attack_s = None
        elif not event.in_game or not event.app_focused or event.paused:
            self.attacks = []
            self.last_attack_s = None
        elif event.event == "hero_damaged":
            self.attacks = []
            self.last_attack_s = None
        elif event.event == "hero_attack" and self.ready:
            self.attacks.append(now_s)
            self.last_attack_s = now_s
        return True

    def snapshot(self, now_s: float) -> GameContextPacket | None:
        if not math.isfinite(now_s):
            raise ValueError("invalid local snapshot time")
        event = self.last_event
        if event is None or self.session_id is None:
            return None
        self.seq += 1
        fresh = (self.last_received_s is not None
                 and 0 <= now_s - self.last_received_s <= self.source_timeout_s)
        playable = bool(fresh and self.ready and event.in_game
                        and event.app_focused and not event.paused)
        recent_attack = bool(self.last_attack_s is not None
                             and 0 <= now_s - self.last_attack_s <= self.activity_hold_s)
        active = playable and recent_attack and len(self.attacks) >= 2
        # 'observed_game_hook_unqualified' is retained through to the
        # presentation widget. It must NEVER become 'verified_game_hook'.
        mode = ("observed_game_hook_unqualified"
                if self.source_mode == "observed_game_hook_unqualified"
                else "synthetic_game_context")
        return make_context_packet(
            self.game_id, self.session_id, self.seq, source=mode,
            ttl_ms=self.output_ttl_ms, in_game=playable,
            app_focused=bool(fresh and event.app_focused),
            paused=event.paused if fresh else True,
            player_alive=bool(playable), player_activity_recent=active,
            # No combat assertion and no verified player anchor.
        )

    def audit_report(self) -> dict[str, object]:
        report = self.audit.report()
        report.update({
            "activity_preview_only": True,
            "combat_and_boss_inference": False,
            "ssvep_stimulus_enabled": False,
            "preview_source_mode": self.source_mode,
        })
        return report
