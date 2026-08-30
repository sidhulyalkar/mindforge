#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Mindforge.Presentation;
using Mindforge.World;

namespace Mindforge.Editor
{
    /// <summary>
    /// Thin editor integration for the V0.7 neural-gothic presentation layer.
    /// Geometry authority remains in Grounded World / V0.6 generation. This builder only
    /// binds materials and asks the reusable presentation kit to decorate existing cells.
    /// </summary>
    public static class NeuralGothicWorldV07Builder
    {
        public const string Revision = "NEURAL_GOTHIC_WORLD_V07";
        private const string CloisterName = "Neural_Cloister_Procedural_Annex";

        [MenuItem("Mindforge/Showcase/Apply Neural Gothic World V0.7", priority = 35)]
        public static void ApplyOpenScene()
        {
            GameObject v06Root = EditorSceneLookup.FindIncludingInactive(WorldV06Builder.RootName);
            if (v06Root == null)
                throw new InvalidOperationException(
                    "Neural Gothic World V0.7 requires Persistent World V0.6 to be installed first.");

            Transform annex = v06Root.transform.Find(CloisterName);
            if (annex == null)
                throw new InvalidOperationException(
                    "Neural Gothic World V0.7 could not find the generated Neural Cloister annex.");

            ModularWorldAssemblerV06 assembler = annex.GetComponent<ModularWorldAssemblerV06>();
            if (assembler == null)
                throw new InvalidOperationException(
                    "Neural Gothic World V0.7 requires ModularWorldAssemblerV06 on the Neural Cloister annex.");

            NeuralGothicWorldKitV07 kit = annex.GetComponent<NeuralGothicWorldKitV07>();
            if (kit == null) kit = annex.gameObject.AddComponent<NeuralGothicWorldKitV07>();

            Material obsidian = FindMaterial("ObsidianArchitecture");
            Material metal = FindMaterial("GuardianMetal");
            Material cyan = FindMaterial("AetherCyan");
            Material green = FindMaterial("WispVerdant");

            kit.ConfigureRuntime(
                assembler,
                obsidian,
                metal,
                cyan,
                green,
                seed: 70713,
                tier: 2);

            if (!kit.Rebuild())
                throw new InvalidOperationException(
                    "Neural Gothic World V0.7 could not decorate the generated Neural Cloister cells.");

            EditorUtility.SetDirty(annex.gameObject);
            EditorUtility.SetDirty(kit);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();

            Debug.Log(
                "[Mindforge:WorldV07] Neural Cloister presentation upgraded with deterministic pointed thresholds, " +
                "flying buttresses, route traces, oculi and a far-east cathedral crown. The pass is static, collider-free " +
                "and downstream of V0.6 generation, so traversal, persistence, contextual E and coded neural stimuli remain authoritative.");
        }

        private static Material FindMaterial(string name)
        {
            string[] guids = AssetDatabase.FindAssets(name + " t:Material", new[] { "Assets/Mindforge" });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material != null && string.Equals(material.name, name, StringComparison.OrdinalIgnoreCase)) return material;
            }

            return guids.Length > 0
                ? AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guids[0]))
                : null;
        }
    }
}
#endif
