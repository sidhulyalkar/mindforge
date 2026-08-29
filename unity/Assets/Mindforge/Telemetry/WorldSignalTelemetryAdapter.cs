using UnityEngine;
using Mindforge.World;

namespace Mindforge.Telemetry
{
    /// <summary>
    /// Passive semantic spectator/evidence adapter. It mirrors WorldSignalBus facts through
    /// the existing fire-and-forget game-marker transport, including its observer port.
    /// Loss of telemetry never changes gameplay and this component owns no game state.
    /// </summary>
    [DefaultExecutionOrder(1200)]
    public sealed class WorldSignalTelemetryAdapter : MonoBehaviour
    {
        [SerializeField] private WorldSignalBus signals;
        [SerializeField] private UdpGameMarkerSender markers;
        private bool _subscribed;

        public void ConfigureRuntime(WorldSignalBus bus, UdpGameMarkerSender markerSender)
        {
            Unsubscribe();
            signals = bus;
            markers = markerSender;
            Subscribe();
        }

        private void Awake() => Resolve();
        private void OnEnable()
        {
            Resolve();
            Subscribe();
        }
        private void OnDisable() => Unsubscribe();

        private void Resolve()
        {
            if (signals == null) signals = GetComponent<WorldSignalBus>();
            if (markers == null) markers = FindObjectOfType<UdpGameMarkerSender>(true);
        }

        private void Subscribe()
        {
            if (_subscribed || signals == null) return;
            signals.SignalPublished += OnSignal;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            if (signals != null) signals.SignalPublished -= OnSignal;
            _subscribed = false;
        }

        private void OnSignal(WorldSignal signal)
        {
            if (signal == null || markers == null) return;
            string target = !string.IsNullOrWhiteSpace(signal.state_key)
                ? signal.state_key
                : signal.subject;
            string reason = signal.kind + (string.IsNullOrWhiteSpace(signal.reason) ? string.Empty : ":" + signal.reason);
            float value = Mathf.Abs(signal.float_value) > 0.000001f ? signal.float_value : signal.int_value;
            markers.Emit(
                signal.id,
                category: "world_semantic",
                target: target,
                reason: reason,
                value: value);
        }
    }
}
