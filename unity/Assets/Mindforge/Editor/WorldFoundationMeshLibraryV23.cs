#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mindforge.Editor
{
    /// <summary>
    /// Small deterministic mesh recipes used by the V0.23 world-foundation pass.
    ///
    /// The inward-facing patch is intentionally separate from WorldSoulMeshLibraryV20:
    /// ordinary terrain needs upward winding, while a cavern ceiling viewed from below needs
    /// inward/downward winding and normals. Keeping those semantic contracts separate prevents
    /// a future terrain-cache change from silently turning the cavern shell inside out.
    ///
    /// Technique provenance:
    /// - aadebdeb/ProceduralMesh (MIT): deterministic recipe-over-binary mesh workflow.
    /// - SebLague/Procedural-Cave-Generation (MIT): generate the visible cave shell and its
    ///   collision from the same topology rather than maintaining unrelated render/collision art.
    /// </summary>
    public static class WorldFoundationMeshLibraryV23
    {
        public const string Root = "Assets/Mindforge/Generated/V23/Meshes";
        public const int MeshRevision = 1;

        private static readonly Dictionary<string, Mesh> Cache = new Dictionary<string, Mesh>();

        public static Mesh InwardTerrainPatch(
            string assetName,
            float xMin,
            float xMax,
            float zMin,
            float zMax,
            int xSegments,
            int zSegments,
            Func<float, float, float> heightSampler)
        {
            EnsureFolder(Root);
            if (Cache.TryGetValue(assetName, out Mesh cached) && cached != null) return cached;

            string path = $"{Root}/{assetName}.asset";
            string expectedName = $"{assetName}_inward_r{MeshRevision}";
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null || mesh.name != expectedName)
            {
                Mesh fresh = BuildTransientInwardPatch(
                    xMin,
                    xMax,
                    zMin,
                    zMax,
                    Mathf.Clamp(xSegments, 2, 96),
                    Mathf.Clamp(zSegments, 2, 160),
                    heightSampler);
                mesh = Upsert(path, expectedName, fresh);
            }

            Cache[assetName] = mesh;
            return mesh;
        }

        public static Mesh BuildTransientInwardPatch(
            float xMin,
            float xMax,
            float zMin,
            float zMax,
            int xSegments,
            int zSegments,
            Func<float, float, float> heightSampler)
        {
            xSegments = Mathf.Clamp(xSegments, 2, 96);
            zSegments = Mathf.Clamp(zSegments, 2, 160);
            int stride = xSegments + 1;
            List<Vector3> vertices = new List<Vector3>((xSegments + 1) * (zSegments + 1));
            List<Vector2> uv = new List<Vector2>(vertices.Capacity);
            List<int> triangles = new List<int>(xSegments * zSegments * 6);

            for (int z = 0; z <= zSegments; z++)
            {
                float tz = z / (float)zSegments;
                float wz = Mathf.Lerp(zMin, zMax, tz);
                for (int x = 0; x <= xSegments; x++)
                {
                    float tx = x / (float)xSegments;
                    float wx = Mathf.Lerp(xMin, xMax, tx);
                    float wy = heightSampler != null ? heightSampler(wx, wz) : 0f;
                    vertices.Add(new Vector3(wx, wy, wz));
                    uv.Add(new Vector2(wx * 0.085f, wz * 0.085f));
                }
            }

            // Reverse the ordinary terrain winding so RecalculateNormals points into the cavern.
            // This is more robust than relying on a double-sided material: lighting, normal maps,
            // shadowing and any future one-sided shader all receive the correct geometric normal.
            for (int z = 0; z < zSegments; z++)
            {
                for (int x = 0; x < xSegments; x++)
                {
                    int a = z * stride + x;
                    int b = a + 1;
                    int d = a + stride;
                    int c = d + 1;
                    triangles.Add(a); triangles.Add(c); triangles.Add(d);
                    triangles.Add(a); triangles.Add(b); triangles.Add(c);
                }
            }

            Mesh mesh = new Mesh { name = "MindforgeInwardTerrain" };
            if (vertices.Count > 65535) mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(triangles, 0, true);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh Upsert(string path, string expectedName, Mesh fresh)
        {
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null)
            {
                fresh.name = expectedName;
                AssetDatabase.CreateAsset(fresh, path);
                EditorUtility.SetDirty(fresh);
                return fresh;
            }

            EditorUtility.CopySerialized(fresh, existing);
            existing.name = expectedName;
            EditorUtility.SetDirty(existing);
            UnityEngine.Object.DestroyImmediate(fresh);
            return existing;
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