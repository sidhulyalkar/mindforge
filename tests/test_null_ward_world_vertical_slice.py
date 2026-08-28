from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
UNITY = ROOT / "unity" / "Assets" / "Mindforge"


def read(*parts: str) -> str:
    return UNITY.joinpath(*parts).read_text(encoding="utf-8")


def test_null_ward_builder_authors_interconnected_vertical_slice_and_real_encounters():
    builder = read("Editor", "NullWardSceneBuilder.cs")

    for token in (
        'RootName = "Mindforge_Null_Ward_V1"',
        "BuildMemoryForge(",
        "BuildCauseway(",
        "BuildMarket(",
        "BuildMaintenanceLoop(",
        "BuildCathedralApproach(",
        '"Memory_Forge_Checkpoint"',
        '"Causeway_NullSentry_A"',
        '"Causeway_NullSentry_B"',
        '"Market_ChromePenitent"',
        '"Market_FracturedEcho"',
        '"MemoryConduit_Shortcut"',
        '"Protocol_Veil"',
        '"Cathedral_Boss_Seal"',
        "JourneyEnemyArchetype.NullSentry",
        "JourneyEnemyArchetype.ChromePenitent",
        "echo.ConfigureWorldEcho",
        "NullWardEncounterZone[] zones",
        'id = "synapse_causeway"',
        'id = "null_market"',
        "requiredForProtocol = true",
        "NullWardEncounterDirector director",
        "MemoryForgeCheckpoint checkpoint",
        "WorldShortcut shortcut",
        "boss.SetActive(false)",
    ):
        assert token in builder

    assert builder.index("BuildMemoryForge(") < builder.index("BuildCauseway(")
    assert builder.index("BuildCauseway(") < builder.index("BuildMarket(")
    assert builder.index("BuildMarket(") < builder.index("BuildMaintenanceLoop(")
    assert builder.index("BuildMaintenanceLoop(") < builder.index("BuildCathedralApproach(")

    # The side path really rejoins the Forge instead of being decorative scenery.
    assert '"Maintenance_EastRun"' in builder
    assert '"Maintenance_SouthRun"' in builder
    assert '"memory_forge_market_loop"' in builder


def test_showcase_shipping_path_builds_null_ward_after_arena_environment():
    menu = read("Editor", "ShowcaseEditorMenu.cs")

    assert "ArenaEnvironmentV3Builder.BuildOpenScene();" in menu
    assert "NullWardSceneBuilder.BuildOpenScene();" in menu
    assert menu.index("ArenaEnvironmentV3Builder.BuildOpenScene();") < menu.index("NullWardSceneBuilder.BuildOpenScene();")
    assert "FirstJourneySceneBuilder.BuildOpenScene();" not in menu
    assert "Memory Forge" in menu
    assert "Synapse Causeway" in menu
    assert "Null Market" in menu
    assert "persistent shortcut" in menu


def test_world_authority_requires_zone_completion_before_protocol_and_boss_threshold():
    world = read("World", "NullWardEncounterDirector.cs")

    for token in (
        "NullWardEncounterZone[] zones",
        "RequiredZonesCleared()",
        "UnlockProtocol()",
        "protocolVeil?.SetOpen(true)",
        "_protocolUnlocked && !_bossStarted",
        "IsNear(bossActivationPoint, bossActivationRadius)",
        "StartBossEncounter()",
        "bossRoot.SetActive(true)",
        "bossSeal?.SetOpen(false)",
        'markers?.Emit("PROTOCOL_VEIL_OPENED"',
        'markers?.Emit("BOSS_THRESHOLD_CROSSED"',
    ):
        assert token in world

    # Dormant future encounters cannot leak into conventional target-lock discovery.
    assert "enemy.gameObject.SetActive(false)" in world
    assert "echo.gameObject.SetActive(false)" in world
    assert "enemy.gameObject.SetActive(true)" in world
    assert "echo.gameObject.SetActive(true)" in world


def test_memory_forge_reconstructs_physical_state_but_never_calibration():
    checkpoint = read("World", "MemoryForgeCheckpoint.cs")
    world = read("World", "NullWardEncounterDirector.cs")
    enemy = read("Journey", "JourneyEnemyController.cs")
    echo = read("Combat", "FracturedEchoNode.cs")

    for token in (
        "world?.ResetForCheckpoint()",
        "playerVitals?.ResetForCheckpoint(true)",
        "guardIntegrity?.ResetFull()",
        "targetLock?.SetLocked(false)",
        "respawnPoint.position",
        'markers?.Emit("CHECKPOINT_RESPAWN"',
        'markers?.Emit("CHECKPOINT_REST"',
    ):
        assert token in checkpoint

    assert "enemy.ConfigureCheckpointLifecycle(true)" in world
    assert "enemy.ResetForCheckpoint()" in world
    assert "echo.ResetForCheckpoint()" in world
    assert "ConfigureCheckpointLifecycle(bool checkpointResettable)" in enemy
    assert "_rngState = deterministicSeed" in enemy
    assert "ConfigureWorldEcho" in echo
    assert "destroyOnShatter = false" in echo

    combined = "\n".join((checkpoint, world, enemy, echo))
    for forbidden in (
        "AwakeningCalibrationDirector",
        "CalibrationReady =",
        "calibration_id =",
        "UdpNeuralReceiver",
        "NeuralEvent",
        "VepAuraStimulus",
    ):
        assert forbidden not in combined


def test_shortcut_is_conventional_persistent_world_state_not_bci_authority():
    shortcut = read("World", "WorldShortcut.cs")
    world = read("World", "NullWardEncounterDirector.cs")

    assert "KeyCode.G" in shortcut
    assert "Input.GetKeyDown(interactKey)" in shortcut
    assert "gate?.SetOpen(true)" in shortcut
    assert 'markers?.Emit("SHORTCUT_UNLOCKED"' in shortcut
    assert "bool _unlocked" in shortcut

    # Checkpoint/world reset deliberately has no reference to the shortcut, so opening
    # it persists for the current run while ordinary encounter state reconstructs.
    reset_slice = world[world.index("public void ResetForCheckpoint()") : world.index("public void SetExternalPause")]
    assert "WorldShortcut" not in reset_slice
    assert "shortcut" not in reset_slice.lower()

    for forbidden in (
        "NeuralEvent",
        "UdpNeuralReceiver",
        "DualAuraCombatDirector",
        "AuraBuffController",
        "VepAuraStimulus",
        "TryLightAttack(",
        "RequestDash(",
        "FirePulse(",
    ):
        assert forbidden not in shortcut


def test_world_echo_uses_fixed_tick_gameplay_and_only_render_time_cosmetic_spin():
    echo = read("Combat", "FracturedEchoNode.cs")

    assert "private void FixedUpdate()" in echo
    assert "_nextFireTick" in echo
    assert "Time.fixedDeltaTime" in echo
    assert "FixedTick < _nextFireTick" in echo
    assert "private void Update()" in echo
    assert "Cosmetic only" in echo

    fixed = echo[echo.index("private void FixedUpdate()") : echo.index("private void Update()")]
    assert "Time.deltaTime" not in fixed
    assert "Time.time" not in fixed


def test_null_ward_scene_builder_has_pinned_unity_guid():
    meta = read("Editor", "NullWardSceneBuilder.cs.meta")
    assert "fileFormatVersion: 2" in meta
    guid = next(line for line in meta.splitlines() if line.startswith("guid: ")).split(":", 1)[1].strip()
    assert len(guid) == 32
