#if UNITY_EDITOR
using System.IO;
using Cinemachine;
using PlayerController;
using States;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Mindforge.Chassis.Editor
{
    /// <summary>
    /// Creates a Mindforge-owned scene by copying Dragon Souls' known working
    /// GameplayTestScene. The upstream scene is never edited in place. This is the
    /// scene where we can widen halls, replace environment kits, relight the arena
    /// and iterate on final encounter art while preserving a clean baseline.
    /// </summary>
    public static class MindforgeCombatSliceBuilderV29
    {
        public const string SourceScene = MindforgeChassisMenu.GameplayTestScene;
        public const string DestinationScene = "Assets/Mindforge/Scenes/MindforgeCombatSliceV29.unity";
        public const string MarkerRoot = "Mindforge_Production_Combat_Slice_V29";

        [MenuItem("Mindforge/Chassis/Build + Open Mindforge Combat Slice", priority = 3)]
        public static void BuildAndOpen()
        {
            Build(refresh: true);
            EditorSceneManager.OpenScene(DestinationScene, OpenSceneMode.Single);
        }

        [MenuItem("Mindforge/Chassis/PLAY MINDFORGE COMBAT SLICE", priority = 4)]
        public static void PlaySlice()
        {
            Build(refresh: false);
            EditorSceneManager.OpenScene(DestinationScene, OpenSceneMode.Single);
            EditorApplication.delayCall += () =>
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode)
                    EditorApplication.isPlaying = true;
            };
        }

        public static void Build(bool refresh)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new UnityEditor.Build.BuildFailedException("Stop Play Mode before rebuilding the V0.29 combat slice.");
            if (!File.Exists(SourceScene))
                throw new UnityEditor.Build.BuildFailedException($"Dragon Souls source scene missing: {SourceScene}");

            EnsureFolder("Assets/Mindforge/Scenes");
            if (refresh && AssetDatabase.LoadAssetAtPath<SceneAsset>(DestinationScene) != null)
                AssetDatabase.DeleteAsset(DestinationScene);

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(DestinationScene) == null)
            {
                if (!AssetDatabase.CopyAsset(SourceScene, DestinationScene))
                    throw new UnityEditor.Build.BuildFailedException(
                        $"Could not copy {SourceScene} to {DestinationScene}."
                    );
                AssetDatabase.ImportAsset(DestinationScene, ImportAssetOptions.ForceSynchronousImport);
            }

            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.OpenScene(DestinationScene, OpenSceneMode.Single);
            GameObject existing = GameObject.Find(MarkerRoot);
            if (existing == null)
            {
                existing = new GameObject(MarkerRoot);
                existing.AddComponent<MindforgeCombatSliceMarkerV29>();
            }

            ValidateInheritedProductionSystems();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, DestinationScene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                "[Mindforge:V29] Production combat slice ready. " +
                "It is a Mindforge-owned copy of Dragon Souls' working GameplayTestScene; upstream remains untouched."
            );
        }

        private static void ValidateInheritedProductionSystems()
        {
            PlayerStateMachine player = Object.FindObjectOfType<PlayerStateMachine>(true);
            Sword sword = Object.FindObjectOfType<Sword>(true);
            CinemachineVirtualCamera[] virtualCameras = Object.FindObjectsOfType<CinemachineVirtualCamera>(true);
            BossManager boss = Object.FindObjectOfType<BossManager>(true);
            EnemyNightmareDragonController dragon = Object.FindObjectOfType<EnemyNightmareDragonController>(true);

            if (player == null)
                throw new UnityEditor.Build.BuildFailedException("V0.29 copied slice lost PlayerStateMachine.");
            if (sword == null)
                throw new UnityEditor.Build.BuildFailedException("V0.29 copied slice lost the authoritative Sword.");
            if (virtualCameras == null || virtualCameras.Length == 0)
                throw new UnityEditor.Build.BuildFailedException("V0.29 copied slice lost Cinemachine camera authority.");
            if (boss == null || dragon == null)
                throw new UnityEditor.Build.BuildFailedException("V0.29 copied slice lost the working dragon boss pipeline.");
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
#endif
