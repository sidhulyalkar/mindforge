using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Mindforge.LivingWorld
{
    /// <summary>
    /// Optional read-only, passive UDP inlet. It never touches NeuralEvent,
    /// UdpNeuralReceiver, gameplay systems, inputs, or rendering itself.
    /// Only binds 127.0.0.1:19747 when explicitly enabled in a scene.
    /// </summary>
    public sealed class LivingWorldPassiveReceiver : MonoBehaviour
    {
        [SerializeField] private int port = 19747;
        [SerializeField] private bool receiveEnabled = true;
        [SerializeField, Range(0f, 1f)] private float minQuality = 0.65f;
        [SerializeField] private int maxQueuedPackets = 64;
        [SerializeField] private int maxPerFrame = 32;

        private struct Packet
        {
            public string Json;
            public long ReceivedAtTicks;
            public Packet(string json, long stamp) { Json = json; ReceivedAtTicks = stamp; }
        }

        private readonly ConcurrentQueue<Packet> _messages = new ConcurrentQueue<Packet>();
        private readonly PassiveFeaturePolicy _policy = new PassiveFeaturePolicy();
        private UdpClient _udp;
        private Thread _thread;
        private volatile bool _running;
        private int _queued;
        private long _droppedForBackpressure;
        private long _droppedForAge;

        public float CurrentValue => _policy.CurrentValue;
        public bool Fresh => _policy.Fresh(Time.realtimeSinceStartupAsDouble);
        public string LastSourceMode => _policy.LastSourceMode;
        public long AcceptedCount => _policy.AcceptedCount;
        public long RejectedCount => _policy.RejectedCount;
        public long BackpressureDrops => Interlocked.Read(ref _droppedForBackpressure);
        public long AgedDrops => Interlocked.Read(ref _droppedForAge);

        private void OnEnable()
        {
            _policy.Reset();
            maxQueuedPackets = Mathf.Clamp(maxQueuedPackets, 8, 256);
            maxPerFrame = Mathf.Clamp(maxPerFrame, 1, maxQueuedPackets);
            _policy.QualityFloor = Mathf.Clamp01(minQuality);
            if (!receiveEnabled) return;
            try
            {
                _udp = new UdpClient(new IPEndPoint(IPAddress.Loopback, port));
                _udp.Client.ReceiveTimeout = 250;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[LivingWorld] Passive receiver unavailable: " + ex.GetType().Name);
                _udp?.Close();
                _udp = null;
                return; // Fail closed; no gameplay effect and no background thread
            }
            _running = true;
            _thread = new Thread(ReceiveLoop) { IsBackground = true, Name = "Mindforge-Passive-UDP" };
            _thread.Start();
        }

        private void ReceiveLoop()
        {
            while (_running)
            {
                try
                {
                    IPEndPoint sender = null;
                    byte[] bytes = _udp.Receive(ref sender);
                    if (sender == null || !IPAddress.IsLoopback(sender.Address) || bytes.Length > 4096)
                        continue;
                    _messages.Enqueue(new Packet(Encoding.UTF8.GetString(bytes), Stopwatch.GetTimestamp()));
                    int count = Interlocked.Increment(ref _queued);
                    while (count > maxQueuedPackets && _messages.TryDequeue(out _))
                    {
                        count = Interlocked.Decrement(ref _queued);
                        Interlocked.Increment(ref _droppedForBackpressure);
                    }
                }
                catch (SocketException ex) when (ex.SocketErrorCode == SocketError.TimedOut) { }
                catch (ObjectDisposedException) { break; }
                catch (Exception) { if (!_running) break; /* Ignore invalid external data. */ }
            }
        }

        private void Update()
        {
            int pulled = 0;
            PassiveFeatureFrame newest = null;
            double newestAge = 0.0;
            while (pulled++ < maxPerFrame && _messages.TryDequeue(out Packet packet))
            {
                Interlocked.Decrement(ref _queued);
                double age = Math.Max(0, (Stopwatch.GetTimestamp() - packet.ReceivedAtTicks) /
                                        (double)Stopwatch.Frequency);
                if (age > 0.75)
                {
                    Interlocked.Increment(ref _droppedForAge);
                    continue;
                }
                if (PassiveFeatureFrame.TryParse(packet.Json, out PassiveFeatureFrame frame))
                {
                    // Select most recent sequenced packet from current drain.
                    // The policy handles session changes, duplicate/out-of-order inputs.
                    if (newest == null || frame.source_session_id != newest.source_session_id ||
                        frame.seq > newest.seq)
                    {
                        newest = frame;
                        newestAge = age;
                    }
                }
            }
            if (newest != null)
                _policy.TryAccept(newest, Time.realtimeSinceStartupAsDouble, newestAge);
            _policy.Advance(Time.unscaledDeltaTime, Time.realtimeSinceStartupAsDouble);
        }

        private void OnDisable()
        {
            _running = false;
            _udp?.Close();
            _udp = null;
            if (_thread != null && _thread.IsAlive) _thread.Join(400);
            _thread = null;
            while (_messages.TryDequeue(out _)) Interlocked.Decrement(ref _queued);
            Interlocked.Exchange(ref _queued, 0);
            _policy.Reset();
        }
    }
}
