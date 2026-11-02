"""Portable, presentation-only BCI bridge for opt-in game mods.

This module never generates SSVEP stimuli or alters game mechanics. Raw EEG stays
in the acquisition process. Timestamps from other processes are not compared.
"""
from __future__ import annotations

from dataclasses import dataclass

from .config import AuraTarget
from .events import EventType, KNOWN_SOURCE_MODES, NEURAL_EVENT_V2, NeuralEvent

MOD_FRAME_SCHEMA = "mindforge.mod_state.v1"
SUPPORTED_GAMES = frozenset({"silksong", "cuphead", "hollow_knight"})


@dataclass(frozen=True)
class ModBridgePolicy:
    min_quality: float = 0.55
    min_confidence: float = 0.60
    effect_preview_s: float = 2.5
    source_timeout_s: float = 1.25
    frame_ttl_ms: int = 400


class ModBridgeState:
    """Fail-neutral state machine between NeuralEvent and a cosmetic mod display.

    Feed events in their local receive order. No absolute source timestamps are
    used: different processes have different monotonic-clock epochs. A bridge
    timeout withdraws the display, not the game's conventional controls.
    """

    def __init__(self, game_id: str, policy: ModBridgePolicy | None = None):
        if game_id not in SUPPORTED_GAMES:
            raise ValueError(f"unsupported game adapter: {game_id}")
        self.game_id = game_id
        self.policy = policy or ModBridgePolicy()
        self.session_id = ""
        self.source_mode = "none"
        self._last_source_seq = -1
        self._last_source_at: float | None = None
        self._frame_seq = 0
        self._lost = False
        self._stopped = False
        self._sight_until = float("-inf")
        self._guard_until = float("-inf")

    def _reset(self, event: NeuralEvent) -> None:
        self.session_id = event.session_id or ""
        self.source_mode = event.source_mode
        self._last_source_seq = -1
        self._last_source_at = None
        self._lost = False
        self._stopped = False
        self._sight_until = float("-inf")
        self._guard_until = float("-inf")

    def accept(self, event: NeuralEvent, now_s: float) -> bool:
        """Consume a locally received, versioned derived neural event."""
        if (
            event.schema != NEURAL_EVENT_V2
            or not event.session_id
            or event.source_mode not in KNOWN_SOURCE_MODES
            or event.seq < 0
        ):
            return False
        if event.session_id != self.session_id:
            eligible = (
                self._last_source_at is None
                or self._stopped
                or now_s - self._last_source_at > self.policy.source_timeout_s
            )
            if not eligible:
                return False
            self._reset(event)
        if event.seq <= self._last_source_seq:
            return False

        self._last_source_seq = event.seq
        self._last_source_at = now_s
        self.source_mode = event.source_mode
        if event.event == EventType.PARTICIPANT_STOP:
            self._stopped = True
            self._lost = True
            self._sight_until = self._guard_until = float("-inf")
        elif self._stopped:
            # The next session must be distinct after an explicit stop.
            return False
        elif event.event == EventType.BCI_LOST:
            self._lost = True
            self._sight_until = self._guard_until = float("-inf")
        elif event.event == EventType.BCI_RECOVERED:
            self._lost = False
        elif event.event == EventType.AURA_SELECTED and not self._lost:
            valid = (
                event.target in (AuraTarget.SIGHT, AuraTarget.GUARD)
                and not event.artifact
                and event.authority_ttl_ms > 0
                and event.quality >= self.policy.min_quality
                and event.confidence >= self.policy.min_confidence
                and (event.source_mode != "live" or (event.has_evidence and bool(event.calibration_id)))
            )
            if valid:
                until = now_s + self.policy.effect_preview_s
                if event.target == AuraTarget.SIGHT:
                    self._sight_until = until
                else:
                    self._guard_until = until
        return True

    def snapshot(self, now_s: float) -> dict[str, object]:
        self._frame_seq += 1
        timed_out = (
            self._last_source_at is None
            or now_s - self._last_source_at > self.policy.source_timeout_s
        )
        connected = not (timed_out or self._lost or self._stopped)
        link = ("offline" if timed_out else
                "stopped" if self._stopped else
                "lost" if self._lost else "connected")
        return {
            "schema": MOD_FRAME_SCHEMA,
            "game_id": self.game_id,
            "seq": self._frame_seq,
            "session_id": self.session_id,
            "source_mode": self.source_mode,
            "link": link,
            "sight_active": bool(connected and now_s < self._sight_until),
            "guard_active": bool(connected and now_s < self._guard_until),
            "ttl_ms": self.policy.frame_ttl_ms,
            "presentation_only": True,
        }
