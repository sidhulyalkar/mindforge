using UnityEngine;

namespace Mindforge.Combat
{
    public sealed class GuardianCombatInput : MonoBehaviour
    {
        [SerializeField] private GuardianMotor motor;
        [SerializeField] private GuardianCombatController combat;
        [SerializeField] private GravityBloomAbility bloom;
        [SerializeField] private Transform aimTarget;

        private void Update()
        {
            if (motor == null || combat == null) return;
            Vector2 move = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            motor.SetMoveInput(move);
            Vector3 aim = aimTarget != null ? aimTarget.position - transform.position : transform.forward;
            aim.y = 0f;
            if (aim.sqrMagnitude < 0.01f) aim = transform.forward;
            aim.Normalize();

            if (Input.GetKey(KeyCode.Space)) combat.FirePulse(aim);
            if (Input.GetKeyDown(KeyCode.F)) combat.RiftCleave(aim);
            if (Input.GetKeyDown(KeyCode.C)) combat.BeginCounter();
            if (Input.GetKeyDown(KeyCode.LeftShift)) motor.RequestDash(aim);
            if (Input.GetKeyDown(KeyCode.R)) bloom?.TryActivate();
        }
    }
}
