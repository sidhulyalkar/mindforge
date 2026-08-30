from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PRESENTATION = ROOT / "unity" / "Assets" / "Mindforge" / "Presentation"


def read(name: str) -> str:
    return (PRESENTATION / name).read_text(encoding="utf-8")


def test_v11_firewall_is_marker_scoped_and_presentation_only():
    source = read("MindforgeDemoV11PresentationFirewall.cs")
    assert "FindObjectOfType<MindforgeDemoV11Marker>(true)" in source
    assert "SuppressedPresentationTypes" in source
    assert "GuardianMotor" not in source
    assert "GuardianCombatController" not in source
    assert "UdpNeuralReceiver" not in source
    assert "DualAuraCombatDirector" not in source
    assert "MindforgeSession" not in source


def test_v11_firewall_suppresses_known_duplicate_visual_layers():
    source = read("MindforgeDemoV11PresentationFirewall.cs")
    for type_name in (
        "GroundedCombatHud",
        "CombatStateHud",
        "PlayerAgencyGuide",
        "GuardianEquipmentMenu",
        "NullWardArtOverrideInstaller",
        "AetherbladeVisualPolishV2",
        "ShowcaseRuntimeInstaller",
        "ShowcasePostProcessing",
        "CinematicArmamentVfxPolish",
        "ProductionGuardianV09",
        "ProductionHudV09",
        "ControllerOnlyQualificationBootstrap",
    ):
        assert f'"{type_name}"' in source


def test_v11_firewall_reasserts_single_ownership_after_late_bootstraps():
    source = read("MindforgeDemoV11PresentationFirewall.cs")
    assert "private void Update()" in source
    assert "_nextSweep = Time.unscaledTime + 0.50f" in source
    assert "Resources.FindObjectsOfTypeAll<MonoBehaviour>()" in source
    assert "behaviour.enabled = false;" in source
    assert 'GameObject.Find("CompetitionHUD")' in source
    assert 'GameObject.Find("MindforgeShowcaseRuntime")' in source
