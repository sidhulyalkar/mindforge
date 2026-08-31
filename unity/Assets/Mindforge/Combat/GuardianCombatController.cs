using System;
using System.Collections.Generic;
using UnityEngine;
using Mindforge.Presentation;
using Mindforge.SoulWisp;

namespace Mindforge.Combat
{
    public sealed class GuardianCombatController : MonoBehaviour
    {
        [SerializeField] private CombatTuning tuning;
        [SerializeField] private GuardianMotor motor;
        [SerializeField] private AuraBuffController auras;
        [SerializeField] private FluxMeter flux;
        [SerializeField] private CombatantVitals vitals;
        [SerializeField] private HitStopController hitStop;
        [SerializeField] private CombatPresentationDirector presentation;
        [SerializeField] private MindforgeProjectile projectilePrefab;
        [SerializeField] private Transform muzzle;
        [SerializeField] private Transform primaryTarget;
        [SerializeField] private GuardianTargetLock targetLock;
        [SerializeField] private LayerMask damageMask;
        [SerializeField] private LayerMask projectileMask;

        private readonly Collider[] _hits = new Collider[48];
        private readonly HashSet<int> _reflectedThisWindow = new HashSet<int>();
        private long _lastShotTick = long.MinValue / 4;
        private long _lastCleaveTick = long.MinValue / 4;
        private long _lastCounterTick = long.MinValue / 4;
        private long _counterUntilTick = long.MinValue / 4;
        private string _lastAura;

        public event Action<string> ActionAccepted;
        public event Action<string> CombatOutcome;
        public event Action<string, float> NeuralPayoffObserved;

        public bool ConcordActive => auras != null && auras.ConcordActive;
        public Transform PrimaryTarget { get => primaryTarget; set => primaryTarget = value; }
        public Transform CurrentConventionalTarget => CombatTargetResolver.Resolve(targetLock, primaryTarget);

        private long FixedTick
        {
            get
            {
                float dt = Mathf.Max(0.0001f, Time.fixedDeltaTime);
                return (long)Math.Round(Time.fixedTime / dt);
            }
        }

        private void Awake()
        {
            if (targetLock == null) targetLock = GetComponent<GuardianTargetLock>();
        }

        private void OnEnable()
        {
            if (auras != null) auras.AuraApplied += OnAuraApplied;
        }

        private void OnDisable()
        {
            if (auras != null) auras.AuraApplied -= OnAuraApplied;
        }

        public void ResetForCheckpoint()
        {
            _lastShotTick = long.MinValue / 4;
            _lastCleaveTick = long.MinValue / 4;
            _lastCounterTick = long.MinValue / 4;
            _counterUntilTick = long.MinValue / 4;
            _reflectedThisWindow.Clear();
        }

        private void FixedUpdate()
        {
            if (FixedTick < _counterUntilTick) ScanCounterProjectiles();

            // Guard resonance regeneration is a gameplay payoff, so it shares the same
            // simulation clock as damage, attacks and deterministic input replay.
            if (auras != null && auras.GuardActive && vitals != null)
            {
                float restored = vitals.Heal(auras.HealingPerSecond * Time.fixedDeltaTime);
                if (restored > 0f)
                    NeuralPayoffObserved?.Invoke("GUARD_REGEN_REALIZED", restored);
            }
        }

        private void OnAuraApplied(string target)
        {
            if (!string.IsNullOrEmpty(_lastAura) && _lastAura != target)
                flux?.Award(tuning != null ? tuning.auraSwitchFlux : 0.13f, "Attention switch");
            _lastAura = target;
        }

        public bool FirePulse(Vector3 aimDirection)
        {
            if (tuning == null || projectilePrefab == null) return false;
            long now = FixedTick;
            if (now - _lastShotTick < SecondsToTicks(tuning.shotCooldown)) return false;
            _lastShotTick = now;

            bool sight = auras != null && auras.SightActive;
            float speed = sight ? tuning.sightShotSpeed : tuning.shotSpeed;
            float damage = sight ? tuning.sightShotDamage : tuning.shotDamage;
            float poise = tuning.shotPoise * (sight ? auras.SightPoiseMultiplier : 1f);
            float neuralBonusDamage = sight ? Mathf.Max(0f, damage - tuning.shotDamage) : 0f;
            int pierce = sight ? 1 : 0;
            Vector3 origin = muzzle != null ? muzzle.position : transform.position + Vector3.up;
            MindforgeProjectile p = Instantiate(projectilePrefab, origin, Quaternion.LookRotation(aimDirection.normalized));
            p.Configure(
                CombatTeam.Guardian,
                aimDirection.normalized * speed,
                damage,
                poise,
                pierce,
                sight ? "SIGHT_PULSE_DAMAGE" : null,
                neuralBonusDamage);
            ActionAccepted?.Invoke("PULSE_SHOT");
            return true;
        }

        public bool RiftCleave(Vector3 aimDirection)
        {
            if (tuning == null) return false;
            long now = FixedTick;
            if (now - _lastCleaveTick < SecondsToTicks(tuning.cleaveCooldown)) return false;
            _lastCleaveTick = now;

            ActionAccepted?.Invoke("RIFT_CLEAVE");
            bool sight = auras != null && auras.SightActive;
            float range = tuning.cleaveRange * (sight ? auras.SightReachMultiplier : 1f);
            float halfArc = tuning.cleaveArcDegrees * (sight ? 1.10f : 1f) * 0.5f;
            int count = Physics.OverlapSphereNonAlloc(transform.position, range, _hits, damageMask, QueryTriggerInteraction.Collide);
            bool hit = false;
            for (int i = 0; i < count; i++)
            {
                CombatantVitals receiver = _hits[i].GetComponentInParent<CombatantVitals>();
                if (receiver == null || receiver.Team == CombatTeam.Guardian || !receiver.IsAlive) continue;
                Vector3 delta = _hits[i].transform.position - transform.position;
                delta.y = 0f;
                if (delta.sqrMagnitude < 0.01f || Vector3.Angle(aimDirection, delta) > halfArc) continue;
                float multiplier = auras != null ? auras.DamageMultiplier : 1f;
                float totalDamage = tuning.cleaveDamage * multiplier;
                float totalPoise = tuning.cleavePoise * (sight ? auras.SightPoiseMultiplier : 1f);
                float neuralBonusDamage = sight ? Mathf.Max(0f, totalDamage - tuning.cleaveDamage) : 0f;
                receiver.ReceiveDamage(new DamagePacket(
                    totalDamage,
                    totalPoise,
                    delta.normalized * tuning.cleaveImpulse,
                    _hits[i].ClosestPoint(transform.position),
                    CombatTeam.Guardian,
                    true,
                    sight ? "SIGHT_CLEAVE_DAMAGE" : null,
                    neuralBonusDamage));
                hit = true;
            }
            if (hit)
            {
                hitStop?.Pulse(tuning.heavyHitStop);
                presentation?.CleaveImpact(aimDirection);
                CombatOutcome?.Invoke("RIFT_CLEAVE_HIT");
            }
            return true;
        }

        public bool BeginCounter()
        {
            if (tuning == null) return false;
            long now = FixedTick;
            if (now - _lastCounterTick < SecondsToTicks(tuning.counterCooldown)) return false;
            _lastCounterTick = now;

            // Preserve the canonical baseline window and fixed-tick contract first.
            _counterUntilTick = now + SecondsToTicks(tuning.counterWindow);
            if (auras != null && auras.GuardActive)
            {
                float extraSeconds = tuning.counterWindow * Mathf.Max(0f, auras.GuardCounterWindowMultiplier - 1f);
                if (extraSeconds > 0.0001f)
                    _counterUntilTick += SecondsToTicks(extraSeconds);
            }

            _reflectedThisWindow.Clear();
            ActionAccepted?.Invoke("COUNTER_PULSE");
            return true;
        }

        private void ScanCounterProjectiles()
        {
            if (tuning == null) return;
            if (targetLock == null) targetLock = GetComponent<GuardianTargetLock>();
            Transform target = CombatTargetResolver.Resolve(targetLock, primaryTarget);
            if (target == null) return;

            bool guard = auras != null && auras.GuardActive;
            float counterRadius = tuning.counterRadius * (guard ? auras.GuardCounterRadiusMultiplier : 1f);
            int count = Physics.OverlapSphereNonAlloc(transform.position, counterRadius, _hits, projectileMask, QueryTriggerInteraction.Collide);
            bool reflectedAny = false;
            Vector3 impactDirection = target.position - transform.position;

            for (int i = 0; i < count; i++)
            {
                MindforgeProjectile p = _hits[i].GetComponentInParent<MindforgeProjectile>();
                if (p == null || !p.IsHostileToGuardian || !_reflectedThisWindow.Add(p.GetInstanceID())) continue;
                bool concord = ConcordActive;
                float baselineDamage = tuning.reflectedDamage;
                float reflectedDamage = baselineDamage * (concord ? 1.25f : 1f);
                p.ReflectTowards(
                    target,
                    tuning.bloomReleaseSpeed,
                    reflectedDamage,
                    tuning.reflectedPoise * (concord ? 1.25f : 1f),
                    auras != null && auras.SightActive ? 1 : 0,
                    concord ? "CONCORD_COUNTER_DAMAGE" : null,
                    concord ? Mathf.Max(0f, reflectedDamage - baselineDamage) : 0f);
                flux?.Award(tuning.counterFlux, "Perfect Counter");
                if (guard && vitals != null)
                {
                    float restored = vitals.Heal(auras.GuardSuccessfulCounterHeal);
                    if (restored > 0f)
                        NeuralPayoffObserved?.Invoke("GUARD_COUNTER_HEAL_REALIZED", restored);
                }
                reflectedAny = true;
            }

            if (reflectedAny)
            {
                hitStop?.Pulse(tuning.parryHitStop);
                presentation?.CounterImpact(impactDirection);
                CombatOutcome?.Invoke("COUNTER_REFLECT");
            }
        }

        private static int SecondsToTicks(float seconds)
        {
            float dt = Mathf.Max(0.0001f, Time.fixedDeltaTime);
            return Mathf.Max(1, Mathf.CeilToInt(Mathf.Max(0f, seconds) / dt));
        }
    }
}
