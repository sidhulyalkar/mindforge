using UnityEngine;
using Mindforge.Calibration;
using Mindforge.Combat;
using Mindforge.Journey;
using Mindforge.World;

namespace Mindforge.Presentation
{
    /// <summary>
    /// Compact production HUD used by the V0.9 scene. It is presentation-only and reads
    /// existing authoritative state. The previous diagnostic HUDs remain in the project for
    /// qualification, but their large panels yield to this layer during ordinary exploration.
    /// Target-lock reticles and contextual E prompts are intentionally owned elsewhere.
    /// </summary>
    public sealed class ProductionHudV09 : MonoBehaviour
    {
        [SerializeField] private CombatantVitals playerVitals;
        [SerializeField] private GuardianStamina endurance;
        [SerializeField] private FluxMeter flux;
        [SerializeField] private AwakeningCalibrationDirector calibration;
        [SerializeField] private NullWardEncounterDirector world;
        [SerializeField] private FirstJourneyDirector firstJourney;
        [SerializeField] private float tutorialSeconds = 12f;

        private GUIStyle _title;
        private GUIStyle _small;
        private GUIStyle _chip;
        private GUIStyle _objective;
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
            if (playerVitals == null || endurance == null || flux == null || calibration == null || world == null)
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

            float scale = Mathf.Clamp(Screen.height / 1080f, 0.78f, 1.18f);
            float x = 24f * scale;
            float y = 22f * scale;
            float width = 258f * scale;
            float height = 72f * scale;
            Rect panel = new Rect(x, y, width, height);
            Fill(panel, new Color(0.018f, 0.025f, 0.036f, 0.70f));
            Stroke(panel, new Color(0.75f, 0.84f, 0.91f, 0.22f), 1f);

            GUI.Label(new Rect(x + 11f * scale, y + 5f * scale, width - 22f * scale, 17f * scale), "GUARDIAN", _title);
            DrawBar(new Rect(x + 11f * scale, y + 28f * scale, width - 22f * scale, 7f * scale),
                Ratio(playerVitals.Health, playerVitals.MaxHealth), new Color(0.88f, 0.34f, 0.42f, 0.95f));
            DrawBar(new Rect(x + 11f * scale, y + 43f * scale, width - 22f * scale, 5f * scale),
                endurance != null ? endurance.Ratio : 0f, new Color(0.58f, 0.88f, 0.72f, 0.95f));
            DrawBar(new Rect(x + 11f * scale, y + 56f * scale, width - 22f * scale, 4f * scale),
                flux != null ? Ratio(flux.Value, flux.Max) : 0f, new Color(0.48f, 0.77f, 0.96f, 0.88f));
            GUI.Label(new Rect(x + 11f * scale, y + 58f * scale, width - 22f * scale, 12f * scale),
                $"{playerVitals.Health:0}/{playerVitals.MaxHealth:0}   ·   ENDURANCE   ·   FLUX", _small);

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
            Rect chip = new Rect(Screen.width - chipSize.x - 38f * scale, 22f * scale, chipSize.x + 16f * scale, 23f * scale);
            Fill(chip, new Color(0.018f, 0.025f, 0.036f, 0.58f));
            Color before = GUI.color;
            GUI.color = neuralColor;
            GUI.Label(new Rect(chip.x + 8f * scale, chip.y + 1f * scale, chip.width - 16f * scale, chip.height - 2f * scale), neural, _chip);
            GUI.color = before;

            string objective = CurrentObjective();
            if (!string.IsNullOrWhiteSpace(objective))
            {
                float objectiveWidth = Mathf.Min(520f * scale, Screen.width * 0.42f);
                Rect objectiveRect = new Rect(24f * scale, Screen.height - 41f * scale, objectiveWidth, 24f * scale);
                Fill(objectiveRect, new Color(0.018f, 0.025f, 0.036f, 0.55f));
                GUI.Label(new Rect(objectiveRect.x + 10f * scale, objectiveRect.y, objectiveRect.width - 20f * scale, objectiveRect.height), objective, _objective);
            }

            if (Time.realtimeSinceStartupAsDouble - _started < tutorialSeconds)
            {
                const string lesson = "WASD   ·   SPACE JUMP/HOVER   ·   SHIFT EVADE   ·   F BLADE   ·   E INTERACT";
                Vector2 size = _small.CalcSize(new GUIContent(lesson));
                Rect r = new Rect((Screen.width - size.x - 26f * scale) * 0.5f, Screen.height - 39f * scale, size.x + 26f * scale, 23f * scale);
                Fill(r, new Color(0.018f, 0.025f, 0.036f, 0.58f));
                GUI.Label(r, lesson, _small);
            }
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
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.94f, 0.96f, 0.98f, 0.96f) },
                alignment = TextAnchor.MiddleLeft,
            };
            _small = new GUIStyle(GUI.skin.label)
            {
                fontSize = 9,
                normal = { textColor = new Color(0.78f, 0.83f, 0.88f, 0.90f) },
                alignment = TextAnchor.MiddleCenter,
            };
            _chip = new GUIStyle(GUI.skin.label)
            {
                fontSize = 9,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
            };
            _objective = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.88f, 0.91f, 0.94f, 0.92f) },
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip,
            };
        }

        private static float Ratio(float value, float max) => max > 0f ? Mathf.Clamp01(value / max) : 0f;

        private static void DrawBar(Rect rect, float ratio, Color color)
        {
            Fill(rect, new Color(0.08f, 0.10f, 0.13f, 0.82f));
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
