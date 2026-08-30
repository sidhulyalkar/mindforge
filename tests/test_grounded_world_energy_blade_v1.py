from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
UNITY = ROOT / "unity" / "Assets" / "Mindforge"


def read(*parts: str) -> str:
    return UNITY.joinpath(*parts).read_text(encoding="utf-8")


def test_grounded_world_has_continuous_floor_hard_perimeter_and_modular_vertical_grammar():
    world = read("Editor", "GroundedWorldV1Builder.cs")

    for token in (
        'RootName = "Mindforge_GroundedWorld_V1"',
        "MinX = -38f",
        "MaxX = 38f",
        "MinZ = -78f",
        "MaxZ = 31f",
        "WallHeight = 11.5f",
        'Primitive("WorldBedrock"',
        '"WestWall"',
        '"EastWall"',
        '"SouthWall"',
        '"NorthWall"',
        "BuildTerraceCluster(",
        "CreateArchitecturalTile(",
        "CreateStairRun(",
        "CreateRamp(",
        "BuildBridge(",
        '"MarketSkybridge"',
        '"CourtSkybridge"',
        '"FarSpire_',
        "RenderSettings.fog = true",
        "FogMode.ExponentialSquared",
    ):
        assert token in world

    assert 'new Vector3(width, 1.25f, depth), basalt, true' in world
    assert 'name + "_CollisionShell"' in world
    assert 'name + "_Mass"' in world
    assert 'name + "_Deck"' in world
    assert 'i % 2 == 0 ? obsidian : metal, false' in world

    for forbidden in (
        "CombatantVitals",
        "ReceiveDamage(",
        "TryLightAttack(",
        "RequestDash(",
        "NeuralEvent",
        "UdpNeuralReceiver",
        "VepAuraStimulus",
        "AuraBuffController",
    ):
        assert forbidden not in world


def test_grounded_world_tuning_recesses_underlay_and_pins_diorama_camera_and_roll_profile():
    tuning = read("Editor", "GroundedWorldTuningV1.cs")

    for token in (
        't.name.StartsWith("GroundPlate_", StringComparison.Ordinal)',
        "p.y = -0.16f",
        'Set(camera, "pivotHeight", 1.52f)',
        'Set(camera, "freeDistance", 6.15f)',
        'Set(camera, "lockDistance", 6.75f)',
        'Set(camera, "shoulderOffset", 0.22f)',
        'Set(camera, "gameplayFieldOfView", 52f)',
        'Set(camera, "initialPitch", 26f)',
        'Set(serialized, "dodgeInvulnerabilitySeconds", 0.16f)',
        'Set(serialized, "dashExitVelocityRetention", 0.28f)',
        'Set(serialized, "airDashInvulnerabilitySeconds", 0.075f)',
        "tuning.dashSpeed = 13.6f",
        "tuning.dashDuration = 0.28f",
    ):
        assert token in tuning


def test_world_safety_is_last_resort_recovery_not_a_second_movement_authority():
    safety = read("World", "GuardianWorldSafety.cs")

    for token in (
        "xBounds = new Vector2(-37.2f, 37.2f)",
        "zBounds = new Vector2(-77.2f, 30.2f)",
        "recoveryHeight = -3.0f",
        "motor.IsGrounded",
        "CaptureSafePose()",
        "body.position = fallback",
        "body.velocity = Vector3.zero",
        "body.angularVelocity = Vector3.zero",
    ):
        assert token in safety

    for forbidden in (
        "private void Update()",
        "AddForce(",
        "MovePosition(",
        "RequestDash(",
        "RequestJump(",
        "ReceiveDamage(",
        "NeuralEvent",
        "VepAuraStimulus",
    ):
        assert forbidden not in safety


def test_grounded_input_retires_shield_and_player_pulse_and_makes_evade_primary_defense():
    input_cs = read("Combat", "GuardianCombatInput.cs")
    controls = read("Combat", "GuardianControlProfileV1.cs")

    assert "rightMouseEvades = true" in controls
    assert "Input.GetMouseButtonDown(1)" in controls
    assert "controls.Pressed(GuardianControlAction.EvadeBoost)" in input_cs

    for token in (
        "endurance.DodgeBaseCost",
        "endurance.CanSpend(cost)",
        "QueueDodgeCommand(aim)",
        "TryConsumeQueuedDodge()",
        "motor.RequestDash(_dodgeCommandAim)",
        'endurance?.TrySpend(cost, grounded ? "DODGE_ROLL" : "AIR_DASH")',
        "fire_held = false",
        "guard_held = false",
        "guard_down = false",
        "physicalCombat?.SetGuardHeld(false, aim)",
        "command.fire_held intentionally has no normal-world action",
    ):
        assert token in input_cs

    assert "combat.FirePulse(aim)" not in input_cs
    assert "Input.GetKey(KeyCode.X)" not in input_cs
    assert "Input.GetKey(KeyCode.E)" not in input_cs
    assert "Input.GetMouseButton(1);" not in input_cs

    for forbidden in ("NeuralEvent", "UdpNeuralReceiver", "VepAuraStimulus"):
        assert forbidden not in input_cs


def test_endurance_is_spent_by_rolls_and_recovers_as_a_visible_conventional_budget():
    stamina = read("Combat", "GuardianStamina.cs")
    loadout = read("Combat", "GuardianEquipmentLoadout.cs")

    assert "recoveryPerSecond = 42f" in stamina
    assert "recoveryDelaySeconds = 0.48f" in stamina
    assert "dodgeBaseCost = 22f" in stamina
    assert "public float DodgeBaseCost => dodgeBaseCost" in stamina
    assert "public bool CanSpend(float amount)" in stamina

    total_mass = loadout.split("public float TotalMassKg =>", 1)[1].split("public float EquipCapacityKg", 1)[0]
    assert "mainHand" in total_mass
    assert "armor" in total_mass
    assert "offHand" not in total_mass
    assert 'displayName = "Aetherblade"' in loadout
    assert 'displayName = "Verdant Ward · Legacy"' in loadout


def test_energy_blade_is_a_coherent_white_core_and_resonant_sheath_without_a_physical_shield():
    bootstrap = read("Combat", "PhysicalArsenalBootstrap.cs")
    rig = read("Combat", "GuardianSwordShieldRig.cs")
    controller = read("Combat", "GuardianSwordShieldController.cs")

    for token in (
        '"AetherbladeWhiteCore"',
        '"AetherbladeResonantSheath"',
        '"AetherbladeEnergyScale"',
        '"SwordEnergyTip"',
        "GuardianDodgeRollPresentation",
        "null,\n                null,\n                null",
        "physical.ConfigureRuntime(resonance, flux, target, null, hitStop, tuning)",
    ):
        assert token in bootstrap

    assert 'NewChild("ShieldRoot"' not in bootstrap
    assert "GuardianShieldHitbox shieldHitbox =" not in bootstrap
    assert "CreateShieldOutline(" not in bootstrap

    assert "maxSwordLengthBonus = 0.72f" in rig
    assert "scale.z *= 1f + maxSwordLengthBonus * sight" in rig
    assert "shieldRoot.gameObject.SetActive(guarding)" in rig

    assert "sightReachBonus" in controller
    assert "weapon.reachMeters * (1f + sightReachBonus * resonanceValue)" in controller
    assert "public bool TryLightAttack(Vector3 aimDirection)" in controller


def test_ground_roll_visual_is_downstream_of_authoritative_motor_state():
    roll = read("Presentation", "GuardianDodgeRollPresentation.cs")

    for token in (
        "motor.DashStarted += OnDashStarted",
        "motor.IsAirDashing",
        "motor.IsGrounded",
        'new GameObject("Motion_DodgeRollRoot")',
        "float angle = -360f * eased",
        "visualRollSeconds = 0.28f",
    ):
        assert token in roll

    for forbidden in (
        "private void FixedUpdate()",
        "body.velocity",
        "MovePosition(",
        "RequestDash(",
        "ReceiveDamage(",
        "TryApply(",
        "NeuralEvent",
    ):
        assert forbidden not in roll


def test_health_hud_has_clear_survival_hierarchy_and_suppresses_legacy_instrumentation():
    hud = read("Presentation", "GroundedCombatHud.cs")
    ward_hud = read("World", "NullWardHud.cs")
    qualification = read("Qualification", "ControllerOnlyQualificationBootstrap.cs")

    for token in (
        '"GUARDIAN · CRITICAL"',
        '"ENDURANCE"',
        '"FLUX"',
        "DrawBar(new Rect(x + 14f, y + 34f, width - 28f, 18f)",
        "SuppressLegacyHud()",
        "legacy.enabled = false",
        "SHIFT / RMB ROLL",
    ):
        assert token in hud

    assert "const float top = 148f" in ward_hud
    assert '"SHIFT/RMB roll · SPACE jump ×2 / hold hover · T lock · F/LMB Aetherblade"' in ward_hud
    assert '"SHOWCASE · BCI OFF · {_activationReason}"' in qualification
    assert "new Rect(12f, 12f, 430f, 48f)" not in qualification

    for forbidden in ("ReceiveDamage(", "TrySpend(", "RequestDash(", "TryApply("):
        assert forbidden not in hud


def test_onboarding_and_loadout_teach_one_consistent_small_control_vocabulary():
    guide = read("Presentation", "PlayerAgencyGuide.cs")
    menu = read("Presentation", "GuardianEquipmentMenu.cs")

    for token in (
        "GuardianControlAction.EvadeBoost",
        "GuardianControlAction.Interact",
        "READ → COMMIT → EVADE → REPOSITION",
        "motor.DashStarted += OnDashStarted",
        "interactionRouter.InteractionPerformed += OnInteraction",
    ):
        assert token in guide

    for token in (
        '"GUARDIAN KIT + CONTROLS"',
        '"Endurance Evade"',
        "GuardianControlAction.EvadeBoost",
        '"Evade · air dash · mounted boost"',
        "GuardianControlAction.Interact",
        '"Context: ride · dismount · reconstruct · use world"',
        "ENDURANCE {stamina}",
        '"BLUE / Sight → bounded blade length, energy and damage',
        "GetPrimaryActiveQuest()",
        "GetCurrentStep(quest.id)",
    ):
        assert token in menu

    for obsolete in (
        '"X / MMB", "Pulse Shot"',
        '"RMB / E", "Shield"',
        '"GUARD INTEGRITY',
    ):
        assert obsolete not in menu


def test_one_click_showcase_builds_world_and_tuning_before_population_and_visual_layers():
    menu = read("Editor", "ShowcaseEditorMenu.cs")

    ordered = (
        "ArenaEnvironmentV3Builder.BuildOpenScene();",
        "NullWardSceneBuilder.BuildOpenScene();",
        "GroundedWorldV1Builder.ApplyOpenScene();",
        "GroundedWorldTuningV1.ApplyOpenScene();",
        "NullWardArenaEcosystemBuilder.ApplyOpenScene();",
        "NullWardEnemyColliderProfileBuilder.ApplyOpenScene();",
        "NullWardEnemySilhouetteV3Builder.ApplyOpenScene();",
        "NullWardVisualInfrastructureBuilder.ApplyOpenScene();",
        "NullWardArenaSetDressingV3Builder.ApplyOpenScene();",
        "NullWardTraversalPlayabilityBuilder.ApplyOpenScene();",
        "GameFoundationV1Builder.ApplyOpenScene();",
        "UxInteractionSaveV05Builder.ApplyOpenScene();",
        "CompetitionGateValidator.ValidateAndWrite(false);",
        "PresentationBudgetAudit.Run();",
    )
    indices = [menu.index(token) for token in ordered]
    assert indices == sorted(indices)

    assert "continuous " in menu and "collision-backed basin" in menu
    assert "energy-blade + endurance dodge roll" in menu
    assert "Pulse fire and the physical" in menu
    assert "shield are retired from the normal control surface" in menu
