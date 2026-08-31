#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Mindforge.World;

namespace Mindforge.Editor
{
    /// <summary>
    /// Visual multiplication pass for the V0.6 persistent world. V0.7 does not author
    /// topology or gameplay. It decorates solved modular cells, adds a handful of authored
    /// silhouette anchors across the world, and keeps every new piece presentation-only.
    ///
    /// The world should read at three scales:
    /// 1. district silhouette at long range;
    /// 2. structural grammar at traversal range;
    /// 3. signal/relic detail at interaction range.
    /// </summary>
    public static class WorldV07Builder
    {
        public const string RootName = "Mindforge_NeuralGothic_World_V07";
        public const string Revision = "NEURAL_GOTHIC_WORLD_V07";

        private static readonly StaticEditorFlags VisualStatic =
            StaticEditorFlags.BatchingStatic |
            StaticEditorFlags.OccludeeStatic |
            StaticEditorFlags.ReflectionProbeStatic;

        [MenuItem("Mindforge/Legacy/Showcase/Apply Neural-Gothic World V0.7", priority = 35)]
        public static void ApplyOpenScene()
        {
            GameObject persistentRoot = EditorSceneLookup.FindIncludingInactive(WorldV06Builder.RootName);
            GameObject annex = EditorSceneLookup.FindIncludingInactive("Neural_Cloister_Procedural_Annex");
            if (persistentRoot == null || annex == null)
                throw new InvalidOperationException("World V0.7 requires Persistent World V0.6 and its Neural Cloister annex.");

            NeuralGothicMaterialAuthoringV07.EnsureAuthored();

            Material stone = RequireMaterial(NeuralGothicMaterialAuthoringV07.Stone);
            Material darkStone = RequireMaterial(NeuralGothicMaterialAuthoringV07.DarkStone);
            Material metal = RequireMaterial(NeuralGothicMaterialAuthoringV07.Metal);
            Material patina = RequireMaterial(NeuralGothicMaterialAuthoringV07.Patina);
            Material ash = RequireMaterial(NeuralGothicMaterialAuthoringV07.AshStone);
            Material cyan = RequireMaterial("AetherCyan");
            Material green = RequireMaterial("WispVerdant");
            Material violet = RequireMaterial("FracturedRing");

            GameObject previous = EditorSceneLookup.FindIncludingInactive(RootName);
            if (previous != null) UnityEngine.Object.DestroyImmediate(previous);

            NeuralGothicWorldDetailerV07 detailer = annex.GetComponent<NeuralGothicWorldDetailerV07>();
            if (detailer == null) detailer = annex.AddComponent<NeuralGothicWorldDetailerV07>();
            detailer.ConfigureRuntime(70731, stone, darkStone, metal, patina, cyan, green, violet);
            int detailedCells = detailer.Rebuild();

            GameObject root = new GameObject(RootName);
            root.transform.SetParent(persistentRoot.transform, false);

            BuildCloisterThreshold(root.transform, stone, darkStone, metal, patina, cyan);
            BuildCloisterArchiveSpire(root.transform, darkStone, metal, patina, cyan, violet);
            BuildResonanceWell(root.transform, stone, metal, patina, green, cyan);
            BuildMemoryLoom(root.transform, darkStone, metal, cyan, green);
            BuildMarketReliquary(root.transform, stone, patina, metal, violet, cyan);
            BuildCathedralRelay(root.transform, darkStone, metal, violet, cyan);
            BuildDistantSilhouetteAnchors(root.transform, ash, darkStone, metal, cyan, violet);
            BuildLightRhythm(root.transform);

            NeuralGothicWorldArtAuditV07 audit = root.AddComponent<NeuralGothicWorldArtAuditV07>();
            audit.ConfigureRuntime(annex.transform, root.transform);
            audit.Evaluate(true);

            EditorUtility.SetDirty(root);
            EditorUtility.SetDirty(annex);
            EditorUtility.SetDirty(detailer);
            EditorUtility.SetDirty(audit);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                $"[Mindforge:WorldV07] Neural-gothic presentation pass ready across {detailedCells} generated cells. " +
                "Cell topology remains V0.6 authority; V0.7 adds deterministic local buttresses/arches/ribs/inlays/relic props plus authored " +
                "Cloister, Forge, Market and Cathedral silhouette anchors. All V0.7 geometry is collider-free presentation.");
        }

        private static void BuildCloisterThreshold(
            Transform parent,
            Material stone,
            Material dark,
            Material metal,
            Material patina,
            Material signal)
        {
            Transform root = Zone(parent, "Cloister_Threshold_V07");
            Vector3 center = new Vector3(20.2f, 0f, -35f);

            for (int side = -1; side <= 1; side += 2)
            {
                float z = center.z + side * 2.25f;
                Primitive("GatePier_" + side, PrimitiveType.Cube, root,
                    new Vector3(center.x, 2.35f, z), new Vector3(0.82f, 4.7f, 0.82f), dark, false,
                    new Vector3(0f, 45f, 0f));
                Primitive("GatePierInner_" + side, PrimitiveType.Cube, root,
                    new Vector3(center.x - 0.34f, 2.15f, z), new Vector3(0.24f, 3.7f, 1.10f), patina, false);
                Primitive("GateSignal_" + side, PrimitiveType.Cube, root,
                    new Vector3(center.x - 0.57f, 2.10f, z), new Vector3(0.055f, 2.55f, 0.30f), signal, false);
                Primitive("GateCrown_" + side, PrimitiveType.Cube, root,
                    new Vector3(center.x, 4.93f, z), new Vector3(1.55f, 0.38f, 1.55f), stone, false,
                    new Vector3(0f, 45f, 0f));
            }

            Primitive("GateLintel", PrimitiveType.Cube, root,
                center + new Vector3(0f, 4.62f, 0f), new Vector3(0.74f, 0.55f, 5.1f), metal, false);
            CreateRing("GateHalo", root, center + new Vector3(-0.44f, 4.7f, 0f), 1.85f, 32, 0.065f, signal,
                Quaternion.Euler(0f, 0f, 90f));
            CreateCable("ThresholdCableA", root,
                center + new Vector3(-0.15f, 4.8f, -2.0f),
                center + new Vector3(-0.15f, 4.8f, 2.0f),
                0.72f, 8, 0.035f, metal);
        }

        private static void BuildCloisterArchiveSpire(
            Transform parent,
            Material stone,
            Material metal,
            Material patina,
            Material cyan,
            Material violet)
        {
            Transform root = Zone(parent, "Cloister_Archive_Spire_V07");
            Vector3 p = new Vector3(32.2f, 0f, -24.0f);

            Primitive("SpireFoot", PrimitiveType.Cylinder, root, p + Vector3.up * 0.34f,
                new Vector3(2.15f, 0.34f, 2.15f), stone, false);
            Primitive("SpireBody", PrimitiveType.Cylinder, root, p + Vector3.up * 3.8f,
                new Vector3(0.72f, 3.55f, 0.72f), metal, false);
            Primitive("SpirePatina", PrimitiveType.Cylinder, root, p + Vector3.up * 4.05f,
                new Vector3(0.88f, 2.55f, 0.88f), patina, false);

            for (int i = 0; i < 4; i++)
            {
                float a = i * 90f * Mathf.Deg2Rad;
                Vector3 radial = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                Primitive("SpireFlyingButtress_" + i, PrimitiveType.Cube, root,
                    p + radial * 1.35f + Vector3.up * 2.55f,
                    new Vector3(0.25f, 3.5f, 0.25f), stone, false,
                    new Vector3(radial.z * 18f, i * 90f, -radial.x * 18f));
                Primitive("SpireNode_" + i, PrimitiveType.Sphere, root,
                    p + radial * 1.55f + Vector3.up * 4.20f,
                    Vector3.one * 0.25f, i % 2 == 0 ? cyan : violet, false);
            }

            Primitive("SpireNeedle", PrimitiveType.Cylinder, root, p + Vector3.up * 8.55f,
                new Vector3(0.18f, 2.0f, 0.18f), metal, false);
            Primitive("SpireCore", PrimitiveType.Sphere, root, p + Vector3.up * 6.82f,
                Vector3.one * 0.58f, cyan, false);
            CreateRing("SpireHaloA", root, p + Vector3.up * 6.82f, 1.25f, 36, 0.055f, cyan,
                Quaternion.Euler(70f, 0f, 0f));
            CreateRing("SpireHaloB", root, p + Vector3.up * 6.82f, 1.65f, 40, 0.045f, violet,
                Quaternion.Euler(20f, 35f, 0f));
        }

        private static void BuildResonanceWell(
            Transform parent,
            Material stone,
            Material metal,
            Material patina,
            Material green,
            Material cyan)
        {
            Transform root = Zone(parent, "Cloister_Resonance_Well_V07");
            Vector3 p = new Vector3(24.0f, 0f, -44.0f);

            Primitive("WellBase", PrimitiveType.Cylinder, root, p + Vector3.up * 0.22f,
                new Vector3(2.35f, 0.22f, 2.35f), stone, false);
            Primitive("WellInner", PrimitiveType.Cylinder, root, p + Vector3.up * 0.42f,
                new Vector3(1.55f, 0.16f, 1.55f), patina, false);
            Primitive("WellCore", PrimitiveType.Sphere, root, p + Vector3.up * 0.78f,
                Vector3.one * 0.45f, green, false);
            CreateRing("WellRingA", root, p + Vector3.up * 0.70f, 1.20f, 30, 0.050f, green,
                Quaternion.Euler(90f, 0f, 0f));
            CreateRing("WellRingB", root, p + Vector3.up * 1.0f, 0.82f, 26, 0.040f, cyan,
                Quaternion.Euler(68f, 0f, 0f));

            for (int i = 0; i < 6; i++)
            {
                float a = i / 6f * Mathf.PI * 2f;
                Vector3 radial = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                Primitive("WellFin_" + i, PrimitiveType.Cube, root,
                    p + radial * 2.15f + Vector3.up * 0.65f,
                    new Vector3(0.22f, 1.30f, 0.46f), metal, false,
                    new Vector3(0f, -a * Mathf.Rad2Deg, 9f));
            }
        }

        private static void BuildMemoryLoom(
            Transform parent,
            Material stone,
            Material metal,
            Material cyan,
            Material green)
        {
            Transform root = Zone(parent, "Memory_Forge_Loom_V07");
            Vector3 p = new Vector3(-9.8f, 0f, -59.5f);

            for (int i = 0; i < 5; i++)
            {
                float z = p.z - 2.8f + i * 1.4f;
                float height = 4.2f + (i % 3) * 0.9f;
                Primitive("LoomRib_" + i, PrimitiveType.Cube, root,
                    new Vector3(p.x, height * 0.5f, z), new Vector3(0.42f, height, 0.42f),
                    i % 2 == 0 ? stone : metal, false,
                    new Vector3(0f, 0f, i % 2 == 0 ? -6f : 6f));
                Primitive("LoomSignal_" + i, PrimitiveType.Cube, root,
                    new Vector3(p.x + 0.28f, height * 0.58f, z), new Vector3(0.045f, height * 0.55f, 0.045f),
                    i % 2 == 0 ? cyan : green, false);
            }
            CreateCable("LoomCableTop", root, new Vector3(p.x, 5.4f, p.z - 2.6f), new Vector3(p.x, 5.4f, p.z + 2.6f),
                1.15f, 12, 0.042f, metal);
            CreateRing("LoomHalo", root, new Vector3(p.x + 0.2f, 4.5f, p.z), 1.75f, 34, 0.055f, cyan,
                Quaternion.Euler(0f, 0f, 90f));
        }

        private static void BuildMarketReliquary(
            Transform parent,
            Material stone,
            Material patina,
            Material metal,
            Material violet,
            Material cyan)
        {
            Transform root = Zone(parent, "Null_Market_Reliquary_V07");
            Vector3 p = new Vector3(-11.5f, 0f, -30.0f);

            Primitive("ReliquaryBase", PrimitiveType.Cylinder, root, p + Vector3.up * 0.22f,
                new Vector3(2.1f, 0.22f, 2.1f), stone, false);
            for (int i = 0; i < 4; i++)
            {
                float a = (45f + i * 90f) * Mathf.Deg2Rad;
                Vector3 radial = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                Primitive("ReliquaryPillar_" + i, PrimitiveType.Cube, root,
                    p + radial * 1.55f + Vector3.up * 1.75f,
                    new Vector3(0.38f, 3.50f, 0.38f), i % 2 == 0 ? patina : metal, false,
                    new Vector3(0f, 45f + i * 90f, 0f));
            }
            Primitive("ReliquaryRelic", PrimitiveType.Sphere, root, p + Vector3.up * 2.0f,
                Vector3.one * 0.58f, violet, false);
            CreateRing("ReliquaryRingA", root, p + Vector3.up * 2.0f, 1.05f, 30, 0.05f, violet,
                Quaternion.Euler(72f, 18f, 0f));
            CreateRing("ReliquaryRingB", root, p + Vector3.up * 2.0f, 1.38f, 32, 0.04f, cyan,
                Quaternion.Euler(18f, 65f, 0f));
        }

        private static void BuildCathedralRelay(
            Transform parent,
            Material stone,
            Material metal,
            Material violet,
            Material cyan)
        {
            Transform root = Zone(parent, "Cathedral_Relay_V07");
            Vector3 p = new Vector3(12.2f, 0f, -7.0f);

            for (int i = 0; i < 3; i++)
            {
                float x = p.x + (i - 1) * 1.9f;
                float h = 5.6f + i * 1.15f;
                Primitive("RelayTower_" + i, PrimitiveType.Cube, root,
                    new Vector3(x, h * 0.5f, p.z), new Vector3(0.72f, h, 0.72f), stone, false,
                    new Vector3(0f, 45f, 0f));
                Primitive("RelayNeedle_" + i, PrimitiveType.Cylinder, root,
                    new Vector3(x, h + 1.0f, p.z), new Vector3(0.12f, 1.0f, 0.12f), metal, false);
                Primitive("RelayNode_" + i, PrimitiveType.Sphere, root,
                    new Vector3(x, h + 2.0f, p.z), Vector3.one * 0.25f,
                    i == 1 ? cyan : violet, false);
            }
            CreateCable("RelayCableA", root,
                new Vector3(p.x - 1.9f, 5.6f, p.z), new Vector3(p.x, 6.75f, p.z),
                0.65f, 9, 0.035f, metal);
            CreateCable("RelayCableB", root,
                new Vector3(p.x, 6.75f, p.z), new Vector3(p.x + 1.9f, 7.9f, p.z),
                0.65f, 9, 0.035f, metal);
        }

        private static void BuildDistantSilhouetteAnchors(
            Transform parent,
            Material ash,
            Material dark,
            Material metal,
            Material cyan,
            Material violet)
        {
            Transform root = Zone(parent, "Distant_Silhouette_Anchors_V07");
            Vector3[] positions =
            {
                new Vector3(-31f, 0f, -66f), new Vector3(32f, 0f, -64f),
                new Vector3(-31f, 0f, -40f), new Vector3(34f, 0f, -10f),
                new Vector3(-29f, 0f, 15f), new Vector3(30f, 0f, 18f),
            };
            for (int i = 0; i < positions.Length; i++)
            {
                Vector3 p = positions[i];
                float h = 6.5f + (i % 3) * 2.4f;
                Primitive("SilhouettePillar_" + i, PrimitiveType.Cube, root,
                    p + Vector3.up * h * 0.5f,
                    new Vector3(1.1f + (i % 2) * 0.45f, h, 1.1f + ((i + 1) % 2) * 0.45f),
                    i % 2 == 0 ? dark : ash, false,
                    new Vector3(0f, 18f + i * 27f, i % 2 == 0 ? 2f : -3f));
                Primitive("SilhouetteCrown_" + i, PrimitiveType.Cube, root,
                    p + Vector3.up * (h + 0.45f), new Vector3(2.5f, 0.35f, 2.5f), metal, false,
                    new Vector3(0f, 45f, 0f));
                Primitive("SilhouetteSignal_" + i, PrimitiveType.Sphere, root,
                    p + Vector3.up * (h + 1.0f), Vector3.one * 0.22f,
                    i % 2 == 0 ? cyan : violet, false);
            }
        }

        private static void BuildLightRhythm(Transform parent)
        {
            Transform root = Zone(parent, "World_Light_Rhythm_V07");
            AddPointLight("CloisterLight_A", root, new Vector3(22.5f, 4.2f, -41f), new Color(0.08f, 0.58f, 0.92f), 1.35f, 7.5f);
            AddPointLight("CloisterLight_B", root, new Vector3(30.5f, 4.4f, -31f), new Color(0.12f, 0.92f, 0.48f), 1.10f, 7.0f);
            AddPointLight("CloisterLight_C", root, new Vector3(31.5f, 6.8f, -24f), new Color(0.55f, 0.14f, 0.96f), 1.15f, 8.5f);
            AddPointLight("ForgeLoomLight", root, new Vector3(-9.0f, 4.0f, -59.5f), new Color(0.06f, 0.48f, 0.92f), 0.85f, 7.0f);
            AddPointLight("MarketReliquaryLight", root, new Vector3(-11.5f, 3.7f, -30f), new Color(0.48f, 0.12f, 0.92f), 0.75f, 6.5f);
            AddPointLight("CathedralRelayLight", root, new Vector3(12.2f, 5.4f, -7f), new Color(0.40f, 0.14f, 0.92f), 0.80f, 7.5f);
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
            Vector3 localPosition,
            Vector3 localScale,
            Material material,
            bool collider,
            Vector3? localEuler = null)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;
            go.transform.localEulerAngles = localEuler ?? Vector3.zero;
            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null && material != null) renderer.sharedMaterial = material;
            Collider shape = go.GetComponent<Collider>();
            if (shape != null && !collider) UnityEngine.Object.DestroyImmediate(shape);
            GameObjectUtility.SetStaticEditorFlags(go, VisualStatic);
            return go;
        }

        private static void CreateRing(
            string name,
            Transform parent,
            Vector3 localCenter,
            float radius,
            int segments,
            float width,
            Material material,
            Quaternion rotation)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localCenter;
            go.transform.localRotation = rotation;
            LineRenderer line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = Mathf.Max(8, segments);
            line.startWidth = width;
            line.endWidth = width;
            line.numCornerVertices = 2;
            line.numCapVertices = 0;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            if (material != null) line.sharedMaterial = material;
            for (int i = 0; i < line.positionCount; i++)
            {
                float a = i / (float)line.positionCount * Mathf.PI * 2f;
                line.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0f));
            }
        }

        private static void CreateCable(
            string name,
            Transform parent,
            Vector3 localStart,
            Vector3 localEnd,
            float sag,
            int segments,
            float width,
            Material material)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            LineRenderer line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.positionCount = Mathf.Max(3, segments);
            line.startWidth = width;
            line.endWidth = width * 0.86f;
            line.numCornerVertices = 2;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            if (material != null) line.sharedMaterial = material;
            for (int i = 0; i < line.positionCount; i++)
            {
                float t = i / (float)(line.positionCount - 1);
                Vector3 p = Vector3.Lerp(localStart, localEnd, t);
                p.y -= 4f * sag * t * (1f - t);
                line.SetPosition(i, p);
            }
        }

        private static void AddPointLight(
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
            light.renderMode = LightRenderMode.Auto;
        }

        private static Material RequireMaterial(string name)
        {
            Material material = CinematicMaterialAuthoring.Load(name);
            if (material == null) throw new InvalidOperationException("World V0.7 required material missing: " + name);
            return material;
        }
    }
}
#endif
