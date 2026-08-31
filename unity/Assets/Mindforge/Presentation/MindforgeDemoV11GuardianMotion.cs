using System.Collections;
using UnityEngine;
using Mindforge.Combat;

namespace Mindforge.Presentation
{
    /// <summary>
    /// Presentation-only locomotion rig for the procedural V0.11 Guardian shell.
    /// It reads authoritative motor/combat state and rotates visual children only.
    /// Rigidbody motion, collision, stamina, attacks, dodge timing and neural state are untouched.
    /// </summary>
    [DefaultExecutionOrder(760)]
    public sealed class MindforgeDemoV11GuardianMotion : MonoBehaviour
    {
        private GuardianMotor _motor;
        private GuardianSwordShieldController _combat;
        private Transform _visualRoot;
        private Transform _armL;
        private Transform _armR;
        private Transform _legL;
        private Transform _legR;
        private Transform _mantle;
        private Quaternion _armLBase;
        private Quaternion _armRBase;
        private Quaternion _legLBase;
        private Quaternion _legRBase;
        private Quaternion _mantleBase;
        private float _locomotionPhase;
        private bool _ready;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            MindforgeDemoV11Marker marker = Object.FindObjectOfType<MindforgeDemoV11Marker>(true);
            if (marker == null) return;
            GuardianCombatInput input = Object.FindObjectOfType<GuardianCombatInput>(true);
            if (input == null || input.GetComponent<MindforgeDemoV11GuardianMotion>() != null) return;
            input.gameObject.AddComponent<MindforgeDemoV11GuardianMotion>();
        }

        private IEnumerator Start()
        {
            _motor = GetComponent<GuardianMotor>();
            _combat = GetComponent<GuardianSwordShieldController>();

            for (int frame = 0; frame < 180; frame++)
            {
                _visualRoot = transform.Find("V11GuardianVisual");
                if (_visualRoot != null) break;
                yield return null;
            }

            if (_motor == null || _visualRoot == null)
            {
                Debug.LogWarning("[Mindforge:V11GuardianMotion] Guardian visual hierarchy not available; locomotion presentation skipped.");
                yield break;
            }

            _armL = _visualRoot.Find("ArmL");
            _armR = _visualRoot.Find("ArmR");
            _legL = _visualRoot.Find("LegL");
            _legR = _visualRoot.Find("LegR");
            _mantle = _visualRoot.Find("Mantle");

            _armLBase = LocalRotation(_armL);
            _armRBase = LocalRotation(_armR);
            _legLBase = LocalRotation(_legL);
            _legRBase = LocalRotation(_legR);
            _mantleBase = LocalRotation(_mantle);
            _ready = true;
        }

        private void LateUpdate()
        {
            if (!_ready || _motor == null) return;

            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            Vector3 planarVelocity = Vector3.ProjectOnPlane(_motor.Velocity, Vector3.up);
            float speed = planarVelocity.magnitude;
            float speed01 = Mathf.Clamp01(speed / 7.2f);

            // Phase advances from actual traveled speed. Standing still freezes the gait instead
            // of running a decorative clock underneath the player.
            _locomotionPhase += speed * dt * 1.55f;
            float stride = Mathf.Sin(_locomotionPhase) * 30f * speed01;
            bool grounded = _motor.IsGrounded;
            bool dashing = _motor.IsDashing;
            bool attacking = _combat != null && _combat.IsAttacking;

            float armLeftX;
            float armRightX;
            float legLeftX;
            float legRightX;
            float mantleX;

            if (!grounded)
            {
                armLeftX = -18f;
                armRightX = attacking ? 2f : -18f;
                legLeftX = -22f;
                legRightX = -22f;
                mantleX = 18f;
            }
            else if (dashing)
            {
                armLeftX = -42f;
                armRightX = attacking ? 0f : -42f;
                legLeftX = 20f;
                legRightX = 20f;
                mantleX = 28f;
            }
            else
            {
                armLeftX = -stride * 0.68f;
                armRightX = attacking ? 0f : stride * 0.68f;
                legLeftX = stride;
                legRightX = -stride;
                mantleX = 6f + speed01 * 10f;
            }

            float blend = 1f - Mathf.Exp(-14f * dt);
            Apply(_armL, _armLBase, Quaternion.Euler(armLeftX, 0f, 0f), blend);
            Apply(_armR, _armRBase, Quaternion.Euler(armRightX, 0f, 0f), blend);
            Apply(_legL, _legLBase, Quaternion.Euler(legLeftX, 0f, dashing ? -8f : 0f), blend);
            Apply(_legR, _legRBase, Quaternion.Euler(legRightX, 0f, dashing ? 8f : 0f), blend);
            Apply(_mantle, _mantleBase, Quaternion.Euler(mantleX, 0f, 0f), blend);
        }

        private static Quaternion LocalRotation(Transform target)
        {
            return target != null ? target.localRotation : Quaternion.identity;
        }

        private static void Apply(Transform target, Quaternion baseRotation, Quaternion offset, float blend)
        {
            if (target == null) return;
            Quaternion desired = baseRotation * offset;
            target.localRotation = Quaternion.Slerp(target.localRotation, desired, Mathf.Clamp01(blend));
        }
    }
}
