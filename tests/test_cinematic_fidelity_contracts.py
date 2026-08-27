from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
UNITY = ROOT / "unity" / "Assets" / "Mindforge"


def read(*parts: str) -> str:
    return UNITY.joinpath(*parts).read_text(encoding="utf-8")


def test_cinematic_showcase_is_an_explicit_layer_above_the_qualified_gameplay_scene():
    menu = read("Editor", "ShowcaseEditorMenu.cs")
    assert "Build + Play Cinematic Showcase" in menu
    order = [
        menu.index("CinematicFidelityConfigurator.Configure()"),
        menu.index("CinematicMaterialAuthoring.EnsureAuthored()"),
        menu.index("CompetitionSceneAssembler.BuildCompetitionScene()"),
        menu.index("ShowcaseSceneDecorator.DecorateOpenScene()"),
        menu.index("CinematicSceneDetailer.EnhanceOpenScene()"),
        menu.index("CompetitionGateValidator.ValidateAndWrite(false)"),
    ]
    assert order == sorted(order)
    assert "BuildAndPlayLegacyAlias" in menu


def test_urp14_fidelity_profile_raises_quality_without_changing_simulation_rate():
    source = read("Editor", "CinematicFidelityConfigurator.cs")
    for token in (
        'ProfileName = "MINDFORGE_CINEMATIC_URP14_V2"',
        "pipeline.renderScale = 1.0f",
        "pipeline.msaaSampleCount = 1",
        "pipeline.shadowDistance = 52f",
        "pipeline.shadowCascadeCount = 4",
        'SetInt(so, "m_MainLightShadowmapResolution", 4096',
        'SetInt(so, "m_AdditionalLightsShadowmapResolution", 2048',
        '"UnityEngine.Rendering.Universal.ScreenSpaceAmbientOcclusion"',
        '"UnityEngine.Rendering.Universal.ScreenSpaceShadows"',
        'SetFloat(ao, "m_Settings.Intensity", 1.35f',
        "QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable",
        "Application.targetFrameRate = 120",
    ):
        assert token in source

    # The fidelity branch must not opportunistically migrate render pipeline or Unity.
    assert "HDRP" not in source
    assert "ProjectVersion" not in source


def test_generated_materials_are_real_pbr_inputs_not_flat_tints():
    source = read("Editor", "CinematicMaterialAuthoring.cs")
    for token in (
        'ResourceFolder = "Assets/Mindforge/Resources/Cinematic"',
        '"ArenaBasalt"',
        '"ObsidianArchitecture"',
        '"GuardianArmor"',
        '"GuardianCloth"',
        '"FracturedShard"',
        '"_Albedo"',
        '"_Normal"',
        '"_MetalSmooth"',
        '"_Occlusion"',
        'material.SetTexture("_BaseMap", albedo)',
        'material.SetTexture("_BumpMap", normal)',
        'material.SetTexture("_MetallicGlossMap", mask)',
        'material.SetTexture("_OcclusionMap", occlusion)',
        'material.EnableKeyword("_NORMALMAP")',
        'material.EnableKeyword("_METALLICSPECGLOSSMAP")',
        "TextureWrapMode.Repeat",
        "FilterMode.Trilinear",
        "anisoLevel = 8",
    ):
        assert token in source


def test_cinematic_scene_detail_is_collider_free_and_adds_reflection_context():
    source = read("Editor", "CinematicSceneDetailer.cs")
    for token in (
        'CinematicRootName = "Mindforge_Cinematic_Fidelity"',
        "BuildFracturedGround",
        "BuildPeripheralRubble",
        "BuildRuinedSilhouette",
        "ReflectionProbeMode.Realtime",
        "ReflectionProbeRefreshMode.OnAwake",
        "probe.boxProjection = true",
        "probe.resolution = 256",
        "renderer.reflectionProbeUsage = ReflectionProbeUsage.BlendProbesAndSkybox",
        "UnityEngine.Object.DestroyImmediate(collider)",
    ):
        assert token in source

    # Set dressing must not become a hidden second physics level.
    assert "collider.enabled = true" not in source
    assert "Rigidbody" not in source


def test_temporal_aa_is_controller_only_so_live_vep_is_not_temporally_filtered():
    source = read("Presentation", "ShowcasePostProcessing.cs")
    assert "ControllerOnlyQualificationActive" in source
    taa = source.index("AntialiasingMode.TemporalAntiAliasing")
    controller_gate = source.index("if (controllerOnly)")
    smaa = source.index("AntialiasingMode.SubpixelMorphologicalAntiAliasing")
    assert controller_gate < taa < smaa
    assert "data.dithering = true" in source
    assert "TonemappingMode.ACES" in source
    assert "FilmGrain" in source
    assert "WhiteBalance" in source

    for forbidden in (
        "VepAuraStimulus",
        "FrequencyHz =",
        "SetTarget(",
        "TryApply(",
        "ReceiveDamage(",
    ):
        assert forbidden not in source


def test_runtime_pbr_rebinding_is_render_only():
    source = read("Presentation", "CinematicRuntimeMaterialOverride.cs")
    for token in (
        'Resources.Load<Material>("Cinematic/GuardianArmor")',
        'Resources.Load<Material>("Cinematic/FracturedShard")',
        'name == "Aetherblade"',
        'name == "VerdantWard"',
        "renderer.sharedMaterial = selected",
        "ReflectionProbeUsage.BlendProbesAndSkybox",
    ):
        assert token in source

    for forbidden in (
        "ReceiveDamage(",
        "TryLightAttack(",
        "SetGuardHeld(",
        "RequestDash(",
        "TrySpend(",
        "Award(",
        "TryApply(",
    ):
        assert forbidden not in source


def test_authored_art_profile_can_replace_visuals_but_strips_second_authority_bodies():
    profile = read("Presentation", "CinematicArtProfile.cs")
    installer = read("Presentation", "CinematicArtOverrideInstaller.cs")
    assert "guardianVisualPrefab" in profile
    assert "fracturedSignalVisualPrefab" in profile
    assert "arenaSetDressPrefab" in profile
    assert 'Resources.Load<CinematicArtProfile>("Cinematic/MindforgeArtProfile")' in installer
    assert 'visual.name = "GuardianAuthoredVisual"' in installer
    assert 'visual.name = "FracturedSignalAuthoredVisual"' in installer
    assert "GetComponentsInChildren<Rigidbody>" in installer
    assert "GetComponentsInChildren<Collider>" in installer
    assert "GetComponentsInChildren<CombatantVitals>" in installer
    assert "GetComponentsInChildren<GuardianCombatInput>" in installer
    assert "GetComponentsInChildren<FracturedSignalDirector>" in installer


def test_production_art_import_rules_preserve_high_frequency_detail_and_linear_maps():
    source = read("Editor", "CinematicAssetImportRules.cs")
    assert 'ArtRoot = "Assets/Mindforge/Art/"' in source
    assert "TextureImporterType.NormalMap" in source
    assert "importer.sRGBTexture = !linearData" in source
    assert "importer.mipmapEnabled = true" in source
    assert "TextureImporterMipFilter.KaiserFilter" in source
    assert "TextureImporterCompression.CompressedHQ" in source
    assert "IsCharacterOrHeroAsset(lower) ? 4096 : 2048" in source
    assert "ModelImporterTangents.CalculateMikk" in source
    assert "ModelImporterMeshCompression.Off" in source
    assert "importer.addCollider = false" in source
