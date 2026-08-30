from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
MESHES = ROOT / "unity/Assets/Mindforge/Editor/ProductionStoryMeshLibraryV09.cs"
STORY = ROOT / "unity/Assets/Mindforge/Editor/ProductionWorldStorytellingV09Builder.cs"
HOOK = ROOT / "unity/Assets/Mindforge/Editor/ProductionArtAutoHookV09.cs"


def read(path: Path) -> str:
    assert path.exists(), path
    return path.read_text(encoding="utf-8")


def test_story_mesh_library_adds_asymmetry_without_falling_back_to_unity_primitives():
    text = read(MESHES)
    for token in (
        "BrokenSlab",
        "SignalShard",
        "HangingRibbon",
        "CableArc",
        "BuildBrokenSlab",
        "BuildSignalShard",
        "BuildHangingRibbon",
        "BuildCableArc",
        "RecalculateNormals",
        "RecalculateTangents",
    ):
        assert token in text
    assert "GameObject.CreatePrimitive" not in text
    assert "AddComponent<Collider>" not in text
    assert "AddComponent<Rigidbody>" not in text


def test_storytelling_has_five_distinct_district_profiles_instead_of_mirrored_generic_clutter():
    text = read(STORY)
    for token in (
        'RootName = "Production_District_Storytelling_V09"',
        '"Story_Sanctum_Repair"',
        '"Story_Promenade_LivedIn"',
        '"Story_Market_Trade"',
        '"Story_Fracture_Damage"',
        '"Story_Cathedral_Repair"',
        "BuildSanctumRepair",
        "BuildPromenadeLife",
        "BuildMarketTrade",
        "BuildFractureDamage",
        "BuildCathedralRepair",
    ):
        assert token in text

    # Named asymmetric props make accidental left/right mechanical mirroring obvious.
    for token in (
        "Sanctum_Left_SettledSlab_A",
        "Sanctum_Right_VotiveRibbon",
        "Promenade_West_GrowthTie",
        "Promenade_East_Waycloth",
        "Market_NorthWest_CounterRemnant",
        "Market_SouthEast_CounterRemnant",
        "Cathedral_West_ProcessionalBanner",
        "Cathedral_East_NarrowBanner",
    ):
        assert token in text


def test_storytelling_protects_core_traversal_and_interaction_clearances():
    text = read(STORY)
    assert "AssertOutsideProtectedTransit(position, name)" in text
    assert "Mathf.Abs(p.x) < 10.0f" in text
    assert "Mathf.Abs(p.x) < 10.2f" in text
    assert 'new Vector3(26.5f, 0f, -29f)) < 6.2f' in text
    assert 'new Vector3(-28.2f, 0f, -18f)) < 4.5f' in text
    assert "Mathf.Abs(p.x - 29.5f) < 6.5f" in text

    # Market service lines were explicitly pushed beyond the protected 6.2m core.
    assert 'new Vector3(0.8f, 7.2f, -6.8f)' in text
    assert 'new Vector3(-1.4f, 6.5f, 6.8f)' in text


def test_storytelling_is_hard_budgeted_and_fails_if_it_acquires_physics_or_lights():
    text = read(STORY)
    assert "MaxStoryRenderers = 56" in text
    assert "MaxStoryLights = 0" in text
    assert "ValidatePresentationOnly(rootGo)" in text
    assert "rendererCount > MaxStoryRenderers" in text
    assert "colliderCount != 0 || bodyCount != 0 || lightCount != MaxStoryLights" in text
    assert "BuildFailedException" in text

    # Reading components to audit the root is allowed; authoring them is not.
    for forbidden in (
        "AddComponent<Collider>",
        "AddComponent<Rigidbody>",
        "AddComponent<Light>",
        "AddComponent<GuardianMotor>",
        "AddComponent<JourneyEnemyController>",
        "TakeDamage(",
        "Input.Get",
        "UdpNeuralReceiver",
        "NeuralEvent",
        "PlayerProfileSave",
        "WorldInteraction",
    ):
        assert forbidden not in text


def test_small_story_detail_culls_and_distant_skyline_stops_spending_shadow_probe_budget():
    text = read(STORY)
    assert "AddCullOnlyLod" in text
    assert "new LOD(transitionHeight, renderers)" in text
    assert "LODFadeMode.None" in text
    assert 'production.Find("Production_Skyline")' in text
    assert "renderer.shadowCastingMode = ShadowCastingMode.Off" in text
    assert "renderer.reflectionProbeUsage = ReflectionProbeUsage.Off" in text


def test_story_meshes_are_reused_as_assets_not_regenerated_per_prop():
    text = read(MESHES)
    assert "AssetDatabase.LoadAssetAtPath<Mesh>(path)" in text
    assert "if (existing != null) return existing" in text
    assert "AssetDatabase.CreateAsset(mesh, path)" in text
    for path in (
        "/BrokenSlab.asset",
        "/SignalShard.asset",
        "/HangingRibbon.asset",
        "/CableArc.asset",
    ):
        assert path in text


def test_complete_v09_hook_runs_storytelling_before_finish_and_external_replacement():
    text = read(HOOK)
    assert "EnsureStorytelling(production);" in text
    assert "ProductionWorldStorytellingV09Builder.ApplyOpenScene();" in text
    apply_section = text.split("private static void ApplyInternal", 1)[1]
    story = apply_section.index("EnsureStorytelling(production);")
    post = apply_section.index("EnsurePostFx(production);")
    external = apply_section.index("ExternalArtReplacementV09.ApplyOpenScene()")
    assert story < post < external


def test_storytelling_script_meta_guids_are_unique():
    metas = [Path(str(MESHES) + ".meta"), Path(str(STORY) + ".meta")]
    all_meta = list((ROOT / "unity/Assets").rglob("*.meta"))
    for meta in metas:
        assert meta.exists()
        guid = next(
            line.split(":", 1)[1].strip()
            for line in meta.read_text(encoding="utf-8").splitlines()
            if line.startswith("guid: ")
        )
        assert len(guid) == 32
        matches = [
            candidate
            for candidate in all_meta
            if f"guid: {guid}" in candidate.read_text(encoding="utf-8", errors="ignore")
        ]
        assert matches == [meta]
