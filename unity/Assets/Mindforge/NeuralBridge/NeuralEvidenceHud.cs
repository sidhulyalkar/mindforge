using UnityEngine;
using UnityEngine.UI;

namespace Mindforge.Neural
{
    /// <summary>
    /// Spectator-facing proof that neural evidence moves before gameplay authority.
    /// Uses the receiver's coalesced EvidenceReceived stream, not the gameplay event
    /// stream, so judges can see the latest FBCCA evidence even when an older valid
    /// selection is the one granted authority after a render stall.
    /// </summary>
    public sealed class NeuralEvidenceHud : MonoBehaviour
    {
        [SerializeField] private UdpNeuralReceiver receiver;
        [SerializeField] private Image sightFill;
        [SerializeField] private Image guardFill;
        [SerializeField] private Image qualityFill;
        [SerializeField] private Text stateText;
        [SerializeField] private Text scoreText;
        [SerializeField] private Text modeText;
        [SerializeField] private Text transportText;
        [SerializeField] private float fillResponsePerSecond = 8f;

        private float _targetSight;
        private float _targetGuard;
        private float _targetQuality;
        private bool _connected;

        private void OnEnable()
        {
            if (receiver == null) return;
            receiver.EvidenceReceived += OnNeuralEvidence;
            receiver.ConnectionStateChanged += OnConnectionStateChanged;
            _connected = receiver.IsConnected;
        }

        private void OnDisable()
        {
            if (receiver == null) return;
            receiver.EvidenceReceived -= OnNeuralEvidence;
            receiver.ConnectionStateChanged -= OnConnectionStateChanged;
        }

        private void Update()
        {
            float step = Mathf.Max(0.1f, fillResponsePerSecond) * Time.unscaledDeltaTime;
            if (sightFill != null) sightFill.fillAmount = Mathf.MoveTowards(sightFill.fillAmount, _targetSight, step);
            if (guardFill != null) guardFill.fillAmount = Mathf.MoveTowards(guardFill.fillAmount, _targetGuard, step);
            if (qualityFill != null) qualityFill.fillAmount = Mathf.MoveTowards(qualityFill.fillAmount, _targetQuality, step);

            if (transportText != null && receiver != null)
            {
                transportText.text = $"Q {receiver.QueueDepth} · old {receiver.DroppedForAge} · overflow {receiver.DroppedForBackpressure}";
            }
        }

        private void OnConnectionStateChanged(bool connected)
        {
            _connected = connected;
            if (!connected)
            {
                _targetSight = 0f;
                _targetGuard = 0f;
                _targetQuality = 0f;
                if (stateText != null) stateText.text = "BCI STALE / OFFLINE";
            }
        }

        private void OnNeuralEvidence(NeuralEvent evt)
        {
            if (evt == null) return;
            _targetSight = evt.has_evidence ? Mathf.Clamp01(evt.sight_score) : 0f;
            _targetGuard = evt.has_evidence ? Mathf.Clamp01(evt.guard_score) : 0f;
            _targetQuality = Mathf.Clamp01(evt.quality);

            if (stateText != null)
            {
                if (evt.IsParticipantStop) stateText.text = "PARTICIPANT STOP";
                else if (evt.IsLost) stateText.text = "BCI LOST";
                else if (evt.IsRecovered) stateText.text = "BCI RECOVERED";
                else if (evt.IsSelection) stateText.text = $"ACCEPT  {evt.target?.ToUpperInvariant()}";
                else stateText.text = $"ABSTAIN  {(string.IsNullOrEmpty(evt.reason) ? "UNCERTAIN" : evt.reason)}";
            }

            if (scoreText != null)
                scoreText.text = $"Sight {evt.sight_score:F3}  Guard {evt.guard_score:F3}  Δ {evt.margin:F3}  Q {evt.quality:F2}";

            if (modeText != null)
            {
                string mode = string.IsNullOrEmpty(evt.source_mode) ? "UNKNOWN" : evt.source_mode.ToUpperInvariant();
                modeText.text = $"{mode} · {evt.paradigm}";
            }
        }
    }
}
