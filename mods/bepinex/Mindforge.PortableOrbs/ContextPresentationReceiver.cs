using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

namespace Mindforge.PortableOrbs
{
    /// <summary>
    /// Cosmetic game-context datagrams only. NEVER authorizes selection stimuli,
    /// modifies a game object or infers game state from unverified object names.
    /// The trusted-loopback development protocol has no authentication.
    /// </summary>
    internal sealed class ContextPresentationReceiver : IDisposable
    {
        [Serializable]
        private sealed class Frame
        {
            public string schema;
            public string game_id;
            public string session_id;
            public int seq;
            public string source;
            public string mode;
            public string placement;
            public float anchor_x;
            public float anchor_y;
            public bool selection_opportunity_only;
            public bool stimulus_enabled;
            public bool presentation_only;
            public string reason;
            public int ttl_ms;
        }

        private UdpClient socket;
        private Frame latest;
        private string session = "";
        private int lastSeq = -1;
        private float receivedAt = float.NegativeInfinity;

        public bool IsFresh
        {
            get
            {
                return latest != null && Time.realtimeSinceStartup - receivedAt <
                    Mathf.Min(0.5f, latest.ttl_ms / 1000f);
            }
        }

        public string Mode { get { return IsFresh ? latest.mode : "hidden"; } }
        public bool HasPlayerAnchor
        {
            get
            {
                return IsFresh && latest.placement == "player_anchor" &&
                    latest.source == "verified_game_hook";
            }
        }
        // Anchor is normalized screen position with TOP-LEFT origin,
        // not an uncalibrated position in Unity's world coordinates.
        public Vector2 ScreenAnchor
        {
            get
            {
                return HasPlayerAnchor
                    ? new Vector2(latest.anchor_x, latest.anchor_y)
                    : new Vector2(0.88f, 0.80f);
            }
        }
        public string Source { get { return IsFresh ? latest.source : "none"; } }

        public ContextPresentationReceiver()
        {
            socket = new UdpClient(new IPEndPoint(IPAddress.Loopback, 19755));
            socket.Client.Blocking = false;
        }

        public void Poll(string gameId)
        {
            if (socket == null)
                return;
            for (int i = 0; i < 32; i++)
            {
                try
                {
                    IPEndPoint peer = new IPEndPoint(IPAddress.Any, 0);
                    byte[] raw = socket.Receive(ref peer);
                    if (!IPAddress.IsLoopback(peer.Address) || raw.Length > 4096)
                        continue;
                    Frame next = JsonUtility.FromJson<Frame>(Encoding.UTF8.GetString(raw));
                    if (next == null || next.schema != "mindforge.orb_context_state.v1" ||
                        next.game_id != gameId || !next.presentation_only ||
                        next.stimulus_enabled || next.seq <= 0 ||
                        next.ttl_ms < 100 || next.ttl_ms > 500 ||
                        !ValidMode(next.mode) || !ValidPlacement(next.placement))
                        continue;
                    if (next.mode != "hidden" && String.IsNullOrEmpty(next.session_id))
                        continue;
                    if (next.placement == "player_anchor" &&
                        (next.source != "verified_game_hook" ||
                         Single.IsNaN(next.anchor_x) || Single.IsNaN(next.anchor_y) ||
                         next.anchor_x < 0 || next.anchor_x > 1 ||
                         next.anchor_y < 0 || next.anchor_y > 1))
                        continue;

                    float now = Time.realtimeSinceStartup;
                    bool expired = !IsFresh;
                    if (next.session_id != session && !expired)
                        continue;
                    if (expired || next.session_id != session)
                    {
                        session = next.session_id;
                        lastSeq = -1;
                    }
                    if (next.seq <= lastSeq)
                        continue;
                    lastSeq = next.seq;
                    latest = next;
                    receivedAt = now;
                }
                catch (SocketException ex)
                {
                    if (ex.SocketErrorCode == SocketError.WouldBlock ||
                        ex.SocketErrorCode == SocketError.TimedOut)
                        break;
                    // The UI fails invisible and is never a gameplay dependency.
                    latest = null;
                    break;
                }
                catch (Exception)
                {
                    // Invalid cosmetic packet. Never surface it as game authority.
                }
            }
        }

        private static bool ValidMode(string s)
        {
            return s == "hidden" || s == "primed" || s == "present" ||
                   s == "docked" || s == "resting";
        }

        private static bool ValidPlacement(string s)
        {
            return s == "none" || s == "edge_compact" || s == "player_anchor";
        }

        public void Dispose()
        {
            if (socket != null)
                socket.Close();
            socket = null;
            latest = null;
        }
    }
}
