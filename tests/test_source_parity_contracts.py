from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
UNITY = ROOT / "unity" / "Assets" / "Mindforge"


def read(*parts: str) -> str:
    return UNITY.joinpath(*parts).read_text(encoding="utf-8")


def test_manual_fixture_uses_single_python_authority_stream_not_direct_buff_authority():
    manual = read("SoulWisp", "SimulatedAuraInput.cs")
    adapter = (ROOT / "neuro/mindforge_neuro/manual_dev.py").read_text(encoding="utf-8")
    cli = (ROOT / "tools/mindforge_dev.py").read_text(encoding="utf-8")

    assert 'schema = "mindforge.manual_intent.v1"' in manual
    assert "manualIntentPort = 19746" in manual
    assert "UdpClient" in manual
    assert '"127.0.0.1", manualIntentPort' in manual
    assert '"-mindforgeManualBCI"' in manual
    assert "new NeuralEvent" not in manual
    assert "buffs?.TryApply" not in manual
    assert "AuraBuffController" not in manual

    assert 'SourceMode.MANUAL.value' in adapter
    assert "manual_selection_event" in adapter
    assert "manual_idle_event" in adapter
    assert 'event=EventType.BCI_HEARTBEAT' in adapter
    assert 'authority_ttl_ms=0' in adapter
    assert '"manual-service"' in cli
    assert "_await_development_calibration" in cli
    assert "UdpManualIntentSource" in cli
    assert "UdpEventSink" in cli


def test_judge_hud_surfaces_expired_authority_separately_from_other_transport_loss():
    receiver = read("NeuralBridge", "UdpNeuralReceiver.cs")
    hud = read("NeuralBridge", "NeuralEvidenceHud.cs")
    assert "DroppedExpiredAuthority" in receiver
    assert "DroppedExpiredAuthority" in hud
    assert "· ttl " in hud
    assert 'string schema = evt.IsV2 ? "v2" : "v1"' in hud


def test_transport_heartbeat_is_not_presented_as_classifier_evidence():
    event = read("NeuralBridge", "NeuralEvent.cs")
    hud = read("NeuralBridge", "NeuralEvidenceHud.cs")
    schema = (ROOT / "contracts/neural_event.v2.schema.json").read_text(encoding="utf-8")
    assert '"BCI_HEARTBEAT"' in schema
    assert 'IsHeartbeat => string.Equals(@event, "BCI_HEARTBEAT"' in event
    assert "if (evt.IsHeartbeat)" in hud
    heartbeat_branch = hud[hud.index("if (evt.IsHeartbeat)"):hud.index("_targetSight = evt.has_evidence")]
    assert "UpdateMode(evt)" in heartbeat_branch
    assert "return;" in heartbeat_branch
