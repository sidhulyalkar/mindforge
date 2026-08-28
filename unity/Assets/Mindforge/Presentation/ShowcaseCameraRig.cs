using System;
using UnityEngine;
using Mindforge.Combat;

namespace Mindforge.Presentation
{
    /// <summary>
    /// Presentation-only tactical camera. Free mode favors Guardian readability and
    /// motion lead. Player-controlled target-focus mode keeps Guardian + enemy in a
    /// steadier shared frame that can later serve as a spatial anchor for gaze/BCI UX.
    ///
    /// Target focus never changes aim, movement, attacks, guard, dodge, neural authority,
    /// or boss behavior. T only changes camera composition.
    /// </summary>
    public sealed class ShowcaseCameraRig : MonoBehaviour
    {
        [SerializeField] private Transform guardian;
        [SerializeField] private Transform boss;
        [SerializeField] private GuardianMotor motor;
        [SerializeField] private Camera gameplayCamera;

        [Header("Free tactical framing")]
        [SerializeField] private Vector3 baseOffset = new Vector3(0f, 10.8f, -10.6f);
        [SerializeField] private float separationHeight = 0.18f;
        [SerializeField] private float separationDistance = 0.12f;
        [SerializeField] private float guardianFocusWeight = 0.68f;
        [SerializeField] private float motionLeadSeconds = 0.14f;

        [Header("Enemy focus framing")]
        [SerializeField] private KeyCode targetFocusToggleKey = KeyCode.T;
        [SerializeField] private Vector3 targetFocusOffset = new Vector3(0f, 11.7f, -11.7f);
        [SerializeField, Range(0.45f, 0.72f)] private float targetFocusGuardianWeight = 0.56f;
        [SerializeField] private float targetFocusMotionLeadSeconds = 0.055f;
        [SerializeField] private float targetFocusSeparationDistance = 0.18f;
        [SerializeField] private float targetFocusExtraHeight = 0.55f;
        [SerializeField] private float freeFieldOfView = 55f;
        [SerializeField] private float targetFocusFieldOfView = 50f;

        [Header("Response")]
        [SerializeField] private float positionSmoothSeconds = 0.105f;
        [SerializeField] private float rotationSharpness = 12.5f;
        [SerializeField] private float targetFocusRotationSharpness = 15f;
        [SerializeField] private float fieldOfViewSharpness = 8f;
        [SerializeField] private float maximumExtraDistance = 4.2f;

        private Vector3 _velocity;
        private bool _initialized;
        private bool _targetFocusActive;

        public event Action<bool> TargetFocusChanged;

        public bool TargetFocusActive => _targetFocusActive;
        public Transform FocusTarget => boss;

        public void Configure(Transform player, Transform target, GuardianMotor guardianMotor, Camera camera)
        {
            guardian = player;
            boss = target;
            motor = guardianMotor;
            gameplayCamera = camera;
        }

        public void SetTargetFocus(bool active)
        {
            bool allowed = active && boss != null && boss.gameObject.activeInHierarchy;
            if (_targetFocusActive == allowed) return;
            _targetFocusActive = allowed;
            TargetFocusChanged?.Invoke(_targetFocusActive);
            Debug.Log($"[Mindforge:Camera] Enemy focus {(_targetFocusActive ? "ON" : "OFF")}. Camera composition only; player aim and combat authority unchanged.");
        }

        private void Update()
        {
            if (Input.GetKeyDown(targetFocusToggleKey))
                SetTargetFocus(!_targetFocusActive);

            // If calibration/runtime hides the target, focus fails safely back to free view.
            if (_targetFocusActive && (boss == null || !boss.gameObject.activeInHierarchy))
                SetTargetFocus(false);
        }

        private void LateUpdate()
        {
            if (guardian == null || boss == null) return;
            float dt = Mathf.Min(0.05f, Time.unscaledDeltaTime);
            if (dt <= 0f) return;

            Vector3 player = guardian.position;
            Vector3 enemy = boss.position;
            bool focused = _targetFocusActive && boss.gameObject.activeInHierarchy;
            float guardianWeight = focused ? targetFocusGuardianWeight : guardianFocusWeight;
            Vector3 focus = Vector3.Lerp(enemy, player, Mathf.Clamp01(guardianWeight));
            focus.y = Mathf.Max(player.y, enemy.y * 0.45f) + (focused ? 0.48f : 0.34f);

            float leadSeconds = focused ? targetFocusMotionLeadSeconds : motionLeadSeconds;
            Vector3 lead = motor != null
                ? Vector3.ProjectOnPlane(motor.Velocity, Vector3.up) * leadSeconds
                : Vector3.zero;
            lead = Vector3.ClampMagnitude(lead, focused ? 0.55f : 1.15f);
            focus += lead;

            float separation = Vector3.Distance(
                Vector3.ProjectOnPlane(player, Vector3.up),
                Vector3.ProjectOnPlane(enemy, Vector3.up));
            float distanceScale = focused ? targetFocusSeparationDistance : separationDistance;
            float extra = Mathf.Clamp((separation - 7f) * distanceScale, 0f, maximumExtraDistance);
            Vector3 offsetBase = focused ? targetFocusOffset : baseOffset;
            float extraHeight = Mathf.Clamp((separation - 7f) * separationHeight, 0f, 2.4f);
            if (focused) extraHeight += targetFocusExtraHeight;
            Vector3 offset = offsetBase + new Vector3(0f, extraHeight, -extra);
            Vector3 desiredPosition = focus + offset;

            if (!_initialized)
            {
                transform.position = desiredPosition;
                _initialized = true;
            }
            else
            {
                transform.position = Vector3.SmoothDamp(
                    transform.position,
                    desiredPosition,
                    ref _velocity,
                    Mathf.Max(0.02f, positionSmoothSeconds),
                    Mathf.Infinity,
                    dt);
            }

            Vector3 look = focus - transform.position;
            if (look.sqrMagnitude > 0.01f)
            {
                float sharpness = focused ? targetFocusRotationSharpness : rotationSharpness;
                Quaternion desiredRotation = Quaternion.LookRotation(look.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    desiredRotation,
                    1f - Mathf.Exp(-Mathf.Max(0.1f, sharpness) * dt));
            }

            if (gameplayCamera != null)
            {
                gameplayCamera.nearClipPlane = 0.08f;
                gameplayCamera.farClipPlane = 120f;
                float desiredFov = focused ? targetFocusFieldOfView : freeFieldOfView;
                gameplayCamera.fieldOfView = Mathf.Lerp(
                    gameplayCamera.fieldOfView,
                    desiredFov,
                    1f - Mathf.Exp(-Mathf.Max(0.1f, fieldOfViewSharpness) * dt));
            }
        }
    }
}
