using System;
using System.IO;
using UnityEngine;

namespace Mindforge.Chassis
{
    /// <summary>
    /// Derived-event/session logger for BCI qualification. It records Unity/decoder
    /// metadata and semantic outcomes only. Raw EEG never enters this component.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MindforgeBciSessionLoggerV33 : MonoBehaviour
    {
        [Serializable]
        private sealed class LogRecord
        {
            public string schema = "mindforge.bci_session_event.v1";
            public string session_id;
            public string kind;
            public double unity_realtime_s;
            public float game_time_s;
            public int frame;
            public long neural_seq = -1;
            public string neural_event;
            public string target;
            public float confidence;
            public float quality;
            public float sight_score;
            public float guard_score;
            public float margin;
            public string source_mode;
            public string calibration_id;
            public long stimulus_epoch = -1;
            public int evidence_ms;
            public string reason;
            public string calibration_state;
            public float observed_refresh_hz;
            public float long_frame_fraction;
        }

        private MindforgeUdpNeuralReceiverV33 _receiver;
        private MindforgeNeuralIntentBridgeV33 _bridge;
        private MindforgeBciCalibrationDirectorV33 _calibration;
        private MindforgeNeuralWindowControllerV33 _windows;
        private MindforgeDisplayTimingMonitorV33 _timing;
        private MindforgeBciMarkerSenderV33 _markers;
        private StreamWriter _writer;

        public string LogPath { get; private set; }
        public int RecordCount { get; private set; }

        private void Start()
        {
            ResolveDependencies();
            OpenWriter();
            Bind();
            Write("session_started", reason: "V0.33 BCI integration spine");
        }

        private void OnDestroy()
        {
            Unbind();
            Write("session_ended");
            _writer?.Flush();
            _writer?.Dispose();
            _writer = null;
        }

        private void ResolveDependencies()
        {
            _receiver = GetComponent<MindforgeUdpNeuralReceiverV33>();
            _bridge = GetComponent<MindforgeNeuralIntentBridgeV33>();
            _calibration = GetComponent<MindforgeBciCalibrationDirectorV33>();
            _windows = GetComponent<MindforgeNeuralWindowControllerV33>();
            _timing = GetComponent<MindforgeDisplayTimingMonitorV33>();
            _markers = GetComponent<MindforgeBciMarkerSenderV33>();
        }

        private void Bind()
        {
            if (_receiver != null) _receiver.EvidenceReceived += HandleEvidence;
            if (_bridge != null)
            {
                _bridge.SelectionAccepted += HandleAccepted;
                _bridge.SelectionRejected += HandleRejected;
            }
            if (_calibration != null)
            {
                _calibration.StateChanged += HandleCalibrationState;
                _calibration.CalibrationRejected += HandleCalibrationRejected;
            }
            if (_windows != null)
            {
                _windows.WindowOpened += HandleWindowOpened;
                _windows.WindowResolved += HandleWindowResolved;
                _windows.WindowEnded += HandleWindowEnded;
            }
        }

        private void Unbind()
        {
            if (_receiver != null) _receiver.EvidenceReceived -= HandleEvidence;
            if (_bridge != null)
            {
                _bridge.SelectionAccepted -= HandleAccepted;
                _bridge.SelectionRejected -= HandleRejected;
            }
            if (_calibration != null)
            {
                _calibration.StateChanged -= HandleCalibrationState;
                _calibration.CalibrationRejected -= HandleCalibrationRejected;
            }
            if (_windows != null)
            {
                _windows.WindowOpened -= HandleWindowOpened;
                _windows.WindowResolved -= HandleWindowResolved;
                _windows.WindowEnded -= HandleWindowEnded;
            }
        }

        private void OpenWriter()
        {
            string directory = Path.Combine(Application.persistentDataPath, "mindforge-bci");
            Directory.CreateDirectory(directory);
            string session = _markers != null && !string.IsNullOrEmpty(_markers.SessionId)
                ? _markers.SessionId
                : Guid.NewGuid().ToString("N");
            LogPath = Path.Combine(directory, $"v33-session-{session}.jsonl");
            _writer = new StreamWriter(LogPath, append: false) { AutoFlush = true };
            Debug.Log($"[Mindforge:V33] BCI session log: {LogPath}");
        }

        private void HandleEvidence(MindforgeNeuralEventV33 evt)
        {
            if (evt == null) return;
            LogRecord record = BaseRecord("neural_evidence");
            record.neural_seq = evt.seq;
            record.neural_event = evt.@event;
            record.target = evt.target;
            record.confidence = evt.confidence;
            record.quality = evt.quality;
            record.sight_score = evt.sight_score;
            record.guard_score = evt.guard_score;
            record.margin = evt.margin;
            record.source_mode = evt.source_mode;
            record.calibration_id = evt.calibration_id;
            record.stimulus_epoch = evt.stimulus_epoch;
            record.evidence_ms = evt.evidence_ms;
            record.reason = evt.reason;
            Write(record);
        }

        private void HandleAccepted(MindforgeIntentEventV29 evt, long epoch)
        {
            LogRecord record = BaseRecord("semantic_intent_accepted");
            record.target = evt.Intent.ToString().ToLowerInvariant();
            record.confidence = evt.Confidence;
            record.source_mode = evt.Source;
            record.stimulus_epoch = epoch;
            Write(record);
        }

        private void HandleRejected(MindforgeNeuralEventV33 evt, string reason)
        {
            LogRecord record = BaseRecord("semantic_intent_rejected");
            if (evt != null)
            {
                record.neural_seq = evt.seq;
                record.target = evt.target;
                record.confidence = evt.confidence;
                record.quality = evt.quality;
                record.source_mode = evt.source_mode;
                record.stimulus_epoch = evt.stimulus_epoch;
            }
            record.reason = reason;
            Write(record);
        }

        private void HandleCalibrationState(MindforgeBciCalibrationDirectorV33.CalibrationState state)
        {
            Write("calibration_state", reason: state.ToString());
        }

        private void HandleCalibrationRejected(string reason)
        {
            Write("calibration_rejected", reason: reason);
        }

        private void HandleWindowOpened(long epoch, string reason)
        {
            LogRecord record = BaseRecord("neural_window_opened");
            record.stimulus_epoch = epoch;
            record.reason = reason;
            Write(record);
        }

        private void HandleWindowResolved(long epoch, MindforgeIntentV29 intent)
        {
            LogRecord record = BaseRecord("neural_window_resolved");
            record.stimulus_epoch = epoch;
            record.target = intent.ToString().ToLowerInvariant();
            Write(record);
        }

        private void HandleWindowEnded(long epoch, string reason)
        {
            LogRecord record = BaseRecord("neural_window_ended");
            record.stimulus_epoch = epoch;
            record.reason = reason;
            Write(record);
        }

        private LogRecord BaseRecord(string kind)
        {
            return new LogRecord
            {
                session_id = _markers != null ? _markers.SessionId : string.Empty,
                kind = kind,
                unity_realtime_s = Time.realtimeSinceStartupAsDouble,
                game_time_s = Time.time,
                frame = Time.frameCount,
                calibration_id = _calibration != null ? _calibration.CalibrationId : null,
                calibration_state = _calibration != null ? _calibration.State.ToString() : "Unknown",
                observed_refresh_hz = _timing != null ? _timing.ObservedRefreshHz : 0f,
                long_frame_fraction = _timing != null ? _timing.LongFrameFraction : 0f,
            };
        }

        private void Write(string kind, string reason = null)
        {
            LogRecord record = BaseRecord(kind);
            record.reason = reason;
            Write(record);
        }

        private void Write(LogRecord record)
        {
            if (_writer == null || record == null) return;
            try
            {
                _writer.WriteLine(JsonUtility.ToJson(record));
                RecordCount++;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Mindforge:V33] BCI session logging failed: {ex.Message}");
            }
        }
    }
}
