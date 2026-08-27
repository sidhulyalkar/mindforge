using System;
using System.Collections.Generic;
using UnityEngine;
using Mindforge.Combat;

namespace Mindforge.Presentation
{
    /// <summary>
    /// Standardized presentation contract for a production Guardian Animator. The
    /// Animator is visual-only: root motion is disabled and no animation event is
    /// allowed to issue combat authority. Clips consume authoritative state that already
    /// exists on the Guardian.
    /// </summary>
    [DefaultExecutionOrder(500)]
    public sealed class GuardianAnimatorBridge : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private GuardianMotor motor;
        [SerializeField] private GuardianCombatInput input;
        [SerializeField] private GuardianSwordShieldController combat;
        [SerializeField] private CombatantVitals vitals;

        private readonly HashSet<int> _floatParams = new HashSet<int>();
        private readonly HashSet<int> _boolParams = new HashSet<int>();
        private readonly HashSet<int> _intParams = new HashSet<int>();
        private readonly HashSet<int> _triggerParams = new HashSet<int>();
        private bool _subscribed;

        private static readonly int Speed = Animator.StringToHash("Speed");
        private static readonly int MoveX = Animator.StringToHash("MoveX");
        private static readonly int MoveY = Animator.StringToHash("MoveY");
        private static readonly int Attack = Animator.StringToHash("Attack");
        private static readonly int AttackProgress = Animator.StringToHash("AttackProgress");
        private static readonly int ComboStep = Animator.StringToHash("ComboStep");
        private static readonly int Guard = Animator.StringToHash("Guard");
        private static readonly int Dodge = Animator.StringToHash("Dodge");
        private static readonly int SightResonance = Animator.StringToHash("SightResonance");
        private static readonly int GuardResonance = Animator.StringToHash("GuardResonance");
        private static readonly int Hit = Animator.StringToHash("Hit");
        private static readonly int PerfectGuard = Animator.StringToHash("PerfectGuard");
        private static readonly int GuardBreak = Animator.StringToHash("GuardBreak");
        private static readonly int AttackTrigger = Animator.StringToHash("AttackTrigger");

        public void Configure(Animator target)
        {
            animator = target;
            RebuildParameterCache();
        }

        private void Awake()
        {
            Resolve();
            FindAnimator();
            RebuildParameterCache();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
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

        private void FindAnimator()
        {
            if (animator != null) return;
            Animator[] animators = GetComponentsInChildren<Animator>(true);
            for (int i = 0; i < animators.Length; i++)
            {
                if (animators[i] == null) continue;
                animator = animators[i];
                break;
            }
        }

        private void RebuildParameterCache()
        {
            _floatParams.Clear();
            _boolParams.Clear();
            _intParams.Clear();
            _triggerParams.Clear();
            if (animator == null) return;
            animator.applyRootMotion = false;
            AnimatorControllerParameter[] parameters = animator.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                AnimatorControllerParameter parameter = parameters[i];
                switch (parameter.type)
                {
                    case AnimatorControllerParameterType.Float: _floatParams.Add(parameter.nameHash); break;
                    case AnimatorControllerParameterType.Bool: _boolParams.Add(parameter.nameHash); break;
                    case AnimatorControllerParameterType.Int: _intParams.Add(parameter.nameHash); break;
                    case AnimatorControllerParameterType.Trigger: _triggerParams.Add(parameter.nameHash); break;
                }
            }
        }

        private void Subscribe()
        {
            if (_subscribed) return;
            Resolve();
            if (combat != null)
            {
                combat.SwordComboStepStarted += OnSwordStep;
                combat.PerfectGuard += OnPerfectGuard;
                combat.GuardBroken += OnGuardBreak;
            }
            if (motor != null) motor.DashStarted += OnDodge;
            if (vitals != null) vitals.Damaged += OnHit;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            if (combat != null)
            {
                combat.SwordComboStepStarted -= OnSwordStep;
                combat.PerfectGuard -= OnPerfectGuard;
                combat.GuardBroken -= OnGuardBreak;
            }
            if (motor != null) motor.DashStarted -= OnDodge;
            if (vitals != null) vitals.Damaged -= OnHit;
            _subscribed = false;
        }

        private void Update()
        {
            Resolve();
            if (animator == null)
            {
                FindAnimator();
                RebuildParameterCache();
                if (animator == null) return;
            }

            animator.applyRootMotion = false;
            Vector3 velocity = motor != null ? Vector3.ProjectOnPlane(motor.Velocity, Vector3.up) : Vector3.zero;
            Vector3 aim = input != null ? Vector3.ProjectOnPlane(input.CurrentAimDirection, Vector3.up) : transform.forward;
            if (aim.sqrMagnitude < 0.01f) aim = transform.forward;
            aim.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, aim).normalized;

            float forward = Vector3.Dot(velocity, aim);
            float strafe = Vector3.Dot(velocity, right);
            float speed = velocity.magnitude;

            SetFloat(Speed, speed, 0.08f);
            SetFloat(MoveX, strafe, 0.08f);
            SetFloat(MoveY, forward, 0.08f);
            SetBool(Attack, combat != null && combat.IsAttacking);
            SetFloat(AttackProgress, combat != null ? combat.AttackProgress : 0f, 0.02f);
            SetInt(ComboStep, combat != null ? combat.ComboStep : 0);
            SetBool(Guard, combat != null && combat.IsGuarding);
            SetFloat(SightResonance, combat != null ? combat.SightResonance : 0f, 0.10f);
            SetFloat(GuardResonance, combat != null ? combat.GuardResonance : 0f, 0.10f);
        }

        private void SetFloat(int hash, float value, float damp)
        {
            if (animator != null && _floatParams.Contains(hash))
                animator.SetFloat(hash, value, Mathf.Max(0f, damp), Time.deltaTime);
        }

        private void SetBool(int hash, bool value)
        {
            if (animator != null && _boolParams.Contains(hash)) animator.SetBool(hash, value);
        }

        private void SetInt(int hash, int value)
        {
            if (animator != null && _intParams.Contains(hash)) animator.SetInteger(hash, value);
        }

        private void SetTrigger(int hash)
        {
            if (animator != null && _triggerParams.Contains(hash)) animator.SetTrigger(hash);
        }

        private void OnSwordStep(int step)
        {
            SetInt(ComboStep, step);
            SetTrigger(AttackTrigger);
        }

        private void OnPerfectGuard() => SetTrigger(PerfectGuard);
        private void OnGuardBreak() => SetTrigger(GuardBreak);
        private void OnDodge() => SetTrigger(Dodge);
        private void OnHit(DamagePacket packet) => SetTrigger(Hit);
    }
}
