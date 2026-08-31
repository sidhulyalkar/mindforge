from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
AUDIT = ROOT / "unity" / "Assets" / "Mindforge" / "Editor" / "MindforgeDemoV11Audit.cs"


def source() -> str:
    return AUDIT.read_text(encoding="utf-8")


def test_v11_audit_is_explicitly_diagnostic_not_promotion_evidence():
    text = source()
    assert 'schema = "mindforge.demo_v11_audit.v1"' in text
    assert "canonical_promotion_evidence = false" in text
    assert "Diagnostic only" in text
    assert "v11-demo-audit-latest.json" in text


def test_v11_audit_checks_visible_traversal_collision_owners():
    text = source()
    for token in (
        "SanctumFloor",
        "CausewayRoad",
        "MarketFloor",
        "AscentRamp",
        "FractureFloor",
    ):
        assert token in text
    assert "renderer.enabled" in text
    assert "collider.enabled" in text


def test_v11_audit_checks_single_runtime_owners_and_legacy_suppression():
    text = source()
    assert '"runtime_experience_director"' in text
    assert '"runtime_presentation_firewall"' in text
    assert '"runtime_encounter_gate"' in text
    assert '"single_v11_hud"' in text
    assert '"legacy_presentation_suppressed"' in text
    for legacy in (
        "GroundedCombatHud",
        "PlayerAgencyGuide",
        "GuardianEquipmentMenu",
        "ShowcaseRuntimeInstaller",
        "AetherbladeVisualPolishV2",
    ):
        assert legacy in text


def test_v11_audit_checks_world_safety_against_new_route_envelope():
    text = source()
    assert "safety.XBounds.x <= -14.5f" in text
    assert "safety.XBounds.y >= 14.5f" in text
    assert "safety.ZBounds.x <= -25.5f" in text
    assert "safety.ZBounds.y >= 108.5f" in text


def test_v11_audit_defers_runtime_only_checks_outside_play_mode():
    text = source()
    assert "if (!EditorApplication.isPlaying)" in text
    assert "deferred until Play Mode" in text
