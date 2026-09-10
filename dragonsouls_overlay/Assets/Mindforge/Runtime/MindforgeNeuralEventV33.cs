using System;

namespace Mindforge.Chassis
{
    /// <summary>
    /// Derived neural-event transport contract for the Dragon Souls production chassis.
    /// This deliberately mirrors the Python mindforge.neural_event.v1/v2 schema while
    /// carrying no raw EEG into Unity.
    /// </summary>
    [Serializable]
    public sealed class MindforgeNeuralEventV33
    {
        public const string SchemaV1 = "mindforge.neural_event.v1";
        public const string SchemaV2 = "mindforge.neural_event.v2";

        public string schema;
        public long seq;
        public long monotonic_ns;
        public string @event;
        public string target;
        public float confidence;
        public float quality;
        public string paradigm;
        public string model_id;
        public bool artifact;
        public string reason;
        public bool has_evidence;
        public float sight_score;
        public float guard_score;
        public float margin;
        public string source_mode;
        public string session_id;
        public string calibration_id;
        public long source_sample_start = -1;
        public long source_sample_end = -1;
        public long decoder_time_ns;
        public int authority_ttl_ms;
        public long stimulus_epoch = -1;
        public int evidence_ms;
        public float stimulus_hz;
        public int candidate_rank;
        public float selected_sight_hz;
        public float selected_guard_hz;

        public bool HasSupportedSchema =>
            string.Equals(schema, SchemaV1, StringComparison.Ordinal) ||
            string.Equals(schema, SchemaV2, StringComparison.Ordinal);

        public bool IsV2 => string.Equals(schema, SchemaV2, StringComparison.Ordinal);
        public bool IsSelection => string.Equals(@event, "AURA_SELECTED", StringComparison.Ordinal);
        public bool IsAbstain => string.Equals(@event, "ABSTAIN", StringComparison.Ordinal);
        public bool IsHeartbeat => string.Equals(@event, "BCI_HEARTBEAT", StringComparison.Ordinal);
        public bool IsLost => string.Equals(@event, "BCI_LOST", StringComparison.Ordinal);
        public bool IsRecovered => string.Equals(@event, "BCI_RECOVERED", StringComparison.Ordinal);
        public bool IsParticipantStop => string.Equals(@event, "PARTICIPANT_STOP", StringComparison.Ordinal);
        public bool IsCalibrationServiceReady => string.Equals(@event, "CALIBRATION_SERVICE_READY", StringComparison.Ordinal);
        public bool IsCalibrationHeartbeat => string.Equals(@event, "CALIBRATION_HEARTBEAT", StringComparison.Ordinal);
        public bool IsCalibrationReady => string.Equals(@event, "CALIBRATION_READY", StringComparison.Ordinal);
        public bool IsCalibrationFailed => string.Equals(@event, "CALIBRATION_FAILED", StringComparison.Ordinal);
        public bool IsCalibrationStatus => IsCalibrationServiceReady || IsCalibrationHeartbeat || IsCalibrationReady || IsCalibrationFailed;

        public MindforgeIntentV29 Intent
        {
            get
            {
                if (string.Equals(target, "sight", StringComparison.OrdinalIgnoreCase)) return MindforgeIntentV29.Sight;
                if (string.Equals(target, "guard", StringComparison.OrdinalIgnoreCase)) return MindforgeIntentV29.Guard;
                return MindforgeIntentV29.None;
            }
        }
    }
}
