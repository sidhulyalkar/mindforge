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
    personalized_ssvep_config,
    rank_participant_frequency_pairs,
)
from .markers import GameMarker, GameMarkerType, UdpGameMarkerSource
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
    "rank_participant_frequency_pairs",
    "personalized_ssvep_config",
    "GameMarker",
    "GameMarkerType",
    "UdpGameMarkerSource",
    "DecisionSimulationConfig",
    "DecisionSimulator",
    "NeuralEventTape",
    "TapeEntry",
]
