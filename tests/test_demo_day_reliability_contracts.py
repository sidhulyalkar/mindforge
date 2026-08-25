from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
UNITY = ROOT / "unity" / "Assets" / "Mindforge"


def text(path: Path) -> str:
    return path.read_text(encoding="utf-8")


def test_calibration_is_a_real_unity_python_handshake():
    unity = text(UNITY / "Calibration" / "AwakeningCalibrationDirector.cs")
    sender = text(UNITY / "Calibration" / "CalibrationMarkerSender.cs")
    python = text(ROOT / "tools" / "run_unity_calibrated_decoder.py")
    assert "evt.IsCalibrationServiceReady" in unity
    assert "evt.IsCalibrationReady" in unity and "evt.IsCalibrationFailed" in unity
    assert 'RunStage("baseline"' in unity and 'RunStage("sight"' in unity and 'RunStage("guard"' in unity
    assert "mindforge.calibration_marker.v1" in sender
    assert "calibrate_decoder" in python
    assert "training_accuracy < 0.70" in python
    assert "resting_alpha_diagnostics" in python


def test_stale_link_is_fairly_paused_not_free_damage():
    receiver = text(UNITY / "NeuralBridge" / "UdpNeuralReceiver.cs")
    gate = text(UNITY / "NeuralBridge" / "NeuralLinkContingency.cs")
    input_src = text(UNITY / "Combat" / "GuardianCombatInput.cs")
    boss = text(UNITY / "Combat" / "FracturedSignalDirector.cs")
    echo = text(UNITY / "Combat" / "FracturedEchoNode.cs")
    assert "staleAfterSeconds = 1.5f" in receiver
    assert "SetExternalPause(true)" in gate
    assert "SetCombatActionsEnabled(false)" in gate
    assert "if (!CombatActionsEnabled) return;" in input_src
    assert "public void SetExternalPause(bool paused)" in boss
    assert "echo?.SetExternalPause(paused)" in boss
    assert "_externalPaused" in echo
    assert "stableRecoverySeconds = 0.75f" in gate


def test_photodiode_patch_is_phase_locked_and_qualification_only():
    patch = text(UNITY / "Presentation" / "PhotodiodePatch.cs")
    stimulus = text(UNITY / "SoulWisp" / "VepAuraStimulus.cs")
    assert "sightStimulus.IsHighPhase" in patch
    assert "toggleKey = KeyCode.F10" in patch
    assert "additional 10 Hz stimulus" in patch
    assert "public bool IsHighPhase" in stimulus
    assert "Time.realtimeSinceStartupAsDouble" in stimulus


def test_telemetry_is_derived_only_checkpointed_and_finalized():
    logger = text(UNITY / "Telemetry" / "MindforgeSessionLogger.cs")
    plotter = text(ROOT / "tools" / "plot_session_report.py")
    assert "mindforge.session.v1" in logger
    assert "partial.json" in logger
    assert "File.Move(temp, path)" in logger
    assert "EvidenceReceived" in logger and "EventReceived" in logger
    assert "raw EEG" in logger
    assert "does not claim to measure cognitive fatigue" in plotter
    assert "suspected-artifact" in plotter


def test_calibration_status_preserves_runtime_sequence_order_and_liveness():
    events = text(ROOT / "neuro" / "mindforge_neuro" / "events.py")
    runtime = text(ROOT / "neuro" / "mindforge_neuro" / "runtime.py")
    script = text(ROOT / "tools" / "run_unity_calibrated_decoder.py")
    assert "CALIBRATION_HEARTBEAT" in events
    assert "EventType.CALIBRATION_HEARTBEAT" in script
    assert "heartbeat_at = now + 0.5" in script
    assert "initial_seq" in runtime
    assert "initial_seq=seq" in script
