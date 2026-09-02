from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
OVERLAY = ROOT / "dragonsouls_overlay" / "Assets" / "Mindforge"
FLOW = OVERLAY / "Runtime" / "MindforgeShowcaseFlowV32.cs"
CHECKPOINT = OVERLAY / "Runtime" / "MindforgeShowcaseStageCheckpointV32.cs"
GRAMMAR = OVERLAY / "Runtime" / "MindforgeWorldGrammarV32.cs"
ENCOUNTERS = OVERLAY / "Runtime" / "MindforgeEncounterLibraryV32.cs"
BUILDER = OVERLAY / "Editor" / "MindforgeShowcaseIntroBuilderV32.cs"
V31_BUILDER = OVERLAY / "Editor" / "MindforgeVerticalSliceBuilderV31.cs"


def read(path: Path) -> str:
    assert path.exists(), f"missing V0.32 source: {path}"
    return path.read_text(encoding="utf-8")


def test_v32_flow_has_complete_showcase_arc_without_combat_authority():
    text = read(FLOW)
    for token in (
        'ProductVersion = "V0.32 Showcase Intro"',
        "Awakening",
        "MemoryForge",
        "BladeTraining",
        "FirstEncounter",
        "BciReveal",
        "SightPuzzle",
        "Traversal",
        "EliteEncounter",
        "BossApproach",
        "BossFight",
        "WorldReveal",
        "FirstSwingWindow",
        "FirstSwordHit",
        "BciOrbRevealed",
        "MindforgeSwordCombatAssuranceV31",
        "ObserveStageArrival",
        "ObserveMilestone",
    ):
        assert token in text

    for forbidden in (
        "TakeDamage(",
        "ChangeState(",
        "CharacterController.Move",
        "NavMeshAgent.destination",
        "StartAttack(",
        "StopAttack(",
        "Time.timeScale",
    ):
        assert forbidden not in text


def test_v32_bci_orb_is_hidden_for_awakening_and_revealed_at_neural_stage():
    text = read(FLOW)
    assert 'camera.transform.Find("Mindforge_BCI_Orb_V31")' in text
    assert "SetBciVisualVisible(false)" in text
    assert "stage >= MindforgeShowcaseStageV32.BciReveal" in text
    assert "SetBciVisualVisible(true)" in text


def test_v32_stage_checkpoints_are_distance_observers_with_no_physics_or_combat_authority():
    text = read(CHECKPOINT)
    for token in (
        "PlayerStateMachine",
        "activationRadius = 5.5f",
        "maximumVerticalDelta = 5f",
        "delta.sqrMagnitude > activationRadius * activationRadius",
        "ObserveStageArrival(stage)",
        "Configure(MindforgeShowcaseStageV32 configuredStage, float radius = 5.5f)",
    ):
        assert token in text
    for forbidden in (
        "OnTriggerEnter(",
        "GetComponent<Collider>",
        "GetComponentsInChildren<Collider>",
        "AddComponent<Collider>",
        "AddComponent<BoxCollider>",
        "AddComponent<SphereCollider>",
        "AddComponent<CapsuleCollider>",
        "GetComponent<Rigidbody>",
        "AddComponent<Rigidbody>",
        "TakeDamage(",
        "ChangeState(",
        "transform.position =",
        "CharacterController.Move",
    ):
        assert forbidden not in text


def test_v32_builder_derives_from_v31_and_adds_nine_collider_free_semantic_checkpoints():
    text = read(BUILDER)
    for token in (
        "SourceScene = MindforgeVerticalSliceBuilderV31.DestinationScene",
        'DestinationScene = "Assets/Mindforge/Scenes/MindforgeShowcaseIntroV32.unity"',
        "MindforgeVerticalSliceBuilderV31.Build(refresh: refresh)",
        "MindforgeShowcaseFlowV32",
        "ExpectedCheckpointCount = 9",
        "CheckpointFractions",
        "CheckpointStages",
        "NavMesh.CalculatePath",
        "MindforgeShowcaseStageCheckpointV32",
        "observer.Configure(stage, 5.5f)",
        "GetComponentsInChildren<Collider>(true).Length != 0",
        "GetComponentsInChildren<Rigidbody>(true).Length != 0",
    ):
        assert token in text
    assert text.count("MindforgeShowcaseStageV32.") >= 9
    for forbidden in (
        "AddComponent<BoxCollider>",
        "AddComponent<Rigidbody>",
        "GameObject.CreatePrimitive",
        "Random.Range",
        "Random.value",
        "BuildNavMesh",
        "NavMeshBuilder",
        "TakeDamage(",
    ):
        assert forbidden not in text


def test_v32_world_grammar_has_six_regions_reusable_chunk_socket_vocabulary_and_scale_contracts():
    text = read(GRAMMAR)
    for token in (
        "MindforgeRegionIdV32",
        "Sanctum Reliquary",
        "Neural Cloister",
        "Fracture Caverns",
        "Memory Gardens",
        "Signal Foundry",
        "Abyssal Archive",
        "MindforgeChunkKindV32",
        "Entry",
        "Hub",
        "Corridor",
        "Vertical",
        "ArenaSmall",
        "ArenaMedium",
        "Boss",
        "Vista",
        "Puzzle",
        "Shrine",
        "Secret",
        "Transition",
        "MindforgeSocketKindV32",
        "Enemy",
        "Loot",
        "Landmark",
        "MinimumGeneralCorridorWidth = 8f",
        "MinimumCombatHallWidth = 14f",
        "MinimumBossArenaDiameter = 32f",
    ):
        assert token in text
    assert text.count("new MindforgeRegionDefinitionV32") == 6


def test_v32_encounter_library_authors_roles_waves_spacing_without_spawning_or_ai_rewrite():
    text = read(ENCOUNTERS)
    for token in (
        "Remnant",
        "Warden",
        "Ranger",
        "Stalker",
        "Resonant",
        "Brute",
        'id = "showcase.first_real_encounter"',
        'id = "showcase.elite_encounter"',
        "maximumSimultaneousAttackers = 1",
        "maximumSimultaneousAttackers = 2",
        "minimumPlayerBreathingRoomMeters",
        "activationDelaySeconds",
        "preferredRangeMeters",
    ):
        assert token in text
    for forbidden in (
        "Instantiate(",
        "Spawn(",
        "ChangeState(",
        "TakeDamage(",
        "NavMeshAgent",
        "Random.Range",
    ):
        assert forbidden not in text


def test_v31_native_pivot_regression_fix_is_inherited_by_v32():
    text = read(V31_BUILDER)
    for token in (
        "AlignBoundaryBoundsToLane",
        "ProjectedHalfExtent",
        "desiredSignedCenter",
        "currentSignedCenter",
        "instance.transform.position += lateral * side * correction",
    ):
        assert token in text
