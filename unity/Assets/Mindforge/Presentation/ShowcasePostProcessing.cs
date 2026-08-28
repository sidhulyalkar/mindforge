using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Mindforge.Calibration;

namespace Mindforge.Presentation
{
    /// <summary>
    /// Runtime URP treatment for the cinematic showcase.
    ///
    /// Important BCI constraint: Temporal AA is enabled only for the explicitly
    /// controller-only visual showcase. Live/calibrated VEP operation uses high-quality
    /// SMAA so a temporal reconstruction filter cannot smear the 10/12 Hz luminance
    /// signal that is being physiologically measured.
    /// </summary>
    public sealed class ShowcasePostProcessing : MonoBehaviour
    {
        [SerializeField] private Camera gameplayCamera;
        [SerializeField] private CombatPresentationDirector presentation;
        [SerializeField] private AwakeningCalibrationDirector calibration;

        private Volume _volume;
        private VolumeProfile _profile;
        private Bloom _bloom;
        private Vignette _vignette;
        private ColorAdjustments _color;
        private WhiteBalance _whiteBalance;
        private FilmGrain _grain;
        private ChromaticAberration _chromatic;
        private bool _taaApplied;

        public void Configure(Camera camera, CombatPresentationDirector director)
        {
            gameplayCamera = camera;
            presentation = director;
        }

        private void Start()
        {
            if (gameplayCamera == null) gameplayCamera = Camera.main;
            if (presentation == null) presentation = FindObjectOfType<CombatPresentationDirector>(true);
            if (calibration == null) calibration = FindObjectOfType<AwakeningCalibrationDirector>(true);
            BuildProfile();
            ApplyCameraQuality(force: true);
        }

        private void BuildProfile()
        {
            if (_volume != null) return;

            GameObject go = new GameObject("MindforgeCinematicPostFX");
            go.transform.SetParent(transform, false);
            _volume = go.AddComponent<Volume>();
            _volume.isGlobal = true;
            _volume.priority = 80f;

            _profile = ScriptableObject.CreateInstance<VolumeProfile>();
            _profile.name = "MindforgeCinematicRuntimeProfile";
            _volume.sharedProfile = _profile;

            _bloom = _profile.Add<Bloom>(true);
            _bloom.intensity.Override(0.27f);
            _bloom.threshold.Override(1.04f);
            _bloom.scatter.Override(0.60f);
            _bloom.clamp.Override(14f);
            _bloom.highQualityFiltering.Override(true);

            _vignette = _profile.Add<Vignette>(true);
            _vignette.intensity.Override(0.060f);
            _vignette.smoothness.Override(0.58f);
            _vignette.rounded.Override(true);

            _color = _profile.Add<ColorAdjustments>(true);
            _color.postExposure.Override(0.38f);
            _color.contrast.Override(7f);
            _color.saturation.Override(2f);

            _whiteBalance = _profile.Add<WhiteBalance>(true);
            _whiteBalance.temperature.Override(-2f);
            _whiteBalance.tint.Override(2f);

            _grain = _profile.Add<FilmGrain>(true);
            _grain.intensity.Override(0.025f);
            _grain.response.Override(0.70f);

            _chromatic = _profile.Add<ChromaticAberration>(true);
            _chromatic.intensity.Override(0.006f);

            Tonemapping tone = _profile.Add<Tonemapping>(true);
            tone.mode.Override(TonemappingMode.ACES);

            if (gameplayCamera != null)
            {
                gameplayCamera.allowHDR = true;
                UniversalAdditionalCameraData data = gameplayCamera.GetUniversalAdditionalCameraData();
                data.renderPostProcessing = true;
                data.dithering = true;
                data.stopNaN = false;
            }
        }

        private void Update()
        {
            ApplyCameraQuality(force: false);
            if (_bloom == null || _vignette == null || _color == null || _grain == null || _chromatic == null) return;
            float dt = Mathf.Min(0.05f, Time.unscaledDeltaTime);
            bool rest = presentation != null && presentation.SensoryRestActive;

            // Signal Break is a sensory exhale. It subtracts visual pressure instead of
            // obscuring the scene with another full-screen effect. The baseline remains
            // deliberately readable enough for fast movement, telegraphs and gaze anchoring.
            float targetBloom = rest ? 0.12f : 0.27f;
            float targetVignette = rest ? 0.035f : 0.060f;
            float targetContrast = rest ? 3f : 7f;
            float targetExposure = rest ? 0.30f : 0.38f;
            float targetGrain = rest ? 0.012f : 0.025f;
            float targetChromatic = rest ? 0.002f : 0.006f;
            float response = 1f - Mathf.Exp(-4f * dt);
            _bloom.intensity.value = Mathf.Lerp(_bloom.intensity.value, targetBloom, response);
            _vignette.intensity.value = Mathf.Lerp(_vignette.intensity.value, targetVignette, response);
            _color.contrast.value = Mathf.Lerp(_color.contrast.value, targetContrast, response);
            _color.postExposure.value = Mathf.Lerp(_color.postExposure.value, targetExposure, response);
            _grain.intensity.value = Mathf.Lerp(_grain.intensity.value, targetGrain, response);
            _chromatic.intensity.value = Mathf.Lerp(_chromatic.intensity.value, targetChromatic, response);
        }

        private void ApplyCameraQuality(bool force)
        {
            if (gameplayCamera == null) return;
            if (calibration == null) calibration = FindObjectOfType<AwakeningCalibrationDirector>(true);

            bool controllerOnly = calibration != null && calibration.ControllerOnlyQualificationActive;
            if (!force && controllerOnly == _taaApplied) return;

            UniversalAdditionalCameraData data = gameplayCamera.GetUniversalAdditionalCameraData();
            if (controllerOnly)
            {
                data.antialiasing = AntialiasingMode.TemporalAntiAliasing;
                data.antialiasingQuality = AntialiasingQuality.High;
                _taaApplied = true;
            }
            else
            {
                data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                data.antialiasingQuality = AntialiasingQuality.High;
                _taaApplied = false;
            }
        }

        private void OnDestroy()
        {
            if (_profile != null) Destroy(_profile);
        }
    }
}
