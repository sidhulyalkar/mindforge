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
    /// actions while still allowing movement/UI. Recovery must remain stable briefly
    /// before combat resumes, preventing connection flapping from creating bursty play.
    /// </summary>
    public sealed class NeuralLinkContingency : MonoBehaviour
    {
        [SerializeField] private UdpNeuralReceiver receiver;
        [SerializeField] private FracturedSignalDirector bossDirector;
        [SerializeField] private GuardianCombatInput guardianInput;
        [SerializeField] private CanvasGroup warningGroup;
        [SerializeField] private Text warningText;
        [SerializeField] private float stableRecoverySeconds = 0.75f;

        private bool _armed;
        private bool _degraded;
        private Coroutine _recovery;

        public bool Degraded => _degraded;
        public event Action<bool> DegradationStateChanged;

        private void OnEnable()
        {
            if (receiver != null) receiver.ConnectionStateChanged += OnConnectionChanged;
            SetWarning(false);
        }

        private void OnDisable()
        {
            if (receiver != null) receiver.ConnectionStateChanged -= OnConnectionChanged;
            if (_recovery != null) StopCoroutine(_recovery);
            _recovery = null;
        }

        public void ArmForCombat()
        {
            _armed = true;
            if (receiver != null && !receiver.IsConnected) EnterDegraded();
        }

        public void Disarm()
        {
            _armed = false;
            ExitDegraded();
        }

        private void OnConnectionChanged(bool connected)
        {
            if (!_armed) return;
            if (!connected)
            {
                if (_recovery != null) StopCoroutine(_recovery);
                _recovery = null;
                EnterDegraded();
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
            if (receiver != null && receiver.IsConnected) ExitDegraded();
        }

        private void EnterDegraded()
        {
            if (_degraded) return;
            _degraded = true;
            bossDirector?.SetExternalPause(true);
            guardianInput?.SetCombatActionsEnabled(false);
            Shader.SetGlobalFloat("_MindforgeNeuralLinkDegraded", 1f);
            SetWarning(true);
            DegradationStateChanged?.Invoke(true);
        }

        private void ExitDegraded()
        {
            if (!_degraded) return;
            _degraded = false;
            bossDirector?.SetExternalPause(false);
            guardianInput?.SetCombatActionsEnabled(true);
            Shader.SetGlobalFloat("_MindforgeNeuralLinkDegraded", 0f);
            SetWarning(false);
            DegradationStateChanged?.Invoke(false);
        }

        private void SetWarning(bool visible)
        {
            if (warningText != null) warningText.text = visible ? "NEURAL LINK UNSTABLE" : string.Empty;
            if (warningGroup == null) return;
            warningGroup.alpha = visible ? 1f : 0f;
            warningGroup.interactable = false;
            warningGroup.blocksRaycasts = false;
        }
    }
}
