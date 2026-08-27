using UnityEngine;
using Mindforge.Combat;
using Mindforge.Neural;
using Mindforge.SoulWisp;

namespace Mindforge.Telemetry
{
    /// <summary>
    /// Mirrors semantically meaningful game actions onto GameMarker v1. This is the
    /// observable half of the closed loop: external tools can align what Unity did
    /// with what the neural pipeline observed without reading gameplay internals.
    /// </summary>
    public sealed class MindforgeGameMarkerBridge : MonoBehaviour
    {
        [SerializeField] private UdpGameMarkerSender sender;
        [SerializeField] private GuardianMotor motor;
        [SerializeField] private GuardianCombatController combat;
        [SerializeField] private GravityBloomAbility bloom;
        [SerializeField] private AuraBuffController buffs;
        [SerializeField] private FracturedSignalDirector bossDirector;
        [SerializeField] private CombatantVitals bossVitals;
        [SerializeField] private CombatantVitals playerVitals;
        [SerializeField] private FluxMeter flux;
        [SerializeField] private NeuralLinkContingency linkContingency;
        [SerializeField] private ProjectileNearMissSensor nearMissSensor;

        private void OnEnable()
        {
            ResolveReferences();
            if (motor != null) motor.DashStarted += OnDash;
            if (combat != null)
            {
                combat.ActionAccepted += OnCombatAction;
                combat.CombatOutcome += OnCombatOutcome;
            }
            if (bloom != null)
            {
                bloom.Activated += OnBloomActivated;
                bloom.Released += OnBloomReleased;
            }
            if (buffs != null)
            {
                buffs.AuraApplied += OnAuraApplied;
                buffs.ConcordTriggered += OnConcord;
            }
            if (bossDirector != null)
            {
                bossDirector.PhaseChanged += OnBossPhase;
                bossDirector.EchoSpawned += OnEchoSpawned;
                bossDirector.EchoShattered += OnEchoShattered;
            }
            if (bossVitals != null)
            {
                bossVitals.Damaged += OnBossDamaged;
                bossVitals.Died += OnBossDied;
                if (bossVitals.Poise != null) bossVitals.Poise.BrokenEvent += OnSignalBreak;
            }
            if (playerVitals != null)
            {
                playerVitals.Damaged += OnPlayerDamaged;
                playerVitals.Died += OnPlayerDied;
            }
            if (flux != null) flux.Changed += OnFluxChanged;
            if (linkContingency != null) linkContingency.DegradationStateChanged += OnLinkState;
            if (nearMissSensor != null) nearMissSensor.NearMissAwarded += OnNearMiss;
        }

        private void ResolveReferences()
        {
            if (sender == null) sender = Object.FindObjectOfType<UdpGameMarkerSender>(true);
            if (motor == null) motor = Object.FindObjectOfType<GuardianMotor>(true);
            if (combat == null) combat = Object.FindObjectOfType<GuardianCombatController>(true);
            if (bloom == null) bloom = Object.FindObjectOfType<GravityBloomAbility>(true);
            if (buffs == null) buffs = Object.FindObjectOfType<AuraBuffController>(true);
            if (bossDirector == null) bossDirector = Object.FindObjectOfType<FracturedSignalDirector>(true);
            if (flux == null) flux = Object.FindObjectOfType<FluxMeter>(true);
            if (linkContingency == null) linkContingency = Object.FindObjectOfType<NeuralLinkContingency>(true);
            if (nearMissSensor == null) nearMissSensor = Object.FindObjectOfType<ProjectileNearMissSensor>(true);

            CombatantVitals[] vitals = Object.FindObjectsOfType<CombatantVitals>(true);
            foreach (CombatantVitals candidate in vitals)
            {
                if (candidate == null) continue;
                if (candidate.Team == CombatTeam.Guardian && playerVitals == null) playerVitals = candidate;
                if (candidate.Team == CombatTeam.Enemy && bossVitals == null) bossVitals = candidate;
            }
        }

        private void OnDisable()
        {
            if (motor != null) motor.DashStarted -= OnDash;
            if (combat != null)
            {
                combat.ActionAccepted -= OnCombatAction;
                combat.CombatOutcome -= OnCombatOutcome;
            }
            if (bloom != null)
            {
                bloom.Activated -= OnBloomActivated;
                bloom.Released -= OnBloomReleased;
            }
            if (buffs != null)
            {
                buffs.AuraApplied -= OnAuraApplied;
                buffs.ConcordTriggered -= OnConcord;
            }
            if (bossDirector != null)
            {
                bossDirector.PhaseChanged -= OnBossPhase;
                bossDirector.EchoSpawned -= OnEchoSpawned;
                bossDirector.EchoShattered -= OnEchoShattered;
            }
            if (bossVitals != null)
            {
                bossVitals.Damaged -= OnBossDamaged;
                bossVitals.Died -= OnBossDied;
                if (bossVitals.Poise != null) bossVitals.Poise.BrokenEvent -= OnSignalBreak;
            }
            if (playerVitals != null)
            {
                playerVitals.Damaged -= OnPlayerDamaged;
                playerVitals.Died -= OnPlayerDied;
            }
            if (flux != null) flux.Changed -= OnFluxChanged;
            if (linkContingency != null) linkContingency.DegradationStateChanged -= OnLinkState;
            if (nearMissSensor != null) nearMissSensor.NearMissAwarded -= OnNearMiss;
        }

        private int Phase => bossDirector != null ? bossDirector.Phase : 0;

        private void OnDash() => sender?.Emit("PHASE_DASH", "combat_action", bossPhase: Phase);
        private void OnCombatAction(string action) => sender?.Emit(action, "combat_action", bossPhase: Phase);
        private void OnCombatOutcome(string outcome) => sender?.Emit(outcome, "combat_outcome", bossPhase: Phase);
        private void OnAuraApplied(string target) => sender?.Emit("NEURAL_BUFF_APPLIED", "neural_payoff", target: target, bossPhase: Phase);
        private void OnConcord() => sender?.Emit("CONCORD_ESTABLISHED", "neural_payoff", bossPhase: Phase);
        private void OnNearMiss() => sender?.Emit("NEAR_MISS", "combat_outcome", reason: "THREAD_THE_NEEDLE", bossPhase: Phase);
        private void OnEchoSpawned() => sender?.Emit("ECHO_SPAWNED", "boss_phase", bossPhase: Phase);
        private void OnEchoShattered() => sender?.Emit("ECHO_SHATTERED", "combat_outcome", reason: "PLAYER_PRIORITY_TARGET", bossPhase: Phase);

        private void OnPlayerDamaged(DamagePacket packet)
        {
            sender?.Emit(
                "PLAYER_DAMAGED",
                "combat_outcome",
                reason: packet.Heavy ? "HEAVY" : "LIGHT",
                value: Mathf.Max(0f, packet.Damage),
                bossPhase: Phase);
        }

        private void OnBossDamaged(DamagePacket packet)
        {
            sender?.Emit(
                "BOSS_DAMAGED",
                "combat_outcome",
                reason: packet.Heavy ? "HEAVY" : "LIGHT",
                value: Mathf.Max(0f, packet.Damage),
                bossPhase: Phase);
        }

        private void OnBloomActivated(bool concord)
        {
            sender?.Emit(concord ? "TWIN_ECLIPSE_CHARGE" : "GRAVITY_BLOOM_CHARGE", "combat_action", bossPhase: Phase);
        }

        private void OnBloomReleased(bool concord, int captured)
        {
            sender?.Emit(
                concord ? "TWIN_ECLIPSE_RELEASE" : "GRAVITY_BLOOM_RELEASE",
                "combat_outcome",
                value: captured,
                bossPhase: Phase);
        }

        private void OnBossPhase(int phase) => sender?.Emit("BOSS_PHASE", "boss_phase", value: phase, bossPhase: phase);
        private void OnSignalBreak() => sender?.Emit("SIGNAL_BREAK", "combat_outcome", bossPhase: Phase);
        private void OnBossDied() => sender?.Emit("VICTORY", "session", bossPhase: Phase);
        private void OnPlayerDied() => sender?.Emit("DEFEAT", "session", bossPhase: Phase);

        private void OnFluxChanged(float before, float after, string reason)
        {
            sender?.Emit("FLUX_CHANGED", "flux", reason: reason, value: after, bossPhase: Phase);
        }

        private void OnLinkState(bool degraded)
        {
            sender?.Emit(degraded ? "BCI_DEGRADED" : "BCI_RECOVERED", "neural_link", bossPhase: Phase);
        }
    }
}
