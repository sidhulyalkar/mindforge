from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
LIGHTING = ROOT / "unity/Assets/Mindforge/Editor/ProductionLightingV09Builder.cs"
HOOK = ROOT / "unity/Assets/Mindforge/Editor/ProductionArtAutoHookV09.cs"


def test_production_lighting_enforces_one_shadowed_directional_sun():
    text = LIGHTING.read_text(encoding="utf-8")
    assert "EnforceSingleDirectionalSun(sun)" in text
    assert "light.type != LightType.Directional" in text
    assert "light.enabled = false" in text
    assert "RenderSettings.sun = sun" in text
    assert "enabledDirectionals != 1" in text
    assert "sun.shadows = LightShadows.Soft" in text
    assert "sun.intensity = 1.16f" in text


def test_opening_fill_stack_is_reduced_to_two_low_cost_accents():
    text = LIGHTING.read_text(encoding="utf-8")
    for name in ("SanctumFillA", "SanctumFillB", "ThresholdFill", "CourtFill"):
        assert f'"{name}"' in text
    assert 'string.Equals(light.name, "SanctumFillA"' in text
    assert 'string.Equals(light.name, "ThresholdFill"' in text
    assert "light.shadows = LightShadows.None" in text
    assert "light.intensity = 0.42f" in text
    assert "light.intensity = 0.50f" in text
    assert "MaxOpeningAccentLights = 3" in text


def test_memory_forge_accent_is_tamed_not_removed():
    text = LIGHTING.read_text(encoding="utf-8")
    assert '"Memory_Forge_Sanctum_Altar_V08"' in text
    assert "light.intensity = 0.52f" in text
    assert "light.range = 5.5f" in text
    assert "light.shadows = LightShadows.None" in text


def test_lighting_pass_remains_presentation_only():
    text = LIGHTING.read_text(encoding="utf-8")
    for forbidden in (
        "AddComponent<Collider",
        "AddComponent<Rigidbody",
        "GuardianMotor",
        "JourneyEnemyController",
        "CombatantVitals",
        "NeuralEvent",
        "UdpNeuralReceiver",
        "AwakeningCalibrationDirector",
        "SanctumCalibrationOrbV08",
        "Time.timeScale",
    ):
        assert forbidden not in text


def test_complete_v09_pipeline_requires_lighting_between_neural_sanctum_and_postfx():
    text = HOOK.read_text(encoding="utf-8")
    neural = text.find("EnsureNeuralSanctum(production);")
    lighting = text.find("EnsureLighting(production);")
    post = text.find("EnsurePostFx(production);")
    assert neural >= 0
    assert lighting > neural
    assert post > lighting
    assert "production.transform.Find(ProductionLightingV09Builder.RootName) == null" in text
    assert "ProductionLightingV09Builder.ApplyOpenScene();" in text
