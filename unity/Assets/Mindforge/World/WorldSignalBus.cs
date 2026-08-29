using System;
using UnityEngine;

namespace Mindforge.World
{
    public enum WorldSignalKind
    {
        Milestone = 0,
        RegionEntered = 1,
        EncounterStarted = 2,
        EncounterWaveStarted = 3,
        EncounterWaveCleared = 4,
        EncounterCleared = 5,
        Checkpoint = 6,
        BossStarted = 7,
        WorldCompleted = 8,
        StateChanged = 9,
        QuestActivated = 10,
        QuestAdvanced = 11,
        QuestCompleted = 12,
        Interaction = 13,
        StoryDiscovered = 14,
        ProgressionChanged = 15,
        RewardGranted = 16,
        RunSplit = 17,
    }

    [Serializable]
    public sealed class WorldSignal
    {
        public long sequence;
        public long fixed_tick;
        public double realtime_s;
        public float game_time_s;
        public WorldSignalKind kind;
        public string id;
        public string subject;
        public string state_key;
        public string string_value;
        public int int_value;
        public float float_value;
        public string reason;

        public WorldSignal Copy()
        {
            return new WorldSignal
            {
                sequence = sequence,
                fixed_tick = fixed_tick,
                realtime_s = realtime_s,
                game_time_s = game_time_s,
                kind = kind,
                id = id,
                subject = subject,
                state_key = state_key,
                string_value = string_value,
                int_value = int_value,
                float_value = float_value,
                reason = reason,
            };
        }
    }

    /// <summary>
    /// Scene-local semantic event stream. Concrete gameplay authorities publish facts here;
    /// observers such as quests, save-state adapters and spectators consume them. Publishing
    /// a signal has no gameplay side effect and never changes movement, combat or BCI state.
    /// </summary>
    [DefaultExecutionOrder(-820)]
    public sealed class WorldSignalBus : MonoBehaviour
    {
        private long _sequence;

        public static WorldSignalBus Instance { get; private set; }
        public event Action<WorldSignal> SignalPublished;
        public long LastSequence => _sequence;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogError("[Mindforge:WorldSignals] More than one WorldSignalBus exists in the active scene.");
                enabled = false;
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public WorldSignal Publish(
            WorldSignalKind kind,
            string id,
            string subject = null,
            string stateKey = null,
            string stringValue = null,
            int intValue = 0,
            float floatValue = 0f,
            string reason = null)
        {
            if (!enabled) return null;
            float dt = Mathf.Max(0.0001f, Time.fixedDeltaTime);
            WorldSignal signal = new WorldSignal
            {
                sequence = ++_sequence,
                fixed_tick = (long)Math.Round(Time.fixedTime / dt),
                realtime_s = Time.realtimeSinceStartupAsDouble,
                game_time_s = Time.time,
                kind = kind,
                id = string.IsNullOrWhiteSpace(id) ? "world.unknown" : id.Trim(),
                subject = subject ?? string.Empty,
                state_key = stateKey ?? string.Empty,
                string_value = stringValue ?? string.Empty,
                int_value = intValue,
                float_value = floatValue,
                reason = reason ?? string.Empty,
            };
            SignalPublished?.Invoke(signal);
            return signal;
        }
    }
}
