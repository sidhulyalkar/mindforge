#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mindforge.Editor
{
    /// <summary>
    /// Distant production-only world scale: fog-layered natural ridges behind the built city,
    /// plus immense cathedral/neural ring structures inspired by the project's reference art.
    /// Everything sits outside the reachable collision basin and carries zero gameplay authority.
    /// </summary>
    public static class ProductionHorizonV09Builder
    {
        public const string RootName = "Production_Horizon_V09";
        public const int MaxHorizonRenderers = 18;
        public const float MidRidgeRadius = 185f;
        public const float FarRidgeRadius = 275f;
        public const float FurthestHeroDepth = 150f;

        [MenuItem("Mindforge/Showcase/Apply Production Horizon V0.9", priority = 45)]
        public static void ApplyOpenScene()
        {
            GameObject production = EditorSceneLookup.FindIncludingInactive(ProductionArtV09Builder.RootName);
            if (production == null)
                throw new InvalidOperationException("Production Horizon V0.9 requires Production Art V0.9.");

            Transform previous = production.transform.Find(RootName);
            if (previous != null) UnityEngine.Object.DestroyImmediate(previous.gameObject);

            ProductionMaterialAuthoringV09.EnsureAuthored();
            Material pearl = Require(ProductionMaterialAuthoringV09.Pearl);
            Material warm = Require(ProductionMaterialAuthoringV09.WarmStone);
            Material graphite = Require(ProductionMaterialAuthoringV09.Graphite);
            Material gold = Require(ProductionMaterialAuthoringV09.Gold);

            Mesh midRidge = ProductionHorizonMeshLibraryV09.MidRidge();
            Mesh farRidge = ProductionHorizonMeshLibraryV09.FarRidge();
            Mesh ring = ProductionCalibrationMeshLibraryV09.PhaseRing();
            Mesh arch = ProductionMeshLibraryV09.PointedArch();
            Mesh spire = ProductionMeshLibraryV09.CathedralSpire();

            GameObject rootGo = new GameObject(RootName);
            rootGo.transform.SetParent(production.transform, false);
            Transform root = rootGo.transform;

            DistantPart("Mid_Foothill_Ring", root, midRidge,
                new Vector3(0f, -1.0f, -16f), new Vector3(MidRidgeRadius, 48f, MidRidgeRadius), warm, Vector3.zero);
            DistantPart("Far_Mountain_Ring", root, farRidge,
                new Vector3(0f, -3.0f, -18f), new Vector3(FarRidgeRadius, 70f, FarRidgeRadius), pearl, new Vector3(0f, 11f, 0f));

            // The central structure is enormous enough to function as a world landmark rather
            // than another prop. Torus geometry replaces the older line-rendered phase-ring idea.
            DistantPart("Neural_MegaRing_Central", root, ring,
                new Vector3(0f, 43f, 118f), new Vector3(92f, 18f, 92f), pearl, new Vector3(90f, 0f, 0f));
            DistantPart("Neural_MegaRing_Central_Gold", root, ring,
                new Vector3(0f, 43f, 116.8f), new Vector3(82f, 14f, 82f), gold, new Vector3(90f, 0f, 0f));
            DistantPart("Neural_MegaRing_West", root, ring,
                new Vector3(-54f, 29f, 136f), new Vector3(50f, 11f, 50f), graphite, new Vector3(90f, 16f, 0f));
            DistantPart("Neural_MegaRing_East", root, ring,
                new Vector3(59f, 33f, 145f), new Vector3(58f, 12f, 58f), gold, new Vector3(90f, -14f, 0f));

            DistantPart("Horizon_Cathedral_Arch", root, arch,
                new Vector3(0f, 18f, 104f), new Vector3(29f, 30f, 8.5f), pearl, Vector3.zero);
            DistantPart("Horizon_Cathedral_Arch_Gold", root, arch,
                new Vector3(0f, 18.4f, 102.8f), new Vector3(26.5f, 27.3f, 9.2f), gold, Vector3.zero);

            float[] towerX = { -43f, -29f, 31f, 46f };
            for (int i = 0; i < towerX.Length; i++)
            {
                float h = 29f + (i % 2) * 7f;
                DistantPart($"Horizon_Spire_{i}", root, spire,
                    new Vector3(towerX[i], 0f, 108f + (i % 2) * 9f),
                    new Vector3(5.2f, h, 5.2f), i == 1 || i == 2 ? pearl : graphite, Vector3.zero);
            }

            Validate(rootGo);
            EditorUtility.SetDirty(rootGo);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();

            Debug.Log(
                "[Mindforge:V09:Horizon] Added fog-layered foothills/mountains plus true-mesh neural mega-rings and a monumental cathedral arch. " +
                "All horizon geometry is collider-free, shadow-free, probe-free and outside the reachable world basin.");
        }

        private static MeshRenderer DistantPart(
            string name,
            Transform parent,
            Mesh mesh,
            Vector3 position,
            Vector3 scale,
            Material material,
            Vector3 euler)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = scale;

            MeshFilter filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            MeshRenderer renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            renderer.allowOcclusionWhenDynamic = true;
            return renderer;
        }

        private static void Validate(GameObject root)
        {
            int renderers = root.GetComponentsInChildren<Renderer>(true).Length;
            if (renderers > MaxHorizonRenderers)
                throw new InvalidOperationException($"Production horizon renderer budget exceeded: {renderers}/{MaxHorizonRenderers}.");
            if (root.GetComponentsInChildren<Collider>(true).Length != 0)
                throw new InvalidOperationException("Production horizon acquired collision authority.");
            if (root.GetComponentsInChildren<Rigidbody>(true).Length != 0)
                throw new InvalidOperationException("Production horizon acquired Rigidbody authority.");
            if (root.GetComponentsInChildren<Light>(true).Length != 0)
                throw new InvalidOperationException("Production horizon acquired lighting authority.");
            if (FarRidgeRadius >= 360f)
                throw new InvalidOperationException("Production horizon must remain comfortably inside the 420m gameplay far clip from the playable basin.");
        }

        private static Material Require(string name)
        {
            Material material = ProductionMaterialAuthoringV09.Load(name);
            if (material == null) throw new InvalidOperationException("Missing production horizon material: " + name);
            return material;
        }
    }
}
#endif
