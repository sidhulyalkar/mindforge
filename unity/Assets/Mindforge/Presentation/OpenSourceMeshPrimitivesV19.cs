using System.Collections.Generic;
using UnityEngine;

namespace Mindforge.Presentation
{
    /// <summary>
    /// Small original procedural-mesh toolkit used by the V19 boss presentation.
    ///
    /// The design is informed by the mesh-first workflow demonstrated by the MIT-licensed
    /// aadebdeb/ProceduralMesh project, but these mesh implementations are authored for
    /// Mindforge so no third-party runtime package or gameplay authority is imported.
    /// </summary>
    public static class OpenSourceMeshPrimitivesV19
    {
        public static Mesh CreateFacetedIcosahedron(float radius = 1f)
        {
            float t = (1f + Mathf.Sqrt(5f)) * 0.5f;
            Vector3[] source =
            {
                new Vector3(-1, t, 0), new Vector3(1, t, 0), new Vector3(-1, -t, 0), new Vector3(1, -t, 0),
                new Vector3(0, -1, t), new Vector3(0, 1, t), new Vector3(0, -1, -t), new Vector3(0, 1, -t),
                new Vector3(t, 0, -1), new Vector3(t, 0, 1), new Vector3(-t, 0, -1), new Vector3(-t, 0, 1),
            };
            for (int i = 0; i < source.Length; i++) source[i] = source[i].normalized * radius;

            int[] faces =
            {
                0,11,5, 0,5,1, 0,1,7, 0,7,10, 0,10,11,
                1,5,9, 5,11,4, 11,10,2, 10,7,6, 7,1,8,
                3,9,4, 3,4,2, 3,2,6, 3,6,8, 3,8,9,
                4,9,5, 2,4,11, 6,2,10, 8,6,7, 9,8,1,
            };

            return FlatMesh("MindforgeV19_FacetedIcosahedron", source, faces);
        }

        public static Mesh CreateTorus(float majorRadius, float minorRadius, int majorSegments = 28, int minorSegments = 7)
        {
            majorSegments = Mathf.Clamp(majorSegments, 8, 96);
            minorSegments = Mathf.Clamp(minorSegments, 3, 24);
            majorRadius = Mathf.Max(0.05f, majorRadius);
            minorRadius = Mathf.Clamp(minorRadius, 0.015f, majorRadius * 0.8f);

            var vertices = new List<Vector3>(majorSegments * minorSegments);
            var normals = new List<Vector3>(majorSegments * minorSegments);
            var uvs = new List<Vector2>(majorSegments * minorSegments);
            var triangles = new List<int>(majorSegments * minorSegments * 6);

            for (int major = 0; major < majorSegments; major++)
            {
                float u = major / (float)majorSegments;
                float a = u * Mathf.PI * 2f;
                Vector3 radial = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                Vector3 center = radial * majorRadius;
                for (int minor = 0; minor < minorSegments; minor++)
                {
                    float v = minor / (float)minorSegments;
                    float b = v * Mathf.PI * 2f;
                    Vector3 normal = radial * Mathf.Cos(b) + Vector3.up * Mathf.Sin(b);
                    vertices.Add(center + normal * minorRadius);
                    normals.Add(normal.normalized);
                    uvs.Add(new Vector2(u, v));
                }
            }

            for (int major = 0; major < majorSegments; major++)
            {
                int nextMajor = (major + 1) % majorSegments;
                for (int minor = 0; minor < minorSegments; minor++)
                {
                    int nextMinor = (minor + 1) % minorSegments;
                    int a = major * minorSegments + minor;
                    int b = nextMajor * minorSegments + minor;
                    int c = nextMajor * minorSegments + nextMinor;
                    int d = major * minorSegments + nextMinor;
                    triangles.Add(a); triangles.Add(b); triangles.Add(c);
                    triangles.Add(a); triangles.Add(c); triangles.Add(d);
                }
            }

            Mesh mesh = new Mesh { name = "MindforgeV19_Torus" };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0, true);
            mesh.RecalculateBounds();
            return mesh;
        }

        public static Mesh CreateShard(float width = 0.5f, float height = 1.4f, float depth = 0.26f, float skew = 0.14f)
        {
            width = Mathf.Max(0.04f, width);
            height = Mathf.Max(0.08f, height);
            depth = Mathf.Max(0.03f, depth);
            float hw = width * 0.5f;
            float hd = depth * 0.5f;

            Vector3[] source =
            {
                new Vector3(-hw, -height * 0.45f, -hd),
                new Vector3(hw, -height * 0.45f, -hd),
                new Vector3(hw * 0.72f, -height * 0.25f, hd),
                new Vector3(-hw * 0.72f, -height * 0.25f, hd),
                new Vector3(skew, height * 0.55f, 0f),
            };
            int[] faces =
            {
                0,1,2, 0,2,3,
                0,4,1,
                1,4,2,
                2,4,3,
                3,4,0,
            };
            return FlatMesh("MindforgeV19_FractureShard", source, faces);
        }

        private static Mesh FlatMesh(string name, Vector3[] source, int[] faces)
        {
            var vertices = new Vector3[faces.Length];
            var triangles = new int[faces.Length];
            var normals = new Vector3[faces.Length];
            for (int i = 0; i < faces.Length; i += 3)
            {
                Vector3 a = source[faces[i]];
                Vector3 b = source[faces[i + 1]];
                Vector3 c = source[faces[i + 2]];
                Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
                vertices[i] = a; vertices[i + 1] = b; vertices[i + 2] = c;
                normals[i] = normal; normals[i + 1] = normal; normals[i + 2] = normal;
                triangles[i] = i; triangles[i + 1] = i + 1; triangles[i + 2] = i + 2;
            }

            Mesh mesh = new Mesh { name = name };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.normals = normals;
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
