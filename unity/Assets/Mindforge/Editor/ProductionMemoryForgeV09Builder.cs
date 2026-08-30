#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Mindforge.Presentation;
using Mindforge.World;

namespace Mindforge.Editor
{
    /// <summary>
    /// Replaces the V0.8 Memory Forge's primitive render shell with production meshes while
    /// retaining its existing collider, checkpoint, E-interaction and persistence authorities.
    /// </summary>
    public static class ProductionMemoryForgeV09Builder
    {
        public const string RootName = "Production_Memory_Forge_V09";
        public const int MaxRenderers = 14;

        private static readonly string[] LegacyVisualNames =
        {
            "ForgeDais",
            "ForgeDaisGold",
            "ForgePedestal",
            "ForgeCore",
            "ForgeWing_-1",
            "ForgeWing_1",
            "ForgeWingGold_-1",
            "ForgeWingGold_1",
            "ForgeMemoryNode_-1",
            "ForgeMemoryNode_1",
            "ForgeHaloOuter",
            "ForgeHaloInner",
        };

        [MenuItem("Mindforge/Showcase/Apply Production Memory Forge V0.9", priority = 45)]
        public static void ApplyOpenScene()
        {
            GameObject production = EditorSceneLookup.FindIncludingInactive(ProductionArtV09Builder.RootName);
            GameObject altar = EditorSceneLookup.FindIncludingInactive("Memory_Forge_Sanctum_Altar_V08");
            MemoryForgeCheckpoint checkpoint = UnityEngine.Object.FindObjectOfType<MemoryForgeCheckpoint>(true);
            if (production == null || altar == null || checkpoint == null)
                throw new InvalidOperationException("Production Memory Forge V0.9 requires production art, V0.8 altar and existing Memory Forge checkpoint.");

            Transform previous = altar.transform.Find(RootName);
            if (previous != null) UnityEngine.Object.DestroyImmediate(previous.gameObject);

            Transform dais = FindNamed(altar.transform, "ForgeDais");
            Collider daisCollider = dais != null ? dais.GetComponent<Collider>() : null;
            if (daisCollider == null || !daisCollider.enabled)
                throw new InvalidOperationException("Memory Forge physical dais collider must remain authoritative before visual replacement.");

            ProductionMaterialAuthoringV09.EnsureAuthored();
            Material ivory = RequireProduction(ProductionMaterialAuthoringV09.Ivory);
            Material pearl = RequireProduction(ProductionMaterialAuthoringV09.Pearl);
            Material graphite = RequireProduction(ProductionMaterialAuthoringV09.Graphite);
            Material gold = RequireProduction(ProductionMaterialAuthoringV09.Gold);
            Material cyan = RequireCinematic("AetherCyan");
            Material green = RequireCinematic("WispVerdant");

            Mesh column = ProductionMeshLibraryV09.FlutedColumn();
            Mesh arch = ProductionMeshLibraryV09.PointedArch();
            Mesh spire = ProductionMeshLibraryV09.CathedralSpire();
            Mesh ring = ProductionCalibrationMeshLibraryV09.PhaseRing();
            Mesh lens = ProductionCalibrationMeshLibraryV09.ResonanceLens();
            Mesh shard = ProductionStoryMeshLibraryV09.SignalShard();

            HideLegacyRenderers(altar.transform);

            GameObject rootGo = new GameObject(RootName);
            rootGo.transform.SetParent(altar.transform, false);
            Transform root = rootGo.transform;

            Part("FoundationRing", root, ring, new Vector3(0f, 0.18f, 0f), new Vector3(4.2f, 0.52f, 4.2f), gold, Vector3.zero, true);
            Part("FoundationInner", root, ring, new Vector3(0f, 0.28f, 0f), new Vector3(3.4f, 0.40f, 3.4f), pearl, Vector3.zero, true);
            Part("ForgeColumn", root, column, new Vector3(0f, 0.92f, 0f), new Vector3(1.55f, 1.55f, 1.55f), ivory, Vector3.zero, true);
            Part("ForgeLens", root, lens, new Vector3(0f, 2.18f, 0f), Vector3.one * 1.24f, cyan, new Vector3(0f, 12f, 0f), false);
            Part("ForgeBackArch", root, arch, new Vector3(0f, 2.72f, 0.68f), new Vector3(2.55f, 3.15f, 0.86f), pearl, Vector3.zero, true);

            Part("WestNeedle", root, spire, new Vector3(-1.55f, 0.42f, 0.18f), new Vector3(0.52f, 3.15f, 0.52f), graphite, new Vector3(0f, 0f, -6f), true);
            Part("EastNeedle", root, spire, new Vector3(1.55f, 0.42f, 0.18f), new Vector3(0.52f, 3.45f, 0.52f), gold, new Vector3(0f, 0f, 7f), true);
            Part("MemoryShardSight", root, shard, new Vector3(-1.38f, 2.82f, -0.12f), new Vector3(0.42f, 0.92f, 0.42f), cyan, new Vector3(18f, -22f, 14f), false);
            Part("MemoryShardGuard", root, shard, new Vector3(1.42f, 2.66f, 0.08f), new Vector3(0.38f, 0.84f, 0.38f), green, new Vector3(-12f, 31f, -18f), false);

            Transform outer = Part("ForgePhaseRingOuter", root, ring,
                new Vector3(0f, 2.18f, 0f), Vector3.one * 3.35f, gold, new Vector3(68f, 8f, 10f), false, false).transform;
            Transform inner = Part("ForgePhaseRingInner", root, ring,
                new Vector3(0f, 2.18f, 0f), Vector3.one * 2.72f, pearl, new Vector3(15f, 58f, 0f), false, false).transform;

            ProductionForgePresentationV09 motion = altar.GetComponent<ProductionForgePresentationV09>();
            if (motion == null) motion = altar.AddComponent<ProductionForgePresentationV09>();
            motion.ConfigureRuntime(outer, inner);
            EditorUtility.SetDirty(motion);

            Validate(rootGo, daisCollider, checkpoint);
            EditorUtility.SetDirty(rootGo);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();

            Debug.Log(
                "[Mindforge:V09:Forge] Memory Forge primitive shell replaced by fluted/pointed/faceted production geometry and true torus mechanisms. " +
                "Existing physical dais collider, checkpoint, contextual E interaction, persistence and cyan/green semantic meaning remain authoritative.");
        }

        private static void HideLegacyRenderers(Transform altar)
        {
            for (int i = 0; i < LegacyVisualNames.Length; i++)
            {
                Transform visual = FindNamed(altar, LegacyVisualNames[i]);
                if (visual == null) continue;
                Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
                for (int r = 0; r < renderers.Length; r++)
                {
                    if (renderers[r] == null) continue;
                    renderers[r].enabled = false;
                    EditorUtility.SetDirty(renderers[r]);
                }
            }
        }

        private static MeshRenderer Part(
            string name,
            Transform parent,
            Mesh mesh,
            Vector3 localPosition,
            Vector3 localScale,
            Material material,
            Vector3 localEuler,
            bool castShadows,
            bool markStatic = true)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.Euler(localEuler);
            go.transform.localScale = localScale;
            if (markStatic)
                GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.ReflectionProbeStatic);

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

        private static Transform FindNamed(Transform root, string name)
        {
            if (root == null) return null;
            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
                if (all[i] != null && string.Equals(all[i].name, name, StringComparison.Ordinal)) return all[i];
            return null;
        }

        private static void Validate(GameObject root, Collider originalDaisCollider, MemoryForgeCheckpoint checkpoint)
        {
            if (checkpoint == null)
                throw new InvalidOperationException("Memory Forge checkpoint authority disappeared during presentation replacement.");
            if (originalDaisCollider == null || !originalDaisCollider.enabled)
                throw new InvalidOperationException("Memory Forge dais collision was modified by production presentation.");
            if (root.GetComponentsInChildren<Collider>(true).Length != 0)
                throw new InvalidOperationException("Production Memory Forge added collision authority.");
            if (root.GetComponentsInChildren<Rigidbody>(true).Length != 0)
                throw new InvalidOperationException("Production Memory Forge added Rigidbody authority.");
            if (root.GetComponentsInChildren<Light>(true).Length != 0)
                throw new InvalidOperationException("Production Memory Forge added lighting authority.");
            int rendererCount = root.GetComponentsInChildren<Renderer>(true).Length;
            if (rendererCount > MaxRenderers)
                throw new InvalidOperationException($"Production Memory Forge renderer budget exceeded: {rendererCount}/{MaxRenderers}.");
        }

        private static Material RequireProduction(string name)
        {
            Material material = ProductionMaterialAuthoringV09.Load(name);
            if (material == null) throw new InvalidOperationException("Missing production Memory Forge material: " + name);
            return material;
        }

        private static Material RequireCinematic(string name)
        {
            Material material = CinematicMaterialAuthoring.Load(name);
            if (material == null) throw new InvalidOperationException("Missing semantic Memory Forge material: " + name);
            return material;
        }
    }
}
#endif
