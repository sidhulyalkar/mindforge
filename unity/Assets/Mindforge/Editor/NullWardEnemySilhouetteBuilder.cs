#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Mindforge.Journey;

namespace Mindforge.Editor
{
    /// <summary>
    /// Replaces the shared capsule body of Null Ward ordinary enemies with distinct,
    /// collider-free presentation silhouettes. Enemy controllers, colliders, attack data,
    /// targeting and checkpoint authority remain untouched.
    /// </summary>
    public static class NullWardEnemySilhouetteBuilder
    {
        public const string RootName = "ArchetypeSilhouetteV2";

        public static void ApplyOpenScene()
        {
            GameObject ward = EditorSceneLookup.FindIncludingInactive(NullWardSceneBuilder.RootName);
            if (ward == null)
                throw new InvalidOperationException("Enemy silhouette pass requires the Null Ward scene root.");

            JourneyEnemyController[] enemies = UnityEngine.Object.FindObjectsOfType<JourneyEnemyController>(true);
            int rebuilt = 0;
            for (int i = 0; i < enemies.Length; i++)
            {
                JourneyEnemyController enemy = enemies[i];
                if (enemy == null || !enemy.transform.IsChildOf(ward.transform)) continue;
                if (enemy.Archetype != JourneyEnemyArchetype.NullSentry &&
                    enemy.Archetype != JourneyEnemyArchetype.ChromePenitent)
                    continue;

                Transform visuals = enemy.transform.Find("Visuals");
                if (visuals == null) continue;

                Transform old = visuals.Find(RootName);
                if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);

                Transform legacyBody = visuals.Find("Body");
                Renderer legacyRenderer = legacyBody != null ? legacyBody.GetComponent<Renderer>() : null;
                Transform core = visuals.Find("Core");
                Renderer coreRenderer = core != null ? core.GetComponent<Renderer>() : null;
                Material bodyMaterial = legacyRenderer != null ? legacyRenderer.sharedMaterial : null;
                Material signalMaterial = coreRenderer != null ? coreRenderer.sharedMaterial : bodyMaterial;
                if (bodyMaterial == null) continue;

                // The shared capsule was useful for graybox scale but makes every enemy
                // read as the same pawn. It has no gameplay collider, so the archetype
                // layer may safely replace only its renderer.
                if (legacyRenderer != null) legacyRenderer.enabled = false;

                GameObject root = new GameObject(RootName);
                root.transform.SetParent(visuals, false);
                root.transform.localPosition = Vector3.zero;
                root.transform.localRotation = Quaternion.identity;

                float scale = Mathf.Max(0.55f, enemy.transform.Find("Visuals/Core") != null
                    ? enemy.transform.Find("Visuals/Core").localScale.x / 0.30f
                    : 1f);

                if (enemy.Archetype == JourneyEnemyArchetype.NullSentry)
                    BuildNullSentry(root.transform, scale, bodyMaterial, signalMaterial);
                else
                    BuildChromePenitent(root.transform, scale, bodyMaterial, signalMaterial);

                rebuilt++;
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log($"[Mindforge:Enemies] Rebuilt {rebuilt} Null Ward enemy silhouettes with presentation-only archetype geometry.");
        }

        private static void BuildNullSentry(Transform parent, float scale, Material body, Material signal)
        {
            // Tall, narrow, sensor-like silhouette. The fins and hanging keel create a
            // readable hovering predator shape even if all emission/color is removed.
            Part("NullSentry_Keel", PrimitiveType.Capsule, parent,
                new Vector3(0f, 0.67f, 0f) * scale,
                new Vector3(0.40f, 0.92f, 0.40f) * scale,
                Vector3.zero, body, true);
            Part("NullSentry_Crown", PrimitiveType.Cube, parent,
                new Vector3(0f, 1.22f, 0f) * scale,
                new Vector3(0.66f, 0.16f, 0.42f) * scale,
                Vector3.zero, body, true);
            Part("NullSentry_Fin_L", PrimitiveType.Cube, parent,
                new Vector3(-0.38f, 0.84f, -0.02f) * scale,
                new Vector3(0.11f, 0.78f, 0.30f) * scale,
                new Vector3(0f, -8f, 25f), body, true);
            Part("NullSentry_Fin_R", PrimitiveType.Cube, parent,
                new Vector3(0.38f, 0.84f, -0.02f) * scale,
                new Vector3(0.11f, 0.78f, 0.30f) * scale,
                new Vector3(0f, 8f, -25f), body, true);
            Part("NullSentry_TailBlade", PrimitiveType.Cube, parent,
                new Vector3(0f, 0.17f, -0.04f) * scale,
                new Vector3(0.10f, 0.56f, 0.24f) * scale,
                new Vector3(8f, 0f, 0f), body, true);
            Part("NullSentry_Visor", PrimitiveType.Cube, parent,
                new Vector3(0f, 1.20f, 0.235f) * scale,
                new Vector3(0.46f, 0.055f, 0.035f) * scale,
                Vector3.zero, signal, false);
            Part("NullSentry_SpineSignal", PrimitiveType.Cube, parent,
                new Vector3(0f, 0.70f, -0.215f) * scale,
                new Vector3(0.055f, 0.58f, 0.035f) * scale,
                Vector3.zero, signal, false);
        }

        private static void BuildChromePenitent(Transform parent, float scale, Material body, Material signal)
        {
            // Wide, low-center-of-mass bruiser silhouette. Shoulders, forward armor and a
            // visual cleaver mass communicate close-range threat without changing melee
            // reach, hitboxes or attack authority.
            Part("ChromePenitent_Chest", PrimitiveType.Cube, parent,
                new Vector3(0f, 0.70f, 0f) * scale,
                new Vector3(0.92f, 0.86f, 0.56f) * scale,
                new Vector3(-4f, 0f, 0f), body, true);
            Part("ChromePenitent_Helm", PrimitiveType.Cube, parent,
                new Vector3(0f, 1.24f, 0.03f) * scale,
                new Vector3(0.60f, 0.38f, 0.52f) * scale,
                Vector3.zero, body, true);
            Part("ChromePenitent_Pauldron_L", PrimitiveType.Cube, parent,
                new Vector3(-0.57f, 0.91f, 0f) * scale,
                new Vector3(0.38f, 0.30f, 0.62f) * scale,
                new Vector3(0f, 0f, -8f), body, true);
            Part("ChromePenitent_Pauldron_R", PrimitiveType.Cube, parent,
                new Vector3(0.57f, 0.91f, 0f) * scale,
                new Vector3(0.38f, 0.30f, 0.62f) * scale,
                new Vector3(0f, 0f, 8f), body, true);
            Part("ChromePenitent_GuardArm", PrimitiveType.Cube, parent,
                new Vector3(-0.56f, 0.46f, 0.16f) * scale,
                new Vector3(0.26f, 0.64f, 0.30f) * scale,
                new Vector3(-15f, 0f, -8f), body, true);
            Part("ChromePenitent_CleaverMass", PrimitiveType.Cube, parent,
                new Vector3(0.68f, 0.50f, 0.18f) * scale,
                new Vector3(0.22f, 0.78f, 0.38f) * scale,
                new Vector3(-18f, 0f, -12f), body, true);
            Part("ChromePenitent_Backplate", PrimitiveType.Cube, parent,
                new Vector3(0f, 0.70f, -0.34f) * scale,
                new Vector3(0.70f, 0.68f, 0.16f) * scale,
                Vector3.zero, body, true);
            Part("ChromePenitent_Visor", PrimitiveType.Cube, parent,
                new Vector3(0f, 1.24f, 0.305f) * scale,
                new Vector3(0.40f, 0.065f, 0.035f) * scale,
                Vector3.zero, signal, false);
            Part("ChromePenitent_ChestSignal", PrimitiveType.Cube, parent,
                new Vector3(0f, 0.72f, 0.305f) * scale,
                new Vector3(0.34f, 0.085f, 0.035f) * scale,
                Vector3.zero, signal, false);
        }

        private static GameObject Part(
            string name,
            PrimitiveType type,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            Vector3 localEuler,
            Material material,
            bool castShadows)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.Euler(localEuler);
            go.transform.localScale = localScale;

            Collider collider = go.GetComponent<Collider>();
            if (collider != null) UnityEngine.Object.DestroyImmediate(collider);

            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
                renderer.receiveShadows = castShadows;
            }
            return go;
        }
    }
}
#endif
