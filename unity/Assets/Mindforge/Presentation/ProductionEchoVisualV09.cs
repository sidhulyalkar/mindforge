using System.Collections.Generic;
using UnityEngine;
using Mindforge.Combat;

namespace Mindforge.Presentation
{
    /// <summary>
    /// Collider-free visual replacement for a FracturedEchoNode. The Echo keeps all fixed-tick
    /// orbit/fire/life authority. This component hides the legacy renderer set, creates a
    /// crystalline reliquary shell, and mirrors only the existing Shattered/Reconstructed
    /// events so checkpoint restoration remains visually correct.
    /// </summary>
    public sealed class ProductionEchoVisualV09 : MonoBehaviour
    {
        [SerializeField] private FracturedEchoNode echo;
        [SerializeField] private Material shell;
        [SerializeField] private Material hostile;
        [SerializeField] private Material trim;

        private Transform _visualRoot;
        private bool _built;

        public void ConfigureRuntime(Material shellMaterial, Material hostileMaterial, Material trimMaterial)
        {
            shell = shellMaterial;
            hostile = hostileMaterial;
            trim = trimMaterial;
            if (echo == null) echo = GetComponent<FracturedEchoNode>();
        }

        private void OnEnable()
        {
            if (echo == null) echo = GetComponent<FracturedEchoNode>();
            if (echo != null)
            {
                echo.Shattered -= OnShattered;
                echo.Reconstructed -= OnReconstructed;
                echo.Shattered += OnShattered;
                echo.Reconstructed += OnReconstructed;
            }
        }

        private void Start() => TryBuild();

        private void Update()
        {
            if (!_built) TryBuild();
            if (_visualRoot != null)
            {
                // Presentation-only counter-rotation gives the reliquary a layered mechanical
                // read while the authoritative Echo transform continues its existing spin.
                _visualRoot.localRotation *= Quaternion.Euler(0f, -22f * Time.deltaTime, 15f * Time.deltaTime);
            }
        }

        private void OnDisable()
        {
            if (echo != null)
            {
                echo.Shattered -= OnShattered;
                echo.Reconstructed -= OnReconstructed;
            }
        }

        private void TryBuild()
        {
            if (_built || echo == null || shell == null || hostile == null || trim == null) return;
            Transform existing = transform.Find("ProductionEchoVisualV09");
            if (existing != null)
            {
                _visualRoot = existing;
                _built = true;
                return;
            }

            Renderer[] legacy = GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < legacy.Length; i++)
                if (legacy[i] != null) legacy[i].enabled = false;

            _visualRoot = new GameObject("ProductionEchoVisualV09").transform;
            _visualRoot.SetParent(transform, false);

            Mesh coreMesh = BuildFacetedGem(8, 0.34f, 0.55f);
            Mesh shardMesh = BuildFacetedGem(6, 0.12f, 0.62f);
            Mesh ringMesh = BuildTorus(30, 8, 0.53f, 0.025f);

            MeshObject("EchoCoreShell", _visualRoot, coreMesh, Vector3.zero, new Vector3(1f, 1f, 0.78f), shell);
            MeshObject("EchoCoreSignal", _visualRoot, coreMesh, new Vector3(0f, 0f, 0.04f), Vector3.one * 0.54f, hostile);
            MeshObject("EchoOuterRing", _visualRoot, ringMesh, Vector3.zero, Vector3.one, trim, new Vector3(68f, 17f, 0f));
            MeshObject("EchoInnerRing", _visualRoot, ringMesh, Vector3.zero, Vector3.one * 0.72f, hostile, new Vector3(102f, -24f, 0f));

            for (int i = 0; i < 5; i++)
            {
                float a = i / 5f * Mathf.PI * 2f;
                Vector3 p = new Vector3(Mathf.Cos(a) * 0.58f, Mathf.Sin(a * 2f) * 0.11f, Mathf.Sin(a) * 0.42f);
                Vector3 euler = new Vector3(26f + i * 9f, i * 72f, 18f - i * 6f);
                MeshObject($"EchoShard_{i:00}", _visualRoot, shardMesh, p,
                    new Vector3(0.75f, 1.05f + (i % 2) * 0.25f, 0.58f), i == 0 ? hostile : shell, euler);
            }

            _built = true;
        }

        private void OnShattered()
        {
            if (_visualRoot != null) _visualRoot.gameObject.SetActive(false);
        }

        private void OnReconstructed()
        {
            if (_visualRoot != null) _visualRoot.gameObject.SetActive(true);
        }

        private static void MeshObject(string name, Transform parent, Mesh mesh, Vector3 position, Vector3 scale, Material material, Vector3? euler = null)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            if (euler.HasValue) go.transform.localRotation = Quaternion.Euler(euler.Value);
            MeshFilter filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            MeshRenderer renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            renderer.receiveShadows = true;
        }

        private static Mesh BuildFacetedGem(int sides, float radius, float halfHeight)
        {
            List<Vector3> vertices = new List<Vector3>();
            List<Vector2> uv = new List<Vector2>();
            List<int> triangles = new List<int>();
            int top = vertices.Count;
            vertices.Add(new Vector3(0f, halfHeight, 0f));
            uv.Add(new Vector2(0.5f, 1f));
            int upper = vertices.Count;
            for (int i = 0; i < sides; i++)
            {
                float a = i / (float)sides * Mathf.PI * 2f;
                vertices.Add(new Vector3(Mathf.Cos(a) * radius, halfHeight * 0.18f, Mathf.Sin(a) * radius));
                uv.Add(new Vector2(i / (float)sides, 0.66f));
            }
            int lower = vertices.Count;
            for (int i = 0; i < sides; i++)
            {
                float a = i / (float)sides * Mathf.PI * 2f;
                vertices.Add(new Vector3(Mathf.Cos(a) * radius * 0.86f, -halfHeight * 0.28f, Mathf.Sin(a) * radius * 0.86f));
                uv.Add(new Vector2(i / (float)sides, 0.32f));
            }
            int bottom = vertices.Count;
            vertices.Add(new Vector3(0f, -halfHeight, 0f));
            uv.Add(new Vector2(0.5f, 0f));

            for (int i = 0; i < sides; i++)
            {
                int next = (i + 1) % sides;
                triangles.Add(top); triangles.Add(upper + next); triangles.Add(upper + i);
                triangles.Add(upper + i); triangles.Add(upper + next); triangles.Add(lower + next);
                triangles.Add(upper + i); triangles.Add(lower + next); triangles.Add(lower + i);
                triangles.Add(bottom); triangles.Add(lower + i); triangles.Add(lower + next);
            }
            return Finish(vertices, uv, triangles);
        }

        private static Mesh BuildTorus(int majorSegments, int minorSegments, float radius, float tube)
        {
            List<Vector3> vertices = new List<Vector3>();
            List<Vector2> uv = new List<Vector2>();
            List<int> triangles = new List<int>();
            for (int i = 0; i <= majorSegments; i++)
            {
                float u = i / (float)majorSegments;
                float a = u * Mathf.PI * 2f;
                for (int j = 0; j <= minorSegments; j++)
                {
                    float v = j / (float)minorSegments;
                    float b = v * Mathf.PI * 2f;
                    float r = radius + Mathf.Cos(b) * tube;
                    vertices.Add(new Vector3(Mathf.Cos(a) * r, Mathf.Sin(b) * tube, Mathf.Sin(a) * r));
                    uv.Add(new Vector2(u, v));
                }
            }
            int stride = minorSegments + 1;
            for (int i = 0; i < majorSegments; i++)
            for (int j = 0; j < minorSegments; j++)
            {
                int a = i * stride + j;
                int b = a + stride;
                int c = b + 1;
                int d = a + 1;
                triangles.Add(a); triangles.Add(b); triangles.Add(c);
                triangles.Add(a); triangles.Add(c); triangles.Add(d);
            }
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
    }
}
