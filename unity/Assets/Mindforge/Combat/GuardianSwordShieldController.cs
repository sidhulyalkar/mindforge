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

    /// <summary>
    /// Small authoritative action vocabulary. It exists so gameplay can answer action
    /// permission questions without consulting Animator state, animation events or VFX.
    /// </summary>
    public enum GuardianActionState
    {
        Locomotion = 0,
        AttackStartup = 1,
        AttackActive = 2,
        AttackRecovery = 3,
        Guard = 4,
        PerfectGuardRecovery = 5,
        Counter = 6,
        Dodge = 7,
        Stagger = 8,
        GuardBreak = 9,
        Interaction = 10,
        Dead = 11,
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
    /// Player-owned sword/shield authority. Movement, dodge and ordinary sword attacks
    /// are intentionally unlimited for the current competition build. Guard Integrity
    /// remains a defensive pressure budget so holding the shield forever is not free.
    /// Accepted neural state may only modulate bounded properties of an action the
    /// player has already chosen.
    ///
    /// Combat commitment is fixed-tick authoritative. Animation/VFX are presentation
    /// consumers and never grant contact, invulnerability, guarding or combo permission.
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
        [SerializeField] private GuardianTargetLock targetLock;
        [SerializeField] private GuardianShieldHitbox shieldHitbox;
        [SerializeField] private GuardianSwordShieldRig rig;
        [SerializeField] private HitStopController hitStop;
        [SerializeField] private LayerMask damageMask = ~0;

        [Header("Sword physics")]
        [SerializeField] private float sightReachBonus = 0.42f;
        [SerializeField] private float referenceSwingMomentum = 37.5f;

        [Header("Sword light chain · 120 Hz fixed-tick authority")]
        [SerializeField] private AttackDefinition[] lightChain;
        [SerializeField, Min(1)] private int comboResetTicks = 86;

        [Header("Sword projectile parry")]
        [SerializeField] private bool swordParryEnabled = true;
        [SerializeField, Min(1)] private int maxProjectileParriesPerSwing = 4;
        [SerializeField] private float swordParrySpeedMultiplier = 1.18f;
        [SerializeField] private float swordParryDamageMultiplier = 1.10f;
        [SerializeField] private float swordParryPoise = 14f;
        [SerializeField] private float swordParrySightBonus = 0.22f;

        [Header("Shield physical stance")]
        [SerializeField, Range(0.2f, 1f)] private float guardMoveMultiplier = 0.70f;
        [SerializeField, Range(0f, 1f)] private float guardIntegrityRecoveryMultiplier = 0.34f;
        [SerializeField, Range(0f, 1f)] private float guardBreakDamageLeak = 0.62f;
        [SerializeField, Min(1)] private int guardBreakLockTicks = 42;
        [SerializeField, Range(0f, 1f)] private float guardBreakMoveMultiplier = 0.28f;

        [Header("Shield neural modulation")]
        [SerializeField] private float maxGuardCoverageBonus = 0.78f;
        [SerializeField] private float maxGuardAbsorptionBonus = 0.17f;
        [SerializeField] private float maxGuardStabilityBonus = 0.45f;
        [SerializeField] private float maxGuardAngleBonus = 0.20f;
        [SerializeField] private float perfectGuardStaminaMultiplier = 0.45f;
        [SerializeField] private float concordPerfectGuardDamageMultiplier = 1.25f;

        private readonly Collider[] _hits = new Collider[48];
        private readonly HashSet<int> _hitThisSwing = new HashSet<int>();
        private readonly HashSet<int> _parriedProjectilesThisSwing = new HashSet<int>();
        private bool _guardHeld;
        private long _guardStartedTick = long.MinValue / 4;
        private long _guardBreakUntilTick = long.MinValue / 4;
        private long _attackStartedTick = long.MinValue / 4;
        private long _attackCommitEndTick = long.MinValue / 4;
        private long _attackRecoveryUntilTick = long.MinValue / 4;
        private long _comboResetTick = long.MinValue / 4;
        private Vector3 _attackAim = Vector3.forward;
        private Vector3 _guardAim = Vector3.forward;
        private int _comboStep;
        private bool _comboQueued;
        private int _projectileParriesThisSwing;

        public event Action SwordAttackStarted;
        public event Action<int> SwordComboStepStarted;
        public event Action<float, float> SwordHit;
        public event Action<float> SwordProjectileParried;
        public event Action<bool> GuardChanged;
        public event Action<float, float> ShieldBlocked;
        public event Action PerfectGuard;
        public event Action GuardBroken;
        public event Action<GuardStrikeResult, float, float> MeleeGuardResolved;

        private long FixedTick
        {
            get
            {
                float dt = Mathf.Max(0.0001f, Time.fixedDeltaTime);
                return (long)Math.Round(Time.fixedTime / dt);
            }
        }

        private AttackDefinition CurrentAttackDefinition
        {
            get
            {
                EnsureAttackDefinitions();
                int index = _comboStep - 1;
                return index >= 0 && index < lightChain.Length ? lightChain[index] : null;
            }
        }

        private long AttackElapsedTicks => FixedTick - _attackStartedTick;

        public bool IsGuarding => _guardHeld;
        public bool IsAttacking => FixedTick < _attackCommitEndTick;
        public bool IsAttackActive => IsAttacking && CurrentAttackDefinition != null && CurrentAttackDefinition.IsActive(AttackElapsedTicks);
        public bool IsAttackRecovering => !IsAttacking && FixedTick < _attackRecoveryUntilTick;
        public GuardianActionState ActionState => ResolveActionState();
        public bool CanAttack => ActionState == GuardianActionState.Locomotion;
        public bool CanDodge => ActionState == GuardianActionState.Locomotion || ActionState == GuardianActionState.Guard;
        public bool CanGuard => ActionState == GuardianActionState.Locomotion || ActionState == GuardianActionState.Guard;
        public bool CanCounter => ActionState == GuardianActionState.Locomotion;
        public bool CanMove => ActionState != GuardianActionState.Dead;
        public bool CanTurn => ActionState != GuardianActionState.Dead && ActionState != GuardianActionState.GuardBreak;
        public float MovementMultiplier
        {
            get
            {
                if (ActionState == GuardianActionState.GuardBreak) return Mathf.Clamp01(guardBreakMoveMultiplier);
                if (IsGuarding) return guardMoveMultiplier;
                AttackDefinition attack = CurrentAttackDefinition;
                return IsAttacking && attack != null ? attack.MovementMultiplier : 1f;
            }
        }
        public float TurnMultiplier
        {
            get
            {
                AttackDefinition attack = CurrentAttackDefinition;
                return IsAttacking && attack != null ? attack.TurnMultiplier : ActionState == GuardianActionState.GuardBreak ? 0.25f : 1f;
            }
        }
        public int ComboStep => _comboStep;
        public Transform CurrentConventionalTarget => CombatTargetResolver.Resolve(targetLock, primaryTarget);
        public string CurrentAttackId => CurrentAttackDefinition != null ? CurrentAttackDefinition.Id : string.Empty;
        public string CurrentAttackPresentationId => CurrentAttackDefinition != null ? CurrentAttackDefinition.PresentationId : string.Empty;
        public float AttackProgress
        {
            get
            {
                AttackDefinition attack = CurrentAttackDefinition;
                return IsAttacking && attack != null ? attack.AttackProgress(AttackElapsedTicks) : 0f;
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
            if (targetLock == null) targetLock = GetComponent<GuardianTargetLock>();
            EnsureAttackDefinitions();
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
            if (targetLock == null) targetLock = GetComponent<GuardianTargetLock>();
            if (combatTuning != null) tuning = combatTuning;
            EnsureAttackDefinitions();
        }

        public void SetFallbackTarget(Transform target) => primaryTarget = target;

        private void OnDisable()
        {
            _guardHeld = false;
            _comboQueued = false;
            _attackCommitEndTick = long.MinValue / 4;
            _attackRecoveryUntilTick = long.MinValue / 4;
            shieldHitbox?.SetGuardActive(false);
            stamina?.SetRecoveryMultiplier(1f);
        }

        public bool TryLightAttack(Vector3 aimDirection)
        {
            EnsureAttackDefinitions();
            WeaponSpec weapon = loadout != null ? loadout.MainHand : null;
            if (weapon == null || _guardHeld || (motor != null && motor.IsDashing)) return false;

            Vector3 aim = Vector3.ProjectOnPlane(aimDirection, Vector3.up);
            if (aim.sqrMagnitude < 0.01f) aim = transform.forward;
            aim.Normalize();

            if (IsAttacking)
            {
                AttackDefinition current = CurrentAttackDefinition;
                if (_comboStep < lightChain.Length && current != null && current.ComboBufferOpen(AttackElapsedTicks))
                {
                    _comboQueued = true;
                    _attackAim = aim;
                    return true;
                }
                return false;
            }

            if (!CanAttack) return false;
            if (FixedTick > _comboResetTick) _comboStep = 0;
            int next = Mathf.Clamp(_comboStep + 1, 1, lightChain.Length);
            return BeginSwordStep(next, aim, weapon);
        }

        private bool BeginSwordStep(int step, Vector3 aim, WeaponSpec weapon)
        {
            EnsureAttackDefinitions();
            if (weapon == null || lightChain == null || lightChain.Length == 0) return false;
            _comboStep = Mathf.Clamp(step, 1, lightChain.Length);
            AttackDefinition attack = CurrentAttackDefinition;
            if (attack == null) return false;

            _comboQueued = false;
            _attackAim = aim.sqrMagnitude > 0.01f ? aim.normalized : transform.forward;
            _attackStartedTick = FixedTick;
            _attackCommitEndTick = _attackStartedTick + attack.CommitmentTicks;
            _attackRecoveryUntilTick = _attackStartedTick + attack.TotalTicks;
            _comboResetTick = _attackRecoveryUntilTick + Mathf.Max(1, comboResetTicks);
            _hitThisSwing.Clear();
            _parriedProjectilesThisSwing.Clear();
            _projectileParriesThisSwing = 0;
            SwordAttackStarted?.Invoke();
            SwordComboStepStarted?.Invoke(_comboStep);
            return true;
        }

        public void SetGuardHeld(bool held, Vector3 aimDirection)
        {
            Vector3 aim = Vector3.ProjectOnPlane(aimDirection, Vector3.up);
            if (aim.sqrMagnitude > 0.01f) _guardAim = aim.normalized;

            bool canGuard = held && CanGuard && stamina != null && stamina.Value > 0.01f;
            if (_guardHeld == canGuard) return;
            _guardHeld = canGuard;
            if (_guardHeld)
            {
                _guardStartedTick = FixedTick;
                _comboStep = 0;
                _comboQueued = false;
            }
            shieldHitbox?.SetGuardActive(_guardHeld);
            stamina?.SetRecoveryMultiplier(_guardHeld ? guardIntegrityRecoveryMultiplier : 1f);
            GuardChanged?.Invoke(_guardHeld);
        }

        private void FixedUpdate()
        {
            EnsureAttackDefinitions();

            if (IsAttacking)
            {
                ResolveSwordSweep();
            }
            else if (_comboQueued)
            {
                WeaponSpec weapon = loadout != null ? loadout.MainHand : null;
                if (weapon != null && _comboStep < lightChain.Length)
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
            AttackDefinition attack = CurrentAttackDefinition;
            if (weapon == null || attack == null || !attack.IsActive(AttackElapsedTicks)) return;

            float activeT = attack.ActiveProgress(AttackElapsedTicks);
            float sweepDegrees = weapon.sweepDegrees * attack.SweepMultiplier;
            float from = -sweepDegrees * 0.5f;
            float to = sweepDegrees * 0.5f;
            if (attack.ReverseSweep)
            {
                from = sweepDegrees * 0.5f;
                to = -sweepDegrees * 0.5f;
            }
            float angle = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, activeT));
            Vector3 bladeDirection = Quaternion.AngleAxis(angle, Vector3.up) * _attackAim;
            float resonanceValue = Mathf.Clamp01(SightResonance);
            float reach = weapon.reachMeters * (1f + sightReachBonus * resonanceValue) * attack.ReachMultiplier;
            Vector3 root = transform.position + Vector3.up * 0.58f;
            Vector3 tip = root + bladeDirection.normalized * reach;
            float radius = Mathf.Max(0.05f, weapon.bladeRadius) * (attack.Heavy ? 1.18f : 1f);
            int count = Physics.OverlapCapsuleNonAlloc(root, tip, radius, _hits, damageMask, QueryTriggerInteraction.Collide);

            float activeSeconds = Mathf.Max(Time.fixedDeltaTime, attack.ActiveTicks * Time.fixedDeltaTime);
            float angularVelocity = Mathf.Deg2Rad * Mathf.Max(1f, sweepDegrees) / activeSeconds;
            float swingMomentum = Mathf.Max(0.01f, weapon.massKg) * Mathf.Max(0.25f, reach) * angularVelocity;
            float impactScale = Mathf.Clamp(swingMomentum / Mathf.Max(1f, referenceSwingMomentum), 0.78f, 1.28f);
            float neuralMultiplier = auras != null && auras.SightActive
                ? 1f + Mathf.Max(0f, auras.sightDamageMultiplier - 1f) * resonanceValue
                : 1f;
            float baseDamage = weapon.baseDamage * impactScale * attack.DamageMultiplier;
            float damage = baseDamage * neuralMultiplier;
            float bonusDamage = Mathf.Max(0f, damage - baseDamage);
            float poise = weapon.poiseDamage * Mathf.Lerp(0.88f, 1.18f, impactScale) * attack.PoiseMultiplier;
            float impulse = Mathf.Clamp(swingMomentum * 0.18f * attack.KnockbackMultiplier, 3f, 11.5f);

            for (int i = 0; i < count; i++)
            {
                Collider hit = _hits[i];
                if (hit == null) continue;

                MindforgeProjectile projectile = hit.GetComponentInParent<MindforgeProjectile>();
                if (TrySwordParry(projectile, weapon, resonanceValue)) continue;

                CombatantVitals receiver = hit.GetComponentInParent<CombatantVitals>();
                if (receiver == null || receiver.Team == CombatTeam.Guardian || !receiver.IsAlive) continue;
                if (!_hitThisSwing.Add(receiver.GetInstanceID())) continue;
                Vector3 delta = receiver.transform.position - transform.position;
                delta.y = 0f;
                receiver.ReceiveDamage(new DamagePacket(
                    damage,
                    poise,
                    delta.sqrMagnitude > 0.01f ? delta.normalized * impulse : bladeDirection.normalized * impulse,
                    hit.ClosestPoint(tip),
                    CombatTeam.Guardian,
                    attack.Heavy,
                    bonusDamage > 0f ? "SIGHT_SWORD_DAMAGE" : null,
                    bonusDamage));
                hitStop?.Pulse(tuning != null ? (attack.Heavy ? tuning.heavyHitStop : tuning.lightHitStop) : (attack.Heavy ? 0.055f : 0.02f));
                SwordHit?.Invoke(damage, bonusDamage);
            }
        }

        private bool TrySwordParry(MindforgeProjectile projectile, WeaponSpec weapon, float sight)
        {
            if (!swordParryEnabled || projectile == null || !projectile.IsHostileToGuardian) return false;
            if (targetLock == null) targetLock = GetComponent<GuardianTargetLock>();
            Transform target = CombatTargetResolver.Resolve(targetLock, primaryTarget);
            if (target == null) return false;

            int id = projectile.GetInstanceID();
            if (_parriedProjectilesThisSwing.Contains(id)) return true;
            if (_projectileParriesThisSwing >= Mathf.Max(1, maxProjectileParriesPerSwing)) return false;

            _parriedProjectilesThisSwing.Add(id);
            _projectileParriesThisSwing++;
            float baseline = Mathf.Max(projectile.Damage * Mathf.Max(1f, swordParryDamageMultiplier), weapon.baseDamage * 0.72f);
            bool sightActive = auras != null && auras.SightActive;
            float bonus = sightActive ? baseline * Mathf.Max(0f, swordParrySightBonus) * Mathf.Clamp01(sight) : 0f;
            float reflectedDamage = baseline + bonus;
            float speed = Mathf.Max(14f, projectile.Speed * Mathf.Max(1f, swordParrySpeedMultiplier));
            projectile.ReflectTowards(
                target,
                speed,
                reflectedDamage,
                Mathf.Max(1f, swordParryPoise),
                1,
                bonus > 0f ? "SIGHT_SWORD_PARRY_DAMAGE" : null,
                bonus);
            flux?.Award(tuning != null ? tuning.nearMissFlux * 0.75f : 0.12f, "Sword Parry");
            hitStop?.Pulse(tuning != null ? tuning.parryHitStop : 0.02f);
            SwordProjectileParried?.Invoke(reflectedDamage);
            return true;
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

            if (targetLock == null) targetLock = GetComponent<GuardianTargetLock>();
            Transform target = CombatTargetResolver.Resolve(targetLock, primaryTarget);
            if (perfect && target != null)
            {
                float baselineDamage = tuning != null ? tuning.reflectedDamage : Mathf.Max(18f, projectile.Damage);
                float baselinePoise = tuning != null ? tuning.reflectedPoise : 18f;
                bool concord = auras != null && auras.ConcordActive;
                float concordMultiplier = concord ? Mathf.Max(1f, concordPerfectGuardDamageMultiplier) : 1f;
                float reflectedDamage = baselineDamage * concordMultiplier;
                float reflectedPoise = baselinePoise * concordMultiplier;
                float concordBonusDamage = concord ? Mathf.Max(0f, reflectedDamage - baselineDamage) : 0f;
                projectile.ReflectTowards(
                    target,
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
                CombatantVitals nearbyAttacker = CombatTargetResolver.FindEnemyNear(attackerPosition, 2.6f);
                Transform conventional = CombatTargetResolver.Resolve(targetLock, primaryTarget);
                CombatantVitals targetVitals = nearbyAttacker != null
                    ? nearbyAttacker
                    : conventional != null ? conventional.GetComponentInParent<CombatantVitals>() : null;
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
        {
            int perfectTicks = SecondsToTicks(Mathf.Max(0.02f, shield.perfectGuardWindowSeconds));
            return FixedTick - _guardStartedTick <= perfectTicks;
        }

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
            _guardBreakUntilTick = FixedTick + Mathf.Max(1, guardBreakLockTicks);
            shieldHitbox?.SetGuardActive(false);
            stamina?.SetRecoveryMultiplier(1f);
            GuardChanged?.Invoke(false);
            GuardBroken?.Invoke();
        }

        private GuardianActionState ResolveActionState()
        {
            if (vitals != null && !vitals.IsAlive) return GuardianActionState.Dead;
            if (FixedTick < _guardBreakUntilTick) return GuardianActionState.GuardBreak;
            if (motor != null && motor.IsDashing) return GuardianActionState.Dodge;
            if (IsAttacking)
            {
                AttackDefinition attack = CurrentAttackDefinition;
                if (attack != null && attack.IsActive(AttackElapsedTicks)) return GuardianActionState.AttackActive;
                return GuardianActionState.AttackStartup;
            }
            if (IsAttackRecovering) return GuardianActionState.AttackRecovery;
            if (_guardHeld) return GuardianActionState.Guard;
            return GuardianActionState.Locomotion;
        }

        private void EnsureAttackDefinitions()
        {
            if (lightChain != null && lightChain.Length >= 3 &&
                lightChain[0] != null && lightChain[1] != null && lightChain[2] != null)
                return;
            lightChain = AttackDefinition.CreateDefaultLightChain();
        }

        private static int SecondsToTicks(float seconds)
        {
            float dt = Mathf.Max(0.0001f, Time.fixedDeltaTime);
            return Mathf.Max(1, Mathf.CeilToInt(Mathf.Max(0f, seconds) / dt));
        }
    }
}
