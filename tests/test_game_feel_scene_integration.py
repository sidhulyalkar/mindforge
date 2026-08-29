from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
UNITY = ROOT / "unity" / "Assets" / "Mindforge"


def read(*parts: str) -> str:
    return UNITY.joinpath(*parts).read_text(encoding="utf-8")


def test_gameplay_fov_is_wider_but_fixed_across_movement_jump_and_mount_state():
    camera = read("Presentation", "ShowcaseCameraRig.cs")

    assert "gameplayFieldOfView = 58f" in camera
    fixed_assignment = "gameplayCamera.fieldOfView = Mathf.Clamp(gameplayFieldOfView, 45f, 75f)"
    assert fixed_assignment in camera
    assert camera.count(fixed_assignment) == 1
    assert "Deliberately fixed across foot, jump, hover and mounted speed" in camera

    # Camera position may respond to mounted velocity, but projection may not. Keeping
    # speed out of the FOV block preserves stable optical scale for the coded VEP targets.
    assert "motor.Velocity" not in camera
    assert "motor.IsDashing" not in camera
    assert "motor.IsGrounded" not in camera
    assert "hoverbike.PlanarVelocity" in camera
    assert "gameplayCamera.fieldOfView = Mathf.Lerp" not in camera
    assert "gameplayCamera.fieldOfView = Mathf.SmoothDamp" not in camera
    fov_block = camera[camera.index("if (gameplayCamera != null)"):]
    assert "hoverbike.PlanarVelocity" not in fov_block
    assert "hoverbike.Speed01" not in fov_block


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
