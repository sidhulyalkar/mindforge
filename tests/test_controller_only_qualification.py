from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
UNITY = ROOT / "unity" / "Assets" / "Mindforge"


def read(*parts: str) -> str:
    return UNITY.joinpath(*parts).read_text(encoding="utf-8")


def test_controller_only_qualification_is_explicit_and_excluded_from_release_builds():
    bootstrap = read("Qualification", "ControllerOnlyQualificationBootstrap.cs")
    awakening = read("Calibration", "AwakeningCalibrationDirector.cs")

    assert bootstrap.startswith("#if UNITY_EDITOR || DEVELOPMENT_BUILD")
    assert "using Mindforge.Combat;" in bootstrap
    assert 'CommandLineFlag = "-mindforge-controller-only"' in bootstrap
    assert 'EnvironmentVariable = "MINDFORGE_CONTROLLER_ONLY"' in bootstrap
    assert "KeyCode.F8" in bootstrap
    assert "contingency?.Disarm()" in bootstrap
    assert "auraAuthority.enabled = false" in bootstrap
    assert "receiver.enabled = false" in bootstrap
    assert '"QUALIFICATION_MODE"' in bootstrap
    assert '"CONTROLLER_ONLY_NO_BCI"' in bootstrap
    assert "BCI DISABLED" in bootstrap

    assert "EnterControllerOnlyQualification" in awakening
    assert "ControllerOnlyQualificationActive = true" in awakening
    assert 'SetStatus("P2 CONTROLLER-ONLY QUALIFICATION · BCI DISABLED")' in awakening
    assert "CalibrationReady = false" in awakening
    assert 'CalibrationStageChanged?.Invoke("controller_only")' in awakening
    assert "if (ControllerOnlyQualificationActive || !_serviceReady || _running) return;" in awakening


def test_controller_only_path_does_not_invent_calibration_success():
    awakening = read("Calibration", "AwakeningCalibrationDirector.cs")
    method = awakening[
        awakening.index("public bool EnterControllerOnlyQualification()"):
        awakening.index("private IEnumerator RunProtocol()")
    ]
    assert "CalibrationReady = false" in method
    assert "calibrationReady?.Invoke()" not in method
    assert "ArmForCombat()" not in method
    assert "linkContingency?.Disarm()" in method


def test_controller_only_mode_suppresses_neural_only_presentation_not_just_authority():
    bootstrap = read("Qualification", "ControllerOnlyQualificationBootstrap.cs")
    assert "NeuralEvidenceHud" in bootstrap
    assert "NeuralAuraFeedback" in bootstrap
    assert "NeuralHapticFeedback" in bootstrap
    assert "evidenceHud.enabled = false" in bootstrap
    assert "auraFeedback.enabled = false" in bootstrap
    assert "haptics.enabled = false" in bootstrap
    assert "intentional neural absence as a fault" in bootstrap
