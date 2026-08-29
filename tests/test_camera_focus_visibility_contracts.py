from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
UNITY = ROOT / "unity" / "Assets" / "Mindforge"


def read(*parts: str) -> str:
    return UNITY.joinpath(*parts).read_text(encoding="utf-8")


def test_target_lock_is_conventional_player_state_and_neural_agnostic():
    lock = read("Combat", "GuardianTargetLock.cs")

    assert "toggleKey = KeyCode.T" in lock
    assert "public bool Locked" in lock
    assert "public Transform Target" in lock
    assert "public void SetLocked(bool locked)" in lock
    assert "Input.GetKeyDown(toggleKey)" in lock
    assert "lockRange = 28f" in lock
    assert "breakRange = 34f" in lock
    assert "LockChanged" in lock
    assert "conventional player input" in lock

    for forbidden in (
        "NeuralEvent",
        "UdpNeuralReceiver",
        "DualAuraCombatDirector",
        "FirePulse(",
        "TryLightAttack(",
        "RequestDash(",
        "SetGuardHeld(",
        "TryApply(",
    ):
        assert forbidden not in lock


def test_camera_is_third_person_orbit_and_consumes_lock_without_owning_it():
    camera = read("Presentation", "ShowcaseCameraRig.cs")

    for token in (
        "Third-person ARPG camera",
        "pivotHeight = 1.28f",
        "freeDistance = 4.45f",
        "lockDistance = 5.20f",
        "shoulderOffset = 0.70f",
        "verticalFollowSmoothSeconds = 0.105f",
        "_smoothedPivotY = Mathf.SmoothDamp",
        'Input.GetAxis("Mouse X")',
        'Input.GetAxis("Mouse Y")',
        "arrowYawSpeed = 105f",
        "arrowPitchSpeed = 72f",
        "GuardianTargetLock targetLock",
        "TargetFocusActive => targetLock != null && targetLock.Locked",
        "Physics.SphereCastNonAlloc",
        "IsGuardianHierarchy",
        "candidate.IsChildOf(guardian)",
        "IsDynamicActor(collider)",
        "GetComponentInParent<CombatantVitals>()",
        "CursorLockMode.Locked",
        "lockYawSharpness",
        "lockLookWeight",
    ):
        assert token in camera

    assert "Input.GetKeyDown(targetFocusToggleKey)" not in camera
    assert "Input.GetKeyDown(KeyCode.T)" not in camera

    for forbidden in (
        "GuardianCombatController",
        "GuardianSwordShieldController",
        "FirePulse(",
        "TryLightAttack(",
        "SetGuardHeld(",
        "RequestDash(",
        "RequestJump(",
        "NeuralEvent",
        "UdpNeuralReceiver",
        "DualAuraCombatDirector",
    ):
        assert forbidden not in camera


def test_arena_visibility_lifts_blacks_without_becoming_gameplay_authority():
    visibility = read("Presentation", "ArenaVisibilityDirector.cs")
    installer = read("Presentation", "ShowcaseRuntimeInstaller.cs")
    post = read("Presentation", "ShowcasePostProcessing.cs")

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


def test_lock_mode_is_discoverable_and_creates_stable_bci_gaze_anchors():
    guide = read("Presentation", "PlayerAgencyGuide.cs")
    menu = read("Presentation", "GuardianEquipmentMenu.cs")
    wisp = read("SoulWisp", "SoulWispController.cs")

    assert "T  LOCK ON" in guide
    assert "TARGET LOCK" in guide
    assert "cameraRig.TargetFocusActive" in guide
    assert "cameraRig.FocusTarget" in guide
    assert '"T", "Lock / unlock enemy"' in menu
    assert "EEG never moves, jumps, hovers, dashes, locks, swings or blocks" in menu

    assert "StableLockAnchorsActive" in wisp
    assert "lockedHorizontalSeparation = 1.18f" in wisp
    assert "PlaceStableLockedTargets" in wisp
    assert "anchor - right * lockedHorizontalSeparation" in wisp
    assert "anchor + right * lockedHorizontalSeparation" in wisp
    assert "VepAuraStimulus" in wisp
    assert "this changes position only" in wisp

    for forbidden in (
        ".TryLightAttack(",
        ".SetGuardHeld(",
        ".RequestDash(",
        ".RequestJump(",
        ".FirePulse(",
        ".BeginCounter(",
    ):
        assert forbidden not in guide
