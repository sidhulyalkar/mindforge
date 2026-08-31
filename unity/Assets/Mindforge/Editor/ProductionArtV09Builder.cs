#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mindforge.Editor
{
    /// <summary>
    /// V0.9 is the first explicitly production-art-oriented world pass. The August 30
    /// playthrough showed that adding more primitives was making the world denser without
    /// making it more believable. This builder therefore treats the old geometry as collision
    /// proxies and replaces the visible language with smooth reusable meshes, authored spacing,
    /// coherent PBR materials, daylight, gardens, water, skyline depth and larger-scale
    /// architectural repetition.
    ///
    /// No combat, input, quest, persistence or neural authority is created here.
    /// </summary>
    public static class ProductionArtV09Builder
    {
        public const string RootName = "Mindforge_Production_Art_V09";
        public const string Revision = "PRODUCTION_ART_V09";

        private static readonly StaticEditorFlags VisualStatic =
            StaticEditorFlags.BatchingStatic |
            StaticEditorFlags.OccludeeStatic |
            StaticEditorFlags.ReflectionProbeStatic;

        [MenuItem("Mindforge/Legacy/Showcase/Apply Production Art V0.9", priority = 42)]
        public static void ApplyOpenScene()
        {
            GameObject arena = EditorSceneLookup.FindIncludingInactive("Fractured_Signal_Arena");
            GameObject sanctum = EditorSceneLookup.FindIncludingInactive(SanctumOnboardingV08Builder.RootName);
            GameObject grounded = EditorSceneLookup.FindIncludingInactive(GroundedWorldV1Builder.RootName);
            if (arena == null || sanctum == null || grounded == null)
                throw new InvalidOperationException("Production Art V0.9 requires arena, Sanctum V0.8 and Grounded World V1.");

            GameObject previous = EditorSceneLookup.FindIncludingInactive(RootName);
            if (previous != null) UnityEngine.Object.DestroyImmediate(previous);

            ProductionMaterialAuthoringV09.EnsureAuthored();
            Material ivory = Require(ProductionMaterialAuthoringV09.Ivory);
            Material pearl = Require(ProductionMaterialAuthoringV09.Pearl);
            Material warmStone = Require(ProductionMaterialAuthoringV09.WarmStone);
            Material graphite = Require(ProductionMaterialAuthoringV09.Graphite);
            Material gold = Require(ProductionMaterialAuthoringV09.Gold);
            Material garden = Require(ProductionMaterialAuthoringV09.Garden);
            Material water = Require(ProductionMaterialAuthoringV09.Water);
            Material glass = Require(ProductionMaterialAuthoringV09.Glass);

            Mesh column = ProductionMeshLibraryV09.FlutedColumn();
            Mesh arch = ProductionMeshLibraryV09.PointedArch();
            Mesh spire = ProductionMeshLibraryV09.CathedralSpire();
            Mesh canopy = ProductionMeshLibraryV09.GardenCanopy();

            // V0.7 is useful as a procedural prototype but is visually much too noisy beside
            // the production pass. Keep its source and topology seam, hide only presentation.
            GameObject v07 = EditorSceneLookup.FindIncludingInactive(WorldV07Builder.RootName);
            if (v07 != null) v07.SetActive(false);

            // Keep V0.8 collision and interactions, but stop rendering the blockout shells that
            // are replaced below. Their colliders remain untouched and authoritative.
            int hiddenProxyRenderers = HideSanctumBlockoutRenderers(sanctum.transform);
            int rethemed = RethemeGroundedWorld(ivory, pearl, warmStone, graphite, gold);

            GameObject root = new GameObject(RootName);
            root.transform.SetParent(arena.transform, false);

            Transform nave = Zone(root.transform, "Production_Sanctum_Nave");
            BuildNave(nave, column, arch, spire, ivory, pearl, gold, glass);

            Transform threshold = Zone(root.transform, "Production_Threshold_Facade");
            BuildThresholdFacade(threshold, column, arch, spire, ivory, pearl, gold, glass);

            Transform promenade = Zone(root.transform, "Production_Processional_Promenade");
            BuildPromenade(promenade, column, arch, ivory, warmStone, gold, glass, garden, water, canopy);

            Transform market = Zone(root.transform, "Production_Market_Arcade");
            BuildMarketArcade(market, column, arch, spire, ivory, pearl, graphite, gold, glass, garden, canopy);

            Transform fracture = Zone(root.transform, "Production_Fracture_Landmark");
            BuildFractureLandmark(fracture, column, arch, spire, pearl, graphite, gold, glass);

            Transform cathedral = Zone(root.transform, "Production_Cathedral_Approach");
            BuildCathedralApproach(cathedral, column, arch, spire, ivory, pearl, graphite, gold, glass, garden, canopy);

            Transform skyline = Zone(root.transform, "Production_Skyline");
            BuildSkyline(skyline, column, arch, spire, ivory, pearl, graphite, gold, glass);

            ConfigureDaylight(root.transform);
            BuildReflectionCoverage(root.transform);

            EditorUtility.SetDirty(root);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                $"[Mindforge:V09:Art] Production visual pass ready. Hidden {hiddenProxyRenderers} blockout renderers while preserving their colliders; " +
                $"rethemed {rethemed} legacy world renderers; replaced hero composition with fluted columns, true extruded pointed arches, " +
                "cathedral spires, textured PBR stone/ceramic/metal, daylight, water, gardens, reflection coverage and a deep skyline. " +
                "V0.7 presentation is hidden but its generation seam remains available for future authored-prefab replacement.");
        }

        private static int HideSanctumBlockoutRenderers(Transform sanctum)
        {
            int hidden = 0;
            Renderer[] renderers = sanctum.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null) continue;
                string n = renderer.gameObject.name;
                bool proxy =
                    n.StartsWith("Bay_", StringComparison.OrdinalIgnoreCase) ||
                    n.StartsWith("ThresholdPier_", StringComparison.OrdinalIgnoreCase) ||
                    n.StartsWith("ThresholdGold_", StringComparison.OrdinalIgnoreCase) ||
                    n.StartsWith("TerraceColonnade_", StringComparison.OrdinalIgnoreCase) ||
                    n.IndexOf("Reference_Architecture", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    string.Equals(n, "SanctumBackWall", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(n, "SanctumLeftBoundary", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(n, "SanctumRightBoundary", StringComparison.OrdinalIgnoreCase);
                if (!proxy) continue;
                renderer.enabled = false;
                EditorUtility.SetDirty(renderer);
                hidden++;
            }

            Transform reference = sanctum.Find(SanctumReferenceFidelityV08Builder.RootName);
            if (reference != null)
            {
                Renderer[] referenceRenderers = reference.GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < referenceRenderers.Length; i++)
                {
                    if (referenceRenderers[i] == null || !referenceRenderers[i].enabled) continue;
                    referenceRenderers[i].enabled = false;
                    EditorUtility.SetDirty(referenceRenderers[i]);
                    hidden++;
                }
            }
            return hidden;
        }

        private static int RethemeGroundedWorld(Material ivory, Material pearl, Material warm, Material graphite, Material gold)
        {
            GameObject composition = EditorSceneLookup.FindIncludingInactive(GroundedWorldCompositionV2Builder.RootName);
            if (composition == null) return 0;
            int changed = 0;
            Renderer[] renderers = composition.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || renderer.sharedMaterial == null) continue;
                string materialName = renderer.sharedMaterial.name;
                Material replacement = null;
                if (materialName.IndexOf("AetherCyan", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    materialName.IndexOf("WispVerdant", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    materialName.IndexOf("FracturedRing", StringComparison.OrdinalIgnoreCase) >= 0)
                    continue;
                if (materialName.IndexOf("ArenaBasalt", StringComparison.OrdinalIgnoreCase) >= 0) replacement = warm;
                else if (materialName.IndexOf("Obsidian", StringComparison.OrdinalIgnoreCase) >= 0) replacement = graphite;
                else if (materialName.IndexOf("GuardianMetal", StringComparison.OrdinalIgnoreCase) >= 0) replacement = pearl;
                else if (renderer.gameObject.name.IndexOf("Signal", StringComparison.OrdinalIgnoreCase) >= 0) replacement = gold;
                else if (renderer.gameObject.name.IndexOf("Stair", StringComparison.OrdinalIgnoreCase) >= 0) replacement = warm;
                if (replacement == null || replacement == renderer.sharedMaterial) continue;
                renderer.sharedMaterial = replacement;
                EditorUtility.SetDirty(renderer);
                changed++;
            }
            return changed;
        }

        private static void BuildNave(Transform root, Mesh column, Mesh arch, Mesh spire, Material ivory, Material pearl, Material gold, Material glass)
        {
            float[] bays = { -60f, -54f, -48f, -42f };
            for (int i = 0; i < bays.Length; i++)
            {
                float z = bays[i];
                for (int side = -1; side <= 1; side += 2)
                {
                    float x = side * 11.1f;
                    MeshPart($"NaveColumn_{i}_{side}", root, column, new Vector3(x, 5.2f, z), new Vector3(1.35f, 10.4f, 1.35f), i % 2 == 0 ? ivory : pearl);
                    RingPlinth($"NaveColumnBase_{i}_{side}", root, new Vector3(x, 0.16f, z), 1.18f, 0.28f, 32, gold);
                    MeshPart($"NaveColumnSpire_{i}_{side}", root, spire, new Vector3(x, 10.32f, z), new Vector3(1.25f, 2.4f, 1.25f), pearl);
                    GlassPanel($"NaveWindow_{i}_{side}", root, new Vector3(side * 14.78f, 6.4f, z + 1.55f), new Vector3(0.08f, 5.8f, 2.65f), glass, new Vector3(0f, side * 1.5f, 0f));
                }

                MeshPart($"NaveArchStone_{i}", root, arch, new Vector3(0f, 5.5f, z), new Vector3(6.45f, 5.7f, 1.8f), ivory);
                MeshPart($"NaveArchGold_{i}", root, arch, new Vector3(0f, 5.54f, z - 0.09f), new Vector3(6.05f, 5.34f, 1.93f), gold);
                MeshPart($"NaveArchPearl_{i}", root, arch, new Vector3(0f, 5.58f, z - 0.17f), new Vector3(5.72f, 5.05f, 2.08f), pearl);
            }

            // Long clerestory beams sit high enough to preserve the aerial movement volume.
            for (int side = -1; side <= 1; side += 2)
            {
                SmoothBeam($"ClerestoryRail_{side}", root, new Vector3(side * 12.8f, 10.3f, -51f), new Vector3(0.42f, 0.48f, 23.5f), pearl);
                SmoothBeam($"ClerestoryGold_{side}", root, new Vector3(side * 12.48f, 9.85f, -51f), new Vector3(0.08f, 0.12f, 22.7f), gold);
            }
        }

        private static void BuildThresholdFacade(Transform root, Mesh column, Mesh arch, Mesh spire, Material ivory, Material pearl, Material gold, Material glass)
        {
            const float z = -38.35f;
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * 7.05f;
                MeshPart($"ThresholdColumn_{side}", root, column, new Vector3(x, 5.2f, z), new Vector3(1.65f, 10.4f, 1.65f), pearl);
                MeshPart($"ThresholdSpire_{side}", root, spire, new Vector3(x, 10.15f, z), new Vector3(1.55f, 4.8f, 1.55f), ivory);
                MeshPart($"ThresholdOuterSpire_{side}", root, spire, new Vector3(side * 10.1f, 5.0f, z + 0.3f), new Vector3(1.2f, 7.2f, 1.2f), pearl);
                GlassPanel($"ThresholdGlassWing_{side}", root, new Vector3(side * 8.7f, 6.1f, z + 0.42f), new Vector3(2.0f, 7.4f, 0.11f), glass, new Vector3(0f, 0f, side * -7f));
            }
            MeshPart("ThresholdHeroArch", root, arch, new Vector3(0f, 4.95f, z), new Vector3(7.1f, 6.7f, 2.4f), ivory);
            MeshPart("ThresholdHeroGold", root, arch, new Vector3(0f, 5.02f, z - 0.12f), new Vector3(6.55f, 6.16f, 2.52f), gold);
            MeshPart("ThresholdHeroInner", root, arch, new Vector3(0f, 5.08f, z - 0.24f), new Vector3(6.12f, 5.75f, 2.65f), pearl);
        }

        private static void BuildPromenade(Transform root, Mesh column, Mesh arch, Material ivory, Material warm, Material gold, Material glass, Material garden, Material water, Mesh canopy)
        {
            // This is a real city-scale promenade: broad movement lane, separate garden/water
            // margins, and repeated structures far enough apart to create readable parallax.
            SmoothBeam("PromenadeRoad", root, new Vector3(0f, -0.36f, 2f), new Vector3(9.8f, 0.22f, 40f), warm);
            SmoothBeam("PromenadeGoldSpine", root, new Vector3(0f, -0.22f, 2f), new Vector3(0.10f, 0.035f, 39f), gold);
            for (int side = -1; side <= 1; side += 2)
            {
                SmoothBeam($"PromenadeWalk_{side}", root, new Vector3(side * 6.25f, -0.30f, 2f), new Vector3(2.6f, 0.17f, 40f), ivory);
                SmoothBeam($"PromenadeCanal_{side}", root, new Vector3(side * 9.2f, -0.16f, 2f), new Vector3(2.5f, 0.08f, 40f), water);
                for (int i = 0; i < 7; i++)
                {
                    float z = -15f + i * 6.0f;
                    CreateTree($"PromenadeTree_{side}_{i}", root, new Vector3(side * 11.3f, 0f, z), 0.9f + (i % 3) * 0.12f, garden, gold, canopy);
                    MeshPart($"PromenadeColumn_{side}_{i}", root, column, new Vector3(side * 13.6f, 3.0f, z), new Vector3(0.85f, 6.0f, 0.85f), i % 2 == 0 ? ivory : warm);
                    if (i % 2 == 0)
                        MeshPart($"PromenadeArch_{side}_{i}", root, arch, new Vector3(side * 13.6f, 5.2f, z + 2.9f), new Vector3(2.9f, 3.0f, 1.1f), gold, new Vector3(0f, 90f, 0f));
                }
            }
        }

        private static void BuildMarketArcade(Transform root, Mesh column, Mesh arch, Mesh spire, Material ivory, Material pearl, Material graphite, Material gold, Material glass, Material garden, Mesh canopy)
        {
            Vector3 center = new Vector3(26.5f, 0f, -29f);
            for (int corner = 0; corner < 4; corner++)
            {
                float sx = corner % 2 == 0 ? -1f : 1f;
                float sz = corner < 2 ? -1f : 1f;
                Vector3 p = center + new Vector3(sx * 5.6f, 3.2f, sz * 4.7f);
                MeshPart($"MarketColumn_{corner}", root, column, p, new Vector3(1.05f, 6.4f, 1.05f), pearl);
                MeshPart($"MarketSpire_{corner}", root, spire, p + Vector3.up * 3.1f, new Vector3(0.9f, 2.7f, 0.9f), graphite);
                CreateTree($"MarketTree_{corner}", root, center + new Vector3(sx * 8.0f, 0f, sz * 6.7f), 0.78f, garden, gold, canopy);
            }
            MeshPart("MarketNorthArch", root, arch, center + new Vector3(0f, 4.0f, -4.7f), new Vector3(5.2f, 4.0f, 1.35f), ivory);
            MeshPart("MarketSouthArch", root, arch, center + new Vector3(0f, 4.0f, 4.7f), new Vector3(5.2f, 4.0f, 1.35f), ivory, new Vector3(0f, 180f, 0f));
            MeshPart("MarketEastArch", root, arch, center + new Vector3(5.6f, 4.0f, 0f), new Vector3(4.3f, 4.0f, 1.35f), gold, new Vector3(0f, 90f, 0f));
            MeshPart("MarketWestArch", root, arch, center + new Vector3(-5.6f, 4.0f, 0f), new Vector3(4.3f, 4.0f, 1.35f), gold, new Vector3(0f, -90f, 0f));
            GlassPanel("MarketCanopyGlass", root, center + new Vector3(0f, 7.6f, 0f), new Vector3(7.2f, 0.09f, 5.8f), glass, new Vector3(0f, 0f, 0f));
        }

        private static void BuildFractureLandmark(Transform root, Mesh column, Mesh arch, Mesh spire, Material pearl, Material graphite, Material gold, Material glass)
        {
            Vector3 center = new Vector3(-28.2f, 0f, -18f);
            for (int i = 0; i < 6; i++)
            {
                float a = i / 6f * Mathf.PI * 2f;
                Vector3 p = center + new Vector3(Mathf.Cos(a) * 3.2f, 3.7f, Mathf.Sin(a) * 3.2f);
                MeshPart($"FractureColumn_{i}", root, column, p, new Vector3(0.8f, 7.4f, 0.8f), i % 2 == 0 ? pearl : graphite);
                MeshPart($"FractureSpire_{i}", root, spire, p + Vector3.up * 3.5f, new Vector3(0.72f, 3.2f + (i % 3) * 0.5f, 0.72f), gold);
            }
            MeshPart("FractureArchNorth", root, arch, center + new Vector3(0f, 5.3f, 3.0f), new Vector3(3.4f, 4.2f, 1.4f), pearl);
            MeshPart("FractureArchEast", root, arch, center + new Vector3(3.0f, 5.3f, 0f), new Vector3(3.4f, 4.2f, 1.4f), pearl, new Vector3(0f, 90f, 0f));
            GlassPanel("FractureCoreGlass", root, center + new Vector3(0f, 4.8f, 0f), new Vector3(1.8f, 7.0f, 1.8f), glass, new Vector3(0f, 45f, 0f));
        }

        private static void BuildCathedralApproach(Transform root, Mesh column, Mesh arch, Mesh spire, Material ivory, Material pearl, Material graphite, Material gold, Material glass, Material garden, Mesh canopy)
        {
            Vector3 center = new Vector3(29.5f, 0f, -8.8f);
            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < 4; i++)
                {
                    float z = -17f + i * 5.0f;
                    float x = center.x + side * 5.4f;
                    MeshPart($"CathedralColumn_{side}_{i}", root, column, new Vector3(x, 4.6f, z), new Vector3(1.12f, 9.2f, 1.12f), i % 2 == 0 ? ivory : pearl);
                    MeshPart($"CathedralSpire_{side}_{i}", root, spire, new Vector3(x, 9.0f, z), new Vector3(1.0f, 4.0f + i * 0.4f, 1.0f), i == 3 ? gold : graphite);
                }
                CreateTree($"CathedralGarden_{side}_A", root, new Vector3(center.x + side * 8.6f, 0f, -12f), 1.1f, garden, gold, canopy);
                CreateTree($"CathedralGarden_{side}_B", root, new Vector3(center.x + side * 8.8f, 0f, -2f), 1.28f, garden, gold, canopy);
            }
            MeshPart("CathedralGateOuter", root, arch, center + new Vector3(0f, 6.2f, 3.8f), new Vector3(6.0f, 6.2f, 2.2f), ivory);
            MeshPart("CathedralGateGold", root, arch, center + new Vector3(0f, 6.28f, 3.68f), new Vector3(5.55f, 5.75f, 2.32f), gold);
            MeshPart("CathedralGateInner", root, arch, center + new Vector3(0f, 6.36f, 3.55f), new Vector3(5.12f, 5.32f, 2.45f), pearl);
            GlassPanel("CathedralRoseGlass", root, center + new Vector3(0f, 11.7f, 4.1f), new Vector3(5.0f, 4.5f, 0.10f), glass, Vector3.zero);
        }

        private static void BuildSkyline(Transform root, Mesh column, Mesh arch, Mesh spire, Material ivory, Material pearl, Material graphite, Material gold, Material glass)
        {
            Vector3[] towers =
            {
                new Vector3(-32f, 0f, 58f), new Vector3(-18f, 0f, 72f), new Vector3(0f, 0f, 86f),
                new Vector3(19f, 0f, 69f), new Vector3(34f, 0f, 53f), new Vector3(-6f, 0f, 58f),
            };
            for (int i = 0; i < towers.Length; i++)
            {
                float height = 18f + (i % 3) * 7f;
                Vector3 p = towers[i];
                MeshPart($"SkyTower_{i}_Core", root, column, p + Vector3.up * height * 0.5f, new Vector3(3.6f + (i % 2), height, 3.6f + (i % 2)), i % 2 == 0 ? pearl : graphite);
                MeshPart($"SkyTower_{i}_Spire", root, spire, p + Vector3.up * height, new Vector3(3.1f, 10f + i * 0.7f, 3.1f), i == 2 ? gold : ivory);
                if (i % 2 == 0)
                    MeshPart($"SkyTower_{i}_Arch", root, arch, p + new Vector3(0f, height * 0.55f, -1.9f), new Vector3(2.7f, 4.2f, 1.3f), gold);
                GlassPanel($"SkyTower_{i}_Glass", root, p + new Vector3(0f, height * 0.58f, -2.1f), new Vector3(2.7f, 5.6f, 0.08f), glass, Vector3.zero);
            }
        }

        private static void ConfigureDaylight(Transform root)
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.56f, 0.69f, 0.86f);
            RenderSettings.ambientEquatorColor = new Color(0.34f, 0.42f, 0.50f);
            RenderSettings.ambientGroundColor = new Color(0.17f, 0.20f, 0.22f);
            RenderSettings.ambientIntensity = 1.15f;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.58f, 0.72f, 0.84f);
            RenderSettings.fogStartDistance = 92f;
            RenderSettings.fogEndDistance = 320f;

            Material sky = EnsureProceduralSky();
            RenderSettings.skybox = sky;

            Light sun = RenderSettings.sun;
            if (sun == null)
            {
                GameObject go = new GameObject("ProductionSunV09");
                go.transform.SetParent(root, false);
                go.transform.rotation = Quaternion.Euler(38f, -34f, 0f);
                sun = go.AddComponent<Light>();
                sun.type = LightType.Directional;
                RenderSettings.sun = sun;
            }
            sun.color = new Color(1f, 0.93f, 0.79f);
            sun.intensity = 1.28f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.86f;
            sun.shadowBias = 0.035f;
            sun.shadowNormalBias = 0.28f;
            sun.transform.rotation = Quaternion.Euler(42f, -38f, 0f);
            EditorUtility.SetDirty(sun);
        }

        private static Material EnsureProceduralSky()
        {
            string path = ProductionMaterialAuthoringV09.Root + "/ProductionSkyV09.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Skybox/Procedural");
                if (shader == null) return RenderSettings.skybox;
                material = new Material(shader) { name = "ProductionSkyV09" };
                AssetDatabase.CreateAsset(material, path);
            }
            if (material.HasProperty("_SkyTint")) material.SetColor("_SkyTint", new Color(0.34f, 0.62f, 0.91f));
            if (material.HasProperty("_GroundColor")) material.SetColor("_GroundColor", new Color(0.52f, 0.57f, 0.57f));
            if (material.HasProperty("_AtmosphereThickness")) material.SetFloat("_AtmosphereThickness", 0.72f);
            if (material.HasProperty("_Exposure")) material.SetFloat("_Exposure", 1.24f);
            if (material.HasProperty("_SunSize")) material.SetFloat("_SunSize", 0.035f);
            if (material.HasProperty("_SunSizeConvergence")) material.SetFloat("_SunSizeConvergence", 4f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void BuildReflectionCoverage(Transform root)
        {
            CreateProbe(root, "Probe_Sanctum", new Vector3(0f, 5f, -50f), new Vector3(30f, 12f, 26f));
            CreateProbe(root, "Probe_Threshold", new Vector3(0f, 5f, -26f), new Vector3(30f, 14f, 22f));
            CreateProbe(root, "Probe_Market", new Vector3(26f, 5f, -29f), new Vector3(22f, 14f, 20f));
            CreateProbe(root, "Probe_Cathedral", new Vector3(29f, 7f, -7f), new Vector3(24f, 18f, 24f));
        }

        private static void CreateProbe(Transform parent, string name, Vector3 position, Vector3 size)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            ReflectionProbe probe = go.AddComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Realtime;
            probe.refreshMode = ReflectionProbeRefreshMode.OnAwake;
            probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.IndividualFaces;
            probe.resolution = 128;
            probe.size = size;
            probe.boxProjection = true;
            probe.intensity = 0.78f;
            probe.importance = 2;
        }

        private static void CreateTree(string name, Transform parent, Vector3 position, float scale, Material leaves, Material trunk, Mesh canopy)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = position;
            Mesh column = ProductionMeshLibraryV09.FlutedColumn();
            MeshPart("Trunk", root.transform, column, new Vector3(0f, 1.6f * scale, 0f), new Vector3(0.24f * scale, 3.2f * scale, 0.24f * scale), trunk);
            MeshPart("CanopyLow", root.transform, canopy, new Vector3(0f, 3.2f * scale, 0f), new Vector3(1.35f * scale, 2.0f * scale, 1.35f * scale), leaves);
            MeshPart("CanopyHigh", root.transform, canopy, new Vector3(0f, 4.45f * scale, 0f), new Vector3(0.92f * scale, 1.65f * scale, 0.92f * scale), leaves);
        }

        private static void RingPlinth(string name, Transform parent, Vector3 position, float diameter, float height, int segments, Material material)
        {
            // A high-segment cylinder is acceptable here because it is a tiny supporting
            // shape rather than the primary architecture silhouette.
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = new Vector3(diameter, height * 0.5f, diameter);
            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = material;
            Collider collider = go.GetComponent<Collider>();
            if (collider != null) UnityEngine.Object.DestroyImmediate(collider);
            GameObjectUtility.SetStaticEditorFlags(go, VisualStatic);
        }

        private static GameObject SmoothBeam(string name, Transform parent, Vector3 position, Vector3 scale, Material material, Vector3? euler = null)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            if (euler.HasValue) go.transform.localRotation = Quaternion.Euler(euler.Value);
            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }
            Collider collider = go.GetComponent<Collider>();
            if (collider != null) UnityEngine.Object.DestroyImmediate(collider);
            GameObjectUtility.SetStaticEditorFlags(go, VisualStatic);
            return go;
        }

        private static void GlassPanel(string name, Transform parent, Vector3 position, Vector3 scale, Material material, Vector3 euler)
            => SmoothBeam(name, parent, position, scale, material, euler);

        private static GameObject MeshPart(string name, Transform parent, Mesh mesh, Vector3 position, Vector3 scale, Material material, Vector3? euler = null)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            if (euler.HasValue) go.transform.localRotation = Quaternion.Euler(euler.Value);
            MeshFilter filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            MeshRenderer renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
            GameObjectUtility.SetStaticEditorFlags(go, VisualStatic);
            return go;
        }

        private static Transform Zone(Transform parent, string name)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        private static Material Require(string name)
        {
            Material material = ProductionMaterialAuthoringV09.Load(name);
            if (material == null) throw new InvalidOperationException("Missing production material: " + name);
            return material;
        }
    }
}
#endif
