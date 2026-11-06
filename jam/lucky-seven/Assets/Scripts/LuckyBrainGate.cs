using System;
using UnityEngine;

namespace LuckySeven
{
    public enum AnimalSense { None, Wind, Root }

    // Wire format is the existing Mindforge neural_event.v2 schema. This is derived evidence,
    // never raw EEG. JsonUtility ignores unneeded fields rather than granting authority to them.
    [Serializable]
    public sealed class NeuralEventV2
    {
        public string schema, eventType, target, source_mode, session_id, calibration_id;
        public string @event;
        public long seq, source_sample_start, source_sample_end;
        public int authority_ttl_ms;
        public float confidence, quality;
        public bool artifact, has_evidence;
    }

    // Deliberately separate from Unity input and movement: evidence is not permission
    // until it crosses a source, calibration, freshness and monotonic-sequence boundary.
    public sealed class LuckyBrainGate
    {
        public bool Ready { get; private set; }
        public string SessionId { get; private set; }
        public string CalibrationId { get; private set; }
        public long LastSequence { get; private set; } = -1;

        public AnimalSense Observe(NeuralEventV2 e, double packetAgeMs)
        {
            if (e == null || e.schema != "mindforge.neural_event.v2"
                || e.source_mode != "live" || packetAgeMs < 0)
                return AnimalSense.None;

            if (e.@event == "CALIBRATION_READY")
            {
                if (string.IsNullOrEmpty(e.session_id) || string.IsNullOrEmpty(e.calibration_id))
                    return AnimalSense.None;
                if (e.session_id != SessionId) LastSequence = -1;
                if (e.seq <= LastSequence) return AnimalSense.None;
                SessionId = e.session_id;
                CalibrationId = e.calibration_id;
                LastSequence = e.seq;
                Ready = true;
                return AnimalSense.None;
            }
            if (e.session_id != SessionId || e.seq <= LastSequence)
                return AnimalSense.None;

            if (e.@event == "BCI_LOST" || e.@event == "PARTICIPANT_STOP"
                || e.@event == "CALIBRATION_FAILED")
            {
                Ready = false;
                LastSequence = e.seq;
                return AnimalSense.None;
            }

            LastSequence = e.seq;
            if (!Ready || e.@event != "AURA_SELECTED"
                || e.calibration_id != CalibrationId
                || !e.has_evidence || e.artifact || e.quality < 0.5f
                || e.confidence < 0.65f
                || e.source_sample_start < 0 || e.source_sample_end < e.source_sample_start
                || e.authority_ttl_ms <= 0 || e.authority_ttl_ms > 2000
                || packetAgeMs > e.authority_ttl_ms)
                return AnimalSense.None;

            if (e.target == "sight") return AnimalSense.Wind;
            if (e.target == "guard") return AnimalSense.Root;
            return AnimalSense.None;
        }
    }
}
