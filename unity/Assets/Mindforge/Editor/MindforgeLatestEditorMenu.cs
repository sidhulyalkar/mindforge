#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Mindforge.Editor
{
    /// <summary>
    /// The only ordinary-development entry point for the current integrated game.
    ///
    /// Product version and scene-asset version are deliberately decoupled. The clean
    /// V0.11 scene builder remains the authoritative systems/traversal assembler. V0.20
    /// World Soul authors the continuous landscape, then V0.21 Arena + Patina performs the
    /// recording-driven arena/collision and environmental-cohesion pass before runtime systems
    /// compose after scene load.
    /// </summary>
    public static class MindforgeLatestEditorMenu
    {
        public const string ProductVersion = "V0.21 Arena + Patina";

        [MenuItem("Mindforge/Latest/PLAY LATEST (BCI Simulation)", priority = 1)]
        public static void PlayLatest()
        {
            if (!PrepareForSceneReplacement()) return;
            BuildCanonical(controllerOnlyByDefault: true);
            OpenCanonicalScene();
            EnsureWorldLayersOpenScene();
            Debug.Log($"[Mindforge:Latest] Starting {ProductVersion} in controller-only BCI simulation mode.");
            EditorApplication.delayCall += () =>
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode)
                    EditorApplication.isPlaying = true;
            };
        }

        [MenuItem("Mindforge/Latest/Rebuild Latest Integrated Scene", priority = 10)]
        public static void RebuildLatest()
        {
            if (!PrepareForSceneReplacement()) return;
            BuildCanonical(controllerOnlyByDefault: true);
            OpenCanonicalScene();
            EnsureWorldLayersOpenScene();
            Debug.Log($"[Mindforge:Latest] Rebuilt {ProductVersion} at {MindforgeDemoV11Builder.DemoScenePath}.");
        }

        [MenuItem("Mindforge/Latest/Open Latest Integrated Scene", priority = 11)]
        public static void OpenLatest()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[Mindforge:Latest] Stop Play Mode before changing scenes.");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EnsureCanonicalSceneExists(controllerOnlyByDefault: true);
            OpenCanonicalScene();
            EnsureWorldLayersOpenScene();
        }

        [MenuItem("Mindforge/Latest/Validate Latest Readiness", priority = 20)]
        public static void ValidateLatest()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                MindforgeLatestReadinessAuditV17.AuditActiveDemo();
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EnsureCanonicalSceneExists(controllerOnlyByDefault: true);
            OpenCanonicalScene();
            EnsureWorldLayersOpenScene();
            MindforgeLatestReadinessAuditV17.AuditActiveDemo();
        }

        [MenuItem("Mindforge/Latest/Build Neural-Hardware Variant", priority = 30)]
        public static void BuildNeuralHardwareVariant()
        {
            if (!PrepareForSceneReplacement()) return;
            BuildCanonical(controllerOnlyByDefault: false);
            OpenCanonicalScene();
            EnsureWorldLayersOpenScene();
            Debug.Log(
                $"[Mindforge:Latest] Built {ProductVersion} neural-hardware variant. " +
                "Use this only with the live neural service and a physically qualified display; " +
                "ordinary Editor playtests should use PLAY LATEST (BCI Simulation)."
            );
        }

        private static void BuildCanonical(bool controllerOnlyByDefault)
        {
            MindforgeDemoV11Builder.BuildDemoScene(controllerOnlyByDefault);
            WorldSoulV20Builder.ApplyOpenScene();
            WorldCohesionV21Builder.ApplyOpenScene();
        }

        private static bool PrepareForSceneReplacement()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[Mindforge:Latest] Stop Play Mode before rebuilding the canonical scene.");
                return false;
            }
            return EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        }

        private static void EnsureCanonicalSceneExists(bool controllerOnlyByDefault)
        {
            if (File.Exists(MindforgeDemoV11Builder.DemoScenePath)) return;
            BuildCanonical(controllerOnlyByDefault);
        }

        private static void OpenCanonicalScene()
        {
            if (!File.Exists(MindforgeDemoV11Builder.DemoScenePath))
                throw new UnityEditor.Build.BuildFailedException(
                    $"Canonical Mindforge scene missing after build: {MindforgeDemoV11Builder.DemoScenePath}"
                );

            EditorSceneManager.OpenScene(MindforgeDemoV11Builder.DemoScenePath, OpenSceneMode.Single);
        }

        private static void EnsureWorldLayersOpenScene()
        {
            if (!WorldSoulV20Builder.PresentInOpenScene())
                WorldSoulV20Builder.ApplyOpenScene();
            if (!WorldCohesionV21Builder.PresentInOpenScene())
                WorldCohesionV21Builder.ApplyOpenScene();
        }
    }
}
#endif
