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
            CombatController[] combatControllers = UnityEngine.Object.FindObjectsOfType<CombatController>(true);
            Sword[] swords = UnityEngine.Object.FindObjectsOfType<Sword>(true);
            Add(report, "single_player", true, players.Length == 1, $"found={players.Length}");
            Add(report, "enemy_population", true, enemies.Length > 0, $"found={enemies.Length}");
            Add(report, "single_sword_authority", true, swords.Length == 1, $"found={swords.Length}");
            Add(report, "single_player_combat_controller", true, combatControllers.Length == 1, $"found={combatControllers.Length}");

            if (swords.Length == 1)
            {
                CapsuleCollider swordCollider = swords[0].GetComponent<CapsuleCollider>();
                Damage swordDamage = swords[0].GetComponent<Damage>();
                TrailRenderer swordTrail = swords[0].GetComponentInChildren<TrailRenderer>(true);
                Add(report, "sword_authority_components", true,
                    swordCollider != null && swordDamage != null && swordTrail != null,
                    $"collider={(swordCollider != null)}, damage={(swordDamage != null)}, trail={(swordTrail != null)}");
            }

            if (combatControllers.Length == 1)
            {
                CombatController combat = combatControllers[0];
                bool lightAuthored = AttackCatalogLooksAuthored(combat.SwordLightAttacks, 3);
                bool heavyAuthored = AttackCatalogLooksAuthored(combat.SwordHeavyAttacks, 1);
                Add(report, "sword_attack_catalog", true, lightAuthored && heavyAuthored,
                    $"light={(combat.SwordLightAttacks == null ? 0 : combat.SwordLightAttacks.Length)}, " +
                    $"heavy={(combat.SwordHeavyAttacks == null ? 0 : combat.SwordHeavyAttacks.Length)}");
            }

            if (EditorApplication.isPlaying)
            {
                NavMeshTriangulation nav = NavMesh.CalculateTriangulation();
                Add(report, "baked_navmesh_runtime", true, nav.vertices != null && nav.vertices.Length > 0,
                    $"vertices={(nav.vertices == null ? 0 : nav.vertices.Length)}");

                MindforgeVerticalSliceRuntimeV31[] runtimes = UnityEngine.Object.FindObjectsOfType<MindforgeVerticalSliceRuntimeV31>(true);
                MindforgeDesktopCombatBindingsV31[] desktopBindings = UnityEngine.Object.FindObjectsOfType<MindforgeDesktopCombatBindingsV31>(true);
                MindforgeProductionCameraV31[] cameras = UnityEngine.Object.FindObjectsOfType<MindforgeProductionCameraV31>(true);
                MindforgeEnemyFormationV31[] formations = UnityEngine.Object.FindObjectsOfType<MindforgeEnemyFormationV31>(true);
                MindforgeEnemyIdentityV31[] identities = UnityEngine.Object.FindObjectsOfType<MindforgeEnemyIdentityV31>(true);
                MindforgeBossEncounterPresentationV31[] bosses = UnityEngine.Object.FindObjectsOfType<MindforgeBossEncounterPresentationV31>(true);
                MindforgeCombatFeedbackV31[] feedback = UnityEngine.Object.FindObjectsOfType<MindforgeCombatFeedbackV31>(true);
                MindforgeHudPresentationV31[] hud = UnityEngine.Object.FindObjectsOfType<MindforgeHudPresentationV31>(true);
                MindforgeSwordCombatAssuranceV31[] swordAssurance = UnityEngine.Object.FindObjectsOfType<MindforgeSwordCombatAssuranceV31>(true);
                MindforgeBciOrbV31[] bciOrbs = UnityEngine.Object.FindObjectsOfType<MindforgeBciOrbV31>(true);

                Add(report, "runtime_installed", true, runtimes.Length == 1 && runtimes[0].Installed,
                    $"owners={runtimes.Length}");
                Add(report, "desktop_combat_bindings_runtime", true,
                    desktopBindings.Length == 1 && desktopBindings[0].Installed && desktopBindings[0].DesktopCombatReady,
                    desktopBindings.Length == 1
                        ? $"owners=1, added={desktopBindings[0].BindingsAdded}, {desktopBindings[0].BindingSummary}"
                        : $"owners={desktopBindings.Length}");
                Add(report, "production_camera_runtime", true, cameras.Length == 1 && cameras[0].Installed,
                    $"owners={cameras.Length}");
                Add(report, "enemy_formation_runtime", true, formations.Length > 0,
                    $"owners={formations.Length}");
                Add(report, "enemy_identity_runtime", true, identities.Length > 0,
                    $"owners={identities.Length}");
                Add(report, "boss_presentation_runtime", true, bosses.Length == 1 && bosses[0].Installed,
                    $"owners={bosses.Length}");
                Add(report, "combat_feedback_runtime", true, feedback.Length >= 2,
                    $"owners={feedback.Length}");
                Add(report, "hud_presentation_runtime", true, hud.Length == 1 && hud[0].Installed,
                    $"owners={hud.Length}");

                bool assuranceReady = swordAssurance.Length == 1 && swordAssurance[0].Installed &&
                    swordAssurance[0].Configured && swordAssurance[0].AetherbladeInstalled;
                Add(report, "sword_combat_assurance_runtime", true, assuranceReady,
                    $"owners={swordAssurance.Length}");
                if (swordAssurance.Length == 1)
                {
                    MindforgeSwordCombatAssuranceV31 assurance = swordAssurance[0];
                    bool sawSwing = assurance.SwingWindowsObserved > 0;
                    Add(report, "sword_swing_window_observed", sawSwing,
                        sawSwing && assurance.PresentedSwingWindowsObserved > 0 &&
                        !assurance.StuckHitboxDetected && !string.IsNullOrEmpty(assurance.LastAttackName),
                        $"windows={assurance.SwingWindowsObserved}, presented={assurance.PresentedSwingWindowsObserved}, " +
                        $"last={assurance.LastAttackName ?? "none"}, stuck={assurance.StuckHitboxDetected}");
                    bool sawHit = assurance.HitsObserved > 0;
                    Add(report, "sword_damage_hit_observed", sawHit, sawHit,
                        $"hits={assurance.HitsObserved}");
                }

                bool orbReady = bciOrbs.Length == 1 && bciOrbs[0].Installed &&
                    bciOrbs[0].NodeCount == MindforgeBciOrbV31.StimulusNodeCount;
                Add(report, "bci_orb_runtime", true, orbReady,
                    $"owners={bciOrbs.Length}, nodes={(bciOrbs.Length == 1 ? bciOrbs[0].NodeCount : 0)}");
                if (bciOrbs.Length == 1)
                {
                    MindforgeBciOrbV31 orb = bciOrbs[0];
                    bool frequencies = Mathf.Approximately(orb.GetRequestedFrequencyHz(MindforgeIntentV29.Sight), 8f) &&
                        Mathf.Approximately(orb.GetRequestedFrequencyHz(MindforgeIntentV29.Guard), 10f) &&
                        Mathf.Approximately(orb.GetRequestedFrequencyHz(MindforgeIntentV29.Concord), 12f);
                    Add(report, "bci_requested_frequency_map", true, frequencies, orb.FrequencyLabel);
                    Add(report, "bci_reduced_contrast_default", true,
                        orb.ReducedContrastDefault && !orb.HighContrastPreviewEnabled,
                        $"simulation={orb.SimulationEnabled}, contrast={orb.CurrentContrast:0.00}, high={orb.HighContrastPreviewEnabled}");
                }
                Add(report, "bci_physical_display_frequency", false, false,
                    "simulation only; requires measured display timing / photodiode qualification");
            }
            else
            {
                Add(report, "baked_navmesh_runtime", false, false, "requires Play Mode");
                Add(report, "runtime_installed", false, false, "requires Play Mode");
                Add(report, "desktop_combat_bindings_runtime", false, false, "requires Play Mode");
                Add(report, "production_camera_runtime", false, false, "requires Play Mode");
                Add(report, "enemy_formation_runtime", false, false, "requires Play Mode");
                Add(report, "enemy_identity_runtime", false, false, "requires Play Mode");
                Add(report, "boss_presentation_runtime", false, false, "requires Play Mode");
                Add(report, "combat_feedback_runtime", false, false, "requires Play Mode");
                Add(report, "hud_presentation_runtime", false, false, "requires Play Mode");
                Add(report, "sword_combat_assurance_runtime", false, false, "requires Play Mode");
                Add(report, "sword_swing_window_observed", false, false, "swing sword in Play Mode, then audit");
                Add(report, "sword_damage_hit_observed", false, false, "land a sword hit in Play Mode, then audit");
                Add(report, "bci_orb_runtime", false, false, "requires Play Mode");
                Add(report, "bci_requested_frequency_map", false, false, "requires Play Mode");
                Add(report, "bci_reduced_contrast_default", false, false, "requires Play Mode");
                Add(report, "bci_physical_display_frequency", false, false,
                    "simulation only; requires measured display timing / photodiode qualification");
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

        private static bool AttackCatalogLooksAuthored(Attack[] attacks, int minimumCount)
        {
            if (attacks == null || attacks.Length < minimumCount) return false;
            for (int i = 0; i < attacks.Length; i++)
            {
                Attack attack = attacks[i];
                if (string.IsNullOrEmpty(attack.animationName)) return false;
                if (attack.attackDuration <= 0f || attack.damage <= 0) return false;
            }
            return true;
        }

        private static void Add(Report report, string id, bool observed, bool passed, string detail)
        {
            report.checks.Add(new Check { id = id, observed = observed, passed = observed && passed, detail = detail });
        }
    }
}
#endif
