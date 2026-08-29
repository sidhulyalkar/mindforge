using UnityEngine;
using Mindforge.Traversal;

namespace Mindforge.Presentation
{
    /// <summary>
    /// Read-only kinetic presentation for the Prism hoverbike. Banking, pitch, exhaust
    /// length and a small rider impulse are derived from authoritative mounted velocity
    /// and events. This component never writes Rigidbody state or gameplay transforms.
    /// </summary>
    [DefaultExecutionOrder(1400)]
    public sealed class HoverbikeKineticPresentationV2 : MonoBehaviour
    {
        [SerializeField] private GuardianHoverbikeController bike;
        [SerializeField] private float maximumBankDegrees = 14f;
        [SerializeField] private float maximumPitchDegrees = 5f;
        [SerializeField] private float response = 10f;
        [SerializeField] private float boostPulseSeconds = 0.28f;
        [SerializeField] private float attackPulseSeconds = 0.18f;

        private Transform _presentation;
        private Transform[] _exhausts;
        private Quaternion _baseRotation;
        private Vector3[] _baseExhaustScale;
        private float _boostPulse;
        private float _attackPulse;
        private Vector3 _previousVelocity;

        private void Awake() => Resolve();

        private void OnEnable()
        {
            Resolve();
            if (bike == null) return;
            bike.MountedChanged += OnMountedChanged;
            bike.BoostStarted += OnBoostStarted;
            bike.MountedAttackIssued += OnMountedAttack;
        }

        private void OnDisable()
        {
            if (bike != null)
            {
                bike.MountedChanged -= OnMountedChanged;
                bike.BoostStarted -= OnBoostStarted;
                bike.MountedAttackIssued -= OnMountedAttack;
            }
            RestorePresentation();
        }

        private void LateUpdate()
        {
            Resolve();
            float dt = Mathf.Min(0.05f, Time.unscaledDeltaTime);
            _boostPulse = Mathf.MoveTowards(_boostPulse, 0f, dt / Mathf.Max(0.02f, boostPulseSeconds));
            _attackPulse = Mathf.MoveTowards(_attackPulse, 0f, dt / Mathf.Max(0.02f, attackPulseSeconds));

            if (bike == null || !bike.Mounted || bike.ActiveBike == null)
            {
                _previousVelocity = Vector3.zero;
                RestorePresentation();
                return;
            }

            BindPresentation();
            if (_presentation == null) return;

            Vector3 velocity = bike.PlanarVelocity;
            Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
            if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
            forward.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

            Vector3 acceleration = dt > 0.0001f ? (velocity - _previousVelocity) / dt : Vector3.zero;
            _previousVelocity = velocity;
            float lateral = Mathf.Clamp(Vector3.Dot(acceleration, right) / 28f, -1f, 1f);
            float longitudinal = Mathf.Clamp(Vector3.Dot(acceleration, forward) / 34f, -1f, 1f);
            float bank = -lateral * Mathf.Max(0f, maximumBankDegrees);
            float pitch = -longitudinal * Mathf.Max(0f, maximumPitchDegrees) - _boostPulse * 1.8f;
            Quaternion target = _baseRotation * Quaternion.Euler(pitch, 0f, bank);
            float t = 1f - Mathf.Exp(-Mathf.Max(0.1f, response) * dt);
            _presentation.localRotation = Quaternion.Slerp(_presentation.localRotation, target, t);

            float speedEnergy = Mathf.Clamp01(bike.Speed01);
            float exhaustEnergy = 1f + speedEnergy * 0.65f + _boostPulse * 1.35f + _attackPulse * 0.18f;
            if (_exhausts != null && _baseExhaustScale != null)
            {
                for (int i = 0; i < _exhausts.Length && i < _baseExhaustScale.Length; i++)
                {
                    Transform exhaust = _exhausts[i];
                    if (exhaust == null) continue;
                    Vector3 scale = _baseExhaustScale[i];
                    scale.z *= exhaustEnergy;
                    exhaust.localScale = Vector3.Lerp(exhaust.localScale, scale, t);
                }
            }
        }

        private void Resolve()
        {
            if (bike == null) bike = GetComponent<GuardianHoverbikeController>();
        }

        private void BindPresentation()
        {
            Transform candidate = bike != null && bike.ActiveBike != null ? bike.ActiveBike.PresentationRoot : null;
            if (candidate == _presentation) return;

            RestorePresentation();
            _presentation = candidate;
            if (_presentation == null) return;
            _baseRotation = _presentation.localRotation;

            Transform[] all = _presentation.GetComponentsInChildren<Transform>(true);
            int count = 0;
            for (int i = 0; i < all.Length; i++)
                if (all[i] != null && all[i].name.IndexOf("Exhaust", System.StringComparison.OrdinalIgnoreCase) >= 0) count++;

            _exhausts = new Transform[count];
            _baseExhaustScale = new Vector3[count];
            int cursor = 0;
            for (int i = 0; i < all.Length; i++)
            {
                Transform item = all[i];
                if (item == null || item.name.IndexOf("Exhaust", System.StringComparison.OrdinalIgnoreCase) < 0) continue;
                _exhausts[cursor] = item;
                _baseExhaustScale[cursor] = item.localScale;
                cursor++;
            }
        }

        private void RestorePresentation()
        {
            if (_presentation != null)
            {
                _presentation.localRotation = _baseRotation;
                if (_exhausts != null && _baseExhaustScale != null)
                {
                    for (int i = 0; i < _exhausts.Length && i < _baseExhaustScale.Length; i++)
                        if (_exhausts[i] != null) _exhausts[i].localScale = _baseExhaustScale[i];
                }
            }
            _presentation = null;
            _exhausts = null;
            _baseExhaustScale = null;
        }

        private void OnMountedChanged(bool mounted)
        {
            if (!mounted) RestorePresentation();
        }

        private void OnBoostStarted() => _boostPulse = 1f;
        private void OnMountedAttack() => _attackPulse = 1f;
    }
}
