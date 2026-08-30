from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
BUILDER = ROOT / "unity/Assets/Mindforge/Editor/ProductionNeuralSanctumV09Builder.cs"
MESHES = ROOT / "unity/Assets/Mindforge/Editor/ProductionCalibrationMeshLibraryV09.cs"
MOTION = ROOT / "unity/Assets/Mindforge/Presentation/ProductionCalibrationPresentationV09.cs"
HOOK = ROOT / "unity/Assets/Mindforge/Editor/ProductionArtAutoHookV09.cs"


def test_calibration_meshes_replace_stock_placeholder_language_with_versioned_recipes():
    text = MESHES.read_text(encoding="utf-8")
    assert 'ResonanceLensPath = Root + "/ResonanceLens.asset"' in text
    assert 'PhaseRingPath = Root + "/PhaseRing.asset"' in text
    assert 'MembranePanelPath = Root + "/ThresholdMembranePanel.asset"' in text
    assert "RecipeVersion = 2" in text
    assert "BuildResonanceLens" in text
    assert "BuildTorus(52, 10" in text
    assert "BuildMembranePanel(8, 12)" in text
    assert "ValidateMesh(mesh, path)" in text
    assert "PrimitiveType" not in text


def test_resonance_interaction_keeps_existing_flicker_renderer_and_authority():
    text = BUILDER.read_text(encoding="utf-8")
    assert 'FindNamed(station, "ResonanceOrb")' in text
    assert "station.GetComponent<SanctumCalibrationOrbV08>()" in text
    assert "coreFilter.sharedMesh = lens" in text
    assert "coreRenderer.enabled = true" in text
    assert "existing render target" in text
    assert "AddComponent<SanctumCalibrationOrbV08>" not in text
    assert "ConfigureRuntime($\"sanctum.resonance" not in text
    assert "MaterialPropertyBlock" not in text
    assert "nominalFrequencyHz" not in text


def test_production_station_retires_legacy_plinth_stem_and_line_orbits_only():
    text = BUILDER.read_text(encoding="utf-8")
    for name in ("Plinth", "GoldStem", "OrbitalA", "OrbitalB"):
        assert f'HideLegacyStationRenderer(station, "{name}")' in text
    for motif in ("ChapelArch", "Pedestal", "PhaseRingA", "PhaseRingB", "LeftNeedle", "RightNeedle"):
        assert f'"{motif}"' in text
    assert "ExpectedStationCount = 3" in text
    assert "MaxAddedRenderers = 32" in text


def test_threshold_visual_moves_with_existing_gate_blocker_without_mutating_collision():
    text = BUILDER.read_text(encoding="utf-8")
    assert 'FindNamed(sanctum.transform, "ThresholdSeal")' in text
    assert "Collider blocker = seal.GetComponent<Collider>()" in text
    assert "!blocker.enabled" in text
    assert "legacyRenderer.enabled = false" in text
    assert "root.transform.SetParent(seal, false)" in text
    assert '"MembraneLeft"' in text and '"MembraneCenter"' in text and '"MembraneRight"' in text
    assert '"PearlSpine"' in text
    assert '"GoldSeamLeft"' in text and '"GoldSeamRight"' in text
    for forbidden in (
        "blocker.enabled = false",
        "DestroyImmediate(blocker",
        "AddComponent<Collider",
        "AddComponent<BoxCollider",
        "AddComponent<MeshCollider",
        "JourneyGate gate =",
        "ConfigureRuntime(seal.transform",
    ):
        assert forbidden not in text


def test_neural_sanctum_visual_roots_fail_closed_on_authority_or_budget_growth():
    text = BUILDER.read_text(encoding="utf-8")
    assert "AssertVisualOnly(apparatus.gameObject" in text
    assert "AssertVisualOnly(membrane.gameObject" in text
    assert "GetComponentsInChildren<Collider>(true).Length != 0" in text
    assert "GetComponentsInChildren<Rigidbody>(true).Length != 0" in text
    assert "GetComponentsInChildren<Light>(true).Length != 0" in text
    assert "renderer budget exceeded" in text


def test_resonance_motion_is_mechanical_only_and_never_modulates_stimulus_luminance():
    text = MOTION.read_text(encoding="utf-8")
    assert "Time.unscaledDeltaTime" in text
    assert "phaseRingA.Rotate" in text
    assert "phaseRingB.Rotate" in text
    for forbidden in (
        "Renderer",
        "MaterialPropertyBlock",
        "EmissionColor",
        "BaseColor",
        "NeuralEvent",
        "UdpNeuralReceiver",
        "SanctumCalibrationOrbV08",
        "Mathf.Sin",
        "Math.Sin",
    ):
        assert forbidden not in text


def test_complete_v09_pipeline_requires_neural_sanctum_before_postfx_and_noop_state():
    text = HOOK.read_text(encoding="utf-8")
    story = text.find("EnsureStorytelling(production);")
    neural = text.find("EnsureNeuralSanctum(production);")
    post = text.find("EnsurePostFx(production);")
    assert story >= 0
    assert neural > story
    assert post > neural
    assert "production.transform.Find(ProductionNeuralSanctumV09Builder.RootName) == null" in text
    assert "ProductionNeuralSanctumV09Builder.ApplyOpenScene();" in text
