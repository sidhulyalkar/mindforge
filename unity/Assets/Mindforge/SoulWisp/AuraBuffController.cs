using System;
using UnityEngine;
using Mindforge.Neural;

namespace Mindforge.SoulWisp
{
    public sealed class AuraBuffController : MonoBehaviour
    {
        [Header("Neural acceptance")]
        [Range(0f, 1f)] public float minConfidence = 0.55f;
        [Range(0f, 1f)] public float minQuality = 0.55f;

        [Header("Sight")]
        public float sightDurationSeconds = 3.6f;
        public float sightDamageMultiplier = 1.58f;

        [Header("Guard")]
        public float guardDurationSeconds = 3.6f;
        public float guardHealingPerSecond = 4.4f;

        private double _sightUntil;
        private double _guardUntil;

        public bool SightActive => Time.realtimeSinceStartupAsDouble < _sightUntil;
        public bool GuardActive => Time.realtimeSinceStartupAsDouble < _guardUntil;
        public bool ConcordActive => SightActive && GuardActive;
        public float DamageMultiplier => SightActive ? sightDamageMultiplier : 1f;
        public float HealingPerSecond => GuardActive ? guardHealingPerSecond : 0f;
        public float SightRemaining => Mathf.Max(0f, (float)(_sightUntil - Time.realtimeSinceStartupAsDouble));
        public float GuardRemaining => Mathf.Max(0f, (float)(_guardUntil - Time.realtimeSinceStartupAsDouble));

        public event Action<string> AuraApplied;

        public bool TryApply(NeuralEvent evt)
        {
            if (evt == null || !evt.IsSelection || evt.artifact) return false;
            if (evt.confidence < minConfidence || evt.quality < minQuality) return false;
            double now = Time.realtimeSinceStartupAsDouble;
            switch (evt.Target)
            {
                case AuraTarget.Sight:
                    _sightUntil = Math.Max(_sightUntil, now + sightDurationSeconds);
                    AuraApplied?.Invoke("sight");
                    return true;
                case AuraTarget.Guard:
                    _guardUntil = Math.Max(_guardUntil, now + guardDurationSeconds);
                    AuraApplied?.Invoke("guard");
                    return true;
                default:
                    return false;
            }
        }

        public void ClearAll()
        {
            _sightUntil = 0;
            _guardUntil = 0;
        }
    }
}
