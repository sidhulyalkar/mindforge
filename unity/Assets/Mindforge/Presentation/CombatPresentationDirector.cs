using UnityEngine;

namespace Mindforge.Presentation
{
    /// <summary>
    /// Owns non-authoritative combat presentation only. Camera displacement, FOV,
    /// ambience dimming and sensory-rest audio never change combat or VEP timing.
    ///
    /// Put impactPivot on a dedicated child below the normal follow/lock-on rig so
    /// this component does not fight the camera's authoritative tracking script.
    /// </summary>
    public sealed class CombatPresentationDirector : MonoBehaviour
    {
        [Header("Camera")]
        [SerializeField] private Transform impactPivot;
        [SerializeField] private Camera gameplayCamera;
        [SerializeField] private float cameraSpring = 62f;
        [SerializeField] private float cameraDamping = 15f;
        [SerializeField] private float maxImpactOffset = 0.22f;
        [SerializeField] private float fovReturnSeconds = 0.11f;

        [Header("Environment-only dimming")]
        [SerializeField] private Light[] ambientLights;
        [Range(0f, 0.8f)] [SerializeField] private float maximumLightDim = 0.40f;
        [SerializeField] private float dimResponsePerSecond = 7f;

        [Header("Signal Break audio rest")]
        [SerializeField] private AudioLowPassFilter combatLowPass;
        [SerializeField] private AudioSource signalBreakPulse;
        [SerializeField] private float normalCutoffHz = 22000f;
        [SerializeField] private float signalBreakCutoffHz = 1450f;

        private Vector3 _basePivotLocalPosition;
        private Vector3 _kickOffset;
        private Vector3 _kickVelocity;
        private float _baseFov;
        private float _fovOffset;
        private float _fovVelocity;
        private float _transientDim;
        private float _currentDim;
        private double _sensoryRestUntil;
        private float[] _ambientLightBase;

        public float AmbientDim => _currentDim;
        public bool SensoryRestActive => Time.realtimeSinceStartupAsDouble < _sensoryRestUntil;

        private void Awake()
        {
            if (impactPivot != null) _basePivotLocalPosition = impactPivot.localPosition;
            if (gameplayCamera != null) _baseFov = gameplayCamera.fieldOfView;
            _ambientLightBase = new float[ambientLights != null ? ambientLights.Length : 0];
            for (int i = 0; i < _ambientLightBase.Length; i++)
                _ambientLightBase[i] = ambientLights[i] != null ? ambientLights[i].intensity : 0f;
            if (combatLowPass != null) combatLowPass.cutoffFrequency = normalCutoffHz;
            Shader.SetGlobalFloat("_MindforgeAmbientDim", 1f);
        }

        public void CleaveImpact(Vector3 worldDirection)
        {
            Kick(worldDirection, 0.14f, 1.0f, 0.10f);
        }

        public void CounterImpact(Vector3 worldDirection)
        {
            Kick(worldDirection, 0.075f, 0.55f, 0.06f);
        }

        public void BloomCharge(bool concord)
        {
            _fovOffset -= concord ? 3.2f : 2.0f;
            _transientDim = Mathf.Max(_transientDim, concord ? 0.25f : 0.16f);
        }

        public void BloomRelease(bool concord)
        {
            _fovOffset += concord ? 7.0f : 4.2f;
            _transientDim = Mathf.Max(_transientDim, concord ? 0.40f : 0.25f);
            _kickOffset += Vector3.up * (concord ? 0.08f : 0.04f);
        }

        public void SignalBreak(float realSeconds)
        {
            if (realSeconds <= 0f) return;
            _sensoryRestUntil = System.Math.Max(_sensoryRestUntil, Time.realtimeSinceStartupAsDouble + realSeconds);
            _transientDim = Mathf.Max(_transientDim, 0.20f);
            if (signalBreakPulse != null) signalBreakPulse.Play();
        }

        private void Kick(Vector3 worldDirection, float distance, float fovKick, float dim)
        {
            if (impactPivot != null && worldDirection.sqrMagnitude > 0.0001f)
            {
                Vector3 local = impactPivot.InverseTransformDirection(worldDirection.normalized);
                Vector2 plane = new Vector2(local.x, local.y);
                if (plane.sqrMagnitude < 0.001f) plane = Vector2.right;
                plane.Normalize();
                _kickOffset += new Vector3(plane.x, plane.y, 0f) * distance;
                _kickOffset = Vector3.ClampMagnitude(_kickOffset, maxImpactOffset);
            }
            _fovOffset += fovKick;
            _transientDim = Mathf.Max(_transientDim, dim);
        }

        private void LateUpdate()
        {
            float dt = Mathf.Min(0.05f, Time.unscaledDeltaTime);
            if (dt <= 0f) return;

            _kickVelocity += (-_kickOffset * cameraSpring - _kickVelocity * cameraDamping) * dt;
            _kickOffset += _kickVelocity * dt;
            if (impactPivot != null) impactPivot.localPosition = _basePivotLocalPosition + _kickOffset;

            _fovOffset = Mathf.SmoothDamp(_fovOffset, 0f, ref _fovVelocity,
                Mathf.Max(0.01f, fovReturnSeconds), Mathf.Infinity, dt);
            if (gameplayCamera != null) gameplayCamera.fieldOfView = Mathf.Max(10f, _baseFov + _fovOffset);

            _transientDim = Mathf.MoveTowards(_transientDim, 0f, dimResponsePerSecond * dt);
            float restDim = SensoryRestActive ? 0.22f : 0f;
            float desiredDim = Mathf.Max(restDim, _transientDim);
            _currentDim = Mathf.MoveTowards(_currentDim, desiredDim, dimResponsePerSecond * dt);
            ApplyAmbientDim(_currentDim);

            if (combatLowPass != null)
            {
                float target = SensoryRestActive ? signalBreakCutoffHz : normalCutoffHz;
                combatLowPass.cutoffFrequency = Mathf.Lerp(combatLowPass.cutoffFrequency, target, 1f - Mathf.Exp(-8f * dt));
            }
        }

        private void ApplyAmbientDim(float amount)
        {
            float lightScale = 1f - Mathf.Clamp01(amount) * maximumLightDim;
            for (int i = 0; i < _ambientLightBase.Length; i++)
                if (ambientLights[i] != null) ambientLights[i].intensity = _ambientLightBase[i] * lightScale;

            // Custom ambience/VFX shaders may opt into this. VEP core materials
            // should intentionally ignore it.
            Shader.SetGlobalFloat("_MindforgeAmbientDim", 1f - Mathf.Clamp01(amount));
        }

        private void OnDisable()
        {
            if (impactPivot != null) impactPivot.localPosition = _basePivotLocalPosition;
            if (gameplayCamera != null) gameplayCamera.fieldOfView = _baseFov;
            for (int i = 0; i < _ambientLightBase.Length; i++)
                if (ambientLights[i] != null) ambientLights[i].intensity = _ambientLightBase[i];
            if (combatLowPass != null) combatLowPass.cutoffFrequency = normalCutoffHz;
            Shader.SetGlobalFloat("_MindforgeAmbientDim", 1f);
        }
    }
}
