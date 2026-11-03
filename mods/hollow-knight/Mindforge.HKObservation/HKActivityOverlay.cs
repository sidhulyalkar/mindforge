using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

namespace Mindforge.HollowKnightObservation
{
    /// <summary>
    /// Optional read-only cosmetic activity diagnostic. Not a neural target,
    /// no flash, no controls, no game objects except this isolated overlay.
    /// The native Modding API observer owns this component's lifecycle.
    /// </summary>
    internal sealed class HKActivityOverlay : MonoBehaviour
    {
        [Serializable]
        private sealed class ContextFrame
        {
            public string schema;
            public string game_id;
            public string session_id;
            public int seq;
            public string source;
            public string mode;
            public string placement;
            public bool presentation_only;
            public bool stimulus_enabled;
            public bool selection_opportunity_only;
            public int ttl_ms;
        }

        private UdpClient receiver;
        private ContextFrame latest;
        private string session = "";
        private int lastSeq = -1;
        private float receivedAt = float.NegativeInfinity;
        private Texture2D marker;
        private GUIStyle labelStyle;

        public bool GameplayAllowed { get; set; }

        private void Awake()
        {
            marker = NewMarker();
            try
            {
                receiver = new UdpClient(new IPEndPoint(IPAddress.Loopback, 19755));
                receiver.Client.Blocking = false;
            }
            catch (SocketException)
            {
                receiver = null;
                // Receiver unavailable: the visual simply never appears.
            }
        }

        private bool IsFresh
        {
            get
            {
                return latest != null && Time.realtimeSinceStartup - receivedAt <
                       Mathf.Min(0.5f, latest.ttl_ms / 1000f);
            }
        }

        private void Update()
        {
            if (receiver == null) return;
            // Strict, bounded read budget. Do not block rendering or gameplay.
            for (int i = 0; i < 16; i++)
            {
                try
                {
                    IPEndPoint peer = new IPEndPoint(IPAddress.Any, 0);
                    byte[] raw = receiver.Receive(ref peer);
                    if (!IPAddress.IsLoopback(peer.Address) || raw.Length > 4096) continue;
                    ContextFrame next = JsonUtility.FromJson<ContextFrame>(
                        Encoding.UTF8.GetString(raw));
                    if (next == null ||
                        next.schema != "mindforge.orb_context_state.v1" ||
                        next.game_id != "hollow_knight" ||
                        !next.presentation_only || next.stimulus_enabled ||
                        next.selection_opportunity_only ||
                        next.ttl_ms < 100 || next.ttl_ms > 500 || next.seq < 1 ||
                        (next.mode != "primed" && next.mode != "hidden") ||
                        (next.mode == "primed" && next.placement != "edge_compact") ||
                        (next.mode == "hidden" && next.placement != "none") ||
                        (next.source != "observed_game_hook_unqualified" &&
                         next.source != "synthetic_game_context" &&
                         next.source != "none"))
                        continue;
                    if (next.mode == "primed" && String.IsNullOrEmpty(next.session_id))
                        continue;
                    float now = Time.realtimeSinceStartup;
                    bool expired = !IsFresh;
                    if (next.session_id != session && !expired) continue;
                    if (expired || next.session_id != session)
                    {
                        session = next.session_id;
                        lastSeq = -1;
                    }
                    if (next.seq <= lastSeq) continue;
                    lastSeq = next.seq;
                    latest = next;
                    receivedAt = now;
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
                    // Unknown context can never modify the game.
                }
            }
        }

        private void OnGUI()
        {
            if (!GameplayAllowed || !Application.isFocused ||
                Time.timeScale <= 0.0001f || !IsFresh ||
                latest.mode != "primed" || marker == null)
                return;

            float width = Screen.width;
            float height = Screen.height;
            float cx = width * 0.88f;
            float cy = height * 0.70f;
            Color prior = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, 0.5f);
            GUI.DrawTexture(new Rect(cx, cy, 20f, 20f), marker);
            GUI.color = prior;
            if (labelStyle == null)
            {
                labelStyle = new GUIStyle(GUI.skin.label);
                labelStyle.fontSize = 10;
                labelStyle.alignment = TextAnchor.MiddleRight;
                labelStyle.normal.textColor = Color.white;
            }
            string label = latest.source == "synthetic_game_context"
                ? "SIMULATED ACTIVITY" : "PLAYER ACTIVITY ONLY";
            GUI.Label(new Rect(cx - 128f, cy + 25f, 150f, 18f),
                      label, labelStyle);
        }

        private static Texture2D NewMarker()
        {
            const int side = 32;
            Texture2D t = new Texture2D(side, side, TextureFormat.ARGB32, false);
            Color32[] pixels = new Color32[side * side];
            for (int y = 0; y < side; y++)
                for (int x = 0; x < side; x++)
                {
                    float dx = (x - 15.5f) / 15.5f;
                    float dy = (y - 15.5f) / 15.5f;
                    float radius = Mathf.Sqrt(dx * dx + dy * dy);
                    byte alpha = (byte)(Mathf.Clamp01((1f - radius) * 6f) * 255f);
                    pixels[y * side + x] = new Color32(95, 194, 244, alpha);
                }
            t.SetPixels32(pixels);
            t.Apply(false, true);
            return t;
        }

        private void OnDestroy()
        {
            if (receiver != null) receiver.Close();
            receiver = null;
            if (marker != null) Destroy(marker);
            marker = null;
        }
    }
}
