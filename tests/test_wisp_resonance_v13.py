from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def read(path: str) -> str:
    return (ROOT / path).read_text(encoding="utf-8")


def test_channel_wisp_is_neutral_conventional_when_control():
    src = read("unity/Assets/Mindforge/Combat/GuardianControlProfileV1.cs")
    assert "ChannelWisp = 10" in src
    assert "KeyCode.V" in src
    assert "GuardianControlAction.ChannelWisp" in src
    assert "Input.GetKeyDown(channelWisp)" in src
    assert "Input.GetKey(channelWisp)" in src
    assert "Sight" not in src.split("private KeyCode channelWisp", 1)[0][-120:]


def test_vep_modulation_is_windowed_not_always_on():
    src = read("unity/Assets/Mindforge/SoulWisp/VepAuraStimulus.cs")
    assert "private bool _codedActive" in src
    assert "public void BeginWindow" in src
    assert "public void EndWindow() => _codedActive = false" in src
    assert "if (!_codedActive || now < _restUntil) return restLuminance" in src
    assert "public bool CodedActive" in src


def test_wisp_separates_fantasy_drift_from_retinal_geometry():
    src = read("unity/Assets/Mindforge/SoulWisp/SoulWispController.cs")
    assert "Resonance coded-core retinal geometry" in src
    assert "codedCoreAngularDiameterDeg" in src
    assert "codedCoreSeparationDeg" in src
    assert "Mathf.Tan(0.5f * codedCoreSeparationDeg * Mathf.Deg2Rad)" in src
    assert "Mathf.Tan(0.5f * codedCoreAngularDiameterDeg * Mathf.Deg2Rad)" in src
    assert "cam.transform.position" in src
    assert "cam.transform.forward * distance" in src
    assert "bool showCodedCores = _calibrationStimuliActive || (combat && _resonanceWindowActive)" in src
    assert "BeginCalibrationStimuli(bool swapSides)" in src
    assert "EndCalibrationStimuli" in src
    assert "BeginCodedResonance" in src
    assert "EndResonanceWindow" in src


def test_resonance_state_machine_abstains_instead_of_guessing():
    src = read("unity/Assets/Mindforge/SoulWisp/WispResonanceWindow.cs")
    for state in ("Idle", "Priming", "Listening", "Resolved", "Abstained", "Cooldown"):
        assert state in src
    assert "requireHoldThroughDecision" in src
    assert 'Abstain("PLAYER_RELEASED")' in src
    assert 'Abstain("TARGET_LOST")' in src
    assert 'Abstain("TIMEOUT")' in src
    assert "evt.seq <= _minimumSelectionSeq" in src
    assert "evt.stimulus_epoch != _windowId" in src
    assert "evt.evidence_ms < Mathf.Max(0, minimumEvidenceMs)" in src
    assert "SelectionAuthorityOpen" in src
    assert "Time.timeScale" not in src
    assert "Attack" not in src
    assert "ReceiveDamage" not in src
    assert "GuardianMotor" not in src


def test_neural_director_requires_open_window_before_buff():
    src = read("unity/Assets/Mindforge/Combat/DualAuraCombatDirector.cs")
    assert "WispResonanceWindow resonanceWindow" in src
    assert "if (resonanceWindow == null || !resonanceWindow.CanAcceptSelection(evt)) return" in src
    gate = src.index("CanAcceptSelection(evt)")
    apply = src.index("buffs.TryApply(evt)")
    resolve = src.index("resonanceWindow.MarkResolved(evt.Target)")
    assert gate < apply < resolve
    assert "resonanceWindow?.ObserveAbstain(evt)" in src
    assert 'AbortForLinkLoss("PARTICIPANT_STOP")' in src
    assert 'AbortForLinkLoss("BCI_LOST")' in src


def test_editor_simulation_is_explicitly_editor_only():
    src = read("unity/Assets/Mindforge/SoulWisp/WispResonanceWindow.cs")
    assert "#if UNITY_EDITOR" in src
    assert "EDITOR_GAMEPLAY_SIM" in src
    assert "unity_editor_resonance_sim" in src
    assert "KeyCode.Alpha1" in src
    assert "KeyCode.Alpha2" in src
    assert "KeyCode.Alpha0" in src


def test_resonance_emits_derived_game_markers_only():
    src = read("unity/Assets/Mindforge/SoulWisp/WispResonanceWindow.cs")
    for marker in (
        "NEURAL_WINDOW_ARMED",
        "NEURAL_WINDOW_LISTENING",
        "NEURAL_WINDOW_RESOLVED",
        "NEURAL_WINDOW_ABSTAINED",
        "NEURAL_WINDOW_ENDED",
    ):
        assert marker in src
    forbidden = ("raw_eeg", "eye_image", "pupil_diameter", "gaze_pixels")
    lowered = src.lower()
    assert all(term not in lowered for term in forbidden)


def test_player_facing_hud_hides_decoder_scores():
    src = read("unity/Assets/Mindforge/SoulWisp/WispResonanceHud.cs")
    assert "SIGHT" in src and "GUARD" in src
    assert "SIGNAL UNCLEAR  ·  NO AURA SPENT" in src
    assert "sight_score" not in src
    assert "guard_score" not in src
    assert "confidence" not in src.lower()
    assert "quality" not in src.lower()
