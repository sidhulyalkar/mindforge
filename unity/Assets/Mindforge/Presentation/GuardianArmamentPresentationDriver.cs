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

        private void LateUpdate()
        {
            if (combat == null || rig == null) return;
            Vector3 aim = input != null ? input.CurrentAimDirection : transform.forward;
            float sight = auras != null && auras.SightActive && resonance != null ? resonance.Sight : 0f;
            float guard = auras != null && auras.GuardActive && resonance != null ? resonance.Guard : 0f;
            rig.SetCombatState(
                combat.IsGuarding,
                combat.IsAttacking,
                combat.AttackProgress,
                aim,
                sight,
                guard,
                combat.GuardCoverageScale,
                combat.ComboStep);
        }
    }
}
