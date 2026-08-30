#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Mindforge.Editor
{
    /// <summary>
    /// Mesh-only refinement immediately after ProductionArtV09Builder. It preserves authored
    /// transforms/materials and swaps the builder's last stock Cube/Cylinder mesh references for
    /// reusable generated production meshes. No collider or gameplay object is created or moved.
    /// </summary>
    public static class ProductionStructuralRefinementV09Builder
    {
        public const string RootName = "Production_Structural_Refinement_V09";

        [MenuItem("Mindforge/Showcase/Apply Production Structural Refinement V0.9", priority = 45)]
        public static void ApplyOpenScene()
        {
            GameObject production = EditorSceneLookup.FindIncludingInactive(ProductionArtV09Builder.RootName);
            if (production == null)
                throw new InvalidOperationException("Structural refinement requires Production Art V0.9.");

            Transform oldMarker = production.transform.Find(RootName);
            if (oldMarker != null) UnityEngine.Object.DestroyImmediate(oldMarker.gameObject);

            Mesh chamfered = ProductionStructuralMeshV09.ChamferedPrism();
            Mesh fluted = ProductionMeshLibraryV09.FlutedColumn();
            int cubes = 0;
            int cylinders = 0;

            MeshFilter[] filters = production.GetComponentsInChildren<MeshFilter>(true);
            for (int i = 0; i < filters.Length; i++)
            {
                MeshFilter filter = filters[i];
                if (filter == null || filter.sharedMesh == null) continue;
                string meshName = filter.sharedMesh.name ?? string.Empty;

                if (string.Equals(meshName, "Cube", StringComparison.OrdinalIgnoreCase))
                {
                    filter.sharedMesh = chamfered;
                    EditorUtility.SetDirty(filter);
                    cubes++;
                    continue;
                }

                if (string.Equals(meshName, "Cylinder", StringComparison.OrdinalIgnoreCase))
                {
                    // Unity's built-in Cylinder is 2 units tall; the generated fluted column is
                    // 1 unit tall. Preserve the exact authored world height when swapping meshes.
                    Vector3 scale = filter.transform.localScale;
                    scale.y *= 2f;
                    filter.transform.localScale = scale;
                    filter.sharedMesh = fluted;
                    EditorUtility.SetDirty(filter.transform);
                    EditorUtility.SetDirty(filter);
                    cylinders++;
                }
            }

            GameObject marker = new GameObject(RootName);
            marker.transform.SetParent(production.transform, false);

            ValidateNoStockStructuralMeshes(production.transform, marker.transform);
            EditorUtility.SetDirty(marker);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();

            Debug.Log(
                $"[Mindforge:V09:Structure] Production blockout DNA removed from visible base composition: " +
                $"stock cubes→chamfered={cubes}; stock cylinders→generated fluted={cylinders}. " +
                "Transforms, materials, collision, interactions and gameplay authority were preserved.");
        }

        private static void ValidateNoStockStructuralMeshes(Transform production, Transform marker)
        {
            if (production == null) throw new InvalidOperationException("Missing production root after structural refinement.");
            MeshFilter[] filters = production.GetComponentsInChildren<MeshFilter>(true);
            for (int i = 0; i < filters.Length; i++)
            {
                MeshFilter filter = filters[i];
                if (filter == null || filter.sharedMesh == null) continue;
                if (marker != null && (filter.transform == marker || filter.transform.IsChildOf(marker))) continue;
                string meshName = filter.sharedMesh.name ?? string.Empty;
                if (string.Equals(meshName, "Cube", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(meshName, "Cylinder", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException(
                        "Production root still contains a stock structural mesh after refinement: " + filter.name + " / " + meshName);
            }

            if (marker.GetComponentsInChildren<Collider>(true).Length != 0 ||
                marker.GetComponentsInChildren<Rigidbody>(true).Length != 0 ||
                marker.GetComponentsInChildren<Light>(true).Length != 0)
                throw new InvalidOperationException("Structural refinement marker acquired gameplay/lighting authority.");
        }
    }
}
#endif
