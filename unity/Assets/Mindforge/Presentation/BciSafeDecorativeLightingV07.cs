using UnityEngine;
using Mindforge.Calibration;

namespace Mindforge.Presentation
{
    /// <summary>
    /// Keeps V0.7 decorative point lights visually useful in controller-only showcase runs
    /// while reducing their luminance contribution during calibrated/live BCI operation.
    ///
    /// This component only scales lights under the explicitly supplied decorative root. It
    /// never touches coded neural stimuli, gameplay timing, post-processing, or global lighting.
    /// The transition is monotonic and non-oscillatory so presentation cannot accidentally
    /// invent another temporal stimulus.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BciSafeDecorativeLightingV07 : MonoBehaviour
    {
        [SerializeField] private Transform decorativeLightRoot;
        [SerializeField] private AwakeningCalibrationDirector calibration;
        [SerializeField, Range(0f, 1f)] private float controllerOnlyScale = 1f;
        [SerializeField, Range(0f, 1f)] private float calibratedBciScale = 0.38f;
        [SerializeField, Min(0.1f)] private float responsePerSecond = 7f;

        private Light[] _lights;
        private float[] _authoredIntensities;
        private float _currentScale = 1f;
        private bool _cached;

        public float CurrentScale => _currentScale;
        public int ControlledLightCount => _lights != null ? _lights.Length : 0;

        public void ConfigureRuntime(
            Transform lightRoot,
            AwakeningCalibrationDirector calibrationDirector,
            float showcaseScale = 1f,
            float bciScale = 0.38f)
        {
            decorativeLightRoot = lightRoot;
            calibration = calibrationDirector;
            controllerOnlyScale = Mathf.Clamp01(showcaseScale);
            calibratedBciScale = Mathf.Clamp01(bciScale);
            CacheLights(force: true);
            ApplyImmediate(ResolveTargetScale());
        }

        private void Awake()
        {
            CacheLights(force: false);
        }

        private void Start()
        {
            if (calibration == null) calibration = FindObjectOfType<AwakeningCalibrationDirector>(true);
            CacheLights(force: false);
            ApplyImmediate(ResolveTargetScale());
        }

        private void Update()
        {
            if (!_cached) CacheLights(force: false);
            if (_lights == null || _lights.Length == 0) return;

            float target = ResolveTargetScale();
            float dt = Mathf.Min(0.05f, Time.unscaledDeltaTime);
            float response = 1f - Mathf.Exp(-Mathf.Max(0.1f, responsePerSecond) * dt);
            _currentScale = Mathf.Lerp(_currentScale, target, response);
            ApplyScale(_currentScale);
        }

        private float ResolveTargetScale()
        {
            // Missing calibration state uses the conservative BCI presentation rather than
            // assuming that the player is in a controller-only qualification run.
            bool controllerOnly = calibration != null && calibration.ControllerOnlyQualificationActive;
            return controllerOnly ? controllerOnlyScale : calibratedBciScale;
        }

        private void CacheLights(bool force)
        {
            if (_cached && !force) return;
            if (decorativeLightRoot == null)
            {
                _lights = new Light[0];
                _authoredIntensities = new float[0];
                _cached = true;
                return;
            }

            _lights = decorativeLightRoot.GetComponentsInChildren<Light>(true);
            _authoredIntensities = new float[_lights.Length];
            for (int i = 0; i < _lights.Length; i++)
                _authoredIntensities[i] = _lights[i] != null ? Mathf.Max(0f, _lights[i].intensity) : 0f;
            _cached = true;
        }

        private void ApplyImmediate(float scale)
        {
            _currentScale = Mathf.Clamp01(scale);
            ApplyScale(_currentScale);
        }

        private void ApplyScale(float scale)
        {
            if (_lights == null || _authoredIntensities == null) return;
            float safe = Mathf.Clamp01(scale);
            int count = Mathf.Min(_lights.Length, _authoredIntensities.Length);
            for (int i = 0; i < count; i++)
            {
                Light light = _lights[i];
                if (light == null) continue;
                light.intensity = _authoredIntensities[i] * safe;
            }
        }

        private void OnDisable()
        {
            // Disabling the safety presentation returns authored lights to their source values;
            // it never leaves the scene in a silently modified state.
            if (_lights == null || _authoredIntensities == null) return;
            int count = Mathf.Min(_lights.Length, _authoredIntensities.Length);
            for (int i = 0; i < count; i++)
                if (_lights[i] != null) _lights[i].intensity = _authoredIntensities[i];
        }
    }
}
