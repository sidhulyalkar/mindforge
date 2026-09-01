#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Mindforge.Editor
{
    /// <summary>
    /// Deterministic production-geometry recipes for V0.26.
    ///
    /// These meshes replace the most visible built-in cube silhouettes without moving the
    /// proven V0.23/V0.24 collision authority. The repository stores recipes; generated mesh
    /// assets remain under the ignored Generated/V26 tree.
    /// </summary>
    public static class ProductionGeometryV26
    {
        public const string Root = "Assets/Mindforge/Generated/V26/Meshes";
        public const int MeshRevision = 1;
        public const string ChamferedBlockPath = Root + "/ChamferedBlock_R1.asset";
        public const string TaperedButtressPath = Root + "/TaperedButtress_R1.asset";
        public const string VaultWebPath = Root + "/VaultWeb_R1.asset";

        public static Mesh ChamferedBlock()
            => Ensure(ChamferedBlockPath, () => BuildChamferedBlock(0.065f));

        public static Mesh TaperedButtress()
            => Ensure(TaperedButtressPath, BuildTaperedButtress);

        public static Mesh VaultWeb()
            => Ensure(VaultWebPath, () => BuildVaultWeb(24, 8));

        public static Mesh BuildTransientChamferedBlock(float bevel = 0.065f)
            => BuildChamferedBlock(bevel);

        public static Mesh BuildTransientVaultWeb(int xSegments = 24, int zSegments = 8)
            => BuildVaultWeb(xSegments, zSegments);

        public static Mesh BuildTransientTaperedButtress()
            => BuildTaperedButtress();

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

        private static Mesh BuildChamferedBlock(float bevel)
        {
            float h = 0.5f;
            float b = Mathf.Clamp(bevel, 0.015f, 0.22f);
            float i = h - b;
            List<Vector3> vertices = new List<Vector3>(120);
            List<Vector2> uv = new List<Vector2>(120);
            List<int> triangles = new List<int>(180);

            // Six inset primary faces.
            AddQuad(vertices, uv, triangles,
                new Vector3(-i, -i, -h), new Vector3(i, -i, -h),
                new Vector3(i, i, -h), new Vector3(-i, i, -h));
            AddQuad(vertices, uv, triangles,
                new Vector3(-i, -i, h), new Vector3(-i, i, h),
                new Vector3(i, i, h), new Vector3(i, -i, h));
            AddQuad(vertices, uv, triangles,
                new Vector3(-h, -i, -i), new Vector3(-h, i, -i),
                new Vector3(-h, i, i), new Vector3(-h, -i, i));
            AddQuad(vertices, uv, triangles,
                new Vector3(h, -i, -i), new Vector3(h, -i, i),
                new Vector3(h, i, i), new Vector3(h, i, -i));
            AddQuad(vertices, uv, triangles,
                new Vector3(-i, -h, -i), new Vector3(-i, -h, i),
                new Vector3(i, -h, i), new Vector3(i, -h, -i));
            AddQuad(vertices, uv, triangles,
                new Vector3(-i, h, -i), new Vector3(i, h, -i),
                new Vector3(i, h, i), new Vector3(-i, h, i));

            // Twelve bevel strips. Duplicated vertices deliberately preserve crisp face normals.
            int[] signs = { -1, 1 };
            for (int xi = 0; xi < signs.Length; xi++)
            for (int zi = 0; zi < signs.Length; zi++)
            {
                float sx = signs[xi];
                float sz = signs[zi];
                AddQuad(vertices, uv, triangles,
                    new Vector3(sx * i, -i, sz * h), new Vector3(sx * i, i, sz * h),
                    new Vector3(sx * h, i, sz * i), new Vector3(sx * h, -i, sz * i));
            }

            for (int yi = 0; yi < signs.Length; yi++)
            for (int zi = 0; zi < signs.Length; zi++)
            {
                float sy = signs[yi];
                float sz = signs[zi];
                AddQuad(vertices, uv, triangles,
                    new Vector3(-i, sy * i, sz * h), new Vector3(i, sy * i, sz * h),
                    new Vector3(i, sy * h, sz * i), new Vector3(-i, sy * h, sz * i));
            }

            for (int xi = 0; xi < signs.Length; xi++)
            for (int yi = 0; yi < signs.Length; yi++)
            {
                float sx = signs[xi];
                float sy = signs[yi];
                AddQuad(vertices, uv, triangles,
                    new Vector3(sx * h, sy * i, -i), new Vector3(sx * h, sy * i, i),
                    new Vector3(sx * i, sy * h, i), new Vector3(sx * i, sy * h, -i));
            }

            // Eight clipped corners complete the bevel shell.
            for (int xi = 0; xi < signs.Length; xi++)
            for (int yi = 0; yi < signs.Length; yi++)
            for (int zi = 0; zi < signs.Length; zi++)
            {
                float sx = signs[xi];
                float sy = signs[yi];
                float sz = signs[zi];
                AddTriangle(vertices, uv, triangles,
                    new Vector3(sx * i, sy * i, sz * h),
                    new Vector3(sx * i, sy * h, sz * i),
                    new Vector3(sx * h, sy * i, sz * i));
            }

            return Finish("V26_ChamferedBlock", vertices, uv, triangles);
        }

        private static Mesh BuildTaperedButtress()
        {
            List<Vector3> vertices = new List<Vector3>(32);
            List<Vector2> uv = new List<Vector2>(32);
            List<int> triangles = new List<int>(48);

            float y0 = -0.5f;
            float y1 = 0.5f;
            float bx = 0.5f;
            float bz = 0.5f;
            float tx = 0.28f;
            float tz = 0.23f;

            Vector3 b0 = new Vector3(-bx, y0, -bz);
            Vector3 b1 = new Vector3(bx, y0, -bz);
            Vector3 b2 = new Vector3(bx, y0, bz);
            Vector3 b3 = new Vector3(-bx, y0, bz);
            Vector3 t0 = new Vector3(-tx, y1, -tz);
            Vector3 t1 = new Vector3(tx, y1, -tz);
            Vector3 t2 = new Vector3(tx, y1, tz);
            Vector3 t3 = new Vector3(-tx, y1, tz);

            AddQuad(vertices, uv, triangles, b0, b1, t1, t0);
            AddQuad(vertices, uv, triangles, b1, b2, t2, t1);
            AddQuad(vertices, uv, triangles, b2, b3, t3, t2);
            AddQuad(vertices, uv, triangles, b3, b0, t0, t3);
            AddQuad(vertices, uv, triangles, b0, b3, b2, b1);
            AddQuad(vertices, uv, triangles, t0, t1, t2, t3);

            return Finish("V26_TaperedButtress", vertices, uv, triangles);
        }

        private static Mesh BuildVaultWeb(int xSegments, int zSegments)
        {
            xSegments = Mathf.Max(8, xSegments);
            zSegments = Mathf.Max(1, zSegments);
            List<Vector3> vertices = new List<Vector3>((xSegments + 1) * (zSegments + 1));
            List<Vector2> uv = new List<Vector2>(vertices.Capacity);
            List<int> triangles = new List<int>(xSegments * zSegments * 6);

            for (int z = 0; z <= zSegments; z++)
            {
                float vz = z / (float)zSegments;
                float pz = vz - 0.5f;
                for (int x = 0; x <= xSegments; x++)
                {
                    float ux = x / (float)xSegments;
                    float px = ux - 0.5f;
                    float lateral = Mathf.Clamp01(Mathf.Abs(px) * 2f);
                    // Exponent < 1 keeps a subtle Gothic point at the crown rather than a barrel arch.
                    float py = 1f - Mathf.Pow(lateral, 0.72f);
                    vertices.Add(new Vector3(px, py, pz));
                    uv.Add(new Vector2(ux, vz));
                }
            }

            int stride = xSegments + 1;
            for (int z = 0; z < zSegments; z++)
            for (int x = 0; x < xSegments; x++)
            {
                int a = z * stride + x;
                int b = a + 1;
                int c = a + stride + 1;
                int d = a + stride;
                // Inward/downward-facing winding: the gameplay camera lives below this shell.
                triangles.Add(a); triangles.Add(c); triangles.Add(d);
                triangles.Add(a); triangles.Add(b); triangles.Add(c);
            }

            return Finish("V26_VaultWeb", vertices, uv, triangles);
        }

        private static void AddQuad(
            List<Vector3> vertices,
            List<Vector2> uv,
            List<int> triangles,
            Vector3 a,
            Vector3 b,
            Vector3 c,
            Vector3 d)
        {
            int start = vertices.Count;
            vertices.Add(a); vertices.Add(b); vertices.Add(c); vertices.Add(d);
            uv.Add(new Vector2(0f, 0f));
            uv.Add(new Vector2(1f, 0f));
            uv.Add(new Vector2(1f, 1f));
            uv.Add(new Vector2(0f, 1f));

            Vector3 normal = Vector3.Cross(b - a, c - a);
            Vector3 centre = (a + b + c + d) * 0.25f;
            bool outward = Vector3.Dot(normal, centre) >= 0f;
            if (outward)
            {
                triangles.Add(start); triangles.Add(start + 1); triangles.Add(start + 2);
                triangles.Add(start); triangles.Add(start + 2); triangles.Add(start + 3);
            }
            else
            {
                triangles.Add(start); triangles.Add(start + 2); triangles.Add(start + 1);
                triangles.Add(start); triangles.Add(start + 3); triangles.Add(start + 2);
            }
        }

        private static void AddTriangle(
            List<Vector3> vertices,
            List<Vector2> uv,
            List<int> triangles,
            Vector3 a,
            Vector3 b,
            Vector3 c)
        {
            int start = vertices.Count;
            vertices.Add(a); vertices.Add(b); vertices.Add(c);
            uv.Add(new Vector2(0f, 0f));
            uv.Add(new Vector2(0.5f, 1f));
            uv.Add(new Vector2(1f, 0f));
            Vector3 normal = Vector3.Cross(b - a, c - a);
            Vector3 centre = (a + b + c) / 3f;
            bool outward = Vector3.Dot(normal, centre) >= 0f;
            triangles.Add(start);
            triangles.Add(outward ? start + 1 : start + 2);
            triangles.Add(outward ? start + 2 : start + 1);
        }

        private static Mesh Finish(string name, List<Vector3> vertices, List<Vector2> uv, List<int> triangles)
        {
            Mesh mesh = new Mesh { name = name };
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
