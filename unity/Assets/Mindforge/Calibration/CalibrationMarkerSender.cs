using System;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using Mindforge.Telemetry;

namespace Mindforge.Calibration
{
    /// <summary>
    /// Sends presentation markers only. No EEG or decoder state crosses this path.
    /// New scenes publish GameMarker v1 through UdpGameMarkerSender. The legacy
    /// calibration-marker fallback remains so older serialized scenes still work.
    /// </summary>
    public sealed class CalibrationMarkerSender : MonoBehaviour
    {
        [Serializable]
        private sealed class LegacyMarkerPayload
        {
            public string schema = "mindforge.calibration_marker.v1";
            public string session_id;
            public string stage;
            public string action;
            public double unity_realtime_s;
            public float planned_duration_s;
        }

        [SerializeField] private UdpGameMarkerSender gameMarkers;
        [SerializeField] private string host = "127.0.0.1";
        [SerializeField] private int port = 19743;
        private UdpClient _legacyClient;

        public void Send(string sessionId, string stage, string action, float plannedDurationSeconds)
        {
            if (gameMarkers == null)
                gameMarkers = UnityEngine.Object.FindObjectOfType<UdpGameMarkerSender>(true);

            if (gameMarkers != null)
            {
                gameMarkers.EmitCalibration(sessionId, stage, action, plannedDurationSeconds);
                return;
            }

            if (_legacyClient == null) _legacyClient = new UdpClient();
            var payload = new LegacyMarkerPayload
            {
                session_id = sessionId,
                stage = stage,
                action = action,
                unity_realtime_s = Time.realtimeSinceStartupAsDouble,
                planned_duration_s = plannedDurationSeconds,
            };
            byte[] bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(payload));
            _legacyClient.Send(bytes, bytes.Length, host, port);
        }

        private void OnDestroy()
        {
            _legacyClient?.Close();
            _legacyClient = null;
        }
    }
}
