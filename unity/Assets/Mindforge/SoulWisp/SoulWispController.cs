using UnityEngine;
using Mindforge.Combat;
using Mindforge.Presentation;

namespace Mindforge.SoulWisp
{
    /// <summary>
    /// Persistent soul companion. In ordinary combat the visual targets occupy a
    /// camera-facing gaze corridor between Guardian and threat. During conventional
    /// third-person target lock they settle into stable left/right positions around the
    /// locked enemy, giving the later Sight/Guard SSVEP interaction a natural spatial
    /// home without changing combat authority.
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

        private Transform _target;
        private float _orbitPhase;

        public bool InCombat => _target != null;
        public Transform CurrentTarget => _target;
        public bool StableLockAnchorsActive =>
            targetLock != null && targetLock.Locked && targetLock.Target != null && targetLock.Target == _target;
        public bool StimuliResting => (sightStimulus != null && sightStimulus.IsResting) ||
                                      (guardStimulus != null && guardStimulus.IsResting);

        private void Awake()
        {
            if (palette != null)
            {
                sightColor = palette.sightTarget;
                guardColor = palette.guardTarget;
            }
            sightStimulus?.Configure(sightFrequencyHz, sightColor);
            guardStimulus?.Configure(guardFrequencyHz, guardColor);
            SetTarget(null);
        }

        public void SetTarget(Transform target)
        {
            _target = target;
            bool combat = target != null;
            if (wispCore != null) wispCore.gameObject.SetActive(!combat);
            if (sightAura != null) sightAura.gameObject.SetActive(combat);
            if (guardAura != null) guardAura.gameObject.SetActive(combat);
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

            if (_target == null)
            {
                Vector3 bob = new Vector3(0f, Mathf.Sin(Time.unscaledTime * 1.9f) * 0.12f, 0f);
                Vector3 desired = player.TransformPoint(idleOffset) + bob;
                transform.position = Vector3.Lerp(
                    transform.position,
                    desired,
                    1f - Mathf.Exp(-followSharpness * Time.unscaledDeltaTime));
                return;
            }

            if (StableLockAnchorsActive)
            {
                PlaceStableLockedTargets();
                return;
            }

            Camera cam = Camera.main;
            Vector3 up = cam != null ? cam.transform.up : Vector3.up;
            Vector3 anchor = Vector3.Lerp(player.position, _target.position, anchorTowardTarget) + up * anchorVerticalOffset;
            transform.position = anchor;
            _orbitPhase = Mathf.Repeat(
                _orbitPhase + orbitAngularSpeedRadians * Time.unscaledDeltaTime,
                Mathf.PI * 2f);
            PlaceOrbitingAura(sightAura, anchor, _orbitPhase);
            PlaceOrbitingAura(guardAura, anchor, _orbitPhase + Mathf.PI);
        }

        private void PlaceStableLockedTargets()
        {
            Camera cam = Camera.main;
            Vector3 right = cam != null ? Vector3.ProjectOnPlane(cam.transform.right, Vector3.up).normalized : Vector3.right;
            Vector3 up = Vector3.up;
            Vector3 towardCamera = cam != null
                ? Vector3.ProjectOnPlane(cam.transform.position - _target.position, Vector3.up).normalized
                : -_target.forward;
            if (towardCamera.sqrMagnitude < 0.001f) towardCamera = -_target.forward;

            Vector3 anchor = _target.position
                + up * lockedTargetHeight
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

        private void ResolveTargetLock()
        {
            if (targetLock == null && player != null)
                targetLock = player.GetComponent<GuardianTargetLock>();
        }
    }
}
