from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
RUNTIME = ROOT / "dragonsouls_overlay" / "Assets" / "Mindforge" / "Runtime"
EDITOR = ROOT / "dragonsouls_overlay" / "Assets" / "Mindforge" / "Editor"
PROFILE = RUNTIME / "MindforgeKeyboardControlProfileV33.cs"
INTEGRATION = RUNTIME / "MindforgeBciIntegrationRuntimeV33.cs"
READINESS = EDITOR / "MindforgeBciReadinessV33.cs"


def read(path: Path) -> str:
    assert path.exists(), f"missing V0.33 keyboard control source: {path}"
    return path.read_text(encoding="utf-8")


def test_v33_installs_the_requested_laptop_keyboard_contract():
    profile = read(PROFILE)
    runtime = read(INTEGRATION)

    assert 'public const string MovementContract = "WASD";' in profile
    assert 'public const string CameraContract = "ARROW_KEYS";' in profile
    assert 'public const string AttackContract = "SPACE_LIGHT_ATTACK";' in profile

    for path in (
        '"<Keyboard>/w"',
        '"<Keyboard>/s"',
        '"<Keyboard>/a"',
        '"<Keyboard>/d"',
        '"<Keyboard>/upArrow"',
        '"<Keyboard>/downArrow"',
        '"<Keyboard>/leftArrow"',
        '"<Keyboard>/rightArrow"',
        '"<Keyboard>/space"',
    ):
        assert path in profile

    assert "EnsureDirectionalComposite(" in profile
    assert "player.Move" in profile
    assert "player.Camera" in profile
    assert "EnsureDirectBinding(player.LightAttack, SpacePath)" in profile
    assert "RemoveBindingsForPath(player.Jump, SpacePath)" in profile
    assert "CinemachineInputProvider" in profile
    assert "provider.XYAxis.action" in profile
    assert "Install<MindforgeKeyboardControlProfileV33>();" in runtime


def test_v33_keyboard_profile_is_binding_only_and_preserves_gameplay_authority():
    profile = read(PROFILE)

    assert 'typeof(InputReader).GetField(' in profile
    assert 'BindingFlags.Instance | BindingFlags.NonPublic' in profile
    assert 'pinned_input_contract_drift:_controls_missing' in profile
    assert 'action.AddCompositeBinding("2DVector")' in profile
    assert "action.ChangeBinding(i).Erase();" in profile
    assert "existing gamepad/mouse bindings preserved" in profile

    for forbidden in (
        "LightAttackEvent?.Invoke",
        "HeavyAttackEvent?.Invoke",
        "MovementOn2DAxis =",
        "CameraMovementOn2DAxis =",
        "CharacterController.Move",
        "ChangeState(",
        "health.CurrentHealth",
        "BossManager",
        "EnemyNightmareDragonController",
        "InputSystem.AddDevice",
        "QueueStateEvent",
        "m_Priority =",
    ):
        assert forbidden not in profile


def test_v33_readiness_fail_closes_on_keyboard_profile_drift():
    readiness = read(READINESS)

    assert "MindforgeKeyboardControlProfileV33 keyboardProfile" in readiness
    assert '!keyboardProfile.WasdMovementBound' in readiness
    assert '!keyboardProfile.ArrowCameraBound' in readiness
    assert '!keyboardProfile.SpaceAttackBound' in readiness
    assert '!keyboardProfile.UpstreamJumpSpaceRemoved' in readiness
    assert 'failures.Add("laptop_keyboard_profile_runtime")' in readiness
    assert 'MindforgeKeyboardControlProfileV33.MovementContract == "WASD"' in readiness
    assert 'MindforgeKeyboardControlProfileV33.CameraContract == "ARROW_KEYS"' in readiness
    assert 'MindforgeKeyboardControlProfileV33.AttackContract == "SPACE_LIGHT_ATTACK"' in readiness
    assert 'failures.Add("laptop_keyboard_static_contract")' in readiness
