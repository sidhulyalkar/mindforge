from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
BRIDGE = ROOT / "dragonsouls_overlay/Assets/Mindforge/Runtime/MindforgeNeuralIntentBridgeV33.cs"
HARNESS = ROOT / "dragonsouls_overlay/Assets/Mindforge/Runtime/MindforgeBciQualificationHarnessV33.cs"


def test_controller_b0_uses_cross_stack_simulated_decision_provenance():
    bridge = BRIDGE.read_text(encoding="utf-8")
    harness = HARNESS.read_text(encoding="utf-8")

    assert 'ApplySemanticIntent(intent, Mathf.Clamp01(confidence), "simulated_decision"' in bridge
    assert 'source_mode = "simulated_decision"' in harness
    assert '"controller_simulation"' not in bridge
