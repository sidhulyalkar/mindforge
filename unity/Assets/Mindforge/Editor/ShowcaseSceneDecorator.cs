#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mindforge.Editor
{
    /// <summary>
    /// Adds the authored-looking static presentation layer on top of the deterministic
    /// qualification scene. The base scene remains deliberately simple; this decorator
    /// owns scenery only and never adds combat or neural authority.
    /// </summary>
    public static class ShowcaseSceneDecorator
    {
        public const string ShowcaseRootName = "Mindforge_Showcase_Environment";
        private const string Generated = "Assets/Mindforge/Generated/Showcase";

        public static void DecorateOpenScene()
        {
            if (!EditorSceneManager.GetActiveScene().IsValid())
                throw new InvalidOperationException("No active Unity scene is available to decorate.");

            EnsureFolders();
            RemoveExisting();
            ConfigureGlobalEnvironment();

            Material floor = EnsureLit("ArenaBasalt.mat", new Color(0.018f, 0.024f, 0.045f), 0.72f, 0.58f);
            Material stone = EnsureLit("ObsidianArchitecture.mat", new Color(0.035f, 0.045f, 0.075f), 0.52f, 0.34f);
            Material metal = EnsureLit("GuardianMetal.mat", new Color(0.16f, 0.20f, 0.30f), 0.92f, 0.72f);
            Material cyan = EnsureEmission("AetherCyan.mat", new Color(0.10f, 0.48f, 1f), 2.6f);
            Material violet = EnsureEmission("FractureViolet.mat", new Color(0.55f, 0.18f, 1f), 2.2f);
            Material ember = EnsureEmission("FractureEmber.mat", new Color(1f, 0.16f, 0.12f), 2.4f);
            Material green = EnsureEmission("WispVerdant.mat", new Color(0.10f, 1f, 0.47f), 2.2f);

            GameObject arena = GameObject.Find("Fractured_Signal_Arena");
            if (arena != null)
                BuildArena(arena.transform, floor, stone, metal, cyan, violet, ember);

            GameObject awakening = GameObject.Find("The_Awakening");
            if (awakening != null)
                BuildAwakening(awakening.transform, floor, stone, cyan, green);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("[Mindforge] Showcase scenery authored into the competition scene.");
        }

        private static void ConfigureGlobalEnvironment()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.0105f;
            RenderSettings.fogColor = new Color(0.018f, 0.025f, 0.055f);
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.055f, 0.075f, 0.14f);
            RenderSettings.ambientEquatorColor = new Color(0.028f, 0.035f, 0.070f);
            RenderSettings.ambientGroundColor = new Color(0.008f, 0.010f, 0.020f);
            RenderSettings.reflectionIntensity = 0.70f;
        }

        private static void BuildArena(
            Transform arena,
            Material floor,
            Material stone,
            Material metal,
            Material cyan,
            Material violet,
            Material ember)
        {
            GameObject root = new GameObject(ShowcaseRootName);
            root.transform.SetParent(arena, false);

            // The arena is a layered circular dueling space. The existing large floor
            // keeps collision authority; these thin surfaces provide visual hierarchy.
            Primitive("DuelFloor", PrimitiveType.Cylinder, root.transform,
                new Vector3(0f, -0.255f, 1f), new Vector3(9.15f, 0.035f, 9.15f), floor, false);
            Primitive("InnerDais", PrimitiveType.Cylinder, root.transform,
                new Vector3(0f, -0.20f, 1f), new Vector3(5.6f, 0.028f, 5.6f), metal, false);

            CreateCircle("ArenaRing_Outer", root.transform, new Vector3(0f, -0.145f, 1f), 8.65f, 128, 0.055f, violet);
            CreateCircle("ArenaRing_Mid", root.transform, new Vector3(0f, -0.135f, 1f), 5.75f, 96, 0.040f, cyan);
            CreateCircle("ArenaRing_Core", root.transform, new Vector3(0f, -0.125f, 1f), 2.75f, 72, 0.032f, ember);

            for (int i = 0; i < 12; i++)
            {
                float angle = i / 12f * Mathf.PI * 2f;
                Vector3 dir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                CreateLine($"ArenaSpoke_{i:00}", root.transform,
                    new Vector3(0f, -0.13f, 1f) + dir * 2.95f,
                    new Vector3(0f, -0.13f, 1f) + dir * 8.35f,
                    0.018f,
                    i % 3 == 0 ? violet : cyan);
            }

            // Broken monoliths create silhouette and readable navigation anchors while
            // leaving the center mechanically clean for telegraphs and dodging.
            for (int i = 0; i < 8; i++)
            {
                float angle = i / 8f * Mathf.PI * 2f + Mathf.PI / 8f;
                Vector3 radial = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                Vector3 position = new Vector3(0f, 0f, 1f) + radial * 9.55f;
                GameObject cluster = new GameObject($"FractureMonolith_{i:00}");
                cluster.transform.SetParent(root.transform, false);
                cluster.transform.position = position;
                cluster.transform.rotation = Quaternion.Euler(0f, -angle * Mathf.Rad2Deg + 90f, 0f);

                Primitive("Base", PrimitiveType.Cube, cluster.transform,
                    new Vector3(0f, 0.18f, 0f), new Vector3(1.45f, 0.32f, 1.15f), stone, false);
                GameObject shardA = Primitive("ShardA", PrimitiveType.Cube, cluster.transform,
                    new Vector3(-0.28f, 1.62f, 0f), new Vector3(0.46f, 2.75f + (i % 3) * 0.35f, 0.52f), stone, false);
                shardA.transform.localRotation = Quaternion.Euler(0f, 0f, -7f + i % 2 * 14f);
                GameObject shardB = Primitive("ShardB", PrimitiveType.Cube, cluster.transform,
                    new Vector3(0.42f, 1.05f, 0.08f), new Vector3(0.24f, 1.65f, 0.32f), metal, false);
                shardB.transform.localRotation = Quaternion.Euler(4f, 16f, 12f);
                CreateLine("FractureSeam", cluster.transform,
                    new Vector3(-0.28f, 0.38f, -0.30f),
                    new Vector3(-0.28f, 3.0f + (i % 3) * 0.35f, -0.30f),
                    0.026f,
                    i % 2 == 0 ? violet : ember,
                    false);
            }

            // Dark perimeter slabs hide the empty horizon from the tactical camera.
            for (int i = 0; i < 4; i++)
            {
                float yaw = i * 90f;
                Vector3 forward = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
                GameObject wall = Primitive($"HorizonWall_{i:00}", PrimitiveType.Cube, root.transform,
                    new Vector3(0f, 2.2f, 1f) + forward * 13.6f,
                    new Vector3(20f, 4.7f, 0.5f),
                    stone,
                    false);
                wall.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            }

            // Four restrained accent lights are enough for rim separation without
            // turning the scene into a rainbow or creating excessive realtime cost.
            for (int i = 0; i < 4; i++)
            {
                float a = (i + 0.5f) / 4f * Mathf.PI * 2f;
                GameObject go = new GameObject($"ArenaRimLight_{i:00}");
                go.transform.SetParent(root.transform, false);
                go.transform.position = new Vector3(Mathf.Cos(a) * 7.8f, 2.4f, 1f + Mathf.Sin(a) * 7.8f);
                Light light = go.AddComponent<Light>();
                light.type = LightType.Point;
                light.range = 9.5f;
                light.intensity = 1.55f;
                light.color = i % 2 == 0 ? new Color(0.18f, 0.42f, 1f) : new Color(0.58f, 0.20f, 1f);
                light.shadows = LightShadows.Soft;
            }
        }

        private static void BuildAwakening(Transform awakening, Material floor, Material stone, Material cyan, Material green)
        {
            GameObject root = new GameObject("Mindforge_Showcase_Awakening");
            root.transform.SetParent(awakening, false);
            Primitive("AwakeningDais", PrimitiveType.Cylinder, root.transform,
                new Vector3(0f, -0.26f, 0f), new Vector3(3.35f, 0.035f, 3.35f), floor, false);
            CreateCircle("ListeningHalo", root.transform, new Vector3(0f, -0.12f, 0f), 2.25f, 96, 0.045f, cyan);
            CreateCircle("CalibrationHalo", root.transform, new Vector3(0f, -0.11f, 0f), 1.35f, 72, 0.028f, green);

            for (int i = 0; i < 6; i++)
            {
                float a = i / 6f * Mathf.PI * 2f;
                Vector3 p = new Vector3(Mathf.Cos(a) * 3.55f, 1.0f, Mathf.Sin(a) * 3.55f);
                GameObject pillar = Primitive($"AwakeningFin_{i:00}", PrimitiveType.Cube, root.transform,
                    p, new Vector3(0.18f, 1.85f, 0.55f), stone, false);
                pillar.transform.rotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 0f);
            }
        }

        private static void RemoveExisting()
        {
            GameObject oldArena = GameObject.Find(ShowcaseRootName);
            if (oldArena != null) UnityEngine.Object.DestroyImmediate(oldArena);
            GameObject oldAwakening = GameObject.Find("Mindforge_Showcase_Awakening");
            if (oldAwakening != null) UnityEngine.Object.DestroyImmediate(oldAwakening);
        }

        private static GameObject Primitive(
            string name,
            PrimitiveType type,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            Material material,
            bool keepCollider)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;
            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = material;
            Collider collider = go.GetComponent<Collider>();
            if (collider != null && !keepCollider) UnityEngine.Object.DestroyImmediate(collider);
            return go;
        }

        private static void CreateCircle(string name, Transform parent, Vector3 localCenter, float radius, int points, float width, Material material)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localCenter;
            LineRenderer line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = Mathf.Max(16, points);
            line.widthMultiplier = width;
            for (int i = 0; i < line.positionCount; i++)
            {
                float a = i / (float)line.positionCount * Mathf.PI * 2f;
                line.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius));
            }
        }

        private static void CreateLine(
            string name,
            Transform parent,
            Vector3 start,
            Vector3 end,
            float width,
            Material material,
            bool world = true)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            LineRenderer line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.useWorldSpace = world;
            line.positionCount = 2;
            line.widthMultiplier = width;
            line.SetPosition(0, start);
            line.SetPosition(1, end);
        }

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Mindforge/Generated"))
                AssetDatabase.CreateFolder("Assets/Mindforge", "Generated");
            if (!AssetDatabase.IsValidFolder(Generated))
                AssetDatabase.CreateFolder("Assets/Mindforge/Generated", "Showcase");
            Directory.CreateDirectory(Generated);
        }

        private static Material EnsureLit(string file, Color color, float metallic, float smoothness)
        {
            string path = $"{Generated}/{file}";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            else material.color = color;
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material EnsureEmission(string file, Color color, float intensity)
        {
            Material material = EnsureLit(file, color * 0.28f, 0.45f, 0.72f);
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * Mathf.Max(0f, intensity));
            }
            EditorUtility.SetDirty(material);
            return material;
        }
    }
}
#endif
