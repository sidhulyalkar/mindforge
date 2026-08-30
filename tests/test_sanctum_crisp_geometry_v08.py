from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
CRISP = ROOT / "unity/Assets/Mindforge/Editor/SanctumCrispGeometryV08Builder.cs"
MENU = ROOT / "unity/Assets/Mindforge/Editor/ShowcaseEditorMenu.cs"


def test_crisp_pass_builds_reusable_real_chamfer_meshes_not_outline_fx():
    text = CRISP.read_text(encoding="utf-8")
    assert 'ArchitecturalMeshPath = "Assets/Mindforge/Generated/SanctumV08/ChamferedArchitecturalPrism.asset"' in text
    assert 'EnemyMeshPath = "Assets/Mindforge/Generated/SanctumV08/ChamferedEnemyPrism.asset"' in text
    assert "ArchitecturalBevel = 0.055f" in text
    assert "EnemyBevel = 0.105f" in text
    assert "BuildChamferedUnitCube" in text
    assert "Twelve planar edge chamfers" in text
    assert "Eight corner facets" in text
    assert "RecalculateNormals" in text
    assert "RecalculateTangents" in text
    assert "LineRenderer" not in text
    assert "Outline" not in text


def test_crisp_pass_swaps_mesh_filters_without_changing_collision_or_gameplay():
    text = CRISP.read_text(encoding="utf-8")
    assert "filter.sharedMesh = chamfered" in text
    for forbidden in (
        "DestroyImmediate(shape)",
        "BoxCollider",
        "MeshCollider",
        "AddComponent<Collider",
        "AddComponent<Rigidbody",
        "JourneyEnemyController.ConfigureRuntime",
        "EnemyAttackDefinition",
        "TakeDamage",
        "NeuralEvent",
        "VepAuraStimulus",
        "transform.position =",
        "transform.localPosition =",
    ):
        assert forbidden not in text


def test_crisp_architecture_targets_hero_structure_not_floor_and_navigation_surfaces():
    text = CRISP.read_text(encoding="utf-8")
    for token in (
        '"Pier"',
        '"Plinth"',
        '"Capital"',
        '"Buttress"',
        '"Threshold"',
        '"Parapet"',
        '"SanctumBlock"',
        '"BridgeDeck"',
        '"ForgeWing"',
    ):
        assert token in text
    assert 'ContainsAny(n, "Floor", "Road", "Spine", "Joint", "Glass", "Gold", "Signal", "Water", "Garden", "RouteNode", "Window", "Rune")' in text


def test_crisp_enemy_pass_only_operates_inside_reference_silhouette_child():
    text = CRISP.read_text(encoding="utf-8")
    assert "SanctumReferenceFidelityV08Builder.EnemyRootName" in text
    assert 'controller.transform.Find("Visuals")' in text
    assert "reference.GetComponentsInChildren<MeshFilter>(true)" in text


def test_one_click_pipeline_scopes_specialized_rosters_before_chamfering_reference_shells():
    menu = MENU.read_text(encoding="utf-8")
    fidelity = menu.find("SanctumReferenceFidelityV08Builder.ApplyOpenScene();")
    scope = menu.find("SanctumEnemyPresentationScopeV08.RemoveReferenceShellsFromSpecializedRosters();")
    crisp = menu.find("SanctumCrispGeometryV08Builder.ApplyOpenScene();")
    assert fidelity >= 0
    assert scope > fidelity
    assert crisp > scope
