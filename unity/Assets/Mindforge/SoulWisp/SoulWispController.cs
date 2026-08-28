using UnityEngine;
using Mindforge.Combat;
using Mindforge.Presentation;

namespace Mindforge.SoulWisp
{
    /// <summary>
    /// Persistent soul companion. A fallback combat target may be supplied by the
    /// encounter, but conventional player target lock takes priority. While locked,
    /// Sight and Guard settle into stable left/right positions around that enemy.
    /// The coded VEP luminance remains owned only by VepAuraStimulus.
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

        [Header("Follow")]
        [SerializeField] private Vector3 idleOffset = new Vector3(0.8f, 1.35f, 0.25f);
        [SerializeField] private float followSharpness = 7f;

        [Header("Free combat gaze corridor")]
        [Range(0f, 1f)]
        [SerializeField] private float anchorTowardTarget = 0.78f;
        [SerializeField] private float anchorVerticalOffset = 0.45f;
        [SerializeField] private float orbitRadius = 1.35f;
        [SerializeField] private float orbitVerticalAmplitude = 0.32f;
        [SerializeField] private float orbitAngularSpeedRadians = 0.78f;

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
        private float _orbitPhase;
        private bool _lockSubscribed;

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
            if (activeTarget == null)
            {
                ApplyCombatVisibility(false);
                Vector3 bob = new Vector3(0f, Mathf.Sin(Time.unscaledTime * 1.9f) * 0.12f, 0f);
                Vector3 desired = player.TransformPoint(idleOffset) + bob;
                transform.position = Vector3.Lerp(
                    transform.position,
                    desired,
                    1f - Mathf.Exp(-followSharpness * Time.unscaledDeltaTime));
                return;
            }

            ApplyCombatVisibility(true);
            if (StableLockAnchorsActive && targetLock.Target == activeTarget)
            {
                PlaceStableLockedTargets(activeTarget);
                return;
            }

            Camera cam = Camera.main;
            Vector3 up = cam != null ? cam.transform.up : Vector3.up;
            Vector3 anchor = Vector3.Lerp(player.position, activeTarget.position, anchorTowardTarget) + up * anchorVerticalOffset;
            transform.position = anchor;
            _orbitPhase = Mathf.Repeat(
                _orbitPhase + orbitAngularSpeedRadians * Time.unscaledDeltaTime,
                Mathf.PI * 2f);
            PlaceOrbitingAura(sightAura, anchor, _orbitPhase);
            PlaceOrbitingAura(guardAura, anchor, _orbitPhase + Mathf.PI);
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
            if (towardCamera.sqrMagnitude < 0.001f) towardCamera = -activeTarget.forward;

            Vector3 anchor = activeTarget.position
                + Vector3.up * lockedTargetHeight
                + towardCamera * lockedDepthTowardCamera;
            float response = 1f - Mathf.Exp(-Mathf.Max(0.1f, lockedAnchorSharpness) * Time.unscaledDeltaTime);
            transform.position = Vector3.Lerp(transform.position, anchor, response);

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

        private void PlaceOrbitingAura(Transform aura, Vector3 anchor, float phase)
        {
            if (aura == null) return;
            Camera cam = Camera.main;
            Vector3 right = cam != null ? cam.transform.right : Vector3.right;
            Vector3 up = cam != null ? cam.transform.up : Vector3.up;
            Vector3 offset = right * Mathf.Cos(phase) * orbitRadius + up * Mathf.Sin(phase) * orbitVerticalAmplitude;
            aura.position = anchor + offset;
            aura.localScale = Vector3.one * auraScale;
            if (cam != null) aura.rotation = Quaternion.LookRotation(aura.position - cam.transform.position, cam.transform.up);
        }

        private void ApplyCombatVisibility(bool combat)
        {
            if (wispCore != null) wispCore.gameObject.SetActive(!combat);
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

        private static bool IsActiveTarget(Transform target)
        {
            if (target == null || !target.gameObject.activeInHierarchy) return false;
            CombatantVitals vitals = target.GetComponentInParent<CombatantVitals>();
            if (vitals == null) vitals = target.GetComponent<CombatantVitals>();
            return vitals == null || (vitals.Team == CombatTeam.Enemy && vitals.IsAlive);
        }
    }
}
