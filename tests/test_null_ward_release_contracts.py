from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
UNITY = ROOT / "unity" / "Assets" / "Mindforge"


def read(*parts: str) -> str:
    return UNITY.joinpath(*parts).read_text(encoding="utf-8")


def test_fractured_signal_projectile_scheduler_uses_fixed_simulation_ticks():
    boss = read("Combat", "FracturedSignalDirector.cs")

    for token in (
        "WaitForFixedUpdate",
        "WaitCombatTicks",
        "SecondsToTicks",
        "AttackAuthorityAvailable",
        "yield return FixedStep",
        "phaseOneInterval",
        "phaseTwoInterval",
        "phaseThreeInterval",
        "phaseOneTelegraph",
        "phaseTwoTelegraph",
        "phaseThreeTelegraph",
    ):
        assert token in boss

    assert "WaitForSeconds" not in boss
    assert "Time.deltaTime" not in boss
    assert "Time.time" not in boss

    # Signal Break sensory relief is presentation and remains explicitly separate from
    # attack scheduling authority.
    assert "RestStimuli(signalBreakVisualRestSeconds)" in boss


def test_fractured_signal_melee_telegraphs_use_same_fixed_clock():
    melee = read("Combat", "FracturedSignalMeleeDirector.cs")

    assert "WaitForFixedUpdate" in melee
    assert "SecondsToTicks" in melee
    assert "yield return FixedStep" in melee
    assert "yield return WaitTelegraph(telegraph)" in melee
    assert "Time.deltaTime" not in melee
    assert "Time.time" not in melee
    assert "WaitForSeconds" not in melee


def test_guardian_secondary_combat_windows_and_realized_healing_are_fixed_tick():
    combat = read("Combat", "GuardianCombatController.cs")

    for token in (
        "private void FixedUpdate()",
        "_lastShotTick",
        "_lastCleaveTick",
        "_lastCounterTick",
        "_counterUntilTick",
        "SecondsToTicks(tuning.shotCooldown)",
        "SecondsToTicks(tuning.cleaveCooldown)",
        "SecondsToTicks(tuning.counterCooldown)",
        "SecondsToTicks(tuning.counterWindow)",
        "HealingPerSecond * Time.fixedDeltaTime",
        "if (FixedTick < _counterUntilTick) ScanCounterProjectiles()",
    ):
        assert token in combat

    assert "Time.time" not in combat
    assert "Time.deltaTime" not in combat
    assert "private void Update()" not in combat


def test_gravity_bloom_capture_and_pause_windows_are_fixed_tick():
    bloom = read("Combat", "GravityBloomAbility.cs")

    for token in (
        "_pauseStartedTick",
        "_endTick",
        "_lastUseTick",
        "SecondsToTicks(tuning.bloomCooldown)",
        "SecondsToTicks(duration)",
        "if (FixedTick >= _endTick) Detonate()",
        "_endTick += Math.Max(0L, FixedTick - _pauseStartedTick)",
    ):
        assert token in bloom

    assert "Time.time" not in bloom
    assert "Time.deltaTime" not in bloom


def test_null_ward_reuses_shared_cinematic_material_vocabulary():
    builder = read("Editor", "NullWardSceneBuilder.cs")

    assert "CinematicMaterialAuthoring.EnsureAuthored();" in builder
    for material in (
        "ArenaBasalt",
        "ObsidianArchitecture",
        "GuardianMetal",
        "AetherCyan",
        "WispVerdant",
        "FracturedCore",
        "FracturedRing",
    ):
        assert f'RequireMaterial("{material}")' in builder

    # World composition must not fork a private material library that presentation
    # agents then need to skin independently.
    assert "Generated/NullWardV1" not in builder
    assert "AssetDatabase.CreateAsset(material" not in builder


def test_null_ward_prefab_instantiation_stays_on_unity_2022_3_safe_surface():
    builder = read("Editor", "NullWardSceneBuilder.cs")

    assert "PrefabUtility.InstantiatePrefab(prefab) as GameObject" in builder
    assert "instance.transform.SetParent(parent, false)" in builder
    assert "PrefabUtility.InstantiatePrefab(prefab, parent)" not in builder


def test_null_ward_world_and_presentation_never_gain_neural_decision_authority():
    sources = (
        read("World", "MemoryForgeCheckpoint.cs"),
        read("World", "NullWardEncounterDirector.cs"),
        read("World", "WorldShortcut.cs"),
        read("World", "NullWardHud.cs"),
        read("Presentation", "LockOnIndicator.cs"),
    )
    forbidden = (
        "UdpNeuralReceiver",
        "NeuralEvent",
        "VepAuraStimulus",
        "DualAuraCombatDirector",
        "sight_score",
        "guard_score",
        "TryApply(",
        "CalibrationReady =",
    )
    for source in sources:
        for token in forbidden:
            assert token not in source
