using System;
using System.Collections.Generic;
using UnityEngine;
using Mindforge.Presentation;
using Mindforge.SoulWisp;

namespace Mindforge.Combat
{
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

        [Header("Shield neural modulation")]
        [SerializeField] private float maxGuardCoverageBonus = 0.78f;
        [SerializeField] private float maxGuardAbsorptionBonus = 0.17f;
        [SerializeField] private float maxGuardStabilityBonus = 0.45f;
        [SerializeField] private float perfectGuardStaminaMultiplier = 0.45f;

        private readonly Collider[] _hits = new Collider[48];
        private readonly HashSet<int> _hitThisSwing = new HashSet<int>();
        private bool _guardHeld;
        private float _guardStartedAt = -999f;
        private float _attackStartedAt = -999f;
        private float _attackEndsAt = -999f;
        private float _attackRecoveryUntil = -999f;
        private Vector3 _attackAim = Vector3.forward;
        private Vector3 _guardAim = Vector3.forward;

        public event Action SwordAttackStarted;
        public event Action<float, float> SwordHit;
        public event Action<bool> GuardChanged;
        public event Action<float, float> ShieldBlocked;
        public event Action PerfectGuard;
        public event Action GuardBroken;

        public bool IsGuarding => _guardHeld;
        public bool IsAttacking => Time.time < _attackEndsAt;
        public float SightResonance => auras != null && auras.SightActive && resonance != null ? resonance.Sight : 0f;
        public float GuardResonance => auras != null && auras.GuardActive && resonance != null ? resonance.Guard : 0f;
        public float GuardCoverageScale => 1f + Mathf.Clamp01(GuardResonance) * maxGuardCoverageBonus;

        private void Awake()
        {
            if (loadout == null) loadout = GetComponent<GuardianEquipmentLoadout>();
            if (stamina == null) stamina = GetComponent<GuardianStamina>();
            if (auras == null) auras = GetComponent<AuraBuffController>();
            if (vitals == null) vitals = GetComponent<CombatantVitals>();
        }

        private void OnDisable()
        {
            _guardHeld = false;
            shieldHitbox?.SetGuardActive(false);
        }

        public bool TryLightAttack(Vector3 aimDirection)
        {
            WeaponSpec weapon = loadout != null ? loadout.MainHand : null;
            if (weapon == null || stamina == null || _guardHeld || Time.time < _attackRecoveryUntil) return false;
            if (!stamina.TrySpend(weapon.staminaCost, "SWORD_LIGHT")) return false;

            Vector3 aim = Vector3.ProjectOnPlane(aimDirection, Vector3.up);
            if (aim.sqrMagnitude < 0.01f) aim = transform.forward;
            _attackAim = aim.normalized;
            _attackStartedAt = Time.time;
            _attackEndsAt = _attackStartedAt + Mathf.Max(0.08f, weapon.lightAttackSeconds);
            _attackRecoveryUntil = _attackEndsAt + Mathf.Max(0f, attackRecoverySeconds);
            _hitThisSwing.Clear();
            SwordAttackStarted?.Invoke();
            return true;
        }

        public void SetGuardHeld(bool held, Vector3 aimDirection)
        {
            Vector3 aim = Vector3.ProjectOnPlane(aimDirection, Vector3.up);
            if (aim.sqrMagnitude > 0.01f) _guardAim = aim.normalized;

            bool canGuard = held && !IsAttacking && stamina != null && stamina.Value > 0.01f;
            if (_guardHeld == canGuard) return;
            _guardHeld = canGuard;
            if (_guardHeld) _guardStartedAt = Time.time;
            shieldHitbox?.SetGuardActive(_guardHeld);
            GuardChanged?.Invoke(_guardHeld);
        }

        private void FixedUpdate()
        {
            if (IsAttacking) ResolveSwordSweep();
            if (_guardHeld && (stamina == null || stamina.Value <= 0.001f)) BreakGuard();

            if (rig != null)
            {
                WeaponSpec weapon = loadout != null ? loadout.MainHand : null;
                float attackProgress = IsAttacking && weapon != null
                    ? Mathf.Clamp01((Time.time - _attackStartedAt) / Mathf.Max(0.08f, weapon.lightAttackSeconds))
                    : 0f;
                rig.SetCombatState(
                    _guardHeld,
                    IsAttacking,
                    attackProgress,
                    IsAttacking ? _attackAim : _guardAim,
                    SightResonance,
                    GuardResonance,
                    GuardCoverageScale);
            }
        }

        private void ResolveSwordSweep()
        {
            WeaponSpec weapon = loadout != null ? loadout.MainHand : null;
            if (weapon == null) return;
            float duration = Mathf.Max(0.08f, weapon.lightAttackSeconds);
            float progress = Mathf.Clamp01((Time.time - _attackStartedAt) / duration);

            // Contact is intentionally narrower than the full animation. Wind-up and
            // recovery remain punishable instead of becoming invisible hit frames.
            const float activeStart = 0.24f;
            const float activeEnd = 0.72f;
            if (progress < activeStart || progress > activeEnd) return;

            float activeT = Mathf.InverseLerp(activeStart, activeEnd, progress);
            float angle = Mathf.Lerp(-weapon.sweepDegrees * 0.5f, weapon.sweepDegrees * 0.5f, Mathf.SmoothStep(0f, 1f, activeT));
            Vector3 bladeDirection = Quaternion.AngleAxis(angle, Vector3.up) * _attackAim;
            float resonanceValue = Mathf.Clamp01(SightResonance);
            float reach = weapon.reachMeters * (1f + sightReachBonus * resonanceValue);
            Vector3 root = transform.position + Vector3.up * 0.58f;
            Vector3 tip = root + bladeDirection.normalized * reach;
            float radius = Mathf.Max(0.05f, weapon.bladeRadius);
            int count = Physics.OverlapCapsuleNonAlloc(root, tip, radius, _hits, damageMask, QueryTriggerInteraction.Collide);

            float angularVelocity = Mathf.Deg2Rad * Mathf.Max(1f, weapon.sweepDegrees) / duration;
            float swingMomentum = Mathf.Max(0.01f, weapon.massKg) * Mathf.Max(0.25f, reach) * angularVelocity;
            float impactScale = Mathf.Clamp(swingMomentum / Mathf.Max(1f, referenceSwingMomentum), 0.78f, 1.28f);
            float neuralMultiplier = auras != null && auras.SightActive
                ? 1f + Mathf.Max(0f, auras.sightDamageMultiplier - 1f) * resonanceValue
                : 1f;
            float baseDamage = weapon.baseDamage * impactScale;
            float damage = baseDamage * neuralMultiplier;
            float bonusDamage = Mathf.Max(0f, damage - baseDamage);
            float poise = weapon.poiseDamage * Mathf.Lerp(0.88f, 1.18f, impactScale);
            float impulse = Mathf.Clamp(swingMomentum * 0.18f, 3f, 9.5f);

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
                    false,
                    bonusDamage > 0f ? "SIGHT_SWORD_DAMAGE" : null,
                    bonusDamage));
                hitStop?.Pulse(tuning != null ? tuning.lightHitStop : 0.02f);
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
            bool perfect = Time.time - _guardStartedAt <= Mathf.Max(0.02f, shield.perfectGuardWindowSeconds);
            if (perfect) staminaCost *= Mathf.Clamp(perfectGuardStaminaMultiplier, 0.1f, 1f);

            if (!stamina.TrySpend(staminaCost, perfect ? "PERFECT_GUARD" : "SHIELD_BLOCK"))
            {
                BreakGuard();
                return false;
            }

            if (perfect && primaryTarget != null)
            {
                float reflectedDamage = tuning != null ? tuning.reflectedDamage : Mathf.Max(18f, projectile.Damage);
                projectile.ReflectTowards(
                    primaryTarget,
                    tuning != null ? tuning.bloomReleaseSpeed : 20f,
                    reflectedDamage,
                    tuning != null ? tuning.reflectedPoise : 18f,
                    0,
                    auras != null && auras.ConcordActive ? "CONCORD_COUNTER_DAMAGE" : null,
                    auras != null && auras.ConcordActive ? reflectedDamage * 0.20f : 0f);
                flux?.Award(tuning != null ? tuning.counterFlux : 0.45f, "Perfect Shield Guard");
                hitStop?.Pulse(tuning != null ? tuning.parryHitStop : 0.02f);
                PerfectGuard?.Invoke();
                return true;
            }

            float absorption = Mathf.Clamp01(shield.baseDamageAbsorption + maxGuardAbsorptionBonus * guard);
            float chip = Mathf.Max(0f, projectile.Damage * (1f - absorption));
            if (chip > 0.001f && vitals != null)
            {
                vitals.ReceiveDamage(new DamagePacket(
                    chip,
                    0f,
                    Vector3.zero,
                    point,
                    CombatTeam.Enemy,
                    false));
            }
            projectile.ConsumeByShield();
            ShieldBlocked?.Invoke(projectile.Damage, chip);
            return true;
        }

        private void BreakGuard()
        {
            if (!_guardHeld) return;
            _guardHeld = false;
            shieldHitbox?.SetGuardActive(false);
            GuardChanged?.Invoke(false);
            GuardBroken?.Invoke();
        }
    }
}
