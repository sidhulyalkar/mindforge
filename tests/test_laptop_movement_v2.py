from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
UNITY = ROOT / "unity" / "Assets" / "Mindforge"


def read(*parts: str) -> str:
    return UNITY.joinpath(*parts).read_text(encoding="utf-8")


def test_wasd_is_direct_input_and_arrows_are_aim_only():
    source = read("Combat", "GuardianCombatInput.cs")

    assert "Input.GetAxisRaw" not in source
    assert "SampleWasdMovement" in source
    assert "SampleArrowAim" in source
    for key in ("KeyCode.W", "KeyCode.A", "KeyCode.S", "KeyCode.D"):
        assert key in source
    for key in ("KeyCode.UpArrow", "KeyCode.DownArrow", "KeyCode.LeftArrow", "KeyCode.RightArrow"):
        assert key in source
    assert source.index("if (_keyboardAim.sqrMagnitude > 0.01f)") < source.index("if (mouseAimEnabled && _mouseAimActive")
    assert "CameraRelativeDirection(_keyboardAim, camera)" in source


def test_movement_uses_target_velocity_response_instead_of_force_drag_slush():
    motor = read("Combat", "GuardianMotor.cs")

    assert "Vector3.MoveTowards" in motor
    assert "minimumAcceleration = 58f" in motor
    assert "deceleration = 76f" in motor
    assert "reversalAcceleration = 92f" in motor
    assert "_body.AddForce" not in motor
    assert "_body.WakeUp()" in motor
    assert "Vector3.ClampMagnitude(right * _moveInput.x + forward * _moveInput.y, 1f)" in motor


def test_dashes_are_unlimited_by_cooldown_and_can_buffer_chains():
    motor = read("Combat", "GuardianMotor.cs")

    assert "tuning.dashCooldown" not in motor
    assert "dashInputBufferSeconds" in motor
    assert "_dashQueued = true" in motor
    assert "_dashQueuedUntil" in motor
    assert "StartDash(ResolveDashDirection(_queuedDashFallback))" in motor
    assert "DashStarted?.Invoke()" in motor


def test_showcase_focuses_game_view_when_play_mode_starts():
    editor = read("Editor", "ShowcaseEditorMenu.cs")

    assert "EditorApplication.playModeStateChanged += FocusGameViewWhenPlayStarts" in editor
    assert "PlayModeStateChange.EnteredPlayMode" in editor
    assert 'GetType("UnityEditor.GameView")' in editor
    assert "gameView?.Focus()" in editor
    assert "WASD moves; arrows/mouse aim; Space dashes" in editor


def test_laptop_control_copy_matches_authoritative_mapping():
    guide = read("Presentation", "PlayerAgencyGuide.cs")
    menu = read("Presentation", "GuardianEquipmentMenu.cs")

    assert "WASD MOVE" in guide
    assert "ARROWS / MOUSE AIM" in guide
    assert "SPACE DASH" in guide
    assert '"WASD", "Move"' in menu
    assert '"ARROWS / MOUSE", "Aim"' in menu
    assert '"SPACE", "Directional dash"' in menu
