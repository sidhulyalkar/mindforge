using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

namespace Mindforge.PortableOrbs
{
    // Intentionally cosmetic. No player modification, input injection, game hooks,
    // artificial 10/12 Hz flicker, or EEG data are allowed in this first adapter.
    [BepInPlugin("dev.mindforge.portableorbs", "Mindforge Portable Orbs", "0.1.0")]
    public sealed class PortableOrbsPlugin : BaseUnityPlugin
    {
        [Serializable]
        private sealed class ModFrame
        {
            public string schema;
            public string game_id;
            public int seq;
            public string session_id;
            public string source_mode;
            public string link;
            public bool sight_active;
            public bool guard_active;
            public int ttl_ms;
            public bool presentation_only;
        }

        private UdpClient receiver;
        private ModFrame latest;
        private string gameId;
        private string sessionId;
        private int lastSeq = -1;
        private float receivedAt = float.NegativeInfinity;
        private Texture2D blueOrb;
        private Texture2D greenOrb;
        private GUIStyle captionStyle;
        private ConfigEntry<bool> enabledOverlay;
        private ConfigEntry<bool> showOffline;

        private void Awake()
        {
            enabledOverlay = Config.Bind("Overlay", "Enabled", true, "Enable the cosmetic Mindforge overlay.");
            showOffline = Config.Bind("Overlay", "ShowWhenOffline", false,
                "Show faint indicator orbs when the bridge is disconnected.");
            string product = (Application.productName ?? "").ToLowerInvariant();
            gameId = product.Contains("silksong") ? "silksong" :
                     product.Contains("cuphead") ? "cuphead" : "";
            if (gameId.Length == 0)
            {
                Logger.LogWarning("Unsupported game; Mindforge Portable Orbs disabled.");
                enabled = false;
                return;
            }

            blueOrb = MakeOrb(new Color32(56, 173, 255, 255));
            greenOrb = MakeOrb(new Color32(88, 230, 154, 255));
            try
            {
                receiver = new UdpClient(new IPEndPoint(IPAddress.Loopback, 19753));
                receiver.Client.Blocking = false;
                Logger.LogInfo("Cosmetic Mindforge bridge ready for " + gameId);
            }
            catch (SocketException ex)
            {
                Logger.LogWarning("Cannot bind bridge UDP port 19753: " + ex.SocketErrorCode);
                receiver = null;
            }
        }

        private void Update()
        {
            if (receiver == null)
                return;
            // Never hold the render thread waiting for a packet.
            for (int i = 0; i < 32; i++)
            {
                try
                {
                    IPEndPoint peer = new IPEndPoint(IPAddress.Any, 0);
                    byte[] bytes = receiver.Receive(ref peer);
                    if (!IPAddress.IsLoopback(peer.Address) || bytes.Length > 4096)
                        continue;
                    ModFrame next = JsonUtility.FromJson<ModFrame>(Encoding.UTF8.GetString(bytes));
                    if (next == null || next.schema != "mindforge.mod_state.v1" ||
                        next.game_id != gameId || !next.presentation_only ||
                        string.IsNullOrEmpty(next.session_id) || next.ttl_ms <= 0 ||
                        next.ttl_ms > 750 || next.seq <= 0)
                        continue;
                    float now = Time.realtimeSinceStartup;
                    if (!string.IsNullOrEmpty(sessionId) && next.session_id != sessionId &&
                        now - receivedAt < 0.75f)
                        continue;
                    // A restarted bridge starts its own output sequence again.
                    // Only accept a reset after the old packet has expired locally.
                    if (now - receivedAt >= 0.75f)
                        lastSeq = -1;
                    if (next.session_id != sessionId)
                    {
                        sessionId = next.session_id;
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
                    Logger.LogWarning("Mod UDP receiver error: " + ex.SocketErrorCode);
                    break;
                }
                catch (Exception)
                {
                    // Malformed local packets are ignored. No gameplay side effects.
                }
            }
        }

        private void OnGUI()
        {
            if (gameId == null || gameId.Length == 0 || enabledOverlay == null ||
                !enabledOverlay.Value || blueOrb == null || greenOrb == null)
                return;
            bool fresh = latest != null &&
                Time.realtimeSinceStartup - receivedAt < Mathf.Min(0.75f, latest.ttl_ms / 1000f);
            bool linked = fresh && latest.link == "connected";
            if (!fresh && !showOffline.Value)
                return;

            float cx = Screen.width * 0.5f;
            float y = Screen.height * 0.80f;
            const float diameter = 28f;
            Color previous = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, linked && latest.sight_active ? 1f : 0.22f);
            GUI.DrawTexture(new Rect(cx - 36f, y, diameter, diameter), blueOrb);
            GUI.color = new Color(1f, 1f, 1f, linked && latest.guard_active ? 1f : 0.22f);
            GUI.DrawTexture(new Rect(cx + 8f, y, diameter, diameter), greenOrb);
            GUI.color = previous;
            if (captionStyle == null)
            {
                captionStyle = new GUIStyle(GUI.skin.label);
                captionStyle.alignment = TextAnchor.MiddleCenter;
                captionStyle.fontSize = 11;
                captionStyle.normal.textColor = Color.white;
            }
            string status = !fresh ? "BCI OFFLINE" :
                !linked ? "BCI " + latest.link.ToUpperInvariant() :
                latest.source_mode == "simulated_decision" ? "BCI SIMULATION" :
                latest.source_mode == "decision_replay" ? "BCI REPLAY" : "BCI CONNECTED";
            GUI.Label(new Rect(cx - 96f, y + 29f, 192f, 20f), status, captionStyle);
        }

        private static Texture2D MakeOrb(Color32 tint)
        {
            const int side = 48;
            Texture2D texture = new Texture2D(side, side, TextureFormat.ARGB32, false);
            Color32[] pixels = new Color32[side * side];
            for (int y = 0; y < side; y++)
                for (int x = 0; x < side; x++)
                {
                    float dx = (x - 23.5f) / 23.5f;
                    float dy = (y - 23.5f) / 23.5f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float edge = Mathf.Clamp01((1f - dist) * 5f);
                    pixels[y * side + x] = new Color32(tint.r, tint.g, tint.b,
                        (byte)(edge * 255f));
                }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private void OnDestroy()
        {
            if (receiver != null)
                receiver.Close();
            if (blueOrb != null)
                Destroy(blueOrb);
            if (greenOrb != null)
                Destroy(greenOrb);
        }
    }
}
