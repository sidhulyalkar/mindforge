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
    /// Full Null Ward enemy silhouette vocabulary. Gameplay colliders and controller
    /// authority remain on the enemy root; every primitive created here is collider-free.
    /// Shape, proportion and negative space carry archetype identity even with emission off.
    /// </summary>
    public static class NullWardEnemySilhouetteV3Builder
    {
        public const string RootName = "ArchetypeSilhouetteV3";
        private const string LegacyRootName = "ArchetypeSilhouetteV2";

        [MenuItem("Mindforge/Legacy/Showcase/Apply Enemy Silhouettes V3", priority = 26)]
        public static void ApplyOpenScene()
        {
            GameObject ward = EditorSceneLookup.FindIncludingInactive(NullWardSceneBuilder.RootName);
            if (ward == null)
                throw new InvalidOperationException("Enemy silhouette V3 requires the Null Ward scene root.");

            Material hostile = CinematicMaterialAuthoring.Load("FracturedCore");
            Material fractured = CinematicMaterialAuthoring.Load("FracturedRing");
            Material metal = CinematicMaterialAuthoring.Load("GuardianMetal");
            Material obsidian = CinematicMaterialAuthoring.Load("ObsidianArchitecture");

            JourneyEnemyController[] enemies = ward.GetComponentsInChildren<JourneyEnemyController>(true);
            int rebuilt = 0;
            for (int i = 0; i < enemies.Length; i++)
            {
                JourneyEnemyController enemy = enemies[i];
                if (enemy == null) continue;
                Transform visuals = enemy.transform.Find("Visuals");
                if (visuals == null) continue;

                DestroyChild(visuals, RootName);
                DestroyChild(visuals, LegacyRootName);

                Transform legacyBody = visuals.Find("Body");
                Renderer legacyRenderer = legacyBody != null ? legacyBody.GetComponent<Renderer>() : null;
                Transform core = visuals.Find("Core");
                Renderer coreRenderer = core != null ? core.GetComponent<Renderer>() : null;

                Material bodyMaterial = legacyRenderer != null && legacyRenderer.sharedMaterial != null
                    ? legacyRenderer.sharedMaterial
                    : obsidian;
                Material signalMaterial = coreRenderer != null && coreRenderer.sharedMaterial != null
                    ? coreRenderer.sharedMaterial
                    : hostile;
                Material accentMaterial = enemy.Archetype == JourneyEnemyArchetype.SignalWarden ||
                                          enemy.Archetype == JourneyEnemyArchetype.Shardcaster
                    ? (fractured != null ? fractured : signalMaterial)
                    : signalMaterial;
                if (bodyMaterial == null || signalMaterial == null) continue;

                if (legacyRenderer != null) legacyRenderer.enabled = false;

                GameObject root = new GameObject(RootName);
                root.transform.SetParent(visuals, false);
                float scale = EstimateScale(core);

                bool needle = enemy.Archetype == JourneyEnemyArchetype.Shardcaster &&
                              enemy.name.IndexOf("AetherNeedle", StringComparison.OrdinalIgnoreCase) >= 0;
                switch (enemy.Archetype)
                {
                    case JourneyEnemyArchetype.Hollow:
                        BuildHollow(root.transform, scale, bodyMaterial, signalMaterial);
                        break;
                    case JourneyEnemyArchetype.Shardcaster:
                        if (needle) BuildAetherNeedle(root.transform, scale, bodyMaterial, signalMaterial, accentMaterial);
                        else BuildShardcaster(root.transform, scale, bodyMaterial, signalMaterial, accentMaterial);
                        break;
                    case JourneyEnemyArchetype.SignalWarden:
                        BuildSignalWarden(root.transform, scale, bodyMaterial, signalMaterial, accentMaterial, metal);
                        break;
                    case JourneyEnemyArchetype.NullSentry:
                        BuildNullSentry(root.transform, scale, bodyMaterial, signalMaterial);
                        break;
                    case JourneyEnemyArchetype.ChromePenitent:
                        BuildChromePenitent(root.transform, scale, bodyMaterial, signalMaterial);
                        break;
                }
                rebuilt++;
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log($"[Mindforge:EnemiesV3] Rebuilt {rebuilt} ordinary enemies with five archetype silhouettes plus the Aether Needle high-lane variant.");
        }

        private static float EstimateScale(Transform core)
        {
            if (core == null) return 1f;
            return Mathf.Clamp(core.localScale.x / 0.30f, 0.50f, 1.80f);
        }

        private static void BuildHollow(Transform parent, float s, Material body, Material signal)
        {
            // Low, forward-leaning knife-hound. Almost no vertical mass so two of them can
            // be read instantly beneath Sentry projectile lanes.
            Part("Hollow_Spine", PrimitiveType.Cube, parent,
                new Vector3(0f, 0.48f, 0.04f) * s,
                new Vector3(0.58f, 0.28f, 0.86f) * s,
                new Vector3(12f, 0f, 0f), body, true);
            Part("Hollow_HeadWedge", PrimitiveType.Cube, parent,
                new Vector3(0f, 0.55f, 0.47f) * s,
                new Vector3(0.42f, 0.22f, 0.42f) * s,
                new Vector3(-18f, 0f, 0f), body, true);
            Part("Hollow_Blade_L", PrimitiveType.Cube, parent,
                new Vector3(-0.34f, 0.28f, 0.26f) * s,
                new Vector3(0.10f, 0.16f, 0.74f) * s,
                new Vector3(0f, -17f, -18f), body, true);
            Part("Hollow_Blade_R", PrimitiveType.Cube, parent,
                new Vector3(0.34f, 0.28f, 0.26f) * s,
                new Vector3(0.10f, 0.16f, 0.74f) * s,
                new Vector3(0f, 17f, 18f), body, true);
            Part("Hollow_RearSpike", PrimitiveType.Cube, parent,
                new Vector3(0f, 0.44f, -0.56f) * s,
                new Vector3(0.10f, 0.12f, 0.62f) * s,
                new Vector3(0f, 0f, 8f), body, true);
            Part("Hollow_Eye", PrimitiveType.Cube, parent,
                new Vector3(0f, 0.58f, 0.695f) * s,
                new Vector3(0.25f, 0.045f, 0.030f) * s,
                Vector3.zero, signal, false);
            Part("Hollow_SpineSignal", PrimitiveType.Cube, parent,
                new Vector3(0f, 0.64f, 0.02f) * s,
                new Vector3(0.055f, 0.035f, 0.50f) * s,
                new Vector3(10f, 0f, 0f), signal, false);
        }

        private static void BuildShardcaster(Transform parent, float s, Material body, Material signal, Material accent)
        {
            // Fragile floating obelisk. The open center and wide orbit blades communicate
            // ranged control rather than armor or melee commitment.
            Part("Shardcaster_Stem", PrimitiveType.Cube, parent,
                new Vector3(0f, 0.72f, 0f) * s,
                new Vector3(0.24f, 1.08f, 0.24f) * s,
                new Vector3(0f, 0f, 45f), body, true);
            Part("Shardcaster_Crown_A", PrimitiveType.Cube, parent,
                new Vector3(0f, 1.22f, 0f) * s,
                new Vector3(0.50f, 0.18f, 0.50f) * s,
                new Vector3(0f, 45f, 45f), body, true);
            Part("Shardcaster_Orbit_L", PrimitiveType.Cube, parent,
                new Vector3(-0.48f, 0.78f, 0f) * s,
                new Vector3(0.10f, 0.66f, 0.22f) * s,
                new Vector3(0f, -18f, 28f), accent, false);
            Part("Shardcaster_Orbit_R", PrimitiveType.Cube, parent,
                new Vector3(0.48f, 0.78f, 0f) * s,
                new Vector3(0.10f, 0.66f, 0.22f) * s,
                new Vector3(0f, 18f, -28f), accent, false);
            Part("Shardcaster_Lens", PrimitiveType.Sphere, parent,
                new Vector3(0f, 0.86f, 0.24f) * s,
                Vector3.one * 0.24f * s,
                Vector3.zero, signal, false);
            Part("Shardcaster_VerticalRune", PrimitiveType.Cube, parent,
                new Vector3(0f, 0.58f, 0.18f) * s,
                new Vector3(0.045f, 0.46f, 0.025f) * s,
                Vector3.zero, signal, false);
        }

        private static void BuildAetherNeedle(Transform parent, float s, Material body, Material signal, Material accent)
        {
            // A high-lane sniper variant of Shardcaster. Narrow enough to read as an
            // aerial needle rather than another person-shaped caster.
            Part("Needle_Main", PrimitiveType.Cube, parent,
                new Vector3(0f, 0.78f, 0f) * s,
                new Vector3(0.20f, 1.48f, 0.20f) * s,
                new Vector3(0f, 0f, 45f), body, true);
            Part("Needle_Fork_L", PrimitiveType.Cube, parent,
                new Vector3(-0.27f, 1.03f, 0f) * s,
                new Vector3(0.08f, 0.72f, 0.12f) * s,
                new Vector3(0f, -10f, 24f), accent, false);
            Part("Needle_Fork_R", PrimitiveType.Cube, parent,
                new Vector3(0.27f, 1.03f, 0f) * s,
                new Vector3(0.08f, 0.72f, 0.12f) * s,
                new Vector3(0f, 10f, -24f), accent, false);
            Part("Needle_Eye", PrimitiveType.Sphere, parent,
                new Vector3(0f, 0.92f, 0.20f) * s,
                Vector3.one * 0.20f * s,
                Vector3.zero, signal, false);
            Part("Needle_TailRune", PrimitiveType.Cube, parent,
                new Vector3(0f, 0.22f, 0f) * s,
                new Vector3(0.035f, 0.46f, 0.035f) * s,
                Vector3.zero, signal, false);
        }

        private static void BuildSignalWarden(
            Transform parent,
            float s,
            Material body,
            Material signal,
            Material accent,
            Material metal)
        {
            Material hard = metal != null ? metal : body;
            // Broad cathedral-guard silhouette with a high crown and two weapon pylons.
            // Large negative gaps around the core keep the central weak visual focus clear.
            Part("Warden_Chest", PrimitiveType.Cube, parent,
                new Vector3(0f, 0.86f, 0f) * s,
                new Vector3(0.90f, 0.94f, 0.56f) * s,
                Vector3.zero, body, true);
            Part("Warden_Pillar_L", PrimitiveType.Cube, parent,
                new Vector3(-0.52f, 0.94f, 0f) * s,
                new Vector3(0.20f, 1.24f, 0.38f) * s,
                new Vector3(0f, 0f, -5f), hard, true);
            Part("Warden_Pillar_R", PrimitiveType.Cube, parent,
                new Vector3(0.52f, 0.94f, 0f) * s,
                new Vector3(0.20f, 1.24f, 0.38f) * s,
                new Vector3(0f, 0f, 5f), hard, true);
            Part("Warden_Crown", PrimitiveType.Cube, parent,
                new Vector3(0f, 1.55f, 0f) * s,
                new Vector3(0.74f, 0.22f, 0.52f) * s,
                new Vector3(0f, 0f, 0f), hard, true);
            Part("Warden_CrownBlade_L", PrimitiveType.Cube, parent,
                new Vector3(-0.28f, 1.82f, -0.02f) * s,
                new Vector3(0.10f, 0.54f, 0.18f) * s,
                new Vector3(0f, 0f, -17f), hard, true);
            Part("Warden_CrownBlade_R", PrimitiveType.Cube, parent,
                new Vector3(0.28f, 1.82f, -0.02f) * s,
                new Vector3(0.10f, 0.54f, 0.18f) * s,
                new Vector3(0f, 0f, 17f), hard, true);
            Part("Warden_CoreFrame", PrimitiveType.Cube, parent,
                new Vector3(0f, 0.93f, 0.31f) * s,
                new Vector3(0.50f, 0.50f, 0.08f) * s,
                new Vector3(0f, 0f, 45f), accent, false);
            Part("Warden_CoreSlit", PrimitiveType.Cube, parent,
                new Vector3(0f, 0.93f, 0.365f) * s,
                new Vector3(0.18f, 0.18f, 0.025f) * s,
                new Vector3(0f, 0f, 45f), signal, false);
        }

        private static void BuildNullSentry(Transform parent, float s, Material body, Material signal)
        {
            Part("Sentry_Keel", PrimitiveType.Capsule, parent,
                new Vector3(0f, 0.67f, 0f) * s,
                new Vector3(0.40f, 0.92f, 0.40f) * s,
                Vector3.zero, body, true);
            Part("Sentry_Crown", PrimitiveType.Cube, parent,
                new Vector3(0f, 1.22f, 0f) * s,
                new Vector3(0.66f, 0.16f, 0.42f) * s,
                Vector3.zero, body, true);
            Part("Sentry_Fin_L", PrimitiveType.Cube, parent,
                new Vector3(-0.38f, 0.84f, -0.02f) * s,
                new Vector3(0.11f, 0.78f, 0.30f) * s,
                new Vector3(0f, -8f, 25f), body, true);
            Part("Sentry_Fin_R", PrimitiveType.Cube, parent,
                new Vector3(0.38f, 0.84f, -0.02f) * s,
                new Vector3(0.11f, 0.78f, 0.30f) * s,
                new Vector3(0f, 8f, -25f), body, true);
            Part("Sentry_TailBlade", PrimitiveType.Cube, parent,
                new Vector3(0f, 0.17f, -0.04f) * s,
                new Vector3(0.10f, 0.56f, 0.24f) * s,
                new Vector3(8f, 0f, 0f), body, true);
            Part("Sentry_Visor", PrimitiveType.Cube, parent,
                new Vector3(0f, 1.20f, 0.235f) * s,
                new Vector3(0.46f, 0.055f, 0.035f) * s,
                Vector3.zero, signal, false);
            Part("Sentry_SpineSignal", PrimitiveType.Cube, parent,
                new Vector3(0f, 0.70f, -0.215f) * s,
                new Vector3(0.055f, 0.58f, 0.035f) * s,
                Vector3.zero, signal, false);
        }

        private static void BuildChromePenitent(Transform parent, float s, Material body, Material signal)
        {
            Part("Penitent_Chest", PrimitiveType.Cube, parent,
                new Vector3(0f, 0.70f, 0f) * s,
                new Vector3(0.92f, 0.86f, 0.56f) * s,
                new Vector3(-4f, 0f, 0f), body, true);
            Part("Penitent_Helm", PrimitiveType.Cube, parent,
                new Vector3(0f, 1.24f, 0.03f) * s,
                new Vector3(0.60f, 0.38f, 0.52f) * s,
                Vector3.zero, body, true);
            Part("Penitent_Pauldron_L", PrimitiveType.Cube, parent,
                new Vector3(-0.57f, 0.91f, 0f) * s,
                new Vector3(0.38f, 0.30f, 0.62f) * s,
                new Vector3(0f, 0f, -8f), body, true);
            Part("Penitent_Pauldron_R", PrimitiveType.Cube, parent,
                new Vector3(0.57f, 0.91f, 0f) * s,
                new Vector3(0.38f, 0.30f, 0.62f) * s,
                new Vector3(0f, 0f, 8f), body, true);
            Part("Penitent_GuardArm", PrimitiveType.Cube, parent,
                new Vector3(-0.56f, 0.46f, 0.16f) * s,
                new Vector3(0.26f, 0.64f, 0.30f) * s,
                new Vector3(-15f, 0f, -8f), body, true);
            Part("Penitent_Cleaver", PrimitiveType.Cube, parent,
                new Vector3(0.68f, 0.50f, 0.18f) * s,
                new Vector3(0.22f, 0.78f, 0.38f) * s,
                new Vector3(-18f, 0f, -12f), body, true);
            Part("Penitent_Backplate", PrimitiveType.Cube, parent,
                new Vector3(0f, 0.70f, -0.34f) * s,
                new Vector3(0.70f, 0.68f, 0.16f) * s,
                Vector3.zero, body, true);
            Part("Penitent_Visor", PrimitiveType.Cube, parent,
                new Vector3(0f, 1.24f, 0.305f) * s,
                new Vector3(0.40f, 0.065f, 0.035f) * s,
                Vector3.zero, signal, false);
            Part("Penitent_ChestSignal", PrimitiveType.Cube, parent,
                new Vector3(0f, 0.72f, 0.305f) * s,
                new Vector3(0.34f, 0.085f, 0.035f) * s,
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

        private static void DestroyChild(Transform parent, string childName)
        {
            Transform child = parent != null ? parent.Find(childName) : null;
            if (child != null) UnityEngine.Object.DestroyImmediate(child.gameObject);
        }
    }
}
#endif
