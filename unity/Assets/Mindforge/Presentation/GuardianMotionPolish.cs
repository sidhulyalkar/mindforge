using System;
using UnityEngine;
using Mindforge.Combat;

namespace Mindforge.Presentation
{
    /// <summary>
    /// Additive motion polish for the procedural Guardian. This component never owns
    /// movement, hitboxes, damage, stamina or neural authority. It observes the fixed-
    /// tick combat state and adds animation principles on top of the existing visual rig:
    /// anticipation, weight transfer, recoil, foot cadence and recovery.
    /// </summary>
    [DefaultExecutionOrder(450)]
    public sealed class GuardianMotionPolish : MonoBehaviour
    {
        [SerializeField] private GuardianMotor motor;
        [SerializeField] private GuardianCombatInput input;
        [SerializeField] private GuardianSwordShieldController combat;
        [SerializeField] private CombatantVitals vitals;

        [Header("Locomotion cadence")]
        [SerializeField] private float fullStrideReferenceSpeed = 11.2f;
        [SerializeField] private float minimumStrideHz = 1.45f;
        [SerializeField] private float maximumStrideHz = 4.10f;

        private Transform _visualRoot;
        private Transform _bodyMotion;
        private Transform _torsoMotion;
        private Transform _headMotion;
        private Transform _leftArmMotion;
        private Transform _rightArmMotion;
        private Transform _leftLegMotion;
        private Transform _rightLegMotion;
        private Transform _mantleMotion;

        private float _locomotionPhase;
        private float _attackImpulse;
        private float _blockImpulse;
        private float _perfectGuardImpulse;
        private float _guardBreakImpulse;
        private float _hitImpulse;
        private float _dashImpulse;
        private float _lastSpeed;
        private float _turnVelocity;
        private bool _bound;

        private void Awake()
        {
            Resolve();
        }

        private void Start()
        {
            BindProceduralRig();
            Subscribe();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void Resolve()
        {
            if (motor == null) motor = GetComponent<GuardianMotor>();
            if (input == null) input = GetComponent<GuardianCombatInput>();
            if (combat == null) combat = GetComponent<GuardianSwordShieldController>();
            if (vitals == null) vitals = GetComponent<CombatantVitals>();
        }

        private void Subscribe()
        {
            if (combat != null)
            {
                combat.SwordComboStepStarted += OnSwordStep;
                combat.ShieldBlocked += OnShieldBlocked;
                combat.PerfectGuard += OnPerfectGuard;
                combat.GuardBroken += OnGuardBroken;
            }
            if (motor != null) motor.DashStarted += OnDash;
            if (vitals != null) vitals.Damaged += OnDamaged;
        }

        private void Unsubscribe()
        {
            if (combat != null)
            {
                combat.SwordComboStepStarted -= OnSwordStep;
                combat.ShieldBlocked -= OnShieldBlocked;
                combat.PerfectGuard -= OnPerfectGuard;
                combat.GuardBroken -= OnGuardBroken;
            }
            if (motor != null) motor.DashStarted -= OnDash;
            if (vitals != null) vitals.Damaged -= OnDamaged;
        }

        private void BindProceduralRig()
        {
            if (_bound) return;
            _visualRoot = transform.Find("GuardianShowcaseAvatar");
            if (_visualRoot == null) return;

            _bodyMotion = EnsureMotionNode("Motion_Body", _visualRoot, null);
            _torsoMotion = Wrap("Motion_Torso", _visualRoot.Find("Torso"));
            _headMotion = Wrap("Motion_Head", _visualRoot.Find("Head"));
            _leftArmMotion = Wrap("Motion_LeftArm", _visualRoot.Find("LeftArm"));
            _rightArmMotion = Wrap("Motion_RightArm", _visualRoot.Find("RightArm"));
            _leftLegMotion = Wrap("Motion_LeftLeg", _visualRoot.Find("LeftLeg"));
            _rightLegMotion = Wrap("Motion_RightLeg", _visualRoot.Find("RightLeg"));
            _mantleMotion = Wrap("Motion_Mantle", _visualRoot.Find("Mantle"));

            // Reparent primary body pieces under one additive body node while retaining
            // their authored local transforms. The original presentation component still
            // owns the child poses; these wrappers only supply secondary motion.
            Transform[] wrappers =
            {
                _torsoMotion, _headMotion, _leftArmMotion, _rightArmMotion,
                _leftLegMotion, _rightLegMotion, _mantleMotion,
            };
            foreach (Transform wrapper in wrappers)
            {
                if (wrapper != null && wrapper.parent == _visualRoot)
                    wrapper.SetParent(_bodyMotion, true);
            }
            _bound = true;
        }

        private static Transform EnsureMotionNode(string name, Transform parent, Transform source)
        {
            Transform existing = parent.Find(name);
            if (existing != null) return existing;
            GameObject go = new GameObject(name);
            Transform t = go.transform;
            t.SetParent(parent, false);
            if (source != null)
            {
                t.localPosition = source.localPosition;
                t.localRotation = source.localRotation;
                t.localScale = Vector3.one;
            }
            return t;
        }

        private static Transform Wrap(string name, Transform child)
        {
            if (child == null || child.parent == null) return null;
            Transform parent = child.parent;
            Transform wrapper = parent.Find(name);
            if (wrapper != null) return wrapper;

            wrapper = EnsureMotionNode(name, parent, child);
            Vector3 localPosition = child.localPosition;
            Quaternion localRotation = child.localRotation;
            child.SetParent(wrapper, false);
            child.localPosition = Vector3.zero;
            child.localRotation = Quaternion.identity;
            wrapper.localPosition = localPosition;
            wrapper.localRotation = localRotation;
            return wrapper;
        }

        private void LateUpdate()
        {
            Resolve();
            if (!_bound) BindProceduralRig();
            if (!_bound || _bodyMotion == null) return;

            float dt = Mathf.Min(0.05f, Time.unscaledDeltaTime);
            if (dt <= 0f) return;

            Vector3 velocity = motor != null ? Vector3.ProjectOnPlane(motor.Velocity, Vector3.up) : Vector3.zero;
            float speed = velocity.magnitude;
            float move01 = Mathf.Clamp01(speed / Mathf.Max(0.1f, fullStrideReferenceSpeed));
            float acceleration = Mathf.Clamp((speed - _lastSpeed) / Mathf.Max(0.001f, dt), -24f, 24f);
            _lastSpeed = speed;

            // World-space translation now reaches a much faster traversal envelope. Keep
            // visual foot cadence proportional to actual velocity so higher speed reads as
            // an intentional sprint/run rather than procedural moonwalking.
            float strideHz = Mathf.Lerp(
                Mathf.Max(0.1f, minimumStrideHz),
                Mathf.Max(minimumStrideHz, maximumStrideHz),
                move01);
            _locomotionPhase += dt * strideHz * Mathf.PI * 2f;
            float step = Mathf.Sin(_locomotionPhase);
            float doubleStep = Mathf.Abs(Mathf.Sin(_locomotionPhase));

            Vector3 aim = input != null ? input.CurrentAimDirection : transform.forward;
            aim = Vector3.ProjectOnPlane(aim, Vector3.up);
            if (aim.sqrMagnitude < 0.01f) aim = transform.forward;
            Vector3 forward = _visualRoot.forward;
            float signedTurn = Vector3.SignedAngle(forward, aim.normalized, Vector3.up);
            _turnVelocity = Mathf.Lerp(_turnVelocity, Mathf.Clamp(signedTurn / 45f, -1f, 1f), 1f - Mathf.Exp(-8f * dt));

            float attackProgress = combat != null ? combat.AttackProgress : 0f;
            int combo = combat != null ? Mathf.Max(1, combat.ComboStep) : 1;
            bool guarding = combat != null && combat.IsGuarding;
            bool attacking = combat != null && combat.IsAttacking;

            float anticipation = attacking ? Window01(attackProgress, 0.00f, 0.22f) : 0f;
            float contact = attacking ? Window01(attackProgress, 0.20f, 0.58f) : 0f;
            float recovery = attacking ? Window01(attackProgress, 0.58f, 1.00f) : 0f;
            float comboSide = combo == 2 ? -1f : 1f;
            float finisher = combo >= 3 ? 1.35f : 1f;

            _attackImpulse = Damp(_attackImpulse, 0f, 7.5f, dt);
            _blockImpulse = Damp(_blockImpulse, 0f, 10f, dt);
            _perfectGuardImpulse = Damp(_perfectGuardImpulse, 0f, 7f, dt);
            _guardBreakImpulse = Damp(_guardBreakImpulse, 0f, 4.8f, dt);
            _hitImpulse = Damp(_hitImpulse, 0f, 5.8f, dt);
            _dashImpulse = Damp(_dashImpulse, 0f, 4.5f, dt);

            float pelvisBob = doubleStep * 0.030f * move01;
            float lateral = step * 0.032f * move01;
            float accelerationLean = Mathf.Clamp(acceleration * 0.26f, -6.5f, 6.5f);
            float combatLean = -anticipation * 5.5f + contact * 8.5f * finisher - recovery * 2.0f;
            float recoilPitch = -_blockImpulse * 4f - _perfectGuardImpulse * 7f + _guardBreakImpulse * 12f + _hitImpulse * 9f;
            float dashPitch = _dashImpulse * 14f;

            _bodyMotion.localPosition = new Vector3(lateral, pelvisBob - _guardBreakImpulse * 0.055f, 0f);
            _bodyMotion.localRotation = Quaternion.Euler(
                accelerationLean + combatLean + recoilPitch + dashPitch,
                contact * comboSide * 7f * finisher + _turnVelocity * 2.5f,
                -step * 2.2f * move01 - contact * comboSide * 4.5f * finisher + _hitImpulse * 3f);

            if (_torsoMotion != null)
            {
                _torsoMotion.localRotation = Quaternion.Euler(
                    contact * 3f * finisher,
                    -step * 3.0f * move01 + contact * comboSide * 8f,
                    guarding ? -2.5f : _turnVelocity * -1.8f);
            }
            if (_headMotion != null)
            {
                _headMotion.localRotation = Quaternion.Euler(
                    guarding ? -2f : 0f,
                    _turnVelocity * 5.5f - contact * comboSide * 2f,
                    -_hitImpulse * 2.5f);
            }
            if (_leftArmMotion != null)
            {
                _leftArmMotion.localRotation = Quaternion.Euler(
                    guarding ? -6f - _blockImpulse * 9f : step * 2.2f * move01,
                    guarding ? -5f : 0f,
                    guarding ? -4f - _perfectGuardImpulse * 7f : 0f);
            }
            if (_rightArmMotion != null)
            {
                _rightArmMotion.localRotation = Quaternion.Euler(
                    -anticipation * 10f + contact * 6f * finisher,
                    contact * comboSide * 8f,
                    contact * comboSide * 5f);
            }
            if (_leftLegMotion != null)
                _leftLegMotion.localRotation = Quaternion.Euler(step * 3.2f * move01, step * 1.8f * move01, -step * 2.2f * move01);
            if (_rightLegMotion != null)
                _rightLegMotion.localRotation = Quaternion.Euler(-step * 3.2f * move01, -step * 1.8f * move01, step * 2.2f * move01);
            if (_mantleMotion != null)
            {
                float inertia = Mathf.Clamp(speed * 1.15f + _dashImpulse * 10f + contact * 5f, 0f, 18f);
                _mantleMotion.localRotation = Quaternion.Euler(inertia, -_turnVelocity * 5f, step * 2.6f * move01);
            }
        }

        private static float Window01(float t, float start, float end)
        {
            if (t <= start || t >= end) return 0f;
            float x = Mathf.InverseLerp(start, end, t);
            return Mathf.Sin(x * Mathf.PI);
        }

        private static float Damp(float value, float target, float sharpness, float dt)
            => Mathf.Lerp(value, target, 1f - Mathf.Exp(-Mathf.Max(0.01f, sharpness) * dt));

        private void OnSwordStep(int step) => _attackImpulse = step >= 3 ? 1f : 0.7f;
        private void OnShieldBlocked(float incoming, float chip) => _blockImpulse = Mathf.Clamp01(0.45f + incoming / 35f);
        private void OnPerfectGuard() => _perfectGuardImpulse = 1f;
        private void OnGuardBroken() => _guardBreakImpulse = 1f;
        private void OnDash() => _dashImpulse = 1f;
        private void OnDamaged(DamagePacket packet) => _hitImpulse = 1f;
    }
}
