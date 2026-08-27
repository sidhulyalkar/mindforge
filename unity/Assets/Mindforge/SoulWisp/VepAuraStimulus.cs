using UnityEngine;

namespace Mindforge.SoulWisp
{
    /// Visual SSVEP stimulus renderer. Physical display timing still requires measurement.
    public sealed class VepAuraStimulus : MonoBehaviour
    {
        [SerializeField] private Renderer targetRenderer;
        [SerializeField] private Light targetLight;
        [SerializeField] private float frequencyHz = 10f;
        [SerializeField, Range(0f, 1f)] private float minLuminance = 0.30f;
        [SerializeField, Range(0f, 1f)] private float maxLuminance = 1.00f;
        [SerializeField, Range(0f, 1f)] private float restLuminance = 0.38f;
        [SerializeField] private Color baseColor = Color.cyan;

        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
        private MaterialPropertyBlock _block;
        private double _sessionStart;
        private double _restUntil;

        public float FrequencyHz => frequencyHz;
        public bool IsResting => Time.realtimeSinceStartupAsDouble < _restUntil;
        public float RestRemaining => Mathf.Max(0f, (float)(_restUntil - Time.realtimeSinceStartupAsDouble));
        public bool IsHighPhase
        {
            get
            {
                if (IsResting) return false;
                double t = Time.realtimeSinceStartupAsDouble - _sessionStart;
                return System.Math.Sin(2.0 * System.Math.PI * frequencyHz * t) >= 0.0;
            }
        }
        public float CurrentLuminance => EvaluateLuminance(Time.realtimeSinceStartupAsDouble);

        private void Awake()
        {
            _block = new MaterialPropertyBlock();
            _sessionStart = Time.realtimeSinceStartupAsDouble;
        }

        public void Configure(float frequency, Color color)
        {
            frequencyHz = frequency;
            baseColor = color;
        }

        public void RestFor(float realSeconds)
        {
            if (realSeconds <= 0f) return;
            _restUntil = System.Math.Max(_restUntil, Time.realtimeSinceStartupAsDouble + realSeconds);
        }

        private float EvaluateLuminance(double now)
        {
            if (now < _restUntil) return restLuminance;
            double t = now - _sessionStart;
            float sine01 = 0.5f + 0.5f * Mathf.Sin((float)(2.0 * System.Math.PI * frequencyHz * t));
            return Mathf.Lerp(minLuminance, maxLuminance, sine01);
        }

        private void LateUpdate()
        {
            float luminance = EvaluateLuminance(Time.realtimeSinceStartupAsDouble);
            if (targetRenderer != null)
            {
                targetRenderer.GetPropertyBlock(_block);
                _block.SetColor(EmissionColor, baseColor * Mathf.LinearToGammaSpace(luminance));
                targetRenderer.SetPropertyBlock(_block);
            }
            if (targetLight != null)
            {
                targetLight.color = baseColor;
                targetLight.intensity = luminance;
            }
        }
    }
}
