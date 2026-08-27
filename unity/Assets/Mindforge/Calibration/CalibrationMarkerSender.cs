using System;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

namespace Mindforge.Calibration
{
    /// <summary>
    /// Sends presentation markers only. No EEG or decoder state crosses this path.
    /// Python uses these markers to label the continuously acquired LSL stream.
    /// </summary>
    public sealed class CalibrationMarkerSender : MonoBehaviour
    {
        [Serializable]
        private sealed class MarkerPayload
        {
            public string schema = "mindforge.calibration_marker.v1";
            public string session_id;
            public string stage;
            public string action;
            public double unity_realtime_s;
            public float planned_duration_s;
        }

        [SerializeField] private string host = "127.0.0.1";
        [SerializeField] private int port = 19743;
        private UdpClient _client;

        private void Awake() => _client = new UdpClient();

        public void Send(string sessionId, string stage, string action, float plannedDurationSeconds)
        {
            if (_client == null) _client = new UdpClient();
            var payload = new MarkerPayload
            {
                session_id = sessionId,
                stage = stage,
                action = action,
                unity_realtime_s = Time.realtimeSinceStartupAsDouble,
                planned_duration_s = plannedDurationSeconds,
            };
            byte[] bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(payload));
            _client.Send(bytes, bytes.Length, host, port);
        }

        private void OnDestroy()
        {
            _client?.Close();
            _client = null;
        }
    }
}
