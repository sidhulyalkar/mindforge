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


def test_equipment_mass_and_endurance_match_the_active_blade_roll_profile():
    equipment = read("Combat", "GuardianEquipmentLoadout.cs")
    motor = read("Combat", "GuardianMotor.cs")
    stamina = read("Combat", "GuardianStamina.cs")
    combat_input = read("Combat", "GuardianCombatInput.cs")

    assert "WeaponArchetype" in equipment
    assert "ShieldArchetype" in equipment
    assert "ArmorWeightClass" in equipment
    assert "EquipLoadClass" in equipment
    assert 'displayName = "Aetherblade"' in equipment
    assert 'displayName = "Verdant Ward · Legacy"' in equipment
    assert 'displayName = "Warden Weave"' in equipment
    assert "public float TotalMassKg" in equipment
    total_mass = equipment.split("public float TotalMassKg =>", 1)[1].split("public float EquipCapacityKg", 1)[0]
    assert "mainHand" in total_mass
    assert "armor" in total_mass
    assert "offHand" not in total_mass
    assert "MoveSpeedMultiplier" in equipment
    assert "RollSpeedMultiplier" in equipment
    assert "RollDurationMultiplier" in equipment

    assert "loadout.RollSpeedMultiplier" in motor
    assert "loadout.RollDurationMultiplier" in motor
    assert "loadout.MoveSpeedMultiplier" in motor
    assert "stamina.DodgeBaseCost" not in motor
    assert "_dashUntilTick" in motor
    assert "_invulnerableUntilTick" in motor
    assert "_dashQueuedUntilTick" in motor
    assert "SecondsToTicks" in motor
    assert "Time.fixedTime" in motor
    assert "Time.time" not in motor

    assert "recoveryPerSecond = 42f" in stamina
    assert "recoveryDelaySeconds = 0.48f" in stamina
    assert "dodgeBaseCost = 22f" in stamina
    assert "TrySpend" in stamina
    assert "DrainUpTo" in stamina

    assert "endurance.DodgeBaseCost" in combat_input
    assert "endurance.CanSpend(cost)" in combat_input
    assert "if (motor.RequestDash(aim))" in combat_input
    assert 'endurance?.TrySpend(cost, motor.IsGrounded ? "DODGE_ROLL" : "AIR_DASH")' in combat_input


def test_sword_is_swept_physical_contact_and_can_parry_projectiles():
    sword = read("Combat", "GuardianSwordShieldController.cs")
    attacks = read("Combat", "AttackDefinition.cs")

    assert "Physics.OverlapCapsuleNonAlloc" in sword
    assert "AttackDefinition[] lightChain" in sword
    assert "attack.IsActive(AttackElapsedTicks)" in sword
    assert "attack.ActiveProgress(AttackElapsedTicks)" in sword
    assert "_hitThisSwing.Add(receiver.GetInstanceID())" in sword
    assert "weapon.massKg" in sword
    assert "weapon.reachMeters" in sword
    assert "angularVelocity" in sword
    assert "swingMomentum" in sword
    assert "current.ComboBufferOpen(AttackElapsedTicks)" in sword
    assert "BeginSwordStep(_comboStep + 1" in sword
    assert "attack.DamageMultiplier" in sword
    assert "attack.PoiseMultiplier" in sword
    assert 'stamina.TrySpend(staminaCost, "SWORD_LIGHT")' not in sword
    assert "Time.time" not in sword

    assert "GuardianActionState" in sword
    assert "public bool CanAttack" in sword
    assert "public bool CanDodge" in sword
    assert "public bool CanGuard" in sword
    assert "public bool CanCounter" in sword
    assert "public bool CanMove" in sword
    assert "public bool CanTurn" in sword
    for forbidden_runtime_dependency in (
        "GetComponent<Animator>",
        "GetComponentInChildren<Animator>",
        "AnimatorStateInfo",
        ".SetTrigger(",
        "AnimationEvent",
    ):
        assert forbidden_runtime_dependency not in sword

    for token in (
        "startupTicks",
        "activeTicks",
        "recoveryTicks",
        "comboBufferOpenTick",
        "comboBufferCloseTick",
        "movementMultiplier",
        "turnMultiplier",
        "damageMultiplier",
        "poiseMultiplier",
        "presentationId",
        "CreateDefaultLightChain",
    ):
        assert token in attacks

    assert "TrySwordParry(projectile, weapon, resonanceValue)" in sword
    assert "projectile.IsHostileToGuardian" in sword
    assert "projectile.ReflectTowards" in sword
    assert "_parriedProjectilesThisSwing" in sword
    assert "maxProjectileParriesPerSwing" in sword
    assert "SwordProjectileParried" in sword
    assert '"SIGHT_SWORD_PARRY_DAMAGE"' in sword

    assert "auras != null && auras.SightActive" in sword
    assert "resonance.Sight" in sword
    assert "sightReachBonus" in sword
    assert "weapon.reachMeters * (1f + sightReachBonus * resonanceValue)" in sword
    assert '"SIGHT_SWORD_DAMAGE"' in sword
    assert "bonusDamage = Mathf.Max(0f, damage - baseDamage)" in sword


def test_legacy_shield_resolution_remains_backward_compatible_but_is_not_the_active_input_path():
    projectile = read("Combat", "MindforgeProjectile.cs")
    shield = read("Combat", "GuardianSwordShieldController.cs")
    combat_input = read("Combat", "GuardianCombatInput.cs")
    bootstrap = read("Combat", "PhysicalArsenalBootstrap.cs")

    shield_index = projectile.index("GuardianShieldHitbox shield")
    vitals_index = projectile.index("CombatantVitals receiver")
    assert shield_index < vitals_index
    assert "shield.TryResolveProjectile(this, point)" in projectile
    assert "ConsumeByShield" in projectile
    assert "TryResolveIncomingStrike" in shield
    assert "GuardStrikeResult" in shield
    assert "IsPerfectGuardWindow" in shield

    assert "guard_held = false" in combat_input
    assert "guard_down = false" in combat_input
    assert "physicalCombat?.SetGuardHeld(false, aim)" in combat_input
    assert "Input.GetKey(KeyCode.E)" not in combat_input
    assert 'NewChild("ShieldRoot"' not in bootstrap
    assert "GuardianShieldHitbox shieldHitbox =" not in bootstrap
    assert "physical.ConfigureRuntime(resonance, flux, target, null, hitStop, tuning)" in bootstrap


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


def test_third_person_commands_are_fixed_tick_recordable_and_old_tapes_remain_supported():
    combat_input = read("Combat", "GuardianCombatInput.cs")
    tape = read("Combat", "GuardianInputTape.cs")

    assert "Input.GetAxisRaw" not in combat_input
    assert "SampleWasdMovement" in combat_input
    assert "GuardianTargetLock targetLock" in combat_input
    assert "targetLock.DirectionFrom(transform.position)" in combat_input
    assert "Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up)" in combat_input
    assert "Input.GetKeyDown(KeyCode.Space)" in combat_input
    assert "Input.GetKeyDown(KeyCode.LeftShift)" in combat_input
    assert "Input.GetMouseButtonDown(1)" in combat_input
    assert "Input.GetKeyDown(KeyCode.LeftControl)" in combat_input
    assert "Input.GetKeyDown(KeyCode.LeftAlt)" in combat_input
    assert "Input.GetKeyDown(KeyCode.F)" in combat_input
    assert "Input.GetKeyDown(KeyCode.Q)" in combat_input
    assert "sword_attack_down = _swordAttackLatched" in combat_input
    assert "jump_down = _jumpLatched" in combat_input
    assert "jump_held = _jumpHeld" in combat_input
    assert "fire_held = false" in combat_input
    assert "guard_held = false" in combat_input
    assert "guard_down = false" in combat_input
    assert "physicalCombat.TryLightAttack(aim)" in combat_input
    assert "physicalCombat.ActionState != GuardianActionState.Locomotion" in combat_input
    assert "if (command.counter_down && combat.BeginCounter()) return;" in combat_input
    assert "if (command.cleave_down && combat.RiftCleave(aim)) return;" in combat_input
    assert "if (command.bloom_down && bloom != null && bloom.TryActivate()) return;" in combat_input
    assert "combat.FirePulse(aim)" not in combat_input

    assert 'SchemaV1 = "mindforge.guardian_input_tape.v1"' in tape
    assert 'SchemaV2 = "mindforge.guardian_input_tape.v2"' in tape
    assert 'SchemaV3 = "mindforge.guardian_input_tape.v3"' in tape
    assert "_tape.schema != SchemaV1 && _tape.schema != SchemaV2 && _tape.schema != SchemaV3" in tape
    assert "sword_attack_down = sword_attack_down" in tape
    assert "jump_down = jump_down" in tape
    assert "jump_held = jump_held" in tape
    assert "guard_held = guard_held" in tape


def test_procedural_rig_hud_and_menu_present_the_grounded_energy_blade_language():
    bootstrap = read("Combat", "PhysicalArsenalBootstrap.cs")
    rig = read("Combat", "GuardianSwordShieldRig.cs")
    hud = read("Presentation", "GroundedCombatHud.cs")
    menu = read("Presentation", "GuardianEquipmentMenu.cs")
    bridge = read("Telemetry", "PhysicalArsenalMarkerBridge.cs")

    assert '"AetherbladeWhiteCore"' in bootstrap
    assert '"AetherbladeResonantSheath"' in bootstrap
    assert '"AetherbladeEnergyScale"' in bootstrap
    assert '"AetherbladeEmitter"' in bootstrap
    assert '"AetherbladeCrossguard"' in bootstrap
    assert '"AetherbladeGrip"' in bootstrap
    assert '"AetherbladePommel"' in bootstrap
    assert '"SwordEnergyTip"' in bootstrap
    assert "GuardianDodgeRollPresentation" in bootstrap
    assert "TrailRenderer" in bootstrap
    assert "FracturedSignalMeleeDirector" in bootstrap
    assert 'NewChild("ShieldRoot"' not in bootstrap

    assert "maxSwordLengthBonus = 0.72f" in rig
    assert "scale.z *= 1f + maxSwordLengthBonus * sight" in rig
    assert "ApplySwordRenderer" in rig
    assert "swordTrail.emitting = attacking" in rig
    assert "swordLight.intensity" in rig

    assert '"GUARDIAN · CRITICAL"' in hud
    assert '"ENDURANCE"' in hud
    assert '"F / LMB BLADE   ·   SHIFT / RMB ROLL' in hud
    assert "SuppressLegacyHud()" in hud

    assert '"GUARDIAN KIT"' in menu
    assert '"THIRD-PERSON CONTROLS"' in menu
    assert '"Endurance Dodge Roll"' in menu
    assert '"SHIFT / RMB"' in menu
    assert '"F / LMB"' in menu
    assert "ENDURANCE {stamina}" in menu
    assert '"X / MMB", "Pulse Shot"' not in menu
    assert '"RMB / E", "Shield"' not in menu
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
