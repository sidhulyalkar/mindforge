using UnityEngine;
using Mindforge.Combat;
using Mindforge.Presentation;

namespace Mindforge.SoulWisp
{
    /// <summary>
    /// Persistent soul companion. The fantasy Wisp itself drifts organically around the
    /// Guardian, while coded Sight/Guard VEP targets remain separately positioned by the
    /// combat gaze contract. A fallback combat target may be supplied by the encounter,
    /// but conventional player target lock always takes priority.
    ///
    /// The companion drift is presentation-only and never changes coded target
    /// luminance/frequency, neural decisions or combat authority.
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

        [Header("Free combat gaze anchors")]
        [Range(0f, 1f)]
        [SerializeField] private float anchorTowardTarget = 0.78f;
        [SerializeField] private float anchorVerticalOffset = 0.45f;
        [SerializeField] private float freeHorizontalSeparation = 1.34f;
        [SerializeField] private float freeDepthTowardCamera = 0.10f;
        [SerializeField] private float freeAnchorSharpness = 7.5f;

        [Header("Third-person target lock gaze anchors")]
        [SerializeField] private float lockedTargetHeight = 1.45f;
        [SerializeField] private float lockedHorizontalSeparation = 1.18f;
        [SerializeField] private float lockedDepthTowardCamera = 0.12f;
        [SerializeField] private float lockedAnchorSharpness = 10f;
        [SerializeField] private float auraScale = 0.30f;

        [Header("VEP")]
        [SerializeField] private float sightFrequencyHz = 10f;
        [SerializeField] private float guardFrequencyHz = 12f;
        [SerializeField] private Color sightColor = new Color(0.20f, 0.55f, 1f);
        [SerializeField] private Color guardColor = new Color(0.18f, 1f, 0.52f);

        private Transform _fallbackTarget;
        private bool _lockSubscribed;
        private float _driftSeedA;
        private float _driftSeedB;
        private float _driftSeedC;

        public bool InCombat => EffectiveTarget != null;
        public Transform CurrentTarget => EffectiveTarget;
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
            InitializeDriftSeeds();
            ResolveTargetLock();
            SetTarget(null);
        }

        private void OnEnable()
        {
            ResolveTargetLock();
            SubscribeLock();
        }

        private void OnDisable() => UnsubscribeLock();

        public void SetTarget(Transform target)
        {
            _fallbackTarget = target;
            ApplyCombatVisibility(EffectiveTarget != null);
        }

        public void RestStimuli(float realSeconds)
        {
            sightStimulus?.RestFor(realSeconds);
            guardStimulus?.RestFor(realSeconds);
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
                ApplyCombatVisibility(false);
                return;
            }

            ApplyCombatVisibility(true);
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
            float angle = (angleNoise * 2f - 1f) * Mathf.PI +
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

            // Free combat no longer makes the coded stimuli orbit. They ease toward a
            // predictable left/right gaze corridor while the fantasy companion is free
            // to wander independently around the Guardian.
            PlaceStableAura(sightAura, anchor - right * freeHorizontalSeparation, cam, response);
            PlaceStableAura(guardAura, anchor + right * freeHorizontalSeparation, cam, response);
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

            // Sight stays screen-left and Guard screen-right while locked. The coded
            // VEP luminance remains owned by VepAuraStimulus; this changes position only.
            PlaceStableAura(sightAura, anchor - right * lockedHorizontalSeparation, cam, response);
            PlaceStableAura(guardAura, anchor + right * lockedHorizontalSeparation, cam, response);
        }

        private void PlaceStableAura(Transform aura, Vector3 desired, Camera cam, float response)
        {
            if (aura == null) return;
            aura.position = Vector3.Lerp(aura.position, desired, response);
            aura.localScale = Vector3.one * auraScale;
            if (cam != null)
                aura.rotation = Quaternion.LookRotation(aura.position - cam.transform.position, cam.transform.up);
        }

        private void ApplyCombatVisibility(bool combat)
        {
            // The fantasy companion remains present during combat. Sight/Guard are
            // separate coded gaze targets rather than replacing the Wisp itself.
            if (wispCore != null) wispCore.gameObject.SetActive(true);
            if (sightAura != null) sightAura.gameObject.SetActive(combat);
            if (guardAura != null) guardAura.gameObject.SetActive(combat);
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
