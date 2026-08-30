#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace Mindforge.Editor
{
    /// <summary>
    /// Bright cathedral-city palette for V0.8. It keeps Mindforge's deterministic normal,
    /// occlusion and metal-response vocabulary but intentionally replaces the dark Null Ward
    /// albedo with a clean base so ivory architecture actually reads as ivory.
    /// </summary>
    public static class SanctumMaterialAuthoringV08
    {
        public const string Ivory = "SanctumIvoryV08";
        public const string Pearl = "SanctumPearlV08";
        public const string Gold = "SanctumGoldV08";
        public const string BlueGlass = "SanctumBlueGlassV08";
        public const string Water = "SanctumWaterV08";
        public const string Garden = "SanctumGardenV08";
        public const string Sky = "SanctumSkyV08";

        [MenuItem("Mindforge/Showcase/Author Sanctum Materials V0.8", priority = 23)]
        public static void EnsureAuthored()
        {
            CinematicMaterialAuthoring.EnsureAuthored();
            NeuralGothicMaterialAuthoringV07.EnsureAuthored();

            Material basalt = Require("ArenaBasalt");
            Material metal = Require("GuardianMetal");
            Material cyan = Require("AetherCyan");
            Material green = Require("WispVerdant");

            Configure(CloneOrUpdate(Ivory, basalt), new Color(0.91f, 0.94f, 0.95f, 1f), 0.03f, 0.64f, 1.55f, Color.black, true);
            Configure(CloneOrUpdate(Pearl, basalt), new Color(0.75f, 0.81f, 0.85f, 1f), 0.08f, 0.76f, 1.75f, Color.black, true);
            Configure(CloneOrUpdate(Gold, metal), new Color(0.72f, 0.51f, 0.16f, 1f), 0.94f, 0.82f, 2.10f, new Color(0.06f, 0.025f, 0.002f), true);
            Configure(CloneOrUpdate(BlueGlass, cyan), new Color(0.055f, 0.30f, 0.46f, 1f), 0.18f, 0.92f, 1.0f, new Color(0.02f, 0.26f, 0.42f), true);
            Configure(CloneOrUpdate(Water, cyan), new Color(0.025f, 0.20f, 0.32f, 1f), 0.02f, 0.98f, 1.0f, new Color(0.005f, 0.09f, 0.15f), true);
            Configure(CloneOrUpdate(Garden, green), new Color(0.055f, 0.24f, 0.105f, 1f), 0.01f, 0.24f, 2.0f, new Color(0.004f, 0.025f, 0.008f), true);
            EnsureSky();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Mindforge:V08] Bright ivory/gold/cyan sanctum palette + blue procedural sky authored without a second render pipeline.");
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
            // Preserve surface relief and response maps, not the dark source albedo.
            CopyTexture(source, existing, "_BumpMap");
            CopyTexture(source, existing, "_MetallicGlossMap");
            CopyTexture(source, existing, "_OcclusionMap");
            return existing;
        }

        private static void Configure(
            Material material,
            Color tint,
            float metallic,
            float smoothness,
            float tile,
            Color emission,
            bool cleanBase)
        {
            if (material == null) return;
            if (cleanBase && material.HasProperty("_BaseMap"))
                material.SetTexture("_BaseMap", Texture2D.whiteTexture);
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

        private static void EnsureSky()
        {
            string path = $"{CinematicMaterialAuthoring.ResourceFolder}/{Sky}.mat";
            Material sky = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find("Skybox/Procedural");
            if (shader == null)
            {
                Debug.LogWarning("[Mindforge:V08] Skybox/Procedural unavailable; retaining project skybox.");
                return;
            }
            if (sky == null)
            {
                sky = new Material(shader) { name = Sky };
                AssetDatabase.CreateAsset(sky, path);
            }
            else if (sky.shader != shader)
            {
                sky.shader = shader;
            }

            SetIfPresent(sky, "_SunSize", 0.035f);
            SetIfPresent(sky, "_SunSizeConvergence", 5.0f);
            SetIfPresent(sky, "_AtmosphereThickness", 0.78f);
            SetIfPresent(sky, "_Exposure", 1.18f);
            if (sky.HasProperty("_SkyTint")) sky.SetColor("_SkyTint", new Color(0.22f, 0.53f, 0.86f, 1f));
            if (sky.HasProperty("_GroundColor")) sky.SetColor("_GroundColor", new Color(0.32f, 0.40f, 0.47f, 1f));
            EditorUtility.SetDirty(sky);
            RenderSettings.skybox = sky;
            DynamicGI.UpdateEnvironment();
        }

        private static void SetIfPresent(Material material, string property, float value)
        {
            if (material.HasProperty(property)) material.SetFloat(property, value);
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
