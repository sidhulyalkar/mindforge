using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Mindforge.Calibration;
using Mindforge.Combat;
using Mindforge.Neural;

namespace Mindforge.Telemetry
{
    [Serializable]
    public sealed class SessionTelemetryRecord
    {
        public double realtime_s;
        public float game_time_s;
        public string category;
        public string event_type;
        public string target;
        public float confidence;
        public float quality;
        public float sight_score;
        public float guard_score;
        public float margin;
        public string reason;
        public string source_mode;
        public int boss_phase;
        public float boss_health;
        public float player_health;
        public float flux;

        // Additive NeuralEvent v2 provenance. Non-neural records leave these at
        // sentinel/default values so session.v1 readers remain backward compatible.
        public string neural_schema;
        public long neural_seq = -1;
        public string neural_session_id;
        public string calibration_id;
        public long source_sample_start = -1;
        public long source_sample_end = -1;
        public long decoder_time_ns;
        public int authority_ttl_ms;

        // Unity-local transport state at the instant the record was observed.
        public int transport_queue_depth;
        public long dropped_packet_age;
        public long dropped_backpressure;
        public long dropped_expired_authority;
    }

    [Serializable]
    public sealed class SessionTelemetryEnvelope
    {
        // v2 fields above are additive. Keep the envelope identifier stable so old
        // competition report tooling can continue to read new captures.
        public string schema = "mindforge.session.v1";
        public string session_id;
        public string calibration_session_id;
        public string started_utc;
        public string ended_utc;
        public string outcome;
        public string source_mode;
        public List<SessionTelemetryRecord> records = new List<SessionTelemetryRecord>();
    }

    /// <summary>
    /// Derived-event/gameplay logger. It deliberately records no raw EEG.
    /// A rolling partial checkpoint protects the demo artifact from a crash/forced quit.
    /// </summary>
    public sealed class MindforgeSessionLogger : MonoBehaviour
    {
        [SerializeField] private UdpNeuralReceiver receiver;
        [SerializeField] private AwakeningCalibrationDirector calibration;
        [SerializeField] private NeuralLinkContingency linkContingency;
        [SerializeField] private FracturedSignalDirector bossDirector;
        [SerializeField] private CombatantVitals bossVitals;
        [SerializeField] private CombatantVitals playerVitals;
        [SerializeField] private FluxMeter flux;
        [SerializeField] private float checkpointEverySeconds = 5f;

        private SessionTelemetryEnvelope _session;
        private double _nextCheckpoint;
        private bool _finalized;
        private string _directory;
        private string _partialPath;

        public string LatestExportPath { get; private set; }

        private void Awake()
        {
            string id = MindforgeSessionContext.GameSessionId;
            _session = new SessionTelemetryEnvelope
            {
                session_id = id,
                started_utc = MindforgeSessionContext.StartedUtc,
            };
            _directory = Path.Combine(Application.persistentDataPath, "mindforge_sessions");
            Directory.CreateDirectory(_directory);
            _partialPath = Path.Combine(_directory, $"mindforge-{id}.partial.json");
            _nextCheckpoint = Time.realtimeSinceStartupAsDouble + Mathf.Max(1f, checkpointEverySeconds);
        }

        private void OnEnable()
        {
            if (receiver != null)
            {
                receiver.EvidenceReceived += OnEvidence;
                receiver.EventReceived += OnAuthority;
            }
            if (calibration != null) calibration.CalibrationStageChanged += OnCalibrationStage;
            if (linkContingency != null) linkContingency.DegradationStateChanged += OnDegradation;
            if (bossDirector != null) bossDirector.PhaseChanged += OnBossPhase;
            if (bossVitals != null)
            {
                bossVitals.Died += OnBossDied;
                if (bossVitals.Poise != null) bossVitals.Poise.BrokenEvent += OnSignalBreak;
            }
            if (playerVitals != null) playerVitals.Died += OnPlayerDied;
            if (flux != null) flux.Changed += OnFluxChanged;
        }

        private void OnDisable()
        {
            if (receiver != null)
            {
                receiver.EvidenceReceived -= OnEvidence;
                receiver.EventReceived -= OnAuthority;
            }
            if (calibration != null) calibration.CalibrationStageChanged -= OnCalibrationStage;
            if (linkContingency != null) linkContingency.DegradationStateChanged -= OnDegradation;
            if (bossDirector != null) bossDirector.PhaseChanged -= OnBossPhase;
            if (bossVitals != null)
            {
                bossVitals.Died -= OnBossDied;
                if (bossVitals.Poise != null) bossVitals.Poise.BrokenEvent -= OnSignalBreak;
            }
            if (playerVitals != null) playerVitals.Died -= OnPlayerDied;
            if (flux != null) flux.Changed -= OnFluxChanged;
        }

        private void Update()
        {
            if (_finalized || Time.realtimeSinceStartupAsDouble < _nextCheckpoint) return;
            _nextCheckpoint = Time.realtimeSinceStartupAsDouble + Mathf.Max(1f, checkpointEverySeconds);
            Write(_partialPath, "IN_PROGRESS", false);
        }

        private void OnEvidence(NeuralEvent evt) => AppendNeural("neural_evidence", evt);
        private void OnAuthority(NeuralEvent evt) => AppendNeural("neural_authority", evt);

        private void AppendNeural(string category, NeuralEvent evt)
        {
            if (evt == null || _finalized) return;
            if (!string.IsNullOrEmpty(evt.source_mode)) _session.source_mode = evt.source_mode;
            SessionTelemetryRecord record = Add(
                category,
                evt.@event,
                evt.target,
                evt.confidence,
                evt.quality,
                evt.sight_score,
                evt.guard_score,
                evt.margin,
                evt.reason,
                evt.source_mode);
            if (record == null) return;

            record.neural_schema = evt.schema;
            record.neural_seq = evt.seq;
            record.neural_session_id = evt.session_id;
            record.calibration_id = evt.calibration_id;
            record.source_sample_start = evt.source_sample_start;
            record.source_sample_end = evt.source_sample_end;
            record.decoder_time_ns = evt.decoder_time_ns;
            record.authority_ttl_ms = evt.authority_ttl_ms;
            if (receiver != null)
            {
                record.transport_queue_depth = receiver.QueueDepth;
                record.dropped_packet_age = receiver.DroppedForAge;
                record.dropped_backpressure = receiver.DroppedForBackpressure;
                record.dropped_expired_authority = receiver.DroppedExpiredAuthority;
            }
        }

        private void OnCalibrationStage(string stage)
        {
            if (calibration != null && !string.IsNullOrEmpty(calibration.SessionId))
                _session.calibration_session_id = calibration.SessionId;
            Add("calibration", stage, _session.calibration_session_id);
        }

        private void OnDegradation(bool degraded) => Add("neural_link", degraded ? "degraded" : "recovered");
        private void OnBossPhase(int phase) => Add("boss_phase", $"phase_{phase}");
        private void OnSignalBreak() => Add("combat", "signal_break");
        private void OnFluxChanged(float before, float after, string reason) => Add("flux", reason ?? "changed");
        private void OnBossDied() => FinalizeSession("VICTORY");
        private void OnPlayerDied() => FinalizeSession("DEFEAT");

        private SessionTelemetryRecord Add(
            string category,
            string eventType,
            string target = null,
            float confidence = 0f,
            float quality = 0f,
            float sight = 0f,
            float guard = 0f,
            float margin = 0f,
            string reason = null,
            string sourceMode = null)
        {
            if (_finalized) return null;
            SessionTelemetryRecord record = new SessionTelemetryRecord
            {
                realtime_s = Time.realtimeSinceStartupAsDouble,
                game_time_s = Time.time,
                category = category,
                event_type = eventType,
                target = target,
                confidence = confidence,
                quality = quality,
                sight_score = sight,
                guard_score = guard,
                margin = margin,
                reason = reason,
                source_mode = sourceMode,
                boss_phase = bossDirector != null ? bossDirector.Phase : 0,
                boss_health = bossVitals != null ? bossVitals.Health : -1f,
                player_health = playerVitals != null ? playerVitals.Health : -1f,
                flux = flux != null ? flux.Value : -1f,
            };
            _session.records.Add(record);
            return record;
        }

        public void FinalizeSession(string outcome)
        {
            if (_finalized) return;
            Add("session", "end");
            _finalized = true;
            string finalPath = Path.Combine(_directory, $"mindforge-{_session.session_id}.json");
            Write(finalPath, outcome, true);
            LatestExportPath = finalPath;
            try { if (File.Exists(_partialPath)) File.Delete(_partialPath); } catch { }
            Debug.Log($"[Mindforge] Session telemetry exported: {finalPath}");
        }

        private void Write(string path, string outcome, bool final)
        {
            _session.outcome = outcome;
            if (final) _session.ended_utc = DateTime.UtcNow.ToString("O");
            string json = JsonUtility.ToJson(_session, true);
            string temp = path + ".tmp";
            File.WriteAllText(temp, json);
            ReplaceCheckpoint(temp, path);
        }

        private static void ReplaceCheckpoint(string temp, string path)
        {
            if (!File.Exists(path))
            {
                File.Move(temp, path);
                return;
            }

            try
            {
                // Same-directory replacement is atomic on the normal desktop filesystems
                // used for competition builds. Keep the previous checkpoint intact until
                // the new temp file has been fully written.
                File.Replace(temp, path, null);
            }
            catch (PlatformNotSupportedException)
            {
                // Last-resort portability path. Qualification should verify the primary
                // File.Replace path on the actual demo machine before competition day.
                File.Delete(path);
                File.Move(temp, path);
            }
        }

        private void OnApplicationQuit()
        {
            if (!_finalized) FinalizeSession("INTERRUPTED");
        }
    }
}
