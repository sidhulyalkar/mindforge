using System;
using UnityEngine;
using Mindforge.Calibration;
using Mindforge.Combat;
using Mindforge.SoulWisp;

namespace Mindforge.Presentation
{
    /// <summary>
    /// Non-authoritative presentation for the competition build.
    ///
    /// It makes the control contract legible without changing combat or neural
    /// authority: hands own movement/aim/actions, while BCI owns only bounded
    /// amplification of accepted Sight/Guard states. F10 toggles a judge-facing
    /// explainer. The reticle is driven from conventional aim and never feeds back.
    /// </summary>
    public sealed class PlayerAgencyGuide : MonoBehaviour
    {
        public const string JudgeLensFlag = "-mindforge-judge-lens";

        [SerializeField] private GuardianCombatInput input;
        [SerializeField] private GuardianCombatController combat;
        [SerializeField] private GuardianSwordShieldController physicalCombat;
        [SerializeField] private FluxMeter flux;
        [SerializeField] private AuraBuffController auras;
        [SerializeField] private AwakeningCalibrationDirector calibration;
        [SerializeField] private float combatGuideSeconds = 34f;

        private bool _judgeLens;
        private bool _combatObserved;
        private bool _swordUsed;
        private bool _shieldRaised;
        private bool _shieldBlocked;
        private bool _pulseUsed;
        private bool _cleaveUsed;
        private bool _counterUsed;
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
            guide.Resolve();
        }

        private void OnEnable()
        {
            Resolve();
            Subscribe();
        }

        private void OnDisable() => Unsubscribe();

        private void Start()
        {
            _judgeLens = CommandLineContains(JudgeLensFlag);
        }

        private void Resolve()
        {
            if (input == null) input = FindObjectOfType<GuardianCombatInput>(true);
            if (combat == null) combat = FindObjectOfType<GuardianCombatController>(true);
            if (physicalCombat == null) physicalCombat = FindObjectOfType<GuardianSwordShieldController>(true);
            if (flux == null) flux = FindObjectOfType<FluxMeter>(true);
            if (auras == null) auras = FindObjectOfType<AuraBuffController>(true);
            if (calibration == null) calibration = FindObjectOfType<AwakeningCalibrationDirector>(true);
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
                physicalCombat.GuardChanged -= OnGuardChanged;
                physicalCombat.ShieldBlocked -= OnShieldBlocked;
                physicalCombat.SwordAttackStarted += OnSwordAttack;
                physicalCombat.GuardChanged += OnGuardChanged;
                physicalCombat.ShieldBlocked += OnShieldBlocked;
            }
        }

        private void Unsubscribe()
        {
            if (combat != null) combat.ActionAccepted -= OnCombatAction;
            if (physicalCombat != null)
            {
                physicalCombat.SwordAttackStarted -= OnSwordAttack;
                physicalCombat.GuardChanged -= OnGuardChanged;
                physicalCombat.ShieldBlocked -= OnShieldBlocked;
            }
        }

        private void Update()
        {
            if (input == null || combat == null || physicalCombat == null || calibration == null)
            {
                Unsubscribe();
                Resolve();
                Subscribe();
            }

            if (Input.GetKeyDown(KeyCode.F10))
                _judgeLens = !_judgeLens;

            if (!_combatObserved && CombatOpen())
            {
                _combatObserved = true;
                _combatGuideUntil = Time.realtimeSinceStartupAsDouble + Mathf.Max(1f, combatGuideSeconds);
            }
        }

        private void OnSwordAttack() => _swordUsed = true;
        private void OnGuardChanged(bool raised) { if (raised) _shieldRaised = true; }
        private void OnShieldBlocked(float incoming, float chip) => _shieldBlocked = true;

        private void OnCombatAction(string action)
        {
            switch (action)
            {
                case "PULSE_SHOT": _pulseUsed = true; break;
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

            float width = Mathf.Min(Screen.width - 32f, 1040f);
            float left = (Screen.width - width) * 0.5f;

            if (!CombatOpen())
            {
                GUI.Box(
                    new Rect(left, Screen.height - 74f, width, 48f),
                    "AWAKENING  |  BRAIN: BLUE resonates with blade, GREEN resonates with shield  |  HANDS keep every combat decision");
            }
            else
            {
                string lesson = CurrentLesson();
                if (!string.IsNullOrEmpty(lesson))
                    GUI.Box(new Rect(left, Screen.height - 74f, width, 48f), lesson);
            }

            GUI.Label(new Rect(Screen.width - 176f, Screen.height - 34f, 160f, 22f), "F10  JUDGE LENS", _centerStyle);

            if (_judgeLens)
                DrawJudgeLens();
        }

        private string CurrentLesson()
        {
            bool guideWindow = Time.realtimeSinceStartupAsDouble <= _combatGuideUntil;

            if (auras != null && auras.ConcordActive && flux != null && flux.IsFull)
                return "CONCORD ACTIVE  |  R TWIN ECLIPSE  |  your brain created the opening, your hand chooses when to cash it in";

            if (flux != null && flux.IsFull)
                return "FLUX FULL  |  R GRAVITY BLOOM  |  capture hostile projectiles, then return them";

            if (!guideWindow) return null;
            if (!_swordUsed)
                return "WASD move  |  MOUSE / ARROWS aim  |  LMB SWORD  |  commit to a sweep, then reposition before the next attack";
            if (!_shieldRaised)
                return "RMB HOLD SHIELD  |  GREEN Guard resonance can enlarge and stabilize it, but your hand decides when it is raised";
            if (!_shieldBlocked)
                return "READ THE TELEGRAPH  |  hold RMB to catch a projectile, or tap the guard just before impact for a PERFECT GUARD";
            if (!_pulseUsed)
                return "SHIFT DODGE ROLL  |  stamina powers sword, shield and roll  |  SPACE remains your ranged Pulse option";
            if (!_cleaveUsed || !_counterUsed)
                return "F Cleave up close  |  C Counter remains an arcane projectile counter  |  TAB opens your current equipment build";
            return "BUILD FLUX  |  near miss, perfect guard/counter, Echo pressure and Signal Break create your high-impact R windows";
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
            const float width = 480f;
            const float height = 180f;
            float left = Screen.width - width - 18f;
            float top = 18f;
            GUI.Box(new Rect(left, top, width, height), string.Empty);

            string bci = calibration != null && calibration.ControllerOnlyQualificationActive
                ? "BCI: deliberately DISABLED for P2"
                : "BCI: bounded blade/shield resonance after accepted Sight/Guard";

            GUI.Label(new Rect(left + 16f, top + 10f, width - 32f, 26f), "MINDFORGE AUTHORITY SPLIT", _leftStyle);
            GUI.Label(new Rect(left + 16f, top + 40f, width - 32f, 24f), "HANDS: move, aim, sword, shield, roll, skills", _leftStyle);
            GUI.Label(new Rect(left + 16f, top + 66f, width - 32f, 42f), bci, _leftStyle);
            GUI.Label(new Rect(left + 16f, top + 110f, width - 32f, 42f), "EEG never swings, raises guard, aims, rolls, fires, or parries", _leftStyle);
            GUI.Label(new Rect(left + 16f, top + 154f, width - 32f, 20f), "F10 hides this explainer", _leftStyle);
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
