using UnityEngine;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Mindforge.Qualification;
#endif

namespace Mindforge.Presentation
{
    /// <summary>
    /// Soft presentation-only budget governor.
    ///
    /// It never changes gameplay timing, render resolution, Time.timeScale, the 120 Hz
    /// simulation, or coded VEP stimulus output. In controller-only development it may
    /// reduce optional effect density when sustained render frame time is high. In live
    /// / release BCI use the default policy is a fixed Showcase tier to avoid introducing
    /// an adaptive visual confound during neural evidence collection.
    /// </summary>
    public sealed class PresentationQualityGovernor : MonoBehaviour
    {
        public enum QualityTier
        {
            Economy = 0,
            Balanced = 1,
            Showcase = 2,
        }

        [SerializeField] private float targetRenderHz = 120f;
        [SerializeField] private float frameTimeSmoothingSharpness = 2.4f;
        [SerializeField] private float decisionIntervalSeconds = 1.0f;
        [SerializeField] private bool adaptDuringControllerOnly = true;
        [SerializeField] private bool allowAdaptiveOutsideControllerOnly = false;

        private float _smoothedFrameMs;
        private float _nextDecisionAt;
        private QualityTier _tier = QualityTier.Showcase;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private ControllerOnlyQualificationBootstrap _controllerOnly;
        private float _nextControllerOnlyResolveAt;
#endif

        public static PresentationQualityGovernor Instance { get; private set; }
        public static float FxDensity => Instance != null ? Instance.CurrentFxDensity : 1f;
        public static int PreferredRingSegments => Instance != null ? Instance.CurrentRingSegments : 48;
        public static bool OptionalShellDetail => Instance == null || Instance._tier != QualityTier.Economy;

        public QualityTier Tier => _tier;
        public float SmoothedFrameMs => _smoothedFrameMs;
        public float CurrentFxDensity => _tier == QualityTier.Economy ? 0.55f : _tier == QualityTier.Balanced ? 0.78f : 1f;
        public int CurrentRingSegments => _tier == QualityTier.Economy ? 24 : _tier == QualityTier.Balanced ? 32 : 48;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (FindObjectOfType<PresentationQualityGovernor>(true) != null) return;
            new GameObject("MindforgePresentationQuality")
                .AddComponent<PresentationQualityGovernor>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            _smoothedFrameMs = 1000f / Mathf.Max(30f, targetRenderHz);
            _nextDecisionAt = Time.unscaledTime + Mathf.Max(0.25f, decisionIntervalSeconds);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            float sampleMs = Mathf.Clamp(Time.unscaledDeltaTime * 1000f, 0f, 100f);
            float blend = 1f - Mathf.Exp(-Mathf.Max(0.01f, frameTimeSmoothingSharpness) * Time.unscaledDeltaTime);
            _smoothedFrameMs = Mathf.Lerp(_smoothedFrameMs, sampleMs, blend);

            if (!ShouldAdapt())
            {
                _tier = QualityTier.Showcase;
                return;
            }

            if (Time.unscaledTime < _nextDecisionAt) return;
            _nextDecisionAt = Time.unscaledTime + Mathf.Max(0.25f, decisionIntervalSeconds);

            float budgetMs = 1000f / Mathf.Max(30f, targetRenderHz);
            if (_smoothedFrameMs > budgetMs * 1.65f)
                _tier = QualityTier.Economy;
            else if (_smoothedFrameMs > budgetMs * 1.28f)
                _tier = QualityTier.Balanced;
            else if (_smoothedFrameMs < budgetMs * 1.12f)
                _tier = QualityTier.Showcase;
        }

        private bool ShouldAdapt()
        {
            if (allowAdaptiveOutsideControllerOnly) return true;
            if (!adaptDuringControllerOnly) return false;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // Resolve this development-only service at a bounded cadence rather than
            // scanning the scene every rendered frame.
            if (_controllerOnly == null && Time.unscaledTime >= _nextControllerOnlyResolveAt)
            {
                _nextControllerOnlyResolveAt = Time.unscaledTime + 0.5f;
                _controllerOnly = FindObjectOfType<ControllerOnlyQualificationBootstrap>(true);
            }
            return _controllerOnly != null && _controllerOnly.Active;
#else
            return false;
#endif
        }
    }
}
