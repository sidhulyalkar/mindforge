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
        [SerializeField] private Color baseColor = Color.cyan;

        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
        private MaterialPropertyBlock _block;
        private double _sessionStart;

        public float FrequencyHz => frequencyHz;

        private void Awake() { _block = new MaterialPropertyBlock(); _sessionStart = Time.realtimeSinceStartupAsDouble; }
        public void Configure(float frequency, Color color) { frequencyHz = frequency; baseColor = color; }

        private void LateUpdate()
        {
            double t = Time.realtimeSinceStartupAsDouble - _sessionStart;
            float sine01 = 0.5f + 0.5f * Mathf.Sin((float)(2.0 * System.Math.PI * frequencyHz * t));
            float luminance = Mathf.Lerp(minLuminance, maxLuminance, sine01);
            if (targetRenderer != null)
            {
                targetRenderer.GetPropertyBlock(_block);
                _block.SetColor(EmissionColor, baseColor * Mathf.LinearToGammaSpace(luminance));
                targetRenderer.SetPropertyBlock(_block);
            }
            if (targetLight != null) { targetLight.color = baseColor; targetLight.intensity = luminance; }
        }
    }
}
