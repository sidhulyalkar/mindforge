#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Mindforge.Editor
{
    /// <summary>
    /// Reproducible project-level configuration for the competition build.
    /// Runtime/physical display timing is still qualified separately.
    /// </summary>
    public static class CompetitionProjectConfigurator
    {
        public const string PipelineAssetPath = "Assets/Mindforge/Generated/MindforgeCompetitionURP.asset";
        public const string RendererAssetPath = "Assets/Mindforge/Generated/MindforgeCompetitionRenderer.asset";

        [MenuItem("Mindforge/Competition/Configure Unity Project")]
        public static void Configure()
        {
            Directory.CreateDirectory("Assets/Mindforge/Generated");
            PlayerSettings.companyName = "MindforgeLab";
            PlayerSettings.productName = "Mindforge First Guardian";
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.runInBackground = true;
            QualitySettings.vSyncCount = 1;
            Application.targetFrameRate = 120;
            EnsureUrp();
            AssetDatabase.SaveAssets();
            Debug.Log("[Mindforge] Unity 2022.3 competition project configured for URP + 120 Hz target.");
        }

        private static void EnsureUrp()
        {
            UniversalRenderPipelineAsset pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelineAssetPath);
            if (pipeline == null)
            {
                UniversalRendererData renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererAssetPath);
                if (renderer == null)
                {
                    renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                    AssetDatabase.CreateAsset(renderer, RendererAssetPath);
                }

                pipeline = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
                AssetDatabase.CreateAsset(pipeline, PipelineAssetPath);

                // URP 14 does not expose the renderer-data list as a normal public
                // setter. Bind the separately persisted renderer asset once, then
                // validate the resulting project in the actual Unity Editor gate.
                SerializedObject serialized = new SerializedObject(pipeline);
                SerializedProperty list = serialized.FindProperty("m_RendererDataList");
                if (list == null)
                    throw new System.InvalidOperationException(
                        "URP renderer-data list field was not found; verify the pinned URP 14.x package before changing the project pin.");
                list.arraySize = 1;
                list.GetArrayElementAtIndex(0).objectReferenceValue = renderer;
                SerializedProperty index = serialized.FindProperty("m_DefaultRendererIndex");
                if (index != null) index.intValue = 0;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(pipeline);
            }

            // Unity 2022.3 project default plus per-quality override. Do not use the
            // deprecated GraphicsSettings.renderPipelineAsset alias.
            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.renderPipeline = pipeline;
        }
    }
}
#endif
