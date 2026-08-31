#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Mindforge.Journey;
using Mindforge.World;

namespace Mindforge.Editor
{
    /// <summary>
    /// Small hero/detail pass downstream of SanctumOnboardingV08Builder. It gives the existing
    /// Memory Forge checkpoint a visible bright-cathedral home and binds V0.8 profile restore
    /// to the sanctum threshold without creating a second checkpoint/gate authority.
    /// </summary>
    public static class SanctumHeroV08Builder
    {
        public const string RootName = "Sanctum_Hero_Props_V08";

        [MenuItem("Mindforge/Legacy/Showcase/Apply Sanctum Hero Props V0.8", priority = 37)]
        public static void ApplyOpenScene()
        {
            GameObject sanctum = EditorSceneLookup.FindIncludingInactive(SanctumOnboardingV08Builder.RootName);
            MemoryForgeCheckpoint checkpoint = UnityEngine.Object.FindObjectOfType<MemoryForgeCheckpoint>(true);
            WorldStateLedger ledger = UnityEngine.Object.FindObjectOfType<WorldStateLedger>(true);
            OpeningExperienceDirectorV08 opening = UnityEngine.Object.FindObjectOfType<OpeningExperienceDirectorV08>(true);
            GameObject gateObject = EditorSceneLookup.FindIncludingInactive("Sanctum_Threshold_Gate_V08");
            JourneyGate threshold = gateObject != null ? gateObject.GetComponent<JourneyGate>() : null;
            if (sanctum == null || checkpoint == null || ledger == null || opening == null || threshold == null)
                throw new InvalidOperationException("Sanctum hero pass requires V0.8 sanctuary, Memory Forge, ledger, opening director and threshold gate.");

            Transform previous = sanctum.transform.Find(RootName);
            if (previous != null) UnityEngine.Object.DestroyImmediate(previous.gameObject);

            Material ivory = Require(SanctumMaterialAuthoringV08.Ivory);
            Material pearl = Require(SanctumMaterialAuthoringV08.Pearl);
            Material gold = Require(SanctumMaterialAuthoringV08.Gold);
            Material glass = Require(SanctumMaterialAuthoringV08.BlueGlass);
            Material cyan = Require("AetherCyan");
            Material green = Require("WispVerdant");

            GameObject root = new GameObject(RootName);
            root.transform.SetParent(sanctum.transform, false);
            BuildMemoryForge(root.transform, checkpoint, ivory, pearl, gold, glass, cyan, green);

            OpeningExperiencePersistenceV08 restore = root.AddComponent<OpeningExperiencePersistenceV08>();
            restore.ConfigureRuntime(ledger, opening, threshold);

            EditorUtility.SetDirty(root);
            EditorUtility.SetDirty(restore);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Mindforge:V08] Memory Forge re-presented as a bright sanctum altar at the existing checkpoint authority; opening phase/threshold restore bound to profile-v2 facts.");
        }

        private static void BuildMemoryForge(
            Transform parent,
            MemoryForgeCheckpoint checkpoint,
            Material ivory,
            Material pearl,
            Material gold,
            Material glass,
            Material cyan,
            Material green)
        {
            Transform anchor = checkpoint.InteractionPoint != null ? checkpoint.InteractionPoint : checkpoint.transform;
            Vector3 p = anchor.position;

            GameObject root = new GameObject("Memory_Forge_Sanctum_Altar_V08");
            root.transform.SetParent(parent, false);
            root.transform.position = p + new Vector3(-2.3f, 0f, 0.4f);

            Primitive("ForgeDais", PrimitiveType.Cylinder, root.transform,
                new Vector3(0f, 0.20f, 0f), new Vector3(2.25f, 0.20f, 2.25f), ivory, true);
            Primitive("ForgeDaisGold", PrimitiveType.Cylinder, root.transform,
                new Vector3(0f, 0.43f, 0f), new Vector3(1.86f, 0.08f, 1.86f), gold, false);
            Primitive("ForgePedestal", PrimitiveType.Cylinder, root.transform,
                new Vector3(0f, 1.10f, 0f), new Vector3(0.58f, 0.72f, 0.58f), pearl, false);
            GameObject core = Primitive("ForgeCore", PrimitiveType.Sphere, root.transform,
                new Vector3(0f, 2.10f, 0f), Vector3.one * 0.72f, cyan, false);

            for (int side = -1; side <= 1; side += 2)
            {
                Primitive("ForgeWing_" + side, PrimitiveType.Cube, root.transform,
                    new Vector3(side * 1.45f, 1.55f, 0f), new Vector3(0.25f, 3.1f, 0.48f), ivory, false,
                    new Vector3(0f, 0f, side * -12f));
                Primitive("ForgeWingGold_" + side, PrimitiveType.Cube, root.transform,
                    new Vector3(side * 1.26f, 1.65f, -0.08f), new Vector3(0.06f, 2.25f, 0.18f), gold, false,
                    new Vector3(0f, 0f, side * -12f));
                Primitive("ForgeMemoryNode_" + side, PrimitiveType.Sphere, root.transform,
                    new Vector3(side * 1.38f, 2.84f, 0f), Vector3.one * 0.26f, side < 0 ? cyan : green, false);
            }

            Ring("ForgeHaloOuter", root.transform, new Vector3(0f, 2.10f, 0f), 1.55f, 48, 0.055f, gold, Quaternion.Euler(74f, 18f, 0f));
            Ring("ForgeHaloInner", root.transform, new Vector3(0f, 2.10f, 0f), 1.18f, 42, 0.045f, glass, Quaternion.Euler(20f, 62f, 0f));

            Light light = core.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(0.34f, 0.86f, 1f);
            light.intensity = 1.25f;
            light.range = 8f;
            light.shadows = LightShadows.None;
        }

        private static GameObject Primitive(
            string name,
            PrimitiveType type,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            Material material,
            bool collider,
            Vector3? euler = null)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;
            if (euler.HasValue) go.transform.localRotation = Quaternion.Euler(euler.Value);
            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }
            if (!collider)
            {
                Collider shape = go.GetComponent<Collider>();
                if (shape != null) UnityEngine.Object.DestroyImmediate(shape);
            }
            return go;
        }

        private static void Ring(
            string name,
            Transform parent,
            Vector3 center,
            float radius,
            int segments,
            float width,
            Material material,
            Quaternion rotation)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            go.transform.localRotation = rotation;
            LineRenderer line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = segments;
            line.startWidth = width;
            line.endWidth = width;
            line.shadowCastingMode = ShadowCastingMode.Off;
            for (int i = 0; i < segments; i++)
            {
                float a = i / (float)segments * Mathf.PI * 2f;
                line.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius));
            }
        }

        private static Material Require(string name)
        {
            Material material = CinematicMaterialAuthoring.Load(name);
            if (material == null) material = SanctumMaterialAuthoringV08.Load(name);
            if (material == null) throw new InvalidOperationException("Required V0.8 material missing: " + name);
            return material;
        }
    }
}
#endif
