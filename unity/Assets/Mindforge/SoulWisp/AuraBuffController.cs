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

        [Header("Concord")]
        [Tooltip("Once true Sight+Guard overlap occurs, Concord remains available long enough for physical execution.")]
        public float concordGraceSeconds = 4.5f;

        private double _sightUntil;
        private double _guardUntil;
        private double _concordUntil;

        private static double Now => Time.realtimeSinceStartupAsDouble;
        public bool SightActive => Now < _sightUntil;
        public bool GuardActive => Now < _guardUntil;
        public bool ConcordActive => Now < _concordUntil;
        public float DamageMultiplier => SightActive ? sightDamageMultiplier : 1f;
        public float HealingPerSecond => GuardActive ? guardHealingPerSecond : 0f;
        public float SightRemaining => Mathf.Max(0f, (float)(_sightUntil - Now));
        public float GuardRemaining => Mathf.Max(0f, (float)(_guardUntil - Now));
        public float ConcordRemaining => Mathf.Max(0f, (float)(_concordUntil - Now));

        public event Action<string> AuraApplied;
        public event Action ConcordTriggered;

        public bool TryApply(NeuralEvent evt)
        {
            if (evt == null || !evt.IsSelection || evt.artifact) return false;
            if (evt.confidence < minConfidence || evt.quality < minQuality) return false;
            double now = Now;
            switch (evt.Target)
            {
                case AuraTarget.Sight:
                    _sightUntil = Math.Max(_sightUntil, now + sightDurationSeconds);
                    AuraApplied?.Invoke("sight");
                    break;
                case AuraTarget.Guard:
                    _guardUntil = Math.Max(_guardUntil, now + guardDurationSeconds);
                    AuraApplied?.Invoke("guard");
                    break;
                default:
                    return false;
            }

            if (now < _sightUntil && now < _guardUntil)
            {
                bool wasActive = now < _concordUntil;
                _concordUntil = Math.Max(_concordUntil, now + concordGraceSeconds);
                if (!wasActive) ConcordTriggered?.Invoke();
            }
            return true;
        }

        public void ClearAll()
        {
            _sightUntil = 0;
            _guardUntil = 0;
            _concordUntil = 0;
        }
    }
}
