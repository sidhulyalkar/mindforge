using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Mindforge.Chassis
{
    /// <summary>
    /// Bounded localhost receiver for derived neural decisions. Socket I/O stays off
    /// the Unity thread; stale/backlogged packets are discarded and only the newest
    /// authoritative event in a frame is allowed downstream. A new decoder model/session
    /// may explicitly reset sequence authority with CALIBRATION_SERVICE_READY, which
    /// keeps local development robust across Python process restarts.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MindforgeUdpNeuralReceiverV33 : MonoBehaviour
    {
        [SerializeField] private int port = 19742;
        [SerializeField] private float staleAfterSeconds = 1.5f;
        [SerializeField] private float maxPacketQueueAgeSeconds = 0.75f;
        [SerializeField] private int maxQueuedPackets = 128;
        [SerializeField] private int maxDrainPerFrame = 96;
        [SerializeField] private bool logEvents;

        public event Action<MindforgeNeuralEventV33> EventReceived;
        public event Action<MindforgeNeuralEventV33> EvidenceReceived;
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
        private int _queuedCount;
        private bool _connected;
        private long _lastSeenSeq = -1;
        private long _lastAuthoritySeq = -1;
        private string _activeModelId;
        private double _lastValidEventTime = double.NegativeInfinity;
        private long _droppedForBackpressure;
        private long _droppedForAge;
        private long _droppedExpiredAuthority;
        private long _droppedForeignModel;

        public bool IsConnected => _connected;
        public int Port => port;
        public int QueueDepth => Volatile.Read(ref _queuedCount);
        public long LastSeenSequence => _lastSeenSeq;
        public string ActiveModelId => _activeModelId;
        public long DroppedForBackpressure => Interlocked.Read(ref _droppedForBackpressure);
        public long DroppedForAge => Interlocked.Read(ref _droppedForAge);
        public long DroppedExpiredAuthority => Interlocked.Read(ref _droppedExpiredAuthority);
        public long DroppedForeignModel => Interlocked.Read(ref _droppedForeignModel);

        private void OnEnable()
        {
            maxQueuedPackets = Mathf.Max(8, maxQueuedPackets);
            maxDrainPerFrame = Mathf.Clamp(maxDrainPerFrame, 1, maxQueuedPackets);
            maxPacketQueueAgeSeconds = Mathf.Max(0.05f, maxPacketQueueAgeSeconds);
            staleAfterSeconds = Mathf.Max(maxPacketQueueAgeSeconds, staleAfterSeconds);
            _lastSeenSeq = -1;
            _lastAuthoritySeq = -1;
            _activeModelId = null;
            _lastValidEventTime = double.NegativeInfinity;
            DrainPending();

            try
            {
                _client = new UdpClient(new IPEndPoint(IPAddress.Loopback, port));
                _client.Client.ReceiveTimeout = 250;
                _running = true;
                _thread = new Thread(ReceiveLoop)
                {
                    IsBackground = true,
                    Name = "Mindforge-V33-Neural-UDP",
                };
                _thread.Start();
            }
            catch (Exception ex)
            {
                _running = false;
                Debug.LogError($"[Mindforge:V33] Could not bind neural UDP {port}: {ex.Message}");
            }
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
                catch (Exception ex)
                {
                    Enqueue("__ERROR__:" + ex.GetType().Name);
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

        private static MindforgeNeuralEventV33 Newer(MindforgeNeuralEventV33 a, MindforgeNeuralEventV33 b)
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

        private static bool AuthorityExpired(MindforgeNeuralEventV33 evt, double packetAgeSeconds)
        {
            if (evt == null || !evt.IsSelection || evt.authority_ttl_ms <= 0) return false;
            return packetAgeSeconds * 1000.0 > evt.authority_ttl_ms;
        }

        private bool AcceptModelIdentity(MindforgeNeuralEventV33 evt)
        {
            string modelId = string.IsNullOrEmpty(evt.model_id) ? "unknown" : evt.model_id;
            if (string.IsNullOrEmpty(_activeModelId))
            {
                _activeModelId = modelId;
                return true;
            }
            if (string.Equals(_activeModelId, modelId, StringComparison.Ordinal))
                return true;

            // A fresh decoder process restarts its sequence counter. Only an explicit
            // service-ready event may transfer authority to a new model identity.
            if (evt.IsCalibrationServiceReady)
            {
                _activeModelId = modelId;
                _lastSeenSeq = -1;
                _lastAuthoritySeq = -1;
                Debug.Log($"[Mindforge:V33] Neural decoder authority reset to model {modelId}.");
                return true;
            }

            Interlocked.Increment(ref _droppedForeignModel);
            return false;
        }

        private void Update()
        {
            MindforgeNeuralEventV33 latestEvidence = null;
            MindforgeNeuralEventV33 latestPassive = null;
            MindforgeNeuralEventV33 latestSelection = null;
            MindforgeNeuralEventV33 latestControl = null;
            MindforgeNeuralEventV33 participantStop = null;
            long frameMaxSeq = _lastSeenSeq;
            int drained = 0;

            while (drained < maxDrainPerFrame && _messages.TryDequeue(out ReceivedPacket packet))
            {
                drained++;
                Interlocked.Decrement(ref _queuedCount);
                string raw = packet.Json;
                if (raw.StartsWith("__ERROR__:", StringComparison.Ordinal))
                {
                    SetConnected(false);
                    continue;
                }

                MindforgeNeuralEventV33 evt;
                try
                {
                    evt = JsonUtility.FromJson<MindforgeNeuralEventV33>(raw);
                }
                catch
                {
                    continue;
                }

                if (evt == null || !evt.HasSupportedSchema || !AcceptModelIdentity(evt)) continue;
                if (evt.seq <= _lastSeenSeq) continue;

                double packetAge = PacketAgeSeconds(packet);
                bool critical = evt.IsParticipantStop || evt.IsLost || evt.IsRecovered;
                if (!critical && packetAge > maxPacketQueueAgeSeconds)
                {
                    Interlocked.Increment(ref _droppedForAge);
                    continue;
                }

                frameMaxSeq = Math.Max(frameMaxSeq, evt.seq);
                latestEvidence = Newer(latestEvidence, evt);

                if (evt.IsParticipantStop)
                    participantStop = Newer(participantStop, evt);
                else if (evt.IsLost || evt.IsRecovered)
                    latestControl = Newer(latestControl, evt);
                else if (evt.IsSelection)
                {
                    if (AuthorityExpired(evt, packetAge))
                        Interlocked.Increment(ref _droppedExpiredAuthority);
                    else
                        latestSelection = Newer(latestSelection, evt);
                }
                else
                    latestPassive = Newer(latestPassive, evt);
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
                EventReceived?.Invoke(participantStop);
                SetConnected(false);
                DrainPending();
                Debug.LogWarning("[Mindforge:V33] PARTICIPANT_STOP received; neural authority cleared.");
                return;
            }

            MindforgeNeuralEventV33 authority = Newer(latestSelection, latestControl);
            if (authority == null) authority = latestPassive;
            if (authority != null && authority.seq > _lastAuthoritySeq)
            {
                _lastAuthoritySeq = authority.seq;
                if (logEvents)
                {
                    Debug.Log(
                        $"[Mindforge:V33] neural {authority.@event} target={authority.target ?? "-"} " +
                        $"c={authority.confidence:F2} q={authority.quality:F2} epoch={authority.stimulus_epoch}"
                    );
                }
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
