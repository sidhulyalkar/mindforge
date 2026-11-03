using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

namespace Mindforge.PortableOrbs
{
    /// <summary>
    /// Cuphead's stage observer may veto display. It can NEVER grant
    /// combat authority or create an orb without an independent context
    /// packet. Unknown, map, stale, unfocused and paused are all hidden.
    /// </summary>
    internal sealed class CupheadStageReceiver : IDisposable
    {
        [Serializable]
        private sealed class Frame
        {
            public string schema;
            public string game_id;
            public string session_id;
            public int seq;
            public string source;
            public string game_version;
            public string stage;
            public string @event;
            public string scene_name;
            public bool app_focused;
            public bool paused;
            public float source_time_s;
        }

        private UdpClient receiver;
        private Frame latest;
        private string session = "";
        private int lastSeq = -1;
        private float receivedAt = float.NegativeInfinity;
        private float lastSourceS = float.NegativeInfinity;
        private float lastTransitionAt = float.NegativeInfinity;
        private int consistentLevelSamples;

        public CupheadStageReceiver()
        {
            receiver = new UdpClient(new IPEndPoint(IPAddress.Loopback, 19758));
            receiver.Client.Blocking = false;
        }

        private bool IsFresh
        {
            get
            {
                return latest != null &&
                    Time.realtimeSinceStartup - receivedAt < 1.25f;
            }
        }

        public bool IsPlayableLevel
        {
            get
            {
                // A single Level.Current != null read may be stale during
                // a scene change. Two consecutive samples and a transition
                // quarantine are required even for a cosmetic indicator.
                return IsFresh && latest.stage == "level" &&
                    latest.@event == "sample" &&
                    latest.app_focused && !latest.paused &&
                    consistentLevelSamples >= 2 &&
                    Time.realtimeSinceStartup - lastTransitionAt >= 0.75f &&
                    latest.source == "observed_stage_reflection_unqualified";
            }
        }

        public void Poll()
        {
            if (receiver == null) return;
            for (int i = 0; i < 16; i++)
            {
                try
                {
                    IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
                    byte[] bytes = receiver.Receive(ref sender);
                    if (!IPAddress.IsLoopback(sender.Address) ||
                        bytes.Length > 4096) continue;
                    Frame next = JsonUtility.FromJson<Frame>(Encoding.UTF8.GetString(bytes));
                    if (next == null ||
                        next.schema != "mindforge.cuphead_stage_observation.v1" ||
                        next.game_id != "cuphead" ||
                        next.source != "observed_stage_reflection_unqualified" ||
                        String.IsNullOrEmpty(next.session_id) ||
                        String.IsNullOrEmpty(next.game_version) ||
                        next.seq <= 0 ||
                        (next.stage != "unknown" && next.stage != "map" &&
                         next.stage != "level") ||
                        (next.@event != "sample" &&
                         next.@event != "scene_changed") ||
                        (next.@event == "scene_changed" && next.stage != "unknown") ||
                        Single.IsNaN(next.source_time_s) ||
                        Single.IsInfinity(next.source_time_s) ||
                        next.source_time_s < 0f)
                        continue;
                    bool expired = !IsFresh;
                    if (!expired && next.session_id != session)
                        continue;  // prevent interleaved producer processes
                    if (expired || next.session_id != session)
                    {
                        session = next.session_id;
                        lastSeq = -1;
                        lastSourceS = float.NegativeInfinity;
                        consistentLevelSamples = 0;
                        lastTransitionAt = Time.realtimeSinceStartup;
                    }
                    if (next.seq <= lastSeq || next.source_time_s < lastSourceS)
                        continue;
                    lastSeq = next.seq;
                    lastSourceS = next.source_time_s;
                    receivedAt = Time.realtimeSinceStartup;
                    if (next.@event == "scene_changed")
                    {
                        lastTransitionAt = receivedAt;
                        consistentLevelSamples = 0;
                    }
                    else if (next.stage == "level" &&
                             next.app_focused && !next.paused)
                    {
                        // Don't reset the game-state requirement just because
                        // the source is active; count exact, consecutive
                        // verified *stage samples*, not combat actions.
                        consistentLevelSamples = Math.Min(2, consistentLevelSamples + 1);
                    }
                    else
                    {
                        consistentLevelSamples = 0;
                    }
                    latest = next;
                }
                catch (SocketException ex)
                {
                    if (ex.SocketErrorCode == SocketError.WouldBlock ||
                        ex.SocketErrorCode == SocketError.TimedOut)
                        break;
                    latest = null;
                    break;
                }
                catch (Exception)
                {
                    // A malformed or missing stage sample only hides the UI.
                }
            }
        }

        public void Dispose()
        {
            if (receiver != null) receiver.Close();
            receiver = null;
            latest = null;
            consistentLevelSamples = 0;
        }
    }
}
