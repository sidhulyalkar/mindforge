#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using Mindforge.Combat;

namespace Mindforge.Editor
{
    /// <summary>
    /// Canonical V0.24 white-cathedral palette.
    ///
    /// Architectural stone uses the existing production triplanar shader so texel density stays
    /// stable on modules of different scale. Textures are generated deterministically into the
    /// ignored Generated/V24 tree; source control stores the recipe rather than binary art.
    ///
    /// Ensure() also normalizes the canonical scene by renderer role, including inactive arena
    /// hierarchy objects. The competition arena is commonly inactive while editor authoring runs,
    /// so relying on activeInHierarchy would silently leave the real route in its old dark palette.
    /// </summary>
    public static class CathedralMaterialLibraryV24
    {
        public const string Root = "Assets/Mindforge/Generated/V24/Materials";
        public const string TextureRoot = "Assets/Mindforge/Generated/V24/Textures";
        public const int SurfaceRevision = 1;

        private sealed class SurfaceSet
        {
            public Texture2D Albedo;
            public Texture2D Normal;
        }

        public sealed class Palette
        {
            public Material IvoryStone;
            public Material WhiteMarble;
            public Material PaleFloor;
            public Material CoolShadowStone;
            public Material Bronze;
            public Material SacredGold;
            public Material FractureDark;
            public Material SignalMagenta;
            public Material LumenCyan;
        }

        public static Palette Ensure()
        {
            EnsureFolder(Root);
            EnsureFolder(TextureRoot);

            SurfaceSet ivory = EnsureStoneSurface(
                "IvoryStone", 24101,
                new Color(0.73f, 0.72f, 0.67f), new Color(0.96f, 0.95f, 0.88f),
                new Color(0.55f, 0.54f, 0.50f), 0.16f, 0.18f, 0.72f);
            SurfaceSet marble = EnsureStoneSurface(
                "WhiteMarble", 24111,
                new Color(0.80f, 0.82f, 0.82f), new Color(1.00f, 0.99f, 0.95f),
                new Color(0.48f, 0.53f, 0.57f), 0.08f, 0.30f, 0.52f);
            SurfaceSet floor = EnsureStoneSurface(
                "PaleFloor", 24121,
                new Color(0.67f, 0.68f, 0.66f), new Color(0.90f, 0.90f, 0.85f),
                new Color(0.44f, 0.46f, 0.47f), 0.22f, 0.13f, 0.80f);
            SurfaceSet shadow = EnsureStoneSurface(
                "CoolShadowStone", 24131,
                new Color(0.13f, 0.15f, 0.18f), new Color(0.31f, 0.34f, 0.37f),
                new Color(0.08f, 0.10f, 0.13f), 0.20f, 0.09f, 0.95f);
            SurfaceSet fracture = EnsureStoneSurface(
                "FractureDark", 24141,
                new Color(0.075f, 0.055f, 0.085f), new Color(0.19f, 0.12f, 0.20f),
                new Color(0.035f, 0.025f, 0.045f), 0.28f, 0.16f, 0.90f);

            Palette palette = new Palette
            {
                IvoryStone = EnsureTriplanar("V24_IvoryStone", ivory, Color.white, 0.01f, 0.32f, 0.72f, 2.4f),
                WhiteMarble = EnsureTriplanar("V24_WhiteMarble", marble, Color.white, 0.02f, 0.52f, 0.52f, 2.0f),
                PaleFloor = EnsureTriplanar("V24_PaleFloor", floor, Color.white, 0.01f, 0.38f, 0.80f, 2.1f),
                CoolShadowStone = EnsureTriplanar("V24_CoolShadowStone", shadow, Color.white, 0.03f, 0.30f, 0.92f, 2.7f),
                Bronze = EnsureStockLit("V24_Bronze", new Color(0.34f, 0.23f, 0.12f), 0.78f, 0.43f),
                SacredGold = EnsureStockLit("V24_SacredGold", new Color(0.71f, 0.54f, 0.25f), 0.86f, 0.58f),
                FractureDark = EnsureTriplanar("V24_FractureDark", fracture, Color.white, 0.06f, 0.30f, 0.86f, 2.2f),
                SignalMagenta = EnsureEmission("V24_SignalMagenta", new Color(0.48f, 0.055f, 0.48f), new Color(1.0f, 0.08f, 0.92f) * 2.0f),
                LumenCyan = EnsureEmission("V24_LumenCyan", new Color(0.08f, 0.34f, 0.42f), new Color(0.16f, 0.78f, 0.92f) * 1.55f),
            };

            NormalizeCanonicalScene(palette);
            return palette;
        }

        private static void NormalizeCanonicalScene(Palette palette)
        {
            GameObject canonical = EditorSceneLookup.FindIncludingInactive(MindforgeDemoV11Builder.RootName);
            if (canonical == null) return;

            Renderer[] renderers = canonical.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null) continue;
                if (renderer.GetComponentInParent<CombatantVitals>() != null) continue;

                string n = renderer.gameObject.name;
                string materialName = renderer.sharedMaterial != null ? renderer.sharedMaterial.name : string.Empty;
                if (IsSemanticSignal(n, materialName)) continue;

                Material replacement = null;
                if (ContainsAny(n, "Floor", "Road", "Ramp", "Platform", "Perch", "Dais", "Threshold", "Transition"))
                    replacement = palette.PaleFloor;
                else if (ContainsAny(n, "Gold"))
                    replacement = palette.SacredGold;
                else if (ContainsAny(n, "Column", "Arch", "Crown", "Spire", "Buttress", "Rib", "Facade"))
                    replacement = ContainsAny(n, "FractureSpire") ? palette.WhiteMarble : palette.IvoryStone;
                else if (ContainsAny(n, "Fracture", "Ember"))
                    replacement = palette.FractureDark;
                else if (ContainsAny(n, "Retainer", "Foundation", "Underlay", "Backing", "Backwall", "Boundary"))
                    replacement = palette.CoolShadowStone;
                else if (ContainsAny(n, "Terrain", "Landmass", "Highlands", "Rock", "Crater", "Earth"))
                    replacement = palette.CoolShadowStone;
                else if (ContainsAny(n, "Wall", "Sanctum", "Market", "Ascent", "Causeway"))
                    replacement = palette.IvoryStone;

                if (replacement != null) renderer.sharedMaterial = replacement;
                renderer.receiveShadows = true;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            }
        }

        private static bool IsSemanticSignal(string objectName, string materialName)
            => ContainsAny(objectName, "MemoryForgeCore", "SignalOrb", "Wisp", "Vep", "Stimulus", "Telegraph", "Aether") ||
               ContainsAny(materialName, "Signal", "Wisp", "Vep", "Stimulus", "Telegraph", "Aether");

        private static bool ContainsAny(string source, params string[] needles)
        {
            if (string.IsNullOrEmpty(source)) return false;
            for (int i = 0; i < needles.Length; i++)
                if (source.IndexOf(needles[i], StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }

        private static SurfaceSet EnsureStoneSurface(
            string name,
            int seed,
            Color low,
            Color high,
            Color veinColor,
            float crackStrength,
            float veinStrength,
            float normalStrength)
        {
            const int size = 128;
            string albedoPath = $"{TextureRoot}/{name}Albedo.asset";
            string normalPath = $"{TextureRoot}/{name}Normal.asset";
            string expectedAlbedo = $"{name}Albedo_r{SurfaceRevision}";
            string expectedNormal = $"{name}Normal_r{SurfaceRevision}";
            Texture2D albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(albedoPath);
            Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
            if (TextureReady(albedo, expectedAlbedo, size) && TextureReady(normal, expectedNormal, size))
                return new SurfaceSet { Albedo = albedo, Normal = normal };

            albedo = EnsureTexture(albedo, albedoPath, expectedAlbedo, size, false);
            normal = EnsureTexture(normal, normalPath, expectedNormal, size, true);

            float[] heights = new float[size * size];
            Color[] colors = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)size;
                    float v = y / (float)size;
                    float px = u * 36f;
                    float py = v * 36f;
                    float broad = WorldSoulNoiseV20.Fbm(px, py, seed, 4, 12f, 0.52f, 2.05f) * 0.5f + 0.5f;
                    float fine = WorldSoulNoiseV20.Fbm(px + 19.1f, py - 7.6f, seed ^ 0x31337, 3, 3.9f, 0.47f, 2.21f) * 0.5f + 0.5f;
                    float veinWave = Mathf.Abs(Mathf.Sin((u * 3.4f + v * 8.7f + broad * 0.62f) * Mathf.PI));
                    float vein = 1f - Mathf.SmoothStep(0.02f, 0.13f, veinWave);
                    float crackField = Mathf.Abs(WorldSoulNoiseV20.Fbm(px - 8f, py + 5f, seed ^ 0x51515, 3, 5.2f, 0.50f, 2.1f));
                    float crack = 1f - Mathf.SmoothStep(0.035f, 0.16f, crackField);
                    float h = Mathf.Clamp01(broad * 0.70f + fine * 0.30f - crack * crackStrength * 0.16f - vein * veinStrength * 0.08f);
                    heights[y * size + x] = h;

                    Color c = Color.Lerp(low, high, Mathf.Clamp01(broad * 0.74f + fine * 0.26f));
                    c = Color.Lerp(c, veinColor, vein * veinStrength);
                    c *= 1f - crack * crackStrength * 0.22f;
                    colors[y * size + x] = new Color(c.r, c.g, c.b, 1f);
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
                    float dx = heights[y * size + xm] - heights[y * size + xp];
                    float dy = heights[ym * size + x] - heights[yp * size + x];
                    Vector3 n = new Vector3(dx * normalStrength, dy * normalStrength, 1f).normalized;
                    normals[y * size + x] = new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f, 1f);
                }
            }

            albedo.SetPixels(colors);
            albedo.Apply(true, false);
            normal.SetPixels(normals);
            normal.Apply(true, false);
            ConfigureTexture(albedo, 5);
            ConfigureTexture(normal, 6);
            EditorUtility.SetDirty(albedo);
            EditorUtility.SetDirty(normal);
            return new SurfaceSet { Albedo = albedo, Normal = normal };
        }

        private static Material EnsureTriplanar(
            string name,
            SurfaceSet surface,
            Color tint,
            float metallic,
            float smoothness,
            float bumpScale,
            float metresPerTile)
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ProductionMaterialAuthoringV09.TriplanarShaderPath);
            if (shader == null) shader = Shader.Find(ProductionMaterialAuthoringV09.TriplanarShaderName);
            if (shader == null || ShaderUtil.ShaderHasError(shader))
                throw new InvalidOperationException("V0.24 requires the production triplanar shader.");

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
            material.SetFloat("_BlendSharpness", 5.5f);
            material.SetFloat("_NormalFadeDistance", 92f);
            material.EnableKeyword("_NORMALMAP");
            material.enableInstancing = true;
            ForceOpaque(material);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material EnsureStockLit(string name, Color color, float metallic, float smoothness)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null) throw new InvalidOperationException("V0.24 requires a lit shader.");
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
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", new Color(color.r, color.g, color.b, 1f));
            else if (material.HasProperty("_Color")) material.SetColor("_Color", new Color(color.r, color.g, color.b, 1f));
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            ForceOpaque(material);
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material EnsureEmission(string name, Color baseColor, Color emission)
        {
            Material material = EnsureStockLit(name, baseColor, 0.18f, 0.62f);
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission);
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void ForceOpaque(Material material)
        {
            if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 0f);
            if (material.HasProperty("_AlphaClip")) material.SetFloat("_AlphaClip", 0f);
            if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 1f);
            material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHATEST_ON");
            material.SetOverrideTag("RenderType", "Opaque");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
        }

        private static Texture2D EnsureTexture(Texture2D existing, string path, string expectedName, int size, bool linear)
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
                anisoLevel = 5,
            };
            AssetDatabase.CreateAsset(texture, path);
            return texture;
        }

        private static bool TextureReady(Texture2D texture, string expectedName, int size)
            => texture != null && texture.name == expectedName && texture.width == size && texture.height == size;

        private static void ConfigureTexture(Texture2D texture, int anisotropy)
        {
            texture.wrapMode = TextureWrapMode.Repeat;
            texture.filterMode = FilterMode.Trilinear;
            texture.anisoLevel = Mathf.Clamp(anisotropy, 1, 9);
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
