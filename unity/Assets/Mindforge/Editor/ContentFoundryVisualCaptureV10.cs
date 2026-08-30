#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Mindforge.Editor
{
    /// <summary>
    /// Deterministic edit-mode reference captures for production-art review.
    /// Captures are diagnostics only: they never mutate combat, collision, neural state or
    /// the canonical Unity promotion result.
    /// </summary>
    public static class ContentFoundryVisualCaptureV10
    {
        private const int Width = 1280;
        private const int Height = 720;

        private struct ViewSpec
        {
            public string Name;
            public string TargetName;
            public Vector3 Offset;

            public ViewSpec(string name, string targetName, Vector3 offset)
            {
                Name = name;
                TargetName = targetName;
                Offset = offset;
            }
        }

        private static readonly ViewSpec[] Views =
        {
            new ViewSpec("sanctum_nave", "Production_Sanctum_Nave", new Vector3(0f, 4.0f, -24f)),
            new ViewSpec("threshold", "Production_Threshold_Facade", new Vector3(0f, 5.5f, -20f)),
            new ViewSpec("market", "Production_Market_Arcade", new Vector3(-16f, 8f, -15f)),
            new ViewSpec("fracture", "Production_Fracture_Landmark", new Vector3(15f, 8f, -15f)),
            new ViewSpec("cathedral", "Production_Cathedral_Approach", new Vector3(-18f, 9f, -21f)),
            new ViewSpec("skyline", "Production_Skyline", new Vector3(0f, 20f, -44f)),
        };

        [MenuItem("Mindforge/Content Foundry/Capture Production Reference Views", priority = 20)]
        public static void CaptureReferenceViews()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new UnityEditor.Build.BuildFailedException("Stop Play Mode before deterministic Foundry capture.");

            GameObject production = EditorSceneLookup.FindIncludingInactive(ProductionArtV09Builder.RootName);
            if (production == null)
                throw new UnityEditor.Build.BuildFailedException("Build V0.9 production art before capturing Foundry reference views.");

            string repo = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
            string output = Path.Combine(repo, "experiments", "reports", "visual-captures");
            Directory.CreateDirectory(output);

            GameObject cameraObject = new GameObject("__MindforgeFoundryCaptureCameraV10");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.enabled = false;
            camera.fieldOfView = 58f;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 650f;
            camera.aspect = Width / (float)Height;
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.allowHDR = true;
            camera.allowMSAA = true;

            List<string> entries = new List<string>();
            try
            {
                for (int i = 0; i < Views.Length; i++)
                {
                    ViewSpec view = Views[i];
                    Transform target = FindNamed(production.transform, view.TargetName);
                    if (target == null)
                        throw new UnityEditor.Build.BuildFailedException("Foundry capture target is missing: " + view.TargetName);
                    Bounds bounds = CalculateBounds(target);
                    Vector3 lookAt = bounds.center + Vector3.up * Mathf.Min(2.5f, bounds.extents.y * 0.18f);
                    camera.transform.position = bounds.center + view.Offset;
                    camera.transform.rotation = Quaternion.LookRotation((lookAt - camera.transform.position).normalized, Vector3.up);
                    string path = Path.Combine(output, view.Name + ".png");
                    byte[] png = Render(camera);
                    File.WriteAllBytes(path, png);
                    entries.Add($"    {{\"name\":\"{view.Name}\",\"file\":\"{view.Name}.png\",\"sha256\":\"{Sha256(png)}\"}}");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }

            string report = Path.Combine(output, "manifest.json");
            File.WriteAllText(
                report,
                "{\n" +
                "  \"schema\": \"mindforge.visual_capture_manifest.v1\",\n" +
                $"  \"generated_utc\": \"{DateTime.UtcNow:o}\",\n" +
                "  \"canonical_promotion_evidence\": false,\n" +
                $"  \"width\": {Width},\n" +
                $"  \"height\": {Height},\n" +
                "  \"views\": [\n" + string.Join(",\n", entries) + "\n  ]\n}\n",
                Encoding.UTF8);

            AssetDatabase.Refresh();
            Debug.Log($"[Mindforge:Foundry] Captured {Views.Length} deterministic production reference views to {output}. Compare these across heads; runtime feel still requires observed playtesting.");
        }

        private static byte[] Render(Camera camera)
        {
            RenderTexture renderTexture = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
            renderTexture.antiAliasing = 1;
            Texture2D texture = new Texture2D(Width, Height, TextureFormat.RGB24, false, false);
            RenderTexture previous = RenderTexture.active;
            try
            {
                camera.targetTexture = renderTexture;
                renderTexture.Create();
                camera.Render();
                RenderTexture.active = renderTexture;
                texture.ReadPixels(new Rect(0, 0, Width, Height), 0, 0, false);
                texture.Apply(false, false);
                return texture.EncodeToPNG();
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previous;
                renderTexture.Release();
                UnityEngine.Object.DestroyImmediate(renderTexture);
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static Transform FindNamed(Transform root, string name)
        {
            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
                if (all[i] != null && string.Equals(all[i].name, name, StringComparison.Ordinal)) return all[i];
            return null;
        }

        private static Bounds CalculateBounds(Transform root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            bool found = false;
            Bounds bounds = new Bounds(root.position, Vector3.one);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || !renderer.enabled) continue;
                if (!found) { bounds = renderer.bounds; found = true; }
                else bounds.Encapsulate(renderer.bounds);
            }
            if (!found)
                throw new UnityEditor.Build.BuildFailedException("Foundry capture target has no enabled renderers: " + root.name);
            return bounds;
        }

        private static string Sha256(byte[] bytes)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(bytes);
                StringBuilder result = new StringBuilder(hash.Length * 2);
                for (int i = 0; i < hash.Length; i++) result.Append(hash[i].ToString("x2"));
                return result.ToString();
            }
        }
    }
}
#endif
