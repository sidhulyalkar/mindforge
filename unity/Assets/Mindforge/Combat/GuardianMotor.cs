using System;
using UnityEngine;

namespace Mindforge.Combat
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class GuardianMotor : MonoBehaviour
    {
        [SerializeField] private CombatTuning tuning;
        [SerializeField] private Transform cameraReference;
        [SerializeField] private GuardianEquipmentLoadout loadout;
        [SerializeField] private GuardianSwordShieldController physicalCombat;
        [SerializeField] private GuardianTargetLock targetLock;

        [Header("Responsive locomotion")]
        [SerializeField] private float minimumAcceleration = 58f;
        [SerializeField] private float deceleration = 76f;
        [SerializeField] private float reversalAcceleration = 92f;
        [SerializeField] private float freeTurnSharpness = 18f;
        [SerializeField] private float lockedTurnSharpness = 28f;
        [SerializeField] private float dodgeInvulnerabilitySeconds = 0.105f;
        [SerializeField] private float dashInputBufferSeconds = 0.13f;
        [SerializeField] private float dashExitVelocityRetention = 0.22f;

        private Rigidbody _body;
        private Vector2 _moveInput;
        private float _dashUntil;
        private float _invulnerableUntil;
        private bool _dashQueued;
        private float _dashQueuedUntil;
        private Vector3 _queuedDashFallback;
        private bool _wasDashing;
        private Vector3 _dashDirection;

        public event Action DashStarted;

        public bool IsDashing => Time.time < _dashUntil;
        public bool IsInvulnerable => Time.time < _invulnerableUntil;
        public Vector3 Velocity => _body != null ? _body.velocity : Vector3.zero;
        public Vector2 MoveInput => _moveInput;

        private void Awake()
        {
            _body = GetComponent<Rigidbody>();
            _body.interpolation = RigidbodyInterpolation.Interpolate;
            _body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            _body.sleepThreshold = 0f;
            ResolvePhysicalState();
        }

        private void ResolvePhysicalState()
        {
            if (loadout == null) loadout = GetComponent<GuardianEquipmentLoadout>();
            if (physicalCombat == null) physicalCombat = GetComponent<GuardianSwordShieldController>();
            if (targetLock == null) targetLock = GetComponent<GuardianTargetLock>();
            if (cameraReference == null && Camera.main != null) cameraReference = Camera.main.transform;
        }

        public void SetMoveInput(Vector2 input)
        {
            _moveInput = Vector2.ClampMagnitude(input, 1f);
            if (_moveInput.sqrMagnitude > 0.0001f && _body != null) _body.WakeUp();
        }

        public bool RequestDash(Vector3 fallbackDirection)
        {
            ResolvePhysicalState();
            if (tuning == null) return false;
            if (physicalCombat != null && !physicalCombat.CanDodge) return false;

            Vector3 direction = ResolveDashDirection(fallbackDirection);
            if (IsDashing)
            {
                // Unlimited dashes means no stamina/cooldown economy. A press near the
                // end of the current dash is buffered so chaining feels intentional.
                _dashQueued = true;
                _dashQueuedUntil = Time.time + Mathf.Max(0.02f, dashInputBufferSeconds);
                _queuedDashFallback = direction;
                return true;
            }

            StartDash(direction);
            return true;
        }

        private Vector3 ResolveDashDirection(Vector3 fallbackDirection)
        {
            Vector3 direction = MoveDirectionWorld();
            if (direction.sqrMagnitude < 0.01f) direction = fallbackDirection;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.01f) direction = transform.forward;
            if (direction.sqrMagnitude < 0.01f) direction = Vector3.forward;
            return direction.normalized;
        }

        private void StartDash(Vector3 direction)
        {
            float speedMultiplier = loadout != null ? loadout.RollSpeedMultiplier : 1f;
            float durationMultiplier = loadout != null ? loadout.RollDurationMultiplier : 1f;
            float rollDuration = Mathf.Max(0.06f, tuning.dashDuration * durationMultiplier);
            _dashUntil = Time.time + rollDuration;
            _invulnerableUntil = Time.time + Mathf.Min(rollDuration, Mathf.Max(0f, dodgeInvulnerabilitySeconds));
            _dashDirection = direction.normalized;
            Vector3 horizontal = _dashDirection * tuning.dashSpeed * speedMultiplier;
            _body.velocity = horizontal + Vector3.up * _body.velocity.y;
            FaceDirectionImmediate(_dashDirection);
            _dashQueued = false;
            _body.WakeUp();
            DashStarted?.Invoke();
        }

        private Vector3 MoveDirectionWorld()
        {
            Transform reference = cameraReference != null ? cameraReference : Camera.main != null ? Camera.main.transform : null;
            Vector3 forward = reference != null ? Vector3.ProjectOnPlane(reference.forward, Vector3.up) : Vector3.forward;
            Vector3 right = reference != null ? Vector3.ProjectOnPlane(reference.right, Vector3.up) : Vector3.right;
            if (forward.sqrMagnitude < 0.0001f) forward = Vector3.forward;
            if (right.sqrMagnitude < 0.0001f) right = Vector3.right;
            forward.Normalize();
            right.Normalize();
            return Vector3.ClampMagnitude(right * _moveInput.x + forward * _moveInput.y, 1f);
        }

        private void FixedUpdate()
        {
            ResolvePhysicalState();
            if (tuning == null || _body == null) return;

            bool dashing = IsDashing;
            if (_wasDashing && !dashing)
            {
                Vector3 horizontal = Vector3.ProjectOnPlane(_body.velocity, Vector3.up) * Mathf.Clamp01(dashExitVelocityRetention);
                _body.velocity = horizontal + Vector3.up * _body.velocity.y;
            }
            _wasDashing = dashing;

            if (dashing) return;

            if (_dashQueued)
            {
                if (Time.time <= _dashQueuedUntil && (physicalCombat == null || physicalCombat.CanDodge))
                {
                    StartDash(ResolveDashDirection(_queuedDashFallback));
                    _wasDashing = true;
                    return;
                }
                _dashQueued = false;
            }

            float loadMultiplier = loadout != null ? loadout.MoveSpeedMultiplier : 1f;
            float stanceMultiplier = physicalCombat != null ? Mathf.Clamp(physicalCombat.MovementMultiplier, 0.2f, 1f) : 1f;
            float maxSpeed = tuning.maxSpeed * loadMultiplier * stanceMultiplier;
            Vector3 desiredDir = MoveDirectionWorld();
            Vector3 targetVelocity = desiredDir * maxSpeed;
            Vector3 horizontalVelocity = Vector3.ProjectOnPlane(_body.velocity, Vector3.up);

            float response;
            if (desiredDir.sqrMagnitude < 0.0001f)
            {
                response = Mathf.Max(1f, deceleration);
            }
            else
            {
                float alignment = horizontalVelocity.sqrMagnitude > 0.01f
                    ? Vector3.Dot(horizontalVelocity.normalized, desiredDir.normalized)
                    : 1f;
                response = alignment < 0.1f
                    ? Mathf.Max(minimumAcceleration, reversalAcceleration)
                    : Mathf.Max(minimumAcceleration, tuning.acceleration);
            }

            Vector3 nextHorizontal = Vector3.MoveTowards(
                horizontalVelocity,
                targetVelocity,
                response * Time.fixedDeltaTime);
            _body.velocity = nextHorizontal + Vector3.up * _body.velocity.y;

            UpdateFacing(desiredDir);
        }

        private void UpdateFacing(Vector3 moveDirection)
        {
            Vector3 facing = Vector3.zero;
            float sharpness = freeTurnSharpness;

            if (targetLock != null && targetLock.Locked)
            {
                facing = targetLock.DirectionFrom(transform.position);
                sharpness = lockedTurnSharpness;
            }
            else if (moveDirection.sqrMagnitude > 0.001f)
            {
                facing = moveDirection;
            }

            facing.y = 0f;
            if (facing.sqrMagnitude < 0.001f) return;

            Quaternion desired = Quaternion.LookRotation(facing.normalized, Vector3.up);
            float t = 1f - Mathf.Exp(-Mathf.Max(0.1f, sharpness) * Time.fixedDeltaTime);
            _body.MoveRotation(Quaternion.Slerp(_body.rotation, desired, t));
        }

        private void FaceDirectionImmediate(Vector3 direction)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f) return;
            _body.MoveRotation(Quaternion.LookRotation(direction.normalized, Vector3.up));
        }
    }
}
