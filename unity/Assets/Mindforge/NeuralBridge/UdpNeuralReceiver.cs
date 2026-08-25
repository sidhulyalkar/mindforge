using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace Mindforge.Neural
{
    /// <summary>
    /// Receives derived neural events only. Raw EEG must never cross this boundary.
    /// Network work stays off the Unity main thread; JSON parsing and callbacks run
    /// in Update so gameplay state is never mutated from a socket thread.
    /// </summary>
    public sealed class UdpNeuralReceiver : MonoBehaviour
    {
        [SerializeField] private int port = 19742;
        [SerializeField] private float staleAfterSeconds = 2.5f;
        [SerializeField] private bool logEvents;

        public event Action<NeuralEvent> EventReceived;
        public event Action<bool> ConnectionStateChanged;

        private readonly ConcurrentQueue<string> _messages = new ConcurrentQueue<string>();
        private UdpClient _client;
        private Thread _thread;
        private volatile bool _running;
        private double _lastValidEventTime = double.NegativeInfinity;
        private bool _connected;
        private long _lastSeq = -1;

        public bool IsConnected => _connected;

        private void OnEnable()
        {
            _client = new UdpClient(new IPEndPoint(IPAddress.Loopback, port));
            _client.Client.ReceiveTimeout = 250;
            _running = true;
            _thread = new Thread(ReceiveLoop) { IsBackground = true, Name = "Mindforge-Neural-UDP" };
            _thread.Start();
        }

        private void ReceiveLoop()
        {
            while (_running)
            {
                try
                {
                    IPEndPoint remote = null;
                    byte[] bytes = _client.Receive(ref remote);
                    _messages.Enqueue(Encoding.UTF8.GetString(bytes));
                }
                catch (SocketException ex) when (ex.SocketErrorCode == SocketError.TimedOut)
                {
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _messages.Enqueue($"__ERROR__:{ex.GetType().Name}");
                }
            }
        }

        private void Update()
        {
            while (_messages.TryDequeue(out string raw))
            {
                if (raw.StartsWith("__ERROR__:", StringComparison.Ordinal))
                {
                    SetConnected(false);
                    continue;
                }

                NeuralEvent evt;
                try { evt = JsonUtility.FromJson<NeuralEvent>(raw); }
                catch { continue; }

                if (evt == null || evt.schema != "mindforge.neural_event.v1") continue;
                if (evt.seq <= _lastSeq) continue;
                _lastSeq = evt.seq;
                _lastValidEventTime = Time.realtimeSinceStartupAsDouble;
                SetConnected(true);

                if (logEvents) Debug.Log($"[BCI] {evt.@event} {evt.target} c={evt.confidence:F2} q={evt.quality:F2}");
                EventReceived?.Invoke(evt);
            }

            if (_connected && Time.realtimeSinceStartupAsDouble - _lastValidEventTime > staleAfterSeconds)
                SetConnected(false);
        }

        private void SetConnected(bool value)
        {
            if (_connected == value) return;
            _connected = value;
            ConnectionStateChanged?.Invoke(value);
        }

        private void OnDisable()
        {
            _running = false;
            _client?.Close();
            _client = null;
            if (_thread != null && _thread.IsAlive) _thread.Join(500);
            _thread = null;
            SetConnected(false);
        }
    }
}
