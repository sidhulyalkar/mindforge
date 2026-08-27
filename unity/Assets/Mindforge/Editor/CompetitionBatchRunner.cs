#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace Mindforge.Editor
{
    /// <summary>
    /// Batch-mode qualification entry points. Reaching these methods already proves
    /// that Unity imported the project and compiled its editor/runtime assemblies.
    /// The assemble path then rebuilds the competition scene from a clean checkout
    /// before Gate 1 validation, so a stale committed scene cannot create a false pass.
    /// </summary>
    public static class CompetitionBatchRunner
    {
        public static void AssembleAndValidate()
        {
            try
            {
                Debug.Log($"[Mindforge] Batch Gate 1 starting. commit={Environment.GetEnvironmentVariable("MINDFORGE_GIT_SHA") ?? "unknown"}");
                CompetitionSceneAssembler.BuildAndValidate();
                Debug.Log("[Mindforge] Batch Gate 1 PASS.");
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Debug.LogError("[Mindforge] Batch Gate 1 FAIL.");
                EditorApplication.Exit(3);
            }
        }

        public static void ValidateExisting()
        {
            try
            {
                bool passed = CompetitionGateValidator.ValidateAndWrite(true);
                EditorApplication.Exit(passed ? 0 : 2);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(3);
            }
        }
    }
}
#endif
