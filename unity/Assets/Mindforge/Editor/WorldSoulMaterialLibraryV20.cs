#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace Mindforge.Editor
{
    /// <summary>
    /// Reproducible surface authoring for the canonical demo world.
    ///
    /// V0.11 intentionally used flat-color URP/Lit materials to establish a clean scene.
    /// V0.20 keeps URP/Lit as the stable shader authority but feeds it deterministic,
    /// generated albedo breakup so large walls, cliffs and terrain stop reading as primitives.
    /// Generated assets live under Assets/Mindforge/Generated and remain reproducible/ignored.
    /// </summary>
    public static class WorldSoulMaterialLibraryV20
    {
        public const string Root = "Assets/Mindforge/Generated/V20/Materials";
        public const string TextureRoot = "Assets/Mindforge/Generated/V20/Textures";

        public sealed class Palette
        {
            public Material Limestone;
            public Material Basalt;
            public Material WornStone;
            public Material Earth;
            public Material Moss;
            public Material Bark;
            public Material Foliage;
            public Material Water;
            public Material EmberStone;
            public Material Skybox;
        }

        public static Palette Ensure()
        {
            EnsureFolder(Root);
            EnsureFolder(TextureRoot);

            Texture2D limestone = EnsureSurfaceTexture(
                "LimestoneAlbedo", 160, 20011,
                new Color(0.25f, 0.27f, 0.27f), new Color(0.59f, 0.58f, 0.53f),
                0.28f, 0.05f);
            Texture2D basalt = EnsureSurfaceTexture(
                "BasaltAlbedo", 160, 20021,
                new Color(0.025f, 0.031f, 0.044f), new Color(0.13f, 0.15f, 0.17f),
                0.42f, 0.01f);
            Texture2D worn = EnsureSurfaceTexture(
                "WornStoneAlbedo", 160, 20031,
                new Color(0.10f, 0.115f, 0.12f), new Color(0.38f, 0.39f, 0.37f),
                0.34f, 0.03f);
            Texture2D earth = EnsureSurfaceTexture(
                "EarthAlbedo", 160, 20041,
                new Color(0.035f, 0.030f, 0.029f), new Color(0.16f, 0.13f, 0.10f),
                0.16f, 0.08f);
            Texture2D moss = EnsureSurfaceTexture(
                "MossAlbedo", 160, 20051,
                new Color(0.025f, 0.055f, 0.040f), new Color(0.16f, 0.27f, 0.15f),
                0.10f, 0.38f);
            Texture2D bark = EnsureSurfaceTexture(
                "BarkAlbedo", 160, 20061,
                new Color(0.025f, 0.020f, 0.020f), new Color(0.18f, 0.11f, 0.075f),
                0.48f, 0.02f);
            Texture2D foliage = EnsureSurfaceTexture(
                "FoliageAlbedo", 160, 20071,
                new Color(0.018f, 0.052f, 0.035f), new Color(0.13f, 0.31f, 0.19f),
                0.10f, 0.46f);
            Texture2D water = EnsureSurfaceTexture(
                "WaterAlbedo", 160, 20081,
                new Color(0.018f, 0.065f, 0.085f), new Color(0.075f, 0.20f, 0.23f),
                0.04f, 0.01f);
            Texture2D ember = EnsureSurfaceTexture(
                "EmberStoneAlbedo", 160, 20091,
                new Color(0.055f, 0.020f, 0.028f), new Color(0.22f, 0.055f, 0.075f),
                0.36f, 0.01f);

            Palette palette = new Palette
            {
                Limestone = EnsureLit("WorldLimestone", limestone, Color.white, 0.03f, 0.34f, new Vector2(4.5f, 4.5f)),
                Basalt = EnsureLit("WorldBasalt", basalt, Color.white, 0.08f, 0.28f, new Vector2(5.5f, 5.5f)),
                WornStone = EnsureLit("WorldWornStone", worn, Color.white, 0.05f, 0.31f, new Vector2(5.0f, 5.0f)),
                Earth = EnsureLit("WorldEarth", earth, Color.white, 0.0f, 0.18f, new Vector2(6.0f, 6.0f)),
                Moss = EnsureLit("WorldMoss", moss, Color.white, 0.0f, 0.22f, new Vector2(5.0f, 5.0f)),
                Bark = EnsureLit("WorldBark", bark, Color.white, 0.0f, 0.24f, new Vector2(3.5f, 5.5f)),
                Foliage = EnsureLit("WorldFoliage", foliage, new Color(0.86f, 0.95f, 0.88f), 0.0f, 0.20f, new Vector2(4.0f, 4.0f)),
                Water = EnsureLit("WorldWater", water, new Color(0.72f, 0.92f, 1f), 0.12f, 0.91f, new Vector2(3.0f, 8.0f)),
                EmberStone = EnsureLit("WorldEmberStone", ember, Color.white, 0.10f, 0.38f, new Vector2(4.0f, 4.0f)),
                Skybox = EnsureSkybox(),
            };

            return palette;
        }

        private static Texture2D EnsureSurfaceTexture(
            string name,
            int size,
            int seed,
            Color low,
            Color high,
            float crackStrength,
            float organicStrength)
        {
            string path = $"{TextureRoot}/{name}.asset";
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null || texture.width != size || texture.height != size)
            {
                if (texture != null) AssetDatabase.DeleteAsset(path);
                texture = new Texture2D(size, size, TextureFormat.RGBA32, true, false)
                {
                    name = name,
                    wrapMode = TextureWrapMode.Repeat,
                    filterMode = FilterMode.Trilinear,
                    anisoLevel = 4,
                };
                AssetDatabase.CreateAsset(texture, path);
            }

            Color[] pixels = new Color[size * size];
            float inv = 1f / Mathf.Max(1, size - 1);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float px = x * inv * 48f;
                    float py = y * inv * 48f;
                    float broad = WorldSoulNoiseV20.Fbm(px, py, seed, 5, 13f, 0.54f, 2.05f) * 0.5f + 0.5f;
                    float fine = WorldSoulNoiseV20.Fbm(px + 29.1f, py - 17.6f, seed ^ 0x13579B, 3, 3.8f, 0.46f, 2.3f) * 0.5f + 0.5f;
                    float ridge = WorldSoulNoiseV20.Ridge(px + 7.3f, py + 13.9f, seed ^ 0x2468AC, 6.2f);
                    float crackField = Mathf.Abs(WorldSoulNoiseV20.Fbm(px - 11.4f, py + 5.2f, seed ^ 0x55AA11, 3, 4.8f, 0.53f, 2.1f));
                    float crack = 1f - Mathf.SmoothStep(0.025f, 0.14f, crackField);
                    float organic = Mathf.Pow(Mathf.Clamp01((fine - 0.52f) * 2.25f), 2.2f);

                    float value = Mathf.Clamp01(broad * 0.72f + fine * 0.20f + ridge * 0.08f);
                    Color color = Color.Lerp(low, high, value);
                    color *= 1f - crack * Mathf.Clamp01(crackStrength) * 0.48f;
                    color = Color.Lerp(color, new Color(0.18f, 0.30f, 0.13f), organic * Mathf.Clamp01(organicStrength));
                    pixels[y * size + x] = new Color(color.r, color.g, color.b, 1f);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(true, false);
            texture.wrapMode = TextureWrapMode.Repeat;
            texture.filterMode = FilterMode.Trilinear;
            texture.anisoLevel = 4;
            EditorUtility.SetDirty(texture);
            return texture;
        }

        private static Material EnsureLit(
            string name,
            Texture2D albedo,
            Color tint,
            float metallic,
            float smoothness,
            Vector2 tiling)
        {
            string path = $"{Root}/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                if (shader == null) throw new InvalidOperationException("V0.20 requires URP/Lit or Standard shader support.");
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }

            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", albedo);
                material.SetTextureScale("_BaseMap", tiling);
            }
            else if (material.HasProperty("_MainTex"))
            {
                material.SetTexture("_MainTex", albedo);
                material.SetTextureScale("_MainTex", tiling);
            }

            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", tint);
            else if (material.HasProperty("_Color")) material.SetColor("_Color", tint);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", Mathf.Clamp01(metallic));
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", Mathf.Clamp01(smoothness));
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material EnsureSkybox()
        {
            string path = $"{Root}/WorldSkybox.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Skybox/Procedural");
                if (shader == null) return null;
                material = new Material(shader) { name = "WorldSkybox" };
                AssetDatabase.CreateAsset(material, path);
            }

            if (material.HasProperty("_SkyTint")) material.SetColor("_SkyTint", new Color(0.26f, 0.34f, 0.46f));
            if (material.HasProperty("_GroundColor")) material.SetColor("_GroundColor", new Color(0.045f, 0.052f, 0.060f));
            if (material.HasProperty("_AtmosphereThickness")) material.SetFloat("_AtmosphereThickness", 0.72f);
            if (material.HasProperty("_Exposure")) material.SetFloat("_Exposure", 0.92f);
            if (material.HasProperty("_SunSize")) material.SetFloat("_SunSize", 0.028f);
            if (material.HasProperty("_SunSizeConvergence")) material.SetFloat("_SunSizeConvergence", 4.2f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void EnsureFolder(string fullPath)
        {
            string[] parts = fullPath.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
#endif
