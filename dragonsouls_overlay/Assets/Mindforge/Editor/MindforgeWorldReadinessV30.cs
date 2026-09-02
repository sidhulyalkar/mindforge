#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Cinemachine;
using PlayerController;
using States;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace Mindforge.Chassis.Editor
{
    public static class MindforgeWorldReadinessV30
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
            public string schema = "mindforge.dragonsouls_world_readiness.v30";
            public string unityVersion;
            public string scene;
            public bool playMode;
            public bool passed;
            public readonly List<Check> checks = new List<Check>();
        }

        [MenuItem("Mindforge/World V0.30/Audit Production World", priority = 20)]
        public static void AuditMenu()
        {
            AuditActiveScene();
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
            Add(report, "mindforge_world_scene", true,
                report.scene == MindforgeProductionWorldBuilderV30.DestinationScene, report.scene);

            GameObject root = GameObject.Find(MindforgeProductionWorldBuilderV30.MarkerRoot);
            Add(report, "presentation_root", true, root != null, root == null ? "missing" : root.name);
            if (root != null)
            {
                Add(report, "presentation_root_no_colliders", true,
                    root.GetComponentsInChildren<Collider>(true).Length == 0,
                    $"found={root.GetComponentsInChildren<Collider>(true).Length}");
                Add(report, "presentation_root_no_rigidbodies", true,
                    root.GetComponentsInChildren<Rigidbody>(true).Length == 0,
                    $"found={root.GetComponentsInChildren<Rigidbody>(true).Length}");
                Add(report, "v30_world_presentation", true,
                    root.GetComponent<MindforgeWorldPresentationV30>() != null,
                    root.GetComponent<MindforgeWorldPresentationV30>() == null ? "missing" : "resolved");
                Light[] lights = root.GetComponentsInChildren<Light>(true);
                Add(report, "mindforge_lighting_rig", true, lights.Length >= 4, $"found={lights.Length}");
            }

            PlayerStateMachine[] players = UnityEngine.Object.FindObjectsOfType<PlayerStateMachine>(true);
            Sword[] swords = UnityEngine.Object.FindObjectsOfType<Sword>(true);
            CinemachineVirtualCamera[] cameras = UnityEngine.Object.FindObjectsOfType<CinemachineVirtualCamera>(true);
            EnemyStateMachine[] enemies = UnityEngine.Object.FindObjectsOfType<EnemyStateMachine>(true);
            BossManager[] bosses = UnityEngine.Object.FindObjectsOfType<BossManager>(true);
            EnemyNightmareDragonController[] dragons = UnityEngine.Object.FindObjectsOfType<EnemyNightmareDragonController>(true);

            Add(report, "single_player", true, players.Length == 1, $"found={players.Length}");
            Add(report, "single_authoritative_sword", true, swords.Length == 1, $"found={swords.Length}");
            Add(report, "cinemachine_virtual_cameras", true, cameras.Length >= 1, $"found={cameras.Length}");
            Add(report, "standard_enemy_population", true, enemies.Length >= 1, $"found={enemies.Length}");
            Add(report, "boss_pipeline", true, bosses.Length >= 1 && dragons.Length >= 1,
                $"bosses={bosses.Length}, dragons={dragons.Length}");

            if (EditorApplication.isPlaying)
            {
                NavMeshTriangulation nav = NavMesh.CalculateTriangulation();
                Add(report, "baked_navmesh_runtime", true, nav.vertices != null && nav.vertices.Length > 0,
                    $"vertices={(nav.vertices == null ? 0 : nav.vertices.Length)}");

                MindforgeWorldPresentationV30 presentation = root == null ? null : root.GetComponent<MindforgeWorldPresentationV30>();
                Add(report, "presentation_installed_runtime", true,
                    presentation != null && presentation.Installed,
                    presentation == null ? "missing" :
                    $"installed={presentation.Installed}, environment={presentation.EnvironmentRenderersRethemed}, enemies={presentation.EnemiesRethemed}");

                MindforgeEnemyPresentationV30[] enemyLooks = UnityEngine.Object.FindObjectsOfType<MindforgeEnemyPresentationV30>(true);
                Add(report, "enemy_identity_runtime", true, enemyLooks.Length > 0, $"found={enemyLooks.Length}");
            }
            else
            {
                Add(report, "baked_navmesh_runtime", false, false, "requires Play Mode");
                Add(report, "presentation_installed_runtime", false, false, "requires Play Mode");
                Add(report, "enemy_identity_runtime", false, false, "requires Play Mode");
            }

            report.passed = true;
            for (int i = 0; i < report.checks.Count; i++)
            {
                Check check = report.checks[i];
                if (check.observed && !check.passed)
                {
                    report.passed = false;
                    break;
                }
            }

            int pass = 0, fail = 0, deferred = 0;
            for (int i = 0; i < report.checks.Count; i++)
            {
                Check check = report.checks[i];
                if (!check.observed) deferred++;
                else if (check.passed) pass++;
                else fail++;
            }

            string message = $"[Mindforge:V30] World readiness {(report.passed ? "PASS" : "INCOMPLETE/FAIL")} " +
                $"({pass} pass, {fail} fail, {deferred} deferred), scene={report.scene}, unity={report.unityVersion}";
            if (fail > 0) Debug.LogError(message);
            else if (report.passed) Debug.Log(message);
            else Debug.LogWarning(message);
            return report;
        }

        private static void Add(Report report, string id, bool observed, bool passed, string detail)
        {
            report.checks.Add(new Check { id = id, observed = observed, passed = observed && passed, detail = detail });
        }
    }
}
#endif
