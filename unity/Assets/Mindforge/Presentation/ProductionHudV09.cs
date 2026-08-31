using UnityEngine;
using Mindforge.Calibration;
using Mindforge.Combat;
using Mindforge.Journey;
using Mindforge.SoulWisp;
using Mindforge.World;

namespace Mindforge.Presentation
{
    /// <summary>
    /// Compact production HUD used by the production scene. It is presentation-only and reads
    /// existing authoritative state. V0.16 strengthens information hierarchy after recording
    /// review: player resources, target health, objective, BCI readiness and the neural-window
    /// affordance are visually separated instead of competing inside one tiny panel.
    /// Target-lock reticles and contextual E prompts remain intentionally owned elsewhere.
    /// </summary>
    public sealed class ProductionHudV09 : MonoBehaviour
    {
        [SerializeField] private CombatantVitals playerVitals;
        [SerializeField] private GuardianStamina endurance;
        [SerializeField] private FluxMeter flux;
        [SerializeField] private AwakeningCalibrationDirector calibration;
        [SerializeField] private NullWardEncounterDirector world;
        [SerializeField] private FirstJourneyDirector firstJourney;
        [SerializeField] private GuardianTargetLock targetLock;
        [SerializeField] private SoulWispController wisp;
        [SerializeField] private float tutorialSeconds = 12f;

        private GUIStyle _title;
        private GUIStyle _small;
        private GUIStyle _chip;
        private GUIStyle _objective;
        private GUIStyle _targetTitle;
        private GUIStyle _resourceLabel;
        private double _started;
        private double _nextLegacyHudCheck;

        public static bool Active { get; private set; }

        private void OnEnable()
        {
            Active = true;
            _started = Time.realtimeSinceStartupAsDouble;
            Resolve();
            SuppressLegacyHuds();
        }

        private void OnDisable() => Active = false;

        private void Update()
        {
            if (playerVitals == null || endurance == null || flux == null || calibration == null || world == null || targetLock == null || wisp == null)
                Resolve();
            if (Time.realtimeSinceStartupAsDouble >= _nextLegacyHudCheck)
            {
                _nextLegacyHudCheck = Time.realtimeSinceStartupAsDouble + 1.0;
                SuppressLegacyHuds();
            }
        }

        private void Resolve()
        {
            if (endurance == null) endurance = FindObjectOfType<GuardianStamina>(true);
            if (flux == null) flux = FindObjectOfType<FluxMeter>(true);
            if (calibration == null) calibration = FindObjectOfType<AwakeningCalibrationDirector>(true);
            if (world == null) world = FindObjectOfType<NullWardEncounterDirector>(true);
            if (firstJourney == null) firstJourney = FindObjectOfType<FirstJourneyDirector>(true);
            if (wisp == null) wisp = FindObjectOfType<SoulWispController>(true);
            if (playerVitals == null)
            {
                CombatantVitals[] vitals = FindObjectsOfType<CombatantVitals>(true);
                for (int i = 0; i < vitals.Length; i++)
                {
                    if (vitals[i] != null && vitals[i].Team == CombatTeam.Guardian)
                    {
                        playerVitals = vitals[i];
                        break;
                    }
                }
            }
            if (targetLock == null && playerVitals != null)
                targetLock = playerVitals.GetComponent<GuardianTargetLock>();
        }

        private static void SuppressLegacyHuds()
        {
            CombatStateHud combatHud = FindObjectOfType<CombatStateHud>(true);
            if (combatHud != null && combatHud.enabled) combatHud.enabled = false;
            NullWardHud worldHud = FindObjectOfType<NullWardHud>(true);
            if (worldHud != null && worldHud.enabled) worldHud.enabled = false;
            FirstJourneyHud journeyHud = FindObjectOfType<FirstJourneyHud>(true);
            if (journeyHud != null && journeyHud.enabled) journeyHud.enabled = false;
        }

        private void OnGUI()
        {
            if (playerVitals == null) return;
            EnsureStyles();

            float scale = Mathf.Clamp(Screen.height / 1080f, 0.82f, 1.24f);
            DrawGuardianPanel(scale);
            DrawTargetPanel(scale);
            DrawNeuralChip(scale);
            DrawObjective(scale);
            DrawNeuralAffordance(scale);
            DrawTutorial(scale);
        }

        private void DrawGuardianPanel(float scale)
        {
            float x = 24f * scale;
            float y = 22f * scale;
            float width = 310f * scale;
            float height = 98f * scale;
            Rect panel = new Rect(x, y, width, height);
            Fill(panel, new Color(0.016f, 0.022f, 0.033f, 0.78f));
            Stroke(panel, new Color(0.78f, 0.84f, 0.90f, 0.28f), 1f);

            GUI.Label(new Rect(x + 13f * scale, y + 5f * scale, width - 26f * scale, 18f * scale), "GUARDIAN", _title);
            GUI.Label(new Rect(x + width - 92f * scale, y + 5f * scale, 78f * scale, 18f * scale),
                $"{playerVitals.Health:0}/{playerVitals.MaxHealth:0}", _resourceLabel);

            DrawLabeledBar(new Rect(x + 13f * scale, y + 30f * scale, width - 26f * scale, 10f * scale),
                "HP", Ratio(playerVitals.Health, playerVitals.MaxHealth), new Color(0.91f, 0.31f, 0.39f, 0.96f), scale);
            DrawLabeledBar(new Rect(x + 13f * scale, y + 52f * scale, width - 26f * scale, 8f * scale),
                "END", endurance != null ? endurance.Ratio : 0f, new Color(0.50f, 0.88f, 0.69f, 0.96f), scale);
            DrawLabeledBar(new Rect(x + 13f * scale, y + 71f * scale, width - 26f * scale, 7f * scale),
                "FLUX", flux != null ? Ratio(flux.Value, flux.Max) : 0f, new Color(0.38f, 0.73f, 0.98f, 0.94f), scale);
        }

        private void DrawTargetPanel(float scale)
        {
            CombatantVitals target = ResolveTargetVitals();
            if (target == null || !target.IsAlive) return;

            float width = Mathf.Min(560f * scale, Screen.width * 0.46f);
            float height = 52f * scale;
            float x = (Screen.width - width) * 0.5f;
            float y = 22f * scale;
            Rect panel = new Rect(x, y, width, height);
            Fill(panel, new Color(0.022f, 0.016f, 0.028f, 0.76f));
            Stroke(panel, new Color(0.88f, 0.32f, 0.48f, 0.30f), 1f);

            GUI.Label(new Rect(x + 14f * scale, y + 4f * scale, width - 28f * scale, 18f * scale),
                DisplayTargetName(target), _targetTitle);
            DrawBar(new Rect(x + 14f * scale, y + 30f * scale, width - 28f * scale, 9f * scale),
                Ratio(target.Health, target.MaxHealth), new Color(0.87f, 0.18f, 0.34f, 0.96f));
        }

        private void DrawNeuralChip(float scale)
        {
            string neural;
            Color neuralColor;
            if (calibration != null && calibration.ControllerOnlyQualificationActive)
            {
                neural = "SHOWCASE  ·  BCI OFF";
                neuralColor = new Color(0.84f, 0.86f, 0.90f, 0.82f);
            }
            else if (calibration != null && calibration.CalibrationReady)
            {
                neural = "NEURAL LINK  ·  READY";
                neuralColor = new Color(0.35f, 0.91f, 0.78f, 0.92f);
            }
            else
            {
                neural = "NEURAL LINK  ·  ATTUNE";
                neuralColor = new Color(0.46f, 0.76f, 0.96f, 0.88f);
            }

            Vector2 chipSize = _chip.CalcSize(new GUIContent(neural));
            Rect chip = new Rect(Screen.width - chipSize.x - 40f * scale, 22f * scale, chipSize.x + 18f * scale, 25f * scale);
            Fill(chip, new Color(0.016f, 0.022f, 0.033f, 0.68f));
            Color before = GUI.color;
            GUI.color = neuralColor;
            GUI.Label(new Rect(chip.x + 9f * scale, chip.y + 1f * scale, chip.width - 18f * scale, chip.height - 2f * scale), neural, _chip);
            GUI.color = before;
        }

        private void DrawObjective(float scale)
        {
            string objective = CurrentObjective();
            if (string.IsNullOrWhiteSpace(objective)) return;
            float objectiveWidth = Mathf.Min(560f * scale, Screen.width * 0.44f);
            Rect objectiveRect = new Rect(24f * scale, Screen.height - 48f * scale, objectiveWidth, 29f * scale);
            Fill(objectiveRect, new Color(0.016f, 0.022f, 0.033f, 0.70f));
            Stroke(objectiveRect, new Color(0.70f, 0.78f, 0.84f, 0.16f), 1f);
            GUI.Label(new Rect(objectiveRect.x + 11f * scale, objectiveRect.y, objectiveRect.width - 22f * scale, objectiveRect.height), objective, _objective);
        }

        private void DrawNeuralAffordance(float scale)
        {
            if (calibration == null || calibration.ControllerOnlyQualificationActive || !calibration.CalibrationReady) return;

            bool active = wisp != null && wisp.ResonanceWindowActive;
            string label = active
                ? "NEURAL WINDOW  ·  KEEP GAZE ON BLUE / GREEN"
                : "V HOLD  ·  CHANNEL WISP";
            Vector2 size = _chip.CalcSize(new GUIContent(label));
            float width = size.x + 24f * scale;
            Rect r = new Rect(Screen.width - width - 24f * scale, Screen.height - 48f * scale, width, 29f * scale);
            Fill(r, new Color(0.016f, 0.022f, 0.033f, active ? 0.80f : 0.66f));
            Stroke(r, active ? new Color(0.36f, 0.79f, 0.98f, 0.36f) : new Color(0.50f, 0.72f, 0.90f, 0.18f), 1f);
            GUI.Label(r, label, _chip);
        }

        private void DrawTutorial(float scale)
        {
            if (Time.realtimeSinceStartupAsDouble - _started >= tutorialSeconds) return;
            const string lesson = "WASD MOVE   ·   SPACE JUMP/HOVER   ·   SHIFT EVADE   ·   F BLADE   ·   T TARGET   ·   E INTERACT";
            Vector2 size = _small.CalcSize(new GUIContent(lesson));
            Rect r = new Rect((Screen.width - size.x - 30f * scale) * 0.5f, Screen.height - 84f * scale, size.x + 30f * scale, 25f * scale);
            Fill(r, new Color(0.016f, 0.022f, 0.033f, 0.64f));
            GUI.Label(r, lesson, _small);
        }

        private CombatantVitals ResolveTargetVitals()
        {
            Transform target = targetLock != null ? targetLock.Target : null;
            if (target == null && wisp != null) target = wisp.CurrentTarget;
            return target != null ? target.GetComponentInParent<CombatantVitals>() : null;
        }

        private static string DisplayTargetName(CombatantVitals target)
        {
            if (target == null) return string.Empty;
            string name = target.gameObject.name.Replace('_', ' ').Trim();
            if (string.IsNullOrWhiteSpace(name)) name = "HOSTILE SIGNAL";
            return name.ToUpperInvariant();
        }

        private string CurrentObjective()
        {
            if (world != null && !string.IsNullOrWhiteSpace(world.CurrentObjective))
                return world.CurrentObjective;
            if (firstJourney != null && !string.IsNullOrWhiteSpace(firstJourney.CurrentObjective))
                return firstJourney.CurrentObjective;
            return string.Empty;
        }

        private void EnsureStyles()
        {
            if (_title != null) return;
            _title = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.95f, 0.97f, 0.99f, 0.98f) },
                alignment = TextAnchor.MiddleLeft,
            };
            _small = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                normal = { textColor = new Color(0.79f, 0.84f, 0.89f, 0.94f) },
                alignment = TextAnchor.MiddleCenter,
            };
            _chip = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.88f, 0.93f, 0.97f, 0.95f) },
            };
            _objective = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.90f, 0.93f, 0.96f, 0.96f) },
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip,
            };
            _targetTitle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.97f, 0.86f, 0.89f, 0.97f) },
                alignment = TextAnchor.MiddleCenter,
            };
            _resourceLabel = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.82f, 0.87f, 0.91f, 0.92f) },
                alignment = TextAnchor.MiddleRight,
            };
        }

        private static float Ratio(float value, float max) => max > 0f ? Mathf.Clamp01(value / max) : 0f;

        private void DrawLabeledBar(Rect rect, string label, float ratio, Color color, float scale)
        {
            GUI.Label(new Rect(rect.x, rect.y - 1f * scale, 34f * scale, rect.height + 2f * scale), label, _resourceLabel);
            Rect bar = rect;
            bar.x += 38f * scale;
            bar.width -= 38f * scale;
            DrawBar(bar, ratio, color);
        }

        private static void DrawBar(Rect rect, float ratio, Color color)
        {
            Fill(rect, new Color(0.07f, 0.085f, 0.11f, 0.90f));
            Rect fill = rect;
            fill.width *= Mathf.Clamp01(ratio);
            Fill(fill, color);
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
    }
}
