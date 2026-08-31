#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Mindforge.Editor
{
    /// <summary>
    /// Deterministic editor-time PBR material authoring for the source-only showcase.
    /// These generated surfaces are deliberately an intermediate art layer: they give
    /// lighting real albedo/normal/occlusion/metallic/smoothness response while final
    /// scanned/authored assets can later replace them through the same material names.
    /// </summary>
    public static class CinematicMaterialAuthoring
    {
        public const string ResourceFolder = "Assets/Mindforge/Resources/Cinematic";
        private const int TextureSize = 512;

        [MenuItem("Mindforge/Legacy/Showcase/Author Cinematic PBR Materials", priority = 21)]
        public static void EnsureAuthored()
        {
            EnsureFolders();

            AuthorSurface("ArenaBasalt", SurfaceKind.Basalt, new Color(0.050f, 0.055f, 0.070f), 0.03f, 0.30f, 11.5f, 0.72f);
            AuthorSurface("ObsidianArchitecture", SurfaceKind.Obsidian, new Color(0.030f, 0.034f, 0.046f), 0.12f, 0.42f, 7.0f, 0.90f);
            AuthorSurface("GuardianMetal", SurfaceKind.WornMetal, new Color(0.18f, 0.21f, 0.28f), 0.88f, 0.58f, 13.0f, 0.66f);
            AuthorSurface("GuardianArmor", SurfaceKind.WornMetal, new Color(0.105f, 0.125f, 0.180f), 0.92f, 0.62f, 15.0f, 0.62f);
            AuthorSurface("GuardianCloth", SurfaceKind.Cloth, new Color(0.030f, 0.040f, 0.072f), 0.00f, 0.20f, 25.0f, 0.48f);
            AuthorSurface("FracturedShard", SurfaceKind.Obsidian, new Color(0.055f, 0.015f, 0.080f), 0.48f, 0.46f, 8.0f, 0.92f);

            AuthorEmission("GuardianAether", new Color(0.10f, 0.42f, 1.00f), 2.1f, 0.42f, 0.74f);
            AuthorEmission("AetherCyan", new Color(0.08f, 0.50f, 1.00f), 2.8f, 0.36f, 0.70f);
            AuthorEmission("FractureViolet", new Color(0.58f, 0.14f, 1.00f), 2.7f, 0.34f, 0.67f);
            AuthorEmission("FractureEmber", new Color(1.00f, 0.10f, 0.055f), 3.0f, 0.26f, 0.61f);
            AuthorEmission("WispVerdant", new Color(0.08f, 1.00f, 0.42f), 2.5f, 0.25f, 0.68f);
            AuthorEmission("FracturedCore", new Color(1.00f, 0.055f, 0.18f), 3.2f, 0.56f, 0.76f);
            AuthorEmission("FracturedRing", new Color(0.56f, 0.11f, 1.00f), 2.5f, 0.36f, 0.72f);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Mindforge:Cinematic] Deterministic PBR material library authored under Resources/Cinematic.");
        }

        public static Material Load(string name)
            => AssetDatabase.LoadAssetAtPath<Material>($"{ResourceFolder}/{name}.mat");

        private enum SurfaceKind { Basalt, Obsidian, WornMetal, Cloth }

        private static void AuthorSurface(
            string name,
            SurfaceKind kind,
            Color baseColor,
            float metallic,
            float meanSmoothness,
            float noiseScale,
            float normalStrength)
        {
            int seed = StableSeed(name);
            float[] height = BuildHeight(kind, seed, noiseScale);
            Color[] albedoPixels = new Color[TextureSize * TextureSize];
            Color[] normalPixels = BuildNormal(height, normalStrength);
            Color[] maskPixels = new Color[TextureSize * TextureSize];
            Color[] occlusionPixels = new Color[TextureSize * TextureSize];

            for (int y = 0; y < TextureSize; y++)
            for (int x = 0; x < TextureSize; x++)
            {
                int i = y * TextureSize + x;
                float h = height[i];
                float n = Fbm(x, y, seed + 53, noiseScale * 2.2f);
                float grain = Fbm(x, y, seed + 101, noiseScale * 6.0f);
                float cavity = Mathf.Clamp01((0.54f - h) * 2.4f);

                Color tint;
                switch (kind)
                {
                    case SurfaceKind.WornMetal:
                        float wear = Mathf.SmoothStep(0.58f, 0.92f, n);
                        tint = Color.Lerp(baseColor * 0.68f, baseColor * 1.35f, wear);
                        break;
                    case SurfaceKind.Cloth:
                        float weave = 0.5f + 0.5f * Mathf.Sin(x * 0.44f + Mathf.Sin(y * 0.13f)) * Mathf.Sin(y * 0.52f);
                        tint = baseColor * Mathf.Lerp(0.68f, 1.18f, 0.52f * n + 0.48f * weave);
                        break;
                    case SurfaceKind.Obsidian:
                        tint = baseColor * Mathf.Lerp(0.55f, 1.52f, Mathf.Pow(n, 1.8f));
                        break;
                    default:
                        tint = baseColor * Mathf.Lerp(0.62f, 1.30f, 0.72f * n + 0.28f * grain);
                        break;
                }

                albedoPixels[i] = new Color(tint.r, tint.g, tint.b, 1f);
                float smoothVariation = meanSmoothness + (n - 0.5f) * 0.24f - cavity * 0.16f;
                float metalVariation = Mathf.Clamp01(metallic + (grain - 0.5f) * (kind == SurfaceKind.WornMetal ? 0.16f : 0.04f));
                maskPixels[i] = new Color(metalVariation, 0f, 0f, Mathf.Clamp01(smoothVariation));
                float occ = Mathf.Clamp01(1f - cavity * 0.56f - Mathf.Max(0f, 0.42f - h) * 0.20f);
                occlusionPixels[i] = new Color(occ, occ, occ, 1f);
            }

            Texture2D albedo = WriteTexture(name + "_Albedo", albedoPixels, false);
            Texture2D normal = WriteTexture(name + "_Normal", normalPixels, true);
            Texture2D mask = WriteTexture(name + "_MetalSmooth", maskPixels, true);
            Texture2D occlusion = WriteTexture(name + "_Occlusion", occlusionPixels, true);
            Material material = EnsureMaterial(name);
            ApplyLitMaps(material, albedo, normal, mask, occlusion, metallic, meanSmoothness);
        }

        private static void AuthorEmission(string name, Color color, float emission, float metallic, float smoothness)
        {
            Material material = EnsureMaterial(name);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color * 0.28f);
            else material.color = color * 0.28f;
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * Mathf.Max(0f, emission));
            }
            EditorUtility.SetDirty(material);
        }

        private static void ApplyLitMaps(
            Material material,
            Texture2D albedo,
            Texture2D normal,
            Texture2D mask,
            Texture2D occlusion,
            float metallic,
            float smoothness)
        {
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", albedo);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
            if (material.HasProperty("_BumpMap"))
            {
                material.SetTexture("_BumpMap", normal);
                material.SetFloat("_BumpScale", 1f);
                material.EnableKeyword("_NORMALMAP");
            }
            if (material.HasProperty("_MetallicGlossMap"))
            {
                material.SetTexture("_MetallicGlossMap", mask);
                material.EnableKeyword("_METALLICSPECGLOSSMAP");
            }
            if (material.HasProperty("_OcclusionMap"))
            {
                material.SetTexture("_OcclusionMap", occlusion);
                material.SetFloat("_OcclusionStrength", 1f);
                material.EnableKeyword("_OCCLUSIONMAP");
            }
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            material.SetTextureScale("_BaseMap", new Vector2(3.2f, 3.2f));
            if (material.HasProperty("_BumpMap")) material.SetTextureScale("_BumpMap", new Vector2(3.2f, 3.2f));
            EditorUtility.SetDirty(material);
        }

        private static float[] BuildHeight(SurfaceKind kind, int seed, float noiseScale)
        {
            float[] height = new float[TextureSize * TextureSize];
            for (int y = 0; y < TextureSize; y++)
            for (int x = 0; x < TextureSize; x++)
            {
                float baseNoise = Fbm(x, y, seed, noiseScale);
                float fine = Fbm(x, y, seed + 29, noiseScale * 4.6f);
                float value = baseNoise * 0.74f + fine * 0.26f;

                if (kind == SurfaceKind.Basalt || kind == SurfaceKind.Obsidian)
                {
                    float veinA = Mathf.Abs(Mathf.Sin((x * 0.032f + y * 0.017f) + baseNoise * 5.6f));
                    float veinB = Mathf.Abs(Mathf.Sin((x * -0.019f + y * 0.041f) + fine * 4.2f));
                    float crack = Mathf.Min(veinA, veinB);
                    float crackMask = 1f - Mathf.SmoothStep(0.00f, kind == SurfaceKind.Obsidian ? 0.075f : 0.105f, crack);
                    value -= crackMask * (kind == SurfaceKind.Obsidian ? 0.34f : 0.24f);
                }
                else if (kind == SurfaceKind.WornMetal)
                {
                    float scratch = 1f - Mathf.SmoothStep(0.0f, 0.08f,
                        Mathf.Abs(Mathf.Sin(y * 0.39f + baseNoise * 7.0f)));
                    value -= scratch * 0.11f;
                }
                else if (kind == SurfaceKind.Cloth)
                {
                    float weave = Mathf.Sin(x * 0.46f) * Mathf.Sin(y * 0.51f);
                    value += weave * 0.055f;
                }

                height[y * TextureSize + x] = Mathf.Clamp01(value);
            }
            return height;
        }

        private static Color[] BuildNormal(float[] height, float strength)
        {
            Color[] pixels = new Color[height.Length];
            for (int y = 0; y < TextureSize; y++)
            for (int x = 0; x < TextureSize; x++)
            {
                float l = height[y * TextureSize + Wrap(x - 1)];
                float r = height[y * TextureSize + Wrap(x + 1)];
                float d = height[Wrap(y - 1) * TextureSize + x];
                float u = height[Wrap(y + 1) * TextureSize + x];
                Vector3 normal = new Vector3((l - r) * strength * 4f, (d - u) * strength * 4f, 1f).normalized;
                pixels[y * TextureSize + x] = new Color(normal.x * 0.5f + 0.5f, normal.y * 0.5f + 0.5f, normal.z * 0.5f + 0.5f, 1f);
            }
            return pixels;
        }

        private static float Fbm(int x, int y, int seed, float scale)
        {
            float px = x / (float)TextureSize;
            float py = y / (float)TextureSize;
            float offsetX = (seed & 255) * 0.173f;
            float offsetY = ((seed >> 8) & 255) * 0.191f;
            float value = 0f;
            float amp = 0.56f;
            float freq = 1f;
            float norm = 0f;
            for (int octave = 0; octave < 4; octave++)
            {
                value += Mathf.PerlinNoise(offsetX + px * scale * freq, offsetY + py * scale * freq) * amp;
                norm += amp;
                amp *= 0.5f;
                freq *= 2.03f;
            }
            return value / Mathf.Max(0.001f, norm);
        }

        private static Texture2D WriteTexture(string name, Color[] pixels, bool linear)
        {
            string path = $"{ResourceFolder}/{name}.asset";
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null || texture.width != TextureSize || texture.height != TextureSize)
            {
                if (texture != null) AssetDatabase.DeleteAsset(path);
                texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, true, linear)
                {
                    name = name,
                    wrapMode = TextureWrapMode.Repeat,
                    filterMode = FilterMode.Trilinear,
                    anisoLevel = 8,
                };
                texture.SetPixels(pixels);
                texture.Apply(true, false);
                AssetDatabase.CreateAsset(texture, path);
            }
            else
            {
                texture.SetPixels(pixels);
                texture.wrapMode = TextureWrapMode.Repeat;
                texture.filterMode = FilterMode.Trilinear;
                texture.anisoLevel = 8;
                texture.Apply(true, false);
                EditorUtility.SetDirty(texture);
            }
            return texture;
        }

        private static Material EnsureMaterial(string name)
        {
            string path = $"{ResourceFolder}/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null) throw new InvalidOperationException("No URP/Lit or Standard shader available for cinematic material authoring.");
            material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Mindforge/Resources"))
                AssetDatabase.CreateFolder("Assets/Mindforge", "Resources");
            if (!AssetDatabase.IsValidFolder(ResourceFolder))
                AssetDatabase.CreateFolder("Assets/Mindforge/Resources", "Cinematic");
            Directory.CreateDirectory(ResourceFolder);
        }

        private static int StableSeed(string value)
        {
            unchecked
            {
                int hash = 17;
                foreach (char c in value) hash = hash * 31 + c;
                return hash;
            }
        }

        private static int Wrap(int value)
        {
            if (value < 0) return TextureSize - 1;
            if (value >= TextureSize) return 0;
            return value;
        }
    }
}
#endif
