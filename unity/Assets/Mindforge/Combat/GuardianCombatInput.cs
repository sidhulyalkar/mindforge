using UnityEngine;

namespace Mindforge.Combat
{
    /// <summary>
    /// Samples conventional third-person PC input in Update, latches one-shot actions,
    /// then applies one complete command frame on the authoritative fixed simulation tick.
    ///
    /// Third-person map:
    /// - WASD: camera-relative movement
    /// - Mouse/trackpad or arrow keys: orbit camera (handled by ShowcaseCameraRig)
    /// - T: conventional target lock (handled by GuardianTargetLock)
    /// - Space: directional dodge/dash
    /// - F or LMB: sword light/combo/parry
    /// - Left/Right Shift: Pulse Shot
    /// - RMB or E: shield
    /// - Q: Rift Cleave
    /// - C: Counter Pulse
    /// - R: Gravity Bloom / Twin Eclipse
    ///
    /// Free combat heading follows the camera. Locked combat heading follows the locked
    /// enemy. Neural evidence never originates movement, target lock, attack, guard,
    /// camera orbit, aim or dodge commands.
    /// </summary>
    public sealed class GuardianCombatInput : MonoBehaviour
    {
        [SerializeField] private GuardianMotor motor;
        [SerializeField] private GuardianCombatController combat;
        [SerializeField] private GuardianSwordShieldController physicalCombat;
        [SerializeField] private GravityBloomAbility bloom;
        [SerializeField] private GuardianTargetLock targetLock;
        [SerializeField] private Transform aimTarget;
        [SerializeField] private GuardianInputTape inputTape;

        [Header("Third-person combat heading")]
        [SerializeField] private Camera aimCamera;
        [SerializeField] private float freeAimDistance = 8f;

        private Vector2 _move;
        private bool _fireHeld;
        private bool _cleaveLatched;
        private bool _counterLatched;
        private bool _dashLatched;
        private bool _bloomLatched;
        private bool _swordAttackLatched;
        private bool _guardHeld;
        private bool _guardDownLatched;
        private long _fixedInputTick;

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
            if (!enabled) physicalCombat?.SetGuardHeld(false, _currentAimDirection);
        }

        private void Start()
        {
            ResolveDependencies();
            _currentAimPoint = transform.position + transform.forward * freeAimDistance;
        }

        private void Update()
        {
            // WASD is sampled directly so movement never depends on Unity Input Manager
            // axis configuration. Arrow keys are intentionally NOT sampled here; they
            // orbit the third-person camera instead of moving or independently aiming.
            _move = SampleWasdMovement();

            _swordAttackLatched |= Input.GetKeyDown(KeyCode.F) || Input.GetMouseButtonDown(0);
            bool guardPressed = Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(1);
            _guardDownLatched |= guardPressed;
            _guardHeld = Input.GetKey(KeyCode.E) || Input.GetMouseButton(1);
            _fireHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            _cleaveLatched |= Input.GetKeyDown(KeyCode.Q);
            _counterLatched |= Input.GetKeyDown(KeyCode.C);
            _dashLatched |= Input.GetKeyDown(KeyCode.Space);
            _bloomLatched |= Input.GetKeyDown(KeyCode.R);
        }

        private static Vector2 SampleWasdMovement()
        {
            float x = 0f;
            float y = 0f;
            if (Input.GetKey(KeyCode.A)) x -= 1f;
            if (Input.GetKey(KeyCode.D)) x += 1f;
            if (Input.GetKey(KeyCode.S)) y -= 1f;
            if (Input.GetKey(KeyCode.W)) y += 1f;
            return Vector2.ClampMagnitude(new Vector2(x, y), 1f);
        }

        private void FixedUpdate()
        {
            ResolveDependencies();
            if (motor == null || combat == null) return;
            _fixedInputTick++;

            Vector3 liveAim = ResolveAimDirection(out Vector3 liveAimPoint, out bool precisionAim);
            GuardianCommandFrame live = new GuardianCommandFrame
            {
                tick = _fixedInputTick,
                move_x = _move.x,
                move_y = _move.y,
                aim_x = liveAim.x,
                aim_y = liveAim.y,
                aim_z = liveAim.z,
                fire_held = _fireHeld,
                cleave_down = _cleaveLatched,
                counter_down = _counterLatched,
                dash_down = _dashLatched,
                bloom_down = _bloomLatched,
                sword_attack_down = _swordAttackLatched,
                guard_held = _guardHeld,
                guard_down = _guardDownLatched,
            };

            _cleaveLatched = false;
            _counterLatched = false;
            _dashLatched = false;
            _bloomLatched = false;
            _swordAttackLatched = false;
            _guardDownLatched = false;

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

            Vector3 aim = command.Aim;
            aim.y = 0f;
            if (aim.sqrMagnitude < 0.01f) aim = transform.forward;
            if (aim.sqrMagnitude < 0.01f) aim = Vector3.forward;
            aim.Normalize();

            if (!CombatActionsEnabled)
            {
                physicalCombat?.SetGuardHeld(false, aim);
                return;
            }

            // Dodge owns the fixed command frame when accepted. Held WASD has direction
            // priority; when stationary, the camera/lock combat heading is the fallback.
            if (command.dash_down && (physicalCombat == null || physicalCombat.CanDodge))
            {
                physicalCombat?.SetGuardHeld(false, aim);
                if (motor.RequestDash(aim)) return;
            }

            if (motor.IsDashing)
            {
                physicalCombat?.SetGuardHeld(false, aim);
                return;
            }

            physicalCombat?.SetGuardHeld(command.guard_held, aim);
            if (physicalCombat != null && physicalCombat.IsGuarding) return;

            if (command.sword_attack_down) physicalCombat?.TryLightAttack(aim);
            if (physicalCombat != null && physicalCombat.IsAttacking) return;

            if (command.fire_held) combat.FirePulse(aim);
            if (command.cleave_down) combat.RiftCleave(aim);
            if (command.counter_down) combat.BeginCounter();
            if (command.bloom_down) bloom?.TryActivate();
        }

        private void ResolveDependencies()
        {
            if (aimCamera == null) aimCamera = Camera.main;
            if (physicalCombat == null) physicalCombat = GetComponent<GuardianSwordShieldController>();
            if (targetLock == null) targetLock = GetComponent<GuardianTargetLock>();
            if (inputTape == null) inputTape = UnityEngine.Object.FindObjectOfType<GuardianInputTape>(true);
        }
    }
}
