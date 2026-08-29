using UnityEngine;
using Mindforge.Combat;

namespace Mindforge.World
{
    /// <summary>
    /// Last-resort safety for the bounded showcase world. Physical floors and perimeter
    /// walls are the primary containment; this component only recovers from tunneling,
    /// malformed imported geometry, or an editor-authored hole. It never creates combat,
    /// neural, camera or target-lock actions.
    /// </summary>
    [DefaultExecutionOrder(900)]
    public sealed class GuardianWorldSafety : MonoBehaviour
    {
        [SerializeField] private GuardianMotor motor;
        [SerializeField] private Rigidbody body;
        [SerializeField] private Vector2 xBounds = new Vector2(-37.2f, 37.2f);
        [SerializeField] private Vector2 zBounds = new Vector2(-77.2f, 30.2f);
        [SerializeField] private float recoveryHeight = -3.0f;
        [SerializeField] private float safeSampleInterval = 0.18f;

        private Vector3 _lastSafePosition;
        private Quaternion _lastSafeRotation;
        private float _nextSampleTime;
        private bool _hasSafePosition;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            GuardianMotor guardian = Object.FindObjectOfType<GuardianMotor>(true);
            if (guardian == null || guardian.GetComponent<GuardianWorldSafety>() != null) return;
            guardian.gameObject.AddComponent<GuardianWorldSafety>();
        }

        private void Awake()
        {
            if (motor == null) motor = GetComponent<GuardianMotor>();
            if (body == null) body = GetComponent<Rigidbody>();
            CaptureSafePose();
        }

        private void FixedUpdate()
        {
            if (body == null || motor == null) return;

            Vector3 p = body.position;
            bool escaped = p.y < recoveryHeight ||
                           p.x < xBounds.x || p.x > xBounds.y ||
                           p.z < zBounds.x || p.z > zBounds.y;
            if (escaped)
            {
                Recover();
                return;
            }

            if (motor.IsGrounded && Time.fixedTime >= _nextSampleTime)
            {
                _nextSampleTime = Time.fixedTime + Mathf.Max(0.05f, safeSampleInterval);
                CaptureSafePose();
            }
        }

        private void CaptureSafePose()
        {
            _lastSafePosition = transform.position;
            _lastSafeRotation = transform.rotation;
            _hasSafePosition = true;
        }

        private void Recover()
        {
            Vector3 fallback = _hasSafePosition ? _lastSafePosition : new Vector3(0f, 0.8f, -60f);
            Quaternion rotation = _hasSafePosition ? _lastSafeRotation : Quaternion.identity;
            fallback.x = Mathf.Clamp(fallback.x, xBounds.x + 1f, xBounds.y - 1f);
            fallback.z = Mathf.Clamp(fallback.z, zBounds.x + 1f, zBounds.y - 1f);
            fallback.y = Mathf.Max(0.72f, fallback.y + 0.10f);

            body.position = fallback;
            body.rotation = rotation;
            body.velocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.WakeUp();
            Debug.LogWarning("[Mindforge:WorldSafety] Guardian recovered inside the bounded world after leaving the collision shell.");
        }
    }
}
