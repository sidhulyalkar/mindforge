#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Mindforge.Editor
{
    /// <summary>
    /// Small deterministic mesh library for the V0.9 art pass. It replaces the most visible
    /// primitive-box silhouettes with reusable curved/fluted architectural meshes while the
    /// existing gameplay colliders remain authoritative. Meshes are editor-generated assets,
    /// so the repository stores the recipe rather than a pile of opaque binary models.
    /// </summary>
    public static class ProductionMeshLibraryV09
    {
        public const string Root = "Assets/Mindforge/Generated/ProductionV09/Meshes";
        public const string FlutedColumnPath = Root + "/FlutedColumn.asset";
        public const string PointedArchPath = Root + "/PointedArch.asset";
        public const string SpirePath = Root + "/CathedralSpire.asset";
        public const string CanopyPath = Root + "/GardenCanopy.asset";

        public static Mesh FlutedColumn() => Ensure(FlutedColumnPath, () => BuildFlutedColumn(40, 10, 0.5f, 1f, 0.055f));
        public static Mesh PointedArch() => Ensure(PointedArchPath, () => BuildPointedArch(34, 1f, 1.35f, 0.16f, 0.18f));
        public static Mesh CathedralSpire() => Ensure(SpirePath, () => BuildSpire(12, 0.5f, 1.35f));
        public static Mesh GardenCanopy() => Ensure(CanopyPath, () => BuildCanopy(24, 12));

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

        private static Mesh BuildFlutedColumn(int segments, int flutes, float radius, float height, float fluteDepth)
        {
            segments = Mathf.Max(16, segments);
            List<Vector3> vertices = new List<Vector3>((segments + 1) * 2 + 2);
            List<Vector2> uv = new List<Vector2>(vertices.Capacity);
            List<int> triangles = new List<int>(segments * 12);

            for (int ring = 0; ring < 2; ring++)
            {
                float y = ring == 0 ? -height * 0.5f : height * 0.5f;
                for (int i = 0; i <= segments; i++)
                {
                    float t = i / (float)segments;
                    float a = t * Mathf.PI * 2f;
                    float r = radius * (1f - fluteDepth + fluteDepth * (0.5f + 0.5f * Mathf.Cos(a * flutes)));
                    vertices.Add(new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r));
                    uv.Add(new Vector2(t, ring));
                }
            }

            int stride = segments + 1;
            for (int i = 0; i < segments; i++)
            {
                int a = i;
                int b = i + 1;
                int c = stride + i + 1;
                int d = stride + i;
                triangles.Add(a); triangles.Add(d); triangles.Add(c);
                triangles.Add(a); triangles.Add(c); triangles.Add(b);
            }

            int bottomCenter = vertices.Count;
            vertices.Add(new Vector3(0f, -height * 0.5f, 0f));
            uv.Add(new Vector2(0.5f, 0.5f));
            int topCenter = vertices.Count;
            vertices.Add(new Vector3(0f, height * 0.5f, 0f));
            uv.Add(new Vector2(0.5f, 0.5f));
            for (int i = 0; i < segments; i++)
            {
                triangles.Add(bottomCenter); triangles.Add(i + 1); triangles.Add(i);
                triangles.Add(topCenter); triangles.Add(stride + i); triangles.Add(stride + i + 1);
            }

            return Finish(vertices, uv, triangles);
        }

        private static Mesh BuildPointedArch(int halfSegments, float halfSpan, float rise, float bandWidth, float depth)
        {
            halfSegments = Mathf.Max(10, halfSegments);
            List<Vector3> centerline = new List<Vector3>(halfSegments * 2 + 1);
            Vector3 left = new Vector3(-halfSpan, 0f, 0f);
            Vector3 apex = new Vector3(0f, rise, 0f);
            Vector3 right = new Vector3(halfSpan, 0f, 0f);
            Vector3 leftControl = new Vector3(-halfSpan * 0.72f, rise * 0.82f, 0f);
            Vector3 rightControl = new Vector3(halfSpan * 0.72f, rise * 0.82f, 0f);
            for (int i = 0; i <= halfSegments; i++)
                centerline.Add(Quadratic(left, leftControl, apex, i / (float)halfSegments));
            for (int i = 1; i <= halfSegments; i++)
                centerline.Add(Quadratic(apex, rightControl, right, i / (float)halfSegments));

            int count = centerline.Count;
            Vector2[] outward = new Vector2[count];
            for (int i = 0; i < count; i++)
            {
                Vector3 prev = centerline[Mathf.Max(0, i - 1)];
                Vector3 next = centerline[Mathf.Min(count - 1, i + 1)];
                Vector2 tangent = new Vector2(next.x - prev.x, next.y - prev.y).normalized;
                Vector2 n = new Vector2(-tangent.y, tangent.x);
                if (Vector2.Dot(n, new Vector2(centerline[i].x, centerline[i].y - rise * 0.35f)) < 0f) n = -n;
                outward[i] = n;
            }

            List<Vector3> vertices = new List<Vector3>(count * 4);
            List<Vector2> uv = new List<Vector2>(count * 4);
            float zFront = -depth * 0.5f;
            float zBack = depth * 0.5f;
            for (int layer = 0; layer < 2; layer++)
            {
                float z = layer == 0 ? zFront : zBack;
                for (int side = 0; side < 2; side++)
                {
                    float offset = side == 0 ? bandWidth * 0.5f : -bandWidth * 0.5f;
                    for (int i = 0; i < count; i++)
                    {
                        Vector2 n = outward[i] * offset;
                        Vector3 p = centerline[i] + new Vector3(n.x, n.y, z);
                        vertices.Add(p);
                        uv.Add(new Vector2(i / (float)(count - 1), side));
                    }
                }
            }

            List<int> triangles = new List<int>((count - 1) * 24 + 24);
            int outerFront = 0;
            int innerFront = count;
            int outerBack = count * 2;
            int innerBack = count * 3;
            for (int i = 0; i < count - 1; i++)
            {
                AddQuad(triangles, outerFront + i, outerFront + i + 1, innerFront + i + 1, innerFront + i);
                AddQuad(triangles, outerBack + i, innerBack + i, innerBack + i + 1, outerBack + i + 1);
                AddQuad(triangles, outerFront + i, outerBack + i, outerBack + i + 1, outerFront + i + 1);
                AddQuad(triangles, innerFront + i, innerFront + i + 1, innerBack + i + 1, innerBack + i);
            }
            AddQuad(triangles, outerFront, innerFront, innerBack, outerBack);
            AddQuad(triangles, outerFront + count - 1, outerBack + count - 1, innerBack + count - 1, innerFront + count - 1);

            return Finish(vertices, uv, triangles);
        }

        private static Mesh BuildSpire(int segments, float radius, float height)
        {
            segments = Mathf.Max(6, segments);
            List<Vector3> vertices = new List<Vector3>();
            List<Vector2> uv = new List<Vector2>();
            List<int> triangles = new List<int>();
            float[] ys = { 0f, height * 0.55f, height * 0.86f };
            float[] rs = { radius, radius * 0.55f, radius * 0.18f };
            for (int ring = 0; ring < ys.Length; ring++)
            {
                for (int i = 0; i < segments; i++)
                {
                    float a = i / (float)segments * Mathf.PI * 2f;
                    vertices.Add(new Vector3(Mathf.Cos(a) * rs[ring], ys[ring], Mathf.Sin(a) * rs[ring]));
                    uv.Add(new Vector2(i / (float)segments, ring / (float)(ys.Length - 1)));
                }
            }
            for (int ring = 0; ring < ys.Length - 1; ring++)
            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                int a = ring * segments + i;
                int b = ring * segments + next;
                int c = (ring + 1) * segments + next;
                int d = (ring + 1) * segments + i;
                AddQuad(triangles, a, d, c, b);
            }
            int tip = vertices.Count;
            vertices.Add(new Vector3(0f, height, 0f));
            uv.Add(new Vector2(0.5f, 1f));
            int last = (ys.Length - 1) * segments;
            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                triangles.Add(last + i); triangles.Add(tip); triangles.Add(last + next);
            }
            return Finish(vertices, uv, triangles);
        }

        private static Mesh BuildCanopy(int radialSegments, int verticalSegments)
        {
            radialSegments = Mathf.Max(12, radialSegments);
            verticalSegments = Mathf.Max(6, verticalSegments);
            List<Vector3> vertices = new List<Vector3>();
            List<Vector2> uv = new List<Vector2>();
            List<int> triangles = new List<int>();
            for (int y = 0; y <= verticalSegments; y++)
            {
                float v = y / (float)verticalSegments;
                float phi = v * Mathf.PI;
                float sy = Mathf.Cos(phi);
                float rr = Mathf.Sin(phi);
                for (int x = 0; x <= radialSegments; x++)
                {
                    float u = x / (float)radialSegments;
                    float a = u * Mathf.PI * 2f;
                    float lobe = 1f + 0.10f * Mathf.Sin(a * 5f + v * 9f) + 0.055f * Mathf.Sin(a * 11f - v * 5f);
                    vertices.Add(new Vector3(Mathf.Cos(a) * rr * lobe, sy, Mathf.Sin(a) * rr * lobe));
                    uv.Add(new Vector2(u, v));
                }
            }
            int stride = radialSegments + 1;
            for (int y = 0; y < verticalSegments; y++)
            for (int x = 0; x < radialSegments; x++)
            {
                int a = y * stride + x;
                int b = a + 1;
                int c = a + stride + 1;
                int d = a + stride;
                AddQuad(triangles, a, d, c, b);
            }
            return Finish(vertices, uv, triangles);
        }

        private static Vector3 Quadratic(Vector3 a, Vector3 b, Vector3 c, float t)
        {
            float u = 1f - t;
            return u * u * a + 2f * u * t * b + t * t * c;
        }

        private static void AddQuad(List<int> triangles, int a, int b, int c, int d)
        {
            triangles.Add(a); triangles.Add(b); triangles.Add(c);
            triangles.Add(a); triangles.Add(c); triangles.Add(d);
        }

        private static Mesh Finish(List<Vector3> vertices, List<Vector2> uv, List<int> triangles)
        {
            Mesh mesh = new Mesh();
            if (vertices.Count > 65535) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
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
