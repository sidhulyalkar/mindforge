using System;
using System.Collections.Generic;
using UnityEngine;
using Mindforge.Presentation;
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
        [SerializeField] private GuardianTargetLock targetLock;
        [SerializeField] private LayerMask projectileMask;
        [SerializeField] private HitStopController hitStop;
        [SerializeField] private CombatPresentationDirector presentation;

        private readonly Collider[] _hits = new Collider[96];
        private readonly List<MindforgeProjectile> _captured = new List<MindforgeProjectile>();
        private readonly HashSet<int> _capturedIds = new HashSet<int>();
        private bool _active;
        private bool _concord;
        private bool _externalPaused;
        private long _pauseStartedTick;
        private long _endTick = long.MinValue / 4;
        private long _lastUseTick = long.MinValue / 4;

        public event Action<bool> Activated;
        public event Action<bool, int> Released;

        public bool Active => _active;
        public bool ConcordCast => _active && _concord;
        public bool ExternalPaused => _externalPaused;
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

        public void SetExternalPause(bool paused)
        {
            if (_externalPaused == paused) return;
            _externalPaused = paused;
            if (paused)
            {
                _pauseStartedTick = FixedTick;
            }
            else if (_active)
            {
                _endTick += Math.Max(0L, FixedTick - _pauseStartedTick);
            }
        }

        public bool TryActivate()
        {
            if (_externalPaused || tuning == null || flux == null || !flux.IsFull) return false;
            long now = FixedTick;
            if (now - _lastUseTick < SecondsToTicks(tuning.bloomCooldown)) return false;
            if (!flux.TryConsumeFull()) return false;

            _lastUseTick = now;
            _active = true;
            _concord = auras != null && auras.ConcordActive;
            float duration = tuning.bloomDuration * (_concord ? 1.15f : 1f);
            _endTick = now + SecondsToTicks(duration);
            _captured.Clear();
            _capturedIds.Clear();

            hitStop?.Pulse(tuning.lightHitStop);
            presentation?.BloomCharge(_concord);
            Activated?.Invoke(_concord);
            return true;
        }

        private void FixedUpdate()
        {
            if (_externalPaused || !_active || tuning == null) return;
            float radius = tuning.bloomRadius * (_concord ? tuning.concordRadiusMultiplier : 1f);
            int count = Physics.OverlapSphereNonAlloc(transform.position, radius, _hits, projectileMask, QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
            {
                MindforgeProjectile p = _hits[i].GetComponentInParent<MindforgeProjectile>();
                if (p == null || !p.IsHostileToGuardian || !_capturedIds.Add(p.GetInstanceID())) continue;
                _captured.Add(p);
                p.Capture(captureAnchor != null ? captureAnchor : transform, _captured.Count * 0.9f);
            }
            if (FixedTick >= _endTick) Detonate();
        }

        private void Detonate()
        {
            if (_externalPaused) return;
            _active = false;
            int capturedCount = _captured.Count;
            if (targetLock == null) targetLock = GetComponent<GuardianTargetLock>();
            Transform target = CombatTargetResolver.Resolve(targetLock, primaryTarget);
            if (target == null)
            {
                foreach (MindforgeProjectile p in _captured)
                    if (p != null) p.ReleaseFromCapture();
                _captured.Clear();
                _capturedIds.Clear();
                Released?.Invoke(_concord, capturedCount);
                return;
            }

            float baselineDamage = tuning.reflectedDamage;
            float damage = baselineDamage * (_concord ? tuning.concordDamageMultiplier : 1f);
            float neuralBonusDamage = _concord ? Mathf.Max(0f, damage - baselineDamage) : 0f;
            float poise = tuning.reflectedPoise * (_concord ? tuning.concordDamageMultiplier : 1f);
            for (int i = 0; i < _captured.Count; i++)
            {
                MindforgeProjectile p = _captured[i];
                if (p == null) continue;
                p.ReflectTowards(
                    target,
                    tuning.bloomReleaseSpeed * (_concord ? 1.18f : 1f),
                    damage,
                    poise,
                    _concord ? 2 : 0,
                    _concord ? "TWIN_ECLIPSE_DAMAGE" : null,
                    neuralBonusDamage);
            }
            _captured.Clear();
            _capturedIds.Clear();

            presentation?.BloomRelease(_concord);
            hitStop?.Pulse(_concord ? tuning.twinEclipseHitStop : tuning.heavyHitStop);
            Released?.Invoke(_concord, capturedCount);
        }

        private static int SecondsToTicks(float seconds)
        {
            float dt = Mathf.Max(0.0001f, Time.fixedDeltaTime);
            return Mathf.Max(1, Mathf.CeilToInt(Mathf.Max(0f, seconds) / dt));
        }
    }
}
