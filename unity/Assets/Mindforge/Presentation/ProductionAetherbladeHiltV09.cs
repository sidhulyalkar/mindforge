using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mindforge.Presentation
{
    /// <summary>
    /// Runtime production skin for the physical Aetherblade hilt. The existing arsenal rig still
    /// owns attack reach/contact/parry/energy scaling. This component hides only the primitive
    /// hilt renderers and parents a richer mesh shell to the same SwordRoot.
    /// </summary>
    public sealed class ProductionAetherbladeHiltV09 : MonoBehaviour
    {
        private const string RootName = "ProductionAetherbladeHiltV09";
        private bool _built;
        private bool _polishVentsHidden;

        private static Mesh _hiltMesh;
        private static Mesh _gripMesh;
        private static Mesh _crossguardMesh;
        private static Mesh _pommelMesh;

        public bool Built => _built;

        private void LateUpdate()
        {
            if (!_built) _built = TryBuild();
            if (_built && !_polishVentsHidden) _polishVentsHidden = HidePolishVents();
        }

        private bool TryBuild()
        {
            Transform arsenal = transform.Find("PhysicalArsenalRig");
            Transform sword = arsenal != null ? arsenal.Find("SwordRoot") : null;
            if (sword == null) return false;
            if (sword.Find(RootName) != null) return true;

            Renderer emitter = FindRenderer(sword, "AetherbladeEmitter");
            Renderer crossguard = FindRenderer(sword, "AetherbladeCrossguard");
            Renderer grip = FindRenderer(sword, "AetherbladeGrip");
            Renderer pommel = FindRenderer(sword, "AetherbladePommel");
            if (emitter == null || crossguard == null || grip == null || pommel == null) return false;

            Material hiltMaterial = emitter.sharedMaterial != null ? emitter.sharedMaterial : crossguard.sharedMaterial;
            Material gripMaterial = grip.sharedMaterial;
            Material pommelMaterial = pommel.sharedMaterial;
            if (hiltMaterial == null || gripMaterial == null || pommelMaterial == null) return false;

            EnsureMeshes();

            // Hide only the four visual primitive renderers after every replacement mesh has
            // passed validation. The underlying Transform hierarchy and all combat authority
            // objects stay exactly where PhysicalArsenalBootstrap authored them.
            emitter.enabled = false;
            crossguard.enabled = false;
            grip.enabled = false;
            pommel.enabled = false;

            GameObject rootGo = new GameObject(RootName);
            rootGo.transform.SetParent(sword, false);
            Transform root = rootGo.transform;

            Part("MachinedHiltBody", root, _hiltMesh, Vector3.zero, Vector3.one, hiltMaterial);
            Part("GripSleeve", root, _gripMesh, Vector3.zero, Vector3.one, gripMaterial);
            Part("TaperedCrossguard", root, _crossguardMesh, new Vector3(0f, 0f, 0.045f), Vector3.one, hiltMaterial);
            Part("FacetedPommel", root, _pommelMesh, new Vector3(0f, 0f, -0.53f), Vector3.one * 0.145f, pommelMaterial);

            // Two small structural collars add silhouette breaks without adding lights/VFX.
            Part("EmitterCollar", root, _gripMesh, new Vector3(0f, 0f, 0.16f), new Vector3(1.55f, 1.55f, 0.28f), hiltMaterial);
            Part("PommelCollar", root, _gripMesh, new Vector3(0f, 0f, -0.47f), new Vector3(1.20f, 1.20f, 0.22f), hiltMaterial);

            if (rootGo.GetComponentInChildren<Collider>(true) != null || rootGo.GetComponentInChildren<Rigidbody>(true) != null)
                throw new InvalidOperationException("Production Aetherblade hilt acquired physics authority.");
            return true;
        }

        private bool HidePolishVents()
        {
            Transform arsenal = transform.Find("PhysicalArsenalRig");
            Transform sword = arsenal != null ? arsenal.Find("SwordRoot") : null;
            Transform polish = sword != null ? sword.Find("AetherbladeVisualPolishV2") : null;
            if (polish == null) return false;

            int found = 0;
            Renderer[] renderers = polish.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || !renderer.name.StartsWith("AetherbladeEmitterVent_", StringComparison.Ordinal)) continue;
                renderer.enabled = false;
                found++;
            }
            return found >= 4;
        }

        private static Renderer FindRenderer(Transform root, string name)
        {
            Transform child = root != null ? root.Find(name) : null;
            return child != null ? child.GetComponent<Renderer>() : null;
        }

        private static void Part(string name, Transform parent, Mesh mesh, Vector3 position, Vector3 scale, Material material)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            MeshFilter filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            MeshRenderer renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
        }

        private static void EnsureMeshes()
        {
            if (_hiltMesh == null)
                _hiltMesh = BuildLathe("ProductionAetherbladeHiltBody", 28,
                    new[] { -0.50f, -0.42f, -0.30f, -0.12f, 0.02f, 0.12f, 0.22f, 0.30f },
                    new[] { 0.060f, 0.074f, 0.082f, 0.086f, 0.115f, 0.145f, 0.138f, 0.095f });
            if (_gripMesh == null)
                _gripMesh = BuildLathe("ProductionAetherbladeGripSleeve", 24,
                    new[] { -0.44f, -0.39f, -0.16f, -0.11f },
                    new[] { 0.074f, 0.081f, 0.081f, 0.074f });
            if (_crossguardMesh == null)
                _crossguardMesh = BuildExtrudedCrossguard();
            if (_pommelMesh == null)
                _pommelMesh = BuildFacetedPommel();
        }

        private static Mesh BuildLathe(string name, int segments, float[] z, float[] radius)
        {
            if (z == null || radius == null || z.Length < 2 || z.Length != radius.Length)
                throw new ArgumentException("Aetherblade lathe profile requires matched radius/z rings.");

            segments = Mathf.Max(12, segments);
            int rings = z.Length;
            List<Vector3> vertices = new List<Vector3>((segments + 1) * rings + 2);
            List<Vector2> uv = new List<Vector2>(vertices.Capacity);
            List<int> triangles = new List<int>(segments * (rings - 1) * 6 + segments * 6);

            for (int r = 0; r < rings; r++)
            {
                if (radius[r] <= 0f) throw new ArgumentException("Aetherblade lathe radius must stay positive.");
                for (int i = 0; i <= segments; i++)
                {
                    float u = i / (float)segments;
                    float a = u * Mathf.PI * 2f;
                    vertices.Add(new Vector3(Mathf.Cos(a) * radius[r], Mathf.Sin(a) * radius[r], z[r]));
                    uv.Add(new Vector2(u, r / (float)Mathf.Max(1, rings - 1)));
                }
            }

            int stride = segments + 1;
            for (int r = 0; r < rings - 1; r++)
            for (int i = 0; i < segments; i++)
            {
                int a = r * stride + i;
                int b = a + 1;
                int c = a + stride + 1;
                int d = a + stride;
                triangles.Add(a); triangles.Add(c); triangles.Add(d);
                triangles.Add(a); triangles.Add(b); triangles.Add(c);
            }

            int backCenter = vertices.Count;
            vertices.Add(new Vector3(0f, 0f, z[0]));
            uv.Add(new Vector2(0.5f, 0.5f));
            int frontCenter = vertices.Count;
            vertices.Add(new Vector3(0f, 0f, z[rings - 1]));
            uv.Add(new Vector2(0.5f, 0.5f));
            int last = (rings - 1) * stride;
            for (int i = 0; i < segments; i++)
            {
                triangles.Add(backCenter); triangles.Add(i + 1); triangles.Add(i);
                triangles.Add(frontCenter); triangles.Add(last + i); triangles.Add(last + i + 1);
            }
            return Finish(name, vertices, uv, triangles);
        }

        private static Mesh BuildExtrudedCrossguard()
        {
            Vector2[] shape =
            {
                new Vector2(-0.30f, 0f), new Vector2(-0.20f, 0.055f), new Vector2(0.20f, 0.055f),
                new Vector2(0.30f, 0f), new Vector2(0.20f, -0.055f), new Vector2(-0.20f, -0.055f),
            };
            const float depth = 0.085f;
            List<Vector3> vertices = new List<Vector3>(shape.Length * 2);
            List<Vector2> uv = new List<Vector2>(shape.Length * 2);
            List<int> triangles = new List<int>(72);
            for (int side = 0; side < 2; side++)
            {
                float z = side == 0 ? -depth * 0.5f : depth * 0.5f;
                for (int i = 0; i < shape.Length; i++)
                {
                    vertices.Add(new Vector3(shape[i].x, shape[i].y, z));
                    uv.Add(new Vector2(shape[i].x + 0.5f, shape[i].y + 0.5f));
                }
            }

            int n = shape.Length;
            for (int i = 1; i < n - 1; i++)
            {
                // Negative-Z face points backward; positive-Z face points forward.
                triangles.Add(0); triangles.Add(i); triangles.Add(i + 1);
                triangles.Add(n); triangles.Add(n + i + 1); triangles.Add(n + i);
            }
            for (int i = 0; i < n; i++)
            {
                int next = (i + 1) % n;
                triangles.Add(i); triangles.Add(next); triangles.Add(n + next);
                triangles.Add(i); triangles.Add(n + next); triangles.Add(n + i);
            }
            return Finish("ProductionAetherbladeCrossguard", vertices, uv, triangles);
        }

        private static Mesh BuildFacetedPommel()
        {
            float t = (1f + Mathf.Sqrt(5f)) * 0.5f;
            Vector3[] source =
            {
                new Vector3(-1,t,0), new Vector3(1,t,0), new Vector3(-1,-t,0), new Vector3(1,-t,0),
                new Vector3(0,-1,t), new Vector3(0,1,t), new Vector3(0,-1,-t), new Vector3(0,1,-t),
                new Vector3(t,0,-1), new Vector3(t,0,1), new Vector3(-t,0,-1), new Vector3(-t,0,1),
            };
            int[,] f =
            {
                {0,11,5},{0,5,1},{0,1,7},{0,7,10},{0,10,11}, {1,5,9},{5,11,4},{11,10,2},{10,7,6},{7,1,8},
                {3,9,4},{3,4,2},{3,2,6},{3,6,8},{3,8,9}, {4,9,5},{2,4,11},{6,2,10},{8,6,7},{9,8,1},
            };
            List<Vector3> vertices = new List<Vector3>(60);
            List<Vector2> uv = new List<Vector2>(60);
            List<int> triangles = new List<int>(60);
            for (int face = 0; face < f.GetLength(0); face++)
            {
                Vector3 a = source[f[face,0]].normalized;
                Vector3 b = source[f[face,1]].normalized;
                Vector3 c = source[f[face,2]].normalized;
                if (Vector3.Dot(Vector3.Cross(b - a, c - a), (a + b + c) / 3f) < 0f)
                {
                    Vector3 tmp = b; b = c; c = tmp;
                }
                int s = vertices.Count;
                vertices.Add(a); vertices.Add(b); vertices.Add(c);
                uv.Add(Vector2.zero); uv.Add(Vector2.right); uv.Add(Vector2.up);
                triangles.Add(s); triangles.Add(s+1); triangles.Add(s+2);
            }
            return Finish("ProductionAetherbladePommel", vertices, uv, triangles);
        }

        private static Mesh Finish(string name, List<Vector3> vertices, List<Vector2> uv, List<int> triangles)
        {
            if (vertices == null || triangles == null || vertices.Count < 3 || triangles.Count < 3 || triangles.Count % 3 != 0)
                throw new InvalidOperationException("Invalid production Aetherblade mesh recipe: " + name);

            Mesh mesh = new Mesh { name = name };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(triangles, 0, true);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            ValidateNormals(mesh);
            return mesh;
        }

        private static void ValidateNormals(Mesh mesh)
        {
            Vector3[] normals = mesh.normals;
            if (normals == null || normals.Length != mesh.vertexCount)
                throw new InvalidOperationException("Production Aetherblade mesh has missing normals: " + mesh.name);
            for (int i = 0; i < normals.Length; i++)
            {
                Vector3 n = normals[i];
                if (float.IsNaN(n.x) || float.IsNaN(n.y) || float.IsNaN(n.z) ||
                    float.IsInfinity(n.x) || float.IsInfinity(n.y) || float.IsInfinity(n.z) ||
                    n.sqrMagnitude < 0.20f)
                    throw new InvalidOperationException("Production Aetherblade mesh has invalid normal: " + mesh.name + " @ " + i);
            }
        }
    }
}
