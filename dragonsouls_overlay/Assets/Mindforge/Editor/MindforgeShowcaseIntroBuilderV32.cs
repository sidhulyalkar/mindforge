#if UNITY_EDITOR
using States;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace Mindforge.Chassis.Editor
{
    /// <summary>
    /// Creates a Mindforge-owned showcase chapter from the qualified V0.31 slice.
    /// V0.32 adds only non-physical semantic checkpoints and the showcase flow owner;
    /// inherited combat, NavMesh, collision and boss authority remain untouched.
    /// </summary>
    public static class MindforgeShowcaseIntroBuilderV32
    {
        public const string SourceScene = MindforgeVerticalSliceBuilderV31.DestinationScene;
        public const string DestinationScene = "Assets/Mindforge/Scenes/MindforgeShowcaseIntroV32.unity";
        public const string ShowcaseRoot = "Mindforge_Showcase_Intro_V32";
        public const string CheckpointRoot = "Mindforge_Showcase_Checkpoints_V32";
        public const int ExpectedCheckpointCount = 9;

        private static readonly float[] CheckpointFractions =
        {
            0.07f,
            0.14f,
            0.24f,
            0.34f,
            0.44f,
            0.56f,
            0.69f,
            0.83f,
            0.92f,
        };

        private static readonly MindforgeShowcaseStageV32[] CheckpointStages =
        {
            MindforgeShowcaseStageV32.MemoryForge,
            MindforgeShowcaseStageV32.BladeTraining,
            MindforgeShowcaseStageV32.FirstEncounter,
            MindforgeShowcaseStageV32.BciReveal,
            MindforgeShowcaseStageV32.SightPuzzle,
            MindforgeShowcaseStageV32.Traversal,
            MindforgeShowcaseStageV32.EliteEncounter,
            MindforgeShowcaseStageV32.BossApproach,
            MindforgeShowcaseStageV32.BossFight,
        };

        [MenuItem("Mindforge/World V0.32/Build + Open Showcase Intro", priority = 1)]
        public static void BuildAndOpen()
        {
            Build(refresh: true);
            EditorSceneManager.OpenScene(DestinationScene, OpenSceneMode.Single);
        }

        [MenuItem("Mindforge/World V0.32/PLAY SHOWCASE INTRO", priority = 2)]
        public static void PlayShowcaseIntro()
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
                throw new UnityEditor.Build.BuildFailedException("Stop Play Mode before rebuilding V0.32.");

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
            RebuildShowcaseRoots();
            ValidateShowcaseScene();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, DestinationScene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[Mindforge:V32] Showcase intro ready. V0.31 combat/world authority preserved; " +
                "nine collider-free route checkpoints now observe the intro-to-boss playthrough."
            );
        }

        private static void RebuildShowcaseRoots()
        {
            GameObject oldShowcase = GameObject.Find(ShowcaseRoot);
            if (oldShowcase != null) Object.DestroyImmediate(oldShowcase);
            GameObject oldCheckpoints = GameObject.Find(CheckpointRoot);
            if (oldCheckpoints != null) Object.DestroyImmediate(oldCheckpoints);

            GameObject showcase = new GameObject(ShowcaseRoot);
            showcase.AddComponent<MindforgeShowcaseFlowV32>();
            GameObject checkpointRoot = new GameObject(CheckpointRoot);

            PlayerStateMachine player = Object.FindObjectOfType<PlayerStateMachine>(true);
            EnemyNightmareDragonController dragon = Object.FindObjectOfType<EnemyNightmareDragonController>(true);
            if (player == null || dragon == null)
                throw new UnityEditor.Build.BuildFailedException("V0.32 checkpoints require player and boss anchors.");

            NavMeshHit playerHit;
            NavMeshHit bossHit;
            if (!NavMesh.SamplePosition(player.transform.position, out playerHit, 5f, NavMesh.AllAreas))
                throw new UnityEditor.Build.BuildFailedException("V0.32 could not anchor player to inherited NavMesh.");
            if (!NavMesh.SamplePosition(dragon.transform.position, out bossHit, 15f, NavMesh.AllAreas))
                throw new UnityEditor.Build.BuildFailedException("V0.32 could not anchor boss to inherited NavMesh.");

            NavMeshPath path = new NavMeshPath();
            if (!NavMesh.CalculatePath(playerHit.position, bossHit.position, NavMesh.AllAreas, path) ||
                path.status != NavMeshPathStatus.PathComplete || path.corners == null || path.corners.Length < 2)
            {
                throw new UnityEditor.Build.BuildFailedException("V0.32 requires the complete inherited player-to-boss path.");
            }

            if (CheckpointFractions.Length != ExpectedCheckpointCount || CheckpointStages.Length != ExpectedCheckpointCount)
                throw new UnityEditor.Build.BuildFailedException("V0.32 checkpoint configuration count drifted.");

            for (int i = 0; i < CheckpointFractions.Length; i++)
            {
                Vector3 center;
                Vector3 tangent;
                SamplePath(path.corners, CheckpointFractions[i], out center, out tangent);
                CreateCheckpoint(checkpointRoot.transform, CheckpointStages[i], center, tangent, i);
            }
        }

        private static void CreateCheckpoint(
            Transform parent,
            MindforgeShowcaseStageV32 stage,
            Vector3 center,
            Vector3 tangent,
            int index)
        {
            GameObject checkpoint = new GameObject($"V32_Checkpoint_{index:00}_{stage}");
            checkpoint.transform.SetParent(parent, true);
            checkpoint.transform.position = center;
            checkpoint.transform.rotation = Quaternion.LookRotation(tangent, Vector3.up);

            MindforgeShowcaseStageCheckpointV32 observer = checkpoint.AddComponent<MindforgeShowcaseStageCheckpointV32>();
            observer.Configure(stage, 5.5f);
        }

        private static void ValidateShowcaseScene()
        {
            GameObject root = GameObject.Find(ShowcaseRoot);
            GameObject checkpoints = GameObject.Find(CheckpointRoot);
            if (root == null || root.GetComponent<MindforgeShowcaseFlowV32>() == null)
                throw new UnityEditor.Build.BuildFailedException("V0.32 showcase flow owner is missing.");
            if (root.GetComponentsInChildren<Collider>(true).Length != 0 ||
                root.GetComponentsInChildren<Rigidbody>(true).Length != 0)
                throw new UnityEditor.Build.BuildFailedException("V0.32 showcase flow root must remain non-physical.");
            if (checkpoints == null || checkpoints.transform.childCount != ExpectedCheckpointCount)
                throw new UnityEditor.Build.BuildFailedException("V0.32 chapter checkpoint set is incomplete.");
            if (checkpoints.GetComponentsInChildren<Collider>(true).Length != 0 ||
                checkpoints.GetComponentsInChildren<Rigidbody>(true).Length != 0)
                throw new UnityEditor.Build.BuildFailedException("V0.32 chapter checkpoints must remain collider-free and non-physical.");
            if (checkpoints.GetComponentsInChildren<MindforgeShowcaseStageCheckpointV32>(true).Length != ExpectedCheckpointCount)
                throw new UnityEditor.Build.BuildFailedException("V0.32 semantic checkpoint observers are incomplete.");

            if (Object.FindObjectsOfType<PlayerStateMachine>(true).Length != 1)
                throw new UnityEditor.Build.BuildFailedException("V0.32 lost the single player authority.");
            if (Object.FindObjectsOfType<Sword>(true).Length != 1)
                throw new UnityEditor.Build.BuildFailedException("V0.32 lost the single sword authority.");
            if (Object.FindObjectsOfType<EnemyNightmareDragonController>(true).Length == 0)
                throw new UnityEditor.Build.BuildFailedException("V0.32 lost the inherited boss pipeline.");
        }

        private static void SamplePath(Vector3[] corners, float fraction, out Vector3 position, out Vector3 tangent)
        {
            fraction = Mathf.Clamp01(fraction);
            float total = 0f;
            for (int i = 0; i < corners.Length - 1; i++)
                total += Vector3.Distance(corners[i], corners[i + 1]);
            if (total < 0.1f)
                throw new UnityEditor.Build.BuildFailedException("V0.32 inherited route has negligible length.");

            float target = total * fraction;
            float traversed = 0f;
            for (int i = 0; i < corners.Length - 1; i++)
            {
                Vector3 a = corners[i];
                Vector3 b = corners[i + 1];
                float segment = Vector3.Distance(a, b);
                if (traversed + segment >= target)
                {
                    float t = segment <= 0.001f ? 0f : (target - traversed) / segment;
                    position = Vector3.Lerp(a, b, t);
                    tangent = b - a;
                    tangent.y = 0f;
                    tangent = tangent.sqrMagnitude < 0.001f ? Vector3.forward : tangent.normalized;
                    return;
                }
                traversed += segment;
            }

            position = corners[corners.Length - 1];
            tangent = corners[corners.Length - 1] - corners[corners.Length - 2];
            tangent.y = 0f;
            tangent = tangent.sqrMagnitude < 0.001f ? Vector3.forward : tangent.normalized;
        }
    }
}
#endif
