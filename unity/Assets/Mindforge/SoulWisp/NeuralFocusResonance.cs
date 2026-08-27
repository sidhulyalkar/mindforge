using UnityEngine;
using Mindforge.Neural;

namespace Mindforge.SoulWisp
{
    /// <summary>
    /// Converts fresh decoder evidence into a smooth bounded resonance signal for
    /// armament presentation and secondary strength modulation.
    ///
    /// This is deliberately not a new action authority. Attack, guard, dodge and
    /// targeting remain conventional-input decisions. Gameplay consumers are also
    /// expected to gate resonance behind an already-accepted Sight/Guard aura.
    /// </summary>
    public sealed class NeuralFocusResonance : MonoBehaviour
    {
        [SerializeField] private UdpNeuralReceiver receiver;
        [SerializeField] private float staleAfterSeconds = 0.70f;
        [SerializeField] private float smoothingSharpness = 9f;
        [SerializeField] private float minimumUsefulScore = 0.18f;
        [SerializeField] private float strongScore = 0.68f;

        private float _sightTarget;
        private float _guardTarget;
        private double _lastEvidenceAt = double.NegativeInfinity;

        public float Sight { get; private set; }
        public float Guard { get; private set; }
        public bool Fresh => Time.realtimeSinceStartupAsDouble - _lastEvidenceAt <= Mathf.Max(0.05f, staleAfterSeconds);

        private void OnEnable()
        {
            if (receiver == null) receiver = Object.FindObjectOfType<UdpNeuralReceiver>(true);
            if (receiver != null) receiver.EvidenceReceived += OnEvidence;
        }

        private void OnDisable()
        {
            if (receiver != null) receiver.EvidenceReceived -= OnEvidence;
            _sightTarget = _guardTarget = 0f;
        }

        private void OnEvidence(NeuralEvent evt)
        {
            if (evt == null) return;
            _lastEvidenceAt = Time.realtimeSinceStartupAsDouble;
            if (!evt.has_evidence || evt.artifact || evt.quality <= 0f)
            {
                _sightTarget = _guardTarget = 0f;
                return;
            }

            float sight = ScoreStrength(evt.sight_score);
            float guard = ScoreStrength(evt.guard_score);
            float total = Mathf.Max(0.0001f, sight + guard);
            float quality = Mathf.Clamp01(evt.quality);

            // Absolute evidence and relative dominance both matter. This avoids a
            // misleading full-strength visual when both class scores are weak.
            _sightTarget = Mathf.Clamp01(sight * Mathf.Lerp(0.55f, 1f, sight / total) * quality);
            _guardTarget = Mathf.Clamp01(guard * Mathf.Lerp(0.55f, 1f, guard / total) * quality);
        }

        private float ScoreStrength(float score)
        {
            float low = Mathf.Max(0f, minimumUsefulScore);
            float high = Mathf.Max(low + 0.001f, strongScore);
            return Mathf.InverseLerp(low, high, Mathf.Max(0f, score));
        }

        private void Update()
        {
            if (!Fresh) _sightTarget = _guardTarget = 0f;
            float blend = 1f - Mathf.Exp(-Mathf.Max(0.01f, smoothingSharpness) * Time.unscaledDeltaTime);
            Sight = Mathf.Lerp(Sight, _sightTarget, blend);
            Guard = Mathf.Lerp(Guard, _guardTarget, blend);
        }
    }
}
