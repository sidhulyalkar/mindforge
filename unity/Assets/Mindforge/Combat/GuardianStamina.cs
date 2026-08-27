using System;
using UnityEngine;

namespace Mindforge.Combat
{
    /// <summary>
    /// Shared physical-action budget. Neural modulation may improve the outcome of a
    /// chosen action, but it never bypasses stamina costs or creates an action itself.
    /// </summary>
    public sealed class GuardianStamina : MonoBehaviour
    {
        [SerializeField] private float maxStamina = 100f;
        [SerializeField] private float recoveryPerSecond = 31f;
        [SerializeField] private float recoveryDelaySeconds = 0.62f;
        [SerializeField] private float dodgeBaseCost = 24f;

        private float _value;
        private float _recoverAfter;
        private float _recoveryMultiplier = 1f;

        public float Value => _value;
        public float Max => Mathf.Max(1f, maxStamina);
        public float Ratio => Mathf.Clamp01(_value / Max);
        public float DodgeBaseCost => dodgeBaseCost;
        public float RecoveryMultiplier => _recoveryMultiplier;

        public event Action<float, float, string> Changed;
        public event Action Exhausted;

        private void Awake() => _value = Max;

        private void Update()
        {
            if (Time.time < _recoverAfter || _value >= Max || _recoveryMultiplier <= 0f) return;
            float before = _value;
            _value = Mathf.Min(Max, _value + Mathf.Max(0f, recoveryPerSecond) * _recoveryMultiplier * Time.deltaTime);
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

        private void SpendUnchecked(float amount, string reason)
        {
            float before = _value;
            _value = Mathf.Max(0f, _value - amount);
            _recoverAfter = Time.time + Mathf.Max(0f, recoveryDelaySeconds);
            Changed?.Invoke(before, _value, reason ?? "ACTION");
            if (_value <= 0.0001f) Exhausted?.Invoke();
        }
    }
}
