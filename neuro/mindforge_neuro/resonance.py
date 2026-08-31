from __future__ import annotations

from dataclasses import dataclass
import numpy as np

from .calibration import CalibrationProfile, normalize_calibrated_scores
from .config import AuraTarget
from .events import EventType, NeuralEvent
from .ssvep import SsvepDecision, SsvepDecoder


@dataclass(frozen=True)
class ResonanceCheckpoint:
    seconds: float
    score_multiplier: float
    raw_margin_multiplier: float
    normalized_margin: float


DEFAULT_RESONANCE_CHECKPOINTS = (
    ResonanceCheckpoint(0.55, 1.25, 1.80, 1.80),
    ResonanceCheckpoint(0.75, 1.15, 1.50, 1.45),
    ResonanceCheckpoint(1.00, 1.05, 1.20, 1.10),
    ResonanceCheckpoint(1.25, 1.00, 1.00, 0.85),
    ResonanceCheckpoint(1.45, 1.00, 1.00, 0.75),
)


class ResonanceEpochBuffer:
    """Fixed-capacity buffer containing only samples acquired after coded onset."""

    def __init__(self, channels: int, sample_rate_hz: float, maximum_seconds: float):
        self.channels = int(channels)
        self.sample_rate_hz = float(sample_rate_hz)
        self.capacity = int(np.ceil(maximum_seconds * sample_rate_hz)) + 8
        self._data = np.empty((self.channels, self.capacity), dtype=float)
        self._timestamps = np.empty(self.capacity, dtype=float)
        self.epoch = -1
        self.count = 0

    def begin(self, epoch: int) -> None:
        self.epoch = int(epoch)
        self.count = 0

    def clear(self) -> None:
        self.epoch = -1
        self.count = 0

    def push(self, samples_uv: np.ndarray, timestamps_s: np.ndarray) -> None:
        if self.epoch < 0:
            return
        x = np.asarray(samples_uv, dtype=float)
        ts = np.asarray(timestamps_s, dtype=float)
        if x.ndim != 2 or x.shape[0] != self.channels:
            raise ValueError(f"expected ({self.channels}, n) samples, got {x.shape}")
        if ts.ndim != 1 or ts.size != x.shape[1]:
            raise ValueError("timestamps must match sample count")
        if ts.size > 1 and np.any(np.diff(ts) < 0):
            raise ValueError("timestamps moved backwards")
        remaining = self.capacity - self.count
        take = min(remaining, x.shape[1])
        if take <= 0:
            return
        self._data[:, self.count:self.count + take] = x[:, :take]
        self._timestamps[self.count:self.count + take] = ts[:take]
        self.count += take

    def samples_for(self, seconds: float) -> int:
        return int(round(float(seconds) * self.sample_rate_hz))

    def ready(self, seconds: float) -> bool:
        return self.count >= self.samples_for(seconds)

    def snapshot(self, seconds: float) -> np.ndarray:
        n = self.samples_for(seconds)
        if n > self.count:
            raise ValueError("requested checkpoint is not available")
        return self._data[:, :n].copy()


class ResonanceEpochRuntime:
    """Convert one Unity-coded epoch into at most one authoritative selection.

    Intermediate uncertainty never emits ABSTAIN because Unity treats an abstain as a terminal
    outcome. Only a successful dynamic checkpoint or the final checkpoint crosses the authority
    boundary. This keeps the causal unit exactly one player-armed resonance window.
    """

    def __init__(
        self,
        decoder: SsvepDecoder,
        profile: CalibrationProfile,
        *,
        source_mode: str = "live",
        initial_seq: int = 0,
        session_id: str | None = None,
        calibration_id: str | None = None,
        checkpoints: tuple[ResonanceCheckpoint, ...] = DEFAULT_RESONANCE_CHECKPOINTS,
        authority_ttl_ms: int = 300,
    ):
        if not checkpoints:
            raise ValueError("at least one resonance checkpoint is required")
        if any(b.seconds <= a.seconds for a, b in zip(checkpoints, checkpoints[1:])):
            raise ValueError("resonance checkpoints must be strictly increasing")
        self.decoder = decoder
        self.profile = profile
        self.source_mode = source_mode
        self.session_id = session_id
        self.calibration_id = calibration_id
        self.checkpoints = tuple(checkpoints)
        self.authority_ttl_ms = max(50, int(authority_ttl_ms))
        self.buffer = ResonanceEpochBuffer(8, decoder.config.sample_rate_hz, checkpoints[-1].seconds)
        self.seq = int(initial_seq)
        self.active_epoch = -1
        self._next_checkpoint = 0

    @property
    def active(self) -> bool:
        return self.active_epoch >= 0

    def begin_epoch(self, epoch: int, *, session_id: str | None = None) -> None:
        if int(epoch) < 0:
            raise ValueError("stimulus epoch must be non-negative")
        self.active_epoch = int(epoch)
        self._next_checkpoint = 0
        self.buffer.begin(self.active_epoch)
        if session_id:
            self.session_id = session_id

    def cancel_epoch(self, epoch: int | None = None) -> None:
        if epoch is not None and self.active_epoch >= 0 and int(epoch) != self.active_epoch:
            return
        self.active_epoch = -1
        self._next_checkpoint = 0
        self.buffer.clear()

    def _event(
        self,
        decision: SsvepDecision,
        checkpoint: ResonanceCheckpoint,
        *,
        target: AuraTarget | None,
        accepted: bool,
        reason: str | None,
    ) -> NeuralEvent:
        self.seq += 1
        evidence_ms = int(round(checkpoint.seconds * 1000.0))
        return NeuralEvent.create(
            seq=self.seq,
            event=EventType.AURA_SELECTED if accepted else EventType.ABSTAIN,
            target=target if accepted else None,
            confidence=decision.confidence,
            quality=decision.quality.score,
            model_id=self.profile.model_id,
            reason=reason,
            artifact=decision.quality.artifact,
            sight_score=decision.scores.get(AuraTarget.SIGHT, 0.0),
            guard_score=decision.scores.get(AuraTarget.GUARD, 0.0),
            margin=max(0.0, decision.margin),
            source_mode=self.source_mode,
            session_id=self.session_id,
            calibration_id=self.calibration_id,
            authority_ttl_ms=self.authority_ttl_ms if accepted else 0,
            stimulus_epoch=self.active_epoch,
            evidence_ms=evidence_ms,
        )

    def _evaluate(self, window: np.ndarray, checkpoint: ResonanceCheckpoint) -> tuple[SsvepDecision, bool, str | None]:
        raw_gate = self.profile.min_score * checkpoint.score_multiplier
        margin_gate = self.profile.min_margin * checkpoint.raw_margin_multiplier
        decision = self.decoder.decide_window(window, min_score=raw_gate, min_margin=0.0)
        if decision.quality.artifact or decision.quality.score < self.decoder.config.min_quality:
            return decision, False, decision.reason or "LOW_QUALITY"
        if not decision.scores:
            return decision, False, "NO_EVIDENCE"

        normalized = normalize_calibrated_scores(self.profile, decision.scores)
        ranked_norm = sorted(normalized.items(), key=lambda kv: kv[1], reverse=True)
        norm_winner, norm_top = ranked_norm[0]
        norm_margin = float(norm_top - ranked_norm[1][1])
        raw_winner = max(decision.scores.items(), key=lambda kv: kv[1])[0]

        # When calibration has enough opposite-target trials, normalized evidence owns target
        # identity. Without it, fall back to the historical raw FBCCA winner/margin contract.
        if self.profile.normalization_ready:
            winner = norm_winner
            if decision.scores[winner] < raw_gate:
                return decision, False, "LOW_SCORE"
            if norm_margin < checkpoint.normalized_margin:
                return decision, False, "LOW_NORMALIZED_MARGIN"
            accepted = True
        else:
            winner = raw_winner
            accepted = decision.scores[winner] >= raw_gate and decision.margin >= margin_gate
            if not accepted:
                return decision, False, decision.reason or "LOW_MARGIN"

        # Rebuild the decision with the normalized winner if calibration corrected a frequency bias.
        if winner != decision.target:
            other = AuraTarget.GUARD if winner == AuraTarget.SIGHT else AuraTarget.SIGHT
            corrected_margin = max(0.0, decision.scores[winner] - decision.scores[other])
            decision = SsvepDecision(
                winner, decision.scores, decision.confidence, corrected_margin,
                decision.quality, True, None,
            )
        return decision, accepted, None

    def push(self, samples_uv: np.ndarray, timestamps_s: np.ndarray) -> NeuralEvent | None:
        if not self.active:
            return None
        self.buffer.push(samples_uv, timestamps_s)
        while self._next_checkpoint < len(self.checkpoints):
            checkpoint = self.checkpoints[self._next_checkpoint]
            if not self.buffer.ready(checkpoint.seconds):
                return None
            window = self.buffer.snapshot(checkpoint.seconds)
            decision, accepted, reason = self._evaluate(window, checkpoint)
            final = self._next_checkpoint == len(self.checkpoints) - 1
            self._next_checkpoint += 1
            if accepted and decision.target is not None:
                event = self._event(decision, checkpoint, target=decision.target, accepted=True, reason=None)
                self.cancel_epoch()
                return event
            if final:
                event = self._event(decision, checkpoint, target=None, accepted=False,
                                    reason=reason or "DYNAMIC_TIMEOUT")
                self.cancel_epoch()
                return event
        return None
