using UnityEngine;
using Mindforge.Presentation;

namespace Mindforge.SoulWisp
{
    /// <summary>
    /// Persistent soul companion. During combat the visual targets occupy a
    /// camera-facing gaze corridor between Guardian and threat instead of orbiting
    /// an arbitrary HUD corner. This reduces eye travel while preserving the
    /// fantasy that the Wisp is binding the enemy.
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

        [Header("Follow")]
        [SerializeField] private Vector3 idleOffset = new Vector3(0.8f, 1.35f, 0.25f);
        [SerializeField] private float followSharpness = 7f;

        [Header("Combat gaze corridor")]
        [Range(0f, 1f)]
        [SerializeField] private float anchorTowardTarget = 0.78f;
        [SerializeField] private float anchorVerticalOffset = 0.45f;
        [SerializeField] private float orbitRadius = 1.35f;
        [SerializeField] private float orbitVerticalAmplitude = 0.32f;
        [SerializeField] private float orbitAngularSpeedRadians = 0.78f;
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
            if (_target == null)
            {
                Vector3 bob = new Vector3(0f, Mathf.Sin(Time.unscaledTime * 1.9f) * 0.12f, 0f);
                Vector3 desired = player.TransformPoint(idleOffset) + bob;
                transform.position = Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-followSharpness * Time.unscaledDeltaTime));
                return;
            }

            Camera cam = Camera.main;
            Vector3 up = cam != null ? cam.transform.up : Vector3.up;
            Vector3 anchor = Vector3.Lerp(player.position, _target.position, anchorTowardTarget) + up * anchorVerticalOffset;
            transform.position = anchor;
            _orbitPhase = Mathf.Repeat(_orbitPhase + orbitAngularSpeedRadians * Time.unscaledDeltaTime, Mathf.PI * 2f);
            PlaceAura(sightAura, anchor, _orbitPhase);
            PlaceAura(guardAura, anchor, _orbitPhase + Mathf.PI);
        }

        private void PlaceAura(Transform aura, Vector3 anchor, float phase)
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
    }
}
