from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
ASSEMBLER = ROOT / "unity/Assets/Mindforge/Editor/CompetitionSceneAssembler.cs"
MENU = ROOT / "unity/Assets/Mindforge/Editor/ShowcaseEditorMenu.cs"
LOOKUP = ROOT / "unity/Assets/Mindforge/Editor/EditorSceneLookup.cs"


def test_competition_arena_remains_dormant_before_runtime_authority():
    source = ASSEMBLER.read_text()
    assert "arena.SetActive(false);" in source


def test_showcase_authoring_finds_inactive_arena_and_restores_it():
    source = MENU.read_text()
    assert 'EditorSceneLookup.FindIncludingInactive("Fractured_Signal_Arena")' in source
    assert "bool arenaWasActive = arena.activeSelf;" in source
    assert "if (!arenaWasActive) arena.SetActive(true);" in source
    assert "finally" in source
    assert "arena.SetActive(false);" in source
    assert "EditorSceneManager.SaveOpenScenes();" in source


def test_inactive_lookup_is_scene_local_and_includes_inactive_children():
    source = LOOKUP.read_text()
    assert "EditorSceneManager.GetActiveScene()" in source
    assert "scene.GetRootGameObjects()" in source
    assert "GetComponentsInChildren<Transform>(true)" in source
    assert "candidate.gameObject.scene != scene" in source
