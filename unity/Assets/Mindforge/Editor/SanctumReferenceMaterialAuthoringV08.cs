#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace Mindforge.Editor
{
    /// <summary>
    /// Secondary reference-fidelity palette for the bright Sanctum. These materials add
    /// edge separation and hostile identity without stealing cyan/green from the neural
    /// Sight/Guard vocabulary.
    /// </summary>
    public static class SanctumReferenceMaterialAuthoringV08
    {
        public const string EdgeDark = "SanctumEdgeDarkV08";
        public const string WarmStone = "SanctumWarmStoneV08";
        public const string EnemyCeramic = "SanctumEnemyCeramicV08";
        public const string ThreatAmber = "SanctumThreatAmberV08";
        public const string ThreatWhite = "SanctumThreatWhiteV08";

        [MenuItem("Mindforge/Legacy/Showcase/Author Sanctum Reference Materials V0.8", priority = 24)]
        public static void EnsureAuthored()
        {
            SanctumMaterialAuthoringV08.EnsureAuthored();
            Material basalt = Require("ArenaBasalt");
            Material metal = Require("GuardianMetal");
            Material fractured = Require("FracturedCore");

            Configure(CloneOrUpdate(EdgeDark, basalt), new Color(0.055f, 0.070f, 0.085f, 1f), 0.22f, 0.68f, Color.black);
            Configure(CloneOrUpdate(WarmStone, basalt), new Color(0.74f, 0.72f, 0.66f, 1f), 0.04f, 0.58f, Color.black);
            Configure(CloneOrUpdate(EnemyCeramic, metal), new Color(0.27f, 0.31f, 0.34f, 1f), 0.58f, 0.73f, Color.black);
            Configure(CloneOrUpdate(ThreatAmber, fractured), new Color(0.84f, 0.28f, 0.045f, 1f), 0.34f, 0.80f, new Color(0.90f, 0.13f, 0.012f, 1f));
            Configure(CloneOrUpdate(ThreatWhite, fractured), new Color(0.93f, 0.95f, 0.96f, 1f), 0.12f, 0.86f, new Color(0.30f, 0.33f, 0.35f, 1f));

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static Material Load(string name) => CinematicMaterialAuthoring.Load(name);

        private static Material CloneOrUpdate(string name, Material source)
        {
            string path = $"{CinematicMaterialAuthoring.ResourceFolder}/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(source) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            CopyTexture(source, material, "_BumpMap");
            CopyTexture(source, material, "_MetallicGlossMap");
            CopyTexture(source, material, "_OcclusionMap");
            return material;
        }

        private static void Configure(Material material, Color baseColor, float metallic, float smoothness, Color emission)
        {
            if (material == null) return;
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", Texture2D.whiteTexture);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", baseColor);
            else material.color = baseColor;
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", Mathf.Clamp01(metallic));
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", Mathf.Clamp01(smoothness));
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission);
            }
            EditorUtility.SetDirty(material);
        }

        private static void CopyTexture(Material source, Material destination, string property)
        {
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
            if (material == null) material = SanctumMaterialAuthoringV08.Load(name);
            if (material == null) throw new InvalidOperationException("Required reference material source missing: " + name);
            return material;
        }
    }
}
#endif