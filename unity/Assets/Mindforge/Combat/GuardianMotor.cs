using UnityEngine;

namespace Mindforge.Combat
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class GuardianMotor : MonoBehaviour
    {
        [SerializeField] private CombatTuning tuning;
        [SerializeField] private Transform cameraReference;

        private Rigidbody _body;
        private Vector2 _moveInput;
        private float _dashUntil;
        private float _lastDash = -999f;

        public bool IsDashing => Time.time < _dashUntil;
        public bool IsInvulnerable => IsDashing;
        public Vector3 Velocity => _body != null ? _body.velocity : Vector3.zero;

        private void Awake()
        {
            _body = GetComponent<Rigidbody>();
            _body.interpolation = RigidbodyInterpolation.Interpolate;
            _body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }

        public void SetMoveInput(Vector2 input) => _moveInput = Vector2.ClampMagnitude(input, 1f);

        public bool RequestDash(Vector3 fallbackDirection)
        {
            if (tuning == null || Time.time - _lastDash < tuning.dashCooldown) return false;
            Vector3 direction = MoveDirectionWorld();
            if (direction.sqrMagnitude < 0.01f) direction = fallbackDirection.normalized;
            if (direction.sqrMagnitude < 0.01f) direction = transform.forward;
            _lastDash = Time.time;
            _dashUntil = Time.time + tuning.dashDuration;
            _body.velocity = direction.normalized * tuning.dashSpeed;
            return true;
        }

        private Vector3 MoveDirectionWorld()
        {
            Transform reference = cameraReference != null ? cameraReference : Camera.main != null ? Camera.main.transform : null;
            Vector3 forward = reference != null ? Vector3.ProjectOnPlane(reference.forward, Vector3.up).normalized : Vector3.forward;
            Vector3 right = reference != null ? Vector3.ProjectOnPlane(reference.right, Vector3.up).normalized : Vector3.right;
            return right * _moveInput.x + forward * _moveInput.y;
        }

        private void FixedUpdate()
        {
            if (tuning == null || IsDashing) return;
            Vector3 desiredDir = MoveDirectionWorld();
            Vector3 horizontal = Vector3.ProjectOnPlane(_body.velocity, Vector3.up);
            Vector3 accel = desiredDir * tuning.acceleration - horizontal * tuning.drag;
            _body.AddForce(accel, ForceMode.Acceleration);
            horizontal = Vector3.ProjectOnPlane(_body.velocity, Vector3.up);
            if (horizontal.magnitude > tuning.maxSpeed)
            {
                Vector3 clamped = horizontal.normalized * tuning.maxSpeed;
                _body.velocity = clamped + Vector3.up * _body.velocity.y;
            }
        }
    }
}
