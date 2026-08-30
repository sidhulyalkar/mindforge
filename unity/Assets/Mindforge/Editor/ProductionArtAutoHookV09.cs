#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Mindforge.Presentation;

namespace Mindforge.Editor
{
    /// <summary>
    /// Transitional integration hook while the V0.9 production-art tranche is developed.
    /// V0.8 builders save the scene during the canonical one-click showcase rebuild; this
    /// hook observes that save and immediately applies V0.9 if it is not already present.
    /// It also installs the compact HUD and smooth Guardian shell using generated materials.
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
            GameObject sanctum = EditorSceneLookup.FindIncludingInactive(SanctumOnboardingV08Builder.RootName);
            if (sanctum == null)
            {
                Debug.LogWarning("[Mindforge:V09:Art] Build the cinematic showcase first; Sanctum V0.8 is not present.");
                return;
            }
            ApplyInternal(true);
        }

        private static void TryApply()
        {
            if (_applying || EditorApplication.isPlayingOrWillChangePlaymode) return;
            GameObject sanctum = EditorSceneLookup.FindIncludingInactive(SanctumOnboardingV08Builder.RootName);
            if (sanctum == null) return;
            GameObject production = EditorSceneLookup.FindIncludingInactive(ProductionArtV09Builder.RootName);
            if (production != null)
            {
                EnsurePresentationComponents();
                return;
            }
            ApplyInternal(false);
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
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                EditorSceneManager.SaveOpenScenes();
                AssetDatabase.SaveAssets();
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
