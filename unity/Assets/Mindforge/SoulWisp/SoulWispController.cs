using UnityEngine;
using Mindforge.Combat;
using Mindforge.Presentation;

namespace Mindforge.SoulWisp
{
    /// <summary>
    /// Persistent soul companion. The fantasy Wisp itself drifts organically around the
    /// Guardian. Sight/Guard coded cores are a separate retinal interface that is hidden
    /// during ordinary combat and only materializes for an explicitly armed resonance window.
    ///
    /// During resonance, coded-core placement is camera-relative and angularly specified.
    /// This keeps the neurophysiology-facing geometry stable while leaving the fantasy shell
    /// free to move. Physical monitor geometry/timing must still be qualified separately.
    /// </summary>
    public sealed class SoulWispController : MonoBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] private Transform wispCore;
        [SerializeField] private Transform sightAura;
        [SerializeField] private Transform guardAura;
        [SerializeField] private VepAuraStimulus sightStimulus;
        [SerializeField] private VepAuraStimulus guardStimulus;
        [SerializeField] private CombatVisualPalette palette;
        [SerializeField] private GuardianTargetLock targetLock;

        [Header("Fantasy companion drift · presentation only")]
        [SerializeField] private float companionNearRadius = 0.85f;
        [SerializeField] private float companionFarRadius = 3.8f;
        [SerializeField] private float companionMinimumHeight = 1.05f;
        [SerializeField] private float companionMaximumHeight = 2.65f;
        [SerializeField] private float companionDriftFrequency = 0.20f;
        [SerializeField] private float companionDriftSharpness = 2.35f;
        [SerializeField] private float companionCatchupSharpness = 8.5f;
        [SerializeField] private float companionCatchupDistance = 5.4f;
        [SerializeField] private float companionTeleportDistance = 10.5f;
        [SerializeField] private float companionCombatBias = 0.30f;
        [SerializeField] private float companionWanderArcRadians = 2.15f;

        [Header("Hidden free-combat anchors · pre-position only")]
        [Range(0f, 1f)]
        [SerializeField] private float anchorTowardTarget = 0.78f;
        [SerializeField] private float anchorVerticalOffset = 0.45f;
        [SerializeField] private float freeHorizontalSeparation = 1.34f;
        [SerializeField] private float freeDepthTowardCamera = 0.10f;
        [SerializeField] private float freeAnchorSharpness = 7.5f;

        [Header("Hidden target-lock anchors · pre-position only")]
        [SerializeField] private float lockedTargetHeight = 1.45f;
        [SerializeField] private float lockedHorizontalSeparation = 1.18f;
        [SerializeField] private float lockedDepthTowardCamera = 0.12f;
        [SerializeField] private float lockedAnchorSharpness = 10f;
        [SerializeField] private float auraScale = 0.30f;

        [Header("Resonance coded-core retinal geometry")]
        [Tooltip("Camera-space distance only. Angular diameter/separation define the retinal geometry.")]
        [SerializeField] private float codedCoreDistance = 3.2f;
        [Range(1f, 8f)]
        [SerializeField] private float codedCoreAngularDiameterDeg = 3.0f;
        [Range(4f, 24f)]
        [SerializeField] private float codedCoreSeparationDeg = 10.0f;
        [Range(-12f, 12f)]
        [SerializeField] private float codedCoreVerticalAngleDeg = -1.5f;
        [SerializeField] private float resonanceAnchorSharpness = 22f;

        [Header("VEP")]
        [SerializeField] private float sightFrequencyHz = 10f;
        [SerializeField] private float guardFrequencyHz = 12f;
        [SerializeField] private Color sightColor = new Color(0.20f, 0.55f, 1f);
        [SerializeField] private Color guardColor = new Color(0.18f, 1f, 0.52f);

        private Transform _fallbackTarget;
        private bool _lockSubscribed;
        private bool _resonanceWindowActive;
        private float _driftSeedA;
        private float _driftSeedB;
        private float _driftSeedC;

        public bool InCombat => EffectiveTarget != null;
        public Transform CurrentTarget => EffectiveTarget;
        public bool ResonanceWindowActive => _resonanceWindowActive;
        public bool StableLockAnchorsActive =>
            targetLock != null && targetLock.Locked && targetLock.Target != null;
        public bool StimuliResting => (sightStimulus != null && sightStimulus.IsResting) ||
                                      (guardStimulus != null && guardStimulus.IsResting);

        private Transform EffectiveTarget
        {
            get
            {
                ResolveTargetLock();
                Transform locked = targetLock != null ? targetLock.Target : null;
                if (IsActiveTarget(locked)) return locked;
                return IsActiveTarget(_fallbackTarget) ? _fallbackTarget : null;
            }
        }

        private void Awake()
        {
            if (palette != null)
            {
                sightColor = palette.sightTarget;
                guardColor = palette.guardTarget;
            }
            sightStimulus?.Configure(sightFrequencyHz, sightColor);
            guardStimulus?.Configure(guardFrequencyHz, guardColor);
            sightStimulus?.EndWindow();
            guardStimulus?.EndWindow();
            InitializeDriftSeeds();
            ResolveTargetLock();
            SetTarget(null);
        }

        private void OnEnable()
        {
            ResolveTargetLock();
            SubscribeLock();
        }

        private void OnDisable()
        {
            EndResonanceWindow();
            UnsubscribeLock();
        }

        public void SetTarget(Transform target)
        {
            _fallbackTarget = target;
            ApplyCombatVisibility(EffectiveTarget != null);
        }

        public void RestStimuli(float realSeconds)
        {
            sightStimulus?.RestFor(realSeconds);
            guardStimulus?.RestFor(realSeconds);
            EndResonanceWindow();
        }

        /// <summary>
        /// Materializes the two neutral coded cores so they can settle into stable geometry.
        /// It does not start luminance modulation and grants no gameplay authority.
        /// </summary>
        public bool PrepareResonanceWindow()
        {
            if (StimuliResting || EffectiveTarget == null) return false;
            _resonanceWindowActive = true;
            sightStimulus?.EndWindow();
            guardStimulus?.EndWindow();
            ApplyCombatVisibility(true);
            return true;
        }

        /// <summary>Starts both coded stimuli from one shared local phase epoch.</summary>
        public bool BeginCodedResonance()
        {
            if (!_resonanceWindowActive || StimuliResting || EffectiveTarget == null) return false;
            double sharedStart = Time.realtimeSinceStartupAsDouble;
            sightStimulus?.BeginWindow(sharedStart);
            guardStimulus?.BeginWindow(sharedStart);
            return true;
        }

        public void EndResonanceWindow()
        {
            _resonanceWindowActive = false;
            sightStimulus?.EndWindow();
            guardStimulus?.EndWindow();
            ApplyCombatVisibility(EffectiveTarget != null);
        }

        private void Update()
        {
            if (player == null) return;
            ResolveTargetLock();
            SubscribeLock();

            Transform activeTarget = EffectiveTarget;
            UpdateCompanionDrift(activeTarget);

            if (activeTarget == null)
            {
                if (_resonanceWindowActive) EndResonanceWindow();
                ApplyCombatVisibility(false);
                return;
            }

            ApplyCombatVisibility(true);
            if (_resonanceWindowActive)
            {
                PlaceResonanceCores();
                return;
            }

            // Hidden cores remain pre-positioned near combat context so a future window can
            // ease cleanly, but they are not visible and VEP modulation is disabled.
            if (StableLockAnchorsActive && targetLock.Target == activeTarget)
            {
                PlaceStableLockedTargets(activeTarget);
                return;
            }

            PlaceFreeCombatTargets(activeTarget);
        }

        private void UpdateCompanionDrift(Transform activeTarget)
        {
            float t = Time.unscaledTime * Mathf.Max(0.01f, companionDriftFrequency);
            float radiusNoise = SmoothNoise(_driftSeedA, t * 0.73f);
            float angleNoise = SmoothNoise(_driftSeedB, t * 0.49f);
            float heightNoise = SmoothNoise(_driftSeedC, t * 0.61f);
            float curlNoise = SmoothNoise(_driftSeedA + 19.7f, t * 0.31f);

            float radius = Mathf.Lerp(
                Mathf.Max(0.15f, companionNearRadius),
                Mathf.Max(companionNearRadius + 0.1f, companionFarRadius),
                Mathf.SmoothStep(0f, 1f, radiusNoise));
            float wanderArc = Mathf.Clamp(companionWanderArcRadians, 0.5f, Mathf.PI);
            float angle = Mathf.Lerp(-wanderArc, wanderArc, angleNoise) +
                          Mathf.Sin(t * 1.37f + curlNoise * Mathf.PI) * 0.42f;

            Camera cam = Camera.main;
            Vector3 cameraForward = cam != null
                ? Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up)
                : Vector3.ProjectOnPlane(player.forward, Vector3.up);
            Vector3 cameraRight = cam != null
                ? Vector3.ProjectOnPlane(cam.transform.right, Vector3.up)
                : Vector3.ProjectOnPlane(player.right, Vector3.up);
            if (cameraForward.sqrMagnitude < 0.001f) cameraForward = Vector3.forward;
            if (cameraRight.sqrMagnitude < 0.001f) cameraRight = Vector3.right;
            cameraForward.Normalize();
            cameraRight.Normalize();

            Vector3 planar = (cameraRight * Mathf.Sin(angle) + cameraForward * Mathf.Cos(angle)).normalized * radius;
            if (activeTarget != null)
            {
                Vector3 towardTarget = Vector3.ProjectOnPlane(activeTarget.position - player.position, Vector3.up);
                if (towardTarget.sqrMagnitude > 0.001f)
                    planar += towardTarget.normalized * Mathf.Max(0f, companionCombatBias);
            }

            float height = Mathf.Lerp(
                Mathf.Max(0.25f, companionMinimumHeight),
                Mathf.Max(companionMinimumHeight + 0.1f, companionMaximumHeight),
                Mathf.SmoothStep(0f, 1f, heightNoise));
            height += Mathf.Sin(Time.unscaledTime * 1.43f + _driftSeedC) * 0.10f;

            Vector3 desired = player.position + planar + Vector3.up * height;
            float distanceFromPlayer = Vector3.Distance(transform.position, player.position);
            if (distanceFromPlayer > Mathf.Max(companionCatchupDistance + 1f, companionTeleportDistance))
            {
                transform.position = desired;
                return;
            }

            float sharpness = distanceFromPlayer > Mathf.Max(1f, companionCatchupDistance)
                ? companionCatchupSharpness
                : companionDriftSharpness;
            float response = 1f - Mathf.Exp(-Mathf.Max(0.05f, sharpness) * Time.unscaledDeltaTime);
            transform.position = Vector3.Lerp(transform.position, desired, response);
        }

        private void PlaceResonanceCores()
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            float distance = Mathf.Max(cam.nearClipPlane + 0.75f, codedCoreDistance);
            float halfSeparation = distance * Mathf.Tan(0.5f * codedCoreSeparationDeg * Mathf.Deg2Rad);
            float vertical = distance * Mathf.Tan(codedCoreVerticalAngleDeg * Mathf.Deg2Rad);
            float diameter = 2f * distance * Mathf.Tan(0.5f * codedCoreAngularDiameterDeg * Mathf.Deg2Rad);
            Vector3 center = cam.transform.position
                + cam.transform.forward * distance
                + cam.transform.up * vertical;
            float response = 1f - Mathf.Exp(-Mathf.Max(0.1f, resonanceAnchorSharpness) * Time.unscaledDeltaTime);

            PlaceStableAura(sightAura, center - cam.transform.right * halfSeparation, cam, response, diameter);
            PlaceStableAura(guardAura, center + cam.transform.right * halfSeparation, cam, response, diameter);
        }

        private void PlaceFreeCombatTargets(Transform activeTarget)
        {
            Camera cam = Camera.main;
            Vector3 right = cam != null
                ? Vector3.ProjectOnPlane(cam.transform.right, Vector3.up).normalized
                : Vector3.right;
            Vector3 towardCamera = cam != null
                ? Vector3.ProjectOnPlane(cam.transform.position - activeTarget.position, Vector3.up).normalized
                : -activeTarget.forward;
            if (right.sqrMagnitude < 0.001f) right = Vector3.right;
            if (towardCamera.sqrMagnitude < 0.001f) towardCamera = -activeTarget.forward;

            Vector3 anchor = Vector3.Lerp(player.position, activeTarget.position, anchorTowardTarget)
                + Vector3.up * anchorVerticalOffset
                + towardCamera * freeDepthTowardCamera;
            float response = 1f - Mathf.Exp(-Mathf.Max(0.1f, freeAnchorSharpness) * Time.unscaledDeltaTime);
            PlaceStableAura(sightAura, anchor - right * freeHorizontalSeparation, cam, response, auraScale);
            PlaceStableAura(guardAura, anchor + right * freeHorizontalSeparation, cam, response, auraScale);
        }

        private void PlaceStableLockedTargets(Transform activeTarget)
        {
            Camera cam = Camera.main;
            Vector3 right = cam != null
                ? Vector3.ProjectOnPlane(cam.transform.right, Vector3.up).normalized
                : Vector3.right;
            Vector3 towardCamera = cam != null
                ? Vector3.ProjectOnPlane(cam.transform.position - activeTarget.position, Vector3.up).normalized
                : -activeTarget.forward;
            if (right.sqrMagnitude < 0.001f) right = Vector3.right;
            if (towardCamera.sqrMagnitude < 0.001f) towardCamera = -activeTarget.forward;

            Vector3 anchor = activeTarget.position
                + Vector3.up * lockedTargetHeight
                + towardCamera * lockedDepthTowardCamera;
            float response = 1f - Mathf.Exp(-Mathf.Max(0.1f, lockedAnchorSharpness) * Time.unscaledDeltaTime);
            PlaceStableAura(sightAura, anchor - right * lockedHorizontalSeparation, cam, response, auraScale);
            PlaceStableAura(guardAura, anchor + right * lockedHorizontalSeparation, cam, response, auraScale);
        }

        private static void PlaceStableAura(Transform aura, Vector3 desired, Camera cam, float response, float scale)
        {
            if (aura == null) return;
            aura.position = Vector3.Lerp(aura.position, desired, response);
            aura.localScale = Vector3.one * Mathf.Max(0.01f, scale);
            if (cam != null)
                aura.rotation = Quaternion.LookRotation(aura.position - cam.transform.position, cam.transform.up);
        }

        private void ApplyCombatVisibility(bool combat)
        {
            if (wispCore != null) wispCore.gameObject.SetActive(true);
            bool showCodedCores = combat && _resonanceWindowActive;
            if (sightAura != null) sightAura.gameObject.SetActive(showCodedCores);
            if (guardAura != null) guardAura.gameObject.SetActive(showCodedCores);
        }

        private void ResolveTargetLock()
        {
            if (targetLock == null && player != null)
                targetLock = player.GetComponent<GuardianTargetLock>();
        }

        private void SubscribeLock()
        {
            if (_lockSubscribed || targetLock == null) return;
            targetLock.LockChanged += OnLockChanged;
            targetLock.TargetChanged += OnTargetChanged;
            _lockSubscribed = true;
        }

        private void UnsubscribeLock()
        {
            if (!_lockSubscribed || targetLock == null) return;
            targetLock.LockChanged -= OnLockChanged;
            targetLock.TargetChanged -= OnTargetChanged;
            _lockSubscribed = false;
        }

        private void OnLockChanged(bool locked) => ApplyCombatVisibility(EffectiveTarget != null);
        private void OnTargetChanged(Transform target) => ApplyCombatVisibility(EffectiveTarget != null);

        private void InitializeDriftSeeds()
        {
            unchecked
            {
                int hash = 17;
                string key = gameObject.name ?? "SoulWisp";
                for (int i = 0; i < key.Length; i++) hash = hash * 31 + key[i];
                uint value = (uint)hash;
                _driftSeedA = 11.3f + (value & 0xFFu) * 0.071f;
                _driftSeedB = 37.7f + ((value >> 8) & 0xFFu) * 0.083f;
                _driftSeedC = 71.9f + ((value >> 16) & 0xFFu) * 0.097f;
            }
        }

        private static float SmoothNoise(float seed, float time)
            => Mathf.PerlinNoise(seed, time);

        private static bool IsActiveTarget(Transform target)
        {
            if (target == null || !target.gameObject.activeInHierarchy) return false;
            CombatantVitals vitals = target.GetComponentInParent<CombatantVitals>();
            if (vitals == null) vitals = target.GetComponent<CombatantVitals>();
            return vitals == null || (vitals.Team == CombatTeam.Enemy && vitals.IsAlive);
        }
    }
}
