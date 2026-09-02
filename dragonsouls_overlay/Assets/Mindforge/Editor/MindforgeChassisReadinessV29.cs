#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Cinemachine;
using PlayerController;
using States;
using UnityEditor;
using UnityEngine;

namespace Mindforge.Chassis.Editor
{
    /// <summary>
    /// Native audit for the materialized Dragon Souls project. This intentionally
    /// validates the chassis we are adopting instead of the historical Mindforge
    /// scene. Edit-mode evidence only proves assets exist; runtime ownership is
    /// observed only after the selected upstream scene actually enters Play Mode.
    /// </summary>
    public static class MindforgeChassisReadinessV29
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
            public string schema = "mindforge.dragonsouls_readiness.v29";
            public string unityVersion;
            public string scene;
            public bool playMode;
            public bool passed;
            public readonly List<Check> checks = new List<Check>();
        }

        public static Report AuditActiveScene()
        {
            Report report = new Report
            {
                unityVersion = Application.unityVersion,
                scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,
                playMode = EditorApplication.isPlaying,
            };

            Add(report, "pinned_unity_2021_3_20f1",
                true,
                string.Equals(Application.unityVersion, "2021.3.20f1", StringComparison.Ordinal),
                Application.unityVersion);

            Add(report, "supported_upstream_scene",
                true,
                report.scene == MindforgeChassisMenu.MainGameScene ||
                report.scene == MindforgeChassisMenu.GameplayTestScene,
                report.scene);

            if (!EditorApplication.isPlaying)
            {
                Add(report, "single_player_state_machine", false, false, "requires Play Mode");
                Add(report, "single_authoritative_sword", false, false, "requires Play Mode");
                Add(report, "cinemachine_brain", false, false, "requires Play Mode");
                Add(report, "cinemachine_collision", false, false, "requires Play Mode");
                Add(report, "boss_manager", false, false, "requires Play Mode");
                Add(report, "nightmare_dragon_controller", false, false, "requires Play Mode");
                Add(report, "aetherblade_presentation", false, false, "requires Play Mode");
                report.passed = false;
                Log(report);
                return report;
            }

            PlayerStateMachine[] players = UnityEngine.Object.FindObjectsOfType<PlayerStateMachine>(true);
            Sword[] swords = UnityEngine.Object.FindObjectsOfType<Sword>(true);
            CinemachineBrain[] brains = UnityEngine.Object.FindObjectsOfType<CinemachineBrain>(true);
            CinemachineCollider[] cameraColliders = UnityEngine.Object.FindObjectsOfType<CinemachineCollider>(true);
            BossManager[] bosses = UnityEngine.Object.FindObjectsOfType<BossManager>(true);
            EnemyNightmareDragonController[] dragons = UnityEngine.Object.FindObjectsOfType<EnemyNightmareDragonController>(true);
            MindforgeAetherbladePresentationV29[] blades =
                UnityEngine.Object.FindObjectsOfType<MindforgeAetherbladePresentationV29>(true);

            Add(report, "single_player_state_machine", true, players.Length == 1, $"found={players.Length}");
            Add(report, "single_authoritative_sword", true, swords.Length == 1, $"found={swords.Length}");
            Add(report, "cinemachine_brain", true, brains.Length == 1, $"found={brains.Length}");
            Add(report, "cinemachine_collision", true, cameraColliders.Length >= 1, $"found={cameraColliders.Length}");
            Add(report, "boss_manager", true, bosses.Length == 1, $"found={bosses.Length}");
            Add(report, "nightmare_dragon_controller", true, dragons.Length == 1, $"found={dragons.Length}");
            Add(report, "aetherblade_presentation", true,
                blades.Length == 1 && blades[0].Installed,
                blades.Length == 1 ? $"installed={blades[0].Installed}" : $"found={blades.Length}");

            if (players.Length == 1)
            {
                PlayerStateMachine player = players[0];
                Add(report, "player_character_controller", true,
                    player.movement != null && player.movement.CharacterController != null,
                    player.movement == null ? "movement missing" : "character controller resolved");
                Add(report, "player_combat_controller", true,
                    player.combatController != null,
                    player.combatController == null ? "missing" : "resolved");
                Add(report, "player_stamina", true,
                    player.stamina != null,
                    player.stamina == null ? "missing" : "resolved");
                Add(report, "player_health", true,
                    player.health != null,
                    player.health == null ? "missing" : "resolved");
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

            Log(report);
            return report;
        }

        private static void Add(Report report, string id, bool observed, bool passed, string detail)
        {
            report.checks.Add(new Check
            {
                id = id,
                observed = observed,
                passed = observed && passed,
                detail = detail,
            });
        }

        private static void Log(Report report)
        {
            int pass = 0;
            int fail = 0;
            int deferred = 0;
            for (int i = 0; i < report.checks.Count; i++)
            {
                Check check = report.checks[i];
                if (!check.observed) deferred++;
                else if (check.passed) pass++;
                else fail++;
            }
            string message =
                $"[Mindforge:V29] Chassis readiness {(report.passed ? "PASS" : "INCOMPLETE/FAIL")} " +
                $"({pass} pass, {fail} fail, {deferred} deferred), scene={report.scene}, unity={report.unityVersion}";
            if (fail > 0) Debug.LogError(message);
            else if (report.passed) Debug.Log(message);
            else Debug.LogWarning(message);
        }
    }
}
#endif
