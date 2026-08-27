using System;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using Mindforge.Calibration;
using Mindforge.Telemetry;

namespace Mindforge.SoulWisp
{
    [Serializable]
    public sealed class ManualNeuralIntent
    {
        public string schema = "mindforge.manual_intent.v1";
        public string session_id;
        public string calibration_id;
        public string target;
        public double unity_realtime_s;
    }

    /// <summary>
    /// Development-only Q/E intent capture. The historical class name is retained so
    /// old scene references remain readable, but this component no longer emits
    /// authoritative NeuralEvents itself. It publishes a tiny dev-only intent on UDP
    /// 19746; the Python manual-service owns calibration, liveness, sequencing and the
    /// single authoritative NeuralEvent stream into Unity.
    ///
    /// The component is installed only when the explicit -mindforgeManualBCI command
    /// line flag is present. It therefore cannot activate accidentally in live builds.
    /// </summary>
    public sealed class SimulatedAuraInput : MonoBehaviour
    {
        [SerializeField] private int manualIntentPort = 19746;
        [SerializeField] private float secondsToSelection = 1.2f;
        [SerializeField] private AwakeningCalibrationDirector calibration;

        private float _sight;
        private float _guard;
        private UdpClient _client;

        private void Awake()
        {
            _client = new UdpClient();
            if (calibration == null)
                calibration = Object.FindObjectOfType<AwakeningCalibrationDirector>(true);
        }

        private void Update()
        {
            bool sight = Input.GetKey(KeyCode.Q);
            bool guard = Input.GetKey(KeyCode.E);
            float decay = Time.unscaledDeltaTime * 0.8f;
            _sight = sight && !guard ? _sight + Time.unscaledDeltaTime : Mathf.Max(0f, _sight - decay);
            _guard = guard && !sight ? _guard + Time.unscaledDeltaTime : Mathf.Max(0f, _guard - decay);

            if (_sight >= secondsToSelection)
            {
                EmitIntent("sight");
                _sight = 0f;
            }
            if (_guard >= secondsToSelection)
            {
                EmitIntent("guard");
                _guard = 0f;
            }
        }

        private void EmitIntent(string target)
        {
            ManualNeuralIntent intent = new ManualNeuralIntent
            {
                session_id = MindforgeSessionContext.GameSessionId,
                calibration_id = calibration != null ? calibration.SessionId : string.Empty,
                target = target,
                unity_realtime_s = Time.realtimeSinceStartupAsDouble,
            };

            byte[] bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(intent));
            try
            {
                _client.Send(bytes, bytes.Length, "127.0.0.1", manualIntentPort);
            }
            catch (SocketException ex)
            {
                Debug.LogWarning($"[ManualBCI] dev-intent send failed: {ex.SocketErrorCode}");
            }
        }

        private void OnDestroy()
        {
            _client?.Close();
            _client = null;
        }
    }

    /// <summary>Explicit command-line installation only. No flag, no manual source.</summary>
    public static class ManualAuraInputBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            bool enabled = false;
            foreach (string arg in Environment.GetCommandLineArgs())
                enabled |= string.Equals(arg, "-mindforgeManualBCI", StringComparison.OrdinalIgnoreCase);
            if (!enabled || Object.FindObjectOfType<SimulatedAuraInput>(true) != null) return;

            GameObject root = GameObject.Find("MindforgeManualBCI");
            if (root == null) root = new GameObject("MindforgeManualBCI");
            root.AddComponent<SimulatedAuraInput>();
            Debug.Log("[Mindforge] Manual BCI intent capture enabled by -mindforgeManualBCI.");
        }
    }
}
