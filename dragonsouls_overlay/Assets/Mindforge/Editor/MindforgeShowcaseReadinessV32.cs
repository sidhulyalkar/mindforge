#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Combat;
using PlayerController;
using States;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace Mindforge.Chassis.Editor
{
    public static class MindforgeShowcaseReadinessV32
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
            public string schema = "mindforge.dragonsouls_showcase_readiness.v32";
            public string unityVersion;
            public string scene;
            public bool playMode;
            public bool passed;
            public readonly List<Check> checks = new List<Check>();
        }

        [MenuItem("Mindforge/World V0.32/Audit Showcase Intro", priority = 20)]
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
            string message = $"[Mindforge:V32] Showcase readiness {(report.passed ? "PASS" : "INCOMPLETE/FAIL")} " +
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
            Add(report, "v32_scene", true,
                report.scene == MindforgeShowcaseIntroBuilderV32.DestinationScene, report.scene);

            GameObject root = GameObject.Find(MindforgeShowcaseIntroBuilderV32.ShowcaseRoot);
            GameObject checkpoints = GameObject.Find(MindforgeShowcaseIntroBuilderV32.CheckpointRoot);
            Add(report, "showcase_root", true, root != null, root == null ? "missing" : root.name);
            Add(report, "showcase_checkpoints", true,
                checkpoints != null && checkpoints.transform.childCount == MindforgeShowcaseIntroBuilderV32.ExpectedCheckpointCount,
                checkpoints == null ? "missing" : $"children={checkpoints.transform.childCount}");

            if (root != null)
            {
                Add(report, "showcase_flow_authored", true,
                    root.GetComponent<MindforgeShowcaseFlowV32>() != null,
                    root.GetComponent<MindforgeShowcaseFlowV32>() == null ? "missing" : "resolved");
                Add(report, "showcase_root_nonphysical", true,
                    root.GetComponentsInChildren<Collider>(true).Length == 0 &&
                    root.GetComponentsInChildren<Rigidbody>(true).Length == 0,
                    $"colliders={root.GetComponentsInChildren<Collider>(true).Length}, rigidbodies={root.GetComponentsInChildren<Rigidbody>(true).Length}");
            }

            if (checkpoints != null)
            {
                MindforgeShowcaseStageCheckpointV32[] observers =
                    checkpoints.GetComponentsInChildren<MindforgeShowcaseStageCheckpointV32>(true);
                Add(report, "checkpoint_observer_count", true,
                    observers.Length == MindforgeShowcaseIntroBuilderV32.ExpectedCheckpointCount,
                    $"found={observers.Length}");
                Add(report, "checkpoints_nonphysical", true,
                    checkpoints.GetComponentsInChildren<Collider>(true).Length == 0 &&
                    checkpoints.GetComponentsInChildren<Rigidbody>(true).Length == 0,
                    $"colliders={checkpoints.GetComponentsInChildren<Collider>(true).Length}, rigidbodies={checkpoints.GetComponentsInChildren<Rigidbody>(true).Length}");
            }

            PlayerStateMachine[] players = UnityEngine.Object.FindObjectsOfType<PlayerStateMachine>(true);
            Sword[] swords = UnityEngine.Object.FindObjectsOfType<Sword>(true);
            CombatController[] combat = UnityEngine.Object.FindObjectsOfType<CombatController>(true);
            EnemyStateMachine[] enemies = UnityEngine.Object.FindObjectsOfType<EnemyStateMachine>(true);
            EnemyNightmareDragonController[] bosses = UnityEngine.Object.FindObjectsOfType<EnemyNightmareDragonController>(true);
            Add(report, "single_player", true, players.Length == 1, $"found={players.Length}");
            Add(report, "single_sword_authority", true, swords.Length == 1, $"found={swords.Length}");
            Add(report, "single_combat_controller", true, combat.Length == 1, $"found={combat.Length}");
            Add(report, "enemy_population", true, enemies.Length > 0, $"found={enemies.Length}");
            Add(report, "boss_pipeline", true, bosses.Length > 0, $"found={bosses.Length}");
            Add(report, "region_grammar_complete", true,
                MindforgeWorldGrammarV32.AllRegions.Count == 6,
                $"regions={MindforgeWorldGrammarV32.AllRegions.Count}");
            Add(report, "encounter_library_complete", true,
                MindforgeEncounterLibraryV32.Recipes.Count >= 2,
                $"recipes={MindforgeEncounterLibraryV32.Recipes.Count}");

            if (EditorApplication.isPlaying)
            {
                NavMeshTriangulation nav = NavMesh.CalculateTriangulation();
                Add(report, "baked_navmesh_runtime", true, nav.vertices != null && nav.vertices.Length > 0,
                    $"vertices={(nav.vertices == null ? 0 : nav.vertices.Length)}");

                MindforgeShowcaseFlowV32[] flows = UnityEngine.Object.FindObjectsOfType<MindforgeShowcaseFlowV32>(true);
                MindforgeSwordCombatAssuranceV31[] assurance = UnityEngine.Object.FindObjectsOfType<MindforgeSwordCombatAssuranceV31>(true);
                MindforgeBciOrbV31[] orbs = UnityEngine.Object.FindObjectsOfType<MindforgeBciOrbV31>(true);
                Add(report, "showcase_flow_runtime", true,
                    flows.Length == 1 && flows[0].Installed,
                    flows.Length == 1 ? $"stage={flows[0].CurrentStage}, milestones={flows[0].Milestones}" : $"owners={flows.Length}");
                Add(report, "sword_assurance_runtime", true,
                    assurance.Length == 1 && assurance[0].Installed && assurance[0].Configured,
                    $"owners={assurance.Length}");
                Add(report, "bci_orb_runtime", true,
                    orbs.Length == 1 && orbs[0].Installed && orbs[0].NodeCount == MindforgeBciOrbV31.StimulusNodeCount,
                    $"owners={orbs.Length}");

                if (flows.Length == 1)
                {
                    MindforgeShowcaseFlowV32 flow = flows[0];
                    bool swingObserved = flow.HasMilestone(MindforgeShowcaseMilestoneV32.FirstSwingWindow);
                    bool hitObserved = flow.HasMilestone(MindforgeShowcaseMilestoneV32.FirstSwordHit);
                    Add(report, "showcase_swing_evidence", swingObserved, swingObserved,
                        swingObserved ? "observed" : "perform at least one authored sword swing");
                    Add(report, "showcase_hit_evidence", hitObserved, hitObserved,
                        hitObserved ? "observed" : "land at least one sword hit");

                    Camera camera = Camera.main;
                    Transform orbVisual = camera == null ? null : camera.transform.Find("Mindforge_BCI_Orb_V31");
                    bool shouldShowOrb = flow.CurrentStage >= MindforgeShowcaseStageV32.BciReveal;
                    bool orbVisibilityCorrect = orbVisual != null && orbVisual.gameObject.activeSelf == shouldShowOrb;
                    Add(report, "bci_reveal_timing", true, orbVisibilityCorrect,
                        $"stage={flow.CurrentStage}, expectedVisible={shouldShowOrb}, actualVisible={(orbVisual != null && orbVisual.gameObject.activeSelf)}");
                }
                Add(report, "bci_physical_display_frequency", false, false,
                    "requested simulation frequencies only; physical optical timing remains unqualified");
            }
            else
            {
                Add(report, "baked_navmesh_runtime", false, false, "requires Play Mode");
                Add(report, "showcase_flow_runtime", false, false, "requires Play Mode");
                Add(report, "sword_assurance_runtime", false, false, "requires Play Mode");
                Add(report, "bci_orb_runtime", false, false, "requires Play Mode");
                Add(report, "showcase_swing_evidence", false, false, "requires Play Mode");
                Add(report, "showcase_hit_evidence", false, false, "requires Play Mode");
                Add(report, "bci_reveal_timing", false, false, "requires Play Mode");
                Add(report, "bci_physical_display_frequency", false, false,
                    "requested simulation frequencies only; physical optical timing remains unqualified");
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
