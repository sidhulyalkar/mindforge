from __future__ import annotations

from mindforge_neuro.config import AuraTarget
from mindforge_neuro.dev_calibration import DevelopmentCalibrationFixture
from mindforge_neuro.dev_sources import DecisionSimulationConfig, DecisionSimulator, NeuralEventTape, TapeEntry
from mindforge_neuro.events import EventType, NeuralEvent, SourceMode
from mindforge_neuro.manual_dev import ManualIntent, manual_idle_event, manual_selection_event
from mindforge_neuro.markers import GameMarker


def calibration_marker(seq: int, stage: str, action: str, *, session: str = "game-1", calibration: str = "cal-1") -> GameMarker:
    return GameMarker(
        schema="mindforge.game_marker.v1",
        seq=seq,
        session_id=session,
        calibration_id=calibration,
        event="CALIBRATION_STAGE",
        category="calibration",
        unity_realtime_s=float(seq),
        game_time_s=float(seq),
        frame=seq,
        fixed_tick=seq,
        stage=stage,
        action=action,
    )


def test_development_calibration_fixture_requires_complete_order_and_declares_no_eeg():
    fixture = DevelopmentCalibrationFixture(SourceMode.SIMULATED_DECISION.value, heartbeat_seconds=0.5)
    ready = fixture.periodic(now=1.0)
    assert ready is not None
    assert ready.event == EventType.CALIBRATION_SERVICE_READY
    assert ready.source_mode == SourceMode.SIMULATED_DECISION.value
    assert "NO_EEG" in (ready.reason or "")

    response = None
    sequence = (
        ("baseline", "begin"),
        ("baseline", "end"),
        ("sight", "begin"),
        ("sight", "end"),
        ("guard", "begin"),
        ("guard", "end"),
    )
    for index, (stage, action) in enumerate(sequence, start=1):
        response = fixture.consume(calibration_marker(index, stage, action))
        assert response is not None
        assert response.session_id == "game-1"
        assert response.calibration_id == "cal-1"
    assert response.event == EventType.CALIBRATION_READY
    assert fixture.completed is True
    assert response.confidence == 1.0
    assert "NO_EEG" in (response.reason or "")


def test_development_calibration_fixture_fails_closed_on_protocol_reordering():
    fixture = DevelopmentCalibrationFixture(SourceMode.MANUAL.value)
    failed = fixture.consume(calibration_marker(1, "sight", "begin"))
    assert failed is not None
    assert failed.event == EventType.CALIBRATION_FAILED
    assert fixture.completed is False
    assert "expected" in (failed.reason or "")


def test_development_calibration_fixture_rejects_live_provenance():
    try:
        DevelopmentCalibrationFixture(SourceMode.LIVE.value)
    except ValueError as exc:
        assert "development provenance" in str(exc)
    else:
        raise AssertionError("live provenance must never use the development calibration fixture")


def test_decision_simulator_continues_fixture_sequence_and_identity():
    simulator = DecisionSimulator(
        DecisionSimulationConfig(seed=3),
        session_id="game-1",
        calibration_id="cal-1",
        initial_seq=17,
    )
    event = simulator.next("sight")
    assert event.seq == 18
    assert event.session_id == "game-1"
    assert event.calibration_id == "cal-1"
    assert event.source_mode == SourceMode.SIMULATED_DECISION.value


def test_decision_replay_can_bind_to_fresh_game_and_calibration_identity():
    original = NeuralEvent.create(
        seq=1,
        event=EventType.AURA_SELECTED,
        target=AuraTarget.GUARD,
        confidence=0.9,
        quality=0.9,
        model_id="recorded",
        source_mode=SourceMode.SIMULATED_DECISION.value,
        session_id="old-game",
        calibration_id="old-cal",
    )
    tape = NeuralEventTape([TapeEntry(0.0, original)])
    replay = next(tape.replay_events(initial_seq=30, session_id="new-game", calibration_id="new-cal"))
    assert replay.event.seq == 31
    assert replay.event.session_id == "new-game"
    assert replay.event.calibration_id == "new-cal"
    assert replay.event.source_mode == SourceMode.DECISION_REPLAY.value


def test_manual_intent_adapter_is_non_authoritative_until_python_creates_neural_event():
    intent = ManualIntent.from_dict({
        "schema": "mindforge.manual_intent.v1",
        "session_id": "game-1",
        "calibration_id": "cal-1",
        "target": "guard",
        "unity_realtime_s": 12.5,
    })
    selection = manual_selection_event(seq=41, session_id="game-1", calibration_id="cal-1", intent=intent)
    assert selection.event == EventType.AURA_SELECTED
    assert selection.target == AuraTarget.GUARD
    assert selection.source_mode == SourceMode.MANUAL.value
    assert selection.session_id == "game-1"
    assert selection.calibration_id == "cal-1"

    idle = manual_idle_event(seq=42, session_id="game-1", calibration_id="cal-1")
    assert idle.event == EventType.BCI_HEARTBEAT
    assert idle.reason == "MANUAL_DEV_IDLE"
    assert idle.authority_ttl_ms == 0
    assert idle.source_mode == SourceMode.MANUAL.value
    assert idle.has_evidence is False
