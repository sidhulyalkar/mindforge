#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Mindforge.World;

namespace Mindforge.Editor
{
    /// <summary>
    /// Final editor-authored presentation pass for the canonical V0.11 -> V0.24 world stack.
    ///
    /// V0.24 owns world grammar and physical scene composition. V0.25 owns rendering fidelity:
    /// URP quality, SSAO, restrained HDR post, brighter cathedral response and static data inlays.
    /// It never creates gameplay colliders, attacks, neural evidence, target authority or timing.
    /// </summary>
    public static class SensoryFidelityV25Builder
    {
        public const string RootName = "Mindforge_Sensory_Fidelity_V25";
        public const string GeneratedRoot = "Assets/Mindforge/Generated/V25";
        public const string ProfilePath = GeneratedRoot + "/SensoryFidelityV25.asset";
        public const string DataInlayMaterialPath = GeneratedRoot + "/V25_DataInlay.mat";

        public static bool PresentInOpenScene()
            => EditorSceneLookup.FindIncludingInactive(RootName) != null;

        public static void ApplyOpenScene()
        {
            GameObject canonical = EditorSceneLookup.FindIncludingInactive(MindforgeDemoV11Builder.RootName);
            if (canonical == null)
                throw new UnityEditor.Build.BuildFailedException(
                    $"V0.25 requires canonical world '{MindforgeDemoV11Builder.RootName}'.");
            if (!WorldCathedralV24Builder.PresentInOpenScene())
                throw new UnityEditor.Build.BuildFailedException(
                    "V0.25 must compose after V0.24 White Cathedral.");

            // Promote the already-proven high-fidelity URP configuration into Latest instead of
            // maintaining a second renderer stack. This enables HDR/depth/normals, SSAO,
            // screen-space shadows, four cascades and high-quality reflection support.
            CinematicFidelityConfigurator.Configure();

            Transform previous = canonical.transform.Find(RootName);
            if (previous != null) UnityEngine.Object.DestroyImmediate(previous.gameObject);

            EnsureFolder(GeneratedRoot);
            CathedralMaterialLibraryV24.Palette cathedral = CathedralMaterialLibraryV24.Ensure();
            Transform root = CathedralModuleLibraryV24.Node(RootName, canonical.transform);

            ConfigureHighKeyEnvironment(canonical.transform);
            ConfigureCamera();
            BuildGlobalVolume(root);
            BuildDataCathedralInlays(canonical.transform, root, cathedral);
            Validate(root);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                "[Mindforge:V25] Sensory fidelity authored: cinematic URP/SSAO promoted to Latest, " +
                "ACES + restrained bloom/color response installed, white-cathedral lighting lifted, " +
                "and collider-free neural/data inlays added. Gameplay and BCI authority unchanged.");
        }

        private static void ConfigureHighKeyEnvironment(Transform canonicalRoot)
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.47f, 0.49f, 0.52f, 1f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.39f, 0.43f, 0.47f, 1f);
            RenderSettings.fogStartDistance = 72f;
            RenderSettings.fogEndDistance = 220f;
            RenderSettings.reflectionIntensity = 0.92f;

            Light[] lights = canonicalRoot.GetComponentsInChildren<Light>(true);
            bool tunedDirectional = false;
            for (int i = 0; i < lights.Length; i++)
            {
                Light light = lights[i];
                if (light == null) continue;

                if (light.type == LightType.Directional && !tunedDirectional)
                {
                    tunedDirectional = true;
                    light.color = new Color(1.0f, 0.955f, 0.875f, 1f);
                    light.intensity = Mathf.Max(1.45f, light.intensity);
                    light.shadows = LightShadows.Soft;
                    light.shadowStrength = 0.82f;
                    light.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
                }
                else if (light.type == LightType.Point || light.type == LightType.Spot)
                {
                    // V0.24 already owns the architectural light locations. Lift their response
                    // instead of adding another forest of presentation lights.
                    light.intensity = Mathf.Clamp(light.intensity * 1.22f, 0.0f, 3.2f);
                    light.range = Mathf.Max(light.range, 8.0f);
                }

                EditorUtility.SetDirty(light);
            }
        }

        private static void ConfigureCamera()
        {
            Camera camera = Camera.main;
            if (camera == null) camera = UnityEngine.Object.FindObjectOfType<Camera>(true);
            if (camera == null)
                throw new UnityEditor.Build.BuildFailedException("V0.25 could not resolve the canonical gameplay camera.");

            camera.allowHDR = true;
            UniversalAdditionalCameraData data = camera.GetComponent<UniversalAdditionalCameraData>();
            if (data == null) data = camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            data.antialiasingQuality = AntialiasingQuality.High;
            data.stopNaN = true;
            data.dithering = true;
            EditorUtility.SetDirty(camera);
            EditorUtility.SetDirty(data);
        }

        private static void BuildGlobalVolume(Transform root)
        {
            VolumeProfile existing = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
            if (existing != null) AssetDatabase.DeleteAsset(ProfilePath);

            VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "SensoryFidelityV25";
            AssetDatabase.CreateAsset(profile, ProfilePath);

            Tonemapping tonemapping = profile.Add<Tonemapping>(true);
            tonemapping.mode.Override(TonemappingMode.ACES);

            Bloom bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(0.94f);
            bloom.intensity.Override(0.34f);
            bloom.scatter.Override(0.63f);
            bloom.clamp.Override(10.0f);
            bloom.highQualityFiltering.Override(true);

            ColorAdjustments color = profile.Add<ColorAdjustments>(true);
            color.postExposure.Override(0.18f);
            color.contrast.Override(8.0f);
            color.hueShift.Override(0f);
            color.saturation.Override(-3.0f);
            color.colorFilter.Override(new Color(1.0f, 0.995f, 0.975f, 1f));

            WhiteBalance balance = profile.Add<WhiteBalance>(true);
            balance.temperature.Override(5f);
            balance.tint.Override(-2f);

            Vignette vignette = profile.Add<Vignette>(true);
            vignette.color.Override(new Color(0.035f, 0.045f, 0.060f, 1f));
            vignette.center.Override(new Vector2(0.5f, 0.5f));
            vignette.intensity.Override(0.055f);
            vignette.smoothness.Override(0.26f);
            vignette.rounded.Override(false);

            GameObject go = new GameObject("V25_Global_PostFX");
            go.transform.SetParent(root, false);
            Volume volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 125f;
            volume.weight = 1f;
            volume.sharedProfile = profile;

            EditorUtility.SetDirty(profile);
            EditorUtility.SetDirty(volume);
        }

        private static void BuildDataCathedralInlays(
            Transform canonicalRoot,
            Transform root,
            CathedralMaterialLibraryV24.Palette cathedral)
        {
            Material data = EnsureDataInlayMaterial(cathedral.LumenCyan);
            Transform inlays = CathedralModuleLibraryV24.Node("V25_Data_Cathedral_Inlays", root);

            // Static, non-flashing floor circuitry. These are route-reading accents, not stimuli.
            AddInlay("Procession_Left", inlays, new Vector3(-1.72f, 0.055f, 17.0f), new Vector3(0.075f, 0.026f, 78f), data);
            AddInlay("Procession_Right", inlays, new Vector3(1.72f, 0.055f, 17.0f), new Vector3(0.075f, 0.026f, 78f), data);
            AddInlay("Market_Transept", inlays, new Vector3(0f, 0.058f, 49f), new Vector3(15.0f, 0.026f, 0.070f), data);

            Transform ramp = FindDeep(canonicalRoot, "AscentRamp");
            if (ramp != null)
            {
                Vector3 rampTop = ramp.position + ramp.up * (ramp.localScale.y * 0.5f + 0.030f);
                Transform strip = AddInlay("Choir_Ascent", inlays, Vector3.zero, new Vector3(0.10f, 0.028f, 25.2f), data);
                strip.position = rampTop;
                strip.rotation = ramp.rotation;
            }

            const float arenaY = 4.125f;
            const float arenaZ = 94f;
            AddInlay("Apse_North", inlays, new Vector3(0f, arenaY, arenaZ + 5.8f), new Vector3(0.085f, 0.024f, 8.0f), data);
            AddInlay("Apse_South", inlays, new Vector3(0f, arenaY, arenaZ - 5.8f), new Vector3(0.085f, 0.024f, 8.0f), data);
            AddInlay("Apse_East", inlays, new Vector3(5.8f, arenaY, arenaZ), new Vector3(8.0f, 0.024f, 0.085f), data);
            AddInlay("Apse_West", inlays, new Vector3(-5.8f, arenaY, arenaZ), new Vector3(8.0f, 0.024f, 0.085f), data);
        }

        private static Transform AddInlay(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            Transform t = CathedralModuleLibraryV24.Block(
                name,
                parent,
                position,
                scale,
                material,
                CathedralRoleV24.StructuralRole.MysticAccent,
                Vector3.zero,
                false);
            Collider collider = t.GetComponent<Collider>();
            if (collider != null) UnityEngine.Object.DestroyImmediate(collider);
            return t;
        }

        private static Material EnsureDataInlayMaterial(Material fallback)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(DataInlayMaterialPath);
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = fallback != null ? fallback.shader : Shader.Find("Standard");
            if (material == null)
            {
                material = new Material(shader) { name = "V25_DataInlay" };
                AssetDatabase.CreateAsset(material, DataInlayMaterialPath);
            }
            else if (shader != null && material.shader != shader)
            {
                material.shader = shader;
            }

            Color baseColor = new Color(0.035f, 0.18f, 0.21f, 1f);
            Color emission = new Color(0.09f, 0.72f, 0.88f, 1f) * 1.15f;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", baseColor);
            if (material.HasProperty("_Color")) material.SetColor("_Color", baseColor);
            if (material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor", emission);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0.25f);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.68f);
            material.EnableKeyword("_EMISSION");
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void Validate(Transform root)
        {
            if (root == null)
                throw new UnityEditor.Build.BuildFailedException("V0.25 presentation root was not created.");

            Volume volume = root.GetComponentInChildren<Volume>(true);
            if (volume == null || volume.sharedProfile == null || !volume.isGlobal)
                throw new UnityEditor.Build.BuildFailedException("V0.25 global post-processing volume is missing or incomplete.");

            Transform inlays = root.Find("V25_Data_Cathedral_Inlays");
            if (inlays == null)
                throw new UnityEditor.Build.BuildFailedException("V0.25 data-cathedral inlays were not authored.");

            Renderer[] renderers = inlays.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length < 7)
                throw new UnityEditor.Build.BuildFailedException($"V0.25 expected at least 7 data inlays; found {renderers.Length}.");
            Collider[] colliders = inlays.GetComponentsInChildren<Collider>(true);
            if (colliders.Length != 0)
                throw new UnityEditor.Build.BuildFailedException("V0.25 data inlays must remain collider-free.");

            Camera camera = Camera.main;
            if (camera == null) camera = UnityEngine.Object.FindObjectOfType<Camera>(true);
            UniversalAdditionalCameraData data = camera != null ? camera.GetComponent<UniversalAdditionalCameraData>() : null;
            if (camera == null || !camera.allowHDR || data == null || !data.renderPostProcessing)
                throw new UnityEditor.Build.BuildFailedException("V0.25 requires HDR + post-processing on the canonical camera.");
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root == null) return null;
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindDeep(root.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            string parent = folder.Substring(0, folder.LastIndexOf('/'));
            string leaf = folder.Substring(folder.LastIndexOf('/') + 1);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
#endif
