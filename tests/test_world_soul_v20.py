import json
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
EDITOR = ROOT / "unity" / "Assets" / "Mindforge" / "Editor"
LATEST = EDITOR / "MindforgeLatestEditorMenu.cs"
WORLD = EDITOR / "WorldSoulV20Builder.cs"
NOISE = EDITOR / "WorldSoulNoiseV20.cs"
MATERIALS = EDITOR / "WorldSoulMaterialLibraryV20.cs"
MESHES = EDITOR / "WorldSoulMeshLibraryV20.cs"
MANIFEST = ROOT / "third_party" / "manifest.json"


def read(path: Path) -> str:
    assert path.exists(), f"missing V0.20 source: {path}"
    return path.read_text(encoding="utf-8")


def test_world_soul_remains_the_first_world_authoring_stage_of_latest():
    latest = read(LATEST)
    world = read(WORLD)
    assert 'ProductVersion = "V0.25 Sensory Fidelity + Data Cathedral"' in latest
    v11_i = latest.index("MindforgeDemoV11Builder.BuildDemoScene(controllerOnlyByDefault);")
    v20_i = latest.index("WorldSoulV20Builder.ApplyOpenScene();", v11_i)
    v21_i = latest.index("WorldCohesionV21Builder.ApplyOpenScene();", v20_i)
    v22_i = latest.index("WorldIntegrityV22Builder.ApplyOpenScene();", v21_i)
    v23_i = latest.index("WorldFoundationV23Builder.ApplyOpenScene();", v22_i)
    v24_i = latest.index("WorldCathedralV24Builder.ApplyOpenScene();", v23_i)
    v25_i = latest.index("SensoryFidelityV25Builder.ApplyOpenScene();", v24_i)
    assert v11_i < v20_i < v21_i < v22_i < v23_i < v24_i < v25_i
    assert "EnsureWorldLayersOpenScene();" in latest
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


def test_world_surfaces_reuse_production_triplanar_pbr_with_generated_normals():
    materials = read(MATERIALS)
    for token in (
        'Assets/Mindforge/Generated/V20/Materials',
        'Assets/Mindforge/Generated/V20/Textures',
        "SurfaceRevision = 2",
        "TextureWrapMode.Repeat",
        "FilterMode.Trilinear",
        "ProductionMaterialAuthoringV09.TriplanarShaderPath",
        "ProductionMaterialAuthoringV09.TriplanarShaderName",
        'material.SetTexture("_BaseMap", surface.Albedo)',
        'material.SetTexture("_BumpMap", surface.Normal)',
        'material.SetFloat("_MetersPerTile"',
        'material.SetFloat("_BlendSharpness"',
        'material.SetFloat("_NormalFadeDistance"',
        "ShaderUtil.ShaderHasError",
        "material.enableInstancing = true",
        'Shader.Find("Universal Render Pipeline/Lit")',
        'Shader.Find("Skybox/Procedural")',
    ):
        assert token in materials


def test_world_mesh_recipes_are_generated_cached_assets_not_binary_source_art():
    meshes = read(MESHES)
    for token in (
        'Assets/Mindforge/Generated/V20/Meshes',
        "MeshRevision = 1",
        "TerrainCache",
        "RockCache",
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

    for forbidden in (
        "jp.keijiro.noiseshader",
        "Packages/",
        "com.github",
        "AssetBundle",
        ".fbx",
        ".blend",
    ):
        assert forbidden not in world + meshes


def test_world_soul_provenance_is_registered_in_the_repository_manifest():
    manifest = json.loads(read(MANIFEST))
    entries = {entry["id"]: entry for entry in manifest["entries"]}

    adapted = entries["seblague.procedural_landmass_generation"]
    assert adapted["license"] == "MIT"
    assert adapted["usage"] == "adapted_code"
    assert "unity/Assets/Mindforge/Editor/WorldSoulNoiseV20.cs" in adapted["vendored_paths"]
    notice = ROOT / "third_party" / "licenses" / "SebLague_Procedural_Landmass_Generation_MIT.txt"
    assert str(notice.relative_to(ROOT)) in adapted["vendored_paths"]
    assert "MIT License" in read(notice)

    for reference_id in ("aadebdeb.procedural_mesh", "keijiro.noise_shader"):
        entry = entries[reference_id]
        assert entry["license"] == "MIT"
        assert entry["usage"] == "reference_only"
        assert entry["vendored_paths"] == []


def test_world_soul_editor_scripts_have_pinned_unique_unity_guids():
    scripts = (
        "WorldSoulNoiseV20.cs",
        "WorldSoulMaterialLibraryV20.cs",
        "WorldSoulMeshLibraryV20.cs",
        "WorldSoulV20Builder.cs",
    )
    guids = []
    for script in scripts:
        meta = EDITOR / f"{script}.meta"
        text = read(meta)
        assert "fileFormatVersion: 2" in text
        guid = next(line.split(":", 1)[1].strip() for line in text.splitlines() if line.startswith("guid: "))
        assert len(guid) == 32
        guids.append(guid)
    assert len(guids) == len(set(guids))
