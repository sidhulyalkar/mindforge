#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mindforge.Editor
{
    /// <summary>
    /// Authored composition layer on top of Grounded World V1. V1 owns the continuous
    /// bedrock/perimeter safety shell and reusable traversal grammar; V2 gives each district
    /// a distinct silhouette and interlocking vertical route so the world reads as a place
    /// rather than six copies of the same terrace stack.
    ///
    /// Every reachable surface authored here has ordinary 3D collision. Decorative signal
    /// geometry is collider-free and never owns gameplay, combat or neural authority.
    /// </summary>
    public static class GroundedWorldCompositionV2Builder
    {
        public const string RootName = "Mindforge_GroundedWorld_Composition_V2";

        private static readonly StaticEditorFlags WorldStatic =
            StaticEditorFlags.BatchingStatic |
            StaticEditorFlags.OccluderStatic |
            StaticEditorFlags.OccludeeStatic |
            StaticEditorFlags.ReflectionProbeStatic;

        private static readonly StaticEditorFlags VisualStatic =
            StaticEditorFlags.BatchingStatic |
            StaticEditorFlags.OccludeeStatic |
            StaticEditorFlags.ReflectionProbeStatic;

        [MenuItem("Mindforge/Legacy/Showcase/Apply Grounded World Composition V2", priority = 25)]
        public static void ApplyOpenScene()
        {
            GameObject grounded = EditorSceneLookup.FindIncludingInactive(GroundedWorldV1Builder.RootName);
            if (grounded == null)
                throw new InvalidOperationException("Grounded World Composition V2 requires Grounded World V1.");

            GameObject previous = EditorSceneLookup.FindIncludingInactive(RootName);
            if (previous != null) UnityEngine.Object.DestroyImmediate(previous);

            CinematicMaterialAuthoring.EnsureAuthored();
            Material basalt = RequireMaterial("ArenaBasalt");
            Material obsidian = RequireMaterial("ObsidianArchitecture");
            Material metal = RequireMaterial("GuardianMetal");
            Material cyan = RequireMaterial("AetherCyan");
            Material green = RequireMaterial("WispVerdant");
            Material violet = RequireMaterial("FracturedRing");

            GameObject root = new GameObject(RootName);
            BuildMemoryForgeKeep(root.transform, basalt, obsidian, metal, cyan);
            BuildCausewayRibGallery(root.transform, obsidian, metal, green);
            BuildNullMarketCourt(root.transform, basalt, obsidian, metal, violet);
            BuildFractureTower(root.transform, obsidian, metal, cyan, violet);
            BuildCathedralAscent(root.transform, basalt, obsidian, metal, green);
            BuildArenaRing(root.transform, obsidian, metal, cyan, violet);
            BuildPerimeterMegastruts(root.transform, obsidian, metal, cyan, violet);

            EditorUtility.SetDirty(root);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[Mindforge:GroundedWorldCompositionV2] Added district-specific keeps, rib galleries, courts, fracture tower, " +
                "cathedral ascent, arena ring, landing pockets and shortcut drops. Grounded World V1 remains the safety shell.");
        }

        private static void BuildMemoryForgeKeep(Transform parent, Material basalt, Material obsidian, Material metal, Material cyan)
        {
            Transform root = Zone(parent, "District_MemoryForgeKeep");

            // Broad low keep on the far-west side. Its roofs form a switchback route from
            // the V1 Forge terraces without walling off the original ground route.
            Block("ForgeKeep_Base", root, new Vector3(-29.1f, 0.45f, -56.2f), new Vector3(8.6f, 0.90f, 9.0f), basalt, true);
            Block("ForgeKeep_Mid", root, new Vector3(-30.0f, 1.55f, -57.0f), new Vector3(6.6f, 1.25f, 6.9f), obsidian, true);
            Block("ForgeKeep_High", root, new Vector3(-30.7f, 2.85f, -56.0f), new Vector3(4.8f, 1.30f, 5.1f), basalt, true);

            CreateStairRun(root, "ForgeKeep_LowerStairs", new Vector3(-24.6f, 0.30f, -58.8f), new Vector3(-27.1f, 1.02f, -58.8f), 8, 2.6f, basalt, cyan);
            CreateStairRun(root, "ForgeKeep_UpperStairs", new Vector3(-27.2f, 1.05f, -53.6f), new Vector3(-30.0f, 2.20f, -53.6f), 10, 2.35f, obsidian, cyan);
            CreateLandingPocket(root, "ForgeKeep_RoofPocket", new Vector3(-29.3f, 3.65f, -56.0f), new Vector3(5.7f, 0.26f, 3.8f), metal, cyan);

            // Furnace towers are mostly visual mass. Their bases still block honestly.
            for (int i = 0; i < 3; i++)
            {
                float z = -59.3f + i * 3.25f;
                Block($"ForgeStack_{i}_Base", root, new Vector3(-33.4f, 1.25f, z), new Vector3(1.7f, 2.5f, 1.7f), obsidian, true);
                Block($"ForgeStack_{i}_Crown", root, new Vector3(-33.4f, 4.55f + i * 0.45f, z), new Vector3(1.15f, 4.1f + i * 0.9f, 1.15f), metal, false);
                SignalStrip($"ForgeStack_{i}_Signal", root, new Vector3(-32.78f, 4.4f + i * 0.45f, z), new Vector3(0.05f, 2.5f, 0.10f), cyan);
            }
        }

        private static void BuildCausewayRibGallery(Transform parent, Material obsidian, Material metal, Material green)
        {
            Transform root = Zone(parent, "District_CausewayRibGallery");

            // Long elevated gallery on the east side. Repeated ribs create strong parallax,
            // while alternating balcony pockets make double-jump traversal readable.
            Block("CausewayGallery_Deck", root, new Vector3(28.0f, 2.05f, -43.0f), new Vector3(11.8f, 0.34f, 4.2f), obsidian, true);
            CreateRamp(root, "CausewayGallery_Ramp", new Vector3(22.5f, 0.22f, -46.0f), new Vector3(25.1f, 2.18f, -46.0f), 2.8f, obsidian, metal);
            CreateLandingPocket(root, "CausewayGallery_LandingA", new Vector3(25.4f, 2.20f, -40.6f), new Vector3(3.4f, 0.26f, 3.1f), metal, green);
            CreateLandingPocket(root, "CausewayGallery_LandingB", new Vector3(30.5f, 3.35f, -45.3f), new Vector3(3.6f, 0.26f, 3.3f), metal, green);
            CreateStairRun(root, "CausewayGallery_HighSteps", new Vector3(28.2f, 2.24f, -44.4f), new Vector3(30.4f, 3.45f, -45.0f), 9, 2.15f, obsidian, green);

            for (int i = 0; i < 6; i++)
            {
                float x = 23.7f + i * 1.75f;
                float ribHeight = 5.0f + (i % 2) * 0.9f;
                Block($"CausewayRib_{i}_L", root, new Vector3(x, ribHeight * 0.5f, -45.0f), new Vector3(0.38f, ribHeight, 0.62f), metal, true);
                Block($"CausewayRib_{i}_R", root, new Vector3(x, ribHeight * 0.5f, -41.0f), new Vector3(0.38f, ribHeight, 0.62f), metal, true);
                Block($"CausewayRib_{i}_Lintel", root, new Vector3(x, ribHeight - 0.20f, -43.0f), new Vector3(0.42f, 0.42f, 4.6f), obsidian, false);
                SignalStrip($"CausewayRib_{i}_Signal", root, new Vector3(x + 0.22f, 3.35f, -40.66f), new Vector3(0.045f, 1.9f, 0.045f), green);
            }
        }

        private static void BuildNullMarketCourt(Transform parent, Material basalt, Material obsidian, Material metal, Material violet)
        {
            Transform root = Zone(parent, "District_NullMarketCourt");

            // The Market gets a low stepped court rather than another linear terrace.
            Block("MarketCourt_Lower", root, new Vector3(25.8f, 0.34f, -29.0f), new Vector3(10.8f, 0.68f, 9.2f), basalt, true);
            Block("MarketCourt_Upper", root, new Vector3(26.6f, 1.15f, -28.4f), new Vector3(7.6f, 0.92f, 6.4f), obsidian, true);
            Block("MarketCourt_Dais", root, new Vector3(27.0f, 2.02f, -28.0f), new Vector3(4.2f, 0.84f, 3.8f), metal, true);

            CreateStairRun(root, "MarketCourt_WestSteps", new Vector3(20.2f, 0.18f, -29.0f), new Vector3(23.0f, 0.72f, -29.0f), 7, 3.0f, basalt, violet);
            CreateStairRun(root, "MarketCourt_DaisSteps", new Vector3(24.0f, 0.75f, -25.4f), new Vector3(26.0f, 2.18f, -25.4f), 10, 2.25f, obsidian, violet);
            CreateLandingPocket(root, "MarketCourt_AerialPocket", new Vector3(31.0f, 3.25f, -30.2f), new Vector3(3.6f, 0.26f, 3.6f), metal, violet);

            // Four crooked archive pylons frame the court but keep its center readable.
            Vector3[] pylons =
            {
                new Vector3(22.0f, 2.5f, -32.4f),
                new Vector3(30.7f, 2.9f, -32.1f),
                new Vector3(22.4f, 3.0f, -24.9f),
                new Vector3(31.0f, 2.6f, -25.0f),
            };
            for (int i = 0; i < pylons.Length; i++)
            {
                Vector3 p = pylons[i];
                Block($"MarketArchivePylon_{i}", root, p, new Vector3(1.05f, 5.0f + (i % 2) * 0.8f, 1.05f), obsidian, true,
                    new Vector3((i - 1) * 2.0f, i * 11f, (i % 2 == 0 ? -1f : 1f) * 3f));
                SignalStrip($"MarketArchivePylon_{i}_Signal", root, p + new Vector3(0.56f, 0.4f, 0f), new Vector3(0.045f, 2.0f, 0.045f), violet);
            }
        }

        private static void BuildFractureTower(Transform parent, Material obsidian, Material metal, Material cyan, Material violet)
        {
            Transform root = Zone(parent, "District_FractureTower");
            Vector3 center = new Vector3(-28.2f, 0f, -18.0f);

            // Vertical landmark deliberately uses offset landing pockets rather than one
            // staircase. A full conventional stair switchback exists, while direct double
            // jumps/air dashes let skilled players cut across its face.
            Block("FractureTower_Core", root, center + Vector3.up * 3.0f, new Vector3(4.1f, 6.0f, 4.1f), obsidian, true);
            for (int level = 0; level < 4; level++)
            {
                float y = 1.10f + level * 1.18f;
                float sx = level % 2 == 0 ? 1f : -1f;
                float sz = level < 2 ? 1f : -1f;
                Vector3 pocket = center + new Vector3(sx * 3.15f, y, sz * 1.75f);
                CreateLandingPocket(root, $"FractureTower_Pocket_{level}", pocket, new Vector3(3.6f, 0.28f, 3.0f), metal, level % 2 == 0 ? cyan : violet);
                if (level > 0)
                {
                    float prevY = 1.10f + (level - 1) * 1.18f;
                    float prevSx = (level - 1) % 2 == 0 ? 1f : -1f;
                    float prevSz = (level - 1) < 2 ? 1f : -1f;
                    Vector3 previous = center + new Vector3(prevSx * 3.15f, prevY + 0.12f, prevSz * 1.75f);
                    CreateStairRun(root, $"FractureTower_Switchback_{level}", previous, pocket + Vector3.up * 0.12f, 10, 1.95f, obsidian, level % 2 == 0 ? cyan : violet);
                }
            }

            Block("FractureTower_Crown", root, center + Vector3.up * 6.35f, new Vector3(5.3f, 0.48f, 5.3f), metal, true, new Vector3(0f, 45f, 0f));
            SignalStrip("FractureTower_CrownSignalA", root, center + new Vector3(0f, 6.68f, 0f), new Vector3(6.0f, 0.05f, 0.08f), cyan, new Vector3(0f, 45f, 0f));
            SignalStrip("FractureTower_CrownSignalB", root, center + new Vector3(0f, 6.70f, 0f), new Vector3(0.08f, 0.05f, 6.0f), violet, new Vector3(0f, 45f, 0f));
        }

        private static void BuildCathedralAscent(Transform parent, Material basalt, Material obsidian, Material metal, Material green)
        {
            Transform root = Zone(parent, "District_CathedralAscent");

            // A broad ceremonial ascent creates a single strong perspective line toward
            // the boss district. Side ledges remain reachable for aerial flanking.
            CreateStairRun(root, "CathedralGrandStair", new Vector3(19.5f, 0.18f, -10.4f), new Vector3(28.6f, 3.65f, -10.4f), 18, 4.8f, basalt, green);
            Block("CathedralUpperTerrace", root, new Vector3(30.4f, 3.55f, -10.4f), new Vector3(5.6f, 0.42f, 7.4f), obsidian, true);
            CreateLandingPocket(root, "CathedralSidePocket_N", new Vector3(27.5f, 2.45f, -5.8f), new Vector3(3.2f, 0.26f, 3.4f), metal, green);
            CreateLandingPocket(root, "CathedralSidePocket_S", new Vector3(27.5f, 2.45f, -15.0f), new Vector3(3.2f, 0.26f, 3.4f), metal, green);

            for (int side = -1; side <= 1; side += 2)
            {
                float z = -10.4f + side * 4.15f;
                Block($"CathedralNeedle_{side}_A", root, new Vector3(31.8f, 4.5f, z), new Vector3(1.15f, 9.0f, 1.15f), metal, true);
                Block($"CathedralNeedle_{side}_B", root, new Vector3(29.8f, 3.4f, z), new Vector3(0.85f, 6.8f, 0.85f), obsidian, true);
                SignalStrip($"CathedralNeedle_{side}_Signal", root, new Vector3(31.18f, 4.6f, z), new Vector3(0.05f, 4.8f, 0.07f), green);
            }
        }

        private static void BuildArenaRing(Transform parent, Material obsidian, Material metal, Material cyan, Material violet)
        {
            Transform root = Zone(parent, "District_ArenaOuterRing");
            Vector3 center = new Vector3(-25.8f, 0f, 6.5f);

            // Chunked pseudo-ring: eight safe platforms around a low center. It gives the
            // endgame district a readable circular motif without introducing a hole/void.
            const int segments = 8;
            for (int i = 0; i < segments; i++)
            {
                float angle = i / (float)segments * Mathf.PI * 2f;
                Vector3 radial = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
                float y = 0.70f + (i % 3) * 0.42f;
                Vector3 p = center + radial * 5.4f + Vector3.up * y;
                Block($"ArenaRing_Segment_{i}", root, p, new Vector3(4.0f, 0.46f, 2.8f), i % 2 == 0 ? obsidian : metal, true,
                    new Vector3(0f, angle * Mathf.Rad2Deg, 0f));
                SignalStrip($"ArenaRing_Signal_{i}", root, p + Vector3.up * 0.28f, new Vector3(2.5f, 0.045f, 0.07f), i % 2 == 0 ? cyan : violet,
                    new Vector3(0f, angle * Mathf.Rad2Deg, 0f));
            }

            Block("ArenaRing_Center", root, center + Vector3.up * 0.22f, new Vector3(7.2f, 0.44f, 7.2f), obsidian, true);
            CreateStairRun(root, "ArenaRing_Approach", new Vector3(-19.0f, 0.18f, 6.5f), new Vector3(-22.2f, 1.05f, 6.5f), 8, 3.2f, obsidian, cyan);
        }

        private static void BuildPerimeterMegastruts(Transform parent, Material obsidian, Material metal, Material cyan, Material violet)
        {
            Transform root = Zone(parent, "World_PerimeterMegastruts");
            Vector3[] positions =
            {
                new Vector3(-34.4f, 5.8f, -70.0f),
                new Vector3(34.4f, 6.4f, -61.0f),
                new Vector3(-34.4f, 6.1f, -5.0f),
                new Vector3(34.4f, 6.8f, 21.0f),
            };

            for (int i = 0; i < positions.Length; i++)
            {
                Vector3 p = positions[i];
                Block($"PerimeterMegastrut_{i}_Mass", root, p, new Vector3(3.2f, 11.6f + i * 0.6f, 3.2f), obsidian, true,
                    new Vector3(i % 2 == 0 ? -3f : 3f, i * 13f, i % 2 == 0 ? 2f : -2f));
                Block($"PerimeterMegastrut_{i}_Shoulder", root, p + Vector3.up * 3.1f, new Vector3(5.4f, 1.2f, 5.4f), metal, false,
                    new Vector3(0f, 45f + i * 9f, 0f));
                SignalStrip($"PerimeterMegastrut_{i}_Signal", root, p + new Vector3(i % 2 == 0 ? 1.72f : -1.72f, 0.3f, 0f),
                    new Vector3(0.055f, 5.9f, 0.08f), i % 2 == 0 ? cyan : violet);
            }
        }

        private static void CreateLandingPocket(Transform parent, string name, Vector3 center, Vector3 size, Material body, Material accent)
        {
            Block(name + "_Deck", parent, center, size, body, true);
            float top = center.y + size.y * 0.5f;
            SignalStrip(name + "_CrossA", parent, new Vector3(center.x, top + 0.035f, center.z), new Vector3(size.x * 0.70f, 0.025f, 0.055f), accent);
            SignalStrip(name + "_CrossB", parent, new Vector3(center.x, top + 0.038f, center.z), new Vector3(0.055f, 0.025f, size.z * 0.70f), accent);
        }

        private static void CreateStairRun(Transform parent, string name, Vector3 from, Vector3 to, int steps, float width, Material body, Material accent)
        {
            Transform root = Zone(parent, name);
            Vector3 delta = to - from;
            Vector3 flat = new Vector3(delta.x, 0f, delta.z);
            float flatLength = Mathf.Max(0.5f, flat.magnitude);
            Vector3 forward = flatLength > 0.001f ? flat.normalized : Vector3.forward;
            float depth = flatLength / Mathf.Max(1, steps);

            for (int i = 0; i < steps; i++)
            {
                float t = (i + 0.5f) / steps;
                Vector3 p = Vector3.Lerp(from, to, t);
                p.y = Mathf.Lerp(from.y, to.y, (i + 1f) / steps) - 0.12f;
                Block($"{name}_Step_{i:00}", root, p, new Vector3(width, 0.24f, depth + 0.06f), body, true,
                    new Vector3(0f, Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg, 0f));
                SignalStrip($"{name}_Edge_{i:00}", root,
                    p + Vector3.up * 0.14f + forward * depth * 0.40f,
                    new Vector3(width * 0.78f, 0.025f, 0.045f), accent,
                    new Vector3(0f, Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg, 0f));
            }
        }

        private static void CreateRamp(Transform parent, string name, Vector3 from, Vector3 to, float width, Material body, Material accent)
        {
            Vector3 delta = to - from;
            Vector3 flat = new Vector3(delta.x, 0f, delta.z);
            float horizontal = Mathf.Max(0.5f, flat.magnitude);
            float vertical = to.y - from.y;
            float length = Mathf.Sqrt(horizontal * horizontal + vertical * vertical);
            float yaw = Mathf.Atan2(flat.x, flat.z) * Mathf.Rad2Deg;
            float pitch = -Mathf.Atan2(vertical, horizontal) * Mathf.Rad2Deg;
            Vector3 center = (from + to) * 0.5f;
            Block(name + "_Collision", parent, center, new Vector3(width, 0.24f, length), body, true, new Vector3(pitch, yaw, 0f));
            SignalStrip(name + "_Signal", parent, center + Vector3.up * 0.15f, new Vector3(0.055f, 0.035f, length * 0.80f), accent,
                new Vector3(pitch, yaw, 0f));
        }

        private static GameObject Block(string name, Transform parent, Vector3 position, Vector3 scale, Material material, bool collider, Vector3 euler = default)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = scale;
            go.transform.rotation = Quaternion.Euler(euler);

            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }

            Collider c = go.GetComponent<Collider>();
            if (!collider && c != null) UnityEngine.Object.DestroyImmediate(c);
            GameObjectUtility.SetStaticEditorFlags(go, collider ? WorldStatic : VisualStatic);
            return go;
        }

        private static void SignalStrip(string name, Transform parent, Vector3 position, Vector3 scale, Material material, Vector3 euler = default)
        {
            Block(name, parent, position, scale, material, false, euler);
        }

        private static Transform Zone(Transform parent, string name)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        private static Material RequireMaterial(string name)
        {
            string[] ids = AssetDatabase.FindAssets(name + " t:Material");
            for (int i = 0; i < ids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(ids[i]);
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material != null && string.Equals(material.name, name, StringComparison.OrdinalIgnoreCase))
                    return material;
            }
            throw new InvalidOperationException("Grounded World Composition V2 could not resolve material: " + name);
        }
    }
}
#endif
