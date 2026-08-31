#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Mindforge.Combat;
using Mindforge.Presentation;

namespace Mindforge.Editor
{
    /// <summary>
    /// Builds the V0.11 presentation slice from the tested competition systems kernel.
    /// Unlike the historical showcase path, this builder does not invoke V0.5-V0.10 world
    /// decorators. It creates one coherent collision-backed route with aligned presentation.
    /// </summary>
    public static class MindforgeDemoV11Builder
    {
        public const string DemoScenePath = "Assets/Mindforge/Scenes/MindforgeDemoV11.unity";
        public const string RootName = "Mindforge_Demo_World_V11";
        public const string MarkerName = "Mindforge_Demo_V11";

        private const string GeneratedRoot = "Assets/Mindforge/Generated/V11";
        private const string MaterialRoot = GeneratedRoot + "/Materials";

        private static readonly Vector3 GuardianSpawn = new Vector3(0f, 0.72f, -18f);
        private static readonly Vector3 BossSpawn = new Vector3(0f, 5.0f, 94f);

        [MenuItem("Mindforge/Legacy/V0.11 Demo/Build + Play Presentable Demo", priority = 1)]
        public static void BuildAndPlay()
        {
            BuildDemoScene(true);
            EditorApplication.delayCall += () =>
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode)
                    EditorApplication.isPlaying = true;
            };
        }

        [MenuItem("Mindforge/Legacy/V0.11 Demo/Rebuild Presentable Demo", priority = 2)]
        public static void RebuildDemo() => BuildDemoScene(true);

        [MenuItem("Mindforge/Legacy/V0.11 Demo/Rebuild Neural-Hardware Demo", priority = 3)]
        public static void RebuildNeuralDemo() => BuildDemoScene(false);

        public static void BuildDemoScene(bool controllerOnlyByDefault)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new UnityEditor.Build.BuildFailedException("Stop Play Mode before rebuilding the V0.11 demo.");

            // CompetitionSceneAssembler remains the systems kernel: Guardian movement/combat,
            // boss, Wisp, BCI receiver/calibration, telemetry and generated combat prefabs.
            CompetitionSceneAssembler.BuildCompetitionScene();
            EnsureFolders();

            GameObject arena = GameObject.Find("Fractured_Signal_Arena");
            GameObject guardian = GameObject.Find("Guardian");
            GameObject boss = GameObject.Find("The_Fractured_Signal");
            if (arena == null || guardian == null || boss == null)
                throw new UnityEditor.Build.BuildFailedException("V0.11 systems kernel is incomplete after competition assembly.");

            StripKernelBlockout(arena.transform);

            Material stone = EnsureLit("V11_Stone", new Color(0.19f, 0.22f, 0.25f), 0.08f, 0.58f);
            Material stoneLight = EnsureLit("V11_StoneLight", new Color(0.55f, 0.55f, 0.52f), 0.02f, 0.46f);
            Material graphite = EnsureLit("V11_Graphite", new Color(0.055f, 0.072f, 0.09f), 0.52f, 0.72f);
            Material gold = EnsureLit("V11_Gold", new Color(0.58f, 0.40f, 0.16f), 0.82f, 0.72f);
            Material aether = EnsureEmission("V11_Aether", new Color(0.10f, 0.56f, 0.72f), new Color(0.16f, 0.76f, 1f) * 2.2f);
            Material water = EnsureLit("V11_Water", new Color(0.055f, 0.18f, 0.23f), 0.12f, 0.88f);
            Material garden = EnsureLit("V11_Garden", new Color(0.12f, 0.25f, 0.20f), 0.0f, 0.38f);
            Material hostile = EnsureEmission("V11_Fracture", new Color(0.28f, 0.055f, 0.12f), new Color(0.95f, 0.12f, 0.30f) * 1.8f);

            GameObject root = new GameObject(RootName);
            root.transform.SetParent(arena.transform, false);

            BuildBackground(root.transform, graphite, stone, water);
            BuildSanctum(root.transform, stone, stoneLight, gold, aether, garden);
            BuildCauseway(root.transform, stone, stoneLight, graphite, gold, aether, water);
            BuildMarket(root.transform, stone, stoneLight, graphite, gold, aether, garden);
            BuildAscent(root.transform, stone, stoneLight, graphite, gold, aether);
            BuildFractureArena(root.transform, stone, graphite, gold, hostile);
            BuildEchoEncounters(root.transform);
            BuildSkyline(root.transform, stoneLight, graphite, gold, aether);

            guardian.transform.position = GuardianSpawn;
            guardian.transform.rotation = Quaternion.identity;
            boss.transform.position = BossSpawn;
            boss.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

            GameObject markerObject = GameObject.Find(MarkerName);
            if (markerObject != null) UnityEngine.Object.DestroyImmediate(markerObject);
            markerObject = new GameObject(MarkerName);
            MindforgeDemoV11Marker marker = markerObject.AddComponent<MindforgeDemoV11Marker>();
            marker.Configure(controllerOnlyByDefault, GuardianSpawn, BossSpawn);

            ConfigureLighting();
            ConfigureCameraKernel();

            Scene scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, DemoScenePath, true);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeGameObject = guardian;

            Debug.Log(
                "[Mindforge:V11] Clean demo rebuilt. Systems kernel preserved; historical world decorators omitted. " +
                "Route: Memory Forge Sanctum → Causeway → Market → Tower Ascent → Fractured Signal. " +
                "All primary traversal floors/walls are visible collision owners; V0.11 runtime owns camera, HUD and Guardian presentation.");
        }

        private static void StripKernelBlockout(Transform arena)
        {
            for (int i = arena.childCount - 1; i >= 0; i--)
            {
                Transform child = arena.GetChild(i);
                if (child == null) continue;
                if (child.name == "ArenaFloor" || child.name.StartsWith("ArenaPillar_", StringComparison.Ordinal))
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
            }
        }

        private static void BuildBackground(Transform root, Material graphite, Material stone, Material water)
        {
            // A visual catch basin prevents the edge-of-world void without adding traversal authority.
            Block("LowerCityMass", root, new Vector3(0f, -5.5f, 38f), new Vector3(70f, 7f, 145f), graphite, false);
            Block("DistantWater", root, new Vector3(0f, -1.25f, 30f), new Vector3(52f, 0.12f, 112f), water, false);
            Block("FarGroundNorth", root, new Vector3(0f, -2.4f, 115f), new Vector3(68f, 3f, 44f), stone, false);
        }

        private static void BuildSanctum(Transform root, Material stone, Material lightStone, Material gold, Material aether, Material garden)
        {
            Transform zone = Node("V11_Memory_Forge_Sanctum", root);
            Block("SanctumFloor", zone, new Vector3(0f, -0.35f, -13f), new Vector3(18f, 0.7f, 22f), lightStone, true);
            Block("SanctumBackWall", zone, new Vector3(0f, 2.0f, -24f), new Vector3(21f, 4.6f, 0.8f), stone, true);
            Block("SanctumWallL", zone, new Vector3(-10.1f, 2.0f, -13f), new Vector3(1.0f, 4.6f, 22f), stone, true);
            Block("SanctumWallR", zone, new Vector3(10.1f, 2.0f, -13f), new Vector3(1.0f, 4.6f, 22f), stone, true);

            for (int side = -1; side <= 1; side += 2)
            {
                Column($"SanctumColumn_{side}_A", zone, new Vector3(side * 6.8f, 2.8f, -18f), new Vector3(1.2f, 5.6f, 1.2f), lightStone);
                Column($"SanctumColumn_{side}_B", zone, new Vector3(side * 6.8f, 2.8f, -7f), new Vector3(1.2f, 5.6f, 1.2f), lightStone);
                Block($"SanctumGarden_{side}", zone, new Vector3(side * 7.6f, 0.22f, -12.5f), new Vector3(2.3f, 0.44f, 5.5f), garden, false);
            }

            Arch("SanctumHeroArch", zone, new Vector3(0f, 4.0f, -1.9f), new Vector3(5.2f, 5.0f, 1.3f), lightStone);
            Block("SanctumGoldThreshold", zone, new Vector3(0f, 0.05f, -1.6f), new Vector3(5.8f, 0.10f, 0.9f), gold, false);
            Sphere("MemoryForgeCore", zone, new Vector3(0f, 1.25f, -15.2f), Vector3.one * 1.25f, aether, false);
            Cylinder("MemoryForgePlinth", zone, new Vector3(0f, 0.25f, -15.2f), new Vector3(1.8f, 0.35f, 1.8f), gold, true);
        }

        private static void BuildCauseway(Transform root, Material stone, Material lightStone, Material graphite, Material gold, Material aether, Material water)
        {
            Transform zone = Node("V11_Neon_Causeway", root);
            Block("CausewayRoad", zone, new Vector3(0f, -0.24f, 15f), new Vector3(8.6f, 0.48f, 34f), stone, true);
            Block("CausewayWallL", zone, new Vector3(-5.1f, 1.05f, 15f), new Vector3(0.8f, 2.6f, 34f), graphite, true);
            Block("CausewayWallR", zone, new Vector3(5.1f, 1.05f, 15f), new Vector3(0.8f, 2.6f, 34f), graphite, true);
            Block("CausewayCanalL", zone, new Vector3(-8.2f, -0.65f, 15f), new Vector3(4.3f, 0.10f, 34f), water, false);
            Block("CausewayCanalR", zone, new Vector3(8.2f, -0.65f, 15f), new Vector3(4.3f, 0.10f, 34f), water, false);
            Block("CausewayAetherSpine", zone, new Vector3(0f, 0.02f, 15f), new Vector3(0.12f, 0.035f, 32f), aether, false);

            for (int i = 0; i < 5; i++)
            {
                float z = 3.5f + i * 6f;
                Column($"CausewayPylonL_{i}", zone, new Vector3(-4.45f, 2.0f, z), new Vector3(0.62f, 4.0f, 0.62f), i % 2 == 0 ? lightStone : stone);
                Column($"CausewayPylonR_{i}", zone, new Vector3(4.45f, 2.0f, z), new Vector3(0.62f, 4.0f, 0.62f), i % 2 == 0 ? lightStone : stone);
            }
            Arch("CausewayExitArch", zone, new Vector3(0f, 4.1f, 32.0f), new Vector3(4.6f, 4.8f, 1.15f), lightStone);
            Block("CausewayExitGold", zone, new Vector3(0f, 0.04f, 32.0f), new Vector3(5.1f, 0.10f, 0.8f), gold, false);
        }

        private static void BuildMarket(Transform root, Material stone, Material lightStone, Material graphite, Material gold, Material aether, Material garden)
        {
            Transform zone = Node("V11_Market_of_Broken_Momentum", root);
            Block("MarketFloor", zone, new Vector3(0f, -0.28f, 45f), new Vector3(20f, 0.56f, 24f), stone, true);
            Block("MarketWallL", zone, new Vector3(-11f, 1.65f, 45f), new Vector3(1.0f, 3.8f, 24f), graphite, true);
            Block("MarketWallR", zone, new Vector3(11f, 1.65f, 45f), new Vector3(1.0f, 3.8f, 24f), graphite, true);

            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < 3; i++)
                {
                    float z = 38f + i * 7f;
                    Column($"MarketColumn_{side}_{i}", zone, new Vector3(side * 7.6f, 2.4f, z), new Vector3(0.85f, 4.8f, 0.85f), lightStone);
                    Block($"MarketStall_{side}_{i}", zone, new Vector3(side * 6.2f, 0.85f, z), new Vector3(2.5f, 1.7f, 2.4f), i == 1 ? graphite : stone, true);
                }
                Block($"MarketGarden_{side}", zone, new Vector3(side * 8.4f, 0.18f, 45f), new Vector3(2.2f, 0.36f, 5.5f), garden, false);
            }

            Block("MarketDuelPlatform", zone, new Vector3(0f, 0.45f, 49f), new Vector3(8.0f, 0.9f, 6.5f), lightStone, true);
            Block("MarketJumpPerchL", zone, new Vector3(-6.4f, 1.20f, 53f), new Vector3(3.0f, 0.55f, 3.0f), stone, true);
            Block("MarketJumpPerchR", zone, new Vector3(6.4f, 1.70f, 54f), new Vector3(3.0f, 0.55f, 3.0f), stone, true);
            Sphere("MarketSignalOrb", zone, new Vector3(0f, 2.25f, 49f), Vector3.one * 0.42f, aether, false);
            Arch("MarketExitArch", zone, new Vector3(0f, 4.1f, 58.2f), new Vector3(5.2f, 5.0f, 1.25f), lightStone);
            Block("MarketGoldAxis", zone, new Vector3(0f, 0.02f, 45f), new Vector3(0.10f, 0.03f, 19f), gold, false);
        }

        private static void BuildAscent(Transform root, Material stone, Material lightStone, Material graphite, Material gold, Material aether)
        {
            Transform zone = Node("V11_Choir_Tower_Ascent", root);
            const float slope = -8.1f;
            Block("AscentRamp", zone, new Vector3(0f, 1.8f, 71.3f), new Vector3(10.5f, 0.65f, 28f), stone, true, new Vector3(slope, 0f, 0f));
            Block("AscentWallL", zone, new Vector3(-6.0f, 3.0f, 71.3f), new Vector3(0.8f, 3.0f, 28f), graphite, true, new Vector3(slope, 0f, 0f));
            Block("AscentWallR", zone, new Vector3(6.0f, 3.0f, 71.3f), new Vector3(0.8f, 3.0f, 28f), graphite, true, new Vector3(slope, 0f, 0f));
            Block("AscentAetherGuide", zone, new Vector3(0f, 1.94f, 71.3f), new Vector3(0.11f, 0.035f, 25f), aether, false, new Vector3(slope, 0f, 0f));

            for (int i = 0; i < 4; i++)
            {
                float z = 62f + i * 6.0f;
                float y = 0.75f + i * 0.82f;
                Column($"AscentColumnL_{i}", zone, new Vector3(-5.2f, y + 2.0f, z), new Vector3(0.70f, 4.0f, 0.70f), lightStone);
                Column($"AscentColumnR_{i}", zone, new Vector3(5.2f, y + 2.0f, z), new Vector3(0.70f, 4.0f, 0.70f), lightStone);
            }
            Arch("AscentCrown", zone, new Vector3(0f, 7.2f, 82.6f), new Vector3(5.3f, 5.1f, 1.3f), lightStone);
            Block("AscentGoldThreshold", zone, new Vector3(0f, 4.12f, 83.0f), new Vector3(5.4f, 0.10f, 0.8f), gold, false);
        }

        private static void BuildFractureArena(Transform root, Material stone, Material graphite, Material gold, Material hostile)
        {
            Transform zone = Node("V11_Fractured_Signal_Arena", root);
            Vector3 center = new Vector3(0f, 3.72f, 94f);
            Block("FractureFloor", zone, center, new Vector3(25f, 0.72f, 24f), stone, true);
            Block("FractureInnerDais", zone, new Vector3(0f, 4.25f, 94f), new Vector3(9.0f, 0.38f, 9.0f), graphite, true);
            Block("FractureGoldAxis", zone, new Vector3(0f, 4.11f, 90f), new Vector3(0.12f, 0.04f, 12f), gold, false);

            const int segments = 14;
            const float radius = 13.0f;
            for (int i = 0; i < segments; i++)
            {
                // Keep a broad south entrance from the ascent.
                if (i == 6 || i == 7 || i == 8) continue;
                float a = i / (float)segments * Mathf.PI * 2f;
                Vector3 position = new Vector3(Mathf.Sin(a) * radius, 5.5f, 94f + Mathf.Cos(a) * radius);
                Block($"FractureWall_{i:00}", zone, position, new Vector3(6.1f, 4.2f, 0.8f), graphite, true, new Vector3(0f, a * Mathf.Rad2Deg, 0f));
            }

            for (int i = 0; i < 4; i++)
            {
                float a = (45f + i * 90f) * Mathf.Deg2Rad;
                Vector3 p = new Vector3(Mathf.Sin(a) * 9.5f, 6.2f, 94f + Mathf.Cos(a) * 9.5f);
                Spire($"FractureSpire_{i}", zone, p, new Vector3(1.4f, 5.5f, 1.4f), hostile);
            }
        }

        private static void BuildEchoEncounters(Transform root)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mindforge/Generated/Prefabs/FracturedEcho.prefab");
            if (prefab == null) return;

            Vector3[] anchors =
            {
                new Vector3(0f, 1.2f, 17f),
                new Vector3(-3.5f, 1.6f, 47f),
                new Vector3(2.8f, 3.1f, 71f),
            };
            for (int i = 0; i < anchors.Length; i++)
            {
                Transform anchor = Node($"V11EchoAnchor_{i:00}", root);
                anchor.position = anchors[i];
                GameObject echo = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                if (echo == null) continue;
                echo.name = $"V11Echo_{i:00}";
                echo.transform.SetParent(root, true);
                echo.transform.position = anchors[i] + Vector3.up * 0.35f;
            }
        }

        private static void BuildSkyline(Transform root, Material lightStone, Material graphite, Material gold, Material aether)
        {
            Transform zone = Node("V11_Skyline", root);
            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < 5; i++)
                {
                    float z = -4f + i * 24f;
                    float x = side * (18f + (i % 2) * 5f);
                    Block($"SkylineMass_{side}_{i}", zone, new Vector3(x, 5.5f, z), new Vector3(8f, 11f + i * 1.4f, 7f), graphite, false);
                    Spire($"SkylineSpire_{side}_{i}", zone, new Vector3(x, 12.0f + i * 0.7f, z), new Vector3(2.3f, 7.5f, 2.3f), i % 2 == 0 ? lightStone : graphite);
                }
            }
            Spire("SkylineHeroSpire", zone, new Vector3(0f, 19f, 132f), new Vector3(6f, 24f, 6f), lightStone);
            Sphere("SkylineAetherBeacon", zone, new Vector3(0f, 26f, 132f), Vector3.one * 1.1f, aether, false);
            Block("SkylineGoldCrown", zone, new Vector3(0f, 17f, 132f), new Vector3(6.5f, 0.18f, 6.5f), gold, false);
        }

        private static void ConfigureLighting()
        {
            Light key = GameObject.Find("KeyLight")?.GetComponent<Light>();
            if (key != null)
            {
                key.color = new Color(1.0f, 0.91f, 0.78f);
                key.intensity = 1.05f;
                key.shadows = LightShadows.Soft;
                key.transform.rotation = Quaternion.Euler(48f, -36f, 0f);
            }

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.25f, 0.29f, 0.34f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.16f, 0.20f, 0.24f);
            RenderSettings.fogStartDistance = 62f;
            RenderSettings.fogEndDistance = 175f;
        }

        private static void ConfigureCameraKernel()
        {
            Camera camera = Camera.main;
            if (camera == null) return;
            camera.fieldOfView = 56f;
            camera.nearClipPlane = 0.08f;
            camera.farClipPlane = 260f;
            camera.clearFlags = CameraClearFlags.Skybox;
        }

        private static Transform Node(string name, Transform parent)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        private static GameObject Block(string name, Transform parent, Vector3 position, Vector3 scale, Material material, bool collider, Vector3? euler = null)
            => Primitive(name, PrimitiveType.Cube, parent, position, scale, material, collider, euler);

        private static GameObject Sphere(string name, Transform parent, Vector3 position, Vector3 scale, Material material, bool collider)
            => Primitive(name, PrimitiveType.Sphere, parent, position, scale, material, collider, null);

        private static GameObject Cylinder(string name, Transform parent, Vector3 position, Vector3 scale, Material material, bool collider)
            => Primitive(name, PrimitiveType.Cylinder, parent, position, scale, material, collider, null);

        private static GameObject Primitive(string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Material material, bool collider, Vector3? euler)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = scale;
            if (euler.HasValue) go.transform.rotation = Quaternion.Euler(euler.Value);
            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = material;
            Collider c = go.GetComponent<Collider>();
            if (c != null && !collider) UnityEngine.Object.DestroyImmediate(c);
            return go;
        }

        private static GameObject MeshPart(string name, Transform parent, Mesh mesh, Vector3 position, Vector3 scale, Material material)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = scale;
            MeshFilter filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            MeshRenderer renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
            return go;
        }

        private static void Column(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
            => MeshPart(name, parent, ProductionMeshLibraryV09.FlutedColumn(), position, scale, material);

        private static void Arch(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
            => MeshPart(name, parent, ProductionMeshLibraryV09.PointedArch(), position, scale, material);

        private static void Spire(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
            => MeshPart(name, parent, ProductionMeshLibraryV09.CathedralSpire(), position, scale, material);

        private static Material EnsureLit(string name, Color color, float metallic, float smoothness)
        {
            string path = MaterialRoot + "/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            else if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", Mathf.Clamp01(metallic));
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", Mathf.Clamp01(smoothness));
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material EnsureEmission(string name, Color baseColor, Color emission)
        {
            Material material = EnsureLit(name, baseColor, 0.22f, 0.72f);
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission);
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void EnsureFolders()
        {
            Directory.CreateDirectory("Assets/Mindforge/Scenes");
            Directory.CreateDirectory(GeneratedRoot);
            Directory.CreateDirectory(MaterialRoot);
        }
    }
}
#endif
