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
        [SerializeField] private float minimumAcceleration = 82f;
        [SerializeField] private float deceleration = 94f;
        [SerializeField] private float reversalAcceleration = 122f;
        [SerializeField] private float forwardSpeedMultiplier = 1.55f;
        [SerializeField] private float strafeSpeedMultiplier = 1.22f;
        [SerializeField] private float backwardSpeedMultiplier = 1.05f;
        [SerializeField] private float freeTurnSharpness = 20f;
        [SerializeField] private float lockedTurnSharpness = 30f;
        [SerializeField] private float airTurnSharpness = 12f;

        [Header("Grounding")]
        [SerializeField] private Collider movementCollider;
        [SerializeField] private LayerMask groundMask = ~0;
        [SerializeField] private float groundProbeLift = 0.10f;
        [SerializeField] private float groundProbeDistance = 0.22f;
        [SerializeField] private float groundProbeRadiusScale = 0.90f;
        [SerializeField] private float maxGroundSlopeDegrees = 52f;
        [SerializeField] private float maximumGroundedRiseSpeed = 1.25f;
        [SerializeField] private float groundStickSpeed = 2.2f;
        [SerializeField] private bool installLowFrictionMovementMaterial = true;

        [Header("Jump and air feel")]
        [SerializeField] private float jumpVelocity = 7.2f;
        [SerializeField] private float coyoteTimeSeconds = 0.11f;
        [SerializeField] private float jumpBufferSeconds = 0.13f;
        [SerializeField, Range(0.2f, 1f)] private float jumpReleaseVelocityMultiplier = 0.52f;
        [SerializeField] private float risingGravityMultiplier = 1.18f;
        [SerializeField] private float apexGravityMultiplier = 0.88f;
        [SerializeField] private float apexVerticalSpeed = 1.25f;
        [SerializeField] private float releasedJumpGravityMultiplier = 2.45f;
        [SerializeField] private float fallingGravityMultiplier = 2.30f;
        [SerializeField] private float terminalFallSpeed = 28f;
        [SerializeField] private float airAcceleration = 34f;
        [SerializeField, Range(0f, 1.5f)] private float airSpeedMultiplier = 0.92f;

        [Header("Dodge")]
        [SerializeField] private float dodgeInvulnerabilitySeconds = 0.105f;
        [SerializeField] private float dashInputBufferSeconds = 0.13f;
        [SerializeField] private float dashExitVelocityRetention = 0.48f;

        private readonly RaycastHit[] _groundHits = new RaycastHit[10];
        private Rigidbody _body;
        private Vector2 _moveInput;
        private long _dashUntilTick = long.MinValue / 4;
        private long _invulnerableUntilTick = long.MinValue / 4;
        private bool _dashQueued;
        private long _dashQueuedUntilTick = long.MinValue / 4;
        private Vector3 _queuedDashFallback;
        private bool _wasDashing;
        private Vector3 _dashDirection;

        private bool _jumpQueued;
        private long _jumpQueuedUntilTick = long.MinValue / 4;
        private bool _jumpHeld;
        private bool _jumpCutConsumed;
        private bool _grounded;
        private bool _groundStateInitialized;
        private long _lastGroundedTick = long.MinValue / 4;
        private Vector3 _groundNormal = Vector3.up;
        private float _airborneSeconds;
        private float _previousVerticalSpeed;
        private PhysicMaterial _runtimeMovementMaterial;

        public event Action DashStarted;
        public event Action Jumped;
        public event Action<float> Landed;

        private long FixedTick
        {
            get
            {
                float dt = Mathf.Max(0.0001f, Time.fixedDeltaTime);
                return (long)Math.Round(Time.fixedTime / dt);
            }
        }

        public bool IsDashing => FixedTick < _dashUntilTick;
        public bool IsInvulnerable => FixedTick < _invulnerableUntilTick;
        public bool IsGrounded => _grounded;
        public bool CanJump =>
            !IsDashing &&
            (_grounded || FixedTick - _lastGroundedTick <= SecondsToTicks(Mathf.Max(0f, coyoteTimeSeconds)));
        public Vector3 GroundNormal => _groundNormal;
        public float AirborneSeconds => _airborneSeconds;
        public float VerticalSpeed => _body != null ? _body.velocity.y : 0f;
        public Vector3 Velocity => _body != null ? _body.velocity : Vector3.zero;
        public Vector2 MoveInput => _moveInput;

        private void Awake()
        {
            _body = GetComponent<Rigidbody>();
            if (movementCollider == null) movementCollider = GetComponent<Collider>();

            // GuardianMotor owns vertical integration explicitly so jump arcs are fixed-tick
            // gameplay facts. The scene assembler historically froze Y and disabled gravity;
            // undo that prototype constraint here so old generated scenes upgrade safely.
            _body.useGravity = false;
            _body.constraints &= ~(RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezePositionY);
            _body.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            _body.interpolation = RigidbodyInterpolation.Interpolate;
            _body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            _body.sleepThreshold = 0f;

            ConfigureMovementCollider();
            ResolvePhysicalState();
            ProbeGround(out _grounded, out _groundNormal);
            if (_grounded) _lastGroundedTick = FixedTick;
            _groundStateInitialized = true;
            _previousVerticalSpeed = _body.velocity.y;
        }

        private void OnDisable()
        {
            _moveInput = Vector2.zero;
            _jumpHeld = false;
            _jumpQueued = false;
            _dashQueued = false;
        }

        private void OnDestroy()
        {
            if (_runtimeMovementMaterial != null)
                Destroy(_runtimeMovementMaterial);
        }

        private void ConfigureMovementCollider()
        {
            if (!installLowFrictionMovementMaterial || movementCollider == null) return;
            if (movementCollider.sharedMaterial != null) return;

            _runtimeMovementMaterial = new PhysicMaterial("MindforgeGuardianLowFriction")
            {
                dynamicFriction = 0f,
                staticFriction = 0f,
                bounciness = 0f,
                frictionCombine = PhysicMaterialCombine.Minimum,
                bounceCombine = PhysicMaterialCombine.Minimum,
            };
            movementCollider.sharedMaterial = _runtimeMovementMaterial;
        }

        private void ResolvePhysicalState()
        {
            if (loadout == null) loadout = GetComponent<GuardianEquipmentLoadout>();
            if (physicalCombat == null) physicalCombat = GetComponent<GuardianSwordShieldController>();
            if (targetLock == null) targetLock = GetComponent<GuardianTargetLock>();
            if (cameraReference == null && Camera.main != null) cameraReference = Camera.main.transform;
            if (movementCollider == null) movementCollider = GetComponent<Collider>();
        }

        public void SetMoveInput(Vector2 input)
        {
            _moveInput = Vector2.ClampMagnitude(input, 1f);
            if (_moveInput.sqrMagnitude > 0.0001f && _body != null) _body.WakeUp();
        }

        public void SetJumpHeld(bool held)
        {
            _jumpHeld = held;
        }

        public bool RequestJump()
        {
            if (_body == null) return false;
            _jumpQueued = true;
            _jumpQueuedUntilTick = FixedTick + SecondsToTicks(Mathf.Max(0.02f, jumpBufferSeconds));
            _body.WakeUp();
            return true;
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
                // end of the current dash is buffered on fixed ticks so chaining remains
                // deterministic under the input-tape replay contract.
                _dashQueued = true;
                _dashQueuedUntilTick = FixedTick + SecondsToTicks(Mathf.Max(0.02f, dashInputBufferSeconds));
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
            int rollTicks = SecondsToTicks(rollDuration);
            int invulnerabilityTicks = Mathf.Min(
                rollTicks,
                SecondsToTicks(Mathf.Min(rollDuration, Mathf.Max(0f, dodgeInvulnerabilitySeconds))));
            _dashUntilTick = FixedTick + rollTicks;
            _invulnerableUntilTick = FixedTick + invulnerabilityTicks;
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

            // Contacts can push the dynamic capsule but may not rotate its authored facing.
            // Yaw is an explicit player/lock-on state resolved below by MoveRotation.
            _body.angularVelocity = Vector3.zero;

            float dt = Mathf.Max(0.0001f, Time.fixedDeltaTime);
            UpdateGroundState(dt);
            ConsumeBufferedJump();

            bool dashing = IsDashing;
            if (_wasDashing && !dashing)
            {
                Vector3 horizontal = Vector3.ProjectOnPlane(_body.velocity, Vector3.up) * Mathf.Clamp01(dashExitVelocityRetention);
                _body.velocity = horizontal + Vector3.up * _body.velocity.y;
            }
            _wasDashing = dashing;

            if (dashing)
            {
                ApplyVerticalMotion(dt);
                return;
            }

            if (_dashQueued)
            {
                if (FixedTick <= _dashQueuedUntilTick && (physicalCombat == null || physicalCombat.CanDodge))
                {
                    StartDash(ResolveDashDirection(_queuedDashFallback));
                    _wasDashing = true;
                    ApplyVerticalMotion(dt);
                    return;
                }
                _dashQueued = false;
            }

            float loadMultiplier = loadout != null ? loadout.MoveSpeedMultiplier : 1f;
            float stanceMultiplier = physicalCombat != null ? Mathf.Clamp(physicalCombat.MovementMultiplier, 0.2f, 1f) : 1f;
            float directionalMultiplier = DirectionalSpeedMultiplier(_moveInput);
            float maxSpeed = tuning.maxSpeed * loadMultiplier * stanceMultiplier * directionalMultiplier;
            Vector3 desiredDir = MoveDirectionWorld();
            Vector3 targetVelocity = desiredDir * maxSpeed;
            Vector3 horizontalVelocity = Vector3.ProjectOnPlane(_body.velocity, Vector3.up);

            float response;
            if (!_grounded)
            {
                if (desiredDir.sqrMagnitude < 0.0001f)
                {
                    targetVelocity = horizontalVelocity;
                    response = 0f;
                }
                else
                {
                    targetVelocity *= Mathf.Max(0f, airSpeedMultiplier);
                    response = Mathf.Max(0f, airAcceleration);
                }
            }
            else if (desiredDir.sqrMagnitude < 0.0001f)
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

            Vector3 nextHorizontal = response > 0f
                ? Vector3.MoveTowards(horizontalVelocity, targetVelocity, response * Time.fixedDeltaTime)
                : horizontalVelocity;
            _body.velocity = nextHorizontal + Vector3.up * _body.velocity.y;

            ApplyVerticalMotion(dt);
            UpdateFacing(desiredDir);
        }

        private void UpdateGroundState(float dt)
        {
            bool wasGrounded = _grounded;
            float priorVertical = _previousVerticalSpeed;
            ProbeGround(out bool probedGround, out Vector3 normal);

            bool risingTooFast = _body.velocity.y > Mathf.Max(0f, maximumGroundedRiseSpeed);
            _grounded = probedGround && !risingTooFast;
            _groundNormal = _grounded ? normal : Vector3.up;

            if (_grounded)
            {
                _lastGroundedTick = FixedTick;
                _airborneSeconds = 0f;
            }
            else
            {
                _airborneSeconds += dt;
            }

            if (_groundStateInitialized && !wasGrounded && _grounded)
                Landed?.Invoke(Mathf.Max(0f, -priorVertical));

            _groundStateInitialized = true;
            _previousVerticalSpeed = _body.velocity.y;
        }

        private void ProbeGround(out bool grounded, out Vector3 normal)
        {
            grounded = false;
            normal = Vector3.up;
            if (movementCollider == null) return;

            ResolveProbeGeometry(out Vector3 bottomSphereCenter, out float radius);
            float probeRadius = Mathf.Max(0.04f, radius * Mathf.Clamp(groundProbeRadiusScale, 0.55f, 0.98f));
            float lift = Mathf.Max(0.01f, groundProbeLift);
            float distance = lift + Mathf.Max(0.02f, groundProbeDistance);
            Vector3 origin = bottomSphereCenter + Vector3.up * lift;
            float minimumNormalY = Mathf.Cos(Mathf.Clamp(maxGroundSlopeDegrees, 1f, 89f) * Mathf.Deg2Rad);

            int count = Physics.SphereCastNonAlloc(
                origin,
                probeRadius,
                Vector3.down,
                _groundHits,
                distance,
                groundMask,
                QueryTriggerInteraction.Ignore);

            float nearest = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = _groundHits[i];
                if (hit.collider == null || IsSelfCollider(hit.collider) || IsDynamicCombatantCollider(hit.collider)) continue;
                if (hit.normal.y < minimumNormalY) continue;
                if (hit.distance < 0f || hit.distance >= nearest) continue;
                nearest = hit.distance;
                normal = hit.normal.normalized;
                grounded = true;
            }
        }

        private void ResolveProbeGeometry(out Vector3 bottomSphereCenter, out float radius)
        {
            if (movementCollider is CapsuleCollider capsule)
            {
                Vector3 scale = capsule.transform.lossyScale;
                float radialScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
                float verticalScale = Mathf.Abs(scale.y);
                radius = Mathf.Max(0.04f, capsule.radius * radialScale);
                float halfHeight = Mathf.Max(radius, capsule.height * verticalScale * 0.5f);
                Vector3 center = capsule.transform.TransformPoint(capsule.center);
                bottomSphereCenter = center - Vector3.up * Mathf.Max(0f, halfHeight - radius);
                return;
            }

            Bounds bounds = movementCollider.bounds;
            radius = Mathf.Max(0.04f, Mathf.Min(bounds.extents.x, bounds.extents.z));
            bottomSphereCenter = new Vector3(bounds.center.x, bounds.min.y + radius, bounds.center.z);
        }

        private bool IsSelfCollider(Collider candidate)
        {
            if (candidate == null) return false;
            Transform t = candidate.transform;
            return t == transform || t.IsChildOf(transform);
        }

        private static bool IsDynamicCombatantCollider(Collider candidate)
        {
            if (candidate == null) return false;
            return candidate.GetComponentInParent<CombatantVitals>() != null;
        }

        private void ConsumeBufferedJump()
        {
            if (!_jumpQueued) return;
            if (FixedTick > _jumpQueuedUntilTick)
            {
                _jumpQueued = false;
                return;
            }
            if (!CanJump) return;

            Vector3 velocity = _body.velocity;
            velocity.y = Mathf.Max(0.1f, jumpVelocity);
            _body.velocity = velocity;
            _grounded = false;
            _airborneSeconds = 0f;
            _jumpCutConsumed = false;
            _jumpQueued = false;
            _lastGroundedTick = long.MinValue / 4;
            _body.WakeUp();
            Jumped?.Invoke();
        }

        private void ApplyVerticalMotion(float dt)
        {
            Vector3 velocity = _body.velocity;

            if (_grounded && velocity.y <= 0f)
            {
                velocity.y = -Mathf.Max(0.1f, groundStickSpeed);
                _body.velocity = velocity;
                _previousVerticalSpeed = velocity.y;
                return;
            }

            if (!_jumpHeld && !_jumpCutConsumed && velocity.y > Mathf.Max(0.35f, apexVerticalSpeed))
            {
                velocity.y *= Mathf.Clamp(jumpReleaseVelocityMultiplier, 0.2f, 1f);
                _jumpCutConsumed = true;
            }

            float gravityMultiplier;
            if (velocity.y > Mathf.Max(0.05f, apexVerticalSpeed))
                gravityMultiplier = _jumpCutConsumed ? releasedJumpGravityMultiplier : risingGravityMultiplier;
            else if (velocity.y > -Mathf.Max(0.05f, apexVerticalSpeed))
                gravityMultiplier = apexGravityMultiplier;
            else
                gravityMultiplier = fallingGravityMultiplier;

            float gravity = Physics.gravity.y;
            if (gravity >= -0.01f) gravity = -9.81f;
            velocity.y += gravity * Mathf.Max(0.05f, gravityMultiplier) * dt;
            velocity.y = Mathf.Max(velocity.y, -Mathf.Max(1f, terminalFallSpeed));
            _body.velocity = velocity;
            _previousVerticalSpeed = velocity.y;
        }

        private float DirectionalSpeedMultiplier(Vector2 input)
        {
            if (input.sqrMagnitude < 0.0001f) return 1f;

            float forwardAmount = Mathf.Clamp01(input.y);
            float backwardAmount = Mathf.Clamp01(-input.y);
            float sideAmount = Mathf.Clamp01(Mathf.Abs(input.x));

            float directional;
            if (forwardAmount > 0f)
            {
                directional = Mathf.Lerp(
                    Mathf.Max(0.1f, strafeSpeedMultiplier),
                    Mathf.Max(0.1f, forwardSpeedMultiplier),
                    forwardAmount);
            }
            else if (backwardAmount > 0f)
            {
                directional = Mathf.Lerp(
                    Mathf.Max(0.1f, strafeSpeedMultiplier),
                    Mathf.Max(0.1f, backwardSpeedMultiplier),
                    backwardAmount);
            }
            else
            {
                directional = Mathf.Max(0.1f, strafeSpeedMultiplier);
            }

            // Diagonal input should preserve the fast-forward character without gaining
            // an extra vector-length speed bonus from pressing two directions at once.
            if (forwardAmount > 0f && sideAmount > 0f)
                directional = Mathf.Lerp(directional, Mathf.Max(0.1f, forwardSpeedMultiplier), forwardAmount * 0.35f);

            return directional;
        }

        private void UpdateFacing(Vector3 moveDirection)
        {
            if (physicalCombat != null && !physicalCombat.CanTurn) return;

            Vector3 facing = Vector3.zero;
            float sharpness = _grounded ? freeTurnSharpness : airTurnSharpness;

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

            float turnMultiplier = physicalCombat != null ? Mathf.Clamp(physicalCombat.TurnMultiplier, 0.05f, 1f) : 1f;
            Quaternion desired = Quaternion.LookRotation(facing.normalized, Vector3.up);
            float t = 1f - Mathf.Exp(-Mathf.Max(0.1f, sharpness * turnMultiplier) * Time.fixedDeltaTime);
            _body.MoveRotation(Quaternion.Slerp(_body.rotation, desired, t));
        }

        private void FaceDirectionImmediate(Vector3 direction)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f) return;
            _body.MoveRotation(Quaternion.LookRotation(direction.normalized, Vector3.up));
        }

        private static int SecondsToTicks(float seconds)
        {
            float dt = Mathf.Max(0.0001f, Time.fixedDeltaTime);
            return Mathf.Max(1, Mathf.CeilToInt(Mathf.Max(0f, seconds) / dt));
        }
    }
}
