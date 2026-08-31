#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Mindforge.Combat;
using Mindforge.Presentation;
using Mindforge.World;

namespace Mindforge.Editor
{
    /// <summary>
    /// Diagnostic architecture audit for the clean V0.11 demo. This is intentionally not
    /// promotion evidence: it checks scene wiring and runtime ownership, not playability,
    /// game feel, visual composition under motion or BCI timing.
    /// </summary>
    public static class MindforgeDemoV11Audit
    {
        private const string ReportRelativePath = "experiments/reports/v11-demo-audit-latest.json";

        [Serializable]
        private sealed class AuditCheck
        {
            public string id;
            public bool passed;
            public string detail;
        }

        [Serializable]
        private sealed class AuditReport
        {
            public string schema = "mindforge.demo_v11_audit.v1";
            public string generated_utc;
            public string unity_version;
            public string scene_path;
            public bool play_mode;
            public bool canonical_promotion_evidence = false;
            public bool all_passed;
            public List<AuditCheck> checks = new List<AuditCheck>();
        }

        [MenuItem("Mindforge/V0.11 Demo/Audit Active Demo Architecture", priority = 50)]
        public static void AuditActiveDemo()
        {
            AuditReport report = new AuditReport
            {
                generated_utc = DateTime.UtcNow.ToString("O"),
                unity_version = Application.unityVersion,
                scene_path = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,
                play_mode = EditorApplication.isPlaying,
            };

            CheckCount<MindforgeDemoV11Marker>(report, "single_demo_marker", 1);
            CheckNamedObject(report, "single_world_root", MindforgeDemoV11Builder.RootName, 1);
            CheckCount<GuardianMotor>(report, "single_guardian_motor", 1);
            CheckCount<FracturedSignalDirector>(report, "single_fractured_signal_director", 1);
            CheckSceneCameras(report);
            CheckTraversalOwners(report);
            CheckEchoWiring(report);
            CheckRuntimeOwners(report);
            CheckWorldSafety(report);
            CheckLegacyPresentation(report);

            report.all_passed = true;
            for (int i = 0; i < report.checks.Count; i++)
                report.all_passed &= report.checks[i].passed;

            string repoRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
            string reportPath = Path.Combine(repoRoot, ReportRelativePath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
            File.WriteAllText(reportPath, JsonUtility.ToJson(report, true));

            string summary = $"[Mindforge:V11Audit] {(report.all_passed ? "PASS" : "FAIL")} " +
                             $"({report.checks.Count} checks) -> {ReportRelativePath}. " +
                             "Diagnostic only; canonical_promotion_evidence=false.";
            if (report.all_passed) Debug.Log(summary);
            else Debug.LogError(summary);
        }

        private static void CheckTraversalOwners(AuditReport report)
        {
            string[] names =
            {
                "SanctumFloor",
                "CausewayRoad",
                "MarketFloor",
                "AscentRamp",
                "FractureFloor",
            };

            for (int i = 0; i < names.Length; i++)
            {
                GameObject go = FindSceneObject(names[i]);
                Renderer renderer = go != null ? go.GetComponent<Renderer>() : null;
                Collider collider = go != null ? go.GetComponent<Collider>() : null;
                bool passed = go != null && renderer != null && renderer.enabled && collider != null && collider.enabled;
                Add(report, "visible_collision_owner_" + names[i], passed,
                    passed
                        ? "visible renderer and enabled collider share the same traversal object"
                        : "missing object, visible renderer or enabled collider");
            }
        }

        private static void CheckEchoWiring(AuditReport report)
        {
            FracturedEchoNode[] all = Resources.FindObjectsOfTypeAll<FracturedEchoNode>();
            int count = 0;
            int invalid = 0;
            for (int i = 0; i < all.Length; i++)
            {
                FracturedEchoNode echo = all[i];
                if (echo == null || !echo.gameObject.scene.IsValid() || !echo.name.StartsWith("V11Echo_", StringComparison.Ordinal)) continue;
                count++;
                if (echo.Vitals == null) invalid++;
            }
            Add(report, "three_route_echoes", count == 3 && invalid == 0,
                $"found={count}, missing_vitals={invalid}");
        }

        private static void CheckRuntimeOwners(AuditReport report)
        {
            if (!EditorApplication.isPlaying)
            {
                AddDeferred(report, "runtime_experience_director");
                AddDeferred(report, "runtime_presentation_firewall");
                AddDeferred(report, "runtime_encounter_gate");
                AddDeferred(report, "single_v11_hud");
                return;
            }

            CheckCount<MindforgeDemoV11ExperienceDirector>(report, "runtime_experience_director", 1);
            CheckCount<MindforgeDemoV11PresentationFirewall>(report, "runtime_presentation_firewall", 1);
            CheckCount<MindforgeDemoV11EncounterGate>(report, "runtime_encounter_gate", 1);
            CheckBehaviourTypeCount(report, "single_v11_hud", "MindforgeDemoHudV11", 1, enabledOnly: true);
        }

        private static void CheckWorldSafety(AuditReport report)
        {
            if (!EditorApplication.isPlaying)
            {
                AddDeferred(report, "v11_world_safety_envelope");
                return;
            }

            GuardianWorldSafety safety = UnityEngine.Object.FindObjectOfType<GuardianWorldSafety>(true);
            bool passed = safety != null &&
                          safety.XBounds.x <= -14.5f && safety.XBounds.y >= 14.5f &&
                          safety.ZBounds.x <= -25.5f && safety.ZBounds.y >= 108.5f;
            Add(report, "v11_world_safety_envelope", passed,
                safety == null
                    ? "GuardianWorldSafety missing"
                    : $"x=[{safety.XBounds.x:0.0},{safety.XBounds.y:0.0}] z=[{safety.ZBounds.x:0.0},{safety.ZBounds.y:0.0}]");
        }

        private static void CheckLegacyPresentation(AuditReport report)
        {
            if (!EditorApplication.isPlaying)
            {
                AddDeferred(report, "legacy_presentation_suppressed");
                return;
            }

            string[] forbiddenEnabledTypes =
            {
                "GroundedCombatHud",
                "PlayerAgencyGuide",
                "GuardianEquipmentMenu",
                "NullWardArtOverrideInstaller",
                "ShowcaseRuntimeInstaller",
                "ProductionHudV09",
                "GuardianAvatarPresentation",
                "GuardianMotionPolish",
                "GuardianPresentationHierarchyBinder",
                "GuardianLocomotionVfx",
                "GuardianAnimatorBridge",
                "CinematicArmamentVfxPolish",
                "AetherbladeVisualPolishV2",
            };

            int enabled = 0;
            List<string> offenders = new List<string>();
            MonoBehaviour[] behaviours = Resources.FindObjectsOfTypeAll<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour == null || !behaviour.gameObject.scene.IsValid() || !behaviour.enabled) continue;
                string typeName = behaviour.GetType().Name;
                for (int j = 0; j < forbiddenEnabledTypes.Length; j++)
                {
                    if (!string.Equals(typeName, forbiddenEnabledTypes[j], StringComparison.Ordinal)) continue;
                    enabled++;
                    offenders.Add(typeName + "@" + behaviour.gameObject.name);
                    break;
                }
            }

            Add(report, "legacy_presentation_suppressed", enabled == 0,
                enabled == 0 ? "no forbidden legacy presentation behaviours enabled" : string.Join(", ", offenders));
        }

        private static void CheckSceneCameras(AuditReport report)
        {
            Camera[] cameras = Resources.FindObjectsOfTypeAll<Camera>();
            int sceneCameras = 0;
            int mainTagged = 0;
            for (int i = 0; i < cameras.Length; i++)
            {
                Camera camera = cameras[i];
                if (camera == null || !camera.gameObject.scene.IsValid()) continue;
                sceneCameras++;
                if (camera.CompareTag("MainCamera")) mainTagged++;
            }
            Add(report, "single_camera_owner", sceneCameras == 1 && mainTagged == 1,
                $"scene_cameras={sceneCameras}, main_tagged={mainTagged}");
        }

        private static void CheckCount<T>(AuditReport report, string id, int expected) where T : UnityEngine.Object
        {
            T[] all = Resources.FindObjectsOfTypeAll<T>();
            int count = 0;
            for (int i = 0; i < all.Length; i++)
            {
                Component component = all[i] as Component;
                if (component != null && component.gameObject.scene.IsValid()) count++;
                else if (all[i] is GameObject go && go.scene.IsValid()) count++;
            }
            Add(report, id, count == expected, $"found={count}, expected={expected}");
        }

        private static void CheckNamedObject(AuditReport report, string id, string name, int expected)
        {
            Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();
            int count = 0;
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform t = transforms[i];
                if (t != null && t.gameObject.scene.IsValid() && string.Equals(t.name, name, StringComparison.Ordinal)) count++;
            }
            Add(report, id, count == expected, $"found={count}, expected={expected}, name={name}");
        }

        private static void CheckBehaviourTypeCount(
            AuditReport report,
            string id,
            string typeName,
            int expected,
            bool enabledOnly)
        {
            MonoBehaviour[] all = Resources.FindObjectsOfTypeAll<MonoBehaviour>();
            int count = 0;
            for (int i = 0; i < all.Length; i++)
            {
                MonoBehaviour behaviour = all[i];
                if (behaviour == null || !behaviour.gameObject.scene.IsValid()) continue;
                if (enabledOnly && !behaviour.enabled) continue;
                if (string.Equals(behaviour.GetType().Name, typeName, StringComparison.Ordinal)) count++;
            }
            Add(report, id, count == expected, $"found={count}, expected={expected}, type={typeName}");
        }

        private static GameObject FindSceneObject(string name)
        {
            Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform t = transforms[i];
                if (t != null && t.gameObject.scene.IsValid() && string.Equals(t.name, name, StringComparison.Ordinal))
                    return t.gameObject;
            }
            return null;
        }

        private static void Add(AuditReport report, string id, bool passed, string detail)
        {
            report.checks.Add(new AuditCheck { id = id, passed = passed, detail = detail });
        }

        private static void AddDeferred(AuditReport report, string id)
        {
            Add(report, id, true, "deferred until Play Mode; diagnostic edit-mode audit remains non-canonical");
        }
    }
}
#endif
