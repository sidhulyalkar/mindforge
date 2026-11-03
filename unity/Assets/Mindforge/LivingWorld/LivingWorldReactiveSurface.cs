using UnityEngine;
using Mindforge.Calibration;
using Mindforge.SoulWisp;
using UnityEngine.SceneManagement;

namespace Mindforge.LivingWorld
{
    /// <summary>
    /// A deliberately opt-in decorative mesh only. Never attach to gameplay actors,
    /// colliders, active VEP cores, or other systems' animated renderers.
    /// The material property block is restored when disabled.
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public sealed class LivingWorldReactiveSurface : MonoBehaviour
    {
        [SerializeField] private LivingWorldPassiveReceiver receiver;
        [SerializeField] private LivingWorldExperienceV31 experience;
        [SerializeField] private Color lowAlphaTint = new Color(0.11f, 0.23f, 0.33f, 1f);
        [SerializeField] private Color highAlphaTint = new Color(0.28f, 0.45f, 0.48f, 1f);

        private Renderer _renderer;
        private MaterialPropertyBlock _original;
        private MaterialPropertyBlock _current;
        private SoulWispController _wisp;
        private AwakeningCalibrationDirector _calibration;
        private bool _originalEnabled;
        private float _nextResolveTime;
        private int _colorProperty;
        private bool _ready;

        public void Configure(LivingWorldPassiveReceiver source, LivingWorldExperienceV31 visualOwner = null)
        {
            receiver = source;
            experience = visualOwner;
        }

        private void OnEnable()
        {
            _renderer = GetComponent<Renderer>();
            if (_renderer == null || GetComponent<Collider>() != null ||
                GetComponent<Collider2D>() != null ||
                GetComponentInParent<VepAuraStimulus>() != null ||
                name.Contains("VepCore") || name.Contains("VEP"))
            {
                Debug.LogWarning("[LivingWorld] Reactive materials require a dedicated, non-colliding, non-stimulus mesh: " + name);
                enabled = false;
                return;
            }
            if (_renderer.sharedMaterial == null) { enabled = false; return; }
            if (_renderer.sharedMaterial.HasProperty("_BaseColor"))
                _colorProperty = Shader.PropertyToID("_BaseColor");
            else if (_renderer.sharedMaterial.HasProperty("_Color"))
                _colorProperty = Shader.PropertyToID("_Color");
            else { enabled = false; return; }

            _originalEnabled = _renderer.enabled;
            _original = new MaterialPropertyBlock();
            _current = new MaterialPropertyBlock();
            _renderer.GetPropertyBlock(_original);
            _ready = true;
        }

        private bool NeuralEvidenceQuiet()
        {
            if (Time.unscaledTime >= _nextResolveTime && (_wisp == null || _calibration == null))
            {
                _nextResolveTime = Time.unscaledTime + 1f;
                if (_wisp == null) _wisp = FindObjectOfType<SoulWispController>(true);
                if (_calibration == null) _calibration = FindObjectOfType<AwakeningCalibrationDirector>(true);
            }
            // In the competition scene, hide until both owners have been found:
            // an asynchronously assembled VEP source must not receive ambient effects.
            if (SceneManager.GetActiveScene().name == "Mindforge_Competition" &&
                (_wisp == null || _calibration == null)) return true;
            if (_calibration != null && _calibration.CalibrationInProgress) return true;
            if (_wisp != null && (_wisp.CalibrationStimuliActive || _wisp.ResonanceWindowActive)) return true;
            return false;
        }

        private void LateUpdate()
        {
            if (!_ready || _renderer == null) return;
            // Keep decorative luminance fully absent during calibration and coded VEP windows.
            if (NeuralEvidenceQuiet())
            {
                _renderer.enabled = false;
                return;
            }
            _renderer.enabled = _originalEnabled;
            if (!_originalEnabled) return;

            if (receiver == null) receiver = FindObjectOfType<LivingWorldPassiveReceiver>();
            float value = receiver != null ? receiver.CurrentValue : PassiveFeaturePolicy.NeutralValue;
            // Existing isolated v3 demo retains its original full-range palette.
            // The in-game v3.1 path is deliberately constrained by user opt-in.
            if (experience != null)
                value = LivingWorldExperienceV31.MapFeature(value, experience.EffectiveIntensity);
            Color tint = Color.Lerp(lowAlphaTint, highAlphaTint, Mathf.Clamp01(value));
            _renderer.GetPropertyBlock(_current);
            _current.SetColor(_colorProperty, tint);
            _renderer.SetPropertyBlock(_current);
        }

        private void OnDisable()
        {
            if (_ready && _renderer != null)
            {
                _renderer.SetPropertyBlock(_original);
                _renderer.enabled = _originalEnabled;
            }
            _ready = false;
        }
    }
}
