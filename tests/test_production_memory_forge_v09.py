from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
FORGE = ROOT / "unity/Assets/Mindforge/Editor/ProductionMemoryForgeV09Builder.cs"
MOTION = ROOT / "unity/Assets/Mindforge/Presentation/ProductionForgePresentationV09.cs"
HOOK = ROOT / "unity/Assets/Mindforge/Editor/ProductionArtAutoHookV09.cs"


def test_memory_forge_replaces_primitive_shell_with_existing_production_mesh_vocabulary():
    text = FORGE.read_text(encoding="utf-8")
    assert "ProductionMeshLibraryV09.FlutedColumn()" in text
    assert "ProductionMeshLibraryV09.PointedArch()" in text
    assert "ProductionMeshLibraryV09.CathedralSpire()" in text
    assert "ProductionCalibrationMeshLibraryV09.PhaseRing()" in text
    assert "ProductionCalibrationMeshLibraryV09.ResonanceLens()" in text
    assert "ProductionStoryMeshLibraryV09.SignalShard()" in text
    for motif in (
        "FoundationRing",
        "ForgeColumn",
        "ForgeLens",
        "ForgeBackArch",
        "MemoryShardSight",
        "MemoryShardGuard",
        "ForgePhaseRingOuter",
        "ForgePhaseRingInner",
    ):
        assert f'"{motif}"' in text
    assert "PrimitiveType" not in text
    assert "LineRenderer" not in text


def test_old_memory_forge_visuals_are_hidden_but_physical_dais_remains_authoritative():
    text = FORGE.read_text(encoding="utf-8")
    for name in (
        "ForgeDais",
        "ForgeDaisGold",
        "ForgePedestal",
        "ForgeCore",
        "ForgeWing_-1",
        "ForgeWing_1",
        "ForgeHaloOuter",
        "ForgeHaloInner",
    ):
        assert f'"{name}"' in text
    assert "Collider daisCollider" in text
    assert "daisCollider == null || !daisCollider.enabled" in text
    assert "originalDaisCollider == null || !originalDaisCollider.enabled" in text
    # Lock the actual renderer-array mutation rather than a variable-name spelling.
    # The previous assertion expected `renderer.enabled = false` even though the
    # implementation intentionally mutates the indexed renderer collection.
    assert "renderers[r].enabled = false;" in text
    assert "DestroyImmediate(daisCollider" not in text
    assert "daisCollider.enabled = false" not in text


def test_production_forge_cannot_take_checkpoint_interaction_or_physics_authority():
    text = FORGE.read_text(encoding="utf-8")
    assert "MemoryForgeCheckpoint checkpoint" in text
    assert "GetComponentsInChildren<Collider>(true).Length != 0" in text
    assert "GetComponentsInChildren<Rigidbody>(true).Length != 0" in text
    assert "GetComponentsInChildren<Light>(true).Length != 0" in text
    assert "MaxRenderers = 14" in text
    for forbidden in (
        "AddComponent<MemoryForgeCheckpoint>",
        "AddComponent<WorldInteraction",
        "ContextualWorldInteractionRouter",
        "PlayerProfileSaveV06",
        "AddComponent<Collider",
        "AddComponent<Rigidbody",
    ):
        assert forbidden not in text


def test_forge_motion_is_slow_mechanical_transform_motion_only():
    text = MOTION.read_text(encoding="utf-8")
    assert "Time.unscaledDeltaTime" in text
    assert "outerRing.Rotate" in text
    assert "innerRing.Rotate" in text
    assert "outerSpeedDeg = 6f" in text
    assert "innerSpeedDeg = 9f" in text
    for forbidden in (
        "Renderer",
        "MaterialPropertyBlock",
        "Light",
        "Collider",
        "Rigidbody",
        "MemoryForgeCheckpoint",
        "NeuralEvent",
        "Mathf.Sin",
    ):
        assert forbidden not in text


def test_complete_pipeline_builds_forge_before_neural_sanctum_and_lighting():
    text = HOOK.read_text(encoding="utf-8")
    story = text.find("EnsureStorytelling(production);")
    forge = text.find("EnsureMemoryForge(production);")
    neural = text.find("EnsureNeuralSanctum(production);")
    lighting = text.find("EnsureLighting(production);")
    assert story >= 0
    assert forge > story
    assert neural > forge
    assert lighting > neural
    assert "ProductionMemoryForgeV09Builder.ApplyOpenScene();" in text
    assert "transform.Find(ProductionMemoryForgeV09Builder.RootName)" in text
