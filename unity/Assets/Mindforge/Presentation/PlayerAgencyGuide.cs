using System;
using UnityEngine;
using Mindforge.Calibration;
using Mindforge.Combat;
using Mindforge.SoulWisp;

namespace Mindforge.Presentation
{
    /// <summary>
    /// Non-authoritative presentation for the grounded-world build. It teaches the
    /// conventional blade/roll/aerial contract without changing combat or neural authority.
    /// </summary>
    public sealed class PlayerAgencyGuide : MonoBehaviour
    {
        public const string JudgeLensFlag = "-mindforge-judge-lens";

        [SerializeField] private GuardianCombatInput input;
        [SerializeField] private GuardianCombatController combat;
        [SerializeField] private GuardianSwordShieldController physicalCombat;
        [SerializeField] private GuardianMotor motor;
        [SerializeField] private FluxMeter flux;
        [SerializeField] private AuraBuffController auras;
        [SerializeField] private AwakeningCalibrationDirector calibration;
        [SerializeField] private ShowcaseCameraRig cameraRig;
        [SerializeField] private float combatGuideSeconds = 28f;

        private bool _judgeLens;
        private bool _combatObserved;
        private bool _swordUsed;
        private bool _rollUsed;
        private bool _cleaveUsed;
        private bool _counterUsed;
        private double _combatGuideUntil;
        private GUIStyle _crosshairStyle;
        private GUIStyle _centerStyle;
        private GUIStyle _leftStyle;
        private GUIStyle _focusStyle;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (FindObjectOfType<PlayerAgencyGuide>(true) != null) return;
            GuardianCombatInput input = FindObjectOfType<GuardianCombatInput>(true);
            if (input == null) return;

            GameObject root = new GameObject("MindforgePlayerAgencyGuide");
            PlayerAgencyGuide guide = root.AddComponent<PlayerAgencyGuide>();
            guide.input = input;
            guide.Resolve();
        }

        private void OnEnable()
        {
            Resolve();
            Subscribe();
        }

        private void OnDisable() => Unsubscribe();
        private void Start() => _judgeLens = CommandLineContains(JudgeLensFlag);

        private void Resolve()
        {
            if (input == null) input = FindObjectOfType<GuardianCombatInput>(true);
            if (combat == null) combat = FindObjectOfType<GuardianCombatController>(true);
            if (physicalCombat == null) physicalCombat = FindObjectOfType<GuardianSwordShieldController>(true);
            if (motor == null) motor = FindObjectOfType<GuardianMotor>(true);
            if (flux == null) flux = FindObjectOfType<FluxMeter>(true);
            if (auras == null) auras = FindObjectOfType<AuraBuffController>(true);
            if (calibration == null) calibration = FindObjectOfType<AwakeningCalibrationDirector>(true);
            if (cameraRig == null) cameraRig = FindObjectOfType<ShowcaseCameraRig>(true);
        }

        private void Subscribe()
        {
            if (combat != null)
            {
                combat.ActionAccepted -= OnCombatAction;
                combat.ActionAccepted += OnCombatAction;
            }
            if (physicalCombat != null)
            {
                physicalCombat.SwordAttackStarted -= OnSwordAttack;
                physicalCombat.SwordAttackStarted += OnSwordAttack;
            }
            if (motor != null)
            {
                motor.DashStarted -= OnDashStarted;
                motor.DashStarted += OnDashStarted;
            }
        }

        private void Unsubscribe()
        {
            if (combat != null) combat.ActionAccepted -= OnCombatAction;
            if (physicalCombat != null) physicalCombat.SwordAttackStarted -= OnSwordAttack;
            if (motor != null) motor.DashStarted -= OnDashStarted;
        }

        private void Update()
        {
            if (input == null || combat == null || physicalCombat == null || motor == null || calibration == null || cameraRig == null)
            {
                Unsubscribe();
                Resolve();
                Subscribe();
            }

            if (Input.GetKeyDown(KeyCode.F10)) _judgeLens = !_judgeLens;

            if (!_combatObserved && CombatOpen())
            {
                _combatObserved = true;
                _combatGuideUntil = Time.realtimeSinceStartupAsDouble + Mathf.Max(1f, combatGuideSeconds);
            }
        }

        private void OnSwordAttack() => _swordUsed = true;
        private void OnDashStarted() => _rollUsed = true;

        private void OnCombatAction(string action)
        {
            switch (action)
            {
                case "RIFT_CLEAVE": _cleaveUsed = true; break;
                case "COUNTER_PULSE": _counterUsed = true; break;
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
            DrawTargetFocusIndicator();

            float width = Mathf.Min(Screen.width - 32f, 1040f);
            float left = (Screen.width - width) * 0.5f;

            if (!CombatOpen())
            {
                GUI.Box(new Rect(left, Screen.height - 74f, width, 48f),
                    "AWAKENING  |  BLUE / Sight resonates with blade length and energy  |  HANDS keep every movement and combat decision");
            }
            else
            {
                string lesson = CurrentLesson();
                if (!string.IsNullOrEmpty(lesson))
                    GUI.Box(new Rect(left, Screen.height - 74f, width, 48f), lesson);
            }

            string focusState = cameraRig != null && cameraRig.TargetFocusActive ? "T  LOCKED" : "T  LOCK ON";
            GUI.Label(new Rect(18f, Screen.height - 34f, 180f, 22f), focusState, _centerStyle);
            GUI.Label(new Rect(Screen.width - 176f, Screen.height - 34f, 160f, 22f), "F10  JUDGE LENS", _centerStyle);
            if (_judgeLens) DrawJudgeLens();
        }

        private string CurrentLesson()
        {
            bool guideWindow = Time.realtimeSinceStartupAsDouble <= _combatGuideUntil;

            if (auras != null && auras.ConcordActive && flux != null && flux.IsFull)
                return "CONCORD ACTIVE  |  R TWIN ECLIPSE  |  neural state creates an opening; your hand still chooses when to strike";

            if (flux != null && flux.IsFull)
                return "FLUX FULL  |  R GRAVITY BLOOM  |  use the opening after you have read the room";

            if (!guideWindow) return null;
            if (!_swordUsed)
                return "WASD MOVE   ·   MOUSE / ARROWS CAMERA   ·   T LOCK   ·   F / LMB AETHERBLADE   ·   SPACE JUMP ×2";
            if (!_rollUsed)
                return "SHIFT / RMB DODGE ROLL   |   roll through telegraphed ground attacks · use Space twice to take the high route";
            if (!_cleaveUsed || !_counterUsed)
                return "VERTICAL WORLD   |   stairs are safe · double-jump / hover / air-dash create shortcuts   ·   Q CLEAVE   ·   C COUNTER";
            return "READ → SWING → ROLL → REPOSITION   |   TAB shows the full kit   ·   blade swings can parry hostile projectiles";
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

        private void DrawTargetFocusIndicator()
        {
            if (cameraRig == null || !cameraRig.TargetFocusActive || cameraRig.FocusTarget == null) return;
            Camera camera = Camera.main;
            if (camera == null) return;

            Vector3 world = cameraRig.FocusTarget.position + Vector3.up * 1.15f;
            Vector3 screen = camera.WorldToScreenPoint(world);
            if (screen.z <= 0f) return;

            float x = screen.x;
            float y = Screen.height - screen.y;
            const float size = 58f;
            const float arm = 14f;
            const float thickness = 2f;
            Color before = GUI.color;
            GUI.color = new Color(0.12f, 0.82f, 1f, 0.92f);

            GUI.DrawTexture(new Rect(x - size * 0.5f, y - size * 0.5f, arm, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(x - size * 0.5f, y - size * 0.5f, thickness, arm), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(x + size * 0.5f - arm, y - size * 0.5f, arm, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(x + size * 0.5f - thickness, y - size * 0.5f, thickness, arm), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(x - size * 0.5f, y + size * 0.5f - thickness, arm, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(x - size * 0.5f, y + size * 0.5f - arm, thickness, arm), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(x + size * 0.5f - arm, y + size * 0.5f - thickness, arm, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(x + size * 0.5f - thickness, y + size * 0.5f - arm, thickness, arm), Texture2D.whiteTexture);
            GUI.color = before;

            GUI.Label(new Rect(x - 70f, y - size * 0.5f - 28f, 140f, 22f), "TARGET LOCK", _focusStyle);
        }

        private void DrawJudgeLens()
        {
            const float width = 500f;
            const float height = 184f;
            float left = Screen.width - width - 18f;
            float top = 38f;
            GUI.Box(new Rect(left, top, width, height), string.Empty);

            string bci = calibration != null && calibration.ControllerOnlyQualificationActive
                ? "BCI: deliberately DISABLED for controller-only validation"
                : "BCI: accepted Sight can boundedly amplify blade length/energy/damage; Guard channel retained for evaluation";

            GUI.Label(new Rect(left + 16f, top + 10f, width - 32f, 26f), "MINDFORGE AUTHORITY SPLIT", _leftStyle);
            GUI.Label(new Rect(left + 16f, top + 40f, width - 32f, 38f), "HANDS: move · camera · target lock · jump/double-jump/hover · roll/air-dash · Aetherblade · skills", _leftStyle);
            GUI.Label(new Rect(left + 16f, top + 78f, width - 32f, 32f), bci, _leftStyle);
            GUI.Label(new Rect(left + 16f, top + 112f, width - 32f, 40f), "EEG never moves, jumps, hovers, rolls, air-dashes, locks a target, rotates the camera, swings, or parries", _leftStyle);
            GUI.Label(new Rect(left + 16f, top + 156f, width - 32f, 20f), "T is conventional target lock · F10 hides this explainer", _leftStyle);
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
                    wordWrap = true,
                };
            }

            if (_focusStyle == null)
            {
                _focusStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 12,
                    fontStyle = FontStyle.Bold,
                };
                _focusStyle.normal.textColor = new Color(0.25f, 0.90f, 1f, 0.96f);
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
