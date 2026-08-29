from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
UNITY = ROOT / "unity" / "Assets" / "Mindforge"


def read(*parts: str) -> str:
    return UNITY.joinpath(*parts).read_text(encoding="utf-8")


def test_wasd_is_direct_input_and_camera_owns_mouse_arrow_orbit():
    combat = read("Combat", "GuardianCombatInput.cs")
    camera = read("Presentation", "ShowcaseCameraRig.cs")

    assert "SampleWasdMovement" in combat
    assert "Input.GetAxisRaw" not in combat
    assert "SampleArrowAim" not in combat
    for key in ("KeyCode.W", "KeyCode.A", "KeyCode.S", "KeyCode.D"):
        assert key in combat

    assert 'Input.GetAxis("Mouse X")' in camera
    assert 'Input.GetAxis("Mouse Y")' in camera
    for key in ("KeyCode.UpArrow", "KeyCode.DownArrow", "KeyCode.LeftArrow", "KeyCode.RightArrow"):
        assert key in camera
    assert "arrowYawSpeed" in camera
    assert "arrowPitchSpeed" in camera
    assert "orbit the third-person camera" in combat


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


def test_ground_dashes_remain_chainable_while_air_dash_is_one_per_airtime():
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


def test_showcase_focuses_game_view_when_play_mode_starts():
    editor = read("Editor", "ShowcaseEditorMenu.cs")

    assert "EditorApplication.playModeStateChanged += FocusGameViewWhenPlayStarts" in editor
    assert "PlayModeStateChange.EnteredPlayMode" in editor
    assert 'GetType("UnityEditor.GameView")' in editor
    assert "gameView?.Focus()" in editor
    assert "Space jumps twice and holds hover while descending" in editor
    assert "Shift dashes on ground or in air" in editor


def test_laptop_control_copy_matches_third_person_aerial_mapping():
    guide = read("Presentation", "PlayerAgencyGuide.cs")
    menu = read("Presentation", "GuardianEquipmentMenu.cs")

    assert "WASD MOVE" in guide
    assert "MOUSE / ARROWS CAMERA" in guide
    assert "SPACE JUMP ×2 / HOLD HOVER" in guide
    assert "SHIFT DASH / AIR DASH" in guide
    assert '"WASD", "Move relative to camera"' in menu
    assert '"MOUSE / ARROWS", "Orbit camera"' in menu
    assert '"SPACE", "Jump ×2 · hold descending to hover"' in menu
    assert '"SHIFT", "Directional dash · one air dash per airtime"' in menu
    assert '"CTRL / ALT", "Compatibility dash aliases"' in menu
    assert '"T", "Lock / unlock enemy"' in menu
