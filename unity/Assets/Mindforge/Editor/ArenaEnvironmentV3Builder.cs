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
    /// Third-generation arena art pass. Replaces the prototype arena presentation with
    /// one coherent, collider-free visual environment while preserving the deterministic
    /// gameplay floor, combat colliders, calibration gate and boss/player authority.
    ///
    /// Palette: midnight/indigo stone, cyan/teal neural light and restrained copper-gold.
    /// Geometry: circular tiered floor, structured pillar rhythm, ritual channels,
    /// peripheral ruins and low-clutter atmospheric anchors.
    /// </summary>
    public static class ArenaEnvironmentV3Builder
    {
        public const string RootName = "Mindforge_Arena_V3";
        private const string GeneratedFolder = "Assets/Mindforge/Generated/ArenaV3";
        private static readonly Vector3 Center = new Vector3(0f, 0f, 1f);

        [MenuItem("Mindforge/Showcase/Apply Arena Environment V3", priority = 23)]
        public static void BuildOpenScene()
        {
            GameObject arena = EditorSceneLookup.FindIncludingInactive("Fractured_Signal_Arena");
            if (arena == null)
                throw new InvalidOperationException("Fractured_Signal_Arena is missing. Build the showcase scene first.");

            EnsureFolders();
            RemovePrototypeArenaVisuals(arena.transform);

            Material midnight = RequireCinematic("ArenaBasalt");
            Material obsidian = RequireCinematic("ObsidianArchitecture");
            Material copper = EnsureLit("ArenaCopper", new Color(0.30f, 0.145f, 0.045f), 0.94f, 0.68f);
            Material bronzeStone = EnsureLit("BronzeStone", new Color(0.115f, 0.080f, 0.055f), 0.52f, 0.38f);
            Material cyan = EnsureEmission("ArenaRuneCyan", new Color(0.025f, 0.62f, 1.00f), 3.4f, 0.28f, 0.76f);
            Material teal = EnsureEmission("ArenaRuneTeal", new Color(0.025f, 1.00f, 0.78f), 2.8f, 0.22f, 0.72f);
            Material indigo = EnsureEmission("ArenaIndigo", new Color(0.20f, 0.12f, 0.80f), 1.45f, 0.26f, 0.60f);

            GameObject root = new GameObject(RootName);
            root.transform.SetParent(arena.transform, false);

            BuildTieredFloor(root.transform, midnight, obsidian, copper, cyan, teal);
            BuildHeroPillars(root.transform, obsidian, bronzeStone, copper, cyan, teal);
            BuildBrokenPillarRhythm(root.transform, obsidian, copper, cyan);
            BuildOuterArchitecture(root.transform, obsidian, bronzeStone, copper);
            BuildBraziers(root.transform, obsidian, copper, cyan, teal);
            BuildRubbleAndFractures(root.transform, midnight, obsidian, copper, teal);
            BuildAtmosphereAndReflections(root.transform);
            ConfigureArenaLighting(root.transform);
            ConfigurePaletteAndFog();
            ConfigureRendererQuality(root.transform);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Mindforge:ArenaV3] Arena authored: tiered ritual floor, pillar rhythm, cyan/teal neural channels, copper-gold trim, ruins, braziers and cinematic lighting. Gameplay colliders unchanged.");
        }

        private static void BuildTieredFloor(
            Transform parent,
            Material midnight,
            Material obsidian,
            Material copper,
            Material cyan,
            Material teal)
        {
            // The authoritative floor already exists. These are thin visual shells only.
            Primitive("ArenaV3_OuterFloor", PrimitiveType.Cylinder, parent,
                Center + Vector3.down * 0.255f, new Vector3(9.65f, 0.030f, 9.65f), midnight);
            Primitive("ArenaV3_MiddleFloor", PrimitiveType.Cylinder, parent,
                Center + Vector3.down * 0.215f, new Vector3(7.95f, 0.030f, 7.95f), obsidian);
            Primitive("ArenaV3_CentralDais", PrimitiveType.Cylinder, parent,
                Center + Vector3.down * 0.165f, new Vector3(4.45f, 0.030f, 4.45f), midnight);

            CreateCircle("CopperBoundaryOuter", parent, Center + Vector3.down * 0.085f, 9.12f, 144, 0.050f, copper);
            CreateCircle("CopperBoundaryInner", parent, Center + Vector3.down * 0.078f, 7.55f, 128, 0.040f, copper);
            CreateCircle("NeuralRingOuter", parent, Center + Vector3.down * 0.066f, 6.52f, 128, 0.030f, cyan);
            CreateCircle("NeuralRingMid", parent, Center + Vector3.down * 0.060f, 4.62f, 112, 0.024f, teal);
            CreateCircle("CopperRitualRing", parent, Center + Vector3.down * 0.054f, 3.05f, 96, 0.040f, copper);
            CreateCircle("NeuralCoreRing", parent, Center + Vector3.down * 0.048f, 2.42f, 96, 0.020f, cyan);

            // Radial floor channels form a readable clock-face without turning the arena
            // into a neon grid. Every third spoke is copper; the rest alternate cyan/teal.
            for (int i = 0; i < 16; i++)
            {
                float a = i / 16f * Mathf.PI * 2f;
                Vector3 dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                Material mat = i % 4 == 0 ? copper : (i % 2 == 0 ? teal : cyan);
                float width = i % 4 == 0 ? 0.032f : 0.018f;
                CreateLine($"RitualChannel_{i:00}", parent,
                    Center + Vector3.down * 0.044f + dir * 3.18f,
                    Center + Vector3.down * 0.044f + dir * 8.92f,
                    width,
                    mat);
            }

            // Small copper index marks around the outer ring make orientation immediate.
            for (int i = 0; i < 24; i++)
            {
                float a = i / 24f * Mathf.PI * 2f;
                Vector3 radial = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                Vector3 tangent = new Vector3(-radial.z, 0f, radial.x);
                GameObject mark = Primitive($"CopperIndex_{i:00}", PrimitiveType.Cube, parent,
                    Center + radial * 8.55f + Vector3.down * 0.068f,
                    new Vector3(0.34f, 0.018f, i % 3 == 0 ? 0.085f : 0.045f),
                    copper);
                mark.transform.rotation = Quaternion.LookRotation(tangent, Vector3.up);
            }
        }

        private static void BuildHeroPillars(
            Transform parent,
            Material obsidian,
            Material bronzeStone,
            Material copper,
            Material cyan,
            Material teal)
        {
            // Six tall hero pillars form a deliberate visual rhythm while staying outside
            // the primary dodge/telegraph zone. Their asymmetry keeps the arena from
            // reading as sterile procedural symmetry.
            float[] angles = { 18f, 74f, 137f, 202f, 254f, 322f };
            float[] heights = { 6.8f, 8.2f, 7.4f, 8.8f, 6.5f, 7.9f };
            float[] radii = { 8.55f, 8.82f, 8.62f, 8.92f, 8.48f, 8.70f };

            for (int i = 0; i < angles.Length; i++)
            {
                float a = angles[i] * Mathf.Deg2Rad;
                Vector3 radial = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                Vector3 position = Center + radial * radii[i];
                Material rune = i % 2 == 0 ? cyan : teal;
                BuildPillarCluster(parent, $"HeroPillar_{i:00}", position, -angles[i] + 90f, heights[i],
                    obsidian, bronzeStone, copper, rune, i % 3 == 0);
            }
        }

        private static void BuildPillarCluster(
            Transform parent,
            string name,
            Vector3 position,
            float yaw,
            float height,
            Material stone,
            Material secondaryStone,
            Material copper,
            Material rune,
            bool fracturedTop)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.position = position;
            root.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            Primitive("PlinthLower", PrimitiveType.Cube, root.transform,
                new Vector3(0f, 0.16f, 0f), new Vector3(1.55f, 0.30f, 1.38f), secondaryStone);
            Primitive("PlinthUpper", PrimitiveType.Cube, root.transform,
                new Vector3(0f, 0.44f, 0f), new Vector3(1.18f, 0.24f, 1.02f), copper);
            Primitive("Shaft", PrimitiveType.Cube, root.transform,
                new Vector3(0f, 0.72f + height * 0.5f, 0f), new Vector3(0.82f, height, 0.72f), stone);
            Primitive("ShaftInset", PrimitiveType.Cube, root.transform,
                new Vector3(0f, 0.72f + height * 0.50f, -0.372f), new Vector3(0.46f, height * 0.84f, 0.035f), secondaryStone);

            // Twin copper vertical rails frame the rune channel.
            Primitive("CopperRailL", PrimitiveType.Cube, root.transform,
                new Vector3(-0.31f, 0.72f + height * 0.50f, -0.397f), new Vector3(0.045f, height * 0.88f, 0.030f), copper);
            Primitive("CopperRailR", PrimitiveType.Cube, root.transform,
                new Vector3(0.31f, 0.72f + height * 0.50f, -0.397f), new Vector3(0.045f, height * 0.88f, 0.030f), copper);
            Primitive("RuneChannel", PrimitiveType.Cube, root.transform,
                new Vector3(0f, 0.72f + height * 0.50f, -0.420f), new Vector3(0.060f, height * 0.72f, 0.026f), rune);

            float capY = 0.72f + height + 0.18f;
            GameObject cap = Primitive("Capital", PrimitiveType.Cube, root.transform,
                new Vector3(0f, capY, 0f), new Vector3(1.18f, 0.30f, 1.02f), copper);
            if (fracturedTop) cap.transform.localRotation = Quaternion.Euler(0f, 0f, 7f);
            Primitive("Crown", PrimitiveType.Cube, root.transform,
                new Vector3(fracturedTop ? 0.10f : 0f, capY + 0.31f, 0f),
                new Vector3(0.84f, 0.28f, 0.74f), stone);
        }

        private static void BuildBrokenPillarRhythm(
            Transform parent,
            Material stone,
            Material copper,
            Material cyan)
        {
            System.Random random = new System.Random(812733);
            for (int i = 0; i < 10; i++)
            {
                float a = (i / 10f * Mathf.PI * 2f) + 0.22f;
                float radius = 10.15f + (i % 2) * 0.48f;
                float h = Mathf.Lerp(1.7f, 4.3f, (float)random.NextDouble());
                Vector3 radial = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                Vector3 p = Center + radial * radius;
                GameObject root = new GameObject($"BrokenPillar_{i:00}");
                root.transform.SetParent(parent, false);
                root.transform.position = p;
                root.transform.rotation = Quaternion.Euler(
                    Mathf.Lerp(-4f, 4f, (float)random.NextDouble()),
                    -a * Mathf.Rad2Deg + 90f,
                    Mathf.Lerp(-7f, 7f, (float)random.NextDouble()));

                Primitive("Base", PrimitiveType.Cube, root.transform,
                    new Vector3(0f, 0.12f, 0f), new Vector3(1.05f, 0.22f, 0.90f), copper);
                Primitive("BrokenShaft", PrimitiveType.Cube, root.transform,
                    new Vector3(0f, 0.40f + h * 0.5f, 0f), new Vector3(0.56f, h, 0.50f), stone);
                if (i % 3 == 0)
                    Primitive("ResidualRune", PrimitiveType.Cube, root.transform,
                        new Vector3(0f, 0.45f + h * 0.46f, -0.265f), new Vector3(0.045f, h * 0.48f, 0.020f), cyan);
            }
        }

        private static void BuildOuterArchitecture(
            Transform parent,
            Material stone,
            Material secondaryStone,
            Material copper)
        {
            // Segmented outer boundary suggests a larger ruined complex without putting a
            // solid wall directly behind every camera angle.
            for (int i = 0; i < 28; i++)
            {
                float a = i / 28f * Mathf.PI * 2f;
                float radius = 12.15f;
                Vector3 radial = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                Vector3 tangent = new Vector3(-radial.z, 0f, radial.x);
                float gap = i % 7 == 0 ? 0.45f : 1f;
                GameObject segment = Primitive($"OuterParapet_{i:00}", PrimitiveType.Cube, parent,
                    Center + radial * radius + Vector3.up * 0.48f,
                    new Vector3(2.15f * gap, 0.82f, 0.58f),
                    i % 5 == 0 ? secondaryStone : stone);
                segment.transform.rotation = Quaternion.LookRotation(radial, Vector3.up);

                if (i % 4 == 0)
                {
                    GameObject trim = Primitive($"OuterCopperCap_{i:00}", PrimitiveType.Cube, parent,
                        Center + radial * (radius - 0.02f) + Vector3.up * 0.93f,
                        new Vector3(1.75f, 0.08f, 0.62f), copper);
                    trim.transform.rotation = Quaternion.LookRotation(radial, Vector3.up);
                }
            }
        }

        private static void BuildBraziers(
            Transform parent,
            Material stone,
            Material copper,
            Material cyan,
            Material teal)
        {
            float[] angles = { 45f, 112f, 178f, 232f, 300f, 350f };
            for (int i = 0; i < angles.Length; i++)
            {
                float a = angles[i] * Mathf.Deg2Rad;
                Vector3 radial = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                Vector3 p = Center + radial * 10.92f;
                Material energy = i % 2 == 0 ? cyan : teal;

                GameObject root = new GameObject($"NeuralBrazier_{i:00}");
                root.transform.SetParent(parent, false);
                root.transform.position = p;
                Primitive("Pedestal", PrimitiveType.Cylinder, root.transform,
                    new Vector3(0f, 0.20f, 0f), new Vector3(0.55f, 0.20f, 0.55f), stone);
                Primitive("CopperBowl", PrimitiveType.Cylinder, root.transform,
                    new Vector3(0f, 0.48f, 0f), new Vector3(0.42f, 0.08f, 0.42f), copper);
                Primitive("EnergyCore", PrimitiveType.Sphere, root.transform,
                    new Vector3(0f, 0.72f, 0f), new Vector3(0.22f, 0.38f, 0.22f), energy);
                Primitive("EnergyWisp", PrimitiveType.Sphere, root.transform,
                    new Vector3(0.06f, 1.00f, -0.02f), new Vector3(0.11f, 0.27f, 0.11f), energy);

                GameObject lightObject = new GameObject("BrazierLight");
                lightObject.transform.SetParent(root.transform, false);
                lightObject.transform.localPosition = new Vector3(0f, 0.95f, 0f);
                Light light = lightObject.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = i % 2 == 0 ? new Color(0.05f, 0.58f, 1.0f) : new Color(0.04f, 1.0f, 0.72f);
                light.intensity = 1.6f;
                light.range = 5.6f;
                light.shadows = LightShadows.None;
            }
        }

        private static void BuildRubbleAndFractures(
            Transform parent,
            Material midnight,
            Material stone,
            Material copper,
            Material teal)
        {
            System.Random random = new System.Random(44201);
            for (int i = 0; i < 38; i++)
            {
                float a = (float)(random.NextDouble() * Math.PI * 2.0);
                float radius = Mathf.Lerp(9.55f, 11.85f, Mathf.Pow((float)random.NextDouble(), 0.72f));
                Vector3 radial = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                float s = Mathf.Lerp(0.12f, 0.55f, Mathf.Pow((float)random.NextDouble(), 1.7f));
                GameObject rock = Primitive($"ArenaDebris_{i:00}", i % 5 == 0 ? PrimitiveType.Sphere : PrimitiveType.Cube,
                    parent,
                    Center + radial * radius + Vector3.up * (s * 0.28f),
                    new Vector3(s * Mathf.Lerp(0.75f, 1.55f, (float)random.NextDouble()), s * 0.55f, s),
                    i % 8 == 0 ? copper : (i % 3 == 0 ? midnight : stone));
                rock.transform.rotation = Quaternion.Euler(
                    Mathf.Lerp(-26f, 26f, (float)random.NextDouble()),
                    Mathf.Lerp(0f, 360f, (float)random.NextDouble()),
                    Mathf.Lerp(-26f, 26f, (float)random.NextDouble()));
            }

            // A few irregular glowing fractures interrupt the perfect radial geometry.
            for (int i = 0; i < 7; i++)
            {
                float a = (i / 7f * Mathf.PI * 2f) + 0.37f;
                Vector3 radial = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                Vector3 tangent = new Vector3(-radial.z, 0f, radial.x);
                Vector3 start = Center + radial * 5.0f + tangent * (i % 2 == 0 ? 0.22f : -0.18f) + Vector3.down * 0.038f;
                Vector3 end = Center + radial * 7.35f + tangent * (i % 3 - 1) * 0.45f + Vector3.down * 0.038f;
                CreateLine($"TealFloorFracture_{i:00}", parent, start, end, 0.022f, teal);
            }
        }

        private static void BuildAtmosphereAndReflections(Transform parent)
        {
            GameObject probeObject = new GameObject("ArenaV3ReflectionProbe");
            probeObject.transform.SetParent(parent, false);
            probeObject.transform.position = Center + Vector3.up * 2.4f;
            ReflectionProbe probe = probeObject.AddComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Realtime;
            probe.refreshMode = ReflectionProbeRefreshMode.OnAwake;
            probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.AllFacesAtOnce;
            probe.hdr = true;
            probe.resolution = 256;
            probe.intensity = 0.88f;
            probe.boxProjection = true;
            probe.size = new Vector3(25f, 12f, 25f);
            probe.center = Vector3.zero;
            probe.nearClipPlane = 0.25f;
            probe.farClipPlane = 38f;
            probe.shadowDistance = 30f;
        }

        private static void ConfigureArenaLighting(Transform parent)
        {
            Light key = EditorSceneLookup.FindIncludingInactive("KeyLight")?.GetComponent<Light>();
            if (key != null)
            {
                key.type = LightType.Directional;
                key.color = new Color(0.93f, 0.83f, 0.70f);
                key.intensity = 1.10f;
                key.shadows = LightShadows.Soft;
                key.shadowStrength = 0.90f;
                key.shadowBias = 0.035f;
                key.shadowNormalBias = 0.24f;
                key.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
                key.useColorTemperature = true;
                key.colorTemperature = 4700f;
                RenderSettings.sun = key;
            }

            // Indigo fill separates silhouettes without bleaching the midnight palette.
            GameObject fillObject = new GameObject("ArenaV3IndigoFill");
            fillObject.transform.SetParent(parent, false);
            fillObject.transform.position = Center + new Vector3(-5.5f, 6.0f, -5.2f);
            fillObject.transform.LookAt(Center + Vector3.up);
            Light fill = fillObject.AddComponent<Light>();
            fill.type = LightType.Spot;
            fill.color = new Color(0.15f, 0.18f, 0.72f);
            fill.intensity = 2.2f;
            fill.range = 20f;
            fill.spotAngle = 58f;
            fill.innerSpotAngle = 34f;
            fill.shadows = LightShadows.None;

            GameObject cyanRimObject = new GameObject("ArenaV3CyanRim");
            cyanRimObject.transform.SetParent(parent, false);
            cyanRimObject.transform.position = Center + new Vector3(6.2f, 5.0f, 5.8f);
            cyanRimObject.transform.LookAt(Center + Vector3.up * 0.8f);
            Light cyanRim = cyanRimObject.AddComponent<Light>();
            cyanRim.type = LightType.Spot;
            cyanRim.color = new Color(0.03f, 0.55f, 1f);
            cyanRim.intensity = 2.5f;
            cyanRim.range = 18f;
            cyanRim.spotAngle = 50f;
            cyanRim.innerSpotAngle = 28f;
            cyanRim.shadows = LightShadows.None;
        }

        private static void ConfigurePaletteAndFog()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.0080f;
            RenderSettings.fogColor = new Color(0.008f, 0.018f, 0.040f);
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.030f, 0.060f, 0.145f);
            RenderSettings.ambientEquatorColor = new Color(0.022f, 0.035f, 0.085f);
            RenderSettings.ambientGroundColor = new Color(0.005f, 0.008f, 0.018f);
            RenderSettings.reflectionIntensity = 0.80f;
        }

        private static void ConfigureRendererQuality(Transform root)
        {
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.BlendProbesAndSkybox;
                renderer.lightProbeUsage = LightProbeUsage.BlendProbes;
            }
        }

        private static void RemovePrototypeArenaVisuals(Transform arena)
        {
            string[] names = { ShowcaseSceneDecorator.ShowcaseRootName, CinematicSceneDetailer.CinematicRootName, RootName };
            foreach (string name in names)
            {
                Transform child = FindChildRecursive(arena, name);
                if (child != null) UnityEngine.Object.DestroyImmediate(child.gameObject);
            }
        }

        private static Transform FindChildRecursive(Transform root, string name)
        {
            if (root == null) return null;
            foreach (Transform child in root)
            {
                if (child.name == name) return child;
                Transform nested = FindChildRecursive(child, name);
                if (nested != null) return nested;
            }
            return null;
        }

        private static Material RequireCinematic(string name)
        {
            Material material = CinematicMaterialAuthoring.Load(name);
            if (material == null)
                throw new InvalidOperationException($"Cinematic material {name} was not authored.");
            return material;
        }

        private static Material EnsureLit(string name, Color color, float metallic, float smoothness)
        {
            string path = $"{GeneratedFolder}/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                if (shader == null) throw new InvalidOperationException("No URP/Lit or Standard shader available for Arena V3.");
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            else material.color = color;
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material EnsureEmission(string name, Color color, float emission, float metallic, float smoothness)
        {
            Material material = EnsureLit(name, color * 0.18f, metallic, smoothness);
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * emission);
            }
            EditorUtility.SetDirty(material);
            return material;
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

        private static void CreateCircle(
            string name,
            Transform parent,
            Vector3 center,
            float radius,
            int points,
            float width,
            Material material)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            LineRenderer line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.useWorldSpace = true;
            line.loop = true;
            line.positionCount = Mathf.Max(24, points);
            line.widthMultiplier = width;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            for (int i = 0; i < line.positionCount; i++)
            {
                float a = i / (float)line.positionCount * Mathf.PI * 2f;
                line.SetPosition(i, center + new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius));
            }
        }

        private static void CreateLine(
            string name,
            Transform parent,
            Vector3 start,
            Vector3 end,
            float width,
            Material material)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            LineRenderer line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.widthMultiplier = width;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.SetPosition(0, start);
            line.SetPosition(1, end);
        }

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Mindforge/Generated"))
                AssetDatabase.CreateFolder("Assets/Mindforge", "Generated");
            if (!AssetDatabase.IsValidFolder(GeneratedFolder))
                AssetDatabase.CreateFolder("Assets/Mindforge/Generated", "ArenaV3");
            Directory.CreateDirectory(GeneratedFolder);
        }
    }
}
#endif
