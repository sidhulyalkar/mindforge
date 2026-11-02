"""Opt-in, derived-only passive ambient modulation policy for game mods.

Consumes the *field contract* designed in Living World v3 (draft PR #64).
This module does not generate neural features, classify emotion/focus, alter
combat, change VEP stimulus surfaces, or directly modify game files. It only
returns a bounded, rate-limited decoration coefficient for future adapters.
"""
from __future__ import annotations

from dataclasses import dataclass
import math
from typing import Mapping

PASSIVE_SCHEMA = "mindforge.neural_feature_frame.v1"
PASSIVE_FEATURE = "posterior_alpha_relative_power"
PASSIVE_MODES = frozenset(("simulated_feature", "feature_replay", "eeg_replay", "live"))

@dataclass(frozen=True)
class AmbientContext:
    focused: bool = True
    in_game: bool = True
    paused: bool = False
    cutscene: bool = False
    immediate_danger: bool = False
    active_ssvep_window: bool = False
    calibration_window: bool = False


class PassiveAmbientPolicy:
    """Low-amplitude decorative response to a verified spectral ratio.

    The coefficient is NOT an emotion, focus, or stress score. The caller must
    opt in and supply a participant-specific baseline measured separately.
    Simulation may use a declared baseline. UI adapters must restrict effects
    to isolated decorative surfaces (not brightness/health/enemy behavior).
    """

    def __init__(self, *, opted_in: bool = False,
                 baseline_alpha: float | None = None,
                 quality_floor: float = 0.65,
                 max_modulation: float = 0.08,
                 slew_per_second: float = 0.08):
        if baseline_alpha is not None and (not math.isfinite(baseline_alpha)
                                           or not 0.0 <= baseline_alpha <= 1.0):
            raise ValueError("invalid baseline")
        if not (0 <= quality_floor <= 1 and 0 < max_modulation <= 0.1
                and math.isfinite(slew_per_second) and 0 < slew_per_second <= 0.1):
            raise ValueError("invalid passive policy bounds")
        self.opted_in = opted_in
        self.baseline_alpha = baseline_alpha
        self.quality_floor = quality_floor
        self.max_modulation = max_modulation
        self.slew_per_second = slew_per_second
        self.source_session_id: str | None = None
        self.seq = -1
        self.last_at: float | None = None
        self.ttl_s = 0.0
        self.target = 0.0
        self.value = 0.0
        self.last_tick: float | None = None

    def withdraw(self) -> None:
        self.target = 0.0
        self.last_at = None

    def accept(self, frame: Mapping[str, object], now_s: float) -> bool:
        """Return true only for validated, quality-gated, passive frames.

        No source monotonic timestamps are compared with the host process clock.
        """
        if not self.opted_in or self.baseline_alpha is None or not math.isfinite(now_s):
            self.withdraw()
            return False
        try:
            source = frame["source_session_id"]
            mode = frame["source_mode"]
            seq = frame["seq"]
            ttl = frame["ttl_ms"]
            value = frame["value"]
            quality = frame["quality"]
            good = (
                frame["schema"] == PASSIVE_SCHEMA
                and frame["feature"] == PASSIVE_FEATURE
                and frame["unit"] == "ratio"
                and frame["passive_only"] is True
                and frame["artifact"] is False
                and mode in PASSIVE_MODES
                and type(source) is str and 1 <= len(source) <= 128
                and type(seq) is int and seq >= 0
                and type(ttl) is int and 100 <= ttl <= 5000
                and type(value) in (float, int) and math.isfinite(value) and 0 <= value <= 1
                and type(quality) in (float, int) and math.isfinite(quality)
                and quality >= self.quality_floor
                and quality <= 1.0
                and (mode != "live" or bool(frame.get("calibration_id")))
            )
        except (KeyError, TypeError, ValueError, OverflowError):
            return False
        if not good:
            # Unreliable or artifact-tainted data withdraws the existing target.
            self.withdraw()
            return False
        if source != self.source_session_id:
            if self.last_at is not None and now_s - self.last_at <= self.ttl_s:
                return False
            self.source_session_id = source
            self.seq = -1
        if seq <= self.seq:
            return False
        self.seq = seq
        self.last_at = now_s
        self.ttl_s = ttl / 1000.0
        # The difference is purely spectral; don't label as calm/engagement.
        self.target = max(-self.max_modulation,
                          min(self.max_modulation, (value - self.baseline_alpha) * 0.2))
        return True

    def step(self, now_s: float, context: AmbientContext) -> float:
        if not math.isfinite(now_s):
            raise ValueError("step time must be finite")
        if self.last_tick is None:
            self.last_tick = now_s
            return self.value
        dt = max(0., min(now_s - self.last_tick, 1.0))
        self.last_tick = now_s
        allowed = (self.opted_in and self.baseline_alpha is not None
                   and context.in_game and context.focused and not context.paused
                   and not context.cutscene and not context.immediate_danger
                   and not context.active_ssvep_window
                   and not context.calibration_window)
        recent = (self.last_at is not None and now_s - self.last_at <= self.ttl_s)
        goal = self.target if allowed and recent else 0.0
        max_step = dt * self.slew_per_second
        self.value += max(-max_step, min(max_step, goal - self.value))
        self.value = max(-self.max_modulation, min(self.max_modulation, self.value))
        return self.value

    def opt_out(self) -> None:
        self.opted_in = False
        self.target = self.value = 0.0
        self.last_at = None
        self.source_session_id = None
        self.seq = -1
