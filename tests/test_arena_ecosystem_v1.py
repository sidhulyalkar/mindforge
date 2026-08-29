from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
UNITY = ROOT / "unity" / "Assets" / "Mindforge"


def read(*parts: str) -> str:
    return UNITY.joinpath(*parts).read_text(encoding="utf-8")


def test_arena_ecosystem_uses_existing_fixed_tick_enemy_authority_for_new_roles():
    ecosystem = read("Editor", "NullWardArenaEcosystemBuilder.cs")
    controller = read("Journey", "JourneyEnemyController.cs")

    for token in (
        'RootName = "Mindforge_NullWard_ArenaEcosystem_V1"',
        '"Causeway_RiftHollow_A"',
        '"Causeway_RiftHollow_B"',
        '"Market_Shardsinger"',
        '"Court_SignalWarden"',
        '"Court_AetherNeedle"',
        "JourneyEnemyArchetype.Hollow",
        "JourneyEnemyArchetype.Shardcaster",
        "JourneyEnemyArchetype.SignalWarden",
        "causeway.enemies = AppendLive(causeway.enemies, hollowA, hollowB)",
        "market.enemies = AppendLive(market.enemies, marketCaster)",
        'id = CourtZoneId',
        'title = "FRACTURE COURT"',
        "requiredForProtocol = true",
        "enemies = new[] { warden, needle }",
        "SetDirectorZones(director, expanded)",
    ):
        assert token in ecosystem

    assert "new Vector3(5.35f, 1.35f, -27.2f)" in ecosystem
    assert "new Vector3(-3.65f, 1.72f, -19.55f)" in ecosystem

    for token in (
        "private void FixedUpdate()",
        "ChooseAttack(distance, toPlayer)",
        "BeginAttack(attackIndex, toPlayer)",
        "ResolvePendingAttack()",
        "ResolveMelee(attack)",
        "ResolveProjectile(attack)",
    ):
        assert token in controller

    assert "private void FixedUpdate()" not in ecosystem
    for forbidden in (
        "ReceiveDamage(new DamagePacket",
        "FirePulse(",
        "TryLightAttack(",
        "TryApply(",
        "NeuralEvent",
        "UdpNeuralReceiver",
        "VepAuraStimulus",
    ):
        assert forbidden not in ecosystem


def test_fracture_court_is_inserted_before_protocol_as_a_required_third_stage():
    ecosystem = read("Editor", "NullWardArenaEcosystemBuilder.cs")
    director = read("World", "NullWardEncounterDirector.cs")

    for token in (
        'CourtZoneId = "fracture_court"',
        'Marker("FractureCourt_EncounterTrigger"',
        "new Vector3(0f, 0f, -22.15f)",
        "activationRadius = 4.15f",
        "ReplaceCourtZone(current, court)",
        'GetField("zones", BindingFlags.Instance | BindingFlags.NonPublic)',
        "field.SetValue(director, zones ?? Array.Empty<NullWardEncounterZone>())",
    ):
        assert token in ecosystem

    assert "if (!_protocolUnlocked && RequiredZonesCleared()) UnlockProtocol();" in director
    assert "if (zone.started && !zone.cleared && IsZoneCleared(zone))" in director
    assert "if (zone == null || !zone.requiredForProtocol) continue;" in director


def test_enemy_collider_profiles_match_archetype_visual_mass_without_new_hit_authority():
    profiles = read("Editor", "NullWardEnemyColliderProfileBuilder.cs")

    for token in (
        "ecosystem.GetComponentsInChildren<JourneyEnemyController>(true)",
        "enemy.GetComponent<CapsuleCollider>()",
        "float scale = Mathf.Clamp(core.localScale.x / 0.30f, 0.50f, 1.80f)",
        "case JourneyEnemyArchetype.Hollow:",
        "radius = 0.38f",
        "height = 1.18f",
        "case JourneyEnemyArchetype.Shardcaster:",
        "radius = needle ? 0.34f : 0.46f",
        "case JourneyEnemyArchetype.SignalWarden:",
        "radius = 0.61f",
        "height = 2.12f",
        "collider.radius = radius * scale",
        "collider.height = Mathf.Max(collider.radius * 2.05f, height * scale)",
    ):
        assert token in profiles

    for forbidden in (
        "AddComponent<CapsuleCollider>",
        "AddComponent<Rigidbody>",
        "ReceiveDamage(",
        "FirePulse(",
        "TryLightAttack(",
        "TryApply(",
        "NeuralEvent",
        "VepAuraStimulus",
    ):
        assert forbidden not in profiles


def test_enemy_intent_vfx_exposes_spatial_attack_geometry_without_combat_authority():
    vfx = read("Presentation", "JourneyEnemyIntentVfx.cs")

    for token in (
        "controller.AttackSelected += OnAttackSelected",
        "controller.AttackResolved += OnAttackResolved",
        "EnemyAttackDefinition _attack",
        "DrawAttackShape(attack)",
        "DrawMeleeArc(attack)",
        "DrawProjectileFan(attack, 1)",
        "DrawProjectileFan(attack, Mathf.Clamp(attack.ProjectileCount, 2, _rays.Length))",
        "DrawRetreatRing()",
        "attack.MaximumRange",
        "attack.MaximumFacingAngle",
        "attack.ProjectileSpreadDegrees",
        "LineRenderer[] _rays = new LineRenderer[5]",
        "Time.unscaledTime",
        "line.shadowCastingMode = ShadowCastingMode.Off",
    ):
        assert token in vfx

    for forbidden in (
        "private void FixedUpdate()",
        ".ReceiveDamage(",
        ".Arm(",
        ".Disarm(",
        ".SetExternalPause(",
        ".RequestDash(",
        ".RequestJump(",
        ".TryLightAttack(",
        ".FirePulse(",
        ".TryApply(",
        "NeuralEvent",
        "UdpNeuralReceiver",
        "VepAuraStimulus",
    ):
        assert forbidden not in vfx


def test_set_dressing_adds_near_mid_far_depth_without_new_collision_or_authority():
    dressing = read("Editor", "NullWardArenaSetDressingV3Builder.cs")

    for token in (
        'WardRootName = "Mindforge_NullWard_SetDressing_V3"',
        'ArenaBackdropRootName = "Mindforge_Arena_Backdrop_V1"',
        'Zone(parent, "Set_MemoryForge")',
        'Zone(parent, "Set_Causeway")',
        'Zone(parent, "Set_Market")',
        'Zone(parent, "Set_FractureCourt")',
        'Zone(parent, "Set_Cathedral")',
        '"Forge_Cradle_',
        '"Causeway_SideTower_',
        '"Market_ArchiveDesk_',
        '"Court_OverheadLintel"',
        '"Cathedral_FloatingFracture_',
        '"Arena_DistantTower_',
        "CreateCable(",
        "CreateCircle(",
        "UnityEngine.Object.DestroyImmediate(collider)",
        "light.shadows = LightShadows.None",
        'RequireMaterial("ArenaBasalt")',
        'RequireMaterial("ObsidianArchitecture")',
        'RequireMaterial("GuardianMetal")',
        'RequireMaterial("AetherCyan")',
        'RequireMaterial("WispVerdant")',
        'RequireMaterial("FracturedRing")',
        'RequireMaterial("FracturedCore")',
    ):
        assert token in dressing

    for forbidden in (
        "AddComponent<BoxCollider>",
        "AddComponent<CapsuleCollider>",
        "AddComponent<Rigidbody>",
        "CombatantVitals",
        "GuardianMotor",
        "JourneyEnemyController",
        "ReceiveDamage(",
        "TryApply(",
        "NeuralEvent",
        "VepAuraStimulus",
    ):
        assert forbidden not in dressing


def test_showcase_rebuild_is_one_click_and_orders_gameplay_before_presentation_layers():
    menu = read("Editor", "ShowcaseEditorMenu.cs")

    steps = (
        "ArenaEnvironmentV3Builder.BuildOpenScene();",
        "NullWardSceneBuilder.BuildOpenScene();",
        "NullWardArenaEcosystemBuilder.ApplyOpenScene();",
        "NullWardEnemyColliderProfileBuilder.ApplyOpenScene();",
        "NullWardEnemySilhouetteV3Builder.ApplyOpenScene();",
        "NullWardVisualInfrastructureBuilder.ApplyOpenScene();",
        "NullWardArenaSetDressingV3Builder.ApplyOpenScene();",
        "NullWardTraversalPlayabilityBuilder.ApplyOpenScene();",
        "CompetitionGateValidator.ValidateAndWrite(false);",
        "PresentationBudgetAudit.Run();",
    )
    indices = [menu.index(step) for step in steps]
    assert indices == sorted(indices)

    assert "Five ordinary enemy" in menu
    assert "persistent shortcut" in menu
    assert "geometric intent telegraphs" in menu
    assert "Layered near/mid/far set dressing" in menu
