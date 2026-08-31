using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Mindforge.Combat;
using Mindforge.Neural;
using Mindforge.SoulWisp;

namespace Mindforge.Calibration
{
    /// <summary>
    /// In-world Unity/Python calibration handshake. Baseline is collected before
    /// periodic visual stimulation so endogenous alpha is characterized without
    /// recent SSVEP carry-over. Combat actions stay locked until Python accepts the
    /// participant-specific calibration.
    ///
    /// V0.15 adds an optional presentation gate: a demo scene may move the camera and
    /// decorative environment first, then explicitly park the camera and release a
    /// visually quiet neural scene. The gate defaults off so non-demo scenes preserve
    /// the original calibration lifecycle.
    /// </summary>
    public sealed class AwakeningCalibrationDirector : MonoBehaviour
    {
        [SerializeField] private UdpNeuralReceiver receiver;
        [SerializeField] private CalibrationMarkerSender markerSender;
        [SerializeField] private NeuralLinkContingency linkContingency;
        [SerializeField] private GuardianCombatInput guardianInput;
        [SerializeField] private SoulWispController soulWisp;
        [SerializeField] private DisplayTimingMonitor displayTiming;
        [SerializeField] private Transform combatTarget;
        [SerializeField] private GameObject wispCoreRoot;
        [SerializeField] private GameObject sightAuraRoot;
        [SerializeField] private GameObject guardAuraRoot;
        [SerializeField] private GameObject awakeningRoomRoot;
        [SerializeField] private GameObject arenaRoot;
        [SerializeField] private Text statusText;

        [Header("Protocol")]
        [SerializeField] private float baselineSeconds = 5f;
        [SerializeField] private float sightSeconds = 5f;
        [SerializeField] private float guardSeconds = 5f;
        [SerializeField] private float codedSettleSeconds = 0.12f;
        [SerializeField] private bool autoStartWhenServiceReady = true;
        [SerializeField] private KeyCode retryKey = KeyCode.Return;

        [Header("Presentation gate")]
        [Tooltip("Optional demo-only gate. Shared/non-demo scenes leave this disabled.")]
        [SerializeField] private bool requireIntroReady = false;

        [Header("Scene events")]
        [SerializeField] private UnityEvent calibrationReady;
        [SerializeField] private UnityEvent calibrationFailed;

        private string _sessionId;
        private bool _serviceReady;
        private bool _running;
        private bool _failed;
        private bool _introReady;

        public string SessionId => _sessionId;
        public bool CalibrationReady { get; private set; }
        public bool ControllerOnlyQualificationActive { get; private set; }
        public bool CalibrationInProgress => _running;
        public bool IntroReady => !requireIntroReady || _introReady;
        public bool NeuralServiceReady => _serviceReady;
        public event Action<string> CalibrationStageChanged;
        private bool DisplayTimingReady => displayTiming != null && displayTiming.HasMeasurement && displayTiming.TimingHealthy;

        private void OnEnable()
        {
            ControllerOnlyQualificationActive = false;
            _introReady = !requireIntroReady;
            if (displayTiming == null) displayTiming = FindObjectOfType<DisplayTimingMonitor>(true);
            if (receiver != null) receiver.EventReceived += OnNeuralEvent;
            guardianInput?.SetCombatActionsEnabled(false);
            soulWisp?.SetTarget(null);
            SetDisplay(false, false);
            if (awakeningRoomRoot != null) awakeningRoomRoot.SetActive(true);
            if (arenaRoot != null) arenaRoot.SetActive(false);
            SetWaitingStatus();
        }

        private void OnDisable()
        {
            if (receiver != null) receiver.EventReceived -= OnNeuralEvent;
            soulWisp?.EndCalibrationStimuli();
        }

        private void Update()
        {
            if (ControllerOnlyQualificationActive) return;
            if (_serviceReady && IntroReady && autoStartWhenServiceReady && !_running && !_failed && !CalibrationReady && DisplayTimingReady)
                BeginCalibration();
            if (_failed && _serviceReady && IntroReady && DisplayTimingReady && Input.GetKeyDown(retryKey)) BeginCalibration();
        }

        /// <summary>
        /// Enables/disables the optional presentation gate. Demo installers call this
        /// before Start/Update authority is possible; shared scenes never need to.
        /// </summary>
        public void ConfigureIntroGate(bool required)
        {
            requireIntroReady = required;
            _introReady = !required;
            if (!_running && !CalibrationReady) SetWaitingStatus();
        }

        /// <summary>
        /// Presentation-only readiness handshake. Calling this never invents neural
        /// calibration success; it only permits the real Unity/Python protocol to begin.
        /// </summary>
        public void SetIntroReady(bool ready)
        {
            _introReady = ready || !requireIntroReady;
            if (!_running && !CalibrationReady) SetWaitingStatus();
        }

        private void OnNeuralEvent(NeuralEvent evt)
        {
            if (ControllerOnlyQualificationActive || evt == null) return;
            if (evt.IsCalibrationServiceReady)
            {
                _serviceReady = true;
                SetWaitingStatus();
                if (IntroReady && autoStartWhenServiceReady && !_running && !CalibrationReady && DisplayTimingReady) BeginCalibration();
            }
            else if (evt.IsCalibrationReady)
            {
                _running = false;
                _failed = false;
                CalibrationReady = true;
                SetStatus("WISP LINK CALIBRATED");
                soulWisp?.EndCalibrationStimuli();
                SetDisplay(false, false);
                if (awakeningRoomRoot != null) awakeningRoomRoot.SetActive(false);
                if (arenaRoot != null) arenaRoot.SetActive(true);
                soulWisp?.SetTarget(combatTarget);
                guardianInput?.SetCombatActionsEnabled(true);
                linkContingency?.ArmForCombat();
                CalibrationStageChanged?.Invoke("ready");
                calibrationReady?.Invoke();
            }
            else if (evt.IsCalibrationFailed)
            {
                _running = false;
                _failed = true;
                CalibrationReady = false;
                guardianInput?.SetCombatActionsEnabled(false);
                soulWisp?.SetTarget(null);
                soulWisp?.EndCalibrationStimuli();
                SetDisplay(false, false);
                SetStatus("WISP LINK UNCLEAR · PRESS ENTER TO RETRY");
                CalibrationStageChanged?.Invoke("failed");
                calibrationFailed?.Invoke();
            }
        }

        public void BeginCalibration()
        {
            if (ControllerOnlyQualificationActive || !_serviceReady || _running || !IntroReady) return;
            if (!DisplayTimingReady)
            {
                SetStatus("WAITING FOR STABLE 120 HZ DISPLAY TIMING");
                return;
            }
            if (soulWisp == null || !soulWisp.StimulusPairAvailable)
            {
                FailCalibration("WISP STIMULUS PAIR MISSING");
                return;
            }
            guardianInput?.SetCombatActionsEnabled(false);
            soulWisp?.SetTarget(null);
            soulWisp?.EndCalibrationStimuli();
            if (awakeningRoomRoot != null) awakeningRoomRoot.SetActive(true);
            if (arenaRoot != null) arenaRoot.SetActive(false);
            _sessionId = Guid.NewGuid().ToString("N");
            CalibrationReady = false;
            _failed = false;
            _running = true;
            StartCoroutine(RunProtocol());
        }

        /// <summary>
        /// Opens the real combat encounter for P2 game-only qualification without
        /// inventing calibration success. This method is unavailable to release
        /// player builds and is only called by the explicitly labelled qualification
        /// bootstrap.
        /// </summary>
        public bool EnterControllerOnlyQualification()
        {
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
            return false;
#else
            StopAllCoroutines();
            _running = false;
            _failed = false;
            _serviceReady = false;
            CalibrationReady = false;
            ControllerOnlyQualificationActive = true;

            soulWisp?.EndCalibrationStimuli();
            SetDisplay(false, false);
            if (awakeningRoomRoot != null) awakeningRoomRoot.SetActive(false);
            if (arenaRoot != null) arenaRoot.SetActive(true);
            soulWisp?.SetTarget(combatTarget);
            guardianInput?.SetCombatActionsEnabled(true);
            linkContingency?.Disarm();
            SetStatus("P2 CONTROLLER-ONLY QUALIFICATION · BCI DISABLED");
            CalibrationStageChanged?.Invoke("controller_only");
            return true;
#endif
        }

        private IEnumerator RunProtocol()
        {
            yield return RunBaseline();
            if (_failed) yield break;
            yield return RunCounterbalancedTarget("sight", sightSeconds, "SIGHT · BLUE");
            if (_failed) yield break;
            yield return RunCounterbalancedTarget("guard", guardSeconds, "GUARD · GREEN");
            if (_failed) yield break;
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
            markerSender?.Send(_sessionId, "baseline", "end", baselineSeconds);
        }

        private IEnumerator RunCounterbalancedTarget(string stage, float totalDuration, string cue)
        {
            float trialSeconds = Mathf.Max(0.75f, totalDuration * 0.5f);
            bool firstSwap = string.Equals(stage, "guard", StringComparison.OrdinalIgnoreCase);
            yield return RunDualTaggedTrial(stage, trialSeconds, firstSwap, cue);
            if (_failed) yield break;
            yield return RunNeutralSettle();
            if (_failed) yield break;
            yield return RunDualTaggedTrial(stage, trialSeconds, !firstSwap, cue);
            if (_failed) yield break;
            yield return RunNeutralSettle();
        }

        private IEnumerator RunDualTaggedTrial(string stage, float duration, bool swapSides, string label)
        {
            SetDisplay(true, true);
            if (!DisplayTimingReady || soulWisp == null || !soulWisp.BeginCalibrationStimuli(swapSides))
            {
                FailCalibration(!DisplayTimingReady ? "DISPLAY TIMING UNHEALTHY" : "WISP STIMULUS UNAVAILABLE");
                yield break;
            }

            SetStatus(label);
            CalibrationStageChanged?.Invoke(stage);
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

        private void FailCalibration(string reason)
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

        private void SetWaitingStatus()
        {
            if (!IntroReady)
            {
                SetStatus(_serviceReady ? "NEURAL READY · INTRO SEQUENCE" : "INITIALIZING NEURAL LINK");
                return;
            }
            if (!_serviceReady)
            {
                SetStatus("WAITING FOR EEG HEADSET");
                return;
            }
            SetStatus(DisplayTimingReady ? "NEURAL SERVICE READY" : "NEURAL READY · WAITING FOR STABLE 120 HZ");
        }

        private void SetDisplay(bool sight, bool guard)
        {
            if (wispCoreRoot != null) wispCoreRoot.SetActive(!sight && !guard);
            if (sightAuraRoot != null) sightAuraRoot.SetActive(sight);
            if (guardAuraRoot != null) guardAuraRoot.SetActive(guard);
        }

        private void SetStatus(string value)
        {
            if (statusText != null) statusText.text = value;
        }
    }
}