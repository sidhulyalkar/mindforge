#if UNITY_EDITOR
using PlayerController;
using States;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Mindforge.Chassis.Editor
{
    /// <summary>
    /// Derives an isolated BCI qualification scene from the native-qualified V0.31
    /// production slice. The source scene is never edited in place.
    /// </summary>
    public static class MindforgeBciIntegrationBuilderV33
    {
        public const string SourceScene = MindforgeVerticalSliceBuilderV31.DestinationScene;
        public const string DestinationScene = "Assets/Mindforge/Scenes/MindforgeBciIntegrationV33.unity";
        public const string IntegrationRoot = "Mindforge_BCI_Integration_V33";

        [MenuItem("Mindforge/World V0.33/Build + Open BCI Integration", priority = 1)]
        public static void BuildAndOpen()
        {
            Build(refresh: true);
            EditorSceneManager.OpenScene(DestinationScene, OpenSceneMode.Single);
        }

        [MenuItem("Mindforge/World V0.33/PLAY BCI INTEGRATION", priority = 2)]
        public static void Play()
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
                throw new UnityEditor.Build.BuildFailedException("Stop Play Mode before rebuilding V0.33.");

            MindforgeVerticalSliceBuilderV31.Build(refresh: refresh);

            if (refresh && AssetDatabase.LoadAssetAtPath<SceneAsset>(DestinationScene) != null)
                AssetDatabase.DeleteAsset(DestinationScene);

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(DestinationScene) == null)
            {
                if (!AssetDatabase.CopyAsset(SourceScene, DestinationScene))
                    throw new UnityEditor.Build.BuildFailedException($"Could not copy {SourceScene} to {DestinationScene}.");
                AssetDatabase.ImportAsset(DestinationScene, ImportAssetOptions.ForceSynchronousImport);
            }

            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.OpenScene(DestinationScene, OpenSceneMode.Single);
            GameObject old = GameObject.Find(IntegrationRoot);
            if (old != null) Object.DestroyImmediate(old);

            GameObject root = new GameObject(IntegrationRoot);
            root.AddComponent<MindforgeBciIntegrationRuntimeV33>();
            ValidateScene(root);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, DestinationScene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[Mindforge:V33] BCI integration scene ready. V0.31 game authority preserved; " +
                "V0.33 adds a derived-event neural seam and two-class test presentation."
            );
        }

        private static void ValidateScene(GameObject root)
        {
            if (root == null || root.GetComponent<MindforgeBciIntegrationRuntimeV33>() == null)
                throw new UnityEditor.Build.BuildFailedException("V0.33 BCI integration owner is missing.");
            if (root.GetComponentsInChildren<Collider>(true).Length != 0 ||
                root.GetComponentsInChildren<Rigidbody>(true).Length != 0)
                throw new UnityEditor.Build.BuildFailedException("V0.33 integration root must remain non-physical before runtime receptors render.");

            if (Object.FindObjectsOfType<PlayerStateMachine>(true).Length != 1)
                throw new UnityEditor.Build.BuildFailedException("V0.33 lost the single player authority.");
            if (Object.FindObjectsOfType<Sword>(true).Length != 1)
                throw new UnityEditor.Build.BuildFailedException("V0.33 lost the single authoritative sword.");
            if (Object.FindObjectsOfType<EnemyNightmareDragonController>(true).Length == 0)
                throw new UnityEditor.Build.BuildFailedException("V0.33 lost the inherited dragon boss pipeline.");
        }
    }
}
#endif
