#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Mindforge.Editor
{
    /// <summary>
    /// High-fidelity renderer configuration for visual review. This intentionally
    /// remains on the pinned Unity 2022.3 / URP 14 project and never changes gameplay,
    /// neural authority, or the 120 Hz simulation contract.
    ///
    /// The configurator uses public URP APIs where stable and SerializedObject for
    /// version-pinned renderer fields that URP exposes read-only at runtime. Missing
    /// optional fields warn instead of silently moving the project to a new pipeline.
    /// </summary>
    public static class CinematicFidelityConfigurator
    {
        public const string ProfileName = "MINDFORGE_CINEMATIC_URP14_V2";

        [MenuItem("Mindforge/Showcase/Configure Cinematic Fidelity", priority = 20)]
        public static void Configure()
        {
            CompetitionProjectConfigurator.Configure();

            UniversalRenderPipelineAsset pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(
                CompetitionProjectConfigurator.PipelineAssetPath);
            UniversalRendererData renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(
                CompetitionProjectConfigurator.RendererAssetPath);
            if (pipeline == null || renderer == null)
                throw new InvalidOperationException("Mindforge URP assets were not created by CompetitionProjectConfigurator.");

            ConfigurePipeline(pipeline);
            ConfigureRenderer(renderer);
            ConfigureQualitySettings();

            EditorUtility.SetDirty(pipeline);
            EditorUtility.SetDirty(renderer);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Mindforge:Cinematic] URP 14 cinematic fidelity configured: HDR, TAA-ready camera path, 4-cascade shadows, SSAO, screen-space shadows and high-quality reflection support.");
        }

        private static void ConfigurePipeline(UniversalRenderPipelineAsset pipeline)
        {
            // Stable public knobs.
            pipeline.renderScale = 1.0f;
            pipeline.msaaSampleCount = 1; // TAA owns edge stability in the showcase camera.
            pipeline.shadowDistance = 52f;
            pipeline.shadowCascadeCount = 4;
            pipeline.maxAdditionalLightsCount = 8;

            // URP 14 exposes several quality properties as read-only APIs but serializes
            // them in the pipeline asset. Keep this pinned and fail visibly on migration.
            SerializedObject so = new SerializedObject(pipeline);
            SetBool(so, "m_SupportsHDR", true, true);
            SetBool(so, "m_RequireDepthTexture", true, true);
            SetBool(so, "m_RequireOpaqueTexture", true, false);
            SetInt(so, "m_MainLightRenderingMode", 1, true); // PerPixel
            SetBool(so, "m_MainLightShadowsSupported", true, true);
            SetInt(so, "m_MainLightShadowmapResolution", 4096, true);
            SetInt(so, "m_AdditionalLightsRenderingMode", 1, true); // PerPixel
            SetInt(so, "m_AdditionalLightsPerObjectLimit", 8, false);
            SetBool(so, "m_AdditionalLightShadowsSupported", true, true);
            SetInt(so, "m_AdditionalLightsShadowmapResolution", 2048, true);
            SetBool(so, "m_SoftShadowsSupported", true, true);
            SetInt(so, "m_SoftShadowQuality", 2, false); // High when present in this URP patch.
            SetFloat(so, "m_ShadowDepthBias", 0.55f, false);
            SetFloat(so, "m_ShadowNormalBias", 0.35f, false);
            SetInt(so, "m_ColorGradingMode", 1, false); // HDR color grading when available.
            SetInt(so, "m_ColorGradingLutSize", 32, false);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureRenderer(UniversalRendererData renderer)
        {
            ScriptableRendererFeature ssao = EnsureFeature(
                renderer,
                "UnityEngine.Rendering.Universal.ScreenSpaceAmbientOcclusion",
                "Mindforge SSAO");
            if (ssao != null)
            {
                SerializedObject ao = new SerializedObject(ssao);
                SetFloat(ao, "m_Settings.Intensity", 1.35f, true);
                SetFloat(ao, "m_Settings.DirectLightingStrength", 0.18f, false);
                SetFloat(ao, "m_Settings.Radius", 0.28f, true);
                SetFloat(ao, "m_Settings.Falloff", 36f, false);
                SetInt(ao, "m_Settings.Source", 1, false); // DepthNormals
                SetInt(ao, "m_Settings.NormalSamples", 2, false); // High
                SetInt(ao, "m_Settings.Samples", 0, false); // High
                SetInt(ao, "m_Settings.BlurQuality", 0, false); // High
                SetBool(ao, "m_Settings.Downsample", false, false);
                ao.ApplyModifiedPropertiesWithoutUndo();
                ssao.SetActive(true);
                EditorUtility.SetDirty(ssao);
            }

            ScriptableRendererFeature screenSpaceShadows = EnsureFeature(
                renderer,
                "UnityEngine.Rendering.Universal.ScreenSpaceShadows",
                "Mindforge Screen Space Shadows");
            if (screenSpaceShadows != null)
            {
                screenSpaceShadows.SetActive(true);
                EditorUtility.SetDirty(screenSpaceShadows);
            }

            // Keep the renderer on the proven forward path. Fidelity comes from better
            // inputs and renderer features, not from changing the project's shading
            // architecture beneath the BCI timing qualification.
            SerializedObject rendererSo = new SerializedObject(renderer);
            SetInt(rendererSo, "m_RenderingMode", 0, false); // Forward in URP 14.
            SetBool(rendererSo, "m_AccurateGbufferNormals", true, false);
            rendererSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static ScriptableRendererFeature EnsureFeature(
            UniversalRendererData renderer,
            string fullTypeName,
            string assetName)
        {
            ScriptableRendererFeature existing = renderer.rendererFeatures
                .FirstOrDefault(feature => feature != null && feature.GetType().FullName == fullTypeName);
            if (existing != null) return existing;

            Type type = typeof(UniversalRendererData).Assembly.GetType(fullTypeName);
            if (type == null || !typeof(ScriptableRendererFeature).IsAssignableFrom(type))
            {
                Debug.LogWarning($"[Mindforge:Cinematic] Optional URP feature unavailable: {fullTypeName}");
                return null;
            }

            ScriptableRendererFeature feature = ScriptableObject.CreateInstance(type) as ScriptableRendererFeature;
            if (feature == null) return null;
            feature.name = assetName;
            feature.Create();
            renderer.rendererFeatures.Add(feature);
            AssetDatabase.AddObjectToAsset(feature, renderer);
            EditorUtility.SetDirty(renderer);
            return feature;
        }

        private static void ConfigureQualitySettings()
        {
            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowResolution = UnityEngine.ShadowResolution.VeryHigh;
            QualitySettings.shadowProjection = ShadowProjection.StableFit;
            QualitySettings.shadowDistance = 52f;
            QualitySettings.shadowCascades = 4;
            QualitySettings.lodBias = Mathf.Max(QualitySettings.lodBias, 2.0f);
            QualitySettings.maximumLODLevel = 0;
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
            QualitySettings.realtimeReflectionProbes = true;
            QualitySettings.skinWeights = SkinWeights.FourBones;
            QualitySettings.vSyncCount = 1;
            Application.targetFrameRate = 120;
        }

        private static void SetBool(SerializedObject so, string path, bool value, bool required)
        {
            SerializedProperty p = so.FindProperty(path);
            if (p != null) p.boolValue = value;
            else Missing(path, required);
        }

        private static void SetInt(SerializedObject so, string path, int value, bool required)
        {
            SerializedProperty p = so.FindProperty(path);
            if (p != null) p.intValue = value;
            else Missing(path, required);
        }

        private static void SetFloat(SerializedObject so, string path, float value, bool required)
        {
            SerializedProperty p = so.FindProperty(path);
            if (p != null) p.floatValue = value;
            else Missing(path, required);
        }

        private static void Missing(string path, bool required)
        {
            string message = $"[Mindforge:Cinematic] URP 14 serialized field not found: {path}";
            if (required) Debug.LogError(message + ". Keep Unity/URP pinned until P1 is requalified.");
            else Debug.LogWarning(message);
        }
    }
}
#endif
