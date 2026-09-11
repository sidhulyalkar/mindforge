using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mindforge.Chassis
{
    /// <summary>
    /// Measures game-interface gaze behavior for tutorial calibration. Metrics describe
    /// interaction geometry only (screen bias, dispersion, acquisition latency, target
    /// occupancy). They are not psychological or cognitive trait claims and never become
    /// combat authority by themselves.
    /// </summary>
    [DefaultExecutionOrder(1220)]
    [DisallowMultipleComponent]
    public sealed class MindforgeGazeProfilerV34 : MonoBehaviour
    {
        [Serializable]
        public sealed class PromptSummary
        {
            public string label;
            public float target_x;
            public float target_y;
            public int sample_count;
            public float occupancy;
            public float bias_x;
            public float bias_y;
            public float dispersion_p50;
            public float dispersion_p90;
            public float acquisition_ms = -1f;
            public bool stable_acquired;
        }

        [Serializable]
        public sealed class ProfileSnapshot
        {
            public string schema = "mindforge.gaze_profile.v1";
            public string source_mode = "unobserved";
            public int total_samples;
            public int usable_samples;
            public float usable_fraction;
            public int prompt_count;
            public int stable_prompt_count;
            public float bias_x;
            public float bias_y;
            public float dispersion_p50;
            public float dispersion_p90;
            public float acquisition_ms_p50 = -1f;
            public float acquisition_ms_p90 = -1f;
            public float occupancy_p50;
            public float recommended_aoi_radius = 0.06f;
            public int recommended_acquisition_lead_ms = 350;
        }

        private struct TimedPoint
        {
            public Vector2 Point;
            public double Time;
        }

        [SerializeField, Range(0f, 1f)] private float minimumConfidence = 0.55f;
        [SerializeField] private float stabilityWindowSeconds = 0.28f;
        [SerializeField] private int minimumStableSamples = 6;
        [SerializeField, Range(0.5f, 1f)] private float minimumInsideFraction = 0.80f;
        [SerializeField] private float maximumStableDispersion = 0.055f;

        private MindforgeUdpGazeReceiverV34 _receiver;
        private readonly List<TimedPoint> _recent = new List<TimedPoint>(64);
        private readonly List<Vector2> _promptPoints = new List<Vector2>(512);
        private readonly List<PromptSummary> _summaries = new List<PromptSummary>(32);

        private bool _promptActive;
        private Vector2 _promptTarget;
        private float _promptRadius = 0.06f;
        private double _promptStartedAt;
        private double _stableAcquiredAt = double.NaN;
        private int _totalSamples;
        private int _usableSamples;
        private string _sourceMode = "unobserved";

        public event Action<PromptSummary> PromptCompleted;
        public event Action StableTargetAcquired;

        public bool PromptActive => _promptActive;
        public bool CurrentPromptStable => _promptActive && !double.IsNaN(_stableAcquiredAt);
        public Vector2 CurrentTarget => _promptTarget;
        public float CurrentRadius => _promptRadius;
        public int PromptCount => _summaries.Count;
        public int TotalSamples => _totalSamples;
        public int UsableSamples => _usableSamples;
        public string SourceMode => _sourceMode;

        private void Start()
        {
            _receiver = GetComponent<MindforgeUdpGazeReceiverV34>();
            if (_receiver != null) _receiver.SampleReceived += HandleSample;
        }

        private void OnDestroy()
        {
            if (_receiver != null) _receiver.SampleReceived -= HandleSample;
        }

        public void BeginPrompt(Vector2 viewportTarget, float radius)
        {
            if (_promptActive) CompletePrompt("interrupted");
            _promptTarget = new Vector2(Mathf.Clamp01(viewportTarget.x), Mathf.Clamp01(viewportTarget.y));
            _promptRadius = Mathf.Clamp(radius, 0.025f, 0.15f);
            _promptStartedAt = Time.realtimeSinceStartupAsDouble;
            _stableAcquiredAt = double.NaN;
            _promptPoints.Clear();
            _recent.Clear();
            _promptActive = true;
        }

        public PromptSummary CompletePrompt(string label)
        {
            if (!_promptActive) return null;

            PromptSummary summary = new PromptSummary
            {
                label = string.IsNullOrEmpty(label) ? "prompt" : label,
                target_x = _promptTarget.x,
                target_y = _promptTarget.y,
                sample_count = _promptPoints.Count,
                stable_acquired = !double.IsNaN(_stableAcquiredAt),
            };

            if (_promptPoints.Count > 0)
            {
                float sumX = 0f;
                float sumY = 0f;
                int inside = 0;
                for (int i = 0; i < _promptPoints.Count; i++)
                {
                    Vector2 point = _promptPoints[i];
                    sumX += point.x - _promptTarget.x;
                    sumY += point.y - _promptTarget.y;
                    if (Vector2.Distance(point, _promptTarget) <= _promptRadius) inside++;
                }
                summary.bias_x = sumX / _promptPoints.Count;
                summary.bias_y = sumY / _promptPoints.Count;
                summary.occupancy = inside / (float)_promptPoints.Count;

                Vector2 median = MedianPoint(_promptPoints);
                List<float> dispersions = new List<float>(_promptPoints.Count);
                for (int i = 0; i < _promptPoints.Count; i++)
                    dispersions.Add(Vector2.Distance(_promptPoints[i], median));
                summary.dispersion_p50 = Percentile(dispersions, 0.50f);
                summary.dispersion_p90 = Percentile(dispersions, 0.90f);
            }

            if (summary.stable_acquired)
                summary.acquisition_ms = (float)Math.Max(0.0, (_stableAcquiredAt - _promptStartedAt) * 1000.0);

            _summaries.Add(summary);
            _promptActive = false;
            _promptPoints.Clear();
            _recent.Clear();
            PromptCompleted?.Invoke(summary);
            return summary;
        }

        public void CancelPrompt()
        {
            _promptActive = false;
            _promptPoints.Clear();
            _recent.Clear();
            _stableAcquiredAt = double.NaN;
        }

        public ProfileSnapshot Snapshot()
        {
            ProfileSnapshot snapshot = new ProfileSnapshot
            {
                source_mode = _sourceMode,
                total_samples = _totalSamples,
                usable_samples = _usableSamples,
                usable_fraction = _totalSamples > 0 ? _usableSamples / (float)_totalSamples : 0f,
                prompt_count = _summaries.Count,
            };

            if (_summaries.Count == 0) return snapshot;

            float biasX = 0f;
            float biasY = 0f;
            int stable = 0;
            List<float> p50 = new List<float>();
            List<float> p90 = new List<float>();
            List<float> acquisitions = new List<float>();
            List<float> occupancies = new List<float>();
            for (int i = 0; i < _summaries.Count; i++)
            {
                PromptSummary summary = _summaries[i];
                biasX += summary.bias_x;
                biasY += summary.bias_y;
                p50.Add(summary.dispersion_p50);
                p90.Add(summary.dispersion_p90);
                occupancies.Add(summary.occupancy);
                if (summary.stable_acquired)
                {
                    stable++;
                    if (summary.acquisition_ms >= 0f) acquisitions.Add(summary.acquisition_ms);
                }
            }

            snapshot.stable_prompt_count = stable;
            snapshot.bias_x = biasX / _summaries.Count;
            snapshot.bias_y = biasY / _summaries.Count;
            snapshot.dispersion_p50 = Percentile(p50, 0.50f);
            snapshot.dispersion_p90 = Percentile(p90, 0.90f);
            snapshot.occupancy_p50 = Percentile(occupancies, 0.50f);
            if (acquisitions.Count > 0)
            {
                snapshot.acquisition_ms_p50 = Percentile(acquisitions, 0.50f);
                snapshot.acquisition_ms_p90 = Percentile(acquisitions, 0.90f);
            }

            snapshot.recommended_aoi_radius = Mathf.Clamp(
                Mathf.Max(0.045f, snapshot.dispersion_p90 * 2.25f),
                0.045f,
                0.11f
            );
            float acquisitionMs = snapshot.acquisition_ms_p90 >= 0f ? snapshot.acquisition_ms_p90 : 250f;
            snapshot.recommended_acquisition_lead_ms = Mathf.RoundToInt(
                Mathf.Clamp(acquisitionMs + 100f, 250f, 800f)
            );
            return snapshot;
        }

        private void HandleSample(MindforgeGazeEventV34 sample)
        {
            _totalSamples++;
            if (sample == null || !sample.IsUsable(minimumConfidence)) return;

            _usableSamples++;
            if (!string.IsNullOrEmpty(sample.source_mode)) _sourceMode = sample.source_mode;

            double now = Time.realtimeSinceStartupAsDouble;
            Vector2 point = sample.UnityViewportPoint;
            _recent.Add(new TimedPoint { Point = point, Time = now });
            PruneRecent(now);

            if (!_promptActive) return;
            _promptPoints.Add(point);

            if (double.IsNaN(_stableAcquiredAt) && IsRecentWindowStable())
            {
                _stableAcquiredAt = now;
                StableTargetAcquired?.Invoke();
            }
        }

        private void PruneRecent(double now)
        {
            double earliest = now - Mathf.Max(0.08f, stabilityWindowSeconds);
            int remove = 0;
            while (remove < _recent.Count && _recent[remove].Time < earliest) remove++;
            if (remove > 0) _recent.RemoveRange(0, remove);
        }

        private bool IsRecentWindowStable()
        {
            if (!_promptActive || _recent.Count < Mathf.Max(3, minimumStableSamples)) return false;

            List<Vector2> points = new List<Vector2>(_recent.Count);
            int inside = 0;
            for (int i = 0; i < _recent.Count; i++)
            {
                Vector2 point = _recent[i].Point;
                points.Add(point);
                if (Vector2.Distance(point, _promptTarget) <= _promptRadius) inside++;
            }
            if (inside / (float)_recent.Count < minimumInsideFraction) return false;

            Vector2 median = MedianPoint(points);
            if (Vector2.Distance(median, _promptTarget) > _promptRadius * 0.70f) return false;

            List<float> radial = new List<float>(points.Count);
            for (int i = 0; i < points.Count; i++) radial.Add(Vector2.Distance(points[i], median));
            float limit = Mathf.Min(maximumStableDispersion, _promptRadius * 0.70f);
            return Percentile(radial, 0.90f) <= limit;
        }

        private static Vector2 MedianPoint(List<Vector2> points)
        {
            if (points == null || points.Count == 0) return Vector2.zero;
            List<float> xs = new List<float>(points.Count);
            List<float> ys = new List<float>(points.Count);
            for (int i = 0; i < points.Count; i++)
            {
                xs.Add(points[i].x);
                ys.Add(points[i].y);
            }
            return new Vector2(Percentile(xs, 0.50f), Percentile(ys, 0.50f));
        }

        private static float Percentile(List<float> values, float percentile)
        {
            if (values == null || values.Count == 0) return 0f;
            List<float> sorted = new List<float>(values);
            sorted.Sort();
            float position = Mathf.Clamp01(percentile) * (sorted.Count - 1);
            int lower = Mathf.FloorToInt(position);
            int upper = Mathf.CeilToInt(position);
            if (lower == upper) return sorted[lower];
            return Mathf.Lerp(sorted[lower], sorted[upper], position - lower);
        }
    }
}
