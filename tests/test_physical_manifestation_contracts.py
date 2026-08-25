import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
UNITY = ROOT / "unity"
MF = UNITY / "Assets" / "Mindforge"


def read(path: Path) -> str:
    return path.read_text(encoding="utf-8")


def test_unity_is_a_pinned_urp_project_boundary():
    version = read(UNITY / "ProjectSettings" / "ProjectVersion.txt")
    manifest = json.loads(read(UNITY / "Packages" / "manifest.json"))
    assert "2022.3.76f1" in version
    assert manifest["dependencies"]["com.unity.render-pipelines.universal"] == "14.0.11"
    assert manifest["dependencies"]["com.unity.ugui"] == "1.0.0"


def test_competition_scene_is_reproducibly_assembled_and_validated():
    assembler = read(MF / "Editor" / "CompetitionSceneAssembler.cs")
    validator = read(MF / "Editor" / "CompetitionGateValidator.cs")
    for token in (
        "AwakeningCalibrationDirector", "CombatBootstrap", "NeuralEvidenceHud",
        "NeuralLinkContingency", "MindforgeSessionLogger", "FracturedSignalDirector",
        "SoulWispController", "PhotodiodePatch", "DisplayQualificationController",
        "Mindforge_Competition.unity",
    ):
        assert token in assembler or token in validator
    assert "unity-gate1-latest.json" in validator
    assert "No missing MonoBehaviours" in validator


def test_awakening_is_a_hard_combat_gate():
    src = read(MF / "Calibration" / "AwakeningCalibrationDirector.cs")
    assert "guardianInput?.SetCombatActionsEnabled(false)" in src
    assert "evt.IsCalibrationReady" in src
    assert "guardianInput?.SetCombatActionsEnabled(true)" in src
    assert "soulWisp?.SetTarget(combatTarget)" in src
    assert 'RunStage("baseline"' in src
    assert 'RunStage("sight"' in src
    assert 'RunStage("guard"' in src


def test_gate2_instrument_can_measure_both_neural_codes():
    patch = read(MF / "Presentation" / "PhotodiodePatch.cs")
    display = read(MF / "Qualification" / "DisplayQualificationController.cs")
    assert "StimulusSource.Sight" in patch and "StimulusSource.Guard" in patch
    assert "KeyCode.F10" in patch and "KeyCode.F11" in patch
    assert "ActiveFrequencyHz" in patch
    assert "targetRefreshHz = 120" in display
    assert "QualitySettings.vSyncCount = 1" in display
    assert "KeyCode.F12" in display
    assert "software" in display.lower() and "photodiode" in display.lower()


def test_torture_harness_injects_render_stalls_without_new_gameplay():
    src = read(MF / "Qualification" / "DemoFaultHarness.cs")
    assert "InjectMainThreadStall(50)" in src
    assert "InjectMainThreadStall(120)" in src
    assert "Stopwatch" in src


def test_feature_freeze_still_has_exactly_two_neural_targets():
    events = read(MF / "NeuralBridge" / "NeuralEvent.cs")
    assert "Sight = 1" in events and "Guard = 2" in events
    assert "AuraTarget" in events
    assert "Resonance" not in events
