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
    intro = read(INTRO)
    assert "requireIntroReady = false" in cal
    assert "public void ConfigureIntroGate(bool required)" in cal
    assert "public bool IntroReady" in cal
    assert "public void SetIntroReady(bool ready)" in cal
    assert "_serviceReady && IntroReady" in cal
    assert "!_serviceReady || _running || !IntroReady" in cal
    assert "Presentation-only readiness handshake" in cal
    assert "_calibration?.ConfigureIntroGate(true);" in intro
    assert "_calibration?.SetIntroReady(false);" in intro
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


def test_research_hud_is_hidden_without_disabling_evidence_telemetry():
    intro = read(INTRO)
    assert "ResolveResearchHudPresentation" in intro
    assert "FindObjectOfType<NeuralEvidenceHud>(true)" in intro
    assert "evidence.GetComponent<CanvasGroup>()" in intro
    assert "evidence.gameObject.AddComponent<CanvasGroup>()" in intro
    assert "SetResearchHud(false)" in intro
    resolver = intro[intro.index("private void ResolveResearchHudPresentation"):intro.index("private void SetResearchHud")]
    assert "evidence.enabled = false" not in resolver


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


def test_legacy_ambient_light_breathing_is_disabled_for_eeg_demo():
    env = read(ENV)
    quiet = read(QUIET)
    assert "decorativePulseHz" in env and "decorativePulseDepth" in env
    assert "FindObjectOfType<NeuralQuietAmbientMotionV15>" in quiet
    assert "ambient.enabled = false" in quiet
    assert "DecorativeRotationDegreesPerSecond" in quiet
    assert "rotator.Rotate" in quiet


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


def test_baseline_quiets_visual_field_before_label_marker():
    cal = read(CAL)
    quiet = read(QUIET)
    baseline = cal[cal.index("private IEnumerator RunBaseline"):cal.index("private IEnumerator RunCounterbalancedTarget")]
    event_at = baseline.index('CalibrationStageChanged?.Invoke("baseline")')
    marker_at = baseline.index('markerSender?.Send(_sessionId, "baseline", "begin"')
    assert event_at < marker_at
    assert "CalibrationStageChanged += OnCalibrationStageChanged" in quiet
    assert 'stage == "baseline"' in quiet
    assert "Apply(true)" in quiet


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
