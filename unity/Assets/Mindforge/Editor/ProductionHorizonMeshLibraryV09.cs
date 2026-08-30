#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Mindforge.Editor
{
    /// <summary>
    /// Deterministic distant ridge geometry viewed from inside the playable basin. These are
    /// silhouette/atmosphere meshes only: no collision, traversal, lighting or world authority.
    /// </summary>
    public static class ProductionHorizonMeshLibraryV09
    {
        public const string Root = "Assets/Mindforge/Generated/ProductionV09/HorizonMeshes";
        public const string MidRidgePath = Root + "/MidRidge.asset";
        public const string FarRidgePath = Root + "/FarRidge.asset";
        public const int RecipeVersion = 1;

        public static Mesh MidRidge() => Ensure(MidRidgePath, "MidRidge", () => BuildRidgeRing(128, 0.17f, 0.08f, 0.045f));
        public static Mesh FarRidge() => Ensure(FarRidgePath, "FarRidge", () => BuildRidgeRing(160, 0.22f, 0.06f, 0.032f));

        private static Mesh Ensure(string path, string baseName, Func<Mesh> factory)
        {
            EnsureFolder(Root);
            string expected = baseName + "_r" + RecipeVersion;
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null && string.Equals(existing.name, expected, StringComparison.Ordinal)) return existing;
            if (existing != null) AssetDatabase.DeleteAsset(path);

            Mesh mesh = factory();
            mesh.name = expected;
            Validate(mesh, path);
            AssetDatabase.CreateAsset(mesh, path);
            EditorUtility.SetDirty(mesh);
            return mesh;
        }

        private static Mesh BuildRidgeRing(int segments, float primaryAmplitude, float secondaryAmplitude, float tertiaryAmplitude)
        {
            segments = Mathf.Max(48, segments);
            List<Vector3> vertices = new List<Vector3>((segments + 1) * 2);
            List<Vector2> uv = new List<Vector2>(vertices.Capacity);
            List<int> triangles = new List<int>(segments * 6);

            for (int i = 0; i <= segments; i++)
            {
                float u = i / (float)segments;
                float a = u * Mathf.PI * 2f;
                float ridge =
                    0.52f +
                    primaryAmplitude * Mathf.Sin(a * 3f + 0.4f) +
                    secondaryAmplitude * Mathf.Sin(a * 7f - 1.2f) +
                    tertiaryAmplitude * Mathf.Sin(a * 13f + 2.1f);
                ridge = Mathf.Clamp(ridge, 0.18f, 0.88f);

                Vector3 radial = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                vertices.Add(radial + Vector3.down * 0.16f);
                vertices.Add(radial + Vector3.up * ridge);
                uv.Add(new Vector2(u, 0f));
                uv.Add(new Vector2(u, 1f));
            }

            for (int i = 0; i < segments; i++)
            {
                int a = i * 2;
                int b = a + 1;
                int c = a + 3;
                int d = a + 2;
                AddQuadFacingInward(vertices, triangles, a, b, c, d);
            }

            Mesh mesh = new Mesh();
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(triangles, 0, true);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AddQuadFacingInward(List<Vector3> vertices, List<int> triangles, int a, int b, int c, int d)
        {
            Vector3 center = (vertices[a] + vertices[b] + vertices[c] + vertices[d]) * 0.25f;
            Vector3 normal = Vector3.Cross(vertices[b] - vertices[a], vertices[c] - vertices[a]);
            Vector3 outward = new Vector3(center.x, 0f, center.z).normalized;
            bool currentlyOutward = Vector3.Dot(normal, outward) > 0f;
            if (currentlyOutward)
            {
                triangles.Add(a); triangles.Add(c); triangles.Add(b);
                triangles.Add(a); triangles.Add(d); triangles.Add(c);
            }
            else
            {
                triangles.Add(a); triangles.Add(b); triangles.Add(c);
                triangles.Add(a); triangles.Add(c); triangles.Add(d);
            }
        }

        private static void Validate(Mesh mesh, string label)
        {
            if (mesh == null || mesh.vertexCount < 96 || mesh.triangles.Length < 144)
                throw new InvalidOperationException("Invalid horizon mesh: " + label);
            Vector3[] normals = mesh.normals;
            Vector3[] vertices = mesh.vertices;
            if (normals == null || normals.Length != vertices.Length)
                throw new InvalidOperationException("Horizon normals missing: " + label);
            for (int i = 0; i < normals.Length; i++)
            {
                Vector3 n = normals[i];
                if (float.IsNaN(n.x) || float.IsNaN(n.y) || float.IsNaN(n.z) || n.sqrMagnitude < 0.20f)
                    throw new InvalidOperationException("Invalid horizon normal: " + label + " @ " + i);
            }
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
