using UnityEngine;

namespace Mindforge.Chassis
{
    /// <summary>
    /// Installs the V0.33 BCI integration spine on top of the native-qualified V0.31
    /// Dragon Souls slice. It owns neural presentation/transport/semantics plus a
    /// presentation-only third-person framing correction. Player movement, sword combat,
    /// enemy AI, health and inherited camera switching remain Dragon Souls-authoritative.
    /// </summary>
    [DefaultExecutionOrder(1200)]
    [DisallowMultipleComponent]
    public sealed class MindforgeBciIntegrationRuntimeV33 : MonoBehaviour
    {
        public const string ProductVersion = "V0.33 BCI Integration Spine";

        public bool Installed { get; private set; }

        private void Start()
        {
            Install<MindforgeNativeProvenanceV33>();
            Install<MindforgeThirdPersonCameraPresentationV33>();
            Install<MindforgeDisplayTimingMonitorV33>();
            Install<MindforgeBciMarkerSenderV33>();
            Install<MindforgeUdpNeuralReceiverV33>();
            Install<MindforgeBciStimulusV33>();
            Install<MindforgeBciCalibrationDirectorV33>();
            Install<MindforgeNeuralWindowControllerV33>();
            Install<MindforgeNeuralIntentBridgeV33>();
            Install<MindforgeBciDevInputV33>();
            Install<MindforgeSightReceptorV33>();
            Install<MindforgeGuardReceptorV33>();
            Install<MindforgeBciStatusHudV33>();
            Install<MindforgeBciSessionLoggerV33>();
            Install<MindforgeBciQualificationHarnessV33>();

            SuppressLegacyPreview();
            Installed = true;
            Debug.Log(
                "[Mindforge:V33] BCI spine installed: Sight=10 Hz, Guard=12 Hz, " +
                "derived events on UDP 19742, Unity markers on UDP 19743."
            );
        }

        private void Update()
        {
            // The V0.31 runtime installs its preview dynamically. Keep it suppressed
            // if script execution order or a domain reload creates it after this owner.
            SuppressLegacyPreview();
        }

        private T Install<T>() where T : Component
        {
            T component = GetComponent<T>();
            if (component == null) component = gameObject.AddComponent<T>();
            return component;
        }

        private static void SuppressLegacyPreview()
        {
            MindforgeBciOrbV31 legacy = FindObjectOfType<MindforgeBciOrbV31>(true);
            if (legacy != null) legacy.enabled = false;

            Camera camera = Camera.main;
            if (camera == null) return;
            Transform oldVisual = camera.transform.Find("Mindforge_BCI_Orb_V31");
            if (oldVisual != null && oldVisual.gameObject.activeSelf)
                oldVisual.gameObject.SetActive(false);
        }
    }
}
