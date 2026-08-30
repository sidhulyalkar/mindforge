#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Mindforge.Editor
{
    /// <summary>
    /// Small reusable mesh vocabulary for authored environmental history. These meshes are
    /// deliberately not gameplay geometry: they exist to break procedural symmetry with
    /// visible repair, fracture, age and suspended infrastructure while canonical collision
    /// remains owned by the underlying world builders.
    /// </summary>
    public static class ProductionStoryMeshLibraryV09
    {
        public const string Root = "Assets/Mindforge/Generated/ProductionV09/StoryMeshes";
        public const string BrokenSlabPath = Root + "/BrokenSlab.asset";
        public const string SignalShardPath = Root + "/SignalShard.asset";
        public const string HangingRibbonPath = Root + "/HangingRibbon.asset";
        public const string CableArcPath = Root + "/CableArc.asset";

        public static Mesh BrokenSlab() => Ensure(BrokenSlabPath, BuildBrokenSlab);
        public static Mesh SignalShard() => Ensure(SignalShardPath, BuildSignalShard);
        public static Mesh HangingRibbon() => Ensure(HangingRibbonPath, () => BuildHangingRibbon(8));
        public static Mesh CableArc() => Ensure(CableArcPath, () => BuildCableArc(14, 6));

        private static Mesh Ensure(string path, Func<Mesh> factory)
        {
            EnsureFolder(Root);
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null) return existing;

            Mesh mesh = factory();
            mesh.name = System.IO.Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(mesh, path);
            EditorUtility.SetDirty(mesh);
            return mesh;
        }

        private static Mesh BuildBrokenSlab()
        {
            // Irregular top/bottom rings avoid the perfect-box silhouette that dominated the
            // prototype. The mesh remains simple enough to repeat dozens of times if needed.
            Vector3[] top =
            {
                new Vector3(-0.54f, 0.13f, -0.42f),
                new Vector3( 0.47f, 0.17f, -0.50f),
                new Vector3( 0.58f, 0.09f,  0.31f),
                new Vector3( 0.14f, 0.16f,  0.53f),
                new Vector3(-0.50f, 0.10f,  0.38f),
            };
            Vector3[] bottom =
            {
                new Vector3(-0.50f, -0.14f, -0.39f),
                new Vector3( 0.43f, -0.12f, -0.46f),
                new Vector3( 0.53f, -0.16f,  0.28f),
                new Vector3( 0.11f, -0.13f,  0.49f),
                new Vector3(-0.46f, -0.15f,  0.35f),
            };

            List<Vector3> vertices = new List<Vector3>(12);
            vertices.AddRange(top);
            vertices.AddRange(bottom);
            int topCenter = vertices.Count;
            vertices.Add(new Vector3(0.01f, 0.14f, 0.01f));
            int bottomCenter = vertices.Count;
            vertices.Add(new Vector3(0.00f, -0.14f, -0.01f));

            List<int> triangles = new List<int>(60);
            for (int i = 0; i < 5; i++)
            {
                int next = (i + 1) % 5;
                triangles.Add(topCenter); triangles.Add(i); triangles.Add(next);
                triangles.Add(bottomCenter); triangles.Add(5 + next); triangles.Add(5 + i);
                AddQuad(triangles, i, 5 + i, 5 + next, next);
            }
            return Finish(vertices, triangles);
        }

        private static Mesh BuildSignalShard()
        {
            List<Vector3> vertices = new List<Vector3>
            {
                new Vector3(0f, 0.72f, 0f),
                new Vector3(-0.34f, 0.06f, -0.18f),
                new Vector3(0.18f, 0.12f, -0.32f),
                new Vector3(0.38f, -0.02f, 0.16f),
                new Vector3(-0.16f, 0.04f, 0.34f),
                new Vector3(0.03f, -0.78f, -0.02f),
            };
            List<int> triangles = new List<int>
            {
                0,1,2, 0,2,3, 0,3,4, 0,4,1,
                5,2,1, 5,3,2, 5,4,3, 5,1,4,
            };
            return Finish(vertices, triangles);
        }

        private static Mesh BuildHangingRibbon(int verticalSegments)
        {
            verticalSegments = Mathf.Max(4, verticalSegments);
            List<Vector3> vertices = new List<Vector3>((verticalSegments + 1) * 2);
            List<Vector2> uv = new List<Vector2>((verticalSegments + 1) * 2);
            List<int> triangles = new List<int>(verticalSegments * 12);

            for (int i = 0; i <= verticalSegments; i++)
            {
                float t = i / (float)verticalSegments;
                float y = 0.5f - t;
                float taper = Mathf.Lerp(1f, 0.78f, t);
                float z = Mathf.Sin(t * Mathf.PI * 1.35f) * 0.075f + t * t * 0.055f;
                vertices.Add(new Vector3(-0.5f * taper, y, z));
                vertices.Add(new Vector3( 0.5f * taper, y, z + 0.018f * Mathf.Sin(t * 9f)));
                uv.Add(new Vector2(0f, 1f - t));
                uv.Add(new Vector2(1f, 1f - t));
            }

            for (int i = 0; i < verticalSegments; i++)
            {
                int a = i * 2;
                int b = a + 1;
                int c = a + 3;
                int d = a + 2;
                AddQuad(triangles, a, d, c, b);
                // Double-sided without requiring a special cull-off material variant.
                AddQuad(triangles, b, c, d, a);
            }
            return Finish(vertices, uv, triangles);
        }

        private static Mesh BuildCableArc(int pathSegments, int radialSegments)
        {
            pathSegments = Mathf.Max(6, pathSegments);
            radialSegments = Mathf.Max(4, radialSegments);
            List<Vector3> vertices = new List<Vector3>((pathSegments + 1) * radialSegments);
            List<Vector2> uv = new List<Vector2>(vertices.Capacity);
            List<int> triangles = new List<int>(pathSegments * radialSegments * 6);

            for (int i = 0; i <= pathSegments; i++)
            {
                float t = i / (float)pathSegments;
                float x = Mathf.Lerp(-0.5f, 0.5f, t);
                float centered = t * 2f - 1f;
                float y = -0.34f * (1f - centered * centered);
                float z = Mathf.Sin(t * Mathf.PI) * 0.035f;
                Vector3 center = new Vector3(x, y, z);

                float dx = 1f;
                float dy = 1.36f * centered;
                Vector3 tangent = new Vector3(dx, dy, 0f).normalized;
                Vector3 side = Vector3.Cross(tangent, Vector3.forward).normalized;
                Vector3 forward = Vector3.Cross(side, tangent).normalized;
                const float radius = 0.025f;

                for (int r = 0; r < radialSegments; r++)
                {
                    float a = r / (float)radialSegments * Mathf.PI * 2f;
                    Vector3 offset = side * (Mathf.Cos(a) * radius) + forward * (Mathf.Sin(a) * radius);
                    vertices.Add(center + offset);
                    uv.Add(new Vector2(t, r / (float)radialSegments));
                }
            }

            for (int i = 0; i < pathSegments; i++)
            for (int r = 0; r < radialSegments; r++)
            {
                int nextR = (r + 1) % radialSegments;
                int a = i * radialSegments + r;
                int b = i * radialSegments + nextR;
                int c = (i + 1) * radialSegments + nextR;
                int d = (i + 1) * radialSegments + r;
                AddQuad(triangles, a, d, c, b);
            }
            return Finish(vertices, uv, triangles);
        }

        private static void AddQuad(List<int> triangles, int a, int b, int c, int d)
        {
            triangles.Add(a); triangles.Add(b); triangles.Add(c);
            triangles.Add(a); triangles.Add(c); triangles.Add(d);
        }

        private static Mesh Finish(List<Vector3> vertices, List<int> triangles)
        {
            List<Vector2> uv = new List<Vector2>(vertices.Count);
            for (int i = 0; i < vertices.Count; i++)
                uv.Add(new Vector2(vertices[i].x + 0.5f, vertices[i].z + 0.5f));
            return Finish(vertices, uv, triangles);
        }

        private static Mesh Finish(List<Vector3> vertices, List<Vector2> uv, List<int> triangles)
        {
            Mesh mesh = new Mesh();
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(triangles, 0, true);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
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
