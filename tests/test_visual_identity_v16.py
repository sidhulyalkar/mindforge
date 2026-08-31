from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PRESENTATION = ROOT / "unity/Assets/Mindforge/Presentation"
INSTALLER = PRESENTATION / "VisualIdentityV16Installer.cs"
MATERIALS = PRESENTATION / "LegacyMaterialHierarchyV16.cs"
OCCLUSION = PRESENTATION / "CameraOcclusionGhostV16.cs"
BACKDROP = PRESENTATION / "WorldDepthBackdropV16.cs"
SILHOUETTE = PRESENTATION / "CombatSilhouetteV16.cs"
HUD = PRESENTATION / "ProductionHudV09.cs"


def read(path: Path) -> str:
    assert path.exists(), f"missing V0.16 source: {path}"
    return path.read_text(encoding="utf-8")


def test_v16_installer_is_presentation_only_and_composes_readability_layers():
    text = read(INSTALLER)
    assert 'RootName = "Mindforge_VisualIdentity_V16"' in text
    for component in (
        "LegacyMaterialHierarchyV16",
        "CameraOcclusionGhostV16",
        "WorldDepthBackdropV16",
        "CombatSilhouetteV16",
    ):
        assert f"AddComponent<{component}>" in text
    for forbidden in (
        "ReceiveDamage",
        "TakeDamage",
        "AddForce",
        "GuardianMotor",
        "NeuralEvent",
        "UdpNeuralReceiver",
        "BeginWindow(",
        "EndWindow(",
    ):
        assert forbidden not in text


def test_material_hierarchy_preserves_coded_and_emissive_signal_renderers():
    text = read(MATERIALS)
    assert "MaterialPropertyBlock" in text
    assert '"SightVepCore"' in text
    assert '"GuardVepCore"' in text
    assert 'material.IsKeywordEnabled("_EMISSION")' in text
    assert "renderer.SetPropertyBlock(_block)" in text
    assert "while (NeuralEvidenceOwnsVisualField()) yield return null;" in text
    for forbidden in (
        "renderer.sharedMaterial =",
        "collider.enabled",
        "SetActive(",
        "VepAuraStimulus",
        "BeginWindow(",
        "EndWindow(",
    ):
        assert forbidden not in text


def test_camera_occlusion_changes_renderer_visibility_only_and_freezes_for_eeg():
    text = read(OCCLUSION)
    assert "bounds.IntersectRay" in text
    assert "renderer.enabled = targetEnabled" in text
    assert "NeuralEvidenceOwnsVisualField" in text
    assert "_calibration.CalibrationInProgress" in text
    assert "_wisp.CalibrationStimuliActive" in text
    assert "_wisp.ResonanceWindowActive" in text
    assert "if (NeuralEvidenceOwnsVisualField()) return;" in text
    for forbidden in (
        "Collider.enabled",
        "collider.enabled",
        "SetActive(",
        "Destroy(collider",
        "transform.position =",
        "transform.localPosition =",
        "BeginWindow(",
        "EndWindow(",
    ):
        assert forbidden not in text


def test_backdrop_is_static_non_emissive_and_collider_free():
    text = read(BACKDROP)
    assert 'RootName = "Mindforge_WorldDepthBackdrop_V16"' in text
    assert "Skyline_L" in text
    assert "SideSpire_" in text
    assert "HorizonShelfNorth" in text
    assert "while (NeuralEvidenceOwnsVisualField()) yield return null;" in text
    assert "collider.enabled = false" in text
    assert "Destroy(collider)" in text
    assert "shadowCastingMode" in text
    for forbidden in (
        "_EmissionColor",
        "EnableKeyword(\"_EMISSION\")",
        "Input.Get",
        "Rigidbody",
        "CombatantVitals",
        "NeuralEvent",
        "frequencyHz",
    ):
        assert forbidden not in text


def test_combat_silhouette_is_visual_only_and_not_periodically_animated():
    text = read(SILHOUETTE)
    assert '"GuardianReadabilityV16"' in text
    assert '"EnemyReadabilityV16"' in text
    assert "collider.enabled = false" in text
    assert "Destroy(collider)" in text
    assert "while (NeuralEvidenceOwnsVisualField()) yield return null;" in text
    assert "if (!_ready || NeuralEvidenceOwnsVisualField()) return;" in text
    for forbidden in (
        "Mathf.Sin",
        "Mathf.Cos",
        "Time.time",
        "Input.Get",
        "ReceiveDamage",
        "TakeDamage",
        "AddComponent<Rigidbody>",
        "BeginWindow(",
        "EndWindow(",
    ):
        assert forbidden not in text


def test_v16_hud_has_clear_player_target_neural_and_objective_hierarchy():
    text = read(HUD)
    for token in (
        "DrawGuardianPanel",
        "DrawTargetPanel",
        "DrawNeuralChip",
        "DrawObjective",
        "DrawNeuralAffordance",
        "ResolveTargetVitals",
        "NEURAL WINDOW  ·  KEEP GAZE ON BLUE / GREEN",
        "V HOLD  ·  CHANNEL WISP",
        "GuardianTargetLock",
        "SoulWispController",
    ):
        assert token in text
    assert "target.GetComponentInParent<CombatantVitals>()" in text
    assert "targetLock.Target" in text
    for forbidden in (
        "targetLock.SetLocked",
        "targetLock.AcquireBest",
        "Input.GetKey",
        "Input.GetMouse",
        "NeuralEvent",
        "UdpNeuralReceiver",
    ):
        assert forbidden not in text
