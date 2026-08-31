from pathlib import Path


def read(path):
    return Path(path).read_text(encoding="utf-8")


def write(path, text):
    Path(path).write_text(text, encoding="utf-8")


def replace_once(path, old, new):
    text = read(path)
    count = text.count(old)
    if count != 1:
        raise SystemExit(f"{path}: expected one target, found {count}: {old[:160]!r}")
    write(path, text.replace(old, new, 1))


# SoulWisp: explicit calibration ownership of the exact gameplay stimuli.
path = "unity/Assets/Mindforge/SoulWisp/SoulWispController.cs"
replace_once(path,
'''        private bool _resonanceWindowActive;
        private float _driftSeedA;''',
'''        private bool _resonanceWindowActive;
        private bool _calibrationStimuliActive;
        private bool _calibrationSwapSides;
        private float _driftSeedA;''')
replace_once(path,
'''        public bool ResonanceWindowActive => _resonanceWindowActive;
        public bool StableLockAnchorsActive =>''',
'''        public bool ResonanceWindowActive => _resonanceWindowActive;
        public bool CalibrationStimuliActive => _calibrationStimuliActive;
        public bool StableLockAnchorsActive =>''')
replace_once(path,
'''        private void OnDisable()
        {
            EndResonanceWindow();
            UnsubscribeLock();
        }''',
'''        private void OnDisable()
        {
            EndCalibrationStimuli();
            EndResonanceWindow();
            UnsubscribeLock();
        }''')
replace_once(path,
'''        public void RestStimuli(float realSeconds)
        {
            sightStimulus?.RestFor(realSeconds);
            guardStimulus?.RestFor(realSeconds);
            EndResonanceWindow();
        }''',
'''        public void RestStimuli(float realSeconds)
        {
            sightStimulus?.RestFor(realSeconds);
            guardStimulus?.RestFor(realSeconds);
            EndCalibrationStimuli();
            EndResonanceWindow();
        }''')
replace_once(path,
'''        public bool PrepareResonanceWindow()
        {
            if (StimuliResting || EffectiveTarget == null) return false;''',
'''        public bool PrepareResonanceWindow()
        {
            if (_calibrationStimuliActive || StimuliResting || EffectiveTarget == null) return false;''')
replace_once(path,
'''        public bool BeginCodedResonance()
        {
            if (!_resonanceWindowActive || StimuliResting || EffectiveTarget == null) return false;''',
'''        public bool BeginCodedResonance()
        {
            if (_calibrationStimuliActive || !_resonanceWindowActive || StimuliResting || EffectiveTarget == null) return false;''')
replace_once(path,
'''        public void EndResonanceWindow()
        {
            _resonanceWindowActive = false;
            sightStimulus?.EndWindow();
            guardStimulus?.EndWindow();
            ApplyCombatVisibility(EffectiveTarget != null);
        }

        private void Update()''',
'''        public void EndResonanceWindow()
        {
            _resonanceWindowActive = false;
            if (!_calibrationStimuliActive)
            {
                sightStimulus?.EndWindow();
                guardStimulus?.EndWindow();
            }
            ApplyCombatVisibility(EffectiveTarget != null);
        }

        /// <summary>
        /// Calibration uses the exact two coded cores used by gameplay, simultaneously.
        /// swapSides counterbalances retinal position so semantic target and gaze direction
        /// cannot remain perfectly confounded across the participant calibration.
        /// </summary>
        public bool BeginCalibrationStimuli(bool swapSides)
        {
            if (StimuliResting) return false;
            _resonanceWindowActive = false;
            _calibrationStimuliActive = true;
            _calibrationSwapSides = swapSides;
            double sharedStart = Time.realtimeSinceStartupAsDouble;
            int sharedFrame = Time.frameCount;
            sightStimulus?.BeginWindow(sharedStart, sharedFrame);
            guardStimulus?.BeginWindow(sharedStart, sharedFrame);
            ApplyCombatVisibility(true);
            return true;
        }

        public void EndCalibrationStimuli()
        {
            if (!_calibrationStimuliActive) return;
            _calibrationStimuliActive = false;
            _calibrationSwapSides = false;
            sightStimulus?.EndWindow();
            guardStimulus?.EndWindow();
            ApplyCombatVisibility(EffectiveTarget != null);
        }

        private void Update()''')
replace_once(path,
'''            Transform activeTarget = EffectiveTarget;
            UpdateCompanionDrift(activeTarget);

            if (activeTarget == null)''',
'''            Transform activeTarget = EffectiveTarget;
            UpdateCompanionDrift(activeTarget);

            if (_calibrationStimuliActive)
            {
                ApplyCombatVisibility(true);
                PlaceResonanceCores();
                return;
            }

            if (activeTarget == null)''')
replace_once(path,
'''            PlaceStableAura(sightAura, center - cam.transform.right * halfSeparation, cam, response, diameter);
            PlaceStableAura(guardAura, center + cam.transform.right * halfSeparation, cam, response, diameter);''',
'''            float sightSide = _calibrationSwapSides ? 1f : -1f;
            float guardSide = -sightSide;
            PlaceStableAura(sightAura, center + cam.transform.right * halfSeparation * sightSide, cam, response, diameter);
            PlaceStableAura(guardAura, center + cam.transform.right * halfSeparation * guardSide, cam, response, diameter);''')
replace_once(path,
'''            bool showCodedCores = combat && _resonanceWindowActive;''',
'''            bool showCodedCores = _calibrationStimuliActive || (combat && _resonanceWindowActive);''')


# Awakening calibration: dual-tag, position-counterbalanced trials with neutral gaps.
path = "unity/Assets/Mindforge/Calibration/AwakeningCalibrationDirector.cs"
text = read(path)
text = text.replace('''        private void OnDisable()
        {
            if (receiver != null) receiver.EventReceived -= OnNeuralEvent;
        }''', '''        private void OnDisable()
        {
            if (receiver != null) receiver.EventReceived -= OnNeuralEvent;
            soulWisp?.EndCalibrationStimuli();
        }''')
text = text.replace('''                SetDisplay(false, false);
                if (awakeningRoomRoot != null) awakeningRoomRoot.SetActive(false);''', '''                soulWisp?.EndCalibrationStimuli();
                SetDisplay(false, false);
                if (awakeningRoomRoot != null) awakeningRoomRoot.SetActive(false);''', 1)
text = text.replace('''                soulWisp?.SetTarget(null);
                SetDisplay(false, false);
                SetStatus("WISP LINK UNCLEAR · PRESS ENTER TO RETRY");''', '''                soulWisp?.SetTarget(null);
                soulWisp?.EndCalibrationStimuli();
                SetDisplay(false, false);
                SetStatus("WISP LINK UNCLEAR · PRESS ENTER TO RETRY");''', 1)
text = text.replace('''            guardianInput?.SetCombatActionsEnabled(false);
            soulWisp?.SetTarget(null);
            if (awakeningRoomRoot != null) awakeningRoomRoot.SetActive(true);''', '''            guardianInput?.SetCombatActionsEnabled(false);
            soulWisp?.SetTarget(null);
            soulWisp?.EndCalibrationStimuli();
            if (awakeningRoomRoot != null) awakeningRoomRoot.SetActive(true);''', 1)
text = text.replace('''            SetDisplay(false, false);
            if (awakeningRoomRoot != null) awakeningRoomRoot.SetActive(false);''', '''            soulWisp?.EndCalibrationStimuli();
            SetDisplay(false, false);
            if (awakeningRoomRoot != null) awakeningRoomRoot.SetActive(false);''', 1)
old = '''        private IEnumerator RunProtocol()
        {
            yield return RunStage("baseline", baselineSeconds, false, false, "BE STILL · LET THE WISP LISTEN");
            yield return RunStage("sight", sightSeconds, true, false, "ATTUNE TO SIGHT · BLUE");
            yield return RunStage("guard", guardSeconds, false, true, "ATTUNE TO GUARD · GREEN");
            SetDisplay(false, false);
            SetStatus("CALCULATING YOUR WISP LINK…");
            CalibrationStageChanged?.Invoke("finalizing");
        }

        private IEnumerator RunStage(string stage, float duration, bool sight, bool guard, string label)
        {
            SetDisplay(sight, guard);
            SetStatus(label);
            CalibrationStageChanged?.Invoke(stage);
            markerSender?.Send(_sessionId, stage, "begin", duration);
            yield return new WaitForSecondsRealtime(Mathf.Max(0.25f, duration));
            markerSender?.Send(_sessionId, stage, "end", duration);
        }
'''
new = '''        private IEnumerator RunProtocol()
        {
            yield return RunBaseline();
            yield return RunCounterbalancedTarget("sight", sightSeconds, "SIGHT · BLUE");
            yield return RunCounterbalancedTarget("guard", guardSeconds, "GUARD · GREEN");
            soulWisp?.EndCalibrationStimuli();
            SetDisplay(false, false);
            SetStatus("CALCULATING YOUR WISP LINK…");
            CalibrationStageChanged?.Invoke("finalizing");
        }

        private IEnumerator RunBaseline()
        {
            soulWisp?.EndCalibrationStimuli();
            SetDisplay(false, false);
            SetStatus("BE STILL · LET THE WISP LISTEN");
            CalibrationStageChanged?.Invoke("baseline");
            markerSender?.Send(_sessionId, "baseline", "begin", baselineSeconds);
            yield return new WaitForSecondsRealtime(Mathf.Max(0.5f, baselineSeconds));
            markerSender?.Send(_sessionId, "baseline", "end", baselineSeconds);
        }

        private IEnumerator RunCounterbalancedTarget(string stage, float totalDuration, string cue)
        {
            float trialSeconds = Mathf.Max(0.75f, totalDuration * 0.5f);
            bool firstSwap = string.Equals(stage, "guard", StringComparison.OrdinalIgnoreCase);
            yield return RunDualTaggedTrial(stage, trialSeconds, firstSwap,
                cue + (firstSwap ? " · LOOK LEFT" : " · LOOK LEFT"));
            yield return RunNeutralSettle();
            yield return RunDualTaggedTrial(stage, trialSeconds, !firstSwap,
                cue + (!firstSwap ? " · LOOK LEFT" : " · LOOK RIGHT"));
            yield return RunNeutralSettle();
        }

        private IEnumerator RunDualTaggedTrial(string stage, float duration, bool swapSides, string label)
        {
            SetDisplay(true, true);
            if (soulWisp == null || !soulWisp.BeginCalibrationStimuli(swapSides))
            {
                _running = false;
                _failed = true;
                SetStatus("WISP STIMULUS UNAVAILABLE · PRESS ENTER TO RETRY");
                CalibrationStageChanged?.Invoke("failed");
                yield break;
            }

            SetStatus(label);
            CalibrationStageChanged?.Invoke(stage);
            // Wait until the coded frame has been submitted before opening the EEG label.
            // Losing a few initial response samples is conservative; including pre-photon EEG is not.
            yield return new WaitForEndOfFrame();
            markerSender?.Send(_sessionId, stage, "begin", duration);
            yield return new WaitForSecondsRealtime(Mathf.Max(0.75f, duration));
            markerSender?.Send(_sessionId, stage, "end", duration);
            soulWisp.EndCalibrationStimuli();
            SetDisplay(false, false);
        }

        private IEnumerator RunNeutralSettle()
        {
            soulWisp?.EndCalibrationStimuli();
            SetDisplay(false, false);
            SetStatus("SHIFT YOUR GAZE · WISP RESETTING");
            yield return new WaitForSecondsRealtime(0.30f);
        }
'''
if old not in text:
    raise SystemExit("Awakening protocol block not found")
text = text.replace(old, new, 1)
write(path, text)


# Python calibration: preserve multiple counterbalanced segments per target instead of
# overwriting the first trial when the second marker with the same stage arrives.
path = "tools/run_unity_calibrated_decoder.py"
replace_once(path,
'''    active_chunks: list[np.ndarray] = []
    epochs: dict[str, np.ndarray] = {}
''',
'''    active_chunks: list[np.ndarray] = []
    epochs: dict[str, list[np.ndarray]] = {}
''')
replace_once(path,
'''                    if active_calibration != calibration_session:
                        epochs = {}
''',
'''                    if active_calibration != calibration_session:
                        epochs = {}
''')
replace_once(path,
'''                elif action == "end" and active_stage == stage and active_calibration == calibration_session:
                    epochs[stage] = (np.concatenate(active_chunks, axis=1)
                                     if active_chunks else np.empty((8, 0), dtype=float))
                    print(f"Calibration END {stage}: {epochs[stage].shape[1]} samples")
                    active_stage = None
                    active_chunks = []
''',
'''                elif action == "end" and active_stage == stage and active_calibration == calibration_session:
                    segment = (np.concatenate(active_chunks, axis=1)
                               if active_chunks else np.empty((8, 0), dtype=float))
                    epochs.setdefault(stage, []).append(segment)
                    print(
                        f"Calibration END {stage}: segment={len(epochs[stage])} "
                        f"samples={segment.shape[1]}")
                    active_stage = None
                    active_chunks = []
''')
replace_once(path,
'''            if all(stage in epochs for stage in STAGES):
''',
'''            if all(stage in epochs and epochs[stage] for stage in STAGES):
''')
replace_once(path,
'''                    for target, stage in ((AuraTarget.SIGHT, "sight"), (AuraTarget.GUARD, "guard")):
                        trials.extend((target, window) for window in split_windows(
                            epochs[stage], cfg.window_samples, hop))
                    profile = calibrate_decoder(decoder, trials, model_id=model_id)
                    baseline = resting_alpha_diagnostics(epochs["baseline"], cfg.sample_rate_hz)
''',
'''                    for target, stage in ((AuraTarget.SIGHT, "sight"), (AuraTarget.GUARD, "guard")):
                        for segment in epochs[stage]:
                            trials.extend((target, window) for window in split_windows(
                                segment, cfg.window_samples, hop))
                    profile = calibrate_decoder(decoder, trials, model_id=model_id)
                    baseline_epoch = np.concatenate(epochs["baseline"], axis=1)
                    baseline = resting_alpha_diagnostics(baseline_epoch, cfg.sample_rate_hz)
''')

# Regression contract.
write("tests/test_v014_calibration_parity.py", '''from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CAL = ROOT / "unity/Assets/Mindforge/Calibration/AwakeningCalibrationDirector.cs"
WISP = ROOT / "unity/Assets/Mindforge/SoulWisp/SoulWispController.cs"
RUNNER = ROOT / "tools/run_unity_calibrated_decoder.py"


def test_calibration_uses_same_simultaneous_coded_pair_as_gameplay():
    cal = CAL.read_text(encoding="utf-8")
    wisp = WISP.read_text(encoding="utf-8")
    assert "BeginCalibrationStimuli(bool swapSides)" in wisp
    assert "sightStimulus?.BeginWindow(sharedStart, sharedFrame);" in wisp
    assert "guardStimulus?.BeginWindow(sharedStart, sharedFrame);" in wisp
    assert "_calibrationStimuliActive" in wisp
    assert "SetDisplay(true, true);" in cal
    assert "BeginCalibrationStimuli(swapSides)" in cal
    assert "WaitForEndOfFrame" in cal


def test_calibration_counterbalances_semantic_target_across_screen_sides():
    cal = CAL.read_text(encoding="utf-8")
    wisp = WISP.read_text(encoding="utf-8")
    assert "RunCounterbalancedTarget(\"sight\"" in cal
    assert "RunCounterbalancedTarget(\"guard\"" in cal
    assert "firstSwap" in cal
    assert "!firstSwap" in cal
    assert "_calibrationSwapSides" in wisp
    assert "float sightSide = _calibrationSwapSides ? 1f : -1f;" in wisp


def test_python_preserves_each_counterbalanced_segment_without_crossing_neutral_gap():
    runner = RUNNER.read_text(encoding="utf-8")
    assert "epochs: dict[str, list[np.ndarray]]" in runner
    assert "epochs.setdefault(stage, []).append(segment)" in runner
    assert "for segment in epochs[stage]:" in runner
    assert "split_windows(\n                                segment" in runner
''')

print("V0.14 calibration parity patch applied")
