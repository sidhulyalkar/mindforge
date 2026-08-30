from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
MESHES = ROOT / "unity/Assets/Mindforge/Editor/ProductionHorizonMeshLibraryV09.cs"
HORIZON = ROOT / "unity/Assets/Mindforge/Editor/ProductionHorizonV09Builder.cs"
HOOK = ROOT / "unity/Assets/Mindforge/Editor/ProductionArtAutoHookV09.cs"
CAMERA = ROOT / "unity/Assets/Mindforge/Presentation/ShowcaseCameraRig.cs"


def test_horizon_uses_versioned_deterministic_ridge_meshes():
    text = MESHES.read_text(encoding="utf-8")
    assert 'MidRidgePath = Root + "/MidRidge.asset"' in text
    assert 'FarRidgePath = Root + "/FarRidge.asset"' in text
    assert "RecipeVersion = 1" in text
    assert "BuildRidgeRing(128" in text
    assert "BuildRidgeRing(160" in text
    assert "AddQuadFacingInward" in text
    assert "Validate(mesh, path)" in text
    assert "PrimitiveType" not in text


def test_reference_phase_ring_idea_is_restored_as_real_production_mesh_geometry():
    text = HORIZON.read_text(encoding="utf-8")
    assert "ProductionCalibrationMeshLibraryV09.PhaseRing()" in text
    assert "ProductionMeshLibraryV09.PointedArch()" in text
    for landmark in (
        "Neural_MegaRing_Central",
        "Neural_MegaRing_Central_Gold",
        "Neural_MegaRing_West",
        "Neural_MegaRing_East",
        "Horizon_Cathedral_Arch",
        "Horizon_Cathedral_Arch_Gold",
    ):
        assert f'"{landmark}"' in text
    assert "LineRenderer" not in text
    assert "PrimitiveType" not in text


def test_horizon_adds_natural_depth_beyond_city_without_reachable_geometry():
    text = HORIZON.read_text(encoding="utf-8")
    assert "MidRidgeRadius = 185f" in text
    assert "FarRidgeRadius = 275f" in text
    assert '"Mid_Foothill_Ring"' in text
    assert '"Far_Mountain_Ring"' in text
    assert "GetComponentsInChildren<Collider>(true).Length != 0" in text
    assert "GetComponentsInChildren<Rigidbody>(true).Length != 0" in text
    assert "GetComponentsInChildren<Light>(true).Length != 0" in text
    assert "MaxHorizonRenderers = 18" in text


def test_distant_world_spends_no_realtime_shadow_or_probe_budget():
    text = HORIZON.read_text(encoding="utf-8")
    assert "renderer.shadowCastingMode = ShadowCastingMode.Off" in text
    assert "renderer.receiveShadows = false" in text
    assert "renderer.lightProbeUsage = LightProbeUsage.Off" in text
    assert "renderer.reflectionProbeUsage = ReflectionProbeUsage.Off" in text


def test_horizon_depth_is_compatible_with_fixed_gameplay_far_clip():
    horizon = HORIZON.read_text(encoding="utf-8")
    camera = CAMERA.read_text(encoding="utf-8")
    assert "FarRidgeRadius = 275f" in horizon
    assert "FarRidgeRadius >= 360f" in horizon
    assert "gameplayFarClip = 420f" in camera
    assert "Mathf.Max(420f, gameplayFarClip)" in camera


def test_complete_pipeline_builds_horizon_before_close_range_story_detail():
    text = HOOK.read_text(encoding="utf-8")
    horizon = text.find("EnsureHorizon(production);")
    story = text.find("EnsureStorytelling(production);")
    assert horizon >= 0
    assert story > horizon
    assert "production.transform.Find(ProductionHorizonV09Builder.RootName) == null" in text
    assert "ProductionHorizonV09Builder.ApplyOpenScene();" in text
