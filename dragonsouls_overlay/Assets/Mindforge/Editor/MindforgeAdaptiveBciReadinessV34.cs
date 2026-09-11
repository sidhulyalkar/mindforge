#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Mindforge.Chassis.Editor
{
    /// <summary>
    /// Native readiness audit for the adaptive onboarding layer. It separates missing
    /// software/runtime contracts from experimental evidence that simply has not been
    /// observed yet.
    /// </summary>
    public static class MindforgeAdaptiveBciReadinessV34
    {
        [MenuItem("Mindforge/World V0.34/Audit Adaptive Tutorial", priority = 20)]
        public static void Audit()
        {
            List<string> failures = new List<string>();
            List<string> deferred = new List<string>();

            MindforgeAdaptiveBciRuntimeV34 runtime = Object.FindObjectOfType<MindforgeAdaptiveBciRuntimeV34>(true);
            MindforgeBciIntegrationRuntimeV33 v33 = Object.FindObjectOfType<MindforgeBciIntegrationRuntimeV33>(true);
            MindforgeUdpGazeReceiverV34 gaze = Object.FindObjectOfType<MindforgeUdpGazeReceiverV34>(true);
            MindforgeGazeProfilerV34 profiler = Object.FindObjectOfType<MindforgeGazeProfilerV34>(true);
            MindforgeAdaptiveStimulusLayoutV34 layout = Object.FindObjectOfType<MindforgeAdaptiveStimulusLayoutV34>(true);
            MindforgeGazeBciEvidenceV34 correlator = Object.FindObjectOfType<MindforgeGazeBciEvidenceV34>(true);
            MindforgeAdaptiveBciTutorialV34 tutorial = Object.FindObjectOfType<MindforgeAdaptiveBciTutorialV34>(true);
            MindforgeAdaptiveBciInvariantV34 invariant = Object.FindObjectOfType<MindforgeAdaptiveBciInvariantV34>(true);
            MindforgeBciStimulusV33 stimulus = Object.FindObjectOfType<MindforgeBciStimulusV33>(true);
            MindforgeBciCalibrationDirectorV33 calibration = Object.FindObjectOfType<MindforgeBciCalibrationDirectorV33>(true);
            MindforgeBciMarkerSenderV33 markers = Object.FindObjectOfType<MindforgeBciMarkerSenderV33>(true);

            if (runtime == null) failures.Add("v34_runtime_missing");
            if (v33 == null) failures.Add("v33_authority_missing");

            if (EditorApplication.isPlaying)
            {
                if (runtime == null || !runtime.Installed) failures.Add("v34_runtime_not_installed");
                if (gaze == null || gaze.Port != 19746) failures.Add("gaze_receiver_contract");
                if (profiler == null) failures.Add("gaze_profiler_runtime");
                if (layout == null) failures.Add("adaptive_layout_runtime");
                if (correlator == null || string.IsNullOrEmpty(correlator.LogPath)) failures.Add("gaze_bci_evidence_runtime");
                if (tutorial == null) failures.Add("tutorial_runtime");
                if (invariant == null) failures.Add("adaptive_invariant_runtime");
                if (stimulus == null || !stimulus.Installed) failures.Add("v33_stimulus_runtime");
                if (calibration == null) failures.Add("v33_calibration_runtime");
                if (markers == null) failures.Add("v33_marker_runtime");

                if (calibration != null && !calibration.RequireFrozenStimulusLayout)
                    failures.Add("calibration_layout_freeze_gate_disabled");
                if (calibration != null && !calibration.AdaptiveRepeatedBlocks)
                    failures.Add("adaptive_repeated_blocks_disabled");
                if (invariant != null && invariant.ViolationCount > 0)
                    failures.Add("adaptive_runtime_invariant_violation");

                if (layout != null && layout.Frozen)
                {
                    if (stimulus == null || !stimulus.LayoutFrozen) failures.Add("layout_freeze_not_reflected_in_stimulus");
                    else if (layout.LayoutId != stimulus.LayoutId) failures.Add("layout_identity_mismatch");
                    if (correlator != null && !correlator.TargetGeometryReady) failures.Add("gaze_bci_target_geometry");
                }
                else deferred.Add("frozen_stimulus_layout");

                if (tutorial == null || !tutorial.Started)
                {
                    deferred.Add("tutorial_not_started");
                }
                else if (!tutorial.Finished)
                {
                    deferred.Add("tutorial_in_progress");
                }
                else
                {
                    if (string.IsNullOrEmpty(tutorial.ReceiptPath) || !File.Exists(tutorial.ReceiptPath))
                        failures.Add("tutorial_receipt_missing");
                    if (tutorial.Stage == MindforgeAdaptiveBciTutorialV34.TutorialStage.Partial)
                        deferred.Add("tutorial_partial_not_promotable");
                    if (tutorial.Stage == MindforgeAdaptiveBciTutorialV34.TutorialStage.Complete &&
                        (calibration == null || !calibration.IsCalibrated))
                        failures.Add("complete_tutorial_without_calibration");
                }

                if (gaze != null && !gaze.IsConnected) deferred.Add("gaze_stream_unobserved");
                if (profiler != null && profiler.UsableSamples == 0) deferred.Add("gaze_profile_unobserved");
                if (correlator != null && correlator.RecordCount == 0) deferred.Add("gaze_bci_correlation_unobserved");
                if (calibration != null && !calibration.IsCalibrated) deferred.Add("neural_calibration_unobserved");
            }
            else
            {
                deferred.Add("runtime_components_install_on_play");
                deferred.Add("gaze_profile_requires_play_mode");
                deferred.Add("gaze_bci_correlation_requires_play_mode");
                deferred.Add("tutorial_receipt_requires_play_mode");
            }

            bool staticNeuralContract =
                Mathf.Approximately(MindforgeBciStimulusV33.SightFrequencyHz, 10f) &&
                Mathf.Approximately(MindforgeBciStimulusV33.GuardFrequencyHz, 12f) &&
                MindforgeBciStimulusV33.ProductionTargetCount == 2;
            if (!staticNeuralContract) failures.Add("two_class_frequency_contract");

            bool staticLayoutContract =
                Mathf.Approximately(MindforgeBciStimulusV33.DefaultSightLocalPosition.x, -0.105f) &&
                Mathf.Approximately(MindforgeBciStimulusV33.DefaultGuardLocalPosition.x, 0.105f);
            if (!staticLayoutContract) failures.Add("default_layout_contract");

            string stage = tutorial != null ? tutorial.StatusLabel : "UNAVAILABLE";
            string layoutId = layout != null ? layout.LayoutId : "unavailable";
            string gazeState = gaze == null ? "unavailable" : gaze.IsConnected ? "linked" : "offline";
            int evidenceRecords = correlator != null ? correlator.RecordCount : 0;
            string summary =
                $"[Mindforge:V34:AUDIT] {(failures.Count == 0 ? "PASS" : "FAIL")} " +
                $"failures={failures.Count} deferred={deferred.Count} tutorial={stage} " +
                $"gaze={gazeState} layout={layoutId} gaze_bci_records={evidenceRecords} " +
                "raw_eeg_in_unity=false physical_timing_observed=false";

            if (failures.Count == 0) Debug.Log(summary);
            else Debug.LogError(summary + " failed=[" + string.Join(",", failures) + "]");
            if (deferred.Count > 0)
                Debug.Log("[Mindforge:V34:AUDIT] UNOBSERVED=[" + string.Join(",", deferred) + "]");
        }
    }
}
#endif
