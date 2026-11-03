using System;
using System.Reflection;
using System.Net;
using System.Net.Sockets;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mindforge.CupheadStageObservation
{
    /// <summary>
    /// Diagnostic only, OFF by default. Reads Cuphead Level.Current and
    /// Map.Current without referencing their mutable game assemblies.
    /// No Harmony patches, input injection, weapons, or boss controls.
    /// </summary>
    [BepInPlugin("dev.mindforge.cuphead.stageobservation",
                 "Mindforge Cuphead Stage Observation", "0.1.0")]
    public sealed class CupheadStageObservationPlugin : BaseUnityPlugin
    {
        [Serializable]
        private sealed class StageFrame
        {
            public string schema = "mindforge.cuphead_stage_observation.v1";
            public string game_id = "cuphead";
            public string session_id;
            public int seq;
            public string source = "observed_stage_reflection_unqualified";
            public string game_version;
            public string stage;
            public string @event;
            public string scene_name;
            public bool app_focused;
            public bool paused;
            public float source_time_s;
        }

        private ConfigEntry<bool> enabledCapture;
        private UdpClient sender;
        private UdpClient overlaySender; // independent read-only stage veto output
        private string sessionId;
        private string build;
        private float startedAt;
        private float nextAt;
        private int sequence;
        private int sendFailures;
        private string lastStage = "";
        private Type levelType;
        private Type mapType;
        private MemberInfo levelCurrent;
        private MemberInfo mapCurrent;
        private bool compatibilityReported;

        private void Awake()
        {
            enabledCapture = Config.Bind("Observation", "Enabled", false,
                "Explicitly enable read-only map/level stage telemetry.");
            if (!enabledCapture.Value)
            {
                enabled = false;
                return;
            }
            if ((Application.productName ?? "").IndexOf(
                    "Cuphead", StringComparison.OrdinalIgnoreCase) < 0)
            {
                Logger.LogWarning("Unsupported game product; Cuphead telemetry disabled.");
                enabled = false;
                return;
            }
            sessionId = Guid.NewGuid().ToString("N");
            build = SafeAscii(Application.version);
            startedAt = Time.realtimeSinceStartup;
            nextAt = startedAt;
            try
            {
                sender = new UdpClient();
                sender.Connect(IPAddress.Loopback, 19757);
                sender.Client.Blocking = false;
                // The UI listens independently; audit and UI must never
                // compete for datagrams on the same UDP port.
                try
                {
                    overlaySender = new UdpClient();
                    overlaySender.Connect(IPAddress.Loopback, 19758);
                    overlaySender.Client.Blocking = false;
                }
                catch (SocketException)
                {
                    if (overlaySender != null) overlaySender.Close();
                    overlaySender = null;  // fail hidden in cosmetic UI
                }
                SceneManager.activeSceneChanged += OnSceneChanged;
                Logger.LogInfo("Read-only Cuphead stage observer started (unqualified), "
                               + build);
            }
            catch (Exception ex)
            {
                Logger.LogWarning("Cuphead observer disabled: " + ex.GetType().Name);
                Stop();
                enabled = false;
            }
        }

        private static string SafeAscii(string value)
        {
            if (String.IsNullOrEmpty(value)) return "unknown";
            char[] chars = value.ToCharArray();
            System.Text.StringBuilder b = new System.Text.StringBuilder();
            foreach (char ch in chars)
            {
                if (b.Length >= 80) break;
                if ((ch >= '0' && ch <= '9') || (ch >= 'a' && ch <= 'z')
                    || (ch >= 'A' && ch <= 'Z') || ch == '_' || ch == '-' || ch == '.')
                    b.Append(ch);
            }
            return b.Length == 0 ? "unknown" : b.ToString();
        }

        private Type FindGameType(string name)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type candidate = assembly.GetType(name, false);
                if (candidate != null)
                    return candidate;
            }
            return null;
        }

        private static MemberInfo CurrentMember(Type type)
        {
            if (type == null) return null;
            const BindingFlags flags =
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
            PropertyInfo p = type.GetProperty("Current", flags);
            if (p != null && p.GetIndexParameters().Length == 0 &&
                p.GetGetMethod(true) != null && p.GetGetMethod(true).IsStatic)
                return p;
            FieldInfo f = type.GetField("Current", flags);
            if (f != null && f.IsStatic)
                return f;
            return null;
        }

        private static object ReadCurrent(MemberInfo member)
        {
            if (member is PropertyInfo)
                return ((PropertyInfo)member).GetValue(null, null);
            if (member is FieldInfo)
                return ((FieldInfo)member).GetValue(null);
            return null;
        }

        private void ResolveStageMembers()
        {
            // Poll type discovery at most 2Hz; cache once located. The public
            // Cuphead4PFixes source uses both Level.Current and Map.Current,
            // but installed game compatibility must still be inspected.
            if (levelCurrent == null)
            {
                levelType = FindGameType("Level");
                levelCurrent = CurrentMember(levelType);
            }
            if (mapCurrent == null)
            {
                mapType = FindGameType("Map");
                mapCurrent = CurrentMember(mapType);
            }
            if (!compatibilityReported)
            {
                compatibilityReported = true;
                Logger.LogInfo("Cuphead Current reflection: Level=" +
                    (levelCurrent != null) + ", Map=" + (mapCurrent != null));
            }
        }

        private string ReadStage()
        {
            ResolveStageMembers();
            if (levelCurrent == null || mapCurrent == null)
                return "unknown";
            try
            {
                bool levelActive = ReadCurrent(levelCurrent) != null;
                bool mapActive = ReadCurrent(mapCurrent) != null;
                if (levelActive == mapActive) return "unknown";
                return levelActive ? "level" : "map";
            }
            catch (Exception ex)
            {
                Logger.LogWarning("Cuphead stage reflection failed: " +
                                  ex.GetType().Name);
                return "unknown";
            }
        }

        private void Update()
        {
            if (sender == null || Time.realtimeSinceStartup < nextAt) return;
            nextAt = Time.realtimeSinceStartup + 0.5f;
            string stage = ReadStage();
            if (stage != lastStage)
            {
                lastStage = stage;
                Logger.LogInfo("Cuphead observed stage: " + stage +
                    " (unqualified, no boss inference)");
            }
            Publish(stage, "sample");
        }

        private void OnSceneChanged(Scene previous, Scene next)
        {
            // A transition cannot certify a playable boss phase.
            lastStage = "unknown";
            Publish("unknown", "scene_changed");
        }

        private void Publish(string stage, string kind)
        {
            if (sender == null) return;
            var frame = new StageFrame();
            frame.session_id = sessionId;
            frame.seq = ++sequence;
            frame.game_version = build;
            frame.stage = stage;
            frame.@event = kind;
            try
            {
                frame.scene_name = SafeAscii(SceneManager.GetActiveScene().name);
                frame.app_focused = Application.isFocused;
                frame.paused = Time.timeScale <= 0.0001f;
                frame.source_time_s = Mathf.Max(0f, Time.realtimeSinceStartup - startedAt);
                byte[] raw = System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(frame));
                if (raw.Length <= 4096)
                {
                    sender.Send(raw, raw.Length);
                    if (overlaySender != null)
                    {
                        try { overlaySender.Send(raw, raw.Length); }
                        catch (SocketException)
                        {
                            // Only cosmetic stage vetoes are lost; they expire
                            // locally and can NEVER authorize an orb.
                        }
                    }
                }
            }
            catch (SocketException)
            {
                if (++sendFailures <= 3)
                    Logger.LogWarning("Cuphead stage event dropped (local UDP)");
            }
            catch (Exception ex)
            {
                if (++sendFailures <= 3)
                    Logger.LogWarning("Cuphead stage observation failed: " +
                        ex.GetType().Name);
            }
        }

        private void Stop()
        {
            SceneManager.activeSceneChanged -= OnSceneChanged;
            if (sender != null)
            {
                sender.Close();
                sender = null;
            }
            if (overlaySender != null)
            {
                overlaySender.Close();
                overlaySender = null;
            }
        }

        private void OnDestroy()
        {
            Stop();
        }
    }
}
