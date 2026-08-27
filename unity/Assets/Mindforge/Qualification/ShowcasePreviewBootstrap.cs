#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Mindforge.Qualification
{
    /// <summary>
    /// Development-only bridge used by the Unity Showcase menu. It opens the real
    /// competition arena through the existing controller-only qualification path;
    /// it never fabricates calibration success or neural evidence.
    /// </summary>
    public sealed class ShowcasePreviewBootstrap : MonoBehaviour
    {
        public const string EditorPreferenceKey = "Mindforge.Showcase.AutoControllerOnly";
        public const string CommandLineFlag = "-mindforge-showcase";
        public const string EnvironmentVariable = "MINDFORGE_SHOWCASE";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (FindObjectOfType<ShowcasePreviewBootstrap>(true) != null) return;
            new GameObject("MindforgeShowcasePreviewBootstrap").AddComponent<ShowcasePreviewBootstrap>();
        }

        private IEnumerator Start()
        {
            if (!Requested()) yield break;

            Application.targetFrameRate = 120;
            // RuntimeInitialize callbacks share a phase. Wait for the qualification
            // bootstrap and scene OnEnable callbacks rather than relying on ordering.
            for (int frame = 0; frame < 90; frame++)
            {
                ControllerOnlyQualificationBootstrap qualification =
                    FindObjectOfType<ControllerOnlyQualificationBootstrap>(true);
                if (qualification != null)
                {
                    if (qualification.EnterControllerOnly("SHOWCASE_PREVIEW"))
                    {
                        Debug.LogWarning(
                            "[Mindforge:Showcase] Combat showcase opened with BCI explicitly disabled. " +
                            "Use the normal calibrated path for neural validation.");
                        yield break;
                    }
                }
                yield return null;
            }

            Debug.LogError("[Mindforge:Showcase] Unable to enter controller-only preview after 90 frames.");
        }

        private static bool Requested()
        {
#if UNITY_EDITOR
            if (EditorPrefs.GetBool(EditorPreferenceKey, false))
            {
                // One-shot intent. A subsequent ordinary Play should return to the
                // real Awakening/calibration flow unless the user chooses Showcase again.
                EditorPrefs.SetBool(EditorPreferenceKey, false);
                return true;
            }
#endif
            string env = Environment.GetEnvironmentVariable(EnvironmentVariable);
            if (string.Equals(env, "1", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(env, "true", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(env, "yes", StringComparison.OrdinalIgnoreCase))
                return true;

            foreach (string arg in Environment.GetCommandLineArgs())
                if (string.Equals(arg, CommandLineFlag, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }
    }
}
#endif
