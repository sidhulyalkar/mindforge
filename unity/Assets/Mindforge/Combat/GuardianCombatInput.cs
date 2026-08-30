using UnityEngine;

namespace Mindforge.Combat
{
    /// <summary>
    /// Samples conventional third-person input in Update, latches one-shot actions, then
    /// applies one complete command frame on the authoritative fixed simulation tick.
    ///
    /// Canonical player vocabulary is supplied by GuardianControlProfileV1:
    /// WASD move · Space jump/hover · Shift/RMB evade · F/LMB blade · Q/C/R skills.
    /// Target lock and contextual E interaction are owned by their dedicated conventional
    /// input authorities. Shield hold and player Pulse fire remain retired from the normal map.
    /// Neural evidence never originates movement, target lock, interaction, attack, camera
    /// orbit, aim, jump, hover or dodge commands.
    /// </summary>
    public sealed class GuardianCombatInput : MonoBehaviour
    {
        [SerializeField] private GuardianMotor motor;
        [SerializeField] private GuardianCombatController combat;
        [SerializeField] private GuardianSwordShieldController physicalCombat;
        [SerializeField] private GuardianStamina endurance;
        [SerializeField] private GravityBloomAbility bloom;
        [SerializeField] private GuardianTargetLock targetLock;
        [SerializeField] private Transform aimTarget;
        [SerializeField] private GuardianInputTape inputTape;
        [SerializeField] private GuardianControlProfileV1 controls;

        [Header("Third-person combat heading")]
        [SerializeField] private Camera aimCamera;
        [SerializeField] private float freeAimDistance = 8f;

        [Header("Combat rhythm")]
        [SerializeField] private float dodgeCommandBufferSeconds = 0.15f;

        private Vector2 _move;
        private bool _cleaveLatched;
        private bool _counterLatched;
        private bool _dashLatched;
        private bool _jumpLatched;
        private bool _jumpHeld;
        private bool _bloomLatched;
        private bool _swordAttackLatched;
        private long _fixedInputTick;

        private bool _dodgeCommandQueued;
        private long _dodgeCommandExpiresTick = long.MinValue / 4;
        private Vector3 _dodgeCommandAim = Vector3.forward;

        private Vector3 _currentAimDirection = Vector3.forward;
        private Vector3 _currentAimPoint;

        public bool CombatActionsEnabled { get; private set; } = true;
        public long FixedInputTick => _fixedInputTick;
        public Vector2 CurrentMoveInput => _move;
        public Vector3 CurrentAimDirection => _currentAimDirection;
        public Vector3 CurrentAimPoint => _currentAimPoint;
        public bool PrecisionAimActive { get; private set; }
        public bool TargetLocked => targetLock != null && targetLock.Locked;

        public void SetCombatActionsEnabled(bool enabled)
        {
            CombatActionsEnabled = enabled;
            if (!enabled) ClearDodgeCommand();
        }

        private void Start()
        {
            ResolveDependencies();
            _currentAimPoint = transform.position + transform.forward * freeAimDistance;
        }

        private void OnDisable()
        {
            // Authority suspension must not preserve an edge-trigger from the frame before
            // death/checkpoint/calibration. Otherwise a re-enabled component can issue a
            // phantom jump, dodge or attack on its first fixed command frame.
            _move = Vector2.zero;
            _cleaveLatched = false;
            _counterLatched = false;
            _dashLatched = false;
            _jumpLatched = false;
            _jumpHeld = false;
            _bloomLatched = false;
            _swordAttackLatched = false;
            ClearDodgeCommand();
            motor?.SetMoveInput(Vector2.zero);
            motor?.SetJumpHeld(false);
            physicalCombat?.SetGuardHeld(false, _currentAimDirection);
        }

        private void Update()
        {
            ResolveDependencies();
            if (controls == null) return;

            _move = controls.SampleMovement();
            _jumpLatched |= controls.Pressed(GuardianControlAction.JumpHover);
            _jumpHeld = controls.Held(GuardianControlAction.JumpHover);

            // The advertised defensive vocabulary is Shift/RMB. Ctrl/Alt remain silent
            // compatibility aliases so older habits/tests do not lose control authority.
            _dashLatched |= controls.Pressed(GuardianControlAction.EvadeBoost) ||
                            Input.GetKeyDown(KeyCode.LeftControl) ||
                            Input.GetKeyDown(KeyCode.RightControl) ||
                            Input.GetKeyDown(KeyCode.LeftAlt) ||
                            Input.GetKeyDown(KeyCode.RightAlt);

            _swordAttackLatched |= controls.Pressed(GuardianControlAction.Blade);
            _cleaveLatched |= controls.Pressed(GuardianControlAction.Cleave);
            _counterLatched |= controls.Pressed(GuardianControlAction.Counter);
            _bloomLatched |= controls.Pressed(GuardianControlAction.Bloom);
        }

        private void FixedUpdate()
        {
            ResolveDependencies();
            if (motor == null || combat == null) return;
            _fixedInputTick = GuardianInputTape.FixedTickNow;

            Vector3 liveAim = ResolveAimDirection(out Vector3 liveAimPoint, out bool precisionAim);
            GuardianCommandFrame live = new GuardianCommandFrame
            {
                tick = _fixedInputTick,
                move_x = _move.x,
                move_y = _move.y,
                aim_x = liveAim.x,
                aim_y = liveAim.y,
                aim_z = liveAim.z,
                fire_held = false,
                cleave_down = _cleaveLatched,
                counter_down = _counterLatched,
                dash_down = _dashLatched,
                jump_down = _jumpLatched,
                jump_held = _jumpHeld,
                bloom_down = _bloomLatched,
                sword_attack_down = _swordAttackLatched,
                guard_held = false,
                guard_down = false,
                mount_toggle_down = false,
                mounted_attack_down = false,
                mounted_boost_down = false,
                context_down = false,
            };

            _cleaveLatched = false;
            _counterLatched = false;
            _dashLatched = false;
            _jumpLatched = false;
            _bloomLatched = false;
            _swordAttackLatched = false;

            int fixedHz = Mathf.Max(1, Mathf.RoundToInt(1f / Mathf.Max(0.0001f, Time.fixedDeltaTime)));
            GuardianCommandFrame command = inputTape != null ? inputTape.Resolve(live, fixedHz) : live;

            UpdateResolvedAimPresentation(command, liveAimPoint, precisionAim);
            Apply(command);
        }

        private Vector3 ResolveAimDirection(out Vector3 aimPoint, out bool precisionAim)
        {
            ResolveDependencies();

            if (targetLock != null && targetLock.Locked && targetLock.Target != null)
            {
                aimPoint = targetLock.AimPoint;
                precisionAim = true;
                return targetLock.DirectionFrom(transform.position);
            }

            Camera camera = aimCamera != null ? aimCamera : Camera.main;
            if (camera != null)
            {
                Vector3 direction = Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up);
                if (direction.sqrMagnitude > 0.001f)
                {
                    direction.Normalize();
                    aimPoint = transform.position + direction * Mathf.Max(1f, freeAimDistance);
                    precisionAim = false;
                    return direction;
                }
            }

            if (aimTarget != null)
            {
                Vector3 delta = aimTarget.position - transform.position;
                delta.y = 0f;
                if (delta.sqrMagnitude > 0.01f)
                {
                    aimPoint = aimTarget.position;
                    precisionAim = false;
                    return delta.normalized;
                }
            }

            Vector3 fallback = transform.forward;
            fallback.y = 0f;
            if (fallback.sqrMagnitude < 0.01f) fallback = Vector3.forward;
            fallback.Normalize();
            aimPoint = transform.position + fallback * Mathf.Max(1f, freeAimDistance);
            precisionAim = false;
            return fallback;
        }

        private void UpdateResolvedAimPresentation(
            GuardianCommandFrame command,
            Vector3 liveAimPoint,
            bool livePrecisionAim)
        {
            if (command == null) return;
            Vector3 direction = command.Aim;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.01f) direction = transform.forward;
            if (direction.sqrMagnitude < 0.01f) direction = Vector3.forward;
            direction.Normalize();
            _currentAimDirection = direction;

            bool replay = inputTape != null && inputTape.Mode == GuardianInputTapeMode.Replay;
            if (replay)
            {
                _currentAimPoint = transform.position + direction * freeAimDistance;
                PrecisionAimActive = false;
            }
            else
            {
                _currentAimPoint = liveAimPoint;
                PrecisionAimActive = livePrecisionAim;
            }
        }

        private void Apply(GuardianCommandFrame command)
        {
            if (command == null) return;
            motor.SetMoveInput(command.Move);
            motor.SetJumpHeld(command.jump_held);

            Vector3 aim = command.Aim;
            aim.y = 0f;
            if (aim.sqrMagnitude < 0.01f) aim = transform.forward;
            if (aim.sqrMagnitude < 0.01f) aim = Vector3.forward;
            aim.Normalize();

            // Shield hold is retired. Old replay tapes may still contain guard bits, but
            // this control map deliberately clears the stance every fixed command frame.
            physicalCombat?.SetGuardHeld(false, aim);

            if (!CombatActionsEnabled)
            {
                ClearDodgeCommand();
                if (command.jump_down) motor.RequestJump();
                return;
            }

            // Defensive intent has first refusal. A resolved fixed-tick dodge edge is held
            // briefly through sword commitment instead of disappearing between Update and the
            // first legal locomotion tick. This is input buffering, not an animation cancel:
            // GuardianSwordShieldController.CanDodge still decides when the roll may begin.
            if (command.dash_down)
            {
                QueueDodgeCommand(aim);
                if (TryConsumeQueuedDodge()) return;
                if (_dodgeCommandQueued) return;
            }
            else if (_dodgeCommandQueued)
            {
                if (TryConsumeQueuedDodge()) return;
                if (_dodgeCommandQueued) return;
            }

            if (motor.IsDashing) return;

            if (command.jump_down &&
                (physicalCombat == null || physicalCombat.ActionState == GuardianActionState.Locomotion))
            {
                if (motor.RequestJump()) return;
            }

            if (command.sword_attack_down)
            {
                bool accepted = physicalCombat != null && physicalCombat.TryLightAttack(aim);
                if (accepted) return;
            }

            // Attack recovery remains a real commitment. Specials may not tunnel through
            // the blade's fixed-tick startup/contact/recovery windows.
            if (physicalCombat != null && physicalCombat.ActionState != GuardianActionState.Locomotion)
                return;

            if (command.counter_down && combat.BeginCounter()) return;
            if (command.cleave_down && combat.RiftCleave(aim)) return;
            if (command.bloom_down && bloom != null && bloom.TryActivate()) return;

            // command.fire_held intentionally has no normal-world action. Pulse Shot code
            // remains available for future experiments without occupying the core loop.
        }

        private void QueueDodgeCommand(Vector3 aim)
        {
            float cost = CurrentDodgeCost();
            if (endurance != null && !endurance.CanSpend(cost))
            {
                ClearDodgeCommand();
                return;
            }

            _dodgeCommandQueued = true;
            _dodgeCommandExpiresTick = _fixedInputTick + SecondsToInputTicks(Mathf.Max(0.02f, dodgeCommandBufferSeconds));
            _dodgeCommandAim = aim.sqrMagnitude > 0.01f ? aim.normalized : transform.forward;
            if (_dodgeCommandAim.sqrMagnitude < 0.01f) _dodgeCommandAim = Vector3.forward;
        }

        private bool TryConsumeQueuedDodge()
        {
            if (!_dodgeCommandQueued) return false;
            if (_fixedInputTick > _dodgeCommandExpiresTick)
            {
                ClearDodgeCommand();
                return false;
            }
            if (motor == null) return false;
            if (physicalCombat != null && !physicalCombat.CanDodge) return false;

            float cost = CurrentDodgeCost();
            if (endurance != null && !endurance.CanSpend(cost))
            {
                // A failed resource check is final for this edge. Do not keep the input alive
                // until Endurance regenerates and surprise the player with a delayed roll.
                ClearDodgeCommand();
                return false;
            }

            bool grounded = motor.IsGrounded;
            if (!motor.RequestDash(_dodgeCommandAim))
            {
                // Rejected air-dash/invalid motor state should not become a latent command.
                ClearDodgeCommand();
                return false;
            }

            endurance?.TrySpend(cost, grounded ? "DODGE_ROLL" : "AIR_DASH");
            ClearDodgeCommand();
            return true;
        }

        private float CurrentDodgeCost()
        {
            float cost = endurance != null ? endurance.DodgeBaseCost : 0f;
            if (motor != null && !motor.IsGrounded) cost *= 1.10f;
            return Mathf.Max(0f, cost);
        }

        private int SecondsToInputTicks(float seconds)
        {
            float dt = Mathf.Max(0.0001f, Time.fixedDeltaTime);
            return Mathf.Max(1, Mathf.CeilToInt(Mathf.Max(0f, seconds) / dt));
        }

        private void ClearDodgeCommand()
        {
            _dodgeCommandQueued = false;
            _dodgeCommandExpiresTick = long.MinValue / 4;
            _dodgeCommandAim = Vector3.forward;
        }

        private void ResolveDependencies()
        {
            if (aimCamera == null) aimCamera = Camera.main;
            if (physicalCombat == null) physicalCombat = GetComponent<GuardianSwordShieldController>();
            if (endurance == null) endurance = GetComponent<GuardianStamina>();
            if (targetLock == null) targetLock = GetComponent<GuardianTargetLock>();
            if (inputTape == null) inputTape = UnityEngine.Object.FindObjectOfType<GuardianInputTape>(true);
            if (controls == null) controls = GuardianControlProfileV1.ResolveOrCreate();
        }
    }
}
