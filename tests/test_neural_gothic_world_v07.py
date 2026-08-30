from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
UNITY = ROOT / "unity" / "Assets" / "Mindforge"


def read(*parts: str) -> str:
    return UNITY.joinpath(*parts).read_text(encoding="utf-8")


def test_neural_gothic_kit_is_deterministic_presentation_only():
    source = read("Presentation", "NeuralGothicWorldKitV07.cs")

    for token in (
        'Revision = "NEURAL_GOTHIC_WORLD_V07"',
        'DecorRootName = "Mindforge_V07_Neural_Gothic_Visuals"',
        "StableHash(cell.name, deterministicSeed)",
        "cells.Sort((a, b) => string.CompareOrdinal(a.name, b.name))",
        "BuildRouteTrace",
        "BuildPointedThreshold",
        "BuildButtressPair",
        "BuildVerticalOculus",
        "BuildSpire",
        "BuildCloisterCrown",
        'StartsWith("cell_", StringComparison.Ordinal)',
        'cell.Find("wall_n")',
        'id.Contains("corridor_ns")',
        'id.Contains("corner_ne")',
    ):
        assert token in source

    # Visual construction strips primitive colliders instead of adding traversal authority.
    assert "Collider shape = go.GetComponent<Collider>()" in source
    assert "Destroy(shape)" in source
    assert "DestroyImmediate(shape)" in source

    # The first V0.7 scene pass is static by construction and has no gameplay/state authority.
    for forbidden in (
        "void Update(",
        "void FixedUpdate(",
        "Time.time",
        "Rigidbody",
        "WorldStateLedger",
        "WorldSignalBus",
        "WorldInteractionSourceV1",
        "GuardianControlAction",
        "Input.GetKey",
        "PlayerPrefs",
    ):
        assert forbidden not in source


def test_v07_builder_is_thin_and_layers_after_persistent_world():
    builder = read("Editor", "NeuralGothicWorldV07Builder.cs")
    showcase = read("Editor", "ShowcaseEditorMenu.cs")

    for token in (
        'Revision = "NEURAL_GOTHIC_WORLD_V07"',
        'CloisterName = "Neural_Cloister_Procedural_Annex"',
        "EditorSceneLookup.FindIncludingInactive(WorldV06Builder.RootName)",
        "annex.GetComponent<ModularWorldAssemblerV06>()",
        "annex.gameObject.AddComponent<NeuralGothicWorldKitV07>()",
        'FindMaterial("ObsidianArchitecture")',
        'FindMaterial("GuardianMetal")',
        'FindMaterial("AetherCyan")',
        'FindMaterial("WispVerdant")',
        "seed: 70713",
        "tier: 2",
        "kit.Rebuild()",
    ):
        assert token in builder

    # V0.7 must remain downstream of V0.6 truth and upstream of final validation/auditing.
    v06 = showcase.index("WorldV06Builder.ApplyOpenScene();")
    v07 = showcase.index("NeuralGothicWorldV07Builder.ApplyOpenScene();")
    validation = showcase.index("CompetitionGateValidator.ValidateAndWrite(false);")
    budget = showcase.index("PresentationBudgetAudit.Run();")
    assert v06 < v07 < validation < budget


def test_v07_doc_preserves_visual_hierarchy_and_scope_discipline():
    doc = (ROOT / "docs" / "NEURAL_GOTHIC_WORLD_V07.md").read_text(encoding="utf-8")

    for token in (
        "Generate gameplay truth first. Decorate it second.",
        "never creates a gameplay collider",
        "never writes a stable world ID",
        "never reads input",
        "never runs per-frame animation",
        "never drives a coded neural stimulus",
        "Cloister Crown",
        "Fractured Signal heroic pass",
        "final-art replacement seams",
        "no coded-stimulus contamination",
        "make one complete slice feel authored, coherent, and worth remembering",
    ):
        assert token in doc


def test_v07_csharp_guids_exist_and_are_unique_repository_wide():
    metas = (
        UNITY / "Presentation" / "NeuralGothicWorldKitV07.cs.meta",
        UNITY / "Editor" / "NeuralGothicWorldV07Builder.cs.meta",
    )
    expected = []
    for path in metas:
        text = path.read_text(encoding="utf-8")
        assert "fileFormatVersion: 2" in text
        guid = next(line for line in text.splitlines() if line.startswith("guid: ")).split(":", 1)[1].strip()
        assert len(guid) == 32
        expected.append(guid)

    assert len(expected) == len(set(expected))

    all_guids = {}
    for path in (ROOT / "unity" / "Assets").rglob("*.meta"):
        text = path.read_text(encoding="utf-8", errors="ignore")
        for line in text.splitlines():
            if not line.startswith("guid: "):
                continue
            guid = line.split(":", 1)[1].strip()
            all_guids.setdefault(guid, []).append(path)
            break

    for guid in expected:
        assert len(all_guids.get(guid, [])) == 1, (guid, all_guids.get(guid, []))
