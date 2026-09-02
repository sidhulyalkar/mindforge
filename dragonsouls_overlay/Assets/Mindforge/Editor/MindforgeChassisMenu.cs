#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Mindforge.Chassis.Editor
{
    /// <summary>
    /// Small, non-invasive development entry point layered on top of the pinned
    /// Dragon Souls project. It deliberately does not rebuild or rewrite upstream
    /// scenes. V0.29 first qualifies the known working game before deeper porting.
    /// </summary>
    public static class MindforgeChassisMenu
    {
        public const string MainGameScene = "Assets/Levels/Scenes/MainGameScene.unity";
        public const string MainMenuScene = "Assets/Levels/Scenes/MainMenuScene.unity";
        public const string GameplayTestScene = "Assets/Levels/Scenes/GameplayTestScene.unity";

        [MenuItem("Mindforge/Chassis/PLAY MAIN GAME", priority = 1)]
        public static void PlayMainGame()
        {
            OpenAndPlay(MainGameScene);
        }

        [MenuItem("Mindforge/Chassis/PLAY COMBAT SANDBOX", priority = 2)]
        public static void PlayCombatSandbox()
        {
            OpenAndPlay(GameplayTestScene);
        }

        [MenuItem("Mindforge/Chassis/Open Main Menu", priority = 10)]
        public static void OpenMainMenu()
        {
            OpenScene(MainMenuScene);
        }

        [MenuItem("Mindforge/Chassis/Open Gameplay Test Scene", priority = 11)]
        public static void OpenGameplayTestScene()
        {
            OpenScene(GameplayTestScene);
        }

        [MenuItem("Mindforge/Chassis/Validate Pinned Base", priority = 20)]
        public static void ValidatePinnedBase()
        {
            string versionPath = Path.Combine(Application.dataPath, "..", "ProjectSettings", "ProjectVersion.txt");
            string version = File.Exists(versionPath) ? File.ReadAllText(versionPath) : string.Empty;
            bool correctVersion = version.Contains("m_EditorVersion: 2021.3.20f1");
            bool scenesPresent = File.Exists(MainGameScene) && File.Exists(MainMenuScene) && File.Exists(GameplayTestScene);
            bool markerPresent = File.Exists(Path.Combine(Application.dataPath, "Mindforge", "Provenance", "UPSTREAM.txt"));

            if (!correctVersion || !scenesPresent || !markerPresent)
            {
                throw new UnityEditor.Build.BuildFailedException(
                    $"Mindforge V0.29 chassis validation failed. Unity2021={correctVersion}, " +
                    $"scenes={scenesPresent}, provenance={markerPresent}"
                );
            }

            Debug.Log("[Mindforge:V29] Pinned Dragon Souls chassis validation PASS.");
        }

        [MenuItem("Mindforge/Chassis/Audit Active Chassis", priority = 21)]
        public static void AuditActiveChassis()
        {
            MindforgeChassisReadinessV29.AuditActiveScene();
        }

        private static void OpenAndPlay(string scenePath)
        {
            if (!OpenScene(scenePath)) return;
            EditorApplication.delayCall += () =>
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode)
                    EditorApplication.isPlaying = true;
            };
        }

        private static bool OpenScene(string scenePath)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[Mindforge:V29] Stop Play Mode before changing scenes.");
                return false;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return false;
            if (!File.Exists(scenePath))
                throw new UnityEditor.Build.BuildFailedException($"Dragon Souls scene missing: {scenePath}");
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            return true;
        }
    }
}
#endif
