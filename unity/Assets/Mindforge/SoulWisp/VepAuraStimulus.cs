using UnityEngine;

namespace Mindforge.SoulWisp
{
    /// <summary>
    /// Frame-indexed visual SSVEP stimulus. The coded core is active only inside an explicitly
    /// opened resonance window. The luminance sequence is derived from presented frame index at
    /// the qualified refresh rate, so renderer phase, photodiode phase and experiment logs share
    /// one deterministic sequence. Physical photon timing still requires final-display measurement.
    ///
    /// By default only the small coded renderer is luminance-modulated. A world-space Light must
    /// never silently splash the tag across the arena because that would destroy the controlled
    /// retinal geometry. Light modulation remains an explicit qualification-only opt-in.
    /// </summary>
    public sealed class VepAuraStimulus : MonoBehaviour
    {
        [SerializeField] private Renderer targetRenderer;
        [SerializeField] private Light targetLight;
        [SerializeField] private bool modulateTargetLight;
        [SerializeField] private float frequencyHz = 10f;
        [SerializeField] private float qualifiedRefreshHz = 120f;
        [SerializeField, Range(0f, 1f)] private float minLuminance = 0.30f;
        [SerializeField, Range(0f, 1f)] private float maxLuminance = 1.00f;
        [SerializeField, Range(0f, 1f)] private float restLuminance = 0.38f;
        [SerializeField] private Color baseColor = Color.cyan;

        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
        private MaterialPropertyBlock _block;
        private double _sessionStart;
        private double _restUntil;
        private int _sessionStartFrame;
        private bool _codedActive;

        public float FrequencyHz => frequencyHz;
        public float QualifiedRefreshHz => qualifiedRefreshHz;
        public int SessionStartFrame => _sessionStartFrame;
        public bool CodedActive => _codedActive && !IsResting;
        public bool IsResting => Time.realtimeSinceStartupAsDouble < _restUntil;
        public float RestRemaining => Mathf.Max(0f, (float)(_restUntil - Time.realtimeSinceStartupAsDouble));
        private double FrameTimeSeconds => Mathf.Max(0, Time.frameCount - _sessionStartFrame) / (double)Mathf.Max(1f, qualifiedRefreshHz);
        public bool IsHighPhase => CodedActive && System.Math.Sin(2.0 * System.Math.PI * frequencyHz * FrameTimeSeconds) >= 0.0;
        public float CurrentLuminance => EvaluateLuminance(Time.realtimeSinceStartupAsDouble, Time.frameCount);

        private void Awake()
        {
            _block = new MaterialPropertyBlock();
            _sessionStart = Time.realtimeSinceStartupAsDouble;
            _sessionStartFrame = Time.frameCount;
            _codedActive = false;
        }

        public void Configure(float frequency, Color color)
        {
            frequencyHz = frequency;
            baseColor = color;
        }

        public void ConfigureTiming(float refreshHz)
        {
            qualifiedRefreshHz = Mathf.Max(1f, refreshHz);
        }

        public void BeginWindow(double sharedStart) => BeginWindow(sharedStart, Time.frameCount);

        /// <summary>Starts a coded window from one shared time+frame phase epoch.</summary>
        public void BeginWindow(double sharedStart, int sharedFrame)
        {
            if (IsResting)
            {
                _codedActive = false;
                return;
            }
            _sessionStart = sharedStart;
            _sessionStartFrame = sharedFrame;
            _codedActive = true;
        }

        public void EndWindow() => _codedActive = false;

        public void RestFor(float realSeconds)
        {
            if (realSeconds <= 0f) return;
            _codedActive = false;
            _restUntil = System.Math.Max(_restUntil, Time.realtimeSinceStartupAsDouble + realSeconds);
        }

        private float EvaluateLuminance(double now, int frame)
        {
            if (!_codedActive || now < _restUntil) return restLuminance;
            double t = Mathf.Max(0, frame - _sessionStartFrame) / (double)Mathf.Max(1f, qualifiedRefreshHz);
            float sine01 = 0.5f + 0.5f * Mathf.Sin((float)(2.0 * System.Math.PI * frequencyHz * t));
            return Mathf.Lerp(minLuminance, maxLuminance, sine01);
        }

        private void LateUpdate()
        {
            float luminance = EvaluateLuminance(Time.realtimeSinceStartupAsDouble, Time.frameCount);
            if (targetRenderer != null)
            {
                targetRenderer.GetPropertyBlock(_block);
                _block.SetColor(EmissionColor, baseColor * Mathf.LinearToGammaSpace(luminance));
                targetRenderer.SetPropertyBlock(_block);
            }
            if (modulateTargetLight && targetLight != null)
            {
                targetLight.color = baseColor;
                targetLight.intensity = luminance;
            }
        }
    }
}
