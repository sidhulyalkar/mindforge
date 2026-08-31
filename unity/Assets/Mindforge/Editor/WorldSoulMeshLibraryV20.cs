#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mindforge.Editor
{
    /// <summary>
    /// Editor-generated organic meshes for V0.20.
    ///
    /// This follows the same recipe-over-binary philosophy already used by
    /// ProductionMeshLibraryV09 and the MIT aadebdeb/ProceduralMesh reference:
    /// source control stores deterministic construction code; local Unity builds generate
    /// reusable mesh assets beneath Assets/Mindforge/Generated.
    /// </summary>
    public static class WorldSoulMeshLibraryV20
    {
        public const string Root = "Assets/Mindforge/Generated/V20/Meshes";

        public static Mesh TerrainPatch(
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
            Mesh fresh = BuildTerrainPatch(
                xMin, xMax, zMin, zMax,
                Mathf.Clamp(xSegments, 2, 96),
                Mathf.Clamp(zSegments, 2, 160),
                heightSampler);
            return Upsert($"{Root}/{assetName}.asset", fresh);
        }

        public static Mesh RockVariant(int variant)
        {
            variant = Mathf.Abs(variant) % 6;
            EnsureFolder(Root);
            Mesh fresh = BuildRock(22000 + variant * 101, 11, 7);
            return Upsert($"{Root}/Rock_{variant:00}.asset", fresh);
        }

        private static Mesh BuildTerrainPatch(
            float xMin,
            float xMax,
            float zMin,
            float zMax,
            int xSegments,
            int zSegments,
            Func<float, float, float> heightSampler)
        {
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

            for (int z = 0; z < zSegments; z++)
            {
                for (int x = 0; x < xSegments; x++)
                {
                    int a = z * stride + x;
                    int b = a + 1;
                    int d = a + stride;
                    int c = d + 1;
                    triangles.Add(a); triangles.Add(d); triangles.Add(c);
                    triangles.Add(a); triangles.Add(c); triangles.Add(b);
                }
            }

            Mesh mesh = new Mesh { name = "WorldSoulTerrain" };
            if (vertices.Count > 65535) mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(triangles, 0, true);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh BuildRock(int seed, int radialSegments, int verticalSegments)
        {
            radialSegments = Mathf.Max(7, radialSegments);
            verticalSegments = Mathf.Max(4, verticalSegments);
            int stride = radialSegments + 1;
            List<Vector3> vertices = new List<Vector3>((radialSegments + 1) * (verticalSegments + 1));
            List<Vector2> uv = new List<Vector2>(vertices.Capacity);
            List<int> triangles = new List<int>(radialSegments * verticalSegments * 6);

            for (int y = 0; y <= verticalSegments; y++)
            {
                float v = y / (float)verticalSegments;
                float phi = v * Mathf.PI;
                float sy = Mathf.Cos(phi);
                float radial = Mathf.Sin(phi);

                for (int x = 0; x <= radialSegments; x++)
                {
                    float u = x / (float)radialSegments;
                    float theta = u * Mathf.PI * 2f;
                    Vector3 direction = new Vector3(Mathf.Cos(theta) * radial, sy, Mathf.Sin(theta) * radial);
                    float n = WorldSoulNoiseV20.Detail(direction.x * 5f + seed * 0.001f,
                        direction.z * 5f - direction.y * 2.2f, seed);
                    float band = WorldSoulNoiseV20.SignedHash(seed, y * 31 + x * 7) * 0.045f;
                    float radius = 0.84f + n * 0.22f + band;
                    Vector3 p = direction * radius;
                    p.y *= 0.82f;
                    p.x *= 1.02f + WorldSoulNoiseV20.SignedHash(seed ^ 0x4141, y) * 0.09f;
                    p.z *= 0.96f + WorldSoulNoiseV20.SignedHash(seed ^ 0x9191, x) * 0.08f;
                    vertices.Add(p);
                    uv.Add(new Vector2(u * 2f, v * 1.35f));
                }
            }

            for (int y = 0; y < verticalSegments; y++)
            {
                for (int x = 0; x < radialSegments; x++)
                {
                    int a = y * stride + x;
                    int b = a + 1;
                    int d = a + stride;
                    int c = d + 1;
                    triangles.Add(a); triangles.Add(d); triangles.Add(c);
                    triangles.Add(a); triangles.Add(c); triangles.Add(b);
                }
            }

            Mesh mesh = new Mesh { name = $"WorldSoulRock_{seed}" };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(triangles, 0, true);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh Upsert(string path, Mesh fresh)
        {
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null)
            {
                fresh.name = System.IO.Path.GetFileNameWithoutExtension(path);
                AssetDatabase.CreateAsset(fresh, path);
                EditorUtility.SetDirty(fresh);
                return fresh;
            }

            string stableName = existing.name;
            EditorUtility.CopySerialized(fresh, existing);
            existing.name = stableName;
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
