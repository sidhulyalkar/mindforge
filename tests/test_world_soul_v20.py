from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
EDITOR = ROOT / "unity" / "Assets" / "Mindforge" / "Editor"
LATEST = EDITOR / "MindforgeLatestEditorMenu.cs"
WORLD = EDITOR / "WorldSoulV20Builder.cs"
NOISE = EDITOR / "WorldSoulNoiseV20.cs"
MATERIALS = EDITOR / "WorldSoulMaterialLibraryV20.cs"
MESHES = EDITOR / "WorldSoulMeshLibraryV20.cs"


def read(path: Path) -> str:
    assert path.exists(), f"missing V0.20 source: {path}"
    return path.read_text(encoding="utf-8")


def test_world_soul_is_part_of_the_single_latest_build():
    latest = read(LATEST)
    world = read(WORLD)
    assert 'ProductVersion = "V0.20 World Soul"' in latest
    assert "MindforgeDemoV11Builder.BuildDemoScene(controllerOnlyByDefault);" in latest
    assert "WorldSoulV20Builder.ApplyOpenScene();" in latest
    assert "EnsureWorldSoulOpenScene();" in latest
    assert 'RootName = "Mindforge_World_Soul_V20"' in world
    assert "MindforgeDemoV11Builder.RootName" in world


def test_world_soul_builds_one_continuous_environment_grammar():
    world = read(WORLD)
    for token in (
        '"WestLandmass"',
        '"EastLandmass"',
        '"SouthLandmass"',
        '"NorthHighlands"',
        '"WorldSoul_Sanctum_Grove"',
        '"WorldSoul_Causeway_Banks"',
        '"WorldSoul_Market_Ruins"',
        '"WorldSoul_Ascent_Geology"',
        '"WorldSoul_Fracture_Crater"',
        '"WorldSoul_Distant_City"',
        '"WorldSoul_Horizon_Landmarks"',
        "RetextureCanonicalArchitecture",
        "BuildAncientTree",
        "ScatterNaturalRock",
        "ConfigureAtmosphereAndLighting",
    ):
        assert token in world

    # Generated terrain wraps the route instead of replacing its collision floors.
    assert '"WorldSoul_WestTerrain"' in world
    assert '"WorldSoul_EastTerrain"' in world
    assert '"WorldSoul_SouthTerrain"' in world
    assert '"WorldSoul_NorthHighlands"' in world
    assert "WorldSoulMeshLibraryV20.TerrainPatch" in world


def test_world_soul_is_editor_authored_static_presentation_not_gameplay_authority():
    world = read(WORLD)
    combined = world + read(NOISE) + read(MATERIALS) + read(MESHES)

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
        "AddComponent<Collider",
        "AddComponent<Rigidbody",
        "UnityEngine.Random",
    ):
        assert forbidden not in combined

    # CreatePrimitive temporarily creates a collider, but every decorative primitive destroys it.
    assert "GameObject.CreatePrimitive" in world
    assert "DestroyImmediate(collider)" in world
    assert "CombatantVitals" in world
    assert "renderer.GetComponentInParent<CombatantVitals>() != null" in world


def test_world_noise_is_deterministic_allocation_light_and_publicly_attributed():
    noise = read(NOISE)
    assert "SebLague/Procedural-Landmass-Generation" in noise
    assert "License: MIT" in noise
    assert "persistence" in noise
    assert "lacunarity" in noise
    assert "Mathf.PerlinNoise" in noise
    assert "Hash01(seed, octave * 2)" in noise
    assert "System.Random" not in noise
    assert "UnityEngine.Random" not in noise


def test_world_surface_and_mesh_recipes_remain_reproducible_generated_assets():
    materials = read(MATERIALS)
    meshes = read(MESHES)

    for token in (
        'Assets/Mindforge/Generated/V20/Materials',
        'Assets/Mindforge/Generated/V20/Textures',
        "TextureWrapMode.Repeat",
        "FilterMode.Trilinear",
        "material.enableInstancing = true",
        'Shader.Find("Universal Render Pipeline/Lit")',
        'Shader.Find("Skybox/Procedural")',
    ):
        assert token in materials

    for token in (
        'Assets/Mindforge/Generated/V20/Meshes',
        "TerrainPatch(",
        "RockVariant(",
        "RecalculateNormals()",
        "RecalculateTangents()",
        "EditorUtility.CopySerialized",
    ):
        assert token in meshes


def test_world_soul_tracks_public_graphics_references_without_vendoring_runtime_packages():
    world = read(WORLD)
    meshes = read(MESHES)
    assert "SebLague/Procedural-Landmass-Generation" in world
    assert "aadebdeb/ProceduralMesh" in world
    assert "keijiro/NoiseShader" in world
    assert "aadebdeb/ProceduralMesh" in meshes

    # V0.20 uses the techniques, not a package-manager/runtime dependency.
    for forbidden in (
        "jp.keijiro.noiseshader",
        "Packages/",
        "com.github",
        "AssetBundle",
        ".fbx",
        ".blend",
    ):
        assert forbidden not in world + meshes
