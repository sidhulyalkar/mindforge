using System;
using UnityEngine;

namespace Mindforge.SoulWisp
{
    /// <summary>
    /// Software frame-timing guard for visual BCI stimuli. This does not replace
    /// photodiode/high-speed-camera measurement of emitted luminance timing.
    /// </summary>
    public sealed class DisplayTimingMonitor : MonoBehaviour
    {
        [SerializeField] private float expectedRefreshHz = 120f;
        [SerializeField] private int sampleFrames = 240;
        [SerializeField] private float droppedFrameMultiplier = 1.55f;
        [SerializeField] private float maximumDropFraction = 0.01f;
        [SerializeField] private bool requestVSync = true;

        private int _frames;
        private int _drops;
        private double _sumDelta;
        private double _windowStarted;

        public bool TimingHealthy { get; private set; }
        public float ObservedRefreshHz { get; private set; }
        public float DropFraction { get; private set; }
        public event Action<bool> TimingHealthChanged;

        private void Awake()
        {
            if (requestVSync) QualitySettings.vSyncCount = 1;
            _windowStarted = Time.realtimeSinceStartupAsDouble;
        }

        private void Update()
        {
            double delta = Time.unscaledDeltaTime;
            if (delta <= 0.0) return;
            _frames++;
            _sumDelta += delta;
            double expectedDelta = 1.0 / Mathf.Max(1f, expectedRefreshHz);
            if (delta > expectedDelta * droppedFrameMultiplier) _drops++;
            if (_frames < Mathf.Max(30, sampleFrames)) return;

            ObservedRefreshHz = (float)(_frames / Math.Max(_sumDelta, 1e-9));
            DropFraction = _drops / (float)_frames;
            bool rateClose = Mathf.Abs(ObservedRefreshHz - expectedRefreshHz) <= Mathf.Max(1f, expectedRefreshHz * 0.03f);
            bool healthy = rateClose && DropFraction <= maximumDropFraction;
            if (healthy != TimingHealthy)
            {
                TimingHealthy = healthy;
                TimingHealthChanged?.Invoke(healthy);
            }

            _frames = 0;
            _drops = 0;
            _sumDelta = 0.0;
            _windowStarted = Time.realtimeSinceStartupAsDouble;
        }
    }
}
