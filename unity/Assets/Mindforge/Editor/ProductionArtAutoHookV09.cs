#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Mindforge.Presentation;

namespace Mindforge.Editor
{
    /// <summary>
    /// Transitional integration hook while the V0.9 production-art tranche is developed.
    /// V0.8 builders save the scene during the canonical one-click showcase rebuild. V0.9
    /// waits for the reference-fidelity root and, crucially, defers scene-save reactions to
    /// the next editor update. That lets the synchronous V0.8 crisp-geometry/enemy-scope
    /// calls finish before V0.9 replaces visible blockout presentation.
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
            if (production != null)
            {
                ProductionLegacyVisualQuarantineV09.ApplyOpenScene();
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

        private static void ApplyInternal(bool forceRebuild)
        {
            if (_applying) return;
            _applying = true;
            try
            {
                GameObject existing = EditorSceneLookup.FindIncludingInactive(ProductionArtV09Builder.RootName);
                if (forceRebuild || existing == null)
                    ProductionArtV09Builder.ApplyOpenScene();
                int quarantined = ProductionLegacyVisualQuarantineV09.ApplyOpenScene();
                EnsurePresentationComponents();
                int external = ExternalArtReplacementV09.ApplyOpenScene();
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                EditorSceneManager.SaveOpenScenes();
                AssetDatabase.SaveAssets();
                Debug.Log(
                    $"[Mindforge:V09:Art] Complete production presentation applied after V0.8 reference fidelity; " +
                    $"legacy renderers quarantined={quarantined}; local external replacements={external}.");
            }
            finally
            {
                _applying = false;
            }
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
