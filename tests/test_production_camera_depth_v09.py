from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
CAMERA = ROOT / "unity/Assets/Mindforge/Presentation/ShowcaseCameraRig.cs"
CLARITY = ROOT / "unity/Assets/Mindforge/World/SanctumVisualClarityV08.cs"


def test_gameplay_camera_no_longer_overrides_sanctum_skyline_with_140m_clip():
    camera = CAMERA.read_text(encoding="utf-8")
    clarity = CLARITY.read_text(encoding="utf-8")
    assert "gameplayFarClip = 420f" in camera
    assert "gameplayCamera.farClipPlane = Mathf.Max(420f, gameplayFarClip);" in camera
    assert "farClipPlane = 140f" not in camera
    assert "minimumFarClip = 420f" in clarity


def test_skyline_depth_fix_does_not_turn_speed_into_dynamic_fov():
    camera = CAMERA.read_text(encoding="utf-8")
    assert "gameplayFieldOfView = 58f" in camera
    assert "gameplayCamera.fieldOfView = Mathf.Clamp(gameplayFieldOfView, 45f, 75f);" in camera
    assert "Speed01" in camera
    assert "fieldOfView = Mathf.Lerp" not in camera
    assert "fieldOfView +=" not in camera
    assert "fieldOfView -=" not in camera


def test_near_clip_remains_small_for_close_third_person_geometry():
    camera = CAMERA.read_text(encoding="utf-8")
    assert "gameplayNearClip = 0.06f" in camera
    assert "Mathf.Clamp(gameplayNearClip, 0.02f, 0.10f)" in camera
