from pathlib import Path


def read(path: str) -> str:
    return Path(path).read_text(encoding="utf-8")


def write(path: str, text: str) -> None:
    Path(path).write_text(text, encoding="utf-8")


def replace_once(path: str, old: str, new: str) -> None:
    text = read(path)
    count = text.count(old)
    if count != 1:
        raise SystemExit(f"{path}: expected one replacement target, found {count}: {old[:180]!r}")
    write(path, text.replace(old, new, 1))


# Central display timing authority exposes its requested rate and measurement state.
write("unity/Assets/Mindforge/SoulWisp/DisplayTimingMonitor.cs", '''using System;
using UnityEngine;

namespace Mindforge.SoulWisp
{
    /// <summary>
    /// Software frame-timing guard for visual BCI stimuli. This does not replace
    /// photodiode/high-speed-camera measurement of emitted luminance timing.
    /// </summary>
    public sealed class DisplayTimingMonitor : MonoBehaviour
    {
        [SerializeField] private float expectedRefreshHz = 120f;
        [SerializeField] private int sampleFrames = 240;
        [SerializeField] private float droppedFrameMultiplier = 1.55f;
        [SerializeField] private float maximumDropFraction = 0.01f;
        [SerializeField] private bool requestVSync = true;

        private int _frames;
        private int _drops;
        private double _sumDelta;
        private double _windowStarted;

        public float ExpectedRefreshHz => Mathf.Max(1f, expectedRefreshHz);
        public bool HasMeasurement { get; private set; }
        public bool TimingHealthy { get; private set; }
        public float ObservedRefreshHz { get; private set; }
        public float DropFraction { get; private set; }
        public event Action<bool> TimingHealthChanged;

        private void Awake()
        {
            if (requestVSync) QualitySettings.vSyncCount = 1;
            _windowStarted = Time.realtimeSinceStartupAsDouble;
        }

        private void Update()
        {
            double delta = Time.unscaledDeltaTime;
            if (delta <= 0.0) return;
            _frames++;
            _sumDelta += delta;
            double expectedDelta = 1.0 / ExpectedRefreshHz;
            if (delta > expectedDelta * droppedFrameMultiplier) _drops++;
            if (_frames < Mathf.Max(30, sampleFrames)) return;

            ObservedRefreshHz = (float)(_frames / Math.Max(_sumDelta, 1e-9));
            DropFraction = _drops / (float)_frames;
            bool rateClose = Mathf.Abs(ObservedRefreshHz - ExpectedRefreshHz) <= Mathf.Max(1f, ExpectedRefreshHz * 0.03f);
            bool healthy = rateClose && DropFraction <= maximumDropFraction;
            HasMeasurement = true;
            if (healthy != TimingHealthy)
            {
                TimingHealthy = healthy;
                TimingHealthChanged?.Invoke(healthy);
            }

            _frames = 0;
            _drops = 0;
            _sumDelta = 0.0;
            _windowStarted = Time.realtimeSinceStartupAsDouble;
        }
    }
}
''')

# Stimulus renderer takes its qualified frame rate from the shared display authority.
path = "unity/Assets/Mindforge/SoulWisp/VepAuraStimulus.cs"
replace_once(path,
'''        public void Configure(float frequency, Color color)
        {
            frequencyHz = frequency;
            baseColor = color;
        }

        public void BeginWindow''',
'''        public void Configure(float frequency, Color color)
        {
            frequencyHz = frequency;
            baseColor = color;
        }

        public void ConfigureTiming(float refreshHz)
        {
            qualifiedRefreshHz = Mathf.Max(1f, refreshHz);
        }

        public void BeginWindow''')

# SoulWisp refuses missing/incomplete stimulus pairs and derives frame rate from one monitor.
path = "unity/Assets/Mindforge/SoulWisp/SoulWispController.cs"
replace_once(path,
'''        private bool _calibrationSwapSides;
        private float _driftSeedA;''',
'''        private bool _calibrationSwapSides;
        private DisplayTimingMonitor _displayTiming;
        private float _driftSeedA;''')
replace_once(path,
'''        public bool CalibrationStimuliActive => _calibrationStimuliActive;
        public bool StableLockAnchorsActive =>''',
'''        public bool CalibrationStimuliActive => _calibrationStimuliActive;
        public bool StimulusPairAvailable => sightStimulus != null && guardStimulus != null &&
                                             sightAura != null && guardAura != null;
        public bool StableLockAnchorsActive =>''')
replace_once(path,
'''            sightStimulus?.Configure(sightFrequencyHz, sightColor);
            guardStimulus?.Configure(guardFrequencyHz, guardColor);
            sightStimulus?.EndWindow();''',
'''            _displayTiming = FindObjectOfType<DisplayTimingMonitor>(true);
            float qualifiedHz = _displayTiming != null ? _displayTiming.ExpectedRefreshHz : 120f;
            sightStimulus?.Configure(sightFrequencyHz, sightColor);
            guardStimulus?.Configure(guardFrequencyHz, guardColor);
            sightStimulus?.ConfigureTiming(qualifiedHz);
            guardStimulus?.ConfigureTiming(qualifiedHz);
            sightStimulus?.EndWindow();''')
replace_once(path,
'''        private void OnEnable()
        {
            ResolveTargetLock();
            SubscribeLock();
        }''',
'''        private void OnEnable()
        {
            if (_displayTiming == null) _displayTiming = FindObjectOfType<DisplayTimingMonitor>(true);
            ResolveTargetLock();
            SubscribeLock();
        }''')
replace_once(path,
'''            if (_calibrationStimuliActive || StimuliResting || EffectiveTarget == null) return false;''',
'''            if (!StimulusPairAvailable || _calibrationStimuliActive || StimuliResting || EffectiveTarget == null) return false;''')
replace_once(path,
'''            if (_calibrationStimuliActive || !_resonanceWindowActive || StimuliResting || EffectiveTarget == null) return false;''',
'''            if (!StimulusPairAvailable || _calibrationStimuliActive || !_resonanceWindowActive || StimuliResting || EffectiveTarget == null) return false;''')
replace_once(path,
'''        public bool BeginCalibrationStimuli(bool swapSides)
        {
            if (StimuliResting) return false;''',
'''        public bool BeginCalibrationStimuli(bool swapSides)
        {
            if (!StimulusPairAvailable || StimuliResting) return false;''')

# Live neural authority requires healthy display timing, but editor keyboard simulation remains
# useful on ordinary monitors because its event is explicitly non-neural provenance.
path = "unity/Assets/Mindforge/SoulWisp/WispResonanceWindow.cs"
replace_once(path,
'''        [SerializeField] private UdpGameMarkerSender markerSender;

        [Header("Decision timing")]''',
'''        [SerializeField] private UdpGameMarkerSender markerSender;
        [SerializeField] private DisplayTimingMonitor displayTiming;

        [Header("Decision timing")]''')
replace_once(path,
'''        [SerializeField] private int minimumEvidenceMs = 450;
''',
'''        [SerializeField] private int minimumEvidenceMs = 450;
        [SerializeField] private bool requireHealthyDisplayTimingForNeuralAuthority = true;
''')
replace_once(path,
'''            if (markerSender == null) markerSender = FindObjectOfType<UdpGameMarkerSender>(true);
#if UNITY_EDITOR''',
'''            if (markerSender == null) markerSender = FindObjectOfType<UdpGameMarkerSender>(true);
            if (displayTiming == null) displayTiming = FindObjectOfType<DisplayTimingMonitor>(true);
#if UNITY_EDITOR''')
replace_once(path,
'''            if (evt.stimulus_epoch != _windowId) return false;
            if (evt.evidence_ms < Mathf.Max(0, minimumEvidenceMs)) return false;
            return true;''',
'''            if (evt.stimulus_epoch != _windowId) return false;
            if (evt.evidence_ms < Mathf.Max(0, minimumEvidenceMs)) return false;
#if UNITY_EDITOR
            bool editorSimulation = string.Equals(evt.source_mode, "unity_editor_resonance_sim", StringComparison.Ordinal);
#else
            bool editorSimulation = false;
#endif
            if (!editorSimulation && requireHealthyDisplayTimingForNeuralAuthority &&
                (displayTiming == null || !displayTiming.HasMeasurement || !displayTiming.TimingHealthy))
                return false;
            return true;''')

# Calibration becomes a measured 120 Hz protocol, aborts immediately on a real failure, and
# delays EEG labelling until after the coded frame plus a conservative photon-onset guard.
path = "unity/Assets/Mindforge/Calibration/AwakeningCalibrationDirector.cs"
replace_once(path,
'''        [SerializeField] private SoulWispController soulWisp;
        [SerializeField] private Transform combatTarget;''',
'''        [SerializeField] private SoulWispController soulWisp;
        [SerializeField] private DisplayTimingMonitor displayTiming;
        [SerializeField] private Transform combatTarget;''')
replace_once(path,
'''        [SerializeField] private float guardSeconds = 5f;
        [SerializeField] private bool autoStartWhenServiceReady = true;''',
'''        [SerializeField] private float guardSeconds = 5f;
        [SerializeField] private float codedSettleSeconds = 0.12f;
        [SerializeField] private bool autoStartWhenServiceReady = true;''')
replace_once(path,
'''        public event Action<string> CalibrationStageChanged;

        private void OnEnable()''',
'''        public event Action<string> CalibrationStageChanged;
        private bool DisplayTimingReady => displayTiming != null && displayTiming.HasMeasurement && displayTiming.TimingHealthy;

        private void OnEnable()''')
replace_once(path,
'''            ControllerOnlyQualificationActive = false;
            if (receiver != null) receiver.EventReceived += OnNeuralEvent;''',
'''            ControllerOnlyQualificationActive = false;
            if (displayTiming == null) displayTiming = FindObjectOfType<DisplayTimingMonitor>(true);
            if (receiver != null) receiver.EventReceived += OnNeuralEvent;''')
replace_once(path,
'''        private void Update()
        {
            if (ControllerOnlyQualificationActive) return;
            if (_failed && _serviceReady && Input.GetKeyDown(retryKey)) BeginCalibration();
        }''',
'''        private void Update()
        {
            if (ControllerOnlyQualificationActive) return;
            if (_serviceReady && autoStartWhenServiceReady && !_running && !_failed && !CalibrationReady && DisplayTimingReady)
                BeginCalibration();
            if (_failed && _serviceReady && DisplayTimingReady && Input.GetKeyDown(retryKey)) BeginCalibration();
        }''')
replace_once(path,
'''                SetStatus("NEURAL SERVICE READY");
                if (autoStartWhenServiceReady && !_running && !CalibrationReady) BeginCalibration();''',
'''                SetStatus(DisplayTimingReady
                    ? "NEURAL SERVICE READY"
                    : "NEURAL READY · WAITING FOR STABLE 120 HZ");
                if (autoStartWhenServiceReady && !_running && !CalibrationReady && DisplayTimingReady) BeginCalibration();''')
replace_once(path,
'''        public void BeginCalibration()
        {
            if (ControllerOnlyQualificationActive || !_serviceReady || _running) return;''',
'''        public void BeginCalibration()
        {
            if (ControllerOnlyQualificationActive || !_serviceReady || _running) return;
            if (!DisplayTimingReady)
            {
                SetStatus("WAITING FOR STABLE 120 HZ DISPLAY TIMING");
                return;
            }
            if (soulWisp == null || !soulWisp.StimulusPairAvailable)
            {
                FailCalibration("WISP STIMULUS PAIR MISSING");
                return;
            }''')
replace_once(path,
'''        private IEnumerator RunProtocol()
        {
            yield return RunBaseline();
            yield return RunCounterbalancedTarget("sight", sightSeconds, "SIGHT · BLUE");
            yield return RunCounterbalancedTarget("guard", guardSeconds, "GUARD · GREEN");
            soulWisp?.EndCalibrationStimuli();''',
'''        private IEnumerator RunProtocol()
        {
            yield return RunBaseline();
            if (_failed) yield break;
            yield return RunCounterbalancedTarget("sight", sightSeconds, "SIGHT · BLUE");
            if (_failed) yield break;
            yield return RunCounterbalancedTarget("guard", guardSeconds, "GUARD · GREEN");
            if (_failed) yield break;
            soulWisp?.EndCalibrationStimuli();''')
replace_once(path,
'''            markerSender?.Send(_sessionId, "baseline", "begin", baselineSeconds);
            yield return new WaitForSecondsRealtime(Mathf.Max(0.5f, baselineSeconds));
            markerSender?.Send(_sessionId, "baseline", "end", baselineSeconds);''',
'''            markerSender?.Send(_sessionId, "baseline", "begin", baselineSeconds);
            double endAt = Time.realtimeSinceStartupAsDouble + Mathf.Max(0.5f, baselineSeconds);
            while (Time.realtimeSinceStartupAsDouble < endAt)
            {
                if (!DisplayTimingReady)
                {
                    markerSender?.Send(_sessionId, "baseline", "end", baselineSeconds);
                    FailCalibration("DISPLAY TIMING LOST DURING BASELINE");
                    yield break;
                }
                yield return null;
            }
            markerSender?.Send(_sessionId, "baseline", "end", baselineSeconds);''')
replace_once(path,
'''            yield return RunDualTaggedTrial(stage, trialSeconds, firstSwap,
                cue);
            yield return RunNeutralSettle();
            yield return RunDualTaggedTrial(stage, trialSeconds, !firstSwap,
                cue);
            yield return RunNeutralSettle();''',
'''            yield return RunDualTaggedTrial(stage, trialSeconds, firstSwap, cue);
            if (_failed) yield break;
            yield return RunNeutralSettle();
            if (_failed) yield break;
            yield return RunDualTaggedTrial(stage, trialSeconds, !firstSwap, cue);
            if (_failed) yield break;
            yield return RunNeutralSettle();''')
replace_once(path,
'''            if (soulWisp == null || !soulWisp.BeginCalibrationStimuli(swapSides))
            {
                _running = false;
                _failed = true;
                SetStatus("WISP STIMULUS UNAVAILABLE · PRESS ENTER TO RETRY");
                CalibrationStageChanged?.Invoke("failed");
                yield break;
            }

            SetStatus(label);''',
'''            if (!DisplayTimingReady || soulWisp == null || !soulWisp.BeginCalibrationStimuli(swapSides))
            {
                FailCalibration(!DisplayTimingReady ? "DISPLAY TIMING UNHEALTHY" : "WISP STIMULUS UNAVAILABLE");
                yield break;
            }

            SetStatus(label);''')
replace_once(path,
'''            // Wait until the coded frame has been submitted before opening the EEG label.
            // Losing a few initial response samples is conservative; including pre-photon EEG is not.
            yield return new WaitForEndOfFrame();
            markerSender?.Send(_sessionId, stage, "begin", duration);
            yield return new WaitForSecondsRealtime(Mathf.Max(0.75f, duration));
            markerSender?.Send(_sessionId, stage, "end", duration);
            soulWisp.EndCalibrationStimuli();''',
'''            // Submit the coded frame, then allow geometry/display latency to settle before
            // opening the labelled EEG epoch. Excluding early response is conservative;
            // accidentally labelling pre-photon EEG as SSVEP evidence is not.
            yield return new WaitForEndOfFrame();
            yield return new WaitForSecondsRealtime(Mathf.Max(0.05f, codedSettleSeconds));
            if (!DisplayTimingReady)
            {
                FailCalibration("DISPLAY TIMING LOST BEFORE CODED TRIAL");
                yield break;
            }
            markerSender?.Send(_sessionId, stage, "begin", duration);
            double endAt = Time.realtimeSinceStartupAsDouble + Mathf.Max(0.75f, duration);
            while (Time.realtimeSinceStartupAsDouble < endAt)
            {
                if (!DisplayTimingReady)
                {
                    markerSender?.Send(_sessionId, stage, "end", duration);
                    FailCalibration("DISPLAY TIMING LOST DURING CODED TRIAL");
                    yield break;
                }
                yield return null;
            }
            markerSender?.Send(_sessionId, stage, "end", duration);
            soulWisp.EndCalibrationStimuli();''')
replace_once(path,
'''        private void SetDisplay(bool sight, bool guard)
        {''',
'''        private void FailCalibration(string reason)
        {
            StopAllCoroutines();
            _running = false;
            _failed = true;
            CalibrationReady = false;
            soulWisp?.EndCalibrationStimuli();
            SetDisplay(false, false);
            SetStatus(reason + " · PRESS ENTER TO RETRY");
            CalibrationStageChanged?.Invoke("failed");
            calibrationFailed?.Invoke();
        }

        private void SetDisplay(bool sight, bool guard)
        {''')

# Scene assembler wires calibration timing explicitly rather than relying on runtime discovery.
path = "unity/Assets/Mindforge/Editor/CompetitionSceneAssembler.cs"
replace_once(path,
'''            SetRef(calibration, "receiver", receiver); SetRef(calibration, "markerSender", marker); SetRef(calibration, "linkContingency", contingency);
            SetRef(calibration, "guardianInput", input); SetRef(calibration, "soulWisp", wispController); SetRef(calibration, "combatTarget", boss.transform);''',
'''            SetRef(calibration, "receiver", receiver); SetRef(calibration, "markerSender", marker); SetRef(calibration, "linkContingency", contingency);
            SetRef(calibration, "guardianInput", input); SetRef(calibration, "soulWisp", wispController); SetRef(calibration, "displayTiming", timing); SetRef(calibration, "combatTarget", boss.transform);''')

# Every calibration segment gets the same queued-EEG purge as a combat resonance epoch.
path = "tools/run_unity_calibrated_decoder.py"
replace_once(path,
'''                    active_game_session = game_session
                    active_calibration = calibration_session
                    active_stage = stage
                    active_chunks = []
                    if phantom_enabled:''',
'''                    active_game_session = game_session
                    active_calibration = calibration_session
                    active_stage = stage
                    active_chunks = []
                    source.flush()
                    if phantom_enabled:''')

# Final regression contract.
write("tests/test_v014_display_and_calibration_hardening.py", '''from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
TIMING = ROOT / "unity/Assets/Mindforge/SoulWisp/DisplayTimingMonitor.cs"
STIM = ROOT / "unity/Assets/Mindforge/SoulWisp/VepAuraStimulus.cs"
WISP = ROOT / "unity/Assets/Mindforge/SoulWisp/SoulWispController.cs"
WINDOW = ROOT / "unity/Assets/Mindforge/SoulWisp/WispResonanceWindow.cs"
CAL = ROOT / "unity/Assets/Mindforge/Calibration/AwakeningCalibrationDirector.cs"
ASSEMBLER = ROOT / "unity/Assets/Mindforge/Editor/CompetitionSceneAssembler.cs"
RUNNER = ROOT / "tools/run_unity_calibrated_decoder.py"


def test_one_display_timing_authority_drives_and_gates_real_stimulus():
    timing = TIMING.read_text(encoding="utf-8")
    stim = STIM.read_text(encoding="utf-8")
    wisp = WISP.read_text(encoding="utf-8")
    window = WINDOW.read_text(encoding="utf-8")
    assert "public float ExpectedRefreshHz" in timing
    assert "public bool HasMeasurement" in timing
    assert "ConfigureTiming(float refreshHz)" in stim
    assert "_displayTiming.ExpectedRefreshHz" in wisp
    assert "StimulusPairAvailable" in wisp
    assert "requireHealthyDisplayTimingForNeuralAuthority" in window
    assert "displayTiming.HasMeasurement" in window
    assert "displayTiming.TimingHealthy" in window
    assert "unity_editor_resonance_sim" in window


def test_calibration_waits_for_and_monitors_qualified_display():
    cal = CAL.read_text(encoding="utf-8")
    assembler = ASSEMBLER.read_text(encoding="utf-8")
    assert "DisplayTimingReady" in cal
    assert "WAITING FOR STABLE 120 HZ DISPLAY TIMING" in cal
    assert "codedSettleSeconds = 0.12f" in cal
    assert "DISPLAY TIMING LOST DURING CODED TRIAL" in cal
    assert "if (_failed) yield break;" in cal
    assert 'SetRef(calibration, "displayTiming", timing)' in assembler


def test_calibration_eeg_is_flushed_at_every_segment_begin():
    runner = RUNNER.read_text(encoding="utf-8")
    anchor = "active_chunks = []\n                    source.flush()"
    assert anchor in runner
''')

print("V0.14 final hardening patch applied")
