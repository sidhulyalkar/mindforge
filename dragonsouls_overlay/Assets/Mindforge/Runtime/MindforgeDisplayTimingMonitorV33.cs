using System;
using UnityEngine;

namespace Mindforge.Chassis
{
    /// <summary>
    /// Software-only frame cadence monitor for BCI presentation. This is a guardrail,
    /// not proof of emitted optical timing. Physical qualification still requires an
    /// external measurement such as a photodiode or high-speed camera.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MindforgeDisplayTimingMonitorV33 : MonoBehaviour
    {
        [SerializeField, Min(30)] private int sampleFrames = 120;
        [SerializeField] private float minimumObservedHz = 55f;
        [SerializeField, Range(0.01f, 0.30f)] private float maximumLongFrameFraction = 0.10f;
        [SerializeField, Range(1.2f, 3.0f)] private float longFrameMultiplier = 1.75f;

        private float[] _deltas;
        private int _count;
        private int _cursor;

        public bool HasMeasurement => _count >= Mathf.Min(sampleFrames, 60);
        public float ObservedRefreshHz { get; private set; }
        public float LongFrameFraction { get; private set; }
        public bool TimingHealthy => HasMeasurement && ObservedRefreshHz >= minimumObservedHz && LongFrameFraction <= maximumLongFrameFraction;

        private void Awake()
        {
            sampleFrames = Mathf.Max(30, sampleFrames);
            _deltas = new float[sampleFrames];
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (dt <= 0f || dt > 0.25f) return;
            _deltas[_cursor] = dt;
            _cursor = (_cursor + 1) % _deltas.Length;
            _count = Mathf.Min(_count + 1, _deltas.Length);
            if (_count < 30) return;

            float[] copy = new float[_count];
            Array.Copy(_deltas, copy, _count);
            Array.Sort(copy);
            float median = copy[_count / 2];
            float sum = 0f;
            int longFrames = 0;
            float longThreshold = median * longFrameMultiplier;
            for (int i = 0; i < _count; i++)
            {
                sum += copy[i];
                if (copy[i] > longThreshold) longFrames++;
            }

            float mean = sum / Mathf.Max(1, _count);
            ObservedRefreshHz = mean > 0.0001f ? 1f / mean : 0f;
            LongFrameFraction = longFrames / (float)Mathf.Max(1, _count);
        }
    }
}
