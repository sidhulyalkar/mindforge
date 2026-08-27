using UnityEngine;

namespace Mindforge.Combat
{
    /// <summary>
    /// Samples device input in Update, latches one-shot actions, then applies a
    /// complete command frame on the authoritative fixed simulation tick. The same
    /// command frame can be recorded/replayed by GuardianInputTape.
    /// </summary>
    public sealed class GuardianCombatInput : MonoBehaviour
    {
        [SerializeField] private GuardianMotor motor;
        [SerializeField] private GuardianCombatController combat;
        [SerializeField] private GravityBloomAbility bloom;
        [SerializeField] private Transform aimTarget;
        [SerializeField] private GuardianInputTape inputTape;

        private Vector2 _move;
        private bool _fireHeld;
        private bool _cleaveLatched;
        private bool _counterLatched;
        private bool _dashLatched;
        private bool _bloomLatched;
        private long _fixedInputTick;

        public bool CombatActionsEnabled { get; private set; } = true;
        public long FixedInputTick => _fixedInputTick;

        public void SetCombatActionsEnabled(bool enabled) => CombatActionsEnabled = enabled;

        private void Start() => ResolveTape();

        private void Update()
        {
            _move = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            _fireHeld = Input.GetKey(KeyCode.Space);
            _cleaveLatched |= Input.GetKeyDown(KeyCode.F);
            _counterLatched |= Input.GetKeyDown(KeyCode.C);
            _dashLatched |= Input.GetKeyDown(KeyCode.LeftShift);
            _bloomLatched |= Input.GetKeyDown(KeyCode.R);
        }

        private void FixedUpdate()
        {
            if (motor == null || combat == null) return;
            _fixedInputTick++;

            Vector3 aim = aimTarget != null ? aimTarget.position - transform.position : transform.forward;
            aim.y = 0f;
            if (aim.sqrMagnitude < 0.01f) aim = transform.forward;
            aim.Normalize();

            GuardianCommandFrame live = new GuardianCommandFrame
            {
                tick = _fixedInputTick,
                move_x = _move.x,
                move_y = _move.y,
                aim_x = aim.x,
                aim_y = aim.y,
                aim_z = aim.z,
                fire_held = _fireHeld,
                cleave_down = _cleaveLatched,
                counter_down = _counterLatched,
                dash_down = _dashLatched,
                bloom_down = _bloomLatched,
            };

            // One-shot device edges are consumed by exactly one fixed command frame.
            _cleaveLatched = false;
            _counterLatched = false;
            _dashLatched = false;
            _bloomLatched = false;

            ResolveTape();
            int fixedHz = Mathf.Max(1, Mathf.RoundToInt(1f / Mathf.Max(0.0001f, Time.fixedDeltaTime)));
            GuardianCommandFrame command = inputTape != null ? inputTape.Resolve(live, fixedHz) : live;
            Apply(command);
        }

        private void Apply(GuardianCommandFrame command)
        {
            if (command == null) return;
            motor.SetMoveInput(command.Move);

            // Signal-loss contingency leaves ordinary movement available but prevents
            // a paused boss from becoming a free damage/Flux opportunity. Replay does
            // not bypass this authority gate.
            if (!CombatActionsEnabled) return;

            Vector3 aim = command.Aim;
            aim.y = 0f;
            if (aim.sqrMagnitude < 0.01f) aim = transform.forward;
            aim.Normalize();

            if (command.fire_held) combat.FirePulse(aim);
            if (command.cleave_down) combat.RiftCleave(aim);
            if (command.counter_down) combat.BeginCounter();
            if (command.dash_down) motor.RequestDash(aim);
            if (command.bloom_down) bloom?.TryActivate();
        }

        private void ResolveTape()
        {
            if (inputTape == null)
                inputTape = Object.FindObjectOfType<GuardianInputTape>(true);
        }
    }
}
