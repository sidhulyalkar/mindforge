using System;
using System.Collections.Generic;
using UnityEngine;
using Mindforge.World;

namespace Mindforge.Telemetry
{
    [Serializable]
    public sealed class CompetitiveRunSplit
    {
        public string id;
        public string subject;
        public long signal_sequence;
        public double elapsed_s;
        public int ordinal;
    }

    /// <summary>
    /// Passive run observer for future time-trial, tournament and spectator surfaces.
    /// It reads semantic facts only and cannot issue player commands, schedule encounters,
    /// mutate progression or grant neural authority. Splits use realtime so pause/loading
    /// policy can later be made explicit per competitive ruleset instead of hiding in combat.
    /// </summary>
    [DefaultExecutionOrder(1200)]
    public sealed class CompetitiveRunObserverV1 : MonoBehaviour
    {
        [SerializeField] private WorldSignalBus signals;
        [SerializeField] private List<CompetitiveRunSplit> splits = new List<CompetitiveRunSplit>();

        private double _startedRealtime;
        private bool _running;
        private bool _complete;

        public event Action<CompetitiveRunSplit> SplitRecorded;
        public event Action<double> RunCompleted;

        public IReadOnlyList<CompetitiveRunSplit> Splits => splits;
        public bool Running => _running && !_complete;
        public bool Complete => _complete;
        public double ElapsedSeconds => _running
            ? Math.Max(0.0, Time.realtimeSinceStartupAsDouble - _startedRealtime)
            : 0.0;

        public void ConfigureRuntime(WorldSignalBus signalBus)
        {
            Unsubscribe();
            signals = signalBus;
            Subscribe();
            ResetRun();
        }

        private void Awake()
        {
            if (signals == null) signals = GetComponent<WorldSignalBus>();
        }

        private void OnEnable()
        {
            if (signals == null) signals = GetComponent<WorldSignalBus>();
            Subscribe();
            if (!_running) ResetRun();
        }

        private void OnDisable() => Unsubscribe();

        public void ResetRun()
        {
            splits.Clear();
            _startedRealtime = Time.realtimeSinceStartupAsDouble;
            _running = true;
            _complete = false;
        }

        private void Subscribe()
        {
            if (signals == null) return;
            signals.SignalPublished -= OnSignal;
            signals.SignalPublished += OnSignal;
        }

        private void Unsubscribe()
        {
            if (signals != null) signals.SignalPublished -= OnSignal;
        }

        private void OnSignal(WorldSignal signal)
        {
            if (!_running || _complete || signal == null) return;
            if (!ShouldSplit(signal)) return;

            CompetitiveRunSplit split = new CompetitiveRunSplit
            {
                id = signal.id ?? string.Empty,
                subject = signal.subject ?? string.Empty,
                signal_sequence = signal.sequence,
                elapsed_s = ElapsedSeconds,
                ordinal = splits.Count + 1,
            };
            splits.Add(split);
            SplitRecorded?.Invoke(split);

            signals.Publish(
                WorldSignalKind.RunSplit,
                "run.split",
                subject: split.subject,
                stringValue: split.id,
                intValue: split.ordinal,
                floatValue: (float)split.elapsed_s,
                reason: "passive_observer");

            if (signal.kind == WorldSignalKind.WorldCompleted)
            {
                _complete = true;
                RunCompleted?.Invoke(split.elapsed_s);
            }
        }

        private static bool ShouldSplit(WorldSignal signal)
        {
            if (signal.kind == WorldSignalKind.RunSplit) return false;
            return signal.kind == WorldSignalKind.RegionEntered ||
                   signal.kind == WorldSignalKind.EncounterWaveCleared ||
                   signal.kind == WorldSignalKind.EncounterCleared ||
                   signal.kind == WorldSignalKind.BossStarted ||
                   signal.kind == WorldSignalKind.WorldCompleted;
        }
    }
}
