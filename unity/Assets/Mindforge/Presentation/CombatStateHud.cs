using UnityEngine;
using Mindforge.Calibration;
using Mindforge.Combat;
using Mindforge.Journey;
using Mindforge.SoulWisp;
using Mindforge.World;

namespace Mindforge.Presentation
{
    /// <summary>
    /// Gameplay-first, non-authoritative encounter HUD. Guardian state persists through
    /// exploration; the Fractured Signal bar appears only after the active world flow
    /// has actually crossed the final boss threshold.
    /// </summary>
    public sealed class CombatStateHud : MonoBehaviour
    {
        [SerializeField] private CombatantVitals playerVitals;
        [SerializeField] private CombatantVitals bossVitals;
        [SerializeField] private FluxMeter flux;
        [SerializeField] private AuraBuffController auras;
        [SerializeField] private GravityBloomAbility bloom;
        [SerializeField] private FracturedSignalDirector bossDirector;
        [SerializeField] private FirstJourneyDirector journey;
        [SerializeField] private NullWardEncounterDirector nullWard;
        [SerializeField] private AwakeningCalibrationDirector calibration;
        [SerializeField] private GuardianStamina guardIntegrity;
        [SerializeField] private GuardianEquipmentLoadout loadout;
        [SerializeField] private GuardianSwordShieldController physicalCombat;
        [SerializeField] private GuardianMotor motor;
        [SerializeField] private NeuralFocusResonance resonance;

        private GUIStyle _small;
        private GUIStyle _label;
        private GUIStyle _phase;
        private GUIStyle _banner;
        private GUIStyle _value;
        private string _bannerText;
        private double _bannerUntil;
        private int _lastObservedPhase;

        private static readonly Color Panel = new Color(0.025f, 0.032f, 0.050f, 0.90f);
        private static readonly Color Text = new Color(0.94f, 0.96f, 1f, 1f);
        private static readonly Color Muted = new Color(0.62f, 0.69f, 0.80f, 1f);

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

        private void OnDisable() => Unsubscribe();

        private void Update()
        {
            if (playerVitals == null || flux == null || guardIntegrity == null || loadout == null ||
                physicalCombat == null || motor == null || resonance == null || (journey == null && nullWard == null))
            {
                Unsubscribe();
                Resolve();
                Subscribe();
            }

            if (BossHudVisible() && bossDirector != null && bossDirector.Phase != _lastObservedPhase)
            {
                _lastObservedPhase = bossDirector.Phase;
                ShowBanner($"FRACTURED SIGNAL · PHASE {_lastObservedPhase}", 1.35f);
            }
        }

        private void Resolve()
        {
            if (flux == null) flux = FindObjectOfType<FluxMeter>(true);
            if (auras == null) auras = FindObjectOfType<AuraBuffController>(true);
            if (bloom == null) bloom = FindObjectOfType<GravityBloomAbility>(true);
            if (bossDirector == null) bossDirector = FindObjectOfType<FracturedSignalDirector>(true);
            if (journey == null) journey = FindObjectOfType<FirstJourneyDirector>(true);
            if (nullWard == null) nullWard = FindObjectOfType<NullWardEncounterDirector>(true);
            if (calibration == null) calibration = FindObjectOfType<AwakeningCalibrationDirector>(true);
            if (guardIntegrity == null) guardIntegrity = FindObjectOfType<GuardianStamina>(true);
            if (loadout == null) loadout = FindObjectOfType<GuardianEquipmentLoadout>(true);
            if (physicalCombat == null) physicalCombat = FindObjectOfType<GuardianSwordShieldController>(true);
            if (motor == null) motor = FindObjectOfType<GuardianMotor>(true);
            if (resonance == null) resonance = FindObjectOfType<NeuralFocusResonance>(true);

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
            if (physicalCombat != null)
            {
                physicalCombat.PerfectGuard -= OnPerfectGuard;
                physicalCombat.GuardBroken -= OnGuardBroken;
                physicalCombat.SwordProjectileParried -= OnSwordParry;
                physicalCombat.PerfectGuard += OnPerfectGuard;
                physicalCombat.GuardBroken += OnGuardBroken;
                physicalCombat.SwordProjectileParried += OnSwordParry;
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
            if (physicalCombat != null)
            {
                physicalCombat.PerfectGuard -= OnPerfectGuard;
                physicalCombat.GuardBroken -= OnGuardBroken;
                physicalCombat.SwordProjectileParried -= OnSwordParry;
            }
        }

        private void OnAuraApplied(string target)
        {
            if (ControllerOnly()) return;
            string label = string.Equals(target, "guard", System.StringComparison.OrdinalIgnoreCase)
                ? "GUARD RESONANCE · VERDANT WARD AMPLIFIED"
                : "SIGHT RESONANCE · AETHERBLADE AMPLIFIED";
            ShowBanner(label, 0.9f);
        }

        private void OnConcord()
        {
            if (!ControllerOnly()) ShowBanner("CONCORD · TWIN ECLIPSE WINDOW", 1.25f);
        }

        private void OnBloomActivated(bool concord)
            => ShowBanner(concord ? "TWIN ECLIPSE · CAPTURE" : "GRAVITY BLOOM · CAPTURE", 0.75f);

        private void OnBloomReleased(bool concord, int captured)
            => ShowBanner(concord ? $"TWIN ECLIPSE · {captured} RETURNED" : $"BLOOM · {captured} RETURNED", concord ? 1.45f : 0.9f);

        private void OnSignalBreak()
        {
            if (BossHudVisible()) ShowBanner("SIGNAL BREAK · ATTACK NOW", 1.2f);
        }

        private void OnPerfectGuard() => ShowBanner("PERFECT GUARD · REFLECT", 0.65f);
        private void OnSwordParry(float damage) => ShowBanner($"AETHER PARRY · {damage:F0} REFLECT DAMAGE", 0.58f);
        private void OnGuardBroken() => ShowBanner("GUARD BROKEN · REPOSITION", 0.9f);

        private void OnPhaseChanged(int phase)
        {
            if (!BossHudVisible()) return;
            _lastObservedPhase = phase;
            ShowBanner($"FRACTURED SIGNAL · PHASE {phase}", 1.35f);
        }

        private void ShowBanner(string text, float seconds)
        {
            _bannerText = text;
            _bannerUntil = Time.realtimeSinceStartupAsDouble + Mathf.Max(0.1f, seconds);
        }

        private bool ControllerOnly() => calibration != null && calibration.ControllerOnlyQualificationActive;

        private bool CombatWorldOpen()
        {
            if (calibration == null) return true;
            return calibration.CalibrationReady || calibration.ControllerOnlyQualificationActive;
        }

        private bool BossHudVisible()
        {
            if (bossVitals == null || !bossVitals.IsAlive || bossDirector == null) return false;
            if (nullWard != null) return nullWard.BossActive;
            if (journey != null) return journey.BossActive;
            return bossDirector.gameObject.activeInHierarchy;
        }

        private void OnGUI()
        {
            EnsureStyles();
            if (playerVitals == null || !CombatWorldOpen()) return;

            if (BossHudVisible()) DrawBossState();
            DrawPlayerState();
            DrawStrategicState();

            if (!string.IsNullOrEmpty(_bannerText) && Time.realtimeSinceStartupAsDouble < _bannerUntil)
            {
                float width = Mathf.Min(680f, Screen.width - 60f);
                Rect r = new Rect((Screen.width - width) * 0.5f, BossHudVisible() ? 92f : 100f, width, 44f);
                Fill(r, new Color(0.025f, 0.035f, 0.055f, 0.94f));
                Stroke(r, new Color(0.28f, 0.52f, 0.90f, 0.75f), 1f);
                GUI.Label(r, _bannerText, _banner);
            }
        }

        private void DrawBossState()
        {
            if (!BossHudVisible()) return;
            float width = Mathf.Min(660f, Screen.width - 80f);
            float x = (Screen.width - width) * 0.5f;
            float y = 18f;
            Rect panel = new Rect(x, y, width, 72f);
            Fill(panel, Panel);
            GUI.Label(new Rect(x + 14f, y + 7f, width - 28f, 20f),
                $"THE FRACTURED SIGNAL    ·    PHASE {bossDirector.Phase}", _phase);
            GUI.Label(new Rect(x + 14f, y + 29f, 104f, 18f), $"HP {bossVitals.Health:F0} / {bossVitals.MaxHealth:F0}", _small);
            DrawBar(new Rect(x + 120f, y + 32f, width - 134f, 12f), Ratio(bossVitals.Health, bossVitals.MaxHealth), new Color(0.95f, 0.16f, 0.28f));
            float poise = bossVitals.Poise != null ? Ratio(bossVitals.Poise.Current, bossVitals.Poise.Max) : 0f;
            GUI.Label(new Rect(x + 14f, y + 50f, 104f, 18f), "SIGNAL POISE", _small);
            DrawBar(new Rect(x + 120f, y + 53f, width - 134f, 7f), poise, new Color(1f, 0.58f, 0.16f));
        }

        private void DrawPlayerState()
        {
            float x = 18f;
            float y = Screen.height - 146f;
            const float width = 326f;
            const float height = 126f;
            Rect panel = new Rect(x, y, width, height);
            Fill(panel, Panel);
            string load = loadout != null ? $" · {loadout.LoadClass.ToString().ToUpperInvariant()}" : string.Empty;
            GUI.Label(new Rect(x + 12f, y + 6f, width - 24f, 18f), "GUARDIAN" + load, _label);

            GUI.Label(new Rect(x + 12f, y + 29f, 76f, 16f), $"HP {playerVitals.Health:F0}/{playerVitals.MaxHealth:F0}", _small);
            DrawBar(new Rect(x + 92f, y + 32f, width - 106f, 9f), Ratio(playerVitals.Health, playerVitals.MaxHealth), new Color(0.48f, 0.72f, 1f));
            GUI.Label(new Rect(x + 12f, y + 49f, 76f, 16f), "GUARD", _small);
            DrawBar(new Rect(x + 92f, y + 52f, width - 106f, 9f), guardIntegrity != null ? guardIntegrity.Ratio : 0f, new Color(0.30f, 0.96f, 0.55f));
            GUI.Label(new Rect(x + 12f, y + 69f, 76f, 16f), "FLUX", _small);
            DrawBar(new Rect(x + 92f, y + 72f, width - 106f, 9f), flux != null ? Ratio(flux.Value, flux.Max) : 0f, new Color(0.72f, 0.36f, 1f));

            if (motor != null && !motor.IsGrounded)
            {
                GUI.Label(new Rect(x + 12f, y + 87f, 76f, 16f), motor.IsHovering ? "HOVER" : "AIR", _small);
                DrawBar(new Rect(x + 92f, y + 90f, width - 106f, 6f), motor.HoverRemaining01, new Color(0.36f, 0.82f, 1f));
            }

            string action;
            if (physicalCombat != null && physicalCombat.IsGuarding)
                action = "RMB/E GUARD · release to recover";
            else if (motor != null && motor.IsAirDashing)
                action = "AIR DASH · steer on exit";
            else if (motor != null && motor.IsHovering)
                action = "SPACE HOLD HOVER · SHIFT AIR DASH";
            else if (motor != null && !motor.IsGrounded)
                action = "SPACE ×2 / HOLD HOVER · SHIFT AIR DASH";
            else
                action = "F SWORD · SPACE ×2 · SHIFT DODGE";
            if (flux != null && flux.IsFull) action += " · R BLOOM";
            GUI.Label(new Rect(x + 12f, y + 101f, width - 24f, 20f), action, _small);
        }

        private void DrawStrategicState()
        {
            const float width = 300f;
            float x = Screen.width - width - 18f;
            float y = Screen.height - 146f;
            const float height = 126f;
            Rect panel = new Rect(x, y, width, height);
            Fill(panel, Panel);
            GUI.Label(new Rect(x + 12f, y + 6f, width - 24f, 18f), "ARMAMENT RESONANCE", _label);

            if (ControllerOnly())
            {
                GUI.Label(new Rect(x + 12f, y + 31f, width - 24f, 38f),
                    "CONTROLLER-ONLY MODE\nBCI intentionally disabled", _value);
                GUI.Label(new Rect(x + 12f, y + 82f, width - 24f, 30f), "F parries bullets · RMB/E shields\nX/MMB fires Pulse", _small);
                return;
            }

            if (auras == null) return;
            float sight = resonance != null ? resonance.Sight : 0f;
            float guard = resonance != null ? resonance.Guard : 0f;
            GUI.Label(new Rect(x + 12f, y + 29f, width - 24f, 16f), auras.SightActive ? $"BLUE · BLADE {sight:P0} · {auras.SightRemaining:F1}s" : "BLUE · blade dormant", _small);
            DrawBar(new Rect(x + 12f, y + 48f, width - 24f, 6f), auras.SightActive ? sight : 0f, new Color(0.20f, 0.58f, 1f));
            GUI.Label(new Rect(x + 12f, y + 62f, width - 24f, 16f), auras.GuardActive ? $"GREEN · SHIELD {guard:P0} · {auras.GuardRemaining:F1}s" : "GREEN · shield dormant", _small);
            DrawBar(new Rect(x + 12f, y + 81f, width - 24f, 6f), auras.GuardActive ? guard : 0f, new Color(0.18f, 1f, 0.52f));
            GUI.Label(new Rect(x + 12f, y + 95f, width - 24f, 22f), auras.ConcordActive ? $"CONCORD {auras.ConcordRemaining:F1}s · R TWIN ECLIPSE" : "Focus amplifies gear, never input", _small);
        }

        private static void DrawBar(Rect rect, float value, Color fill)
        {
            Fill(rect, new Color(0.09f, 0.105f, 0.14f, 1f));
            Fill(new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(value), rect.height), fill);
        }

        private static void Fill(Rect rect, Color color)
        {
            Color before = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = before;
        }

        private static void Stroke(Rect rect, Color color, float thickness)
        {
            Fill(new Rect(rect.x, rect.y, rect.width, thickness), color);
            Fill(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
            Fill(new Rect(rect.x, rect.y, thickness, rect.height), color);
            Fill(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
        }

        private static float Ratio(float value, float max) => max > 0.001f ? Mathf.Clamp01(value / max) : 0f;

        private void EnsureStyles()
        {
            if (_small == null)
            {
                _small = new GUIStyle(GUI.skin.label) { fontSize = 11, alignment = TextAnchor.MiddleLeft };
                _small.normal.textColor = Muted;
            }
            if (_label == null)
            {
                _label = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
                _label.normal.textColor = Text;
            }
            if (_phase == null)
            {
                _phase = new GUIStyle(GUI.skin.label) { fontSize = 15, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                _phase.normal.textColor = Text;
            }
            if (_banner == null)
            {
                _banner = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                _banner.normal.textColor = Text;
            }
            if (_value == null)
            {
                _value = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
                _value.normal.textColor = Text;
            }
        }
    }
}
