using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace LuckySeven
{
    // Native only. WebGL cannot assume access to an external local EEG sidecar.
    public sealed class LuckyUdpBridge : MonoBehaviour
    {
        private sealed class Packet { public string Json; public long Tick; }
        private readonly Queue<Packet> pending = new Queue<Packet>();
        private readonly LuckyBrainGate gate = new LuckyBrainGate();
        private Thread worker;
        private UdpClient socket;
        private volatile bool running;
        public Action<AnimalSense> OnSense;
        public bool Calibrated { get { return gate.Ready; } }

        private void Start()
        {
#if !UNITY_WEBGL
            try
            {
                socket = new UdpClient(new IPEndPoint(IPAddress.Loopback, 19742));
                socket.Client.ReceiveTimeout = 250;
                running = true;
                worker = new Thread(ReceiveLoop) { IsBackground = true, Name = "LuckySeven EEG receiver" };
                worker.Start();
            }
            catch (Exception ex) { UnityEngine.Debug.LogWarning("EEG receiver unavailable: " + ex.Message); }
#endif
        }

        private void ReceiveLoop()
        {
            while (running)
            {
                try
                {
                    IPEndPoint remote = new IPEndPoint(IPAddress.Loopback, 0);
                    byte[] bytes = socket.Receive(ref remote);
                    if (!IPAddress.IsLoopback(remote.Address) || bytes.Length > 8192) continue;
                    Packet item = new Packet { Json = Encoding.UTF8.GetString(bytes), Tick = Stopwatch.GetTimestamp() };
                    lock (pending)
                    {
                        if (pending.Count >= 64) pending.Dequeue();
                        pending.Enqueue(item);
                    }
                }
                catch (SocketException ex)
                {
                    if (ex.SocketErrorCode != SocketError.TimedOut && running) break;
                }
                catch (ObjectDisposedException) { break; }
            }
        }

        private void Update()
        {
            // All parsing, state changes and game callbacks occur on the Unity main thread.
            for (int i = 0; i < 64; i++)
            {
                Packet item;
                lock (pending)
                {
                    if (pending.Count == 0) break;
                    item = pending.Dequeue();
                }
                try
                {
                    var decision = JsonUtility.FromJson<NeuralEventV2>(item.Json);
                    double ageMs = (Stopwatch.GetTimestamp() - item.Tick) * 1000.0 / Stopwatch.Frequency;
                    AnimalSense sense = gate.Observe(decision, ageMs);
                    if (sense != AnimalSense.None && OnSense != null) OnSense(sense);
                }
                catch (Exception) { /* malformed packets fail neutral */ }
            }
        }

        private void OnDestroy()
        {
            running = false;
            if (socket != null) socket.Close();
            if (worker != null && worker.IsAlive) worker.Join(400);
        }
    }
}
