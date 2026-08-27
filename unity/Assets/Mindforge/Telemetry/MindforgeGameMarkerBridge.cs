using System.Collections.Generic;
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
        [SerializeField] private float guardHealMarkerInterval = 0.75f;

        private readonly List<CombatantVitals> _observedEchoVitals = new List<CombatantVitals>();
        private float _guardRegenPending;
        private float _guardRegenFlushAt;

        private void OnEnable()
        {
            ResolveReferences();
            if (motor != null) motor.DashStarted += OnDash;
            if (combat != null)
            {
                combat.ActionAccepted += OnCombatAction;
                combat.CombatOutcome += OnCombatOutcome;
                combat.NeuralPayoffObserved += OnNeuralPayoffObserved;
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
                bossDirector.AttackTelegraphed += OnBossAttackTelegraphed;
                bossDirector.AttackFired += OnBossAttackFired;
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
            ObserveEchoVitals();

            // Capability marker makes zero-payoff diagnostics backward compatible.
            // Old session tapes that predate this ledger are not retroactively judged
            // as if they could have emitted realized-payoff evidence.
            sender?.Emit(
                "NEURAL_PAYOFF_LEDGER_READY",
                "neural_payoff",
                reason: "CONSERVATIVE_DIRECT_DAMAGE_AND_HEAL_V1",
                bossPhase: Phase);
        }

        private void Update()
        {
            if (_guardRegenPending > 0f && Time.time >= _guardRegenFlushAt)
                FlushGuardRegen();
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
                if (candidate.Team == CombatTeam.Enemy && candidate.GetComponent<FracturedSignalDirector>() != null)
                    bossVitals = candidate;
            }
        }

        private void ObserveEchoVitals()
        {
            FracturedEchoNode[] echoes = Object.FindObjectsOfType<FracturedEchoNode>(true);
            foreach (FracturedEchoNode echo in echoes)
            {
                CombatantVitals vitals = echo != null ? echo.Vitals : null;
                if (vitals == null || _observedEchoVitals.Contains(vitals)) continue;
                _observedEchoVitals.Add(vitals);
                vitals.Damaged += OnEchoDamaged;
            }
        }

        private void OnDisable()
        {
            FlushGuardRegen();
            if (motor != null) motor.DashStarted -= OnDash;
            if (combat != null)
            {
                combat.ActionAccepted -= OnCombatAction;
                combat.CombatOutcome -= OnCombatOutcome;
                combat.NeuralPayoffObserved -= OnNeuralPayoffObserved;
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
                bossDirector.AttackTelegraphed -= OnBossAttackTelegraphed;
                bossDirector.AttackFired -= OnBossAttackFired;
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
            foreach (CombatantVitals echoVitals in _observedEchoVitals)
                if (echoVitals != null) echoVitals.Damaged -= OnEchoDamaged;
            _observedEchoVitals.Clear();
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

        private void OnEchoSpawned()
        {
            sender?.Emit("ECHO_SPAWNED", "boss_phase", bossPhase: Phase);
            ObserveEchoVitals();
        }

        private void OnEchoShattered() => sender?.Emit("ECHO_SHATTERED", "combat_outcome", reason: "PLAYER_PRIORITY_TARGET", bossPhase: Phase);

        private void OnBossAttackTelegraphed(string pattern, int projectileCount, bool heavy)
        {
            sender?.Emit("BOSS_ATTACK_TELEGRAPH", "boss_pattern", reason: $"{pattern}_{(heavy ? "HEAVY" : "LIGHT")}", value: projectileCount, bossPhase: Phase);
        }

        private void OnBossAttackFired(string pattern, int projectileCount, bool heavy)
        {
            sender?.Emit("BOSS_ATTACK_FIRED", "boss_pattern", reason: $"{pattern}_{(heavy ? "HEAVY" : "LIGHT")}", value: projectileCount, bossPhase: Phase);
        }

        private void OnPlayerDamaged(DamagePacket packet)
        {
            sender?.Emit("PLAYER_DAMAGED", "combat_outcome", reason: packet.Heavy ? "HEAVY" : "LIGHT", value: Mathf.Max(0f, packet.Damage), bossPhase: Phase);
        }

        private void OnBossDamaged(DamagePacket packet)
        {
            sender?.Emit("BOSS_DAMAGED", "combat_outcome", reason: packet.Heavy ? "HEAVY" : "LIGHT", value: Mathf.Max(0f, packet.Damage), bossPhase: Phase);
            EmitNeuralDamageBonus(packet, "boss");
        }

        private void OnEchoDamaged(DamagePacket packet) => EmitNeuralDamageBonus(packet, "echo");

        private void EmitNeuralDamageBonus(DamagePacket packet, string target)
        {
            if (packet.NeuralBonusDamage <= 0f || string.IsNullOrEmpty(packet.NeuralPayoffKind)) return;
            sender?.Emit("NEURAL_DAMAGE_BONUS_REALIZED", "neural_payoff", target: target, reason: packet.NeuralPayoffKind, value: packet.NeuralBonusDamage, bossPhase: Phase);
        }

        private void OnNeuralPayoffObserved(string kind, float value)
        {
            if (value <= 0f || string.IsNullOrEmpty(kind)) return;
            if (kind == "GUARD_REGEN_REALIZED")
            {
                _guardRegenPending += value;
                if (_guardRegenFlushAt <= 0f)
                    _guardRegenFlushAt = Time.time + Mathf.Max(0.1f, guardHealMarkerInterval);
                return;
            }
            sender?.Emit("NEURAL_GUARD_HEAL_REALIZED", "neural_payoff", target: "guardian", reason: kind, value: value, bossPhase: Phase);
        }

        private void FlushGuardRegen()
        {
            if (_guardRegenPending <= 0f) return;
            sender?.Emit("NEURAL_GUARD_HEAL_REALIZED", "neural_payoff", target: "guardian", reason: "GUARD_REGEN_REALIZED", value: _guardRegenPending, bossPhase: Phase);
            _guardRegenPending = 0f;
            _guardRegenFlushAt = 0f;
        }

        private void OnBloomActivated(bool concord) => sender?.Emit(concord ? "TWIN_ECLIPSE_CHARGE" : "GRAVITY_BLOOM_CHARGE", "combat_action", bossPhase: Phase);

        private void OnBloomReleased(bool concord, int captured)
        {
            sender?.Emit(concord ? "TWIN_ECLIPSE_RELEASE" : "GRAVITY_BLOOM_RELEASE", "combat_outcome", value: captured, bossPhase: Phase);
        }

        private void OnBossPhase(int phase) => sender?.Emit("BOSS_PHASE", "boss_phase", value: phase, bossPhase: phase);
        private void OnSignalBreak() => sender?.Emit("SIGNAL_BREAK", "combat_outcome", bossPhase: Phase);

        private void OnBossDied()
        {
            FlushGuardRegen();
            sender?.Emit("VICTORY", "session", bossPhase: Phase);
        }

        private void OnPlayerDied()
        {
            FlushGuardRegen();
            sender?.Emit("DEFEAT", "session", bossPhase: Phase);
        }

        private void OnFluxChanged(float before, float after, string reason) => sender?.Emit("FLUX_CHANGED", "flux", reason: reason, value: after, bossPhase: Phase);
        private void OnLinkState(bool degraded) => sender?.Emit(degraded ? "BCI_DEGRADED" : "BCI_RECOVERED", "neural_link", bossPhase: Phase);
    }
}
