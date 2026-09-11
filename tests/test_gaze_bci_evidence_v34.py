from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
RUNTIME = ROOT / "dragonsouls_overlay" / "Assets" / "Mindforge" / "Runtime"
EDITOR = ROOT / "dragonsouls_overlay" / "Assets" / "Mindforge" / "Editor"


def read(path: Path) -> str:
    return path.read_text(encoding="utf-8")


def test_v34_runtime_installs_passive_gaze_bci_correlator():
    runtime = read(RUNTIME / "MindforgeAdaptiveBciRuntimeV34.cs")
    evidence = read(RUNTIME / "MindforgeGazeBciEvidenceV34.cs")
    assert "Install<MindforgeGazeBciEvidenceV34>()" in runtime
    assert 'schema = "mindforge.gaze_bci_evidence.v1"' in evidence


def test_correlator_observes_but_does_not_gate_neural_or_gameplay_authority():
    text = read(RUNTIME / "MindforgeGazeBciEvidenceV34.cs")
    for token in (
        "_gaze.SampleReceived += HandleGaze",
        "_calibration.StateChanged += HandleCalibrationState",
        "_windows.WindowOpened += HandleWindowOpened",
        "_windows.WindowResolved += HandleWindowResolved",
        "_windows.WindowEnded += HandleWindowEnded",
        "_bridge.SelectionAccepted += HandleSelectionAccepted",
        "selected_target_occupancy",
        "selected_target_distance_p50",
        "selected_target_distance_p90",
        '"v34-gaze-bci-" + session + ".jsonl"',
    ):
        assert token in text

    for forbidden in (
        "MindforgeIntentBusV29.Publish",
        "InjectControllerSimulation",
        "OpenWindow(",
        "BeginCalibration(",
        "CharacterController.Move",
        "CurrentHealth =",
        "Damage(",
    ):
        assert forbidden not in text


def test_correlator_persists_aggregates_not_gaze_coordinates():
    text = read(RUNTIME / "MindforgeGazeBciEvidenceV34.cs")
    record = text.split("private sealed class EvidenceRecord", 1)[1].split("private sealed class ActiveEvidence", 1)[0]
    assert "usable_samples" in record
    assert "sight_occupancy" in record
    assert "guard_occupancy" in record
    assert "off_target_fraction" in record
    assert "selected_target_distance_p90" in record
    assert " public float x;" not in record
    assert " public float y;" not in record


def test_v34_audit_requires_correlator_contract_but_treats_missing_observation_as_deferred():
    text = read(EDITOR / "MindforgeAdaptiveBciReadinessV34.cs")
    assert 'failures.Add("gaze_bci_evidence_runtime")' in text
    assert 'failures.Add("gaze_bci_target_geometry")' in text
    assert 'deferred.Add("gaze_bci_correlation_unobserved")' in text
    assert "gaze_bci_records=" in text
