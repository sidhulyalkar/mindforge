#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Mindforge.Editor
{
    /// <summary>
    /// Production structural prism with actual bevel faces. This replaces the last high-usage
    /// stock Cube path in roads, rails, glass panels and architectural beams.
    /// </summary>
    public static class ProductionStructuralMeshV09
    {
        public const string Root = "Assets/Mindforge/Generated/ProductionV09/Meshes";
        public const string ChamferedPrismPath = Root + "/ChamferedStructuralPrism.asset";
        public const int RecipeVersion = 1;
        public const float Bevel = 0.055f;

        public static Mesh ChamferedPrism()
        {
            EnsureFolder(Root);
            string expected = "ChamferedStructuralPrism_r" + RecipeVersion;
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(ChamferedPrismPath);
            if (existing != null && string.Equals(existing.name, expected, StringComparison.Ordinal)) return existing;
            if (existing != null) AssetDatabase.DeleteAsset(ChamferedPrismPath);

            Mesh mesh = Build(Mathf.Clamp(Bevel, 0.005f, 0.20f));
            mesh.name = expected;
            Validate(mesh);
            AssetDatabase.CreateAsset(mesh, ChamferedPrismPath);
            EditorUtility.SetDirty(mesh);
            return mesh;
        }

        private static Mesh Build(float bevel)
        {
            float h = 0.5f;
            float q = h - bevel;
            List<Vector3> vertices = new List<Vector3>(120);
            List<Vector2> uv = new List<Vector2>(120);
            List<int> triangles = new List<int>(180);

            // Six inset main faces.
            Quad(vertices, uv, triangles, new Vector3(-q,-q,h), new Vector3(q,-q,h), new Vector3(q,q,h), new Vector3(-q,q,h));
            Quad(vertices, uv, triangles, new Vector3(q,-q,-h), new Vector3(-q,-q,-h), new Vector3(-q,q,-h), new Vector3(q,q,-h));
            Quad(vertices, uv, triangles, new Vector3(h,-q,q), new Vector3(h,-q,-q), new Vector3(h,q,-q), new Vector3(h,q,q));
            Quad(vertices, uv, triangles, new Vector3(-h,-q,-q), new Vector3(-h,-q,q), new Vector3(-h,q,q), new Vector3(-h,q,-q));
            Quad(vertices, uv, triangles, new Vector3(-q,h,q), new Vector3(q,h,q), new Vector3(q,h,-q), new Vector3(-q,h,-q));
            Quad(vertices, uv, triangles, new Vector3(-q,-h,-q), new Vector3(q,-h,-q), new Vector3(q,-h,q), new Vector3(-q,-h,q));

            // Twelve edge chamfers create the physical highlight/shadow break.
            Quad(vertices, uv, triangles, new Vector3(-q,q,h), new Vector3(q,q,h), new Vector3(q,h,q), new Vector3(-q,h,q));
            Quad(vertices, uv, triangles, new Vector3(-q,h,-q), new Vector3(q,h,-q), new Vector3(q,q,-h), new Vector3(-q,q,-h));
            Quad(vertices, uv, triangles, new Vector3(-q,-h,q), new Vector3(q,-h,q), new Vector3(q,-q,h), new Vector3(-q,-q,h));
            Quad(vertices, uv, triangles, new Vector3(-q,-q,-h), new Vector3(q,-q,-h), new Vector3(q,-h,-q), new Vector3(-q,-h,-q));
            Quad(vertices, uv, triangles, new Vector3(h,-q,q), new Vector3(h,q,q), new Vector3(q,q,h), new Vector3(q,-q,h));
            Quad(vertices, uv, triangles, new Vector3(-q,-q,h), new Vector3(-q,q,h), new Vector3(-h,q,q), new Vector3(-h,-q,q));
            Quad(vertices, uv, triangles, new Vector3(q,-q,-h), new Vector3(q,q,-h), new Vector3(h,q,-q), new Vector3(h,-q,-q));
            Quad(vertices, uv, triangles, new Vector3(-h,-q,-q), new Vector3(-h,q,-q), new Vector3(-q,q,-h), new Vector3(-q,-q,-h));
            Quad(vertices, uv, triangles, new Vector3(q,h,-q), new Vector3(q,h,q), new Vector3(h,q,q), new Vector3(h,q,-q));
            Quad(vertices, uv, triangles, new Vector3(-h,q,-q), new Vector3(-h,q,q), new Vector3(-q,h,q), new Vector3(-q,h,-q));
            Quad(vertices, uv, triangles, new Vector3(h,-q,-q), new Vector3(h,-q,q), new Vector3(q,-h,q), new Vector3(q,-h,-q));
            Quad(vertices, uv, triangles, new Vector3(-q,-h,-q), new Vector3(-q,-h,q), new Vector3(-h,-q,q), new Vector3(-h,-q,-q));

            // Eight planar corner facets complete the closed prism.
            for (int sx = -1; sx <= 1; sx += 2)
            for (int sy = -1; sy <= 1; sy += 2)
            for (int sz = -1; sz <= 1; sz += 2)
                Triangle(vertices, uv, triangles,
                    new Vector3(sx * h, sy * q, sz * q),
                    new Vector3(sx * q, sy * h, sz * q),
                    new Vector3(sx * q, sy * q, sz * h));

            Mesh mesh = new Mesh();
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(triangles, 0, true);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void Quad(List<Vector3> v, List<Vector2> uv, List<int> t, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            Vector3 center = (a + b + c + d) * 0.25f;
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), center) < 0f)
            {
                Vector3 tmp = b; b = d; d = tmp;
            }
            int s = v.Count;
            v.Add(a); v.Add(b); v.Add(c); v.Add(d);
            uv.Add(new Vector2(0,0)); uv.Add(new Vector2(1,0)); uv.Add(new Vector2(1,1)); uv.Add(new Vector2(0,1));
            t.Add(s); t.Add(s+1); t.Add(s+2); t.Add(s); t.Add(s+2); t.Add(s+3);
        }

        private static void Triangle(List<Vector3> v, List<Vector2> uv, List<int> t, Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 center = (a + b + c) / 3f;
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), center) < 0f)
            {
                Vector3 tmp = b; b = c; c = tmp;
            }
            int s = v.Count;
            v.Add(a); v.Add(b); v.Add(c);
            uv.Add(new Vector2(0,0)); uv.Add(new Vector2(1,0)); uv.Add(new Vector2(0.5f,1));
            t.Add(s); t.Add(s+1); t.Add(s+2);
        }

        private static void Validate(Mesh mesh)
        {
            if (mesh == null || mesh.vertexCount < 70 || mesh.triangles.Length < 100)
                throw new InvalidOperationException("Generated production structural prism is incomplete.");
            Vector3[] normals = mesh.normals;
            if (normals == null || normals.Length != mesh.vertexCount)
                throw new InvalidOperationException("Generated production structural prism has no valid normals.");
            for (int i = 0; i < normals.Length; i++)
            {
                Vector3 n = normals[i];
                if (float.IsNaN(n.x) || float.IsNaN(n.y) || float.IsNaN(n.z) || n.sqrMagnitude < 0.25f)
                    throw new InvalidOperationException("Generated production structural prism has collapsed normal @ " + i);
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
