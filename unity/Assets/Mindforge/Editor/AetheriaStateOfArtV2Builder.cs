#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Mindforge.Combat;
using Mindforge.Presentation;

namespace Mindforge.Editor
{
    /// <summary>
    /// Installs the Aetheria V2 production-polish layer after V1 world and Horde authoring.
    /// This pass adds no collision, damage, input, enemy scheduling or BCI authority.
    /// Keeping it isolated makes V1/V2 A-B testing deterministic in the editor pipeline.
    /// </summary>
    public static class AetheriaStateOfArtV2Builder
    {
        public const string RootName = "Mindforge_Aetheria_StateOfArt_V2";

        [MenuItem("Mindforge/Legacy/Showcase/Apply Aetheria State-of-Art V2", priority = 30)]
        public static void ApplyOpenScene()
        {
            GameObject world = EditorSceneLookup.FindIncludingInactive(AetheriaWorldV1Builder.RootName);
            if (world == null)
                throw new InvalidOperationException("Aetheria V2 requires Aetheria World V1 first.");

            GameObject previous = EditorSceneLookup.FindIncludingInactive(RootName);
            if (previous != null) UnityEngine.Object.DestroyImmediate(previous);
            GameObject marker = new GameObject(RootName);
            marker.transform.SetParent(world.transform, false);

            GameObject guardian = EditorSceneLookup.FindIncludingInactive("Guardian");
            if (guardian == null)
                throw new InvalidOperationException("Aetheria V2 requires Guardian.");

            if (guardian.GetComponent<HoverbikeKineticPresentationV2>() == null)
                guardian.AddComponent<HoverbikeKineticPresentationV2>();
            if (guardian.GetComponent<AetheriaCombatAudioV2>() == null)
                guardian.AddComponent<AetheriaCombatAudioV2>();

            FracturedSignalDirector boss = UnityEngine.Object.FindObjectOfType<FracturedSignalDirector>(true);
            if (boss == null)
                throw new InvalidOperationException("Aetheria V2 requires the existing Fractured Signal boss authority.");
            if (boss.GetComponent<LordMalatractPresentationV1>() == null)
                throw new InvalidOperationException("Aetheria V2 requires Lord Malatract V1 presentation first.");
            if (boss.GetComponent<LordMalatractPhaseStagingV2>() == null)
                boss.gameObject.AddComponent<LordMalatractPhaseStagingV2>();

            EditorUtility.SetDirty(marker);
            EditorUtility.SetDirty(guardian);
            EditorUtility.SetDirty(boss.gameObject);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[Mindforge:AetheriaV2] Installed replay-ready mounted presentation, event-driven procedural audio and " +
                "phase-readable Malatract staging. No new gameplay, collision, enemy, boss or neural authority was added.");
        }
    }
}
#endif
