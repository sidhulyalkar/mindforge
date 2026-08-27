using System.Collections.Generic;
using UnityEngine;
using Mindforge.Combat;

namespace Mindforge.Presentation
{
    /// <summary>
    /// Visual-only Animator contract for a future authored Fractured Signal rig. It
    /// consumes scheduler/vitals events and never drives the boss transform or attacks.
    /// </summary>
    public sealed class FracturedSignalAnimatorBridge : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private FracturedSignalDirector director;
        [SerializeField] private CombatantVitals vitals;

        private readonly HashSet<int> _ints = new HashSet<int>();
        private readonly HashSet<int> _bools = new HashSet<int>();
        private readonly HashSet<int> _triggers = new HashSet<int>();
        private bool _subscribed;

        private static readonly int Phase = Animator.StringToHash("Phase");
        private static readonly int Heavy = Animator.StringToHash("Heavy");
        private static readonly int Telegraph = Animator.StringToHash("Telegraph");
        private static readonly int Fire = Animator.StringToHash("Fire");
        private static readonly int Hit = Animator.StringToHash("Hit");
        private static readonly int PhaseChanged = Animator.StringToHash("PhaseChanged");

        private void Awake()
        {
            if (director == null) director = GetComponent<FracturedSignalDirector>();
            if (vitals == null) vitals = GetComponent<CombatantVitals>();
            FindAnimator();
            Cache();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void FindAnimator()
        {
            if (animator != null) return;
            Animator[] found = GetComponentsInChildren<Animator>(true);
            if (found != null && found.Length > 0) animator = found[0];
        }

        private void Cache()
        {
            _ints.Clear();
            _bools.Clear();
            _triggers.Clear();
            if (animator == null) return;
            animator.applyRootMotion = false;
            foreach (AnimatorControllerParameter p in animator.parameters)
            {
                if (p.type == AnimatorControllerParameterType.Int) _ints.Add(p.nameHash);
                else if (p.type == AnimatorControllerParameterType.Bool) _bools.Add(p.nameHash);
                else if (p.type == AnimatorControllerParameterType.Trigger) _triggers.Add(p.nameHash);
            }
        }

        private void Subscribe()
        {
            if (_subscribed) return;
            if (director != null)
            {
                director.PhaseChanged += OnPhase;
                director.AttackTelegraphed += OnTelegraph;
                director.AttackFired += OnFire;
            }
            if (vitals != null) vitals.Damaged += OnDamaged;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            if (director != null)
            {
                director.PhaseChanged -= OnPhase;
                director.AttackTelegraphed -= OnTelegraph;
                director.AttackFired -= OnFire;
            }
            if (vitals != null) vitals.Damaged -= OnDamaged;
            _subscribed = false;
        }

        private void Update()
        {
            if (animator == null)
            {
                FindAnimator();
                Cache();
                if (animator == null) return;
            }
            animator.applyRootMotion = false;
            if (_ints.Contains(Phase)) animator.SetInteger(Phase, director != null ? director.Phase : 1);
        }

        private void OnPhase(int phase)
        {
            if (animator != null && _ints.Contains(Phase)) animator.SetInteger(Phase, phase);
            Trigger(PhaseChanged);
        }

        private void OnTelegraph(string pattern, int count, bool heavy)
        {
            if (animator != null && _bools.Contains(Heavy)) animator.SetBool(Heavy, heavy);
            Trigger(Telegraph);
        }

        private void OnFire(string pattern, int count, bool heavy)
        {
            if (animator != null && _bools.Contains(Heavy)) animator.SetBool(Heavy, heavy);
            Trigger(Fire);
        }

        private void OnDamaged(DamagePacket packet) => Trigger(Hit);

        private void Trigger(int hash)
        {
            if (animator != null && _triggers.Contains(hash)) animator.SetTrigger(hash);
        }
    }
}
