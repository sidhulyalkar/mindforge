#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace Mindforge.Editor
{
    /// <summary>
    /// Small material palette for the V0.7 modular world kit. It reuses the deterministic
    /// PBR texture maps authored by CinematicMaterialAuthoring rather than introducing a
    /// shader zoo. The visual identity comes from controlled albedo/metal/smoothness and
    /// emission hierarchy on top of the existing URP/Lit-compatible assets.
    /// </summary>
    public static class NeuralGothicMaterialAuthoringV07
    {
        public const string Stone = "CloisterStoneV07";
        public const string DarkStone = "CloisterDarkStoneV07";
        public const string Metal = "CloisterMetalV07";
        public const string Patina = "CloisterPatinaV07";
        public const string AshStone = "CloisterAshStoneV07";

        [MenuItem("Mindforge/Legacy/Showcase/Author Neural-Gothic Materials V0.7", priority = 22)]
        public static void EnsureAuthored()
        {
            CinematicMaterialAuthoring.EnsureAuthored();

            Material basalt = Require("ArenaBasalt");
            Material obsidian = Require("ObsidianArchitecture");
            Material metal = Require("GuardianMetal");

            Material stone = CloneOrUpdate(Stone, basalt);
            ConfigureSurface(stone, new Color(0.095f, 0.105f, 0.128f, 1f), 0.06f, 0.30f, 2.65f);

            Material dark = CloneOrUpdate(DarkStone, obsidian);
            ConfigureSurface(dark, new Color(0.040f, 0.047f, 0.068f, 1f), 0.14f, 0.46f, 2.25f);

            Material structural = CloneOrUpdate(Metal, metal);
            ConfigureSurface(structural, new Color(0.19f, 0.22f, 0.27f, 1f), 0.86f, 0.52f, 2.85f);

            Material patina = CloneOrUpdate(Patina, metal);
            ConfigureSurface(patina, new Color(0.070f, 0.155f, 0.145f, 1f), 0.72f, 0.34f, 3.15f);

            Material ash = CloneOrUpdate(AshStone, basalt);
            ConfigureSurface(ash, new Color(0.145f, 0.145f, 0.155f, 1f), 0.02f, 0.22f, 2.35f);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Mindforge:WorldV07] Neural-gothic material palette authored from the existing deterministic PBR library.");
        }

        public static Material Load(string name) => CinematicMaterialAuthoring.Load(name);

        private static Material CloneOrUpdate(string name, Material source)
        {
            string path = $"{CinematicMaterialAuthoring.ResourceFolder}/{name}.mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                CopyTextureProperty(source, existing, "_BaseMap");
                CopyTextureProperty(source, existing, "_BumpMap");
                CopyTextureProperty(source, existing, "_MetallicGlossMap");
                CopyTextureProperty(source, existing, "_OcclusionMap");
                return existing;
            }

            Material material = new Material(source) { name = name };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void ConfigureSurface(Material material, Color tint, float metallic, float smoothness, float tile)
        {
            if (material == null) return;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", tint);
            else material.color = tint;
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", Mathf.Clamp01(metallic));
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", Mathf.Clamp01(smoothness));
            if (material.HasProperty("_BaseMap")) material.SetTextureScale("_BaseMap", new Vector2(tile, tile));
            if (material.HasProperty("_BumpMap")) material.SetTextureScale("_BumpMap", new Vector2(tile, tile));
            if (material.HasProperty("_OcclusionMap")) material.SetTextureScale("_OcclusionMap", new Vector2(tile, tile));
            EditorUtility.SetDirty(material);
        }

        private static void CopyTextureProperty(Material source, Material destination, string property)
        {
            if (source == null || destination == null) return;
            if (!source.HasProperty(property) || !destination.HasProperty(property)) return;
            Texture texture = source.GetTexture(property);
            if (texture == null) return;
            destination.SetTexture(property, texture);
            destination.SetTextureScale(property, source.GetTextureScale(property));
            destination.SetTextureOffset(property, source.GetTextureOffset(property));
        }

        private static Material Require(string name)
        {
            Material material = CinematicMaterialAuthoring.Load(name);
            if (material == null) throw new InvalidOperationException("Required cinematic material missing: " + name);
            return material;
        }
    }
}
#endif
