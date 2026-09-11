from __future__ import annotations

import importlib.util
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
OVERLAY = ROOT / "dragonsouls_overlay" / "Assets" / "Mindforge"
RUNTIME = OVERLAY / "Runtime"
EDITOR = OVERLAY / "Editor"

EVENT = RUNTIME / "MindforgeNeuralEventV33.cs"
RECEIVER = RUNTIME / "MindforgeUdpNeuralReceiverV33.cs"
MARKERS = RUNTIME / "MindforgeBciMarkerSenderV33.cs"
TIMING = RUNTIME / "MindforgeDisplayTimingMonitorV33.cs"
STIMULUS = RUNTIME / "MindforgeBciStimulusV33.cs"
CALIBRATION = RUNTIME / "MindforgeBciCalibrationDirectorV33.cs"
WINDOW = RUNTIME / "MindforgeNeuralWindowControllerV33.cs"
BRIDGE = RUNTIME / "MindforgeNeuralIntentBridgeV33.cs"
SIGHT = RUNTIME / "MindforgeSightReceptorV33.cs"
GUARD = RUNTIME / "MindforgeGuardReceptorV33.cs"
LOGGER = RUNTIME / "MindforgeBciSessionLoggerV33.cs"
PROVENANCE = RUNTIME / "MindforgeNativeProvenanceV33.cs"
QUALIFICATION = RUNTIME / "MindforgeBciQualificationHarnessV33.cs"
INSTALLER = RUNTIME / "MindforgeBciIntegrationRuntimeV33.cs"
BUILDER = EDITOR / "MindforgeBciIntegrationBuilderV33.cs"
AUDIT = EDITOR / "MindforgeBciReadinessV33.cs"
PY_CONFIG = ROOT / "neuro" / "mindforge_neuro" / "config.py"
PY_RUNNER = ROOT / "tools" / "run_unity_calibrated_decoder.py"
BOOTSTRAP = ROOT / "tools" / "bootstrap_dragonsouls_chassis.sh"
B0_VALIDATOR = ROOT / "tools" / "validate_bci_b0_receipt.py"


def read(path: Path) -> str:
    assert path.exists(), f"missing V0.33 BCI source: {path}"
    return path.read_text(encoding="utf-8")


def test_v33_production_stimulus_matches_existing_two_class_decoder():
    stimulus = read(STIMULUS)
    config = read(PY_CONFIG)

    assert "public const float SightFrequencyHz = 10f;" in stimulus
    assert "public const float GuardFrequencyHz = 12f;" in stimulus
    assert "public const int ProductionTargetCount = 2;" in stimulus
    assert 'blue_frequency_hz: float = 10.0' in config
    assert 'green_frequency_hz: float = 12.0' in config
    assert 'AuraTarget.SIGHT: self.blue_frequency_hz' in config
    assert 'AuraTarget.GUARD: self.green_frequency_hz' in config

    assert "ConcordFrequencyHz" not in stimulus
    assert "ProductionTargetCount = 3" not in stimulus


def test_v33_transport_accepts_python_v1_v2_schema_without_raw_eeg():
    event = read(EVENT)
    receiver = read(RECEIVER)

    for token in (
        'SchemaV1 = "mindforge.neural_event.v1"',
        'SchemaV2 = "mindforge.neural_event.v2"',
        "authority_ttl_ms",
        "stimulus_epoch",
        "evidence_ms",
        "selected_sight_hz",
        "selected_guard_hz",
    ):
        assert token in event

    for token in (
        "ConcurrentQueue<ReceivedPacket>",
        "maxQueuedPackets",
        "maxPacketQueueAgeSeconds",
        "AuthorityExpired",
        "evt.seq <= _lastSeenSeq",
        "IPAddress.Loopback",
        "port = 19742",
    ):
        assert token in receiver

    forbidden = "raw_eeg eeg_samples samples_uv channel_data fft_spectrum".split()
    combined = event.lower() + receiver.lower()
    for token in forbidden:
        assert token not in combined


def test_v33_receiver_handles_decoder_restart_only_via_explicit_service_ready_transfer():
    receiver = read(RECEIVER)
    for token in (
        "_activeModelId",
        "AcceptModelIdentity",
        "evt.IsCalibrationServiceReady",
        "_lastSeenSeq = -1",
        "_lastAuthoritySeq = -1",
        "DroppedForeignModel",
        "_droppedForeignModel",
    ):
        assert token in receiver

    authority_transfer = receiver.split("private bool AcceptModelIdentity", 1)[1].split(
        "private void Update", 1
    )[0]
    assert "if (evt.IsCalibrationServiceReady)" in authority_transfer
    assert "Interlocked.Increment(ref _droppedForeignModel)" in authority_transfer


def test_v33_unity_markers_match_calibration_and_epoch_runner_contract():
    markers = read(MARKERS)
    runner = read(PY_RUNNER)

    assert "processingPort = 19743" in markers
    assert "observationPort = 19745" in markers
    assert 'category = "calibration"' in markers
    assert 'category = "neural_window"' in markers
    assert '"NEURAL_WINDOW_LISTENING"' in read(WINDOW)
    assert '"NEURAL_WINDOW_RESOLVED"' in read(WINDOW)
    assert '"NEURAL_WINDOW_ABSTAINED"' in read(WINDOW)

    for token in (
        'STAGES = ("baseline", "sight", "guard")',
        'marker.category != "calibration"',
        'marker.event == "NEURAL_WINDOW_LISTENING"',
        "terminal_markers = {",
        '"NEURAL_WINDOW_ENDED"',
        '"NEURAL_WINDOW_ABSTAINED"',
        '"NEURAL_WINDOW_RESOLVED"',
    ):
        assert token in runner


def test_v33_calibration_requires_decoder_ack_and_never_invents_success():
    calibration = read(CALIBRATION)
    for token in (
        "CALIBRATION_READY",
        "evt.IsCalibrationReady",
        "MatchesActiveCalibration(evt)",
        "Guid.NewGuid().ToString(\"N\")",
        'SendCalibrationStage(_calibrationId, "baseline", "begin"',
        'RunTarget(MindforgeIntentV29.Sight, "sight"',
        'RunTarget(MindforgeIntentV29.Guard, "guard"',
    ):
        assert token in calibration

    assert "CalibrationReady = true" not in calibration
    assert "PublishControllerSimulation" not in calibration


def test_v33_semantic_bridge_is_confidence_quality_epoch_and_calibration_gated():
    bridge = read(BRIDGE)
    for token in (
        "minimumConfidence",
        "minimumQuality",
        "minimumEvidenceMs",
        "evt.artifact",
        "calibration_not_ready",
        "no_active_window",
        "epoch_mismatch",
        "semantic_refractory",
        "MindforgeIntentBusV29.Publish",
        "_windows.ResolveSelection",
        "SendSemanticSelection",
    ):
        assert token in bridge

    for forbidden in (
        "TakeDamage(",
        "ReceiveDamage(",
        "StartAttack(",
        "MovePosition(",
        "MoveRotation(",
        "Animator.Play(",
    ):
        assert forbidden not in bridge


def test_v33_participant_pause_is_explicit_and_terminates_active_causal_window():
    stimulus = read(STIMULUS)
    window = read(WINDOW)
    assert "SetParticipantPaused(bool paused)" in stimulus
    assert "ToggleParticipantPause()" in stimulus
    assert "ParticipantPauseChanged" in stimulus
    assert "_stimulus.ParticipantPaused" in window
    assert 'Abort("participant_paused")' in window
    assert 'SendNeuralWindow("NEURAL_WINDOW_ENDED"' in window


def test_v33_gameplay_receptors_are_semantic_and_non_authoritative():
    sight = read(SIGHT)
    guard = read(GUARD)
    combined = sight + guard

    assert "MindforgeIntentV29.Sight" in sight
    assert "MindforgeIntentV29.Guard" in guard
    assert "GuardActive" in guard
    assert "LastRevealedTarget" in sight
    assert "Destroy(collider)" in sight
    assert "Destroy(collider)" in guard

    for forbidden in (
        "TakeDamage(",
        "ReceiveDamage(",
        "StartAttack(",
        "StopAttack(",
        "AddForce(",
        "MovePosition(",
        "MoveRotation(",
        "ChangeState(",
    ):
        assert forbidden not in combined


def test_v33_session_logger_is_derived_only_and_records_experimental_context():
    logger = read(LOGGER)
    for token in (
        '"mindforge.bci_session_event.v1"',
        "neural_seq",
        "confidence",
        "quality",
        "sight_score",
        "guard_score",
        "margin",
        "stimulus_epoch",
        "evidence_ms",
        "observed_refresh_hz",
        "long_frame_fraction",
        'Path.Combine(Application.persistentDataPath, "mindforge-bci")',
    ):
        assert token in logger

    assert "raw eeg" in logger.lower()
    for token in ("samples_uv", "channel_data", "eeg_array"):
        assert token not in logger.lower()


def test_v33_bootstrap_seals_exact_overlay_source_and_dirty_state():
    bootstrap = read(BOOTSTRAP)
    provenance = read(PROVENANCE)

    for token in (
        'MINDFORGE_COMMIT="$(git -C "${REPO_ROOT}" rev-parse HEAD)"',
        'status --porcelain --untracked-files=normal',
        '"schema": "mindforge.overlay_provenance.v1"',
        '"mindforge_source_commit": sys.argv[5]',
        '"mindforge_worktree_dirty": sys.argv[6] == "1"',
        '"overlay_applied": sys.argv[7] == "1"',
        '".mindforge_overlay.json"',
    ):
        assert token in bootstrap

    assert 'Schema = "mindforge.overlay_provenance.v1"' in provenance
    assert '".mindforge_overlay.json"' in provenance
    assert "IsCleanSource" in provenance
    assert "WorktreeDirty" in provenance
    assert "OverlayApplied" in provenance


def test_v33_b0_harness_exercises_semantic_path_and_never_claims_eeg():
    harness = read(QUALIFICATION)
    for token in (
        'ReceiptSchema = "mindforge.bci_b0_receipt.v1"',
        "keyboard.f8Key.wasPressedThisFrame",
        '_windows.OpenWindow("b0_sight", requireCalibration: false)',
        "_bridge.InjectControllerSimulation(MindforgeIntentV29.Sight, 1f)",
        '_windows.OpenWindow("b0_guard", requireCalibration: false)',
        "_bridge.InjectControllerSimulation(MindforgeIntentV29.Guard, 1f)",
        'string.Equals(_lastWindowEndReason, "timeout_abstain"',
        "_stimulus.SetParticipantPaused(true)",
        'string.Equals(_lastWindowEndReason, "participant_paused"',
        'mode = "controller_only"',
        'source_mode = "simulated_decision"',
        "eeg_observed = false",
        "physical_display_timing_observed = false",
        "receipt.passed = receipt.functional_pass && receipt.provenance_available && receipt.source_clean",
    ):
        assert token in harness

    for forbidden in ("TakeDamage(", "ReceiveDamage(", "StartAttack(", "MovePosition(", "MoveRotation("):
        assert forbidden not in harness


def test_v33_b0_validator_requires_clean_exact_source_and_scientific_boundaries():
    spec = importlib.util.spec_from_file_location("validate_bci_b0_receipt", B0_VALIDATOR)
    assert spec is not None and spec.loader is not None
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)

    commit = "a" * 40
    payload = {
        "schema": module.SCHEMA,
        "mode": "controller_only",
        "source_mode": "simulated_decision",
        "source_commit": commit,
        "provenance_available": True,
        "source_clean": True,
        "worktree_dirty": False,
        "overlay_applied": True,
        "upstream_commit": "b" * 40,
        "unity_version": "2021.3.20f1",
        "runtime_platform": "OSXEditor",
        "sight_selection_resolved": True,
        "sight_receptor_observed": True,
        "guard_selection_resolved": True,
        "guard_receptor_observed": True,
        "timeout_abstention_observed": True,
        "participant_pause_abort_observed": True,
        "functional_pass": True,
        "passed": True,
        "eeg_observed": False,
        "physical_display_timing_observed": False,
    }
    assert module.validate(payload, expected_commit=commit) == []

    wrong = dict(payload, eeg_observed=True)
    assert "eeg_observed_must_be_false" in module.validate(wrong, expected_commit=commit)
    assert "source_commit_mismatch" in module.validate(payload, expected_commit="c" * 40)


def test_v33_installer_keeps_legacy_combat_authority_and_installs_qualification_spine():
    installer = read(INSTALLER)
    builder = read(BUILDER)

    for token in (
        "Install<MindforgeNativeProvenanceV33>()",
        "Install<MindforgeUdpNeuralReceiverV33>()",
        "Install<MindforgeBciStimulusV33>()",
        "Install<MindforgeBciCalibrationDirectorV33>()",
        "Install<MindforgeNeuralWindowControllerV33>()",
        "Install<MindforgeNeuralIntentBridgeV33>()",
        "Install<MindforgeSightReceptorV33>()",
        "Install<MindforgeGuardReceptorV33>()",
        "Install<MindforgeBciSessionLoggerV33>()",
        "Install<MindforgeBciQualificationHarnessV33>()",
        "MindforgeBciOrbV31",
        'camera.transform.Find("Mindforge_BCI_Orb_V31")',
    ):
        assert token in installer

    assert "SourceScene = MindforgeVerticalSliceBuilderV31.DestinationScene" in builder
    assert 'DestinationScene = "Assets/Mindforge/Scenes/MindforgeBciIntegrationV33.unity"' in builder
    assert "AssetDatabase.CopyAsset(SourceScene, DestinationScene)" in builder
    assert "PlayerStateMachine" in builder and "Sword" in builder


def test_v33_timing_monitor_is_explicitly_software_only():
    timing = read(TIMING)
    assert "software-only" in timing.lower()
    assert "photodiode" in timing.lower()
    assert "TimingHealthy" in timing
    assert "ObservedRefreshHz" in timing
    assert "LongFrameFraction" in timing


def test_v33_native_audit_tracks_b0_separately_from_live_eeg():
    audit = read(AUDIT)
    assert 'MenuItem("Mindforge/World V0.33/Audit BCI Integration"' in audit
    assert '"controller_only_b0_receipt"' in audit
    assert '"controller_only_b0_functional"' in audit
    assert '"controller_only_b0_promotable_receipt"' in audit
    assert '"clean_overlay_provenance"' in audit
    assert '"human_or_synthetic_calibration"' in audit
    assert "physical_timing_observed=false" in audit
