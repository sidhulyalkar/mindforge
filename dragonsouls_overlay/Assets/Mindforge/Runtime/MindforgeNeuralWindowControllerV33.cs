using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Mindforge.Chassis
{
    /// <summary>
    /// Owns the causal listening epoch seen by both Unity and the Python decoder.
    /// A neural decision is only gameplay-authoritative when it belongs to the
    /// currently open epoch. The N key opens a developer test window.
    /// </summary>
    [DefaultExecutionOrder(970)]
    [DisallowMultipleComponent]
    public sealed class MindforgeNeuralWindowControllerV33 : MonoBehaviour
    {
        [SerializeField] private float listeningSeconds = 3.0f;
        [SerializeField] private float retryCooldownSeconds = 0.75f;
        [SerializeField] private bool allowDeveloperWindowWithoutCalibration = true;

        private MindforgeBciStimulusV33 _stimulus;
        private MindforgeBciMarkerSenderV33 _markers;
        private MindforgeBciCalibrationDirectorV33 _calibration;
        private long _nextEpoch;
        private float _openedAt;
        private float _nextOpenAllowedAt;

        public event Action<long, string> WindowOpened;
        public event Action<long, MindforgeIntentV29> WindowResolved;
        public event Action<long, string> WindowEnded;

        public bool IsListening { get; private set; }
        public long ActiveEpoch { get; private set; } = -1;
        public float RemainingSeconds => IsListening
            ? Mathf.Max(0f, listeningSeconds - (Time.unscaledTime - _openedAt))
            : 0f;

        private void Start()
        {
            ResolveDependencies();
        }

        private void Update()
        {
            if (_stimulus == null || _markers == null || _calibration == null)
                ResolveDependencies();

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.nKey.wasPressedThisFrame && !IsListening)
            {
                OpenWindow("developer_manual", requireCalibration: !allowDeveloperWindowWithoutCalibration);
            }

            if (IsListening && Time.unscaledTime - _openedAt >= listeningSeconds)
                EndWithoutSelection("timeout_abstain");
        }

        public bool OpenWindow(string reason, bool requireCalibration = true)
        {
            ResolveDependencies();
            if (IsListening || Time.unscaledTime < _nextOpenAllowedAt || _stimulus == null || _markers == null)
                return false;
            if (requireCalibration && (_calibration == null || !_calibration.IsCalibrated))
                return false;
            if (_stimulus.ParticipantPaused)
                return false;

            ActiveEpoch = ++_nextEpoch;
            _openedAt = Time.unscaledTime;
            IsListening = true;
            _stimulus.BeginListening(ActiveEpoch);
            _markers.SendNeuralWindow("NEURAL_WINDOW_LISTENING", ActiveEpoch, reason);
            WindowOpened?.Invoke(ActiveEpoch, reason);
            Debug.Log($"[Mindforge:V33] Neural window #{ActiveEpoch} listening ({reason}).");
            return true;
        }

        public bool ResolveSelection(MindforgeIntentV29 intent, string source)
        {
            if (!IsListening || ActiveEpoch < 0 || intent == MindforgeIntentV29.None)
                return false;

            long epoch = ActiveEpoch;
            IsListening = false;
            ActiveEpoch = -1;
            _nextOpenAllowedAt = Time.unscaledTime + retryCooldownSeconds;
            _stimulus?.ResolveSelection(intent);
            _markers?.SendNeuralWindow("NEURAL_WINDOW_RESOLVED", epoch, source);
            WindowResolved?.Invoke(epoch, intent);
            return true;
        }

        public void EndWithoutSelection(string reason)
        {
            if (!IsListening) return;
            long epoch = ActiveEpoch;
            IsListening = false;
            ActiveEpoch = -1;
            _nextOpenAllowedAt = Time.unscaledTime + retryCooldownSeconds;
            _stimulus?.EndListening();
            _markers?.SendNeuralWindow("NEURAL_WINDOW_ABSTAINED", epoch, reason);
            WindowEnded?.Invoke(epoch, reason);
            Debug.Log($"[Mindforge:V33] Neural window #{epoch} abstained ({reason}).");
        }

        public void Abort(string reason)
        {
            if (!IsListening) return;
            long epoch = ActiveEpoch;
            IsListening = false;
            ActiveEpoch = -1;
            _nextOpenAllowedAt = Time.unscaledTime + retryCooldownSeconds;
            _stimulus?.EndListening();
            _markers?.SendNeuralWindow("NEURAL_WINDOW_ENDED", epoch, reason);
            WindowEnded?.Invoke(epoch, reason);
        }

        private void ResolveDependencies()
        {
            if (_stimulus == null) _stimulus = GetComponent<MindforgeBciStimulusV33>();
            if (_markers == null) _markers = GetComponent<MindforgeBciMarkerSenderV33>();
            if (_calibration == null) _calibration = GetComponent<MindforgeBciCalibrationDirectorV33>();
        }
    }
}
