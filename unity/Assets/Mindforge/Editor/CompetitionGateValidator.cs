#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Mindforge.Calibration;
using Mindforge.Combat;
using Mindforge.Neural;
using Mindforge.Presentation;
using Mindforge.Qualification;
using Mindforge.SoulWisp;
using Mindforge.Telemetry;

namespace Mindforge.Editor
{
    [Serializable]
    public sealed class GateCheck { public string name; public bool passed; public string detail; }
    [Serializable]
    public sealed class GateReport
    {
        public string schema = "mindforge.unity_gate1.v1";
        public string editor_version;
        public string generated_utc;
        public bool passed;
        public string scene_path;
        public List<GateCheck> checks = new List<GateCheck>();
    }

    public static class CompetitionGateValidator
    {
        public const string ScenePath = "Assets/Mindforge/Scenes/Mindforge_Competition.unity";

        [MenuItem("Mindforge/Competition/Validate Gate 1 Scene")]
        public static void ValidateMenu() => ValidateAndWrite(true);

        public static bool ValidateAndWrite(bool log)
        {
            GateReport report = new GateReport
            {
                editor_version = Application.unityVersion,
                generated_utc = DateTime.UtcNow.ToString("O"),
                scene_path = ScenePath,
            };
            Check(report, "Unity 2022.3", Application.unityVersion.StartsWith("2022.3."), Application.unityVersion);
            Check(report, "URP active", GraphicsSettings.currentRenderPipeline != null && GraphicsSettings.currentRenderPipeline.GetType().Name.Contains("Universal"),
                GraphicsSettings.currentRenderPipeline != null ? GraphicsSettings.currentRenderPipeline.GetType().Name : "none");
            Check(report, "Competition scene exists", File.Exists(ScenePath), ScenePath);
            if (File.Exists(ScenePath))
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                CheckComponent<UdpNeuralReceiver>(report, "UDP neural receiver");
                CheckComponent<AwakeningCalibrationDirector>(report, "Awakening calibration");
                CheckComponent<NeuralEvidenceHud>(report, "Spectator evidence HUD");
                CheckComponent<NeuralLinkContingency>(report, "Link contingency");
                CheckComponent<CombatBootstrap>(report, "120 Hz combat bootstrap");
                CheckComponent<FracturedSignalDirector>(report, "Fractured Signal boss");
                CheckComponent<SoulWispController>(report, "Soul Wisp");
                CheckComponent<PhotodiodePatch>(report, "10/12 Hz photodiode instrument");
                CheckComponent<DisplayTimingMonitor>(report, "Display timing monitor");
                CheckComponent<DisplayQualificationController>(report, "Display qualification logger");
                CheckComponent<DemoFaultHarness>(report, "Render-stall fault harness");
                CheckComponent<MindforgeSessionLogger>(report, "Derived session logger");
                VepAuraStimulus[] stimuli = UnityEngine.Object.FindObjectsOfType<VepAuraStimulus>(true);
                bool ten = false, twelve = false;
                foreach (VepAuraStimulus stimulus in stimuli)
                {
                    ten |= Mathf.Abs(stimulus.FrequencyHz - 10f) < 0.01f;
                    twelve |= Mathf.Abs(stimulus.FrequencyHz - 12f) < 0.01f;
                }
                Check(report, "Sight 10 Hz core", ten, $"stimuli={stimuli.Length}");
                Check(report, "Guard 12 Hz core", twelve, $"stimuli={stimuli.Length}");
                Check(report, "No missing MonoBehaviours", CountMissingScripts() == 0, $"missing={CountMissingScripts()}");
                bool inBuild = false;
                foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
                    inBuild |= scene.enabled && scene.path == ScenePath;
                Check(report, "Scene in build settings", inBuild, ScenePath);
            }
            report.passed = report.checks.TrueForAll(c => c.passed);
            string repoRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
            string outputDir = Path.Combine(repoRoot, "experiments", "reports");
            Directory.CreateDirectory(outputDir);
            string output = Path.Combine(outputDir, "unity-gate1-latest.json");
            File.WriteAllText(output, JsonUtility.ToJson(report, true));
            if (log) Debug.Log($"[Mindforge] Gate 1 {(report.passed ? "PASS" : "FAIL")}: {output}");
            return report.passed;
        }

        private static void Check(GateReport report, string name, bool passed, string detail)
            => report.checks.Add(new GateCheck { name = name, passed = passed, detail = detail });

        private static void CheckComponent<T>(GateReport report, string name) where T : UnityEngine.Object
        {
            T value = UnityEngine.Object.FindObjectOfType<T>(true);
            Check(report, name, value != null, value != null ? value.name : "missing");
        }

        private static int CountMissingScripts()
        {
            int missing = 0;
            foreach (GameObject go in UnityEngine.Object.FindObjectsOfType<GameObject>(true))
                missing += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
            return missing;
        }
    }
}
#endif
