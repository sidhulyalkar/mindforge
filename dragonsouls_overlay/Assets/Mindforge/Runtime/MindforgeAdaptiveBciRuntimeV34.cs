using UnityEngine;

namespace Mindforge.Chassis
{
    /// <summary>
    /// V0.34 participant-onboarding layer. It is intentionally stacked on V0.33 rather
    /// than replacing it: V0.33 remains neural transport/semantic authority while V0.34
    /// adds gaze measurement, conservative presentation personalization and tutorial flow.
    /// </summary>
    [DefaultExecutionOrder(1300)]
    [DisallowMultipleComponent]
    public sealed class MindforgeAdaptiveBciRuntimeV34 : MonoBehaviour
    {
        public const string ProductVersion = "V0.34 Adaptive BCI Tutorial";

        public bool Installed { get; private set; }

        private void Start()
        {
            MindforgeBciIntegrationRuntimeV33 v33 = GetComponent<MindforgeBciIntegrationRuntimeV33>();
            if (v33 == null)
            {
                Debug.LogError("[Mindforge:V34] V0.33 integration owner missing; adaptive tutorial refused to install.");
                enabled = false;
                return;
            }

            Install<MindforgeUdpGazeReceiverV34>();
            Install<MindforgeGazeProfilerV34>();
            Install<MindforgeAdaptiveStimulusLayoutV34>();
            Install<MindforgeGazeBciEvidenceV34>();
            Install<MindforgeAdaptiveBciTutorialV34>();
            Install<MindforgeAdaptiveBciInvariantV34>();

            MindforgeBciCalibrationDirectorV33 calibration = GetComponent<MindforgeBciCalibrationDirectorV33>();
            if (calibration != null) calibration.SetRequireFrozenStimulusLayout(true);

            Installed = true;
            Debug.Log(
                "[Mindforge:V34] Adaptive BCI tutorial ready: gaze UDP 19746 -> profile -> " +
                "frozen layout -> V0.33 calibration -> Sight/Guard practice; aggregate gaze/BCI evidence enabled."
            );
        }

        private T Install<T>() where T : Component
        {
            T component = GetComponent<T>();
            if (component == null) component = gameObject.AddComponent<T>();
            return component;
        }
    }
}
