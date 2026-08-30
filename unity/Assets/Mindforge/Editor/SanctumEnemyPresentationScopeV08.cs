#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Mindforge.Journey;
using Mindforge.World;

namespace Mindforge.Editor
{
    /// <summary>
    /// Scope guard for V0.8 reference silhouettes. The bright Sanctum pass may refine the
    /// ordinary journey roster, but it must not flatten the specialized ten-identity
    /// Menagerie/Aetheria roster into the five base archetype shells. Gameplay authority is
    /// untouched; this class only removes an accidental presentation child if one was added.
    /// </summary>
    [InitializeOnLoad]
    public static class SanctumEnemyPresentationScopeV08
    {
        static SanctumEnemyPresentationScopeV08()
        {
            EditorApplication.delayCall += RemoveReferenceShellsFromSpecializedRosters;
            EditorSceneManager.sceneSaved += _ => RemoveReferenceShellsFromSpecializedRosters();
        }

        [MenuItem("Mindforge/Showcase/Validate Sanctum Enemy Presentation Scope V0.8", priority = 39)]
        public static void RemoveReferenceShellsFromSpecializedRosters()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            JourneyEnemyController[] enemies = UnityEngine.Object.FindObjectsOfType<JourneyEnemyController>(true);
            int removed = 0;
            for (int i = 0; i < enemies.Length; i++)
            {
                JourneyEnemyController enemy = enemies[i];
                if (enemy == null) continue;
                if (enemy.GetComponentInParent<ArenaMenagerieDirector>(true) == null) continue;
                Transform visuals = enemy.transform.Find("Visuals");
                if (visuals == null) continue;
                Transform reference = visuals.Find(SanctumReferenceFidelityV08Builder.EnemyRootName);
                if (reference == null) continue;
                UnityEngine.Object.DestroyImmediate(reference.gameObject);
                removed++;
            }

            if (removed <= 0) return;
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log($"[Mindforge:V08:Fidelity] Removed {removed} ordinary reference shells from specialized Menagerie/Aetheria enemies; their authored ten-identity presentation remains intact.");
        }
    }
}
#endif