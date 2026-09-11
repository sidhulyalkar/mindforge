using System;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

namespace Mindforge.Chassis
{
    /// <summary>
    /// Unity-to-Python marker lane. It reports presentation/calibration/window facts
    /// only. Raw EEG and decoder internals never travel on this channel.
    ///
    /// V0.34 binds every calibration/window/tutorial marker to the currently frozen
    /// stimulus layout through the existing trial_id field. This preserves the v1
    /// transport schema while preventing evidence from becoming detached from the
    /// visual geometry that produced it.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MindforgeBciMarkerSenderV33 : MonoBehaviour
    {
        [Serializable]
        private sealed class MarkerPayload
        {
            public string schema = "mindforge.game_marker.v1";
            public long seq;
            public string session_id;
            public string calibration_id;
            public string @event;
            public string category;
            public double unity_realtime_s;
            public float game_time_s;
            public int frame;
            public int fixed_tick = -1;
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

        [SerializeField] private string host = "127.0.0.1";
        [SerializeField] private int processingPort = 19743;
        [SerializeField] private bool mirrorForObservation = true;
        [SerializeField] private int observationPort = 19745;

        private UdpClient _client;
        private long _seq;

        public string SessionId { get; private set; }
        public long LastSequence => _seq;
        public string CurrentLayoutId
        {
            get
            {
                MindforgeBciStimulusV33 stimulus = GetComponent<MindforgeBciStimulusV33>();
                return stimulus != null ? stimulus.LayoutId : null;
            }
        }

        private void Awake()
        {
            SessionId = Guid.NewGuid().ToString("N");
            _client = new UdpClient();
        }

        public void SendCalibrationStage(string calibrationId, string stage, string action, float plannedDurationSeconds)
        {
            Send(new MarkerPayload
            {
                seq = ++_seq,
                session_id = SessionId,
                calibration_id = calibrationId,
                @event = "CALIBRATION_STAGE",
                category = "calibration",
                unity_realtime_s = Time.realtimeSinceStartupAsDouble,
                game_time_s = Time.time,
                frame = Time.frameCount,
                stage = stage,
                action = action,
                trial_id = CurrentLayoutId,
                planned_duration_s = plannedDurationSeconds,
            });
        }

        public void SendTutorialStage(string stage, string action, string reason = null, float value = 0f)
        {
            Send(new MarkerPayload
            {
                seq = ++_seq,
                session_id = SessionId,
                @event = "TUTORIAL_STAGE",
                category = "tutorial",
                unity_realtime_s = Time.realtimeSinceStartupAsDouble,
                game_time_s = Time.time,
                frame = Time.frameCount,
                stage = stage,
                action = action,
                reason = reason,
                value = value,
                trial_id = CurrentLayoutId,
            });
        }

        public void SendNeuralWindow(string eventName, long epoch, string reason = null)
        {
            Send(new MarkerPayload
            {
                seq = ++_seq,
                session_id = SessionId,
                @event = eventName,
                category = "neural_window",
                unity_realtime_s = Time.realtimeSinceStartupAsDouble,
                game_time_s = Time.time,
                frame = Time.frameCount,
                stimulus_epoch = epoch,
                reason = reason,
                trial_id = CurrentLayoutId,
            });
        }

        public void SendSemanticSelection(MindforgeIntentV29 intent, float confidence, string source, long epoch)
        {
            Send(new MarkerPayload
            {
                seq = ++_seq,
                session_id = SessionId,
                @event = "SEMANTIC_INTENT_APPLIED",
                category = "bci_semantic",
                unity_realtime_s = Time.realtimeSinceStartupAsDouble,
                game_time_s = Time.time,
                frame = Time.frameCount,
                target = intent.ToString().ToLowerInvariant(),
                value = Mathf.Clamp01(confidence),
                reason = source,
                stimulus_epoch = epoch,
                trial_id = CurrentLayoutId,
            });
        }

        private void Send(MarkerPayload payload)
        {
            if (_client == null) return;
            string json = JsonUtility.ToJson(payload);
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            try
            {
                _client.Send(bytes, bytes.Length, host, processingPort);
                if (mirrorForObservation && observationPort != processingPort)
                    _client.Send(bytes, bytes.Length, host, observationPort);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Mindforge:V33] Marker send failed: {ex.Message}");
            }
        }

        private void OnDestroy()
        {
            _client?.Close();
            _client = null;
        }
    }
}
