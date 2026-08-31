using UnityEngine;
using Mindforge.Combat;

namespace Mindforge.SoulWisp
{
    /// <summary>
    /// Read-only locomotion sensor for neural-window quality control.
    ///
    /// This component is deliberately separated from WispResonanceWindow so the neural
    /// decision state machine never depends on GuardianMotor. It may observe movement facts
    /// and classify an EEG interval as stable/contaminated, but it never changes movement,
    /// Rigidbody state, combat authority, camera authority, target lock or player input.
    /// </summary>
    public sealed class WispMotionQualification : MonoBehaviour
    {
        [SerializeField] private GuardianMotor motor;

        [Header("Initial low-motion EEG contract")]
        [SerializeField] private bool requireLowMotionToArm = true;
        [SerializeField] private bool requireGroundedToArm = true;
        [SerializeField] private bool abortOnMotionDuringEvidence = true;
        [SerializeField] private float maximumArmPlanarSpeed = 0.90f;
        [SerializeField] private float maximumArmVerticalSpeed = 0.55f;
        [SerializeField] private float maximumEvidencePlanarSpeed = 1.40f;
        [SerializeField] private float maximumEvidenceVerticalSpeed = 0.85f;

        public bool MotionQualifiedForArm => Evaluate(arming: true, out _);
        public bool EvidenceQualified => Evaluate(arming: false, out _);
        public string ArmBlockReason
        {
            get
            {
                Evaluate(arming: true, out string reason);
                return reason;
            }
        }
        public string EvidenceBlockReason
        {
            get
            {
                Evaluate(arming: false, out string reason);
                return reason;
            }
        }

        public float PlanarSpeed
        {
            get
            {
                ResolveMotor();
                if (motor == null) return 0f;
                Vector3 velocity = motor.Velocity;
                return new Vector2(velocity.x, velocity.z).magnitude;
            }
        }

        public float VerticalSpeed
        {
            get
            {
                ResolveMotor();
                return motor != null ? Mathf.Abs(motor.Velocity.y) : 0f;
            }
        }

        public bool Grounded
        {
            get
            {
                ResolveMotor();
                return motor != null && motor.IsGrounded;
            }
        }

        public bool Dashing
        {
            get
            {
                ResolveMotor();
                return motor != null && (motor.IsDashing || motor.IsAirDashing);
            }
        }

        public bool Hovering
        {
            get
            {
                ResolveMotor();
                return motor != null && motor.IsHovering;
            }
        }

        private void Awake() => ResolveMotor();
        private void OnEnable() => ResolveMotor();

        public void Bind(GuardianMotor guardianMotor)
        {
            if (guardianMotor != null) motor = guardianMotor;
        }

        public bool TryGetEvidenceInstability(out string reason)
        {
            bool qualified = Evaluate(arming: false, out reason);
            return !qualified;
        }

        private bool Evaluate(bool arming, out string reason)
        {
            ResolveMotor();

            if (arming && !requireLowMotionToArm)
            {
                reason = string.Empty;
                return true;
            }
            if (!arming && !abortOnMotionDuringEvidence)
            {
                reason = string.Empty;
                return true;
            }
            if (motor == null)
            {
                reason = "MOTION_STATE_UNAVAILABLE";
                return false;
            }
            if (motor.IsDashing || motor.IsAirDashing)
            {
                reason = "PLAYER_DASHING";
                return false;
            }
            if (motor.IsHovering)
            {
                reason = "PLAYER_HOVERING";
                return false;
            }
            if (requireGroundedToArm && !motor.IsGrounded)
            {
                reason = "PLAYER_AIRBORNE";
                return false;
            }

            Vector3 velocity = motor.Velocity;
            float planar = new Vector2(velocity.x, velocity.z).magnitude;
            float vertical = Mathf.Abs(velocity.y);
            float maximumPlanar = arming ? maximumArmPlanarSpeed : maximumEvidencePlanarSpeed;
            float maximumVertical = arming ? maximumArmVerticalSpeed : maximumEvidenceVerticalSpeed;

            if (vertical > Mathf.Max(0f, maximumVertical))
            {
                reason = "PLAYER_VERTICAL_MOTION";
                return false;
            }
            if (planar > Mathf.Max(0f, maximumPlanar))
            {
                reason = "PLAYER_MOVING";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        private void ResolveMotor()
        {
            if (motor == null) motor = FindObjectOfType<GuardianMotor>(true);
        }
    }
}
