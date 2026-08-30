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
    /// Production presentation for the first BCI-facing space. Existing resonance-station E
    /// interactions, flicker renderer ownership, calibration authority and JourneyGate collision
    /// remain unchanged. This pass only replaces their obvious primitive presentation.
    /// </summary>
    public static class ProductionNeuralSanctumV09Builder
    {
        public const string RootName = "Production_Neural_Sanctum_V09";
        public const string StationVisualRoot = "Production_Resonance_Apparatus_V09";
        public const string ThresholdVisualRoot = "Production_Threshold_Membrane_V09";
        public const int ExpectedStationCount = 3;
        public const int MaxAddedRenderers = 32;

        private static readonly StaticEditorFlags StaticFlags =
            StaticEditorFlags.BatchingStatic |
            StaticEditorFlags.OccludeeStatic |
            StaticEditorFlags.ReflectionProbeStatic;

        [MenuItem("Mindforge/Showcase/Apply Neural Sanctum Presentation V0.9", priority = 45)]
        public static void ApplyOpenScene()
        {
            GameObject production = EditorSceneLookup.FindIncludingInactive(ProductionArtV09Builder.RootName);
            GameObject sanctum = EditorSceneLookup.FindIncludingInactive(SanctumOnboardingV08Builder.RootName);
            if (production == null || sanctum == null)
                throw new InvalidOperationException("Neural Sanctum V0.9 requires production art and Sanctum V0.8.");

            Transform previous = production.transform.Find(RootName);
            if (previous != null) UnityEngine.Object.DestroyImmediate(previous.gameObject);

            ProductionMaterialAuthoringV09.EnsureAuthored();
            Material ivory = Require(ProductionMaterialAuthoringV09.Ivory);
            Material pearl = Require(ProductionMaterialAuthoringV09.Pearl);
            Material gold = Require(ProductionMaterialAuthoringV09.Gold);
            Material glass = Require(ProductionMaterialAuthoringV09.Glass);
            Material graphite = Require(ProductionMaterialAuthoringV09.Graphite);

            Mesh column = ProductionMeshLibraryV09.FlutedColumn();
            Mesh arch = ProductionMeshLibraryV09.PointedArch();
            Mesh spire = ProductionMeshLibraryV09.CathedralSpire();
            Mesh lens = ProductionCalibrationMeshLibraryV09.ResonanceLens();
            Mesh ring = ProductionCalibrationMeshLibraryV09.PhaseRing();
            Mesh membrane = ProductionCalibrationMeshLibraryV09.ThresholdMembranePanel();

            GameObject rootGo = new GameObject(RootName);
            rootGo.transform.SetParent(production.transform, false);
            GameObjectUtility.SetStaticEditorFlags(rootGo, StaticFlags);

            Transform[] all = sanctum.GetComponentsInChildren<Transform>(true);
            int stations = 0;
            for (int i = 0; i < all.Length; i++)
            {
                Transform t = all[i];
                if (t == null || !t.name.StartsWith("Resonance_Station_", StringComparison.Ordinal)) continue;
                BuildStation(t, column, arch, spire, lens, ring, ivory, pearl, gold, graphite);
                stations++;
            }
            if (stations != ExpectedStationCount)
                throw new InvalidOperationException($"Expected {ExpectedStationCount} Sanctum resonance stations, found {stations}.");

            Transform seal = FindNamed(sanctum.transform, "ThresholdSeal");
            if (seal == null)
                throw new InvalidOperationException("Sanctum threshold seal was not found; V0.9 refuses to invent a second gate.");
            BuildThresholdMembrane(seal, membrane, glass, pearl, gold);

            // Marker child lets the production root prove this pass completed synchronously.
            GameObject marker = new GameObject("NeuralSanctumReady");
            marker.transform.SetParent(rootGo.transform, false);

            ValidatePresentationOnly(rootGo, sanctum.transform);
            EditorUtility.SetDirty(rootGo);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();

            Debug.Log(
                "[Mindforge:V09:NeuralSanctum] Replaced three primitive resonance apparatuses and the visible threshold cube with production presentation. " +
                "Existing SanctumCalibrationOrbV08 renderers still own luminance/flicker; JourneyGate collision and all calibration authority are unchanged.");
        }

        private static void BuildStation(
            Transform station,
            Mesh column,
            Mesh arch,
            Mesh spire,
            Mesh lens,
            Mesh ring,
            Material ivory,
            Material pearl,
            Material gold,
            Material graphite)
        {
            Transform previous = station.Find(StationVisualRoot);
            if (previous != null) UnityEngine.Object.DestroyImmediate(previous.gameObject);

            Transform core = FindNamed(station, "ResonanceOrb");
            if (core == null)
                throw new InvalidOperationException("Resonance station is missing the existing ResonanceOrb renderer: " + station.name);
            SanctumCalibrationOrbV08 interaction = station.GetComponent<SanctumCalibrationOrbV08>();
            if (interaction == null)
                throw new InvalidOperationException("Resonance station lost SanctumCalibrationOrbV08 authority: " + station.name);

            MeshFilter coreFilter = core.GetComponent<MeshFilter>();
            Renderer coreRenderer = core.GetComponent<Renderer>();
            if (coreFilter == null || coreRenderer == null)
                throw new InvalidOperationException("ResonanceOrb must retain its existing render target: " + station.name);

            // Keep the same renderer object because SanctumCalibrationOrbV08 writes its
            // luminance property block. Only its visible mesh changes from a stock sphere.
            coreFilter.sharedMesh = lens;
            core.localScale = Vector3.one * 0.96f;
            coreRenderer.enabled = true;
            coreRenderer.shadowCastingMode = ShadowCastingMode.Off;
            coreRenderer.receiveShadows = false;
            EditorUtility.SetDirty(coreFilter);
            EditorUtility.SetDirty(coreRenderer);

            HideLegacyStationRenderer(station, "Plinth");
            HideLegacyStationRenderer(station, "GoldStem");
            HideLegacyStationRenderer(station, "OrbitalA");
            HideLegacyStationRenderer(station, "OrbitalB");

            GameObject apparatus = new GameObject(StationVisualRoot);
            apparatus.transform.SetParent(station, false);
            GameObjectUtility.SetStaticEditorFlags(apparatus, StaticFlags);

            MeshPart("ChapelArch", apparatus.transform, arch,
                new Vector3(0f, 3.05f, 0.72f), new Vector3(2.35f, 2.85f, 0.82f), pearl, Vector3.zero, true);
            MeshPart("Pedestal", apparatus.transform, column,
                new Vector3(0f, 0.72f, 0f), new Vector3(1.65f, 1.35f, 1.65f), ivory, Vector3.zero, true);
            MeshPart("PedestalCrown", apparatus.transform, ring,
                new Vector3(0f, 1.42f, 0f), new Vector3(2.15f, 0.64f, 2.15f), gold, Vector3.zero, true);
            MeshPart("LeftNeedle", apparatus.transform, spire,
                new Vector3(-1.62f, 0.42f, 0.18f), new Vector3(0.44f, 2.5f, 0.44f), graphite, new Vector3(0f, 0f, -4f), true);
            MeshPart("RightNeedle", apparatus.transform, spire,
                new Vector3(1.62f, 0.42f, 0.18f), new Vector3(0.44f, 2.12f, 0.44f), gold, new Vector3(0f, 0f, 5f), true);

            Transform ringA = MeshPart("PhaseRingA", apparatus.transform, ring,
                new Vector3(0f, 2.15f, 0f), Vector3.one * 2.42f, gold, new Vector3(68f, 0f, 12f), false).transform;
            Transform ringB = MeshPart("PhaseRingB", apparatus.transform, ring,
                new Vector3(0f, 2.15f, 0f), Vector3.one * 2.82f, pearl, new Vector3(18f, 53f, 0f), false).transform;

            ProductionCalibrationPresentationV09 motion = station.GetComponent<ProductionCalibrationPresentationV09>();
            if (motion == null) motion = station.gameObject.AddComponent<ProductionCalibrationPresentationV09>();
            motion.ConfigureRuntime(ringA, ringB);
            EditorUtility.SetDirty(motion);
        }

        private static void BuildThresholdMembrane(Transform seal, Mesh membrane, Material glass, Material pearl, Material gold)
        {
            Renderer legacyRenderer = seal.GetComponent<Renderer>();
            Collider blocker = seal.GetComponent<Collider>();
            if (legacyRenderer == null || blocker == null || !blocker.enabled)
                throw new InvalidOperationException("ThresholdSeal must retain its renderer proxy and enabled gameplay blocker.");
            legacyRenderer.enabled = false;
            EditorUtility.SetDirty(legacyRenderer);

            Transform previous = seal.Find(ThresholdVisualRoot);
            if (previous != null) UnityEngine.Object.DestroyImmediate(previous.gameObject);
            GameObject root = new GameObject(ThresholdVisualRoot);
            root.transform.SetParent(seal, false);

            // ThresholdSeal itself is deliberately left as the moving/collision authority.
            // Local scales compensate for its existing 11.6 x 7.4 x 0.42 proxy transform.
            MeshPart("MembraneLeft", root.transform, membrane,
                new Vector3(-0.325f, 0f, -0.20f), new Vector3(0.305f, 0.88f, 0.52f), glass, Vector3.zero, false, false);
            MeshPart("MembraneCenter", root.transform, membrane,
                new Vector3(0f, 0f, -0.25f), new Vector3(0.305f, 0.92f, 0.58f), pearl, Vector3.zero, false, false);
            MeshPart("MembraneRight", root.transform, membrane,
                new Vector3(0.325f, 0f, -0.20f), new Vector3(0.305f, 0.88f, 0.52f), glass, Vector3.zero, false, false);
            MeshPart("GoldSeamLeft", root.transform, membrane,
                new Vector3(-0.163f, 0f, -0.32f), new Vector3(0.018f, 0.90f, 0.64f), gold, Vector3.zero, false, false);
            MeshPart("GoldSeamRight", root.transform, membrane,
                new Vector3(0.163f, 0f, -0.32f), new Vector3(0.018f, 0.90f, 0.64f), gold, Vector3.zero, false, false);
        }

        private static void HideLegacyStationRenderer(Transform station, string name)
        {
            Transform child = FindNamed(station, name);
            if (child == null) return;
            Renderer[] renderers = child.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                renderers[i].enabled = false;
                EditorUtility.SetDirty(renderers[i]);
            }
        }

        private static MeshRenderer MeshPart(
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
            if (markStatic) GameObjectUtility.SetStaticEditorFlags(go, StaticFlags);

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

        private static void ValidatePresentationOnly(GameObject markerRoot, Transform sanctum)
        {
            if (markerRoot.GetComponentsInChildren<Collider>(true).Length != 0)
                throw new InvalidOperationException("Neural Sanctum V0.9 added collision authority.");
            if (markerRoot.GetComponentsInChildren<Rigidbody>(true).Length != 0)
                throw new InvalidOperationException("Neural Sanctum V0.9 added Rigidbody authority.");
            if (markerRoot.GetComponentsInChildren<Light>(true).Length != 0)
                throw new InvalidOperationException("Neural Sanctum V0.9 added unbudgeted lights.");

            int added = 0;
            Transform[] stations = sanctum.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < stations.Length; i++)
            {
                if (stations[i] == null || !stations[i].name.StartsWith("Resonance_Station_", StringComparison.Ordinal)) continue;
                Transform apparatus = stations[i].Find(StationVisualRoot);
                if (apparatus != null) added += apparatus.GetComponentsInChildren<Renderer>(true).Length;
            }
            Transform seal = FindNamed(sanctum, "ThresholdSeal");
            if (seal != null)
            {
                Transform membrane = seal.Find(ThresholdVisualRoot);
                if (membrane != null) added += membrane.GetComponentsInChildren<Renderer>(true).Length;
            }
            if (added > MaxAddedRenderers)
                throw new InvalidOperationException($"Neural Sanctum V0.9 renderer budget exceeded: {added}/{MaxAddedRenderers}.");
        }

        private static Material Require(string name)
        {
            Material material = ProductionMaterialAuthoringV09.Load(name);
            if (material == null) throw new InvalidOperationException("Missing V0.9 production material: " + name);
            return material;
        }
    }
}
#endif
