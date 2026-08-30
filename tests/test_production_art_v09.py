from pathlib import Path
import json


ROOT = Path(__file__).resolve().parents[1]
ART = ROOT / "unity/Assets/Mindforge/Editor/ProductionArtV09Builder.cs"
MATERIALS = ROOT / "unity/Assets/Mindforge/Editor/ProductionMaterialAuthoringV09.cs"
MESHES = ROOT / "unity/Assets/Mindforge/Editor/ProductionMeshLibraryV09.cs"
EXTERNAL = ROOT / "unity/Assets/Mindforge/Editor/ExternalArtDropV09.cs"
REPLACEMENT = ROOT / "unity/Assets/Mindforge/Editor/ExternalArtReplacementV09.cs"
HOOK = ROOT / "unity/Assets/Mindforge/Editor/ProductionArtAutoHookV09.cs"
HUD = ROOT / "unity/Assets/Mindforge/Presentation/ProductionHudV09.cs"
GUARDIAN = ROOT / "unity/Assets/Mindforge/Presentation/ProductionGuardianV09.cs"
ECHO = ROOT / "unity/Assets/Mindforge/Presentation/ProductionEchoVisualV09.cs"
ECHO_BOOTSTRAP = ROOT / "unity/Assets/Mindforge/Presentation/ProductionEchoVisualBootstrapV09.cs"
PHYSICAL_ARSENAL = ROOT / "unity/Assets/Mindforge/Combat/PhysicalArsenalBootstrap.cs"
GITIGNORE = ROOT / ".gitignore"
MANIFEST = ROOT / "third_party/manifest.json"


def read(path: Path) -> str:
    assert path.exists(), f"missing V0.9 source: {path}"
    return path.read_text(encoding="utf-8")


def test_v09_is_a_visual_replacement_pass_not_another_primitive_clutter_layer():
    text = read(ART)
    assert 'RootName = "Mindforge_Production_Art_V09"' in text
    assert "HideSanctumBlockoutRenderers" in text
    assert "RethemeGroundedWorld" in text
    assert "v07.SetActive(false)" in text
    for token in (
        "Production_Sanctum_Nave",
        "Production_Threshold_Facade",
        "Production_Processional_Promenade",
        "Production_Market_Arcade",
        "Production_Fracture_Landmark",
        "Production_Cathedral_Approach",
        "Production_Skyline",
    ):
        assert token in text

    # Lock behavior, not prose: selected V0.8 renderers become invisible while their
    # collider components are never enumerated or disabled by the production pass.
    assert "renderer.enabled = false;" in text
    assert "referenceRenderers[i].enabled = false;" in text
    assert "renderer.sharedMaterial = replacement;" in text
    assert "GetComponentsInChildren<Collider>" not in text


def test_production_materials_generate_real_albedo_and_normal_texture_detail():
    text = read(MATERIALS)
    assert "TextureSize = 256" in text
    assert "EnsureSurfaceTexture" in text
    assert "EnsureNormalTexture" in text
    assert "Fractal(" in text
    assert "Mathf.PerlinNoise" in text
    assert 'material.SetTexture("_BaseMap", albedo)' in text
    assert 'material.SetTexture("_BumpMap", normal)' in text
    assert 'material.EnableKeyword("_NORMALMAP")' in text
    assert "anisoLevel = 8" in text
    for name in (
        "ProdIvoryStoneV09",
        "ProdPearlCeramicV09",
        "ProdWarmStoneV09",
        "ProdGraphiteV09",
        "ProdGoldV09",
        "ProdGardenV09",
    ):
        assert name in text


def test_production_mesh_library_uses_real_curved_meshes_not_line_renderers():
    text = read(MESHES)
    assert "BuildFlutedColumn" in text
    assert "BuildPointedArch" in text
    assert "BuildSpire" in text
    assert "BuildCanopy" in text
    assert "RecalculateNormals" in text
    assert "RecalculateTangents" in text
    assert "LineRenderer" not in text
    assert "GameObject.CreatePrimitive" not in text


def test_v09_city_spacing_and_depth_are_explicit():
    text = read(ART)
    assert 'new Vector3(9.8f, 0.22f, 40f)' in text
    assert 'new Vector3(2.6f, 0.17f, 40f)' in text
    assert 'new Vector3(side * 9.2f, -0.16f, 2f)' in text
    assert 'float z = -15f + i * 6.0f' in text
    assert "BuildSkyline" in text
    assert "fogStartDistance = 92f" in text
    assert "fogEndDistance = 320f" in text
    assert "BuildReflectionCoverage" in text
    assert text.count("CreateProbe(root") >= 4


def test_v09_production_art_does_not_create_gameplay_authority():
    text = read(ART)
    forbidden = (
        "AddComponent<JourneyEnemyController>",
        "AddComponent<CombatantVitals>",
        "AddComponent<GuardianMotor>",
        "AddComponent<Rigidbody>",
        "AddComponent<JourneyGate>",
        "AddComponent<WorldInteraction",
        "NeuralEvent",
        "UdpNeuralReceiver",
        "TakeDamage",
    )
    for token in forbidden:
        assert token not in text
    assert "UnityEngine.Object.DestroyImmediate(collider)" in text
    assert "GetComponentsInChildren<Collider>" not in text


def test_local_asset_ingestion_is_gitignored_and_strips_external_authority():
    external = read(EXTERNAL)
    gitignore = read(GITIGNORE)
    assert 'LocalRoot = "Assets/Mindforge/LocalArt"' in external
    assert "unity/Assets/Mindforge/LocalArt/" in gitignore
    assert "PrefabUtility.InstantiatePrefab" in external
    assert "StripGameplayAuthority" in external
    assert "colliders[i].enabled = false" in external
    assert "DestroyImmediate(bodies[i])" in external
    assert "behaviours[i].enabled = false" in external
    assert "FitToSize" in external
    for role in ("Column", "Arch", "Door", "Spire", "Tree", "Humanoid", "Robot"):
        assert role in external


def test_local_asset_replacement_can_swap_environment_roles_without_touching_mindforge_collision():
    text = read(REPLACEMENT)
    assert "ExternalArtDropV09.TryInstantiateBest" in text
    assert "Role.Tree" in text
    assert "Role.Arch" in text
    assert "Role.Spire" in text
    assert "Role.Column" in text
    assert "old[r].enabled = false" in text
    for forbidden in ("JourneyEnemyController", "CombatantVitals", "Rigidbody", "Collider"):
        assert forbidden not in text


def test_third_party_manifest_records_magictools_material_maker_and_conservative_quaternius_policy():
    manifest = json.loads(read(MANIFEST))
    entries = {entry["id"]: entry for entry in manifest["entries"]}
    assert entries["ellisonleao.magictools"]["usage"] == "reference_only"
    assert entries["rodzill4.material_maker"]["license"] == "MIT"
    quaternius = entries["quaternius.production_art_source"]
    assert quaternius["usage"] == "local_asset_source"
    assert quaternius["vendored_paths"] == []
    assert "never committed" in quaternius["asset_policy"]
    assert manifest["policy"]["local_only_restricted_source_art_must_be_gitignored"] is True


def test_guardian_fallback_replaces_visible_primitives_without_replacing_pose_or_physics_authority():
    text = read(GUARDIAN)
    assert 'transform.Find("GuardianShowcaseAvatar")' in text
    assert "Renderer[] legacy = avatar.GetComponentsInChildren<Renderer>(true);" in text
    assert "legacy[i].enabled = false" in text
    assert 'Node("ProductionGuardianV09"' in text
    for token in (
        "BuildSuperEllipsoid",
        "BuildTaperedCylinder",
        "BuildTaperedPrism",
        "BuildTorus",
        "BuildCurvedMantle",
        "Helmet",
        "ChestShell",
        "Pauldron",
        "Thigh",
        "Shin",
    ):
        assert token in text
    for forbidden in (
        "GetComponent<Rigidbody>",
        "GetComponent<Collider>",
        "GetComponentsInChildren<Collider>",
        "AddComponent<Rigidbody>",
        "AddComponent<Collider>",
        "AddComponent<GuardianMotor>",
        "AddComponent<GuardianCombatInput>",
        "TakeDamage",
    ):
        assert forbidden not in text
    assert 'Transform torso = avatar.Find("Torso")' in text
    assert 'BuildArm(avatar.Find("LeftArm")' in text
    assert 'BuildLeg(avatar.Find("LeftLeg")' in text


def test_guardian_renderer_replacement_cannot_hide_the_aetherblade_sibling_rig():
    guardian = read(GUARDIAN)
    arsenal = read(PHYSICAL_ARSENAL)
    assert 'transform.Find("GuardianShowcaseAvatar")' in guardian
    assert "avatar.GetComponentsInChildren<Renderer>(true)" in guardian
    assert 'new GameObject("PhysicalArsenalRig")' in arsenal
    assert "arsenalRoot.transform.SetParent(guardian.transform, false);" in arsenal
    assert 'NewChild("SwordRoot", arsenalRoot.transform' in arsenal
    # ProductionGuardian only suppresses renderers below GuardianShowcaseAvatar. The blade
    # remains a sibling under the Guardian root and therefore stays outside that renderer set.
    assert "transform.GetComponentsInChildren<Renderer>" not in guardian


def test_fractured_echo_gets_a_lifecycle_safe_production_reliquary_shell():
    text = read(ECHO)
    assert "FracturedEchoNode echo" in text
    assert "echo.Shattered += OnShattered" in text
    assert "echo.Reconstructed += OnReconstructed" in text
    assert "BuildFacetedGem" in text
    assert "BuildTorus" in text
    assert '"EchoCoreSignal"' in text
    assert '"EchoOuterRing"' in text
    assert '"EchoShard_' in text
    assert "CaptureAndHideLegacyRenderers" in text
    assert "HideLegacyRenderers();" in text
    assert "_visualRoot.gameObject.SetActive(false)" in text
    assert "_visualRoot.gameObject.SetActive(true)" in text
    assert "Destroy(_ownedMeshes[i])" in text
    for forbidden in (
        "Rigidbody",
        "Collider",
        "TakeDamage",
        "SetExternalPause",
        "projectilePrefab",
        "Input.Get",
        "UdpNeuralReceiver",
    ):
        assert forbidden not in text


def test_spawned_boss_echoes_are_skinned_by_a_bounded_presentation_only_scanner():
    text = read(ECHO_BOOTSTRAP)
    assert "scanIntervalSeconds = 0.35f" in text
    assert "Time.unscaledTime < _nextScan" in text
    assert "FindObjectsOfType<FracturedEchoNode>(true)" in text
    assert "echo.gameObject.AddComponent<ProductionEchoVisualV09>()" in text
    assert "visual.ConfigureRuntime(shell, hostile, trim)" in text
    for forbidden in (
        "Rigidbody",
        "Collider",
        "TakeDamage",
        "SetExternalPause",
        "ConfigureWorldEcho",
        "Input.Get",
        "NeuralEvent",
    ):
        assert forbidden not in text


def test_compact_hud_replaces_debug_panels_without_claiming_neural_authority():
    text = read(HUD)
    assert "SuppressLegacyHuds" in text
    assert "combatHud.enabled = false" in text
    assert "worldHud.enabled = false" in text
    assert "journeyHud.enabled = false" in text
    assert "CurrentObjective()" in text
    assert '"SHOWCASE  ·  BCI OFF"' in text
    assert '"NEURAL LINK  ·  READY"' in text
    assert '"NEURAL LINK  ·  ATTUNE"' in text
    assert "tutorialSeconds = 12f" in text
    for forbidden in ("NeuralEvent", "UdpNeuralReceiver", "EnterControllerOnly", "CalibrationReady ="):
        assert forbidden not in text


def test_deferred_scene_save_hook_runs_v09_after_the_synchronous_v08_visual_stack():
    text = read(HOOK)
    assert "EditorSceneManager.sceneSaved += _ =>" in text
    assert "if (!_applying) EditorApplication.delayCall += TryApply;" in text
    assert "EditorSceneManager.sceneSaved += _ => TryApply();" not in text
    assert "ReferenceFidelityReady()" in text
    assert "ProductionArtV09Builder.ApplyOpenScene();" in text
    assert "ExternalArtReplacementV09.ApplyOpenScene();" in text
    assert "arena.AddComponent<ProductionHudV09>()" in text
    assert "arena.AddComponent<ProductionEchoVisualBootstrapV09>()" in text
    assert "echoBootstrap.ConfigureRuntime(graphite, hostile, gold);" in text
    assert "guardian.AddComponent<ProductionGuardianV09>()" in text
    assert "production.ConfigureRuntime(pearl, graphite, gold, aether);" in text


def test_v09_new_runtime_scripts_have_unique_unity_meta_guids():
    assets = ROOT / "unity/Assets"
    metas = [Path(str(ECHO) + ".meta"), Path(str(ECHO_BOOTSTRAP) + ".meta")]
    for meta in metas:
        assert meta.exists(), meta
        guid_line = next(line for line in meta.read_text(encoding="utf-8").splitlines() if line.startswith("guid: "))
        guid = guid_line.split(":", 1)[1].strip()
        assert len(guid) == 32
        matches = []
        for candidate in assets.rglob("*.meta"):
            if f"guid: {guid}" in candidate.read_text(encoding="utf-8", errors="ignore"):
                matches.append(candidate)
        assert matches == [meta]
