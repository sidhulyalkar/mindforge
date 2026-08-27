using System;
using UnityEngine;
using Mindforge.Calibration;
using Mindforge.Combat;

namespace Mindforge.Presentation
{
    /// <summary>
    /// Non-authoritative presentation for the competition build.
    ///
    /// It makes the control contract legible without changing combat or neural
    /// authority: hands own movement/aim/actions, while BCI owns only the strategic
    /// Sight/Guard aura layer. F10 toggles a judge-facing explainer. The reticle is
    /// driven from the already-resolved conventional aim vector and never feeds back
    /// into gameplay.
    /// </summary>
    public sealed class PlayerAgencyGuide : MonoBehaviour
    {
        public const string JudgeLensFlag = "-mindforge-judge-lens";

        [SerializeField] private GuardianCombatInput input;
        [SerializeField] private AwakeningCalibrationDirector calibration;
        [SerializeField] private float combatGuideSeconds = 14f;

        private bool _judgeLens;
        private bool _combatObserved;
        private double _combatGuideUntil;
        private GUIStyle _crosshairStyle;
        private GUIStyle _centerStyle;
        private GUIStyle _leftStyle;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (FindObjectOfType<PlayerAgencyGuide>(true) != null) return;
            GuardianCombatInput input = FindObjectOfType<GuardianCombatInput>(true);
            if (input == null) return;

            GameObject root = new GameObject("MindforgePlayerAgencyGuide");
            PlayerAgencyGuide guide = root.AddComponent<PlayerAgencyGuide>();
            guide.input = input;
            guide.calibration = FindObjectOfType<AwakeningCalibrationDirector>(true);
        }

        private void Start()
        {
            _judgeLens = CommandLineContains(JudgeLensFlag);
        }

        private void Update()
        {
            if (input == null) input = FindObjectOfType<GuardianCombatInput>(true);
            if (calibration == null) calibration = FindObjectOfType<AwakeningCalibrationDirector>(true);

            if (Input.GetKeyDown(KeyCode.F10))
                _judgeLens = !_judgeLens;

            if (!_combatObserved && CombatOpen())
            {
                _combatObserved = true;
                _combatGuideUntil = Time.realtimeSinceStartupAsDouble + Mathf.Max(1f, combatGuideSeconds);
            }
        }

        private bool CombatOpen()
        {
            if (calibration == null) return true;
            return calibration.CalibrationReady || calibration.ControllerOnlyQualificationActive;
        }

        private void OnGUI()
        {
            EnsureStyles();
            DrawAimReticle();

            float width = Mathf.Min(Screen.width - 32f, 960f);
            float left = (Screen.width - width) * 0.5f;

            if (!CombatOpen())
            {
                GUI.Box(
                    new Rect(left, Screen.height - 74f, width, 48f),
                    "AWAKENING  |  BRAIN: attend BLUE for Sight offense, GREEN for Guard recovery  |  HANDS keep precision");
            }
            else if (Time.realtimeSinceStartupAsDouble <= _combatGuideUntil)
            {
                GUI.Box(
                    new Rect(left, Screen.height - 74f, width, 48f),
                    "WASD move  |  MOUSE / ARROWS aim  |  SPACE Pulse  |  F Cleave  |  C Counter  |  SHIFT Dash  |  R Bloom");
            }

            GUI.Label(new Rect(Screen.width - 176f, Screen.height - 34f, 160f, 22f), "F10  JUDGE LENS", _centerStyle);

            if (_judgeLens)
                DrawJudgeLens();
        }

        private void DrawAimReticle()
        {
            if (input == null || !input.PrecisionAimActive) return;
            Camera camera = Camera.main;
            if (camera == null) return;

            Vector3 screen = camera.WorldToScreenPoint(input.CurrentAimPoint);
            if (screen.z <= 0f) return;
            float x = screen.x;
            float y = Screen.height - screen.y;
            GUI.Label(new Rect(x - 18f, y - 19f, 36f, 36f), "+", _crosshairStyle);
        }

        private void DrawJudgeLens()
        {
            const float width = 430f;
            const float height = 154f;
            float left = Screen.width - width - 18f;
            float top = 18f;
            GUI.Box(new Rect(left, top, width, height), string.Empty);

            string bci = calibration != null && calibration.ControllerOnlyQualificationActive
                ? "BCI: deliberately DISABLED for P2"
                : "BCI: Sight offense / Guard recovery only";

            GUI.Label(new Rect(left + 16f, top + 10f, width - 32f, 26f), "MINDFORGE AUTHORITY SPLIT", _leftStyle);
            GUI.Label(new Rect(left + 16f, top + 40f, width - 32f, 24f), "HANDS: move, aim, fire, cleave, counter, dash", _leftStyle);
            GUI.Label(new Rect(left + 16f, top + 66f, width - 32f, 24f), bci, _leftStyle);
            GUI.Label(new Rect(left + 16f, top + 92f, width - 32f, 24f), "EEG never moves, aims, fires, dodges, or parries", _leftStyle);
            GUI.Label(new Rect(left + 16f, top + 120f, width - 32f, 22f), "F10 hides this explainer", _leftStyle);
        }

        private void EnsureStyles()
        {
            if (_crosshairStyle == null)
            {
                _crosshairStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 30,
                    fontStyle = FontStyle.Bold,
                };
            }

            if (_centerStyle == null)
            {
                _centerStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 13,
                };
            }

            if (_leftStyle == null)
            {
                _leftStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleLeft,
                    fontSize = 15,
                };
            }
        }

        private static bool CommandLineContains(string flag)
        {
            foreach (string arg in Environment.GetCommandLineArgs())
                if (string.Equals(arg, flag, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }
    }
}
