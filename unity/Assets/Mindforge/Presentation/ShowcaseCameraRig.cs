using UnityEngine;
using Mindforge.Combat;

namespace Mindforge.Presentation
{
    /// <summary>
    /// Presentation-only tactical camera. It frames player + boss, leads slightly with
    /// Guardian motion, and leaves impact kick/FOV authority to CombatPresentationDirector.
    /// </summary>
    public sealed class ShowcaseCameraRig : MonoBehaviour
    {
        [SerializeField] private Transform guardian;
        [SerializeField] private Transform boss;
        [SerializeField] private GuardianMotor motor;
        [SerializeField] private Camera gameplayCamera;

        [Header("Framing")]
        [SerializeField] private Vector3 baseOffset = new Vector3(0f, 10.8f, -10.6f);
        [SerializeField] private float separationHeight = 0.18f;
        [SerializeField] private float separationDistance = 0.12f;
        [SerializeField] private float guardianFocusWeight = 0.68f;
        [SerializeField] private float motionLeadSeconds = 0.14f;
        [SerializeField] private float positionSmoothSeconds = 0.11f;
        [SerializeField] private float rotationSharpness = 11f;
        [SerializeField] private float maximumExtraDistance = 3.2f;

        private Vector3 _velocity;
        private bool _initialized;

        public void Configure(Transform player, Transform target, GuardianMotor guardianMotor, Camera camera)
        {
            guardian = player;
            boss = target;
            motor = guardianMotor;
            gameplayCamera = camera;
        }

        private void LateUpdate()
        {
            if (guardian == null || boss == null) return;
            float dt = Mathf.Min(0.05f, Time.unscaledDeltaTime);
            if (dt <= 0f) return;

            Vector3 player = guardian.position;
            Vector3 enemy = boss.position;
            Vector3 focus = Vector3.Lerp(enemy, player, Mathf.Clamp01(guardianFocusWeight));
            focus.y = Mathf.Max(player.y, enemy.y * 0.45f) + 0.34f;

            Vector3 lead = motor != null ? Vector3.ProjectOnPlane(motor.Velocity, Vector3.up) * motionLeadSeconds : Vector3.zero;
            lead = Vector3.ClampMagnitude(lead, 1.15f);
            focus += lead;

            float separation = Vector3.Distance(Vector3.ProjectOnPlane(player, Vector3.up), Vector3.ProjectOnPlane(enemy, Vector3.up));
            float extra = Mathf.Clamp((separation - 7f) * separationDistance, 0f, maximumExtraDistance);
            Vector3 offset = baseOffset + new Vector3(0f, Mathf.Clamp((separation - 7f) * separationHeight, 0f, 2.4f), -extra);
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
                Quaternion desiredRotation = Quaternion.LookRotation(look.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    desiredRotation,
                    1f - Mathf.Exp(-Mathf.Max(0.1f, rotationSharpness) * dt));
            }

            if (gameplayCamera != null)
            {
                gameplayCamera.nearClipPlane = 0.08f;
                gameplayCamera.farClipPlane = 120f;
            }
        }
    }
}
