#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using UnityEngine;
using Mindforge.Calibration;
using Mindforge.Combat;
using Mindforge.Neural;
using Mindforge.Presentation;
using Mindforge.SoulWisp;
using Mindforge.Telemetry;

namespace Mindforge.Qualification
{
    /// <summary>
    /// Explicit development-only escape hatch for P2 controller-only qualification.
    ///
    /// This is deliberately not a fake neural source. It disables neural authority,
    /// disarms neural-link contingency, opens the real competition arena through the
    /// Awakening director, and leaves a persistent visual + GameMarker declaration
    /// that the run contains NO BCI AUTHORITY.
    ///
    /// The entire type is excluded from non-development player builds.
    /// </summary>
    public sealed class ControllerOnlyQualificationBootstrap : MonoBehaviour
    {
        public const string CommandLineFlag = "-mindforge-controller-only";
        public const string EnvironmentVariable = "MINDFORGE_CONTROLLER_ONLY";
        private const KeyCode EditorHotkey = KeyCode.F8;

        private bool _active;
        private string _activationReason = string.Empty;
        private GUIStyle _watermark;

        public bool Active => _active;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (FindObjectOfType<ControllerOnlyQualificationBootstrap>() != null) return;
            new GameObject("MindforgeControllerOnlyQualification")
                .AddComponent<ControllerOnlyQualificationBootstrap>();
        }

        private IEnumerator Start()
        {
            // Let scene Awake/OnEnable and the telemetry bootstrap settle first.
            yield return null;
            if (RequestedByEnvironmentOrCommandLine())
                EnterControllerOnly("EXPLICIT_LAUNCH_FLAG");
        }

        private void Update()
        {
#if UNITY_EDITOR
            if (!_active && Input.GetKeyDown(EditorHotkey))
                EnterControllerOnly("EDITOR_F8");
#endif
        }

        public bool EnterControllerOnly(string reason)
        {
            if (_active) return true;

            AwakeningCalibrationDirector calibration = FindObjectOfType<AwakeningCalibrationDirector>(true);
            if (calibration == null)
            {
                Debug.LogError("[Mindforge:P2] Controller-only mode requested but AwakeningCalibrationDirector is missing.");
                return false;
            }

            NeuralLinkContingency contingency = FindObjectOfType<NeuralLinkContingency>(true);
            UdpNeuralReceiver receiver = FindObjectOfType<UdpNeuralReceiver>(true);
            DualAuraCombatDirector auraAuthority = FindObjectOfType<DualAuraCombatDirector>(true);
            NeuralEvidenceHud evidenceHud = FindObjectOfType<NeuralEvidenceHud>(true);
            NeuralAuraFeedback auraFeedback = FindObjectOfType<NeuralAuraFeedback>(true);
            NeuralHapticFeedback haptics = FindObjectOfType<NeuralHapticFeedback>(true);

            // Fail closed with respect to neural authority. P2 is a game-quality test,
            // not a simulated BCI session.
            contingency?.Disarm();
            if (auraAuthority != null) auraAuthority.enabled = false;

            // P2 should not present intentional neural absence as a fault. Suppress
            // neural-only status/feedback while preserving the explicit qualification
            // watermark and GameMarker provenance below.
            if (evidenceHud != null) evidenceHud.enabled = false;
            if (auraFeedback != null) auraFeedback.enabled = false;
            if (haptics != null) haptics.enabled = false;
            if (receiver != null) receiver.enabled = false;

            if (!calibration.EnterControllerOnlyQualification())
            {
                Debug.LogError("[Mindforge:P2] Awakening refused controller-only qualification mode.");
                return false;
            }

            _active = true;
            _activationReason = string.IsNullOrWhiteSpace(reason) ? "EXPLICIT" : reason;

            UdpGameMarkerSender markers = FindObjectOfType<UdpGameMarkerSender>(true);
            markers?.Emit(
                "QUALIFICATION_MODE",
                "qualification",
                reason: "CONTROLLER_ONLY_NO_BCI");

            Debug.LogWarning(
                "[Mindforge:P2] CONTROLLER-ONLY QUALIFICATION ACTIVE. " +
                "Neural receiver and aura authority are disabled; this run is not BCI evidence.");
            return true;
        }

        private static bool RequestedByEnvironmentOrCommandLine()
        {
            string env = Environment.GetEnvironmentVariable(EnvironmentVariable);
            if (string.Equals(env, "1", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(env, "true", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(env, "yes", StringComparison.OrdinalIgnoreCase))
                return true;

            string[] args = Environment.GetCommandLineArgs();
            foreach (string arg in args)
                if (string.Equals(arg, CommandLineFlag, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        private void OnGUI()
        {
            if (!_active) return;

            // The compact V0.9 HUD carries the same explicit SHOWCASE · BCI OFF declaration.
            // Avoid rendering a second qualification watermark over the production frame.
            // Non-production qualification scenes retain this original persistent watermark.
            if (ProductionHudV09.Active) return;

            if (_watermark == null)
            {
                _watermark = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 10,
                    alignment = TextAnchor.UpperRight,
                    normal = { textColor = new Color(0.64f, 0.70f, 0.80f, 0.78f) },
                };
            }
            GUI.Label(
                new Rect(Screen.width - 290f, 10f, 278f, 20f),
                $"SHOWCASE · BCI OFF · {_activationReason}",
                _watermark);
        }
    }
}
#endif
