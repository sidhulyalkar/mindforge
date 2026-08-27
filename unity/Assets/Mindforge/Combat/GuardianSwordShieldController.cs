using System;
using System.Collections.Generic;
using UnityEngine;
using Mindforge.Presentation;
using Mindforge.SoulWisp;

namespace Mindforge.Combat
{
    public enum GuardStrikeResult
    {
        NotGuarded = 0,
        OutsideCoverage = 1,
        Blocked = 2,
        PerfectGuard = 3,
        GuardBroken = 4,
    }

    public sealed class GuardianShieldHitbox : MonoBehaviour
    {
        [SerializeField] private GuardianSwordShieldController owner;
        [SerializeField] private Collider shieldCollider;

        public void Configure(GuardianSwordShieldController controller, Collider collider)
        {
            owner = controller;
            shieldCollider = collider;
            if (shieldCollider != null) shieldCollider.enabled = false;
        }

        public void SetGuardActive(bool active)
        {
            if (shieldCollider != null) shieldCollider.enabled = active;
        }

        public bool TryResolveProjectile(MindforgeProjectile projectile, Vector3 point)
            => owner != null && owner.TryResolveProjectile(projectile, point);
    }

    /// <summary>
    /// Player-owned sword/shield authority. Conventional input chooses attack, guard,
    /// aim and dodge timing. Accepted neural state may only modulate bounded properties
    /// of an action the player has already chosen.
    /// </summary>
    public sealed class GuardianSwordShieldController : MonoBehaviour
    {
        [SerializeField] private GuardianEquipmentLoadout loadout;
        [SerializeField] private GuardianStamina stamina;
        [SerializeField] private GuardianMotor motor;
        [SerializeField] private AuraBuffController auras;
        [SerializeField] private NeuralFocusResonance resonance;
        [SerializeField] private CombatantVitals vitals;
        [SerializeField] private FluxMeter flux;
        [SerializeField] private CombatTuning tuning;
        [SerializeField] private Transform primaryTarget;
        [SerializeField] private GuardianShieldHitbox shieldHitbox;
        [SerializeField] private GuardianSwordShieldRig rig;
        [SerializeField] private HitStopController hitStop;
        [SerializeField] private LayerMask damageMask = ~0;

        [Header("Sword physics")]
        [SerializeField] private float attackRecoverySeconds = 0.12f;
        [SerializeField] private float sightReachBonus = 0.42f;
        [SerializeField] private float referenceSwingMomentum = 37.5f;
        [SerializeField, Range(0.2f, 1f)] private float attackingMoveMultiplier = 0.78f;

        [Header("Sword light chain")]
        [SerializeField, Range(0.2f, 0.9f)] private float comboQueueOpensAt = 0.48f;
        [SerializeField] private float comboResetSeconds = 0.72f;
        [SerializeField] private float finisherDamageMultiplier = 1.28f;
        [SerializeField] private float finisherPoiseMultiplier = 1.55f;
        [SerializeField] private float finisherStaminaMultiplier = 1.22f;

        [Header("Shield physical stance")]
        [SerializeField, Range(0.2f, 1f)] private float guardMoveMultiplier = 0.70f;
        [SerializeField, Range(0f, 1f)] private float guardStaminaRecoveryMultiplier = 0.34f;
        [SerializeField, Range(0f, 1f)] private float guardBreakDamageLeak = 0.62f;

        [Header("Shield neural modulation")]
        [SerializeField] private float maxGuardCoverageBonus = 0.78f;
        [SerializeField] private float maxGuardAbsorptionBonus = 0.17f;
        [SerializeField] private float maxGuardStabilityBonus = 0.45f;
        [SerializeField] private float maxGuardAngleBonus = 0.20f;
        [SerializeField] private float perfectGuardStaminaMultiplier = 0.45f;
        [SerializeField] private float concordPerfectGuardDamageMultiplier = 1.25f;

        private readonly Collider[] _hits = new Collider[48];
        private readonly HashSet<int> _hitThisSwing = new HashSet<int>();
        private bool _guardHeld;
        private float _guardStartedAt = -999f;
        private float _attackStartedAt = -999f;
        private float _attackEndsAt = -999f;
        private float _attackRecoveryUntil = -999f;
        private float _comboResetAt = -999f;
        private Vector3 _attackAim = Vector3.forward;
        private Vector3 _guardAim = Vector3.forward;
        private int _comboStep;
        private bool _comboQueued;

        public event Action SwordAttackStarted;
        public event Action<int> SwordComboStepStarted;
        public event Action<float, float> SwordHit;
        public event Action<bool> GuardChanged;
        public event Action<float, float> ShieldBlocked;
        public event Action PerfectGuard;
        public event Action GuardBroken;
        public event Action<GuardStrikeResult, float, float> MeleeGuardResolved;

        public bool IsGuarding => _guardHeld;
        public bool IsAttacking => Time.time < _attackEndsAt;
        public bool CanDodge => !IsAttacking && Time.time >= _attackRecoveryUntil;
        public float MovementMultiplier => IsGuarding ? guardMoveMultiplier : IsAttacking ? attackingMoveMultiplier : 1f;
        public int ComboStep => _comboStep;
        public float AttackProgress
        {
            get
            {
                if (!IsAttacking) return 0f;
                return Mathf.Clamp01((Time.time - _attackStartedAt) / Mathf.Max(0.01f, _attackEndsAt - _attackStartedAt));
            }
        }
        public float SightResonance => auras != null && auras.SightActive && resonance != null ? resonance.Sight : 0f;
        public float GuardResonance => auras != null && auras.GuardActive && resonance != null ? resonance.Guard : 0f;
        public float GuardCoverageScale => 1f + Mathf.Clamp01(GuardResonance) * maxGuardCoverageBonus;

        private void Awake()
        {
            if (loadout == null) loadout = GetComponent<GuardianEquipmentLoadout>();
            if (stamina == null) stamina = GetComponent<GuardianStamina>();
            if (motor == null) motor = GetComponent<GuardianMotor>();
            if (auras == null) auras = GetComponent<AuraBuffController>();
            if (vitals == null) vitals = GetComponent<CombatantVitals>();
        }

        public void ConfigureRuntime(
            NeuralFocusResonance focus,
            FluxMeter fluxMeter,
            Transform target,
            GuardianShieldHitbox hitbox,
            HitStopController stop,
            CombatTuning combatTuning = null)
        {
            resonance = focus;
            flux = fluxMeter;
            primaryTarget = target;
            shieldHitbox = hitbox;
            hitStop = stop;
            if (combatTuning != null) tuning = combatTuning;
        }

        private void OnDisable()
        {
            _guardHeld = false;
            _comboQueued = false;
            shieldHitbox?.SetGuardActive(false);
            stamina?.SetRecoveryMultiplier(1f);
        }

        public bool TryLightAttack(Vector3 aimDirection)
        {
            WeaponSpec weapon = loadout != null ? loadout.MainHand : null;
            if (weapon == null || stamina == null || _guardHeld || (motor != null && motor.IsDashing)) return false;

            Vector3 aim = Vector3.ProjectOnPlane(aimDirection, Vector3.up);
            if (aim.sqrMagnitude < 0.01f) aim = transform.forward;
            aim.Normalize();

            if (IsAttacking)
            {
                if (_comboStep < 3 && AttackProgress >= Mathf.Clamp01(comboQueueOpensAt))
                {
                    _comboQueued = true;
                    _attackAim = aim;
                    return true;
                }
                return false;
            }

            if (Time.time < _attackRecoveryUntil) return false;
            if (Time.time > _comboResetAt) _comboStep = 0;
            int next = Mathf.Clamp(_comboStep + 1, 1, 3);
            return BeginSwordStep(next, aim, weapon);
        }

        private bool BeginSwordStep(int step, Vector3 aim, WeaponSpec weapon)
        {
            float staminaMultiplier = step == 3 ? Mathf.Max(1f, finisherStaminaMultiplier) : step == 2 ? 1.06f : 1f;
            float staminaCost = weapon.staminaCost * staminaMultiplier;
            if (!stamina.TrySpend(staminaCost, "SWORD_LIGHT"))
            {
                _comboQueued = false;
                _comboStep = 0;
                return false;
            }

            _comboStep = Mathf.Clamp(step, 1, 3);
            _comboQueued = false;
            _attackAim = aim.sqrMagnitude > 0.01f ? aim.normalized : transform.forward;
            _attackStartedAt = Time.time;
            float durationMultiplier = _comboStep == 1 ? 0.92f : _comboStep == 2 ? 0.96f : 1.10f;
            float duration = Mathf.Max(0.08f, weapon.lightAttackSeconds * durationMultiplier);
            _attackEndsAt = _attackStartedAt + duration;
            _attackRecoveryUntil = _attackEndsAt + Mathf.Max(0f, attackRecoverySeconds) * (_comboStep == 3 ? 1.55f : 1f);
            _comboResetAt = _attackRecoveryUntil + Mathf.Max(0.1f, comboResetSeconds);
            _hitThisSwing.Clear();
            SwordAttackStarted?.Invoke();
            SwordComboStepStarted?.Invoke(_comboStep);
            return true;
        }

        public void SetGuardHeld(bool held, Vector3 aimDirection)
        {
            Vector3 aim = Vector3.ProjectOnPlane(aimDirection, Vector3.up);
            if (aim.sqrMagnitude > 0.01f) _guardAim = aim.normalized;

            bool canGuard = held && !IsAttacking && (motor == null || !motor.IsDashing) && stamina != null && stamina.Value > 0.01f;
            if (_guardHeld == canGuard) return;
            _guardHeld = canGuard;
            if (_guardHeld)
            {
                _guardStartedAt = Time.time;
                _comboStep = 0;
                _comboQueued = false;
            }
            shieldHitbox?.SetGuardActive(_guardHeld);
            stamina?.SetRecoveryMultiplier(_guardHeld ? guardStaminaRecoveryMultiplier : 1f);
            GuardChanged?.Invoke(_guardHeld);
        }

        private void FixedUpdate()
        {
            if (IsAttacking)
            {
                ResolveSwordSweep();
            }
            else if (_comboQueued)
            {
                WeaponSpec weapon = loadout != null ? loadout.MainHand : null;
                if (weapon != null && _comboStep < 3)
                    BeginSwordStep(_comboStep + 1, _attackAim, weapon);
                else
                    _comboQueued = false;
            }

            if (_guardHeld && (stamina == null || stamina.Value <= 0.001f)) BreakGuard();

            if (rig != null)
            {
                rig.SetCombatState(
                    _guardHeld,
                    IsAttacking,
                    AttackProgress,
                    IsAttacking ? _attackAim : _guardAim,
                    SightResonance,
                    GuardResonance,
                    GuardCoverageScale,
                    _comboStep);
            }
        }

        private void ResolveSwordSweep()
        {
            WeaponSpec weapon = loadout != null ? loadout.MainHand : null;
            if (weapon == null) return;
            float duration = Mathf.Max(0.08f, _attackEndsAt - _attackStartedAt);
            float progress = AttackProgress;

            // Contact is intentionally narrower than the full animation. Wind-up and
            // recovery remain punishable instead of becoming invisible hit frames.
            const float activeStart = 0.24f;
            const float activeEnd = 0.72f;
            if (progress < activeStart || progress > activeEnd) return;

            float activeT = Mathf.InverseLerp(activeStart, activeEnd, progress);
            float sweepMultiplier = _comboStep == 3 ? 1.16f : _comboStep == 2 ? 1.04f : 1f;
            float sweepDegrees = weapon.sweepDegrees * sweepMultiplier;
            float from = -sweepDegrees * 0.5f;
            float to = sweepDegrees * 0.5f;
            if (_comboStep == 2)
            {
                from = sweepDegrees * 0.5f;
                to = -sweepDegrees * 0.5f;
            }
            float angle = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, activeT));
            Vector3 bladeDirection = Quaternion.AngleAxis(angle, Vector3.up) * _attackAim;
            float resonanceValue = Mathf.Clamp01(SightResonance);
            float reach = weapon.reachMeters * (1f + sightReachBonus * resonanceValue) * (_comboStep == 3 ? 1.08f : 1f);
            Vector3 root = transform.position + Vector3.up * 0.58f;
            Vector3 tip = root + bladeDirection.normalized * reach;
            float radius = Mathf.Max(0.05f, weapon.bladeRadius) * (_comboStep == 3 ? 1.18f : 1f);
            int count = Physics.OverlapCapsuleNonAlloc(root, tip, radius, _hits, damageMask, QueryTriggerInteraction.Collide);

            float angularVelocity = Mathf.Deg2Rad * Mathf.Max(1f, sweepDegrees) / duration;
            float swingMomentum = Mathf.Max(0.01f, weapon.massKg) * Mathf.Max(0.25f, reach) * angularVelocity;
            float impactScale = Mathf.Clamp(swingMomentum / Mathf.Max(1f, referenceSwingMomentum), 0.78f, 1.28f);
            float neuralMultiplier = auras != null && auras.SightActive
                ? 1f + Mathf.Max(0f, auras.sightDamageMultiplier - 1f) * resonanceValue
                : 1f;
            float comboDamage = _comboStep == 1 ? 0.92f : _comboStep == 2 ? 1f : Mathf.Max(1f, finisherDamageMultiplier);
            float comboPoise = _comboStep == 1 ? 0.90f : _comboStep == 2 ? 1f : Mathf.Max(1f, finisherPoiseMultiplier);
            float baseDamage = weapon.baseDamage * impactScale * comboDamage;
            float damage = baseDamage * neuralMultiplier;
            float bonusDamage = Mathf.Max(0f, damage - baseDamage);
            float poise = weapon.poiseDamage * Mathf.Lerp(0.88f, 1.18f, impactScale) * comboPoise;
            float impulse = Mathf.Clamp(swingMomentum * 0.18f * (_comboStep == 3 ? 1.35f : 1f), 3f, 11.5f);

            for (int i = 0; i < count; i++)
            {
                CombatantVitals receiver = _hits[i].GetComponentInParent<CombatantVitals>();
                if (receiver == null || receiver.Team == CombatTeam.Guardian || !receiver.IsAlive) continue;
                if (!_hitThisSwing.Add(receiver.GetInstanceID())) continue;
                Vector3 delta = receiver.transform.position - transform.position;
                delta.y = 0f;
                receiver.ReceiveDamage(new DamagePacket(
                    damage,
                    poise,
                    delta.sqrMagnitude > 0.01f ? delta.normalized * impulse : bladeDirection.normalized * impulse,
                    _hits[i].ClosestPoint(tip),
                    CombatTeam.Guardian,
                    _comboStep == 3,
                    bonusDamage > 0f ? "SIGHT_SWORD_DAMAGE" : null,
                    bonusDamage));
                hitStop?.Pulse(tuning != null ? (_comboStep == 3 ? tuning.heavyHitStop : tuning.lightHitStop) : (_comboStep == 3 ? 0.055f : 0.02f));
                SwordHit?.Invoke(damage, bonusDamage);
            }
        }

        public bool TryResolveProjectile(MindforgeProjectile projectile, Vector3 point)
        {
            if (!_guardHeld || projectile == null || !projectile.IsHostileToGuardian || stamina == null) return false;
            ShieldSpec shield = loadout != null ? loadout.OffHand : null;
            if (shield == null) return false;

            float guard = Mathf.Clamp01(GuardResonance);
            float stability = Mathf.Max(0.05f, shield.stability * (1f + maxGuardStabilityBonus * guard));
            float staminaCost = Mathf.Max(1f, projectile.Damage * shield.guardStaminaScale / stability);
            bool perfect = IsPerfectGuardWindow(shield);
            if (perfect) staminaCost *= Mathf.Clamp(perfectGuardStaminaMultiplier, 0.1f, 1f);

            if (!stamina.TrySpend(staminaCost, perfect ? "PERFECT_GUARD" : "SHIELD_BLOCK"))
            {
                BreakGuard();
                return false;
            }

            if (perfect && primaryTarget != null)
            {
                float baselineDamage = tuning != null ? tuning.reflectedDamage : Mathf.Max(18f, projectile.Damage);
                float baselinePoise = tuning != null ? tuning.reflectedPoise : 18f;
                bool concord = auras != null && auras.ConcordActive;
                float concordMultiplier = concord ? Mathf.Max(1f, concordPerfectGuardDamageMultiplier) : 1f;
                float reflectedDamage = baselineDamage * concordMultiplier;
                float reflectedPoise = baselinePoise * concordMultiplier;
                float concordBonusDamage = concord ? Mathf.Max(0f, reflectedDamage - baselineDamage) : 0f;
                projectile.ReflectTowards(
                    primaryTarget,
                    tuning != null ? tuning.bloomReleaseSpeed : 20f,
                    reflectedDamage,
                    reflectedPoise,
                    0,
                    concord ? "CONCORD_COUNTER_DAMAGE" : null,
                    concordBonusDamage);
                flux?.Award(tuning != null ? tuning.counterFlux : 0.45f, "Perfect Shield Guard");
                hitStop?.Pulse(tuning != null ? tuning.parryHitStop : 0.02f);
                PerfectGuard?.Invoke();
                return true;
            }

            float absorption = Mathf.Clamp01(shield.baseDamageAbsorption + maxGuardAbsorptionBonus * guard);
            float chip = Mathf.Max(0f, projectile.Damage * (1f - absorption));
            ApplyShieldChip(chip, point, false);
            projectile.ConsumeByShield();
            ShieldBlocked?.Invoke(projectile.Damage, chip);
            return true;
        }

        /// <summary>
        /// Resolves a telegraphed direct boss strike against the player's current
        /// physical shield stance. Facing matters: a raised shield does not protect a
        /// rear flank. This is called by boss melee authority, never by neural input.
        /// </summary>
        public GuardStrikeResult TryResolveIncomingStrike(
            float incomingDamage,
            float incomingPoise,
            Vector3 attackerPosition,
            Vector3 hitPoint,
            bool heavy)
        {
            if (!_guardHeld || stamina == null) return GuardStrikeResult.NotGuarded;
            ShieldSpec shield = loadout != null ? loadout.OffHand : null;
            if (shield == null) return GuardStrikeResult.NotGuarded;

            Vector3 towardThreat = Vector3.ProjectOnPlane(attackerPosition - transform.position, Vector3.up);
            if (towardThreat.sqrMagnitude < 0.001f) towardThreat = _guardAim;
            towardThreat.Normalize();
            Vector3 guardFacing = Vector3.ProjectOnPlane(_guardAim, Vector3.up);
            if (guardFacing.sqrMagnitude < 0.001f) guardFacing = transform.forward;
            guardFacing.Normalize();

            float guard = Mathf.Clamp01(GuardResonance);
            float coverage = Mathf.Clamp(shield.coverageDegrees * (1f + maxGuardAngleBonus * guard), 30f, 175f);
            if (Vector3.Angle(guardFacing, towardThreat) > coverage * 0.5f)
                return GuardStrikeResult.OutsideCoverage;

            float stability = Mathf.Max(0.05f, shield.stability * (1f + maxGuardStabilityBonus * guard));
            float pressure = Mathf.Max(0f, incomingDamage) + Mathf.Max(0f, incomingPoise) * 0.35f;
            float staminaCost = Mathf.Max(1f, pressure * shield.guardStaminaScale / stability);
            bool perfect = IsPerfectGuardWindow(shield);
            if (perfect) staminaCost *= Mathf.Clamp(perfectGuardStaminaMultiplier, 0.1f, 1f);

            if (!stamina.TrySpend(staminaCost, perfect ? "PERFECT_GUARD_MELEE" : "SHIELD_BLOCK_MELEE"))
            {
                float leaked = Mathf.Max(0f, incomingDamage) * Mathf.Clamp01(guardBreakDamageLeak);
                ApplyShieldChip(leaked, hitPoint, heavy);
                BreakGuard();
                MeleeGuardResolved?.Invoke(GuardStrikeResult.GuardBroken, incomingDamage, leaked);
                return GuardStrikeResult.GuardBroken;
            }

            if (perfect)
            {
                CombatantVitals targetVitals = primaryTarget != null ? primaryTarget.GetComponent<CombatantVitals>() : null;
                float retaliationPoise = Mathf.Max(8f, incomingPoise * 0.85f);
                targetVitals?.Poise?.Apply(retaliationPoise);
                flux?.Award(tuning != null ? tuning.counterFlux : 0.45f, "Perfect Melee Guard");
                hitStop?.Pulse(tuning != null ? tuning.parryHitStop : 0.02f);
                PerfectGuard?.Invoke();
                MeleeGuardResolved?.Invoke(GuardStrikeResult.PerfectGuard, incomingDamage, 0f);
                return GuardStrikeResult.PerfectGuard;
            }

            float absorption = Mathf.Clamp01(shield.baseDamageAbsorption + maxGuardAbsorptionBonus * guard);
            float chip = Mathf.Max(0f, incomingDamage * (1f - absorption));
            ApplyShieldChip(chip, hitPoint, heavy);
            ShieldBlocked?.Invoke(incomingDamage, chip);
            MeleeGuardResolved?.Invoke(GuardStrikeResult.Blocked, incomingDamage, chip);
            return GuardStrikeResult.Blocked;
        }

        private bool IsPerfectGuardWindow(ShieldSpec shield)
            => Time.time - _guardStartedAt <= Mathf.Max(0.02f, shield.perfectGuardWindowSeconds);

        private void ApplyShieldChip(float chip, Vector3 point, bool heavy)
        {
            if (chip <= 0.001f || vitals == null) return;
            vitals.ReceiveDamage(new DamagePacket(
                chip,
                0f,
                Vector3.zero,
                point,
                CombatTeam.Enemy,
                heavy));
        }

        private void BreakGuard()
        {
            if (!_guardHeld) return;
            _guardHeld = false;
            shieldHitbox?.SetGuardActive(false);
            stamina?.SetRecoveryMultiplier(1f);
            GuardChanged?.Invoke(false);
            GuardBroken?.Invoke();
        }
    }
}
