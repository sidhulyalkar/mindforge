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
            // prototype. Winding is explicitly outward so stock URP back-face culling remains
            // valid and we do not need a more expensive Cull Off material workaround.
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
                triangles.Add(topCenter); triangles.Add(next); triangles.Add(i);
                triangles.Add(bottomCenter); triangles.Add(5 + i); triangles.Add(5 + next);
                AddQuad(triangles, i, next, 5 + next, 5 + i);
            }
            AssertConvexOutward(vertices, triangles, "BrokenSlab");
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
                0,2,1, 0,3,2, 0,4,3, 0,1,4,
                5,1,2, 5,2,3, 5,3,4, 5,4,1,
            };
            AssertConvexOutward(vertices, triangles, "SignalShard");
            return Finish(vertices, triangles);
        }

        private static Mesh BuildHangingRibbon(int verticalSegments)
        {
            verticalSegments = Mathf.Max(4, verticalSegments);
            const float thickness = 0.018f;
            List<Vector3> vertices = new List<Vector3>((verticalSegments + 1) * 4);
            List<Vector2> uv = new List<Vector2>((verticalSegments + 1) * 4);
            List<int> triangles = new List<int>(verticalSegments * 24 + 12);

            // Give the cloth real thickness and independent front/back vertices. The earlier
            // tempting shortcut of drawing opposite-winding triangles over the same vertices
            // makes RecalculateNormals average opposing faces toward zero. A tiny closed ribbon
            // is both more robust and better lit under the ordinary production material.
            for (int i = 0; i <= verticalSegments; i++)
            {
                float t = i / (float)verticalSegments;
                float y = 0.5f - t;
                float taper = Mathf.Lerp(1f, 0.78f, t);
                float z = Mathf.Sin(t * Mathf.PI * 1.35f) * 0.075f + t * t * 0.055f;
                float leftX = -0.5f * taper;
                float rightX = 0.5f * taper;
                float ripple = 0.018f * Mathf.Sin(t * 9f);

                vertices.Add(new Vector3(leftX, y, z - thickness));
                vertices.Add(new Vector3(rightX, y, z + ripple - thickness));
                vertices.Add(new Vector3(leftX, y, z + thickness));
                vertices.Add(new Vector3(rightX, y, z + ripple + thickness));
                uv.Add(new Vector2(0f, 1f - t));
                uv.Add(new Vector2(1f, 1f - t));
                uv.Add(new Vector2(0f, 1f - t));
                uv.Add(new Vector2(1f, 1f - t));
            }

            for (int i = 0; i < verticalSegments; i++)
            {
                int fl = i * 4;
                int fr = fl + 1;
                int bl = fl + 2;
                int br = fl + 3;
                int nfl = fl + 4;
                int nfr = fl + 5;
                int nbl = fl + 6;
                int nbr = fl + 7;

                AddQuad(triangles, fl, fr, nfr, nfl);      // front, -Z
                AddQuad(triangles, bl, nbl, nbr, br);      // back, +Z
                AddQuad(triangles, fl, nfl, nbl, bl);      // left edge
                AddQuad(triangles, fr, br, nbr, nfr);      // right edge
            }

            int last = verticalSegments * 4;
            AddQuad(triangles, 0, 2, 3, 1);                       // top cap
            AddQuad(triangles, last, last + 1, last + 3, last + 2); // bottom cap
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

        private static void AssertConvexOutward(List<Vector3> vertices, List<int> triangles, string label)
        {
            Vector3 center = Vector3.zero;
            for (int i = 0; i < vertices.Count; i++) center += vertices[i];
            center /= Mathf.Max(1, vertices.Count);

            for (int i = 0; i + 2 < triangles.Count; i += 3)
            {
                Vector3 a = vertices[triangles[i]];
                Vector3 b = vertices[triangles[i + 1]];
                Vector3 c = vertices[triangles[i + 2]];
                Vector3 face = Vector3.Cross(b - a, c - a);
                Vector3 centroid = (a + b + c) / 3f;
                if (face.sqrMagnitude < 1e-9f || Vector3.Dot(face, centroid - center) <= 0f)
                    throw new InvalidOperationException($"{label} generated a degenerate or inward-facing triangle at index {i / 3}.");
            }
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

            Vector3[] normals = mesh.normals;
            for (int i = 0; i < normals.Length; i++)
                if (!IsFinite(normals[i]) || normals[i].sqrMagnitude < 0.25f)
                    throw new InvalidOperationException($"Generated story mesh contains an invalid normal at vertex {i}.");
            return mesh;
        }

        private static bool IsFinite(Vector3 v)
            => !float.IsNaN(v.x) && !float.IsNaN(v.y) && !float.IsNaN(v.z) &&
               !float.IsInfinity(v.x) && !float.IsInfinity(v.y) && !float.IsInfinity(v.z);

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
