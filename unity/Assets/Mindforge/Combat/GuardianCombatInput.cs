using UnityEngine;

namespace Mindforge.Combat
{
    public sealed class GuardianCombatInput : MonoBehaviour
    {
        [SerializeField] private GuardianMotor motor;
        [SerializeField] private GuardianCombatController combat;
        [SerializeField] private GravityBloomAbility bloom;
        [SerializeField] private Transform aimTarget;

        public bool CombatActionsEnabled { get; private set; } = true;
        public void SetCombatActionsEnabled(bool enabled) => CombatActionsEnabled = enabled;

        private void Update()
        {
            if (motor == null || combat == null) return;
            Vector2 move = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            motor.SetMoveInput(move);
            Vector3 aim = aimTarget != null ? aimTarget.position - transform.position : transform.forward;
            aim.y = 0f;
            if (aim.sqrMagnitude < 0.01f) aim = transform.forward;
            aim.Normalize();

            // Signal-loss contingency leaves ordinary movement available but prevents
            // a paused boss from becoming a free damage/Flux opportunity.
            if (!CombatActionsEnabled) return;
            if (Input.GetKey(KeyCode.Space)) combat.FirePulse(aim);
            if (Input.GetKeyDown(KeyCode.F)) combat.RiftCleave(aim);
            if (Input.GetKeyDown(KeyCode.C)) combat.BeginCounter();
            if (Input.GetKeyDown(KeyCode.LeftShift)) motor.RequestDash(aim);
            if (Input.GetKeyDown(KeyCode.R)) bloom?.TryActivate();
        }
    }
}
