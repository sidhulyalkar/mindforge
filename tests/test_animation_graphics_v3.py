from __future__ import annotations

from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
UNITY = ROOT / "unity" / "Assets" / "Mindforge"


def read(*parts: str) -> str:
    return UNITY.joinpath(*parts).read_text(encoding="utf-8")


def test_animation_graphics_v3_installs_motion_animator_and_armament_polish():
    installer = read("Presentation", "ShowcaseRuntimeInstaller.cs")
    for token in (
        "GuardianMotionPolish",
        "GuardianLocomotionVfx",
        "GuardianAnimatorBridge",
        "CinematicArmamentVfxPolish",
        "FracturedSignalMotionPolish",
        "FracturedSignalAnimatorBridge",
    ):
        assert token in installer


def test_guardian_motion_polish_is_additive_and_event_driven_not_authoritative():
    source = read("Presentation", "GuardianMotionPolish.cs")
    for token in (
        "SwordComboStepStarted += OnSwordStep",
        "ShieldBlocked += OnShieldBlocked",
        "PerfectGuard += OnPerfectGuard",
        "GuardBroken += OnGuardBroken",
        "DashStarted += OnDash",
        "vitals.Damaged += OnDamaged",
        'Wrap("Motion_Torso"',
        'Wrap("Motion_LeftArm"',
        'Wrap("Motion_RightLeg"',
        "Window01(attackProgress",
        "_bodyMotion.localRotation",
    ):
        assert token in source

    for forbidden in (
        "ReceiveDamage(",
        "TryLightAttack(",
        "SetGuardHeld(",
        "RequestDash(",
        "TrySpend(",
        "TryApply(",
        "Award(",
    ):
        assert forbidden not in source


def test_production_guardian_animator_contract_disables_root_motion_and_only_consumes_state():
    source = read("Presentation", "GuardianAnimatorBridge.cs")
    for token in (
        'Animator.StringToHash("Speed")',
        'Animator.StringToHash("MoveX")',
        'Animator.StringToHash("MoveY")',
        'Animator.StringToHash("AttackProgress")',
        'Animator.StringToHash("ComboStep")',
        'Animator.StringToHash("Guard")',
        'Animator.StringToHash("SightResonance")',
        'Animator.StringToHash("GuardResonance")',
        'Animator.StringToHash("PerfectGuard")',
        'Animator.StringToHash("GuardBreak")',
        "animator.applyRootMotion = false",
    ):
        assert token in source

    for forbidden in (
        "transform.position =",
        "transform.rotation =",
        "ReceiveDamage(",
        "TryLightAttack(",
        "SetGuardHeld(",
        "RequestDash(",
        "TryApply(",
    ):
        assert forbidden not in source


def test_fractured_signal_motion_and_animator_are_scheduler_observers_only():
    motion = read("Presentation", "FracturedSignalMotionPolish.cs")
    bridge = read("Presentation", "FracturedSignalAnimatorBridge.cs")

    for token in (
        "director.PhaseChanged += OnPhase",
        "director.AttackTelegraphed += OnTelegraph",
        "director.AttackFired += OnFire",
        "vitals.Damaged += OnDamaged",
        "_avatarRoot.localScale",
    ):
        assert token in motion

    for token in (
        'Animator.StringToHash("Phase")',
        'Animator.StringToHash("Heavy")',
        'Animator.StringToHash("Telegraph")',
        'Animator.StringToHash("Fire")',
        "animator.applyRootMotion = false",
    ):
        assert token in bridge

    for source in (motion, bridge):
        for forbidden in (
            "ReceiveDamage(",
            "SpawnProjectile",
            "ResolveCleave(",
            "ResolveSlam(",
            "TryResolveIncomingStrike(",
        ):
            assert forbidden not in source


def test_armament_vfx_is_state_driven_and_never_changes_weapon_or_shield_authority():
    source = read("Presentation", "CinematicArmamentVfxPolish.cs")
    for token in (
        '"AetherbladeAfterimage"',
        '"AetherbladeMotes"',
        '"VerdantWardMotes"',
        "combat.SightResonance",
        "combat.GuardResonance",
        "combat.ShieldBlocked += OnBlocked",
        "combat.PerfectGuard += OnPerfectGuard",
        "combat.GuardBroken += OnGuardBroken",
        "_afterimage.emitting = attacking",
    ):
        assert token in source

    for forbidden in (
        "ReceiveDamage(",
        "TryLightAttack(",
        "SetGuardHeld(",
        "GuardCoverageScale =",
        "SightResonance =",
        "TrySpend(",
    ):
        assert forbidden not in source


def test_locomotion_vfx_reads_velocity_but_never_mutates_motor():
    source = read("Presentation", "GuardianLocomotionVfx.cs")
    assert "motor.Velocity" in source
    assert "motor.DashStarted += OnDash" in source
    assert '"CinematicGroundInteractionVfx"' in source
    assert "ParticleSystemSimulationSpace.World" in source
    for forbidden in ("RequestDash(", "SetMoveInput(", "AddForce(", "velocity ="):
        assert forbidden not in source
