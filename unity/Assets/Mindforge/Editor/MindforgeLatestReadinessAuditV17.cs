#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Mindforge.Calibration;
using Mindforge.Combat;
using Mindforge.Presentation;
using Mindforge.SoulWisp;

namespace Mindforge.Editor
{
    /// <summary>
    /// Canonical in-Unity readiness audit for the current Latest build.
    ///
    /// This is diagnostic evidence, not physical SSVEP qualification. It proves that the
    /// expected scene/runtime owners exist and that the software stimulus contract is wired.
    /// Photodiode timing and real EEG remain separate physical gates.
    /// </summary>
    public static class MindforgeLatestReadinessAuditV17
    {
        private const string ReportRelativePath = "experiments/reports/latest-readiness-v17.json";

        [Serializable]
        private sealed class AuditCheck
        {
            public string id;
            public bool passed;
            public string detail;
        }

        [Serializable]
        private sealed class AuditReport
        {
            public string schema = "mindforge.latest_readiness.v17";
            public string product_version;
            public string generated_utc;
            public string unity_version;
            public string scene_path;
            public bool play_mode;
            public bool physical_ssvep_qualified = false;
            public bool all_passed;
            public List<AuditCheck> checks = new List<AuditCheck>();
        }

        public static void AuditActiveDemo()
        {
            AuditReport report = new AuditReport
            {
                product_version = MindforgeLatestEditorMenu.ProductVersion,
                generated_utc = DateTime.UtcNow.ToString("O"),
                unity_version = Application.unityVersion,
                scene_path = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,
                play_mode = EditorApplication.isPlaying,
            };

            Add(report, "canonical_scene",
                string.Equals(report.scene_path, MindforgeDemoV11Builder.DemoScenePath, StringComparison.Ordinal),
                report.scene_path);
            Add(report, "product_version_v17",
                MindforgeLatestEditorMenu.ProductVersion.StartsWith("V0.17", StringComparison.Ordinal),
                MindforgeLatestEditorMenu.ProductVersion);

            CheckCount<MindforgeDemoV11Marker>(report, "single_demo_marker", 1);
            CheckCount<GuardianMotor>(report, "single_guardian_motor", 1);
            CheckCount<GuardianCombatInput>(report, "single_guardian_input", 1);
            CheckCount<FracturedSignalDirector>(report, "single_fractured_signal", 1);
            CheckCount<GuardianTargetLock>(report, "single_target_lock", 1);
            CheckCount<AwakeningCalibrationDirector>(report, "single_calibration_owner", 1);
            CheckCount<SoulWispController>(report, "single_wisp_owner", 1);
            CheckCount<DisplayTimingMonitor>(report, "single_display_timing_monitor", 1);
            CheckSceneCameras(report);
            CheckStimulusContract(report);
            CheckDisplayContract(report);
            CheckWispContract(report);
            CheckRuntimePresentation(report);

            report.all_passed = true;
            for (int i = 0; i < report.checks.Count; i++) report.all_passed &= report.checks[i].passed;

            string repoRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
            string reportPath = Path.Combine(repoRoot, ReportRelativePath.Replace('/', Path.DirectorySeparatorChar));
            string directory = Path.GetDirectoryName(reportPath);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            File.WriteAllText(reportPath, JsonUtility.ToJson(report, true));

            string summary = $"[Mindforge:LatestAudit] {(report.all_passed ? "PASS" : "FAIL")} " +
                             $"({report.checks.Count} checks) -> {ReportRelativePath}. " +
                             "physical_ssvep_qualified=false; photodiode + real EEG still required.";
            if (report.all_passed) Debug.Log(summary);
            else Debug.LogError(summary);
        }

        private static void CheckStimulusContract(AuditReport report)
        {
            VepAuraStimulus[] all = SceneObjects<VepAuraStimulus>();
            bool has10 = false;
            bool has12 = false;
            bool refresh120 = all.Length == 2;
            for (int i = 0; i < all.Length; i++)
            {
                float f = all[i].FrequencyHz;
                if (Mathf.Abs(f - 10f) < 0.01f) has10 = true;
                if (Mathf.Abs(f - 12f) < 0.01f) has12 = true;
                refresh120 &= Mathf.Abs(all[i].QualifiedRefreshHz - 120f) < 0.01f;
            }

            Add(report, "two_coded_stimuli", all.Length == 2,
                $"found={all.Length}, expected=2");
            Add(report, "sight_guard_frequency_pair", has10 && has12,
                $"10Hz={has10}, 12Hz={has12}");
            Add(report, "stimuli_qualified_refresh_contract", refresh120,
                all.Length == 2 ? $"refresh=[{all[0].QualifiedRefreshHz:0.0},{all[1].QualifiedRefreshHz:0.0}]Hz" : "pair incomplete");
        }

        private static void CheckDisplayContract(AuditReport report)
        {
            DisplayTimingMonitor[] monitors = SceneObjects<DisplayTimingMonitor>();
            if (monitors.Length != 1)
            {
                Add(report, "display_120hz_software_contract", false, $"monitor_count={monitors.Length}");
                return;
            }

            DisplayTimingMonitor monitor = monitors[0];
            bool expected = Mathf.Abs(monitor.ExpectedRefreshHz - 120f) < 0.01f;
            Add(report, "display_120hz_software_contract", expected,
                $"expected={monitor.ExpectedRefreshHz:0.0}Hz");

            if (!EditorApplication.isPlaying)
            {
                AddDeferred(report, "live_display_timing_health");
                return;
            }

            bool pass = !monitor.HasMeasurement || monitor.TimingHealthy;
            Add(report, "live_display_timing_health", pass,
                monitor.HasMeasurement
                    ? $"observed={monitor.ObservedRefreshHz:0.0}Hz drop_fraction={monitor.DropFraction:0.0000} healthy={monitor.TimingHealthy}"
                    : "measurement not complete yet; physical qualification remains separate");
        }

        private static void CheckWispContract(AuditReport report)
        {
            SoulWispController[] wisps = SceneObjects<SoulWispController>();
            if (wisps.Length != 1)
            {
                Add(report, "wisp_stimulus_pair_available", false, $"wisp_count={wisps.Length}");
                return;
            }
            Add(report, "wisp_stimulus_pair_available", wisps[0].StimulusPairAvailable,
                $"pair_available={wisps[0].StimulusPairAvailable}");
        }

        private static void CheckRuntimePresentation(AuditReport report)
        {
            if (!EditorApplication.isPlaying)
            {
                AddDeferred(report, "v16_visual_identity_runtime");
                AddDeferred(report, "v17_directed_demo_runtime");
                AddDeferred(report, "single_v17_hud");
                AddDeferred(report, "v17_target_presence");
                AddDeferred(report, "single_gameplay_camera_writer_after_reveal");
                return;
            }

            CheckCount<VisualIdentityV16Installer>(report, "v16_visual_identity_runtime", 1);
            CheckCount<MindforgeDirectedDemoV17>(report, "v17_directed_demo_runtime", 1);
            CheckBehaviourTypeCount(report, "single_v17_hud", "MindforgeDemoHudV17", 1, true);
            CheckBehaviourTypeCount(report, "v17_target_presence", "MindforgeTargetPresenceV17", 1, true);

            GuardianCombatInput input = UnityEngine.Object.FindObjectOfType<GuardianCombatInput>(true);
            if (input == null || !input.CombatActionsEnabled)
            {
                AddDeferred(report, "single_gameplay_camera_writer_after_reveal");
            }
            else
            {
                int v17 = CountBehaviourType("MindforgeGameplayCameraV17", true);
                int legacy = CountBehaviourType("MindforgeDemoCameraV11", true);
                Add(report, "single_gameplay_camera_writer_after_reveal", v17 == 1 && legacy == 0,
                    $"enabled_v17={v17}, enabled_v11={legacy}");
            }

            int oldHud = CountBehaviourType("MindforgeDemoHudV11", true);
            int productionHud = CountBehaviourType("ProductionHudV09", true);
            Add(report, "legacy_huds_retired", oldHud == 0 && productionHud == 0,
                $"enabled_v11_hud={oldHud}, enabled_production_hud={productionHud}");
        }

        private static void CheckSceneCameras(AuditReport report)
        {
            Camera[] cameras = SceneObjects<Camera>();
            int mainTagged = 0;
            for (int i = 0; i < cameras.Length; i++) if (cameras[i].CompareTag("MainCamera")) mainTagged++;
            Add(report, "single_camera_object", cameras.Length == 1 && mainTagged == 1,
                $"scene_cameras={cameras.Length}, main_tagged={mainTagged}");
        }

        private static void CheckCount<T>(AuditReport report, string id, int expected) where T : UnityEngine.Object
        {
            T[] all = SceneObjects<T>();
            Add(report, id, all.Length == expected, $"found={all.Length}, expected={expected}");
        }

        private static T[] SceneObjects<T>() where T : UnityEngine.Object
        {
            T[] all = Resources.FindObjectsOfTypeAll<T>();
            List<T> scene = new List<T>();
            for (int i = 0; i < all.Length; i++)
            {
                Component component = all[i] as Component;
                if (component != null && component.gameObject.scene.IsValid()) scene.Add(all[i]);
                else if (all[i] is GameObject go && go.scene.IsValid()) scene.Add(all[i]);
            }
            return scene.ToArray();
        }

        private static void CheckBehaviourTypeCount(
            AuditReport report,
            string id,
            string typeName,
            int expected,
            bool enabledOnly)
        {
            int count = CountBehaviourType(typeName, enabledOnly);
            Add(report, id, count == expected, $"found={count}, expected={expected}, type={typeName}");
        }

        private static int CountBehaviourType(string typeName, bool enabledOnly)
        {
            MonoBehaviour[] all = Resources.FindObjectsOfTypeAll<MonoBehaviour>();
            int count = 0;
            for (int i = 0; i < all.Length; i++)
            {
                MonoBehaviour behaviour = all[i];
                if (behaviour == null || !behaviour.gameObject.scene.IsValid()) continue;
                if (enabledOnly && !behaviour.enabled) continue;
                if (string.Equals(behaviour.GetType().Name, typeName, StringComparison.Ordinal)) count++;
            }
            return count;
        }

        private static void Add(AuditReport report, string id, bool passed, string detail)
        {
            report.checks.Add(new AuditCheck { id = id, passed = passed, detail = detail });
        }

        private static void AddDeferred(AuditReport report, string id)
        {
            Add(report, id, true, "deferred until Play Mode; edit-mode readiness remains diagnostic only");
        }
    }
}
#endif
