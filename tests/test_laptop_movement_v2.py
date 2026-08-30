from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
UNITY = ROOT / "unity" / "Assets" / "Mindforge"


def read(*parts: str) -> str:
    return UNITY.joinpath(*parts).read_text(encoding="utf-8")


def test_wasd_is_canonical_direct_input_and_camera_owns_mouse_arrow_orbit():
    combat = read("Combat", "GuardianCombatInput.cs")
    controls = read("Combat", "GuardianControlProfileV1.cs")
    camera = read("Presentation", "ShowcaseCameraRig.cs")

    assert "controls.SampleMovement()" in combat
    assert "Input.GetAxisRaw" not in combat
    assert "SampleArrowAim" not in combat
    for key in ("KeyCode.W", "KeyCode.A", "KeyCode.S", "KeyCode.D"):
        assert key in controls

    assert 'Input.GetAxis("Mouse X")' in camera
    assert 'Input.GetAxis("Mouse Y")' in camera
    for key in ("KeyCode.UpArrow", "KeyCode.DownArrow", "KeyCode.LeftArrow", "KeyCode.RightArrow"):
        assert key in camera
    assert "arrowYawSpeed" in camera
    assert "arrowPitchSpeed" in camera
    assert "Canonical player vocabulary is supplied by GuardianControlProfileV1" in combat
    assert "KeyCode.LeftArrow" not in read("Combat", "GuardianTargetLock.cs")
    assert "KeyCode.RightArrow" not in read("Combat", "GuardianTargetLock.cs")


def test_movement_uses_target_velocity_response_and_third_person_facing():
    motor = read("Combat", "GuardianMotor.cs")

    assert "Vector3.MoveTowards" in motor
    assert "minimumAcceleration = 82f" in motor
    assert "deceleration = 94f" in motor
    assert "reversalAcceleration = 122f" in motor
    assert "forwardSpeedMultiplier = 1.55f" in motor
    assert "strafeSpeedMultiplier = 1.22f" in motor
    assert "backwardSpeedMultiplier = 1.05f" in motor
    assert "DirectionalSpeedMultiplier(_moveInput)" in motor
    assert "_body.AddForce" not in motor
    assert "_body.WakeUp()" in motor
    assert "Vector3.ClampMagnitude(right * _moveInput.x + forward * _moveInput.y, 1f)" in motor

    assert "GuardianTargetLock targetLock" in motor
    assert "freeTurnSharpness" in motor
    assert "lockedTurnSharpness" in motor
    assert "airTurnSharpness" in motor
    assert "UpdateFacing(desiredDir)" in motor
    assert "targetLock.DirectionFrom(transform.position)" in motor
    assert "_body.MoveRotation" in motor
    assert "RigidbodyConstraints.FreezePositionY" in motor
    assert "_body.constraints &= ~(RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezePositionY)" in motor


def test_ground_rolls_remain_chainable_while_air_dash_is_one_per_airtime():
    motor = read("Combat", "GuardianMotor.cs")

    assert "tuning.dashCooldown" not in motor
    assert "dashInputBufferSeconds" in motor
    assert "_dashQueued = true" in motor
    assert "_dashQueuedUntil" in motor
    assert "StartDash(ResolveDashDirection(_queuedDashFallback), queuedAirDash)" in motor
    assert "if (!_grounded && _airDashConsumed) return false" in motor
    assert "_airDashConsumed = false" in motor
    assert "FaceDirectionImmediate(_dashDirection)" in motor
    assert "DashStarted?.Invoke()" in motor
    assert "AirDashStarted?.Invoke()" in motor


def test_showcase_focuses_game_view_when_play_mode_starts_and_teaches_v05_vocabulary():
    editor = read("Editor", "ShowcaseEditorMenu.cs")

    assert "EditorApplication.playModeStateChanged += FocusGameViewWhenPlayStarts" in editor
    assert "PlayModeStateChange.EnteredPlayMode" in editor
    assert 'GetType("UnityEditor.GameView")' in editor
    assert "gameView?.Focus()" in editor
    assert "Space jumps twice and holds hover" in editor
    assert "Shift/RMB evades on foot and boosts while mounted" in editor
    assert "T locks and mouse wheel cycles targets" in editor
    assert "E is the single contextual world action" in editor
    assert "Tab opens kit + controls + objective" in editor


def test_laptop_control_copy_is_rendered_from_one_canonical_profile():
    guide = read("Presentation", "PlayerAgencyGuide.cs")
    menu = read("Presentation", "GuardianEquipmentMenu.cs")
    controls = read("Combat", "GuardianControlProfileV1.cs")

    for token in (
        "interact = KeyCode.E",
        "targetLock = KeyCode.T",
        "jumpHover = KeyCode.Space",
        "evadeBoostPrimary = KeyCode.LeftShift",
        "rightMouseEvades = true",
        "blade = KeyCode.F",
        "menu = KeyCode.Tab",
    ):
        assert token in controls

    assert "WASD MOVE" in guide
    assert "MOUSE / ARROWS CAMERA" in guide
    assert "GuardianControlAction.JumpHover" in guide
    assert "GuardianControlAction.EvadeBoost" in guide
    assert "GuardianControlAction.Interact" in guide
    assert "MOUSE WHEEL CYCLES LOCKED TARGETS" in guide

    assert '"WASD", "Move relative to camera"' in menu
    assert '"MOUSE / ARROWS", "Orbit camera"' in menu
    assert "GuardianControlAction.JumpHover" in menu
    assert '"Jump ×2 · hold descending to hover"' in menu
    assert "GuardianControlAction.EvadeBoost" in menu
    assert '"Evade · air dash · mounted boost"' in menu
    assert "GuardianControlAction.Interact" in menu
    assert '"Context: ride · dismount · reconstruct · use world"' in menu
    assert "GuardianControlAction.TargetLock" in menu
    assert '"Lock / unlock enemy · wheel cycles target"' in menu
    assert "Compatibility roll aliases" not in menu
