#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Mindforge.Combat;
using Mindforge.Journey;
using Mindforge.SoulWisp;
using Mindforge.Telemetry;
using Mindforge.World;

namespace Mindforge.Editor
{
    /// <summary>
    /// One-click authored Null Ward vertical slice. This class owns scene composition
    /// only. Combat, checkpoint, shortcut, boss and neural authority remain in their
    /// respective runtime components.
    ///
    /// Topology:
    /// Memory Forge -> Synapse Causeway -> Null Market -> Protocol Veil -> Cathedral
    ///                    ^                    |
    ///                    |--- maintenance ----|
    ///                          shortcut
    /// </summary>
    public static class NullWardSceneBuilder
    {
        public const string RootName = "Mindforge_Null_Ward_V1";
        private const string GeneratedFolder = "Assets/Mindforge/Generated/NullWardV1";
        private const string ProjectilePrefabPath = "Assets/Mindforge/Generated/Prefabs/MindforgeProjectile.prefab";
        private const string EchoPrefabPath = "Assets/Mindforge/Generated/Prefabs/FracturedEcho.prefab";

        [MenuItem("Mindforge/Showcase/Apply Null Ward Vertical Slice", priority = 24)]
        public static void BuildOpenScene()
        {
            GameObject arena = EditorSceneLookup.FindIncludingInactive("Fractured_Signal_Arena");
            GameObject guardian = EditorSceneLookup.FindIncludingInactive("Guardian");
            GameObject boss = EditorSceneLookup.FindIncludingInactive("The_Fractured_Signal");
            if (arena == null || guardian == null || boss == null)
                throw new InvalidOperationException("Null Ward requires Fractured_Signal_Arena, Guardian and The_Fractured_Signal.");

            GameObject previous = EditorSceneLookup.FindIncludingInactive(RootName);
            if (previous != null) UnityEngine.Object.DestroyImmediate(previous);
            GameObject legacyJourney = EditorSceneLookup.FindIncludingInactive(FirstJourneySceneBuilder.RootName);
            if (legacyJourney != null) UnityEngine.Object.DestroyImmediate(legacyJourney);
            EnsureFolders();

            Material basalt = EnsureLit("NullBasalt", new Color(0.028f, 0.035f, 0.055f), 0.28f, 0.52f);
            Material obsidian = EnsureLit("NullObsidian", new Color(0.012f, 0.016f, 0.028f), 0.42f, 0.70f);
            Material metal = EnsureLit("NullWornMetal", new Color(0.17f, 0.19f, 0.23f), 0.82f, 0.50f);
            Material copper = EnsureLit("NullCopper", new Color(0.30f, 0.12f, 0.045f), 0.90f, 0.64f);
            Material cyan = EnsureEmission("NullCyan", new Color(0.025f, 0.66f, 1.0f), 2.25f, 0.18f, 0.62f);
            Material viridian = EnsureEmission("NullViridian", new Color(0.04f, 0.95f, 0.57f), 2.10f, 0.15f, 0.58f);
            Material hostile = EnsureEmission("NullHostile", new Color(0.96f, 0.065f, 0.22f), 2.35f, 0.16f, 0.56f);
            Material echoMat = EnsureEmission("NullEcho", new Color(0.68f, 0.14f, 0.92f), 2.35f, 0.22f, 0.62f);

            MindforgeProjectile projectile = AssetDatabase.LoadAssetAtPath<MindforgeProjectile>(ProjectilePrefabPath);
            GameObject echoPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EchoPrefabPath);
            if (projectile == null || echoPrefab == null)
                throw new InvalidOperationException("Null Ward requires generated projectile and Fractured Echo prefabs. Build the competition scene first.");

            GameObject root = new GameObject(RootName);
            root.transform.SetParent(arena.transform, false);
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;

            BuildMemoryForge(root.transform, basalt, metal, copper, cyan, viridian);
            BuildCauseway(root.transform, basalt, obsidian, metal, copper, cyan);
            BuildMarket(root.transform, basalt, obsidian, metal, copper, cyan, viridian);
            BuildMaintenanceLoop(root.transform, basalt, metal, copper, viridian);
            BuildCathedralApproach(root.transform, basalt, obsidian, metal, copper, cyan, viridian);

            CombatantVitals playerVitals = guardian.GetComponent<CombatantVitals>();
            GuardianMotor playerMotor = guardian.GetComponent<GuardianMotor>();
            GuardianSwordShieldController playerDefense = guardian.GetComponent<GuardianSwordShieldController>();
            GuardianStamina guardIntegrity = guardian.GetComponent<GuardianStamina>();
            FluxMeter playerFlux = guardian.GetComponent<FluxMeter>();
            GuardianTargetLock targetLock = guardian.GetComponent<GuardianTargetLock>();
            if (targetLock == null) targetLock = guardian.AddComponent<GuardianTargetLock>();
            targetLock.Configure(boss.transform);

            SoulWispController wisp = UnityEngine.Object.FindObjectOfType<SoulWispController>(true);
            UdpGameMarkerSender markers = UnityEngine.Object.FindObjectOfType<UdpGameMarkerSender>(true);
            CombatantVitals bossVitals = boss.GetComponent<CombatantVitals>();
            FracturedSignalDirector bossDirector = boss.GetComponent<FracturedSignalDirector>();

            Transform worldStart = Marker("NullWard_WorldStart", root.transform, new Vector3(0f, 0.5f, -59.2f));
            Transform forgeSpawn = Marker("MemoryForge_Respawn", root.transform, new Vector3(0f, 0.5f, -58.2f));
            Transform forgeInteract = Marker("MemoryForge_Interact", root.transform, new Vector3(-2.2f, 0.2f, -56.8f));
            Transform causewayTrigger = Marker("Causeway_EncounterTrigger", root.transform, new Vector3(0f, 0f, -49.4f));
            Transform marketTrigger = Marker("Market_EncounterTrigger", root.transform, new Vector3(0f, 0f, -32.0f));
            Transform echoAnchor = Marker("Market_EchoAnchor", root.transform, new Vector3(2.8f, 0.75f, -27.5f));
            Transform shortcutInteract = Marker("Maintenance_ShortcutInteract", root.transform, new Vector3(6.7f, 0.2f, -54.0f));
            Transform bossTrigger = Marker("Cathedral_BossActivation", root.transform, new Vector3(0f, 0f, -0.8f));

            JourneyGate shortcutGate = CreateGate(
                "MemoryConduit_Shortcut", root.transform, new Vector3(5.45f, 0f, -55.0f), 5.0f, metal, viridian,
                new Vector3(0f, -4.4f, 0f));
            JourneyGate protocolVeil = CreateGate(
                "Protocol_Veil", root.transform, new Vector3(0f, 0f, -18.0f), 8.4f, copper, cyan,
                new Vector3(0f, -5.0f, 0f));
            JourneyGate bossSeal = CreateGate(
                "Cathedral_Boss_Seal", root.transform, new Vector3(0f, 0f, -3.6f), 10.4f, metal, echoMat,
                new Vector3(0f, -5.4f, 0f));

            JourneyEnemyController sentryA = CreateEnemy(
                "Causeway_NullSentry_A", JourneyEnemyArchetype.NullSentry, root.transform,
                new Vector3(-1.8f, -0.30f, -45.6f), guardian.transform, playerVitals, playerMotor, playerDefense,
                projectile, playerFlux, hostile, metal, 58f, 52f, 0.88f);
            JourneyEnemyController sentryB = CreateEnemy(
                "Causeway_NullSentry_B", JourneyEnemyArchetype.NullSentry, root.transform,
                new Vector3(1.9f, -0.30f, -39.2f), guardian.transform, playerVitals, playerMotor, playerDefense,
                projectile, playerFlux, hostile, obsidian, 58f, 52f, 0.88f);

            JourneyEnemyController penitent = CreateEnemy(
                "Market_ChromePenitent", JourneyEnemyArchetype.ChromePenitent, root.transform,
                new Vector3(-3.2f, -0.30f, -29.0f), guardian.transform, playerVitals, playerMotor, playerDefense,
                projectile, playerFlux, hostile, metal, 78f, 72f, 1.02f);

            FracturedEchoNode worldEcho = CreateWorldEcho(
                echoPrefab, root.transform, echoAnchor, guardian.transform, playerFlux, 0.35f, echoMat);

            NullWardEncounterZone[] zones =
            {
                new NullWardEncounterZone
                {
                    id = "synapse_causeway",
                    title = "SYNAPSE CAUSEWAY",
                    lesson = "Tracking bolts punish panic rolls · read the cast, then commit",
                    activationPoint = causewayTrigger,
                    activationRadius = 5.6f,
                    requiredForProtocol = true,
                    enemies = new[] { sentryA, sentryB },
                    echoes = Array.Empty<FracturedEchoNode>(),
                },
                new NullWardEncounterZone
                {
                    id = "null_market",
                    title = "NULL MARKET",
                    lesson = "Separate melee pressure from the Echo · break the tactical object when space opens",
                    activationPoint = marketTrigger,
                    activationRadius = 7.0f,
                    requiredForProtocol = true,
                    enemies = new[] { penitent },
                    echoes = new[] { worldEcho },
                },
            };

            GameObject checkpointObject = new GameObject("Memory_Forge_Checkpoint");
            checkpointObject.transform.SetParent(root.transform, false);
            checkpointObject.transform.localPosition = forgeInteract.localPosition;
            MemoryForgeCheckpoint checkpoint = checkpointObject.AddComponent<MemoryForgeCheckpoint>();

            WorldShortcut shortcut = shortcutGate.gameObject.AddComponent<WorldShortcut>();
            shortcut.ConfigureRuntime(
                guardian.transform,
                shortcutInteract,
                shortcutGate,
                "memory_forge_market_loop",
                markers);

            NullWardEncounterDirector director = root.AddComponent<NullWardEncounterDirector>();
            director.ConfigureRuntime(
                guardian.transform,
                playerVitals,
                targetLock,
                wisp,
                worldStart,
                checkpoint,
                zones,
                protocolVeil,
                boss,
                boss.transform,
                bossVitals,
                bossDirector,
                bossTrigger,
                bossSeal,
                markers);

            checkpoint.ConfigureRuntime(
                guardian.transform,
                playerVitals,
                guardIntegrity,
                targetLock,
                forgeSpawn,
                forgeInteract,
                director,
                markers);

            NullWardHud hud = root.AddComponent<NullWardHud>();
            hud.ConfigureRuntime(director, checkpoint, shortcut);

            // The director owns when final boss authority becomes active.
            boss.SetActive(false);
            shortcutGate.SetOpen(false, true);
            protocolVeil.SetOpen(false, true);
            bossSeal.SetOpen(true, true);

            EditorUtility.SetDirty(root);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Mindforge:NullWard] Authored Memory Forge -> Synapse Causeway -> Null Market + maintenance shortcut -> Protocol Veil -> Signal Cathedral -> existing Fractured Signal.");
        }

        private static void BuildMemoryForge(Transform parent, Material basalt, Material metal, Material copper, Material cyan, Material viridian)
        {
            Primitive("MemoryForge_Floor", PrimitiveType.Cube, parent, new Vector3(0f, -0.55f, -57f), new Vector3(12f, 0.50f, 9f), basalt, true);
            Primitive("MemoryForge_Back", PrimitiveType.Cube, parent, new Vector3(0f, 1.45f, -61.7f), new Vector3(12f, 4.0f, 0.55f), basalt, true);
            Primitive("MemoryForge_Left", PrimitiveType.Cube, parent, new Vector3(-6.25f, 1.2f, -57.2f), new Vector3(0.55f, 3.4f, 8.7f), basalt, true);
            Primitive("MemoryForge_Right_A", PrimitiveType.Cube, parent, new Vector3(6.25f, 1.2f, -59.1f), new Vector3(0.55f, 3.4f, 4.8f), basalt, true);

            Primitive("MemoryForge_AnvilBase", PrimitiveType.Cylinder, parent, new Vector3(-2.2f, 0.0f, -56.8f), new Vector3(1.5f, 0.38f, 1.5f), metal, true);
            Primitive("MemoryForge_Core", PrimitiveType.Cylinder, parent, new Vector3(-2.2f, 0.72f, -56.8f), new Vector3(0.72f, 1.35f, 0.72f), copper, false);
            Primitive("MemoryForge_Signal", PrimitiveType.Sphere, parent, new Vector3(-2.2f, 1.38f, -56.8f), Vector3.one * 0.48f, cyan, false);

            for (int i = 0; i < 4; i++)
            {
                float x = -4.5f + i * 3.0f;
                Primitive($"MemoryForge_Rib_{i:00}", PrimitiveType.Cube, parent,
                    new Vector3(x, 2.05f, -60.8f), new Vector3(0.20f, 3.2f, 0.22f), copper, false);
            }
            CreateLine("MemoryForge_Conduit_Cyan", parent, new Vector3(-1.4f, -0.27f, -56f), new Vector3(-1.4f, -0.27f, -51.8f), 0.035f, cyan);
            CreateLine("MemoryForge_Conduit_Green", parent, new Vector3(1.4f, -0.27f, -56f), new Vector3(1.4f, -0.27f, -51.8f), 0.030f, viridian);
            PointLight("MemoryForge_Light", parent, new Vector3(-2.2f, 2.5f, -56.8f), new Color(0.08f, 0.60f, 0.82f), 2.0f, 7.0f);
        }

        private static void BuildCauseway(Transform parent, Material basalt, Material obsidian, Material metal, Material copper, Material cyan)
        {
            Primitive("Causeway_Floor", PrimitiveType.Cube, parent, new Vector3(0f, -0.55f, -44.2f), new Vector3(8.5f, 0.50f, 17.5f), obsidian, true);
            Primitive("Causeway_Rail_L", PrimitiveType.Cube, parent, new Vector3(-4.55f, 0.15f, -44.2f), new Vector3(0.38f, 1.0f, 17.5f), metal, true);
            Primitive("Causeway_Rail_R_A", PrimitiveType.Cube, parent, new Vector3(4.55f, 0.15f, -47.3f), new Vector3(0.38f, 1.0f, 11.0f), metal, true);
            Primitive("Causeway_Rail_R_B", PrimitiveType.Cube, parent, new Vector3(4.55f, 0.15f, -38.3f), new Vector3(0.38f, 1.0f, 4.0f), metal, true);

            for (int i = 0; i < 6; i++)
            {
                float z = -51f + i * 2.7f;
                float x = i % 2 == 0 ? -4.05f : 4.05f;
                Primitive($"Causeway_Buttress_{i:00}", PrimitiveType.Cube, parent,
                    new Vector3(x, 1.05f, z), new Vector3(0.68f, 2.45f, 0.82f), basalt, true);
                Primitive($"Causeway_Copper_{i:00}", PrimitiveType.Cube, parent,
                    new Vector3(x * 0.93f, 1.85f, z), new Vector3(0.12f, 1.8f, 0.12f), copper, false);
            }
            CreateLine("Causeway_DataRail", parent, new Vector3(0f, -0.26f, -52f), new Vector3(0f, -0.26f, -35.8f), 0.040f, cyan);
            PointLight("Causeway_Light_A", parent, new Vector3(-3.7f, 2.5f, -47f), new Color(0.06f, 0.46f, 0.70f), 1.6f, 7.5f);
            PointLight("Causeway_Light_B", parent, new Vector3(3.7f, 2.2f, -39f), new Color(0.05f, 0.38f, 0.62f), 1.4f, 7.0f);
        }

        private static void BuildMarket(Transform parent, Material basalt, Material obsidian, Material metal, Material copper, Material cyan, Material viridian)
        {
            Primitive("NullMarket_Floor", PrimitiveType.Cube, parent, new Vector3(0f, -0.55f, -29f), new Vector3(22f, 0.50f, 14f), basalt, true);
            Primitive("NullMarket_Wall_L", PrimitiveType.Cube, parent, new Vector3(-11.25f, 1.25f, -29f), new Vector3(0.55f, 3.6f, 14f), obsidian, true);
            Primitive("NullMarket_Wall_R_North", PrimitiveType.Cube, parent, new Vector3(11.25f, 1.25f, -26f), new Vector3(0.55f, 3.6f, 8f), obsidian, true);

            Vector3[] kiosks =
            {
                new Vector3(-7.4f, 0.2f, -31.8f), new Vector3(6.6f, 0.2f, -32.0f),
                new Vector3(-6.2f, 0.2f, -24.8f), new Vector3(7.4f, 0.2f, -25.3f),
            };
            for (int i = 0; i < kiosks.Length; i++)
            {
                Primitive($"Market_Stall_{i:00}", PrimitiveType.Cube, parent, kiosks[i], new Vector3(2.2f, 1.15f, 1.3f), metal, true);
                Primitive($"Market_StallCap_{i:00}", PrimitiveType.Cube, parent, kiosks[i] + Vector3.up * 0.9f, new Vector3(2.55f, 0.12f, 1.55f), copper, false);
            }

            Primitive("Market_CentralRelay", PrimitiveType.Cylinder, parent, new Vector3(2.8f, 0.1f, -27.5f), new Vector3(1.15f, 0.45f, 1.15f), metal, true);
            CreateLine("Market_Relay_Cyan", parent, new Vector3(-8.5f, -0.26f, -29f), new Vector3(8.5f, -0.26f, -29f), 0.030f, cyan);
            CreateLine("Market_Relay_Green", parent, new Vector3(0f, -0.25f, -34.8f), new Vector3(0f, -0.25f, -22.4f), 0.030f, viridian);
            PointLight("Market_Light", parent, new Vector3(0f, 3.0f, -29f), new Color(0.10f, 0.38f, 0.58f), 1.7f, 10f);
        }

        private static void BuildMaintenanceLoop(Transform parent, Material basalt, Material metal, Material copper, Material viridian)
        {
            // East-side loop connects the Null Market back to the Memory Forge.
            Primitive("Maintenance_EastRun", PrimitiveType.Cube, parent, new Vector3(9.0f, -0.55f, -42.0f), new Vector3(5.0f, 0.50f, 24f), basalt, true);
            Primitive("Maintenance_SouthRun", PrimitiveType.Cube, parent, new Vector3(6.8f, -0.55f, -54.7f), new Vector3(9.4f, 0.50f, 4.0f), basalt, true);
            Primitive("Maintenance_OuterWall", PrimitiveType.Cube, parent, new Vector3(11.75f, 1.05f, -42f), new Vector3(0.55f, 3.0f, 26f), metal, true);

            for (int i = 0; i < 5; i++)
            {
                float z = -51f + i * 5.0f;
                Primitive($"Maintenance_Riser_{i:00}", PrimitiveType.Cylinder, parent,
                    new Vector3(10.7f, 0.45f, z), new Vector3(0.38f, 1.8f, 0.38f), copper, false);
            }
            CreateLine("Maintenance_Conduit", parent, new Vector3(8.4f, -0.25f, -53.5f), new Vector3(8.4f, -0.25f, -31.0f), 0.032f, viridian);
        }

        private static void BuildCathedralApproach(Transform parent, Material basalt, Material obsidian, Material metal, Material copper, Material cyan, Material viridian)
        {
            Primitive("ProtocolWalk_Floor", PrimitiveType.Cube, parent, new Vector3(0f, -0.55f, -18.0f), new Vector3(10f, 0.50f, 9.0f), obsidian, true);
            Primitive("Cathedral_Floor", PrimitiveType.Cube, parent, new Vector3(0f, -0.55f, -9.0f), new Vector3(15f, 0.50f, 11f), basalt, true);
            Primitive("Cathedral_Wall_L", PrimitiveType.Cube, parent, new Vector3(-7.75f, 2.2f, -9f), new Vector3(0.60f, 5.2f, 11f), obsidian, true);
            Primitive("Cathedral_Wall_R", PrimitiveType.Cube, parent, new Vector3(7.75f, 2.2f, -9f), new Vector3(0.60f, 5.2f, 11f), obsidian, true);

            for (int i = 0; i < 4; i++)
            {
                float z = -13.5f + i * 3.0f;
                Primitive($"Cathedral_Pier_L_{i:00}", PrimitiveType.Cube, parent, new Vector3(-6.6f, 1.7f, z), new Vector3(0.8f, 4.3f, 0.8f), metal, true);
                Primitive($"Cathedral_Pier_R_{i:00}", PrimitiveType.Cube, parent, new Vector3(6.6f, 1.7f, z), new Vector3(0.8f, 4.3f, 0.8f), metal, true);
                Primitive($"Cathedral_Copper_L_{i:00}", PrimitiveType.Cube, parent, new Vector3(-6.6f, 3.0f, z), new Vector3(0.15f, 1.3f, 0.15f), copper, false);
                Primitive($"Cathedral_Copper_R_{i:00}", PrimitiveType.Cube, parent, new Vector3(6.6f, 3.0f, z), new Vector3(0.15f, 1.3f, 0.15f), copper, false);
            }
            CreateLine("Cathedral_SightConduit", parent, new Vector3(-1.15f, -0.25f, -21.5f), new Vector3(-1.15f, -0.25f, -3.6f), 0.034f, cyan);
            CreateLine("Cathedral_GuardConduit", parent, new Vector3(1.15f, -0.25f, -21.5f), new Vector3(1.15f, -0.25f, -3.6f), 0.034f, viridian);
            PointLight("Cathedral_Light", parent, new Vector3(0f, 4.2f, -8.0f), new Color(0.19f, 0.28f, 0.55f), 1.8f, 11f);
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
            float poise,
            float scale)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = position;

            CapsuleCollider collider = root.AddComponent<CapsuleCollider>();
            collider.radius = 0.42f * scale;
            collider.height = 1.8f * scale;
            collider.center = Vector3.up * 0.65f * scale;

            Rigidbody body = root.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            PoiseSystem enemyPoise = root.AddComponent<PoiseSystem>();
            SetFloat(enemyPoise, "maxPoise", poise);
            CombatantVitals vitals = root.AddComponent<CombatantVitals>();
            SetEnum(vitals, "team", (int)CombatTeam.Enemy);
            SetFloat(vitals, "maxHealth", health);
            SetRef(vitals, "poise", enemyPoise);
            SetRef(vitals, "body", body);

            GameObject visuals = new GameObject("Visuals");
            visuals.transform.SetParent(root.transform, false);
            GameObject torso = PrimitiveLocal("Body", PrimitiveType.Capsule, visuals.transform,
                Vector3.up * 0.65f * scale, new Vector3(0.72f, 0.88f, 0.72f) * scale, bodyMaterial, false);
            DestroyCollider(torso);
            GameObject core = PrimitiveLocal("Core", PrimitiveType.Sphere, visuals.transform,
                Vector3.up * 1.10f * scale, Vector3.one * 0.30f * scale, coreMaterial, false);
            DestroyCollider(core);
            GameObject ring = CreateLocalRing("TelegraphRing", visuals.transform, 0.82f * scale, coreMaterial);
            ring.transform.localPosition = Vector3.up * 0.05f;

            Light coreLight = core.AddComponent<Light>();
            coreLight.type = LightType.Point;
            coreLight.color = archetype == JourneyEnemyArchetype.NullSentry
                ? new Color(0.92f, 0.10f, 0.30f)
                : new Color(0.98f, 0.24f, 0.08f);
            coreLight.range = 3.2f * scale;
            coreLight.intensity = archetype == JourneyEnemyArchetype.ChromePenitent ? 1.6f : 1.25f;
            coreLight.shadows = LightShadows.None;

            Transform origin = MarkerLocal("ProjectileOrigin", root.transform, new Vector3(0f, 1.22f * scale, 0.48f * scale));
            JourneyEnemyController controller = root.AddComponent<JourneyEnemyController>();
            controller.ConfigureRuntime(archetype, player, playerVitals, playerMotor, defense, projectile, origin, playerFlux);
            controller.ConfigureCheckpointLifecycle(true);
            controller.Disarm();

            JourneyEnemyPresentation presentation = root.AddComponent<JourneyEnemyPresentation>();
            presentation.ConfigureRuntime(controller, visuals.transform, core.transform, ring.transform, core.GetComponent<Renderer>(), coreLight);
            return controller;
        }

        private static FracturedEchoNode CreateWorldEcho(
            GameObject prefab,
            Transform parent,
            Transform anchor,
            Transform player,
            FluxMeter playerFlux,
            float phase,
            Material fallbackMaterial)
        {
            GameObject instance = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
            if (instance == null) instance = UnityEngine.Object.Instantiate(prefab, parent);
            instance.name = "Market_FracturedEcho";
            FracturedEchoNode echo = instance.GetComponent<FracturedEchoNode>();
            if (echo == null) throw new InvalidOperationException("Generated FracturedEcho prefab has no FracturedEchoNode.");

            Renderer renderer = instance.GetComponentInChildren<Renderer>(true);
            if (renderer != null && renderer.sharedMaterial == null) renderer.sharedMaterial = fallbackMaterial;
            echo.ConfigureWorldEcho(anchor, player, playerFlux, phase, 1.15f);
            echo.SetExternalPause(true);
            return echo;
        }

        private static JourneyGate CreateGate(
            string name,
            Transform parent,
            Vector3 localPosition,
            float width,
            Material frame,
            Material signal,
            Vector3 openOffset)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = localPosition;

            BoxCollider blocker = root.AddComponent<BoxCollider>();
            blocker.center = new Vector3(0f, 1.45f, 0f);
            blocker.size = new Vector3(width, 3.2f, 0.55f);

            GameObject visuals = new GameObject("Visuals");
            visuals.transform.SetParent(root.transform, false);
            PrimitiveLocal("Seal", PrimitiveType.Cube, visuals.transform, new Vector3(0f, 1.45f, 0f), new Vector3(width, 3.0f, 0.24f), signal, false);
            PrimitiveLocal("FrameL", PrimitiveType.Cube, visuals.transform, new Vector3(-width * 0.52f, 1.45f, 0f), new Vector3(0.28f, 3.4f, 0.42f), frame, false);
            PrimitiveLocal("FrameR", PrimitiveType.Cube, visuals.transform, new Vector3(width * 0.52f, 1.45f, 0f), new Vector3(0.28f, 3.4f, 0.42f), frame, false);

            JourneyGate gate = root.AddComponent<JourneyGate>();
            gate.ConfigureRuntime(visuals.transform, new Collider[] { blocker });
            SetVector(gate, "openLocalOffset", openOffset);
            return gate;
        }

        private static Transform Marker(string name, Transform parent, Vector3 localPosition)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            return go.transform;
        }

        private static Transform MarkerLocal(string name, Transform parent, Vector3 localPosition)
            => Marker(name, parent, localPosition);

        private static GameObject Primitive(
            string name,
            PrimitiveType type,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            Material material,
            bool collider)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;
            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = material;
            if (!collider) DestroyCollider(go);
            return go;
        }

        private static GameObject PrimitiveLocal(
            string name,
            PrimitiveType type,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            Material material,
            bool collider)
            => Primitive(name, type, parent, localPosition, localScale, material, collider);

        private static void DestroyCollider(GameObject go)
        {
            Collider c = go != null ? go.GetComponent<Collider>() : null;
            if (c != null) UnityEngine.Object.DestroyImmediate(c);
        }

        private static GameObject CreateLocalRing(string name, Transform parent, float radius, Material material)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            LineRenderer line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = 40;
            line.widthMultiplier = 0.035f;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            for (int i = 0; i < line.positionCount; i++)
            {
                float angle = i / (float)line.positionCount * Mathf.PI * 2f;
                line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
            }
            return go;
        }

        private static void CreateLine(string name, Transform parent, Vector3 localStart, Vector3 localEnd, float width, Material material)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            LineRenderer line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.useWorldSpace = false;
            line.positionCount = 2;
            line.SetPosition(0, localStart);
            line.SetPosition(1, localEnd);
            line.widthMultiplier = width;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
        }

        private static void PointLight(string name, Transform parent, Vector3 localPosition, Color color, float intensity, float range)
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

        private static Material EnsureLit(string name, Color color, float metallic, float smoothness)
        {
            string path = GeneratedFolder + "/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetColor("_BaseColor", color);
            material.color = color;
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", Mathf.Clamp01(metallic));
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", Mathf.Clamp01(smoothness));
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material EnsureEmission(string name, Color color, float emission, float metallic, float smoothness)
        {
            Material material = EnsureLit(name, color * 0.42f, metallic, smoothness);
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * Mathf.Max(0f, emission));
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Mindforge/Generated"))
                AssetDatabase.CreateFolder("Assets/Mindforge", "Generated");
            if (!AssetDatabase.IsValidFolder(GeneratedFolder))
                AssetDatabase.CreateFolder("Assets/Mindforge/Generated", "NullWardV1");
        }

        private static void SetRef(UnityEngine.Object target, string property, UnityEngine.Object value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty field = serialized.FindProperty(property);
            if (field == null) throw new InvalidOperationException($"Missing serialized field {target.GetType().Name}.{property}");
            field.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFloat(UnityEngine.Object target, string property, float value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty field = serialized.FindProperty(property);
            if (field == null) throw new InvalidOperationException($"Missing serialized field {target.GetType().Name}.{property}");
            field.floatValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetEnum(UnityEngine.Object target, string property, int value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty field = serialized.FindProperty(property);
            if (field == null) throw new InvalidOperationException($"Missing serialized field {target.GetType().Name}.{property}");
            field.enumValueIndex = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetVector(UnityEngine.Object target, string property, Vector3 value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty field = serialized.FindProperty(property);
            if (field == null) throw new InvalidOperationException($"Missing serialized field {target.GetType().Name}.{property}");
            field.vector3Value = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
