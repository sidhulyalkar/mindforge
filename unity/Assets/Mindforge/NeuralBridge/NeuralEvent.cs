using System;

namespace Mindforge.Neural
{
    public enum AuraTarget { None = 0, Sight = 1, Guard = 2 }

    [Serializable]
    public sealed class NeuralEvent
    {
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

        public AuraTarget Target
        {
            get
            {
                if (string.Equals(target, "sight", StringComparison.OrdinalIgnoreCase)) return AuraTarget.Sight;
                if (string.Equals(target, "guard", StringComparison.OrdinalIgnoreCase)) return AuraTarget.Guard;
                return AuraTarget.None;
            }
        }

        public bool IsSelection => string.Equals(@event, "AURA_SELECTED", StringComparison.Ordinal);
        public bool IsAbstain => string.Equals(@event, "ABSTAIN", StringComparison.Ordinal);
        public bool IsLost => string.Equals(@event, "BCI_LOST", StringComparison.Ordinal);
        public bool IsRecovered => string.Equals(@event, "BCI_RECOVERED", StringComparison.Ordinal);
        public bool IsParticipantStop => string.Equals(@event, "PARTICIPANT_STOP", StringComparison.Ordinal);
        public bool IsControl => IsLost || IsRecovered || IsParticipantStop;
    }
}
