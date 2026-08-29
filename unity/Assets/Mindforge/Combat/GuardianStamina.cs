using System;
using UnityEngine;

namespace Mindforge.Combat
{
    /// <summary>
    /// Shared conventional endurance budget. Ground dodge rolls and air dashes spend it;
    /// the removed shield stance no longer owns the player's primary defensive economy.
    /// Neural modulation never creates a dodge or movement action.
    /// </summary>
    public sealed class GuardianStamina : MonoBehaviour
    {
        [SerializeField] private float maxStamina = 100f;
        [SerializeField] private float recoveryPerSecond = 42f;
        [SerializeField] private float recoveryDelaySeconds = 0.48f;
        [SerializeField] private float dodgeBaseCost = 22f;

        private float _value;
        private long _recoverAfterTick = long.MinValue / 4;
        private float _recoveryMultiplier = 1f;

        public float Value => _value;
        public float Max => Mathf.Max(1f, maxStamina);
        public float Ratio => Mathf.Clamp01(_value / Max);
        public float DodgeBaseCost => dodgeBaseCost;
        public float RecoveryMultiplier => _recoveryMultiplier;

        public event Action<float, float, string> Changed;
        public event Action Exhausted;

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
            _value = Max;
            _recoverAfterTick = long.MinValue / 4;
        }

        private void FixedUpdate()
        {
            if (FixedTick < _recoverAfterTick || _value >= Max || _recoveryMultiplier <= 0f) return;
            float before = _value;
            _value = Mathf.Min(Max, _value + Mathf.Max(0f, recoveryPerSecond) * _recoveryMultiplier * Time.fixedDeltaTime);
            if (!Mathf.Approximately(before, _value)) Changed?.Invoke(before, _value, "RECOVERY");
        }

        public void SetRecoveryMultiplier(float multiplier)
            => _recoveryMultiplier = Mathf.Clamp(multiplier, 0f, 2f);

        public bool CanSpend(float amount) => _value + 0.0001f >= Mathf.Max(0f, amount);

        public bool TrySpend(float amount, string reason)
        {
            amount = Mathf.Max(0f, amount);
            if (!CanSpend(amount))
            {
                Exhausted?.Invoke();
                return false;
            }
            SpendUnchecked(amount, reason);
            return true;
        }

        public float DrainUpTo(float amount, string reason)
        {
            amount = Mathf.Max(0f, amount);
            float spent = Mathf.Min(_value, amount);
            if (spent > 0f) SpendUnchecked(spent, reason);
            if (spent + 0.0001f < amount) Exhausted?.Invoke();
            return spent;
        }

        public void ResetFull(string reason = "CHECKPOINT_RESET")
        {
            float before = _value;
            _value = Max;
            _recoverAfterTick = long.MinValue / 4;
            _recoveryMultiplier = 1f;
            if (!Mathf.Approximately(before, _value)) Changed?.Invoke(before, _value, reason);
        }

        private void SpendUnchecked(float amount, string reason)
        {
            float before = _value;
            _value = Mathf.Max(0f, _value - amount);
            _recoverAfterTick = FixedTick + SecondsToTicks(Mathf.Max(0f, recoveryDelaySeconds));
            Changed?.Invoke(before, _value, reason ?? "ACTION");
            if (_value <= 0.0001f) Exhausted?.Invoke();
        }

        private static int SecondsToTicks(float seconds)
        {
            float dt = Mathf.Max(0.0001f, Time.fixedDeltaTime);
            return Mathf.Max(1, Mathf.CeilToInt(Mathf.Max(0f, seconds) / dt));
        }
    }
}
