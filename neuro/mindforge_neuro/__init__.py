"""Mindforge neural runtime.

The package exports derived neural events rather than raw EEG to gameplay. The
initial competition mechanic is a two-target SSVEP decoder: blue/SIGHT and
green/GUARD.
"""

from .config import AuraTarget, SsvepConfig
from .events import EventType, NeuralEvent
from .ssvep import SsvepDecision, SsvepDecoder
from .calibration import CalibrationProfile, calibrate_decoder

__all__ = [
    "AuraTarget",
    "SsvepConfig",
    "EventType",
    "NeuralEvent",
    "SsvepDecision",
    "SsvepDecoder",
    "CalibrationProfile",
    "calibrate_decoder",
]
