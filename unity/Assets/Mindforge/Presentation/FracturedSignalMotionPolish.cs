using UnityEngine;
using Mindforge.Combat;

namespace Mindforge.Presentation
{
    /// <summary>
    /// Secondary-motion layer for The Fractured Signal. It gives the procedural boss
    /// anticipation, recoil, phase eruption and inertial drift without changing the
    /// authoritative collider, scheduler or telegraph geometry.
    /// </summary>
    [DefaultExecutionOrder(460)]
    public sealed class FracturedSignalMotionPolish : MonoBehaviour
    {
        [SerializeField] private FracturedSignalDirector director;
        [SerializeField] private CombatantVitals vitals;

        private Transform _avatarRoot;
        private float _telegraph;
        private float _fire;
        private float _hit;
        private float _phasePulse;
        private float _heavy;
        private int _phase = 1;
        private Vector3 _baseLocalPosition;
        private bool _bound;

        private void Awake()
        {
            if (director == null) director = GetComponent<FracturedSignalDirector>();
            if (vitals == null) vitals = GetComponent<CombatantVitals>();
        }

        private void Start()
        {
            Bind();
            if (director != null)
            {
                director.PhaseChanged += OnPhase;
                director.AttackTelegraphed += OnTelegraph;
                director.AttackFired += OnFire;
                _phase = director.Phase;
            }
            if (vitals != null) vitals.Damaged += OnDamaged;
        }

        private void OnDestroy()
        {
            if (director != null)
            {
                director.PhaseChanged -= OnPhase;
                director.AttackTelegraphed -= OnTelegraph;
                director.AttackFired -= OnFire;
            }
            if (vitals != null) vitals.Damaged -= OnDamaged;
        }

        private void Bind()
        {
            if (_bound) return;
            _avatarRoot = transform.Find("FracturedSignalShowcaseAvatar");
            if (_avatarRoot == null) return;
            _baseLocalPosition = _avatarRoot.localPosition;
            _bound = true;
        }

        private void LateUpdate()
        {
            if (!_bound) Bind();
            if (!_bound || _avatarRoot == null) return;

            float dt = Mathf.Min(0.05f, Time.unscaledDeltaTime);
            float time = Time.unscaledTime;
            _telegraph = Damp(_telegraph, 0f, 3.8f, dt);
            _fire = Damp(_fire, 0f, 8.5f, dt);
            _hit = Damp(_hit, 0f, 7f, dt);
            _phasePulse = Damp(_phasePulse, 0f, 2.5f, dt);
            _heavy = Damp(_heavy, 0f, 4.0f, dt);

            float phase01 = Mathf.InverseLerp(1f, 3f, _phase);
            float hover = Mathf.Sin(time * Mathf.Lerp(1.25f, 1.9f, phase01)) * Mathf.Lerp(0.035f, 0.075f, phase01);
            float preCompression = _telegraph * (0.045f + _heavy * 0.035f);
            float fireExpansion = _fire * (0.08f + _heavy * 0.06f);
            float hitKick = _hit * 0.055f;
            float phaseExpansion = _phasePulse * 0.16f;

            float radial = 1f - preCompression + fireExpansion + phaseExpansion;
            float vertical = 1f + preCompression * 1.8f + fireExpansion * 0.45f + phaseExpansion * 0.65f;
            _avatarRoot.localScale = new Vector3(radial, vertical, radial);
            _avatarRoot.localPosition = _baseLocalPosition + new Vector3(
                Mathf.Sin(time * 0.73f) * 0.025f * (0.3f + phase01),
                hover + fireExpansion * 0.10f,
                -hitKick);
            _avatarRoot.localRotation = Quaternion.Euler(
                _hit * 5f,
                time * Mathf.Lerp(4f, 9f, phase01) + _fire * 8f,
                Mathf.Sin(time * 0.55f) * 1.8f + _hit * 3f);
        }

        private static float Damp(float value, float target, float sharpness, float dt)
            => Mathf.Lerp(value, target, 1f - Mathf.Exp(-Mathf.Max(0.01f, sharpness) * dt));

        private void OnPhase(int phase)
        {
            _phase = Mathf.Clamp(phase, 1, 3);
            _phasePulse = 1f;
        }

        private void OnTelegraph(string pattern, int count, bool heavy)
        {
            _telegraph = 1f;
            _heavy = heavy ? 1f : 0.35f;
        }

        private void OnFire(string pattern, int count, bool heavy)
        {
            _fire = 1f;
            _heavy = heavy ? 1f : Mathf.Max(_heavy, 0.35f);
        }

        private void OnDamaged(DamagePacket packet) => _hit = 1f;
    }
}
