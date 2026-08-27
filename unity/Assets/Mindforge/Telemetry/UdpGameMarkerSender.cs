using System;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

namespace Mindforge.Telemetry
{
    /// <summary>
    /// Publishes typed Unity-originated facts to acquisition/replay/qualification
    /// processes. The sender is intentionally fire-and-forget: loss of a recorder
    /// must never stall gameplay or grant neural authority.
    /// </summary>
    public sealed class UdpGameMarkerSender : MonoBehaviour
    {
        [SerializeField] private string host = "127.0.0.1";
        [SerializeField] private int port = 19743;
        [SerializeField] private bool logMarkers;

        private UdpClient _client;
        private long _seq;
        private long _fixedTick;
        private string _sessionId;

        public string SessionId => _sessionId;
        public long LastSequence => _seq;

        private void Awake()
        {
            _sessionId = DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffZ");
            _client = new UdpClient();
        }

        private void FixedUpdate() => _fixedTick++;

        public void Emit(
            string eventType,
            string category = "game",
            string target = null,
            string reason = null,
            float value = 0f,
            int bossPhase = 0,
            long stimulusEpoch = -1,
            string trialId = null)
        {
            EmitWithSession(
                _sessionId,
                eventType,
                category,
                null,
                null,
                target,
                reason,
                value,
                bossPhase,
                stimulusEpoch,
                trialId,
                0f);
        }

        public void EmitCalibration(
            string sessionId,
            string stage,
            string action,
            float plannedDurationSeconds)
        {
            EmitWithSession(
                sessionId,
                "CALIBRATION_STAGE",
                "calibration",
                stage,
                action,
                null,
                null,
                0f,
                0,
                -1,
                null,
                plannedDurationSeconds);
        }

        public void EmitWithSession(
            string sessionId,
            string eventType,
            string category,
            string stage,
            string action,
            string target,
            string reason,
            float value,
            int bossPhase,
            long stimulusEpoch,
            string trialId,
            float plannedDurationSeconds)
        {
            if (_client == null) _client = new UdpClient();
            GameMarker marker = new GameMarker
            {
                seq = ++_seq,
                session_id = string.IsNullOrEmpty(sessionId) ? _sessionId : sessionId,
                @event = eventType ?? "CUSTOM",
                category = category ?? "game",
                unity_realtime_s = Time.realtimeSinceStartupAsDouble,
                game_time_s = Time.time,
                frame = Time.frameCount,
                fixed_tick = _fixedTick,
                stage = stage ?? string.Empty,
                action = action ?? string.Empty,
                target = target ?? string.Empty,
                reason = reason ?? string.Empty,
                value = value,
                boss_phase = bossPhase,
                stimulus_epoch = stimulusEpoch,
                trial_id = trialId ?? string.Empty,
                planned_duration_s = plannedDurationSeconds,
            };

            byte[] bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(marker));
            try
            {
                _client.Send(bytes, bytes.Length, host, port);
                if (logMarkers)
                    Debug.Log($"[GameMarker] #{marker.seq} {marker.category}/{marker.@event} {marker.action}");
            }
            catch (SocketException ex)
            {
                // Telemetry is evidence, never gameplay authority. A missing recorder
                // may be visible in logs but must not interrupt the participant.
                if (logMarkers) Debug.LogWarning($"[GameMarker] UDP send failed: {ex.SocketErrorCode}");
            }
        }

        private void OnDestroy()
        {
            _client?.Close();
            _client = null;
        }
    }
}
