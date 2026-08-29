using System;
using UnityEngine;
using Mindforge.Combat;

namespace Mindforge.Traversal
{
    /// <summary>
    /// Exclusive mounted locomotion authority for the Guardian. The existing Guardian
    /// Rigidbody remains the only player physics body. While mounted, foot input/motor are
    /// disabled; Aetherblade attacks still go through GuardianSwordShieldController.
    /// BCI/neural state is intentionally absent from this component.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class GuardianHoverbikeController : MonoBehaviour
    {
        [SerializeField] private GuardianMotor footMotor;
        [SerializeField] private GuardianCombatInput footInput;
        [SerializeField] private GuardianSwordShieldController bladeCombat;
        [SerializeField] private GuardianTargetLock targetLock;
        [SerializeField] private CombatantVitals vitals;
        [SerializeField] private Camera cameraReference;

        [Header("Mount interaction")]
        [SerializeField] private KeyCode mountKey = KeyCode.E;
        [SerializeField, Min(0.5f)] private float discoveryRadius = 3.2f;
        [SerializeField, Min(0.05f)] private float nearbyRefreshSeconds = 0.18f;

        [Header("Mounted movement · fixed tick")]
        [SerializeField] private float cruiseSpeed = 15.2f;
        [SerializeField] private float boostSpeed = 21.2f;
        [SerializeField] private float acceleration = 34f;
        [SerializeField] private float braking = 42f;
        [SerializeField] private float turnSharpness = 11.5f;
        [SerializeField] private float rideHeight = 1.05f;
        [SerializeField] private float hoverSpring = 34f;
        [SerializeField] private float hoverDamping = 7.5f;
        [SerializeField] private float missingGroundGravity = 18f;
        [SerializeField] private float terminalFallSpeed = 18f;

        [Header("Boost")]
        [SerializeField] private float boostDurationSeconds = 0.52f;
        [SerializeField] private float boostCooldownSeconds = 0.46f;

        private readonly RaycastHit[] _groundHits = new RaycastHit[12];
        private Rigidbody _body;
        private Vector2 _moveInput;
        private bool _mountLatched;
        private bool _attackLatched;
        private bool _boostLatched;
        private bool _mounted;
        private bool _footMotorWasEnabled;
        private bool _footInputWasEnabled;
        private long _boostUntilTick = long.MinValue / 4;
        private long _boostCooldownUntilTick = long.MinValue / 4;
        private AetherHoverbikeMount _activeBike;
        private AetherHoverbikeMount _nearbyBike;
        private float _nextNearbyRefresh;

        public event Action<bool> MountedChanged;
        public event Action BoostStarted;

        public bool Mounted => _mounted && _activeBike != null;
        public bool Boosting => Mounted && FixedTick < _boostUntilTick;
        public AetherHoverbikeMount ActiveBike => Mounted ? _activeBike : null;
        public AetherHoverbikeMount NearbyBike => !_mounted ? _nearbyBike : null;
        public bool CanMountNearby => !_mounted && _nearbyBike != null;
        public float HorizontalSpeed => _body != null ? Vector3.ProjectOnPlane(_body.velocity, Vector3.up).magnitude : 0f;
        public float Speed01 => Mathf.Clamp01(HorizontalSpeed / Mathf.Max(0.1f, Boosting ? boostSpeed : cruiseSpeed));

        private long FixedTick
        {
            get
            {
                float dt = Mathf.Max(0.0001f, Time.fixedDeltaTime);
                return (long)Math.Round(Time.fixedTime / dt);
            }
        }

        private void Awake()
        {
            _body = GetComponent<Rigidbody>();
            ResolveDependencies();
        }

        private void OnDisable()
        {
            _moveInput = Vector2.zero;
            _mountLatched = false;
            _attackLatched = false;
            _boostLatched = false;
            if (_mounted) Dismount(true);
        }

        private void Update()
        {
            ResolveDependencies();
            if (vitals != null && !vitals.IsAlive)
            {
                if (_mounted) Dismount(true);
                return;
            }

            if (!_mounted && Time.unscaledTime >= _nextNearbyRefresh)
            {
                _nearbyBike = FindNearestAvailableBike(Mathf.Max(discoveryRadius, 0.5f));
                _nextNearbyRefresh = Time.unscaledTime + Mathf.Max(0.05f, nearbyRefreshSeconds);
            }

            _mountLatched |= Input.GetKeyDown(mountKey);
            if (!_mounted) return;

            _moveInput = SampleWasdMovement();
            _attackLatched |= Input.GetKeyDown(KeyCode.F) || Input.GetMouseButtonDown(0);
            _boostLatched |= Input.GetKeyDown(KeyCode.LeftShift) ||
                             Input.GetKeyDown(KeyCode.RightShift) ||
                             Input.GetMouseButtonDown(1);
        }

        private void FixedUpdate()
        {
            ResolveDependencies();
            if (_body == null) return;

            if (!_mounted)
            {
                if (_mountLatched) TryMountNearest();
                _mountLatched = false;
                return;
            }

            if (_mountLatched)
            {
                _mountLatched = false;
                Dismount(false);
                return;
            }

            if (_boostLatched)
            {
                TryStartBoost();
                _boostLatched = false;
            }

            Vector3 aim = ResolveAimDirection();
            if (_attackLatched)
            {
                _attackLatched = false;
                bladeCombat?.TryLightAttack(aim);
            }

            ApplyMountedMovement(aim);
        }

        private void TryMountNearest()
        {
            if (_mounted) return;
            AetherHoverbikeMount bike = FindNearestAvailableBike(Mathf.Max(discoveryRadius, 0.5f));
            if (bike == null || !bike.InRange(transform.position)) return;

            Vector3 mountPoint = bike.MountWorldPoint;
            _footMotorWasEnabled = footMotor != null && footMotor.enabled;
            _footInputWasEnabled = footInput != null && footInput.enabled;

            if (footInput != null) footInput.enabled = false;
            if (footMotor != null) footMotor.enabled = false;

            _body.position = mountPoint;
            _body.velocity = Vector3.zero;
            _body.angularVelocity = Vector3.zero;
            if (!bike.AttachTo(transform))
            {
                RestoreFootAuthority();
                return;
            }

            _activeBike = bike;
            _nearbyBike = null;
            _mounted = true;
            _boostUntilTick = long.MinValue / 4;
            _boostCooldownUntilTick = FixedTick;
            _moveInput = Vector2.zero;
            _attackLatched = false;
            _boostLatched = false;
            MountedChanged?.Invoke(true);
        }

        private void Dismount(bool emergency)
        {
            if (!_mounted)
            {
                RestoreFootAuthority();
                return;
            }

            AetherHoverbikeMount bike = _activeBike;
            _mounted = false;
            _activeBike = null;
            _boostUntilTick = long.MinValue / 4;
            _moveInput = Vector2.zero;
            _attackLatched = false;
            _boostLatched = false;

            if (bike != null)
            {
                Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
                if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
                Vector3 park = transform.position - forward.normalized * 1.55f - Vector3.up * 0.72f;
                bike.DetachTo(park, Quaternion.LookRotation(forward.normalized, Vector3.up));
            }

            if (_body != null)
            {
                Vector3 horizontal = Vector3.ProjectOnPlane(_body.velocity, Vector3.up) * (emergency ? 0f : 0.45f);
                _body.velocity = horizontal;
                _body.angularVelocity = Vector3.zero;
            }

            RestoreFootAuthority();
            _nextNearbyRefresh = 0f;
            MountedChanged?.Invoke(false);
        }

        private void RestoreFootAuthority()
        {
            if (footMotor != null) footMotor.enabled = _footMotorWasEnabled;
            if (footInput != null) footInput.enabled = _footInputWasEnabled;
        }

        private void ApplyMountedMovement(Vector3 aim)
        {
            float dt = Mathf.Max(0.0001f, Time.fixedDeltaTime);
            Vector3 desiredDirection = CameraRelativeDirection(_moveInput);
            float magnitude = Mathf.Clamp01(_moveInput.magnitude);
            float actionScale = bladeCombat != null ? Mathf.Clamp(bladeCombat.MovementMultiplier, 0.58f, 1f) : 1f;
            float topSpeed = (Boosting ? boostSpeed : cruiseSpeed) * actionScale;
            Vector3 desiredHorizontal = desiredDirection * topSpeed * magnitude;
            Vector3 horizontal = Vector3.ProjectOnPlane(_body.velocity, Vector3.up);
            float rate = magnitude > 0.01f ? acceleration : braking;
            horizontal = Vector3.MoveTowards(horizontal, desiredHorizontal, Mathf.Max(0f, rate) * dt);

            float vertical = ResolveHoverVerticalVelocity(dt);
            _body.velocity = horizontal + Vector3.up * vertical;
            _body.angularVelocity = Vector3.zero;

            Vector3 face = desiredDirection.sqrMagnitude > 0.01f ? desiredDirection : aim;
            face = Vector3.ProjectOnPlane(face, Vector3.up);
            if (face.sqrMagnitude > 0.01f)
            {
                Quaternion target = Quaternion.LookRotation(face.normalized, Vector3.up);
                float t = 1f - Mathf.Exp(-Mathf.Max(0.1f, turnSharpness) * dt);
                _body.MoveRotation(Quaternion.Slerp(_body.rotation, target, t));
            }
        }

        private float ResolveHoverVerticalVelocity(float dt)
        {
            if (TryFindGround(out RaycastHit hit))
            {
                float targetY = hit.point.y + Mathf.Max(0.25f, rideHeight);
                float error = targetY - _body.position.y;
                float accelerationY = error * Mathf.Max(0f, hoverSpring) - _body.velocity.y * Mathf.Max(0f, hoverDamping);
                return Mathf.Clamp(_body.velocity.y + accelerationY * dt, -8f, 8f);
            }

            return Mathf.Max(
                -Mathf.Max(1f, terminalFallSpeed),
                _body.velocity.y - Mathf.Max(0f, missingGroundGravity) * dt);
        }

        private bool TryFindGround(out RaycastHit nearest)
        {
            nearest = default;
            Vector3 origin = _body.position + Vector3.up * 1.6f;
            int count = Physics.RaycastNonAlloc(
                origin,
                Vector3.down,
                _groundHits,
                5.0f,
                ~0,
                QueryTriggerInteraction.Ignore);

            float best = float.PositiveInfinity;
            bool found = false;
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = _groundHits[i];
                Transform hitTransform = hit.transform;
                if (hitTransform == null) continue;
                if (hitTransform == transform || hitTransform.IsChildOf(transform)) continue;
                if (hit.distance >= best) continue;
                best = hit.distance;
                nearest = hit;
                found = true;
            }
            return found;
        }

        private void TryStartBoost()
        {
            if (!Mounted || FixedTick < _boostCooldownUntilTick) return;
            int duration = SecondsToTicks(Mathf.Max(0.05f, boostDurationSeconds));
            int cooldown = SecondsToTicks(Mathf.Max(0f, boostCooldownSeconds));
            _boostUntilTick = FixedTick + duration;
            _boostCooldownUntilTick = _boostUntilTick + cooldown;
            BoostStarted?.Invoke();
        }

        private Vector3 ResolveAimDirection()
        {
            if (targetLock != null && targetLock.Locked)
                return targetLock.DirectionFrom(transform.position);

            Camera camera = cameraReference != null ? cameraReference : Camera.main;
            Vector3 aim = camera != null
                ? Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up)
                : Vector3.ProjectOnPlane(transform.forward, Vector3.up);
            if (aim.sqrMagnitude < 0.01f) aim = Vector3.forward;
            return aim.normalized;
        }

        private Vector3 CameraRelativeDirection(Vector2 input)
        {
            Camera camera = cameraReference != null ? cameraReference : Camera.main;
            Vector3 forward = camera != null ? Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up) : Vector3.forward;
            Vector3 right = camera != null ? Vector3.ProjectOnPlane(camera.transform.right, Vector3.up) : Vector3.right;
            if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
            if (right.sqrMagnitude < 0.01f) right = Vector3.right;
            forward.Normalize();
            right.Normalize();
            Vector3 world = right * input.x + forward * input.y;
            return Vector3.ClampMagnitude(world, 1f);
        }

        private AetherHoverbikeMount FindNearestAvailableBike(float radius)
        {
            AetherHoverbikeMount[] bikes = FindObjectsOfType<AetherHoverbikeMount>(true);
            AetherHoverbikeMount best = null;
            float maxSqr = radius * radius;
            float bestSqr = maxSqr;
            for (int i = 0; i < bikes.Length; i++)
            {
                AetherHoverbikeMount bike = bikes[i];
                if (bike == null || bike.Occupied) continue;
                float sqr = Vector3.SqrMagnitude(transform.position - bike.MountWorldPoint);
                if (sqr > bestSqr) continue;
                bestSqr = sqr;
                best = bike;
            }
            return best;
        }

        private static Vector2 SampleWasdMovement()
        {
            float x = 0f;
            float y = 0f;
            if (Input.GetKey(KeyCode.A)) x -= 1f;
            if (Input.GetKey(KeyCode.D)) x += 1f;
            if (Input.GetKey(KeyCode.S)) y -= 1f;
            if (Input.GetKey(KeyCode.W)) y += 1f;
            return Vector2.ClampMagnitude(new Vector2(x, y), 1f);
        }

        private int SecondsToTicks(float seconds)
        {
            float dt = Mathf.Max(0.0001f, Time.fixedDeltaTime);
            return Mathf.Max(1, Mathf.CeilToInt(Mathf.Max(0f, seconds) / dt));
        }

        private void ResolveDependencies()
        {
            if (_body == null) _body = GetComponent<Rigidbody>();
            if (footMotor == null) footMotor = GetComponent<GuardianMotor>();
            if (footInput == null) footInput = GetComponent<GuardianCombatInput>();
            if (bladeCombat == null) bladeCombat = GetComponent<GuardianSwordShieldController>();
            if (targetLock == null) targetLock = GetComponent<GuardianTargetLock>();
            if (vitals == null) vitals = GetComponent<CombatantVitals>();
            if (cameraReference == null) cameraReference = Camera.main;
        }
    }
}
