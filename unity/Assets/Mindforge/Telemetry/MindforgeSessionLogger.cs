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
    }

    [Serializable]
    public sealed class SessionTelemetryEnvelope
    {
        public string schema = "mindforge.session.v1";
        public string session_id;
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
            string id = DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffZ");
            _session = new SessionTelemetryEnvelope { session_id = id, started_utc = DateTime.UtcNow.ToString("O") };
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
            Add(category, evt.@event, evt.target, evt.confidence, evt.quality,
                evt.sight_score, evt.guard_score, evt.margin, evt.reason, evt.source_mode);
        }

        private void OnCalibrationStage(string stage) => Add("calibration", stage);
        private void OnDegradation(bool degraded) => Add("neural_link", degraded ? "degraded" : "recovered");
        private void OnBossPhase(int phase) => Add("boss_phase", $"phase_{phase}");
        private void OnSignalBreak() => Add("combat", "signal_break");
        private void OnFluxChanged(float before, float after, string reason) => Add("flux", reason ?? "changed");
        private void OnBossDied() => FinalizeSession("VICTORY");
        private void OnPlayerDied() => FinalizeSession("DEFEAT");

        private void Add(string category, string eventType, string target = null, float confidence = 0f,
                         float quality = 0f, float sight = 0f, float guard = 0f, float margin = 0f,
                         string reason = null, string sourceMode = null)
        {
            if (_finalized) return;
            _session.records.Add(new SessionTelemetryRecord
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
            });
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
            if (File.Exists(path)) File.Delete(path);
            File.Move(temp, path);
        }

        private void OnApplicationQuit()
        {
            if (!_finalized) FinalizeSession("INTERRUPTED");
        }
    }
}
