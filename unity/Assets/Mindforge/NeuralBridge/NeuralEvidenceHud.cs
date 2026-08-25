using UnityEngine;
using UnityEngine.UI;

namespace Mindforge.Neural
{
    /// <summary>
    /// Spectator-facing proof that neural evidence moves before gameplay state.
    /// Keep this presentation secondary for the player but clearly visible to judges.
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

        private void OnEnable()
        {
            if (receiver != null) receiver.EventReceived += OnNeuralEvent;
        }

        private void OnDisable()
        {
            if (receiver != null) receiver.EventReceived -= OnNeuralEvent;
        }

        private void OnNeuralEvent(NeuralEvent evt)
        {
            if (evt == null) return;
            if (sightFill != null) sightFill.fillAmount = evt.has_evidence ? Mathf.Clamp01(evt.sight_score) : 0f;
            if (guardFill != null) guardFill.fillAmount = evt.has_evidence ? Mathf.Clamp01(evt.guard_score) : 0f;
            if (qualityFill != null) qualityFill.fillAmount = Mathf.Clamp01(evt.quality);

            if (stateText != null)
            {
                if (evt.IsSelection) stateText.text = $"ACCEPT  {evt.target?.ToUpperInvariant()}";
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
