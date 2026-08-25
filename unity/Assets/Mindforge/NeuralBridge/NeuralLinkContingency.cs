using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Mindforge.Combat;

namespace Mindforge.Neural
{
    /// <summary>
    /// Demo-day fairness gate for acquisition silence. It arms only after successful
    /// calibration. A stale neural stream pauses enemy authority and Guardian combat
    /// actions while still allowing movement/UI. PARTICIPANT_STOP is terminal.
    /// </summary>
    public sealed class NeuralLinkContingency : MonoBehaviour
    {
        [SerializeField] private UdpNeuralReceiver receiver;
        [SerializeField] private FracturedSignalDirector bossDirector;
        [SerializeField] private GuardianCombatInput guardianInput;
        [SerializeField] private CanvasGroup warningGroup;
        [SerializeField] private Text warningText;
        [Tooltip("Neutral gray fullscreen veil. Keep it outside the VEP-core renderers.")]
        [SerializeField] private CanvasGroup desaturationVeil;
        [SerializeField, Range(0f, 1f)] private float degradedVeilAlpha = 0.58f;
        [SerializeField] private float stableRecoverySeconds = 0.75f;

        private bool _armed;
        private bool _degraded;
        private bool _participantStopped;
        private Coroutine _recovery;

        public bool Degraded => _degraded;
        public bool ParticipantStopped => _participantStopped;
        public event Action<bool> DegradationStateChanged;

        private void OnEnable()
        {
            if (receiver != null)
            {
                receiver.ConnectionStateChanged += OnConnectionChanged;
                receiver.EventReceived += OnNeuralAuthority;
            }
            SetPresentation(false, false);
        }

        private void OnDisable()
        {
            if (receiver != null)
            {
                receiver.ConnectionStateChanged -= OnConnectionChanged;
                receiver.EventReceived -= OnNeuralAuthority;
            }
            if (_recovery != null) StopCoroutine(_recovery);
            _recovery = null;
        }

        public void ArmForCombat()
        {
            if (_participantStopped) return;
            _armed = true;
            if (receiver != null && !receiver.IsConnected) EnterDegraded(false);
        }

        public void Disarm()
        {
            if (_participantStopped) return;
            _armed = false;
            ExitDegraded();
        }

        private void OnNeuralAuthority(NeuralEvent evt)
        {
            if (evt == null || !evt.IsParticipantStop) return;
            _participantStopped = true;
            _armed = true;
            if (_recovery != null) StopCoroutine(_recovery);
            _recovery = null;
            EnterDegraded(true);
        }

        private void OnConnectionChanged(bool connected)
        {
            if (!_armed || _participantStopped) return;
            if (!connected)
            {
                if (_recovery != null) StopCoroutine(_recovery);
                _recovery = null;
                EnterDegraded(false);
            }
            else if (_degraded && _recovery == null)
            {
                _recovery = StartCoroutine(RecoverWhenStable());
            }
        }

        private IEnumerator RecoverWhenStable()
        {
            yield return new WaitForSecondsRealtime(Mathf.Max(0.1f, stableRecoverySeconds));
            _recovery = null;
            if (!_participantStopped && receiver != null && receiver.IsConnected) ExitDegraded();
        }

        private void EnterDegraded(bool participantStop)
        {
            bool changed = !_degraded;
            _degraded = true;
            bossDirector?.SetExternalPause(true);
            guardianInput?.SetCombatActionsEnabled(false);
            Shader.SetGlobalFloat("_MindforgeNeuralLinkDegraded", 1f);
            SetPresentation(true, participantStop || _participantStopped);
            if (changed) DegradationStateChanged?.Invoke(true);
        }

        private void ExitDegraded()
        {
            if (!_degraded || _participantStopped) return;
            _degraded = false;
            bossDirector?.SetExternalPause(false);
            guardianInput?.SetCombatActionsEnabled(true);
            Shader.SetGlobalFloat("_MindforgeNeuralLinkDegraded", 0f);
            SetPresentation(false, false);
            DegradationStateChanged?.Invoke(false);
        }

        private void SetPresentation(bool visible, bool participantStop)
        {
            if (warningText != null)
                warningText.text = visible ? (participantStop ? "PARTICIPANT STOP · COMBAT SAFE" : "NEURAL LINK UNSTABLE") : string.Empty;
            if (warningGroup != null)
            {
                warningGroup.alpha = visible ? 1f : 0f;
                warningGroup.interactable = false;
                warningGroup.blocksRaycasts = false;
            }
            if (desaturationVeil != null)
            {
                desaturationVeil.alpha = visible ? degradedVeilAlpha : 0f;
                desaturationVeil.interactable = false;
                desaturationVeil.blocksRaycasts = false;
            }
        }
    }
}
