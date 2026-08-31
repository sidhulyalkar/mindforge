from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
WINDOW = ROOT / "unity/Assets/Mindforge/SoulWisp/WispResonanceWindow.cs"
MOTION = ROOT / "unity/Assets/Mindforge/SoulWisp/WispMotionQualification.cs"
HUD = ROOT / "unity/Assets/Mindforge/SoulWisp/WispResonanceHud.cs"


def test_neural_window_consumes_abstract_motion_quality_not_locomotion_authority():
    window = WINDOW.read_text(encoding="utf-8")
    motion = MOTION.read_text(encoding="utf-8")

    assert "WispMotionQualification motionQualification" in window
    assert "MotionQualifiedForArm" in window
    assert "motionQualification.EvidenceQualified" in window
    assert "TryGetEvidenceInstability" in window
    assert "GuardianMotor" not in window

    assert "GuardianMotor motor" in motion
    assert "requireLowMotionToArm = true" in motion
    assert "requireGroundedToArm = true" in motion
    assert "maximumArmPlanarSpeed = 0.90f" in motion
    assert "maximumArmVerticalSpeed = 0.55f" in motion
    assert "maximumEvidencePlanarSpeed = 1.40f" in motion
    assert "maximumEvidenceVerticalSpeed = 0.85f" in motion


def test_motion_sensor_is_strictly_read_only_and_cannot_steal_player_authority():
    motion = MOTION.read_text(encoding="utf-8")
    for forbidden in (
        "SetMoveInput(",
        "SetJumpHeld(",
        "RequestDash(",
        "RequestJump(",
        "SetCombatActionsEnabled(",
        "RigidbodyConstraints",
        ".velocity =",
        ".position =",
        ".rotation =",
        "motor.enabled = false",
    ):
        assert forbidden not in motion


def test_motion_contamination_abstains_and_cannot_authorize_a_late_selection():
    window = WINDOW.read_text(encoding="utf-8")
    motion = MOTION.read_text(encoding="utf-8")

    assert "motionQualification == null || motionQualification.TryGetEvidenceInstability" in window
    assert 'Abstain(string.IsNullOrEmpty(motionReason) ? "MOTION_STATE_UNAVAILABLE" : motionReason);' in window
    assert "if (motionQualification == null || !motionQualification.EvidenceQualified) return false;" in window

    for reason in (
        '"MOTION_STATE_UNAVAILABLE"',
        '"PLAYER_DASHING"',
        '"PLAYER_HOVERING"',
        '"PLAYER_AIRBORNE"',
        '"PLAYER_VERTICAL_MOTION"',
        '"PLAYER_MOVING"',
    ):
        assert reason in motion


def test_player_gets_actionable_stillness_feedback_without_raw_decoder_metrics():
    hud = HUD.read_text(encoding="utf-8")
    assert "window.MotionQualifiedForArm" in hud
    assert "window.MotionBlockReason" in hud
    assert "CREATE SPACE · HOLD STILL TO CHANNEL" in hud
    assert "LAND TO CHANNEL WISP" in hud
    assert "KEEP STILL · LET THE WISP RESOLVE" in hud
    for forbidden in ("confidence", "cca", "fbcca", "raw_score"):
        assert forbidden not in hud.lower()
