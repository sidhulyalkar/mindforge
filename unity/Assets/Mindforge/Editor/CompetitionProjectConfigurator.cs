#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Mindforge.Editor
{
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
                UniversalRendererData renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer, RendererAssetPath);
                pipeline = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
                AssetDatabase.CreateAsset(pipeline, PipelineAssetPath);
                SerializedObject serialized = new SerializedObject(pipeline);
                SerializedProperty list = serialized.FindProperty("m_RendererDataList");
                if (list == null) throw new System.InvalidOperationException("URP renderer-data list field was not found; verify URP 14.x package/API.");
                list.arraySize = 1;
                list.GetArrayElementAtIndex(0).objectReferenceValue = renderer;
                SerializedProperty index = serialized.FindProperty("m_DefaultRendererIndex");
                if (index != null) index.intValue = 0;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            GraphicsSettings.renderPipelineAsset = pipeline;
            QualitySettings.renderPipeline = pipeline;
        }
    }
}
#endif
