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
    /// V0.11 scene builder is still the newest authoritative world assembler, while the
    /// V0.13 Wisp/combat systems install on top of that runtime. Keeping the proven scene
    /// path avoids a destructive rename while giving developers one unambiguous menu.
    /// </summary>
    public static class MindforgeLatestEditorMenu
    {
        public const string ProductVersion = "V0.13 Integrated";

        [MenuItem("Mindforge/Latest/PLAY LATEST (BCI Simulation)", priority = 1)]
        public static void PlayLatest()
        {
            if (!PrepareForSceneReplacement()) return;
            MindforgeDemoV11Builder.BuildDemoScene(controllerOnlyByDefault: true);
            OpenCanonicalScene();
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
            MindforgeDemoV11Builder.BuildDemoScene(controllerOnlyByDefault: true);
            OpenCanonicalScene();
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
        }

        [MenuItem("Mindforge/Latest/Validate Latest Architecture", priority = 20)]
        public static void ValidateLatest()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                // Runtime ownership checks are more complete in Play Mode, so auditing the
                // currently-running canonical scene is useful and intentionally supported.
                MindforgeDemoV11Audit.AuditActiveDemo();
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EnsureCanonicalSceneExists(controllerOnlyByDefault: true);
            OpenCanonicalScene();
            MindforgeDemoV11Audit.AuditActiveDemo();
        }

        [MenuItem("Mindforge/Latest/Build Neural-Hardware Variant", priority = 30)]
        public static void BuildNeuralHardwareVariant()
        {
            if (!PrepareForSceneReplacement()) return;
            MindforgeDemoV11Builder.BuildDemoScene(controllerOnlyByDefault: false);
            OpenCanonicalScene();
            Debug.Log(
                $"[Mindforge:Latest] Built {ProductVersion} neural-hardware variant. " +
                "Use this only with the live neural service; ordinary Editor playtests should use PLAY LATEST (BCI Simulation)."
            );
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
            MindforgeDemoV11Builder.BuildDemoScene(controllerOnlyByDefault);
        }

        private static void OpenCanonicalScene()
        {
            if (!File.Exists(MindforgeDemoV11Builder.DemoScenePath))
                throw new UnityEditor.Build.BuildFailedException(
                    $"Canonical Mindforge scene missing after build: {MindforgeDemoV11Builder.DemoScenePath}"
                );

            EditorSceneManager.OpenScene(MindforgeDemoV11Builder.DemoScenePath, OpenSceneMode.Single);
        }
    }
}
#endif
