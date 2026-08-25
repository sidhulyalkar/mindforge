using System.Collections.Generic;
using UnityEngine;
using Mindforge.SoulWisp;

namespace Mindforge.Combat
{
    public sealed class GravityBloomAbility : MonoBehaviour
    {
        [SerializeField] private CombatTuning tuning;
        [SerializeField] private FluxMeter flux;
        [SerializeField] private AuraBuffController auras;
        [SerializeField] private Transform captureAnchor;
        [SerializeField] private Transform primaryTarget;
        [SerializeField] private LayerMask projectileMask;
        [SerializeField] private HitStopController hitStop;

        private readonly Collider[] _hits = new Collider[96];
        private readonly List<MindforgeProjectile> _captured = new List<MindforgeProjectile>();
        private readonly HashSet<int> _capturedIds = new HashSet<int>();
        private bool _active;
        private bool _concord;
        private float _endAt;
        private float _lastUse = -999f;

        public bool Active => _active;

        public bool TryActivate()
        {
            if (tuning == null || flux == null || !flux.IsFull || Time.time - _lastUse < tuning.bloomCooldown) return false;
            if (!flux.TryConsumeFull()) return false;
            _lastUse = Time.time;
            _active = true;
            _concord = auras != null && auras.SightActive && auras.GuardActive;
            _endAt = Time.time + tuning.bloomDuration * (_concord ? 1.15f : 1f);
            _captured.Clear();
            _capturedIds.Clear();
            hitStop?.Pulse(_concord ? tuning.heavyHitStop : tuning.lightHitStop);
            return true;
        }

        private void FixedUpdate()
        {
            if (!_active || tuning == null) return;
            float radius = tuning.bloomRadius * (_concord ? tuning.concordRadiusMultiplier : 1f);
            int count = Physics.OverlapSphereNonAlloc(transform.position, radius, _hits, projectileMask, QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
            {
                MindforgeProjectile p = _hits[i].GetComponentInParent<MindforgeProjectile>();
                if (p == null || !p.IsHostileToGuardian || !_capturedIds.Add(p.GetInstanceID())) continue;
                _captured.Add(p);
                p.Capture(captureAnchor != null ? captureAnchor : transform, _captured.Count * 0.9f);
            }
            if (Time.time >= _endAt) Detonate();
        }

        private void Detonate()
        {
            _active = false;
            if (primaryTarget == null)
            {
                foreach (MindforgeProjectile p in _captured) if (p != null) p.ReleaseFromCapture();
                return;
            }
            float damage = tuning.reflectedDamage * (_concord ? tuning.concordDamageMultiplier : 1f);
            float poise = tuning.reflectedPoise * (_concord ? tuning.concordDamageMultiplier : 1f);
            for (int i = 0; i < _captured.Count; i++)
            {
                MindforgeProjectile p = _captured[i];
                if (p == null) continue;
                p.ReflectTowards(primaryTarget, tuning.bloomReleaseSpeed * (_concord ? 1.18f : 1f), damage, poise, _concord ? 2 : 0);
            }
            _captured.Clear();
            _capturedIds.Clear();
            hitStop?.Pulse(_concord ? tuning.poiseBreakHitStop : tuning.heavyHitStop);
        }
    }
}
