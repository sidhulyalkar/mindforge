from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
RUNTIME = ROOT / "dragonsouls_overlay" / "Assets" / "Mindforge" / "Runtime"
EDITOR = ROOT / "dragonsouls_overlay" / "Assets" / "Mindforge" / "Editor"
CAMERA = RUNTIME / "MindforgeThirdPersonCameraPresentationV33.cs"
INTEGRATION = RUNTIME / "MindforgeBciIntegrationRuntimeV33.cs"
READINESS = EDITOR / "MindforgeBciReadinessV33.cs"


def read(path: Path) -> str:
    assert path.exists(), f"missing V0.33 playthrough camera source: {path}"
    return path.read_text(encoding="utf-8")


def test_v33_installs_a_scoped_third_person_camera_presentation_layer():
    camera = read(CAMERA)
    runtime = read(INTEGRATION)

    assert "public const float PreferredFieldOfView = 55f;" in camera
    assert "public const float MinimumFollowDistance = 5.6f;" in camera
    assert 'name.IndexOf("FreeLook"' in camera
    assert 'name.IndexOf("TargetState"' in camera
    assert 'name.IndexOf("Aim"' in camera
    assert 'name.IndexOf("Bonfire"' in camera
    assert "CinemachineTransposer" in camera
    assert "Mathf.Min(offset.z, -minimumFollowDistance)" in camera
    assert "target.IsChildOf(playerRoot)" in camera
    assert "Install<MindforgeThirdPersonCameraPresentationV33>();" in runtime


def test_v33_camera_layer_does_not_take_gameplay_or_camera_switching_authority():
    camera = read(CAMERA)

    for forbidden in (
        "CharacterController.Move",
        "PlayerStateMachine.State",
        "BossManager",
        "EnemyNightmareDragonController",
        "health.CurrentHealth",
        "transform.position =",
        "m_Priority =",
        "CinemachineBrain",
        "StateDrivenCamera",
    ):
        assert forbidden not in camera

    assert "Aim/bonfire framing remains inherited" in camera


def test_v33_readiness_audits_runtime_and_static_camera_contracts():
    readiness = read(READINESS)

    assert "MindforgeThirdPersonCameraPresentationV33 cameraPresentation" in readiness
    assert 'failures.Add("third_person_camera_presentation_runtime")' in readiness
    assert "cameraPresentation.CandidateCameraCount < 1" in readiness
    assert "MindforgeThirdPersonCameraPresentationV33.PreferredFieldOfView, 55f" in readiness
    assert "MindforgeThirdPersonCameraPresentationV33.MinimumFollowDistance, 5.6f" in readiness
    assert 'failures.Add("third_person_camera_static_contract")' in readiness
