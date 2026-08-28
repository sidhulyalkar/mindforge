using System;
using UnityEngine;

namespace Mindforge.Combat
{
    public sealed class PoiseSystem : MonoBehaviour
    {
        [SerializeField] private float maxPoise = 120f;
        [SerializeField] private float recoveryPerSecond = 5f;
        [SerializeField] private float breakDuration = 2.6f;

        public float Current { get; private set; }
        public float Max => maxPoise;
        public bool Broken => FixedTick < _brokenUntilTick;
        public float BreakRemaining => Mathf.Max(0f, (_brokenUntilTick - FixedTick) * Time.fixedDeltaTime);

        private long _brokenUntilTick = long.MinValue / 4;
        public event Action BrokenEvent;

        private long FixedTick
        {
            get
            {
                float dt = Mathf.Max(0.0001f, Time.fixedDeltaTime);
                return (long)Math.Round(Time.fixedTime / dt);
            }
        }

        private void Awake() => ResetFull();

        private void FixedUpdate()
        {
            if (Broken) return;
            Current = Mathf.Min(maxPoise, Current + recoveryPerSecond * Time.fixedDeltaTime);
        }

        public bool Apply(float amount)
        {
            if (amount <= 0f || Broken) return false;
            Current = Mathf.Max(0f, Current - amount);
            if (Current > 0f) return false;
            Current = maxPoise;
            _brokenUntilTick = FixedTick + SecondsToTicks(Mathf.Max(0f, breakDuration));
            BrokenEvent?.Invoke();
            return true;
        }

        public void ResetFull()
        {
            Current = Mathf.Max(0f, maxPoise);
            _brokenUntilTick = long.MinValue / 4;
        }

        private static int SecondsToTicks(float seconds)
        {
            float dt = Mathf.Max(0.0001f, Time.fixedDeltaTime);
            return Mathf.Max(1, Mathf.CeilToInt(Mathf.Max(0f, seconds) / dt));
        }
    }
}
