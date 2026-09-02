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
    /// Promotes Dragon Souls' complete MainGameScene into a Mindforge-owned V0.30
    /// production world. The source scene is copied, never edited in place. V0.30
    /// adds only a collider-free presentation root and preserves all inherited world,
    /// navigation, progression, camera and combat authority.
    /// </summary>
    public static class MindforgeProductionWorldBuilderV30
    {
        public const string SourceScene = MindforgeChassisMenu.MainGameScene;
        public const string DestinationScene = "Assets/Mindforge/Scenes/MindforgeWorldV30.unity";
        public const string MarkerRoot = "Mindforge_Production_World_V30";
        public const string LightingRoot = "Mindforge_Lighting_Rig_V30";

        [MenuItem("Mindforge/World V0.30/Build + Open Production World", priority = 1)]
        public static void BuildAndOpen()
        {
            Build(refresh: true);
            EditorSceneManager.OpenScene(DestinationScene, OpenSceneMode.Single);
        }

        [MenuItem("Mindforge/World V0.30/PLAY PRODUCTION WORLD", priority = 2)]
        public static void PlayWorld()
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
                throw new UnityEditor.Build.BuildFailedException("Stop Play Mode before rebuilding the V0.30 production world.");
            if (!File.Exists(SourceScene))
                throw new UnityEditor.Build.BuildFailedException($"Dragon Souls full world scene missing: {SourceScene}");

            EnsureFolder("Assets/Mindforge/Scenes");
            if (refresh && AssetDatabase.LoadAssetAtPath<SceneAsset>(DestinationScene) != null)
                AssetDatabase.DeleteAsset(DestinationScene);

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(DestinationScene) == null)
            {
                if (!AssetDatabase.CopyAsset(SourceScene, DestinationScene))
                    throw new UnityEditor.Build.BuildFailedException($"Could not copy {SourceScene} to {DestinationScene}.");
                AssetDatabase.ImportAsset(DestinationScene, ImportAssetOptions.ForceSynchronousImport);
            }

            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.OpenScene(DestinationScene, OpenSceneMode.Single);
            GameObject root = GameObject.Find(MarkerRoot);
            if (root == null) root = new GameObject(MarkerRoot);
            if (root.GetComponent<MindforgeWorldPresentationV30>() == null)
                root.AddComponent<MindforgeWorldPresentationV30>();

            RebuildLightingRig(root.transform);
            ValidateInheritedWorldSystems();
            ValidateMindforgeRootIsPresentationOnly(root);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, DestinationScene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[Mindforge:V30] Production world ready. Full Dragon Souls MainGameScene was copied into a " +
                "Mindforge-owned scene; inherited geometry, NavMesh, progression, combat and collision remain authoritative."
            );
        }

        private static void RebuildLightingRig(Transform root)
        {
            Transform old = root.Find(LightingRoot);
            if (old != null) Object.DestroyImmediate(old.gameObject);

            GameObject rig = new GameObject(LightingRoot);
            rig.transform.SetParent(root, false);

            PlayerStateMachine player = Object.FindObjectOfType<PlayerStateMachine>(true);
            EnemyNightmareDragonController dragon = Object.FindObjectOfType<EnemyNightmareDragonController>(true);

            Vector3 start = player != null ? player.transform.position : Vector3.zero;
            Vector3 boss = dragon != null ? dragon.transform.position : start + Vector3.forward * 40f;

            CreatePointLight(rig.transform, "Spawn_NeuralFill", start + new Vector3(-2f, 4.5f, 1f),
                new Color(0.31f, 0.68f, 0.82f), 0.46f, 17f);
            CreatePointLight(rig.transform, "Spawn_StoneFill", start + new Vector3(4f, 3.2f, -2f),
                new Color(0.46f, 0.53f, 0.66f), 0.32f, 14f);
            CreatePointLight(rig.transform, "Boss_CorruptionFill", boss + new Vector3(0f, 6.5f, 0f),
                new Color(0.72f, 0.22f, 0.61f), 0.58f, 27f);
            CreatePointLight(rig.transform, "Boss_NeuralRim", boss + new Vector3(-7f, 4f, 5f),
                new Color(0.25f, 0.67f, 0.82f), 0.40f, 22f);
        }

        private static void CreatePointLight(Transform parent, string name, Vector3 worldPosition, Color color, float intensity, float range)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, true);
            go.transform.position = worldPosition;
            Light light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
            light.renderMode = LightRenderMode.Auto;
        }

        private static void ValidateInheritedWorldSystems()
        {
            PlayerStateMachine[] players = Object.FindObjectsOfType<PlayerStateMachine>(true);
            Sword[] swords = Object.FindObjectsOfType<Sword>(true);
            CinemachineVirtualCamera[] virtualCameras = Object.FindObjectsOfType<CinemachineVirtualCamera>(true);
            EnemyStateMachine[] enemies = Object.FindObjectsOfType<EnemyStateMachine>(true);
            BossManager[] bosses = Object.FindObjectsOfType<BossManager>(true);
            EnemyNightmareDragonController[] dragons = Object.FindObjectsOfType<EnemyNightmareDragonController>(true);
            Bonfire[] bonfires = Object.FindObjectsOfType<Bonfire>(true);
            BonfiresManager[] bonfireManagers = Object.FindObjectsOfType<BonfiresManager>(true);

            if (players.Length != 1)
                throw new UnityEditor.Build.BuildFailedException($"V0.30 full world expected one player, found {players.Length}.");
            if (swords.Length != 1)
                throw new UnityEditor.Build.BuildFailedException($"V0.30 full world expected one authoritative sword, found {swords.Length}.");
            if (virtualCameras.Length == 0)
                throw new UnityEditor.Build.BuildFailedException("V0.30 full world lost Cinemachine camera authority.");
            if (enemies.Length == 0)
                throw new UnityEditor.Build.BuildFailedException("V0.30 full world contains no standard enemy state machines.");
            if (bosses.Length == 0 || dragons.Length == 0)
                throw new UnityEditor.Build.BuildFailedException("V0.30 full world lost the dragon boss pipeline.");
            if (bonfires.Length == 0 || bonfireManagers.Length != 1)
                throw new UnityEditor.Build.BuildFailedException(
                    $"V0.30 full world lost bonfire progression authority: bonfires={bonfires.Length}, managers={bonfireManagers.Length}."
                );
        }

        private static void ValidateMindforgeRootIsPresentationOnly(GameObject root)
        {
            int colliders = root.GetComponentsInChildren<Collider>(true).Length;
            int rigidbodies = root.GetComponentsInChildren<Rigidbody>(true).Length;
            int characterControllers = root.GetComponentsInChildren<CharacterController>(true).Length;
            if (colliders != 0 || rigidbodies != 0 || characterControllers != 0)
            {
                throw new UnityEditor.Build.BuildFailedException(
                    $"V0.30 Mindforge presentation root acquired gameplay physics: colliders={colliders}, " +
                    $"rigidbodies={rigidbodies}, characterControllers={characterControllers}."
                );
            }
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
#endif
