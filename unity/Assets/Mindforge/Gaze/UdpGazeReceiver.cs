using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Mindforge.Gaze
{
    /// <summary>
    /// Receives bounded GazeEvent samples on loopback only. Network I/O stays off the
    /// Unity thread and gameplay observes at most the newest valid sample each frame.
    /// Gaze is attention evidence, never combat authority by itself.
    /// </summary>
    public sealed class UdpGazeReceiver : MonoBehaviour
    {
        [SerializeField] private int port = 19746;
        [SerializeField] private float staleAfterSeconds = 0.55f;
        [SerializeField] private float maxPacketQueueAgeSeconds = 0.18f;
        [SerializeField] private int maxQueuedPackets = 96;
        [SerializeField] private int maxDrainPerFrame = 64;
        [SerializeField] private bool logSamples;

        public event Action<GazeEvent> SampleReceived;
        public event Action<bool> ConnectionStateChanged;

        private readonly struct ReceivedPacket
        {
            public readonly string Json;
            public readonly long ReceiveTicks;

            public ReceivedPacket(string json, long receiveTicks)
            {
                Json = json;
                ReceiveTicks = receiveTicks;
            }
        }

        private readonly ConcurrentQueue<ReceivedPacket> _messages = new ConcurrentQueue<ReceivedPacket>();
        private UdpClient _client;
        private Thread _thread;
        private volatile bool _running;
        private bool _connected;
        private long _lastSeenSequence = -1;
        private double _lastValidSampleTime = double.NegativeInfinity;
        private int _queuedCount;
        private long _droppedForBackpressure;
        private long _droppedForAge;
        private long _droppedMalformed;

        public bool IsConnected => _connected;
        public int Port => port;
        public int QueueDepth => Volatile.Read(ref _queuedCount);
        public long LastSeenSequence => _lastSeenSequence;
        public long DroppedForBackpressure => Interlocked.Read(ref _droppedForBackpressure);
        public long DroppedForAge => Interlocked.Read(ref _droppedForAge);
        public long DroppedMalformed => Interlocked.Read(ref _droppedMalformed);

        private void OnEnable()
        {
            maxQueuedPackets = Mathf.Max(8, maxQueuedPackets);
            maxDrainPerFrame = Mathf.Clamp(maxDrainPerFrame, 1, maxQueuedPackets);
            maxPacketQueueAgeSeconds = Mathf.Max(0.03f, maxPacketQueueAgeSeconds);
            staleAfterSeconds = Mathf.Max(maxPacketQueueAgeSeconds, staleAfterSeconds);
            _lastSeenSequence = -1;
            _lastValidSampleTime = double.NegativeInfinity;
            DrainPending();

            try
            {
                _client = new UdpClient(new IPEndPoint(IPAddress.Loopback, port));
                _client.Client.ReceiveTimeout = 250;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Mindforge:Gaze] Could not bind UDP {port}: {ex.GetType().Name}");
                _client = null;
                return;
            }

            _running = true;
            _thread = new Thread(ReceiveLoop)
            {
                IsBackground = true,
                Name = "Mindforge-Gaze-UDP"
            };
            _thread.Start();
        }

        private void ReceiveLoop()
        {
            while (_running && _client != null)
            {
                try
                {
                    IPEndPoint remote = null;
                    byte[] bytes = _client.Receive(ref remote);
                    if (remote != null && !IPAddress.IsLoopback(remote.Address)) continue;
                    Enqueue(Encoding.UTF8.GetString(bytes));
                }
                catch (SocketException ex) when (ex.SocketErrorCode == SocketError.TimedOut) { }
                catch (ObjectDisposedException) { break; }
                catch (Exception ex)
                {
                    Enqueue($"__ERROR__:{ex.GetType().Name}");
                }
            }
        }

        private void Enqueue(string raw)
        {
            _messages.Enqueue(new ReceivedPacket(raw, Stopwatch.GetTimestamp()));
            int depth = Interlocked.Increment(ref _queuedCount);
            while (depth > maxQueuedPackets && _messages.TryDequeue(out _))
            {
                Interlocked.Decrement(ref _queuedCount);
                Interlocked.Increment(ref _droppedForBackpressure);
                depth--;
            }
        }

        private static double PacketAgeSeconds(ReceivedPacket packet)
        {
            long elapsed = Stopwatch.GetTimestamp() - packet.ReceiveTicks;
            return Math.Max(0.0, elapsed / (double)Stopwatch.Frequency);
        }

        private void Update()
        {
            GazeEvent newest = null;
            long newestSequence = _lastSeenSequence;
            int drained = 0;

            while (drained < maxDrainPerFrame && _messages.TryDequeue(out ReceivedPacket packet))
            {
                drained++;
                Interlocked.Decrement(ref _queuedCount);

                if (packet.Json.StartsWith("__ERROR__:", StringComparison.Ordinal))
                {
                    SetConnected(false);
                    continue;
                }

                if (PacketAgeSeconds(packet) > maxPacketQueueAgeSeconds)
                {
                    Interlocked.Increment(ref _droppedForAge);
                    continue;
                }

                GazeEvent sample;
                try
                {
                    sample = JsonUtility.FromJson<GazeEvent>(packet.Json);
                }
                catch
                {
                    Interlocked.Increment(ref _droppedMalformed);
                    continue;
                }

                if (sample == null || !sample.HasSupportedSchema || !sample.IsFinite)
                {
                    Interlocked.Increment(ref _droppedMalformed);
                    continue;
                }
                if (sample.seq <= _lastSeenSequence || sample.seq <= newestSequence) continue;

                newest = sample;
                newestSequence = sample.seq;
            }

            if (newest != null)
            {
                _lastSeenSequence = newestSequence;
                _lastValidSampleTime = Time.realtimeSinceStartupAsDouble;
                SetConnected(true);
                if (logSamples)
                    Debug.Log($"[Mindforge:Gaze] {newest.source_mode} ({newest.x:F3},{newest.y:F3}) c={newest.confidence:F2} fix={newest.fixation}");
                SampleReceived?.Invoke(newest);
            }

            if (_connected && Time.realtimeSinceStartupAsDouble - _lastValidSampleTime > staleAfterSeconds)
                SetConnected(false);
        }

        private void SetConnected(bool value)
        {
            if (_connected == value) return;
            _connected = value;
            ConnectionStateChanged?.Invoke(value);
        }

        private void DrainPending()
        {
            while (_messages.TryDequeue(out _)) Interlocked.Decrement(ref _queuedCount);
            if (Volatile.Read(ref _queuedCount) < 0) Interlocked.Exchange(ref _queuedCount, 0);
        }

        private void OnDisable()
        {
            _running = false;
            _client?.Close();
            _client = null;
            if (_thread != null && _thread.IsAlive) _thread.Join(500);
            _thread = null;
            DrainPending();
            SetConnected(false);
        }
    }
}
