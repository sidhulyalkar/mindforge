#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Mindforge.Journey;
using Mindforge.World;

namespace Mindforge.Editor
{
    /// <summary>
    /// Converts the V0.8 macro-layout into the sharper generated-reference language:
    /// crisp architectural edge hierarchy, pointed cathedral ribs, believable road/door
    /// spacing, explicit wayfinding, layered city depth and unmistakable enemy silhouettes.
    /// Everything authored here is presentation-only. Existing floors, walls, gates,
    /// enemy controllers, colliders, interactions and neural authority remain canonical.
    /// </summary>
    [InitializeOnLoad]
    public static class SanctumReferenceFidelityV08Builder
    {
        public const string RootName = "Sanctum_Reference_Fidelity_V08";
        public const string EnemyRootName = "ReferenceSilhouetteV08";
        public const float HallClearHalfWidth = 5.0f;
        public const float TerraceClearHalfWidth = 5.25f;
        public const float CourtClearHalfWidth = 8.0f;
        public const float MinimumOpeningSentrySpacing = 10.0f;
        public const float ProcessionalRoadWidth = 9.5f;

        private static readonly StaticEditorFlags VisualStatic =
            StaticEditorFlags.BatchingStatic |
            StaticEditorFlags.OccludeeStatic |
            StaticEditorFlags.ReflectionProbeStatic;

        private static bool _applying;

        static SanctumReferenceFidelityV08Builder()
        {
            EditorApplication.delayCall += TryAutoApply;
            EditorSceneManager.sceneSaved += _ => TryAutoApply();
        }

        [MenuItem("Mindforge/Legacy/Showcase/Apply Sanctum Reference Fidelity V0.8", priority = 38)]
        public static void ApplyOpenScene()
        {
            if (_applying || EditorApplication.isPlayingOrWillChangePlaymode) return;
            _applying = true;
            try
            {
                GameObject sanctum = EditorSceneLookup.FindIncludingInactive(SanctumOnboardingV08Builder.RootName);
                GameObject ward = EditorSceneLookup.FindIncludingInactive(NullWardSceneBuilder.RootName);
                if (sanctum == null || ward == null)
                    throw new InvalidOperationException("Reference fidelity V0.8 requires the authored Sanctum and Null Ward roots.");

                SanctumReferenceMaterialAuthoringV08.EnsureAuthored();
                Material ivory = Require(SanctumMaterialAuthoringV08.Ivory);
                Material pearl = Require(SanctumMaterialAuthoringV08.Pearl);
                Material gold = Require(SanctumMaterialAuthoringV08.Gold);
                Material glass = Require(SanctumMaterialAuthoringV08.BlueGlass);
                Material garden = Require(SanctumMaterialAuthoringV08.Garden);
                Material edge = Require(SanctumReferenceMaterialAuthoringV08.EdgeDark);
                Material warm = Require(SanctumReferenceMaterialAuthoringV08.WarmStone);
                Material enemyCeramic = Require(SanctumReferenceMaterialAuthoringV08.EnemyCeramic);
                Material threatAmber = Require(SanctumReferenceMaterialAuthoringV08.ThreatAmber);
                Material threatWhite = Require(SanctumReferenceMaterialAuthoringV08.ThreatWhite);

                Transform previous = sanctum.transform.Find(RootName);
                if (previous != null) UnityEngine.Object.DestroyImmediate(previous.gameObject);

                GameObject root = new GameObject(RootName);
                root.transform.SetParent(sanctum.transform, false);

                RepositionResonanceStations(sanctum.transform);
                BuildArchitecturalDefinition(root.transform, ivory, pearl, gold, glass, edge, warm);
                BuildNavigationLanguage(root.transform, ivory, pearl, gold, edge, warm);
                BuildLayeredWorldVista(root.transform, ivory, pearl, gold, glass, garden, edge, warm);
                int enemyCount = BuildReferenceEnemySilhouettes(ward.transform, enemyCeramic, edge, threatAmber, threatWhite);

                SanctumVisualClarityV08 clarity = sanctum.GetComponent<SanctumVisualClarityV08>();
                if (clarity == null) clarity = sanctum.AddComponent<SanctumVisualClarityV08>();
                clarity.ConfigureRuntime(UnityEngine.Object.FindObjectOfType<Camera>(true));

                ValidateProtectedClearance(sanctum.transform);
                ValidateOpeningEnemySpacing(ward.transform);

                EditorUtility.SetDirty(root);
                EditorUtility.SetDirty(sanctum);
                EditorUtility.SetDirty(clarity);
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                EditorSceneManager.SaveOpenScenes();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log(
                    $"[Mindforge:V08:Fidelity] Generated-reference pass applied: pointed ribs, crisp shadow reveals, protected navigation spine, " +
                    $"layered road/canal/cathedral vista and {enemyCount} collider-free enemy silhouettes. " +
                    "Central travel clearance remains obstacle-free and cyan/green remain reserved for neural meaning.");
            }
            finally
            {
                _applying = false;
            }
        }

        private static void TryAutoApply()
        {
            if (_applying || EditorApplication.isPlayingOrWillChangePlaymode) return;
            GameObject sanctum = EditorSceneLookup.FindIncludingInactive(SanctumOnboardingV08Builder.RootName);
            if (sanctum == null || sanctum.transform.Find(RootName) != null) return;
            if (EditorSceneLookup.FindIncludingInactive(NullWardSceneBuilder.RootName) == null) return;
            ApplyOpenScene();
        }

        private static void RepositionResonanceStations(Transform sanctum)
        {
            // Treat calibration as side chapels. The processional axis stays fully open for
            // camera orbit, running, dodge rolls, double jump, hover and air-dash.
            MoveDeepChild(sanctum, "Resonance_Station_01_8Hz", new Vector3(-8.4f, 0f, -56.7f));
            MoveDeepChild(sanctum, "Resonance_Station_02_10Hz", new Vector3(8.4f, 0f, -52.0f));
            MoveDeepChild(sanctum, "Resonance_Station_03_12Hz", new Vector3(-8.4f, 0f, -47.2f));
        }

        private static void BuildArchitecturalDefinition(
            Transform parent,
            Material ivory,
            Material pearl,
            Material gold,
            Material glass,
            Material edge,
            Material warm)
        {
            Transform root = Zone(parent, "Reference_Architecture");

            float[] hallSeams = { -61.4f, -58f, -54f, -50f, -46f, -42f, -39.4f };
            for (int i = 0; i < hallSeams.Length; i++)
            {
                Primitive($"HallFloorJoint_{i:00}", PrimitiveType.Cube, root,
                    new Vector3(0f, -0.155f, hallSeams[i]), new Vector3(29.2f, 0.018f, 0.055f), edge, false);
            }

            float[] bays = { -60f, -54f, -48f, -42f };
            for (int i = 0; i < bays.Length; i++)
            {
                float z = bays[i];
                for (int side = -1; side <= 1; side += 2)
                {
                    float x = side * 11.1f;
                    Primitive($"PierPlinth_{i}_{side}", PrimitiveType.Cube, root,
                        new Vector3(x, 0.16f, z), new Vector3(1.75f, 0.26f, 1.85f), warm, false);
                    Primitive($"PierBasePearl_{i}_{side}", PrimitiveType.Cube, root,
                        new Vector3(x, 0.38f, z), new Vector3(1.42f, 0.16f, 1.50f), pearl, false);
                    Primitive($"PierShadowReveal_{i}_{side}", PrimitiveType.Cube, root,
                        new Vector3(x - side * 0.535f, 5.35f, z - 0.59f), new Vector3(0.035f, 9.75f, 0.035f), edge, false);
                    Primitive($"PierGoldReveal_{i}_{side}", PrimitiveType.Cube, root,
                        new Vector3(x - side * 0.585f, 6.0f, z + 0.595f), new Vector3(0.045f, 7.6f, 0.05f), gold, false);
                    Primitive($"PierCapital_{i}_{side}", PrimitiveType.Cube, root,
                        new Vector3(x, 10.65f, z), new Vector3(1.58f, 0.24f, 1.55f), ivory, false);
                    Primitive($"OuterButtress_{i}_{side}", PrimitiveType.Cube, root,
                        new Vector3(side * 14.15f, 4.0f, z + 0.4f), new Vector3(0.55f, 7.2f, 2.65f), pearl, false,
                        new Vector3(0f, side * 7f, side * -4f));
                    BuildWindowLancet(root, $"Window_{i}_{side}", side * 14.82f, 5.8f, z + 2.25f, side, glass, gold, edge);
                }

                BuildPointedRib(root, $"PointedRib_{i:00}", new Vector3(0f, 7.0f, z - 0.72f),
                    10.6f, 5.35f, 28, ivory, 0.18f);
                BuildPointedRib(root, $"PointedGoldRib_{i:00}", new Vector3(0f, 7.05f, z - 0.66f),
                    10.15f, 4.95f, 28, gold, 0.055f);
            }

            // Layer the 12m threshold in depth while keeping the entire opening usable.
            for (int layer = 0; layer < 3; layer++)
            {
                float inset = layer * 0.26f;
                BuildPointedRib(root, $"ThresholdDeepRib_{layer}",
                    new Vector3(0f, 7.35f + layer * 0.10f, -38.62f - layer * 0.12f),
                    12.65f - inset, 6.55f - inset * 0.25f, 32,
                    layer == 1 ? gold : pearl, layer == 1 ? 0.075f : 0.22f);
            }

            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < 5; i++)
                {
                    float z = -60f + i * 4.6f;
                    Primitive($"WallPilaster_{side}_{i}", PrimitiveType.Cube, root,
                        new Vector3(side * 14.86f, 3.0f, z), new Vector3(0.12f, 5.5f, 0.72f), warm, false);
                    Primitive($"WallGlassInset_{side}_{i}", PrimitiveType.Cube, root,
                        new Vector3(side * 14.79f, 6.8f, z + 1.35f), new Vector3(0.035f, 2.75f, 1.10f), glass, false);
                }
            }
        }

        private static void BuildNavigationLanguage(
            Transform parent,
            Material ivory,
            Material pearl,
            Material gold,
            Material edge,
            Material warm)
        {
            Transform root = Zone(parent, "Reference_Navigation");

            // One quiet orientation axis is more useful than many floating arrows.
            Primitive("ProcessionalSpine", PrimitiveType.Cube, root,
                new Vector3(0f, -0.105f, -38.5f), new Vector3(0.16f, 0.025f, 45f), gold, false);
            Primitive("SpineShadow", PrimitiveType.Cube, root,
                new Vector3(0f, -0.132f, -38.5f), new Vector3(0.36f, 0.012f, 45f), edge, false);

            float[] nodeZ = { -58.5f, -52f, -45.5f, -38.8f, -31.0f, -24.2f, -17.8f };
            for (int i = 0; i < nodeZ.Length; i++)
            {
                Primitive($"RouteNode_{i:00}", PrimitiveType.Cylinder, root,
                    new Vector3(0f, -0.09f, nodeZ[i]), new Vector3(0.62f, 0.025f, 0.62f),
                    i % 2 == 0 ? pearl : ivory, false);
                Ring($"RouteNodeRing_{i:00}", root, new Vector3(0f, -0.045f, nodeZ[i]),
                    0.72f, 32, 0.035f, gold, Quaternion.identity);
            }

            RouteBranch(root, new Vector3(-4.8f, -0.08f, -56.7f), new Vector3(-8.1f, -0.08f, -56.7f), gold, "CalibrationBranch8");
            RouteBranch(root, new Vector3(4.8f, -0.08f, -52.0f), new Vector3(8.1f, -0.08f, -52.0f), gold, "CalibrationBranch10");
            RouteBranch(root, new Vector3(-4.8f, -0.08f, -47.2f), new Vector3(-8.1f, -0.08f, -47.2f), gold, "CalibrationBranch12");

            // A road, not a hallway: 9.5m carriage/processional width plus 2.4m walkways.
            Primitive("VistaProcessionalRoad", PrimitiveType.Cube, root,
                new Vector3(0f, -0.42f, 20f), new Vector3(ProcessionalRoadWidth, 0.18f, 72f), warm, false);
            Primitive("VistaRoadCenterJoint", PrimitiveType.Cube, root,
                new Vector3(0f, -0.31f, 20f), new Vector3(0.10f, 0.02f, 71f), gold, false);
            for (int side = -1; side <= 1; side += 2)
            {
                Primitive($"VistaWalkway_{side}", PrimitiveType.Cube, root,
                    new Vector3(side * 6.2f, -0.36f, 20f), new Vector3(2.4f, 0.14f, 72f), ivory, false);
                Primitive($"VistaWalkwayJoint_{side}", PrimitiveType.Cube, root,
                    new Vector3(side * 4.92f, -0.28f, 20f), new Vector3(0.07f, 0.025f, 72f), edge, false);
            }
        }

        private static void BuildLayeredWorldVista(
            Transform parent,
            Material ivory,
            Material pearl,
            Material gold,
            Material glass,
            Material garden,
            Material edge,
            Material warm)
        {
            Transform root = Zone(parent, "Reference_World_Vista");

            // Near layer: broad planted terraces make the city feel inhabitable.
            for (int side = -1; side <= 1; side += 2)
            {
                Primitive($"NearGardenTerrace_{side}", PrimitiveType.Cube, root,
                    new Vector3(side * 12.0f, -0.25f, 8f), new Vector3(9.5f, 0.42f, 18f), warm, false);
                Primitive($"NearGardenWall_{side}", PrimitiveType.Cube, root,
                    new Vector3(side * 7.2f, 0.55f, 8f), new Vector3(0.24f, 1.25f, 18f), ivory, false);
                for (int i = 0; i < 4; i++)
                {
                    float z = 2f + i * 4f;
                    BuildSignalCypress(root, $"Cypress_{side}_{i}",
                        new Vector3(side * (10.0f + (i % 2) * 2.6f), 0f, z), garden, gold);
                }
            }

            // Mid layer: wide bridge and sanctuary masses provide scale before the skyline.
            Primitive("CityBridgeDeck", PrimitiveType.Cube, root,
                new Vector3(0f, 0.35f, 14f), new Vector3(16f, 0.34f, 5.4f), ivory, false);
            for (int side = -1; side <= 1; side += 2)
            {
                Primitive($"CityBridgeParapet_{side}", PrimitiveType.Cube, root,
                    new Vector3(side * 7.7f, 1.05f, 14f), new Vector3(0.30f, 1.25f, 5.4f), pearl, false);
                Primitive($"MidSanctumBlock_{side}", PrimitiveType.Cube, root,
                    new Vector3(side * 18f, 5.0f, 27f), new Vector3(11f, 10f, 15f), side < 0 ? ivory : pearl, false);
                for (int rib = 0; rib < 3; rib++)
                {
                    BuildPointedRib(root, $"MidBlockRib_{side}_{rib}",
                        new Vector3(side * 18f, 6f, 20.1f + rib * 5.5f), 6.8f, 5.5f, 22,
                        rib == 1 ? gold : edge, rib == 1 ? 0.09f : 0.12f);
                }
            }

            // Far layer: structured phase geometry is infrastructure rather than generic fog.
            BuildSkyPhaseRing(root, "FarPhaseRing_A", new Vector3(-24f, 24f, 52f), 8.5f, gold, glass, 0f);
            BuildSkyPhaseRing(root, "FarPhaseRing_B", new Vector3(28f, 31f, 65f), 11.0f, gold, glass, 17f);
            BuildSkyPhaseRing(root, "FarPhaseRing_C", new Vector3(2f, 38f, 82f), 14.0f, pearl, glass, -9f);

            Primitive("FarRoadForkL", PrimitiveType.Cube, root,
                new Vector3(-17f, -0.55f, 55f), new Vector3(8.5f, 0.16f, 42f), warm, false, new Vector3(0f, -28f, 0f));
            Primitive("FarRoadForkR", PrimitiveType.Cube, root,
                new Vector3(18f, -0.55f, 58f), new Vector3(8.5f, 0.16f, 46f), warm, false, new Vector3(0f, 31f, 0f));
        }

        private static int BuildReferenceEnemySilhouettes(
            Transform ward,
            Material ceramic,
            Material edge,
            Material threatAmber,
            Material threatWhite)
        {
            JourneyEnemyController[] enemies = ward.GetComponentsInChildren<JourneyEnemyController>(true);
            int rebuilt = 0;
            for (int i = 0; i < enemies.Length; i++)
            {
                JourneyEnemyController enemy = enemies[i];
                if (enemy == null) continue;
                Transform visuals = enemy.transform.Find("Visuals");
                if (visuals == null) continue;

                DestroyChild(visuals, EnemyRootName);
                DisableRenderers(visuals.Find("ArchetypeSilhouetteV3"));
                DisableRenderers(visuals.Find("ArchetypeSilhouetteV2"));

                GameObject visualRoot = new GameObject(EnemyRootName);
                visualRoot.transform.SetParent(visuals, false);
                float s = EstimateScale(visuals.Find("Core"));
                bool needle = enemy.name.IndexOf("AetherNeedle", StringComparison.OrdinalIgnoreCase) >= 0;

                switch (enemy.Archetype)
                {
                    case JourneyEnemyArchetype.NullSentry:
                        BuildChoirReliquarySentry(visualRoot.transform, s, ceramic, edge, threatAmber, threatWhite);
                        break;
                    case JourneyEnemyArchetype.ChromePenitent:
                        BuildChromePenitentLancer(visualRoot.transform, s, ceramic, edge, threatAmber, threatWhite);
                        break;
                    case JourneyEnemyArchetype.Shardcaster:
                        if (needle) BuildNeedleSeraph(visualRoot.transform, s, ceramic, edge, threatAmber, threatWhite);
                        else BuildShardCantor(visualRoot.transform, s, ceramic, edge, threatAmber, threatWhite);
                        break;
                    case JourneyEnemyArchetype.SignalWarden:
                        BuildCathedralWarden(visualRoot.transform, s, ceramic, edge, threatAmber, threatWhite);
                        break;
                    case JourneyEnemyArchetype.Hollow:
                        BuildRiftStalker(visualRoot.transform, s, ceramic, edge, threatAmber);
                        break;
                }
                rebuilt++;
            }
            return rebuilt;
        }

        private static void BuildChoirReliquarySentry(Transform parent, float s, Material body, Material edge, Material threat, Material white)
        {
            Part("ReliquaryKeel", PrimitiveType.Capsule, parent, new Vector3(0f, 0.84f, 0f) * s,
                new Vector3(0.34f, 0.95f, 0.34f) * s, Vector3.zero, body);
            Part("ReliquaryShoulderL", PrimitiveType.Cube, parent, new Vector3(-0.42f, 0.92f, -0.02f) * s,
                new Vector3(0.10f, 0.74f, 0.28f) * s, new Vector3(0f, -12f, 34f), edge);
            Part("ReliquaryShoulderR", PrimitiveType.Cube, parent, new Vector3(0.42f, 0.92f, -0.02f) * s,
                new Vector3(0.10f, 0.74f, 0.28f) * s, new Vector3(0f, 12f, -34f), edge);
            Part("ReliquaryCrown", PrimitiveType.Cube, parent, new Vector3(0f, 1.42f, 0f) * s,
                new Vector3(0.72f, 0.11f, 0.36f) * s, Vector3.zero, white);
            Part("ReliquaryLens", PrimitiveType.Sphere, parent, new Vector3(0f, 0.92f, 0.31f) * s,
                Vector3.one * 0.20f * s, Vector3.zero, threat);
            Part("ReliquaryLensSlit", PrimitiveType.Cube, parent, new Vector3(0f, 0.92f, 0.44f) * s,
                new Vector3(0.30f, 0.055f, 0.025f) * s, Vector3.zero, white);
            Ring("ReliquaryHalo", parent, new Vector3(0f, 1.05f, -0.12f) * s,
                0.70f * s, 36, 0.035f * s, edge, Quaternion.Euler(82f, 0f, 0f));
        }

        private static void BuildChromePenitentLancer(Transform parent, float s, Material body, Material edge, Material threat, Material white)
        {
            Part("PenitentTorso", PrimitiveType.Cube, parent, new Vector3(0f, 1.22f, 0f) * s,
                new Vector3(0.58f, 0.78f, 0.34f) * s, new Vector3(0f, 0f, 2f), body);
            Part("PenitentPelvis", PrimitiveType.Cube, parent, new Vector3(0f, 0.78f, 0f) * s,
                new Vector3(0.48f, 0.28f, 0.32f) * s, Vector3.zero, edge);
            Part("PenitentLegL", PrimitiveType.Cube, parent, new Vector3(-0.18f, 0.34f, 0f) * s,
                new Vector3(0.17f, 0.70f, 0.20f) * s, new Vector3(0f, 0f, -3f), body);
            Part("PenitentLegR", PrimitiveType.Cube, parent, new Vector3(0.18f, 0.34f, 0f) * s,
                new Vector3(0.17f, 0.70f, 0.20f) * s, new Vector3(0f, 0f, 3f), body);
            Part("PenitentShoulderL", PrimitiveType.Cube, parent, new Vector3(-0.43f, 1.48f, 0f) * s,
                new Vector3(0.28f, 0.16f, 0.42f) * s, new Vector3(0f, 0f, -11f), white);
            Part("PenitentShoulderR", PrimitiveType.Cube, parent, new Vector3(0.43f, 1.48f, 0f) * s,
                new Vector3(0.28f, 0.16f, 0.42f) * s, new Vector3(0f, 0f, 11f), white);
            Part("PenitentMask", PrimitiveType.Cube, parent, new Vector3(0f, 1.85f, 0.02f) * s,
                new Vector3(0.28f, 0.30f, 0.24f) * s, new Vector3(-5f, 0f, 0f), edge);
            Part("PenitentVisor", PrimitiveType.Cube, parent, new Vector3(0f, 1.88f, 0.155f) * s,
                new Vector3(0.24f, 0.045f, 0.025f) * s, Vector3.zero, threat);
            Part("PenitentLanceShaft", PrimitiveType.Cube, parent, new Vector3(0.58f, 1.02f, 0.08f) * s,
                new Vector3(0.075f, 2.35f, 0.075f) * s, new Vector3(0f, 0f, -13f), edge);
            Part("PenitentLanceBlade", PrimitiveType.Cube, parent, new Vector3(0.84f, 2.10f, 0.08f) * s,
                new Vector3(0.13f, 0.72f, 0.08f) * s, new Vector3(0f, 0f, -13f), threat);
        }

        private static void BuildShardCantor(Transform parent, float s, Material body, Material edge, Material threat, Material white)
        {
            Part("CantorCore", PrimitiveType.Sphere, parent, new Vector3(0f, 0.95f, 0f) * s,
                Vector3.one * 0.30f * s, Vector3.zero, threat);
            for (int i = 0; i < 3; i++)
            {
                float angle = i * 120f;
                float rad = angle * Mathf.Deg2Rad;
                Vector3 p = new Vector3(Mathf.Cos(rad) * 0.50f, 1.0f + (i == 0 ? 0.28f : -0.02f), Mathf.Sin(rad) * 0.26f) * s;
                Part($"CantorShard_{i}", PrimitiveType.Cube, parent, p,
                    new Vector3(0.13f, 0.92f, 0.18f) * s, new Vector3(0f, angle, 42f - i * 19f), i == 0 ? white : body);
            }
            Ring("CantorChoirRing", parent, new Vector3(0f, 1.02f, 0f) * s,
                0.72f * s, 34, 0.03f * s, edge, Quaternion.Euler(76f, 18f, 0f));
        }

        private static void BuildNeedleSeraph(Transform parent, float s, Material body, Material edge, Material threat, Material white)
        {
            Part("NeedleSpine", PrimitiveType.Cube, parent, new Vector3(0f, 1.05f, 0f) * s,
                new Vector3(0.14f, 1.72f, 0.14f) * s, new Vector3(0f, 0f, 45f), body);
            Part("NeedleWingL", PrimitiveType.Cube, parent, new Vector3(-0.31f, 1.20f, 0f) * s,
                new Vector3(0.07f, 0.92f, 0.18f) * s, new Vector3(0f, -10f, 30f), white);
            Part("NeedleWingR", PrimitiveType.Cube, parent, new Vector3(0.31f, 1.20f, 0f) * s,
                new Vector3(0.07f, 0.92f, 0.18f) * s, new Vector3(0f, 10f, -30f), white);
            Part("NeedleEye", PrimitiveType.Sphere, parent, new Vector3(0f, 1.12f, 0.16f) * s,
                Vector3.one * 0.18f * s, Vector3.zero, threat);
            Part("NeedleTail", PrimitiveType.Cube, parent, new Vector3(0f, 0.18f, 0f) * s,
                new Vector3(0.045f, 0.52f, 0.045f) * s, Vector3.zero, edge);
        }

        private static void BuildCathedralWarden(Transform parent, float s, Material body, Material edge, Material threat, Material white)
        {
            Part("WardenBody", PrimitiveType.Cube, parent, new Vector3(0f, 1.0f, 0f) * s,
                new Vector3(0.98f, 1.10f, 0.54f) * s, Vector3.zero, body);
            Part("WardenButtressL", PrimitiveType.Cube, parent, new Vector3(-0.61f, 1.02f, 0f) * s,
                new Vector3(0.22f, 1.42f, 0.38f) * s, new Vector3(0f, 0f, -7f), edge);
            Part("WardenButtressR", PrimitiveType.Cube, parent, new Vector3(0.61f, 1.02f, 0f) * s,
                new Vector3(0.22f, 1.42f, 0.38f) * s, new Vector3(0f, 0f, 7f), edge);
            Part("WardenCrown", PrimitiveType.Cube, parent, new Vector3(0f, 1.72f, 0f) * s,
                new Vector3(0.86f, 0.20f, 0.48f) * s, Vector3.zero, white);
            Part("WardenCrownL", PrimitiveType.Cube, parent, new Vector3(-0.31f, 2.02f, 0f) * s,
                new Vector3(0.10f, 0.58f, 0.15f) * s, new Vector3(0f, 0f, -16f), white);
            Part("WardenCrownR", PrimitiveType.Cube, parent, new Vector3(0.31f, 2.02f, 0f) * s,
                new Vector3(0.10f, 0.58f, 0.15f) * s, new Vector3(0f, 0f, 16f), white);
            Part("WardenCore", PrimitiveType.Cube, parent, new Vector3(0f, 1.02f, 0.31f) * s,
                new Vector3(0.42f, 0.42f, 0.055f) * s, new Vector3(0f, 0f, 45f), threat);
            Part("WardenWeaponPylon", PrimitiveType.Cube, parent, new Vector3(0.82f, 0.96f, 0.04f) * s,
                new Vector3(0.13f, 1.92f, 0.18f) * s, new Vector3(0f, 0f, -8f), edge);
        }

        private static void BuildRiftStalker(Transform parent, float s, Material body, Material edge, Material threat)
        {
            // Hollows are absent from the opening. Deeper examples now read as intentional
            // blade-stalkers rather than indistinct objects crawling under projectile lanes.
            Part("StalkerChest", PrimitiveType.Cube, parent, new Vector3(0f, 0.75f, 0f) * s,
                new Vector3(0.50f, 0.64f, 0.44f) * s, new Vector3(8f, 0f, 0f), body);
            Part("StalkerForelegL", PrimitiveType.Cube, parent, new Vector3(-0.28f, 0.34f, 0.20f) * s,
                new Vector3(0.12f, 0.60f, 0.16f) * s, new Vector3(0f, 0f, -18f), edge);
            Part("StalkerForelegR", PrimitiveType.Cube, parent, new Vector3(0.28f, 0.34f, 0.20f) * s,
                new Vector3(0.12f, 0.60f, 0.16f) * s, new Vector3(0f, 0f, 18f), edge);
            Part("StalkerBladeL", PrimitiveType.Cube, parent, new Vector3(-0.43f, 0.70f, 0.10f) * s,
                new Vector3(0.08f, 0.70f, 0.14f) * s, new Vector3(0f, -20f, 38f), body);
            Part("StalkerBladeR", PrimitiveType.Cube, parent, new Vector3(0.43f, 0.70f, 0.10f) * s,
                new Vector3(0.08f, 0.70f, 0.14f) * s, new Vector3(0f, 20f, -38f), body);
            Part("StalkerEye", PrimitiveType.Cube, parent, new Vector3(0f, 0.84f, 0.25f) * s,
                new Vector3(0.25f, 0.05f, 0.025f) * s, Vector3.zero, threat);
        }

        private static void ValidateProtectedClearance(Transform sanctum)
        {
            ValidateZoneClearance(FindDeepChild(sanctum, "Sanctum_Initiation_Hall_V08"), HallClearHalfWidth, -62.0f, -39.0f, "initiation hall");
            ValidateZoneClearance(FindDeepChild(sanctum, "Sanctum_Threshold_Terrace_V08"), TerraceClearHalfWidth, -37.5f, -24.5f, "threshold terrace");
            ValidateZoneClearance(FindDeepChild(sanctum, "Sanctum_First_Sentinel_Court_V08"), CourtClearHalfWidth, -29.7f, -18.0f, "first sentinel court");
        }

        private static void ValidateZoneClearance(Transform zone, float halfWidth, float minZ, float maxZ, string label)
        {
            if (zone == null) throw new InvalidOperationException("Reference fidelity clearance audit missing zone: " + label);
            Collider[] colliders = zone.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider collider = colliders[i];
                if (collider == null || collider.isTrigger || !collider.enabled) continue;
                Bounds b = collider.bounds;
                Vector3 center = zone.parent != null ? zone.parent.InverseTransformPoint(b.center) : b.center;
                if (b.max.y < 0.55f) continue;
                if (center.z + b.extents.z < minZ || center.z - b.extents.z > maxZ) continue;
                if (Mathf.Abs(center.x) - b.extents.x >= halfWidth) continue;
                throw new InvalidOperationException(
                    $"Sanctum {label} violates protected movement clearance: '{collider.name}' enters +/-{halfWidth:0.##}m central lane.");
            }
        }

        private static void ValidateOpeningEnemySpacing(Transform ward)
        {
            JourneyEnemyController[] enemies = ward.GetComponentsInChildren<JourneyEnemyController>(true);
            List<JourneyEnemyController> sentries = new List<JourneyEnemyController>();
            for (int i = 0; i < enemies.Length; i++)
            {
                JourneyEnemyController enemy = enemies[i];
                if (enemy == null || enemy.Archetype != JourneyEnemyArchetype.NullSentry) continue;
                if (enemy.name.IndexOf("Causeway", StringComparison.OrdinalIgnoreCase) < 0) continue;
                sentries.Add(enemy);
            }
            if (sentries.Count < 2) return;
            Vector3 a = sentries[0].transform.position;
            Vector3 b = sentries[1].transform.position;
            a.y = 0f;
            b.y = 0f;
            float spacing = Vector3.Distance(a, b);
            if (spacing < MinimumOpeningSentrySpacing)
                throw new InvalidOperationException(
                    $"Opening Sentries are only {spacing:0.0}m apart; reference fidelity requires >= {MinimumOpeningSentrySpacing:0.0}m.");
        }

        private static void BuildWindowLancet(Transform parent, string name, float x, float y, float z, int side, Material glass, Material gold, Material edge)
        {
            Primitive(name + "_Glass", PrimitiveType.Cube, parent, new Vector3(x, y, z), new Vector3(0.04f, 4.3f, 2.2f), glass, false);
            Primitive(name + "_Mullion", PrimitiveType.Cube, parent, new Vector3(x - side * 0.03f, y, z), new Vector3(0.035f, 4.4f, 0.08f), gold, false);
            Primitive(name + "_Sill", PrimitiveType.Cube, parent, new Vector3(x - side * 0.04f, y - 2.18f, z), new Vector3(0.06f, 0.12f, 2.35f), edge, false);
        }

        private static void BuildSignalCypress(Transform parent, string name, Vector3 p, Material garden, Material gold)
        {
            Primitive(name + "_Trunk", PrimitiveType.Cylinder, parent, p + Vector3.up * 1.6f,
                new Vector3(0.16f, 1.6f, 0.16f), gold, false);
            Primitive(name + "_CrownLow", PrimitiveType.Sphere, parent, p + Vector3.up * 2.5f,
                new Vector3(0.70f, 1.65f, 0.70f), garden, false);
            Primitive(name + "_CrownHigh", PrimitiveType.Sphere, parent, p + Vector3.up * 4.0f,
                new Vector3(0.50f, 1.45f, 0.50f), garden, false);
        }

        private static void BuildSkyPhaseRing(Transform parent, string name, Vector3 p, float radius, Material rim, Material glass, float yaw)
        {
            Ring(name + "_Outer", parent, p, radius, 72, 0.11f, rim, Quaternion.Euler(90f, yaw, 0f));
            Ring(name + "_Inner", parent, p + new Vector3(0f, 0f, 0.18f), radius * 0.78f, 64, 0.055f, glass,
                Quaternion.Euler(90f, yaw + 11f, 0f));
        }

        private static void RouteBranch(Transform parent, Vector3 a, Vector3 b, Material material, string name)
        {
            Vector3 center = (a + b) * 0.5f;
            float length = Vector3.Distance(a, b);
            Primitive(name, PrimitiveType.Cube, parent, center, new Vector3(length, 0.025f, 0.08f), material, false);
        }

        private static void MoveDeepChild(Transform root, string name, Vector3 localPosition)
        {
            Transform child = FindDeepChild(root, name);
            if (child != null) child.localPosition = localPosition;
        }

        private static Transform FindDeepChild(Transform root, string name)
        {
            if (root == null) return null;
            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
                if (all[i] != null && string.Equals(all[i].name, name, StringComparison.Ordinal)) return all[i];
            return null;
        }

        private static float EstimateScale(Transform core)
        {
            if (core == null) return 1f;
            return Mathf.Clamp(core.localScale.x / 0.30f, 0.55f, 1.8f);
        }

        private static void DisableRenderers(Transform root)
        {
            if (root == null) return;
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
                if (renderers[i] != null) renderers[i].enabled = false;
        }

        private static void DestroyChild(Transform parent, string name)
        {
            if (parent == null) return;
            Transform child = parent.Find(name);
            if (child != null) UnityEngine.Object.DestroyImmediate(child.gameObject);
        }

        private static void Part(string name, PrimitiveType type, Transform parent, Vector3 localPosition, Vector3 localScale, Vector3 localEuler, Material material)
        {
            Primitive(name, type, parent, localPosition, localScale, material, false, localEuler);
        }

        private static void BuildPointedRib(
            Transform parent,
            string name,
            Vector3 springCenter,
            float span,
            float rise,
            int halfSegments,
            Material material,
            float width)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = springCenter;
            LineRenderer line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.useWorldSpace = false;
            line.alignment = LineAlignment.TransformZ;
            int half = Mathf.Max(8, halfSegments);
            line.positionCount = half * 2 + 1;
            line.startWidth = width;
            line.endWidth = width;
            line.numCornerVertices = 2;
            line.numCapVertices = 2;
            line.shadowCastingMode = ShadowCastingMode.On;
            line.receiveShadows = true;

            Vector3 left = new Vector3(-span * 0.5f, 0f, 0f);
            Vector3 apex = new Vector3(0f, rise, 0f);
            Vector3 right = new Vector3(span * 0.5f, 0f, 0f);
            Vector3 leftControl = new Vector3(-span * 0.34f, rise * 0.78f, 0f);
            Vector3 rightControl = new Vector3(span * 0.34f, rise * 0.78f, 0f);
            for (int i = 0; i <= half; i++)
            {
                float t = i / (float)half;
                line.SetPosition(i, Quadratic(left, leftControl, apex, t));
            }
            for (int i = 1; i <= half; i++)
            {
                float t = i / (float)half;
                line.SetPosition(half + i, Quadratic(apex, rightControl, right, t));
            }
        }

        private static Vector3 Quadratic(Vector3 a, Vector3 b, Vector3 c, float t)
        {
            float u = 1f - t;
            return u * u * a + 2f * u * t * b + t * t * c;
        }

        private static void Ring(
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
            line.sharedMaterial = material;
            line.useWorldSpace = false;
            line.alignment = LineAlignment.TransformZ;
            line.loop = true;
            line.positionCount = Mathf.Max(16, segments);
            line.startWidth = width;
            line.endWidth = width;
            line.numCornerVertices = 2;
            line.shadowCastingMode = ShadowCastingMode.Off;
            for (int i = 0; i < line.positionCount; i++)
            {
                float a = i / (float)line.positionCount * Mathf.PI * 2f;
                line.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius));
            }
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
            if (localEuler.HasValue) go.transform.localRotation = Quaternion.Euler(localEuler.Value);
            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
                GameObjectUtility.SetStaticEditorFlags(go, VisualStatic);
            }
            if (!collider)
            {
                Collider shape = go.GetComponent<Collider>();
                if (shape != null) UnityEngine.Object.DestroyImmediate(shape);
            }
            return go;
        }

        private static Material Require(string name)
        {
            Material material = CinematicMaterialAuthoring.Load(name);
            if (material == null) material = SanctumMaterialAuthoringV08.Load(name);
            if (material == null) material = SanctumReferenceMaterialAuthoringV08.Load(name);
            if (material == null) throw new InvalidOperationException("Required V0.8 reference material missing: " + name);
            return material;
        }
    }
}
#endif