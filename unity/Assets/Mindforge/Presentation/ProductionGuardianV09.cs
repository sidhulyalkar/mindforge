using System.Collections.Generic;
using UnityEngine;

namespace Mindforge.Presentation
{
    /// <summary>
    /// Production fallback shell for the Guardian. The existing GuardianAvatarPresentation
    /// remains the animation/pose owner; this component waits until that procedural rig exists,
    /// hides only its renderers, and attaches smooth meshes to the same animated transforms.
    /// This avoids touching Rigidbody, colliders, combat, input or locomotion authority while
    /// removing the obvious cube/sphere/capsule placeholder look from the play camera.
    /// </summary>
    public sealed class ProductionGuardianV09 : MonoBehaviour
    {
        [SerializeField] private Material armor;
        [SerializeField] private Material secondary;
        [SerializeField] private Material gold;
        [SerializeField] private Material aether;

        private bool _built;

        public void ConfigureRuntime(Material armorMaterial, Material secondaryMaterial, Material goldMaterial, Material aetherMaterial)
        {
            armor = armorMaterial;
            secondary = secondaryMaterial;
            gold = goldMaterial;
            aether = aetherMaterial;
        }

        private void Start() => TryBuild();

        private void Update()
        {
            if (!_built) TryBuild();
        }

        private void TryBuild()
        {
            Transform avatar = transform.Find("GuardianShowcaseAvatar");
            if (avatar == null || avatar.Find("ProductionGuardianV09") != null) return;
            if (armor == null || secondary == null || gold == null || aether == null) return;

            Renderer[] legacy = avatar.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < legacy.Length; i++)
                if (legacy[i] != null) legacy[i].enabled = false;

            Transform production = Node("ProductionGuardianV09", avatar, Vector3.zero);
            BuildTorso(avatar, production);
            BuildHead(avatar, production);
            BuildArms(avatar, production);
            BuildLegs(avatar, production);
            BuildMantle(avatar, production);
            _built = true;
        }

        private void BuildTorso(Transform avatar, Transform production)
        {
            Transform torso = avatar.Find("Torso");
            Transform pelvis = avatar.Find("Pelvis");
            if (torso != null)
            {
                MeshObject("ChestShell", torso, BuildSuperEllipsoid(28, 12, 0.39f, 0.36f, 0.24f, 1.55f),
                    new Vector3(0f, 0.01f, 0f), Vector3.one, armor);
                MeshObject("ChestPlate", torso, BuildTaperedPrism(0.31f, 0.23f, 0.28f, 0.055f, 0.09f),
                    new Vector3(0f, 0.06f, 0.235f), Vector3.one, secondary);
                MeshObject("SternumGold", torso, BuildTaperedPrism(0.09f, 0.055f, 0.34f, 0.025f, 0.018f),
                    new Vector3(0f, 0.02f, 0.278f), Vector3.one, gold);
                MeshObject("AetherCore", torso, BuildOctahedron(0.095f, 0.13f),
                    new Vector3(0f, 0.075f, 0.31f), Vector3.one, aether);
            }
            if (pelvis != null)
            {
                MeshObject("PelvisShell", pelvis, BuildSuperEllipsoid(24, 9, 0.33f, 0.19f, 0.23f, 1.75f),
                    Vector3.zero, Vector3.one, secondary);
                MeshObject("HipGoldL", pelvis, BuildTaperedPrism(0.075f, 0.055f, 0.22f, 0.04f, 0.035f),
                    new Vector3(-0.24f, 0f, 0.02f), Vector3.one, gold, new Vector3(0f, 0f, 18f));
                MeshObject("HipGoldR", pelvis, BuildTaperedPrism(0.075f, 0.055f, 0.22f, 0.04f, 0.035f),
                    new Vector3(0.24f, 0f, 0.02f), Vector3.one, gold, new Vector3(0f, 0f, -18f));
            }
        }

        private void BuildHead(Transform avatar, Transform production)
        {
            Transform head = avatar.Find("Head");
            if (head == null) return;
            MeshObject("Helmet", head, BuildSuperEllipsoid(28, 14, 0.225f, 0.25f, 0.215f, 1.9f),
                Vector3.zero, Vector3.one, armor);
            MeshObject("FacePlate", head, BuildTaperedPrism(0.18f, 0.145f, 0.18f, 0.045f, 0.035f),
                new Vector3(0f, -0.015f, 0.208f), Vector3.one, secondary, new Vector3(0f, 0f, 90f));
            MeshObject("Visor", head, BuildTaperedPrism(0.16f, 0.13f, 0.045f, 0.018f, 0.022f),
                new Vector3(0f, 0.025f, 0.247f), Vector3.one, aether, new Vector3(0f, 0f, 90f));
            MeshObject("Crown", head, BuildTaperedPrism(0.075f, 0.018f, 0.30f, 0.045f, 0.055f),
                new Vector3(0f, 0.22f, -0.01f), Vector3.one, gold);
        }

        private void BuildArms(Transform avatar, Transform production)
        {
            BuildArm(avatar.Find("LeftArm"), -1f);
            BuildArm(avatar.Find("RightArm"), 1f);
        }

        private void BuildArm(Transform arm, float side)
        {
            if (arm == null) return;
            MeshObject("UpperArm", arm, BuildTaperedCylinder(18, 0.125f, 0.105f, 0.48f),
                new Vector3(0f, -0.18f, 0f), Vector3.one, secondary);
            MeshObject("Pauldron", arm, BuildSuperEllipsoid(20, 8, 0.19f, 0.13f, 0.21f, 1.7f),
                new Vector3(side * 0.015f, 0.02f, -0.01f), Vector3.one, armor, new Vector3(0f, 0f, side * -12f));
            MeshObject("Forearm", arm, BuildTaperedCylinder(18, 0.11f, 0.085f, 0.36f),
                new Vector3(0f, -0.49f, 0.015f), Vector3.one, armor);
            MeshObject("WristGold", arm, BuildTorus(18, 8, 0.095f, 0.018f),
                new Vector3(0f, -0.64f, 0.025f), Vector3.one, gold, new Vector3(90f, 0f, 0f));
        }

        private void BuildLegs(Transform avatar, Transform production)
        {
            BuildLeg(avatar.Find("LeftLeg"), -1f);
            BuildLeg(avatar.Find("RightLeg"), 1f);
        }

        private void BuildLeg(Transform leg, float side)
        {
            if (leg == null) return;
            MeshObject("Thigh", leg, BuildTaperedCylinder(18, 0.145f, 0.118f, 0.48f),
                new Vector3(0f, -0.14f, 0f), Vector3.one, secondary);
            MeshObject("Knee", leg, BuildSuperEllipsoid(18, 7, 0.14f, 0.11f, 0.15f, 1.8f),
                new Vector3(0f, -0.39f, 0.045f), Vector3.one, armor);
            MeshObject("Shin", leg, BuildTaperedCylinder(18, 0.12f, 0.085f, 0.43f),
                new Vector3(0f, -0.57f, 0f), Vector3.one, armor);
            MeshObject("Boot", leg, BuildTaperedPrism(0.14f, 0.105f, 0.28f, 0.22f, 0.16f),
                new Vector3(0f, -0.76f, 0.08f), Vector3.one, secondary, new Vector3(90f, 0f, 0f));
            MeshObject("ShinGold", leg, BuildTaperedPrism(0.042f, 0.02f, 0.30f, 0.018f, 0.025f),
                new Vector3(side * 0.015f, -0.56f, 0.105f), Vector3.one, gold);
        }

        private void BuildMantle(Transform avatar, Transform production)
        {
            Transform torso = avatar.Find("Torso");
            if (torso == null) return;
            MeshObject("ShoulderMantle", torso, BuildCurvedMantle(12, 8, 0.74f, 0.62f),
                new Vector3(0f, 0.10f, -0.22f), Vector3.one, secondary, new Vector3(8f, 0f, 0f));
        }

        private static Transform Node(string name, Transform parent, Vector3 localPosition)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            return go.transform;
        }

        private static GameObject MeshObject(string name, Transform parent, Mesh mesh, Vector3 localPosition, Vector3 scale, Material material, Vector3? euler = null)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = scale;
            if (euler.HasValue) go.transform.localRotation = Quaternion.Euler(euler.Value);
            MeshFilter filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            MeshRenderer renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            renderer.receiveShadows = true;
            return go;
        }

        private static Mesh BuildSuperEllipsoid(int radial, int vertical, float rx, float ry, float rz, float exponent)
        {
            List<Vector3> vertices = new List<Vector3>();
            List<Vector2> uv = new List<Vector2>();
            List<int> triangles = new List<int>();
            for (int y = 0; y <= vertical; y++)
            {
                float v = y / (float)vertical;
                float lat = (v - 0.5f) * Mathf.PI;
                float cl = Mathf.Cos(lat);
                float sl = Mathf.Sin(lat);
                float sy = SignedPow(sl, exponent);
                float cr = SignedPow(cl, exponent);
                for (int x = 0; x <= radial; x++)
                {
                    float u = x / (float)radial;
                    float lon = u * Mathf.PI * 2f;
                    vertices.Add(new Vector3(rx * cr * SignedPow(Mathf.Cos(lon), exponent), ry * sy, rz * cr * SignedPow(Mathf.Sin(lon), exponent)));
                    uv.Add(new Vector2(u, v));
                }
            }
            int stride = radial + 1;
            for (int y = 0; y < vertical; y++)
            for (int x = 0; x < radial; x++)
            {
                int a = y * stride + x;
                int b = a + 1;
                int c = a + stride + 1;
                int d = a + stride;
                AddQuad(triangles, a, d, c, b);
            }
            return Finish(vertices, uv, triangles);
        }

        private static float SignedPow(float value, float exponent)
            => Mathf.Sign(value) * Mathf.Pow(Mathf.Abs(value), 2f / Mathf.Max(0.2f, exponent));

        private static Mesh BuildTaperedCylinder(int segments, float topRadius, float bottomRadius, float height)
        {
            List<Vector3> vertices = new List<Vector3>();
            List<Vector2> uv = new List<Vector2>();
            List<int> triangles = new List<int>();
            for (int ring = 0; ring < 2; ring++)
            {
                float y = ring == 0 ? -height * 0.5f : height * 0.5f;
                float r = ring == 0 ? bottomRadius : topRadius;
                for (int i = 0; i <= segments; i++)
                {
                    float u = i / (float)segments;
                    float a = u * Mathf.PI * 2f;
                    vertices.Add(new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r));
                    uv.Add(new Vector2(u, ring));
                }
            }
            int stride = segments + 1;
            for (int i = 0; i < segments; i++) AddQuad(triangles, i, stride + i, stride + i + 1, i + 1);
            return Finish(vertices, uv, triangles);
        }

        private static Mesh BuildTaperedPrism(float topWidth, float bottomWidth, float height, float topDepth, float bottomDepth)
        {
            float h = height * 0.5f;
            Vector3[] v =
            {
                new Vector3(-bottomWidth,-h,-bottomDepth), new Vector3(bottomWidth,-h,-bottomDepth), new Vector3(bottomWidth,-h,bottomDepth), new Vector3(-bottomWidth,-h,bottomDepth),
                new Vector3(-topWidth,h,-topDepth), new Vector3(topWidth,h,-topDepth), new Vector3(topWidth,h,topDepth), new Vector3(-topWidth,h,topDepth),
            };
            List<Vector3> vertices = new List<Vector3>(v);
            List<Vector2> uv = new List<Vector2>();
            for (int i = 0; i < 8; i++) uv.Add(new Vector2((i & 1) == 0 ? 0f : 1f, i < 4 ? 0f : 1f));
            List<int> triangles = new List<int>();
            AddQuad(triangles, 0, 1, 2, 3);
            AddQuad(triangles, 7, 6, 5, 4);
            AddQuad(triangles, 0, 4, 5, 1);
            AddQuad(triangles, 1, 5, 6, 2);
            AddQuad(triangles, 2, 6, 7, 3);
            AddQuad(triangles, 3, 7, 4, 0);
            return Finish(vertices, uv, triangles);
        }

        private static Mesh BuildOctahedron(float radius, float height)
        {
            List<Vector3> v = new List<Vector3>
            {
                new Vector3(0,height,0), new Vector3(radius,0,0), new Vector3(0,0,radius),
                new Vector3(-radius,0,0), new Vector3(0,0,-radius), new Vector3(0,-height,0),
            };
            List<Vector2> uv = new List<Vector2>();
            for (int i = 0; i < v.Count; i++) uv.Add(new Vector2(0.5f, 0.5f));
            List<int> t = new List<int>
            {
                0,2,1, 0,3,2, 0,4,3, 0,1,4,
                5,1,2, 5,2,3, 5,3,4, 5,4,1,
            };
            return Finish(v, uv, t);
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
                AddQuad(triangles, a, a + stride, a + stride + 1, a + 1);
            }
            return Finish(vertices, uv, triangles);
        }

        private static Mesh BuildCurvedMantle(int xSegments, int ySegments, float width, float height)
        {
            List<Vector3> vertices = new List<Vector3>();
            List<Vector2> uv = new List<Vector2>();
            List<int> triangles = new List<int>();
            for (int y = 0; y <= ySegments; y++)
            {
                float v = y / (float)ySegments;
                for (int x = 0; x <= xSegments; x++)
                {
                    float u = x / (float)xSegments;
                    float px = (u - 0.5f) * width;
                    float py = (0.5f - v) * height;
                    float pz = -0.035f - Mathf.Pow(Mathf.Abs(u - 0.5f) * 2f, 1.8f) * 0.055f - v * v * 0.06f;
                    vertices.Add(new Vector3(px, py, pz));
                    uv.Add(new Vector2(u, v));
                }
            }
            int stride = xSegments + 1;
            for (int y = 0; y < ySegments; y++)
            for (int x = 0; x < xSegments; x++)
            {
                int a = y * stride + x;
                AddQuad(triangles, a, a + stride, a + stride + 1, a + 1);
            }
            return Finish(vertices, uv, triangles);
        }

        private static void AddQuad(List<int> triangles, int a, int b, int c, int d)
        {
            triangles.Add(a); triangles.Add(b); triangles.Add(c);
            triangles.Add(a); triangles.Add(c); triangles.Add(d);
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
