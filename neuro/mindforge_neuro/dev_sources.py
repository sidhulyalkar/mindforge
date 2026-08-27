from __future__ import annotations

import json
import math
import time
from dataclasses import dataclass, replace
from pathlib import Path
from typing import Iterable, Iterator

import numpy as np

from .config import AuraTarget
from .events import EventType, NeuralEvent, SourceMode


@dataclass(frozen=True)
class DecisionSimulationConfig:
    seed: int = 17
    confidence_mean: float = 0.86
    quality_mean: float = 0.91
    score_floor: float = 0.10
    score_peak: float = 0.72
    jitter: float = 0.035
    authority_ttl_ms: int = 900
    model_id: str = "decision-simulator-v1"


class DecisionSimulator:
    """Deterministic decision-level BCI source for game development.

    This deliberately does *not* pretend to simulate EEG. It substitutes the
    decoder output boundary so designers can exercise authority, abstention and
    disconnect behavior without implying physiological validity.
    """

    def __init__(
        self,
        config: DecisionSimulationConfig | None = None,
        *,
        session_id: str | None = None,
        calibration_id: str | None = None,
        initial_seq: int = 0,
    ):
        self.config = config or DecisionSimulationConfig()
        self.rng = np.random.default_rng(self.config.seed)
        self.seq = max(0, int(initial_seq))
        self.session_id = session_id or f"dev-{int(time.time())}"
        self.calibration_id = calibration_id or None

    def next(self, state: str | AuraTarget) -> NeuralEvent:
        self.seq += 1
        state_text = state.value if isinstance(state, AuraTarget) else str(state).strip().lower()
        if state_text in {"lost", "disconnect", "offline"}:
            return self._control(EventType.BCI_LOST, "SIMULATED_LINK_LOSS")
        if state_text in {"recovered", "recover", "online"}:
            return self._control(EventType.BCI_RECOVERED, "SIMULATED_LINK_RECOVERY")
        if state_text in {"stop", "participant_stop"}:
            return self._control(EventType.PARTICIPANT_STOP, "SIMULATED_PARTICIPANT_STOP")
        if state_text in {"none", "abstain", "rest"}:
            return NeuralEvent.create(
                seq=self.seq,
                event=EventType.ABSTAIN,
                target=None,
                confidence=self._clip(self.config.confidence_mean * 0.35),
                quality=self._clip(self.config.quality_mean),
                model_id=self.config.model_id,
                reason="SIMULATED_ABSTAIN",
                sight_score=self._score(self.config.score_floor),
                guard_score=self._score(self.config.score_floor),
                margin=0.0,
                source_mode=SourceMode.SIMULATED_DECISION.value,
                session_id=self.session_id,
                calibration_id=self.calibration_id,
                authority_ttl_ms=self.config.authority_ttl_ms,
            )

        target = AuraTarget(state_text)
        winner = self._score(self.config.score_peak)
        loser = self._score(self.config.score_floor)
        sight, guard = (winner, loser) if target == AuraTarget.SIGHT else (loser, winner)
        return NeuralEvent.create(
            seq=self.seq,
            event=EventType.AURA_SELECTED,
            target=target,
            confidence=self._clip(self.config.confidence_mean + self._noise()),
            quality=self._clip(self.config.quality_mean + self._noise()),
            model_id=self.config.model_id,
            reason="SIMULATED_DECISION",
            sight_score=sight,
            guard_score=guard,
            margin=abs(winner - loser),
            source_mode=SourceMode.SIMULATED_DECISION.value,
            session_id=self.session_id,
            calibration_id=self.calibration_id,
            authority_ttl_ms=self.config.authority_ttl_ms,
        )

    def _control(self, kind: EventType, reason: str) -> NeuralEvent:
        return NeuralEvent.create(
            seq=self.seq,
            event=kind,
            target=None,
            confidence=0.0,
            quality=0.0 if kind == EventType.BCI_LOST else self._clip(self.config.quality_mean),
            model_id=self.config.model_id,
            reason=reason,
            source_mode=SourceMode.SIMULATED_DECISION.value,
            session_id=self.session_id,
            calibration_id=self.calibration_id,
            authority_ttl_ms=0,
        )

    def _noise(self) -> float:
        return float(self.rng.normal(0.0, self.config.jitter))

    def _score(self, center: float) -> float:
        return max(0.0, float(center + self._noise()))

    @staticmethod
    def _clip(value: float) -> float:
        return max(0.0, min(1.0, float(value)))


@dataclass(frozen=True)
class TapeEntry:
    offset_s: float
    event: NeuralEvent


class NeuralEventTape:
    """Portable decision-event recording for exact gameplay reproduction."""

    schema = "mindforge.neural_tape.v1"

    def __init__(self, entries: Iterable[TapeEntry] = ()):
        self.entries = tuple(sorted(entries, key=lambda item: item.offset_s))
        if any(not math.isfinite(item.offset_s) or item.offset_s < 0.0 for item in self.entries):
            raise ValueError("tape offsets must be finite and non-negative")

    @classmethod
    def load(cls, path: str | Path) -> "NeuralEventTape":
        path = Path(path)
        text = path.read_text(encoding="utf-8")
        stripped = text.lstrip()
        if not stripped:
            return cls()
        if stripped.startswith("["):
            records = json.loads(text)
        else:
            records = [json.loads(line) for line in text.splitlines() if line.strip()]
        entries: list[TapeEntry] = []
        for record in records:
            if not isinstance(record, dict):
                raise ValueError("tape entry must be an object")
            if record.get("schema") == cls.schema:
                payload = record.get("event")
                offset = float(record.get("offset_s", 0.0))
            else:
                # Raw NeuralEvent JSONL is also accepted. This is intentionally
                # forgiving for small developer captures.
                payload = record
                offset = float(record.get("offset_s", 0.0))
            if not isinstance(payload, dict):
                raise ValueError("tape event must be an object")
            entries.append(TapeEntry(offset, NeuralEvent.from_dict(payload)))
        return cls(entries)

    def save(self, path: str | Path) -> None:
        path = Path(path)
        path.parent.mkdir(parents=True, exist_ok=True)
        lines = [json.dumps({
            "schema": self.schema,
            "offset_s": entry.offset_s,
            "event": entry.event.to_dict(),
        }, separators=(",", ":"), sort_keys=True) for entry in self.entries]
        path.write_text("\n".join(lines) + ("\n" if lines else ""), encoding="utf-8")

    def replay_events(
        self,
        *,
        initial_seq: int = 0,
        session_id: str | None = None,
        calibration_id: str | None = None,
    ) -> Iterator[TapeEntry]:
        replay_session = session_id or f"decision-replay-{int(time.time())}"
        for index, entry in enumerate(self.entries, start=1):
            original = entry.event
            yield TapeEntry(
                entry.offset_s,
                replace(
                    original,
                    schema="mindforge.neural_event.v2",
                    seq=initial_seq + index,
                    monotonic_ns=0,
                    decoder_time_ns=0,
                    source_mode=SourceMode.DECISION_REPLAY.value,
                    session_id=replay_session,
                    calibration_id=calibration_id or original.calibration_id,
                ),
            )
