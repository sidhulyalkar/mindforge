using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using GlobalEnums;
using Modding;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mindforge.HollowKnightObservation
{
    /// <summary>
    /// OFF by default. Uses the game's Modding API instead of Silksong Harmony.
    /// Sends only observed hero action/scene events. Never mutates gameplay.
    /// </summary>
    public sealed class HollowKnightObservation : Mod, ITogglableMod
    {
        private const int Port = 19756;
        private const int MaxBuffered = 96;
        private const string OptInName = "Mindforge.Observer.optin";
        private const string Consent = "ENABLE_READ_ONLY_GAME_TELEMETRY_V1";
        private readonly Queue<string> pending = new Queue<string>();
        private UdpClient sender;
        private GameObject overlayHost;
        private HKActivityOverlay overlay;
        private bool subscribed;
        private bool heroReady;
        private string session = "";
        private string build = "unknown";
        private int seq;
        private int dropped;
        private float startAt;
        private float nextHeartbeat;

        public override string GetVersion() => "0.1.0-observation";

        public override void Initialize()
        {
            if (subscribed) return;

            string folder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string optin = Path.Combine(folder ?? ".", OptInName);
            if (!File.Exists(optin) ||
                File.ReadAllText(optin).Trim() != Consent)
            {
                Log("Read-only observer is OFF. A separate exact opt-in token is required.");
                return;
            }

            try
            {
                sender = new UdpClient();
                sender.Connect(IPAddress.Loopback, Port);
                sender.Client.Blocking = false;
                build = SafeAscii(Application.version);
                session = Guid.NewGuid().ToString("N");
                seq = 0;
                dropped = 0;
                startAt = Time.realtimeSinceStartup;
                nextHeartbeat = startAt;
                heroReady = false;

                // Set before registration so a partial failure still gets
                // a full unsubscribe and clean unload.
                subscribed = true;
                // Confirmed from hk-modding/api ModHooks implementation and
                // SimpleHooks sample. Both events are observational.
                ModHooks.AttackHook += OnAttack;
                ModHooks.HeroUpdateHook += OnHeroUpdate;
                SceneManager.activeSceneChanged += OnSceneChanged;

                // An entirely cosmetic companion; this is never a coded EEG
                // stimulus and is off until the opt-in marker is present.
                overlayHost = new GameObject("MindforgeHKReadOnlyDiagnostic");
                UnityEngine.Object.DontDestroyOnLoad(overlayHost);
                overlay = overlayHost.AddComponent<HKActivityOverlay>();
                overlay.GameplayAllowed = false;
                Log("Mindforge read-only observation enabled (unqualified), build " + build);
            }
            catch (Exception e)
            {
                Log("Observer initialization failed: " + e.GetType().Name);
                Unload();
            }
        }

        private void OnAttack(AttackDirection direction)
        {
            Enqueue("hero_attack", "ModHooks.AttackHook", heroReady);
        }

        private void OnHeroUpdate()
        {
            if (!subscribed || sender == null) return;
            bool currentlyReady = HeroController.instance != null;
            float now = Time.realtimeSinceStartup;
            if (currentlyReady && !heroReady)
            {
                heroReady = true;
                Enqueue("hero_ready", "ModHooks.HeroUpdateHook", true);
            }
            if (!currentlyReady)
            {
                heroReady = false;
            }
            if (overlay != null) overlay.GameplayAllowed = heroReady;
            if (now >= nextHeartbeat)
            {
                nextHeartbeat = now + 0.5f;
                Enqueue("heartbeat", "ModHooks.HeroUpdateHook", currentlyReady);
            }
            Flush();
        }

        private void OnSceneChanged(Scene prior, Scene current)
        {
            heroReady = false;
            if (overlay != null) overlay.GameplayAllowed = false;
            Enqueue("scene_changed", "SceneManager.activeSceneChanged", false);
            Flush();
        }

        private void Enqueue(string eventName, string hook, bool inGame)
        {
            if (!subscribed || sender == null) return;
            if (pending.Count >= MaxBuffered)
            {
                pending.Dequeue();
                dropped++;
            }
            seq++;
            var inv = CultureInfo.InvariantCulture;
            string payload = "{"
                + "\"schema\":\"mindforge.game_observation.v1\","
                + "\"game_id\":\"hollow_knight\","
                + "\"source_mode\":\"observed_game_hook_unqualified\","
                + "\"session_id\":\"" + session + "\","
                + "\"seq\":" + seq.ToString(inv) + ","
                + "\"event\":\"" + eventName + "\","
                + "\"game_version\":\"" + build + "\","
                + "\"hook_target\":\"" + hook + "\","
                + "\"in_game\":" + (inGame ? "true" : "false") + ","
                + "\"app_focused\":" + (Application.isFocused ? "true" : "false") + ","
                + "\"paused\":" + (Time.timeScale <= 0.0001f ? "true" : "false") + ","
                + "\"hero_x\":null,\"hero_y\":null,"
                + "\"source_time_s\":" + Mathf.Max(0f, Time.realtimeSinceStartup - startAt).ToString("R", inv)
                + "}";
            pending.Enqueue(payload);
        }

        private void Flush()
        {
            if (sender == null) return;
            // Cap work in the game loop. No retries, no gameplay dependency.
            for (int n = 0; n < 16 && pending.Count > 0; n++)
            {
                string payload = pending.Dequeue();
                try
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(payload);
                    sender.Send(bytes, bytes.Length);
                }
                catch (SocketException)
                {
                    // Drop this telemetry sample and continue game execution.
                    dropped++;
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
            }
        }

        private static string SafeAscii(string text)
        {
            string value = new string((text ?? "unknown").Where(ch =>
                (ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z') ||
                (ch >= '0' && ch <= '9') || ch == '.' || ch == '_' || ch == '-')
                .Take(80).ToArray());
            return value.Length == 0 ? "unknown" : value;
        }

        public void Unload()
        {
            if (subscribed)
            {
                ModHooks.AttackHook -= OnAttack;
                ModHooks.HeroUpdateHook -= OnHeroUpdate;
                SceneManager.activeSceneChanged -= OnSceneChanged;
                subscribed = false;
            }
            pending.Clear();
            heroReady = false;
            if (overlay != null) overlay.GameplayAllowed = false;
            overlay = null;
            if (overlayHost != null)
            {
                UnityEngine.Object.Destroy(overlayHost);
                overlayHost = null;
            }
            if (sender != null)
            {
                sender.Close();
                sender = null;
            }
            if (dropped > 0)
                Log("Dropped noncritical observation records: " + dropped);
        }
    }
}
