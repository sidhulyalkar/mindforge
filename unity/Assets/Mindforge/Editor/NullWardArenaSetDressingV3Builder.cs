#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mindforge.Editor
{
    /// <summary>
    /// Third-layer set dressing for the combat world. It adds near/mid/far visual depth
    /// while keeping all authored pieces collider-free and outside combat authority.
    /// Existing shared materials are reused and realtime lights never cast shadows.
    /// </summary>
    public static class NullWardArenaSetDressingV3Builder
    {
        public const string WardRootName = "Mindforge_NullWard_SetDressing_V3";
        public const string ArenaBackdropRootName = "Mindforge_Arena_Backdrop_V1";

        private static readonly StaticEditorFlags StaticFlags =
            StaticEditorFlags.BatchingStatic |
            StaticEditorFlags.OccludeeStatic |
            StaticEditorFlags.ReflectionProbeStatic;

        [MenuItem("Mindforge/Legacy/Showcase/Apply Arena Set Dressing V3", priority = 28)]
        public static void ApplyOpenScene()
        {
            GameObject ward = EditorSceneLookup.FindIncludingInactive(NullWardSceneBuilder.RootName);
            GameObject arena = EditorSceneLookup.FindIncludingInactive("Fractured_Signal_Arena");
            if (ward == null || arena == null)
                throw new InvalidOperationException("Arena set dressing requires Null Ward and Fractured_Signal_Arena.");

            DestroyChild(ward.transform, WardRootName);
            DestroyChild(arena.transform, ArenaBackdropRootName);

            Material basalt = RequireMaterial("ArenaBasalt");
            Material obsidian = RequireMaterial("ObsidianArchitecture");
            Material metal = RequireMaterial("GuardianMetal");
            Material cyan = RequireMaterial("AetherCyan");
            Material green = RequireMaterial("WispVerdant");
            Material violet = RequireMaterial("FracturedRing");
            Material hostile = RequireMaterial("FracturedCore");

            GameObject wardRoot = new GameObject(WardRootName);
            wardRoot.transform.SetParent(ward.transform, false);
            BuildMemoryForge(wardRoot.transform, basalt, metal, cyan, green);
            BuildCauseway(wardRoot.transform, obsidian, metal, cyan, violet);
            BuildMarket(wardRoot.transform, basalt, obsidian, metal, cyan, green, violet);
            BuildFractureCourt(wardRoot.transform, obsidian, metal, violet, hostile);
            BuildCathedral(wardRoot.transform, obsidian, metal, cyan, green, violet);

            GameObject arenaRoot = new GameObject(ArenaBackdropRootName);
            arenaRoot.transform.SetParent(arena.transform, false);
            BuildArenaBackdrop(arenaRoot.transform, obsidian, metal, cyan, green, violet);

            EditorUtility.SetDirty(wardRoot);
            EditorUtility.SetDirty(arenaRoot);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                "[Mindforge:SetDressingV3] Added layered Null Ward props, fracture-court architecture and distant arena skyline with zero gameplay colliders.");
        }

        private static void BuildMemoryForge(
            Transform parent,
            Material basalt,
            Material metal,
            Material cyan,
            Material green)
        {
            Transform root = Zone(parent, "Set_MemoryForge");
            // Reconstruction cradles line the walls and make the checkpoint feel like an
            // actual machine rather than a glowing pedestal in an empty room.
            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < 3; i++)
                {
                    float z = -59.6f + i * 1.72f;
                    float x = side * 4.85f;
                    Primitive($"Forge_Cradle_{side}_{i}_Base", PrimitiveType.Cube, root,
                        new Vector3(x, 0.36f, z), new Vector3(1.05f, 0.62f, 1.18f), basalt, true);
                    Primitive($"Forge_Cradle_{side}_{i}_Frame", PrimitiveType.Cube, root,
                        new Vector3(x, 1.22f, z), new Vector3(0.72f, 1.50f, 0.22f), metal, true);
                    Primitive($"Forge_Cradle_{side}_{i}_Signal", PrimitiveType.Cube, root,
                        new Vector3(x - side * 0.38f, 1.24f, z), new Vector3(0.035f, 0.92f, 0.34f),
                        i % 2 == 0 ? cyan : green, false);
                }
            }

            for (int i = 0; i < 5; i++)
            {
                float x = -3.5f + i * 1.75f;
                Primitive($"Forge_SuspendedShard_{i:00}", PrimitiveType.Cube, root,
                    new Vector3(x, 3.15f + (i % 2) * 0.32f, -58.0f),
                    new Vector3(0.10f, 0.72f, 0.18f),
                    i % 2 == 0 ? cyan : green,
                    false,
                    new Vector3(12f + i * 3f, i * 17f, 42f));
            }
        }

        private static void BuildCauseway(
            Transform parent,
            Material obsidian,
            Material metal,
            Material cyan,
            Material violet)
        {
            Transform root = Zone(parent, "Set_Causeway");
            // Towers sit beyond the rails, giving parallax and height without narrowing the
            // actual lane used by Sentries, Hollows and the camera.
            for (int i = 0; i < 6; i++)
            {
                float z = -50.0f + i * 2.85f;
                int side = i % 2 == 0 ? -1 : 1;
                float x = side * (6.25f + (i % 3) * 0.45f);
                float height = 4.6f + (i % 3) * 1.15f;
                Primitive($"Causeway_SideTower_{i:00}", PrimitiveType.Cube, root,
                    new Vector3(x, height * 0.5f - 0.10f, z),
                    new Vector3(1.15f, height, 1.10f), obsidian, true);
                Primitive($"Causeway_TowerSpine_{i:00}", PrimitiveType.Cube, root,
                    new Vector3(x - side * 0.59f, height * 0.56f, z),
                    new Vector3(0.045f, height * 0.68f, 0.20f), i % 2 == 0 ? cyan : violet, false);
                Primitive($"Causeway_TowerCap_{i:00}", PrimitiveType.Cube, root,
                    new Vector3(x, height + 0.18f, z),
                    new Vector3(1.48f, 0.18f, 1.36f), metal, true,
                    new Vector3(0f, i * 13f, 0f));
            }

            CreateCable("Causeway_HangingConduit_A", root,
                new Vector3(-4.1f, 4.0f, -50.4f), new Vector3(4.1f, 3.75f, -44.0f), 0.42f, 0.025f, metal);
            CreateCable("Causeway_HangingConduit_B", root,
                new Vector3(4.0f, 4.15f, -46.6f), new Vector3(-4.0f, 3.95f, -38.1f), 0.55f, 0.022f, metal);

            for (int i = 0; i < 5; i++)
            {
                float z = -48.5f + i * 2.7f;
                float x = i % 2 == 0 ? -5.05f : 5.05f;
                Primitive($"Causeway_FloatingPlate_{i:00}", PrimitiveType.Cube, root,
                    new Vector3(x, 2.1f + (i % 3) * 0.42f, z),
                    new Vector3(0.72f, 0.08f, 1.20f), metal, true,
                    new Vector3(5f * i, 19f * i, 10f - i * 4f));
            }
        }

        private static void BuildMarket(
            Transform parent,
            Material basalt,
            Material obsidian,
            Material metal,
            Material cyan,
            Material green,
            Material violet)
        {
            Transform root = Zone(parent, "Set_Market");
            Vector3[] clusters =
            {
                new Vector3(-8.1f, 0f, -32.5f),
                new Vector3(7.7f, 0f, -32.0f),
                new Vector3(-7.8f, 0f, -26.1f),
                new Vector3(8.0f, 0f, -24.8f),
            };
            for (int i = 0; i < clusters.Length; i++)
            {
                Vector3 c = clusters[i];
                Primitive($"Market_ArchiveDesk_{i:00}", PrimitiveType.Cube, root,
                    c + new Vector3(0f, 0.50f, 0f), new Vector3(2.35f, 0.82f, 1.12f), basalt, true,
                    new Vector3(0f, i % 2 == 0 ? 8f : -11f, 0f));
                Primitive($"Market_ArchiveBack_{i:00}", PrimitiveType.Cube, root,
                    c + new Vector3(0f, 1.55f, -0.38f), new Vector3(1.85f, 1.28f, 0.20f), obsidian, true);
                Primitive($"Market_Sign_{i:00}", PrimitiveType.Cube, root,
                    c + new Vector3(0f, 1.78f, -0.50f), new Vector3(0.95f, 0.14f, 0.035f),
                    i % 3 == 0 ? cyan : i % 3 == 1 ? green : violet, false);
                for (int j = 0; j < 3; j++)
                {
                    Primitive($"Market_Crate_{i:00}_{j:00}", PrimitiveType.Cube, root,
                        c + new Vector3(-0.70f + j * 0.70f, 0.22f + (j % 2) * 0.10f, 0.86f),
                        new Vector3(0.52f, 0.42f + (j % 2) * 0.18f, 0.52f), metal, true,
                        new Vector3(0f, j * 15f - 12f, 0f));
                }
            }

            CreateCable("Market_OverheadCable_A", root,
                new Vector3(-8.2f, 3.10f, -32.3f), new Vector3(7.6f, 2.75f, -25.0f), 1.15f, 0.020f, metal);
            CreateCable("Market_OverheadCable_B", root,
                new Vector3(7.8f, 3.25f, -32.0f), new Vector3(-7.7f, 2.85f, -26.0f), 0.92f, 0.020f, metal);

            Primitive("Market_BrokenDirectory", PrimitiveType.Cube, root,
                new Vector3(-0.4f, 2.65f, -24.4f), new Vector3(1.05f, 2.80f, 0.22f), obsidian, true,
                new Vector3(0f, -18f, 9f));
            Primitive("Market_BrokenDirectoryRune", PrimitiveType.Cube, root,
                new Vector3(-0.33f, 2.72f, -24.55f), new Vector3(0.52f, 1.70f, 0.030f), violet, false,
                new Vector3(0f, -18f, 9f));
        }

        private static void BuildFractureCourt(
            Transform parent,
            Material obsidian,
            Material metal,
            Material violet,
            Material hostile)
        {
            Transform root = Zone(parent, "Set_FractureCourt");
            // Four heavy pylons frame the new encounter without blocking its center.
            Vector3[] pylons =
            {
                new Vector3(-4.35f, 2.25f, -21.5f),
                new Vector3(4.35f, 2.25f, -21.5f),
                new Vector3(-4.35f, 2.65f, -18.8f),
                new Vector3(4.35f, 2.65f, -18.8f),
            };
            for (int i = 0; i < pylons.Length; i++)
            {
                Vector3 p = pylons[i];
                Primitive($"Court_Pylon_{i:00}", PrimitiveType.Cube, root,
                    p, new Vector3(0.72f, p.y * 2.0f, 0.72f), obsidian, true);
                Primitive($"Court_PylonSignal_{i:00}", PrimitiveType.Cube, root,
                    p + new Vector3(i % 2 == 0 ? 0.37f : -0.37f, 0.05f, 0f),
                    new Vector3(0.035f, p.y * 1.25f, 0.32f), i < 2 ? violet : hostile, false);
                Primitive($"Court_PylonCrown_{i:00}", PrimitiveType.Cube, root,
                    new Vector3(p.x, p.y * 2.0f + 0.18f, p.z),
                    new Vector3(1.06f, 0.16f, 0.98f), metal, true,
                    new Vector3(0f, 14f * i, 0f));
            }

            Primitive("Court_OverheadLintel", PrimitiveType.Cube, root,
                new Vector3(0f, 4.72f, -20.1f), new Vector3(8.6f, 0.18f, 0.36f), metal, true,
                new Vector3(0f, 0f, -3f));
            CreateCircle("Court_FractureHalo", root,
                new Vector3(0f, 4.55f, -20.1f), 2.15f, 56, 0.025f, violet, new Vector3(90f, 0f, 0f));
            PointLight("Court_ThreatFill_L", root, new Vector3(-3.4f, 2.4f, -20.2f),
                new Color(0.48f, 0.08f, 0.75f), 1.15f, 5.2f);
            PointLight("Court_ThreatFill_R", root, new Vector3(3.4f, 2.2f, -19.8f),
                new Color(0.82f, 0.08f, 0.22f), 1.05f, 5.0f);
        }

        private static void BuildCathedral(
            Transform parent,
            Material obsidian,
            Material metal,
            Material cyan,
            Material green,
            Material violet)
        {
            Transform root = Zone(parent, "Set_Cathedral");
            for (int i = 0; i < 8; i++)
            {
                float z = -15.0f + i * 1.55f;
                int side = i % 2 == 0 ? -1 : 1;
                float x = side * (8.7f + (i % 3) * 0.55f);
                float height = 5.5f + (i % 4) * 0.85f;
                Primitive($"Cathedral_DistantButtress_{i:00}", PrimitiveType.Cube, root,
                    new Vector3(x, height * 0.50f, z), new Vector3(1.12f, height, 1.28f), obsidian, true,
                    new Vector3(0f, side * (5f + i), side * 2f));
                Primitive($"Cathedral_ButtressRune_{i:00}", PrimitiveType.Cube, root,
                    new Vector3(x - side * 0.58f, height * 0.58f, z),
                    new Vector3(0.040f, height * 0.52f, 0.34f), i % 2 == 0 ? cyan : green, false);
            }

            for (int i = 0; i < 9; i++)
            {
                float x = -5.4f + i * 1.35f;
                float z = -5.2f + Mathf.Sin(i * 1.7f) * 1.2f;
                Primitive($"Cathedral_FloatingFracture_{i:00}", PrimitiveType.Cube, root,
                    new Vector3(x, 4.4f + (i % 3) * 0.58f, z),
                    new Vector3(0.10f, 0.85f + (i % 2) * 0.42f, 0.20f), violet, false,
                    new Vector3(12f + i * 5f, i * 27f, 32f - i * 3f));
            }

            CreateCable("Cathedral_SuspendedBus_A", root,
                new Vector3(-6.1f, 5.25f, -14.7f), new Vector3(6.0f, 5.65f, -8.0f), 0.82f, 0.024f, metal);
            CreateCable("Cathedral_SuspendedBus_B", root,
                new Vector3(6.0f, 5.15f, -12.6f), new Vector3(-5.8f, 5.45f, -5.5f), 0.68f, 0.022f, metal);
        }

        private static void BuildArenaBackdrop(
            Transform parent,
            Material obsidian,
            Material metal,
            Material cyan,
            Material green,
            Material violet)
        {
            // Low-detail distant towers extend the ritual arena into a larger complex. They
            // sit well beyond the Arena V3 combat radius and never cast realtime shadows.
            const float centerZ = 1f;
            for (int i = 0; i < 14; i++)
            {
                float angle = i / 14f * Mathf.PI * 2f + 0.14f;
                float radius = 17.5f + (i % 3) * 2.4f;
                float height = 7.0f + (i % 5) * 1.75f;
                Vector3 radial = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                Vector3 p = new Vector3(0f, 0f, centerZ) + radial * radius;
                Primitive($"Arena_DistantTower_{i:00}", PrimitiveType.Cube, parent,
                    p + Vector3.up * height * 0.5f,
                    new Vector3(1.4f + (i % 2) * 0.5f, height, 1.25f), obsidian, i % 3 == 0,
                    new Vector3(0f, -angle * Mathf.Rad2Deg + 90f, (i % 3 - 1) * 2.5f));
                Material rune = i % 3 == 0 ? cyan : i % 3 == 1 ? green : violet;
                Primitive($"Arena_DistantRune_{i:00}", PrimitiveType.Cube, parent,
                    p + Vector3.up * height * 0.58f - radial * 0.68f,
                    new Vector3(0.045f, height * 0.42f, 0.30f), rune, false,
                    new Vector3(0f, -angle * Mathf.Rad2Deg + 90f, 0f));
                if (i % 4 == 0)
                    Primitive($"Arena_DistantCrown_{i:00}", PrimitiveType.Cube, parent,
                        p + Vector3.up * (height + 0.30f), new Vector3(2.1f, 0.18f, 1.8f), metal, true,
                        new Vector3(0f, i * 11f, 0f));
            }
        }

        private static Transform Zone(Transform parent, string name)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            GameObjectUtility.SetStaticEditorFlags(go, StaticFlags);
            return go.transform;
        }

        private static GameObject Primitive(
            string name,
            PrimitiveType type,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            Material material,
            bool castShadows,
            Vector3? localEuler = null)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;
            go.transform.localRotation = Quaternion.Euler(localEuler ?? Vector3.zero);

            Collider collider = go.GetComponent<Collider>();
            if (collider != null) UnityEngine.Object.DestroyImmediate(collider);
            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
                renderer.receiveShadows = castShadows;
            }
            GameObjectUtility.SetStaticEditorFlags(go, StaticFlags);
            return go;
        }

        private static void CreateCable(
            string name,
            Transform parent,
            Vector3 start,
            Vector3 end,
            float sag,
            float width,
            Material material)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            LineRenderer line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.useWorldSpace = false;
            line.loop = false;
            line.positionCount = 9;
            line.widthMultiplier = width;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            for (int i = 0; i < line.positionCount; i++)
            {
                float t = i / (float)(line.positionCount - 1);
                Vector3 p = Vector3.Lerp(start, end, t);
                p.y -= Mathf.Sin(t * Mathf.PI) * sag;
                line.SetPosition(i, p);
            }
        }

        private static void CreateCircle(
            string name,
            Transform parent,
            Vector3 center,
            float radius,
            int segments,
            float width,
            Material material,
            Vector3 localEuler)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            go.transform.localRotation = Quaternion.Euler(localEuler);
            LineRenderer line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = Mathf.Max(12, segments);
            line.widthMultiplier = width;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            for (int i = 0; i < line.positionCount; i++)
            {
                float a = i / (float)line.positionCount * Mathf.PI * 2f;
                line.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius));
            }
        }

        private static void PointLight(
            string name,
            Transform parent,
            Vector3 localPosition,
            Color color,
            float intensity,
            float range)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            Light light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
        }

        private static Material RequireMaterial(string name)
        {
            Material material = CinematicMaterialAuthoring.Load(name);
            if (material == null)
                throw new InvalidOperationException($"Missing shared cinematic material {name}.");
            return material;
        }

        private static void DestroyChild(Transform parent, string name)
        {
            Transform child = parent != null ? parent.Find(name) : null;
            if (child != null) UnityEngine.Object.DestroyImmediate(child.gameObject);
        }
    }
}
#endif
