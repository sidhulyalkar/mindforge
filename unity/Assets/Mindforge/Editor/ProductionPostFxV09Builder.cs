#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Mindforge.Editor
{
    /// <summary>
    /// Restrained URP finishing pass for V0.9. It improves highlight rolloff, emissive blade
    /// readability and overall contrast without changing gameplay visibility through depth of
    /// field, motion blur, chromatic aberration or aggressive bloom. Camera and volume state are
    /// presentation only; no input, combat, world or neural authority lives here.
    /// </summary>
    public static class ProductionPostFxV09Builder
    {
        public const string RootName = "Production_PostFX_V09";
        public const string ProfilePath = "Assets/Mindforge/Generated/ProductionV09/ProductionPostFxV09.asset";

        [MenuItem("Mindforge/Legacy/Showcase/Apply Production Post FX V0.9", priority = 44)]
        public static void ApplyOpenScene()
        {
            GameObject production = EditorSceneLookup.FindIncludingInactive(ProductionArtV09Builder.RootName);
            if (production == null)
                throw new InvalidOperationException("Production Post FX V0.9 requires the Production Art V0.9 root.");

            Transform previous = production.transform.Find(RootName);
            if (previous != null) UnityEngine.Object.DestroyImmediate(previous.gameObject);

            VolumeProfile profile = BuildProfile();
            GameObject root = new GameObject(RootName);
            root.transform.SetParent(production.transform, false);

            Volume volume = root.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 90f;
            volume.weight = 1f;
            volume.sharedProfile = profile;

            Camera camera = Camera.main;
            if (camera == null) camera = UnityEngine.Object.FindObjectOfType<Camera>(true);
            if (camera != null)
            {
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

            EditorUtility.SetDirty(root);
            EditorUtility.SetDirty(volume);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();

            Debug.Log(
                "[Mindforge:V09:PostFX] ACES + restrained bloom/contrast/white balance/vignette + SMAA enabled. " +
                "No depth of field, motion blur or chromatic aberration is authored for gameplay.");
        }

        private static VolumeProfile BuildProfile()
        {
            if (AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath) != null)
                AssetDatabase.DeleteAsset(ProfilePath);

            VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "ProductionPostFxV09";
            AssetDatabase.CreateAsset(profile, ProfilePath);

            Tonemapping tonemapping = profile.Add<Tonemapping>(true);
            tonemapping.mode.Override(TonemappingMode.ACES);

            Bloom bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(1.08f);
            bloom.intensity.Override(0.22f);
            bloom.scatter.Override(0.56f);
            bloom.clamp.Override(7.5f);
            bloom.highQualityFiltering.Override(true);

            ColorAdjustments color = profile.Add<ColorAdjustments>(true);
            color.postExposure.Override(0.06f);
            color.contrast.Override(7f);
            color.hueShift.Override(0f);
            color.saturation.Override(-2f);
            color.colorFilter.Override(new Color(1f, 0.992f, 0.975f, 1f));

            WhiteBalance whiteBalance = profile.Add<WhiteBalance>(true);
            whiteBalance.temperature.Override(3f);
            whiteBalance.tint.Override(-1f);

            Vignette vignette = profile.Add<Vignette>(true);
            vignette.color.Override(new Color(0.025f, 0.035f, 0.050f, 1f));
            vignette.center.Override(new Vector2(0.5f, 0.5f));
            vignette.intensity.Override(0.115f);
            vignette.smoothness.Override(0.34f);
            vignette.rounded.Override(false);

            EditorUtility.SetDirty(profile);
            return profile;
        }
    }
}
#endif
