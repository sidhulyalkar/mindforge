from __future__ import annotations

from pathlib import Path

from mindforge_neuro.encounter import analyze_encounter
from mindforge_neuro.markers import GAME_MARKER_V1, GameMarker


ROOT = Path(__file__).resolve().parents[1]
UNITY = ROOT / "unity" / "Assets" / "Mindforge"


def read(*parts: str) -> str:
    return UNITY.joinpath(*parts).read_text(encoding="utf-8")


def marker(
    seq: int,
    event: str,
    *,
    value: float = 0.0,
    reason: str | None = None,
    target: str | None = None,
    category: str = "combat_outcome",
) -> GameMarker:
    return GameMarker(
        schema=GAME_MARKER_V1,
        seq=seq,
        session_id="physical-arsenal-session",
        calibration_id="cal-arsenal",
        event=event,
        category=category,
        unity_realtime_s=float(seq) * 12.0,
        game_time_s=float(seq) * 12.0,
        frame=seq * 120,
        fixed_tick=seq * 120,
        reason=reason,
        target=target,
        value=value,
        boss_phase=2,
    )


def test_equipment_mass_remains_mechanical_but_basic_movement_and_dodge_are_unlimited():
    equipment = read("Combat", "GuardianEquipmentLoadout.cs")
    motor = read("Combat", "GuardianMotor.cs")
    stamina = read("Combat", "GuardianStamina.cs")

    assert "WeaponArchetype" in equipment
    assert "ShieldArchetype" in equipment
    assert "ArmorWeightClass" in equipment
    assert "EquipLoadClass" in equipment
    assert 'displayName = "Aetherblade Longsword"' in equipment
    assert 'displayName = "Verdant Ward Shield"' in equipment
    assert 'displayName = "Warden Weave"' in equipment
    assert "coverageDegrees = 112f" in equipment
    assert "public float TotalMassKg" in equipment
    assert "MoveSpeedMultiplier" in equipment
    assert "RollSpeedMultiplier" in equipment
    assert "RollDurationMultiplier" in equipment

    assert "loadout.RollSpeedMultiplier" in motor
    assert "loadout.RollDurationMultiplier" in motor
    assert "loadout.MoveSpeedMultiplier" in motor
    assert "tuning.dashCooldown" not in motor
    assert "dashInputBufferSeconds" in motor
    assert "Vector3.MoveTowards" in motor
    assert "stamina.DodgeBaseCost" not in motor
    assert 'stamina.TrySpend(staminaCost, "DODGE_ROLL")' not in motor

    # Guard Integrity still uses the shared defensive budget.
    assert "recoveryDelaySeconds" in stamina
    assert "TrySpend" in stamina
    assert "DrainUpTo" in stamina


def test_sword_is_swept_physical_contact_free_to_swing_and_can_parry_projectiles():
    sword = read("Combat", "GuardianSwordShieldController.cs")

    assert "Physics.OverlapCapsuleNonAlloc" in sword
    assert "activeStart = 0.24f" in sword
    assert "activeEnd = 0.72f" in sword
    assert "_hitThisSwing.Add(receiver.GetInstanceID())" in sword
    assert "weapon.massKg" in sword
    assert "weapon.reachMeters" in sword
    assert "angularVelocity" in sword
    assert "swingMomentum" in sword
    assert "comboQueueOpensAt" in sword
    assert "_comboStep < 3" in sword
    assert "BeginSwordStep(_comboStep + 1" in sword
    assert "finisherDamageMultiplier" in sword
    assert "finisherPoiseMultiplier" in sword
    assert 'stamina.TrySpend(staminaCost, "SWORD_LIGHT")' not in sword

    # The same active sword volume can intercept hostile projectiles.
    assert "TrySwordParry(projectile, weapon, resonanceValue)" in sword
    assert "projectile.IsHostileToGuardian" in sword
    assert "projectile.ReflectTowards" in sword
    assert "_parriedProjectilesThisSwing" in sword
    assert "maxProjectileParriesPerSwing" in sword
    assert "SwordProjectileParried" in sword
    assert '"SIGHT_SWORD_PARRY_DAMAGE"' in sword

    assert "auras != null && auras.SightActive" in sword
    assert "resonance.Sight" in sword
    assert '"SIGHT_SWORD_DAMAGE"' in sword
    assert "bonusDamage = Mathf.Max(0f, damage - baseDamage)" in sword


def test_shield_is_directional_collision_with_guard_integrity_chip_and_true_concord_counterfactual():
    projectile = read("Combat", "MindforgeProjectile.cs")
    shield = read("Combat", "GuardianSwordShieldController.cs")

    shield_index = projectile.index("GuardianShieldHitbox shield")
    vitals_index = projectile.index("CombatantVitals receiver")
    assert shield_index < vitals_index
    assert "shield.TryResolveProjectile(this, point)" in projectile
    assert "ConsumeByShield" in projectile

    assert "shield.baseDamageAbsorption" in shield
    assert "maxGuardAbsorptionBonus * guard" in shield
    assert "shield.guardStaminaScale / stability" in shield
    assert '"PERFECT_GUARD"' in shield
    assert "BreakGuard();" in shield
    assert "guardIntegrityRecoveryMultiplier" in shield

    assert "TryResolveIncomingStrike" in shield
    assert "GuardStrikeResult" in shield
    assert "shield.coverageDegrees" in shield
    assert "Vector3.Angle(guardFacing, towardThreat)" in shield
    assert "GuardStrikeResult.OutsideCoverage" in shield
    assert "guardBreakDamageLeak" in shield

    assert "float baselineDamage" in shield
    assert "float reflectedDamage = baselineDamage * concordMultiplier" in shield
    assert "reflectedDamage - baselineDamage" in shield
    assert 'concord ? "CONCORD_COUNTER_DAMAGE" : null' in shield


def test_continuous_neural_resonance_cannot_issue_conventional_player_commands():
    resonance = read("SoulWisp", "NeuralFocusResonance.cs")

    assert "EvidenceReceived += OnEvidence" in resonance
    assert "evt.has_evidence" in resonance
    assert "evt.artifact" in resonance
    assert "evt.quality" in resonance
    assert "sight_score" in resonance
    assert "guard_score" in resonance
    assert "staleAfterSeconds" in resonance

    forbidden = (
        "TryLightAttack(",
        "SetGuardHeld(",
        "RequestDash(",
        "FirePulse(",
        "RiftCleave(",
        "BeginCounter(",
        "ReceiveDamage(",
    )
    assert all(token not in resonance for token in forbidden)


def test_keyboard_first_commands_are_fixed_tick_recordable_and_old_tapes_remain_supported():
    combat_input = read("Combat", "GuardianCombatInput.cs")
    tape = read("Combat", "GuardianInputTape.cs")

    assert "Input.GetAxisRaw" not in combat_input
    assert "SampleWasdMovement" in combat_input
    assert "SampleArrowAim" in combat_input
    assert "Input.GetKeyDown(KeyCode.Space)" in combat_input
    assert "Input.GetKeyDown(KeyCode.F)" in combat_input
    assert "Input.GetKey(KeyCode.LeftShift)" in combat_input
    assert "Input.GetKey(KeyCode.E)" in combat_input
    assert "Input.GetKeyDown(KeyCode.Q)" in combat_input
    assert "sword_attack_down = _swordAttackLatched" in combat_input
    assert "guard_held = _guardHeld" in combat_input
    assert "physicalCombat?.SetGuardHeld(command.guard_held, aim)" in combat_input
    assert "physicalCombat?.TryLightAttack(aim)" in combat_input

    assert 'SchemaV1 = "mindforge.guardian_input_tape.v1"' in tape
    assert 'SchemaV2 = "mindforge.guardian_input_tape.v2"' in tape
    assert "_tape.schema != SchemaV1 && _tape.schema != SchemaV2" in tape
    assert "sword_attack_down = sword_attack_down" in tape
    assert "guard_held = guard_held" in tape


def test_procedural_rig_hud_and_menu_present_the_new_combat_language():
    bootstrap = read("Combat", "PhysicalArsenalBootstrap.cs")
    rig = read("Combat", "GuardianSwordShieldRig.cs")
    hud = read("Presentation", "CombatStateHud.cs")
    menu = read("Presentation", "GuardianEquipmentMenu.cs")
    bridge = read("Telemetry", "PhysicalArsenalMarkerBridge.cs")

    assert '"AetherbladeCore"' in bootstrap
    assert '"AetherbladeEnergyEdge"' in bootstrap
    assert '"AetherbladeCrossguard"' in bootstrap
    assert '"AetherbladeGrip"' in bootstrap
    assert '"AetherbladePommel"' in bootstrap
    assert '"VerdantWard"' in bootstrap
    assert "CreatePbrMaterial" in bootstrap
    assert "BoxCollider shieldCollider" in bootstrap
    assert "TrailRenderer" in bootstrap
    assert "FracturedSignalMeleeDirector" in bootstrap

    assert "maxSwordLengthBonus = 0.42f" in rig
    assert "guardCoverageScale" in rig
    assert "ApplySwordRenderer" in rig
    assert "Color forged" in rig
    assert "swordTrail.emitting = attacking" in rig
    assert "shieldLight.intensity" in rig

    assert '"GUARD"' in hud
    assert '"F  SWORD   SPACE  DODGE   SHIFT  PULSE   TAB  BUILD"' in hud
    assert '"HP {bossVitals.Health:F0} / {bossVitals.MaxHealth:F0}"' in hud
    assert '"AETHER PARRY' in hud

    assert '"WARDEN LOADOUT"' in menu
    assert '"COMBAT CONTROLS"' in menu
    assert "GUARD INTEGRITY" in menu
    assert '"WASD"' in menu
    assert '"ARROWS / MOUSE"' in menu
    assert '"SPACE"' in menu
    assert '"F"' in menu
    assert "FindObjectOfType<GuardianEquipmentLoadout>(true)" in menu

    assert '"SWORD_PARRY"' in bridge
    assert "combat.SwordProjectileParried += OnSwordParry" in bridge


def test_encounter_report_separates_physical_skill_and_sight_sword_payoff():
    markers = [
        marker(1, "PHYSICAL_ARSENAL_READY", value=26.6, reason="MEDIUM", category="equipment"),
        marker(2, "NEURAL_PAYOFF_LEDGER_READY", reason="CONSERVATIVE_DIRECT_DAMAGE_AND_HEAL_V1", category="neural_payoff"),
        marker(3, "NEURAL_BUFF_APPLIED", target="sight", category="neural_payoff"),
        marker(4, "SWORD_LIGHT", category="combat_action"),
        marker(5, "SWORD_HIT", value=29.0, reason="SIGHT_AMPLIFIED"),
        marker(6, "NEURAL_DAMAGE_BONUS_REALIZED", value=5.0, reason="SIGHT_SWORD_DAMAGE", target="boss", category="neural_payoff"),
        marker(7, "SWORD_LIGHT", category="combat_action"),
        marker(8, "SHIELD_RAISED", category="combat_action"),
        marker(9, "SHIELD_BLOCK", value=2.4, reason="IN_12.00_CHIP_2.40"),
        marker(10, "SHIELD_LOWERED", category="combat_action"),
        marker(11, "SHIELD_RAISED", category="combat_action"),
        marker(12, "PERFECT_GUARD"),
        marker(13, "GUARD_BROKEN"),
        marker(14, "SIGNAL_BREAK"),
        marker(15, "VICTORY"),
    ]

    report = analyze_encounter(markers)
    assert report.physical_arsenal_ready is True
    assert report.equipment_load_class == "MEDIUM"
    assert report.equipped_mass_kg == 26.6
    assert report.sword_attacks == 2
    assert report.sword_hits == 1
    assert report.sword_hit_rate == 0.5
    assert report.shield_raises == 2
    assert report.shield_blocks == 1
    assert report.perfect_guards == 1
    assert report.guard_breaks == 1
    assert report.shield_chip_damage_total == 2.4
    assert report.sight_sword_bonus_damage == 5.0
    assert report.realized_neural_bonus_damage_total == 5.0


def test_physical_arsenal_analytics_are_additive_for_legacy_sessions():
    report = analyze_encounter([
        marker(1, "SIGNAL_BREAK"),
        marker(2, "VICTORY"),
    ])
    assert report.physical_arsenal_ready is False
    assert report.equipment_load_class is None
    assert report.equipped_mass_kg is None
    assert report.sword_attacks == 0
    assert report.shield_blocks == 0
    assert report.sight_sword_bonus_damage == 0.0
