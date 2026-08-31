from __future__ import annotations

from pathlib import Path


def read(path: str) -> str:
    return Path(path).read_text(encoding="utf-8")


def write(path: str, text: str) -> None:
    Path(path).write_text(text, encoding="utf-8")


def replace_once(path: str, old: str, new: str) -> None:
    text = read(path)
    count = text.count(old)
    if count != 1:
        raise SystemExit(f"{path}: expected exactly one replacement target, found {count}\nTARGET:\n{old[:300]}")
    write(path, text.replace(old, new, 1))


# ---------------------------------------------------------------------------
# Python event provenance: a Unity resonance epoch owns every accepted event.
# ---------------------------------------------------------------------------
path = "neuro/mindforge_neuro/events.py"
replace_once(path,
'''    authority_ttl_ms: int = 900

    # V0.8 participant-calibration metadata.''',
'''    authority_ttl_ms: int = 900

    # V0.14 causal provenance. stimulus_epoch is Unity's resonance-window id.
    # evidence_ms is the amount of EEG acquired strictly after coded onset.
    stimulus_epoch: int = -1
    evidence_ms: int = 0

    # V0.8 participant-calibration metadata.''')
replace_once(path,
'''        authority_ttl_ms: int = 900,
        stimulus_hz: float = 0.0,''',
'''        authority_ttl_ms: int = 900,
        stimulus_epoch: int = -1,
        evidence_ms: int = 0,
        stimulus_hz: float = 0.0,''')
replace_once(path,
'''            authority_ttl_ms=max(0, int(authority_ttl_ms)),
            stimulus_hz=float(max(0.0, stimulus_hz)),''',
'''            authority_ttl_ms=max(0, int(authority_ttl_ms)),
            stimulus_epoch=int(stimulus_epoch),
            evidence_ms=max(0, int(evidence_ms)),
            stimulus_hz=float(max(0.0, stimulus_hz)),''')
replace_once(path,
'''            authority_ttl_ms=int(payload.get("authority_ttl_ms", 0 if schema == NEURAL_EVENT_V1 else 900)),
            stimulus_hz=float(payload.get("stimulus_hz", 0.0)),''',
'''            authority_ttl_ms=int(payload.get("authority_ttl_ms", 0 if schema == NEURAL_EVENT_V1 else 900)),
            stimulus_epoch=int(payload.get("stimulus_epoch", -1)),
            evidence_ms=int(payload.get("evidence_ms", 0)),
            stimulus_hz=float(payload.get("stimulus_hz", 0.0)),''')

# JSON schema remains backwards-compatible with historical v2 recordings while allowing
# the new causal provenance fields emitted by V0.14.
path = "contracts/neural_event.v2.schema.json"
replace_once(path,
'''    "decoder_time_ns": {"type": "integer", "minimum": 0},
    "authority_ttl_ms": {"type": "integer", "minimum": 0}
''',
'''    "decoder_time_ns": {"type": "integer", "minimum": 0},
    "authority_ttl_ms": {"type": "integer", "minimum": 0},
    "stimulus_epoch": {"type": "integer", "minimum": -1},
    "evidence_ms": {"type": "integer", "minimum": 0}
''')

# ---------------------------------------------------------------------------
# Cache filter designs and allow cumulative variable-length FBCCA windows.
# ---------------------------------------------------------------------------
write("neuro/mindforge_neuro/ssvep.py", '''from __future__ import annotations

from dataclasses import dataclass
import numpy as np
from scipy.signal import butter, sosfiltfilt

from .config import AuraTarget, SsvepConfig
from .quality import SignalQuality, assess_window_quality


@dataclass(frozen=True)
class SsvepDecision:
    target: AuraTarget | None
    scores: dict[AuraTarget, float]
    confidence: float
    margin: float
    quality: SignalQuality
    accepted: bool
    reason: str | None


def _invsqrt_psd(matrix: np.ndarray, floor: float = 1e-8) -> np.ndarray:
    vals, vecs = np.linalg.eigh(matrix)
    vals = np.maximum(vals, floor)
    return (vecs * (1.0 / np.sqrt(vals))) @ vecs.T


def canonical_correlation(x: np.ndarray, y: np.ndarray, regularization: float = 1e-4) -> float:
    x = np.asarray(x, dtype=float)
    y = np.asarray(y, dtype=float)
    if x.ndim != 2 or y.ndim != 2 or x.shape[1] != y.shape[1]:
        raise ValueError("x and y must be 2D with equal sample count")
    x = x - np.mean(x, axis=1, keepdims=True)
    y = y - np.mean(y, axis=1, keepdims=True)
    n = max(1, x.shape[1] - 1)
    cxx = (x @ x.T) / n + regularization * np.eye(x.shape[0])
    cyy = (y @ y.T) / n + regularization * np.eye(y.shape[0])
    cxy = (x @ y.T) / n
    whitened = _invsqrt_psd(cxx) @ cxy @ _invsqrt_psd(cyy)
    singular = np.linalg.svd(whitened, compute_uv=False)
    return float(np.clip(singular[0] if singular.size else 0.0, 0.0, 1.0))


def reference_bank(frequency_hz: float, sample_rate_hz: float, samples: int, harmonics: int) -> np.ndarray:
    t = np.arange(samples, dtype=float) / sample_rate_hz
    rows: list[np.ndarray] = []
    for harmonic in range(1, harmonics + 1):
        phase = 2.0 * np.pi * frequency_hz * harmonic * t
        rows.extend((np.sin(phase), np.cos(phase)))
    return np.stack(rows, axis=0)


class SsvepDecoder:
    """Two-target FBCCA decoder with cached filters and cumulative-window scoring.

    The original fixed-window API remains unchanged for calibration/replay. V0.14 adds
    ``score_window``/``decide_window`` so a single Unity resonance epoch can be checked
    at progressively longer evidence durations without inventing overlapping dwell trials.
    """

    def __init__(self, config: SsvepConfig | None = None):
        self.config = config or SsvepConfig()
        self.config.validate()
        self.min_score = self.config.min_score
        self.min_margin = self.config.min_margin
        self._reference_cache: dict[int, dict[AuraTarget, np.ndarray]] = {}
        self._filter_sos: list[np.ndarray] = []
        nyquist = self.config.sample_rate_hz / 2.0
        for low, high in self.config.filter_bands_hz:
            high = min(high, nyquist * 0.95)
            self._filter_sos.append(
                butter(4, [low / nyquist, high / nyquist], btype="bandpass", output="sos")
            )
        self._references_for(self.config.window_samples)

    def set_thresholds(self, *, min_score: float, min_margin: float) -> None:
        self.min_score = float(min_score)
        self.min_margin = float(min_margin)

    def _references_for(self, samples: int) -> dict[AuraTarget, np.ndarray]:
        samples = int(samples)
        refs = self._reference_cache.get(samples)
        if refs is None:
            refs = {
                target: reference_bank(freq, self.config.sample_rate_hz, samples, self.config.harmonics)
                for target, freq in self.config.target_frequencies.items()
            }
            self._reference_cache[samples] = refs
        return refs

    def _filter(self, eeg_uv: np.ndarray, bank_index: int) -> np.ndarray:
        return sosfiltfilt(self._filter_sos[bank_index], eeg_uv, axis=1)

    def score(self, eeg_uv: np.ndarray) -> dict[AuraTarget, float]:
        x = np.asarray(eeg_uv, dtype=float)
        if x.ndim != 2 or x.shape[1] != self.config.window_samples:
            raise ValueError(f"expected (channels, {self.config.window_samples}) EEG window, got {x.shape}")
        return self.score_window(x)

    def score_window(self, eeg_uv: np.ndarray) -> dict[AuraTarget, float]:
        x = np.asarray(eeg_uv, dtype=float)
        if x.ndim != 2 or x.shape[1] < 64:
            raise ValueError(f"expected (channels, >=64) EEG window, got {x.shape}")
        indices = np.asarray(self.config.decode_channel_indices, dtype=int)
        if np.any(indices >= x.shape[0]):
            raise ValueError(f"decode channel index exceeds EEG channel count {x.shape[0]}")
        x_decode = x[indices]
        refs_for_length = self._references_for(x.shape[1])
        aggregate = {target: 0.0 for target in self.config.target_frequencies}
        total_weight = float(sum(self.config.filter_bank_weights))
        for bank_index, weight in enumerate(self.config.filter_bank_weights):
            filtered = self._filter(x_decode, bank_index)
            for target, refs in refs_for_length.items():
                rho = canonical_correlation(filtered, refs)
                aggregate[target] += weight * rho * rho
        return {target: value / total_weight for target, value in aggregate.items()}

    def decide(self, eeg_uv: np.ndarray) -> SsvepDecision:
        x = np.asarray(eeg_uv, dtype=float)
        if x.ndim != 2 or x.shape[1] != self.config.window_samples:
            raise ValueError(f"expected (channels, {self.config.window_samples}) EEG window, got {x.shape}")
        return self.decide_window(x)

    def decide_window(
        self,
        eeg_uv: np.ndarray,
        *,
        min_score: float | None = None,
        min_margin: float | None = None,
    ) -> SsvepDecision:
        x = np.asarray(eeg_uv, dtype=float)
        quality = assess_window_quality(x, self.config.sample_rate_hz)
        if quality.artifact or quality.score < self.config.min_quality:
            return SsvepDecision(None, {t: 0.0 for t in self.config.target_frequencies}, 0.0, 0.0,
                                 quality, False, quality.reason or "LOW_QUALITY")

        scores = self.score_window(x)
        ranked = sorted(scores.items(), key=lambda kv: kv[1], reverse=True)
        winner, top = ranked[0]
        second = ranked[1][1]
        margin = float(top - second)
        confidence = float(np.clip(0.5 + 0.5 * margin / max(top, 1e-9), 0.0, 1.0))
        score_gate = self.min_score if min_score is None else float(min_score)
        margin_gate = self.min_margin if min_margin is None else float(min_margin)
        if top < score_gate:
            return SsvepDecision(winner, scores, confidence, margin, quality, False, "LOW_SCORE")
        if margin < margin_gate:
            return SsvepDecision(winner, scores, confidence, margin, quality, False, "LOW_MARGIN")
        return SsvepDecision(winner, scores, confidence, margin, quality, True, None)
''')

# ---------------------------------------------------------------------------
# Participant-specific target leakage normalization from calibration.
# ---------------------------------------------------------------------------
path = "neuro/mindforge_neuro/calibration.py"
replace_once(path,
'''    trials_per_target: dict[AuraTarget, int]


@dataclass(frozen=True)
class FrequencyPairEvaluation:''',
'''    trials_per_target: dict[AuraTarget, int]
    sight_off_center: float = 0.0
    sight_off_scale: float = 1.0
    guard_off_center: float = 0.0
    guard_off_scale: float = 1.0
    normalization_ready: bool = False


@dataclass(frozen=True)
class FrequencyPairEvaluation:''')
replace_once(path,
'''    accepted = sum(int(decoder.decide(eeg).accepted and decoder.decide(eeg).target == truth)
                   for truth, eeg in trials)
    return CalibrationProfile(model_id, min_score, min_margin, accuracy,
                              accepted / max(1, sum(counts.values())), counts)


def _has_low_order_harmonic_collision''',
'''    accepted = sum(int(decoder.decide(eeg).accepted and decoder.decide(eeg).target == truth)
                   for truth, eeg in trials)

    # Learn each frequency's unattended leakage floor from the opposite-target trials.
    # This prevents a participant's intrinsically strong 10 Hz response from receiving a
    # permanent advantage over 12 Hz simply because raw CCA scores have different baselines.
    off_scores = {AuraTarget.SIGHT: [], AuraTarget.GUARD: []}
    for truth, eeg in trials:
        decision = decoder.decide(eeg)
        if decision.quality.artifact:
            continue
        scores = decoder.score(eeg)
        for target in (AuraTarget.SIGHT, AuraTarget.GUARD):
            if target != truth:
                off_scores[target].append(float(scores[target]))

    def robust_center_scale(values: list[float]) -> tuple[float, float]:
        if len(values) < 3:
            return 0.0, 1.0
        x = np.asarray(values, dtype=float)
        center = float(np.median(x))
        mad = float(np.median(np.abs(x - center))) * 1.4826
        return center, max(0.02, mad)

    sight_center, sight_scale = robust_center_scale(off_scores[AuraTarget.SIGHT])
    guard_center, guard_scale = robust_center_scale(off_scores[AuraTarget.GUARD])
    normalization_ready = min(len(off_scores[AuraTarget.SIGHT]), len(off_scores[AuraTarget.GUARD])) >= 3

    return CalibrationProfile(
        model_id, min_score, min_margin, accuracy,
        accepted / max(1, sum(counts.values())), counts,
        sight_center, sight_scale, guard_center, guard_scale, normalization_ready,
    )


def normalize_calibrated_scores(
    profile: CalibrationProfile,
    scores: dict[AuraTarget, float],
) -> dict[AuraTarget, float]:
    """Express each target score relative to its participant-specific unattended leakage."""
    if not profile.normalization_ready:
        return {target: float(value) for target, value in scores.items()}
    return {
        AuraTarget.SIGHT: (float(scores[AuraTarget.SIGHT]) - profile.sight_off_center) / profile.sight_off_scale,
        AuraTarget.GUARD: (float(scores[AuraTarget.GUARD]) - profile.guard_off_center) / profile.guard_off_scale,
    }


def _has_low_order_harmonic_collision''')

# ---------------------------------------------------------------------------
# Causal epoch buffer + dynamic stopping. No overlapping dwell windows.
# ---------------------------------------------------------------------------
write("neuro/mindforge_neuro/resonance.py", '''from __future__ import annotations

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
''')

# Export new runtime and normalization helpers.
path = "neuro/mindforge_neuro/__init__.py"
replace_once(path,
'''    calibrate_decoder,
    personalized_ssvep_config,''',
'''    calibrate_decoder,
    normalize_calibrated_scores,
    personalized_ssvep_config,''')
replace_once(path,
'''from .markers import GameMarker, GameMarkerType, UdpGameMarkerSource
from .dev_sources''',
'''from .markers import GameMarker, GameMarkerType, UdpGameMarkerSource
from .resonance import DEFAULT_RESONANCE_CHECKPOINTS, ResonanceCheckpoint, ResonanceEpochBuffer, ResonanceEpochRuntime
from .dev_sources''')
replace_once(path,
'''    "calibrate_decoder",
    "rank_participant_frequency_pairs",''',
'''    "calibrate_decoder",
    "normalize_calibrated_scores",
    "rank_participant_frequency_pairs",''')
replace_once(path,
'''    "UdpGameMarkerSource",
    "DecisionSimulationConfig",''',
'''    "UdpGameMarkerSource",
    "ResonanceCheckpoint",
    "DEFAULT_RESONANCE_CHECKPOINTS",
    "ResonanceEpochBuffer",
    "ResonanceEpochRuntime",
    "DecisionSimulationConfig",''')

# LSL queue flush at coded onset is the hard boundary that prevents pre-stimulus backlog
# from contaminating a newly armed epoch.
path = "neuro/mindforge_neuro/acquisition.py"
replace_once(path,
'''    def close(self) -> None:
        if self._inlet is not None:''',
'''    def flush(self) -> int:
        """Discard queued LSL samples before a new Unity stimulus epoch begins."""
        if self._inlet is None:
            raise RuntimeError("source is not connected")
        flush = getattr(self._inlet, "flush", None)
        return int(flush()) if callable(flush) else 0

    def close(self) -> None:
        if self._inlet is not None:''')

# ---------------------------------------------------------------------------
# Production Unity-calibrated runner: markers now gate post-onset cumulative EEG.
# ---------------------------------------------------------------------------
path = "tools/run_unity_calibrated_decoder.py"
replace_once(path,
'''from mindforge_neuro.acquisition import SlidingWindowBuffer, UnicornLslSource
from mindforge_neuro.calibration import calibrate_decoder
from mindforge_neuro.events import EventType, NeuralEvent
from mindforge_neuro.markers import GameMarker
from mindforge_neuro.runtime import AuraSelectionRuntime, UdpEventSink
''',
'''from mindforge_neuro.acquisition import UnicornLslSource
from mindforge_neuro.calibration import calibrate_decoder
from mindforge_neuro.events import EventType, NeuralEvent
from mindforge_neuro.markers import GameMarker
from mindforge_neuro.resonance import ResonanceEpochRuntime
from mindforge_neuro.runtime import UdpEventSink
''')
replace_once(path,
'''    parser.add_argument("--hop-seconds", type=float, default=0.25)
    parser.add_argument("--calibration-hop-seconds", type=float, default=0.50)
''',
'''    parser.add_argument("--calibration-hop-seconds", type=float, default=0.50)
''')
old_runtime = '''        runtime = AuraSelectionRuntime(
            decoder,
            profile,
            source_mode=args.source_mode,
            initial_seq=seq,
            session_id=active_game_session,
            calibration_id=active_calibration,
        )
        buffer = SlidingWindowBuffer(8, cfg.window_samples,
                                     max(1, int(round(args.hop_seconds * cfg.sample_rate_hz))))
        print("Streaming calibrated derived events to Unity. Ctrl-C to stop.")
        if phantom_enabled:
            print("For simulated combat, drive attention/faults with tools/phantom_control.py.")
        while True:
            chunk = source.pull_chunk(max_samples=128, timeout_s=0.35)
            if chunk is None:
                continue
            for window, _timestamps in buffer.push(chunk.samples_uv, chunk.timestamps_s):
                event = runtime.process(window)
                sink.send(event)
                print(
                    f"{event.event.value:13s} target={(event.target.value if event.target else '-'):5s} "
                    f"S={event.sight_score:.3f} G={event.guard_score:.3f} "
                    f"margin={event.margin:.3f} q={event.quality:.2f} reason={event.reason or '-'}")
'''
new_runtime = '''        runtime = ResonanceEpochRuntime(
            decoder,
            profile,
            source_mode=args.source_mode,
            initial_seq=seq,
            session_id=active_game_session,
            calibration_id=active_calibration,
        )
        print("Calibrated. Waiting for Unity NEURAL_WINDOW_LISTENING epochs. Ctrl-C to stop.")
        if phantom_enabled:
            print("For simulated combat, drive attention/faults with tools/phantom_control.py.")

        terminal_markers = {"NEURAL_WINDOW_ENDED", "NEURAL_WINDOW_ABSTAINED", "NEURAL_WINDOW_RESOLVED"}
        while True:
            # Markers are polled before EEG. When LISTENING arrives, flush any queued LSL
            # backlog and reset the cumulative buffer so every future sample is post-onset.
            while True:
                try:
                    raw, _ = markers.recvfrom(65535)
                except BlockingIOError:
                    break
                try:
                    marker = GameMarker.from_json(raw)
                except Exception:
                    continue
                if marker.category != "neural_window" or marker.stimulus_epoch < 0:
                    continue
                if marker.event == "NEURAL_WINDOW_LISTENING":
                    source.flush()
                    runtime.begin_epoch(marker.stimulus_epoch, session_id=marker.session_id or active_game_session)
                    print(f"Epoch {marker.stimulus_epoch}: coded onset; EEG queue flushed")
                elif marker.event in terminal_markers:
                    runtime.cancel_epoch(marker.stimulus_epoch)

            chunk = source.pull_chunk(max_samples=32, timeout_s=0.02)
            if chunk is None or not runtime.active:
                continue
            event = runtime.push(chunk.samples_uv, chunk.timestamps_s)
            if event is None:
                continue
            sink.send(event)
            print(
                f"epoch={event.stimulus_epoch} {event.event.value:13s} "
                f"target={(event.target.value if event.target else '-'):5s} "
                f"evidence={event.evidence_ms:4d}ms S={event.sight_score:.3f} G={event.guard_score:.3f} "
                f"margin={event.margin:.3f} q={event.quality:.2f} reason={event.reason or '-'}")
'''
replace_once(path, old_runtime, new_runtime)
replace_once(path,
'''                        "min_margin": profile.min_margin,
                        **baseline,''',
'''                        "min_margin": profile.min_margin,
                        "sight_off_center": profile.sight_off_center,
                        "sight_off_scale": profile.sight_off_scale,
                        "guard_off_center": profile.guard_off_center,
                        "guard_off_scale": profile.guard_off_scale,
                        "normalization_ready": profile.normalization_ready,
                        **baseline,''')

# ---------------------------------------------------------------------------
# Unity event contract and fail-closed epoch matching.
# ---------------------------------------------------------------------------
path = "unity/Assets/Mindforge/NeuralBridge/NeuralEvent.cs"
replace_once(path,
'''        public int authority_ttl_ms;

        // Optional V0.8 derived calibration metadata.''',
'''        public int authority_ttl_ms;

        // V0.14 causal provenance. Only events from the currently listening Unity epoch
        // may gain Wisp authority, and evidence_ms excludes pre-stimulus EEG.
        public long stimulus_epoch = -1;
        public int evidence_ms;

        // Optional V0.8 derived calibration metadata.''')

path = "unity/Assets/Mindforge/SoulWisp/WispResonanceWindow.cs"
replace_once(path,
'''        [SerializeField] private float settleSeconds = 0.18f;
        [Tooltip("Maximum coded decision duration. Dynamic stopping may resolve earlier.")]
        [SerializeField] private float listeningSeconds = 1.25f;''',
'''        [SerializeField] private float settleSeconds = 0.09f;
        [Tooltip("Maximum coded decision duration. Dynamic stopping may resolve earlier.")]
        [SerializeField] private float listeningSeconds = 1.50f;''')
replace_once(path,
'''        [SerializeField] private bool requireCombatTarget = true;
        [SerializeField] private bool requireHoldThroughDecision = true;
''',
'''        [SerializeField] private bool requireCombatTarget = true;
        [SerializeField] private bool requireHoldThroughDecision = true;
        [Tooltip("Selections with less post-onset EEG than this never gain gameplay authority.")]
        [SerializeField] private int minimumEvidenceMs = 450;
''')
replace_once(path,
'''            if (evt.Target == AuraTarget.None || evt.artifact) return false;
            if (evt.seq <= _minimumSelectionSeq) return false;
            return true;''',
'''            if (evt.Target == AuraTarget.None || evt.artifact || !evt.IsV2) return false;
            if (evt.seq <= _minimumSelectionSeq) return false;
            if (evt.stimulus_epoch != _windowId) return false;
            if (evt.evidence_ms < Mathf.Max(0, minimumEvidenceMs)) return false;
            return true;''')
replace_once(path,
'''            if (State != WispResonanceState.Listening || evt == null || !evt.IsAbstain) return;
            if (evt.seq <= _minimumSelectionSeq) return;
            Abstain''',
'''            if (State != WispResonanceState.Listening || evt == null || !evt.IsAbstain) return;
            if (evt.seq <= _minimumSelectionSeq || !evt.IsV2) return;
            if (evt.stimulus_epoch != _windowId) return;
            Abstain''')
replace_once(path,
'''                authority_ttl_ms = 250,
            };''',
'''                authority_ttl_ms = 250,
                stimulus_epoch = _windowId,
                evidence_ms = 750,
            };''')

# ---------------------------------------------------------------------------
# Frame-indexed stimulus phase: frame sequence, not LateUpdate wall-clock jitter.
# ---------------------------------------------------------------------------
write("unity/Assets/Mindforge/SoulWisp/VepAuraStimulus.cs", '''using UnityEngine;

namespace Mindforge.SoulWisp
{
    /// <summary>
    /// Frame-indexed visual SSVEP stimulus. The coded core is active only inside an explicitly
    /// opened resonance window. The luminance sequence is derived from presented frame index at
    /// the qualified refresh rate, so renderer phase, photodiode phase and experiment logs share
    /// one deterministic sequence. Physical photon timing still requires final-display measurement.
    /// </summary>
    public sealed class VepAuraStimulus : MonoBehaviour
    {
        [SerializeField] private Renderer targetRenderer;
        [SerializeField] private Light targetLight;
        [SerializeField] private float frequencyHz = 10f;
        [SerializeField] private float qualifiedRefreshHz = 120f;
        [SerializeField, Range(0f, 1f)] private float minLuminance = 0.30f;
        [SerializeField, Range(0f, 1f)] private float maxLuminance = 1.00f;
        [SerializeField, Range(0f, 1f)] private float restLuminance = 0.38f;
        [SerializeField] private Color baseColor = Color.cyan;

        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
        private MaterialPropertyBlock _block;
        private double _sessionStart;
        private double _restUntil;
        private int _sessionStartFrame;
        private bool _codedActive;

        public float FrequencyHz => frequencyHz;
        public float QualifiedRefreshHz => qualifiedRefreshHz;
        public int SessionStartFrame => _sessionStartFrame;
        public bool CodedActive => _codedActive && !IsResting;
        public bool IsResting => Time.realtimeSinceStartupAsDouble < _restUntil;
        public float RestRemaining => Mathf.Max(0f, (float)(_restUntil - Time.realtimeSinceStartupAsDouble));
        private double FrameTimeSeconds => Mathf.Max(0, Time.frameCount - _sessionStartFrame) / (double)Mathf.Max(1f, qualifiedRefreshHz);
        public bool IsHighPhase => CodedActive && System.Math.Sin(2.0 * System.Math.PI * frequencyHz * FrameTimeSeconds) >= 0.0;
        public float CurrentLuminance => EvaluateLuminance(Time.realtimeSinceStartupAsDouble, Time.frameCount);

        private void Awake()
        {
            _block = new MaterialPropertyBlock();
            _sessionStart = Time.realtimeSinceStartupAsDouble;
            _sessionStartFrame = Time.frameCount;
            _codedActive = false;
        }

        public void Configure(float frequency, Color color)
        {
            frequencyHz = frequency;
            baseColor = color;
        }

        public void BeginWindow(double sharedStart) => BeginWindow(sharedStart, Time.frameCount);

        /// <summary>Starts a coded window from one shared time+frame phase epoch.</summary>
        public void BeginWindow(double sharedStart, int sharedFrame)
        {
            if (IsResting)
            {
                _codedActive = false;
                return;
            }
            _sessionStart = sharedStart;
            _sessionStartFrame = sharedFrame;
            _codedActive = true;
        }

        public void EndWindow() => _codedActive = false;

        public void RestFor(float realSeconds)
        {
            if (realSeconds <= 0f) return;
            _codedActive = false;
            _restUntil = System.Math.Max(_restUntil, Time.realtimeSinceStartupAsDouble + realSeconds);
        }

        private float EvaluateLuminance(double now, int frame)
        {
            if (!_codedActive || now < _restUntil) return restLuminance;
            double t = Mathf.Max(0, frame - _sessionStartFrame) / (double)Mathf.Max(1f, qualifiedRefreshHz);
            float sine01 = 0.5f + 0.5f * Mathf.Sin((float)(2.0 * System.Math.PI * frequencyHz * t));
            return Mathf.Lerp(minLuminance, maxLuminance, sine01);
        }

        private void LateUpdate()
        {
            float luminance = EvaluateLuminance(Time.realtimeSinceStartupAsDouble, Time.frameCount);
            if (targetRenderer != null)
            {
                targetRenderer.GetPropertyBlock(_block);
                _block.SetColor(EmissionColor, baseColor * Mathf.LinearToGammaSpace(luminance));
                targetRenderer.SetPropertyBlock(_block);
            }
            if (targetLight != null)
            {
                targetLight.color = baseColor;
                targetLight.intensity = luminance;
            }
        }
    }
}
''')

path = "unity/Assets/Mindforge/SoulWisp/SoulWispController.cs"
replace_once(path,
'''            double sharedStart = Time.realtimeSinceStartupAsDouble;
            sightStimulus?.BeginWindow(sharedStart);
            guardStimulus?.BeginWindow(sharedStart);''',
'''            double sharedStart = Time.realtimeSinceStartupAsDouble;
            int sharedFrame = Time.frameCount;
            sightStimulus?.BeginWindow(sharedStart, sharedFrame);
            guardStimulus?.BeginWindow(sharedStart, sharedFrame);''')

# ---------------------------------------------------------------------------
# Regression tests: causal epoch, dynamic latency, calibration normalization, Unity contract.
# ---------------------------------------------------------------------------
write("tests/test_v014_resonance.py", '''from __future__ import annotations

import numpy as np

from mindforge_neuro import AuraTarget, SsvepConfig, SsvepDecoder
from mindforge_neuro.calibration import calibrate_decoder, normalize_calibrated_scores
from mindforge_neuro.resonance import ResonanceEpochRuntime


def synthetic(freq: float, cfg: SsvepConfig, seconds: float, seed: int, amplitude: float = 16.0) -> np.ndarray:
    rng = np.random.default_rng(seed)
    n = int(round(seconds * cfg.sample_rate_hz))
    t = np.arange(n) / cfg.sample_rate_hz
    channels = []
    for i in range(8):
        phase = rng.uniform(0, 2 * np.pi)
        sig = amplitude * np.sin(2 * np.pi * freq * t + phase)
        sig += 0.35 * amplitude * np.sin(2 * np.pi * 2 * freq * t + 0.5 * phase)
        sig += rng.normal(0.0, 3.0, n)
        channels.append(sig)
    return np.stack(channels)


def profile(cfg: SsvepConfig, decoder: SsvepDecoder):
    trials = []
    for i in range(10):
        trials.append((AuraTarget.SIGHT, synthetic(cfg.blue_frequency_hz, cfg, cfg.window_seconds, 100 + i)))
        trials.append((AuraTarget.GUARD, synthetic(cfg.green_frequency_hz, cfg, cfg.window_seconds, 200 + i)))
    return calibrate_decoder(decoder, trials, model_id="v014-test")


def test_variable_fbcca_can_score_short_cumulative_windows():
    cfg = SsvepConfig(window_seconds=1.25)
    decoder = SsvepDecoder(cfg)
    sight = decoder.decide_window(synthetic(cfg.blue_frequency_hz, cfg, 0.75, 1), min_score=0.05, min_margin=0.01)
    guard = decoder.decide_window(synthetic(cfg.green_frequency_hz, cfg, 0.75, 2), min_score=0.05, min_margin=0.01)
    assert sight.target == AuraTarget.SIGHT
    assert guard.target == AuraTarget.GUARD


def test_calibration_learns_target_specific_unattended_leakage():
    cfg = SsvepConfig(window_seconds=1.25)
    decoder = SsvepDecoder(cfg)
    p = profile(cfg, decoder)
    assert p.normalization_ready
    assert p.sight_off_scale >= 0.02
    assert p.guard_off_scale >= 0.02
    z = normalize_calibrated_scores(p, {AuraTarget.SIGHT: p.sight_off_center, AuraTarget.GUARD: p.guard_off_center})
    assert abs(z[AuraTarget.SIGHT]) < 1e-9
    assert abs(z[AuraTarget.GUARD]) < 1e-9


def test_resonance_runtime_emits_only_for_current_epoch_and_reports_post_onset_duration():
    cfg = SsvepConfig(window_seconds=1.25)
    decoder = SsvepDecoder(cfg)
    p = profile(cfg, decoder)
    runtime = ResonanceEpochRuntime(decoder, p, source_mode="synthetic_eeg", initial_seq=40)
    runtime.begin_epoch(17, session_id="game")

    eeg = synthetic(cfg.blue_frequency_hz, cfg, 1.45, 901, amplitude=24.0)
    ts = np.arange(eeg.shape[1]) / cfg.sample_rate_hz
    event = None
    for start in range(0, eeg.shape[1], 25):
        candidate = runtime.push(eeg[:, start:start + 25], ts[start:start + 25])
        if candidate is not None:
            event = candidate
            break
    assert event is not None
    assert event.event.value == "AURA_SELECTED"
    assert event.target == AuraTarget.SIGHT
    assert event.stimulus_epoch == 17
    assert 550 <= event.evidence_ms <= 1450
    assert not runtime.active


def test_resonance_runtime_fails_closed_at_final_checkpoint():
    cfg = SsvepConfig(window_seconds=1.25)
    decoder = SsvepDecoder(cfg)
    p = profile(cfg, decoder)
    runtime = ResonanceEpochRuntime(decoder, p, source_mode="synthetic_eeg")
    runtime.begin_epoch(99)
    n = int(round(1.45 * cfg.sample_rate_hz))
    flat = np.zeros((8, n), dtype=float)
    ts = np.arange(n) / cfg.sample_rate_hz
    event = runtime.push(flat, ts)
    assert event is not None
    assert event.event.value == "ABSTAIN"
    assert event.target is None
    assert event.stimulus_epoch == 99
    assert event.evidence_ms == 1450
''')

write("tests/test_v014_unity_epoch_contract.py", '''from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
WISP = ROOT / "unity/Assets/Mindforge/SoulWisp/WispResonanceWindow.cs"
STIM = ROOT / "unity/Assets/Mindforge/SoulWisp/VepAuraStimulus.cs"
SOUL = ROOT / "unity/Assets/Mindforge/SoulWisp/SoulWispController.cs"
EVENT = ROOT / "unity/Assets/Mindforge/NeuralBridge/NeuralEvent.cs"
RUNNER = ROOT / "tools/run_unity_calibrated_decoder.py"
SCHEMA = ROOT / "contracts/neural_event.v2.schema.json"


def test_unity_requires_current_epoch_and_post_onset_evidence():
    wisp = WISP.read_text(encoding="utf-8")
    event = EVENT.read_text(encoding="utf-8")
    assert "public long stimulus_epoch = -1;" in event
    assert "public int evidence_ms;" in event
    assert "evt.stimulus_epoch != _windowId" in wisp
    assert "evt.evidence_ms < Mathf.Max(0, minimumEvidenceMs)" in wisp
    assert "private float settleSeconds = 0.09f" in wisp
    assert "private float listeningSeconds = 1.50f" in wisp


def test_stimulus_phase_is_frame_indexed_and_both_targets_share_start_frame():
    stim = STIM.read_text(encoding="utf-8")
    soul = SOUL.read_text(encoding="utf-8")
    assert "private float qualifiedRefreshHz = 120f" in stim
    assert "Time.frameCount - _sessionStartFrame" in stim
    assert "BeginWindow(double sharedStart, int sharedFrame)" in stim
    assert "int sharedFrame = Time.frameCount;" in soul
    assert "BeginWindow(sharedStart, sharedFrame)" in soul
    assert "Time.realtimeSinceStartupAsDouble - _sessionStart" not in stim


def test_production_runner_flushes_pre_epoch_lsl_and_uses_dynamic_runtime():
    runner = RUNNER.read_text(encoding="utf-8")
    assert "source.flush()" in runner
    assert "ResonanceEpochRuntime" in runner
    assert "NEURAL_WINDOW_LISTENING" in runner
    assert "SlidingWindowBuffer" not in runner
    assert "AuraSelectionRuntime" not in runner


def test_v2_schema_allows_epoch_provenance():
    schema = SCHEMA.read_text(encoding="utf-8")
    assert '"stimulus_epoch"' in schema
    assert '"evidence_ms"' in schema
''')

print("V0.14 low-latency patch applied")
