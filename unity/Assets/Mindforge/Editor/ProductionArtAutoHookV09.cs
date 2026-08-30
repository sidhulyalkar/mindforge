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
    /// waits specifically for the reference-fidelity root, preventing the production layer
    /// from running halfway through the V0.8 visual sequence. It then installs the compact
    /// HUD and smooth Guardian shell and applies optional local licensed-art substitutions.
    /// </summary>
    [InitializeOnLoad]
    public static class ProductionArtAutoHookV09
    {
        private static bool _applying;

        static ProductionArtAutoHookV09()
        {
            EditorApplication.delayCall += TryApply;
            EditorSceneManager.sceneSaved += _ => TryApply();
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
                EnsurePresentationComponents();
                int external = ExternalArtReplacementV09.ApplyOpenScene();
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                EditorSceneManager.SaveOpenScenes();
                AssetDatabase.SaveAssets();
                Debug.Log($"[Mindforge:V09:Art] Complete production presentation applied after V0.8 reference fidelity; local external replacements={external}.");
            }
            finally
            {
                _applying = false;
            }
        }

        private static void EnsurePresentationComponents()
        {
            ProductionMaterialAuthoringV09.EnsureAuthored();
            GameObject arena = EditorSceneLookup.FindIncludingInactive("Fractured_Signal_Arena");
            GameObject guardian = EditorSceneLookup.FindIncludingInactive("Guardian");
            if (arena != null)
            {
                ProductionHudV09 hud = arena.GetComponent<ProductionHudV09>();
                if (hud == null) hud = arena.AddComponent<ProductionHudV09>();
                EditorUtility.SetDirty(hud);
            }
            if (guardian != null)
            {
                ProductionGuardianV09 production = guardian.GetComponent<ProductionGuardianV09>();
                if (production == null) production = guardian.AddComponent<ProductionGuardianV09>();
                production.ConfigureRuntime(
                    ProductionMaterialAuthoringV09.Load(ProductionMaterialAuthoringV09.Pearl),
                    ProductionMaterialAuthoringV09.Load(ProductionMaterialAuthoringV09.Graphite),
                    ProductionMaterialAuthoringV09.Load(ProductionMaterialAuthoringV09.Gold),
                    CinematicMaterialAuthoring.Load("AetherCyan"));
                EditorUtility.SetDirty(production);
            }
        }
    }
}
#endif
