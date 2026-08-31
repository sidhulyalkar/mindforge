#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Mindforge.Calibration;
using Mindforge.Combat;
using Mindforge.Journey;
using Mindforge.SoulWisp;

namespace Mindforge.Editor
{
    /// <summary>
    /// Editor-authored first journey: cavern -> ruined house -> cellar -> Warden
    /// chamber -> existing Fractured Signal arena. The final Arena V3 remains at its
    /// qualified location; the journey extends backward along -Z and the runtime
    /// director moves the Guardian to the journey start only when combat opens.
    /// </summary>
    public static class FirstJourneySceneBuilder
    {
        public const string RootName = "Mindforge_First_Journey_V1";
        private const string GeneratedFolder = "Assets/Mindforge/Generated/JourneyV1";
        private const string ProjectilePrefabPath = "Assets/Mindforge/Generated/Prefabs/MindforgeProjectile.prefab";

        [MenuItem("Mindforge/Legacy/Showcase/Apply First Journey Vertical Slice", priority = 24)]
        public static void BuildOpenScene()
        {
            GameObject arena = EditorSceneLookup.FindIncludingInactive("Fractured_Signal_Arena");
            GameObject guardian = EditorSceneLookup.FindIncludingInactive("Guardian");
            GameObject boss = EditorSceneLookup.FindIncludingInactive("The_Fractured_Signal");
            if (arena == null || guardian == null || boss == null)
                throw new InvalidOperationException("First Journey requires Fractured_Signal_Arena, Guardian and The_Fractured_Signal.");

            GameObject previous = EditorSceneLookup.FindIncludingInactive(RootName);
            if (previous != null) UnityEngine.Object.DestroyImmediate(previous);
            EnsureFolders();

            Material stone = EnsureLit("JourneyStone", new Color(0.055f, 0.065f, 0.105f), 0.18f, 0.36f);
            Material deepStone = EnsureLit("JourneyDeepStone", new Color(0.024f, 0.030f, 0.052f), 0.25f, 0.44f);
            Material plaster = EnsureLit("RuinedPlaster", new Color(0.19f, 0.19f, 0.22f), 0.05f, 0.31f);
            Material timber = EnsureLit("RuinedTimber", new Color(0.12f, 0.070f, 0.040f), 0.08f, 0.28f);
            Material copper = EnsureLit("JourneyCopper", new Color(0.31f, 0.15f, 0.055f), 0.88f, 0.66f);
            Material cyan = EnsureEmission("JourneyCyan", new Color(0.025f, 0.68f, 0.98f), 2.6f, 0.20f, 0.66f);
            Material teal = EnsureEmission("JourneyTeal", new Color(0.025f, 0.90f, 0.68f), 2.2f, 0.16f, 0.62f);
            Material hostile = EnsureEmission("JourneyHostile", new Color(0.94f, 0.075f, 0.24f), 2.4f, 0.14f, 0.56f);
            Material warden = EnsureEmission("JourneyWarden", new Color(0.69f, 0.12f, 0.90f), 2.8f, 0.22f, 0.64f);

            GameObject root = new GameObject(RootName);
            root.transform.SetParent(arena.transform, false);

            BuildCavern(root.transform, stone, deepStone, cyan, copper);
            BuildRuinedHouse(root.transform, stone, plaster, timber, copper, cyan);
            BuildCellar(root.transform, deepStone, timber, copper, teal);
            BuildWardenChamber(root.transform, deepStone, stone, copper, hostile, warden);
            BuildFinalApproach(root.transform, stone, copper, cyan, teal);

            GuardianTargetLock targetLock = guardian.GetComponent<GuardianTargetLock>();
            if (targetLock == null) targetLock = guardian.AddComponent<GuardianTargetLock>();
            targetLock.Configure(boss.transform);

            CombatantVitals playerVitals = guardian.GetComponent<CombatantVitals>();
            GuardianMotor playerMotor = guardian.GetComponent<GuardianMotor>();
            FluxMeter playerFlux = guardian.GetComponent<FluxMeter>();
            GuardianSwordShieldController defense = guardian.GetComponent<GuardianSwordShieldController>();
            SoulWispController wisp = UnityEngine.Object.FindObjectOfType<SoulWispController>(true);
            AwakeningCalibrationDirector calibration = UnityEngine.Object.FindObjectOfType<AwakeningCalibrationDirector>(true);
            CombatantVitals bossVitals = boss.GetComponent<CombatantVitals>();
            MindforgeProjectile projectile = AssetDatabase.LoadAssetAtPath<MindforgeProjectile>(ProjectilePrefabPath);
            if (projectile == null)
                throw new InvalidOperationException($"First Journey could not load {ProjectilePrefabPath}. Build the competition scene first.");

            Transform journeyStart = Marker("JourneyStart", root.transform, new Vector3(0f, 0.5f, -70f));
            Transform cavernTrigger = Marker("CavernEncounterTrigger", root.transform, new Vector3(0f, 0f, -65.5f));
            Transform houseTrigger = Marker("HouseEncounterTrigger", root.transform, new Vector3(0f, 0f, -47f));
            Transform cellarTrigger = Marker("CellarEncounterTrigger", root.transform, new Vector3(0f, 0f, -29.5f));
            Transform wardenTrigger = Marker("WardenEncounterTrigger", root.transform, new Vector3(0f, 0f, -14.3f));
            Transform bossTrigger = Marker("BossActivationTrigger", root.transform, new Vector3(0f, 0f, -0.8f));

            JourneyGate cavernGate = CreateGate("CavernSeal", root.transform, -51.5f, 9.2f, copper, cyan);
            JourneyGate houseGate = CreateGate("HouseCellarSeal", root.transform, -32.4f, 10.4f, copper, cyan);
            JourneyGate cellarGate = CreateGate("CellarWardenSeal", root.transform, -16.4f, 9.4f, copper, teal);
            JourneyGate wardenGate = CreateGate("WardenThresholdSeal", root.transform, -6.8f, 10.8f, copper, hostile);
            JourneyGate bossSeal = CreateGate("BossArenaSeal", root.transform, -4.1f, 11.2f, copper, warden);

            // The first Hollow starts close to the lesson trigger. The second waits by
            // the cavern exit so a new player gets a real 1v1 before pressure escalates.
            JourneyEnemyController cavernHollowA = CreateEnemy(
                "Cavern_Hollow_A", JourneyEnemyArchetype.Hollow, root.transform,
                new Vector3(-1.25f, -0.30f, -63.0f), guardian.transform, playerVitals, playerMotor, defense,
                projectile, playerFlux, hostile, stone, 46f, 44f);
            JourneyEnemyController cavernHollowB = CreateEnemy(
                "Cavern_Hollow_B", JourneyEnemyArchetype.Hollow, root.transform,
                new Vector3(1.65f, -0.30f, -52.8f), guardian.transform, playerVitals, playerMotor, defense,
                projectile, playerFlux, hostile, stone, 48f, 46f);

            JourneyEnemyController houseHollow = CreateEnemy(
                "House_Hollow", JourneyEnemyArchetype.Hollow, root.transform,
                new Vector3(-2.4f, -0.30f, -41.5f), guardian.transform, playerVitals, playerMotor, defense,
                projectile, playerFlux, hostile, plaster, 54f, 50f);
            JourneyEnemyController houseCaster = CreateEnemy(
                "House_Shardcaster", JourneyEnemyArchetype.Shardcaster, root.transform,
                new Vector3(2.75f, -0.30f, -36.2f), guardian.transform, playerVitals, playerMotor, defense,
                projectile, playerFlux, hostile, deepStone, 44f, 38f);

            JourneyEnemyController cellarHollowA = CreateEnemy(
                "Cellar_Hollow_A", JourneyEnemyArchetype.Hollow, root.transform,
                new Vector3(-2.2f, -0.30f, -26.0f), guardian.transform, playerVitals, playerMotor, defense,
                projectile, playerFlux, hostile, deepStone, 58f, 52f);
            JourneyEnemyController cellarHollowB = CreateEnemy(
                "Cellar_Hollow_B", JourneyEnemyArchetype.Hollow, root.transform,
                new Vector3(2.15f, -0.30f, -21.8f), guardian.transform, playerVitals, playerMotor, defense,
                projectile, playerFlux, hostile, deepStone, 58f, 52f);
            JourneyEnemyController cellarCaster = CreateEnemy(
                "Cellar_Shardcaster", JourneyEnemyArchetype.Shardcaster, root.transform,
                new Vector3(0.25f, -0.30f, -18.3f), guardian.transform, playerVitals, playerMotor, defense,
                projectile, playerFlux, hostile, deepStone, 50f, 44f);

            JourneyEnemyController signalWarden = CreateEnemy(
                "Signal_Warden", JourneyEnemyArchetype.SignalWarden, root.transform,
                new Vector3(0f, -0.30f, -10.6f), guardian.transform, playerVitals, playerMotor, defense,
                projectile, playerFlux, warden, deepStone, 190f, 118f);

            JourneyEncounterStage[] stages =
            {
                new JourneyEncounterStage
                {
                    id = "listening_cavern",
                    title = "THE LISTENING CAVERN",
                    lesson = "T lock · circle with WASD · F/LMB sword · Space directional dodge",
                    activationPoint = cavernTrigger,
                    activationRadius = 4.8f,
                    exitGate = cavernGate,
                    enemies = new[] { cavernHollowA, cavernHollowB },
                    clearHealFraction = 0.08f,
                },
                new JourneyEncounterStage
                {
                    id = "ruined_house",
                    title = "THE RUINED HOUSE",
                    lesson = "Switch targets while locked · RMB/E shield · reflected shots punish the caster",
                    activationPoint = houseTrigger,
                    activationRadius = 4.8f,
                    exitGate = houseGate,
                    enemies = new[] { houseHollow, houseCaster },
                    clearHealFraction = 0.08f,
                },
                new JourneyEncounterStage
                {
                    id = "cellar_passage",
                    title = "THE CELLAR",
                    lesson = "Read staggered pressure · C Counter Pulse · build Flux · R releases captured fire",
                    activationPoint = cellarTrigger,
                    activationRadius = 4.8f,
                    exitGate = cellarGate,
                    enemies = new[] { cellarHollowA, cellarHollowB, cellarCaster },
                    clearHealFraction = 0.10f,
                },
                new JourneyEncounterStage
                {
                    id = "signal_warden",
                    title = "THE SIGNAL WARDEN",
                    lesson = "One complete duel before the source · attack only after readable recovery",
                    activationPoint = wardenTrigger,
                    activationRadius = 4.8f,
                    exitGate = wardenGate,
                    enemies = new[] { signalWarden },
                    clearHealFraction = 0.12f,
                },
            };

            FirstJourneyDirector director = root.AddComponent<FirstJourneyDirector>();
            director.ConfigureRuntime(
                guardian.transform,
                playerVitals,
                targetLock,
                wisp,
                stages,
                boss,
                boss.transform,
                bossVitals,
                bossTrigger,
                bossSeal);
            SetRef(director, "journeyStart", journeyStart);

            FirstJourneyHud hud = root.AddComponent<FirstJourneyHud>();
            hud.ConfigureRuntime(director, calibration);

            // Keep boss dormant even when the combat arena root becomes active. The
            // journey director is the only component allowed to activate the boss after
            // the player crosses the final threshold.
            boss.SetActive(false);

            EditorUtility.SetDirty(root);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Mindforge:Journey] First Journey authored: cavern -> ruined house -> cellar -> Signal Warden -> existing Fractured Signal arena.");
        }

        private static void BuildCavern(Transform parent, Material stone, Material deepStone, Material cyan, Material copper)
        {
            Primitive("CavernFloor", PrimitiveType.Cube, parent, new Vector3(0f, -0.55f, -61f), new Vector3(12f, 0.50f, 20f), deepStone, true);
            Primitive("CavernWall_L", PrimitiveType.Cube, parent, new Vector3(-6.25f, 1.45f, -61f), new Vector3(0.90f, 4.0f, 20f), stone, true);
            Primitive("CavernWall_R", PrimitiveType.Cube, parent, new Vector3(6.25f, 1.45f, -61f), new Vector3(0.90f, 4.0f, 20f), stone, true);

            Vector3[] rocks =
            {
                new Vector3(-4.35f, 0.65f, -66.5f), new Vector3(4.55f, 0.85f, -63.0f),
                new Vector3(-4.75f, 0.55f, -58.2f), new Vector3(4.15f, 0.72f, -54.4f),
            };
            for (int i = 0; i < rocks.Length; i++)
            {
                GameObject rock = Primitive($"CavernButtress_{i:00}", PrimitiveType.Cube, parent,
                    rocks[i], new Vector3(1.45f, 2.3f + (i % 2) * 0.55f, 1.65f), stone, true);
                rock.transform.rotation = Quaternion.Euler(i % 2 == 0 ? 8f : -5f, 14f + i * 29f, i % 2 == 0 ? -6f : 5f);
            }

            for (int i = 0; i < 6; i++)
            {
                float z = -68f + i * 3.1f;
                float x = i % 2 == 0 ? -5.45f : 5.45f;
                Primitive($"CavernCopperBrace_{i:00}", PrimitiveType.Cube, parent,
                    new Vector3(x, 1.25f, z), new Vector3(0.16f, 2.5f, 1.15f), copper, false);
            }
            CreateLine("CavernSignalVein_L", parent, new Vector3(-3.6f, -0.27f, -69f), new Vector3(-2.2f, -0.27f, -52f), 0.035f, cyan);
            CreateLine("CavernSignalVein_R", parent, new Vector3(3.9f, -0.27f, -69f), new Vector3(2.4f, -0.27f, -52f), 0.022f, cyan);
            PointLight("CavernGuideLight_A", parent, new Vector3(-4.8f, 2.1f, -63f), new Color(0.10f, 0.58f, 0.82f), 2.4f, 8f);
            PointLight("CavernGuideLight_B", parent, new Vector3(4.6f, 1.8f, -54f), new Color(0.08f, 0.42f, 0.64f), 1.8f, 7f);
        }

        private static void BuildRuinedHouse(Transform parent, Material stone, Material plaster, Material timber, Material copper, Material cyan)
        {
            Primitive("HouseFloor", PrimitiveType.Cube, parent, new Vector3(0f, -0.55f, -41.5f), new Vector3(14f, 0.50f, 18f), stone, true);
            Primitive("HouseWall_L", PrimitiveType.Cube, parent, new Vector3(-7.2f, 1.55f, -41.5f), new Vector3(0.55f, 4.2f, 18f), plaster, true);
            Primitive("HouseWall_R", PrimitiveType.Cube, parent, new Vector3(7.2f, 1.55f, -41.5f), new Vector3(0.55f, 4.2f, 18f), plaster, true);
            Primitive("HouseFacade_L", PrimitiveType.Cube, parent, new Vector3(-4.55f, 1.55f, -50.1f), new Vector3(5.2f, 4.2f, 0.55f), plaster, true);
            Primitive("HouseFacade_R", PrimitiveType.Cube, parent, new Vector3(4.55f, 1.55f, -50.1f), new Vector3(5.2f, 4.2f, 0.55f), plaster, true);
            Primitive("HouseRear_L", PrimitiveType.Cube, parent, new Vector3(-4.35f, 1.55f, -32.9f), new Vector3(5.4f, 4.2f, 0.55f), plaster, true);
            Primitive("HouseRear_R", PrimitiveType.Cube, parent, new Vector3(4.35f, 1.55f, -32.9f), new Vector3(5.4f, 4.2f, 0.55f), plaster, true);

            for (int i = 0; i < 5; i++)
            {
                Primitive($"HouseCeilingBeam_{i:00}", PrimitiveType.Cube, parent,
                    new Vector3(0f, 3.35f, -48f + i * 3.2f), new Vector3(13.6f, 0.22f, 0.30f), timber, false);
            }
            Primitive("HouseCoverTable", PrimitiveType.Cube, parent, new Vector3(-2.6f, 0.28f, -40.2f), new Vector3(2.3f, 1.05f, 1.2f), timber, true);
            Primitive("HouseBrokenCabinet", PrimitiveType.Cube, parent, new Vector3(3.1f, 0.48f, -44.4f), new Vector3(1.4f, 1.55f, 0.85f), timber, true).transform.rotation = Quaternion.Euler(0f, 18f, -5f);
            Primitive("HouseCopperThreshold", PrimitiveType.Cube, parent, new Vector3(0f, -0.24f, -33.25f), new Vector3(4.0f, 0.08f, 0.34f), copper, false);
            CreateLine("HouseSignalInlay", parent, new Vector3(0f, -0.27f, -49.5f), new Vector3(0f, -0.27f, -33.2f), 0.028f, cyan);
            PointLight("HouseWarmRemnant", parent, new Vector3(-4.6f, 2.35f, -43f), new Color(0.76f, 0.34f, 0.12f), 2.0f, 8f);
            PointLight("HouseSignalLight", parent, new Vector3(4.8f, 2.0f, -35.5f), new Color(0.08f, 0.62f, 0.82f), 2.1f, 8f);
        }

        private static void BuildCellar(Transform parent, Material stone, Material timber, Material copper, Material teal)
        {
            Primitive("CellarFloor", PrimitiveType.Cube, parent, new Vector3(0f, -0.55f, -24.4f), new Vector3(11f, 0.50f, 17f), stone, true);
            Primitive("CellarWall_L", PrimitiveType.Cube, parent, new Vector3(-5.7f, 1.40f, -24.4f), new Vector3(0.70f, 3.8f, 17f), stone, true);
            Primitive("CellarWall_R", PrimitiveType.Cube, parent, new Vector3(5.7f, 1.40f, -24.4f), new Vector3(0.70f, 3.8f, 17f), stone, true);
            for (int i = 0; i < 6; i++)
            {
                float z = -31f + i * 2.8f;
                Primitive($"CellarBeam_{i:00}", PrimitiveType.Cube, parent,
                    new Vector3(0f, 3.05f, z), new Vector3(10.9f, 0.26f, 0.34f), timber, false);
            }
            Primitive("CellarCover_L", PrimitiveType.Cube, parent, new Vector3(-2.9f, 0.62f, -24f), new Vector3(1.1f, 1.8f, 1.4f), stone, true);
            Primitive("CellarCover_R", PrimitiveType.Cube, parent, new Vector3(2.6f, 0.50f, -20.1f), new Vector3(1.35f, 1.45f, 1.0f), stone, true);
            for (int i = 0; i < 5; i++)
                Primitive($"CellarCopperRib_{i:00}", PrimitiveType.Cube, parent,
                    new Vector3(i % 2 == 0 ? -5.15f : 5.15f, 1.0f, -30f + i * 3.0f),
                    new Vector3(0.12f, 2.0f, 0.55f), copper, false);
            CreateLine("CellarSignalSpine", parent, new Vector3(0f, -0.26f, -32f), new Vector3(0f, -0.26f, -16.8f), 0.040f, teal);
            PointLight("CellarSignalLight_A", parent, new Vector3(-3.9f, 1.7f, -27f), new Color(0.05f, 0.68f, 0.54f), 2.2f, 7f);
            PointLight("CellarSignalLight_B", parent, new Vector3(3.8f, 1.8f, -18.5f), new Color(0.08f, 0.56f, 0.48f), 2.0f, 7f);
        }

        private static void BuildWardenChamber(Transform parent, Material deepStone, Material stone, Material copper, Material hostile, Material warden)
        {
            // The base competition ArenaFloor spans z=-11..13. End the authored
            // journey floor exactly at z=-11 so there is one authoritative floor surface
            // at every point and no coplanar static-collider or renderer overlap.
            Primitive("WardenFloor", PrimitiveType.Cube, parent, new Vector3(0f, -0.55f, -13.6f), new Vector3(15f, 0.50f, 5.2f), deepStone, true);
            Primitive("WardenWall_L", PrimitiveType.Cube, parent, new Vector3(-7.65f, 1.6f, -12.45f), new Vector3(0.70f, 4.4f, 7.5f), stone, true);
            Primitive("WardenWall_R", PrimitiveType.Cube, parent, new Vector3(7.65f, 1.6f, -12.45f), new Vector3(0.70f, 4.4f, 7.5f), stone, true);
            CreateCircle("WardenOuterRing", parent, new Vector3(0f, -0.25f, -10.8f), 4.2f, 64, 0.045f, copper);
            CreateCircle("WardenHostileRing", parent, new Vector3(0f, -0.24f, -10.8f), 2.8f, 56, 0.035f, hostile);
            for (int i = 0; i < 6; i++)
            {
                float a = i / 6f * Mathf.PI * 2f;
                Vector3 radial = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                Primitive($"WardenObelisk_{i:00}", PrimitiveType.Cube, parent,
                    new Vector3(radial.x * 5.7f, 1.15f, -10.8f + radial.z * 3.5f),
                    new Vector3(0.55f, 2.8f, 0.55f), i % 2 == 0 ? copper : deepStone, true);
            }
            PointLight("WardenCoreLight", parent, new Vector3(0f, 2.5f, -10.8f), new Color(0.58f, 0.10f, 0.82f), 3.0f, 10f);
            PointLight("WardenThreatLight", parent, new Vector3(0f, 1.0f, -7.3f), new Color(0.82f, 0.07f, 0.22f), 1.7f, 7f);
        }

        private static void BuildFinalApproach(Transform parent, Material stone, Material copper, Material cyan, Material teal)
        {
            // Arena V3 already owns the floor here. Add only architectural guidance so
            // the transition stays visually rich without duplicate floor authority.
            Primitive("BossApproachPier_L", PrimitiveType.Cube, parent, new Vector3(-5.5f, 1.0f, -3.5f), new Vector3(0.65f, 2.6f, 7f), stone, true);
            Primitive("BossApproachPier_R", PrimitiveType.Cube, parent, new Vector3(5.5f, 1.0f, -3.5f), new Vector3(0.65f, 2.6f, 7f), stone, true);
            Primitive("BossApproachCopper_L", PrimitiveType.Cube, parent, new Vector3(-4.8f, 0.05f, -3.4f), new Vector3(0.07f, 0.07f, 6.4f), copper, false);
            Primitive("BossApproachCopper_R", PrimitiveType.Cube, parent, new Vector3(4.8f, 0.05f, -3.4f), new Vector3(0.07f, 0.07f, 6.4f), copper, false);
            CreateLine("BossApproachSignal_L", parent, new Vector3(-1.2f, -0.25f, -6.5f), new Vector3(-1.2f, -0.25f, 0f), 0.030f, cyan);
            CreateLine("BossApproachSignal_R", parent, new Vector3(1.2f, -0.25f, -6.5f), new Vector3(1.2f, -0.25f, 0f), 0.030f, teal);
        }

        private static JourneyEnemyController CreateEnemy(
            string name,
            JourneyEnemyArchetype archetype,
            Transform parent,
            Vector3 position,
            Transform player,
            CombatantVitals playerVitals,
            GuardianMotor playerMotor,
            GuardianSwordShieldController defense,
            MindforgeProjectile projectile,
            FluxMeter playerFlux,
            Material coreMaterial,
            Material bodyMaterial,
            float health,
            float poiseValue)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.position = position;

            CapsuleCollider collider = root.AddComponent<CapsuleCollider>();
            collider.center = new Vector3(0f, archetype == JourneyEnemyArchetype.SignalWarden ? 1.05f : 0.88f, 0f);
            collider.height = archetype == JourneyEnemyArchetype.SignalWarden ? 2.1f : 1.75f;
            collider.radius = archetype == JourneyEnemyArchetype.SignalWarden ? 0.58f : 0.43f;

            Rigidbody body = root.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.mass = archetype == JourneyEnemyArchetype.SignalWarden ? 3.2f : 1.3f;
            body.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            PoiseSystem poise = root.AddComponent<PoiseSystem>();
            SetFloat(poise, "maxPoise", poiseValue);
            SetFloat(poise, "recoveryPerSecond", archetype == JourneyEnemyArchetype.SignalWarden ? 7f : 5f);
            SetFloat(poise, "breakDuration", archetype == JourneyEnemyArchetype.SignalWarden ? 1.45f : 1.25f);

            CombatantVitals vitals = root.AddComponent<CombatantVitals>();
            SetEnum(vitals, "team", (int)CombatTeam.Enemy);
            SetFloat(vitals, "maxHealth", health);
            SetRef(vitals, "poise", poise);
            SetRef(vitals, "body", body);

            GameObject visuals = new GameObject("VisualRoot");
            visuals.transform.SetParent(root.transform, false);

            float scale = archetype == JourneyEnemyArchetype.SignalWarden ? 1.18f : archetype == JourneyEnemyArchetype.Shardcaster ? 0.88f : 1f;
            Primitive("Torso", archetype == JourneyEnemyArchetype.Shardcaster ? PrimitiveType.Cylinder : PrimitiveType.Capsule,
                visuals.transform, root.transform.position + new Vector3(0f, 0.90f * scale, 0f),
                new Vector3(0.62f * scale, 0.86f * scale, 0.62f * scale), bodyMaterial, false);
            Primitive("Shoulder_L", PrimitiveType.Cube, visuals.transform,
                root.transform.position + new Vector3(-0.48f * scale, 1.18f * scale, 0f),
                new Vector3(0.38f * scale, 0.24f * scale, 0.54f * scale), bodyMaterial, false);
            Primitive("Shoulder_R", PrimitiveType.Cube, visuals.transform,
                root.transform.position + new Vector3(0.48f * scale, 1.18f * scale, 0f),
                new Vector3(0.38f * scale, 0.24f * scale, 0.54f * scale), bodyMaterial, false);
            GameObject head = Primitive("Head", PrimitiveType.Cube, visuals.transform,
                root.transform.position + new Vector3(0f, 1.68f * scale, 0f),
                new Vector3(0.48f * scale, 0.48f * scale, 0.46f * scale), bodyMaterial, false);
            head.transform.localRotation = Quaternion.Euler(8f, 45f, 0f);
            GameObject core = Primitive("SignalCore", PrimitiveType.Sphere, visuals.transform,
                root.transform.position + new Vector3(0f, 1.05f * scale, -0.34f * scale),
                Vector3.one * (archetype == JourneyEnemyArchetype.SignalWarden ? 0.34f : 0.24f), coreMaterial, false);

            if (archetype == JourneyEnemyArchetype.SignalWarden)
            {
                Primitive("WardenCrown", PrimitiveType.Cylinder, visuals.transform,
                    root.transform.position + new Vector3(0f, 2.05f, 0f), new Vector3(0.48f, 0.12f, 0.48f), coreMaterial, false);
                Primitive("WardenBlade", PrimitiveType.Cube, visuals.transform,
                    root.transform.position + new Vector3(0.78f, 1.05f, 0.10f), new Vector3(0.12f, 1.35f, 0.18f), coreMaterial, false)
                    .transform.rotation = Quaternion.Euler(0f, 0f, -22f);
            }
            else if (archetype == JourneyEnemyArchetype.Shardcaster)
            {
                Primitive("CasterHalo", PrimitiveType.Cylinder, visuals.transform,
                    root.transform.position + new Vector3(0f, 1.72f, 0f), new Vector3(0.72f, 0.045f, 0.72f), coreMaterial, false);
            }

            GameObject ring = CreateLocalRing("AttackTelegraph", root.transform, archetype == JourneyEnemyArchetype.SignalWarden ? 1.45f : 1.0f, coreMaterial);
            Light coreLight = new GameObject("CoreLight").AddComponent<Light>();
            coreLight.transform.SetParent(visuals.transform, false);
            coreLight.transform.localPosition = new Vector3(0f, 1.15f * scale, -0.25f);
            coreLight.type = LightType.Point;
            coreLight.range = archetype == JourneyEnemyArchetype.SignalWarden ? 5.5f : 3.5f;
            coreLight.intensity = archetype == JourneyEnemyArchetype.SignalWarden ? 2.0f : 1.2f;

            Transform origin = Marker("ProjectileOrigin", root.transform,
                root.transform.position + new Vector3(0f, 1.22f * scale, 0.48f * scale));
            JourneyEnemyController controller = root.AddComponent<JourneyEnemyController>();
            controller.ConfigureRuntime(archetype, player, playerVitals, playerMotor, defense, projectile, origin, playerFlux);
            JourneyEnemyPresentation presentation = root.AddComponent<JourneyEnemyPresentation>();
            presentation.ConfigureRuntime(controller, visuals.transform, core.transform, ring.transform, core.GetComponent<Renderer>(), coreLight);

            root.SetActive(false);
            return controller;
        }

        private static JourneyGate CreateGate(string name, Transform parent, float z, float width, Material frame, Material energy)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.position = new Vector3(0f, 0f, z);
            BoxCollider blocker = root.AddComponent<BoxCollider>();
            blocker.center = new Vector3(0f, 1.45f, 0f);
            blocker.size = new Vector3(width, 3.2f, 0.42f);

            GameObject visuals = new GameObject("SealVisuals");
            visuals.transform.SetParent(root.transform, false);
            for (int i = 0; i < 5; i++)
            {
                float x = Mathf.Lerp(-width * 0.42f, width * 0.42f, i / 4f);
                Primitive($"EnergyBar_{i:00}", PrimitiveType.Cube, visuals.transform,
                    root.transform.position + new Vector3(x, 1.45f, 0f), new Vector3(0.09f, 2.9f, 0.14f), energy, false);
            }
            Primitive("FrameTop", PrimitiveType.Cube, visuals.transform,
                root.transform.position + new Vector3(0f, 3.02f, 0f), new Vector3(width, 0.18f, 0.32f), frame, false);
            Primitive("FrameL", PrimitiveType.Cube, visuals.transform,
                root.transform.position + new Vector3(-width * 0.50f, 1.45f, 0f), new Vector3(0.22f, 3.2f, 0.38f), frame, false);
            Primitive("FrameR", PrimitiveType.Cube, visuals.transform,
                root.transform.position + new Vector3(width * 0.50f, 1.45f, 0f), new Vector3(0.22f, 3.2f, 0.38f), frame, false);

            JourneyGate gate = root.AddComponent<JourneyGate>();
            gate.ConfigureRuntime(visuals.transform, new Collider[] { blocker });
            return gate;
        }

        private static GameObject CreateLocalRing(string name, Transform parent, float radius, Material material)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            LineRenderer line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = 48;
            line.widthMultiplier = 0.055f;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            for (int i = 0; i < line.positionCount; i++)
            {
                float a = i / (float)line.positionCount * Mathf.PI * 2f;
                line.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius));
            }
            return go;
        }

        private static Transform Marker(string name, Transform parent, Vector3 worldPosition)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = worldPosition;
            return go.transform;
        }

        private static GameObject Primitive(string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Material material, bool keepCollider)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = scale;
            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }
            if (!keepCollider)
            {
                Collider collider = go.GetComponent<Collider>();
                if (collider != null) UnityEngine.Object.DestroyImmediate(collider);
            }
            return go;
        }

        private static void CreateLine(string name, Transform parent, Vector3 start, Vector3 end, float width, Material material)
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

        private static void CreateCircle(string name, Transform parent, Vector3 center, float radius, int points, float width, Material material)
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

        private static void PointLight(string name, Transform parent, Vector3 position, Color color, float intensity, float range)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            Light light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.Soft;
        }

        private static Material EnsureLit(string name, Color color, float metallic, float smoothness)
        {
            string path = $"{GeneratedFolder}/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                if (shader == null) throw new InvalidOperationException("No URP/Lit or Standard shader available for First Journey.");
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

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Mindforge/Generated"))
                AssetDatabase.CreateFolder("Assets/Mindforge", "Generated");
            if (!AssetDatabase.IsValidFolder(GeneratedFolder))
                AssetDatabase.CreateFolder("Assets/Mindforge/Generated", "JourneyV1");
        }

        private static void SetRef(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty property = so.FindProperty(field);
            if (property == null) throw new InvalidOperationException($"{target.GetType().Name}.{field} not found");
            property.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFloat(UnityEngine.Object target, string field, float value)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty property = so.FindProperty(field);
            if (property == null) throw new InvalidOperationException($"{target.GetType().Name}.{field} not found");
            property.floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetEnum(UnityEngine.Object target, string field, int value)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty property = so.FindProperty(field);
            if (property == null) throw new InvalidOperationException($"{target.GetType().Name}.{field} not found");
            property.enumValueIndex = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
