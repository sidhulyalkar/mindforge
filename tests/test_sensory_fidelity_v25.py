from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
EDITOR = ROOT / "unity" / "Assets" / "Mindforge" / "Editor"
PRESENTATION = ROOT / "unity" / "Assets" / "Mindforge" / "Presentation"
SHADERS = ROOT / "unity" / "Assets" / "Mindforge" / "Shaders"


def read(path: Path) -> str:
    return path.read_text(encoding="utf-8")


def test_v25_promotes_existing_cinematic_renderer_instead_of_forking_pipeline():
    builder = read(EDITOR / "SensoryFidelityV25Builder.cs")
    cinematic = read(EDITOR / "CinematicFidelityConfigurator.cs")
    assert 'RootName = "Mindforge_Sensory_Fidelity_V25"' in builder
    assert "CinematicFidelityConfigurator.Configure();" in builder
    assert "ScreenSpaceAmbientOcclusion" in cinematic
    assert "ScreenSpaceShadows" in cinematic
    assert "m_RequireDepthTexture" in cinematic
    assert "shadowCascadeCount = 4" in cinematic


def test_v25_post_stack_is_restrained_and_gameplay_readable():
    builder = read(EDITOR / "SensoryFidelityV25Builder.cs")
    for token in (
        "VolumeProfile",
        "TonemappingMode.ACES",
        "Bloom",
        "ColorAdjustments",
        "WhiteBalance",
        "Vignette",
        "renderPostProcessing = true",
        "allowHDR = true",
        "SubpixelMorphologicalAntiAliasing",
    ):
        assert token in builder
    for forbidden in ("DepthOfField", "MotionBlur", "ChromaticAberration"):
        assert forbidden not in builder


def test_v25_data_cathedral_inlays_are_static_and_collider_free():
    builder = read(EDITOR / "SensoryFidelityV25Builder.cs")
    assert '"V25_Data_Cathedral_Inlays"' in builder
    assert '"Procession_Left"' in builder
    assert '"Choir_Ascent"' in builder
    assert '"Apse_North"' in builder
    assert "CathedralRoleV24.StructuralRole.MysticAccent" in builder
    assert "false);" in builder
    assert "colliders.Length != 0" in builder
    for forbidden in ("Update()", "LateUpdate()", "FixedUpdate()", "Mathf.Sin(", "Time.time"):
        assert forbidden not in builder


def test_v16_cannot_repaint_current_cathedral_back_to_greybox_palette():
    legacy = read(PRESENTATION / "LegacyMaterialHierarchyV16.cs")
    assert "IsCurrentAuthoredMaterial(material)" in legacy
    assert 'StartsWith("V24_"' in legacy
    assert 'StartsWith("V25_"' in legacy
    assert "PreservedAuthoredRendererCount" in legacy


def test_v25_runtime_replaces_utilitarian_hud_and_promotes_pooled_feedback():
    runtime = read(PRESENTATION / "MindforgeSensoryFidelityV25.cs")
    assert "MindforgeDemoHudV17 v17Hud" in runtime
    assert "v17Hud.enabled = false" in runtime
    assert "AddComponent<CombatVfxOrchestrator>()" in runtime
    assert "MindforgeDemoHudV25" in runtime
    assert "MindforgeDiegeticGuideV25" in runtime
    assert "MindforgeLocomotionVfxV25" in runtime
    assert "MindforgeCameraImpactV25" in runtime
    assert "MindforgeSpatialAudioV25" in runtime
    assert "PresentationFxPool.GetOrCreate()" in runtime


def test_v25_feedback_is_downstream_of_authoritative_events_only():
    runtime = read(PRESENTATION / "MindforgeSensoryFidelityV25.cs")
    for token in (
        "_physical.SwordHit += OnSwordHit",
        "_physical.PerfectGuard += OnPerfectGuard",
        "_motor.DashStarted += OnDash",
        "_motor.DoubleJumped += OnDoubleJump",
        "_motor.Landed += OnLanded",
        "_bossVitals.Damaged += OnBossDamaged",
    ):
        assert token in runtime
    for forbidden in (
        "SetMoveInput(",
        "RequestDash(",
        "RequestJump(",
        "ApplyDamage(",
        "TakeDamage(",
        "SetExternalPause(",
        "Time.timeScale =",
        "Flux.Award(",
    ):
        assert forbidden not in runtime


def test_fractured_signal_shader_has_depth_and_neural_freeze_contract():
    shader = read(SHADERS / "FracturedSignalV25.shader")
    runtime = read(PRESENTATION / "MindforgeSensoryFidelityV25.cs")
    assert 'Shader "Mindforge/FracturedSignalV25"' in shader
    assert "_Displacement" in shader
    assert "_FresnelStrength" in shader
    assert "GetMainLight" in shader
    assert "_MotionScale" in shader
    assert 'Shader.Find("Mindforge/FracturedSignalV25")' in runtime
    assert 'SetFloat("_MotionScale", motion)' in runtime
    assert "NeuralVisualFieldActive()" in runtime


def test_diegetic_prompts_hide_during_neural_visual_field():
    runtime = read(PRESENTATION / "MindforgeSensoryFidelityV25.cs")
    assert '"T  //  LOCK FRACTURED SIGNAL"' in runtime
    assert '"V HOLD  //  CHANNEL WISP"' in runtime
    assert '"NEURAL WINDOW  //  HOLD GAZE ON THE CODED CORES"' in runtime
    assert '"CALIBRATION  //  FOLLOW THE CODED CORES"' in runtime
    assert "SetVisible(_targetText, false);" in runtime
    assert "SetVisible(_guardianText, false);" in runtime
    assert "CalibrationStimuliActive" in runtime
    assert "ResonanceWindowActive" in runtime


def test_v25_spatial_audio_is_restrained_and_suppressed_for_neural_windows():
    runtime = read(PRESENTATION / "MindforgeSensoryFidelityV25.cs")
    assert "spatialBlend = 1f" in runtime
    assert "rolloffMode = AudioRolloffMode.Logarithmic" in runtime
    assert "BuildTone(" in runtime
    assert "_bossHum.volume = neural ? 0f" in runtime
    assert "_playerFx.volume = neural ? 0f" in runtime
