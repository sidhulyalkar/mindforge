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
        [SerializeField] private LayerMask damageMask;
        [SerializeField] private LayerMask projectileMask;

        private readonly Collider[] _hits = new Collider[48];
        private readonly HashSet<int> _reflectedThisWindow = new HashSet<int>();
        private float _lastShot = -999f, _lastCleave = -999f, _lastCounter = -999f;
        private float _counterUntil;
        private string _lastAura;

        public bool ConcordActive => auras != null && auras.ConcordActive;
        public Transform PrimaryTarget { get => primaryTarget; set => primaryTarget = value; }

        private void OnEnable()
        {
            if (auras != null) auras.AuraApplied += OnAuraApplied;
        }

        private void OnDisable()
        {
            if (auras != null) auras.AuraApplied -= OnAuraApplied;
        }

        private void Update()
        {
            if (Time.time < _counterUntil) ScanCounterProjectiles();
            if (auras != null && auras.GuardActive && vitals != null)
                vitals.Heal(auras.HealingPerSecond * Time.deltaTime);
        }

        private void OnAuraApplied(string target)
        {
            if (!string.IsNullOrEmpty(_lastAura) && _lastAura != target)
                flux?.Award(tuning != null ? tuning.auraSwitchFlux : 0.13f, "Attention switch");
            _lastAura = target;
        }

        public bool FirePulse(Vector3 aimDirection)
        {
            if (tuning == null || projectilePrefab == null || Time.time - _lastShot < tuning.shotCooldown) return false;
            _lastShot = Time.time;
            bool sight = auras != null && auras.SightActive;
            float speed = sight ? tuning.sightShotSpeed : tuning.shotSpeed;
            float damage = sight ? tuning.sightShotDamage : tuning.shotDamage;
            int pierce = sight ? 1 : 0;
            Vector3 origin = muzzle != null ? muzzle.position : transform.position + Vector3.up;
            MindforgeProjectile p = Instantiate(projectilePrefab, origin, Quaternion.LookRotation(aimDirection.normalized));
            p.Configure(CombatTeam.Guardian, aimDirection.normalized * speed, damage, tuning.shotPoise, pierce);
            return true;
        }

        public bool RiftCleave(Vector3 aimDirection)
        {
            if (tuning == null || Time.time - _lastCleave < tuning.cleaveCooldown) return false;
            _lastCleave = Time.time;
            float range = tuning.cleaveRange * (auras != null && auras.SightActive ? 1.18f : 1f);
            float halfArc = tuning.cleaveArcDegrees * (auras != null && auras.SightActive ? 1.12f : 1f) * 0.5f;
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
                receiver.ReceiveDamage(new DamagePacket(
                    tuning.cleaveDamage * multiplier,
                    tuning.cleavePoise,
                    delta.normalized * tuning.cleaveImpulse,
                    _hits[i].ClosestPoint(transform.position),
                    CombatTeam.Guardian,
                    true));
                hit = true;
            }
            if (hit)
            {
                hitStop?.Pulse(tuning.heavyHitStop);
                presentation?.CleaveImpact(aimDirection);
            }
            return true;
        }

        public bool BeginCounter()
        {
            if (tuning == null || Time.time - _lastCounter < tuning.counterCooldown) return false;
            _lastCounter = Time.time;
            _counterUntil = Time.time + tuning.counterWindow;
            _reflectedThisWindow.Clear();
            return true;
        }

        private void ScanCounterProjectiles()
        {
            if (tuning == null) return;
            int count = Physics.OverlapSphereNonAlloc(transform.position, tuning.counterRadius, _hits, projectileMask, QueryTriggerInteraction.Collide);
            bool reflectedAny = false;
            Vector3 impactDirection = primaryTarget != null ? primaryTarget.position - transform.position : transform.forward;

            for (int i = 0; i < count; i++)
            {
                MindforgeProjectile p = _hits[i].GetComponentInParent<MindforgeProjectile>();
                if (p == null || !p.IsHostileToGuardian || !_reflectedThisWindow.Add(p.GetInstanceID())) continue;
                p.ReflectTowards(
                    primaryTarget,
                    tuning.bloomReleaseSpeed,
                    tuning.reflectedDamage * (ConcordActive ? 1.25f : 1f),
                    tuning.reflectedPoise * (ConcordActive ? 1.25f : 1f),
                    auras != null && auras.SightActive ? 1 : 0);
                flux?.Award(tuning.counterFlux, "Perfect Counter");
                if (auras != null && auras.GuardActive) vitals?.Heal(2.4f);
                reflectedAny = true;
            }

            // Multiple projectiles may be reflected by one parry field, but camera
            // and hit-stop fire once per successful Counter Pulse rather than once
            // per projectile. This keeps multi-reflections crisp instead of sticky.
            if (reflectedAny)
            {
                hitStop?.Pulse(tuning.parryHitStop);
                presentation?.CounterImpact(impactDirection);
            }
        }
    }
}
