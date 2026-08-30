from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "unity" / "Assets" / "Mindforge" / "Presentation" / "MindforgeDemoV11GuardianMotion.cs"


def source() -> str:
    return SOURCE.read_text(encoding="utf-8")


def test_v11_guardian_motion_is_demo_marker_scoped():
    text = source()
    assert "FindObjectOfType<MindforgeDemoV11Marker>(true)" in text
    assert "FindObjectOfType<GuardianCombatInput>(true)" in text
    assert "MindforgeDemoV11GuardianMotion" in text


def test_v11_guardian_motion_reads_motor_and_combat_but_writes_visual_children_only():
    text = source()
    assert "_motor.Velocity" in text
    assert "_motor.IsGrounded" in text
    assert "_motor.IsDashing" in text
    assert "_combat.IsAttacking" in text
    for child in ("V11GuardianVisual", "ArmL", "ArmR", "LegL", "LegR", "Mantle"):
        assert child in text
    forbidden = (
        "AddForce(",
        ".velocity =",
        "TakeDamage(",
        "ApplyDamage(",
        "SetExternalPause(",
        "NeuralEvent",
        "AuraBuffController",
        "stamina.",
        "PrimaryTarget =",
    )
    for token in forbidden:
        assert token not in text


def test_v11_guardian_gait_advances_from_actual_speed_and_freezes_at_rest():
    text = source()
    assert "_locomotionPhase += speed * dt * 1.55f" in text
    assert "Mathf.Sin(_locomotionPhase)" in text
    assert "speed01" in text


def test_v11_guardian_motion_has_airborne_dash_and_attack_readability():
    text = source()
    assert "if (!grounded)" in text
    assert "else if (dashing)" in text
    assert "attacking ?" in text
    assert "Quaternion.Slerp" in text
