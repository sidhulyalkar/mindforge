from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
UNITY = ROOT / "unity" / "Assets" / "Mindforge"


def read(*parts: str) -> str:
    return UNITY.joinpath(*parts).read_text(encoding="utf-8")


def test_gameplay_fov_is_wider_but_fixed_across_movement_and_jump_state():
    camera = read("Presentation", "ShowcaseCameraRig.cs")

    assert "gameplayFieldOfView = 58f" in camera
    assert "gameplayCamera.fieldOfView = Mathf.Clamp(gameplayFieldOfView, 45f, 75f)" in camera
    assert "Keep FOV fixed rather than speed-reactive" in camera

    # Never couple camera projection to movement velocity/dash/jump state. This keeps the
    # world-space VEP projection stable over time even though the fixed framing changed.
    assert "motor.Velocity" not in camera
    assert "motor.IsDashing" not in camera
    assert "motor.IsGrounded" not in camera
    assert "gameplayCamera.fieldOfView = Mathf.Lerp" not in camera
    assert "gameplayCamera.fieldOfView = Mathf.SmoothDamp" not in camera


def test_unfrozen_guardian_gets_nonpenetrating_world_entry_and_respawn_markers():
    motor = read("Combat", "GuardianMotor.cs")
    traversal = read("Editor", "NullWardTraversalPlayabilityBuilder.cs")

    assert "RigidbodyConstraints.FreezePositionY" in motor
    assert "_body.constraints &= ~(RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezePositionY)" in motor
    assert "GuardianGroundedSpawnY = 0.72f" in traversal
    assert 'NormalizeMarkerHeight(ward.transform, "NullWard_WorldStart", GuardianGroundedSpawnY)' in traversal
    assert 'NormalizeMarkerHeight(ward.transform, "MemoryForge_Respawn", GuardianGroundedSpawnY)' in traversal
    assert "deterministic physical clearance" in traversal


def test_one_click_showcase_builds_world_visuals_then_traversal_geometry_before_audit():
    showcase = read("Editor", "ShowcaseEditorMenu.cs")

    stages = (
        "NullWardSceneBuilder.BuildOpenScene();",
        "NullWardVisualInfrastructureBuilder.ApplyOpenScene();",
        "NullWardTraversalPlayabilityBuilder.ApplyOpenScene();",
        "CompetitionGateValidator.ValidateAndWrite(false);",
        "PresentationBudgetAudit.Run();",
    )
    indices = [showcase.index(stage) for stage in stages]
    assert indices == sorted(indices)
