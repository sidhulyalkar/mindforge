#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Mindforge.Presentation;

namespace Mindforge.Editor
{
    /// <summary>
    /// Fallback integration hook for manual editor workflows. The canonical one-click showcase
    /// now applies V0.9 synchronously. This hook only repairs incomplete manually assembled
    /// scenes and becomes a no-op once the complete production presentation is already present.
    /// </summary>
    [InitializeOnLoad]
    public static class ProductionArtAutoHookV09
    {
        private static bool _applying;

        static ProductionArtAutoHookV09()
        {
            EditorApplication.delayCall += TryApply;
            EditorSceneManager.sceneSaved += _ =>
            {
                if (!_applying) EditorApplication.delayCall += TryApply;
            };
        }

        [MenuItem("Mindforge/Showcase/Apply Complete Production Presentation V0.9", priority = 43)]
        public static void ApplyNow()
        {
            if (!ReferenceFidelityReady())
            {
                Debug.LogWarning("[Mindforge:V09:Art] Build the complete V0.8 reference-fidelity showcase first.");
                return;
            }
            ApplyInternal(true);
        }

        private static void TryApply()
        {
            if (_applying || EditorApplication.isPlayingOrWillChangePlaymode || !ReferenceFidelityReady()) return;
            GameObject production = EditorSceneLookup.FindIncludingInactive(ProductionArtV09Builder.RootName);

            // Canonical Build + Play has already completed every V0.9 stage synchronously.
            // Its final scene save may still enqueue this fallback callback; detect that state
            // and do literally no additional presentation work before Play Mode starts.
            if (production != null && CompletePresentationReady(production)) return;

            if (production != null)
            {
                ProductionLegacyVisualQuarantineV09.ApplyOpenScene();
                EnsureHorizon(production);
                EnsureStorytelling(production);
                EnsureMemoryForge(production);
                EnsureNeuralSanctum(production);
                EnsureLighting(production);
                EnsurePostFx(production);
                EnsurePresentationComponents();
                ExternalArtReplacementV09.ApplyOpenScene();
                return;
            }
            ApplyInternal(false);
        }

        private static bool ReferenceFidelityReady()
        {
            GameObject sanctum = EditorSceneLookup.FindIncludingInactive(SanctumOnboardingV08Builder.RootName);
            if (sanctum == null) return false;
            return sanctum.transform.Find(SanctumReferenceFidelityV08Builder.RootName) != null;
        }

        private static bool CompletePresentationReady(GameObject production)
        {
            if (production == null) return false;
            if (production.transform.Find(ProductionHorizonV09Builder.RootName) == null) return false;
            if (production.transform.Find(ProductionWorldStorytellingV09Builder.RootName) == null) return false;
            if (EditorSceneLookup.FindIncludingInactive("Memory_Forge_Sanctum_Altar_V08")?.transform.Find(ProductionMemoryForgeV09Builder.RootName) == null) return false;
            if (production.transform.Find(ProductionNeuralSanctumV09Builder.RootName) == null) return false;
            if (production.transform.Find(ProductionLightingV09Builder.RootName) == null) return false;
            if (production.transform.Find(ProductionPostFxV09Builder.RootName) == null) return false;

            GameObject arena = EditorSceneLookup.FindIncludingInactive("Fractured_Signal_Arena");
            GameObject guardian = EditorSceneLookup.FindIncludingInactive("Guardian");
            if (arena == null || guardian == null) return false;

            return arena.GetComponent<ProductionHudV09>() != null &&
                   arena.GetComponent<ProductionEchoVisualBootstrapV09>() != null &&
                   guardian.GetComponent<ProductionGuardianV09>() != null;
        }

        private static void ApplyInternal(bool forceRebuild)
        {
            if (_applying) return;
            _applying = true;
            try
            {
                GameObject existing = EditorSceneLookup.FindIncludingInactive(ProductionArtV09Builder.RootName);
                if (forceRebuild || existing == null)
                    ProductionArtV09Builder.ApplyOpenScene();

                GameObject production = EditorSceneLookup.FindIncludingInactive(ProductionArtV09Builder.RootName);
                int quarantined = ProductionLegacyVisualQuarantineV09.ApplyOpenScene();
                EnsureHorizon(production);
                EnsureStorytelling(production);
                EnsureMemoryForge(production);
                EnsureNeuralSanctum(production);
                EnsureLighting(production);
                EnsurePostFx(production);
                EnsurePresentationComponents();
                int external = ExternalArtReplacementV09.ApplyOpenScene();
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                EditorSceneManager.SaveOpenScenes();
                AssetDatabase.SaveAssets();
                Debug.Log(
                    $"[Mindforge:V09:Art] Complete production presentation applied after V0.8 reference fidelity; " +
                    $"legacy renderers quarantined={quarantined}; local external replacements={external}; " +
                    "natural horizon + neural megastructures + district storytelling + production Memory Forge + production neural sanctum + consolidated light hierarchy + restrained URP finish enabled.");
            }
            finally
            {
                _applying = false;
            }
        }

        private static void EnsureHorizon(GameObject production)
        {
            if (production == null) return;
            if (production.transform.Find(ProductionHorizonV09Builder.RootName) != null) return;
            ProductionHorizonV09Builder.ApplyOpenScene();
        }

        private static void EnsureStorytelling(GameObject production)
        {
            if (production == null) return;
            if (production.transform.Find(ProductionWorldStorytellingV09Builder.RootName) != null) return;
            ProductionWorldStorytellingV09Builder.ApplyOpenScene();
        }

        private static void EnsureMemoryForge(GameObject production)
        {
            if (production == null) return;
            GameObject altar = EditorSceneLookup.FindIncludingInactive("Memory_Forge_Sanctum_Altar_V08");
            if (altar == null) return;
            if (altar.transform.Find(ProductionMemoryForgeV09Builder.RootName) != null) return;
            ProductionMemoryForgeV09Builder.ApplyOpenScene();
        }

        private static void EnsureNeuralSanctum(GameObject production)
        {
            if (production == null) return;
            if (production.transform.Find(ProductionNeuralSanctumV09Builder.RootName) != null) return;
            ProductionNeuralSanctumV09Builder.ApplyOpenScene();
        }

        private static void EnsureLighting(GameObject production)
        {
            if (production == null) return;
            if (production.transform.Find(ProductionLightingV09Builder.RootName) != null) return;
            ProductionLightingV09Builder.ApplyOpenScene();
        }

        private static void EnsurePostFx(GameObject production)
        {
            if (production == null) return;
            if (production.transform.Find(ProductionPostFxV09Builder.RootName) != null) return;
            ProductionPostFxV09Builder.ApplyOpenScene();
        }

        private static void EnsurePresentationComponents()
        {
            ProductionMaterialAuthoringV09.EnsureAuthored();
            Material pearl = ProductionMaterialAuthoringV09.Load(ProductionMaterialAuthoringV09.Pearl);
            Material graphite = ProductionMaterialAuthoringV09.Load(ProductionMaterialAuthoringV09.Graphite);
            Material gold = ProductionMaterialAuthoringV09.Load(ProductionMaterialAuthoringV09.Gold);
            Material aether = CinematicMaterialAuthoring.Load("AetherCyan");
            Material hostile = CinematicMaterialAuthoring.Load("FracturedCore");

            GameObject arena = EditorSceneLookup.FindIncludingInactive("Fractured_Signal_Arena");
            GameObject guardian = EditorSceneLookup.FindIncludingInactive("Guardian");
            if (arena != null)
            {
                ProductionHudV09 hud = arena.GetComponent<ProductionHudV09>();
                if (hud == null) hud = arena.AddComponent<ProductionHudV09>();
                EditorUtility.SetDirty(hud);

                ProductionEchoVisualBootstrapV09 echoBootstrap = arena.GetComponent<ProductionEchoVisualBootstrapV09>();
                if (echoBootstrap == null) echoBootstrap = arena.AddComponent<ProductionEchoVisualBootstrapV09>();
                echoBootstrap.ConfigureRuntime(graphite, hostile, gold);
                EditorUtility.SetDirty(echoBootstrap);
            }
            if (guardian != null)
            {
                ProductionGuardianV09 production = guardian.GetComponent<ProductionGuardianV09>();
                if (production == null) production = guardian.AddComponent<ProductionGuardianV09>();
                production.ConfigureRuntime(pearl, graphite, gold, aether);
                EditorUtility.SetDirty(production);
            }
        }
    }
}
#endif