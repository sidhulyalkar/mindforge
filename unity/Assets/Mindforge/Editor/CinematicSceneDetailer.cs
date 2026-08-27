#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mindforge.Editor
{
    /// <summary>
    /// Second-stage visual pass over the deterministic showcase scene. It adds surface
    /// variation, silhouette breakup, reflections and cinematic lighting while keeping
    /// all new geometry collider-free so gameplay authority remains unchanged.
    /// </summary>
    public static class CinematicSceneDetailer
    {
        public const string CinematicRootName = "Mindforge_Cinematic_Fidelity";

        [MenuItem("Mindforge/Showcase/Apply Cinematic Environment Detail", priority = 22)]
        public static void EnhanceOpenScene()
        {
            CinematicMaterialAuthoring.EnsureAuthored();
            RemoveExisting();

            GameObject arena = GameObject.Find("Fractured_Signal_Arena");
            if (arena == null)
                throw new InvalidOperationException("Fractured_Signal_Arena is missing. Build the showcase scene first.");

            Material basalt = Require("ArenaBasalt");
            Material stone = Require("ObsidianArchitecture");
            Material metal = Require("GuardianMetal");
            Material violet = Require("FractureViolet");

            ApplyMaterialByName(arena.transform, "DuelFloor", basalt);
            ApplyMaterialByName(arena.transform, "InnerDais", metal);
            ApplyMaterialContains(arena.transform, "FractureMonolith", stone);
            ApplyMaterialContains(arena.transform, "HorizonWall", stone);
            ApplyMaterialContains(arena.transform, "ArenaPillar", stone);

            GameObject root = new GameObject(CinematicRootName);
            root.transform.SetParent(arena.transform, false);

            BuildFracturedGround(root.transform, basalt, stone, metal);
            BuildPeripheralRubble(root.transform, basalt, stone);
            BuildRuinedSilhouette(root.transform, stone, metal, violet);
            BuildReflectionVolume(root.transform);
            ConfigureLighting(root.transform);
            ConfigureSkyAndFog();
            ConfigureRendererProbeUsage(arena.transform);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("[Mindforge:Cinematic] Environment fidelity applied: mapped PBR surfaces, debris breakup, ruin silhouette, reflections and filmic light hierarchy.");
        }

        private static void BuildFracturedGround(Transform parent, Material basalt, Material stone, Material metal)
        {
            System.Random random = new System.Random(41027);
            for (int i = 0; i < 34; i++)
            {
                float angle = (float)(random.NextDouble() * Math.PI * 2.0);
                float radius = Mathf.Lerp(5.9f, 9.1f, (float)random.NextDouble());
                Vector3 radial = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                Vector3 tangent = new Vector3(-radial.z, 0f, radial.x);
                Vector3 position = new Vector3(0f, -0.095f, 1f) + radial * radius + tangent * Mathf.Lerp(-0.34f, 0.34f, (float)random.NextDouble());
                Vector3 scale = new Vector3(
                    Mathf.Lerp(0.28f, 1.05f, (float)random.NextDouble()),
                    Mathf.Lerp(0.018f, 0.055f, (float)random.NextDouble()),
                    Mathf.Lerp(0.55f, 1.65f, (float)random.NextDouble()));
                Material material = i % 9 == 0 ? metal : i % 4 == 0 ? stone : basalt;
                GameObject plate = Primitive($"FracturedFloorPlate_{i:00}", PrimitiveType.Cube, parent, position, scale, material);
                plate.transform.rotation = Quaternion.Euler(
                    Mathf.Lerp(-2.2f, 2.2f, (float)random.NextDouble()),
                    -angle * Mathf.Rad2Deg + Mathf.Lerp(-22f, 22f, (float)random.NextDouble()),
                    Mathf.Lerp(-1.5f, 1.5f, (float)random.NextDouble()));
            }
        }

        private static void BuildPeripheralRubble(Transform parent, Material basalt, Material stone)
        {
            System.Random random = new System.Random(99281);
            for (int i = 0; i < 58; i++)
            {
                float angle = (float)(random.NextDouble() * Math.PI * 2.0);
                float radius = Mathf.Lerp(9.0f, 12.2f, Mathf.Pow((float)random.NextDouble(), 0.64f));
                Vector3 position = new Vector3(Mathf.Cos(angle) * radius, -0.02f, 1f + Mathf.Sin(angle) * radius);
                float size = Mathf.Lerp(0.16f, 0.62f, Mathf.Pow((float)random.NextDouble(), 1.8f));
                GameObject rock = Primitive(
                    $"Rubble_{i:00}",
                    i % 3 == 0 ? PrimitiveType.Sphere : PrimitiveType.Cube,
                    parent,
                    position,
                    new Vector3(size * Mathf.Lerp(0.65f, 1.55f, (float)random.NextDouble()), size * 0.72f, size * Mathf.Lerp(0.75f, 1.45f, (float)random.NextDouble())),
                    i % 5 == 0 ? basalt : stone);
                rock.transform.rotation = Quaternion.Euler(
                    Mathf.Lerp(-28f, 28f, (float)random.NextDouble()),
                    Mathf.Lerp(0f, 360f, (float)random.NextDouble()),
                    Mathf.Lerp(-28f, 28f, (float)random.NextDouble()));
            }
        }

        private static void BuildRuinedSilhouette(Transform parent, Material stone, Material metal, Material violet)
        {
            System.Random random = new System.Random(7719);
            for (int i = 0; i < 16; i++)
            {
                float angle = i / 16f * Mathf.PI * 2f + 0.11f;
                float radius = i % 2 == 0 ? 12.5f : 13.3f;
                float height = Mathf.Lerp(4.5f, 9.5f, (float)random.NextDouble());
                Vector3 position = new Vector3(Mathf.Cos(angle) * radius, height * 0.5f - 0.3f, 1f + Mathf.Sin(angle) * radius);
                GameObject fin = Primitive(
                    $"CathedralRuin_{i:00}",
                    PrimitiveType.Cube,
                    parent,
                    position,
                    new Vector3(Mathf.Lerp(0.22f, 0.55f, (float)random.NextDouble()), height, Mathf.Lerp(0.45f, 1.20f, (float)random.NextDouble())),
                    i % 5 == 0 ? metal : stone);
                fin.transform.rotation = Quaternion.Euler(
                    Mathf.Lerp(-8f, 8f, (float)random.NextDouble()),
                    -angle * Mathf.Rad2Deg + 90f,
                    Mathf.Lerp(-9f, 9f, (float)random.NextDouble()));

                if (i % 4 == 0)
                {
                    GameObject seam = Primitive(
                        $"RuinEnergySeam_{i:00}",
                        PrimitiveType.Cube,
                        parent,
                        position + Vector3.up * Mathf.Lerp(-0.4f, 0.6f, (float)random.NextDouble()),
                        new Vector3(0.032f, height * 0.72f, 0.035f),
                        violet);
                    seam.transform.rotation = fin.transform.rotation;
                }
            }
        }

        private static void BuildReflectionVolume(Transform parent)
        {
            GameObject go = new GameObject("ArenaReflectionProbe");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(0f, 1.45f, 1f);
            ReflectionProbe probe = go.AddComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Realtime;
            probe.refreshMode = ReflectionProbeRefreshMode.OnAwake;
            probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.AllFacesAtOnce;
            probe.hdr = true;
            probe.resolution = 256;
            probe.intensity = 0.82f;
            probe.boxProjection = true;
            probe.size = new Vector3(23f, 8f, 23f);
            probe.center = new Vector3(0f, 1.1f, 0f);
            probe.nearClipPlane = 0.2f;
            probe.farClipPlane = 35f;
            probe.shadowDistance = 32f;
        }

        private static void ConfigureLighting(Transform parent)
        {
            Light key = GameObject.Find("KeyLight")?.GetComponent<Light>();
            if (key != null)
            {
                key.type = LightType.Directional;
                key.color = new Color(1.00f, 0.88f, 0.76f);
                key.intensity = 1.35f;
                key.shadows = LightShadows.Soft;
                key.shadowStrength = 0.92f;
                key.shadowBias = 0.035f;
                key.shadowNormalBias = 0.26f;
                key.transform.rotation = Quaternion.Euler(44f, -36f, 0f);
                key.useColorTemperature = true;
                key.colorTemperature = 5200f;
                RenderSettings.sun = key;
            }

            Color[] colors =
            {
                new Color(0.10f, 0.28f, 1f),
                new Color(0.45f, 0.10f, 1f),
                new Color(1f, 0.10f, 0.055f),
            };
            Vector3[] positions =
            {
                new Vector3(-7.8f, 3.8f, -5.2f),
                new Vector3(7.2f, 4.4f, 5.8f),
                new Vector3(0f, 5.6f, 10.8f),
            };
            for (int i = 0; i < positions.Length; i++)
            {
                GameObject go = new GameObject($"CinematicRim_{i:00}");
                go.transform.SetParent(parent, false);
                go.transform.position = positions[i];
                go.transform.LookAt(new Vector3(0f, 0.8f, 1f));
                Light light = go.AddComponent<Light>();
                light.type = LightType.Spot;
                light.color = colors[i];
                light.intensity = i == 2 ? 3.0f : 2.2f;
                light.range = 17f;
                light.spotAngle = i == 2 ? 62f : 48f;
                light.innerSpotAngle = light.spotAngle * 0.54f;
                light.shadows = i == 2 ? LightShadows.Soft : LightShadows.None;
            }
        }

        private static void ConfigureSkyAndFog()
        {
            string path = $"{CinematicMaterialAuthoring.ResourceFolder}/CinematicSkybox.mat";
            Material sky = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find("Skybox/Procedural");
            if (sky == null && shader != null)
            {
                sky = new Material(shader) { name = "CinematicSkybox" };
                AssetDatabase.CreateAsset(sky, path);
            }
            if (sky != null)
            {
                if (sky.HasProperty("_SkyTint")) sky.SetColor("_SkyTint", new Color(0.075f, 0.095f, 0.165f));
                if (sky.HasProperty("_GroundColor")) sky.SetColor("_GroundColor", new Color(0.010f, 0.012f, 0.020f));
                if (sky.HasProperty("_AtmosphereThickness")) sky.SetFloat("_AtmosphereThickness", 0.38f);
                if (sky.HasProperty("_Exposure")) sky.SetFloat("_Exposure", 0.62f);
                if (sky.HasProperty("_SunSize")) sky.SetFloat("_SunSize", 0.018f);
                EditorUtility.SetDirty(sky);
                RenderSettings.skybox = sky;
            }

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.0074f;
            RenderSettings.fogColor = new Color(0.016f, 0.020f, 0.033f);
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.070f, 0.085f, 0.135f);
            RenderSettings.ambientEquatorColor = new Color(0.028f, 0.030f, 0.047f);
            RenderSettings.ambientGroundColor = new Color(0.006f, 0.007f, 0.010f);
            RenderSettings.reflectionIntensity = 0.82f;
        }

        private static void ConfigureRendererProbeUsage(Transform root)
        {
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.BlendProbesAndSkybox;
                renderer.lightProbeUsage = LightProbeUsage.BlendProbes;
            }
        }

        private static void ApplyMaterialByName(Transform root, string name, Material material)
        {
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
                if (renderer.gameObject.name == name) renderer.sharedMaterial = material;
        }

        private static void ApplyMaterialContains(Transform root, string token, Material material)
        {
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                bool selfMatches = renderer.gameObject.name.Contains(token);
                bool parentMatches = renderer.transform.parent != null && renderer.transform.parent.name.Contains(token);
                if (selfMatches || parentMatches) renderer.sharedMaterial = material;
            }
        }

        private static GameObject Primitive(
            string name,
            PrimitiveType type,
            Transform parent,
            Vector3 position,
            Vector3 scale,
            Material material)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = scale;
            Collider collider = go.GetComponent<Collider>();
            if (collider != null) UnityEngine.Object.DestroyImmediate(collider);
            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.BlendProbesAndSkybox;
            }
            return go;
        }

        private static Material Require(string name)
        {
            Material material = CinematicMaterialAuthoring.Load(name);
            if (material == null) throw new InvalidOperationException($"Cinematic material {name} was not authored.");
            return material;
        }

        private static void RemoveExisting()
        {
            GameObject existing = GameObject.Find(CinematicRootName);
            if (existing != null) UnityEngine.Object.DestroyImmediate(existing);
        }
    }
}
#endif
