using System;
using UnityEngine;

namespace Mindforge.Combat
{
    public sealed class FluxMeter : MonoBehaviour
    {
        [SerializeField] private CombatTuning tuning;
        public float Value { get; private set; }
        public float Max => tuning != null ? tuning.maxFlux : 3f;
        public bool IsFull => Value >= Max - 0.001f;

        public event Action<float, float, string> Changed;

        public bool Award(float amount, string reason)
        {
            if (amount <= 0f) return false;
            float before = Value;
            Value = Mathf.Clamp(Value + amount, 0f, Max);
            if (Mathf.Approximately(before, Value)) return false;
            Changed?.Invoke(before, Value, reason);
            return true;
        }

        public bool TryConsumeFull(string reason = "Gravity Bloom")
        {
            if (!IsFull) return false;
            float before = Value;
            Value = 0f;
            Changed?.Invoke(before, Value, reason);
            return true;
        }

        public void ResetMeter()
        {
            float before = Value;
            Value = 0f;
            Changed?.Invoke(before, Value, "Reset");
        }
    }
}
