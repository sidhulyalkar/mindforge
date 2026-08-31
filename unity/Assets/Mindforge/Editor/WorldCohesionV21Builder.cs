#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mindforge.Editor
{
    /// <summary>
    /// V0.21 is the recording-driven cohesion pass layered after World Soul V0.20.
    ///
    /// It has two narrow jobs:
    /// 1) fix the first-boss arena as an actual movement space by widening the existing
    ///    collision-backed V0.11 floor/wall ring and removing the central dais as collision;
    /// 2) add static environmental evidence at material boundaries so architecture feels grown,
    ///    eroded, repaired and inhabited rather than assembled from isolated procedural props.
    ///
    /// All new V0.21 scenery is editor-authored, collider-free and temporally static. Existing
    /// V0.11 traversal remains authoritative except for the explicitly retuned boss-arena shell.
    /// No neural, combat, input, persistence or runtime animation authority is introduced here.
    /// </summary>
    public static class WorldCohesionV21Builder
    {
        public const string RootName = "Mindforge_World_Cohesion_V21";
        public const float ArenaFloorWidth = 36f;
        public const float ArenaFloorDepth = 34f;
        public const float ArenaWallRadius = 18.3f;
        private const float ArenaCenterZ = 94f;
        private const int Seed = 21021;
        private const string GeneratedMaterialRoot = "Assets/Mindforge/Generated/V21/Materials";

        public static bool PresentInOpenScene()
            => EditorSceneLookup.FindIncludingInactive(RootName) != null;

        public static void ApplyOpenScene()
        {
            GameObject canonical = EditorSceneLookup.FindIncludingInactive(MindforgeDemoV11Builder.RootName);
            if (canonical == null)
                throw new UnityEditor.Build.BuildFailedException(
                    $"V0.21 requires canonical world '{MindforgeDemoV11Builder.RootName}' in the open scene."
                );
            if (!WorldSoulV20Builder.PresentInOpenScene())
                throw new UnityEditor.Build.BuildFailedException("V0.21 must compose after World Soul V0.20.");

            Apply(canonical.transform);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                "[Mindforge:V21] Arena + Patina authored: widened Fractured Signal combat bowl, flat center, " +
                "terrain/material seams, fracture/soot scars, foreground ecology, near-city facade depth, roofs and landmark composition."
            );
        }

        public static void Apply(Transform canonicalRoot)
        {
            if (canonicalRoot == null) throw new ArgumentNullException(nameof(canonicalRoot));

            Transform previous = canonicalRoot.Find(RootName);
            if (previous != null) UnityEngine.Object.DestroyImmediate(previous.gameObject);

            WorldSoulMaterialLibraryV20.Palette palette = WorldSoulMaterialLibraryV20.Ensure();
            Transform arena = Require(canonicalRoot, "V11_Fractured_Signal_Arena");
            RetuneArenaShell(arena);
            PushWorldSoulCraterOutward(canonicalRoot);

            GameObject rootObject = new GameObject(RootName);
            rootObject.transform.SetParent(canonicalRoot, false);
            Transform root = rootObject.transform;

            Material warmWindow = EnsureGlowMaterial("V21_WindowWarm", new Color(0.78f, 0.29f, 0.07f), 1.8f);
            Material fractureGlow = EnsureGlowMaterial("V21_FractureGlow", new Color(0.92f, 0.05f, 0.16f), 1.45f);

            BuildSurfaceTransitions(root, palette);
            BuildArenaPatina(root, palette, fractureGlow);
            BuildForegroundEcology(root, palette);
            BuildNearCityFacades(root, palette, warmWindow);
            BuildLandmarkComposition(root, palette);
            ConfigureStaticRenderers(root);
        }

        private static void RetuneArenaShell(Transform arena)
        {
            Transform floor = Require(arena, "FractureFloor");
            floor.position = new Vector3(0f, 3.72f, ArenaCenterZ);
            floor.localScale = new Vector3(ArenaFloorWidth, 0.72f, ArenaFloorDepth);

            // The old 9x9 raised dais repeatedly compressed the player and boss into one pocket.
            // Keep a visual medallion, but make the movement plane effectively flat.
            Transform dais = Require(arena, "FractureInnerDais");
            dais.position = new Vector3(0f, 4.095f, ArenaCenterZ);
            dais.localScale = new Vector3(12.5f, 0.08f, 12.5f);
            Collider daisCollider = dais.GetComponent<Collider>();
            if (daisCollider != null) UnityEngine.Object.DestroyImmediate(daisCollider);

            Transform goldAxis = arena.Find("FractureGoldAxis");
            if (goldAxis != null)
            {
                goldAxis.position = new Vector3(0f, 4.11f, 91.5f);
                goldAxis.localScale = new Vector3(0.12f, 0.04f, 17f);
            }

            for (int i = 0; i < arena.childCount; i++)
            {
                Transform child = arena.GetChild(i);
                if (child == null || !child.name.StartsWith("FractureWall_", StringComparison.Ordinal)) continue;
                string suffix = child.name.Substring("FractureWall_".Length);
                if (!int.TryParse(suffix, out int segment)) continue;
                const int totalSegments = 14;
                float angle = segment / (float)totalSegments * Mathf.PI * 2f;
                child.position = new Vector3(
                    Mathf.Sin(angle) * ArenaWallRadius,
                    5.55f,
                    ArenaCenterZ + Mathf.Cos(angle) * ArenaWallRadius);
                child.localScale = new Vector3(8.1f, 4.5f, 0.86f);
                child.rotation = Quaternion.Euler(0f, angle * Mathf.Rad2Deg, 0f);
            }

            for (int i = 0; i < arena.childCount; i++)
            {
                Transform child = arena.GetChild(i);
                if (child == null || !child.name.StartsWith("FractureSpire_", StringComparison.Ordinal)) continue;
                Vector3 radial = Vector3.ProjectOnPlane(child.position - new Vector3(0f, child.position.y, ArenaCenterZ), Vector3.up);
                if (radial.sqrMagnitude < 0.001f) continue;
                radial.Normalize();
                child.position = new Vector3(radial.x * 16.4f, 6.2f, ArenaCenterZ + radial.z * 16.4f);
            }
        }

        private static void PushWorldSoulCraterOutward(Transform canonicalRoot)
        {
            Transform crater = canonicalRoot.Find(WorldSoulV20Builder.RootName + "/WorldSoul_Fracture_Crater");
            if (crater == null) return;

            Vector3 center = new Vector3(0f, 0f, ArenaCenterZ);
            for (int i = 0; i < crater.childCount; i++)
            {
                Transform child = crater.GetChild(i);
                if (child == null) continue;
                Vector3 flat = Vector3.ProjectOnPlane(child.position - center, Vector3.up);
                if (flat.sqrMagnitude < 0.001f) continue;
                Vector3 dir = flat.normalized;

                if (child.name.StartsWith("CraterRock_", StringComparison.Ordinal))
                {
                    float target = Mathf.Max(21.2f, flat.magnitude + 2.0f);
                    child.position = new Vector3(dir.x * target, child.position.y, ArenaCenterZ + dir.z * target);
                }
                else if (child.name.StartsWith("CraterResidualSignal_", StringComparison.Ordinal))
                {
                    float target = Mathf.Max(19.4f, flat.magnitude + 4.0f);
                    child.position = new Vector3(dir.x * target, child.position.y, ArenaCenterZ + dir.z * target);
                }
            }
        }

        private static void BuildSurfaceTransitions(Transform root, WorldSoulMaterialLibraryV20.Palette palette)
        {
            Transform seams = Node("V21_Surface_Transitions", root);

            // Causeway: break the hard wall -> empty gap -> water edge with damp stone and moss.
            for (int sideIndex = 0; sideIndex < 2; sideIndex++)
            {
                float side = sideIndex == 0 ? -1f : 1f;
                for (int i = 0; i < 18; i++)
                {
                    float z = 1.0f + i * 1.82f + WorldSoulNoiseV20.SignedHash(Seed, sideIndex * 100 + i) * 0.38f;
                    float x = side * Mathf.Lerp(5.58f, 5.96f, WorldSoulNoiseV20.Hash01(Seed ^ 0x2231, sideIndex * 100 + i));
                    float s = Mathf.Lerp(0.30f, 0.72f, WorldSoulNoiseV20.Hash01(Seed ^ 0x3811, sideIndex * 100 + i));
                    MeshObject($"CausewayWetStone_{sideIndex}_{i:00}", seams,
                        WorldSoulMeshLibraryV20.RockVariant(i + sideIndex * 3),
                        i % 3 == 0 ? palette.Moss : palette.WornStone,
                        new Vector3(x, -0.13f, z),
                        new Vector3(s * 1.4f, s * 0.36f, s),
                        new Vector3(0f, WorldSoulNoiseV20.Hash01(Seed, 400 + i) * 360f, side * 5f));
                }
            }

            // Sanctum/market: moss and soil accumulate where old masonry meets the ground.
            BuildContactScatter(seams, palette, new Vector3(-9.55f, 0.03f, -13f), new Vector3(0.45f, 0.06f, 19f), 12, 0);
            BuildContactScatter(seams, palette, new Vector3(9.55f, 0.03f, -13f), new Vector3(0.45f, 0.06f, 19f), 12, 20);
            BuildContactScatter(seams, palette, new Vector3(-10.45f, 0.05f, 45f), new Vector3(0.55f, 0.07f, 22f), 14, 40);
            BuildContactScatter(seams, palette, new Vector3(10.45f, 0.05f, 45f), new Vector3(0.55f, 0.07f, 22f), 14, 60);

            // Ascent: a broken rubble toe visually roots the ramp into geology.
            for (int sideIndex = 0; sideIndex < 2; sideIndex++)
            {
                float side = sideIndex == 0 ? -1f : 1f;
                for (int i = 0; i < 10; i++)
                {
                    float z = 60f + i * 2.45f;
                    float routeY = z <= 54f ? 0f : (z < 86f ? Mathf.Lerp(0f, 3.65f, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(54f, 86f, z))) : 3.65f);
                    float s = Mathf.Lerp(0.36f, 0.92f, WorldSoulNoiseV20.Hash01(Seed, 700 + sideIndex * 20 + i));
                    MeshObject($"AscentToe_{sideIndex}_{i:00}", seams,
                        WorldSoulMeshLibraryV20.RockVariant(i + 2),
                        i % 4 == 0 ? palette.Moss : palette.Basalt,
                        new Vector3(side * 6.35f, routeY - 0.08f, z),
                        new Vector3(s, s * 0.45f, s * 1.25f),
                        new Vector3(side * 4f, WorldSoulNoiseV20.Hash01(Seed, 760 + i) * 360f, 0f));
                }
            }
        }

        private static void BuildContactScatter(
            Transform parent,
            WorldSoulMaterialLibraryV20.Palette palette,
            Vector3 center,
            Vector3 extent,
            int count,
            int offset)
        {
            for (int i = 0; i < count; i++)
            {
                float z = center.z + WorldSoulNoiseV20.SignedHash(Seed, offset + i * 3) * extent.z * 0.5f;
                float x = center.x + WorldSoulNoiseV20.SignedHash(Seed ^ 0x4422, offset + i * 3 + 1) * extent.x * 0.5f;
                float s = Mathf.Lerp(0.24f, 0.58f, WorldSoulNoiseV20.Hash01(Seed ^ 0x7711, offset + i * 3 + 2));
                MeshObject($"ContactPatina_{offset}_{i:00}", parent,
                    WorldSoulMeshLibraryV20.RockVariant(offset + i),
                    i % 3 == 0 ? palette.Earth : palette.Moss,
                    new Vector3(x, center.y, z),
                    new Vector3(s * 1.5f, s * 0.18f, s),
                    new Vector3(0f, WorldSoulNoiseV20.Hash01(Seed, 900 + offset + i) * 360f, 0f));
            }
        }

        private static void BuildArenaPatina(
            Transform root,
            WorldSoulMaterialLibraryV20.Palette palette,
            Material fractureGlow)
        {
            Transform zone = Node("V21_Fracture_Arena_Patina", root);
            float floorY = 4.091f;

            // Long irregular fracture strokes create directional history without adding obstacles.
            for (int i = 0; i < 15; i++)
            {
                float angle = WorldSoulNoiseV20.Hash01(Seed, 1200 + i) * Mathf.PI * 2f;
                float startRadius = Mathf.Lerp(2.8f, 8.5f, WorldSoulNoiseV20.Hash01(Seed ^ 0x1122, 1200 + i));
                Vector3 dir = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
                Vector3 tangent = new Vector3(dir.z, 0f, -dir.x);
                Vector3 start = new Vector3(0f, floorY, ArenaCenterZ) + dir * startRadius;
                int segments = 2 + (i % 3);
                Vector3 cursor = start;
                for (int s = 0; s < segments; s++)
                {
                    float length = Mathf.Lerp(1.2f, 3.1f, WorldSoulNoiseV20.Hash01(Seed, 1300 + i * 7 + s));
                    float bend = WorldSoulNoiseV20.SignedHash(Seed ^ 0x5533, 1400 + i * 7 + s) * 0.42f;
                    Vector3 next = cursor + (dir + tangent * bend).normalized * length;
                    BlockBetween($"ArenaFracture_{i:00}_{s}", zone, cursor, next,
                        Mathf.Lerp(0.035f, 0.085f, WorldSoulNoiseV20.Hash01(Seed, 1500 + i + s)),
                        i % 4 == 0 ? fractureGlow : palette.EmberStone,
                        0.018f);
                    cursor = next;
                }
            }

            // Sooted arcs and chips live near the perimeter, leaving the duel floor visually quiet in the middle.
            for (int i = 0; i < 22; i++)
            {
                float angle = i / 22f * Mathf.PI * 2f + WorldSoulNoiseV20.SignedHash(Seed, 1600 + i) * 0.08f;
                float radius = Mathf.Lerp(13.8f, 16.2f, WorldSoulNoiseV20.Hash01(Seed, 1700 + i));
                Vector3 p = new Vector3(Mathf.Sin(angle) * radius, floorY + 0.015f, ArenaCenterZ + Mathf.Cos(angle) * radius);
                float s = Mathf.Lerp(0.22f, 0.80f, WorldSoulNoiseV20.Hash01(Seed ^ 0x2871, 1800 + i));
                MeshObject($"ArenaSootChip_{i:00}", zone, WorldSoulMeshLibraryV20.RockVariant(i),
                    i % 6 == 0 ? palette.EmberStone : palette.Basalt,
                    p, new Vector3(s * 1.4f, s * 0.12f, s),
                    new Vector3(0f, angle * Mathf.Rad2Deg, 0f));
            }

            // Wall-foot erosion makes the arena shell read as old masonry, not fresh primitives.
            for (int i = 0; i < 18; i++)
            {
                float angle = i / 18f * Mathf.PI * 2f;
                if (Mathf.Abs(Mathf.DeltaAngle(180f, angle * Mathf.Rad2Deg)) < 30f) continue;
                Vector3 p = new Vector3(Mathf.Sin(angle) * 17.45f, 4.14f, ArenaCenterZ + Mathf.Cos(angle) * 17.45f);
                MeshObject($"ArenaWallFoot_{i:00}", zone, WorldSoulMeshLibraryV20.RockVariant(i + 3),
                    i % 4 == 0 ? palette.EmberStone : palette.WornStone,
                    p, new Vector3(0.7f, 0.24f, 1.15f),
                    new Vector3(0f, angle * Mathf.Rad2Deg, 0f));
            }
        }

        private static void BuildForegroundEcology(Transform root, WorldSoulMaterialLibraryV20.Palette palette)
        {
            Transform zone = Node("V21_Foreground_Ecology", root);
            Vector3[] fernAnchors =
            {
                new Vector3(-8.8f, 0.02f, -20.5f), new Vector3(-8.7f, 0.02f, -10.5f),
                new Vector3(8.8f, 0.02f, -17.5f), new Vector3(8.7f, 0.02f, -6.0f),
                new Vector3(-10.65f, -0.38f, 6.5f), new Vector3(10.65f, -0.38f, 11.5f),
                new Vector3(-10.7f, -0.38f, 23.5f), new Vector3(10.7f, -0.38f, 29.0f),
            };
            for (int i = 0; i < fernAnchors.Length; i++)
                BuildFern($"ForegroundFern_{i:00}", zone, fernAnchors[i], palette, Seed + 2000 + i * 19);

            // Dense small stones/leaf clusters at camera height beat giant isolated props.
            for (int i = 0; i < 18; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                float z = Mathf.Lerp(-21f, -1.5f, WorldSoulNoiseV20.Hash01(Seed, 2200 + i));
                float x = side * Mathf.Lerp(8.3f, 9.4f, WorldSoulNoiseV20.Hash01(Seed ^ 0x6262, 2200 + i));
                float s = Mathf.Lerp(0.25f, 0.55f, WorldSoulNoiseV20.Hash01(Seed ^ 0x7373, 2200 + i));
                MeshObject($"SanctumLeafLitter_{i:00}", zone, ProductionMeshLibraryV09.GardenCanopy(),
                    i % 4 == 0 ? palette.Moss : palette.Foliage,
                    new Vector3(x, 0.12f, z), new Vector3(s * 1.3f, s * 0.38f, s),
                    new Vector3(0f, WorldSoulNoiseV20.Hash01(Seed, 2300 + i) * 360f, 0f));
            }
        }

        private static void BuildFern(
            string name,
            Transform parent,
            Vector3 position,
            WorldSoulMaterialLibraryV20.Palette palette,
            int seed)
        {
            Transform fern = Node(name, parent);
            fern.localPosition = position;
            DecorativeCylinder("Stem", fern, new Vector3(0f, 0.22f, 0f), new Vector3(0.018f, 0.22f, 0.018f), palette.Moss);

            int leaves = 7;
            for (int i = 0; i < leaves; i++)
            {
                float yaw = i / (float)leaves * 360f + WorldSoulNoiseV20.SignedHash(seed, i) * 13f;
                float length = Mathf.Lerp(0.44f, 0.82f, WorldSoulNoiseV20.Hash01(seed, 20 + i));
                Transform leaf = DecorativeBlock($"Leaf_{i:00}", fern,
                    new Vector3(0f, 0.30f + i * 0.018f, 0f),
                    new Vector3(0.075f, 0.018f, length),
                    i % 3 == 0 ? palette.Moss : palette.Foliage,
                    new Vector3(-26f + WorldSoulNoiseV20.SignedHash(seed, 30 + i) * 8f, yaw, 0f));
                leaf.localPosition += (leaf.localRotation * Vector3.forward) * (length * 0.43f);
            }
        }

        private static void BuildNearCityFacades(
            Transform root,
            WorldSoulMaterialLibraryV20.Palette palette,
            Material warmWindow)
        {
            Transform zone = Node("V21_Near_City_Facades", root);
            for (int sideIndex = 0; sideIndex < 2; sideIndex++)
            {
                float side = sideIndex == 0 ? -1f : 1f;
                for (int i = 0; i < 4; i++)
                {
                    float z = 35.5f + i * 7.1f;
                    float x = side * (16.8f + (i % 2) * 1.35f);
                    float width = 4.4f + (i % 3) * 0.55f;
                    float depth = 5.0f + (i % 2) * 1.0f;
                    float height = 6.4f + i * 1.15f;
                    BuildFacadeHouse($"NearFacade_{sideIndex}_{i}", zone,
                        new Vector3(x, 0f, z), side, width, depth, height,
                        palette, warmWindow, Seed + 2600 + sideIndex * 100 + i * 11);
                }
            }
        }

        private static void BuildFacadeHouse(
            string name,
            Transform parent,
            Vector3 position,
            float side,
            float width,
            float depth,
            float height,
            WorldSoulMaterialLibraryV20.Palette palette,
            Material warmWindow,
            int seed)
        {
            Transform building = Node(name, parent);
            building.localPosition = position;
            building.localRotation = Quaternion.Euler(0f, side * WorldSoulNoiseV20.SignedHash(seed, 1) * 5f, 0f);

            DecorativeBlock("FacadeMass", building, new Vector3(0f, height * 0.5f, 0f),
                new Vector3(width, height, depth), palette.Basalt, Vector3.zero);
            DecorativeBlock("StoneBase", building, new Vector3(-side * width * 0.49f, 0.65f, 0f),
                new Vector3(0.18f, 1.3f, depth * 0.94f), palette.WornStone, Vector3.zero);

            for (int bay = -1; bay <= 1; bay += 2)
            {
                DecorativeBlock($"Pier_{bay}", building,
                    new Vector3(-side * (width * 0.515f), height * 0.54f, bay * depth * 0.30f),
                    new Vector3(0.20f, height * 0.86f, 0.24f), palette.Limestone, Vector3.zero);
            }

            for (int level = 0; level < 3; level++)
            {
                float y = height * (0.27f + level * 0.22f);
                for (int bay = -1; bay <= 1; bay += 2)
                {
                    float z = bay * depth * 0.22f;
                    DecorativeBlock($"Window_{level}_{bay}", building,
                        new Vector3(-side * width * 0.521f, y, z),
                        new Vector3(0.07f, 0.62f, 0.70f), warmWindow, Vector3.zero);
                    DecorativeBlock($"WindowLintel_{level}_{bay}", building,
                        new Vector3(-side * width * 0.535f, y + 0.43f, z),
                        new Vector3(0.11f, 0.12f, 0.94f), palette.Limestone, Vector3.zero);
                }
            }

            // Two offset roof slabs create a readable roofline instead of another box silhouette.
            float roofY = height + 0.62f;
            DecorativeBlock("RoofInner", building,
                new Vector3(-width * 0.22f, roofY, 0f),
                new Vector3(width * 0.62f, 0.30f, depth * 1.08f), palette.WornStone,
                new Vector3(0f, 0f, 26f));
            DecorativeBlock("RoofOuter", building,
                new Vector3(width * 0.22f, roofY, 0f),
                new Vector3(width * 0.62f, 0.30f, depth * 1.08f), palette.WornStone,
                new Vector3(0f, 0f, -26f));

            if (WorldSoulNoiseV20.Hash01(seed, 7) > 0.42f)
                MeshObject("RoofSpire", building, ProductionMeshLibraryV09.CathedralSpire(), palette.Limestone,
                    new Vector3(0f, height + 2.1f, 0f), new Vector3(1.05f, 2.7f, 1.05f),
                    new Vector3(0f, WorldSoulNoiseV20.Hash01(seed, 8) * 35f, 0f), false);
        }

        private static void BuildLandmarkComposition(Transform root, WorldSoulMaterialLibraryV20.Palette palette)
        {
            Transform zone = Node("V21_Landmark_Composition", root);

            // Broken outer arches frame the boss from the approach but sit beyond the movement bowl.
            MeshObject("BossOuterArchLeft", zone, ProductionMeshLibraryV09.PointedArch(), palette.Limestone,
                new Vector3(-14.8f, 8.2f, 99.0f), new Vector3(4.4f, 5.7f, 1.5f), new Vector3(0f, 20f, -7f));
            MeshObject("BossOuterArchRight", zone, ProductionMeshLibraryV09.PointedArch(), palette.WornStone,
                new Vector3(14.8f, 7.6f, 98.0f), new Vector3(4.0f, 5.1f, 1.5f), new Vector3(0f, -18f, 9f));

            // Memory Forge gets a small ring of age/ritual evidence rather than another glowing hero prop.
            for (int i = 0; i < 9; i++)
            {
                float a = i / 9f * Mathf.PI * 2f;
                float r = 2.8f + WorldSoulNoiseV20.SignedHash(Seed, 3100 + i) * 0.35f;
                Vector3 p = new Vector3(Mathf.Sin(a) * r, 0.08f, -15.2f + Mathf.Cos(a) * r);
                MeshObject($"ForgeOfferingStone_{i:00}", zone, WorldSoulMeshLibraryV20.RockVariant(i + 1),
                    i % 3 == 0 ? palette.Moss : palette.WornStone,
                    p, new Vector3(0.42f, 0.22f, 0.58f), new Vector3(0f, a * Mathf.Rad2Deg, 0f));
            }
        }

        private static Transform Require(Transform parent, string path)
        {
            Transform found = parent.Find(path);
            if (found == null)
                throw new UnityEditor.Build.BuildFailedException($"V0.21 required canonical object missing: {path}");
            return found;
        }

        private static Transform Node(string name, Transform parent)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        private static Transform DecorativeBlock(
            string name,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            Material material,
            Vector3 euler)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = localScale;
            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = material;
            Collider collider = go.GetComponent<Collider>();
            if (collider != null) UnityEngine.Object.DestroyImmediate(collider);
            return go.transform;
        }

        private static Transform DecorativeCylinder(
            string name,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            Material material)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;
            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = material;
            Collider collider = go.GetComponent<Collider>();
            if (collider != null) UnityEngine.Object.DestroyImmediate(collider);
            return go.transform;
        }

        private static void BlockBetween(
            string name,
            Transform parent,
            Vector3 start,
            Vector3 end,
            float width,
            Material material,
            float thickness)
        {
            Vector3 delta = end - start;
            float length = delta.magnitude;
            if (length < 0.01f) return;
            Transform block = DecorativeBlock(name, parent, (start + end) * 0.5f,
                new Vector3(width, thickness, length), material, Vector3.zero);
            block.rotation = Quaternion.LookRotation(delta.normalized, Vector3.up);
        }

        private static void MeshObject(
            string name,
            Transform parent,
            Mesh mesh,
            Material material,
            Vector3 localPosition,
            Vector3 localScale,
            Vector3 euler,
            bool castShadows = true)
        {
            if (mesh == null) return;
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = localScale;
            MeshFilter filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            MeshRenderer renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            renderer.receiveShadows = true;
        }

        private static Material EnsureGlowMaterial(string name, Color color, float intensity)
        {
            EnsureFolder(GeneratedMaterialRoot);
            string path = GeneratedMaterialRoot + "/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            else if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * Mathf.Max(0f, intensity));
            }
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.32f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void EnsureFolder(string fullPath)
        {
            string[] parts = fullPath.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private static void ConfigureStaticRenderers(Transform root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null) continue;
                renderer.receiveShadows = true;
                renderer.lightProbeUsage = LightProbeUsage.BlendProbes;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.BlendProbes;
            }
        }
    }
}
#endif
