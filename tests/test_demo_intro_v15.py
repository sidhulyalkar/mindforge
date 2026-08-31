from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CAL = ROOT / "unity/Assets/Mindforge/Calibration/AwakeningCalibrationDirector.cs"
INTRO = ROOT / "unity/Assets/Mindforge/Presentation/MindforgeDemoIntroDirector.cs"
ENV = ROOT / "unity/Assets/Mindforge/Presentation/MindforgeDemoEnvironmentV15.cs"
QUIET = ROOT / "unity/Assets/Mindforge/Presentation/NeuralQuietPresentationGateV15.cs"


def read(path: Path) -> str:
    return path.read_text(encoding="utf-8")


def test_intro_is_a_real_gate_not_neural_success():
    cal = read(CAL)
    assert "requireIntroReady" in cal
    assert "public bool IntroReady" in cal
    assert "public void SetIntroReady(bool ready)" in cal
    assert "_serviceReady && IntroReady" in cal
    assert "!_serviceReady || _running || !IntroReady" in cal
    assert "Presentation-only readiness handshake" in cal
    assert "CalibrationReady = true" not in cal.split("public void SetIntroReady(bool ready)", 1)[1].split("private void OnNeuralEvent", 1)[0]


def test_camera_parks_before_calibration_gate_opens():
    intro = read(INTRO)
    park = intro.index("SnapRig(_calibrationPose);")
    rendered = intro.index("yield return new WaitForEndOfFrame();", park)
    release = intro.index("_calibration.SetIntroReady(true);", rendered)
    assert park < rendered < release
    assert "Camera motion, title animation and decorative presentation are allowed only before" in intro


def test_player_instructions_match_actual_neural_contract():
    intro = read(INTRO)
    assert "HOLD V TO OPEN A NEURAL WINDOW" in intro
    assert "LOOK AT BLUE: SIGHT" in intro
    assert "LOOK AT GREEN: GUARD" in intro
    assert "KEEP YOUR GAZE ON YOUR CHOICE" in intro
    assert "UNCLEAR SIGNALS DO NOTHING" in intro
    assert "V NEURAL WINDOW" in intro
    assert "KeyCode.F7" in intro and "researchHudKey" in intro
    assert "KeyCode.F8" not in intro


def test_demo_overlays_never_steal_gameplay_input():
    intro = read(INTRO)
    fade = intro[intro.index("private IEnumerator FadeGroup"):intro.index("private IEnumerator WaitOrSkip")]
    assert "group.interactable = false" in fade
    assert "group.blocksRaycasts = false" in fade
    assert "group.interactable = target >" not in fade


def test_arena_reveal_cannot_deal_free_damage():
    intro = read(INTRO)
    assert "_input?.SetCombatActionsEnabled(false);" in intro
    assert "_boss?.SetExternalPause(true);" in intro
    assert "_boss?.SetExternalPause(false);" in intro
    assert "_input?.SetCombatActionsEnabled(true);" in intro
    pause = intro.index("private IEnumerator RunArenaReveal")
    unpause = intro.index("_boss?.SetExternalPause(false);", pause)
    enable = intro.index("_input?.SetCombatActionsEnabled(true);", unpause)
    assert pause < unpause < enable


def test_environment_is_presentation_only_and_competition_scoped():
    env = read(ENV)
    assert 'CompetitionSceneName = "Mindforge_Competition"' in env
    assert "RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)" in env
    assert "presentation-only" in env.lower()
    assert "GetComponent<VepAuraStimulus>" not in env
    assert ".BeginWindow(" not in env
    assert ".EndWindow(" not in env
    assert "frequencyHz" not in env
    assert "collider.enabled = false" in env
    assert "Destroy(collider)" in env
    assert "Renderer placeholder" in env


def test_decorative_motion_freezes_for_all_neural_evidence_intervals():
    env = read(ENV)
    cal = read(CAL)
    assert "public bool CalibrationInProgress => _running;" in cal
    assert "_calibration.CalibrationInProgress" in env
    assert "_wisp.CalibrationStimuliActive" in env
    assert "_wisp.ResonanceWindowActive" in env
    quiet = env.index("if (neuralQuiet)")
    rotate = env.index("rotator.Rotate", quiet)
    assert quiet < rotate
    assert "_lights[i].intensity = _baseLightIntensities[i]" in env


def test_decorative_emission_is_hidden_while_eeg_has_authority():
    quiet = read(QUIET)
    assert "_calibration.CalibrationInProgress" in quiet
    assert "_wisp.CalibrationStimuliActive" in quiet
    assert "_wisp.ResonanceWindowActive" in quiet
    assert '"SightVepCore"' in quiet and '"GuardVepCore"' in quiet
    assert "continue;" in quiet.split('"GuardVepCore"', 1)[1]
    assert "renderer.enabled = quiet ? false : _baselineEnabled[i];" in quiet
    for token in (
        "WispHalo",
        "SanctumSignalRing",
        "SignalPylonCore",
        "ArenaRune",
        "FracturedSignalHalo",
    ):
        assert token in quiet


def test_opaque_boss_placeholder_cage_is_permanently_suppressed():
    quiet = read(QUIET)
    assert 'string.Equals(objectName, "FracturedSignalCage"' in quiet
    cage = quiet.split('string.Equals(objectName, "FracturedSignalCage"', 1)[1].split("continue;", 1)[0]
    assert "renderer.enabled = false" in cage


def test_demo_scene_has_distinct_visual_silhouettes():
    env = read(ENV)
    for token in (
        "AwakeningVisualV15",
        "SanctumPortalRing",
        "FractureSpire_",
        "GuardianVisualV15",
        "GuardianEnergyBlade",
        "FracturedSignalVisualV15",
        "WispPresentationV15",
    ):
        assert token in env
