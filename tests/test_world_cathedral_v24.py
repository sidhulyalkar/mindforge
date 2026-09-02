from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
UNITY = ROOT / "unity" / "Assets" / "Mindforge"
EDITOR = UNITY / "Editor"
WORLD = UNITY / "World"
LATEST = EDITOR / "MindforgeLatestEditorMenu.cs"
BUILDER = EDITOR / "WorldCathedralV24Builder.cs"
MATERIALS = EDITOR / "CathedralMaterialLibraryV24.cs"
MODULES = EDITOR / "CathedralModuleLibraryV24.cs"
ROLE = WORLD / "CathedralRoleV24.cs"
DOC = ROOT / "docs" / "WORLD_CATHEDRAL_V24.md"
SMOKE = UNITY / "Tests" / "Editor" / "WorldCathedralV24SmokeTests.cs"


def read(path: Path) -> str:
    assert path.exists(), f"missing V0.24 source: {path}"
    return path.read_text(encoding="utf-8")


def test_v24_remains_the_final_canonical_layout_before_render_and_encounter_presentation():
    latest = read(LATEST)
    source = read(BUILDER)
    assert 'ProductVersion = "V0.27 Guardian Embodiment + Fractured Beast"' in latest
    v11 = latest.index("MindforgeDemoV11Builder.BuildDemoScene(controllerOnlyByDefault);")
    v20 = latest.index("WorldSoulV20Builder.ApplyOpenScene();", v11)
    v21 = latest.index("WorldCohesionV21Builder.ApplyOpenScene();", v20)
    v22 = latest.index("WorldIntegrityV22Builder.ApplyOpenScene();", v21)
    v23 = latest.index("WorldFoundationV23Builder.ApplyOpenScene();", v22)
    v24 = latest.index("WorldCathedralV24Builder.ApplyOpenScene();", v23)
    v25 = latest.index("SensoryFidelityV25Builder.ApplyOpenScene();", v24)
    v26 = latest.index("WorldRenderingV26Builder.ApplyOpenScene();", v25)
    v27 = latest.index("CombatEmbodimentV27Builder.ApplyOpenScene();", v26)
    assert v11 < v20 < v21 < v22 < v23 < v24 < v25 < v26 < v27
    assert "if (!WorldCathedralV24Builder.PresentInOpenScene())" in latest
    assert "if (!SensoryFidelityV25Builder.PresentInOpenScene())" in latest
    assert "if (!WorldRenderingV26Builder.PresentInOpenScene())" in latest
    assert "if (!CombatEmbodimentV27Builder.PresentInOpenScene())" in latest
    assert 'RootName = "Mindforge_White_Cathedral_V24"' in source


def test_v24_palette_is_light_cathedral_first_not_dark_blockout_first():
    source = read(MATERIALS)
    for token in (
        'Assets/Mindforge/Generated/V24/Materials', 'Assets/Mindforge/Generated/V24/Textures', '"IvoryStone"',
        '"WhiteMarble"', '"PaleFloor"', '"CoolShadowStone"', '"V24_Bronze"', '"V24_SacredGold"',
        '"V24_SignalMagenta"', '"V24_LumenCyan"', "ProductionMaterialAuthoringV09.TriplanarShaderPath",
        "ProductionMaterialAuthoringV09.TriplanarShaderName", 'material.SetTexture("_BaseMap", surface.Albedo)',
        'material.SetTexture("_BumpMap", surface.Normal)', "WorldSoulNoiseV20.Fbm", "TextureWrapMode.Repeat",
        "FilterMode.Trilinear", "NormalizeCanonicalScene(palette)",
    ):
        assert token in source
    assert "UnityEngine.Random" not in source


def test_v24_suppresses_patchy_foreground_grammars_instead_of_piling_on_more_scatter():
    source = read(BUILDER)
    for token in (
        "WorldSoul_Natural_Rock", "WorldSoul_Sanctum_Grove", "WorldSoul_Causeway_Banks", "WorldSoul_Market_Ruins",
        "WorldSoul_Ascent_Geology", "V21_Surface_Transitions", "V21_Foreground_Ecology", "V21_Near_City_Facades",
        "V21_Landmark_Composition", "V22_Route_Luminance_Anchors", "V11_Skyline",
        'DisableChildrenByPrefix(market, "MarketStall_", "MarketGarden_")', "target.gameObject.SetActive(false)",
    ):
        assert token in source
    materials = read(MATERIALS)
    for token in (
        "CullLegacyDuplicateArchitecture", 'DisableChildrenByPrefix(sanctum, "SanctumColumn_", "SanctumGarden_")',
        'DisableChildrenByPrefix(causeway, "CausewayPylon")',
        'DisableChildrenByPrefix(market, "MarketColumn_", "MarketStall_", "MarketGarden_")',
        'DisableChildrenByPrefix(vaultTransitions, "VaultRib_")', "oldBossCrown.gameObject.SetActive(false)",
    ):
        assert token in materials


def test_v24_uses_one_floor_visual_language_over_existing_collision_authority():
    source = read(BUILDER)
    modules = read(MODULES)
    for floor in ("SanctumFloor", "CausewayRoad", "MarketFloor", "AscentRamp", "FractureFloor"):
        assert f'"{floor}"' in source
    for token in (
        'replacement = palette.PaleFloor', '"V24_Processional_Spine"', '"SanctumAisle"', '"CausewayAisle"',
        '"MarketAisle"', '"MarketTransept"', '"ChoirRampAisle"', '"Threshold_',
        "ramp.localScale.y * 0.5f + 0.020f", "WorldFoundationV23Builder.AscentSlopeDegrees",
    ):
        assert token in source
    floor_section = modules[modules.index("public static Transform FloorSkin") : modules.index("public static Transform Trim")]
    assert "WalkableFloor" in floor_section
    assert "false" in floor_section


def test_v24_module_kit_is_semantic_and_reusable():
    modules = read(MODULES)
    role = read(ROLE)
    for token in (
        "FloorSkin(", "RetainingBlock(", "BoundaryBlock(", "Column(", "PointedArch(", "Buttress(",
        "WallPanel(", "LumenSconce(", "BeamBetween(", "ProductionMeshLibraryV09.FlutedColumn()",
        "ProductionMeshLibraryV09.PointedArch()", "AddComponent<CathedralRoleV24>()",
    ):
        assert token in modules
    for role_name in (
        "WalkableFloor", "StructuralSupport", "BoundaryWall", "VaultCeiling", "RetainingSubstructure",
        "DecorativePatina", "MysticAccent",
    ):
        assert role_name in role
    for forbidden in ("Update(", "LateUpdate(", "FixedUpdate(", "Rigidbody"):
        assert forbidden not in role


def test_v24_composes_named_cathedral_zones_with_repeated_architectural_rhythm():
    source = read(BUILDER)
    for token in (
        '"V24_Sanctum_Narthex"', '"V24_Causeway_Nave"', '"V24_Market_Cloister"', '"V24_Choir_Ascent"',
        '"V24_Fractured_Signal_Apse"', '"V24_Vault_Rhythm"', '"CathedralColumn_Sanctum_',
        '"CathedralColumn_Nave_', '"CathedralColumn_Cloister_', '"CathedralColumn_Apse_',
        '"CathedralArch_Sanctum_', '"CathedralArch_Nave_', '"CathedralArch_Cloister_',
        '"CathedralArch_Choir_', '"CathedralVaultRib_',
    ):
        assert token in source


def test_v24_boss_apse_frames_but_does_not_compress_the_existing_fight():
    source = read(BUILDER)
    assert "const float radius = 19.4f" in source
    assert "ArenaCenterZ = 94f" in source
    assert '"BossApseArch_' in source
    assert '"ApseFloorRing_' in source
    assert '"BossFractureAxis"' in source
    assert "collider = false" not in source
    for forbidden in ("FracturedSignalDirector", "SetExternalPause(", "ReceiveDamage(", "MovePosition(", "MoveRotation("):
        assert forbidden not in source


def test_v24_static_lighting_has_no_temporal_or_neural_authority():
    source = read(BUILDER)
    for token in (
        '"V24_Static_Lighting"', "LightType.Point", "LightShadows.None", "RenderSettings.ambientLight",
        "RenderSettings.fogStartDistance = 78f", "RenderSettings.fogEndDistance = 225f",
    ):
        assert token in source
    for forbidden in (
        "RuntimeInitializeOnLoadMethod", "private void Update(", "private void LateUpdate(", "private void FixedUpdate(",
        "Time.deltaTime", "Time.unscaledDeltaTime", "NeuralEvent", "UdpNeuralReceiver", "UnityEngine.Random",
    ):
        assert forbidden not in source + read(MODULES) + read(MATERIALS)


def test_v24_builder_has_fail_closed_visual_structure_validation():
    source = read(BUILDER)
    for token in (
        "ValidateCathedral", "renderer.sharedMaterial != palette.PaleFloor", "floors < 12 || supports < 45 || mystic < 8",
        "has no cathedral structural role", "noisy legacy layer still active", 'FindDeep(canonicalRoot, "AscentUnderlay") != null',
    ):
        assert token in source


def test_v24_native_smoke_docs_and_guids_are_present():
    smoke = read(SMOKE)
    doc = read(DOC)
    for token in (
        "V24CathedralRole_IsPureSemanticMarker", "V24CathedralModuleKit_CanConstructSemanticGeometry",
        "CathedralRoleV24.StructuralRole.StructuralSupport",
    ):
        assert token in smoke
    assert "white cathedral" in doc.lower()
    assert "one floor authority" in doc.lower()
    assert "narthex" in doc.lower()
    assert "nave" in doc.lower()
    assert "cloister" in doc.lower()
    assert "choir" in doc.lower()
    assert "apse" in doc.lower()
    paths = (
        EDITOR / "CathedralMaterialLibraryV24.cs.meta", EDITOR / "CathedralModuleLibraryV24.cs.meta",
        EDITOR / "WorldCathedralV24Builder.cs.meta", WORLD / "CathedralRoleV24.cs.meta",
        UNITY / "Tests" / "Editor" / "WorldCathedralV24SmokeTests.cs.meta",
    )
    guids = []
    for path in paths:
        text = read(path)
        assert "fileFormatVersion: 2" in text
        guid = next(line.split(":", 1)[1].strip() for line in text.splitlines() if line.startswith("guid: "))
        assert len(guid) == 32
        guids.append(guid)
    assert len(guids) == len(set(guids))
