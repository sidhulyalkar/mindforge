from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
OVERLAY = ROOT / "dragonsouls_overlay" / "Assets" / "Mindforge"
DESKTOP = OVERLAY / "Runtime" / "MindforgeDesktopCombatBindingsV31.cs"
RUNTIME = OVERLAY / "Runtime" / "MindforgeVerticalSliceRuntimeV31.cs"
READINESS = OVERLAY / "Editor" / "MindforgeVerticalSliceReadinessV31.cs"


def read(path: Path) -> str:
    assert path.exists(), f"missing V0.31 desktop combat source: {path}"
    return path.read_text(encoding="utf-8")


def test_v31_desktop_adapter_adds_missing_bindings_to_the_inherited_action_map():
    text = read(DESKTOP)
    for token in (
        "using System.Reflection;",
        "using Inputs;",
        "using UnityEngine.InputSystem;",
        "DefaultExecutionOrder(-120)",
        'GetField("_controls", PrivateInstance)',
        "global::Controllers",
        "global::Controllers.PlayerActions",
        'AddBinding(player.Camera, "<Mouse>/delta", "scaleVector2(x=0.035,y=0.035)")',
        'AddBinding(player.Target, "<Mouse>/middleButton")',
        'AddBinding(player.Sprint, "<Keyboard>/leftShift")',
        'AddBinding(player.LightAttack, "<Mouse>/leftButton")',
        'AddBinding(player.HeavyAttack, "<Mouse>/rightButton")',
        'AddBinding(player.SheathSword, "<Keyboard>/x")',
        'AddBinding(player.Aim, "<Keyboard>/q")',
        'AddBinding(player.WeaponReturn, "<Keyboard>/r")',
        'AddBinding(player.Roll, "<Keyboard>/leftAlt")',
        'AddBinding(player.Heal, "<Keyboard>/h")',
        'AddBinding(player.LightBonfire, "<Keyboard>/e")',
        'AddBinding(player.Pause, "<Keyboard>/escape")',
        "DesktopCombatReady",
        "HasBinding",
        "InputActionSetupExtensions.BindingSyntax",
        "WithProcessors(processors)",
    ):
        assert token in text


def test_v31_desktop_adapter_never_bypasses_inputreader_or_owns_gameplay():
    text = read(DESKTOP)
    for forbidden in (
        "LightAttackEvent?.Invoke",
        "HeavyAttackEvent?.Invoke",
        "TargetEvent?.Invoke",
        "RollEvent?.Invoke",
        "WeaponReturnEvent?.Invoke",
        "StartAttack(",
        "StopAttack(",
        "TakeDamage(",
        "GiveDamageForced(",
        "ChangeState(",
        "CharacterController.Move",
        "transform.position =",
        "Animator.Set",
        "Time.timeScale",
        "MindforgeIntentBusV29.Publish(",
    ):
        assert forbidden not in text


def test_v31_runtime_installs_desktop_bindings_before_combat_assurance():
    text = read(RUNTIME)
    assert "InstallDesktopBindings();" in text
    assert "MindforgeDesktopCombatBindingsV31" in text
    assert "InstallSwordAssurance();" in text
    assert text.index("InstallDesktopBindings();") < text.index("InstallSwordAssurance();")


def test_v31_readiness_requires_desktop_combat_actions_to_resolve_in_play_mode():
    text = read(READINESS)
    for token in (
        "MindforgeDesktopCombatBindingsV31[] desktopBindings",
        '"desktop_combat_bindings_runtime"',
        "desktopBindings[0].Installed",
        "desktopBindings[0].DesktopCombatReady",
        "desktopBindings[0].BindingsAdded",
        "desktopBindings[0].BindingSummary",
    ):
        assert token in text
