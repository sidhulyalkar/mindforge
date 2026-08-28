using System;
using UnityEngine;
using Mindforge.Combat;

namespace Mindforge.Presentation
{
    /// <summary>
    /// Third-person ARPG camera for the competition showcase.
    ///
    /// Free mode follows behind the Guardian with full mouse/trackpad orbit and arrow-key
    /// orbit fallback. Conventional target lock rotates the camera around the Guardian so
    /// the locked enemy remains a stable visual anchor. The camera never creates player
    /// actions, neural events, damage, movement or lock state; it only consumes them.
    /// </summary>
    public sealed class ShowcaseCameraRig : MonoBehaviour
    {
        [SerializeField] private Transform guardian;
        [SerializeField] private Transform boss;
        [SerializeField] private GuardianMotor motor;
        [SerializeField] private GuardianTargetLock targetLock;
        [SerializeField] private Camera gameplayCamera;

        [Header("Third-person shoulder camera")]
        [SerializeField] private float pivotHeight = 1.28f;
        [SerializeField] private float freeDistance = 4.45f;
        [SerializeField] private float lockDistance = 5.20f;
        [SerializeField] private float shoulderOffset = 0.70f;
        [SerializeField] private float freeLookAhead = 5.6f;
        [SerializeField] private float gameplayFieldOfView = 58f;
        [SerializeField] private float initialYaw = 0f;
        [SerializeField] private float initialPitch = 12f;
        [SerializeField] private float minPitch = -10f;
        [SerializeField] private float maxPitch = 40f;

        [Header("Orbit input")]
        [SerializeField] private float mouseYawSensitivity = 2.35f;
        [SerializeField] private float mousePitchSensitivity = 1.85f;
        [SerializeField] private float arrowYawSpeed = 105f;
        [SerializeField] private float arrowPitchSpeed = 72f;
        [SerializeField] private bool invertY;
        [SerializeField] private bool lockCursorDuringGameplay = true;

        [Header("Target lock framing")]
        [SerializeField] private float lockYawSharpness = 14f;
        [SerializeField] private float lockPitchSharpness = 10f;
        [SerializeField] private float lockLookWeight = 0.58f;
        [SerializeField] private float lockTargetHeight = 0.95f;
        [SerializeField] private float lockShoulderOffset = 0.30f;

        [Header("Camera response")]
        [SerializeField] private float positionSmoothSeconds = 0.040f;
        [SerializeField] private float verticalFollowSmoothSeconds = 0.105f;
        [SerializeField] private float freeRotationSharpness = 26f;
        [SerializeField] private float collisionRadius = 0.22f;
        [SerializeField] private float collisionPadding = 0.16f;
        [SerializeField] private LayerMask collisionMask = ~0;

        private readonly RaycastHit[] _collisionHits = new RaycastHit[12];
        private Vector3 _positionVelocity;
        private float _pivotYVelocity;
        private float _smoothedPivotY;
        private float _yaw;
        private float _pitch;
        private bool _initialized;
        private bool _pivotInitialized;
        private bool _subscribed;

        public event Action<bool> TargetFocusChanged;

        public bool TargetFocusActive => targetLock != null && targetLock.Locked;
        public Transform FocusTarget => targetLock != null ? targetLock.Target : null;
        public float Yaw => _yaw;
        public float Pitch => _pitch;

        public void Configure(
            Transform player,
            Transform target,
            GuardianMotor guardianMotor,
            GuardianTargetLock lockState,
            Camera camera)
        {
            UnsubscribeLock();
            guardian = player;
            boss = target;
            motor = guardianMotor;
            targetLock = lockState;
            gameplayCamera = camera;
            SubscribeLock();
            InitializeOrbitFromScene();
        }

        // Kept as a compatibility surface for UI/debug callers. Player input itself is
        // owned by GuardianTargetLock, not this presentation component.
        public void SetTargetFocus(bool active)
        {
            targetLock?.SetLocked(active);
        }

        private void Awake()
        {
            _yaw = initialYaw;
            _pitch = initialPitch;
        }

        private void Start()
        {
            SubscribeLock();
            InitializeOrbitFromScene();
            if (lockCursorDuringGameplay) CaptureCursor();
        }

        private void OnEnable() => SubscribeLock();

        private void OnDisable()
        {
            UnsubscribeLock();
            ReleaseCursor();
        }

        private void Update()
        {
            if (guardian == null) return;

            if (lockCursorDuringGameplay)
            {
                if (Input.GetKeyDown(KeyCode.Escape)) ReleaseCursor();
                else if (Cursor.lockState != CursorLockMode.Locked && Input.GetMouseButtonDown(0)) CaptureCursor();
            }

            if (TargetFocusActive) return;

            float dt = Mathf.Min(0.05f, Time.unscaledDeltaTime);
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");
            float arrowX = 0f;
            float arrowY = 0f;
            if (Input.GetKey(KeyCode.LeftArrow)) arrowX -= 1f;
            if (Input.GetKey(KeyCode.RightArrow)) arrowX += 1f;
            if (Input.GetKey(KeyCode.DownArrow)) arrowY -= 1f;
            if (Input.GetKey(KeyCode.UpArrow)) arrowY += 1f;

            _yaw += mouseX * mouseYawSensitivity + arrowX * arrowYawSpeed * dt;
            float pitchSign = invertY ? 1f : -1f;
            _pitch += mouseY * mousePitchSensitivity * pitchSign + arrowY * arrowPitchSpeed * dt;
            _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);
        }

        private void LateUpdate()
        {
            if (guardian == null) return;
            float dt = Mathf.Min(0.05f, Time.unscaledDeltaTime);
            if (dt <= 0f) return;

            bool locked = TargetFocusActive && FocusTarget != null;
            float desiredPivotY = guardian.position.y + pivotHeight;
            if (!_pivotInitialized)
            {
                _smoothedPivotY = desiredPivotY;
                _pivotInitialized = true;
            }
            else
            {
                _smoothedPivotY = Mathf.SmoothDamp(
                    _smoothedPivotY,
                    desiredPivotY,
                    ref _pivotYVelocity,
                    Mathf.Max(0.02f, verticalFollowSmoothSeconds),
                    Mathf.Infinity,
                    dt);
            }

            // Horizontal framing stays responsive while vertical follow is slightly softer.
            // The Guardian can rise/fall inside the composition without the camera copying
            // every jump/landing centimeter and making traversal feel visually brittle.
            Vector3 pivot = new Vector3(guardian.position.x, _smoothedPivotY, guardian.position.z);

            if (locked)
            {
                Vector3 targetPoint = FocusTarget.position + Vector3.up * lockTargetHeight;
                Vector3 flatToTarget = Vector3.ProjectOnPlane(targetPoint - pivot, Vector3.up);
                if (flatToTarget.sqrMagnitude > 0.001f)
                {
                    float desiredYaw = Mathf.Atan2(flatToTarget.x, flatToTarget.z) * Mathf.Rad2Deg;
                    _yaw = Mathf.LerpAngle(_yaw, desiredYaw,
                        1f - Mathf.Exp(-Mathf.Max(0.1f, lockYawSharpness) * dt));

                    float horizontal = Mathf.Max(0.1f, flatToTarget.magnitude);
                    float desiredPitch = -Mathf.Atan2(targetPoint.y - pivot.y, horizontal) * Mathf.Rad2Deg + 8f;
                    desiredPitch = Mathf.Clamp(desiredPitch, 3f, 25f);
                    _pitch = Mathf.Lerp(_pitch, desiredPitch,
                        1f - Mathf.Exp(-Mathf.Max(0.1f, lockPitchSharpness) * dt));
                }
            }

            Quaternion orbit = Quaternion.Euler(_pitch, _yaw, 0f);
            Vector3 back = orbit * Vector3.back;
            Vector3 right = Quaternion.Euler(0f, _yaw, 0f) * Vector3.right;
            float distance = locked ? lockDistance : freeDistance;
            float shoulder = locked ? lockShoulderOffset : shoulderOffset;
            Vector3 desiredPosition = pivot + back * distance + right * shoulder;
            desiredPosition = ResolveCameraCollision(pivot, desiredPosition);

            Vector3 lookPoint;
            if (locked)
            {
                Vector3 targetPoint = FocusTarget.position + Vector3.up * lockTargetHeight;
                lookPoint = Vector3.Lerp(pivot, targetPoint, Mathf.Clamp01(lockLookWeight));
            }
            else
            {
                Vector3 forward = Quaternion.Euler(_pitch, _yaw, 0f) * Vector3.forward;
                lookPoint = pivot + forward * freeLookAhead;
            }

            Vector3 lookDirection = lookPoint - desiredPosition;
            if (lookDirection.sqrMagnitude < 0.0001f) lookDirection = transform.forward;
            Quaternion desiredRotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);

            if (!_initialized)
            {
                transform.position = desiredPosition;
                transform.rotation = desiredRotation;
                _initialized = true;
            }
            else
            {
                transform.position = Vector3.SmoothDamp(
                    transform.position,
                    desiredPosition,
                    ref _positionVelocity,
                    Mathf.Max(0.015f, positionSmoothSeconds),
                    Mathf.Infinity,
                    dt);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    desiredRotation,
                    1f - Mathf.Exp(-Mathf.Max(0.1f, freeRotationSharpness) * dt));
            }

            if (gameplayCamera != null)
            {
                // Keep FOV fixed rather than speed-reactive. A wider constant projection
                // improves traversal readability without changing VEP target angular size
                // as a function of movement speed or jump state.
                gameplayCamera.fieldOfView = Mathf.Clamp(gameplayFieldOfView, 45f, 75f);
                gameplayCamera.nearClipPlane = 0.06f;
                gameplayCamera.farClipPlane = 140f;
            }
        }

        private Vector3 ResolveCameraCollision(Vector3 pivot, Vector3 desired)
        {
            Vector3 delta = desired - pivot;
            float distance = delta.magnitude;
            if (distance <= 0.01f) return desired;

            Vector3 direction = delta / distance;
            int count = Physics.SphereCastNonAlloc(
                pivot,
                Mathf.Max(0.05f, collisionRadius),
                direction,
                _collisionHits,
                distance,
                collisionMask,
                QueryTriggerInteraction.Ignore);

            bool foundWorldHit = false;
            float nearest = distance;
            for (int i = 0; i < count; i++)
            {
                Collider collider = _collisionHits[i].collider;
                if (collider == null || IsGuardianHierarchy(collider.transform) || IsDynamicActor(collider)) continue;
                float hitDistance = _collisionHits[i].distance;
                if (hitDistance < 0f || hitDistance >= nearest) continue;
                nearest = hitDistance;
                foundWorldHit = true;
            }

            if (foundWorldHit)
            {
                float resolved = Mathf.Max(0.35f, nearest - Mathf.Max(0.02f, collisionPadding));
                return pivot + direction * resolved;
            }

            return desired;
        }

        private bool IsGuardianHierarchy(Transform candidate)
        {
            if (guardian == null || candidate == null) return false;
            return candidate == guardian || candidate.IsChildOf(guardian);
        }

        private static bool IsDynamicActor(Collider collider)
        {
            if (collider == null) return false;
            CombatantVitals actor = collider.GetComponentInParent<CombatantVitals>();
            return actor != null;
        }

        private void InitializeOrbitFromScene()
        {
            if (guardian == null) return;
            Vector3 flatForward = Vector3.ProjectOnPlane(guardian.forward, Vector3.up);
            if (flatForward.sqrMagnitude > 0.001f)
                _yaw = Mathf.Atan2(flatForward.x, flatForward.z) * Mathf.Rad2Deg;
            _pitch = Mathf.Clamp(initialPitch, minPitch, maxPitch);
            _smoothedPivotY = guardian.position.y + pivotHeight;
            _pivotInitialized = true;
        }

        private void SubscribeLock()
        {
            if (_subscribed || targetLock == null) return;
            targetLock.LockChanged += OnLockChanged;
            _subscribed = true;
        }

        private void UnsubscribeLock()
        {
            if (!_subscribed || targetLock == null) return;
            targetLock.LockChanged -= OnLockChanged;
            _subscribed = false;
        }

        private void OnLockChanged(bool locked)
        {
            TargetFocusChanged?.Invoke(locked);
            Debug.Log($"[Mindforge:Camera] Third-person target lock {(locked ? "ON" : "OFF")}. Camera follows conventional lock state.");
        }

        private static void CaptureCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private static void ReleaseCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
