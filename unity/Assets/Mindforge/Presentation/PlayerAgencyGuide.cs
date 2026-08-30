using System;
using UnityEngine;
using Mindforge.Calibration;
using Mindforge.Combat;
using Mindforge.SoulWisp;

namespace Mindforge.Presentation
{
    /// <summary>
    /// Non-authoritative presentation for the grounded-world build. V0.5 uses progressive
    /// disclosure: teach the tiny core vocabulary first, reveal advanced skills only after
    /// the player has swung and evaded, and keep the complete kit one Tab away.
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
        [SerializeField] private GuardianControlProfileV1 controls;
        [SerializeField] private GuardianInteractionRouterV1 interactionRouter;
        [SerializeField] private float combatGuideSeconds = 22f;

        private bool _judgeLens;
        private bool _combatObserved;
        private bool _swordUsed;
        private bool _rollUsed;
        private bool _interactionUsed;
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
            if (controls == null) controls = GuardianControlProfileV1.ResolveOrCreate();
            if (interactionRouter == null) interactionRouter = FindObjectOfType<GuardianInteractionRouterV1>(true);
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
            if (interactionRouter != null)
            {
                interactionRouter.InteractionPerformed -= OnInteraction;
                interactionRouter.InteractionPerformed += OnInteraction;
            }
        }

        private void Unsubscribe()
        {
            if (combat != null) combat.ActionAccepted -= OnCombatAction;
            if (physicalCombat != null) physicalCombat.SwordAttackStarted -= OnSwordAttack;
            if (motor != null) motor.DashStarted -= OnDashStarted;
            if (interactionRouter != null) interactionRouter.InteractionPerformed -= OnInteraction;
        }

        private void Update()
        {
            if (input == null || combat == null || physicalCombat == null || motor == null || calibration == null || cameraRig == null || controls == null)
            {
                Unsubscribe();
                Resolve();
                Subscribe();
            }

            if (controls != null && controls.Pressed(GuardianControlAction.JudgeLens))
                _judgeLens = !_judgeLens;

            if (!_combatObserved && CombatOpen())
            {
                _combatObserved = true;
                _combatGuideUntil = Time.realtimeSinceStartupAsDouble + Mathf.Max(1f, combatGuideSeconds);
            }
        }

        private void OnSwordAttack() => _swordUsed = true;
        private void OnDashStarted() => _rollUsed = true;
        private void OnInteraction(string id) => _interactionUsed = true;

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

            // V0.9 gives ordinary production play one UI voice. Reticles stay here because
            // they are spatially coupled to aim/lock state, and the judge lens remains an
            // explicit opt-in authority explainer. The large progressive lesson, lock footer
            // and Tab footer yield to the compact ProductionHudV09 while it is active.
            if (ProductionHudV09.Active)
            {
                if (_judgeLens) DrawJudgeLens();
                return;
            }

            float width = Mathf.Min(Screen.width - 32f, 860f);
            float left = (Screen.width - width) * 0.5f;

            if (!CombatOpen())
            {
                GUI.Box(new Rect(left, Screen.height - 74f, width, 44f),
                    "AWAKENING  ·  Sight can transform the blade  ·  your hands still own every action");
            }
            else
            {
                string lesson = CurrentLesson();
                // Do not compete with a nearby context prompt for the same bottom-center space.
                bool contextPromptVisible = interactionRouter != null && interactionRouter.HasOffer;
                if (!string.IsNullOrEmpty(lesson) && !contextPromptVisible)
                    GUI.Box(new Rect(left, Screen.height - 74f, width, 44f), lesson);
            }

            string lockKey = Label(GuardianControlAction.TargetLock, "T");
            string focusState = cameraRig != null && cameraRig.TargetFocusActive ? lockKey + "  LOCKED" : lockKey + "  LOCK";
            GUI.Label(new Rect(18f, Screen.height - 34f, 180f, 22f), focusState, _centerStyle);
            GUI.Label(new Rect(Screen.width - 196f, Screen.height - 34f, 178f, 22f),
                Label(GuardianControlAction.Menu, "TAB") + "  KIT + CONTROLS", _centerStyle);
            if (_judgeLens) DrawJudgeLens();
        }

        private string CurrentLesson()
        {
            if (auras != null && auras.ConcordActive && flux != null && flux.IsFull)
                return "CONCORD ACTIVE  ·  " + Label(GuardianControlAction.Bloom, "R") + " TWIN ECLIPSE  ·  neural state opens the window; you choose the strike";

            if (flux != null && flux.IsFull)
                return "FLUX FULL  ·  " + Label(GuardianControlAction.Bloom, "R") + " GRAVITY BLOOM";

            if (Time.realtimeSinceStartupAsDouble > _combatGuideUntil) return null;
            if (!_swordUsed)
                return "WASD MOVE   ·   MOUSE / ARROWS CAMERA   ·   " +
                       Label(GuardianControlAction.JumpHover, "SPACE") + " JUMP / HOVER   ·   " +
                       Label(GuardianControlAction.Blade, "F / LMB") + " AETHERBLADE";
            if (!_rollUsed)
                return Label(GuardianControlAction.EvadeBoost, "SHIFT / RMB") + " EVADE   ·   " +
                       Label(GuardianControlAction.TargetLock, "T") + " LOCK   ·   MOUSE WHEEL CYCLES LOCKED TARGETS";
            if (!_interactionUsed)
                return Label(GuardianControlAction.Interact, "E") + " INTERACT   ·   one context button rides bikes, reconstructs at shrines, and will operate the world";
            if (!_cleaveUsed || !_counterUsed)
                return Label(GuardianControlAction.Cleave, "Q") + " CLEAVE   ·   " +
                       Label(GuardianControlAction.Counter, "C") + " COUNTER   ·   " +
                       Label(GuardianControlAction.Bloom, "R") + " BLOOM";
            return "READ → COMMIT → EVADE → REPOSITION   ·   " + Label(GuardianControlAction.Menu, "TAB") + " shows the full kit";
        }

        private string Label(GuardianControlAction action, string fallback)
            => controls != null ? controls.Label(action) : fallback;

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
                : "BCI: accepted Sight can boundedly amplify blade length/energy/damage; Guard remains an evaluated neural channel";

            GUI.Label(new Rect(left + 16f, top + 10f, width - 32f, 26f), "MINDFORGE AUTHORITY SPLIT", _leftStyle);
            GUI.Label(new Rect(left + 16f, top + 40f, width - 32f, 38f), "HANDS: move · camera · interact · target lock · jump/hover · evade · Aetherblade · skills", _leftStyle);
            GUI.Label(new Rect(left + 16f, top + 78f, width - 32f, 32f), bci, _leftStyle);
            GUI.Label(new Rect(left + 16f, top + 112f, width - 32f, 40f), "EEG never moves, jumps, hovers, evades, interacts, locks a target, rotates the camera, swings, or parries", _leftStyle);
            GUI.Label(new Rect(left + 16f, top + 156f, width - 32f, 20f), Label(GuardianControlAction.JudgeLens, "F10") + " hides this explainer", _leftStyle);
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
