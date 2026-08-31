using System;
using UnityEngine;
using Mindforge.Neural;

namespace Mindforge.SoulWisp
{
    /// <summary>
    /// Stores accepted slower neural transformations only. Frame-critical combat still belongs
    /// to conventional player input. Sight changes how effectively the player can exploit an
    /// opening; Guard changes how forgiving a player-executed counter opportunity becomes.
    /// Neither aura attacks, blocks, parries, moves, aims or targets on the player's behalf.
    /// </summary>
    public sealed class AuraBuffController : MonoBehaviour
    {
        [Header("Neural acceptance")]
        [Range(0f, 1f)] public float minConfidence = 0.55f;
        [Range(0f, 1f)] public float minQuality = 0.55f;

        [Header("Sight · expose and punish")]
        public float sightDurationSeconds = 3.6f;
        [Tooltip("Kept meaningful but deliberately smaller than the old pure damage-buff identity.")]
        public float sightDamageMultiplier = 1.30f;
        [Tooltip("Sight should make a created opening easier to exploit, not execute the hit itself.")]
        public float sightReachMultiplier = 1.16f;
        [Tooltip("Poise pressure makes Sight useful against readable vulnerability windows.")]
        public float sightPoiseMultiplier = 1.45f;

        [Header("Guard · survive by executing defense")]
        public float guardDurationSeconds = 3.6f;
        [Tooltip("Small recovery remains, but Guard's main identity is a better physical counter opportunity.")]
        public float guardHealingPerSecond = 2.2f;
        [Tooltip("Multiplies the conventional counter timing window. Guard never triggers the counter itself.")]
        public float guardCounterWindowMultiplier = 1.28f;
        [Tooltip("Modestly increases projectile capture geometry for an already-player-triggered counter.")]
        public float guardCounterRadiusMultiplier = 1.10f;
        [Tooltip("Reward delivered only after the player successfully reflects a projectile while Guard is active.")]
        public float guardSuccessfulCounterHeal = 3.2f;

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
        public float DamageMultiplier => SightActive ? Mathf.Max(1f, sightDamageMultiplier) : 1f;
        public float SightReachMultiplier => SightActive ? Mathf.Max(1f, sightReachMultiplier) : 1f;
        public float SightPoiseMultiplier => SightActive ? Mathf.Max(1f, sightPoiseMultiplier) : 1f;
        public float HealingPerSecond => GuardActive ? Mathf.Max(0f, guardHealingPerSecond) : 0f;
        public float GuardCounterWindowMultiplier => GuardActive ? Mathf.Max(1f, guardCounterWindowMultiplier) : 1f;
        public float GuardCounterRadiusMultiplier => GuardActive ? Mathf.Max(1f, guardCounterRadiusMultiplier) : 1f;
        public float GuardSuccessfulCounterHeal => GuardActive ? Mathf.Max(0f, guardSuccessfulCounterHeal) : 0f;
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
