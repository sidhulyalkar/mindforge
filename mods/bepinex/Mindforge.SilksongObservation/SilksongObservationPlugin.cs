using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mindforge.GameObservation
{
    // A read-only diagnostic plugin. Event names are observations, NOT boss
    // context or game authority. No input or gameplay field is ever changed.
    [BepInPlugin("dev.mindforge.silksong.observation", "Mindforge Silksong Observation", "0.1.0")]
    [BepInProcess("Hollow Knight Silksong")]
    public sealed class SilksongObservationPlugin : BaseUnityPlugin
    {
        private sealed class Pending
        {
            public string Event;
            public string Hook;
            public bool InGame, Focus, Paused;
            public float TimeS;
            public float? X, Y;
        }

        private const int Port = 19756;
        private const int QueueLimit = 128;
        private const int FlushLimit = 16;
        private static SilksongObservationPlugin active;
        private readonly Queue<Pending> pending = new Queue<Pending>();
        private readonly Dictionary<string, string> activeHooks = new Dictionary<string, string>();
        private ConfigEntry<bool> enabledCapture;
        private UdpClient sender;
        private Harmony harmony;
        private Component hero;
        private bool alive;
        private string session;
        private string gameVersion;
        private int seq;
        private int dropped;
        private float nextHeartbeat;
        private float startedAt;

        private void Awake()
        {
            enabledCapture = Config.Bind("Observation", "Enabled", false,
                "Opt-in diagnostic hook capture; never changes game actions or BCI authority.");
            if (!enabledCapture.Value)
            {
                Logger.LogInfo("Mindforge game observation disabled by default.");
                enabled = false;
                return;
            }
            if (!(Application.productName ?? "").ToLowerInvariant().Contains("silksong"))
            {
                Logger.LogWarning("Not a known Silksong product. Observation disabled.");
                enabled = false;
                return;
            }
            session = Guid.NewGuid().ToString("N");
            startedAt = Time.realtimeSinceStartup;
            gameVersion = SafeAscii(Application.version);
            active = this;
            try
            {
                sender = new UdpClient();
                sender.Connect(IPAddress.Loopback, Port);
                harmony = new Harmony("dev.mindforge.silksong.observation");
                // The signatures below were observed in public Silksong mod
                // source. They must STILL be tested against the installed game.
                TryHook("Start", "hero_ready", nameof(OnHeroStart));
                TryHook("Attack", "hero_attack", nameof(OnHeroAttack));
                TryHook("HeroDash", "hero_dash", nameof(OnHeroDash));
                TryHook("OnHeroJumped", "hero_jump", nameof(OnHeroJump));
                TryHook("HeroDamaged", "hero_damaged", nameof(OnHeroDamaged));
                TryHook("Die", "hero_die", nameof(OnHeroDie));
                SceneManager.activeSceneChanged += OnSceneChanged;
                Logger.LogInfo("Read-only observation active. Hook count=" +
                               activeHooks.Count + ", session=" + session);
            }
            catch (Exception ex)
            {
                Logger.LogWarning("Game observation initialization failed: " + ex.GetType().Name);
                StopObservation();
                enabled = false;
            }
        }

        private void TryHook(string targetName, string eventName, string callback)
        {
            Type heroType = AccessTools.TypeByName("HeroController");
            if (heroType == null)
            {
                Logger.LogWarning("HeroController type unavailable; hook " + targetName + " skipped.");
                return;
            }
            MethodInfo[] candidates = heroType
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(m => m.Name == targetName && !m.IsAbstract && !m.ContainsGenericParameters)
                .ToArray();
            // Never guess an overload. A renamed/missing target is a
            // compatibility failure, NOT a reason to patch a nearby method.
            if (candidates.Length != 1)
            {
                Logger.LogWarning("Skipping " + targetName + " (method count=" +
                                  candidates.Length + ").");
                return;
            }
            MethodInfo postfix = typeof(SilksongObservationPlugin).GetMethod(
                callback, BindingFlags.NonPublic | BindingFlags.Static);
            if (postfix == null)
                return;
            try
            {
                harmony.Patch(candidates[0], postfix: new HarmonyMethod(postfix));
                activeHooks[targetName] = eventName;
            }
            catch (Exception ex)
            {
                Logger.LogWarning("Hook " + targetName + " failed: " + ex.GetType().Name);
            }
        }

        // All patch callbacks merely enqueue observables. No game-state mutation.
        private static void OnHeroStart(object __instance)
        {
            if (active == null) return;
            active.hero = __instance as Component;
            active.alive = active.hero != null;
            active.Queue("hero_ready", "HeroController.Start");
        }
        private static void OnHeroAttack() { if (active != null) active.Queue("hero_attack", "HeroController.Attack"); }
        private static void OnHeroDash() { if (active != null) active.Queue("hero_dash", "HeroController.HeroDash"); }
        private static void OnHeroJump() { if (active != null) active.Queue("hero_jump", "HeroController.OnHeroJumped"); }
        private static void OnHeroDamaged() { if (active != null) active.Queue("hero_damaged", "HeroController.HeroDamaged"); }
        private static void OnHeroDie()
        {
            if (active == null) return;
            active.alive = false;
            active.hero = null;
            active.Queue("hero_die", "HeroController.Die");
        }

        private void OnSceneChanged(Scene before, Scene after)
        {
            alive = false;
            hero = null;
            Queue("scene_changed", "SceneManager.activeSceneChanged");
        }

        private void Queue(string eventName, string hook)
        {
            if (sender == null) return;
            var item = new Pending
            {
                Event = eventName, Hook = hook,
                Focus = Application.isFocused,
                Paused = Time.timeScale <= 0.0001f,
                InGame = alive && hero != null,
                TimeS = Mathf.Max(0f, Time.realtimeSinceStartup - startedAt),
            };
            // The measurement is simply a screen projection of an observed
            // player Component. It is NOT certified free of HUD occlusion.
            if (item.InGame && eventName != "hero_die" && eventName != "scene_changed")
            {
                Camera camera = Camera.main;
                if (camera != null)
                {
                    Vector3 point = camera.WorldToViewportPoint(hero.transform.position);
                    if (point.z > 0 && point.x >= 0f && point.x <= 1f &&
                        point.y >= 0f && point.y <= 1f &&
                        !Single.IsNaN(point.x) && !Single.IsNaN(point.y))
                    {
                        item.X = point.x;
                        item.Y = 1f - point.y; // top-left normalized origin
                    }
                }
            }
            if (pending.Count >= QueueLimit)
            {
                pending.Dequeue();
                dropped++;
            }
            pending.Enqueue(item);
        }

        private void Update()
        {
            if (sender == null) return;
            if (Time.realtimeSinceStartup >= nextHeartbeat)
            {
                nextHeartbeat = Time.realtimeSinceStartup + 0.5f;
                Queue("heartbeat", "GameTelemetryPlugin.Update");
            }
            for (int i = 0; i < FlushLimit && pending.Count > 0; i++)
            {
                Pending item = pending.Dequeue();
                string encoded = Serialize(item);
                try
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(encoded);
                    sender.Send(bytes, bytes.Length);
                }
                catch (Exception ex)
                {
                    Logger.LogWarning("Game observation send failure: " + ex.GetType().Name);
                    // Drop telemetry. Never retry or block the game loop.
                    break;
                }
            }
        }

        private string Serialize(Pending p)
        {
            seq++;
            CultureInfo inv = CultureInfo.InvariantCulture;
            string x = p.X.HasValue ? p.X.Value.ToString("R", inv) : "null";
            string y = p.Y.HasValue ? p.Y.Value.ToString("R", inv) : "null";
            // Every textual field is generated in this assembly and sanitized
            // to alphanumeric/dot/underscore/hyphen, not user-controlled JSON.
            return "{"
                + "\"schema\":\"mindforge.game_observation.v1\","
                + "\"game_id\":\"silksong\","
                + "\"source_mode\":\"observed_game_hook_unqualified\","
                + "\"session_id\":\"" + session + "\","
                + "\"seq\":" + seq.ToString(inv) + ","
                + "\"event\":\"" + p.Event + "\","
                + "\"game_version\":\"" + gameVersion + "\","
                + "\"hook_target\":\"" + p.Hook + "\","
                + "\"in_game\":" + (p.InGame ? "true" : "false") + ","
                + "\"app_focused\":" + (p.Focus ? "true" : "false") + ","
                + "\"paused\":" + (p.Paused ? "true" : "false") + ","
                + "\"hero_x\":" + x + ","
                + "\"hero_y\":" + y + ","
                + "\"source_time_s\":" + p.TimeS.ToString("R", inv)
                + "}";
        }

        private static string SafeAscii(string value)
        {
            if (String.IsNullOrEmpty(value)) return "unknown";
            string result = new string(value.Where(c =>
                (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') ||
                (c >= '0' && c <= '9') || c == '.' || c == '_' || c == '-')
                .Take(80).ToArray());
            return result.Length > 0 ? result : "unknown";
        }

        private void StopObservation()
        {
            SceneManager.activeSceneChanged -= OnSceneChanged;
            if (harmony != null)
            {
                try { harmony.UnpatchSelf(); }
                catch (Exception) { }
            }
            if (sender != null)
            {
                sender.Close();
                sender = null;
            }
            pending.Clear();
            hero = null;
            if (active == this) active = null;
        }

        private void OnDestroy()
        {
            if (dropped > 0)
                Logger.LogWarning("Dropped noncritical observation records: " + dropped);
            StopObservation();
        }
    }
}
