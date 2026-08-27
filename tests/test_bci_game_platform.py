from __future__ import annotations

import json
from pathlib import Path

from mindforge_neuro import AuraTarget
from mindforge_neuro.dev_sources import DecisionSimulationConfig, DecisionSimulator, NeuralEventTape, TapeEntry
from mindforge_neuro.events import EventType, NeuralEvent, SourceMode
from mindforge_neuro.markers import GameMarker


ROOT = Path(__file__).resolve().parents[1]


def test_neural_event_v2_round_trip_preserves_provenance_and_authority_contract():
    event = NeuralEvent.create(
        seq=7,
        event=EventType.AURA_SELECTED,
        target=AuraTarget.SIGHT,
        confidence=0.82,
        quality=0.91,
        model_id="cal-7",
        sight_score=0.71,
        guard_score=0.18,
        margin=0.53,
        source_mode=SourceMode.SYNTHETIC_EEG.value,
        session_id="session-a",
        calibration_id="calibration-a",
        source_sample_start=1000,
        source_sample_end=1312,
        authority_ttl_ms=650,
    )
    restored = NeuralEvent.from_json(event.to_json())
    assert restored.schema == "mindforge.neural_event.v2"
    assert restored.target == AuraTarget.SIGHT
    assert restored.session_id == "session-a"
    assert restored.calibration_id == "calibration-a"
    assert restored.source_sample_start == 1000
    assert restored.source_sample_end == 1312
    assert restored.authority_ttl_ms == 650
    assert "raw_eeg" not in restored.to_dict()


def test_v1_neural_event_remains_replayable_without_inventing_ttl():
    legacy = {
        "schema": "mindforge.neural_event.v1",
        "seq": 3,
        "monotonic_ns": 42,
        "event": "ABSTAIN",
        "target": None,
        "confidence": 0.2,
        "quality": 0.8,
        "paradigm": "ssvep_fbcca",
        "model_id": "legacy",
        "artifact": False,
        "reason": "DWELL",
        "has_evidence": True,
        "sight_score": 0.4,
        "guard_score": 0.3,
        "margin": 0.1,
        "source_mode": "simulation",
    }
    event = NeuralEvent.from_dict(legacy)
    assert event.schema == "mindforge.neural_event.v1"
    assert event.authority_ttl_ms == 0
    assert event.source_sample_start == -1
    assert event.source_mode == "simulation"


def test_decision_simulator_is_deterministic_and_truthfully_labelled():
    cfg = DecisionSimulationConfig(seed=123)
    first = DecisionSimulator(cfg, session_id="same")
    second = DecisionSimulator(cfg, session_id="same")
    a = first.next("guard")
    b = second.next("guard")
    assert a.target == b.target == AuraTarget.GUARD
    assert a.confidence == b.confidence
    assert a.quality == b.quality
    assert a.sight_score == b.sight_score
    assert a.guard_score == b.guard_score
    assert a.source_mode == SourceMode.SIMULATED_DECISION.value


def test_decision_tape_round_trip_and_replay_changes_provenance(tmp_path: Path):
    simulator = DecisionSimulator(DecisionSimulationConfig(seed=5), session_id="recorded")
    tape = NeuralEventTape([
        TapeEntry(0.0, simulator.next("sight")),
        TapeEntry(0.25, simulator.next("abstain")),
        TapeEntry(0.50, simulator.next("guard")),
    ])
    path = tmp_path / "session.jsonl"
    tape.save(path)
    loaded = NeuralEventTape.load(path)
    replay = list(loaded.replay_events(initial_seq=100, session_id="replayed"))
    assert [item.offset_s for item in replay] == [0.0, 0.25, 0.5]
    assert [item.event.seq for item in replay] == [101, 102, 103]
    assert all(item.event.source_mode == SourceMode.DECISION_REPLAY.value for item in replay)
    assert all(item.event.session_id == "replayed" for item in replay)


def test_game_marker_parses_new_contract_and_legacy_calibration_adapter():
    modern = GameMarker.from_dict({
        "schema": "mindforge.game_marker.v1",
        "seq": 9,
        "session_id": "s",
        "event": "PHASE_DASH",
        "category": "combat_action",
        "unity_realtime_s": 1.2,
        "game_time_s": 0.8,
        "frame": 60,
        "fixed_tick": 120,
        "stage": None,
        "action": None,
        "target": None,
        "reason": None,
        "value": 0.0,
        "boss_phase": 2,
        "stimulus_epoch": -1,
        "trial_id": None,
        "planned_duration_s": 0.0,
    })
    assert modern.event == "PHASE_DASH"
    assert modern.boss_phase == 2

    legacy = GameMarker.from_dict({
        "schema": "mindforge.calibration_marker.v1",
        "session_id": "old",
        "stage": "sight",
        "action": "begin",
        "unity_realtime_s": 2.5,
        "planned_duration_s": 5.0,
    })
    assert legacy.category == "calibration"
    assert legacy.event == "CALIBRATION_STAGE"
    assert legacy.stage == "sight"
    assert legacy.action == "begin"


def test_contract_files_and_unity_boundary_encode_platform_invariants():
    neural_schema = json.loads((ROOT / "contracts/neural_event.v2.schema.json").read_text())
    marker_schema = json.loads((ROOT / "contracts/game_marker.v1.schema.json").read_text())
    assert neural_schema["properties"]["schema"]["const"] == "mindforge.neural_event.v2"
    assert "authority_ttl_ms" in neural_schema["required"]
    assert marker_schema["properties"]["schema"]["const"] == "mindforge.game_marker.v1"

    receiver = (ROOT / "unity/Assets/Mindforge/NeuralBridge/UdpNeuralReceiver.cs").read_text()
    neural_event = (ROOT / "unity/Assets/Mindforge/NeuralBridge/NeuralEvent.cs").read_text()
    sender = (ROOT / "unity/Assets/Mindforge/Telemetry/UdpGameMarkerSender.cs").read_text()
    bootstrap = (ROOT / "unity/Assets/Mindforge/Telemetry/MindforgePlatformBootstrap.cs").read_text()
    assert "HasSupportedSchema" in receiver
    assert "AuthorityExpired" in receiver
    assert "authority_ttl_ms" in neural_event
    assert "mindforge.game_marker.v1" in sender
    assert "RuntimeInitializeOnLoadMethod" in bootstrap
    combined = (receiver + neural_event + sender).lower()
    assert "raw_eeg" not in combined
