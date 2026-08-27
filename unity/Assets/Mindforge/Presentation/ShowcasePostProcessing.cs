using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Mindforge.Presentation
{
    /// <summary>
    /// Runtime URP profile for the showcase vertical slice. The treatment is restrained
    /// so reserved SSVEP target colors remain discriminable; the coded VEP core does
    /// not depend on post-processing for timing or luminance qualification.
    /// </summary>
    public sealed class ShowcasePostProcessing : MonoBehaviour
    {
        [SerializeField] private Camera gameplayCamera;
        [SerializeField] private CombatPresentationDirector presentation;

        private Volume _volume;
        private VolumeProfile _profile;
        private Bloom _bloom;
        private Vignette _vignette;
        private ColorAdjustments _color;

        public void Configure(Camera camera, CombatPresentationDirector director)
        {
            gameplayCamera = camera;
            presentation = director;
        }

        private void Start()
        {
            if (gameplayCamera == null) gameplayCamera = Camera.main;
            if (presentation == null) presentation = FindObjectOfType<CombatPresentationDirector>(true);
            BuildProfile();
        }

        private void BuildProfile()
        {
            if (_volume != null) return;

            GameObject go = new GameObject("MindforgeShowcasePostFX");
            go.transform.SetParent(transform, false);
            _volume = go.AddComponent<Volume>();
            _volume.isGlobal = true;
            _volume.priority = 80f;

            _profile = ScriptableObject.CreateInstance<VolumeProfile>();
            _profile.name = "MindforgeShowcaseRuntimeProfile";
            _volume.sharedProfile = _profile;

            _bloom = _profile.Add<Bloom>(true);
            _bloom.intensity.Override(0.42f);
            _bloom.threshold.Override(0.92f);
            _bloom.scatter.Override(0.68f);
            _bloom.clamp.Override(18f);

            _vignette = _profile.Add<Vignette>(true);
            _vignette.intensity.Override(0.16f);
            _vignette.smoothness.Override(0.58f);
            _vignette.rounded.Override(true);

            _color = _profile.Add<ColorAdjustments>(true);
            _color.postExposure.Override(0.08f);
            _color.contrast.Override(10f);
            _color.saturation.Override(-7f);

            Tonemapping tone = _profile.Add<Tonemapping>(true);
            tone.mode.Override(TonemappingMode.ACES);

            if (gameplayCamera != null)
            {
                UniversalAdditionalCameraData data = gameplayCamera.GetUniversalAdditionalCameraData();
                data.renderPostProcessing = true;
            }
        }

        private void Update()
        {
            if (_bloom == null || _vignette == null || _color == null) return;
            float dt = Mathf.Min(0.05f, Time.unscaledDeltaTime);
            bool rest = presentation != null && presentation.SensoryRestActive;

            // Signal Break is a sensory exhale: less bloom/contrast rather than a
            // screen-obscuring effect that would fight the intended visual rest.
            float targetBloom = rest ? 0.18f : 0.42f;
            float targetVignette = rest ? 0.08f : 0.16f;
            float targetContrast = rest ? 2f : 10f;
            _bloom.intensity.value = Mathf.Lerp(_bloom.intensity.value, targetBloom, 1f - Mathf.Exp(-4f * dt));
            _vignette.intensity.value = Mathf.Lerp(_vignette.intensity.value, targetVignette, 1f - Mathf.Exp(-4f * dt));
            _color.contrast.value = Mathf.Lerp(_color.contrast.value, targetContrast, 1f - Mathf.Exp(-4f * dt));
        }

        private void OnDestroy()
        {
            if (_profile != null) Destroy(_profile);
        }
    }
}
