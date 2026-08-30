#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Mindforge.Calibration;
using Mindforge.Combat;
using Mindforge.Journey;
using Mindforge.Neural;
using Mindforge.Telemetry;
using Mindforge.World;

namespace Mindforge.Editor
{
    /// <summary>
    /// V0.8 rebuilds the first minutes around human-scale traversal and cathedral-scale awe.
    /// It deliberately removes dense legacy opening geometry and low Rift Hollow pressure,
    /// then authors one broad sanctuary, a calibration threshold and an exterior reveal court.
    /// Existing combat, quest, persistence, E routing and neural authorities remain intact.
    /// </summary>
    public static class SanctumOnboardingV08Builder
    {
        public const string RootName = "Mindforge_Sanctum_Onboarding_V08";
        public const string Revision = "SANCTUM_ONBOARDING_V08";

        private static readonly StaticEditorFlags VisualStatic =
            StaticEditorFlags.BatchingStatic |
            StaticEditorFlags.OccludeeStatic |
            StaticEditorFlags.ReflectionProbeStatic;

        [MenuItem("Mindforge/Showcase/Apply Sanctum Onboarding V0.8", priority = 36)]
        public static void ApplyOpenScene()
        {
            GameObject ward = EditorSceneLookup.FindIncludingInactive(NullWardSceneBuilder.RootName);
            GameObject arena = EditorSceneLookup.FindIncludingInactive("Fractured_Signal_Arena");
            GameObject guardian = EditorSceneLookup.FindIncludingInactive("Guardian");
            GameObject foundation = FindFoundation();
            if (ward == null || arena == null || guardian == null || foundation == null)
                throw new InvalidOperationException("Sanctum V0.8 requires Null Ward, arena, Guardian and Game Foundation.");

            NullWardEncounterDirector encounterDirector = ward.GetComponent<NullWardEncounterDirector>();
            if (encounterDirector == null)
                throw new InvalidOperationException("Sanctum V0.8 requires NullWardEncounterDirector.");

            GameObject previous = EditorSceneLookup.FindIncludingInactive(RootName);
            if (previous != null) UnityEngine.Object.DestroyImmediate(previous);

            SanctumMaterialAuthoringV08.EnsureAuthored();
            Material ivory = Require(SanctumMaterialAuthoringV08.Ivory);
            Material pearl = Require(SanctumMaterialAuthoringV08.Pearl);
            Material gold = Require(SanctumMaterialAuthoringV08.Gold);
            Material glass = Require(SanctumMaterialAuthoringV08.BlueGlass);
            Material water = Require(SanctumMaterialAuthoringV08.Water);
            Material garden = Require(SanctumMaterialAuthoringV08.Garden);
            Material cyan = Require("AetherCyan");
            Material green = Require("WispVerdant");

            // Remove the cramped primitive corridor but not gameplay objects. V0.8 owns the
            // replacement floor/walls/collision shell for the opening segment.
            int disabledLegacy = DisableLegacyOpeningGeometry(ward.transform);
            DisableDarkOpeningHeroProps();

            // Remove the two causeway Rift Hollows from the actual encounter definition.
            int removedCrawlers = RemoveOpeningFloorRushers(encounterDirector);
            RecomposeOpeningEncounters(encounterDirector, ward.transform);

            GameObject root = new GameObject(RootName);
            root.transform.SetParent(arena.transform, false);

            OpeningExperienceDirectorV08 opening = root.AddComponent<OpeningExperienceDirectorV08>();
            WorldStateLedger ledger = foundation.GetComponent<WorldStateLedger>();
            WorldSignalBus signals = foundation.GetComponent<WorldSignalBus>();
            UdpGameMarkerSender markers = UnityEngine.Object.FindObjectOfType<UdpGameMarkerSender>(true);
            AwakeningCalibrationDirector neuralCalibration = UnityEngine.Object.FindObjectOfType<AwakeningCalibrationDirector>(true);
            UdpNeuralReceiver neuralReceiver = UnityEngine.Object.FindObjectOfType<UdpNeuralReceiver>(true);

            JourneyGate threshold = BuildSanctumInterior(root.transform, ivory, pearl, gold, glass, cyan, green, garden);
            BuildThresholdTerrace(root.transform, ivory, pearl, gold, glass, water, garden, cyan);
            BuildPracticeCourt(root.transform, ivory, pearl, gold, glass, water, garden, cyan, green);
            BuildWorldReveal(root.transform, ivory, pearl, gold, glass, water, garden, cyan);
            BuildLighting(root.transform);

            SanctumCalibrationSequenceV08 sequence = root.AddComponent<SanctumCalibrationSequenceV08>();
            sequence.ConfigureRuntime(opening, neuralCalibration, threshold, 2, ledger, signals, markers);
            BuildCalibrationStations(root.transform, sequence, ivory, gold, glass, cyan);

            ParticipantCalibrationProfileV08 calibrationProfile = foundation.GetComponent<ParticipantCalibrationProfileV08>();
            if (calibrationProfile == null) calibrationProfile = foundation.AddComponent<ParticipantCalibrationProfileV08>();
            calibrationProfile.ConfigureRuntime(neuralReceiver, ledger);

            BuildPhaseTrigger(root.transform, "Phase_Practice", new Vector3(0f, 1.5f, -36.6f), new Vector3(12f, 4f, 1.2f), opening,
                OpeningExperiencePhaseV08.Practice, "sanctum_threshold_crossed");
            BuildPhaseTrigger(root.transform, "Phase_WorldReveal", new Vector3(0f, 1.5f, -33.0f), new Vector3(16f, 4f, 1.2f), opening,
                OpeningExperiencePhaseV08.WorldReveal, "threshold_overlook_reached");
            BuildPhaseTrigger(root.transform, "Phase_FirstEncounter", new Vector3(0f, 1.5f, -29.8f), new Vector3(20f, 4f, 1.2f), opening,
                OpeningExperiencePhaseV08.FirstEncounter, "first_sentinel_court_entered");
            BuildPhaseTrigger(root.transform, "Phase_Released", new Vector3(0f, 1.5f, -17.4f), new Vector3(18f, 4f, 1.2f), opening,
                OpeningExperiencePhaseV08.Released, "sanctum_onboarding_complete");

            ConfigureRenderEnvironment();

            EditorUtility.SetDirty(root);
            EditorUtility.SetDirty(encounterDirector);
            EditorUtility.SetDirty(foundation);
            EditorUtility.SetDirty(calibrationProfile);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                $"[Mindforge:V08] Sanctum opening rebuilt. Disabled {disabledLegacy} cramped legacy render/collider pieces; " +
                $"removed {removedCrawlers} low Rift Hollow rushers; authored ~30m-wide sanctuary + threshold terrace + readable encounter courts. " +
                "Two resonance-station inspections open the controller-preview threshold without inventing neural success; real Python-accepted calibration opens it directly. " +
                "Enemy projectiles now scale from 60% in onboarding to 82% after release through the existing projectile authority.");
        }

        private static JourneyGate BuildSanctumInterior(
            Transform parent,
            Material ivory,
            Material pearl,
            Material gold,
            Material glass,
            Material cyan,
            Material green,
            Material garden)
        {
            Transform root = Zone(parent, "Sanctum_Initiation_Hall_V08");

            // Human-scale traversal envelope: 30m clear width, 24m depth, 12m door opening.
            Primitive("SanctumFloor", PrimitiveType.Cube, root, new Vector3(0f, -0.48f, -50.5f), new Vector3(30f, 0.58f, 25f), ivory, true);
            Primitive("SanctumBackWall", PrimitiveType.Cube, root, new Vector3(0f, 5.5f, -63.3f), new Vector3(30f, 12f, 0.55f), pearl, true);
            Primitive("SanctumLeftBoundary", PrimitiveType.Cube, root, new Vector3(-15.2f, 4.2f, -50.5f), new Vector3(0.55f, 9f, 25f), pearl, true);
            Primitive("SanctumRightBoundary", PrimitiveType.Cube, root, new Vector3(15.2f, 4.2f, -50.5f), new Vector3(0.55f, 9f, 25f), pearl, true);

            // Clear 10m central procession lane. Structural rhythm lives outside +/-8m.
            float[] bays = { -60f, -54f, -48f, -42f };
            for (int i = 0; i < bays.Length; i++)
                BuildCathedralBay(root, bays[i], i, ivory, pearl, gold, glass, cyan);

            // Large rear apsidal signal motif, intentionally presentation-only.
            CreateRing("ApsisHaloOuter", root, new Vector3(0f, 7.3f, -62.92f), 4.2f, 64, 0.085f, gold, Quaternion.Euler(90f, 0f, 0f));
            CreateRing("ApsisHaloInner", root, new Vector3(0f, 7.3f, -62.86f), 3.4f, 56, 0.055f, cyan, Quaternion.Euler(90f, 0f, 0f));

            // Planters stay beyond the combat/traversal lane.
            BuildGardenPair(root, new Vector3(11.2f, 0f, -57f), ivory, gold, garden, 0);
            BuildGardenPair(root, new Vector3(11.2f, 0f, -46f), ivory, gold, garden, 1);

            // Threshold: 12m usable door, 13m vertical read. The moving seal itself is simple
            // and legible; ornate arches around it never own collision.
            GameObject gateRoot = new GameObject("Sanctum_Threshold_Gate_V08");
            gateRoot.transform.SetParent(root, false);
            gateRoot.transform.localPosition = new Vector3(0f, 0f, -38.15f);

            for (int side = -1; side <= 1; side += 2)
            {
                Primitive("ThresholdPier_" + side, PrimitiveType.Cube, gateRoot.transform,
                    new Vector3(side * 6.65f, 5.8f, 0f), new Vector3(1.15f, 11.6f, 1.35f), pearl, true,
                    new Vector3(0f, 0f, side * -6f));
                Primitive("ThresholdGold_" + side, PrimitiveType.Cube, gateRoot.transform,
                    new Vector3(side * 6.02f, 5.9f, -0.05f), new Vector3(0.12f, 9.7f, 0.30f), gold, false);
            }
            CreateRing("ThresholdHalo", gateRoot.transform, new Vector3(0f, 8.2f, 0.25f), 5.9f, 72, 0.09f, gold, Quaternion.Euler(90f, 0f, 0f));
            CreateRing("ThresholdSignalHalo", gateRoot.transform, new Vector3(0f, 8.15f, 0.18f), 5.35f, 64, 0.045f, cyan, Quaternion.Euler(90f, 0f, 0f));

            GameObject seal = Primitive("ThresholdSeal", PrimitiveType.Cube, gateRoot.transform,
                new Vector3(0f, 3.7f, 0f), new Vector3(11.6f, 7.4f, 0.42f), glass, true);
            Collider blocker = seal.GetComponent<Collider>();
            JourneyGate gate = gateRoot.AddComponent<JourneyGate>();
            gate.ConfigureRuntime(seal.transform, blocker != null ? new[] { blocker } : Array.Empty<Collider>());
            gate.SetOpen(false, true);
            return gate;
        }

        private static void BuildCathedralBay(
            Transform parent,
            float z,
            int index,
            Material ivory,
            Material pearl,
            Material gold,
            Material glass,
            Material cyan)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * 11.1f;
                Primitive($"Bay_{index:00}_Pier_{side}", PrimitiveType.Cube, parent,
                    new Vector3(x, 5.4f, z), new Vector3(1.05f, 10.8f, 1.15f), index % 2 == 0 ? ivory : pearl, true,
                    new Vector3(side * -5f, 0f, side * -3f));
                Primitive($"Bay_{index:00}_Gold_{side}", PrimitiveType.Cube, parent,
                    new Vector3(x - side * 0.58f, 5.6f, z - 0.12f), new Vector3(0.11f, 8.5f, 0.25f), gold, false);
                Primitive($"Bay_{index:00}_Glass_{side}", PrimitiveType.Cube, parent,
                    new Vector3(x - side * 0.66f, 5.5f, z + 0.32f), new Vector3(0.07f, 5.8f, 1.55f), glass, false);
            }

            // Tall inverted-U silhouette. The line ring is visual only and leaves the entire
            // center lane empty from floor through jumping height.
            CreateRing($"Bay_{index:00}_Arch", parent, new Vector3(0f, 7.2f, z), 7.3f, 72, 0.095f,
                index % 2 == 0 ? gold : cyan, Quaternion.Euler(90f, 0f, 0f));
        }

        private static void BuildCalibrationStations(
            Transform parent,
            SanctumCalibrationSequenceV08 sequence,
            Material ivory,
            Material gold,
            Material glass,
            Material cyan)
        {
            Transform root = Zone(parent, "Sanctum_Resonance_Gallery_V08");
            float[] x = { -7.2f, 0f, 7.2f };
            float[] hz = { 8f, 10f, 12f };
            for (int i = 0; i < x.Length; i++)
            {
                GameObject station = new GameObject($"Resonance_Station_{i + 1:00}_{hz[i]:0}Hz");
                station.transform.SetParent(root, false);
                station.transform.localPosition = new Vector3(x[i], 0f, -54.2f);

                Primitive("Plinth", PrimitiveType.Cylinder, station.transform, new Vector3(0f, 0.22f, 0f), new Vector3(1.45f, 0.22f, 1.45f), ivory, true);
                Primitive("GoldStem", PrimitiveType.Cylinder, station.transform, new Vector3(0f, 1.15f, 0f), new Vector3(0.16f, 1.0f, 0.16f), gold, false);
                GameObject orb = Primitive("ResonanceOrb", PrimitiveType.Sphere, station.transform, new Vector3(0f, 2.15f, 0f), Vector3.one * 0.78f, cyan, false);
                CreateRing("OrbitalA", station.transform, new Vector3(0f, 2.15f, 0f), 1.15f, 40, 0.045f, gold, Quaternion.Euler(72f, 0f, 0f));
                CreateRing("OrbitalB", station.transform, new Vector3(0f, 2.15f, 0f), 1.38f, 44, 0.035f, glass, Quaternion.Euler(18f, 52f, 0f));

                SanctumCalibrationOrbV08 interaction = station.AddComponent<SanctumCalibrationOrbV08>();
                interaction.ConfigureRuntime($"sanctum.resonance.{i + 1:00}", hz[i], sequence, orb.GetComponent<Renderer>());
            }
        }

        private static void BuildThresholdTerrace(
            Transform parent,
            Material ivory,
            Material pearl,
            Material gold,
            Material glass,
            Material water,
            Material garden,
            Material cyan)
        {
            Transform root = Zone(parent, "Sanctum_Threshold_Terrace_V08");
            Primitive("TerraceFloor", PrimitiveType.Cube, root, new Vector3(0f, -0.46f, -31.0f), new Vector3(30f, 0.54f, 14f), ivory, true);
            Primitive("ProcessionalLane", PrimitiveType.Cube, root, new Vector3(0f, -0.14f, -31.0f), new Vector3(10.5f, 0.08f, 13.5f), pearl, false);
            Primitive("GoldLaneL", PrimitiveType.Cube, root, new Vector3(-5.5f, -0.08f, -31.0f), new Vector3(0.10f, 0.05f, 13.5f), gold, false);
            Primitive("GoldLaneR", PrimitiveType.Cube, root, new Vector3(5.5f, -0.08f, -31.0f), new Vector3(0.10f, 0.05f, 13.5f), gold, false);

            // Narrow water/garden margins preserve a broad 11m movement/combat lane.
            Primitive("WaterL", PrimitiveType.Cube, root, new Vector3(-10.5f, -0.16f, -31.0f), new Vector3(4.0f, 0.08f, 12.0f), water, false);
            Primitive("WaterR", PrimitiveType.Cube, root, new Vector3(10.5f, -0.16f, -31.0f), new Vector3(4.0f, 0.08f, 12.0f), water, false);
            BuildGardenPair(root, new Vector3(12.8f, 0f, -34f), ivory, gold, garden, 3);
            BuildGardenPair(root, new Vector3(12.8f, 0f, -27f), ivory, gold, garden, 4);

            for (int side = -1; side <= 1; side += 2)
            {
                Primitive("TerraceRail_" + side, PrimitiveType.Cube, root,
                    new Vector3(side * 14.6f, 0.55f, -31f), new Vector3(0.35f, 1.1f, 14f), pearl, true);
                CreateRing("TerraceHalo_" + side, root, new Vector3(side * 12.8f, 4.4f, -25.5f), 2.2f, 48, 0.05f, cyan,
                    Quaternion.Euler(90f, 0f, 0f));
            }
        }

        private static void BuildPracticeCourt(
            Transform parent,
            Material ivory,
            Material pearl,
            Material gold,
            Material glass,
            Material water,
            Material garden,
            Material cyan,
            Material green)
        {
            Transform root = Zone(parent, "Sanctum_First_Sentinel_Court_V08");
            // 30 x 12.5m open court around the first two suspended Sentries.
            Primitive("CourtFloor", PrimitiveType.Cube, root, new Vector3(0f, -0.47f, -23.8f), new Vector3(30f, 0.55f, 12.5f), pearl, true);
            CreateRing("CourtFloorSigil", root, new Vector3(0f, -0.14f, -25.5f), 4.2f, 60, 0.05f, gold, Quaternion.Euler(0f, 0f, 0f));

            for (int side = -1; side <= 1; side += 2)
            {
                Primitive("CourtPierA_" + side, PrimitiveType.Cube, root, new Vector3(side * 12f, 4.5f, -27f), new Vector3(0.9f, 9f, 0.9f), ivory, true);
                Primitive("CourtPierB_" + side, PrimitiveType.Cube, root, new Vector3(side * 12f, 4.5f, -20.4f), new Vector3(0.9f, 9f, 0.9f), ivory, true);
                Primitive("CourtSignal_" + side, PrimitiveType.Cube, root, new Vector3(side * 11.45f, 4.4f, -23.8f), new Vector3(0.08f, 4.8f, 0.28f), side < 0 ? cyan : green, false);
            }
        }

        private static void BuildWorldReveal(
            Transform parent,
            Material ivory,
            Material pearl,
            Material gold,
            Material glass,
            Material water,
            Material garden,
            Material cyan)
        {
            Transform root = Zone(parent, "Sanctum_World_Reveal_V08");

            // Distant presentation-only city grammar. These structures sit outside the first
            // traversal volume and create the promise of a larger world through the threshold.
            Vector3[] towers =
            {
                new Vector3(0f, 0f, 48f),
                new Vector3(-22f, 0f, 34f),
                new Vector3(24f, 0f, 38f),
                new Vector3(-38f, 0f, 56f),
                new Vector3(42f, 0f, 62f),
            };
            float[] heights = { 34f, 22f, 26f, 18f, 20f };
            for (int i = 0; i < towers.Length; i++)
                BuildDistantCathedralTower(root, "VistaTower_" + i, towers[i], heights[i], i == 0 ? ivory : pearl, gold, glass, cyan);

            Primitive("VistaCanal", PrimitiveType.Cube, root, new Vector3(0f, -1.8f, 24f), new Vector3(13f, 0.20f, 54f), water, false);
            Primitive("VistaBridge", PrimitiveType.Cube, root, new Vector3(0f, 1.2f, 13f), new Vector3(16f, 0.45f, 4.5f), ivory, false,
                new Vector3(0f, 0f, 0f));

            // Layered greenery keeps the world from reading as an empty white machine.
            for (int i = 0; i < 8; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                float z = 8f + (i / 2) * 10f;
                Primitive("VistaGarden_" + i, PrimitiveType.Sphere, root,
                    new Vector3(side * (12f + (i % 3) * 4f), 1.7f, z), new Vector3(2.2f, 3.2f, 2.2f), garden, false);
            }
        }

        private static void BuildDistantCathedralTower(
            Transform parent,
            string name,
            Vector3 p,
            float height,
            Material stone,
            Material gold,
            Material glass,
            Material cyan)
        {
            Transform root = Zone(parent, name);
            Primitive("Core", PrimitiveType.Cube, root, p + Vector3.up * height * 0.42f,
                new Vector3(5.2f, height * 0.84f, 5.2f), stone, false);
            for (int side = -1; side <= 1; side += 2)
            {
                Primitive("Spire_" + side, PrimitiveType.Cube, root,
                    p + new Vector3(side * 3.6f, height * 0.55f, 0f), new Vector3(1.0f, height * 1.10f, 1.0f), stone, false,
                    new Vector3(0f, 0f, side * -8f));
                Primitive("Gold_" + side, PrimitiveType.Cube, root,
                    p + new Vector3(side * 2.65f, height * 0.52f, -0.1f), new Vector3(0.12f, height * 0.65f, 0.35f), gold, false);
            }
            Primitive("Needle", PrimitiveType.Cylinder, root, p + Vector3.up * height,
                new Vector3(0.25f, height * 0.35f, 0.25f), gold, false);
            CreateRing("CrownHalo", root, p + Vector3.up * height * 0.64f, 3.6f, 52, 0.06f, cyan, Quaternion.Euler(90f, 0f, 0f));
            Primitive("Window", PrimitiveType.Cube, root, p + new Vector3(0f, height * 0.48f, -2.65f), new Vector3(2.0f, height * 0.18f, 0.08f), glass, false);
        }

        private static void BuildGardenPair(Transform parent, Vector3 p, Material ivory, Material gold, Material garden, int seed)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 basePos = new Vector3(side * Mathf.Abs(p.x), p.y, p.z + side * 0.6f);
                Primitive($"Planter_{seed}_{side}", PrimitiveType.Cylinder, parent, basePos + Vector3.up * 0.28f,
                    new Vector3(1.35f, 0.28f, 1.35f), ivory, true);
                Primitive($"PlanterGold_{seed}_{side}", PrimitiveType.Cylinder, parent, basePos + Vector3.up * 0.56f,
                    new Vector3(1.10f, 0.08f, 1.10f), gold, false);
                Primitive($"TreeTrunk_{seed}_{side}", PrimitiveType.Cylinder, parent, basePos + Vector3.up * 1.45f,
                    new Vector3(0.18f, 1.15f, 0.18f), gold, false);
                Primitive($"TreeCrown_{seed}_{side}", PrimitiveType.Sphere, parent, basePos + Vector3.up * 3.15f,
                    new Vector3(1.15f, 2.05f, 1.15f), garden, false);
            }
        }

        private static JourneyEnemyController[] FilterEnemies(JourneyEnemyController[] enemies, Func<JourneyEnemyController, bool> keep)
        {
            List<JourneyEnemyController> result = new List<JourneyEnemyController>();
            if (enemies != null)
            {
                for (int i = 0; i < enemies.Length; i++)
                {
                    JourneyEnemyController enemy = enemies[i];
                    if (enemy != null && keep(enemy)) result.Add(enemy);
                }
            }
            return result.ToArray();
        }

        private static int RemoveOpeningFloorRushers(NullWardEncounterDirector director)
        {
            int removed = 0;
            NullWardEncounterZone[] zones = director.Zones ?? Array.Empty<NullWardEncounterZone>();
            for (int i = 0; i < zones.Length; i++)
            {
                NullWardEncounterZone zone = zones[i];
                if (zone == null || !string.Equals(zone.id, "synapse_causeway", StringComparison.Ordinal)) continue;
                JourneyEnemyController[] original = zone.enemies ?? Array.Empty<JourneyEnemyController>();
                for (int e = 0; e < original.Length; e++)
                {
                    JourneyEnemyController enemy = original[e];
                    if (enemy == null || enemy.Archetype != JourneyEnemyArchetype.Hollow) continue;
                    removed++;
                    UnityEngine.Object.DestroyImmediate(enemy.gameObject);
                }
                zone.enemies = FilterEnemies(original, enemy => enemy.Archetype != JourneyEnemyArchetype.Hollow);
                zone.lesson = "Suspended Sentries telegraph slow tracking bolts across a wide court · read the lock, move once, then close distance";
            }
            return removed;
        }

        private static void RecomposeOpeningEncounters(NullWardEncounterDirector director, Transform ward)
        {
            NullWardEncounterZone[] zones = director.Zones ?? Array.Empty<NullWardEncounterZone>();
            for (int i = 0; i < zones.Length; i++)
            {
                NullWardEncounterZone zone = zones[i];
                if (zone == null) continue;
                if (string.Equals(zone.id, "synapse_causeway", StringComparison.Ordinal))
                {
                    if (zone.activationPoint != null) zone.activationPoint.localPosition = new Vector3(0f, 0f, -30.8f);
                    zone.activationRadius = 4.8f;
                    MoveEnemy(zone.enemies, "Causeway_NullSentry_A", new Vector3(-7.1f, -0.30f, -27.4f));
                    MoveEnemy(zone.enemies, "Causeway_NullSentry_B", new Vector3(7.0f, -0.30f, -25.6f));
                }
                else if (string.Equals(zone.id, "null_market", StringComparison.Ordinal))
                {
                    if (zone.activationPoint != null) zone.activationPoint.localPosition = new Vector3(0f, 0f, -23.2f);
                    zone.activationRadius = 5.4f;
                    MoveEnemy(zone.enemies, "Market_ChromePenitent", new Vector3(-4.8f, -0.30f, -20.7f));
                    MoveEnemy(zone.enemies, "Market_Shardsinger", new Vector3(5.8f, 1.35f, -20.2f));
                    if (zone.echoes != null)
                        for (int e = 0; e < zone.echoes.Length; e++)
                            if (zone.echoes[e] != null && zone.echoes[e].transform.parent != null)
                                zone.echoes[e].transform.parent.localPosition = zone.echoes[e].transform.parent.localPosition;
                }
                else if (string.Equals(zone.id, "fracture_court", StringComparison.Ordinal))
                {
                    if (zone.activationPoint != null) zone.activationPoint.localPosition = new Vector3(0f, 0f, -11.8f);
                    zone.activationRadius = 4.6f;
                    MoveEnemy(zone.enemies, "Court_SignalWarden", new Vector3(3.8f, -0.30f, -8.8f));
                    MoveEnemy(zone.enemies, "Court_AetherNeedle", new Vector3(-5.4f, 1.72f, -8.1f));
                }
            }

            Transform echoAnchor = FindTransform(ward, "Market_EchoAnchor");
            if (echoAnchor != null) echoAnchor.localPosition = new Vector3(7.2f, 0.75f, -20.5f);
        }

        private static void MoveEnemy(JourneyEnemyController[] enemies, string name, Vector3 localPosition)
        {
            if (enemies == null) return;
            for (int i = 0; i < enemies.Length; i++)
            {
                JourneyEnemyController enemy = enemies[i];
                if (enemy == null || !string.Equals(enemy.name, name, StringComparison.Ordinal)) continue;
                enemy.transform.localPosition = localPosition;
            }
        }

        private static int DisableLegacyOpeningGeometry(Transform ward)
        {
            int count = 0;
            Renderer[] renderers = ward.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null) continue;
                Transform t = renderer.transform;
                if (t.GetComponentInParent<JourneyEnemyController>() != null || t.GetComponentInParent<FracturedEchoNode>() != null) continue;
                string name = t.name;
                bool openingGeometry =
                    name.StartsWith("MemoryForge_", StringComparison.Ordinal) ||
                    name.StartsWith("Causeway_", StringComparison.Ordinal) ||
                    name.StartsWith("Market_", StringComparison.Ordinal);
                if (!openingGeometry) continue;
                renderer.enabled = false;
                Collider[] colliders = t.GetComponents<Collider>();
                for (int c = 0; c < colliders.Length; c++) colliders[c].enabled = false;
                count++;
            }
            return count;
        }

        private static void DisableDarkOpeningHeroProps()
        {
            GameObject v07 = EditorSceneLookup.FindIncludingInactive(WorldV07Builder.RootName);
            if (v07 == null) return;
            string[] names = { "Memory_Forge_Loom_V07", "Null_Market_Reliquary_V07" };
            for (int i = 0; i < names.Length; i++)
            {
                Transform child = v07.transform.Find(names[i]);
                if (child != null) child.gameObject.SetActive(false);
            }
        }

        private static void BuildPhaseTrigger(
            Transform parent,
            string name,
            Vector3 position,
            Vector3 size,
            OpeningExperienceDirectorV08 director,
            OpeningExperiencePhaseV08 phase,
            string reason)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            BoxCollider trigger = go.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = size;
            OpeningPhaseTriggerV08 phaseTrigger = go.AddComponent<OpeningPhaseTriggerV08>();
            phaseTrigger.ConfigureRuntime(director, phase, reason);
        }

        private static void BuildLighting(Transform parent)
        {
            GameObject sun = new GameObject("SanctumSun_V08");
            sun.transform.SetParent(parent, false);
            sun.transform.localRotation = Quaternion.Euler(38f, -32f, 0f);
            Light directional = sun.AddComponent<Light>();
            directional.type = LightType.Directional;
            directional.color = new Color(1f, 0.94f, 0.82f);
            directional.intensity = 0.82f;
            directional.shadows = LightShadows.Soft;

            PointLight("SanctumFillA", parent, new Vector3(-10f, 7f, -54f), new Color(0.55f, 0.80f, 1f), 1.4f, 18f);
            PointLight("SanctumFillB", parent, new Vector3(10f, 7f, -48f), new Color(1f, 0.82f, 0.52f), 1.2f, 18f);
            PointLight("ThresholdFill", parent, new Vector3(0f, 9f, -39f), new Color(0.62f, 0.86f, 1f), 1.6f, 20f);
            PointLight("CourtFill", parent, new Vector3(0f, 7f, -24f), new Color(1f, 0.88f, 0.68f), 1.05f, 18f);
        }

        private static void ConfigureRenderEnvironment()
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.34f, 0.40f, 0.48f);
            RenderSettings.ambientIntensity = 1.08f;
            RenderSettings.fog = false;
        }

        private static GameObject FindFoundation()
        {
            WorldStateLedger ledger = UnityEngine.Object.FindObjectOfType<WorldStateLedger>(true);
            return ledger != null ? ledger.gameObject : null;
        }

        private static Transform FindTransform(Transform root, string name)
        {
            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
                if (all[i] != null && string.Equals(all[i].name, name, StringComparison.Ordinal)) return all[i];
            return null;
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

        private static void CreateRing(
            string name,
            Transform parent,
            Vector3 localCenter,
            float radius,
            int segments,
            float width,
            Material material,
            Quaternion localRotation)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localCenter;
            go.transform.localRotation = localRotation;
            LineRenderer line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = Mathf.Max(12, segments);
            line.startWidth = width;
            line.endWidth = width;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            for (int i = 0; i < line.positionCount; i++)
            {
                float a = i / (float)line.positionCount * Mathf.PI * 2f;
                line.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius));
            }
        }

        private static void PointLight(string name, Transform parent, Vector3 position, Color color, float intensity, float range)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            Light light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
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
