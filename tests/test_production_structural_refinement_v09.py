from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
MESH = ROOT / "unity/Assets/Mindforge/Editor/ProductionStructuralMeshV09.cs"
REFINE = ROOT / "unity/Assets/Mindforge/Editor/ProductionStructuralRefinementV09Builder.cs"
ART = ROOT / "unity/Assets/Mindforge/Editor/ProductionArtV09Builder.cs"
HOOK = ROOT / "unity/Assets/Mindforge/Editor/ProductionArtAutoHookV09.cs"


def test_structural_prism_has_real_main_faces_edge_chamfers_and_corner_facets():
    text = MESH.read_text(encoding="utf-8")
    assert 'ChamferedPrismPath = Root + "/ChamferedStructuralPrism.asset"' in text
    assert "RecipeVersion = 1" in text
    assert "Bevel = 0.055f" in text
    assert "Six inset main faces" in text
    assert "Twelve edge chamfers" in text
    assert "Eight planar corner facets" in text
    assert "Vector3.Dot(Vector3.Cross" in text
    assert "Validate(mesh)" in text
    assert "GameObject.CreatePrimitive" not in text


def test_refinement_swaps_stock_cube_and_cylinder_meshes_without_reauthoring_layout():
    text = REFINE.read_text(encoding="utf-8")
    assert "ProductionStructuralMeshV09.ChamferedPrism()" in text
    assert "ProductionMeshLibraryV09.FlutedColumn()" in text
    assert 'string.Equals(meshName, "Cube"' in text
    assert 'string.Equals(meshName, "Cylinder"' in text
    assert "filter.sharedMesh = chamfered" in text
    assert "filter.sharedMesh = fluted" in text
    assert "scale.y *= 2f" in text
    for forbidden in (
        "transform.localPosition =",
        "transform.position =",
        "transform.localRotation =",
        "transform.rotation =",
        "Renderer.sharedMaterial =",
        "AddComponent<Collider",
        "AddComponent<Rigidbody",
    ):
        assert forbidden not in text


def test_refinement_fails_if_stock_structural_mesh_survives():
    text = REFINE.read_text(encoding="utf-8")
    assert "ValidateNoStockStructuralMeshes" in text
    assert 'string.Equals(meshName, "Cube"' in text
    assert 'string.Equals(meshName, "Cylinder"' in text
    assert "still contains a stock structural mesh" in text


def test_production_art_stock_helper_is_explicitly_downstream_of_refinement():
    art = ART.read_text(encoding="utf-8")
    hook = HOOK.read_text(encoding="utf-8")
    # The base builder may still use simple primitives as a deterministic authoring recipe,
    # but the canonical V0.9 path must refine them before any later production presentation.
    assert "GameObject.CreatePrimitive(PrimitiveType.Cube)" in art
    refine = hook.find("EnsureStructuralRefinement(production);")
    horizon = hook.find("EnsureHorizon(production);")
    assert refine >= 0
    assert horizon > refine
    assert "production.transform.Find(ProductionStructuralRefinementV09Builder.RootName) == null" in hook
    assert "ProductionStructuralRefinementV09Builder.ApplyOpenScene();" in hook
