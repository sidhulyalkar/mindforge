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
    /// </summary>
    public sealed class AwakeningCalibrationDirector : MonoBehaviour
    {
        [SerializeField] private UdpNeuralReceiver receiver;
        [SerializeField] private CalibrationMarkerSender markerSender;
        [SerializeField] private NeuralLinkContingency linkContingency;
        [SerializeField] private GuardianCombatInput guardianInput;
        [SerializeField] private SoulWispController soulWisp;
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
        [SerializeField] private bool autoStartWhenServiceReady = true;
        [SerializeField] private KeyCode retryKey = KeyCode.Return;

        [Header("Scene events")]
        [SerializeField] private UnityEvent calibrationReady;
        [SerializeField] private UnityEvent calibrationFailed;

        private string _sessionId;
        private bool _serviceReady;
        private bool _running;
        private bool _failed;

        public string SessionId => _sessionId;
        public bool CalibrationReady { get; private set; }
        public event Action<string> CalibrationStageChanged;

        private void OnEnable()
        {
            if (receiver != null) receiver.EventReceived += OnNeuralEvent;
            guardianInput?.SetCombatActionsEnabled(false);
            soulWisp?.SetTarget(null);
            SetDisplay(false, false);
            if (awakeningRoomRoot != null) awakeningRoomRoot.SetActive(true);
            if (arenaRoot != null) arenaRoot.SetActive(false);
            SetStatus("WAITING FOR NEURAL CALIBRATION SERVICE");
        }

        private void OnDisable()
        {
            if (receiver != null) receiver.EventReceived -= OnNeuralEvent;
        }

        private void Update()
        {
            if (_failed && _serviceReady && Input.GetKeyDown(retryKey)) BeginCalibration();
        }

        private void OnNeuralEvent(NeuralEvent evt)
        {
            if (evt == null) return;
            if (evt.IsCalibrationServiceReady)
            {
                _serviceReady = true;
                SetStatus("NEURAL SERVICE READY");
                if (autoStartWhenServiceReady && !_running && !CalibrationReady) BeginCalibration();
            }
            else if (evt.IsCalibrationReady)
            {
                _running = false;
                _failed = false;
                CalibrationReady = true;
                SetStatus("WISP LINK CALIBRATED");
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
                SetDisplay(false, false);
                SetStatus("WISP LINK UNCLEAR · PRESS ENTER TO RETRY");
                CalibrationStageChanged?.Invoke("failed");
                calibrationFailed?.Invoke();
            }
        }

        public void BeginCalibration()
        {
            if (!_serviceReady || _running) return;
            guardianInput?.SetCombatActionsEnabled(false);
            soulWisp?.SetTarget(null);
            if (awakeningRoomRoot != null) awakeningRoomRoot.SetActive(true);
            if (arenaRoot != null) arenaRoot.SetActive(false);
            _sessionId = Guid.NewGuid().ToString("N");
            CalibrationReady = false;
            _failed = false;
            _running = true;
            StartCoroutine(RunProtocol());
        }

        private IEnumerator RunProtocol()
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
