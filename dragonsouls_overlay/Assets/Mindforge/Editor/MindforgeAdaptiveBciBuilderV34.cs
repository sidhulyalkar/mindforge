#if UNITY_EDITOR
using PlayerController;
using States;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Mindforge.Chassis.Editor
{
    /// <summary>
    /// Builds V0.34 as a strict derivative of the isolated V0.33 BCI integration scene.
    /// V0.33 remains the neural/gameplay authority and can continue native qualification
    /// independently while this tutorial tranche evolves on its stacked branch.
    /// </summary>
    public static class MindforgeAdaptiveBciBuilderV34
    {
        public const string SourceScene = MindforgeBciIntegrationBuilderV33.DestinationScene;
        public const string DestinationScene = "Assets/Mindforge/Scenes/MindforgeAdaptiveBciTutorialV34.unity";

        [MenuItem("Mindforge/World V0.34/Build + Open Adaptive Tutorial", priority = 1)]
        public static void BuildAndOpen()
        {
            Build(refresh: true);
            EditorSceneManager.OpenScene(DestinationScene, OpenSceneMode.Single);
        }

        [MenuItem("Mindforge/World V0.34/PLAY ADAPTIVE BCI TUTORIAL", priority = 2)]
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
                throw new UnityEditor.Build.BuildFailedException("Stop Play Mode before rebuilding V0.34.");

            MindforgeBciIntegrationBuilderV33.Build(refresh: refresh);

            if (refresh && AssetDatabase.LoadAssetAtPath<SceneAsset>(DestinationScene) != null)
                AssetDatabase.DeleteAsset(DestinationScene);

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(DestinationScene) == null)
            {
                if (!AssetDatabase.CopyAsset(SourceScene, DestinationScene))
                    throw new UnityEditor.Build.BuildFailedException($"Could not copy {SourceScene} to {DestinationScene}.");
                AssetDatabase.ImportAsset(DestinationScene, ImportAssetOptions.ForceSynchronousImport);
            }

            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.OpenScene(DestinationScene, OpenSceneMode.Single);
            GameObject root = GameObject.Find(MindforgeBciIntegrationBuilderV33.IntegrationRoot);
            if (root == null)
                throw new UnityEditor.Build.BuildFailedException("V0.34 source lost the V0.33 integration root.");

            MindforgeAdaptiveBciRuntimeV34 old = root.GetComponent<MindforgeAdaptiveBciRuntimeV34>();
            if (old != null) Object.DestroyImmediate(old);
            root.AddComponent<MindforgeAdaptiveBciRuntimeV34>();

            ValidateScene(root);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, DestinationScene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[Mindforge:V34] Adaptive tutorial scene ready. Press F9 in Play Mode to begin " +
                "controls -> gaze profile -> frozen layout -> BCI calibration -> practice."
            );
        }

        private static void ValidateScene(GameObject root)
        {
            if (root.GetComponent<MindforgeBciIntegrationRuntimeV33>() == null)
                throw new UnityEditor.Build.BuildFailedException("V0.34 lost the V0.33 BCI authority owner.");
            if (root.GetComponent<MindforgeAdaptiveBciRuntimeV34>() == null)
                throw new UnityEditor.Build.BuildFailedException("V0.34 adaptive tutorial owner is missing.");
            if (root.GetComponentsInChildren<Collider>(true).Length != 0 ||
                root.GetComponentsInChildren<Rigidbody>(true).Length != 0)
                throw new UnityEditor.Build.BuildFailedException("V0.34 tutorial root must remain non-physical in the authored scene.");

            if (Object.FindObjectsOfType<PlayerStateMachine>(true).Length != 1)
                throw new UnityEditor.Build.BuildFailedException("V0.34 lost the single player authority.");
            if (Object.FindObjectsOfType<Sword>(true).Length != 1)
                throw new UnityEditor.Build.BuildFailedException("V0.34 lost the single authoritative sword.");
            if (Object.FindObjectsOfType<EnemyNightmareDragonController>(true).Length == 0)
                throw new UnityEditor.Build.BuildFailedException("V0.34 lost the inherited dragon boss pipeline.");
        }
    }
}
#endif
