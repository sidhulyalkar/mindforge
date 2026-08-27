using System;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using Mindforge.Neural;

namespace Mindforge.SoulWisp
{
    /// <summary>
    /// Development-only manual neural fixture. The historical class name is retained
    /// so existing scene references do not break, but this component no longer applies
    /// buffs directly. Q/E generate NeuralEvent v2 packets on the production localhost
    /// transport, so manual development exercises the same receiver, freshness and
    /// authority path as replay, synthetic EEG and live EEG.
    ///
    /// Never enable this component in observed-hardware builds.
    /// </summary>
    public sealed class SimulatedAuraInput : MonoBehaviour
    {
        [SerializeField] private int neuralEventPort = 19742;
        [SerializeField] private float secondsToSelection = 1.2f;
        [SerializeField] private int authorityTtlMs = 900;

        private float _sight;
        private float _guard;
        private long _seq;
        private UdpClient _client;
        private string _sessionId;

        private void Awake()
        {
            _client = new UdpClient();
            _sessionId = $"unity-manual-{Guid.NewGuid():N}";
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
                Emit("sight");
                _sight = 0f;
            }
            if (_guard >= secondsToSelection)
            {
                Emit("guard");
                _guard = 0f;
            }
        }

        private void Emit(string target)
        {
            _seq++;
            bool sight = string.Equals(target, "sight", StringComparison.Ordinal);
            NeuralEvent evt = new NeuralEvent
            {
                schema = NeuralEvent.SchemaV2,
                seq = _seq,
                monotonic_ns = 0,
                decoder_time_ns = 0,
                @event = "AURA_SELECTED",
                target = target,
                confidence = 0.90f,
                quality = 0.92f,
                paradigm = "manual_fixture",
                model_id = "unity-manual-fixture-v1",
                artifact = false,
                reason = "MANUAL_DEV_SELECTION",
                has_evidence = true,
                sight_score = sight ? 0.75f : 0.10f,
                guard_score = sight ? 0.10f : 0.75f,
                margin = 0.65f,
                source_mode = "manual",
                session_id = _sessionId,
                calibration_id = string.Empty,
                source_sample_start = -1,
                source_sample_end = -1,
                authority_ttl_ms = Mathf.Max(1, authorityTtlMs),
            };

            byte[] bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(evt));
            try
            {
                _client.Send(bytes, bytes.Length, "127.0.0.1", neuralEventPort);
            }
            catch (SocketException ex)
            {
                Debug.LogWarning($"[ManualBCI] localhost send failed: {ex.SocketErrorCode}");
            }
        }

        private void OnDestroy()
        {
            _client?.Close();
            _client = null;
        }
    }
}
