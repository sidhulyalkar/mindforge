using UnityEngine;
using Mindforge.Combat;

namespace Mindforge.Telemetry
{
    /// <summary>
    /// Semantic evidence for the physical combat layer. This bridge observes player
    /// actions/outcomes only and has no path back into combat authority.
    /// </summary>
    public sealed class PhysicalArsenalMarkerBridge : MonoBehaviour
    {
        [SerializeField] private UdpGameMarkerSender sender;
        [SerializeField] private GuardianSwordShieldController combat;
        [SerializeField] private GuardianEquipmentLoadout loadout;
        [SerializeField] private GuardianStamina stamina;
        [SerializeField] private FracturedSignalDirector bossDirector;
        [SerializeField] private FracturedSignalMeleeDirector meleeDirector;

        private int Phase => bossDirector != null ? bossDirector.Phase : 0;

        private void OnEnable()
        {
            Resolve();
            if (combat != null)
            {
                combat.SwordAttackStarted += OnSwordAttack;
                combat.SwordHit += OnSwordHit;
                combat.GuardChanged += OnGuardChanged;
                combat.ShieldBlocked += OnShieldBlocked;
                combat.PerfectGuard += OnPerfectGuard;
                combat.GuardBroken += OnGuardBroken;
            }
            if (meleeDirector != null)
            {
                meleeDirector.MeleeTelegraphed += OnBossMeleeTelegraph;
                meleeDirector.MeleeResolved += OnBossMeleeResolved;
            }
        }

        private void Start()
        {
            sender?.Emit(
                "PHYSICAL_ARSENAL_READY",
                "equipment",
                reason: loadout != null ? loadout.LoadClass.ToString().ToUpperInvariant() : "UNKNOWN",
                value: loadout != null ? loadout.TotalMassKg : 0f,
                bossPhase: Phase);
        }

        private void Resolve()
        {
            if (sender == null) sender = Object.FindObjectOfType<UdpGameMarkerSender>(true);
            if (combat == null) combat = GetComponent<GuardianSwordShieldController>();
            if (loadout == null) loadout = GetComponent<GuardianEquipmentLoadout>();
            if (stamina == null) stamina = GetComponent<GuardianStamina>();
            if (bossDirector == null) bossDirector = Object.FindObjectOfType<FracturedSignalDirector>(true);
            if (meleeDirector == null && bossDirector != null) meleeDirector = bossDirector.GetComponent<FracturedSignalMeleeDirector>();
        }

        private void OnDisable()
        {
            if (combat != null)
            {
                combat.SwordAttackStarted -= OnSwordAttack;
                combat.SwordHit -= OnSwordHit;
                combat.GuardChanged -= OnGuardChanged;
                combat.ShieldBlocked -= OnShieldBlocked;
                combat.PerfectGuard -= OnPerfectGuard;
                combat.GuardBroken -= OnGuardBroken;
            }
            if (meleeDirector != null)
            {
                meleeDirector.MeleeTelegraphed -= OnBossMeleeTelegraph;
                meleeDirector.MeleeResolved -= OnBossMeleeResolved;
            }
        }

        private void OnSwordAttack()
            => sender?.Emit(
                "SWORD_LIGHT",
                "combat_action",
                reason: combat != null ? $"COMBO_{Mathf.Clamp(combat.ComboStep, 1, 3)}" : "COMBO_UNKNOWN",
                value: stamina != null ? stamina.Value : 0f,
                bossPhase: Phase);

        private void OnSwordHit(float damage, float neuralBonus)
            => sender?.Emit(
                "SWORD_HIT",
                "combat_outcome",
                reason: neuralBonus > 0f ? "SIGHT_AMPLIFIED" : "PHYSICAL_ONLY",
                value: Mathf.Max(0f, damage),
                bossPhase: Phase);

        private void OnGuardChanged(bool raised)
            => sender?.Emit(raised ? "SHIELD_RAISED" : "SHIELD_LOWERED", "combat_action", value: stamina != null ? stamina.Value : 0f, bossPhase: Phase);

        private void OnShieldBlocked(float incoming, float chip)
            => sender?.Emit(
                "SHIELD_BLOCK",
                "combat_outcome",
                reason: $"IN_{Mathf.Max(0f, incoming):F2}_CHIP_{Mathf.Max(0f, chip):F2}",
                value: Mathf.Max(0f, chip),
                bossPhase: Phase);

        private void OnPerfectGuard()
            => sender?.Emit("PERFECT_GUARD", "combat_outcome", bossPhase: Phase);

        private void OnGuardBroken()
            => sender?.Emit("GUARD_BROKEN", "combat_outcome", value: stamina != null ? stamina.Value : 0f, bossPhase: Phase);

        private void OnBossMeleeTelegraph(string pattern, Vector3 direction, float range, float arcDegrees, bool heavy)
            => sender?.Emit(
                "BOSS_MELEE_TELEGRAPH",
                "boss_pattern",
                target: pattern,
                reason: heavy ? "HEAVY" : "LIGHT",
                value: Mathf.Max(0f, range),
                bossPhase: Phase);

        private void OnBossMeleeResolved(string pattern, string outcome, float damage)
            => sender?.Emit(
                "BOSS_MELEE_RESOLVED",
                "boss_pattern",
                target: pattern,
                reason: outcome,
                value: Mathf.Max(0f, damage),
                bossPhase: Phase);
    }
}
