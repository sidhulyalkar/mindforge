using System;
using UnityEngine;

namespace Mindforge.LivingWorld
{
    /// <summary>Derived passive evidence only. NEVER a NeuralEvent or gameplay command.</summary>
    [Serializable]
    public sealed class PassiveFeatureFrame
    {
        public const string Schema = "mindforge.neural_feature_frame.v1";
        public const string Feature = "posterior_alpha_relative_power";

        public string schema;
        public long seq;
        public string source_session_id;
        public string source_mode;
        public string calibration_id;
        public string feature;
        public string unit;
        public float value;
        public float quality;
        public bool artifact;
        public bool passive_only;
        public int ttl_ms;
        public long sample_start;
        public long sample_end;
        public long producer_monotonic_ns;

        public bool Valid
        {
            get
            {
                if (schema != Schema || feature != Feature || unit != "ratio" || !passive_only)
                    return false;
                if (source_mode != "simulated_feature" && source_mode != "feature_replay" &&
                    source_mode != "eeg_replay" && source_mode != "live") return false;
                if (string.IsNullOrEmpty(source_session_id) || source_session_id.Length > 128)
                    return false;
                if (seq < 0 || ttl_ms < 100 || ttl_ms > 5000 ||
                    producer_monotonic_ns < 0 || sample_start < -1 || sample_end < -1 ||
                    (sample_start >= 0 && sample_end >= 0 && sample_end < sample_start)) return false;
                return !float.IsNaN(value) && !float.IsInfinity(value) &&
                       !float.IsNaN(quality) && !float.IsInfinity(quality) &&
                       value >= 0f && value <= 1f && quality >= 0f && quality <= 1f;
            }
        }

        public static bool TryParse(string json, out PassiveFeatureFrame frame)
        {
            frame = null;
            if (string.IsNullOrEmpty(json) || json.Length > 4096) return false;
            try
            {
                frame = JsonUtility.FromJson<PassiveFeatureFrame>(json);
                return frame != null && frame.Valid;
            }
            catch (Exception)
            {
                frame = null;
                return false;
            }
        }
    }

    /// <summary>
    /// Deterministic policy for acceptance and slow bounded visual transitions.
    /// Host receives timestamp evidence; producer monotonic timestamps are never
    /// compared against Unity's separate process clock.
    /// </summary>
    public sealed class PassiveFeaturePolicy
    {
        public const float NeutralValue = 0.5f;
        public float QualityFloor = 0.65f;
        public float SmoothingSeconds = 3.5f;
        public float MaxDeltaPerSecond = 0.10f;

        public float CurrentValue { get; private set; } = NeutralValue;
        public bool Fresh(double now) => now < _expiry;
        public long AcceptedCount { get; private set; }
        public long RejectedCount { get; private set; }
        public string LastSourceMode { get; private set; } = "none";

        private string _session;
        private long _sequence = -1;
        private float _target = NeutralValue;
        private double _expiry = double.NegativeInfinity;

        public bool TryAccept(PassiveFeatureFrame frame, double now, double packetAgeSeconds)
        {
            if (frame == null || !frame.Valid || double.IsNaN(now) ||
                double.IsInfinity(now) || double.IsNaN(packetAgeSeconds) ||
                packetAgeSeconds < 0.0 || packetAgeSeconds > 0.75)
                return Reject();

            double remaining = frame.ttl_ms / 1000.0 - packetAgeSeconds;
            if (remaining <= 0.0) return Reject();
            if (_session != null && frame.source_session_id != _session && Fresh(now))
                return Reject(); // Do not silently switch producers while current data is fresh.
            if (frame.source_session_id != _session)
            {
                _session = frame.source_session_id;
                _sequence = -1;
            }
            if (frame.seq <= _sequence) return Reject();
            _sequence = frame.seq;
            // A newer artifact/low-quality packet explicitly withdraws the previous
            // target, rather than leaving a misleading old value until TTL expiry.
            if (frame.artifact || frame.quality < QualityFloor)
            {
                _expiry = double.NegativeInfinity;
                _target = NeutralValue;
                return Reject();
            }
            _target = frame.value;
            _expiry = now + remaining;
            LastSourceMode = frame.source_mode;
            AcceptedCount++;
            return true;
        }

        private bool Reject() { RejectedCount++; return false; }

        public float Advance(float unscaledDeltaSeconds, double now)
        {
            float dt = Mathf.Clamp(unscaledDeltaSeconds, 0f, 0.1f);
            float desired = Fresh(now) ? _target : NeutralValue;
            float exponential = 1f - Mathf.Exp(-dt / Mathf.Max(0.5f, SmoothingSeconds));
            float next = Mathf.Lerp(CurrentValue, desired, exponential);
            CurrentValue = Mathf.MoveTowards(CurrentValue, next, Mathf.Max(0.001f, MaxDeltaPerSecond) * dt);
            CurrentValue = Mathf.Clamp01(CurrentValue);
            return CurrentValue;
        }

        public void Reset()
        {
            _session = null; _sequence = -1; _expiry = double.NegativeInfinity;
            _target = NeutralValue; CurrentValue = NeutralValue;
            LastSourceMode = "none";
        }
    }
}
