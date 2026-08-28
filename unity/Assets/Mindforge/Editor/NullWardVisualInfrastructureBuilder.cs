#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Mindforge.Combat;
using Mindforge.Journey;

namespace Mindforge.Editor
{
    /// <summary>
    /// Additive visual-infrastructure pass for the shipping Null Ward scene.
    ///
    /// It creates collider-free modular detail and authored-art anchors and marks only
    /// proven static architectural MeshRenderers for Unity static batching/occlusion.
    /// Gameplay actors, gates, Echoes, LineRenderers and particle renderers are excluded.
    /// Shared URP materials are not mutated by this pass.
    /// </summary>
    public static class NullWardVisualInfrastructureBuilder
    {
        public const string DetailRootName = "Mindforge_NullWard_StaticDetail_V2";

        private static readonly StaticEditorFlags EnvironmentStaticFlags =
            StaticEditorFlags.BatchingStatic |
            StaticEditorFlags.OccluderStatic |
            StaticEditorFlags.OccludeeStatic |
            StaticEditorFlags.ReflectionProbeStatic;

        [MenuItem("Mindforge/Showcase/Apply Null Ward Visual Infrastructure V2", priority = 25)]
        public static void ApplyOpenScene()
        {
            GameObject ward = EditorSceneLookup.FindIncludingInactive(NullWardSceneBuilder.RootName);
            if (ward == null)
                throw new InvalidOperationException("Build the Null Ward before applying visual infrastructure V2.");

            CreateArtAnchors(ward.transform);
            BuildStaticDetail(ward.transform);
            OptimizeStaticScenery(ward.transform);

            EditorUtility.SetDirty(ward);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            Debug.Log(
                "[Mindforge:NullWardVisualV2] Static detail, production-art anchors and " +
                "static batching/occlusion flags applied without changing gameplay, shared material assets or BCI authority.");
        }

        private static void CreateArtAnchors(Transform ward)
        {
            EnsureAnchor(ward, "NullWard_ArtAnchor_MemoryForge", new Vector3(0f, 0f, -57f));
            EnsureAnchor(ward, "NullWard_ArtAnchor_Causeway", new Vector3(0f, 0f, -44.2f));
            EnsureAnchor(ward, "NullWard_ArtAnchor_Market", new Vector3(0f, 0f, -29f));
            EnsureAnchor(ward, "NullWard_ArtAnchor_Maintenance", new Vector3(9f, 0f, -42f));
            EnsureAnchor(ward, "NullWard_ArtAnchor_Cathedral", new Vector3(0f, 0f, -9f));
        }

        private static void EnsureAnchor(Transform ward, string name, Vector3 localPosition)
        {
            Transform anchor = ward.Find(name);
            if (anchor == null)
            {
                GameObject go = new GameObject(name);
                anchor = go.transform;
                anchor.SetParent(ward, false);
            }
            anchor.localPosition = localPosition;
            anchor.localRotation = Quaternion.identity;
            anchor.localScale = Vector3.one;
        }

        private static void BuildStaticDetail(Transform ward)
        {
            Transform previous = ward.Find(DetailRootName);
            if (previous != null) UnityEngine.Object.DestroyImmediate(previous.gameObject);

            GameObject detailRootObject = new GameObject(DetailRootName);
            detailRootObject.transform.SetParent(ward, false);
            Transform detailRoot = detailRootObject.transform;

            Material basalt = RequireMaterial("ArenaBasalt");
            Material obsidian = RequireMaterial("ObsidianArchitecture");
            Material metal = RequireMaterial("GuardianMetal");
            Material cyan = RequireMaterial("AetherCyan");
            Material viridian = RequireMaterial("WispVerdant");
            Material violet = RequireMaterial("FracturedRing");

            BuildMemoryForgeDetail(ZoneRoot(detailRoot, "Detail_MemoryForge"), metal, cyan, viridian);
            BuildCausewayDetail(ZoneRoot(detailRoot, "Detail_Causeway"), basalt, metal, cyan);
            BuildMarketDetail(ZoneRoot(detailRoot, "Detail_Market"), obsidian, metal, cyan, viridian);
            BuildMaintenanceDetail(ZoneRoot(detailRoot, "Detail_Maintenance"), metal, viridian);
            BuildCathedralDetail(ZoneRoot(detailRoot, "Detail_Cathedral"), obsidian, metal, cyan, viridian, violet);
        }

        private static Transform ZoneRoot(Transform parent, string name)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            GameObjectUtility.SetStaticEditorFlags(go, EnvironmentStaticFlags);
            return go.transform;
        }

        private static void BuildMemoryForgeDetail(Transform parent, Material metal, Material cyan, Material viridian)
        {
            for (int i = 0; i < 7; i++)
            {
                float angle = (-78f + i * 26f) * Mathf.Deg2Rad;
                Vector3 pos = new Vector3(
                    -2.2f + Mathf.Sin(angle) * 2.45f,
                    1.65f,
                    -56.8f + Mathf.Cos(angle) * 1.55f);
                CreateDetailCube($"MemoryForge_NeuralFin_{i:00}", parent, pos,
                    new Vector3(0.10f, 2.55f + (i % 2) * 0.45f, 0.22f), metal, true);
            }

            for (int i = 0; i < 5; i++)
            {
                float x = -4.2f + i * 2.1f;
                CreateDetailCube($"MemoryForge_FloorInlay_C_{i:00}", parent,
                    new Vector3(x, -0.285f, -54.2f), new Vector3(0.07f, 0.018f, 4.2f), cyan, false);
                CreateDetailCube($"MemoryForge_FloorInlay_G_{i:00}", parent,
                    new Vector3(x + 0.42f, -0.284f, -54.4f), new Vector3(0.045f, 0.016f, 3.5f), viridian, false);
            }
        }

        private static void BuildCausewayDetail(Transform parent, Material basalt, Material metal, Material cyan)
        {
            for (int i = 0; i < 5; i++)
            {
                float z = -50.4f + i * 3.25f;
                CreateDetailCube($"Causeway_Arch_L_{i:00}", parent,
                    new Vector3(-3.75f, 2.35f, z), new Vector3(0.20f, 3.65f, 0.28f), metal, true);
                CreateDetailCube($"Causeway_Arch_R_{i:00}", parent,
                    new Vector3(3.75f, 2.35f, z), new Vector3(0.20f, 3.65f, 0.28f), metal, true);
                CreateDetailCube($"Causeway_Arch_Top_{i:00}", parent,
                    new Vector3(0f, 4.10f, z), new Vector3(7.7f, 0.18f, 0.30f), metal, true);
                CreateDetailCube($"Causeway_FloorPlate_{i:00}", parent,
                    new Vector3(0f, -0.282f, z), new Vector3(6.9f, 0.025f, 2.65f), basalt, false);
            }
            CreateDetailCube("Causeway_CeilingSignal", parent,
                new Vector3(0f, 3.92f, -43.9f), new Vector3(0.055f, 0.055f, 14.6f), cyan, false);
        }

        private static void BuildMarketDetail(
            Transform parent,
            Material obsidian,
            Material metal,
            Material cyan,
            Material viridian)
        {
            Vector3[] canopyCenters =
            {
                new Vector3(-7.2f, 2.15f, -31.6f),
                new Vector3(6.5f, 2.05f, -31.7f),
                new Vector3(-6.0f, 2.25f, -24.7f),
                new Vector3(7.2f, 2.10f, -25.2f),
            };
            for (int i = 0; i < canopyCenters.Length; i++)
            {
                Vector3 c = canopyCenters[i];
                CreateDetailCube($"Market_Canopy_{i:00}", parent, c,
                    new Vector3(3.0f, 0.12f, 2.0f), obsidian, true);
                CreateDetailCube($"Market_SignBlade_{i:00}", parent, c + new Vector3(0f, 0.65f, 0.15f),
                    new Vector3(1.25f, 0.58f, 0.05f), i % 2 == 0 ? cyan : viridian, false);
                CreateDetailCube($"Market_CanopyRib_{i:00}", parent, c + new Vector3(0f, -0.45f, 0f),
                    new Vector3(2.65f, 0.08f, 0.12f), metal, true);
            }

            for (int i = 0; i < 6; i++)
            {
                float x = -8.0f + i * 3.2f;
                CreateDetailCube($"Market_FloorTrace_{i:00}", parent,
                    new Vector3(x, -0.283f, -29f), new Vector3(0.035f, 0.018f, 10.5f),
                    i % 2 == 0 ? cyan : viridian, false);
            }
        }

        private static void BuildMaintenanceDetail(Transform parent, Material metal, Material viridian)
        {
            for (int i = 0; i < 6; i++)
            {
                float z = -52.0f + i * 4.0f;
                CreateDetailCylinder($"Maintenance_Pipe_{i:00}", parent,
                    new Vector3(10.95f, 2.15f + (i % 2) * 0.35f, z),
                    new Vector3(0.18f, 1.8f, 0.18f), new Vector3(90f, 0f, 0f), metal, true);
                CreateDetailCube($"Maintenance_ServiceGlow_{i:00}", parent,
                    new Vector3(10.56f, 1.35f, z), new Vector3(0.06f, 0.85f, 0.22f), viridian, false);
            }
            CreateDetailCube("Maintenance_OverheadBus", parent,
                new Vector3(10.65f, 3.05f, -42f), new Vector3(0.18f, 0.18f, 22.5f), metal, true);
        }

        private static void BuildCathedralDetail(
            Transform parent,
            Material obsidian,
            Material metal,
            Material cyan,
            Material viridian,
            Material violet)
        {
            for (int i = 0; i < 5; i++)
            {
                float z = -15f + i * 2.75f;
                float height = 4.8f + i * 0.42f;
                CreateDetailCube($"Cathedral_Rib_L_{i:00}", parent,
                    new Vector3(-5.9f, height * 0.52f, z), new Vector3(0.16f, height, 0.26f), metal, true);
                CreateDetailCube($"Cathedral_Rib_R_{i:00}", parent,
                    new Vector3(5.9f, height * 0.52f, z), new Vector3(0.16f, height, 0.26f), metal, true);
                CreateDetailCube($"Cathedral_Vault_{i:00}", parent,
                    new Vector3(0f, height, z), new Vector3(12.0f, 0.16f, 0.28f), obsidian, true);
            }

            CreateDetailCube("Cathedral_SightSpine", parent,
                new Vector3(-1.0f, 0.02f, -10.5f), new Vector3(0.06f, 0.05f, 13.5f), cyan, false);
            CreateDetailCube("Cathedral_GuardSpine", parent,
                new Vector3(1.0f, 0.02f, -10.5f), new Vector3(0.06f, 0.05f, 13.5f), viridian, false);
            CreateDetailCube("Cathedral_FractureCrown", parent,
                new Vector3(0f, 5.45f, -5.1f), new Vector3(4.4f, 0.09f, 0.26f), violet, false,
                new Vector3(0f, 0f, 18f));
        }

        private static GameObject CreateDetailCube(
            string name,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            Material material,
            bool castShadows,
            Vector3? localEuler = null)
        {
            return CreateDetailPrimitive(
                name,
                PrimitiveType.Cube,
                parent,
                localPosition,
                localScale,
                localEuler ?? Vector3.zero,
                material,
                castShadows);
        }

        private static GameObject CreateDetailCylinder(
            string name,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            Vector3 localEuler,
            Material material,
            bool castShadows)
        {
            return CreateDetailPrimitive(
                name,
                PrimitiveType.Cylinder,
                parent,
                localPosition,
                localScale,
                localEuler,
                material,
                castShadows);
        }

        private static GameObject CreateDetailPrimitive(
            string name,
            PrimitiveType primitiveType,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            Vector3 localEuler,
            Material material,
            bool castShadows)
        {
            GameObject go = GameObject.CreatePrimitive(primitiveType);
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

            GameObjectUtility.SetStaticEditorFlags(go, EnvironmentStaticFlags);
            return go;
        }

        private static void OptimizeStaticScenery(Transform ward)
        {
            Renderer[] renderers = ward.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (!EligibleStaticRenderer(renderer)) continue;
                GameObjectUtility.SetStaticEditorFlags(renderer.gameObject, EnvironmentStaticFlags);
            }
        }

        private static bool EligibleStaticRenderer(Renderer renderer)
        {
            if (renderer == null) return false;
            if (renderer is LineRenderer || renderer is TrailRenderer || renderer is ParticleSystemRenderer) return false;
            if (renderer.GetComponentInParent<CombatantVitals>() != null) return false;
            if (renderer.GetComponentInParent<JourneyGate>() != null) return false;
            if (renderer.GetComponentInParent<FracturedEchoNode>() != null) return false;
            return renderer is MeshRenderer;
        }

        private static Material RequireMaterial(string name)
        {
            Material material = CinematicMaterialAuthoring.Load(name);
            if (material == null)
                throw new InvalidOperationException($"Missing shared cinematic material {name}.");
            return material;
        }
    }
}
#endif
