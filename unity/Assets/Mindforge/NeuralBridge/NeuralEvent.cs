using System;

namespace Mindforge.Neural
{
    public enum AuraTarget { None = 0, Sight = 1, Guard = 2 }

    [Serializable]
    public sealed class NeuralEvent
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

        // v2 provenance. Sentinel -1 means a source sample range was not available.
        public string session_id;
        public string calibration_id;
        public long source_sample_start = -1;
        public long source_sample_end = -1;
        public long decoder_time_ns;
        public int authority_ttl_ms;

        // Optional V0.8 derived calibration metadata. These are scalar decoder outputs,
        // never raw EEG. Older v1/v2 producers may omit them without changing behavior.
        public float stimulus_hz;
        public int candidate_rank;
        public float selected_sight_hz;
        public float selected_guard_hz;

        public AuraTarget Target
        {
            get
            {
                if (string.Equals(target, "sight", StringComparison.OrdinalIgnoreCase)) return AuraTarget.Sight;
                if (string.Equals(target, "guard", StringComparison.OrdinalIgnoreCase)) return AuraTarget.Guard;
                return AuraTarget.None;
            }
        }

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
        public bool IsCalibrationCandidateScore => string.Equals(@event, "CALIBRATION_CANDIDATE_SCORE", StringComparison.Ordinal);
        public bool IsCalibrationReady => string.Equals(@event, "CALIBRATION_READY", StringComparison.Ordinal);
        public bool IsCalibrationFailed => string.Equals(@event, "CALIBRATION_FAILED", StringComparison.Ordinal);
        public bool IsCalibrationStatus => IsCalibrationServiceReady || IsCalibrationHeartbeat || IsCalibrationCandidateScore || IsCalibrationReady || IsCalibrationFailed;
        public bool IsControl => IsLost || IsRecovered || IsParticipantStop;
    }
}
