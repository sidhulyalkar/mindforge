using UnityEngine;

namespace Mindforge.Combat
{
    /// <summary>
    /// Samples device input in Update, latches one-shot actions, then applies a
    /// complete command frame on the authoritative fixed simulation tick. The same
    /// command frame can be recorded/replayed by GuardianInputTape.
    ///
    /// Precision aim is player-owned. Mouse movement activates world-space pointer
    /// aim; arrow keys provide a keyboard-only directional aim path. Neural evidence
    /// never originates attack, guard, aim or dodge commands.
    /// </summary>
    public sealed class GuardianCombatInput : MonoBehaviour
    {
        [SerializeField] private GuardianMotor motor;
        [SerializeField] private GuardianCombatController combat;
        [SerializeField] private GuardianSwordShieldController physicalCombat;
        [SerializeField] private GravityBloomAbility bloom;
        [SerializeField] private Transform aimTarget;
        [SerializeField] private GuardianInputTape inputTape;

        [Header("Player-owned aim")]
        [SerializeField] private Camera aimCamera;
        [SerializeField] private bool mouseAimEnabled = true;
        [SerializeField] private float mouseActivationPixels = 2f;
        [SerializeField] private float minimumPointerWorldDistance = 0.35f;

        private Vector2 _move;
        private Vector2 _keyboardAim;
        private bool _fireHeld;
        private bool _cleaveLatched;
        private bool _counterLatched;
        private bool _dashLatched;
        private bool _bloomLatched;
        private bool _swordAttackLatched;
        private bool _guardHeld;
        private bool _guardDownLatched;
        private long _fixedInputTick;

        private Vector3 _pointerScreen;
        private Vector3 _previousPointerScreen;
        private bool _pointerInitialized;
        private bool _mouseAimActive;
        private Vector3 _currentAimDirection = Vector3.forward;
        private Vector3 _currentAimPoint;

        public bool CombatActionsEnabled { get; private set; } = true;
        public long FixedInputTick => _fixedInputTick;
        public Vector3 CurrentAimDirection => _currentAimDirection;
        public Vector3 CurrentAimPoint => _currentAimPoint;
        public bool PrecisionAimActive { get; private set; }

        public void SetCombatActionsEnabled(bool enabled)
        {
            CombatActionsEnabled = enabled;
            if (!enabled) physicalCombat?.SetGuardHeld(false, _currentAimDirection);
        }

        private void Start()
        {
            ResolveTape();
            if (aimCamera == null) aimCamera = Camera.main;
            if (physicalCombat == null) physicalCombat = GetComponent<GuardianSwordShieldController>();
            _currentAimPoint = transform.position + transform.forward * 6f;
        }

        private void Update()
        {
            _move = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            _keyboardAim = new Vector2(
                (Input.GetKey(KeyCode.RightArrow) ? 1f : 0f) - (Input.GetKey(KeyCode.LeftArrow) ? 1f : 0f),
                (Input.GetKey(KeyCode.UpArrow) ? 1f : 0f) - (Input.GetKey(KeyCode.DownArrow) ? 1f : 0f));

            Vector3 pointer = Input.mousePosition;
            if (!_pointerInitialized)
            {
                _pointerInitialized = true;
                _previousPointerScreen = pointer;
            }
            else
            {
                float threshold = Mathf.Max(0f, mouseActivationPixels);
                if ((pointer - _previousPointerScreen).sqrMagnitude >= threshold * threshold)
                    _mouseAimActive = true;
                _previousPointerScreen = pointer;
            }
            _pointerScreen = pointer;

            _swordAttackLatched |= Input.GetMouseButtonDown(0);
            _guardDownLatched |= Input.GetMouseButtonDown(1);
            _guardHeld = Input.GetMouseButton(1);
            _fireHeld = Input.GetKey(KeyCode.Space);
            _cleaveLatched |= Input.GetKeyDown(KeyCode.F);
            _counterLatched |= Input.GetKeyDown(KeyCode.C);
            _dashLatched |= Input.GetKeyDown(KeyCode.LeftShift);
            _bloomLatched |= Input.GetKeyDown(KeyCode.R);
        }

        private void FixedUpdate()
        {
            if (motor == null || combat == null) return;
            if (physicalCombat == null) physicalCombat = GetComponent<GuardianSwordShieldController>();
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

            ResolveTape();
            int fixedHz = Mathf.Max(1, Mathf.RoundToInt(1f / Mathf.Max(0.0001f, Time.fixedDeltaTime)));
            GuardianCommandFrame command = inputTape != null ? inputTape.Resolve(live, fixedHz) : live;

            UpdateResolvedAimPresentation(command, liveAimPoint, precisionAim);
            Apply(command);
        }

        private Vector3 ResolveAimDirection(out Vector3 aimPoint, out bool precisionAim)
        {
            Camera camera = aimCamera != null ? aimCamera : Camera.main;

            if (_keyboardAim.sqrMagnitude > 0.01f)
            {
                Vector3 forward = camera != null
                    ? Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up).normalized
                    : Vector3.forward;
                Vector3 right = camera != null
                    ? Vector3.ProjectOnPlane(camera.transform.right, Vector3.up).normalized
                    : Vector3.right;
                Vector3 direction = (right * _keyboardAim.x + forward * _keyboardAim.y).normalized;
                if (direction.sqrMagnitude > 0.01f)
                {
                    aimPoint = transform.position + direction * 6f;
                    precisionAim = true;
                    return direction;
                }
            }

            if (mouseAimEnabled && _mouseAimActive && camera != null)
            {
                Ray ray = camera.ScreenPointToRay(_pointerScreen);
                Plane ground = new Plane(Vector3.up, transform.position);
                if (ground.Raycast(ray, out float distance))
                {
                    Vector3 point = ray.GetPoint(distance);
                    Vector3 delta = point - transform.position;
                    delta.y = 0f;
                    float minimum = Mathf.Max(0.01f, minimumPointerWorldDistance);
                    if (delta.sqrMagnitude >= minimum * minimum)
                    {
                        aimPoint = point;
                        precisionAim = true;
                        return delta.normalized;
                    }
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
            aimPoint = transform.position + fallback * 6f;
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
                _currentAimPoint = transform.position + direction * 6f;
                PrecisionAimActive = true;
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

            // Dodge owns the whole fixed command frame when it succeeds. This makes
            // roll timing a real commitment rather than a free overlay on attacks.
            if (command.dash_down && (physicalCombat == null || physicalCombat.CanDodge))
            {
                physicalCombat?.SetGuardHeld(false, aim);
                if (motor.RequestDash(aim)) return;
            }

            // No attacks or guarding while a roll is already in flight.
            if (motor.IsDashing)
            {
                physicalCombat?.SetGuardHeld(false, aim);
                return;
            }

            physicalCombat?.SetGuardHeld(command.guard_held, aim);

            // Raised shield is a stance. It suppresses simultaneous offensive verbs.
            if (physicalCombat != null && physicalCombat.IsGuarding) return;

            if (command.sword_attack_down) physicalCombat?.TryLightAttack(aim);

            // Sword wind-up/contact/recovery owns subsequent combat input until its
            // commitment ends; the player cannot fire a spell through the animation.
            if (physicalCombat != null && physicalCombat.IsAttacking) return;

            if (command.fire_held) combat.FirePulse(aim);
            if (command.cleave_down) combat.RiftCleave(aim);
            if (command.counter_down) combat.BeginCounter();
            if (command.bloom_down) bloom?.TryActivate();
        }

        private void ResolveTape()
        {
            if (inputTape == null)
                inputTape = Object.FindObjectOfType<GuardianInputTape>(true);
        }
    }
}
