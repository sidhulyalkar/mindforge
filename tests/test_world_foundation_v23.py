import json
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
UNITY = ROOT / "unity" / "Assets" / "Mindforge"
EDITOR = UNITY / "Editor"
LATEST = EDITOR / "MindforgeLatestEditorMenu.cs"
BUILDER = EDITOR / "WorldFoundationV23Builder.cs"
MESHES = EDITOR / "WorldFoundationMeshLibraryV23.cs"
V11 = EDITOR / "MindforgeDemoV11Builder.cs"
V22 = EDITOR / "WorldIntegrityV22Builder.cs"
SMOKE = UNITY / "Tests" / "Editor" / "WorldFoundationV23SmokeTests.cs"
MANIFEST = ROOT / "third_party" / "manifest.json"
DOC = ROOT / "docs" / "WORLD_FOUNDATION_V23.md"


def read(path: Path) -> str:
    assert path.exists(), f"missing V0.23 source: {path}"
    return path.read_text(encoding="utf-8")


def test_v23_remains_the_foundation_stage_before_v24_v25_and_v26_rendering():
    latest = read(LATEST)
    assert 'ProductVersion = "V0.26 Production Geometry + Cathedral Depth"' in latest
    v11 = latest.index("MindforgeDemoV11Builder.BuildDemoScene(controllerOnlyByDefault);")
    v20 = latest.index("WorldSoulV20Builder.ApplyOpenScene();", v11)
    v21 = latest.index("WorldCohesionV21Builder.ApplyOpenScene();", v20)
    v22 = latest.index("WorldIntegrityV22Builder.ApplyOpenScene();", v21)
    v23 = latest.index("WorldFoundationV23Builder.ApplyOpenScene();", v22)
    v24 = latest.index("WorldCathedralV24Builder.ApplyOpenScene();", v23)
    v25 = latest.index("SensoryFidelityV25Builder.ApplyOpenScene();", v24)
    v26 = latest.index("WorldRenderingV26Builder.ApplyOpenScene();", v25)
    assert v11 < v20 < v21 < v22 < v23 < v24 < v25 < v26
    assert "if (!WorldFoundationV23Builder.PresentInOpenScene())" in latest
    assert "if (!WorldRenderingV26Builder.PresentInOpenScene())" in latest
    assert 'RootName = "Mindforge_World_Foundation_V23"' in read(BUILDER)


def test_v23_removes_the_recording_visible_crossing_ascent_slab():
    builder = read(BUILDER)
    v11 = read(V11)
    v22 = read(V22)

    assert "const float slope = -8.1f" in v11
    assert 'Block("AscentUnderlay"' in v22
    assert "new Vector3(6.5f, 0f, 0f)" in v22

    for token in (
        "AscentSlopeDegrees = -8.1f",
        'underlayRoot.Find("AscentUnderlay")',
        "DestroyImmediate(stale.gameObject)",
        '"V23_Ascent_Visual_Reconciliation"',
        '"AscentFoundationSkin"',
        "new Vector3(AscentSlopeDegrees, 0f, 0f)",
    ):
        assert token in builder


def test_v23_fills_the_known_causeway_market_hole_with_one_visible_collision_surface():
    source = read(BUILDER)
    for token in (
        '"V23_Route_Transition_Caps"',
        '"CausewayMarketTransition"',
        "causeway ends at z=32",
        "market starts at z=33",
        "new Vector3(0f, -0.05f, 32.5f)",
        "new Vector3(8.6f, 0.10f, 1.10f)",
        "palette.WornStone",
        'Require(root, "V23_Route_Transition_Caps/CausewayMarketTransition")',
        "transition.GetComponent<BoxCollider>() == null",
    ):
        assert token in source


def test_v23_keeps_recessed_guards_below_authoritative_route_surfaces():
    source = read(BUILDER)
    for token in (
        '"V23_Collision_Reconciliation"',
        '"LowerRouteSeamGuard"',
        '"AscentSeamGuard"',
        '"BossArenaSeamGuard"',
        "BoxCollider collider = go.AddComponent<BoxCollider>()",
        "guards.GetComponentsInChildren<BoxCollider>(true).Length != 3",
    ):
        assert token in source

    collision_section = source[source.index("private static void CollisionBlock(") :]
    collision_section = collision_section[: collision_section.index("private static void MeshObject(")]
    assert "new GameObject(name)" in collision_section
    assert "AddComponent<BoxCollider>()" in collision_section
    assert "MeshRenderer" not in collision_section
    assert "GameObject.CreatePrimitive" not in collision_section


def test_v23_makes_all_four_generated_outer_landmasses_physically_explorable():
    source = read(BUILDER)
    for token in (
        "ReconcileOuterTerrainCollision",
        'worldRoot + "WestLandmass"',
        'worldRoot + "EastLandmass"',
        'worldRoot + "SouthLandmass"',
        'worldRoot + "NorthHighlands"',
        "terrain.gameObject.AddComponent<MeshCollider>()",
        "collider.sharedMesh = filter.sharedMesh",
        "collider.convex = false",
        "terrainCollider.sharedMesh != terrainFilter.sharedMesh",
    ):
        assert token in source


def test_v23_reconciles_large_visual_solids_without_turning_patina_into_obstacles():
    source = read(BUILDER)
    for token in (
        "AddStructuralContactProxies",
        "ShouldReceiveContactProxy",
        '"SanctumColumn_"',
        '"CausewayPylon"',
        '"MarketColumn_"',
        '"AscentColumn"',
        '"FractureSpire_"',
        '"FieldRock_"',
        '"CraterRock_"',
        '"WallShoulder_"',
        '"ChamberButtress_"',
        "size.x * 0.72f",
        "size.y * 0.90f",
        "size.z * 0.72f",
    ):
        assert token in source

    proxy_section = source[
        source.index("private static bool ShouldReceiveContactProxy") :
        source.index("private static void RebuildInwardCavernCeiling")
    ]
    for forbidden_solid in (
        '"Reed_"',
        '"Fern_"',
        '"Shrub_"',
        '"ArenaFracture_"',
        '"RouteLumen_"',
    ):
        assert forbidden_solid not in proxy_section


def test_v23_cavern_mesh_faces_inward_and_render_collision_share_topology():
    builder = read(BUILDER)
    meshes = read(MESHES)

    for token in (
        "WorldFoundationMeshLibraryV23.InwardTerrainPatch",
        "filter.sharedMesh = inward",
        "collider.sharedMesh = null",
        "collider.sharedMesh = inward",
        "mesh.normals[centre].y > -0.20f",
    ):
        assert token in builder

    for token in (
        "InwardTerrainPatch(",
        "BuildTransientInwardPatch(",
        "triangles.Add(a); triangles.Add(c); triangles.Add(d);",
        "triangles.Add(a); triangles.Add(b); triangles.Add(c);",
        "RecalculateNormals()",
        "RecalculateTangents()",
        "MeshRevision = 1",
        "Assets/Mindforge/Generated/V23/Meshes",
    ):
        assert token in meshes


def test_v23_seals_high_vault_ends_and_visually_roots_the_route():
    source = read(BUILDER)
    for token in (
        '"V23_Cavern_Upper_End_Seals"',
        '"SouthUpperBacking"',
        '"NorthUpperBacking"',
        '"UpperSealRock_',
        '"V23_Route_Foundations"',
        '"CausewayRetainerL"',
        '"CausewayRetainerR"',
        '"MarketRetainerL"',
        '"MarketRetainerR"',
        '"AscentFoundationRock_',
    ):
        assert token in source


def test_v23_is_static_editor_authoring_not_new_runtime_gameplay_authority():
    combined = read(BUILDER) + read(MESHES)
    for forbidden in (
        "RuntimeInitializeOnLoadMethod",
        "private void Update(",
        "private void LateUpdate(",
        "private void FixedUpdate(",
        "Time.deltaTime",
        "Time.unscaledDeltaTime",
        "NeuralEvent",
        "UdpNeuralReceiver",
        "ReceiveDamage(",
        "SetExternalPause(",
        "TryApply(",
        "MarkResolved(",
        "AddComponent<Rigidbody",
        "UnityEngine.Random",
    ):
        assert forbidden not in combined


def test_v23_public_reference_provenance_is_license_explicit_and_reference_only():
    meshes = read(MESHES)
    manifest = json.loads(read(MANIFEST))
    entries = {entry["id"]: entry for entry in manifest["entries"]}

    assert "SebLague/Procedural-Cave-Generation" in meshes
    assert "aadebdeb/ProceduralMesh" in meshes

    cave = entries["seblague.procedural_cave_generation"]
    assert cave["license"] == "MIT"
    assert cave["usage"] == "reference_only"
    assert cave["vendored_paths"] == []
    assert "no upstream scene, mesh, texture or script is copied" in cave["asset_policy"].lower()

    procedural_mesh = entries["aadebdeb.procedural_mesh"]
    assert procedural_mesh["license"] == "MIT"
    assert procedural_mesh["usage"] == "reference_only"


def test_v23_has_native_inward_mesh_smoke_documentation_and_pinned_unique_guids():
    smoke = read(SMOKE)
    doc = read(DOC)
    assert "V23InwardPatch_FacesIntoTheCavern" in smoke
    assert "BuildTransientInwardPatch" in smoke
    assert "normals[mesh.normals.Length / 2].y" in smoke
    assert "-8.1 degrees" in doc
    assert "+6.5 degrees" in doc
    assert "z=32" in doc and "z=33" in doc
    assert "SebLague/Procedural-Cave-Generation" in doc

    paths = (
        EDITOR / "WorldFoundationV23Builder.cs.meta",
        EDITOR / "WorldFoundationMeshLibraryV23.cs.meta",
        UNITY / "Tests" / "Editor" / "WorldFoundationV23SmokeTests.cs.meta",
    )
    guids = []
    for path in paths:
        text = read(path)
        assert "fileFormatVersion: 2" in text
        guid = next(line.split(":", 1)[1].strip() for line in text.splitlines() if line.startswith("guid: "))
        assert len(guid) == 32
        guids.append(guid)
    assert len(guids) == len(set(guids))
