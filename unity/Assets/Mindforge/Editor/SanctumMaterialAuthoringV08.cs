#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace Mindforge.Editor
{
    /// <summary>
    /// Bright cathedral-city palette for V0.8. Reuses Mindforge's deterministic PBR maps and
    /// URP/Lit-compatible materials so the new opening changes art direction without adding a
    /// second render stack.
    /// </summary>
    public static class SanctumMaterialAuthoringV08
    {
        public const string Ivory = "SanctumIvoryV08";
        public const string Pearl = "SanctumPearlV08";
        public const string Gold = "SanctumGoldV08";
        public const string BlueGlass = "SanctumBlueGlassV08";
        public const string Water = "SanctumWaterV08";
        public const string Garden = "SanctumGardenV08";

        [MenuItem("Mindforge/Showcase/Author Sanctum Materials V0.8", priority = 23)]
        public static void EnsureAuthored()
        {
            CinematicMaterialAuthoring.EnsureAuthored();
            NeuralGothicMaterialAuthoringV07.EnsureAuthored();

            Material basalt = Require("ArenaBasalt");
            Material metal = Require("GuardianMetal");
            Material cyan = Require("AetherCyan");
            Material green = Require("WispVerdant");

            Configure(CloneOrUpdate(Ivory, basalt), new Color(0.82f, 0.86f, 0.89f, 1f), 0.03f, 0.62f, 1.55f, Color.black);
            Configure(CloneOrUpdate(Pearl, basalt), new Color(0.68f, 0.74f, 0.79f, 1f), 0.08f, 0.74f, 1.75f, Color.black);
            Configure(CloneOrUpdate(Gold, metal), new Color(0.66f, 0.48f, 0.17f, 1f), 0.92f, 0.78f, 2.10f, new Color(0.08f, 0.042f, 0.005f));
            Configure(CloneOrUpdate(BlueGlass, cyan), new Color(0.07f, 0.28f, 0.40f, 1f), 0.22f, 0.90f, 1.0f, new Color(0.02f, 0.22f, 0.35f));
            Configure(CloneOrUpdate(Water, cyan), new Color(0.025f, 0.18f, 0.28f, 1f), 0.04f, 0.96f, 1.0f, new Color(0.01f, 0.08f, 0.12f));
            Configure(CloneOrUpdate(Garden, green), new Color(0.055f, 0.21f, 0.11f, 1f), 0.02f, 0.28f, 2.0f, new Color(0.005f, 0.03f, 0.01f));

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Mindforge:V08] Bright ivory/gold/cyan sanctum palette authored from existing deterministic PBR assets.");
        }

        public static Material Load(string name) => CinematicMaterialAuthoring.Load(name);

        private static Material CloneOrUpdate(string name, Material source)
        {
            string path = $"{CinematicMaterialAuthoring.ResourceFolder}/{name}.mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing == null)
            {
                existing = new Material(source) { name = name };
                AssetDatabase.CreateAsset(existing, path);
            }
            CopyTexture(source, existing, "_BaseMap");
            CopyTexture(source, existing, "_BumpMap");
            CopyTexture(source, existing, "_MetallicGlossMap");
            CopyTexture(source, existing, "_OcclusionMap");
            return existing;
        }

        private static void Configure(Material material, Color tint, float metallic, float smoothness, float tile, Color emission)
        {
            if (material == null) return;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", tint);
            else material.color = tint;
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", Mathf.Clamp01(metallic));
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", Mathf.Clamp01(smoothness));
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission);
            }
            SetTiling(material, "_BaseMap", tile);
            SetTiling(material, "_BumpMap", tile);
            SetTiling(material, "_OcclusionMap", tile);
            EditorUtility.SetDirty(material);
        }

        private static void SetTiling(Material material, string property, float tile)
        {
            if (material.HasProperty(property)) material.SetTextureScale(property, Vector2.one * tile);
        }

        private static void CopyTexture(Material source, Material destination, string property)
        {
            if (source == null || destination == null || !source.HasProperty(property) || !destination.HasProperty(property)) return;
            Texture texture = source.GetTexture(property);
            if (texture == null) return;
            destination.SetTexture(property, texture);
            destination.SetTextureScale(property, source.GetTextureScale(property));
            destination.SetTextureOffset(property, source.GetTextureOffset(property));
        }

        private static Material Require(string name)
        {
            Material material = CinematicMaterialAuthoring.Load(name);
            if (material == null) throw new InvalidOperationException("Required material missing: " + name);
            return material;
        }
    }
}
#endif
