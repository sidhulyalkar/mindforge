#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Mindforge.Editor
{
    /// <summary>
    /// Small generated mesh vocabulary for the BCI-facing Sanctum presentation. These assets
    /// replace obvious sphere/cube placeholders while leaving the existing calibration and gate
    /// components completely authoritative.
    /// </summary>
    public static class ProductionCalibrationMeshLibraryV09
    {
        public const string Root = "Assets/Mindforge/Generated/ProductionV09/CalibrationMeshes";
        public const string ResonanceLensPath = Root + "/ResonanceLens.asset";
        public const string PhaseRingPath = Root + "/PhaseRing.asset";
        public const string MembranePanelPath = Root + "/ThresholdMembranePanel.asset";
        public const int RecipeVersion = 2;

        public static Mesh ResonanceLens() => Ensure(ResonanceLensPath, "ResonanceLens", BuildResonanceLens);
        public static Mesh PhaseRing() => Ensure(PhaseRingPath, "PhaseRing", () => BuildTorus(52, 10, 0.5f, 0.038f));
        public static Mesh ThresholdMembranePanel() => Ensure(MembranePanelPath, "ThresholdMembranePanel", () => BuildMembranePanel(8, 12));

        private static Mesh Ensure(string path, string baseName, Func<Mesh> factory)
        {
            EnsureFolder(Root);
            string versionedName = baseName + "_r" + RecipeVersion;
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null && string.Equals(existing.name, versionedName, StringComparison.Ordinal))
                return existing;

            if (existing != null) AssetDatabase.DeleteAsset(path);
            Mesh mesh = factory();
            mesh.name = versionedName;
            ValidateMesh(mesh, path);
            AssetDatabase.CreateAsset(mesh, path);
            EditorUtility.SetDirty(mesh);
            return mesh;
        }

        private static Mesh BuildResonanceLens()
        {
            float t = (1f + Mathf.Sqrt(5f)) * 0.5f;
            Vector3[] v =
            {
                new Vector3(-1, t, 0), new Vector3(1, t, 0), new Vector3(-1, -t, 0), new Vector3(1, -t, 0),
                new Vector3(0, -1, t), new Vector3(0, 1, t), new Vector3(0, -1, -t), new Vector3(0, 1, -t),
                new Vector3(t, 0, -1), new Vector3(t, 0, 1), new Vector3(-t, 0, -1), new Vector3(-t, 0, 1),
            };
            int[,] faces =
            {
                {0,11,5},{0,5,1},{0,1,7},{0,7,10},{0,10,11},
                {1,5,9},{5,11,4},{11,10,2},{10,7,6},{7,1,8},
                {3,9,4},{3,4,2},{3,2,6},{3,6,8},{3,8,9},
                {4,9,5},{2,4,11},{6,2,10},{8,6,7},{9,8,1},
            };

            List<Vector3> vertices = new List<Vector3>(60);
            List<Vector2> uv = new List<Vector2>(60);
            List<int> triangles = new List<int>(60);
            for (int f = 0; f < faces.GetLength(0); f++)
            {
                int start = vertices.Count;
                for (int c = 0; c < 3; c++)
                {
                    Vector3 p = v[faces[f, c]].normalized * 0.5f;
                    p.y *= 1.14f;
                    vertices.Add(p);
                    uv.Add(new Vector2(0.5f + Mathf.Atan2(p.z, p.x) / (Mathf.PI * 2f), Mathf.InverseLerp(-0.57f, 0.57f, p.y)));
                }
                triangles.Add(start);
                triangles.Add(start + 1);
                triangles.Add(start + 2);
            }
            return Finish(vertices, uv, triangles, false);
        }

        private static Mesh BuildTorus(int majorSegments, int minorSegments, float radius, float tube)
        {
            majorSegments = Mathf.Max(16, majorSegments);
            minorSegments = Mathf.Max(6, minorSegments);
            List<Vector3> vertices = new List<Vector3>((majorSegments + 1) * (minorSegments + 1));
            List<Vector2> uv = new List<Vector2>(vertices.Capacity);
            List<int> triangles = new List<int>(majorSegments * minorSegments * 6);

            for (int i = 0; i <= majorSegments; i++)
            {
                float u = i / (float)majorSegments;
                float a = u * Mathf.PI * 2f;
                Vector3 radial = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                for (int j = 0; j <= minorSegments; j++)
                {
                    float vv = j / (float)minorSegments;
                    float b = vv * Mathf.PI * 2f;
                    Vector3 p = radial * (radius + Mathf.Cos(b) * tube) + Vector3.up * (Mathf.Sin(b) * tube);
                    vertices.Add(p);
                    uv.Add(new Vector2(u, vv));
                }
            }

            int stride = minorSegments + 1;
            for (int i = 0; i < majorSegments; i++)
            for (int j = 0; j < minorSegments; j++)
            {
                int a = i * stride + j;
                int b = a + 1;
                int c = a + stride + 1;
                int d = a + stride;
                AddQuad(triangles, a, d, c, b);
            }
            return Finish(vertices, uv, triangles, true);
        }

        private static Mesh BuildMembranePanel(int columns, int rows)
        {
            columns = Mathf.Max(3, columns);
            rows = Mathf.Max(4, rows);
            List<Vector3> vertices = new List<Vector3>((columns + 1) * (rows + 1) * 2);
            List<Vector2> uv = new List<Vector2>(vertices.Capacity);
            List<int> triangles = new List<int>(columns * rows * 12);
            const float thickness = 0.018f;

            for (int side = 0; side < 2; side++)
            {
                float sign = side == 0 ? -1f : 1f;
                for (int y = 0; y <= rows; y++)
                {
                    float v = y / (float)rows;
                    float py = v - 0.5f;
                    for (int x = 0; x <= columns; x++)
                    {
                        float u = x / (float)columns;
                        float px = u - 0.5f;
                        float dome = (1f - Mathf.Clamp01(px * px * 3.2f + py * py * 2.2f)) * 0.055f;
                        vertices.Add(new Vector3(px, py, dome + sign * thickness));
                        uv.Add(new Vector2(u, v));
                    }
                }
            }

            int stride = columns + 1;
            int layer = stride * (rows + 1);
            for (int y = 0; y < rows; y++)
            for (int x = 0; x < columns; x++)
            {
                int a = y * stride + x;
                int b = a + 1;
                int c = a + stride + 1;
                int d = a + stride;
                AddQuad(triangles, a, d, c, b);

                int aa = layer + a;
                int bb = layer + b;
                int cc = layer + c;
                int dd = layer + d;
                AddQuad(triangles, aa, bb, cc, dd);
            }
            return Finish(vertices, uv, triangles, true);
        }

        private static Mesh Finish(List<Vector3> vertices, List<Vector2> uv, List<int> triangles, bool smooth)
        {
            Mesh mesh = new Mesh();
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(triangles, 0, true);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            if (!smooth) mesh.RecalculateNormals();
            return mesh;
        }

        private static void ValidateMesh(Mesh mesh, string label)
        {
            if (mesh == null || mesh.vertexCount < 3 || mesh.triangles.Length < 3)
                throw new InvalidOperationException("Invalid generated calibration mesh: " + label);
            Vector3[] normals = mesh.normals;
            if (normals == null || normals.Length != mesh.vertexCount)
                throw new InvalidOperationException("Missing generated normals: " + label);
            for (int i = 0; i < normals.Length; i++)
            {
                Vector3 n = normals[i];
                if (float.IsNaN(n.x) || float.IsNaN(n.y) || float.IsNaN(n.z) || n.sqrMagnitude < 0.25f)
                    throw new InvalidOperationException("Collapsed generated normal: " + label + " @ " + i);
            }
        }

        private static void AddQuad(List<int> triangles, int a, int b, int c, int d)
        {
            triangles.Add(a); triangles.Add(b); triangles.Add(c);
            triangles.Add(a); triangles.Add(c); triangles.Add(d);
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
