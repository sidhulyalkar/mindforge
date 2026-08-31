from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
INTRO = ROOT / "unity/Assets/Mindforge/Presentation/MindforgeCanonicalIntroV17.cs"
V15 = ROOT / "unity/Assets/Mindforge/Presentation/MindforgeDemoEnvironmentV15.cs"


def read(path: Path) -> str:
    return path.read_text(encoding="utf-8")


def test_canonical_intro_installs_on_v11_and_closes_calibration_gate_before_update():
    text = read(INTRO)
    assert "RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)" in text
    assert "FindObjectOfType<MindforgeDemoV11Marker>(true) == null" in text
    install = text.split("private static void Install()", 1)[1].split("private IEnumerator Start()", 1)[0]
    assert "calibration.ConfigureIntroGate(true)" in install
    assert "calibration.SetIntroReady(false)" in install
    assert "new GameObject(RootName).AddComponent<MindforgeCanonicalIntroV17>()" in install


def test_intro_has_no_ssvep_stimulus_authority_and_releases_gate_only_after_static_frame():
    text = read(INTRO)
    assert "yield return new WaitForEndOfFrame();" in text
    assert "yield return new WaitForSecondsRealtime(0.12f);" in text
    assert "_calibration.SetIntroReady(true);" in text
    assert text.index("WaitForEndOfFrame") < text.index("_calibration.SetIntroReady(true)")
    for forbidden in (
        "VepAuraStimulus",
        "BeginWindow(",
        "EndWindow(",
        "ConfigureTiming(",
        "NeuralEvent",
        "UdpNeuralReceiver",
        "CalibrationReady =",
        "EnterControllerOnlyQualification(",
    ):
        assert forbidden not in text


def test_intro_suspends_conventional_combat_and_boss_during_camera_motion():
    text = read(INTRO)
    assert "_input.SetCombatActionsEnabled(false);" in text
    assert "_boss.SetExternalPause(true);" in text
    assert "_legacyCamera.enabled = false;" in text
    assert "_camera.fieldOfView = 56f;" in text
    assert "_calibration.ControllerOnlyQualificationActive" in text
    assert "_input.SetCombatActionsEnabled(true);" in text
    assert "_boss.SetExternalPause(false);" in text


def test_v15_cinematic_is_not_mistaken_for_the_canonical_v11_intro():
    v15 = read(V15)
    assert 'CompetitionSceneName = "Mindforge_Competition"' in v15
    assert "MindforgeDemoV11Marker" not in v15
    intro = read(INTRO)
    assert "MindforgeDemoV11Marker" in intro
    assert "canonical V0.11 scene" in intro
