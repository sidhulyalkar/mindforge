#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Mindforge.Combat;

namespace Mindforge.Editor
{
    /// <summary>
    /// V0.20 turns the clean V0.11 route into one continuous authored landscape.
    ///
    /// This layer is intentionally editor-only and presentation-only. It does not create
    /// traversal colliders, runtime animation, particles, neural consumers or combat state.
    /// The existing V0.11 geometry remains the collision and navigation authority.
    ///
    /// Public-code technique references:
    /// - SebLague/Procedural-Landmass-Generation (MIT): deterministic octave-noise terrain grammar.
    /// - aadebdeb/ProceduralMesh (MIT): mesh-recipe workflow rather than opaque model imports.
    /// - keijiro/NoiseShader (MIT): evaluated for a future GPU surface pass; not a V0.20 dependency.
    /// </summary>
    public static class WorldSoulV20Builder
    {
        public const string RootName = "Mindforge_World_Soul_V20";
        private const int TerrainSeed = 20481;
        private const int ScatterSeed = 20482;
        private const int EcologySeed = 20483;

        public static bool PresentInOpenScene()
            => EditorSceneLookup.FindIncludingInactive(RootName) != null;

        public static void ApplyOpenScene()
        {
            GameObject canonical = EditorSceneLookup.FindIncludingInactive(MindforgeDemoV11Builder.RootName);
            if (canonical == null)
                throw new UnityEditor.Build.BuildFailedException(
                    $"V0.20 requires canonical world '{MindforgeDemoV11Builder.RootName}' in the open scene."
                );

            Apply(canonical.transform);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                "[Mindforge:V20] World Soul authored: continuous landform, weathered surfaces, sanctum grove, " +
                "causeway banks, market ruins, ascent geology, crater rim, distant city and static atmospheric lighting. " +
                "Canonical traversal/combat/neural authority remains unchanged."
            );
        }

        public static void Apply(Transform canonicalRoot)
        {
            if (canonicalRoot == null) throw new ArgumentNullException(nameof(canonicalRoot));

            Transform previous = canonicalRoot.Find(RootName);
            if (previous != null) UnityEngine.Object.DestroyImmediate(previous.gameObject);

            WorldSoulMaterialLibraryV20.Palette palette = WorldSoulMaterialLibraryV20.Ensure();
            RetextureCanonicalArchitecture(canonicalRoot, palette);

            GameObject rootObject = new GameObject(RootName);
            rootObject.transform.SetParent(canonicalRoot, false);
            Transform root = rootObject.transform;

            Material warmWindow = EnsureGlowMaterial(
                "WorldSoulWindowWarm", new Color(0.72f, 0.31f, 0.08f), 2.4f);
            Material coolWindow = EnsureGlowMaterial(
                "WorldSoulWindowCool", new Color(0.08f, 0.42f, 0.72f), 1.65f);
            Material fractureGlow = EnsureGlowMaterial(
                "WorldSoulFractureGlow", new Color(0.88f, 0.055f, 0.12f), 2.3f);

            BuildOuterTerrain(root, palette);
            ScatterNaturalRock(root, palette);
            BuildSanctumEcology(root, palette, warmWindow);
            BuildCausewayBanks(root, palette, coolWindow);
            BuildMarketRuins(root, palette, warmWindow);
            BuildAscentGeology(root, palette, coolWindow);
            BuildFractureCrater(root, palette, fractureGlow);
            BuildDistantCity(root, palette, warmWindow, coolWindow);
            BuildAtmosphericLandmarks(root, palette);
            ConfigureAtmosphereAndLighting(root, palette);
            ConfigureStaticRenderers(root);
        }

        private static void RetextureCanonicalArchitecture(
            Transform canonicalRoot,
            WorldSoulMaterialLibraryV20.Palette palette)
        {
            Renderer[] renderers = canonicalRoot.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null) continue;
                if (renderer.GetComponentInParent<CombatantVitals>() != null) continue;

                string n = renderer.gameObject.name;
                if (n.StartsWith("V11Echo", StringComparison.OrdinalIgnoreCase)) continue;
                if (ContainsAny(n, "Gold", "Aether", "SignalOrb", "MemoryForgeCore", "FractureSpire")) continue;

                Material replacement = null;
                if (ContainsAny(n, "Water", "Canal")) replacement = palette.Water;
                else if (ContainsAny(n, "Garden")) replacement = palette.Moss;
                else if (ContainsAny(n, "Fracture")) replacement = palette.EmberStone;
                else if (ContainsAny(n, "Column", "Arch", "Spire", "Crown")) replacement = palette.Limestone;
                else if (ContainsAny(n, "Wall", "SkylineMass", "LowerCityMass", "Graphite")) replacement = palette.Basalt;
                else if (ContainsAny(n, "Floor", "Road", "Ramp", "Platform", "Perch", "Dais", "Stall", "Plinth"))
                    replacement = palette.WornStone;

                if (replacement != null) renderer.sharedMaterial = replacement;
            }
        }

        private static void BuildOuterTerrain(Transform root, WorldSoulMaterialLibraryV20.Palette palette)
        {
            Mesh west = WorldSoulMeshLibraryV20.TerrainPatch(
                "WorldSoul_WestTerrain", -52f, -13.4f, -58f, 150f, 20, 72, SideTerrainHeight);
            Mesh east = WorldSoulMeshLibraryV20.TerrainPatch(
                "WorldSoul_EastTerrain", 13.4f, 52f, -58f, 150f, 20, 72, SideTerrainHeight);
            Mesh south = WorldSoulMeshLibraryV20.TerrainPatch(
                "WorldSoul_SouthTerrain", -14f, 14f, -66f, -25.2f, 24, 18, SouthTerrainHeight);
            Mesh north = WorldSoulMeshLibraryV20.TerrainPatch(
                "WorldSoul_NorthHighlands", -20f, 20f, 111f, 174f, 26, 30, NorthTerrainHeight);

            MeshObject("WestLandmass", root, west, palette.Earth);
            MeshObject("EastLandmass", root, east, palette.Earth);
            MeshObject("SouthLandmass", root, south, palette.Moss);
            MeshObject("NorthHighlands", root, north, palette.Basalt);
        }

        private static float SideTerrainHeight(float x, float z)
        {
            float route = RouteElevation(z) - 1.15f;
            float outer = Mathf.Clamp01((Mathf.Abs(x) - 13.4f) / 36f);
            float broad = WorldSoulNoiseV20.Fbm(x, z, TerrainSeed, 5, 24f, 0.54f, 2.03f);
            float ridge = WorldSoulNoiseV20.Ridge(x + 23f, z - 11f, TerrainSeed ^ 0x5151, 17f);
            float rise = Mathf.SmoothStep(0f, 1f, outer) * (6.4f + Mathf.Max(0f, z - 100f) * 0.030f);
            return route + rise + broad * Mathf.Lerp(0.45f, 2.15f, outer) + ridge * outer * 1.35f;
        }

        private static float SouthTerrainHeight(float x, float z)
        {
            float away = Mathf.Clamp01(Mathf.InverseLerp(-25.2f, -66f, z));
            float n = WorldSoulNoiseV20.Fbm(x, z, TerrainSeed ^ 0xA101, 5, 19f, 0.55f, 2.05f);
            return -1.45f + Mathf.SmoothStep(0f, 1f, away) * 7.2f + n * Mathf.Lerp(0.45f, 1.8f, away);
        }

        private static float NorthTerrainHeight(float x, float z)
        {
            float away = Mathf.Clamp01(Mathf.InverseLerp(111f, 174f, z));
            float side = Mathf.Clamp01(Mathf.Abs(x) / 20f);
            float n = WorldSoulNoiseV20.Fbm(x, z, TerrainSeed ^ 0xB202, 5, 21f, 0.57f, 2.07f);
            float ridge = WorldSoulNoiseV20.Ridge(x - 5f, z + 9f, TerrainSeed ^ 0xB2B2, 13f);
            return 2.7f + Mathf.SmoothStep(0f, 1f, away) * 11.5f + side * 2.2f + n * 2.3f + ridge * away * 2.0f;
        }

        private static float RouteElevation(float z)
        {
            if (z <= 54f) return 0f;
            if (z < 86f) return Mathf.Lerp(0f, 3.65f, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(54f, 86f, z)));
            return 3.65f;
        }

        private static void ScatterNaturalRock(Transform root, WorldSoulMaterialLibraryV20.Palette palette)
        {
            Transform group = Node("WorldSoul_Natural_Rock", root);
            for (int sideIndex = 0; sideIndex < 2; sideIndex++)
            {
                float side = sideIndex == 0 ? -1f : 1f;
                for (int i = 0; i < 38; i++)
                {
                    int h = sideIndex * 100 + i;
                    float z = Mathf.Lerp(-48f, 142f, WorldSoulNoiseV20.Hash01(ScatterSeed, h * 4));
                    float x = side * Mathf.Lerp(14.8f, 45f, WorldSoulNoiseV20.Hash01(ScatterSeed, h * 4 + 1));
                    float y = SideTerrainHeight(x, z);
                    float scale = Mathf.Lerp(0.38f, 2.25f, Mathf.Pow(WorldSoulNoiseV20.Hash01(ScatterSeed, h * 4 + 2), 1.6f));
                    Vector3 squash = new Vector3(
                        scale * Mathf.Lerp(0.72f, 1.35f, WorldSoulNoiseV20.Hash01(ScatterSeed, h * 4 + 3)),
                        scale,
                        scale * Mathf.Lerp(0.78f, 1.30f, WorldSoulNoiseV20.Hash01(ScatterSeed ^ 0x3131, h)));

                    MeshObject(
                        $"FieldRock_{sideIndex}_{i:00}",
                        group,
                        WorldSoulMeshLibraryV20.RockVariant(h),
                        i % 7 == 0 ? palette.Moss : palette.WornStone,
                        new Vector3(x, y + squash.y * 0.25f, z),
                        squash,
                        new Vector3(
                            WorldSoulNoiseV20.SignedHash(ScatterSeed, h) * 11f,
                            WorldSoulNoiseV20.Hash01(ScatterSeed ^ 0x4141, h) * 360f,
                            WorldSoulNoiseV20.SignedHash(ScatterSeed ^ 0x5151, h) * 9f));
                }
            }
        }

        private static void BuildSanctumEcology(
            Transform root,
            WorldSoulMaterialLibraryV20.Palette palette,
            Material warmWindow)
        {
            Transform zone = Node("WorldSoul_Sanctum_Grove", root);
            Vector3[] treePositions =
            {
                new Vector3(-12.8f, -0.25f, -21f), new Vector3(-14.6f, -0.12f, -14f),
                new Vector3(-12.3f, -0.20f, -5.5f), new Vector3(12.6f, -0.24f, -20f),
                new Vector3(14.8f, -0.10f, -12.5f), new Vector3(12.1f, -0.18f, -5f),
            };
            for (int i = 0; i < treePositions.Length; i++)
                BuildAncientTree($"SanctumTree_{i:00}", zone, treePositions[i], 3.8f + (i % 3) * 0.55f, palette, EcologySeed + i * 13);

            for (int i = 0; i < 18; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                float z = Mathf.Lerp(-22f, -3f, WorldSoulNoiseV20.Hash01(EcologySeed, i * 3));
                float x = side * Mathf.Lerp(10.8f, 15.5f, WorldSoulNoiseV20.Hash01(EcologySeed, i * 3 + 1));
                BuildShrub($"SanctumShrub_{i:00}", zone, new Vector3(x, -0.08f, z), palette, EcologySeed + 200 + i);
            }

            // Small warm votive niches create evidence of prior human use without moving lights.
            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < 3; i++)
                {
                    float z = -21f + i * 7.8f;
                    DecorativeBlock($"SanctumVotive_{side}_{i}", zone,
                        new Vector3(side * 9.55f, 1.55f, z), new Vector3(0.14f, 0.24f, 0.06f), warmWindow,
                        new Vector3(0f, side < 0 ? 90f : -90f, 0f));
                }
            }
        }

        private static void BuildCausewayBanks(
            Transform root,
            WorldSoulMaterialLibraryV20.Palette palette,
            Material coolWindow)
        {
            Transform zone = Node("WorldSoul_Causeway_Banks", root);
            for (int sideIndex = 0; sideIndex < 2; sideIndex++)
            {
                float side = sideIndex == 0 ? -1f : 1f;
                for (int i = 0; i < 26; i++)
                {
                    float z = 0.8f + i * 1.25f + WorldSoulNoiseV20.SignedHash(EcologySeed, sideIndex * 100 + i) * 0.35f;
                    float x = side * Mathf.Lerp(9.6f, 10.9f, WorldSoulNoiseV20.Hash01(EcologySeed ^ 0x2121, sideIndex * 100 + i));
                    float height = Mathf.Lerp(0.55f, 1.35f, WorldSoulNoiseV20.Hash01(EcologySeed ^ 0x3131, sideIndex * 100 + i));
                    DecorativeCylinder($"Reed_{sideIndex}_{i:00}", zone,
                        new Vector3(x, -0.45f + height * 0.5f, z),
                        new Vector3(0.035f, height * 0.5f, 0.035f), palette.Foliage,
                        new Vector3(WorldSoulNoiseV20.SignedHash(EcologySeed, i) * 6f, 0f,
                            WorldSoulNoiseV20.SignedHash(EcologySeed ^ 0x4040, i) * 6f));
                }

                for (int i = 0; i < 7; i++)
                {
                    float z = 3f + i * 4.5f;
                    float x = side * 11.6f;
                    MeshObject($"CausewayBankRock_{sideIndex}_{i}", zone,
                        WorldSoulMeshLibraryV20.RockVariant(i + sideIndex * 7), palette.WornStone,
                        new Vector3(x, -0.15f, z), new Vector3(1.5f, 0.85f, 1.15f),
                        new Vector3(0f, WorldSoulNoiseV20.Hash01(EcologySeed, i + sideIndex * 9) * 360f, side * 4f));
                }
            }

            DecorativeBlock("CausewayFarBeacon", zone, new Vector3(-15.5f, 5.2f, 26f),
                new Vector3(0.16f, 4.2f, 0.16f), coolWindow, new Vector3(0f, 0f, -3f));
        }

        private static void BuildMarketRuins(
            Transform root,
            WorldSoulMaterialLibraryV20.Palette palette,
            Material warmWindow)
        {
            Transform zone = Node("WorldSoul_Market_Ruins", root);
            for (int sideIndex = 0; sideIndex < 2; sideIndex++)
            {
                float side = sideIndex == 0 ? -1f : 1f;
                for (int i = 0; i < 4; i++)
                {
                    float z = 36.5f + i * 6.2f;
                    Transform ruin = Node($"MarketRuin_{sideIndex}_{i}", zone);
                    ruin.position = new Vector3(side * (13.3f + (i % 2) * 1.4f), 0f, z);
                    ruin.rotation = Quaternion.Euler(0f, side * (8f + i * 4f), side * (i % 2 == 0 ? 1.5f : -2.2f));
                    MeshObject("RuinColumnA", ruin, ProductionMeshLibraryV09.FlutedColumn(), palette.Limestone,
                        new Vector3(-0.9f, 1.7f, 0f), new Vector3(0.70f, 3.4f, 0.70f), Vector3.zero, false);
                    MeshObject("RuinColumnB", ruin, ProductionMeshLibraryV09.FlutedColumn(), palette.WornStone,
                        new Vector3(1.0f, 1.25f, 0.15f), new Vector3(0.62f, 2.5f, 0.62f), new Vector3(0f, 0f, 7f), false);
                    DecorativeBlock("RuinLintel", ruin, new Vector3(0f, 3.25f, 0f),
                        new Vector3(2.8f, 0.34f, 0.52f), palette.WornStone, new Vector3(0f, 0f, side * (3f + i)));
                    if (i % 2 == 0)
                        DecorativeBlock("RuinLamp", ruin, new Vector3(0f, 2.2f, -0.32f),
                            new Vector3(0.10f, 0.18f, 0.08f), warmWindow, Vector3.zero);
                }
            }

            for (int i = 0; i < 24; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                float x = side * Mathf.Lerp(11.7f, 16.5f, WorldSoulNoiseV20.Hash01(ScatterSeed, 600 + i));
                float z = Mathf.Lerp(34f, 57f, WorldSoulNoiseV20.Hash01(ScatterSeed, 700 + i));
                float s = Mathf.Lerp(0.28f, 0.85f, WorldSoulNoiseV20.Hash01(ScatterSeed, 800 + i));
                MeshObject($"MarketRubble_{i:00}", zone, WorldSoulMeshLibraryV20.RockVariant(i),
                    i % 5 == 0 ? palette.Moss : palette.WornStone,
                    new Vector3(x, s * 0.20f - 0.20f, z), new Vector3(s, s * 0.55f, s * 0.8f),
                    new Vector3(0f, WorldSoulNoiseV20.Hash01(ScatterSeed, 900 + i) * 360f, 0f));
            }
        }

        private static void BuildAscentGeology(
            Transform root,
            WorldSoulMaterialLibraryV20.Palette palette,
            Material coolWindow)
        {
            Transform zone = Node("WorldSoul_Ascent_Geology", root);
            for (int sideIndex = 0; sideIndex < 2; sideIndex++)
            {
                float side = sideIndex == 0 ? -1f : 1f;
                for (int i = 0; i < 12; i++)
                {
                    float z = 59f + i * 2.2f;
                    float y = RouteElevation(z) - 0.1f;
                    float x = side * Mathf.Lerp(7.2f, 12.2f, WorldSoulNoiseV20.Hash01(ScatterSeed, 1000 + sideIndex * 50 + i));
                    float s = Mathf.Lerp(0.9f, 2.45f, WorldSoulNoiseV20.Hash01(ScatterSeed, 1100 + sideIndex * 50 + i));
                    MeshObject($"AscentRock_{sideIndex}_{i:00}", zone, WorldSoulMeshLibraryV20.RockVariant(i + sideIndex * 3),
                        i % 4 == 0 ? palette.Moss : palette.Basalt,
                        new Vector3(x, y + s * 0.30f, z), new Vector3(s, s * 1.35f, s * 0.82f),
                        new Vector3(side * 4f, WorldSoulNoiseV20.Hash01(ScatterSeed, 1200 + i) * 360f, side * 7f));
                }
            }

            MeshObject("AscentBrokenArch", zone, ProductionMeshLibraryV09.PointedArch(), palette.Limestone,
                new Vector3(-9.0f, 6.0f, 76f), new Vector3(3.4f, 4.1f, 1.6f), new Vector3(0f, 18f, -12f));
            DecorativeBlock("AscentColdLamp", zone, new Vector3(-8.5f, 7.0f, 75.8f),
                new Vector3(0.12f, 0.34f, 0.12f), coolWindow, Vector3.zero);
        }

        private static void BuildFractureCrater(
            Transform root,
            WorldSoulMaterialLibraryV20.Palette palette,
            Material fractureGlow)
        {
            Transform zone = Node("WorldSoul_Fracture_Crater", root);
            const float centerZ = 94f;
            const int count = 28;
            for (int i = 0; i < count; i++)
            {
                float degrees = i / (float)count * 360f;
                // Preserve the broad south entrance from the canonical ascent.
                float signed = Mathf.DeltaAngle(180f, degrees);
                if (Mathf.Abs(signed) < 27f) continue;

                float a = degrees * Mathf.Deg2Rad;
                float radius = Mathf.Lerp(15.0f, 19.8f, WorldSoulNoiseV20.Hash01(ScatterSeed, 1500 + i));
                float s = Mathf.Lerp(0.75f, 2.25f, WorldSoulNoiseV20.Hash01(ScatterSeed, 1600 + i));
                Vector3 p = new Vector3(Mathf.Sin(a) * radius, 3.55f + s * 0.35f, centerZ + Mathf.Cos(a) * radius);
                MeshObject($"CraterRock_{i:00}", zone, WorldSoulMeshLibraryV20.RockVariant(i),
                    i % 5 == 0 ? palette.EmberStone : palette.Basalt,
                    p, new Vector3(s * 1.25f, s, s * 0.92f),
                    new Vector3(WorldSoulNoiseV20.SignedHash(ScatterSeed, i) * 13f,
                        degrees + 90f, WorldSoulNoiseV20.SignedHash(ScatterSeed ^ 0x6161, i) * 10f));
            }

            for (int i = 0; i < 7; i++)
            {
                float a = (22f + i * 49f) * Mathf.Deg2Rad;
                float r = 14.15f;
                Vector3 p = new Vector3(Mathf.Sin(a) * r, 4.34f, centerZ + Mathf.Cos(a) * r);
                DecorativeBlock($"CraterResidualSignal_{i:00}", zone, p,
                    new Vector3(0.055f, Mathf.Lerp(0.65f, 1.7f, WorldSoulNoiseV20.Hash01(ScatterSeed, 1700 + i)), 0.055f),
                    fractureGlow, new Vector3(0f, -a * Mathf.Rad2Deg, WorldSoulNoiseV20.SignedHash(ScatterSeed, 1800 + i) * 13f));
            }
        }

        private static void BuildDistantCity(
            Transform root,
            WorldSoulMaterialLibraryV20.Palette palette,
            Material warmWindow,
            Material coolWindow)
        {
            Transform zone = Node("WorldSoul_Distant_City", root);
            for (int sideIndex = 0; sideIndex < 2; sideIndex++)
            {
                float side = sideIndex == 0 ? -1f : 1f;
                for (int i = 0; i < 11; i++)
                {
                    float z = Mathf.Lerp(-12f, 125f, i / 10f) + WorldSoulNoiseV20.SignedHash(ScatterSeed, 2000 + sideIndex * 20 + i) * 5f;
                    float x = side * Mathf.Lerp(29f, 47f, WorldSoulNoiseV20.Hash01(ScatterSeed, 2100 + sideIndex * 20 + i));
                    float baseY = RouteElevation(z) - 2.5f;
                    float height = Mathf.Lerp(7f, 18f, WorldSoulNoiseV20.Hash01(ScatterSeed, 2200 + sideIndex * 20 + i));
                    float width = Mathf.Lerp(3.2f, 6.8f, WorldSoulNoiseV20.Hash01(ScatterSeed, 2300 + sideIndex * 20 + i));

                    Transform cluster = Node($"FarCity_{sideIndex}_{i:00}", zone);
                    cluster.position = new Vector3(x, baseY, z);
                    cluster.rotation = Quaternion.Euler(0f, side * WorldSoulNoiseV20.SignedHash(ScatterSeed, 2400 + i) * 11f, 0f);
                    DecorativeBlock("CityMass", cluster, new Vector3(0f, height * 0.5f, 0f),
                        new Vector3(width, height, width * 0.72f), i % 4 == 0 ? palette.WornStone : palette.Basalt, Vector3.zero, false);
                    MeshObject("CitySpire", cluster, ProductionMeshLibraryV09.CathedralSpire(),
                        i % 3 == 0 ? palette.Limestone : palette.Basalt,
                        new Vector3(0f, height, 0f), new Vector3(width * 0.42f, height * 0.62f, width * 0.42f), Vector3.zero, false);

                    Material window = i % 3 == 0 ? warmWindow : coolWindow;
                    for (int w = 0; w < 3; w++)
                    {
                        float wy = height * (0.28f + w * 0.17f);
                        DecorativeBlock($"Window_{w}", cluster,
                            new Vector3(side * width * -0.505f, wy, (w - 1) * width * 0.15f),
                            new Vector3(0.035f, 0.18f, 0.22f), window, Vector3.zero, false);
                    }
                }
            }
        }

        private static void BuildAtmosphericLandmarks(
            Transform root,
            WorldSoulMaterialLibraryV20.Palette palette)
        {
            Transform zone = Node("WorldSoul_Horizon_Landmarks", root);
            Vector3[] positions =
            {
                new Vector3(-38f, 13f, 156f),
                new Vector3(39f, 16f, 165f),
                new Vector3(-52f, 11f, 78f),
                new Vector3(54f, 14f, 103f),
            };
            for (int i = 0; i < positions.Length; i++)
            {
                MeshObject($"HorizonSpire_{i:00}", zone, ProductionMeshLibraryV09.CathedralSpire(),
                    i % 2 == 0 ? palette.Limestone : palette.Basalt,
                    positions[i], new Vector3(6f + i, 22f + i * 3f, 6f + i),
                    new Vector3(0f, i * 21f, WorldSoulNoiseV20.SignedHash(ScatterSeed, 2600 + i) * 3f));
            }
        }

        private static void BuildAncientTree(
            string name,
            Transform parent,
            Vector3 position,
            float height,
            WorldSoulMaterialLibraryV20.Palette palette,
            int seed)
        {
            Transform tree = Node(name, parent);
            tree.position = position;
            float trunkRadius = height * 0.075f;
            DecorativeCylinder("Trunk", tree, new Vector3(0f, height * 0.48f, 0f),
                new Vector3(trunkRadius, height * 0.48f, trunkRadius), palette.Bark,
                new Vector3(WorldSoulNoiseV20.SignedHash(seed, 1) * 3f, 0f, WorldSoulNoiseV20.SignedHash(seed, 2) * 3f), false);

            for (int i = 0; i < 4; i++)
            {
                Vector2 dir2 = WorldSoulNoiseV20.UnitDirection(seed, i + 10);
                Vector3 start = new Vector3(0f, height * Mathf.Lerp(0.48f, 0.72f, i / 3f), 0f);
                Vector3 end = start + new Vector3(dir2.x, Mathf.Lerp(0.55f, 0.95f, WorldSoulNoiseV20.Hash01(seed, 40 + i)), dir2.y) * height * 0.28f;
                CylinderBetween($"Branch_{i:00}", tree, start, end, trunkRadius * 0.52f, palette.Bark);
            }

            for (int i = 0; i < 5; i++)
            {
                Vector2 dir2 = WorldSoulNoiseV20.UnitDirection(seed, i + 60);
                float radius = i == 0 ? 0f : height * Mathf.Lerp(0.12f, 0.28f, WorldSoulNoiseV20.Hash01(seed, 70 + i));
                Vector3 canopyPosition = new Vector3(dir2.x * radius, height * Mathf.Lerp(0.68f, 0.98f, WorldSoulNoiseV20.Hash01(seed, 80 + i)), dir2.y * radius);
                float size = height * Mathf.Lerp(0.23f, 0.34f, WorldSoulNoiseV20.Hash01(seed, 90 + i));
                MeshObject($"Canopy_{i:00}", tree, ProductionMeshLibraryV09.GardenCanopy(),
                    i % 4 == 0 ? palette.Moss : palette.Foliage,
                    canopyPosition, new Vector3(size, size * 0.62f, size),
                    new Vector3(0f, WorldSoulNoiseV20.Hash01(seed, 100 + i) * 360f, 0f), false);
            }
        }

        private static void BuildShrub(
            string name,
            Transform parent,
            Vector3 position,
            WorldSoulMaterialLibraryV20.Palette palette,
            int seed)
        {
            float s = Mathf.Lerp(0.38f, 0.82f, WorldSoulNoiseV20.Hash01(seed, 1));
            MeshObject(name, parent, ProductionMeshLibraryV09.GardenCanopy(), palette.Foliage,
                position + Vector3.up * s * 0.26f,
                new Vector3(s * 1.25f, s * 0.55f, s),
                new Vector3(0f, WorldSoulNoiseV20.Hash01(seed, 2) * 360f, 0f));
        }

        private static void ConfigureAtmosphereAndLighting(
            Transform root,
            WorldSoulMaterialLibraryV20.Palette palette)
        {
            if (palette.Skybox != null) RenderSettings.skybox = palette.Skybox;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.0048f;
            RenderSettings.fogColor = new Color(0.115f, 0.145f, 0.175f);
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.27f, 0.32f, 0.39f);
            RenderSettings.ambientEquatorColor = new Color(0.15f, 0.17f, 0.19f);
            RenderSettings.ambientGroundColor = new Color(0.045f, 0.050f, 0.055f);
            RenderSettings.reflectionIntensity = 0.72f;

            Light key = GameObject.Find("KeyLight")?.GetComponent<Light>();
            if (key != null)
            {
                key.color = new Color(1.0f, 0.89f, 0.75f);
                key.intensity = 1.12f;
                key.shadows = LightShadows.Soft;
                key.shadowStrength = 0.86f;
                key.useColorTemperature = true;
                key.colorTemperature = 5100f;
                key.transform.rotation = Quaternion.Euler(46f, -31f, 0f);
                RenderSettings.sun = key;
            }

            // Few static, shadowless locality lights. These create spatial identity without
            // animation or frame-varying luminance during neural evidence windows.
            CreatePointLight("SanctumWarmth", root, new Vector3(0f, 4.2f, -14f), new Color(1f, 0.54f, 0.25f), 1.25f, 14f);
            CreatePointLight("SanctumAetherFill", root, new Vector3(0f, 3.1f, -4f), new Color(0.16f, 0.56f, 0.86f), 0.82f, 12f);
            CreatePointLight("CausewayMoonFill", root, new Vector3(-7f, 5.0f, 18f), new Color(0.16f, 0.30f, 0.48f), 0.72f, 18f);
            CreatePointLight("MarketHearth", root, new Vector3(0f, 4.0f, 47f), new Color(1f, 0.43f, 0.18f), 0.88f, 15f);
            CreatePointLight("AscentColdFill", root, new Vector3(7f, 8.0f, 73f), new Color(0.14f, 0.31f, 0.55f), 0.72f, 17f);
            CreatePointLight("FractureAmbient", root, new Vector3(0f, 8.5f, 97f), new Color(0.55f, 0.065f, 0.10f), 0.68f, 20f);
        }

        private static void ConfigureStaticRenderers(Transform root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null) continue;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
                renderer.lightProbeUsage = LightProbeUsage.BlendProbes;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.BlendProbesAndSkybox;
                GameObjectUtility.SetStaticEditorFlags(
                    renderer.gameObject,
                    StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.ReflectionProbeStatic);
            }
        }

        private static Material EnsureGlowMaterial(string name, Color color, float intensity)
        {
            string path = $"{WorldSoulMaterialLibraryV20.Root}/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                if (shader == null) throw new InvalidOperationException("V0.20 glow material requires URP/Lit or Standard shader.");
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            Color baseColor = color * 0.18f;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", baseColor);
            else if (material.HasProperty("_Color")) material.SetColor("_Color", baseColor);
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * intensity);
            }
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0.18f);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.68f);
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Transform Node(string name, Transform parent)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        private static GameObject MeshObject(
            string name,
            Transform parent,
            Mesh mesh,
            Material material,
            Vector3? position = null,
            Vector3? scale = null,
            Vector3? euler = null,
            bool worldSpace = true)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            if (worldSpace) go.transform.position = position ?? Vector3.zero;
            else go.transform.localPosition = position ?? Vector3.zero;
            go.transform.localScale = scale ?? Vector3.one;
            go.transform.localRotation = Quaternion.Euler(euler ?? Vector3.zero);
            MeshFilter filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            MeshRenderer renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
            return go;
        }

        private static GameObject DecorativeBlock(
            string name,
            Transform parent,
            Vector3 position,
            Vector3 scale,
            Material material,
            Vector3 euler,
            bool worldSpace = true)
            => DecorativePrimitive(name, PrimitiveType.Cube, parent, position, scale, material, euler, worldSpace);

        private static GameObject DecorativeCylinder(
            string name,
            Transform parent,
            Vector3 position,
            Vector3 scale,
            Material material,
            Vector3 euler,
            bool worldSpace = true)
            => DecorativePrimitive(name, PrimitiveType.Cylinder, parent, position, scale, material, euler, worldSpace);

        private static GameObject DecorativePrimitive(
            string name,
            PrimitiveType primitive,
            Transform parent,
            Vector3 position,
            Vector3 scale,
            Material material,
            Vector3 euler,
            bool worldSpace)
        {
            GameObject go = GameObject.CreatePrimitive(primitive);
            go.name = name;
            go.transform.SetParent(parent, false);
            if (worldSpace) go.transform.position = position;
            else go.transform.localPosition = position;
            go.transform.localScale = scale;
            go.transform.localRotation = Quaternion.Euler(euler);
            Collider collider = go.GetComponent<Collider>();
            if (collider != null) UnityEngine.Object.DestroyImmediate(collider);
            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = material;
            return go;
        }

        private static void CylinderBetween(
            string name,
            Transform parent,
            Vector3 localStart,
            Vector3 localEnd,
            float radius,
            Material material)
        {
            Vector3 delta = localEnd - localStart;
            float length = delta.magnitude;
            if (length <= 0.001f) return;
            GameObject branch = DecorativeCylinder(name, parent,
                (localStart + localEnd) * 0.5f,
                new Vector3(radius, length * 0.5f, radius), material, Vector3.zero, false);
            branch.transform.localRotation = Quaternion.FromToRotation(Vector3.up, delta / length);
        }

        private static Light CreatePointLight(
            string name,
            Transform parent,
            Vector3 position,
            Color color,
            float intensity,
            float range)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            Light light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
            light.renderMode = LightRenderMode.Auto;
            return light;
        }

        private static bool ContainsAny(string value, params string[] needles)
        {
            for (int i = 0; i < needles.Length; i++)
                if (value.IndexOf(needles[i], StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }
    }
}
#endif
