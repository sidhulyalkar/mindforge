#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace Mindforge.Editor
{
    /// <summary>
    /// Reproducible surface authoring for the canonical demo world.
    ///
    /// V0.20 intentionally reuses Mindforge/ProductionTriplanarLitV09 instead of inventing a
    /// second world shader. That shader already provides world-space texel density, triplanar
    /// blending, generated RGB-normal decoding, shadow/fog integration and distance-bounded
    /// normal sampling. Its URP structure was previously cross-checked against the CC0 Cyanilux
    /// templates and the procedural texture workflow was informed by MIT Material Maker.
    ///
    /// World Soul generates its own albedo + linear RGB normal textures from deterministic
    /// octave noise. Generated assets live under Assets/Mindforge/Generated and are ignored.
    /// Bump SurfaceRevision when the recipe changes to refresh a local cache exactly once.
    /// </summary>
    public static class WorldSoulMaterialLibraryV20
    {
        public const string Root = "Assets/Mindforge/Generated/V20/Materials";
        public const string TextureRoot = "Assets/Mindforge/Generated/V20/Textures";
        public const int SurfaceRevision = 2;

        private sealed class SurfaceSet
        {
            public Texture2D Albedo;
            public Texture2D Normal;
        }

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

            SurfaceSet limestone = EnsureSurface(
                "Limestone", 160, 20011,
                new Color(0.25f, 0.27f, 0.27f), new Color(0.59f, 0.58f, 0.53f),
                0.28f, 0.05f, 1.35f);
            SurfaceSet basalt = EnsureSurface(
                "Basalt", 160, 20021,
                new Color(0.025f, 0.031f, 0.044f), new Color(0.13f, 0.15f, 0.17f),
                0.42f, 0.01f, 1.65f);
            SurfaceSet worn = EnsureSurface(
                "WornStone", 160, 20031,
                new Color(0.10f, 0.115f, 0.12f), new Color(0.38f, 0.39f, 0.37f),
                0.34f, 0.03f, 1.48f);
            SurfaceSet earth = EnsureSurface(
                "Earth", 160, 20041,
                new Color(0.035f, 0.030f, 0.029f), new Color(0.16f, 0.13f, 0.10f),
                0.16f, 0.08f, 1.70f);
            SurfaceSet moss = EnsureSurface(
                "Moss", 160, 20051,
                new Color(0.025f, 0.055f, 0.040f), new Color(0.16f, 0.27f, 0.15f),
                0.10f, 0.38f, 1.18f);
            SurfaceSet bark = EnsureSurface(
                "Bark", 160, 20061,
                new Color(0.025f, 0.020f, 0.020f), new Color(0.18f, 0.11f, 0.075f),
                0.48f, 0.02f, 1.82f);
            SurfaceSet foliage = EnsureSurface(
                "Foliage", 160, 20071,
                new Color(0.018f, 0.052f, 0.035f), new Color(0.13f, 0.31f, 0.19f),
                0.10f, 0.46f, 0.95f);
            SurfaceSet ember = EnsureSurface(
                "EmberStone", 160, 20091,
                new Color(0.055f, 0.020f, 0.028f), new Color(0.22f, 0.055f, 0.075f),
                0.36f, 0.01f, 1.52f);
            Texture2D water = EnsureWaterTexture();

            return new Palette
            {
                Limestone = EnsureWorldLit("WorldLimestone", limestone, Color.white, 0.03f, 0.34f, 0.92f, 2.8f, 5.0f, 86f),
                Basalt = EnsureWorldLit("WorldBasalt", basalt, Color.white, 0.08f, 0.28f, 1.08f, 2.0f, 6.0f, 74f),
                WornStone = EnsureWorldLit("WorldWornStone", worn, Color.white, 0.05f, 0.31f, 0.98f, 2.4f, 5.3f, 82f),
                Earth = EnsureWorldLit("WorldEarth", earth, Color.white, 0.0f, 0.18f, 0.88f, 1.65f, 4.2f, 62f),
                Moss = EnsureWorldLit("WorldMoss", moss, Color.white, 0.0f, 0.22f, 0.72f, 1.15f, 4.0f, 58f),
                Bark = EnsureWorldLit("WorldBark", bark, Color.white, 0.0f, 0.24f, 1.05f, 0.90f, 5.4f, 52f),
                Foliage = EnsureWorldLit("WorldFoliage", foliage, new Color(0.86f, 0.95f, 0.88f), 0.0f, 0.20f, 0.58f, 0.78f, 3.6f, 42f),
                Water = EnsureStockLit("WorldWater", water, new Color(0.72f, 0.92f, 1f), 0.12f, 0.91f, new Vector2(3f, 8f)),
                EmberStone = EnsureWorldLit("WorldEmberStone", ember, Color.white, 0.10f, 0.38f, 1.0f, 1.8f, 5.5f, 76f),
                Skybox = EnsureSkybox(),
            };
        }

        private static SurfaceSet EnsureSurface(
            string name,
            int size,
            int seed,
            Color low,
            Color high,
            float crackStrength,
            float organicStrength,
            float normalStrength)
        {
            string albedoPath = $"{TextureRoot}/{name}Albedo.asset";
            string normalPath = $"{TextureRoot}/{name}Normal.asset";
            string albedoName = $"{name}Albedo_r{SurfaceRevision}";
            string normalName = $"{name}Normal_r{SurfaceRevision}";
            Texture2D albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(albedoPath);
            Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);

            bool ready = TextureReady(albedo, size, albedoName) && TextureReady(normal, size, normalName);
            if (ready) return new SurfaceSet { Albedo = albedo, Normal = normal };

            albedo = EnsureTextureAsset(albedo, albedoPath, albedoName, size, linear: false);
            normal = EnsureTextureAsset(normal, normalPath, normalName, size, linear: true);

            float[] heights = new float[size * size];
            Color[] colors = new Color[size * size];
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
                    float height = Mathf.Clamp01(broad * 0.66f + fine * 0.22f + ridge * 0.12f - crack * crackStrength * 0.34f + organic * organicStrength * 0.12f);
                    heights[y * size + x] = height;

                    Color color = Color.Lerp(low, high, Mathf.Clamp01(broad * 0.72f + fine * 0.20f + ridge * 0.08f));
                    color *= 1f - crack * Mathf.Clamp01(crackStrength) * 0.48f;
                    color = Color.Lerp(color, new Color(0.18f, 0.30f, 0.13f), organic * Mathf.Clamp01(organicStrength));
                    colors[y * size + x] = new Color(color.r, color.g, color.b, 1f);
                }
            }

            Color[] normals = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                int ym = (y - 1 + size) % size;
                int yp = (y + 1) % size;
                for (int x = 0; x < size; x++)
                {
                    int xm = (x - 1 + size) % size;
                    int xp = (x + 1) % size;
                    float hx0 = heights[y * size + xm];
                    float hx1 = heights[y * size + xp];
                    float hy0 = heights[ym * size + x];
                    float hy1 = heights[yp * size + x];
                    Vector3 n = new Vector3((hx0 - hx1) * normalStrength, (hy0 - hy1) * normalStrength, 1f).normalized;
                    normals[y * size + x] = new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f, 1f);
                }
            }

            albedo.SetPixels(colors);
            albedo.Apply(true, false);
            normal.SetPixels(normals);
            normal.Apply(true, false);
            ConfigureTexture(albedo, 4);
            ConfigureTexture(normal, 6);
            EditorUtility.SetDirty(albedo);
            EditorUtility.SetDirty(normal);
            return new SurfaceSet { Albedo = albedo, Normal = normal };
        }

        private static Texture2D EnsureWaterTexture()
        {
            const string name = "WaterAlbedo";
            const int size = 160;
            const int seed = 20081;
            string path = $"{TextureRoot}/{name}.asset";
            string expectedName = $"{name}_r{SurfaceRevision}";
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (TextureReady(texture, size, expectedName)) return texture;
            texture = EnsureTextureAsset(texture, path, expectedName, size, linear: false);

            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = x / (float)size;
                float v = y / (float)size;
                float broad = WorldSoulNoiseV20.Fbm(u * 16f, v * 36f, seed, 4, 8f, 0.50f, 2.1f) * 0.5f + 0.5f;
                float line = Mathf.Sin((u * 7f + v * 19f + broad * 1.8f) * Mathf.PI * 2f) * 0.5f + 0.5f;
                Color c = Color.Lerp(new Color(0.018f, 0.065f, 0.085f), new Color(0.075f, 0.20f, 0.23f), broad * 0.78f + line * 0.22f);
                pixels[y * size + x] = new Color(c.r, c.g, c.b, 1f);
            }
            texture.SetPixels(pixels);
            texture.Apply(true, false);
            ConfigureTexture(texture, 4);
            EditorUtility.SetDirty(texture);
            return texture;
        }

        private static bool TextureReady(Texture2D texture, int size, string expectedName)
            => texture != null && texture.width == size && texture.height == size && texture.name == expectedName;

        private static Texture2D EnsureTextureAsset(
            Texture2D existing,
            string path,
            string expectedName,
            int size,
            bool linear)
        {
            if (existing != null && (existing.width != size || existing.height != size))
            {
                AssetDatabase.DeleteAsset(path);
                existing = null;
            }
            if (existing != null)
            {
                existing.name = expectedName;
                return existing;
            }

            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, true, linear)
            {
                name = expectedName,
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Trilinear,
                anisoLevel = 4,
            };
            AssetDatabase.CreateAsset(texture, path);
            return texture;
        }

        private static void ConfigureTexture(Texture2D texture, int anisotropy)
        {
            texture.wrapMode = TextureWrapMode.Repeat;
            texture.filterMode = FilterMode.Trilinear;
            texture.anisoLevel = Mathf.Clamp(anisotropy, 1, 9);
        }

        private static Material EnsureWorldLit(
            string name,
            SurfaceSet surface,
            Color tint,
            float metallic,
            float smoothness,
            float bumpScale,
            float metresPerTile,
            float blendSharpness,
            float normalFadeDistance)
        {
            Shader shader = RequireTriplanarShader();
            string path = $"{Root}/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }

            material.SetTexture("_BaseMap", surface.Albedo);
            material.SetTexture("_BumpMap", surface.Normal);
            material.SetColor("_BaseColor", tint);
            material.SetFloat("_Metallic", Mathf.Clamp01(metallic));
            material.SetFloat("_Smoothness", Mathf.Clamp01(smoothness));
            material.SetFloat("_BumpScale", Mathf.Clamp(bumpScale, 0f, 2f));
            material.SetFloat("_MetersPerTile", Mathf.Max(0.25f, metresPerTile));
            material.SetFloat("_BlendSharpness", Mathf.Clamp(blendSharpness, 1f, 12f));
            material.SetFloat("_NormalFadeDistance", Mathf.Max(10f, normalFadeDistance));
            material.EnableKeyword("_NORMALMAP");
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Shader RequireTriplanarShader()
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ProductionMaterialAuthoringV09.TriplanarShaderPath);
            if (shader == null) shader = Shader.Find(ProductionMaterialAuthoringV09.TriplanarShaderName);
            if (shader == null)
                throw new InvalidOperationException($"{ProductionMaterialAuthoringV09.TriplanarShaderName} is required for V0.20 World Soul surfaces.");
            if (ShaderUtil.ShaderHasError(shader))
                throw new InvalidOperationException(
                    $"{ProductionMaterialAuthoringV09.TriplanarShaderName} has Unity shader compiler errors; V0.20 refuses a magenta fallback.");
            return shader;
        }

        private static Material EnsureStockLit(
            string name,
            Texture2D albedo,
            Color tint,
            float metallic,
            float smoothness,
            Vector2 tiling)
        {
            string path = $"{Root}/{name}.mat";
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null) throw new InvalidOperationException("V0.20 requires URP/Lit or Standard shader support.");
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
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
