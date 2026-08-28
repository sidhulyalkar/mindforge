from __future__ import annotations

from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
UNITY = ROOT / "unity" / "Assets" / "Mindforge"


def read(*parts: str) -> str:
    return UNITY.joinpath(*parts).read_text(encoding="utf-8")


def test_dodge_has_real_short_iframe_not_full_motion_immunity():
    motor = read("Combat", "GuardianMotor.cs")
    vitals = read("Combat", "CombatantVitals.cs")
    projectile = read("Combat", "MindforgeProjectile.cs")

    assert "dodgeInvulnerabilitySeconds = 0.105f" in motor
    assert "public bool IsDashing => FixedTick < _dashUntilTick" in motor
    assert "public bool IsInvulnerable => FixedTick < _invulnerableUntilTick" in motor
    assert "Mathf.Min(rollDuration, Mathf.Max(0f, dodgeInvulnerabilitySeconds))" in motor
    assert "int rollTicks = SecondsToTicks(rollDuration)" in motor
    assert "int invulnerabilityTicks = Mathf.Min(" in motor
    assert "_invulnerableUntilTick = FixedTick + invulnerabilityTicks" in motor
    assert "Time.time" not in motor

    assert "public bool IsTemporarilyInvulnerable" in vitals
    assert "packet.SourceTeam == team || IsTemporarilyInvulnerable" in vitals

    assert "if (receiver.IsTemporarilyInvulnerable) return;" in projectile
    iframe_index = projectile.index("if (receiver.IsTemporarilyInvulnerable) return;")
    destroy_index = projectile.index("if (pierce > 0) pierce--; else Destroy(gameObject);")
    assert iframe_index < destroy_index


def test_fixed_tick_action_grammar_prevents_guard_roll_attack_overlays():
    source = read("Combat", "GuardianCombatInput.cs")
    physical = read("Combat", "GuardianSwordShieldController.cs")

    dash_block = source.index("if (command.dash_down && (physicalCombat == null || physicalCombat.CanDodge))")
    guard_block = source.index("physicalCombat?.SetGuardHeld(command.guard_held, aim)")
    sword_block = source.index("if (command.sword_attack_down) physicalCombat?.TryLightAttack(aim)")
    ranged_block = source.index("if (command.fire_held) combat.FirePulse(aim)")
    assert dash_block < guard_block < sword_block < ranged_block

    assert "if (motor.RequestDash(aim)) return;" in source
    assert "if (motor.IsDashing)" in source
    assert "if (physicalCombat != null && physicalCombat.IsGuarding) return;" in source
    assert "if (physicalCombat != null && physicalCombat.IsAttacking) return;" in source

    assert "public GuardianActionState ActionState => ResolveActionState()" in physical
    assert "public bool CanDodge => ActionState == GuardianActionState.Locomotion || ActionState == GuardianActionState.Guard" in physical
    assert "public bool CanAttack => ActionState == GuardianActionState.Locomotion" in physical
    assert "motor != null && motor.IsDashing" in physical


def test_guard_stance_costs_mobility_and_guard_integrity_recovery_even_without_being_hit():
    physical = read("Combat", "GuardianSwordShieldController.cs")
    stamina = read("Combat", "GuardianStamina.cs")
    motor = read("Combat", "GuardianMotor.cs")

    assert "guardMoveMultiplier = 0.70f" in physical
    assert "guardIntegrityRecoveryMultiplier = 0.34f" in physical
    assert "if (IsGuarding) return guardMoveMultiplier" in physical
    assert "stamina?.SetRecoveryMultiplier(_guardHeld ? guardIntegrityRecoveryMultiplier : 1f)" in physical
    assert "stamina?.SetRecoveryMultiplier(1f)" in physical

    assert "private float _recoveryMultiplier = 1f" in stamina
    assert "SetRecoveryMultiplier" in stamina
    assert "recoveryPerSecond) * _recoveryMultiplier" in stamina

    assert "physicalCombat.MovementMultiplier" in motor
    assert "maxSpeed = tuning.maxSpeed * loadMultiplier * stanceMultiplier" in motor
