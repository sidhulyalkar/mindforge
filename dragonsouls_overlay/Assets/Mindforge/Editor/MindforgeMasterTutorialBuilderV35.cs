#if UNITY_EDITOR
using States;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Mindforge.Chassis.Editor
{
    /// <summary>
    /// Builds the first canonical tutorial scene directly from V0.34 so all combat,
    /// BCI calibration and boss authority are exercised in one inherited world.
    /// </summary>
    public static class MindforgeMasterTutorialBuilderV35
    {
        public const string SourceScene = MindforgeAdaptiveBciBuilderV34.DestinationScene;
        public const string DestinationScene = "Assets/Mindforge/Scenes/MindforgeMasterTutorialV35.unity";

        [MenuItem("Mindforge/World V0.35/Build + Open Master Tutorial", priority = 1)]
        public static void BuildAndOpen()
        {
            Build(refresh: true);
            EditorSceneManager.OpenScene(DestinationScene, OpenSceneMode.Single);
        }

        [MenuItem("Mindforge/World V0.35/PLAY MASTER TUTORIAL", priority = 2)]
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
                throw new UnityEditor.Build.BuildFailedException("Stop Play Mode before rebuilding V0.35.");

            MindforgeAdaptiveBciBuilderV34.Build(refresh: refresh);

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
                throw new UnityEditor.Build.BuildFailedException("V0.35 source lost the V0.33/V0.34 integration root.");

            MindforgeMasterTutorialRuntimeV35 old = root.GetComponent<MindforgeMasterTutorialRuntimeV35>();
            if (old != null) Object.DestroyImmediate(old);
            root.AddComponent<MindforgeMasterTutorialRuntimeV35>();

            ValidateScene(root);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, DestinationScene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[Mindforge:V35] Master tutorial scene ready. " +
                "Use PLAY MASTER TUTORIAL for movement -> combat -> recovery -> BCI -> boss evidence."
            );
        }

        private static void ValidateScene(GameObject root)
        {
            if (root.GetComponent<MindforgeBciIntegrationRuntimeV33>() == null)
                throw new UnityEditor.Build.BuildFailedException("V0.35 lost the V0.33 BCI authority owner.");
            if (root.GetComponent<MindforgeAdaptiveBciRuntimeV34>() == null)
                throw new UnityEditor.Build.BuildFailedException("V0.35 lost the V0.34 adaptive BCI owner.");
            if (root.GetComponent<MindforgeMasterTutorialRuntimeV35>() == null)
                throw new UnityEditor.Build.BuildFailedException("V0.35 master tutorial owner is missing.");

            if (root.GetComponentsInChildren<Collider>(true).Length != 0 ||
                root.GetComponentsInChildren<Rigidbody>(true).Length != 0)
                throw new UnityEditor.Build.BuildFailedException(
                    "V0.35 tutorial integration root must remain collider-free and non-physical."
                );

            if (Object.FindObjectsOfType<PlayerStateMachine>(true).Length != 1)
                throw new UnityEditor.Build.BuildFailedException("V0.35 lost the single player authority.");
            if (Object.FindObjectsOfType<Sword>(true).Length != 1)
                throw new UnityEditor.Build.BuildFailedException("V0.35 lost the single authoritative sword.");
            if (Object.FindObjectsOfType<EnemyNightmareDragonController>(true).Length == 0)
                throw new UnityEditor.Build.BuildFailedException("V0.35 lost the inherited boss pipeline.");
            if (Object.FindObjectOfType<MindforgeVerticalSliceRuntimeV31>(true) == null)
                throw new UnityEditor.Build.BuildFailedException("V0.35 lost the V0.31 runtime that installs sword evidence.");
        }
    }
}
#endif
