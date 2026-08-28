from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
UNITY = ROOT / "unity" / "Assets" / "Mindforge"


def read(*parts: str) -> str:
    return UNITY.joinpath(*parts).read_text(encoding="utf-8")


def test_first_journey_plan_has_explicit_learning_and_promotion_gates():
    plan = (ROOT / "docs" / "FIRST_JOURNEY_VERTICAL_SLICE_PLAN.md").read_text(encoding="utf-8")

    for token in (
        "The Listening Cavern",
        "The Ruined House",
        "The Cellar Passage",
        "The Warden Chamber",
        "The Fractured Signal Threshold",
        "Hands own precision. The brain owns transformation.",
        "Control gate",
        "Combat gate",
        "Camera gate",
        "Visual gate",
        "BCI gate",
        "Evidence gate",
    ):
        assert token in plan


def test_target_lock_v2_discovers_cycles_and_reacquires_only_conventional_enemies():
    lock = read("Combat", "GuardianTargetLock.cs")

    for token in (
        "FindObjectsOfType<CombatantVitals>(true)",
        "CombatTeam.Enemy",
        "AcquireBestTarget",
        "TargetChanged",
        "Cycle(int direction)",
        "KeyCode.LeftArrow",
        "KeyCode.RightArrow",
        "Input.mouseScrollDelta.y",
        "HasLineOfSight",
        "ReacquireOrUnlock",
        "maximumAcquireAngle = 105f",
        "distanceScoreWeight = 0.42f",
        "angleScoreWeight = 0.58f",
    ):
        assert token in lock

    for forbidden in (
        "NeuralEvent",
        "UdpNeuralReceiver",
        "DualAuraCombatDirector",
        "TryApply(",
        "FirePulse(",
        "RequestDash(",
        "SetGuardHeld(",
    ):
        assert forbidden not in lock


def test_reflection_and_wisp_systems_follow_current_player_owned_target():
    resolver = read("Combat", "CombatTargetResolver.cs")
    combat = read("Combat", "GuardianCombatController.cs")
    bloom = read("Combat", "GravityBloomAbility.cs")
    sword = read("Combat", "GuardianSwordShieldController.cs")
    wisp = read("SoulWisp", "SoulWispController.cs")

    assert "GuardianTargetLock targetLock" in resolver
    assert "CombatTargetResolver.Resolve(targetLock, primaryTarget)" in combat
    assert "CombatTargetResolver.Resolve(targetLock, primaryTarget)" in bloom
    assert "CombatTargetResolver.Resolve(targetLock, primaryTarget)" in sword
    assert "CombatTargetResolver.FindEnemyNear(attackerPosition, 2.6f)" in sword

    assert "targetLock.TargetChanged += OnTargetChanged" in wisp
    assert "Transform activeTarget = EffectiveTarget" in wisp
    assert "PlaceStableLockedTargets(activeTarget)" in wisp
    assert "VepAuraStimulus" in wisp
    assert "coded VEP luminance remains owned by VepAuraStimulus" in wisp


def test_journey_enemy_authority_is_readable_reusable_and_uses_existing_defense_rules():
    enemy = read("Journey", "JourneyEnemyController.cs")

    for token in (
        "JourneyEnemyArchetype.Hollow",
        "JourneyEnemyArchetype.Shardcaster",
        "JourneyEnemyArchetype.SignalWarden",
        "AttackTelegraphed?.Invoke",
        "ResolvePendingAttack",
        "playerMotor.IsInvulnerable",
        "playerDefense.TryResolveIncomingStrike",
        "MindforgeProjectile p = Instantiate",
        "CombatTeam.Enemy",
        "ResolveDependencies",
        "player.GetComponent<GuardianSwordShieldController>()",
        "Defeated?.Invoke(this)",
    ):
        assert token in enemy

    # One pending attack at a time is the readability contract for the teaching enemies.
    assert "JourneyEnemyAttackKind _pendingAttack" in enemy
    assert "if (_pendingAttack != JourneyEnemyAttackKind.None)" in enemy
    assert "_pendingAttack = JourneyEnemyAttackKind.None" in enemy

    for forbidden in (
        "NeuralEvent",
        "UdpNeuralReceiver",
        "DualAuraCombatDirector",
        "SetLocked(",
        "Input.GetKey",
    ):
        assert forbidden not in enemy


def test_journey_enemy_presentation_cannot_issue_combat_or_neural_authority():
    presentation = read("Journey", "JourneyEnemyPresentation.cs")

    assert "AttackTelegraphed += OnAttackTelegraphed" in presentation
    assert "AttackResolved += OnAttackResolved" in presentation
    assert "MaterialPropertyBlock" in presentation
    assert "telegraphRing" in presentation

    for forbidden in (
        "ReceiveDamage(",
        "TryResolveIncomingStrike(",
        "MindforgeProjectile",
        "RequestDash(",
        "SetLocked(",
        "NeuralEvent",
        "UdpNeuralReceiver",
        "TryApply(",
    ):
        assert forbidden not in presentation


def test_journey_progression_requires_stage_clear_before_boss_threshold():
    director = read("Journey", "FirstJourneyDirector.cs")

    for token in (
        "JourneyEncounterStage[] stages",
        "BeginStage(_currentStage)",
        "IsStageCleared(stage)",
        "stage.exitGate?.SetOpen(true)",
        "_bossUnlocked = true",
        "IsNear(bossActivationPoint, bossActivationRadius)",
        "StartBossEncounter()",
        "bossRoot.SetActive(true)",
        "bossSeal?.SetOpen(false)",
        "journeyStart",
        "EnterJourneyStart()",
        "body.position = journeyStart.position",
        "Physics.SyncTransforms()",
    ):
        assert token in director

    initialize = director[director.index("private void InitializeJourney()") : director.index("private void EnterJourneyStart()")]
    assert "bossRoot.SetActive(false)" in initialize

    for forbidden in (
        "CalibrationReady = true",
        "NeuralEvent(",
        "ReceiveDamage(",
        "FirePulse(",
        "RequestDash(",
    ):
        assert forbidden not in director


def test_authored_route_contains_all_teaching_spaces_and_keeps_final_arena_in_place():
    builder = read("Editor", "FirstJourneySceneBuilder.cs")
    menu = read("Editor", "ShowcaseEditorMenu.cs")

    for token in (
        'RootName = "Mindforge_First_Journey_V1"',
        "BuildCavern(",
        "BuildRuinedHouse(",
        "BuildCellar(",
        "BuildWardenChamber(",
        "BuildFinalApproach(",
        '"Cavern_Hollow_A"',
        '"Cavern_Hollow_B"',
        '"House_Shardcaster"',
        '"Cellar_Shardcaster"',
        '"Signal_Warden"',
        '"BossActivationTrigger"',
        "boss.SetActive(false)",
    ):
        assert token in builder

    assert builder.index("BuildCavern(") < builder.index("BuildRuinedHouse(")
    assert builder.index("BuildRuinedHouse(") < builder.index("BuildCellar(")
    assert builder.index("BuildCellar(") < builder.index("BuildWardenChamber(")
    assert builder.index("BuildWardenChamber(") < builder.index("BuildFinalApproach(")

    assert "ArenaEnvironmentV3Builder.BuildOpenScene();" in menu
    assert "FirstJourneySceneBuilder.BuildOpenScene();" in menu
    assert menu.index("ArenaEnvironmentV3Builder.BuildOpenScene();") < menu.index("FirstJourneySceneBuilder.BuildOpenScene();")


def test_journey_hud_and_combat_hud_reveal_information_at_the_right_scale():
    journey_hud = read("Journey", "FirstJourneyHud.cs")
    combat_hud = read("Presentation", "CombatStateHud.cs")

    assert "CurrentObjective" in journey_hud
    assert "T lock · ←/→ or wheel cycle" in journey_hud
    assert "CONTROLLER-ONLY PREVIEW · neural authority disabled" in journey_hud

    assert "FirstJourneyDirector journey" in combat_hud
    assert "CombatWorldOpen()" in combat_hud
    assert "BossHudVisible()" in combat_hud
    assert "if (BossHudVisible()) DrawBossState();" in combat_hud
    assert "DrawPlayerState();" in combat_hud
    assert "DrawStrategicState();" in combat_hud

    for source in (journey_hud, combat_hud):
        for forbidden in (
            "ReceiveDamage(",
            "RequestDash(",
            "FirePulse(",
            "TryLightAttack(",
            "SetLocked(",
            "TryApply(",
        ):
            assert forbidden not in source


def test_new_serialized_unity_scripts_have_pinned_meta_guids():
    paths = (
        UNITY / "Combat" / "CombatTargetResolver.cs.meta",
        UNITY / "Journey" / "JourneyEnemyController.cs.meta",
        UNITY / "Journey" / "JourneyEnemyPresentation.cs.meta",
        UNITY / "Journey" / "JourneyGate.cs.meta",
        UNITY / "Journey" / "FirstJourneyDirector.cs.meta",
        UNITY / "Journey" / "FirstJourneyHud.cs.meta",
        UNITY / "Editor" / "FirstJourneySceneBuilder.cs.meta",
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
