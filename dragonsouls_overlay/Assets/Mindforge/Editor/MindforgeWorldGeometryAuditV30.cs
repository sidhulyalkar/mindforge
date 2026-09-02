#if UNITY_EDITOR
using System;
using PlayerController;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace Mindforge.Chassis.Editor
{
    /// <summary>
    /// Read-only geometry audit for the inherited full Dragon Souls world. It samples
    /// the baked NavMesh route from player to dragon and measures actual lateral
    /// collider clearance without moving or mutating any scene object.
    /// </summary>
    public static class MindforgeWorldGeometryAuditV30
    {
        public const float OrdinaryCorridorTarget = 8f;
        public const float BossArenaRadiusTarget = 16f;
        private const float MaxProbeDistance = 20f;
        private const float SampleSpacing = 2f;

        [Serializable]
        public sealed class Report
        {
            public string schema = "mindforge.world_geometry_audit.v30";
            public string scene;
            public bool navMeshObserved;
            public bool pathComplete;
            public int pathCornerCount;
            public int clearanceSamples;
            public int chokeSamples;
            public float minimumPathClearWidth;
            public float minimumBossClearRadius;
            public int largeInvisibleColliderCandidates;
            public bool passed;
        }

        [MenuItem("Mindforge/World V0.30/Audit Traversal Geometry", priority = 21)]
        public static void AuditMenu()
        {
            Report report = AuditActiveScene();
            string message =
                $"[Mindforge:V30] Geometry audit {(report.passed ? "PASS" : "NEEDS REVIEW")}: " +
                $"pathComplete={report.pathComplete}, samples={report.clearanceSamples}, chokes={report.chokeSamples}, " +
                $"minPathWidth={report.minimumPathClearWidth:F2}m, bossRadius={report.minimumBossClearRadius:F2}m, " +
                $"largeInvisibleColliderCandidates={report.largeInvisibleColliderCandidates}.";
            if (report.passed) Debug.Log(message); else Debug.LogWarning(message);
        }

        public static Report AuditActiveScene()
        {
            Report report = new Report
            {
                scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,
                minimumPathClearWidth = MaxProbeDistance * 2f,
                minimumBossClearRadius = MaxProbeDistance,
            };

            PlayerStateMachine player = UnityEngine.Object.FindObjectOfType<PlayerStateMachine>(true);
            EnemyNightmareDragonController dragon = UnityEngine.Object.FindObjectOfType<EnemyNightmareDragonController>(true);
            if (player == null || dragon == null)
            {
                report.passed = false;
                return report;
            }

            NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();
            report.navMeshObserved = triangulation.vertices != null && triangulation.vertices.Length > 0;
            if (!report.navMeshObserved)
            {
                report.passed = false;
                return report;
            }

            NavMeshPath path = new NavMeshPath();
            bool calculated = NavMesh.CalculatePath(player.transform.position, dragon.transform.position, NavMesh.AllAreas, path);
            report.pathComplete = calculated && path.status == NavMeshPathStatus.PathComplete;
            Vector3[] corners = path.corners;
            report.pathCornerCount = corners == null ? 0 : corners.Length;

            if (corners != null && corners.Length >= 2)
                MeasurePathClearance(corners, report);

            report.minimumBossClearRadius = MeasureBossArenaRadius(dragon.transform.position);
            report.largeInvisibleColliderCandidates = CountLargeInvisibleColliderCandidates();
            report.passed = report.pathComplete && report.clearanceSamples > 0 &&
                report.minimumPathClearWidth >= OrdinaryCorridorTarget &&
                report.minimumBossClearRadius >= BossArenaRadiusTarget;
            return report;
        }

        private static void MeasurePathClearance(Vector3[] corners, Report report)
        {
            for (int segment = 0; segment < corners.Length - 1; segment++)
            {
                Vector3 a = corners[segment];
                Vector3 b = corners[segment + 1];
                Vector3 flat = b - a;
                flat.y = 0f;
                float length = flat.magnitude;
                if (length < 0.1f) continue;
                Vector3 forward = flat / length;
                Vector3 lateral = Vector3.Cross(Vector3.up, forward).normalized;
                int steps = Mathf.Max(1, Mathf.CeilToInt(length / SampleSpacing));

                for (int step = 0; step <= steps; step++)
                {
                    float t = Mathf.Clamp01(step / (float)steps);
                    Vector3 sample = Vector3.Lerp(a, b, t) + Vector3.up * 1.0f;
                    float left = DistanceToWorldBoundary(sample, -lateral, MaxProbeDistance);
                    float right = DistanceToWorldBoundary(sample, lateral, MaxProbeDistance);
                    float width = left + right;
                    report.clearanceSamples++;
                    report.minimumPathClearWidth = Mathf.Min(report.minimumPathClearWidth, width);
                    if (width < OrdinaryCorridorTarget) report.chokeSamples++;
                }
            }
        }

        private static float MeasureBossArenaRadius(Vector3 bossPosition)
        {
            float minimum = MaxProbeDistance;
            Vector3 origin = bossPosition + Vector3.up * 1.2f;
            const int rays = 24;
            for (int i = 0; i < rays; i++)
            {
                float angle = (Mathf.PI * 2f * i) / rays;
                Vector3 direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                minimum = Mathf.Min(minimum, DistanceToWorldBoundary(origin, direction, MaxProbeDistance));
            }
            return minimum;
        }

        private static float DistanceToWorldBoundary(Vector3 origin, Vector3 direction, float maxDistance)
        {
            RaycastHit[] hits = Physics.RaycastAll(origin, direction, maxDistance, ~0, QueryTriggerInteraction.Ignore);
            float nearest = maxDistance;
            for (int i = 0; i < hits.Length; i++)
            {
                Collider collider = hits[i].collider;
                if (collider == null || IgnoreActorOrPresentation(collider)) continue;
                if (hits[i].distance < nearest) nearest = hits[i].distance;
            }
            return nearest;
        }

        private static bool IgnoreActorOrPresentation(Collider collider)
        {
            if (collider.GetComponentInParent<PlayerStateMachine>() != null) return true;
            if (collider.GetComponentInParent<EnemyStateMachine>() != null) return true;
            GameObject root = GameObject.Find(MindforgeProductionWorldBuilderV30.MarkerRoot);
            return root != null && collider.transform.IsChildOf(root.transform);
        }

        private static int CountLargeInvisibleColliderCandidates()
        {
            Collider[] colliders = UnityEngine.Object.FindObjectsOfType<Collider>(true);
            int count = 0;
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider collider = colliders[i];
                if (collider == null || collider.isTrigger || IgnoreActorOrPresentation(collider)) continue;
                Bounds bounds = collider.bounds;
                if (bounds.size.magnitude < 5f) continue;
                if (collider.GetComponent<Renderer>() != null) continue;
                if (collider.GetComponentInChildren<Renderer>(true) != null) continue;
                count++;
            }
            return count;
        }
    }
}
#endif
