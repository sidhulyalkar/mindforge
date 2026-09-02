#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using States;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace Mindforge.Chassis.Editor
{
    public static class MindforgeVerticalSliceReadinessV31
    {
        [Serializable]
        public sealed class Check
        {
            public string id;
            public bool observed;
            public bool passed;
            public string detail;
        }

        [Serializable]
        public sealed class Report
        {
            public string schema = "mindforge.dragonsouls_vertical_slice_readiness.v31";
            public string unityVersion;
            public string scene;
            public bool playMode;
            public bool passed;
            public readonly List<Check> checks = new List<Check>();
        }

        [MenuItem("Mindforge/World V0.31/Audit Vertical Slice", priority = 20)]
        public static void AuditMenu()
        {
            Report report = AuditActiveScene();
            int pass = 0, fail = 0, deferred = 0;
            for (int i = 0; i < report.checks.Count; i++)
            {
                if (!report.checks[i].observed) deferred++;
                else if (report.checks[i].passed) pass++;
                else fail++;
            }
            string message = $"[Mindforge:V31] Vertical slice readiness {(report.passed ? "PASS" : "INCOMPLETE/FAIL")} " +
                $"({pass} pass, {fail} fail, {deferred} deferred), scene={report.scene}.";
            if (fail > 0) Debug.LogError(message); else Debug.Log(message);
        }

        public static Report AuditActiveScene()
        {
            Report report = new Report
            {
                unityVersion = Application.unityVersion,
                scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,
                playMode = EditorApplication.isPlaying,
            };

            Add(report, "pinned_unity_2021_3_20f1", true,
                string.Equals(Application.unityVersion, "2021.3.20f1", StringComparison.Ordinal), Application.unityVersion);
            Add(report, "v31_scene", true,
                report.scene == MindforgeVerticalSliceBuilderV31.DestinationScene, report.scene);

            GameObject marker = GameObject.Find(MindforgeVerticalSliceBuilderV31.MarkerRoot);
            GameObject architecture = GameObject.Find(MindforgeVerticalSliceBuilderV31.ArchitectureRoot);
            Add(report, "v31_runtime_marker", true, marker != null, marker == null ? "missing" : marker.name);
            Add(report, "v31_architecture_root", true, architecture != null, architecture == null ? "missing" : architecture.name);

            if (marker != null)
            {
                Add(report, "runtime_marker_no_colliders", true,
                    marker.GetComponentsInChildren<Collider>(true).Length == 0,
                    $"found={marker.GetComponentsInChildren<Collider>(true).Length}");
                Add(report, "runtime_marker_no_rigidbodies", true,
                    marker.GetComponentsInChildren<Rigidbody>(true).Length == 0,
                    $"found={marker.GetComponentsInChildren<Rigidbody>(true).Length}");
                Add(report, "runtime_owner_authored", true,
                    marker.GetComponent<MindforgeVerticalSliceRuntimeV31>() != null,
                    marker.GetComponent<MindforgeVerticalSliceRuntimeV31>() == null ? "missing" : "resolved");
            }

            if (architecture != null)
            {
                Collider[] colliders = architecture.GetComponentsInChildren<Collider>(true);
                int real = 0;
                for (int i = 0; i < colliders.Length; i++)
                {
                    Collider collider = colliders[i];
                    if (collider == null || !collider.enabled || collider.isTrigger) continue;
                    MeshCollider mesh = collider as MeshCollider;
                    if (mesh == null || mesh.sharedMesh != null) real++;
                }
                Add(report, "authored_boundaries_have_real_collision", true, real >= 6,
                    $"usable={real}, total={colliders.Length}");
                Add(report, "authored_boundary_budget", true,
                    architecture.transform.childCount >= 6 && architecture.transform.childCount <= MindforgeVerticalSliceBuilderV31.MaximumAddedSolidModules,
                    $"children={architecture.transform.childCount}");
            }

            PlayerStateMachine[] players = UnityEngine.Object.FindObjectsOfType<PlayerStateMachine>(true);
            EnemyStateMachine[] enemies = UnityEngine.Object.FindObjectsOfType<EnemyStateMachine>(true);
            Add(report, "single_player", true, players.Length == 1, $"found={players.Length}");
            Add(report, "enemy_population", true, enemies.Length > 0, $"found={enemies.Length}");

            if (EditorApplication.isPlaying)
            {
                NavMeshTriangulation nav = NavMesh.CalculateTriangulation();
                Add(report, "baked_navmesh_runtime", true, nav.vertices != null && nav.vertices.Length > 0,
                    $"vertices={(nav.vertices == null ? 0 : nav.vertices.Length)}");

                MindforgeVerticalSliceRuntimeV31[] runtimes = UnityEngine.Object.FindObjectsOfType<MindforgeVerticalSliceRuntimeV31>(true);
                MindforgeProductionCameraV31[] cameras = UnityEngine.Object.FindObjectsOfType<MindforgeProductionCameraV31>(true);
                MindforgeEnemyFormationV31[] formations = UnityEngine.Object.FindObjectsOfType<MindforgeEnemyFormationV31>(true);
                MindforgeCombatFeedbackV31[] feedback = UnityEngine.Object.FindObjectsOfType<MindforgeCombatFeedbackV31>(true);
                MindforgeHudPresentationV31[] hud = UnityEngine.Object.FindObjectsOfType<MindforgeHudPresentationV31>(true);

                Add(report, "runtime_installed", true, runtimes.Length == 1 && runtimes[0].Installed,
                    $"owners={runtimes.Length}");
                Add(report, "production_camera_runtime", true, cameras.Length == 1 && cameras[0].Installed,
                    $"owners={cameras.Length}");
                Add(report, "enemy_formation_runtime", true, formations.Length > 0,
                    $"owners={formations.Length}");
                Add(report, "combat_feedback_runtime", true, feedback.Length >= 2,
                    $"owners={feedback.Length}");
                Add(report, "hud_presentation_runtime", true, hud.Length == 1 && hud[0].Installed,
                    $"owners={hud.Length}");
            }
            else
            {
                Add(report, "baked_navmesh_runtime", false, false, "requires Play Mode");
                Add(report, "runtime_installed", false, false, "requires Play Mode");
                Add(report, "production_camera_runtime", false, false, "requires Play Mode");
                Add(report, "enemy_formation_runtime", false, false, "requires Play Mode");
                Add(report, "combat_feedback_runtime", false, false, "requires Play Mode");
                Add(report, "hud_presentation_runtime", false, false, "requires Play Mode");
            }

            report.passed = true;
            for (int i = 0; i < report.checks.Count; i++)
            {
                if (report.checks[i].observed && !report.checks[i].passed)
                {
                    report.passed = false;
                    break;
                }
            }
            return report;
        }

        private static void Add(Report report, string id, bool observed, bool passed, string detail)
        {
            report.checks.Add(new Check { id = id, observed = observed, passed = observed && passed, detail = detail });
        }
    }
}
#endif
