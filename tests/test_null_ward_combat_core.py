from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
UNITY = ROOT / "unity" / "Assets" / "Mindforge"


def read(*parts: str) -> str:
    return UNITY.joinpath(*parts).read_text(encoding="utf-8")


def test_guardian_action_authority_is_fixed_tick_and_animation_independent():
    attack = read("Combat", "AttackDefinition.cs")
    sword = read("Combat", "GuardianSwordShieldController.cs")
    motor = read("Combat", "GuardianMotor.cs")

    for token in (
        "startupTicks",
        "activeTicks",
        "recoveryTicks",
        "ComboBufferOpen",
        "MovementMultiplier",
        "TurnMultiplier",
        "CreateDefaultLightChain",
    ):
        assert token in attack

    for token in (
        "GuardianActionState.AttackStartup",
        "GuardianActionState.AttackActive",
        "GuardianActionState.AttackRecovery",
        "GuardianActionState.GuardBreak",
        "public bool CanAttack",
        "public bool CanDodge",
        "public bool CanGuard",
        "public bool CanCounter",
        "_attackCommitEndTick",
        "_attackRecoveryUntilTick",
        "_guardStartedTick",
        "_guardBreakUntilTick",
        "Time.fixedTime",
    ):
        assert token in sword

    assert "Time.time" not in sword
    assert "Animator" not in sword
    assert "AnimationEvent" not in sword

    for token in (
        "_dashUntilTick",
        "_invulnerableUntilTick",
        "_dashQueuedUntilTick",
        "SecondsToTicks",
        "physicalCombat.CanTurn",
        "physicalCombat.TurnMultiplier",
    ):
        assert token in motor
    assert "Time.time" not in motor


def test_enemy_attacks_are_data_not_animation_names():
    definition = read("Enemies", "EnemyAttackDefinition.cs")

    for token in (
        "minimumRange",
        "maximumRange",
        "maximumFacingAngle",
        "weight",
        "cooldownTicks",
        "telegraphTicks",
        "activeTicks",
        "recoveryTicks",
        "trackingStrength",
        "damage",
        "poiseDamage",
        "knockback",
        "projectileSpeed",
        "requiresLineOfSight",
        "presentationId",
        "RangeValid",
        "FacingValid",
    ):
        assert token in definition

    for forbidden in (
        "Animator",
        "AnimationEvent",
        "NeuralEvent",
        "UdpNeuralReceiver",
        "Input.Get",
    ):
        assert forbidden not in definition


def test_enemy_brain_filters_then_uses_stable_weighted_selection():
    brain = read("Journey", "JourneyEnemyController.cs")

    for token in (
        "EnemyAttackDefinition[] attackDefinitions",
        "_attackCooldownUntil",
        "attack.RangeValid(distance)",
        "attack.FacingValid(facingAngle)",
        "attack.RequiresLineOfSight && !HasLineOfSight()",
        "totalWeight += attack.Weight",
        "int roll = NextInt(totalWeight)",
        "_rngState = _rngState * 1664525u + 1013904223u",
        "ComputeStableSeed",
        "Physics.RaycastNonAlloc",
        "FixedTick",
        "Time.fixedDeltaTime",
    ):
        assert token in brain

    # Gameplay selection may not depend on rendered frames, instance IDs, or Unity RNG.
    assert "UnityEngine.Random" not in brain
    assert "Random.Range" not in brain
    assert "GetInstanceID" not in brain
    assert "Time.time" not in brain


def test_null_sentry_and_chrome_penitent_have_distinct_teaching_grammars():
    brain = read("Journey", "JourneyEnemyController.cs")

    for token in (
        "JourneyEnemyArchetype.NullSentry",
        '"sentry_tracking_bolt"',
        '"sentry_fan_burst"',
        '"sentry_retreat_pulse"',
        "JourneyEnemyArchetype.ChromePenitent",
        '"penitent_fast_slash"',
        '"penitent_delayed_overhead"',
        '"penitent_sweep"',
    ):
        assert token in brain

    assert "EnemyAttackType.Projectile" in brain
    assert "EnemyAttackType.Burst" in brain
    assert "EnemyAttackType.Retreat" in brain
    assert "EnemyAttackType.Melee" in brain


def test_new_enemy_authority_cannot_originate_neural_or_player_commands():
    brain = read("Journey", "JourneyEnemyController.cs")
    definition = read("Enemies", "EnemyAttackDefinition.cs")

    forbidden = (
        "NeuralEvent",
        "UdpNeuralReceiver",
        "DualAuraCombatDirector",
        "SetLocked(",
        "RequestDash(",
        "TryLightAttack(",
        "SetGuardHeld(",
        "FirePulse(",
        "BeginCounter(",
        "Input.GetKey",
    )
    for source in (brain, definition):
        for token in forbidden:
            assert token not in source


def test_new_serialized_combat_assets_have_pinned_unity_guids():
    paths = (
        UNITY / "Combat" / "AttackDefinition.cs.meta",
        UNITY / "Enemies" / "EnemyAttackDefinition.cs.meta",
    )
    guids = []
    for path in paths:
        text = path.read_text(encoding="utf-8")
        assert "fileFormatVersion: 2" in text
        line = next(line for line in text.splitlines() if line.startswith("guid: "))
        guid = line.split(":", 1)[1].strip()
        assert len(guid) == 32
        guids.append(guid)
    assert len(guids) == len(set(guids))
