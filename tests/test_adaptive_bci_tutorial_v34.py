from pathlib import Path
import runpy


ROOT = Path(__file__).resolve().parents[1]
RUNTIME = ROOT / "dragonsouls_overlay" / "Assets" / "Mindforge" / "Runtime"
EDITOR = ROOT / "dragonsouls_overlay" / "Assets" / "Mindforge" / "Editor"


def read(path: Path) -> str:
    assert path.exists(), f"missing V0.34 source: {path}"
    return path.read_text(encoding="utf-8")


def test_v34_runtime_stacks_on_v33_and_installs_only_measurement_tutorial_layers():
    text = read(RUNTIME / "MindforgeAdaptiveBciRuntimeV34.cs")
    for token in (
        "MindforgeBciIntegrationRuntimeV33",
        "Install<MindforgeUdpGazeReceiverV34>()",
        "Install<MindforgeGazeProfilerV34>()",
        "Install<MindforgeAdaptiveStimulusLayoutV34>()",
        "Install<MindforgeAdaptiveBciTutorialV34>()",
        "Install<MindforgeAdaptiveBciInvariantV34>()",
        "calibration.SetRequireFrozenStimulusLayout(true)",
    ):
        assert token in text
    for forbidden in ("CharacterController.Move", "health.CurrentHealth", "BossManager", "EnemyNightmareDragonController"):
        assert forbidden not in text


def test_gaze_boundary_is_derived_loopback_bounded_and_not_biometric_transport():
    event = read(RUNTIME / "MindforgeGazeEventV34.cs")
    receiver = read(RUNTIME / "MindforgeUdpGazeReceiverV34.cs")

    assert 'schema = "mindforge.gaze_event.v1"' in event
    assert "IPAddress.Loopback" in receiver
    assert "maxQueuedPackets = 96" in receiver
    assert "maxPacketQueueAgeSeconds" in receiver
    assert "Stopwatch.GetTimestamp()" in receiver
    assert "DroppedForBackpressure" in receiver
    assert "DroppedOutOfOrder" in receiver
    for forbidden in ("eye_image", "scene_video", "pupil_diameter", "raw_eeg", "channel_samples"):
        assert forbidden not in event.lower()
        assert forbidden not in receiver.lower()


def test_profiler_measures_interface_geometry_and_has_stability_not_fixation_authority():
    text = read(RUNTIME / "MindforgeGazeProfilerV34.cs")
    for token in (
        'schema = "mindforge.gaze_profile.v1"',
        "dispersion_p50",
        "dispersion_p90",
        "acquisition_ms_p50",
        "acquisition_ms_p90",
        "occupancy_p50",
        "recommended_aoi_radius",
        "recommended_acquisition_lead_ms",
        "IsRecentWindowStable()",
    ):
        assert token in text
    assert ".fixation" not in text
    for forbidden in ("intelligence", "personality", "diagnosis", "cognitive_score"):
        assert forbidden not in text.lower()


def test_v33_stimulus_can_be_configured_once_then_frozen_before_neural_use():
    text = read(RUNTIME / "MindforgeBciStimulusV33.cs")
    for token in (
        "public bool LayoutFrozen { get; private set; }",
        "public string LayoutId { get; private set; }",
        "TryConfigureLayout",
        "if (!Installed || LayoutFrozen || ModulationActive || Mode == PresentationMode.Listening)",
        "Vector2.Distance",
        "LayoutFrozen = freeze",
    ):
        assert token in text
    assert "SightFrequencyHz = 10f" in text
    assert "GuardFrequencyHz = 12f" in text


def test_v34_fail_closes_calibration_and_presentation_until_layout_is_frozen():
    calibration = read(RUNTIME / "MindforgeBciCalibrationDirectorV33.cs")
    invariant = read(RUNTIME / "MindforgeAdaptiveBciInvariantV34.cs")

    assert "SetRequireFrozenStimulusLayout" in calibration
    assert "_requireFrozenStimulusLayout && !_stimulus.LayoutFrozen" in calibration
    assert 'CalibrationRejected?.Invoke("stimulus_layout_not_frozen")' in calibration
    assert "if (_requireFrozenStimulusLayout && !_stimulus.LayoutFrozen) return false;" in calibration
    assert "preFreeze && _stimulus != null" in invariant
    assert "_stimulus.SetVisible(false)" in invariant
    assert "preFreeze && _calibration != null && _calibration.InProgress" in invariant
    assert "_calibration.ResetCalibration()" in invariant


def test_layout_personalization_is_conservative_symmetric_and_does_not_touch_decoder_or_gameplay():
    text = read(RUNTIME / "MindforgeAdaptiveStimulusLayoutV34.cs")
    assert "profile.dispersion_p90" in text
    assert "TargetSeparation" in text
    assert "new Vector3(-half, 0f, -0.025f)" in text
    assert "new Vector3(half, 0f, -0.025f)" in text
    assert "TryConfigureLayout(sight, guard, LayoutId, freeze: true)" in text
    for forbidden in (
        "SsvepDecoder",
        "MindforgeIntentBusV29.Publish",
        "CharacterController.Move",
        "health.CurrentHealth",
        "Damage",
    ):
        assert forbidden not in text


def test_game_markers_bind_calibration_and_windows_to_layout_identity():
    text = read(RUNTIME / "MindforgeBciMarkerSenderV33.cs")
    assert "public string CurrentLayoutId" in text
    calibration = text.split("public void SendCalibrationStage", 1)[1].split("public void SendTutorialStage", 1)[0]
    tutorial = text.split("public void SendTutorialStage", 1)[1].split("public void SendNeuralWindow", 1)[0]
    window = text.split("public void SendNeuralWindow", 1)[1].split("public void SendSemanticSelection", 1)[0]
    assert "trial_id = CurrentLayoutId" in calibration
    assert "trial_id = CurrentLayoutId" in tutorial
    assert "trial_id = CurrentLayoutId" in window


def test_tutorial_order_profiles_gaze_freezes_layout_then_uses_v33_calibration_and_semantics():
    text = read(RUNTIME / "MindforgeAdaptiveBciTutorialV34.cs")
    expected_order = (
        "Controls = 1",
        "GazeHealth = 2",
        "GazeGrid = 3",
        "FreeExplore = 4",
        "GazeSwitch = 5",
        "LayoutFreeze = 6",
        "WaitingForNeuralService = 7",
        "Calibration = 8",
        "SightPractice = 9",
        "GuardPractice = 10",
        "MovementStress = 11",
        "Complete = 12",
    )
    positions = [text.index(token) for token in expected_order]
    assert positions == sorted(positions)
    assert "keyboard.f9Key.wasPressedThisFrame" in text
    assert "_layout.FreezeFromProfile()" in text
    assert "_calibration.BeginCalibration()" in text
    assert '_windows.OpenWindow("tutorial_"' in text
    assert "_bridge.SelectionAccepted += HandleSelectionAccepted" in text
    assert 'schema = "mindforge.tutorial_receipt.v1"' in text
    assert "raw_eeg_in_unity = false" in text
    assert "physical_display_timing_observed = false" in text


def test_v34_builder_derives_from_v33_scene_and_preserves_inherited_authorities():
    text = read(EDITOR / "MindforgeAdaptiveBciBuilderV34.cs")
    assert "MindforgeBciIntegrationBuilderV33.DestinationScene" in text
    assert "MindforgeBciIntegrationBuilderV33.Build(refresh: refresh)" in text
    assert "root.AddComponent<MindforgeAdaptiveBciRuntimeV34>()" in text
    assert "FindObjectsOfType<PlayerStateMachine>(true).Length != 1" in text
    assert "FindObjectsOfType<Sword>(true).Length != 1" in text
    assert "FindObjectsOfType<EnemyNightmareDragonController>(true).Length == 0" in text


def test_v34_readiness_separates_failures_from_unobserved_evidence():
    text = read(EDITOR / "MindforgeAdaptiveBciReadinessV34.cs")
    assert 'failures.Add("v33_authority_missing")' in text
    assert 'failures.Add("layout_identity_mismatch")' in text
    assert 'deferred.Add("gaze_stream_unobserved")' in text
    assert 'deferred.Add("neural_calibration_unobserved")' in text
    assert "raw_eeg_in_unity=false physical_timing_observed=false" in text


def test_independent_tutorial_receipt_validator_accepts_complete_and_rejects_claim_leaks():
    namespace = runpy.run_path(str(ROOT / "tools" / "validate_tutorial_receipt_v34.py"))
    validate = namespace["validate"]

    payload = {
        "schema": "mindforge.tutorial_receipt.v1",
        "source_commit": "abc123",
        "clean_source": True,
        "tutorial_status": "complete",
        "raw_eeg_in_unity": False,
        "physical_display_timing_observed": False,
        "stimulus_layout_id": "v34-s210-a060-l350",
        "stimulus_layout_frozen": True,
        "calibration_id": "cal-1",
        "calibration_state": "Calibrated",
        "sight_practice_pass": True,
        "guard_practice_pass": True,
        "gaze_profile": {
            "schema": "mindforge.gaze_profile.v1",
            "usable_fraction": 0.9,
            "recommended_aoi_radius": 0.06,
            "recommended_acquisition_lead_ms": 350,
        },
    }
    assert validate(payload, expected_commit="abc123", require_clean=True) == []

    leaked = dict(payload)
    leaked["raw_eeg_in_unity"] = True
    assert "raw_eeg_boundary" in validate(leaked)

    unfrozen = dict(payload)
    unfrozen["stimulus_layout_frozen"] = False
    assert "stimulus_layout_not_frozen" in validate(unfrozen)
