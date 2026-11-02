from __future__ import annotations

from dataclasses import replace

from mindforge_neuro.config import AuraTarget
from mindforge_neuro.dev_sources import DecisionSimulationConfig, DecisionSimulator
from mindforge_neuro.events import EventType, NeuralEvent
from mindforge_neuro.mod_bridge import MOD_FRAME_SCHEMA, ModBridgeState


def simulator():
    return DecisionSimulator(DecisionSimulationConfig(seed=123), session_id="session-a")


def test_cosmetic_game_frame_is_derived_only_and_game_specific():
    state = ModBridgeState("silksong")
    assert state.accept(simulator().next("sight"), 1.0)
    frame = state.snapshot(1.1)
    assert frame["schema"] == MOD_FRAME_SCHEMA
    assert frame["game_id"] == "silksong"
    assert frame["sight_active"] is True and frame["guard_active"] is False
    assert frame["presentation_only"] is True
    assert not {"raw_eeg", "samples", "stimulus_hz", "command", "damage"} & frame.keys()


def test_preview_duration_and_no_data_timeout_are_independent():
    state = ModBridgeState("cuphead")
    state.accept(simulator().next("guard"), 1.0)
    assert state.snapshot(1.5)["guard_active"] is True
    assert state.snapshot(2.26)["link"] == "offline"
    assert state.snapshot(2.26)["guard_active"] is False


def test_lost_neutralizes_and_requires_recovery():
    state = ModBridgeState("silksong")
    src = simulator()
    state.accept(src.next("sight"), 1.0)
    state.accept(src.next("lost"), 1.1)
    assert state.snapshot(1.2)["link"] == "lost"
    assert not state.snapshot(1.2)["sight_active"]
    state.accept(src.next("guard"), 1.3)
    assert not state.snapshot(1.3)["guard_active"]
    state.accept(src.next("recovered"), 1.4)
    state.accept(src.next("guard"), 1.5)
    assert state.snapshot(1.6)["guard_active"]


def test_explicit_stop_requires_new_session():
    state = ModBridgeState("silksong")
    src = simulator()
    state.accept(src.next("stop"), 1.0)
    assert state.snapshot(1.1)["link"] == "stopped"
    assert not state.accept(src.next("sight"), 1.2)
    assert not state.snapshot(1.3)["sight_active"]
    other = DecisionSimulator(DecisionSimulationConfig(), session_id="session-b")
    assert state.accept(other.next("guard"), 1.4)
    assert state.snapshot(1.4)["guard_active"]


def test_sequence_replay_and_cross_session_interleaving_rejected():
    state = ModBridgeState("cuphead")
    a = simulator().next("sight")
    b = DecisionSimulator(DecisionSimulationConfig(), session_id="session-b").next("guard")
    assert state.accept(a, 0.0)
    assert not state.accept(a, 0.1)
    assert not state.accept(b, 0.2)
    assert state.snapshot(0.3)["session_id"] == "session-a"
    assert state.accept(b, 2.0)
    assert state.snapshot(2.0)["session_id"] == "session-b"


def test_unsafe_selection_is_cosmetically_neutral():
    src = simulator()
    baseline = src.next("sight")
    cases = [
        replace(baseline, artifact=True),
        replace(baseline, quality=0.10),
        replace(baseline, confidence=0.10),
        replace(baseline, authority_ttl_ms=0),
        replace(baseline, target=None),
        replace(baseline, schema="mindforge.neural_event.v1"),
        replace(baseline, session_id=None),
    ]
    for event in cases:
        state = ModBridgeState("cuphead")
        state.accept(event, 1.0)
        assert not state.snapshot(1.1)["sight_active"]


def test_live_selection_requires_evidence_and_calibration_identity():
    src = simulator()
    event = replace(src.next("sight"), source_mode="live", has_evidence=False)
    state = ModBridgeState("silksong")
    state.accept(event, 1.0)
    assert not state.snapshot(1.0)["sight_active"]
    ready = replace(event, seq=event.seq + 1, has_evidence=True, calibration_id="cal-1")
    state.accept(ready, 1.1)
    assert state.snapshot(1.1)["sight_active"]


def test_unsupported_game_rejected():
    import pytest

    with pytest.raises(ValueError, match="unsupported game"):
        ModBridgeState("elden_ring")


def test_mod_frame_sequence_monotonic():
    state = ModBridgeState("cuphead")
    assert state.snapshot(0)["seq"] + 1 == state.snapshot(0)["seq"]
