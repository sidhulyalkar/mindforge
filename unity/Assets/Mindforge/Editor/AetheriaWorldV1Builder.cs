#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Mindforge.Presentation;
using Mindforge.Traversal;

namespace Mindforge.Editor
{
    /// <summary>
    /// Aetheria identity layer over the collision-qualified Grounded World. This pass
    /// adds landmarks, faction signal language, two parked Prism hoverbikes, and passive
    /// narrative/player presentation components. It does not replace the world safety shell.
    /// All geometry created here is decorative and collider-free.
    /// </summary>
    public static class AetheriaWorldV1Builder
    {
        public const string RootName = "Mindforge_AetheriaWorld_V1";

        private static readonly StaticEditorFlags VisualStatic =
            StaticEditorFlags.BatchingStatic |
            StaticEditorFlags.OccludeeStatic |
            StaticEditorFlags.ReflectionProbeStatic;

        [MenuItem("Mindforge/Showcase/Apply Aetheria World V1", priority = 24)]
        public static void ApplyOpenScene()
        {
            GameObject grounded = EditorSceneLookup.FindIncludingInactive(GroundedWorldV1Builder.RootName);
            if (grounded == null)
                throw new InvalidOperationException("Aetheria World V1 requires Grounded World V1 safety topology.");

            GameObject previous = EditorSceneLookup.FindIncludingInactive(RootName);
            if (previous != null) UnityEngine.Object.DestroyImmediate(previous);

            CinematicMaterialAuthoring.EnsureAuthored();
            Material obsidian = RequireMaterial("ObsidianArchitecture");
            Material metal = RequireMaterial("GuardianMetal");
            Material cyan = RequireMaterial("AetherCyan");
            Material green = RequireMaterial("WispVerdant");
            Material violet = RequireMaterial("FracturedRing");
            Material hostile = RequireMaterial("FracturedCore");

            GameObject root = new GameObject(RootName);
            BuildPrismBastion(root.transform, obsidian, metal, cyan, green);
            BuildNeonCauseway(root.transform, obsidian, metal, cyan, green);
            BuildBrokenMomentumMarket(root.transform, obsidian, metal, cyan, violet, hostile);
            BuildRuinedChoir(root.transform, obsidian, metal, cyan, violet);
            BuildMalatractApproach(root.transform, obsidian, metal, violet, hostile);
            BuildPrismHoverbike(root.transform, "PrismHoverbike_Causeway", new Vector3(10.6f, 0.02f, -44.8f), Quaternion.Euler(0f, 8f, 0f), metal, cyan, green);
            BuildPrismHoverbike(root.transform, "PrismHoverbike_Arena", new Vector3(-15.2f, 0.02f, 4.8f), Quaternion.Euler(0f, 160f, 0f), metal, cyan, green);

            if (root.GetComponent<AetheriaNarrativeDirector>() == null)
                root.AddComponent<AetheriaNarrativeDirector>();

            InstallGuardianPresentationAndMountAuthority();

            EditorUtility.SetDirty(root);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                "[Mindforge:AetheriaV1] Added Prism Bastion, Neon Causeway, Broken Momentum Market, Ruined Choir, " +
                "Malatract approach, two optional Prism hoverbikes, block-squire presentation and passive story cards. " +
                "Grounded World V1 remains the collision/safety authority.");
        }

        private static void InstallGuardianPresentationAndMountAuthority()
        {
            GameObject guardian = EditorSceneLookup.FindIncludingInactive("Guardian");
            if (guardian == null)
                throw new InvalidOperationException("Aetheria World V1 requires Guardian.");

            if (guardian.GetComponent<GuardianHoverbikeController>() == null)
                guardian.AddComponent<GuardianHoverbikeController>();
            if (guardian.GetComponent<HoverbikeHud>() == null)
                guardian.AddComponent<HoverbikeHud>();
            if (guardian.GetComponent<PrismSquirePresentationV1>() == null)
                guardian.AddComponent<PrismSquirePresentationV1>();

            EditorUtility.SetDirty(guardian);
        }

        private static void BuildPrismBastion(Transform parent, Material obsidian, Material metal, Material cyan, Material green)
        {
            Transform root = Zone(parent, "Aetheria_PrismBastion");
            Vector3[] towers =
            {
                new Vector3(-16.8f, 4.8f, -57.2f),
                new Vector3(16.8f, 5.4f, -57.2f),
                new Vector3(-20.8f, 6.0f, -52.5f),
                new Vector3(20.8f, 6.6f, -52.5f),
            };

            for (int i = 0; i < towers.Length; i++)
            {
                Vector3 p = towers[i];
                Part($"BastionTower_{i}", PrimitiveType.Cube, root, p, new Vector3(2.0f, 9.6f + i * 0.7f, 2.0f), obsidian, new Vector3(0f, i * 7f, 0f));
                Part($"BastionCrown_{i}", PrimitiveType.Cube, root, p + Vector3.up * (5.0f + i * 0.35f), new Vector3(3.0f, 0.35f, 3.0f), metal, new Vector3(0f, 45f, 0f));
                Part($"BastionSignal_{i}", PrimitiveType.Cube, root, p + new Vector3(i % 2 == 0 ? 1.04f : -1.04f, 0.8f, 0f), new Vector3(0.055f, 5.6f, 0.12f), i % 2 == 0 ? cyan : green, Vector3.zero);
            }

            // Oversized guild crest suspended above spawn. It is intentionally simple and
            // readable from the diorama camera rather than a tiny texture detail.
            Part("PrismGuildCrest_A", PrimitiveType.Cube, root, new Vector3(0f, 7.2f, -60.8f), new Vector3(0.38f, 4.8f, 0.22f), cyan, new Vector3(0f, 0f, 45f));
            Part("PrismGuildCrest_B", PrimitiveType.Cube, root, new Vector3(0f, 7.2f, -60.8f), new Vector3(0.38f, 4.8f, 0.22f), green, new Vector3(0f, 0f, -45f));
        }

        private static void BuildNeonCauseway(Transform parent, Material obsidian, Material metal, Material cyan, Material green)
        {
            Transform root = Zone(parent, "Aetheria_NeonCauseway");

            // Visual guide rails sit outside the collision-backed travel lane. Their repeated
            // notches create speed parallax for the hoverbike without becoming rail-grind gameplay.
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * 7.7f;
                Part($"CausewayHardLightRail_{side}", PrimitiveType.Cube, root, new Vector3(x, 0.42f, -43.2f), new Vector3(0.10f, 0.10f, 20.0f), side < 0 ? cyan : green, Vector3.zero);
                for (int i = 0; i < 8; i++)
                {
                    float z = -51.5f + i * 2.35f;
                    Part($"CausewayRailPylon_{side}_{i}", PrimitiveType.Cube, root, new Vector3(x, 1.15f, z), new Vector3(0.34f, 2.3f, 0.34f), metal, new Vector3(0f, i * 8f, 0f));
                    Part($"CausewayRailPulse_{side}_{i}", PrimitiveType.Cube, root, new Vector3(x - side * 0.20f, 1.45f, z), new Vector3(0.045f, 0.85f, 0.08f), side < 0 ? cyan : green, Vector3.zero);
                }
            }

            Part("CausewayBikeBayCanopy", PrimitiveType.Cube, root, new Vector3(11.0f, 3.15f, -45.0f), new Vector3(5.6f, 0.32f, 5.0f), obsidian, new Vector3(0f, 8f, 0f));
            Part("CausewayBikeBayRune", PrimitiveType.Cube, root, new Vector3(11.0f, 2.93f, -45.0f), new Vector3(3.6f, 0.05f, 3.0f), cyan, new Vector3(0f, 8f, 0f));
        }

        private static void BuildBrokenMomentumMarket(Transform parent, Material obsidian, Material metal, Material cyan, Material violet, Material hostile)
        {
            Transform root = Zone(parent, "Aetheria_BrokenMomentumMarket");

            // Speaker shrine foreshadows Bass-Golem chest architecture.
            Part("MarketSpeakerShrine", PrimitiveType.Cube, root, new Vector3(-12.8f, 3.8f, -28.5f), new Vector3(4.4f, 7.6f, 2.4f), obsidian, new Vector3(0f, -8f, 0f));
            for (int i = 0; i < 3; i++)
            {
                float y = 1.9f + i * 1.75f;
                Part($"MarketSpeakerCore_{i}", PrimitiveType.Cylinder, root, new Vector3(-11.52f, y, -28.5f), new Vector3(1.15f + i * 0.14f, 0.12f, 1.15f + i * 0.14f), i == 2 ? hostile : violet, new Vector3(0f, 0f, 90f));
            }

            for (int i = 0; i < 7; i++)
            {
                float angle = i * 47f * Mathf.Deg2Rad;
                Vector3 p = new Vector3(13.0f + Mathf.Cos(angle) * 4.6f, 0.55f + (i % 3) * 0.28f, -29.0f + Mathf.Sin(angle) * 4.2f);
                Part($"MomentumDriveScrap_{i}", PrimitiveType.Cube, root, p, new Vector3(1.1f, 0.28f, 1.8f), metal, new Vector3(i * 9f, i * 21f, i * 4f));
                Part($"MomentumDriveSignal_{i}", PrimitiveType.Cube, root, p + Vector3.up * 0.22f, new Vector3(0.75f, 0.04f, 1.25f), i % 2 == 0 ? cyan : violet, new Vector3(i * 9f, i * 21f, i * 4f));
            }
        }

        private static void BuildRuinedChoir(Transform parent, Material obsidian, Material metal, Material cyan, Material violet)
        {
            Transform root = Zone(parent, "Aetheria_RuinedChoir");
            for (int i = 0; i < 6; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                float z = -18.5f + (i / 2) * 5.0f;
                float x = side * (17.0f + (i / 2) * 2.2f);
                float height = 8.0f + i * 0.85f;
                Part($"ChoirFork_{i}_Stem", PrimitiveType.Cube, root, new Vector3(x, height * 0.5f, z), new Vector3(0.85f, height, 0.85f), obsidian, Vector3.zero);
                Part($"ChoirFork_{i}_L", PrimitiveType.Cube, root, new Vector3(x - 0.72f, height - 0.2f, z), new Vector3(0.28f, 3.1f, 0.45f), metal, new Vector3(0f, 0f, -8f));
                Part($"ChoirFork_{i}_R", PrimitiveType.Cube, root, new Vector3(x + 0.72f, height - 0.2f, z), new Vector3(0.28f, 3.1f, 0.45f), metal, new Vector3(0f, 0f, 8f));
                Part($"ChoirFork_{i}_Signal", PrimitiveType.Cube, root, new Vector3(x, height * 0.60f, z + 0.46f), new Vector3(0.07f, 3.7f, 0.06f), i % 2 == 0 ? cyan : violet, Vector3.zero);
            }
        }

        private static void BuildMalatractApproach(Transform parent, Material obsidian, Material metal, Material violet, Material hostile)
        {
            Transform root = Zone(parent, "Aetheria_HallOfExcessiveGravitas");
            for (int i = 0; i < 5; i++)
            {
                float z = -2.0f + i * 3.6f;
                for (int side = -1; side <= 1; side += 2)
                {
                    float x = side * 19.5f;
                    Part($"GravitasColumn_{i}_{side}", PrimitiveType.Cube, root, new Vector3(x, 6.0f, z), new Vector3(1.8f, 12f + i * 0.5f, 1.8f), obsidian, Vector3.zero);
                    Part($"GravitasSignal_{i}_{side}", PrimitiveType.Cube, root, new Vector3(x - side * 0.94f, 6.2f, z), new Vector3(0.055f, 5.8f, 0.12f), violet, Vector3.zero);
                }
            }

            // Malatract vista icon: severe, symmetrical and intentionally less colorful.
            Part("MalatractMonolith", PrimitiveType.Cube, root, new Vector3(0f, 8.2f, 18.2f), new Vector3(4.2f, 16.4f, 2.0f), obsidian, Vector3.zero);
            Part("MalatractCrownL", PrimitiveType.Cube, root, new Vector3(-1.7f, 15.2f, 18.2f), new Vector3(0.55f, 5.2f, 0.55f), metal, new Vector3(0f, 0f, -20f));
            Part("MalatractCrownR", PrimitiveType.Cube, root, new Vector3(1.7f, 15.2f, 18.2f), new Vector3(0.55f, 5.2f, 0.55f), metal, new Vector3(0f, 0f, 20f));
            Part("MalatractVisor", PrimitiveType.Cube, root, new Vector3(0f, 10.2f, 17.14f), new Vector3(2.4f, 0.24f, 0.08f), hostile, Vector3.zero);
        }

        private static void BuildPrismHoverbike(
            Transform parent,
            string name,
            Vector3 position,
            Quaternion rotation,
            Material metal,
            Material cyan,
            Material green)
        {
            GameObject bike = new GameObject(name);
            bike.transform.SetParent(parent, false);
            bike.transform.SetPositionAndRotation(position, rotation);
            bike.AddComponent<AetherHoverbikeMount>();

            Transform visual = Zone(bike.transform, "HoverbikePresentation");
            Part("BikeChassis", PrimitiveType.Cube, visual, new Vector3(0f, 0.58f, 0f), new Vector3(0.82f, 0.30f, 2.35f), metal, new Vector3(7f, 0f, 0f));
            Part("BikeNose", PrimitiveType.Cube, visual, new Vector3(0f, 0.70f, 1.65f), new Vector3(0.40f, 0.18f, 1.55f), metal, new Vector3(-7f, 0f, 0f));
            Part("BikeSaddle", PrimitiveType.Cube, visual, new Vector3(0f, 0.88f, -0.22f), new Vector3(0.72f, 0.18f, 0.82f), green, Vector3.zero);
            Part("BikeRailL", PrimitiveType.Cube, visual, new Vector3(-0.68f, 0.34f, 0f), new Vector3(0.13f, 0.10f, 2.80f), cyan, Vector3.zero);
            Part("BikeRailR", PrimitiveType.Cube, visual, new Vector3(0.68f, 0.34f, 0f), new Vector3(0.13f, 0.10f, 2.80f), cyan, Vector3.zero);
            Part("BikeRearDrive", PrimitiveType.Sphere, visual, new Vector3(0f, 0.52f, -1.45f), new Vector3(0.72f, 0.72f, 0.42f), green, Vector3.zero);

            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI * 0.25f;
                Vector3 p = new Vector3(Mathf.Cos(a) * 0.70f, 0.52f + Mathf.Sin(a) * 0.70f, -1.62f);
                Part($"BikeDriveRing_{i}", PrimitiveType.Cube, visual, p, new Vector3(0.16f, 0.16f, 0.10f), i % 2 == 0 ? cyan : green, new Vector3(0f, 0f, -i * 45f));
            }

            for (int side = -1; side <= 1; side += 2)
            {
                Part($"BikeHoverVane_{side}", PrimitiveType.Cube, visual, new Vector3(side * 0.84f, 0.16f, 0.30f), new Vector3(0.36f, 0.08f, 1.42f), metal, new Vector3(0f, side * 5f, side * 8f));
                Part($"BikeGuildPennant_{side}", PrimitiveType.Cube, visual, new Vector3(side * 0.52f, 0.95f, -1.10f), new Vector3(0.08f, 0.42f, 0.32f), side < 0 ? cyan : green, new Vector3(0f, side * 9f, side * 13f));
            }
        }

        private static Transform Zone(Transform parent, string name)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        private static GameObject Part(
            string name,
            PrimitiveType primitive,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            Material material,
            Vector3 localEuler)
        {
            GameObject go = GameObject.CreatePrimitive(primitive);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.Euler(localEuler);
            go.transform.localScale = localScale;
            Collider collider = go.GetComponent<Collider>();
            if (collider != null) UnityEngine.Object.DestroyImmediate(collider);
            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = material;
            GameObjectUtility.SetStaticEditorFlags(go, VisualStatic);
            return go;
        }

        private static Material RequireMaterial(string key)
        {
            Material material = CinematicMaterialAuthoring.Load(key);
            if (material == null)
                throw new InvalidOperationException($"Aetheria World V1 missing cinematic material '{key}'.");
            return material;
        }
    }
}
#endif
