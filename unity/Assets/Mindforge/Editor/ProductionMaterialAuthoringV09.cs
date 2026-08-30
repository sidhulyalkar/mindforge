#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace Mindforge.Editor
{
    /// <summary>
    /// Editor-time material synthesis for the production-art pass. The important design
    /// change is that large surfaces no longer depend on a single flat base color: each
    /// material gets deterministic low-frequency structure, fine grain and a tangent-space
    /// normal texture. Assets are generated locally under Assets/Mindforge/Generated so
    /// the public repository stays text-first and can later swap to authored PBR scans.
    ///
    /// The procedural-texture workflow is conceptually informed by tools such as the
    /// MIT-licensed Material Maker project, but this implementation is original Unity/C#
    /// code and copies no upstream source or binary artwork.
    /// </summary>
    public static class ProductionMaterialAuthoringV09
    {
        public const string Root = "Assets/Mindforge/Generated/ProductionV09";
        public const string Ivory = "ProdIvoryStoneV09";
        public const string Pearl = "ProdPearlCeramicV09";
        public const string WarmStone = "ProdWarmStoneV09";
        public const string Graphite = "ProdGraphiteV09";
        public const string Gold = "ProdGoldV09";
        public const string Garden = "ProdGardenV09";
        public const string Water = "ProdWaterV09";
        public const string Glass = "ProdBlueGlassV09";

        private const int TextureSize = 256;

        [MenuItem("Mindforge/Showcase/Author Production Materials V0.9", priority = 41)]
        public static void EnsureAuthored()
        {
            EnsureFolder(Root);

            Texture2D ivoryAlbedo = EnsureSurfaceTexture("IvoryAlbedo", new Color(0.91f, 0.92f, 0.89f), 0.10f, 2.7f, 17.3f);
            Texture2D ivoryNormal = EnsureNormalTexture("IvoryNormal", 2.7f, 17.3f, 1.15f);
            Texture2D pearlAlbedo = EnsureSurfaceTexture("PearlAlbedo", new Color(0.80f, 0.86f, 0.88f), 0.075f, 3.9f, 31.7f);
            Texture2D pearlNormal = EnsureNormalTexture("PearlNormal", 3.9f, 31.7f, 0.85f);
            Texture2D warmAlbedo = EnsureSurfaceTexture("WarmStoneAlbedo", new Color(0.62f, 0.57f, 0.49f), 0.13f, 2.1f, 67.1f);
            Texture2D warmNormal = EnsureNormalTexture("WarmStoneNormal", 2.1f, 67.1f, 1.35f);
            Texture2D graphiteAlbedo = EnsureSurfaceTexture("GraphiteAlbedo", new Color(0.12f, 0.145f, 0.17f), 0.085f, 4.8f, 91.7f);
            Texture2D graphiteNormal = EnsureNormalTexture("GraphiteNormal", 4.8f, 91.7f, 0.95f);
            Texture2D gardenAlbedo = EnsureSurfaceTexture("GardenAlbedo", new Color(0.18f, 0.33f, 0.16f), 0.17f, 5.6f, 123.4f);
            Texture2D gardenNormal = EnsureNormalTexture("GardenNormal", 5.6f, 123.4f, 1.15f);

            EnsureLitMaterial(Ivory, ivoryAlbedo, ivoryNormal, new Color(0.99f, 0.99f, 0.97f), 0.03f, 0.54f, 0.72f);
            EnsureLitMaterial(Pearl, pearlAlbedo, pearlNormal, new Color(0.91f, 0.96f, 0.98f), 0.08f, 0.67f, 0.62f);
            EnsureLitMaterial(WarmStone, warmAlbedo, warmNormal, Color.white, 0.02f, 0.39f, 0.86f);
            EnsureLitMaterial(Graphite, graphiteAlbedo, graphiteNormal, new Color(0.58f, 0.64f, 0.72f), 0.40f, 0.43f, 0.65f);
            EnsureLitMaterial(Garden, gardenAlbedo, gardenNormal, Color.white, 0.0f, 0.25f, 0.9f);
            EnsureMetalMaterial(Gold, new Color(0.88f, 0.67f, 0.24f), 0.92f, 0.74f);
            EnsureTransparentMaterial(Water, new Color(0.08f, 0.34f, 0.48f, 0.66f), 0.86f, 0.10f);
            EnsureTransparentMaterial(Glass, new Color(0.18f, 0.52f, 0.70f, 0.34f), 0.93f, 0.02f);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static Material Load(string name)
            => AssetDatabase.LoadAssetAtPath<Material>($"{Root}/{name}.mat");

        private static Texture2D EnsureSurfaceTexture(string name, Color baseColor, float variation, float scale, float seed)
        {
            string path = $"{Root}/{name}.asset";
            Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing != null) return existing;

            Texture2D texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, true, false)
            {
                name = name,
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Trilinear,
                anisoLevel = 8,
            };

            Color[] pixels = new Color[TextureSize * TextureSize];
            for (int y = 0; y < TextureSize; y++)
            for (int x = 0; x < TextureSize; x++)
            {
                float u = x / (float)TextureSize;
                float v = y / (float)TextureSize;
                float n = Fractal(u, v, scale, seed);
                float vein = Mathf.Pow(Mathf.Abs(Fractal(u + 0.19f, v - 0.23f, scale * 0.43f, seed + 43.1f) - 0.5f) * 2f, 4f);
                float grain = (Mathf.PerlinNoise(u * 42f + seed, v * 42f + seed * 0.37f) - 0.5f) * 0.18f;
                float delta = (n - 0.5f) * variation + grain * variation - vein * variation * 0.42f;
                Color c = new Color(
                    Mathf.Clamp01(baseColor.r + delta),
                    Mathf.Clamp01(baseColor.g + delta),
                    Mathf.Clamp01(baseColor.b + delta),
                    1f);
                pixels[y * TextureSize + x] = c;
            }
            texture.SetPixels(pixels);
            texture.Apply(true, false);
            AssetDatabase.CreateAsset(texture, path);
            return texture;
        }

        private static Texture2D EnsureNormalTexture(string name, float scale, float seed, float strength)
        {
            string path = $"{Root}/{name}.asset";
            Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing != null) return existing;

            Texture2D texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, true, true)
            {
                name = name,
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Trilinear,
                anisoLevel = 8,
            };
            Color[] pixels = new Color[TextureSize * TextureSize];
            float px = 1f / TextureSize;
            for (int y = 0; y < TextureSize; y++)
            for (int x = 0; x < TextureSize; x++)
            {
                float u = x / (float)TextureSize;
                float v = y / (float)TextureSize;
                float hx0 = Fractal(u - px, v, scale, seed);
                float hx1 = Fractal(u + px, v, scale, seed);
                float hy0 = Fractal(u, v - px, scale, seed);
                float hy1 = Fractal(u, v + px, scale, seed);
                Vector3 normal = new Vector3((hx0 - hx1) * strength, (hy0 - hy1) * strength, 1f).normalized;
                pixels[y * TextureSize + x] = new Color(normal.x * 0.5f + 0.5f, normal.y * 0.5f + 0.5f, normal.z * 0.5f + 0.5f, 1f);
            }
            texture.SetPixels(pixels);
            texture.Apply(true, false);
            AssetDatabase.CreateAsset(texture, path);
            return texture;
        }

        private static float Fractal(float u, float v, float scale, float seed)
        {
            float sum = 0f;
            float amplitude = 0.55f;
            float frequency = scale;
            float norm = 0f;
            for (int octave = 0; octave < 5; octave++)
            {
                sum += Mathf.PerlinNoise(u * frequency + seed, v * frequency + seed * 0.63f) * amplitude;
                norm += amplitude;
                amplitude *= 0.52f;
                frequency *= 2.03f;
            }
            return norm > 0f ? sum / norm : 0.5f;
        }

        private static Material EnsureLitMaterial(string name, Texture2D albedo, Texture2D normal, Color tint, float metallic, float smoothness, float bumpScale)
        {
            string path = $"{Root}/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) throw new InvalidOperationException("URP/Lit shader is required for V0.9 production materials.");
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetTexture("_BaseMap", albedo);
            material.SetColor("_BaseColor", tint);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Smoothness", smoothness);
            material.SetTexture("_BumpMap", normal);
            material.SetFloat("_BumpScale", bumpScale);
            material.SetFloat("_OcclusionStrength", 0.9f);
            material.EnableKeyword("_NORMALMAP");
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material EnsureMetalMaterial(string name, Color color, float metallic, float smoothness)
        {
            string path = $"{Root}/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) throw new InvalidOperationException("URP/Lit shader is required for V0.9 production materials.");
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material EnsureTransparentMaterial(string name, Color color, float smoothness, float metallic)
        {
            string path = $"{Root}/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) throw new InvalidOperationException("URP/Lit shader is required for V0.9 production materials.");
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_ZWrite", 0f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = 3000;
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetShaderPassEnabled("ShadowCaster", false);
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
