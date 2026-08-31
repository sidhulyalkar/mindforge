from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
WORLD = ROOT / "unity" / "Assets" / "Mindforge" / "World"


def read(name: str) -> str:
    return (WORLD / name).read_text(encoding="utf-8")


def test_world_safety_is_explicitly_revision_configurable():
    source = read("GuardianWorldSafety.cs")
    assert "public void ConfigureBounds(Vector2 x, Vector2 z, float minimumRecoveryHeight)" in source
    assert "xBounds = Ordered(x);" in source
    assert "zBounds = Ordered(z);" in source
    assert "CaptureSafePose();" in source


def test_v11_safety_profile_covers_the_complete_new_route():
    source = read("MindforgeDemoV11SafetyProfile.cs")
    assert "new Vector2(-14.5f, 14.5f)" in source
    assert "new Vector2(-25.5f, 108.5f)" in source
    assert "DemoRecoveryHeight = -4.0f" in source
    assert "safety.ConfigureBounds(DemoXBounds, DemoZBounds, DemoRecoveryHeight);" in source


def test_v11_safety_profile_is_marker_scoped_not_global_policy():
    source = read("MindforgeDemoV11SafetyProfile.cs")
    assert "FindObjectOfType<MindforgeDemoV11Marker>(true)" in source
    assert "GuardianWorldSafety" in source
    assert "GuardianCombatController" not in source
    assert "UdpNeuralReceiver" not in source
