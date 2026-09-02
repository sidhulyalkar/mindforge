#if UNITY_EDITOR
using System;
using Cinemachine;
using PlayerController;
using States;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace Mindforge.Chassis.Editor
{
    /// <summary>
    /// Builds the first production vertical slice from the Mac-qualified V0.30 world.
    /// It never edits the V0.30 scene in place. Added solid scenery comes from authored
    /// upstream prefabs, is grounded from renderer/collider bounds, and must carry a
    /// real non-trigger collider. A 14 m clear route is protected around the baked path.
    /// </summary>
    public static class MindforgeVerticalSliceBuilderV31
    {
        public const string SourceScene = MindforgeProductionWorldBuilderV30.DestinationScene;
        public const string DestinationScene = "Assets/Mindforge/Scenes/MindforgeVerticalSliceV31.unity";
        public const string MarkerRoot = "Mindforge_Production_Vertical_Slice_V31";
        public const string ArchitectureRoot = "Mindforge_Authored_Route_V31";
        public const float ProtectedHalfWidth = 7f;
        public const float BossExclusionRadius = 20f;
        public const int MaximumAddedSolidModules = 12;

        private const float BoundaryPadding = 0.75f;
        private const string CathedralBoundaryPrefab =
            "Assets/ThirdPartySources/Inguz Media Studio/The Beauty Medieval War Banners and PROPS/Prefab/Metal_Wall_With_Pillars.prefab";
        private const string CavernBoundaryPrefab =
            "Assets/ThirdPartySources/Inguz Media Studio/The Beauty Medieval War Banners and PROPS/Prefab/Rock_Wall.prefab";

        private static readonly float[] StationFractions = { 0.10f, 0.27f, 0.45f, 0.63f, 0.80f };

        [MenuItem("Mindforge/World V0.31/Build + Open Vertical Slice", priority = 1)]
        public static void BuildAndOpen()
        {
            Build(refresh: true);
            EditorSceneManager.OpenScene(DestinationScene, OpenSceneMode.Single);
        }

        [MenuItem("Mindforge/World V0.31/PLAY VERTICAL SLICE", priority = 2)]
        public static void PlayVerticalSlice()
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
                throw new UnityEditor.Build.BuildFailedException("Stop Play Mode before rebuilding V0.31.");

            MindforgeProductionWorldBuilderV30.Build(refresh: refresh);

            if (refresh && AssetDatabase.LoadAssetAtPath<SceneAsset>(DestinationScene) != null)
                AssetDatabase.DeleteAsset(DestinationScene);

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(DestinationScene) == null)
            {
                if (!AssetDatabase.CopyAsset(SourceScene, DestinationScene))
                    throw new UnityEditor.Build.BuildFailedException($"Could not copy {SourceScene} to {DestinationScene}.");
                AssetDatabase.ImportAsset(DestinationScene, ImportAssetOptions.ForceSynchronousImport);
            }

            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.OpenScene(DestinationScene, OpenSceneMode.Single);
            GameObject marker = GameObject.Find(MarkerRoot);
            if (marker == null) marker = new GameObject(MarkerRoot);
            if (marker.GetComponent<MindforgeVerticalSliceRuntimeV31>() == null)
                marker.AddComponent<MindforgeVerticalSliceRuntimeV31>();

            RebuildAuthoredRoute();
            ValidateInheritedGame();
            ValidateProductionRoots(marker);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, DestinationScene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[Mindforge:V31] Production vertical slice ready. V0.30 gameplay remains intact; " +
                "V0.31 added deterministic grounded route architecture outside a protected 14 m combat corridor."
            );
        }

        private static void RebuildAuthoredRoute()
        {
            GameObject old = GameObject.Find(ArchitectureRoot);
            if (old != null) UnityEngine.Object.DestroyImmediate(old);
            GameObject architecture = new GameObject(ArchitectureRoot);

            PlayerStateMachine player = UnityEngine.Object.FindObjectOfType<PlayerStateMachine>(true);
            EnemyNightmareDragonController dragon = UnityEngine.Object.FindObjectOfType<EnemyNightmareDragonController>(true);
            if (player == null || dragon == null)
                throw new UnityEditor.Build.BuildFailedException("V0.31 route authoring requires player and dragon anchors.");

            NavMeshHit playerHit;
            NavMeshHit bossHit;
            if (!NavMesh.SamplePosition(player.transform.position, out playerHit, 5f, NavMesh.AllAreas))
                throw new UnityEditor.Build.BuildFailedException("V0.31 could not anchor the player to the baked NavMesh.");
            if (!NavMesh.SamplePosition(dragon.transform.position, out bossHit, 15f, NavMesh.AllAreas))
                throw new UnityEditor.Build.BuildFailedException("V0.31 could not anchor the boss to the baked NavMesh.");

            NavMeshPath path = new NavMeshPath();
            if (!NavMesh.CalculatePath(playerHit.position, bossHit.position, NavMesh.AllAreas, path) ||
                path.status != NavMeshPathStatus.PathComplete || path.corners == null || path.corners.Length < 2)
            {
                throw new UnityEditor.Build.BuildFailedException("V0.31 requires a complete inherited player-to-boss NavMesh route.");
            }

            GameObject cathedral = AssetDatabase.LoadAssetAtPath<GameObject>(CathedralBoundaryPrefab);
            GameObject cavern = AssetDatabase.LoadAssetAtPath<GameObject>(CavernBoundaryPrefab);
            if (cavern == null)
                throw new UnityEditor.Build.BuildFailedException($"Required authored cavern boundary is missing: {CavernBoundaryPrefab}");

            int created = 0;
            for (int i = 0; i < StationFractions.Length; i++)
            {
                Vector3 center;
                Vector3 tangent;
                SamplePath(path.corners, StationFractions[i], out center, out tangent);
                Vector3 toBoss = center - bossHit.position;
                toBoss.y = 0f;
                if (toBoss.sqrMagnitude < BossExclusionRadius * BossExclusionRadius) continue;

                Vector3 lateral = Vector3.Cross(Vector3.up, tangent).normalized;
                GameObject preferred = i < 3 && cathedral != null ? cathedral : cavern;

                GameObject left = InstantiateBoundaryWithFallback(preferred, cavern, architecture.transform,
                    $"V31_Station_{i:00}_L", center, tangent, lateral, -1f);
                GameObject right = InstantiateBoundaryWithFallback(preferred, cavern, architecture.transform,
                    $"V31_Station_{i:00}_R", center, tangent, lateral, 1f);

                created += left == null ? 0 : 1;
                created += right == null ? 0 : 1;
            }

            if (created < 6 || created > MaximumAddedSolidModules)
                throw new UnityEditor.Build.BuildFailedException(
                    $"V0.31 expected 6-{MaximumAddedSolidModules} authored solid modules, created {created}."
                );
        }

        private static GameObject InstantiateBoundaryWithFallback(
            GameObject preferred,
            GameObject fallback,
            Transform parent,
            string name,
            Vector3 routeCenter,
            Vector3 tangent,
            Vector3 lateral,
            float side)
        {
            GameObject instance = InstantiatePrefab(preferred, parent, name);
            if (!HasRealBoundaryCollider(instance))
            {
                UnityEngine.Object.DestroyImmediate(instance);
                instance = InstantiatePrefab(fallback, parent, name + "_Fallback");
            }
            if (!HasRealBoundaryCollider(instance))
            {
                UnityEngine.Object.DestroyImmediate(instance);
                throw new UnityEditor.Build.BuildFailedException($"Authored boundary {name} has no usable non-trigger collider.");
            }

            instance.transform.rotation = Quaternion.LookRotation(tangent, Vector3.up);
            instance.transform.position = routeCenter + lateral * side * (ProtectedHalfWidth + 4f) + Vector3.up * 40f;

            AlignBoundaryBoundsToLane(instance, routeCenter, lateral, side);
            GroundInstance(instance, routeCenter.y);
            ValidateBoundaryClearance(instance, routeCenter, lateral, side);
            ConfigureRenderers(instance);
            return instance;
        }

        /// <summary>
        /// Positions the actual visible/physical bounds, not the prefab root pivot.
        /// Some upstream modular prefabs have very large local pivot offsets, so placing
        /// transform.position directly can put the renderer on the opposite side of the route.
        /// </summary>
        private static void AlignBoundaryBoundsToLane(GameObject instance, Vector3 routeCenter, Vector3 lateral, float side)
        {
            lateral = lateral.normalized;
            Bounds bounds = CalculateBounds(instance);
            float projectedRadius = ProjectedHalfExtent(bounds, lateral);
            float desiredSignedCenter = ProtectedHalfWidth + projectedRadius + BoundaryPadding;

            Vector3 delta = bounds.center - routeCenter;
            float currentSignedCenter = Vector3.Dot(delta, lateral) * side;
            float correction = desiredSignedCenter - currentSignedCenter;
            instance.transform.position += lateral * side * correction;

            Bounds aligned = CalculateBounds(instance);
            Vector3 alignedDelta = aligned.center - routeCenter;
            float resolvedSignedCenter = Vector3.Dot(alignedDelta, lateral) * side;
            if (Mathf.Abs(resolvedSignedCenter - desiredSignedCenter) > 0.05f)
            {
                throw new UnityEditor.Build.BuildFailedException(
                    $"{instance.name} bounds-center alignment failed: resolved={resolvedSignedCenter:F2}m, " +
                    $"target={desiredSignedCenter:F2}m."
                );
            }
        }

        private static float ProjectedHalfExtent(Bounds bounds, Vector3 axis)
        {
            axis = axis.normalized;
            return Mathf.Abs(axis.x) * bounds.extents.x +
                   Mathf.Abs(axis.y) * bounds.extents.y +
                   Mathf.Abs(axis.z) * bounds.extents.z;
        }

        private static GameObject InstantiatePrefab(GameObject prefab, Transform parent, string name)
        {
            if (prefab == null)
                throw new UnityEditor.Build.BuildFailedException($"Cannot instantiate missing prefab for {name}.");
            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null)
                throw new UnityEditor.Build.BuildFailedException($"PrefabUtility failed for {prefab.name}.");
            instance.name = name;
            instance.transform.SetParent(parent, true);
            instance.transform.localScale = Vector3.one;
            return instance;
        }

        private static void GroundInstance(GameObject instance, float fallbackGroundY)
        {
            Bounds before = CalculateBounds(instance);
            Vector3 origin = new Vector3(before.center.x, Mathf.Max(before.max.y + 40f, fallbackGroundY + 80f), before.center.z);
            RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, 500f, ~0, QueryTriggerInteraction.Ignore);
            float groundY = fallbackGroundY;
            float bestDistance = float.PositiveInfinity;
            for (int i = 0; i < hits.Length; i++)
            {
                Collider collider = hits[i].collider;
                if (collider == null || collider.transform.IsChildOf(instance.transform)) continue;
                if (collider.GetComponentInParent<PlayerStateMachine>() != null) continue;
                if (collider.GetComponentInParent<EnemyStateMachine>() != null) continue;
                if (hits[i].distance < bestDistance)
                {
                    bestDistance = hits[i].distance;
                    groundY = hits[i].point.y;
                }
            }

            Bounds bounds = CalculateBounds(instance);
            Vector3 position = instance.transform.position;
            position.y += groundY - bounds.min.y + 0.015f;
            instance.transform.position = position;
        }

        private static void ValidateBoundaryClearance(GameObject instance, Vector3 routeCenter, Vector3 lateral, float side)
        {
            lateral = lateral.normalized;
            Bounds bounds = CalculateBounds(instance);
            Vector3 delta = bounds.center - routeCenter;
            float signedCenter = Vector3.Dot(delta, lateral) * side;
            float projectedRadius = ProjectedHalfExtent(bounds, lateral);
            float innerEdgeDistance = signedCenter - projectedRadius;
            if (innerEdgeDistance < ProtectedHalfWidth - 0.05f)
            {
                throw new UnityEditor.Build.BuildFailedException(
                    $"{instance.name} violates the V0.31 protected route: innerEdge={innerEdgeDistance:F2}m, " +
                    $"required={ProtectedHalfWidth:F2}m."
                );
            }
        }

        private static bool HasRealBoundaryCollider(GameObject instance)
        {
            if (instance == null) return false;
            Collider[] colliders = instance.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider collider = colliders[i];
                if (collider == null || !collider.enabled || collider.isTrigger) continue;
                MeshCollider mesh = collider as MeshCollider;
                if (mesh == null || mesh.sharedMesh != null) return true;
            }
            return false;
        }

        private static Bounds CalculateBounds(GameObject instance)
        {
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
            Collider[] colliders = instance.GetComponentsInChildren<Collider>(true);
            bool hasBounds = false;
            Bounds bounds = new Bounds(instance.transform.position, Vector3.zero);

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || !renderer.enabled) continue;
                if (!hasBounds) { bounds = renderer.bounds; hasBounds = true; }
                else bounds.Encapsulate(renderer.bounds);
            }
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider collider = colliders[i];
                if (collider == null || !collider.enabled || collider.isTrigger) continue;
                if (!hasBounds) { bounds = collider.bounds; hasBounds = true; }
                else bounds.Encapsulate(collider.bounds);
            }

            if (!hasBounds)
                throw new UnityEditor.Build.BuildFailedException($"{instance.name} has neither visible nor physical bounds.");
            return bounds;
        }

        private static void ConfigureRenderers(GameObject instance)
        {
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                renderers[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                renderers[i].receiveShadows = true;
            }
        }

        private static void SamplePath(Vector3[] corners, float fraction, out Vector3 position, out Vector3 tangent)
        {
            fraction = Mathf.Clamp01(fraction);
            float total = 0f;
            for (int i = 0; i < corners.Length - 1; i++)
                total += Vector3.Distance(corners[i], corners[i + 1]);
            if (total < 0.1f)
                throw new UnityEditor.Build.BuildFailedException("V0.31 inherited NavMesh route has negligible length.");

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

        private static void ValidateInheritedGame()
        {
            if (UnityEngine.Object.FindObjectsOfType<PlayerStateMachine>(true).Length != 1)
                throw new UnityEditor.Build.BuildFailedException("V0.31 lost the single player authority.");
            if (UnityEngine.Object.FindObjectsOfType<Sword>(true).Length != 1)
                throw new UnityEditor.Build.BuildFailedException("V0.31 lost the single authoritative sword.");
            if (UnityEngine.Object.FindObjectsOfType<CinemachineVirtualCamera>(true).Length == 0)
                throw new UnityEditor.Build.BuildFailedException("V0.31 lost Cinemachine camera authority.");
            if (UnityEngine.Object.FindObjectsOfType<EnemyStateMachine>(true).Length == 0)
                throw new UnityEditor.Build.BuildFailedException("V0.31 lost the standard enemy population.");
            if (UnityEngine.Object.FindObjectsOfType<Bonfire>(true).Length == 0 ||
                UnityEngine.Object.FindObjectsOfType<BonfiresManager>(true).Length != 1)
                throw new UnityEditor.Build.BuildFailedException("V0.31 lost bonfire/progression authority.");
            if (UnityEngine.Object.FindObjectsOfType<EnemyNightmareDragonController>(true).Length == 0)
                throw new UnityEditor.Build.BuildFailedException("V0.31 lost the inherited boss pipeline.");
        }

        private static void ValidateProductionRoots(GameObject marker)
        {
            if (marker.GetComponentsInChildren<Collider>(true).Length != 0 ||
                marker.GetComponentsInChildren<Rigidbody>(true).Length != 0)
                throw new UnityEditor.Build.BuildFailedException("V0.31 runtime marker must remain presentation-only.");

            GameObject architecture = GameObject.Find(ArchitectureRoot);
            if (architecture == null)
                throw new UnityEditor.Build.BuildFailedException("V0.31 authored architecture root is missing.");
            Collider[] colliders = architecture.GetComponentsInChildren<Collider>(true);
            if (colliders.Length == 0)
                throw new UnityEditor.Build.BuildFailedException("V0.31 authored boundaries contain no collision.");
        }
    }
}
#endif