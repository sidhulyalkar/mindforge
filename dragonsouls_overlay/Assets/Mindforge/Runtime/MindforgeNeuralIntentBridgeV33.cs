using System;
using UnityEngine;

namespace Mindforge.Chassis
{
    /// <summary>
    /// Single semantic authority seam between derived neural decisions and gameplay.
    /// It validates confidence, quality, causal epoch, calibration and refractory
    /// state before publishing Sight/Guard onto the existing Mindforge intent bus.
    /// </summary>
    [DefaultExecutionOrder(980)]
    [DisallowMultipleComponent]
    public sealed class MindforgeNeuralIntentBridgeV33 : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] private float minimumConfidence = 0.55f;
        [SerializeField, Range(0f, 1f)] private float minimumQuality = 0.55f;
        [SerializeField, Min(0)] private int minimumEvidenceMs = 500;
        [SerializeField] private float semanticRefractorySeconds = 0.45f;
        [SerializeField] private bool requireCalibrationForDecoderSelections = true;

        private MindforgeUdpNeuralReceiverV33 _receiver;
        private MindforgeBciCalibrationDirectorV33 _calibration;
        private MindforgeNeuralWindowControllerV33 _windows;
        private MindforgeBciMarkerSenderV33 _markers;
        private float _nextSemanticAllowedAt;
        private bool _bound;

        public event Action<MindforgeIntentEventV29, long> SelectionAccepted;
        public event Action<MindforgeNeuralEventV33, string> SelectionRejected;

        public int AcceptedCount { get; private set; }
        public int RejectedCount { get; private set; }
        public MindforgeIntentV29 LastAcceptedIntent { get; private set; } = MindforgeIntentV29.None;
        public string LastRejectionReason { get; private set; }

        private void Start()
        {
            ResolveDependencies();
            BindReceiver();
        }

        private void Update()
        {
            if (!_bound)
            {
                ResolveDependencies();
                BindReceiver();
            }
        }

        private void OnDestroy()
        {
            if (_receiver != null) _receiver.EventReceived -= HandleNeuralEvent;
        }

        public bool InjectControllerSimulation(MindforgeIntentV29 intent, float confidence = 1f)
        {
            ResolveDependencies();
            if (_windows == null || !_windows.IsListening)
            {
                Reject(null, "no_active_window");
                return false;
            }
            if (intent != MindforgeIntentV29.Sight && intent != MindforgeIntentV29.Guard)
            {
                Reject(null, "unsupported_target");
                return false;
            }
            return ApplySemanticIntent(intent, Mathf.Clamp01(confidence), "controller_simulation", _windows.ActiveEpoch);
        }

        private void HandleNeuralEvent(MindforgeNeuralEventV33 evt)
        {
            if (evt == null || !evt.IsSelection) return;
            ResolveDependencies();

            MindforgeIntentV29 intent = evt.Intent;
            if (intent != MindforgeIntentV29.Sight && intent != MindforgeIntentV29.Guard)
            {
                Reject(evt, "unsupported_target");
                return;
            }
            if (evt.artifact)
            {
                Reject(evt, "artifact_flagged");
                return;
            }
            if (evt.confidence < minimumConfidence)
            {
                Reject(evt, "confidence_below_gate");
                return;
            }
            if (evt.quality < minimumQuality)
            {
                Reject(evt, "quality_below_gate");
                return;
            }
            if (evt.evidence_ms > 0 && evt.evidence_ms < minimumEvidenceMs)
            {
                Reject(evt, "evidence_too_short");
                return;
            }
            if (requireCalibrationForDecoderSelections && (_calibration == null || !_calibration.IsCalibrated))
            {
                Reject(evt, "calibration_not_ready");
                return;
            }
            if (_windows == null || !_windows.IsListening)
            {
                Reject(evt, "no_active_window");
                return;
            }
            if (evt.stimulus_epoch != _windows.ActiveEpoch)
            {
                Reject(evt, "epoch_mismatch");
                return;
            }
            if (Time.unscaledTime < _nextSemanticAllowedAt)
            {
                Reject(evt, "semantic_refractory");
                return;
            }

            ApplySemanticIntent(intent, evt.confidence, string.IsNullOrEmpty(evt.source_mode) ? "decoder" : evt.source_mode,
                evt.stimulus_epoch);
        }

        private bool ApplySemanticIntent(MindforgeIntentV29 intent, float confidence, string source, long epoch)
        {
            if (_windows == null || !_windows.IsListening || _windows.ActiveEpoch != epoch)
            {
                Reject(null, "window_closed_before_apply");
                return false;
            }

            MindforgeIntentBusV29.Publish(intent, confidence, Time.unscaledTimeAsDouble, source);
            _markers?.SendSemanticSelection(intent, confidence, source, epoch);
            _windows.ResolveSelection(intent, source);

            _nextSemanticAllowedAt = Time.unscaledTime + semanticRefractorySeconds;
            AcceptedCount++;
            LastAcceptedIntent = intent;
            LastRejectionReason = null;
            SelectionAccepted?.Invoke(MindforgeIntentBusV29.Last, epoch);
            Debug.Log($"[Mindforge:V33] semantic {intent} accepted from {source} c={confidence:F2} epoch={epoch}.");
            return true;
        }

        private void Reject(MindforgeNeuralEventV33 evt, string reason)
        {
            RejectedCount++;
            LastRejectionReason = reason;
            SelectionRejected?.Invoke(evt, reason);
        }

        private void ResolveDependencies()
        {
            if (_receiver == null) _receiver = GetComponent<MindforgeUdpNeuralReceiverV33>();
            if (_calibration == null) _calibration = GetComponent<MindforgeBciCalibrationDirectorV33>();
            if (_windows == null) _windows = GetComponent<MindforgeNeuralWindowControllerV33>();
            if (_markers == null) _markers = GetComponent<MindforgeBciMarkerSenderV33>();
        }

        private void BindReceiver()
        {
            if (_receiver == null) return;
            _receiver.EventReceived -= HandleNeuralEvent;
            _receiver.EventReceived += HandleNeuralEvent;
            _bound = true;
        }
    }
}
