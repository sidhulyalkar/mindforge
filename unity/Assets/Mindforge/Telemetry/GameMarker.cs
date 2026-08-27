using System;

namespace Mindforge.Telemetry
{
    /// <summary>
    /// Unity -> external-tools contract. This describes presentation/gameplay facts,
    /// never raw EEG or hidden decoder state.
    /// </summary>
    [Serializable]
    public sealed class GameMarker
    {
        public const string SchemaV1 = "mindforge.game_marker.v1";

        public string schema = SchemaV1;
        public long seq;
        public string session_id;
        public string calibration_id;
        public string @event;
        public string category;
        public double unity_realtime_s;
        public float game_time_s;
        public int frame;
        public long fixed_tick;
        public string stage;
        public string action;
        public string target;
        public string reason;
        public float value;
        public int boss_phase;
        public long stimulus_epoch = -1;
        public string trial_id;
        public float planned_duration_s;
    }
}
