using System.Collections.Generic;
using UnityEngine;

namespace Mindforge.Combat
{
    public sealed class SignalBreakReward : MonoBehaviour
    {
        [SerializeField] private PoiseSystem poise;
        [SerializeField] private FluxMeter flux;
        [SerializeField] private CombatTuning tuning;
        [SerializeField] private HitStopController hitStop;

        private void OnEnable()
        {
            if (poise != null) poise.BrokenEvent += OnBroken;
        }

        private void OnDisable()
        {
            if (poise != null) poise.BrokenEvent -= OnBroken;
        }

        private void OnBroken()
        {
            flux?.Award(tuning != null ? tuning.poiseBreakFlux : 0.5f, "Signal Break");
            hitStop?.Pulse(tuning != null ? tuning.poiseBreakHitStop : 0.075f);
        }
    }
}
