#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mindforge.Editor
{
    /// <summary>
    /// Converts the showcase from a corridor suspended over a void into a bounded,
    /// collision-backed world. The main route remains intact, while a reusable tile kit
    /// adds climbable terraces, stairs, ramps, bridges and dense perimeter architecture.
    /// Decorative depth sits outside the collision shell so the playable world feels much
    /// larger than its actual safe bounds without introducing new fall edges.
    /// </summary>
    public static class GroundedWorldV1Builder
    {
        public const string RootName = "Mindforge_GroundedWorld_V1";
        public const float MinX = -38f;
        public const float MaxX = 38f;
        public const float MinZ = -78f;
        public const float MaxZ = 31f;
        private const float BaseFloorY = -0.72f;
        private const float WallHeight = 11.5f;

        private static readonly StaticEditorFlags WorldStatic =
            StaticEditorFlags.BatchingStatic |
            StaticEditorFlags.OccluderStatic |
            StaticEditorFlags.OccludeeStatic |
            StaticEditorFlags.ReflectionProbeStatic;

        private static readonly StaticEditorFlags VisualStatic =
            StaticEditorFlags.BatchingStatic |
            StaticEditorFlags.OccludeeStatic |
            StaticEditorFlags.ReflectionProbeStatic;

        [MenuItem("Mindforge/Legacy/Showcase/Apply Grounded World V1", priority = 24)]
        public static void ApplyOpenScene()
        {
            GameObject ward = EditorSceneLookup.FindIncludingInactive(NullWardSceneBuilder.RootName);
            GameObject arena = EditorSceneLookup.FindIncludingInactive("Fractured_Signal_Arena");
            if (ward == null || arena == null)
                throw new InvalidOperationException("Grounded World V1 requires the Null Ward and Fractured Signal arena.");

            GameObject previous = EditorSceneLookup.FindIncludingInactive(RootName);
            if (previous != null) UnityEngine.Object.DestroyImmediate(previous);

            CinematicMaterialAuthoring.EnsureAuthored();
            Material basalt = RequireMaterial("ArenaBasalt");
            Material obsidian = RequireMaterial("ObsidianArchitecture");
            Material metal = RequireMaterial("GuardianMetal");
            Material cyan = RequireMaterial("AetherCyan");
            Material green = RequireMaterial("WispVerdant");
            Material violet = RequireMaterial("FracturedRing");

            GameObject root = new GameObject(RootName);
            BuildGroundBasin(root.transform, basalt, obsidian, metal);
            BuildPerimeter(root.transform, obsidian, metal, cyan, violet);
            BuildVerticalDistricts(root.transform, basalt, obsidian, metal, cyan, green, violet);
            BuildDistantWorld(root.transform, obsidian, metal, cyan, violet);
            ConfigureAtmosphere();

            EditorUtility.SetDirty(root);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[Mindforge:GroundedWorldV1] Installed a continuous collision-backed ground basin, 11.5 m enclosing wall, " +
                "modular climbable terraces/stairs/ramps/bridges and fog-layered skyline. Reachable space no longer exposes the void.");
        }

        private static void BuildGroundBasin(Transform parent, Material basalt, Material obsidian, Material metal)
        {
            Transform root = Zone(parent, "World_GroundBasin");
            float width = MaxX - MinX;
            float depth = MaxZ - MinZ;
            Vector3 center = new Vector3((MinX + MaxX) * 0.5f, BaseFloorY, (MinZ + MaxZ) * 0.5f);

            Primitive("WorldBedrock", PrimitiveType.Cube, root, center,
                new Vector3(width, 1.25f, depth), basalt, true);

            // Broad inset plates give the world a readable authored floor even when the
            // older corridor geometry opens up. They overlap slightly so there are no seams.
            const float plate = 9.5f;
            int xCount = Mathf.CeilToInt(width / plate);
            int zCount = Mathf.CeilToInt(depth / plate);
            for (int x = 0; x < xCount; x++)
            {
                for (int z = 0; z < zCount; z++)
                {
                    float px = MinX + plate * 0.5f + x * plate;
                    float pz = MinZ + plate * 0.5f + z * plate;
                    Material material = (x + z) % 3 == 0 ? obsidian : basalt;
                    Primitive($"GroundPlate_{x:00}_{z:00}", PrimitiveType.Cube, root,
                        new Vector3(px, 0.01f, pz),
                        new Vector3(plate + 0.10f, 0.16f, plate + 0.10f), material, true);

                    if ((x + z) % 2 == 0)
                    {
                        Primitive($"GroundInlay_{x:00}_{z:00}", PrimitiveType.Cube, root,
                            new Vector3(px, 0.105f, pz),
                            new Vector3(plate * 0.56f, 0.025f, 0.055f), metal, false);
                    }
                }
            }
        }

        private static void BuildPerimeter(Transform parent, Material obsidian, Material metal, Material cyan, Material violet)
        {
            Transform root = Zone(parent, "World_Perimeter");
            float centerZ = (MinZ + MaxZ) * 0.5f;
            float centerX = (MinX + MaxX) * 0.5f;
            float depth = MaxZ - MinZ;
            float width = MaxX - MinX;

            BuildWallRun(root, "WestWall", new Vector3(MinX - 0.8f, WallHeight * 0.5f - 0.15f, centerZ),
                new Vector3(1.8f, WallHeight, depth + 3.4f), depth, false, obsidian, metal, cyan);
            BuildWallRun(root, "EastWall", new Vector3(MaxX + 0.8f, WallHeight * 0.5f - 0.15f, centerZ),
                new Vector3(1.8f, WallHeight, depth + 3.4f), depth, false, obsidian, metal, violet);
            BuildWallRun(root, "SouthWall", new Vector3(centerX, WallHeight * 0.5f - 0.15f, MinZ - 0.8f),
                new Vector3(width + 3.4f, WallHeight, 1.8f), width, true, obsidian, metal, cyan);
            BuildWallRun(root, "NorthWall", new Vector3(centerX, WallHeight * 0.5f - 0.15f, MaxZ + 0.8f),
                new Vector3(width + 3.4f, WallHeight, 1.8f), width, true, obsidian, metal, violet);
        }

        private static void BuildWallRun(
            Transform parent,
            string name,
            Vector3 center,
            Vector3 size,
            float runLength,
            bool alongX,
            Material obsidian,
            Material metal,
            Material signal)
        {
            Transform root = Zone(parent, name);
            Primitive(name + "_CollisionShell", PrimitiveType.Cube, root, center, size, obsidian, true);

            int count = Mathf.Max(2, Mathf.FloorToInt(runLength / 5.4f));
            for (int i = 0; i <= count; i++)
            {
                float t = i / (float)count;
                Vector3 p = center;
                if (alongX) p.x = Mathf.Lerp(MinX, MaxX, t);
                else p.z = Mathf.Lerp(MinZ, MaxZ, t);

                Primitive($"{name}_Buttress_{i:00}", PrimitiveType.Cube, root,
                    p + (alongX ? Vector3.back : Vector3.left) * 0.85f + Vector3.up * 0.45f,
                    alongX ? new Vector3(1.05f, WallHeight + 1.5f, 2.15f) : new Vector3(2.15f, WallHeight + 1.5f, 1.05f),
                    metal, true);
                Primitive($"{name}_Signal_{i:00}", PrimitiveType.Cube, root,
                    p + (alongX ? Vector3.back : Vector3.left) * 1.13f + Vector3.up * 1.1f,
                    alongX ? new Vector3(0.10f, WallHeight * 0.58f, 0.055f) : new Vector3(0.055f, WallHeight * 0.58f, 0.10f),
                    signal, false);
                Primitive($"{name}_Crown_{i:00}", PrimitiveType.Cube, root,
                    new Vector3(p.x, WallHeight + 0.25f + (i % 3) * 0.32f, p.z),
                    alongX ? new Vector3(2.6f, 0.34f, 2.15f) : new Vector3(2.15f, 0.34f, 2.6f),
                    obsidian, false, new Vector3(0f, i * 7f, 0f));
            }
        }

        private static void BuildVerticalDistricts(
            Transform parent,
            Material basalt,
            Material obsidian,
            Material metal,
            Material cyan,
            Material green,
            Material violet)
        {
            Transform root = Zone(parent, "World_VerticalDistricts");

            // Each district uses the same reusable tile grammar but a different stack.
            // The central corridor remains open; climbable play grows sideways/upward.
            BuildTerraceCluster(root, "WestForgeTerraces", new Vector3(-12.5f, 0f, -56f), -1,
                new[] { 0.35f, 1.15f, 2.05f, 3.00f }, basalt, metal, cyan);
            BuildTerraceCluster(root, "EastCausewayTerraces", new Vector3(12.5f, 0f, -43f), 1,
                new[] { 0.55f, 1.45f, 2.30f, 3.25f }, obsidian, metal, green);
            BuildTerraceCluster(root, "WestMarketTerraces", new Vector3(-14.5f, 0f, -29f), -1,
                new[] { 0.40f, 1.30f, 2.25f, 3.35f }, basalt, metal, violet);
            BuildTerraceCluster(root, "EastCourtTerraces", new Vector3(14.0f, 0f, -18f), 1,
                new[] { 0.65f, 1.55f, 2.45f, 3.55f }, obsidian, metal, cyan);
            BuildTerraceCluster(root, "WestCathedralTerraces", new Vector3(-13.0f, 0f, -7f), -1,
                new[] { 0.45f, 1.25f, 2.10f, 3.10f }, basalt, metal, green);
            BuildTerraceCluster(root, "ArenaOuterTerraces", new Vector3(13.8f, 0f, 6.0f), 1,
                new[] { 0.35f, 1.20f, 2.15f, 3.25f }, obsidian, metal, violet);

            // High cross-world bridges reward double-jump/air-dash exploration while the
            // stairs beneath guarantee every district still has a conventional route.
            BuildBridge(root, "MarketSkybridge", new Vector3(0f, 3.45f, -27.5f), 22f, 3.2f, metal, cyan);
            BuildBridge(root, "CourtSkybridge", new Vector3(0f, 3.65f, -17.0f), 20f, 3.1f, metal, violet);
        }

        private static void BuildTerraceCluster(
            Transform parent,
            string name,
            Vector3 origin,
            int side,
            float[] heights,
            Material body,
            Material metal,
            Material accent)
        {
            Transform root = Zone(parent, name);
            float direction = side < 0 ? -1f : 1f;

            for (int i = 0; i < heights.Length; i++)
            {
                float y = heights[i];
                float x = origin.x + direction * i * 3.65f;
                float z = origin.z + ((i % 2 == 0) ? -1.55f : 1.35f);
                Vector3 size = new Vector3(6.1f - i * 0.35f, Mathf.Max(0.45f, y * 2f), 5.2f - i * 0.18f);
                CreateArchitecturalTile(root, $"Tile_{i:00}", new Vector3(x, y - size.y * 0.5f + 0.18f, z), size, body, metal, accent, i);

                if (i > 0)
                {
                    Vector3 from = new Vector3(origin.x + direction * (i - 1) * 3.65f, heights[i - 1] + 0.18f, origin.z + (((i - 1) % 2 == 0) ? -1.55f : 1.35f));
                    Vector3 to = new Vector3(x, y + 0.18f, z);
                    CreateStairRun(root, $"Stairs_{i:00}", from, to, 7, body, accent);
                }
            }

            // A broad ramp back toward the low district makes traversal readable from the
            // camera and avoids creating a collection of jump-only islands.
            Vector3 low = new Vector3(origin.x, 0.18f, origin.z + 4.6f);
            Vector3 high = new Vector3(origin.x + direction * 5.8f, Mathf.Min(2.2f, heights[Mathf.Min(2, heights.Length - 1)]), origin.z + 4.6f);
            CreateRamp(root, "AccessRamp", low, high, 3.0f, body, metal);
        }

        private static void CreateArchitecturalTile(
            Transform parent,
            string name,
            Vector3 center,
            Vector3 size,
            Material body,
            Material metal,
            Material accent,
            int variant)
        {
            Transform root = Zone(parent, name);
            Primitive(name + "_Mass", PrimitiveType.Cube, root, center, size, body, true);
            float top = center.y + size.y * 0.5f;

            // Raised lip is decorative only so traversal does not snag on tiny trim.
            Primitive(name + "_TopFrame", PrimitiveType.Cube, root,
                new Vector3(center.x, top + 0.055f, center.z),
                new Vector3(size.x * 0.88f, 0.06f, size.z * 0.88f), metal, false);
            Primitive(name + "_InlayA", PrimitiveType.Cube, root,
                new Vector3(center.x, top + 0.095f, center.z),
                new Vector3(size.x * 0.60f, 0.025f, 0.055f), accent, false, new Vector3(0f, variant * 17f, 0f));
            Primitive(name + "_InlayB", PrimitiveType.Cube, root,
                new Vector3(center.x, top + 0.10f, center.z),
                new Vector3(0.055f, 0.025f, size.z * 0.55f), accent, false, new Vector3(0f, variant * 11f, 0f));

            for (int corner = 0; corner < 4; corner++)
            {
                float sx = corner % 2 == 0 ? -1f : 1f;
                float sz = corner < 2 ? -1f : 1f;
                Primitive($"{name}_Corner_{corner}", PrimitiveType.Cube, root,
                    new Vector3(center.x + sx * size.x * 0.43f, top + 0.24f, center.z + sz * size.z * 0.43f),
                    new Vector3(0.24f, 0.48f + (variant % 2) * 0.20f, 0.24f), metal, false);
            }
        }

        private static void CreateStairRun(
            Transform parent,
            string name,
            Vector3 from,
            Vector3 to,
            int steps,
            Material body,
            Material accent)
        {
            Transform root = Zone(parent, name);
            Vector3 delta = to - from;
            Vector3 flat = new Vector3(delta.x, 0f, delta.z);
            float flatLength = Mathf.Max(0.5f, flat.magnitude);
            Vector3 forward = flat.normalized;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            float width = 2.4f;
            float depth = flatLength / Mathf.Max(1, steps);

            for (int i = 0; i < steps; i++)
            {
                float t = (i + 0.5f) / steps;
                float y = Mathf.Lerp(from.y, to.y, (i + 1f) / steps);
                Vector3 p = Vector3.Lerp(from, to, t);
                p.y = y - 0.11f;
                GameObject step = Primitive($"{name}_Step_{i:00}", PrimitiveType.Cube, root, p,
                    new Vector3(width, 0.22f, depth + 0.04f), body, true);
                step.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
                Primitive($"{name}_Edge_{i:00}", PrimitiveType.Cube, root,
                    p + Vector3.up * 0.13f + forward * depth * 0.42f,
                    new Vector3(width * 0.84f, 0.025f, 0.045f), accent, false);
            }
        }

        private static void CreateRamp(
            Transform parent,
            string name,
            Vector3 from,
            Vector3 to,
            float width,
            Material body,
            Material metal)
        {
            Vector3 delta = to - from;
            Vector3 flat = new Vector3(delta.x, 0f, delta.z);
            float horizontal = Mathf.Max(0.5f, flat.magnitude);
            float vertical = to.y - from.y;
            float length = Mathf.Sqrt(horizontal * horizontal + vertical * vertical);
            float pitch = -Mathf.Atan2(vertical, horizontal) * Mathf.Rad2Deg;
            Vector3 center = (from + to) * 0.5f;
            GameObject ramp = Primitive(name + "_Collision", PrimitiveType.Cube, parent, center,
                new Vector3(width, 0.22f, length), body, true);
            ramp.transform.rotation = Quaternion.LookRotation(flat.normalized, Vector3.up) * Quaternion.Euler(pitch, 0f, 0f);
            Primitive(name + "_CenterRail", PrimitiveType.Cube, parent,
                center + Vector3.up * 0.15f,
                new Vector3(0.05f, 0.035f, length * 0.82f), metal, false,
                new Vector3(pitch, Mathf.Atan2(flat.x, flat.z) * Mathf.Rad2Deg, 0f));
        }

        private static void BuildBridge(Transform parent, string name, Vector3 center, float width, float depth, Material body, Material accent)
        {
            Transform root = Zone(parent, name);
            Primitive(name + "_Deck", PrimitiveType.Cube, root, center,
                new Vector3(width, 0.30f, depth), body, true);
            for (int side = -1; side <= 1; side += 2)
            {
                Primitive(name + "_Rail_" + side, PrimitiveType.Cube, root,
                    center + new Vector3(0f, 0.34f, side * (depth * 0.5f - 0.12f)),
                    new Vector3(width, 0.58f, 0.18f), body, true);
                Primitive(name + "_Signal_" + side, PrimitiveType.Cube, root,
                    center + new Vector3(0f, 0.64f, side * (depth * 0.5f - 0.23f)),
                    new Vector3(width * 0.72f, 0.035f, 0.035f), accent, false);
            }
        }

        private static void BuildDistantWorld(Transform parent, Material obsidian, Material metal, Material cyan, Material violet)
        {
            Transform root = Zone(parent, "World_DistantSilhouette");
            // Beyond the collision walls: large, collider-free silhouettes plus fog imply
            // continuation without inviting the player into unbounded simulation space.
            for (int i = 0; i < 30; i++)
            {
                float angle = i / 30f * Mathf.PI * 2f;
                float radiusX = 52f + (i % 4) * 4.2f;
                float radiusZ = 72f + (i % 5) * 4.8f;
                float x = Mathf.Sin(angle) * radiusX;
                float z = -24f + Mathf.Cos(angle) * radiusZ;
                float h = 10f + (i % 6) * 3.1f;
                Primitive($"FarSpire_{i:00}", PrimitiveType.Cube, root,
                    new Vector3(x, h * 0.5f - 0.4f, z),
                    new Vector3(2.4f + (i % 3) * 0.7f, h, 2.4f + ((i + 1) % 3) * 0.6f),
                    i % 2 == 0 ? obsidian : metal, false,
                    new Vector3((i % 3 - 1) * 4f, i * 13f, (i % 2) * 5f));
                Primitive($"FarSignal_{i:00}", PrimitiveType.Cube, root,
                    new Vector3(x, h * 0.62f, z),
                    new Vector3(0.08f, h * 0.46f, 0.08f),
                    i % 2 == 0 ? cyan : violet, false);
            }
        }

        private static void ConfigureAtmosphere()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.035f, 0.055f, 0.080f, 1f);
            RenderSettings.fogDensity = 0.0105f;
        }

        private static Transform Zone(Transform parent, string name)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        private static GameObject Primitive(
            string name,
            PrimitiveType type,
            Transform parent,
            Vector3 position,
            Vector3 scale,
            Material material,
            bool collider,
            Vector3 euler = default)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = scale;
            go.transform.rotation = Quaternion.Euler(euler);

            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }

            Collider c = go.GetComponent<Collider>();
            if (!collider && c != null) UnityEngine.Object.DestroyImmediate(c);
            GameObjectUtility.SetStaticEditorFlags(go, collider ? WorldStatic : VisualStatic);
            return go;
        }

        private static Material RequireMaterial(string name)
        {
            string[] ids = AssetDatabase.FindAssets(name + " t:Material");
            for (int i = 0; i < ids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(ids[i]);
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material != null && string.Equals(material.name, name, StringComparison.OrdinalIgnoreCase))
                    return material;
            }
            throw new InvalidOperationException("Grounded World V1 could not resolve material: " + name);
        }
    }
}
#endif
