from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
UNITY = ROOT / "unity" / "Assets" / "Mindforge"
DOCS = ROOT / "docs"


def read(*parts: str) -> str:
    return UNITY.joinpath(*parts).read_text(encoding="utf-8")


def test_world_state_snapshot_restore_notifies_derived_systems_without_choosing_storage():
    ledger = read("World", "WorldStateLedger.cs")
    quests = read("World", "WorldQuestRuntime.cs")

    for token in (
        'schema = "mindforge.world_state.v1"',
        "public event Action SnapshotRestored",
        "SnapshotRestored?.Invoke()",
        "entries.Sort((a, b) => string.CompareOrdinal(a.key, b.key))",
    ):
        assert token in ledger

    assert "ledger.SnapshotRestored += OnSnapshotRestored" in quests
    assert "RebuildFromWorld(false)" in quests

    for forbidden in ("PlayerPrefs.", "File.Write", "Application.persistentDataPath", "BinaryFormatter"):
        assert forbidden not in ledger


def test_quest_runtime_is_ordered_prerequisite_graph_and_remains_read_only():
    source = read("World", "WorldQuestRuntime.cs")

    for token in (
        "WorldQuestStepDefinition",
        "prerequisite_ids",
        "WorldQuestRewardDefinition[] rewards",
        "QuestActivated",
        "QuestAdvanced",
        "QuestCompleted",
        "PrerequisitesSatisfied",
        "while (state.current_step < steps.Length && StepSatisfied",
        "definitions.Length + 1",
    ):
        assert token in source

    for forbidden in (
        "ReceiveDamage(",
        "RequestDash(",
        "RequestJump(",
        "TryLightAttack(",
        "SetActive(",
        "Instantiate(",
        "Input.Get",
        "NeuralEvent",
        "UdpNeuralReceiver",
        "VepAuraStimulus",
    ):
        assert forbidden not in source


def test_progression_isolated_and_reward_receipts_make_reconciliation_idempotent():
    progression = read("World", "PlayerProgressionLedger.cs")
    rewards = read("World", "WorldQuestRewardRuntime.cs")

    for token in (
        'schema = "mindforge.player_progression.v1"',
        "reward_receipts",
        "TryClaimRewardReceipt",
        "HasRewardReceipt",
        "AddResonance",
        "AddMastery",
        "Unlock(",
        "SaturatingAdd",
    ):
        assert token in progression

    assert rewards.index("progression.TryClaimRewardReceipt(questId)") < rewards.index("progression.Grant(rewards[i]")
    assert "progression.SnapshotRestored += ReconcileCompletedQuests" in rewards
    assert "QuestCompleted += OnQuestCompleted" in rewards

    for source in (progression, rewards):
        for forbidden in (
            "ReceiveDamage(",
            "RequestDash(",
            "RequestJump(",
            "TryLightAttack(",
            "SetMoveInput(",
            "NeuralEvent",
            "UdpNeuralReceiver",
            "VepAuraStimulus",
        ):
            assert forbidden not in source


def test_semantic_bridge_persists_wave_clear_facts_and_prefix_region_entry():
    source = read("World", "HackathonWorldSemanticBridgeV1.cs")

    for token in (
        "RecordRegionPrefix(playthrough.StageIndex",
        "for (int i = 0; i <= max; i++)",
        '"region." + value.ToString().ToLowerInvariant() + ".entered"',
        'SetInt("encounter.menagerie.waves_cleared", index + 1',
        'SetInt("encounter.menagerie.waves_cleared", 3',
        'SetBool("encounter.menagerie.complete", true',
    ):
        assert token in source

    for forbidden in (
        "ReceiveDamage(",
        "RequestDash(",
        "RequestJump(",
        "TryLightAttack(",
        "enemy.Arm()",
        "SetOpen(",
        "NeuralEvent",
    ):
        assert forbidden not in source


def test_foundation_authors_three_step_journey_rewards_and_six_story_discoveries():
    builder = read("Editor", "GameFoundationV1Builder.cs")

    for quest in (
        'id = "journey.read_aetheria"',
        'id = "journey.menagerie_exam"',
        'id = "journey.reconnect_null_ward"',
        'prerequisite_ids = new[] { "journey.read_aetheria" }',
        'prerequisite_ids = new[] { "journey.menagerie_exam" }',
        'IntAtLeast("encounter.menagerie.waves_cleared", 1)',
        'IntAtLeast("encounter.menagerie.waves_cleared", 2)',
        'Unlock("challenge.menagerie_replay")',
        'Unlock("region.aetheria_frontier")',
    ):
        assert quest in builder

    for story in (
        '"prism_bastion"',
        '"neon_causeway"',
        '"broken_momentum"',
        '"ruined_choir"',
        '"hall_gravitas"',
        '"menagerie_crucible"',
    ):
        assert story in builder

    assert builder.count("Story(parent, guardian, ledger, bus,") == 6


def test_story_beacons_are_collider_free_position_observers_and_restore_safe():
    source = read("World", "WorldStoryBeaconV1.cs")

    for token in (
        "Vector3.ProjectOnPlane(guardian.position - transform.position",
        'string key = "story." + id + ".discovered"',
        "ledger.TryGetBool(key, out bool already)",
        "ledger.SetBool(key, true, \"world_story_discovery\")",
        "WorldSignalKind.StoryDiscovered",
        "ledger.SnapshotRestored += ResolveExistingState",
        "ledger.StateChanged += OnWorldStateChanged",
    ):
        assert token in source

    for forbidden in (
        "Input.Get",
        "AddComponent<Collider>",
        "ReceiveDamage(",
        "RequestDash(",
        "TryLightAttack(",
        "NeuralEvent",
        "VepAuraStimulus",
    ):
        assert forbidden not in source


def test_encounter_contracts_distinguish_candidate_from_ranked_qualification():
    registry = read("World", "EncounterContractRegistry.cs")
    builder = read("Editor", "GameFoundationV1Builder.cs")

    assert "public bool competitive_candidate" in registry
    assert "public bool ranked_eligible" in registry
    assert 'id = "menagerie.crucible"' in builder
    assert 'id = "boss.lord_malatract"' in builder
    assert builder.count("competitive_candidate = true") == 2
    assert builder.count("ranked_eligible = false") >= 2
    assert 'authority_component = "ArenaMenagerieDirector + JourneyEnemyController"' in builder
    assert 'authority_component = "FracturedSignalDirector + FracturedSignalMeleeDirector"' in builder

    for forbidden in ("ReceiveDamage(", "Arm()", "Disarm()", "SetOpen(", "NeuralEvent"):
        assert forbidden not in registry


def test_competitive_observer_records_splits_without_becoming_run_authority():
    source = read("Telemetry", "CompetitiveRunObserverV1.cs")

    for token in (
        "WorldSignalKind.RegionEntered",
        "WorldSignalKind.EncounterWaveCleared",
        "WorldSignalKind.EncounterCleared",
        "WorldSignalKind.BossStarted",
        "WorldSignalKind.WorldCompleted",
        "if (signal.kind == WorldSignalKind.RunSplit) return false",
        'WorldSignalKind.RunSplit,\n                "run.split"',
    ):
        assert token in source

    for forbidden in (
        "Input.Get",
        "ReceiveDamage(",
        "RequestDash(",
        "TryLightAttack(",
        "SetActive(",
        "SetBool(",
        "NeuralEvent",
    ):
        assert forbidden not in source


def test_foundation_hud_is_read_only_and_exposes_current_journey_progress():
    source = read("Presentation", "GameFoundationHudV1.cs")

    for token in (
        "GetPrimaryActiveQuest()",
        "GetCurrentStep(quest.id)",
        "progression.Resonance",
        "progression.Mastery",
        "runObserver.ElapsedSeconds",
        "WorldSignalKind.StoryDiscovered",
    ):
        assert token in source

    for forbidden in (
        "Input.Get",
        "SetBool(",
        "AddResonance(",
        "AddMastery(",
        "ReceiveDamage(",
        "RequestDash(",
        "TryLightAttack(",
        "NeuralEvent",
    ):
        assert forbidden not in source


def test_one_click_builder_binds_foundation_after_final_world_authoring():
    menu = read("Editor", "ShowcaseEditorMenu.cs")
    hackathon = menu.index("HackathonPlaythroughV1Builder.ApplyOpenScene();")
    safety = menu.index("AetheriaDynamicMountSafetyBuilder.ApplyOpenScene();")
    visual = menu.index("NullWardVisualInfrastructureBuilder.ApplyOpenScene();")
    dressing = menu.index("NullWardArenaSetDressingV3Builder.ApplyOpenScene();")
    traversal = menu.index("NullWardTraversalPlayabilityBuilder.ApplyOpenScene();")
    foundation = menu.index("GameFoundationV1Builder.ApplyOpenScene();")
    assert hackathon < safety < visual < dressing < traversal < foundation
    assert "Competitive candidates remain explicitly NOT ranked-qualified" in menu


def test_foundation_docs_define_large_game_and_runtime_acceptance_contracts():
    master = DOCS / "GAME_MASTERPLAN_V1.md"
    foundation = DOCS / "GAME_FOUNDATION_V1.md"
    assert master.exists()
    assert foundation.exists()

    master_text = master.read_text(encoding="utf-8")
    for token in (
        "Hands own precision. The brain owns transformation.",
        "Act I: Aetheria, the Fractured City",
        "Menagerie Time Trial",
        "ranked_eligible",
        "Version 0.5: Interaction + Save Contract",
        "Version 1.0 target",
    ):
        assert token in master_text

    foundation_text = foundation.read_text(encoding="utf-8")
    for token in (
        "Reward idempotence",
        "Region entry is prefix-monotonic",
        "Six proximity-only, collider-free discovery beacons",
        "ranked_eligible = false",
        "Unity acceptance checklist",
    ):
        assert token in foundation_text


def test_new_foundation_unity_scripts_have_unique_guids():
    metas = (
        UNITY / "World" / "PlayerProgressionLedger.cs.meta",
        UNITY / "World" / "WorldQuestRewardRuntime.cs.meta",
        UNITY / "World" / "EncounterContractRegistry.cs.meta",
        UNITY / "World" / "WorldStoryBeaconV1.cs.meta",
        UNITY / "Telemetry" / "CompetitiveRunObserverV1.cs.meta",
        UNITY / "Presentation" / "GameFoundationHudV1.cs.meta",
    )
    guids = []
    for path in metas:
        text = path.read_text(encoding="utf-8")
        assert "fileFormatVersion: 2" in text
        guid = next(line for line in text.splitlines() if line.startswith("guid: ")).split(":", 1)[1].strip()
        assert len(guid) == 32
        guids.append(guid)
    assert len(guids) == len(set(guids))
