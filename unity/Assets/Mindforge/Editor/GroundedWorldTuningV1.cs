#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using Mindforge.Combat;
using Mindforge.Presentation;

namespace Mindforge.Editor
{
    /// <summary>
    /// Deterministic authoring pass for the grounded-world feel profile. Serialized scene
    /// values and existing CombatTuning assets can otherwise preserve older prototype
    /// defaults even after code defaults change, so the showcase builder explicitly pins
    /// the intended camera/roll values here.
    /// </summary>
    public static class GroundedWorldTuningV1
    {
        public static void ApplyOpenScene()
        {
            NormalizeGroundUnderlay();
            ConfigureGuardianMotor();

            ShowcaseCameraRig cameraRig = UnityEngine.Object.FindObjectOfType<ShowcaseCameraRig>(true);
            if (cameraRig != null)
            {
                SerializedObject camera = new SerializedObject(cameraRig);
                Set(camera, "pivotHeight", 1.52f);
                Set(camera, "freeDistance", 6.15f);
                Set(camera, "lockDistance", 6.75f);
                Set(camera, "shoulderOffset", 0.22f);
                Set(camera, "freeLookAhead", 4.45f);
                Set(camera, "gameplayFieldOfView", 52f);
                Set(camera, "initialPitch", 26f);
                Set(camera, "minPitch", 12f);
                Set(camera, "maxPitch", 40f);
                Set(camera, "positionSmoothSeconds", 0.055f);
                Set(camera, "verticalFollowSmoothSeconds", 0.11f);
                Set(camera, "collisionRadius", 0.24f);
                camera.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(cameraRig);
            }

            string[] tuningIds = AssetDatabase.FindAssets("t:CombatTuning");
            for (int i = 0; i < tuningIds.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(tuningIds[i]);
                CombatTuning tuning = AssetDatabase.LoadAssetAtPath<CombatTuning>(path);
                if (tuning == null) continue;
                tuning.dashSpeed = 13.6f;
                tuning.dashDuration = 0.28f;
                tuning.dashCooldown = 0.20f;
                tuning.lightHitStop = 0.026f;
                tuning.heavyHitStop = 0.065f;
                tuning.parryHitStop = 0.024f;
                tuning.poiseBreakHitStop = 0.085f;
                EditorUtility.SetDirty(tuning);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[Mindforge:GroundedTuningV1] Applied recessed safety underlay, elevated diorama framing and weighted endurance dodge-roll tuning.");
        }

        private static void ConfigureGuardianMotor()
        {
            GuardianMotor motor = UnityEngine.Object.FindObjectOfType<GuardianMotor>(true);
            if (motor == null) return;
            SerializedObject serialized = new SerializedObject(motor);
            Set(serialized, "dodgeInvulnerabilitySeconds", 0.16f);
            Set(serialized, "dashInputBufferSeconds", 0.12f);
            Set(serialized, "dashExitVelocityRetention", 0.28f);
            // Air movement stays expressive but does not inherit the full ground-roll i-frame.
            Set(serialized, "airDashInvulnerabilitySeconds", 0.075f);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(motor);
        }

        private static void NormalizeGroundUnderlay()
        {
            GameObject root = EditorSceneLookup.FindIncludingInactive(GroundedWorldV1Builder.RootName);
            if (root == null) return;
            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                Transform t = all[i];
                if (t == null || !t.name.StartsWith("GroundPlate_", StringComparison.Ordinal)) continue;
                Vector3 p = t.position;
                p.y = -0.16f;
                t.position = p;
            }
        }

        private static void Set(SerializedObject target, string propertyName, float value)
        {
            SerializedProperty property = target.FindProperty(propertyName);
            if (property == null)
                throw new MissingFieldException(target.targetObject.GetType().FullName, propertyName);
            property.floatValue = value;
        }
    }
}
#endif
