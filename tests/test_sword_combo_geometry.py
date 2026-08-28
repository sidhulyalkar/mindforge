from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
COMBAT = ROOT / "unity" / "Assets" / "Mindforge" / "Combat"
PRESENTATION = ROOT / "unity" / "Assets" / "Mindforge" / "Presentation"


def test_second_combo_step_reverses_authoritative_sweep_direction():
    controller = (COMBAT / "GuardianSwordShieldController.cs").read_text(encoding="utf-8")
    definition = (COMBAT / "AttackDefinition.cs").read_text(encoding="utf-8")

    # Geometry consumes the attack-data flag instead of hard-coding combo-step identity.
    assert "if (attack.ReverseSweep)" in controller
    assert "from = sweepDegrees * 0.5f;" in controller
    assert "to = -sweepDegrees * 0.5f;" in controller
    assert "Quaternion.AngleAxis(angle, Vector3.up) * _attackAim" in controller

    # The second default light-chain definition is the reverse sweep.
    assert 'Create("aetherblade_light_2"' in definition
    light_two = definition[definition.index('Create("aetherblade_light_2"'):definition.index('Create("aetherblade_light_3"')]
    assert "true, false, \"guardian_light_2\"" in light_two


def test_rendered_weapon_uses_same_combo_step_direction():
    rig = (COMBAT / "GuardianSwordShieldRig.cs").read_text(encoding="utf-8")
    driver = (PRESENTATION / "GuardianArmamentPresentationDriver.cs").read_text(encoding="utf-8")
    controller = (COMBAT / "GuardianSwordShieldController.cs").read_text(encoding="utf-8")

    assert "comboStep == 2" in rig
    assert "Mathf.Lerp(72f, -72f, eased)" in rig
    assert "comboStep >= 3" in rig
    assert "combat.ComboStep" in driver
    assert "combat.AttackProgress" in driver
    assert "GuardCoverageScale,\n                    _comboStep" in controller
