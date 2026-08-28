from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
PRESENTATION = ROOT / "unity" / "Assets" / "Mindforge" / "Presentation"


def read(name: str) -> str:
    return (PRESENTATION / name).read_text(encoding="utf-8")


def test_enemy_focus_is_explicit_player_controlled_camera_composition_only():
    camera = read("ShowcaseCameraRig.cs")

    assert "targetFocusToggleKey = KeyCode.T" in camera
    assert "public bool TargetFocusActive" in camera
    assert "public Transform FocusTarget" in camera
    assert "public void SetTargetFocus(bool active)" in camera
    assert "Input.GetKeyDown(targetFocusToggleKey)" in camera
    assert "targetFocusGuardianWeight" in camera
    assert "targetFocusMotionLeadSeconds" in camera
    assert "targetFocusFieldOfView" in camera
    assert "TargetFocusChanged" in camera
    assert "Camera composition only; player aim and combat authority unchanged" in camera

    for forbidden in (
        "GuardianCombatInput",
        "GuardianCombatController",
        "GuardianSwordShieldController",
        "FirePulse(",
        "TryLightAttack(",
        "SetGuardHeld(",
        "RequestDash(",
        "NeuralEvent",
        "UdpNeuralReceiver",
        "DualAuraCombatDirector",
    ):
        assert forbidden not in camera


def test_arena_visibility_lifts_blacks_without_becoming_gameplay_authority():
    visibility = read("ArenaVisibilityDirector.cs")
    installer = read("ShowcaseRuntimeInstaller.cs")
    post = read("ShowcasePostProcessing.cs")

    assert "RenderSettings.ambientMode = AmbientMode.Trilight" in visibility
    assert "RenderSettings.fogDensity" in visibility
    assert "fogDensity = 0.0042f" in visibility
    assert "ArenaCombatReadabilityLight" in visibility
    assert "readabilityIntensity = 1.65f" in visibility
    assert "key.intensity = Mathf.Max(key.intensity, 1.42f)" in visibility
    assert "fill.intensity = Mathf.Max(fill.intensity, 2.85f)" in visibility
    assert "rim.intensity = Mathf.Max(rim.intensity, 3.05f)" in visibility
    assert "gameObject.AddComponent<ArenaVisibilityDirector>()" in installer

    assert "_color.postExposure.Override(0.38f)" in post
    assert "_color.contrast.Override(7f)" in post
    assert "_vignette.intensity.Override(0.060f)" in post

    for forbidden in (
        "ReceiveDamage(",
        "FirePulse(",
        "TryLightAttack(",
        "RequestDash(",
        "TryActivate(",
        "NeuralEvent",
        "UdpNeuralReceiver",
        "DualAuraCombatDirector",
    ):
        assert forbidden not in visibility


def test_focus_mode_is_discoverable_and_visually_marks_target_without_aiming_for_player():
    guide = read("PlayerAgencyGuide.cs")
    menu = read("GuardianEquipmentMenu.cs")

    assert "T ENEMY FOCUS" in guide
    assert "TARGET FOCUS" in guide
    assert "cameraRig.TargetFocusActive" in guide
    assert "cameraRig.FocusTarget" in guide
    assert '"T", "Enemy focus camera"' in menu
    assert "T focus is camera-only" in menu

    for forbidden in (
        ".TryLightAttack(",
        ".SetGuardHeld(",
        ".RequestDash(",
        ".FirePulse(",
        ".BeginCounter(",
    ):
        assert forbidden not in guide
