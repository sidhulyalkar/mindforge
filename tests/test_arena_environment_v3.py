from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
UNITY = ROOT / "unity" / "Assets" / "Mindforge"


def read(*parts: str) -> str:
    return UNITY.joinpath(*parts).read_text(encoding="utf-8")


def test_arena_v3_is_the_final_coherent_showcase_environment_pass():
    menu = read("Editor", "ShowcaseEditorMenu.cs")
    arena = read("Editor", "ArenaEnvironmentV3Builder.cs")

    assert "ArenaEnvironmentV3Builder.BuildOpenScene();" in menu
    assert menu.index("ShowcaseSceneDecorator.DecorateOpenScene();") < menu.index("CinematicSceneDetailer.EnhanceOpenScene();")
    assert menu.index("CinematicSceneDetailer.EnhanceOpenScene();") < menu.index("ArenaEnvironmentV3Builder.BuildOpenScene();")

    assert 'public const string RootName = "Mindforge_Arena_V3"' in arena
    assert "RemovePrototypeArenaVisuals(arena.transform);" in arena
    assert "ShowcaseSceneDecorator.ShowcaseRootName" in arena
    assert "CinematicSceneDetailer.CinematicRootName" in arena


def test_arena_v3_has_pillar_rhythm_ritual_floor_and_unique_palette():
    arena = read("Editor", "ArenaEnvironmentV3Builder.cs")

    for token in (
        "BuildTieredFloor",
        "BuildHeroPillars",
        "BuildBrokenPillarRhythm",
        "BuildOuterArchitecture",
        "BuildBraziers",
        "BuildRubbleAndFractures",
        "CopperBoundaryOuter",
        "NeuralRingOuter",
        "RitualChannel_",
        "HeroPillar_",
        "BrokenPillar_",
        "NeuralBrazier_",
        "ArenaRuneCyan",
        "ArenaRuneTeal",
        "ArenaCopper",
        "ArenaIndigo",
    ):
        assert token in arena

    # The scene palette is deliberately midnight/indigo + cyan/teal + copper rather
    # than rainbow lighting. These values are intentionally approximate art contracts.
    assert "new Color(0.30f, 0.145f, 0.045f)" in arena
    assert "new Color(0.025f, 0.62f, 1.00f)" in arena
    assert "new Color(0.025f, 1.00f, 0.78f)" in arena
    assert "new Color(0.008f, 0.018f, 0.040f)" in arena


def test_arena_v3_is_render_only_and_cannot_change_combat_authority():
    arena = read("Editor", "ArenaEnvironmentV3Builder.cs")

    assert "UnityEngine.Object.DestroyImmediate(collider);" in arena
    assert "AddComponent<Collider>" not in arena
    assert "AddComponent<Rigidbody>" not in arena
    assert "ReceiveDamage(" not in arena
    assert "RequestDash(" not in arena
    assert "TryLightAttack(" not in arena
    assert "SetGuardHeld(" not in arena
    assert "NeuralEvent" not in arena
    assert "AuraBuffController" not in arena
    assert "FracturedSignalDirector" not in arena


def test_arena_v3_preserves_editor_time_inactive_arena_restore():
    menu = read("Editor", "ShowcaseEditorMenu.cs")

    assert 'EditorSceneLookup.FindIncludingInactive("Fractured_Signal_Arena")' in menu
    assert "bool arenaWasActive = arena.activeSelf" in menu
    assert "if (!arenaWasActive) arena.SetActive(true);" in menu
    assert "finally" in menu
    assert "arena.SetActive(false);" in menu
    assert menu.index("arena.SetActive(false);") < menu.index("CompetitionGateValidator.ValidateAndWrite(false);")
