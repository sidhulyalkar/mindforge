#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mindforge.Editor
{
    /// <summary>
    /// Final V0.9 art-direction pass: break procedural symmetry with a deliberately small
    /// amount of district-specific repair, fracture, suspended infrastructure and lived-in
    /// detail. Everything produced here is visual-only and is kept outside protected movement
    /// / interaction corridors by explicit authoring clearances.
    /// </summary>
    public static class ProductionWorldStorytellingV09Builder
    {
        public const string RootName = "Production_District_Storytelling_V09";
        public const int MaxStoryRenderers = 56;
        public const int MaxStoryLights = 0;

        private static readonly StaticEditorFlags VisualStatic =
            StaticEditorFlags.BatchingStatic |
            StaticEditorFlags.OccludeeStatic |
            StaticEditorFlags.ReflectionProbeStatic;

        [MenuItem("Mindforge/Showcase/Apply District Storytelling V0.9", priority = 44)]
        public static void ApplyOpenScene()
        {
            GameObject production = EditorSceneLookup.FindIncludingInactive(ProductionArtV09Builder.RootName);
            if (production == null)
                throw new InvalidOperationException("District storytelling requires the V0.9 production-art root.");

            Transform previous = production.transform.Find(RootName);
            if (previous != null) UnityEngine.Object.DestroyImmediate(previous.gameObject);

            ProductionMaterialAuthoringV09.EnsureAuthored();
            Material ivory = Require(ProductionMaterialAuthoringV09.Ivory);
            Material pearl = Require(ProductionMaterialAuthoringV09.Pearl);
            Material warm = Require(ProductionMaterialAuthoringV09.WarmStone);
            Material graphite = Require(ProductionMaterialAuthoringV09.Graphite);
            Material gold = Require(ProductionMaterialAuthoringV09.Gold);
            Material garden = Require(ProductionMaterialAuthoringV09.Garden);
            Material fracture = CinematicMaterialAuthoring.Load("FracturedCore") ?? graphite;

            Mesh slab = ProductionStoryMeshLibraryV09.BrokenSlab();
            Mesh shard = ProductionStoryMeshLibraryV09.SignalShard();
            Mesh ribbon = ProductionStoryMeshLibraryV09.HangingRibbon();
            Mesh cable = ProductionStoryMeshLibraryV09.CableArc();

            GameObject rootGo = new GameObject(RootName);
            rootGo.transform.SetParent(production.transform, false);
            Transform root = rootGo.transform;

            Transform sanctum = Zone(root, "Story_Sanctum_Repair");
            BuildSanctumRepair(sanctum, slab, ribbon, cable, ivory, pearl, graphite, gold);
            AddCullOnlyLod(sanctum, 0.0065f);

            Transform promenade = Zone(root, "Story_Promenade_LivedIn");
            BuildPromenadeLife(promenade, slab, ribbon, cable, warm, graphite, gold, garden);
            AddCullOnlyLod(promenade, 0.0055f);

            Transform market = Zone(root, "Story_Market_Trade");
            BuildMarketTrade(market, slab, ribbon, cable, pearl, warm, graphite, gold);
            AddCullOnlyLod(market, 0.0055f);

            Transform fractureRoot = Zone(root, "Story_Fracture_Damage");
            BuildFractureDamage(fractureRoot, slab, shard, cable, graphite, gold, fracture);
            AddCullOnlyLod(fractureRoot, 0.0060f);

            Transform cathedral = Zone(root, "Story_Cathedral_Repair");
            BuildCathedralRepair(cathedral, slab, ribbon, cable, ivory, pearl, graphite, gold);
            AddCullOnlyLod(cathedral, 0.0055f);

            OptimizeSkyline(production.transform);
            ValidatePresentationOnly(rootGo);

            EditorUtility.SetDirty(rootGo);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();

            Renderer[] storyRenderers = rootGo.GetComponentsInChildren<Renderer>(true);
            Debug.Log(
                $"[Mindforge:V09:Story] Authored district history ready: renderers={storyRenderers.Length}/{MaxStoryRenderers}; " +
                "protected traversal corridors remain empty; distant skyline no longer casts realtime shadows; no light, collider, Rigidbody, input, combat, persistence or BCI authority added.");
        }

        private static void BuildSanctumRepair(
            Transform root, Mesh slab, Mesh ribbon, Mesh cable,
            Material ivory, Material pearl, Material graphite, Material gold)
        {
            // The pristine nave gets two intentionally unequal repair stories on its outer edge.
            Part("Sanctum_Left_SettledSlab_A", root, slab, new Vector3(-12.8f, 0.55f, -54.5f), new Vector3(2.8f, 1.2f, 1.6f), warmOr(ivory), new Vector3(11f, -18f, 7f));
            Part("Sanctum_Left_SettledSlab_B", root, slab, new Vector3(-13.5f, 1.05f, -51.4f), new Vector3(1.9f, 0.9f, 1.2f), graphite, new Vector3(-23f, 31f, 18f));
            Part("Sanctum_Right_VotiveRibbon", root, ribbon, new Vector3(12.55f, 6.25f, -47.7f), new Vector3(1.25f, 5.2f, 1f), pearl, new Vector3(0f, -8f, 0f), false);
            Part("Sanctum_Right_GoldMend", root, cable, new Vector3(12.7f, 5.7f, -52.0f), new Vector3(4.4f, 2.1f, 1.0f), gold, new Vector3(0f, 90f, 8f), false);
            Part("Sanctum_Left_GraphiteMend", root, cable, new Vector3(-12.75f, 7.4f, -45.2f), new Vector3(3.2f, 1.5f, 1.0f), graphite, new Vector3(0f, 88f, -10f), false);
        }

        private static void BuildPromenadeLife(
            Transform root, Mesh slab, Mesh ribbon, Mesh cable,
            Material warm, Material graphite, Material gold, Material garden)
        {
            // Keep the whole 20m central aerial/movement ribbon clean. Story detail lives at
            // the colonnade/garden edge and changes side rather than mirroring mechanically.
            Part("Promenade_West_RestSlab", root, slab, new Vector3(-12.3f, 0.42f, -8.0f), new Vector3(2.6f, 0.85f, 1.25f), warm, new Vector3(0f, 18f, 4f));
            Part("Promenade_East_RestSlab", root, slab, new Vector3(12.45f, 0.38f, 10.7f), new Vector3(2.2f, 0.75f, 1.1f), warm, new Vector3(0f, -29f, -3f));
            Part("Promenade_West_GrowthTie", root, cable, new Vector3(-13.35f, 4.9f, 7.2f), new Vector3(5.8f, 2.4f, 1f), garden, new Vector3(0f, 90f, 14f), false);
            Part("Promenade_East_ServiceCable", root, cable, new Vector3(13.45f, 6.2f, -2.5f), new Vector3(6.8f, 2.8f, 1f), graphite, new Vector3(0f, 90f, -8f), false);
            Part("Promenade_East_Waycloth", root, ribbon, new Vector3(13.2f, 5.8f, 16.1f), new Vector3(0.95f, 3.7f, 1f), gold, new Vector3(0f, -12f, 0f), false);
        }

        private static void BuildMarketTrade(
            Transform root, Mesh slab, Mesh ribbon, Mesh cable,
            Material pearl, Material warm, Material graphite, Material gold)
        {
            Vector3 c = new Vector3(26.5f, 0f, -29f);
            Part("Market_NorthWest_CounterRemnant", root, slab, c + new Vector3(-7.5f, 0.55f, -5.9f), new Vector3(2.8f, 1.05f, 1.6f), warm, new Vector3(4f, 24f, -8f));
            Part("Market_SouthEast_CounterRemnant", root, slab, c + new Vector3(7.8f, 0.48f, 5.3f), new Vector3(2.1f, 0.8f, 1.35f), graphite, new Vector3(-7f, -31f, 5f));
            Part("Market_West_TradeBanner", root, ribbon, c + new Vector3(-6.9f, 5.4f, 1.8f), new Vector3(1.35f, 4.4f, 1f), pearl, new Vector3(0f, 78f, 0f), false);
            Part("Market_East_TradeBanner", root, ribbon, c + new Vector3(7.1f, 4.8f, -2.3f), new Vector3(0.9f, 3.4f, 1f), gold, new Vector3(0f, -84f, 0f), false);
            Part("Market_North_ServiceCable", root, cable, c + new Vector3(0.8f, 7.2f, -5.9f), new Vector3(8.6f, 2.2f, 1f), graphite, new Vector3(0f, 0f, 1f), false);
            Part("Market_South_ServiceCable", root, cable, c + new Vector3(-1.4f, 6.5f, 5.8f), new Vector3(6.7f, 1.9f, 1f), gold, new Vector3(0f, 180f, -4f), false);
        }

        private static void BuildFractureDamage(
            Transform root, Mesh slab, Mesh shard, Mesh cable,
            Material graphite, Material gold, Material fracture)
        {
            Vector3 c = new Vector3(-28.2f, 0f, -18f);
            Vector3[] offsets =
            {
                new Vector3(-5.2f, 1.4f, -1.0f),
                new Vector3( 4.7f, 2.2f, -2.4f),
                new Vector3(-2.6f, 3.6f,  4.8f),
                new Vector3( 1.8f, 5.0f,  5.2f),
                new Vector3( 5.5f, 1.1f,  1.6f),
                new Vector3(-4.4f, 4.4f,  3.0f),
            };
            for (int i = 0; i < offsets.Length; i++)
            {
                Vector3 p = c + offsets[i];
                Part($"Fracture_OrbitShard_{i}", root, shard, p,
                    new Vector3(0.65f + (i % 3) * 0.16f, 1.7f + (i % 2) * 0.55f, 0.65f),
                    i == 1 || i == 3 ? fracture : (i % 2 == 0 ? graphite : gold),
                    new Vector3(13f * i, 31f * i + 8f, 17f - 9f * i), false);
            }
            Part("Fracture_FallenOuterSlab", root, slab, c + new Vector3(-5.4f, 0.44f, -4.1f), new Vector3(3.0f, 0.95f, 1.4f), graphite, new Vector3(18f, 43f, 11f));
            Part("Fracture_TornConduit", root, cable, c + new Vector3(5.0f, 5.8f, 3.0f), new Vector3(6.8f, 3.2f, 1f), fracture, new Vector3(0f, 43f, 28f), false);
        }

        private static void BuildCathedralRepair(
            Transform root, Mesh slab, Mesh ribbon, Mesh cable,
            Material ivory, Material pearl, Material graphite, Material gold)
        {
            Vector3 c = new Vector3(29.5f, 0f, -8.8f);
            Part("Cathedral_West_ProcessionalBanner", root, ribbon, c + new Vector3(-8.0f, 7.2f, 0.5f), new Vector3(1.3f, 5.6f, 1f), pearl, new Vector3(0f, 83f, 0f), false);
            Part("Cathedral_East_NarrowBanner", root, ribbon, c + new Vector3(8.25f, 8.5f, -5.8f), new Vector3(0.92f, 4.4f, 1f), gold, new Vector3(0f, -86f, 0f), false);
            Part("Cathedral_West_RepairCable", root, cable, c + new Vector3(-7.8f, 10.2f, -8.0f), new Vector3(5.6f, 2.6f, 1f), gold, new Vector3(0f, 90f, 18f), false);
            Part("Cathedral_East_ServiceCable", root, cable, c + new Vector3(7.9f, 6.8f, 1.6f), new Vector3(4.7f, 2.1f, 1f), graphite, new Vector3(0f, 91f, -12f), false);
            Part("Cathedral_West_FallenCap", root, slab, c + new Vector3(-8.4f, 0.52f, -12.8f), new Vector3(2.7f, 1.0f, 1.45f), ivory, new Vector3(9f, -21f, 14f));
            Part("Cathedral_East_FallenCap", root, slab, c + new Vector3(8.7f, 0.46f, 5.1f), new Vector3(2.1f, 0.82f, 1.25f), graphite, new Vector3(-12f, 34f, -9f));
        }

        private static Material warmOr(Material fallback) => fallback;

        private static Transform Zone(Transform parent, string name)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            GameObjectUtility.SetStaticEditorFlags(go, VisualStatic);
            return go.transform;
        }

        private static MeshRenderer Part(
            string name, Transform parent, Mesh mesh, Vector3 position, Vector3 scale,
            Material material, Vector3 euler, bool castShadows = true)
        {
            AssertOutsideProtectedTransit(position, name);
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = scale;
            GameObjectUtility.SetStaticEditorFlags(go, VisualStatic);

            MeshFilter filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            MeshRenderer renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            renderer.receiveShadows = castShadows;
            renderer.lightProbeUsage = LightProbeUsage.BlendProbes;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.BlendProbes;
            return renderer;
        }

        private static void AddCullOnlyLod(Transform district, float transitionHeight)
        {
            Renderer[] renderers = district.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return;
            LODGroup lod = district.gameObject.AddComponent<LODGroup>();
            lod.fadeMode = LODFadeMode.None;
            lod.animateCrossFading = false;
            lod.SetLODs(new[] { new LOD(transitionHeight, renderers) });
            lod.RecalculateBounds();
        }

        private static void OptimizeSkyline(Transform production)
        {
            Transform skyline = production.Find("Production_Skyline");
            if (skyline == null) return;
            Renderer[] renderers = skyline.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null) continue;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                EditorUtility.SetDirty(renderer);
            }
        }

        private static void ValidatePresentationOnly(GameObject root)
        {
            int rendererCount = root.GetComponentsInChildren<Renderer>(true).Length;
            int colliderCount = root.GetComponentsInChildren<Collider>(true).Length;
            int bodyCount = root.GetComponentsInChildren<Rigidbody>(true).Length;
            int lightCount = root.GetComponentsInChildren<Light>(true).Length;

            if (rendererCount > MaxStoryRenderers)
                throw new UnityEditor.Build.BuildFailedException(
                    $"V0.9 storytelling renderer budget exceeded: {rendererCount}>{MaxStoryRenderers}.");
            if (colliderCount != 0 || bodyCount != 0 || lightCount != MaxStoryLights)
                throw new UnityEditor.Build.BuildFailedException(
                    $"V0.9 storytelling must remain presentation-only: colliders={colliderCount}, rigidbodies={bodyCount}, lights={lightCount}.");
        }

        private static void AssertOutsideProtectedTransit(Vector3 p, string name)
        {
            bool sanctumAisle = Mathf.Abs(p.x) < 10.0f && p.z >= -65f && p.z <= -34f;
            bool promenade = Mathf.Abs(p.x) < 10.2f && p.z >= -22f && p.z <= 25f;
            bool marketCore = DistanceXZ(p, new Vector3(26.5f, 0f, -29f)) < 6.2f;
            bool fractureCore = DistanceXZ(p, new Vector3(-28.2f, 0f, -18f)) < 4.5f;
            bool cathedralAisle = Mathf.Abs(p.x - 29.5f) < 6.5f && p.z >= -20f && p.z <= 3f;
            if (sanctumAisle || promenade || marketCore || fractureCore || cathedralAisle)
                throw new InvalidOperationException($"Decorative story prop '{name}' entered a protected traversal/interaction clearance at {p}.");
        }

        private static float DistanceXZ(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        private static Material Require(string materialName)
        {
            Material material = ProductionMaterialAuthoringV09.Load(materialName);
            if (material == null)
                throw new InvalidOperationException($"Missing production material '{materialName}'.");
            return material;
        }
    }
}
#endif
