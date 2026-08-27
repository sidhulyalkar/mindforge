using UnityEngine;
using Mindforge.Combat;
using Mindforge.SoulWisp;

namespace Mindforge.Presentation
{
    /// <summary>
    /// Gameplay-first, non-authoritative encounter HUD.
    ///
    /// This view intentionally sits conceptually in front of the decoder evidence HUD:
    /// the player first needs to understand health, poise, Flux, strategic aura state,
    /// and the next meaningful payoff. It observes existing authoritative objects and
    /// never invokes combat, neural, Flux, damage, or calibration authority.
    /// </summary>
    public sealed class CombatStateHud : MonoBehaviour
    {
        [SerializeField] private CombatantVitals playerVitals;
        [SerializeField] private CombatantVitals bossVitals;
        [SerializeField] private FluxMeter flux;
        [SerializeField] private AuraBuffController auras;
        [SerializeField] private GravityBloomAbility bloom;
        [SerializeField] private FracturedSignalDirector bossDirector;

        private GUIStyle _small;
        private GUIStyle _label;
        private GUIStyle _phase;
        private GUIStyle _banner;
        private string _bannerText;
        private double _bannerUntil;
        private int _lastObservedPhase;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (FindObjectOfType<CombatStateHud>(true) != null) return;
            if (FindObjectOfType<GuardianCombatInput>(true) == null) return;
            new GameObject("MindforgeCombatStateHud").AddComponent<CombatStateHud>();
        }

        private void OnEnable()
        {
            Resolve();
            Subscribe();
            _lastObservedPhase = bossDirector != null ? bossDirector.Phase : 0;
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Update()
        {
            if (playerVitals == null || bossVitals == null || flux == null || auras == null || bossDirector == null)
            {
                Unsubscribe();
                Resolve();
                Subscribe();
            }

            if (bossDirector != null && bossDirector.Phase != _lastObservedPhase)
            {
                _lastObservedPhase = bossDirector.Phase;
                ShowBanner($"FRACTURED SIGNAL · PHASE {_lastObservedPhase}", 1.35f);
            }
        }

        private void Resolve()
        {
            FluxMeter discoveredFlux = FindObjectOfType<FluxMeter>(true);
            AuraBuffController discoveredAuras = FindObjectOfType<AuraBuffController>(true);
            GravityBloomAbility discoveredBloom = FindObjectOfType<GravityBloomAbility>(true);
            FracturedSignalDirector discoveredBoss = FindObjectOfType<FracturedSignalDirector>(true);

            if (flux == null) flux = discoveredFlux;
            if (auras == null) auras = discoveredAuras;
            if (bloom == null) bloom = discoveredBloom;
            if (bossDirector == null) bossDirector = discoveredBoss;

            CombatantVitals[] vitals = FindObjectsOfType<CombatantVitals>(true);
            foreach (CombatantVitals candidate in vitals)
            {
                if (candidate == null) continue;
                if (candidate.Team == CombatTeam.Guardian && playerVitals == null) playerVitals = candidate;
                if (candidate.Team == CombatTeam.Enemy && candidate.GetComponent<FracturedSignalDirector>() != null)
                    bossVitals = candidate;
            }
        }

        private void Subscribe()
        {
            if (auras != null)
            {
                auras.AuraApplied -= OnAuraApplied;
                auras.ConcordTriggered -= OnConcord;
                auras.AuraApplied += OnAuraApplied;
                auras.ConcordTriggered += OnConcord;
            }
            if (bloom != null)
            {
                bloom.Activated -= OnBloomActivated;
                bloom.Released -= OnBloomReleased;
                bloom.Activated += OnBloomActivated;
                bloom.Released += OnBloomReleased;
            }
            if (bossVitals != null && bossVitals.Poise != null)
            {
                bossVitals.Poise.BrokenEvent -= OnSignalBreak;
                bossVitals.Poise.BrokenEvent += OnSignalBreak;
            }
            if (bossDirector != null)
            {
                bossDirector.PhaseChanged -= OnPhaseChanged;
                bossDirector.PhaseChanged += OnPhaseChanged;
            }
        }

        private void Unsubscribe()
        {
            if (auras != null)
            {
                auras.AuraApplied -= OnAuraApplied;
                auras.ConcordTriggered -= OnConcord;
            }
            if (bloom != null)
            {
                bloom.Activated -= OnBloomActivated;
                bloom.Released -= OnBloomReleased;
            }
            if (bossVitals != null && bossVitals.Poise != null)
                bossVitals.Poise.BrokenEvent -= OnSignalBreak;
            if (bossDirector != null)
                bossDirector.PhaseChanged -= OnPhaseChanged;
        }

        private void OnAuraApplied(string target)
        {
            string label = string.Equals(target, "guard", System.StringComparison.OrdinalIgnoreCase)
                ? "GUARD · RECOVERY ONLINE"
                : "SIGHT · OFFENSE AMPLIFIED";
            ShowBanner(label, 0.9f);
        }

        private void OnConcord() => ShowBanner("CONCORD · TWIN ECLIPSE WINDOW", 1.25f);

        private void OnBloomActivated(bool concord)
            => ShowBanner(concord ? "TWIN ECLIPSE · CAPTURE" : "GRAVITY BLOOM · CAPTURE", 0.75f);

        private void OnBloomReleased(bool concord, int captured)
            => ShowBanner(
                concord ? $"TWIN ECLIPSE · {captured} PROJECTILES RETURNED" : $"BLOOM RELEASE · {captured} RETURNED",
                concord ? 1.45f : 0.9f);

        private void OnSignalBreak() => ShowBanner("SIGNAL BREAK · PUNISH WINDOW", 1.2f);

        private void OnPhaseChanged(int phase)
        {
            _lastObservedPhase = phase;
            ShowBanner($"FRACTURED SIGNAL · PHASE {phase}", 1.35f);
        }

        private void ShowBanner(string text, float seconds)
        {
            _bannerText = text;
            _bannerUntil = Time.realtimeSinceStartupAsDouble + Mathf.Max(0.1f, seconds);
        }

        private void OnGUI()
        {
            EnsureStyles();
            if (playerVitals == null || bossVitals == null || !bossVitals.IsAlive) return;

            DrawBossState();
            DrawPlayerState();
            DrawStrategicState();

            if (!string.IsNullOrEmpty(_bannerText) && Time.realtimeSinceStartupAsDouble < _bannerUntil)
            {
                float width = Mathf.Min(780f, Screen.width - 60f);
                GUI.Box(new Rect((Screen.width - width) * 0.5f, 92f, width, 48f), string.Empty);
                GUI.Label(new Rect((Screen.width - width) * 0.5f, 96f, width, 40f), _bannerText, _banner);
            }
        }

        private void DrawBossState()
        {
            const float width = 620f;
            float x = (Screen.width - width) * 0.5f;
            float y = 18f;
            GUI.Box(new Rect(x, y, width, 64f), string.Empty);
            GUI.Label(new Rect(x + 12f, y + 4f, width - 24f, 20f),
                $"THE FRACTURED SIGNAL    PHASE {(bossDirector != null ? bossDirector.Phase : 0)}", _phase);
            DrawBar(new Rect(x + 12f, y + 28f, width - 24f, 13f), Ratio(bossVitals.Health, bossVitals.MaxHealth), new Color(0.92f, 0.18f, 0.28f));
            float poise = bossVitals.Poise != null ? Ratio(bossVitals.Poise.Current, bossVitals.Poise.Max) : 0f;
            DrawBar(new Rect(x + 12f, y + 46f, width - 24f, 8f), poise, new Color(1f, 0.58f, 0.16f));
        }

        private void DrawPlayerState()
        {
            float x = 18f;
            float y = Screen.height - 142f;
            const float width = 300f;
            GUI.Box(new Rect(x, y, width, 122f), string.Empty);
            GUI.Label(new Rect(x + 12f, y + 7f, width - 24f, 20f), "GUARDIAN", _label);
            GUI.Label(new Rect(x + 12f, y + 30f, 72f, 18f), "VITAL", _small);
            DrawBar(new Rect(x + 82f, y + 32f, width - 96f, 12f), Ratio(playerVitals.Health, playerVitals.MaxHealth), new Color(0.68f, 0.80f, 1f));
            GUI.Label(new Rect(x + 12f, y + 54f, 72f, 18f), "FLUX", _small);
            DrawBar(new Rect(x + 82f, y + 56f, width - 96f, 12f), flux != null ? Ratio(flux.Value, flux.Max) : 0f, new Color(0.82f, 0.38f, 1f));

            string action = "BUILD FLUX · NEAR MISS / COUNTER / BREAK";
            if (flux != null && flux.IsFull)
                action = auras != null && auras.ConcordActive ? "R · TWIN ECLIPSE READY" : "R · GRAVITY BLOOM READY";
            GUI.Label(new Rect(x + 12f, y + 80f, width - 24f, 28f), action, _small);
        }

        private void DrawStrategicState()
        {
            if (auras == null) return;
            float width = 322f;
            float x = Screen.width - width - 18f;
            float y = Screen.height - 142f;
            GUI.Box(new Rect(x, y, width, 122f), string.Empty);
            GUI.Label(new Rect(x + 12f, y + 7f, width - 24f, 20f), "SOUL WISP", _label);
            GUI.Label(new Rect(x + 12f, y + 34f, width - 24f, 20f),
                auras.SightActive ? $"SIGHT  offense  {auras.SightRemaining:F1}s" : "SIGHT  waiting", _small);
            GUI.Label(new Rect(x + 12f, y + 57f, width - 24f, 20f),
                auras.GuardActive ? $"GUARD  recovery  {auras.GuardRemaining:F1}s" : "GUARD  waiting", _small);
            GUI.Label(new Rect(x + 12f, y + 82f, width - 24f, 26f),
                auras.ConcordActive ? $"CONCORD  {auras.ConcordRemaining:F1}s · TWIN ECLIPSE ENABLED" : "CONCORD  overlap Sight + Guard", _small);
        }

        private static void DrawBar(Rect rect, float value, Color fill)
        {
            Color before = GUI.color;
            GUI.color = new Color(0.10f, 0.11f, 0.15f, 0.95f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = fill;
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(value), rect.height), Texture2D.whiteTexture);
            GUI.color = before;
        }

        private static float Ratio(float value, float max) => max > 0.001f ? Mathf.Clamp01(value / max) : 0f;

        private void EnsureStyles()
        {
            if (_small == null)
                _small = new GUIStyle(GUI.skin.label) { fontSize = 13, alignment = TextAnchor.MiddleLeft };
            if (_label == null)
                _label = new GUIStyle(GUI.skin.label) { fontSize = 17, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
            if (_phase == null)
                _phase = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            if (_banner == null)
                _banner = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        }
    }
}
