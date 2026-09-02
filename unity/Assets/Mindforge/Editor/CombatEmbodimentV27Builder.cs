#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Mindforge.Presentation;
using Mindforge.World;

namespace Mindforge.Editor
{
    /// <summary>
    /// V0.27 encounter-stage authoring.
    ///
    /// V0.11/V0.23 remain gameplay/collision authority, V0.24 remains cathedral layout,
    /// V0.25 remains sensory/post authority and V0.26 remains production world rendering.
    /// V0.27 adds a collider-free boss-stage visual grammar that supports the new animalistic
    /// Fractured Signal presentation without shrinking or re-authoring the proven fight space.
    /// </summary>
    public static class CombatEmbodimentV27Builder
    {
        public const string RootName = "Mindforge_Combat_Embodiment_V27";
        public const string ArenaRootName = "V27_Fractured_Signal_Arena";
        private const float ArenaCenterZ = 94f;
        private const float ArenaFloorY = 4.095f;

        public static bool PresentInOpenScene()
            => EditorSceneLookup.FindIncludingInactive(RootName) != null;

        public static void ApplyOpenScene()
        {
            GameObject canonical = EditorSceneLookup.FindIncludingInactive(MindforgeDemoV11Builder.RootName);
            if (canonical == null)
                throw new UnityEditor.Build.BuildFailedException(
                    $"V0.27 requires canonical world '{MindforgeDemoV11Builder.RootName}'.");
            if (!WorldRenderingV26Builder.PresentInOpenScene())
                throw new UnityEditor.Build.BuildFailedException(
                    "V0.27 must compose after V0.26 Production Geometry + Cathedral Depth.");

            Apply(canonical.transform);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                "[Mindforge:V27] Combat embodiment authored: Fractured Signal arena rite geometry, " +
                "phase-response spines, encounter-local lighting and beast altar framing installed without collision authority.");
        }

        public static void Apply(Transform canonicalRoot)
        {
            if (canonicalRoot == null) throw new ArgumentNullException(nameof(canonicalRoot));
            Transform previous = canonicalRoot.Find(RootName);
            if (previous != null) UnityEngine.Object.DestroyImmediate(previous.gameObject);

            CathedralMaterialLibraryV24.Palette palette = CathedralMaterialLibraryV24.Ensure();
            Transform root = CathedralModuleLibraryV24.Node(RootName, canonicalRoot);
            Transform arena = CathedralModuleLibraryV24.Node(ArenaRootName, root);

            BuildRiteFloor(arena, palette);
            BuildPerimeterSignalSpines(arena, palette);
            BuildBeastAltarFrame(arena, palette);
            BuildEncounterLights(arena);

            FracturedArenaDynamicsV27 dynamics = arena.gameObject.AddComponent<FracturedArenaDynamicsV27>();
            if (dynamics == null)
                throw new UnityEditor.Build.BuildFailedException("V0.27 failed to install arena dynamics presentation.");

            ConfigureRenderers(root);
            Validate(root, arena);
        }

        private static void BuildRiteFloor(Transform arena, CathedralMaterialLibraryV24.Palette palette)
        {
            Transform rites = CathedralModuleLibraryV24.Node("V27_RiteFloor", arena);
            const int ringSegments = 20;
            const float ringRadius = 7.4f;
            for (int i = 0; i < ringSegments; i++)
            {
                float angle = i / (float)ringSegments * 360f;
                float rad = angle * Mathf.Deg2Rad;
                Vector3 p = new Vector3(Mathf.Sin(rad) * ringRadius, ArenaFloorY + 0.026f, ArenaCenterZ + Mathf.Cos(rad) * ringRadius);
                CathedralModuleLibraryV24.Trim(
                    $"V27_RiteRing_{i:00}", rites, p,
                    new Vector3(0.075f, 0.018f, 2.34f), palette.SacredGold,
                    new Vector3(0f, angle + 90f, 0f));
            }

            const int axes = 8;
            for (int i = 0; i < axes; i++)
            {
                float angle = i / (float)axes * 360f;
                float rad = angle * Mathf.Deg2Rad;
                Vector3 radial = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));
                Vector3 p = new Vector3(0f, ArenaFloorY + 0.032f, ArenaCenterZ) + radial * 10.7f;
                CathedralModuleLibraryV24.Trim(
                    $"V27_SignalRiteAxis_{i:00}", rites, p,
                    new Vector3(0.070f, 0.020f, 6.0f), palette.SignalMagenta,
                    new Vector3(0f, angle, 0f));
            }

            // A second broken ring outside the main duel footprint gives the boss a ritual stage
            // without adding a curb or collider the Guardian can snag on.
            const int outerSegments = 16;
            const float outerRadius = 13.8f;
            for (int i = 0; i < outerSegments; i++)
            {
                if (i == 8 || i == 9) continue; // preserve the south/processional entrance read.
                float angle = i / (float)outerSegments * 360f;
                float rad = angle * Mathf.Deg2Rad;
                Vector3 p = new Vector3(Mathf.Sin(rad) * outerRadius, ArenaFloorY + 0.020f, ArenaCenterZ + Mathf.Cos(rad) * outerRadius);
                CathedralModuleLibraryV24.Trim(
                    $"V27_OuterRite_{i:00}", rites, p,
                    new Vector3(0.055f, 0.016f, 3.15f), palette.LumenCyan,
                    new Vector3(0f, angle + 90f, 0f));
            }
        }

        private static void BuildPerimeterSignalSpines(Transform arena, CathedralMaterialLibraryV24.Palette palette)
        {
            Transform spines = CathedralModuleLibraryV24.Node("V27_PhaseSpines", arena);
            Mesh shell = ProductionGeometryV26.TaperedButtress();
            const int count = 10;
            const float radius = 16.3f;
            for (int i = 0; i < count; i++)
            {
                float angle = (i / (float)count * 360f + 18f) * Mathf.Deg2Rad;
                Vector3 radial = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
                Vector3 p = new Vector3(radial.x * radius, ArenaFloorY + 2.1f, ArenaCenterZ + radial.z * radius);
                Transform spine = MeshPart(
                    $"V27_CorruptionSpine_{i:00}", shell, spines, p,
                    new Vector3(0.72f, 4.15f + (i % 3) * 0.42f, 0.72f),
                    palette.FractureDark,
                    Quaternion.Euler(0f, -Mathf.Atan2(radial.x, radial.z) * Mathf.Rad2Deg, 0f),
                    CathedralRoleV24.StructuralRole.MysticAccent);

                CathedralModuleLibraryV24.Trim(
                    "SignalVein", spine, new Vector3(0f, 0f, -0.37f),
                    new Vector3(0.075f, 0.72f, 0.035f), palette.SignalMagenta,
                    Vector3.zero);
            }
        }

        private static void BuildBeastAltarFrame(Transform arena, CathedralMaterialLibraryV24.Palette palette)
        {
            Transform altar = CathedralModuleLibraryV24.Node("V27_Beast_Altar_Frame", arena);
            const float z = ArenaCenterZ + 15.4f;

            Mesh block = ProductionGeometryV26.ChamferedBlock();
            MeshPart("V27_AltarLeft", block, altar,
                new Vector3(-6.8f, ArenaFloorY + 2.6f, z), new Vector3(1.35f, 5.2f, 1.25f),
                palette.IvoryStone, Quaternion.Euler(0f, -12f, 0f), CathedralRoleV24.StructuralRole.DecorativePatina);
            MeshPart("V27_AltarRight", block, altar,
                new Vector3(6.8f, ArenaFloorY + 2.6f, z), new Vector3(1.35f, 5.2f, 1.25f),
                palette.IvoryStone, Quaternion.Euler(0f, 12f, 0f), CathedralRoleV24.StructuralRole.DecorativePatina);

            CathedralModuleLibraryV24.PointedArch(
                "V27_Beast_Altar_Arch", altar,
                new Vector3(0f, ArenaFloorY + 6.35f, z + 0.35f),
                new Vector3(8.0f, 5.9f, 0.72f), palette.WhiteMarble);

            CathedralModuleLibraryV24.Trim(
                "V27_AltarSignalScar", altar,
                new Vector3(0f, ArenaFloorY + 5.15f, z + 0.78f),
                new Vector3(0.14f, 4.8f, 0.065f), palette.SignalMagenta,
                new Vector3(0f, 0f, -7f));

            CathedralModuleLibraryV24.Trim(
                "V27_AltarLumenCrossbar", altar,
                new Vector3(0f, ArenaFloorY + 3.4f, z + 0.80f),
                new Vector3(4.9f, 0.09f, 0.055f), palette.LumenCyan,
                Vector3.zero);
        }

        private static void BuildEncounterLights(Transform arena)
        {
            Transform lights = CathedralModuleLibraryV24.Node("V27_EncounterLights", arena);
            const int count = 6;
            for (int i = 0; i < count; i++)
            {
                float angle = (i / (float)count * 360f + 30f) * Mathf.Deg2Rad;
                Vector3 p = new Vector3(Mathf.Sin(angle) * 13.6f, ArenaFloorY + 6.8f, ArenaCenterZ + Mathf.Cos(angle) * 13.6f);
                GameObject go = new GameObject($"V27_ArenaLight_{i:00}");
                go.transform.SetParent(lights, false);
                go.transform.localPosition = p;
                Light light = go.AddComponent<Light>();
                light.type = LightType.Point;
                bool rear = p.z > ArenaCenterZ + 3f;
                light.color = rear ? new Color(0.78f, 0.18f, 0.68f) : new Color(0.20f, 0.55f, 0.72f);
                light.range = 12.5f;
                light.intensity = rear ? 1.10f : 0.82f;
                light.shadows = LightShadows.None;
                light.renderMode = LightRenderMode.Auto;
            }
        }

        private static Transform MeshPart(
            string name,
            Mesh mesh,
            Transform parent,
            Vector3 position,
            Vector3 scale,
            Material material,
            Quaternion rotation,
            CathedralRoleV24.StructuralRole role)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localRotation = rotation;
            go.transform.localScale = scale;
            MeshFilter filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            MeshRenderer renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
            CathedralRoleV24 marker = go.AddComponent<CathedralRoleV24>();
            marker.Configure(role);
            return go.transform;
        }

        private static void ConfigureRenderers(Transform root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null) continue;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }
        }

        private static void Validate(Transform root, Transform arena)
        {
            if (root.GetComponentsInChildren<Collider>(true).Length != 0)
                throw new UnityEditor.Build.BuildFailedException("V0.27 encounter presentation must remain collider-free.");
            if (root.GetComponentsInChildren<Rigidbody>(true).Length != 0)
                throw new UnityEditor.Build.BuildFailedException("V0.27 encounter presentation must remain Rigidbody-free.");

            int spines = 0;
            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
                if (all[i] != null && all[i].name.StartsWith("V27_CorruptionSpine_", StringComparison.Ordinal)) spines++;
            if (spines < 8)
                throw new UnityEditor.Build.BuildFailedException($"V0.27 expected at least 8 phase spines, found {spines}.");
            if (arena.GetComponent<FracturedArenaDynamicsV27>() == null)
                throw new UnityEditor.Build.BuildFailedException("V0.27 arena dynamics presentation missing.");
        }
    }
}
#endif
