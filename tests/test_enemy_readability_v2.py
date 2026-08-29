from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
UNITY = ROOT / "unity" / "Assets" / "Mindforge"


def read(*parts: str) -> str:
    return UNITY.joinpath(*parts).read_text(encoding="utf-8")


def test_enemy_attack_data_has_explicit_tracking_commit_fraction():
    definition = read("Enemies", "EnemyAttackDefinition.cs")

    for token in (
        "trackingLock01 = 0.72f",
        "public float TrackingLock01",
        "trackingLock01 <= 0f ? 0.72f : trackingLock01",
        "float trackingLock,",
        "trackingLock01 = trackingLock",
        "TrackingLock01 defines the fixed-tick point",
    ):
        assert token in definition

    for forbidden in (
        "Time.time",
        "Time.deltaTime",
        "Animator",
        "NeuralEvent",
        "UdpNeuralReceiver",
        "Input.Get",
    ):
        assert forbidden not in definition


def test_enemy_controller_stops_tracking_before_resolution_and_exposes_phase_truth():
    brain = read("Journey", "JourneyEnemyController.cs")

    for token in (
        "private long _attackStartedTick",
        "private long _recoveryStartedTick",
        "private int _recoveryDurationTicks",
        "public float AttackTelegraphProgress01",
        "public bool AttackTrackingLocked",
        "AttackTelegraphProgress01 >= attack.TrackingLock01",
        "public bool IsRecovering",
        "public float RecoveryProgress01",
        "_attackStartedTick = FixedTick",
        "attack == null || attack.TrackingStrength <= 0f || AttackTrackingLocked",
        "_recoveryDurationTicks = Mathf.Max(1, attack.ActiveTicks + attack.RecoveryTicks)",
        "_recoveryStartedTick = FixedTick",
    ):
        assert token in brain

    # External pause shifts the fixed-tick phase anchors, so pausing cannot consume the
    # player's readable warning or punish window.
    pause = brain[brain.index("public void SetExternalPause"):brain.index("private void FixedUpdate()")]
    assert "_attackStartedTick += shift" in pause
    assert "_attackResolveTick += shift" in pause
    assert "_recoveryStartedTick += shift" in pause
    assert "_recoverUntilTick += shift" in pause

    assert "Time.time" not in brain
    assert "Time.deltaTime" not in brain


def test_archetypes_commit_aim_at_distinct_but_dodgeable_points():
    brain = read("Journey", "JourneyEnemyController.cs")

    # Ranged attacks track long enough to communicate intent but freeze well before fire.
    assert '"shard_bolt", EnemyAttackType.Projectile' in brain
    assert "0.72f, 0.66f, 7f" in brain
    assert '"sentry_tracking_bolt", EnemyAttackType.Projectile' in brain
    assert "0.78f, 0.62f, 7.5f" in brain
    assert '"sentry_fan_burst", EnemyAttackType.Burst' in brain
    assert "0.46f, 0.58f, 5.5f" in brain

    # Fast melee commits earlier than a deliberately delayed overhead.
    assert '"penitent_fast_slash", EnemyAttackType.Melee' in brain
    assert "0.20f, 0.50f, 9.5f" in brain
    assert '"penitent_delayed_overhead", EnemyAttackType.Melee' in brain
    assert "0.34f, 0.60f, 14f" in brain


def test_resolved_attacks_use_the_locked_direction_not_live_player_position_for_aim():
    brain = read("Journey", "JourneyEnemyController.cs")

    melee = brain[brain.index("private void ResolveMelee"):brain.index("private void ResolveProjectile")]
    projectile = brain[brain.index("private void ResolveProjectile"):brain.index("private Vector3 ResolveProjectileAimDirection")]

    assert "Vector3.Angle(_lockedAttackDirection, delta.normalized)" in melee
    assert "_lockedProjectileDirection.sqrMagnitude > 0.001f" in projectile
    assert "_lockedProjectileDirection.normalized" in projectile
    assert "ResolveProjectileAimDirection()" in projectile  # fail-safe only if no lock exists


def test_intent_vfx_reads_fixed_tick_phase_and_marks_commit_then_recovery():
    vfx = read("Presentation", "JourneyEnemyIntentVfx.cs")

    for token in (
        'new GameObject("IntentTelegraphV2")',
        "controller.AttackTelegraphProgress01",
        "controller.AttackTrackingLocked",
        "_attack.TrackingLock01",
        "committedWidthMultiplier = 1.65f",
        "controller.IsRecovering",
        "controller.RecoveryProgress01",
        "DrawRecoveryRing()",
        "recoveryRingRadius = 0.78f",
        "SetWidths(",
    ):
        assert token in vfx

    # Render time may animate a small breath, but it cannot advance attack/recovery phase.
    assert "Time.unscaledTime" in vfx
    assert "_startedAt" not in vfx
    assert "_until" not in vfx
    assert "Time.unscaledTime >=" not in vfx

    for forbidden in (
        "private void FixedUpdate()",
        "ReceiveDamage(",
        "RequestDash(",
        "RequestJump(",
        "TryLightAttack(",
        "FirePulse(",
        "TryApply(",
        "NeuralEvent",
        "UdpNeuralReceiver",
        "VepAuraStimulus",
    ):
        assert forbidden not in vfx
