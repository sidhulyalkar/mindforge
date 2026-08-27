using UnityEngine;
using Mindforge.Combat;
using Mindforge.SoulWisp;

namespace Mindforge.Presentation
{
    /// <summary>
    /// Presentation-only adapter. It mirrors fixed-authority combat state into the
    /// procedural rig and never calls attack, guard, damage, stamina or neural authority.
    /// </summary>
    public sealed class GuardianArmamentPresentationDriver : MonoBehaviour
    {
        [SerializeField] private GuardianSwordShieldController combat;
        [SerializeField] private GuardianSwordShieldRig rig;
        [SerializeField] private GuardianCombatInput input;
        [SerializeField] private GuardianEquipmentLoadout loadout;
        [SerializeField] private AuraBuffController auras;
        [SerializeField] private NeuralFocusResonance resonance;

        private float _attackStartedAt = -999f;

        public void Configure(
            GuardianSwordShieldController controller,
            GuardianSwordShieldRig armamentRig,
            GuardianCombatInput combatInput,
            GuardianEquipmentLoadout equipment,
            AuraBuffController auraController,
            NeuralFocusResonance focus)
        {
            combat = controller;
            rig = armamentRig;
            input = combatInput;
            loadout = equipment;
            auras = auraController;
            resonance = focus;
        }

        private void OnEnable()
        {
            if (combat != null) combat.SwordAttackStarted += OnSwordAttack;
        }

        private void Start()
        {
            if (combat != null)
            {
                combat.SwordAttackStarted -= OnSwordAttack;
                combat.SwordAttackStarted += OnSwordAttack;
            }
        }

        private void OnDisable()
        {
            if (combat != null) combat.SwordAttackStarted -= OnSwordAttack;
        }

        private void OnSwordAttack() => _attackStartedAt = Time.time;

        private void LateUpdate()
        {
            if (combat == null || rig == null) return;
            float duration = loadout != null && loadout.MainHand != null
                ? Mathf.Max(0.08f, loadout.MainHand.lightAttackSeconds)
                : 0.42f;
            float progress = combat.IsAttacking ? Mathf.Clamp01((Time.time - _attackStartedAt) / duration) : 0f;
            Vector3 aim = input != null ? input.CurrentAimDirection : transform.forward;
            float sight = auras != null && auras.SightActive && resonance != null ? resonance.Sight : 0f;
            float guard = auras != null && auras.GuardActive && resonance != null ? resonance.Guard : 0f;
            rig.SetCombatState(
                combat.IsGuarding,
                combat.IsAttacking,
                progress,
                aim,
                sight,
                guard,
                combat.GuardCoverageScale);
        }
    }
}
