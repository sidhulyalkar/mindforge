"""Mindforge neural runtime.

The package exports derived neural events rather than raw EEG to gameplay. The
competition mechanic remains a two-target SSVEP decoder: blue/SIGHT and
green/GUARD. Development sources are explicitly provenance-labelled so manual,
simulated, replayed, synthetic-EEG and live sessions cannot be confused.
"""

from .config import AuraTarget, SsvepConfig
from .events import EventType, NeuralEvent, SourceMode
from .ssvep import SsvepDecision, SsvepDecoder
from .calibration import (
    CalibrationProfile,
    FrequencyPairEvaluation,
    ParticipantFrequencyProfile,
    calibrate_decoder,
    normalize_calibrated_scores,
    personalized_ssvep_config,
    rank_participant_frequency_pairs,
)
from .gaze_confound import (
    EvidenceWindow,
    GameDesignRecommendation,
    PolicyMetrics,
    SelectionPolicy,
    decide as decide_gaze_conditioned_ssvep,
    evaluate_policy,
    gameplay_loss,
    recommend_game_architecture,
    tune_policy,
)
from .public_validation import (
    CrossValidatedCohort,
    HeldOutSubjectResult,
    leave_one_subject_out_validation,
)
from .markers import GameMarker, GameMarkerType, UdpGameMarkerSource
from .ssvep_observations import (
    SSVEP_OBSERVATION_V1,
    SsvepObservation,
    UdpSsvepObservationSource,
)
from .resonance import DEFAULT_RESONANCE_CHECKPOINTS, ResonanceCheckpoint, ResonanceEpochBuffer, ResonanceEpochRuntime
from .dev_sources import DecisionSimulationConfig, DecisionSimulator, NeuralEventTape, TapeEntry

__all__ = [
    "AuraTarget",
    "SsvepConfig",
    "EventType",
    "NeuralEvent",
    "SourceMode",
    "SsvepDecision",
    "SsvepDecoder",
    "CalibrationProfile",
    "FrequencyPairEvaluation",
    "ParticipantFrequencyProfile",
    "calibrate_decoder",
    "normalize_calibrated_scores",
    "rank_participant_frequency_pairs",
    "personalized_ssvep_config",
    "EvidenceWindow",
    "SelectionPolicy",
    "PolicyMetrics",
    "GameDesignRecommendation",
    "decide_gaze_conditioned_ssvep",
    "evaluate_policy",
    "gameplay_loss",
    "tune_policy",
    "recommend_game_architecture",
    "HeldOutSubjectResult",
    "CrossValidatedCohort",
    "leave_one_subject_out_validation",
    "GameMarker",
    "GameMarkerType",
    "UdpGameMarkerSource",
    "SSVEP_OBSERVATION_V1",
    "SsvepObservation",
    "UdpSsvepObservationSource",
    "ResonanceCheckpoint",
    "DEFAULT_RESONANCE_CHECKPOINTS",
    "ResonanceEpochBuffer",
    "ResonanceEpochRuntime",
    "DecisionSimulationConfig",
    "DecisionSimulator",
    "NeuralEventTape",
    "TapeEntry",
]
