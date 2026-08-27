using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace Mindforge.Neural
{
    /// <summary>
    /// Receives derived neural events only. Raw EEG must never cross this boundary.
    /// Network I/O stays on a dedicated background thread. Unity-main-thread work is
    /// bounded and latest-authoritative so a render stall cannot burst stale/conflicting
    /// neural state changes into one gameplay frame.
    ///
    /// Python monotonic_ns and Unity realtime do not share an epoch. Cross-process
    /// timestamps remain provenance/order metadata; packet age is measured using the
    /// Unity process receive clock captured on the socket thread.
    /// </summary>
    public sealed class UdpNeuralReceiver : MonoBehaviour
    {
        [SerializeField] private int port = 19742;
        [SerializeField] private float staleAfterSeconds = 1.5f;
        [SerializeField] private float maxPacketQueueAgeSeconds = 0.75f;
        [SerializeField] private int maxQueuedPackets = 128;
        [SerializeField] private int maxDrainPerFrame = 96;
        [SerializeField] private bool logEvents;

        public event Action<NeuralEvent> EventReceived;
        public event Action<NeuralEvent> EvidenceReceived;
        public event Action<bool> ConnectionStateChanged;

        private readonly struct ReceivedPacket
        {
            public readonly string Json;
            public readonly long ReceiveTicks;
            public ReceivedPacket(string json, long receiveTicks) { Json = json; ReceiveTicks = receiveTicks; }
        }

        private readonly ConcurrentQueue<ReceivedPacket> _messages = new ConcurrentQueue<ReceivedPacket>();
        private UdpClient _client;
        private Thread _thread;
        private volatile bool _running;
        private double _lastValidEventTime = double.NegativeInfinity;
        private bool _connected;
        private long _lastSeenSeq = -1;
        private long _lastAuthoritySeq = -1;
        private int _queuedCount;
        private long _droppedForBackpressure;
        private long _droppedForAge;

        public bool IsConnected => _connected;
        public int QueueDepth => Volatile.Read(ref _queuedCount);
        public long DroppedForBackpressure => Interlocked.Read(ref _droppedForBackpressure);
        public long DroppedForAge => Interlocked.Read(ref _droppedForAge);
        public long LastSeenSequence => _lastSeenSeq;

        private void OnEnable()
        {
            maxQueuedPackets = Mathf.Max(8, maxQueuedPackets);
            maxDrainPerFrame = Mathf.Clamp(maxDrainPerFrame, 1, maxQueuedPackets);
            maxPacketQueueAgeSeconds = Mathf.Max(0.05f, maxPacketQueueAgeSeconds);
            staleAfterSeconds = Mathf.Max(maxPacketQueueAgeSeconds, staleAfterSeconds);
            _lastSeenSeq = -1;
            _lastAuthoritySeq = -1;
            _lastValidEventTime = double.NegativeInfinity;
            DrainPending();
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
                    Enqueue(Encoding.UTF8.GetString(bytes));
                }
                catch (SocketException ex) when (ex.SocketErrorCode == SocketError.TimedOut) { }
                catch (ObjectDisposedException) { break; }
                catch (Exception ex) { Enqueue($"__ERROR__:{ex.GetType().Name}"); }
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

        private static NeuralEvent Newer(NeuralEvent a, NeuralEvent b)
        {
            if (a == null) return b;
            if (b == null) return a;
            return b.seq > a.seq ? b : a;
        }

        private static double PacketAgeSeconds(ReceivedPacket packet)
        {
            long elapsed = Stopwatch.GetTimestamp() - packet.ReceiveTicks;
            return Math.Max(0.0, elapsed / (double)Stopwatch.Frequency);
        }

        private void Update()
        {
            NeuralEvent latestEvidence = null;
            NeuralEvent latestSelection = null;
            NeuralEvent latestControl = null;
            NeuralEvent participantStop = null;
            long frameMaxSeq = _lastSeenSeq;
            int drained = 0;

            while (drained < maxDrainPerFrame && _messages.TryDequeue(out ReceivedPacket packet))
            {
                drained++;
                Interlocked.Decrement(ref _queuedCount);
                string raw = packet.Json;
                if (raw.StartsWith("__ERROR__:", StringComparison.Ordinal)) { SetConnected(false); continue; }

                NeuralEvent evt;
                try { evt = JsonUtility.FromJson<NeuralEvent>(raw); }
                catch { continue; }
                if (evt == null || evt.schema != "mindforge.neural_event.v1") continue;
                if (evt.seq <= _lastSeenSeq) continue;

                bool critical = evt.IsParticipantStop || evt.IsLost || evt.IsRecovered;
                if (!critical && PacketAgeSeconds(packet) > maxPacketQueueAgeSeconds)
                {
                    Interlocked.Increment(ref _droppedForAge);
                    continue;
                }

                frameMaxSeq = Math.Max(frameMaxSeq, evt.seq);
                latestEvidence = Newer(latestEvidence, evt);
                if (evt.IsParticipantStop) participantStop = Newer(participantStop, evt);
                else if (evt.IsLost || evt.IsRecovered) latestControl = Newer(latestControl, evt);
                else if (evt.IsSelection) latestSelection = Newer(latestSelection, evt);
            }

            if (latestEvidence != null)
            {
                _lastSeenSeq = Math.Max(_lastSeenSeq, frameMaxSeq);
                _lastValidEventTime = Time.realtimeSinceStartupAsDouble;
                if (!latestEvidence.IsLost && !latestEvidence.IsParticipantStop) SetConnected(true);
                EvidenceReceived?.Invoke(latestEvidence);
            }

            if (participantStop != null)
            {
                _lastAuthoritySeq = Math.Max(_lastAuthoritySeq, participantStop.seq);
                if (logEvents) Debug.LogWarning("[BCI] PARTICIPANT_STOP");
                EventReceived?.Invoke(participantStop);
                SetConnected(false);
                DrainPending();
                return;
            }

            NeuralEvent authority = Newer(latestSelection, latestControl);
            if (authority == null) authority = latestEvidence;
            if (authority != null && authority.seq > _lastAuthoritySeq)
            {
                _lastAuthoritySeq = authority.seq;
                if (logEvents)
                    Debug.Log($"[BCI] {authority.@event} {authority.target} c={authority.confidence:F2} q={authority.quality:F2} queue={QueueDepth}");
                EventReceived?.Invoke(authority);
            }

            if (latestControl != null)
            {
                if (latestControl.IsLost) SetConnected(false);
                else if (latestControl.IsRecovered) SetConnected(true);
            }

            if (_connected && Time.realtimeSinceStartupAsDouble - _lastValidEventTime > staleAfterSeconds)
                SetConnected(false);
        }

        private void DrainPending()
        {
            while (_messages.TryDequeue(out _)) Interlocked.Decrement(ref _queuedCount);
            if (Volatile.Read(ref _queuedCount) < 0) Interlocked.Exchange(ref _queuedCount, 0);
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
            DrainPending();
            SetConnected(false);
        }
    }
}
