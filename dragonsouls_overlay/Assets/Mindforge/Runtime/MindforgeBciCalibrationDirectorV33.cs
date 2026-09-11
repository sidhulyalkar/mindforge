using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Mindforge.Chassis
{
    /// <summary>
    /// Unity-owned calibration ceremony for the production Dragon Souls chassis.
    /// Python remains decoder authority. Unity only controls presentation and sends
    /// stage markers; calibration is considered ready only after a matching
    /// CALIBRATION_READY event returns from the decoder service.
    ///
    /// V0.34 can opt into two additional safeguards without changing standalone V0.33:
    /// the visual layout must be frozen before calibration, and repeated balanced target
    /// blocks are presented so Python can reserve an independent block for validation.
    /// </summary>
    [DefaultExecutionOrder(960)]
    [DisallowMultipleComponent]
    public sealed class MindforgeBciCalibrationDirectorV33 : MonoBehaviour
    {
        public enum CalibrationState
        {
            Offline = 0,
            WaitingForTiming = 1,
            ReadyToCalibrate = 2,
            Baseline = 3,
            Sight = 4,
            Guard = 5,
            AwaitingDecoder = 6,
            Calibrated = 7,
            Failed = 8,
        }

        [Header("Protocol")]
        [SerializeField] private float baselineSeconds = 4f;
        [SerializeField] private float sightSeconds = 5f;
        [SerializeField] private float guardSeconds = 5f;
        [SerializeField] private float codedSettleSeconds = 0.15f;
        [SerializeField] private float neutralSettleSeconds = 0.35f;
        [SerializeField] private bool requireHealthySoftwareTiming = true;

        [Header("Adaptive repeated-block protocol")]
        [SerializeField] private float adaptiveBlockSeconds = 2.75f;

        private MindforgeUdpNeuralReceiverV33 _receiver;
        private MindforgeBciStimulusV33 _stimulus;
        private MindforgeBciMarkerSenderV33 _markers;
        private MindforgeDisplayTimingMonitorV33 _timing;
        private Coroutine _protocol;
        private bool _serviceReady;
        private bool _requireFrozenStimulusLayout;
        private bool _adaptiveRepeatedBlocks;
        private string _calibrationId;

        public event Action<CalibrationState> StateChanged;
        public event Action<string> CalibrationAccepted;
        public event Action<string> CalibrationRejected;

        public CalibrationState State { get; private set; } = CalibrationState.Offline;
        public bool IsCalibrated => State == CalibrationState.Calibrated;
        public bool ServiceReady => _serviceReady;
        public bool RequireFrozenStimulusLayout => _requireFrozenStimulusLayout;
        public bool AdaptiveRepeatedBlocks => _adaptiveRepeatedBlocks;
        public bool InProgress => State == CalibrationState.Baseline || State == CalibrationState.Sight ||
                                  State == CalibrationState.Guard || State == CalibrationState.AwaitingDecoder;
        public string CalibrationId => _calibrationId;

        private void Start()
        {
            ResolveDependencies();
            BindReceiver();
            RefreshIdleState();
        }

        private void OnDestroy()
        {
            if (_receiver != null) _receiver.EventReceived -= HandleNeuralEvent;
        }

        private void Update()
        {
            if (_receiver == null)
            {
                ResolveDependencies();
                BindReceiver();
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.cKey.wasPressedThisFrame && !InProgress)
                BeginCalibration();
            if (keyboard != null && keyboard.enterKey.wasPressedThisFrame && State == CalibrationState.Failed)
                BeginCalibration();

            if (!InProgress && !IsCalibrated)
                RefreshIdleState();
        }

        public void SetRequireFrozenStimulusLayout(bool required)
        {
            _requireFrozenStimulusLayout = required;
        }

        public void SetAdaptiveRepeatedBlocks(bool enabled)
        {
            _adaptiveRepeatedBlocks = enabled;
        }

        public bool BeginCalibration()
        {
            ResolveDependencies();
            if (_receiver == null || _stimulus == null || _markers == null || !_serviceReady)
            {
                SetState(CalibrationState.Offline);
                return false;
            }
            if (_requireFrozenStimulusLayout && !_stimulus.LayoutFrozen)
            {
                CalibrationRejected?.Invoke("stimulus_layout_not_frozen");
                Debug.LogWarning("[Mindforge:V33] BCI calibration refused: stimulus_layout_not_frozen");
                return false;
            }
            if (requireHealthySoftwareTiming && (_timing == null || !_timing.TimingHealthy))
            {
                SetState(CalibrationState.WaitingForTiming);
                return false;
            }
            if (_stimulus.ParticipantPaused)
            {
                Fail("stimulus_paused");
                return false;
            }

            if (_protocol != null) StopCoroutine(_protocol);
            _calibrationId = Guid.NewGuid().ToString("N");
            _protocol = StartCoroutine(RunProtocol());
            return true;
        }

        public void ResetCalibration()
        {
            if (_protocol != null)
            {
                StopCoroutine(_protocol);
                _protocol = null;
            }
            _calibrationId = null;
            if (_stimulus != null) _stimulus.EndListening();
            RefreshIdleState();
        }

        private IEnumerator RunProtocol()
        {
            if (_adaptiveRepeatedBlocks)
            {
                yield return RunAdaptiveProtocol();
                yield break;
            }

            yield return RunBaseline();
            if (State == CalibrationState.Failed) yield break;
            yield return RunTarget(MindforgeIntentV29.Sight, "sight", sightSeconds);
            if (State == CalibrationState.Failed) yield break;
            yield return new WaitForSecondsRealtime(neutralSettleSeconds);
            yield return RunTarget(MindforgeIntentV29.Guard, "guard", guardSeconds);
            if (State == CalibrationState.Failed) yield break;

            FinishPresentationAndAwaitDecoder();
        }

        private IEnumerator RunAdaptiveProtocol()
        {
            // Interleaved S-G-S-G-S-G blocks reduce simple time/order confounds while
            // keeping the final block for each class close in time. Python reserves each
            // class's final block for held-out validation rather than promotion on only
            // overlapping threshold-fit windows.
            MindforgeIntentV29[] sequence =
            {
                MindforgeIntentV29.Sight,
                MindforgeIntentV29.Guard,
                MindforgeIntentV29.Sight,
                MindforgeIntentV29.Guard,
                MindforgeIntentV29.Sight,
                MindforgeIntentV29.Guard,
            };

            float blockSeconds = Mathf.Max(1.75f, adaptiveBlockSeconds);
            float plannedSeconds = baselineSeconds + sequence.Length * blockSeconds +
                                   Mathf.Max(0, sequence.Length - 1) * neutralSettleSeconds;
            _markers.SendCalibrationStage(_calibrationId, "protocol", "begin", plannedSeconds);

            yield return RunBaseline();
            if (State == CalibrationState.Failed) yield break;

            for (int i = 0; i < sequence.Length; i++)
            {
                MindforgeIntentV29 intent = sequence[i];
                string stage = intent == MindforgeIntentV29.Sight ? "sight" : "guard";
                yield return RunTarget(intent, stage, blockSeconds);
                if (State == CalibrationState.Failed) yield break;
                if (i + 1 < sequence.Length)
                    yield return new WaitForSecondsRealtime(neutralSettleSeconds);
            }

            _markers.SendCalibrationStage(_calibrationId, "protocol", "complete", plannedSeconds);
            FinishPresentationAndAwaitDecoder();
        }

        private void FinishPresentationAndAwaitDecoder()
        {
            _stimulus.EndListening();
            SetState(CalibrationState.AwaitingDecoder);
            _protocol = null;
        }

        private IEnumerator RunBaseline()
        {
            SetState(CalibrationState.Baseline);
            _stimulus.BeginCalibrationBaseline();
            _markers.SendCalibrationStage(_calibrationId, "baseline", "begin", baselineSeconds);
            yield return WaitStage(baselineSeconds, "baseline");
            if (State == CalibrationState.Failed) yield break;
            _markers.SendCalibrationStage(_calibrationId, "baseline", "end", baselineSeconds);
            _stimulus.EndListening();
        }

        private IEnumerator RunTarget(MindforgeIntentV29 intent, string stage, float seconds)
        {
            SetState(intent == MindforgeIntentV29.Sight ? CalibrationState.Sight : CalibrationState.Guard);
            _stimulus.BeginCalibrationTarget(intent);
            yield return new WaitForSecondsRealtime(Mathf.Max(0.05f, codedSettleSeconds));
            if (!StageCanContinue())
            {
                Fail("presentation_unavailable_before_" + stage);
                yield break;
            }

            _markers.SendCalibrationStage(_calibrationId, stage, "begin", seconds);
            yield return WaitStage(seconds, stage);
            if (State == CalibrationState.Failed) yield break;
            _markers.SendCalibrationStage(_calibrationId, stage, "end", seconds);
            _stimulus.EndListening();
        }

        private IEnumerator WaitStage(float seconds, string stage)
        {
            double endAt = Time.realtimeSinceStartupAsDouble + Mathf.Max(0.5f, seconds);
            while (Time.realtimeSinceStartupAsDouble < endAt)
            {
                if (!StageCanContinue())
                {
                    _markers.SendCalibrationStage(_calibrationId, stage, "end", seconds);
                    Fail("presentation_lost_during_" + stage);
                    yield break;
                }
                yield return null;
            }
        }

        private bool StageCanContinue()
        {
            if (_stimulus == null || _stimulus.ParticipantPaused) return false;
            if (_requireFrozenStimulusLayout && !_stimulus.LayoutFrozen) return false;
            return !requireHealthySoftwareTiming || (_timing != null && _timing.TimingHealthy);
        }

        private void HandleNeuralEvent(MindforgeNeuralEventV33 evt)
        {
            if (evt == null) return;
            if (evt.IsCalibrationServiceReady || evt.IsCalibrationHeartbeat)
            {
                _serviceReady = true;
                if (!InProgress && !IsCalibrated) RefreshIdleState();
                return;
            }

            if (evt.IsLost)
            {
                _serviceReady = false;
                if (InProgress) Fail("neural_service_lost");
                else if (!IsCalibrated) SetState(CalibrationState.Offline);
                return;
            }

            if (!MatchesActiveCalibration(evt)) return;
            if (evt.IsCalibrationReady)
            {
                _serviceReady = true;
                _stimulus?.EndListening();
                SetState(CalibrationState.Calibrated);
                CalibrationAccepted?.Invoke(_calibrationId);
                Debug.Log(
                    $"[Mindforge:V33] BCI calibration accepted id={_calibrationId} " +
                    $"accuracy={evt.confidence:F2} accepted={evt.quality:F2}."
                );
            }
            else if (evt.IsCalibrationFailed)
            {
                Fail(string.IsNullOrEmpty(evt.reason) ? "decoder_rejected" : evt.reason);
            }
        }

        private bool MatchesActiveCalibration(MindforgeNeuralEventV33 evt)
        {
            if (string.IsNullOrEmpty(_calibrationId)) return false;
            return string.Equals(evt.calibration_id, _calibrationId, StringComparison.Ordinal);
        }

        private void Fail(string reason)
        {
            if (_protocol != null)
            {
                StopCoroutine(_protocol);
                _protocol = null;
            }
            _stimulus?.EndListening();
            SetState(CalibrationState.Failed);
            CalibrationRejected?.Invoke(reason);
            Debug.LogWarning($"[Mindforge:V33] BCI calibration failed: {reason}");
        }

        private void RefreshIdleState()
        {
            if (IsCalibrated) return;
            if (!_serviceReady)
            {
                SetState(CalibrationState.Offline);
                return;
            }
            if (requireHealthySoftwareTiming && (_timing == null || !_timing.TimingHealthy))
            {
                SetState(CalibrationState.WaitingForTiming);
                return;
            }
            SetState(CalibrationState.ReadyToCalibrate);
        }

        private void SetState(CalibrationState state)
        {
            if (State == state) return;
            State = state;
            StateChanged?.Invoke(state);
        }

        private void ResolveDependencies()
        {
            if (_receiver == null) _receiver = GetComponent<MindforgeUdpNeuralReceiverV33>();
            if (_stimulus == null) _stimulus = GetComponent<MindforgeBciStimulusV33>();
            if (_markers == null) _markers = GetComponent<MindforgeBciMarkerSenderV33>();
            if (_timing == null) _timing = GetComponent<MindforgeDisplayTimingMonitorV33>();
        }

        private void BindReceiver()
        {
            if (_receiver == null) return;
            _receiver.EventReceived -= HandleNeuralEvent;
            _receiver.EventReceived += HandleNeuralEvent;
        }
    }
}
