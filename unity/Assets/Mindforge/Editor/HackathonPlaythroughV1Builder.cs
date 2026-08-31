#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Mindforge.Journey;
using Mindforge.Presentation;
using Mindforge.World;

namespace Mindforge.Editor
{
    /// <summary>
    /// Hackathon-ready composition pass on top of the collision-qualified Aetheria V2 scene.
    /// It densifies every major district, stages the first ten-enemy encounter as 3/4/3,
    /// and installs richer hero/enemy presentation plus a reusable monotonic story seam.
    /// Existing world collision, JourneyEnemyController combat and BCI authority remain intact.
    /// </summary>
    public static class HackathonPlaythroughV1Builder
    {
        public const string RootName = "Mindforge_HackathonPlaythrough_V1";
        public const string Revision = "HACKATHON_PLAYTHROUGH_V1";

        private static readonly StaticEditorFlags VisualStatic =
            StaticEditorFlags.BatchingStatic |
            StaticEditorFlags.OccludeeStatic |
            StaticEditorFlags.ReflectionProbeStatic;

        private static readonly Vector3 CrucibleCenter = new Vector3(5f, 0f, 18f);

        [MenuItem("Mindforge/Legacy/Showcase/Apply Hackathon Playthrough V1", priority = 31)]
        public static void ApplyOpenScene()
        {
            GameObject aetheria = EditorSceneLookup.FindIncludingInactive(AetheriaWorldV1Builder.RootName);
            GameObject guardian = EditorSceneLookup.FindIncludingInactive("Guardian");
            ArenaMenagerieDirector menagerie = UnityEngine.Object.FindObjectOfType<ArenaMenagerieDirector>(true);
            if (aetheria == null || guardian == null || menagerie == null)
                throw new InvalidOperationException("Hackathon Playthrough V1 requires Aetheria World V1, Guardian and Arena Menagerie.");

            GameObject previous = EditorSceneLookup.FindIncludingInactive(RootName);
            if (previous != null) UnityEngine.Object.DestroyImmediate(previous);

            CinematicMaterialAuthoring.EnsureAuthored();
            Material obsidian = RequireMaterial("ObsidianArchitecture");
            Material basalt = RequireMaterial("ArenaBasalt");
            Material metal = RequireMaterial("GuardianMetal");
            Material cyan = RequireMaterial("AetherCyan");
            Material green = RequireMaterial("WispVerdant");
            Material violet = RequireMaterial("FracturedRing");
            Material hostile = RequireMaterial("FracturedCore");

            GameObject root = new GameObject(RootName);
            BuildArrivalCourt(root.transform, obsidian, metal, cyan, green);
            BuildCausewayMegastructure(root.transform, obsidian, metal, cyan, green);
            BuildBrokenMomentumBazaar(root.transform, basalt, obsidian, metal, cyan, violet, hostile);
            BuildRuinedChoir(root.transform, obsidian, metal, cyan, violet);
            Transform[] beacons = BuildCrucible(root.transform, basalt, obsidian, metal, cyan, violet, hostile, out Transform victoryCrown);
            BuildGravitasProcessional(root.transform, obsidian, metal, violet, hostile);
            BuildDistantAetheria(root.transform, obsidian, metal, cyan, violet);

            JourneyEnemyController[] ordered = ConfigureEncounter(menagerie, guardian.transform);
            InstallEnemyDetail(ordered);
            InstallGuardianDetail(guardian);
            InstallEncounterPresentation(menagerie, beacons, victoryCrown);
            InstallPlaythroughDirector(root, guardian.transform, menagerie);

            EditorUtility.SetDirty(root);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                "[Mindforge:Hackathon] Playthrough V1 ready: dense Prism Bastion arrival, cathedral-scale Causeway ribs, Broken Momentum bazaar, " +
                "Ruined Choir skyline, 3/4/3 Menagerie Crucible staging, Gravitas processional and distant Aetheria skyline. " +
                "All ten Menagerie roles receive unique V2 silhouette detail and the Guardian receives Prism Squire V2 presentation. " +
                "Gameplay/neural authority is unchanged.");
        }

        private static void BuildArrivalCourt(Transform parent, Material obsidian, Material metal, Material cyan, Material green)
        {
            Transform root = Zone(parent, "Hackathon_PrismBastionArrival");
            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < 4; i++)
                {
                    float z = -64f + i * 3.0f;
                    float x = side * (8.6f + i * 0.65f);
                    Part($"ArrivalButtress_{side}_{i}", PrimitiveType.Cube, root, new Vector3(x, 2.8f + i * 0.3f, z), new Vector3(1.2f, 5.6f + i * 0.6f, 1.8f), obsidian, new Vector3(0f, side * (8f + i * 3f), 0f));
                    Part($"ArrivalFin_{side}_{i}", PrimitiveType.Cube, root, new Vector3(x - side * 0.72f, 4.0f + i * 0.35f, z), new Vector3(0.12f, 3.2f, 0.50f), i % 2 == 0 ? cyan : green, new Vector3(0f, 0f, side * 12f));
                }
            }

            for (int i = 0; i < 7; i++)
            {
                float x = -6f + i * 2f;
                Part($"ArrivalPathInlay_{i}", PrimitiveType.Cube, root, new Vector3(x, 0.035f, -57.8f), new Vector3(1.2f, 0.035f, 0.12f), i % 2 == 0 ? cyan : green, new Vector3(0f, 12f * (i - 3), 0f));
            }

            Part("ArrivalHeroArchL", PrimitiveType.Cube, root, new Vector3(-4.2f, 5.1f, -53.4f), new Vector3(1.0f, 10.2f, 1.0f), metal, new Vector3(0f, 0f, -7f));
            Part("ArrivalHeroArchR", PrimitiveType.Cube, root, new Vector3(4.2f, 5.1f, -53.4f), new Vector3(1.0f, 10.2f, 1.0f), metal, new Vector3(0f, 0f, 7f));
            Part("ArrivalHeroArchCrown", PrimitiveType.Cube, root, new Vector3(0f, 9.6f, -53.4f), new Vector3(9.2f, 0.62f, 1.15f), obsidian, Vector3.zero);
        }

        private static void BuildCausewayMegastructure(Transform parent, Material obsidian, Material metal, Material cyan, Material green)
        {
            Transform root = Zone(parent, "Hackathon_NeonCausewayMegastructure");
            for (int i = 0; i < 9; i++)
            {
                float z = -51.0f + i * 2.65f;
                float height = 6.4f + Mathf.Sin(i * 0.7f) * 0.8f;
                for (int side = -1; side <= 1; side += 2)
                {
                    float x = side * 9.8f;
                    Part($"CausewayRib_{i}_{side}", PrimitiveType.Cube, root, new Vector3(x, height * 0.5f, z), new Vector3(0.72f, height, 0.72f), obsidian, new Vector3(0f, 0f, side * 5f));
                    Part($"CausewayRibSignal_{i}_{side}", PrimitiveType.Cube, root, new Vector3(x - side * 0.40f, height * 0.58f, z), new Vector3(0.06f, height * 0.55f, 0.12f), side < 0 ? cyan : green, Vector3.zero);
                }
                Part($"CausewayOverbeam_{i}", PrimitiveType.Cube, root, new Vector3(0f, height - 0.28f, z), new Vector3(20.4f, 0.44f, 0.70f), i % 3 == 0 ? metal : obsidian, Vector3.zero);
                Part($"CausewayCrownLight_{i}", PrimitiveType.Cube, root, new Vector3(0f, height - 0.02f, z), new Vector3(5.2f, 0.05f, 0.16f), i % 2 == 0 ? cyan : green, Vector3.zero);
            }

            for (int i = 0; i < 12; i++)
            {
                float z = -51.3f + i * 1.92f;
                float side = i % 2 == 0 ? -1f : 1f;
                Part($"CausewayUnderFin_{i}", PrimitiveType.Cube, root, new Vector3(side * 6.7f, 1.2f, z), new Vector3(0.22f, 2.4f, 1.18f), metal, new Vector3(0f, side * 18f, side * 28f));
            }
        }

        private static void BuildBrokenMomentumBazaar(Transform parent, Material basalt, Material obsidian, Material metal, Material cyan, Material violet, Material hostile)
        {
            Transform root = Zone(parent, "Hackathon_BrokenMomentumBazaar");
            Part("BazaarCentralPlinth", PrimitiveType.Cylinder, root, new Vector3(3.2f, 0.42f, -29.2f), new Vector3(3.8f, 0.42f, 3.8f), basalt, Vector3.zero);
            Part("BazaarMomentumCore", PrimitiveType.Sphere, root, new Vector3(3.2f, 2.5f, -29.2f), new Vector3(1.05f, 1.05f, 1.05f), hostile, Vector3.zero);
            for (int i = 0; i < 8; i++)
            {
                float a = i / 8f * Mathf.PI * 2f;
                Vector3 p = new Vector3(3.2f + Mathf.Cos(a) * 2.6f, 2.5f + Mathf.Sin(a * 2f) * 0.16f, -29.2f + Mathf.Sin(a) * 2.6f);
                Part($"BazaarCoreOrbit_{i}", PrimitiveType.Cube, root, p, new Vector3(0.16f, 0.48f, 0.16f), i % 2 == 0 ? cyan : violet, new Vector3(i * 17f, i * 29f, i * 9f));
            }

            for (int stall = 0; stall < 10; stall++)
            {
                float side = stall % 2 == 0 ? -1f : 1f;
                float z = -35.0f + (stall / 2) * 2.8f;
                float x = side * (13.2f + (stall % 3) * 0.75f);
                Part($"BazaarStallShell_{stall}", PrimitiveType.Cube, root, new Vector3(x, 1.35f, z), new Vector3(3.0f, 2.7f, 2.0f), obsidian, new Vector3(0f, -side * 5f, 0f));
                Part($"BazaarStallCanopy_{stall}", PrimitiveType.Cube, root, new Vector3(x - side * 0.55f, 2.82f, z), new Vector3(3.6f, 0.18f, 2.4f), stall % 2 == 0 ? violet : metal, new Vector3(0f, 0f, side * 4f));
                Part($"BazaarStallSignal_{stall}", PrimitiveType.Cube, root, new Vector3(x - side * 1.53f, 1.55f, z), new Vector3(0.05f, 1.5f, 1.20f), stall % 3 == 0 ? hostile : cyan, Vector3.zero);
            }
        }

        private static void BuildRuinedChoir(Transform parent, Material obsidian, Material metal, Material cyan, Material violet)
        {
            Transform root = Zone(parent, "Hackathon_RuinedChoirSkyline");
            for (int i = 0; i < 8; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                float z = -20f + (i / 2) * 4.5f;
                float x = side * (14.0f + (i / 2) * 2.1f);
                float h = 10.0f + i * 0.8f;
                Part($"ChoirMegaSpine_{i}", PrimitiveType.Cube, root, new Vector3(x, h * 0.5f, z), new Vector3(1.1f, h, 1.1f), obsidian, new Vector3(0f, side * 7f, 0f));
                Part($"ChoirMegaForkL_{i}", PrimitiveType.Cube, root, new Vector3(x - 0.82f, h - 0.5f, z), new Vector3(0.28f, 4.0f, 0.44f), metal, new Vector3(0f, 0f, -12f));
                Part($"ChoirMegaForkR_{i}", PrimitiveType.Cube, root, new Vector3(x + 0.82f, h - 0.5f, z), new Vector3(0.28f, 4.0f, 0.44f), metal, new Vector3(0f, 0f, 12f));
                Part($"ChoirMegaSignal_{i}", PrimitiveType.Cube, root, new Vector3(x, h * 0.62f, z - side * 0.58f), new Vector3(0.07f, 4.5f, 0.07f), i % 2 == 0 ? cyan : violet, Vector3.zero);
            }

            for (int i = 0; i < 5; i++)
            {
                float z = -18f + i * 3.7f;
                Part($"ChoirHangingBell_{i}", PrimitiveType.Sphere, root, new Vector3((i - 2) * 2.1f, 5.8f + (i % 2) * 0.7f, z), new Vector3(0.52f, 0.72f, 0.52f), i % 2 == 0 ? violet : cyan, Vector3.zero);
                Part($"ChoirBellStem_{i}", PrimitiveType.Cube, root, new Vector3((i - 2) * 2.1f, 7.5f + (i % 2) * 0.7f, z), new Vector3(0.08f, 2.8f, 0.08f), metal, Vector3.zero);
            }
        }

        private static Transform[] BuildCrucible(Transform parent, Material basalt, Material obsidian, Material metal, Material cyan, Material violet, Material hostile, out Transform victoryCrown)
        {
            Transform root = Zone(parent, "Hackathon_MenagerieCrucible");

            for (int ring = 0; ring < 3; ring++)
            {
                float radius = 8.6f + ring * 2.2f;
                for (int i = 0; i < 16; i++)
                {
                    float a = i / 16f * Mathf.PI * 2f;
                    Vector3 p = CrucibleCenter + new Vector3(Mathf.Cos(a) * radius, 0.18f + ring * 0.42f, Mathf.Sin(a) * radius);
                    Part($"CrucibleTerrace_{ring}_{i}", PrimitiveType.Cube, root, p, new Vector3(1.72f, 0.30f + ring * 0.20f, 1.10f), ring == 0 ? basalt : obsidian, new Vector3(0f, -a * Mathf.Rad2Deg, 0f));
                }
            }

            for (int i = 0; i < 12; i++)
            {
                float a = i / 12f * Mathf.PI * 2f;
                Vector3 p = CrucibleCenter + new Vector3(Mathf.Cos(a) * 11.6f, 3.0f, Mathf.Sin(a) * 11.6f);
                Part($"CrucibleBannerMast_{i}", PrimitiveType.Cube, root, p, new Vector3(0.18f, 5.6f, 0.18f), metal, Vector3.zero);
                Vector3 banner = p + new Vector3(-Mathf.Sin(a) * 0.38f, 0.75f, Mathf.Cos(a) * 0.38f);
                Part($"CrucibleBanner_{i}", PrimitiveType.Cube, root, banner, new Vector3(0.72f, 1.30f, 0.05f), i % 3 == 0 ? hostile : i % 2 == 0 ? cyan : violet, new Vector3(0f, -a * Mathf.Rad2Deg, 0f));
            }

            Transform[] beacons = new Transform[3];
            for (int wave = 0; wave < 3; wave++)
            {
                float a = (-60f + wave * 60f) * Mathf.Deg2Rad;
                Transform beacon = Zone(root, $"HackathonWaveBeacon_{wave}");
                beacon.localPosition = CrucibleCenter + new Vector3(Mathf.Cos(a) * 7.8f, 2.7f, Mathf.Sin(a) * 7.8f);
                Part("Pillar", PrimitiveType.Cylinder, beacon, Vector3.zero, new Vector3(0.42f, 2.4f, 0.42f), metal, Vector3.zero);
                Part("Signal", PrimitiveType.Sphere, beacon, new Vector3(0f, 2.45f, 0f), new Vector3(0.58f, 0.58f, 0.58f), wave == 0 ? cyan : wave == 1 ? violet : hostile, Vector3.zero);
                beacons[wave] = beacon;
            }

            victoryCrown = Zone(root, "HackathonVictoryCrown");
            victoryCrown.localPosition = CrucibleCenter + new Vector3(0f, 4.6f, 0f);
            for (int i = 0; i < 8; i++)
            {
                float a = i / 8f * Mathf.PI * 2f;
                Part($"VictoryShard_{i}", PrimitiveType.Cube, victoryCrown, new Vector3(Mathf.Cos(a) * 1.4f, Mathf.Sin(a * 2f) * 0.18f, Mathf.Sin(a) * 1.4f), new Vector3(0.16f, 0.70f, 0.16f), i % 2 == 0 ? cyan : violet, new Vector3(i * 12f, i * 45f, i * 8f));
            }
            return beacons;
        }

        private static void BuildGravitasProcessional(Transform parent, Material obsidian, Material metal, Material violet, Material hostile)
        {
            Transform root = Zone(parent, "Hackathon_GravitasProcessional");
            for (int i = 0; i < 6; i++)
            {
                float z = 0.5f + i * 3.2f;
                for (int side = -1; side <= 1; side += 2)
                {
                    float x = side * (15.6f + i * 0.55f);
                    float h = 9.0f + i * 1.1f;
                    Part($"GravitasBladeMonolith_{i}_{side}", PrimitiveType.Cube, root, new Vector3(x, h * 0.5f, z), new Vector3(0.72f, h, 1.6f), obsidian, new Vector3(0f, side * 8f, side * 7f));
                    Part($"GravitasBladeEdge_{i}_{side}", PrimitiveType.Cube, root, new Vector3(x - side * 0.42f, h * 0.62f, z - 0.35f), new Vector3(0.06f, h * 0.62f, 0.10f), i == 5 ? hostile : violet, Vector3.zero);
                }
            }
            Part("GravitasFinalLintel", PrimitiveType.Cube, root, new Vector3(0f, 12.8f, 15.8f), new Vector3(28f, 0.72f, 1.8f), metal, Vector3.zero);
            Part("GravitasFinalScar", PrimitiveType.Cube, root, new Vector3(0f, 12.3f, 14.84f), new Vector3(7.2f, 0.08f, 0.05f), hostile, new Vector3(0f, 0f, 9f));
        }

        private static void BuildDistantAetheria(Transform parent, Material obsidian, Material metal, Material cyan, Material violet)
        {
            Transform root = Zone(parent, "Hackathon_DistantAetheria");
            for (int i = 0; i < 28; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                float lane = i / 2;
                float x = side * (28f + (lane % 4f) * 3.5f);
                float z = -65f + lane * 7.2f;
                float h = 6f + (i * 17 % 9) * 1.35f;
                Part($"FarAetherSpire_{i}", PrimitiveType.Cube, root, new Vector3(x, h * 0.5f, z), new Vector3(1.6f + (i % 3) * 0.45f, h, 1.6f), obsidian, new Vector3(0f, side * (i * 13 % 24), 0f));
                Part($"FarAetherCrown_{i}", PrimitiveType.Cube, root, new Vector3(x, h + 0.35f, z), new Vector3(2.3f, 0.16f, 2.3f), metal, new Vector3(0f, 45f, 0f));
                if (i % 2 == 0)
                    Part($"FarAetherSignal_{i}", PrimitiveType.Cube, root, new Vector3(x - side * 0.84f, h * 0.62f, z), new Vector3(0.05f, h * 0.42f, 0.07f), i % 4 == 0 ? cyan : violet, Vector3.zero);
            }
        }

        private static JourneyEnemyController[] ConfigureEncounter(ArenaMenagerieDirector director, Transform guardian)
        {
            JourneyEnemyController[] source = director.GetComponentsInChildren<JourneyEnemyController>(true);
            string[] names =
            {
                "Menagerie_ScrapGoblin",
                "Menagerie_Shardsinger",
                "Menagerie_BassGolem",
                "Menagerie_ChromePenitent",
                "Menagerie_RiftStalker",
                "Menagerie_ChoirDrone",
                "Menagerie_AeroGargoyle",
                "Menagerie_PrismMaw",
                "Menagerie_VeilReaper",
                "Menagerie_OrbitSeraph",
            };
            Vector3[] positions =
            {
                CrucibleCenter + new Vector3(-5.2f, -0.30f, -4.8f),
                CrucibleCenter + new Vector3(5.2f, 1.02f, -4.8f),
                CrucibleCenter + new Vector3(0f, -0.30f, 5.2f),
                CrucibleCenter + new Vector3(-5.8f, -0.30f, -1.0f),
                CrucibleCenter + new Vector3(5.8f, -0.30f, -1.0f),
                CrucibleCenter + new Vector3(-3.0f, 1.02f, 5.2f),
                CrucibleCenter + new Vector3(3.0f, 1.45f, 5.2f),
                CrucibleCenter + new Vector3(-5.5f, -0.30f, 2.8f),
                CrucibleCenter + new Vector3(0f, -0.30f, 6.2f),
                CrucibleCenter + new Vector3(5.5f, 1.02f, 2.8f),
            };

            JourneyEnemyController[] ordered = new JourneyEnemyController[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                ordered[i] = Find(source, names[i]);
                if (ordered[i] == null)
                    throw new InvalidOperationException($"Hackathon encounter missing required enemy '{names[i]}'.");
                ordered[i].transform.position = positions[i];
                EditorUtility.SetDirty(ordered[i].gameObject);
            }

            Transform activation = director.transform.Find("Menagerie_Activation");
            if (activation == null)
                throw new InvalidOperationException("Hackathon encounter missing Menagerie_Activation marker.");
            director.ConfigureRuntime(guardian, activation, ordered, new[] { 3, 4, 3 });
            EditorUtility.SetDirty(director);
            return ordered;
        }

        private static JourneyEnemyController Find(JourneyEnemyController[] enemies, string exactName)
        {
            for (int i = 0; i < enemies.Length; i++)
            {
                JourneyEnemyController enemy = enemies[i];
                if (enemy != null && string.Equals(enemy.name, exactName, StringComparison.Ordinal)) return enemy;
            }
            return null;
        }

        private static void InstallEnemyDetail(JourneyEnemyController[] ordered)
        {
            for (int i = 0; i < ordered.Length; i++)
            {
                JourneyEnemyController enemy = ordered[i];
                HackathonEnemyPresentationV1 detail = enemy.GetComponent<HackathonEnemyPresentationV1>();
                if (detail == null) detail = enemy.gameObject.AddComponent<HackathonEnemyPresentationV1>();
                detail.Configure((HackathonEnemyIdentity)i);
                EditorUtility.SetDirty(detail);
            }
        }

        private static void InstallGuardianDetail(GameObject guardian)
        {
            if (guardian.GetComponent<PrismSquirePresentationV2>() == null)
                guardian.AddComponent<PrismSquirePresentationV2>();
            EditorUtility.SetDirty(guardian);
        }

        private static void InstallEncounterPresentation(ArenaMenagerieDirector director, Transform[] beacons, Transform crown)
        {
            HackathonEncounterPresentationV1 presentation = director.GetComponent<HackathonEncounterPresentationV1>();
            if (presentation == null) presentation = director.gameObject.AddComponent<HackathonEncounterPresentationV1>();
            presentation.ConfigureRuntime(director, beacons, crown);
            EditorUtility.SetDirty(presentation);
        }

        private static void InstallPlaythroughDirector(GameObject root, Transform guardian, ArenaMenagerieDirector menagerie)
        {
            HackathonPlaythroughDirectorV1 director = root.GetComponent<HackathonPlaythroughDirectorV1>();
            if (director == null) director = root.AddComponent<HackathonPlaythroughDirectorV1>();
            director.ConfigureRuntime(guardian, menagerie);
            EditorUtility.SetDirty(director);
        }

        private static Transform Zone(Transform parent, string name)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        private static GameObject Part(string name, PrimitiveType primitive, Transform parent, Vector3 position, Vector3 scale, Material material, Vector3 euler)
        {
            GameObject go = GameObject.CreatePrimitive(primitive);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = scale;
            Collider collider = go.GetComponent<Collider>();
            if (collider != null) UnityEngine.Object.DestroyImmediate(collider);
            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }
            GameObjectUtility.SetStaticEditorFlags(go, VisualStatic);
            return go;
        }

        private static Material RequireMaterial(string key)
        {
            Material material = CinematicMaterialAuthoring.Load(key);
            if (material == null)
                throw new InvalidOperationException($"Hackathon Playthrough V1 missing cinematic material '{key}'.");
            return material;
        }
    }
}
#endif
