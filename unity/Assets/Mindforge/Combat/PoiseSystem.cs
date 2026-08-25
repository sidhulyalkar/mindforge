using System;
using UnityEngine;

namespace Mindforge.Combat
{
    public sealed class PoiseSystem : MonoBehaviour
    {
        [SerializeField] private float maxPoise = 120f;
        [SerializeField] private float recoveryPerSecond = 5f;
        [SerializeField] private float breakDuration = 1.15f;

        public float Current { get; private set; }
        public float Max => maxPoise;
        public bool Broken => Time.time < _brokenUntil;
        public float BreakRemaining => Mathf.Max(0f, _brokenUntil - Time.time);

        private float _brokenUntil;
        public event Action BrokenEvent;

        private void Awake() => Current = maxPoise;

        private void Update()
        {
            if (Broken) return;
            Current = Mathf.Min(maxPoise, Current + recoveryPerSecond * Time.deltaTime);
        }

        public bool Apply(float amount)
        {
            if (amount <= 0f || Broken) return false;
            Current = Mathf.Max(0f, Current - amount);
            if (Current > 0f) return false;
            Current = maxPoise;
            _brokenUntil = Time.time + breakDuration;
            BrokenEvent?.Invoke();
            return true;
        }
    }
}
