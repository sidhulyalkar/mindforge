#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Mindforge.Chassis.Editor
{
    /// <summary>
    /// Focused native audit for the V0.33 closed loop. Static installation checks can
    /// pass without a headset; calibration/selection evidence remains explicitly
    /// unobserved until Play Mode produces it.
    /// </summary>
    public static class MindforgeBciReadinessV33
    {
        [MenuItem("Mindforge/World V0.33/Audit BCI Integration", priority = 20)]
        public static void Audit()
        {
            List<string> failures = new List<string>();
            List<string> deferred = new List<string>();

            MindforgeBciIntegrationRuntimeV33 runtime = Object.FindObjectOfType<MindforgeBciIntegrationRuntimeV33>(true);
            if (runtime == null) failures.Add("integration_runtime_missing");

            MindforgeBciStimulusV33 stimulus = Object.FindObjectOfType<MindforgeBciStimulusV33>(true);
            MindforgeUdpNeuralReceiverV33 receiver = Object.FindObjectOfType<MindforgeUdpNeuralReceiverV33>(true);
            MindforgeBciCalibrationDirectorV33 calibration = Object.FindObjectOfType<MindforgeBciCalibrationDirectorV33>(true);
            MindforgeNeuralWindowControllerV33 windows = Object.FindObjectOfType<MindforgeNeuralWindowControllerV33>(true);
            MindforgeNeuralIntentBridgeV33 bridge = Object.FindObjectOfType<MindforgeNeuralIntentBridgeV33>(true);
            MindforgeSightReceptorV33 sight = Object.FindObjectOfType<MindforgeSightReceptorV33>(true);
            MindforgeGuardReceptorV33 guard = Object.FindObjectOfType<MindforgeGuardReceptorV33>(true);
            MindforgeBciSessionLoggerV33 logger = Object.FindObjectOfType<MindforgeBciSessionLoggerV33>(true);

            if (EditorApplication.isPlaying)
            {
                if (stimulus == null || !stimulus.Installed || stimulus.NodeCount != 2) failures.Add("two_class_stimulus_runtime");
                if (receiver == null) failures.Add("neural_udp_receiver_runtime");
                if (calibration == null) failures.Add("calibration_director_runtime");
                if (windows == null) failures.Add("neural_window_runtime");
                if (bridge == null) failures.Add("intent_bridge_runtime");
                if (sight == null || guard == null) failures.Add("semantic_receptors_runtime");
                if (logger == null || string.IsNullOrEmpty(logger.LogPath)) failures.Add("session_logger_runtime");

                if (calibration != null && !calibration.IsCalibrated) deferred.Add("human_or_synthetic_calibration");
                if (bridge != null && bridge.AcceptedCount == 0) deferred.Add("semantic_selection_observed");
                if (sight != null && sight.ActivationCount == 0) deferred.Add("sight_receptor_observed");
                if (guard != null && guard.ActivationCount == 0) deferred.Add("guard_receptor_observed");
            }
            else
            {
                deferred.Add("runtime_components_install_on_play");
                deferred.Add("calibration_and_selection_require_play_mode");
            }

            bool staticContract =
                Mathf.Approximately(MindforgeBciStimulusV33.SightFrequencyHz, 10f) &&
                Mathf.Approximately(MindforgeBciStimulusV33.GuardFrequencyHz, 12f) &&
                MindforgeBciStimulusV33.ProductionTargetCount == 2;
            if (!staticContract) failures.Add("decoder_frequency_contract");

            string summary =
                $"[Mindforge:V33:AUDIT] {(failures.Count == 0 ? "PASS" : "FAIL")} " +
                $"failures={failures.Count} deferred={deferred.Count} " +
                $"freq=Sight10/Guard12 raw_eeg_in_unity=false";

            if (failures.Count == 0) Debug.Log(summary);
            else Debug.LogError(summary + " failed=[" + string.Join(",", failures) + "]");
            if (deferred.Count > 0)
                Debug.Log("[Mindforge:V33:AUDIT] UNOBSERVED=[" + string.Join(",", deferred) + "]");
        }
    }
}
#endif
