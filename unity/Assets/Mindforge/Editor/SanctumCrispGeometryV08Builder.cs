#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Mindforge.Journey;

namespace Mindforge.Editor
{
    /// <summary>
    /// Replaces selected visible Unity cube meshes with reusable chamfered prism assets.
    /// BoxColliders, transforms, materials and gameplay components are untouched. The result
    /// is a real edge highlight/shadow break under ordinary lighting instead of an outline
    /// effect or extra bloom. Enemy reference shells receive a slightly stronger chamfer so
    /// their silhouette facets remain readable at combat distance.
    /// </summary>
    [InitializeOnLoad]
    public static class SanctumCrispGeometryV08Builder
    {
        public const string ArchitecturalMeshPath = "Assets/Mindforge/Generated/SanctumV08/ChamferedArchitecturalPrism.asset";
        public const string EnemyMeshPath = "Assets/Mindforge/Generated/SanctumV08/ChamferedEnemyPrism.asset";
        public const float ArchitecturalBevel = 0.055f;
        public const float EnemyBevel = 0.105f;

        private static bool _applying;

        static SanctumCrispGeometryV08Builder()
        {
            EditorApplication.delayCall += TryAutoApply;
            EditorSceneManager.sceneSaved += _ => TryAutoApply();
        }

        [MenuItem("Mindforge/Legacy/Showcase/Apply Sanctum Crisp Geometry V0.8", priority = 40)]
        public static void ApplyOpenScene()
        {
            if (_applying || EditorApplication.isPlayingOrWillChangePlaymode) return;
            GameObject sanctum = EditorSceneLookup.FindIncludingInactive(SanctumOnboardingV08Builder.RootName);
            if (sanctum == null || sanctum.transform.Find(SanctumReferenceFidelityV08Builder.RootName) == null) return;

            _applying = true;
            try
            {
                Mesh architecture = EnsureChamferedMesh(ArchitecturalMeshPath, "ChamferedArchitecturalPrism", ArchitecturalBevel);
                Mesh enemy = EnsureChamferedMesh(EnemyMeshPath, "ChamferedEnemyPrism", EnemyBevel);

                int architectureCount = ApplyArchitectureMeshes(sanctum.transform, architecture);
                int enemyCount = ApplyEnemyMeshes(enemy);

                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                AssetDatabase.SaveAssets();
                Debug.Log(
                    $"[Mindforge:V08:Crisp] Applied reusable chamfered mesh edges to {architectureCount} hero architectural pieces and " +
                    $"{enemyCount} ordinary reference-enemy facets without changing collision or gameplay authority.");
            }
            finally
            {
                _applying = false;
            }
        }

        private static void TryAutoApply()
        {
            if (_applying || EditorApplication.isPlayingOrWillChangePlaymode) return;
            GameObject sanctum = EditorSceneLookup.FindIncludingInactive(SanctumOnboardingV08Builder.RootName);
            if (sanctum == null || sanctum.transform.Find(SanctumReferenceFidelityV08Builder.RootName) == null) return;
            ApplyOpenScene();
        }

        private static int ApplyArchitectureMeshes(Transform sanctum, Mesh chamfered)
        {
            int count = 0;
            MeshFilter[] filters = sanctum.GetComponentsInChildren<MeshFilter>(true);
            for (int i = 0; i < filters.Length; i++)
            {
                MeshFilter filter = filters[i];
                if (filter == null || !ShouldChamferArchitecture(filter.transform)) continue;
                if (!IsCubeLike(filter.sharedMesh) && filter.sharedMesh != chamfered) continue;
                if (filter.sharedMesh == chamfered) continue;
                filter.sharedMesh = chamfered;
                EditorUtility.SetDirty(filter);
                count++;
            }
            return count;
        }

        private static int ApplyEnemyMeshes(Mesh chamfered)
        {
            int count = 0;
            JourneyEnemyController[] enemies = UnityEngine.Object.FindObjectsOfType<JourneyEnemyController>(true);
            for (int i = 0; i < enemies.Length; i++)
            {
                JourneyEnemyController controller = enemies[i];
                if (controller == null) continue;
                Transform visuals = controller.transform.Find("Visuals");
                Transform reference = visuals != null ? visuals.Find(SanctumReferenceFidelityV08Builder.EnemyRootName) : null;
                if (reference == null) continue;

                MeshFilter[] filters = reference.GetComponentsInChildren<MeshFilter>(true);
                for (int f = 0; f < filters.Length; f++)
                {
                    MeshFilter filter = filters[f];
                    if (filter == null || !IsCubeLike(filter.sharedMesh) || filter.sharedMesh == chamfered) continue;
                    filter.sharedMesh = chamfered;
                    EditorUtility.SetDirty(filter);
                    count++;
                }
            }
            return count;
        }

        private static bool ShouldChamferArchitecture(Transform t)
        {
            if (t == null) return false;
            string n = t.name;

            // Keep floors, roads, glass panes, tiny inlays and line-work planar. Chamfer only
            // load-bearing-looking or hero structural masses where edge response carries scale.
            if (ContainsAny(n, "Floor", "Road", "Spine", "Joint", "Glass", "Gold", "Signal", "Water", "Garden", "RouteNode", "Window", "Rune"))
                return false;

            return ContainsAny(
                n,
                "Pier",
                "Plinth",
                "Pedestal",
                "Capital",
                "Buttress",
                "Boundary",
                "BackWall",
                "Threshold",
                "Parapet",
                "SanctumBlock",
                "BridgeDeck",
                "ForgeWing",
                "ForgeDais",
                "ForgePedestal",
                "WallPilaster");
        }

        private static bool ContainsAny(string value, params string[] needles)
        {
            for (int i = 0; i < needles.Length; i++)
                if (value.IndexOf(needles[i], StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }

        private static bool IsCubeLike(Mesh mesh)
        {
            if (mesh == null) return false;
            if (string.Equals(mesh.name, "Cube", StringComparison.OrdinalIgnoreCase)) return true;
            return mesh.vertexCount == 24 && mesh.subMeshCount == 1;
        }

        private static Mesh EnsureChamferedMesh(string path, string name, float bevel)
        {
            EnsureFolder("Assets/Mindforge/Generated/SanctumV08");
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh != null) return mesh;

            mesh = BuildChamferedUnitCube(Mathf.Clamp(bevel, 0.005f, 0.22f));
            mesh.name = name;
            AssetDatabase.CreateAsset(mesh, path);
            EditorUtility.SetDirty(mesh);
            return mesh;
        }

        private static void EnsureFolder(string fullPath)
        {
            string[] parts = fullPath.Split('/');
            if (parts.Length == 0 || parts[0] != "Assets")
                throw new InvalidOperationException("Generated mesh folder must live under Assets.");

            string current = "Assets";
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private static Mesh BuildChamferedUnitCube(float bevel)
        {
            float h = 0.5f;
            float q = h - bevel;
            List<Vector3> vertices = new List<Vector3>(160);
            List<int> triangles = new List<int>(240);
            List<Vector2> uvs = new List<Vector2>(160);

            // Six inset primary faces.
            AddQuad(vertices, triangles, uvs, new Vector3(-q,-q,h), new Vector3(q,-q,h), new Vector3(q,q,h), new Vector3(-q,q,h));
            AddQuad(vertices, triangles, uvs, new Vector3(q,-q,-h), new Vector3(-q,-q,-h), new Vector3(-q,q,-h), new Vector3(q,q,-h));
            AddQuad(vertices, triangles, uvs, new Vector3(h,-q,q), new Vector3(h,-q,-q), new Vector3(h,q,-q), new Vector3(h,q,q));
            AddQuad(vertices, triangles, uvs, new Vector3(-h,-q,-q), new Vector3(-h,-q,q), new Vector3(-h,q,q), new Vector3(-h,q,-q));
            AddQuad(vertices, triangles, uvs, new Vector3(-q,h,q), new Vector3(q,h,q), new Vector3(q,h,-q), new Vector3(-q,h,-q));
            AddQuad(vertices, triangles, uvs, new Vector3(-q,-h,-q), new Vector3(q,-h,-q), new Vector3(q,-h,q), new Vector3(-q,-h,q));

            // Twelve planar edge chamfers.
            AddQuad(vertices, triangles, uvs, new Vector3(-q,q,h), new Vector3(q,q,h), new Vector3(q,h,q), new Vector3(-q,h,q));
            AddQuad(vertices, triangles, uvs, new Vector3(-q,h,-q), new Vector3(q,h,-q), new Vector3(q,q,-h), new Vector3(-q,q,-h));
            AddQuad(vertices, triangles, uvs, new Vector3(-q,-h,q), new Vector3(q,-h,q), new Vector3(q,-q,h), new Vector3(-q,-q,h));
            AddQuad(vertices, triangles, uvs, new Vector3(-q,-q,-h), new Vector3(q,-q,-h), new Vector3(q,-h,-q), new Vector3(-q,-h,-q));

            AddQuad(vertices, triangles, uvs, new Vector3(h,-q,q), new Vector3(h,q,q), new Vector3(q,q,h), new Vector3(q,-q,h));
            AddQuad(vertices, triangles, uvs, new Vector3(-q,-q,h), new Vector3(-q,q,h), new Vector3(-h,q,q), new Vector3(-h,-q,q));
            AddQuad(vertices, triangles, uvs, new Vector3(q,-q,-h), new Vector3(q,q,-h), new Vector3(h,q,-q), new Vector3(h,-q,-q));
            AddQuad(vertices, triangles, uvs, new Vector3(-h,-q,-q), new Vector3(-h,q,-q), new Vector3(-q,q,-h), new Vector3(-q,-q,-h));

            AddQuad(vertices, triangles, uvs, new Vector3(q,h,-q), new Vector3(q,h,q), new Vector3(h,q,q), new Vector3(h,q,-q));
            AddQuad(vertices, triangles, uvs, new Vector3(-h,q,-q), new Vector3(-h,q,q), new Vector3(-q,h,q), new Vector3(-q,h,-q));
            AddQuad(vertices, triangles, uvs, new Vector3(h,-q,-q), new Vector3(h,-q,q), new Vector3(q,-h,q), new Vector3(q,-h,-q));
            AddQuad(vertices, triangles, uvs, new Vector3(-q,-h,-q), new Vector3(-q,-h,q), new Vector3(-h,-q,q), new Vector3(-h,-q,-q));

            // Eight corner facets close the chamfer shell.
            for (int sx = -1; sx <= 1; sx += 2)
            for (int sy = -1; sy <= 1; sy += 2)
            for (int sz = -1; sz <= 1; sz += 2)
            {
                Vector3 px = new Vector3(sx * h, sy * q, sz * q);
                Vector3 py = new Vector3(sx * q, sy * h, sz * q);
                Vector3 pz = new Vector3(sx * q, sy * q, sz * h);
                AddTriangle(vertices, triangles, uvs, px, py, pz);
            }

            Mesh mesh = new Mesh();
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0, true);
            mesh.SetUVs(0, uvs);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AddQuad(
            List<Vector3> vertices,
            List<int> triangles,
            List<Vector2> uvs,
            Vector3 a,
            Vector3 b,
            Vector3 c,
            Vector3 d)
        {
            Vector3 center = (a + b + c + d) * 0.25f;
            Vector3 normal = Vector3.Cross(b - a, c - a);
            if (Vector3.Dot(normal, center) < 0f)
            {
                Vector3 tmp = b;
                b = d;
                d = tmp;
            }

            int start = vertices.Count;
            vertices.Add(a);
            vertices.Add(b);
            vertices.Add(c);
            vertices.Add(d);
            uvs.Add(new Vector2(0f,0f));
            uvs.Add(new Vector2(1f,0f));
            uvs.Add(new Vector2(1f,1f));
            uvs.Add(new Vector2(0f,1f));
            triangles.Add(start);
            triangles.Add(start + 1);
            triangles.Add(start + 2);
            triangles.Add(start);
            triangles.Add(start + 2);
            triangles.Add(start + 3);
        }

        private static void AddTriangle(
            List<Vector3> vertices,
            List<int> triangles,
            List<Vector2> uvs,
            Vector3 a,
            Vector3 b,
            Vector3 c)
        {
            Vector3 center = (a + b + c) / 3f;
            Vector3 normal = Vector3.Cross(b - a, c - a);
            if (Vector3.Dot(normal, center) < 0f)
            {
                Vector3 tmp = b;
                b = c;
                c = tmp;
            }

            int start = vertices.Count;
            vertices.Add(a);
            vertices.Add(b);
            vertices.Add(c);
            uvs.Add(new Vector2(0f,0f));
            uvs.Add(new Vector2(1f,0f));
            uvs.Add(new Vector2(0.5f,1f));
            triangles.Add(start);
            triangles.Add(start + 1);
            triangles.Add(start + 2);
        }
    }
}
#endif