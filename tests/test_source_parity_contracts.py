from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
UNITY = ROOT / "unity" / "Assets" / "Mindforge"


def read(*parts: str) -> str:
    return UNITY.joinpath(*parts).read_text(encoding="utf-8")


def test_manual_fixture_uses_the_production_neural_transport_not_direct_buff_authority():
    manual = read("SoulWisp", "SimulatedAuraInput.cs")
    assert 'source_mode = "manual"' in manual
    assert "NeuralEvent.SchemaV2" in manual
    assert "UdpClient" in manual
    assert '"127.0.0.1", neuralEventPort' in manual
    assert "buffs?.TryApply" not in manual
    assert "AuraBuffController" not in manual


def test_judge_hud_surfaces_expired_authority_separately_from_other_transport_loss():
    receiver = read("NeuralBridge", "UdpNeuralReceiver.cs")
    hud = read("NeuralBridge", "NeuralEvidenceHud.cs")
    assert "DroppedExpiredAuthority" in receiver
    assert "DroppedExpiredAuthority" in hud
    assert "· ttl " in hud
    assert 'string schema = evt.IsV2 ? "v2" : "v1"' in hud
