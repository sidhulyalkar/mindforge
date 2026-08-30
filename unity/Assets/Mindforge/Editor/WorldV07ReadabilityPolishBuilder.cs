#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Mindforge.Calibration;
using Mindforge.Presentation;
using Mindforge.World;

namespace Mindforge.Editor
{
    /// <summary>
    /// Small downstream polish pass over WorldV07Builder. It adds only two reusable
    /// presentation concerns that are easier to validate independently: pointed arch crowns
    /// for generated shared seams and runtime BCI-safe scaling for decorative V0.7 lights.
    /// </summary>
    public static class WorldV07ReadabilityPolishBuilder
    {
        public const string Revision = "NEURAL_GOTHIC_READABILITY_POLISH_V07";
        private const string AnnexName = "Neural_Cloister_Procedural_Annex";
        private const string DecorativeLightRootName = "World_Light_Rhythm_V07";

        [MenuItem("Mindforge/Showcase/Apply Neural-Gothic Readability Polish V0.7", priority = 36)]
        public static void ApplyOpenScene()
        {
            GameObject v07Root = EditorSceneLookup.FindIncludingInactive(WorldV07Builder.RootName);
            GameObject annex = EditorSceneLookup.FindIncludingInactive(AnnexName);
            if (v07Root == null || annex == null)
                throw new InvalidOperationException(
                    "Neural-Gothic readability polish requires World V0.7 and its generated Neural Cloister annex.");

            Transform lightRoot = v07Root.transform.Find(DecorativeLightRootName);
            if (lightRoot == null)
                throw new InvalidOperationException(
                    "Neural-Gothic readability polish could not find the V0.7 decorative light rhythm root.");

            NeuralGothicMaterialAuthoringV07.EnsureAuthored();
            Material architecture = RequireMaterial(NeuralGothicMaterialAuthoringV07.DarkStone);
            Material signal = RequireMaterial("AetherCyan");

            NeuralGothicArchPolishV07 arches = annex.GetComponent<NeuralGothicArchPolishV07>();
            if (arches == null) arches = annex.AddComponent<NeuralGothicArchPolishV07>();
            arches.ConfigureRuntime(architecture, signal, archBudget: 24);
            int archCount = arches.Rebuild();

            BciSafeDecorativeLightingV07 lighting = v07Root.GetComponent<BciSafeDecorativeLightingV07>();
            if (lighting == null) lighting = v07Root.AddComponent<BciSafeDecorativeLightingV07>();
            AwakeningCalibrationDirector calibration = UnityEngine.Object.FindObjectOfType<AwakeningCalibrationDirector>(true);
            lighting.ConfigureRuntime(lightRoot, calibration, showcaseScale: 1f, bciScale: 0.38f);

            NeuralGothicWorldArtAuditV07 audit = v07Root.GetComponent<NeuralGothicWorldArtAuditV07>();
            bool budgetPass = audit == null || audit.Evaluate(true);
            if (!budgetPass)
                Debug.LogWarning(
                    "[Mindforge:WorldV07] Readability polish exceeded the current V0.7 scene-art budget; " +
                    "reduce decorative density before promotion.");

            EditorUtility.SetDirty(v07Root);
            EditorUtility.SetDirty(annex);
            EditorUtility.SetDirty(arches);
            EditorUtility.SetDirty(lighting);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                $"[Mindforge:WorldV07] Readability polish installed {archCount} shared pointed arches and BCI-safe control for " +
                $"{lighting.ControlledLightCount} decorative lights. No collider, topology, interaction, persistence, combat or coded-stimulus authority was added.");
        }

        private static Material RequireMaterial(string name)
        {
            Material material = CinematicMaterialAuthoring.Load(name);
            if (material == null)
                throw new InvalidOperationException("V0.7 readability polish required material missing: " + name);
            return material;
        }
    }
}
#endif
