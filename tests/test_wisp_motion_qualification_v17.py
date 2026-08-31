from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
WINDOW = ROOT / "unity/Assets/Mindforge/SoulWisp/WispResonanceWindow.cs"
HUD = ROOT / "unity/Assets/Mindforge/SoulWisp/WispResonanceHud.cs"


def test_wisp_arms_only_from_a_low_motion_grounded_opening_without_stealing_movement():
    text = WINDOW.read_text(encoding="utf-8")
    assert "GuardianMotor guardianMotor" in text
    assert "requireLowMotionToArm = true" in text
    assert "requireGroundedToArm = true" in text
    assert "maximumArmPlanarSpeed = 0.90f" in text
    assert "maximumArmVerticalSpeed = 0.55f" in text
    assert "MotionQualifiedForArm" in text
    assert "MotionQualifiedForArm;" in text
    assert "guardianMotor.IsGrounded" in text
    assert "guardianMotor.IsDashing" in text
    assert "guardianMotor.IsHovering" in text
    assert "guardianMotor.Velocity" in text
    for forbidden in (
        "SetMoveInput(Vector2.zero",
        "SetMoveInput(Vector2.zero)",
        "RigidbodyConstraints.FreezePosition",
        "guardianMotor.enabled = false",
    ):
        assert forbidden not in text


def test_motion_contamination_abstains_and_cannot_authorize_a_late_selection():
    text = WINDOW.read_text(encoding="utf-8")
    assert "abortOnMotionDuringEvidence = true" in text
    assert "maximumEvidencePlanarSpeed = 1.40f" in text
    assert "maximumEvidenceVerticalSpeed = 0.85f" in text
    assert 'Abstain(MotionReason(arming: false));' in text
    assert "if (abortOnMotionDuringEvidence && !MotionQualified(arming: false)) return false;" in text
    for reason in (
        '"PLAYER_DASHING"',
        '"PLAYER_HOVERING"',
        '"PLAYER_AIRBORNE"',
        '"PLAYER_VERTICAL_MOTION"',
        '"PLAYER_MOVING"',
    ):
        assert reason in text


def test_player_gets_actionable_stillness_feedback_without_raw_decoder_metrics():
    hud = HUD.read_text(encoding="utf-8")
    assert "window.MotionQualifiedForArm" in hud
    assert "window.MotionBlockReason" in hud
    assert "CREATE SPACE · HOLD STILL TO CHANNEL" in hud
    assert "LAND TO CHANNEL WISP" in hud
    assert "KEEP STILL · LET THE WISP RESOLVE" in hud
    for forbidden in ("confidence", "cca", "fbcca", "raw_score"):
        assert forbidden not in hud.lower()
