#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Mindforge.Presentation;
using Mindforge.Telemetry;
using Mindforge.World;

namespace Mindforge.Editor
{
    /// <summary>
    /// Installs the semantic large-game foundation over existing concrete gameplay systems.
    /// It adds ordered quests, isolated progression, story discoveries, encounter metadata
    /// and passive competitive observation without moving authority out of existing gameplay.
    /// </summary>
    public static class GameFoundationV1Builder
    {
        public const string RootName = "Mindforge_GameFoundation_V1";
        public const string Revision = "GAME_FOUNDATION_V1";

        [MenuItem("Mindforge/Legacy/Showcase/Apply Game Foundation V1", priority = 32)]
        public static void ApplyOpenScene()
        {
            GameObject guardian = EditorSceneLookup.FindIncludingInactive("Guardian");
            HackathonPlaythroughDirectorV1 playthrough = UnityEngine.Object.FindObjectOfType<HackathonPlaythroughDirectorV1>(true);
            ArenaMenagerieDirector menagerie = UnityEngine.Object.FindObjectOfType<ArenaMenagerieDirector>(true);
            NullWardEncounterDirector nullWard = UnityEngine.Object.FindObjectOfType<NullWardEncounterDirector>(true);
            MemoryForgeCheckpoint checkpoint = UnityEngine.Object.FindObjectOfType<MemoryForgeCheckpoint>(true);
            UdpGameMarkerSender markers = UnityEngine.Object.FindObjectOfType<UdpGameMarkerSender>(true);

            if (guardian == null || playthrough == null || menagerie == null || nullWard == null || checkpoint == null)
                throw new InvalidOperationException("Game Foundation V1 requires Guardian, Hackathon playthrough, Menagerie, Null Ward and Memory Forge checkpoint.");

            GameObject previous = EditorSceneLookup.FindIncludingInactive(RootName);
            if (previous != null) UnityEngine.Object.DestroyImmediate(previous);

            GameObject root = new GameObject(RootName);
            root.SetActive(false);

            WorldSignalBus bus = root.AddComponent<WorldSignalBus>();
            WorldStateLedger ledger = root.AddComponent<WorldStateLedger>();
            WorldQuestRuntime quests = root.AddComponent<WorldQuestRuntime>();
            PlayerProgressionLedger progression = root.AddComponent<PlayerProgressionLedger>();
            WorldQuestRewardRuntime rewards = root.AddComponent<WorldQuestRewardRuntime>();
            EncounterContractRegistry contracts = root.AddComponent<EncounterContractRegistry>();
            HackathonWorldSemanticBridgeV1 bridge = root.AddComponent<HackathonWorldSemanticBridgeV1>();
            WorldSignalTelemetryAdapter telemetry = root.AddComponent<WorldSignalTelemetryAdapter>();
            CompetitiveRunObserverV1 observer = root.AddComponent<CompetitiveRunObserverV1>();
            GameFoundationHudV1 hud = root.AddComponent<GameFoundationHudV1>();

            ledger.ConfigureRuntime(bus);
            progression.ConfigureRuntime(bus);
            quests.ConfigureRuntime(ledger, bus, BuildQuestDefinitions());
            rewards.ConfigureRuntime(quests, progression, bus);
            contracts.ConfigureRuntime(BuildEncounterContracts(nullWard));
            bridge.ConfigureRuntime(bus, ledger, playthrough, menagerie, nullWard, checkpoint);
            telemetry.ConfigureRuntime(bus, markers);
            observer.ConfigureRuntime(bus);
            hud.ConfigureRuntime(quests, progression, bus, observer);
            BuildStoryBeacons(root.transform, guardian.transform, ledger, bus);

            root.SetActive(true);

            EditorUtility.SetDirty(root);
            EditorUtility.SetDirty(bus);
            EditorUtility.SetDirty(ledger);
            EditorUtility.SetDirty(quests);
            EditorUtility.SetDirty(progression);
            EditorUtility.SetDirty(rewards);
            EditorUtility.SetDirty(contracts);
            EditorUtility.SetDirty(bridge);
            EditorUtility.SetDirty(telemetry);
            EditorUtility.SetDirty(observer);
            EditorUtility.SetDirty(hud);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[Mindforge:FoundationV1] Full journey foundation installed: typed semantic state -> ordered prerequisite quests -> idempotent progression rewards -> " +
                "six durable Aetheria story discoveries -> encounter contracts -> passive run splits/observer telemetry. Menagerie, Null Ward, checkpoint, " +
                "Guardian combat/movement and BCI systems remain the only concrete authorities for their domains.");
        }

        private static WorldQuestDefinition[] BuildQuestDefinitions()
        {
            return new[]
            {
                new WorldQuestDefinition
                {
                    id = "journey.read_aetheria",
                    title = "Read the Fractured City",
                    description = "Cross the first districts and learn Aetheria's visual grammar before the combat exam.",
                    sort_order = 10,
                    steps = new[]
                    {
                        Step("causeway", "Cross the Neon Causeway", Bool("region.causeway.entered")),
                        Step("broken_momentum", "Enter the Market of Broken Momentum", Bool("region.brokenmomentum.entered")),
                        Step("ruined_choir", "Reach the Choir of Ruined Towers", Bool("region.ruinedchoir.entered")),
                    },
                    rewards = new[]
                    {
                        Resonance(10),
                        Unlock("codex.aetheria_regions"),
                    },
                },
                new WorldQuestDefinition
                {
                    id = "journey.menagerie_exam",
                    title = "The Menagerie Exam",
                    description = "Enter the Crucible and survive the authored 3 / 4 / 3 ten-enemy combat examination.",
                    sort_order = 20,
                    prerequisite_ids = new[] { "journey.read_aetheria" },
                    steps = new[]
                    {
                        Step("gravitas", "Cross the Hall of Excessive Gravitas", Bool("region.gravitas.entered")),
                        Step("crucible", "Enter the Menagerie Crucible", Bool("region.crucible.entered")),
                        Step("wave_one", "Break the first formation", IntAtLeast("encounter.menagerie.waves_cleared", 1)),
                        Step("wave_two", "Survive the mixed second formation", IntAtLeast("encounter.menagerie.waves_cleared", 2)),
                        Step("exam_complete", "Defeat the final formation", Bool("encounter.menagerie.complete")),
                    },
                    rewards = new[]
                    {
                        Resonance(30),
                        Mastery(1),
                        Unlock("challenge.menagerie_replay"),
                    },
                },
                new WorldQuestDefinition
                {
                    id = "journey.reconnect_null_ward",
                    title = "Reconnect the Null Ward",
                    description = "Open the Protocol Veil, cross the cathedral threshold and silence Lord Malatract.",
                    sort_order = 30,
                    prerequisite_ids = new[] { "journey.menagerie_exam" },
                    steps = new[]
                    {
                        Step("protocol", "Open the Protocol Veil", Bool("world.null_ward.protocol_open")),
                        Step("malatract", "Confront Lord Malatract", Bool("boss.malatract.started")),
                        Step("reconnect", "Silence the fractured signal", Bool("world.null_ward.complete")),
                    },
                    rewards = new[]
                    {
                        Resonance(60),
                        Mastery(2),
                        Unlock("region.aetheria_frontier"),
                    },
                },
            };
        }

        private static EncounterContract[] BuildEncounterContracts(NullWardEncounterDirector nullWard)
        {
            List<EncounterContract> result = new List<EncounterContract>();
            NullWardEncounterZone[] zones = nullWard != null ? nullWard.Zones : null;
            if (zones != null)
            {
                for (int i = 0; i < zones.Length; i++)
                {
                    NullWardEncounterZone zone = zones[i];
                    if (zone == null) continue;
                    result.Add(new EncounterContract
                    {
                        id = "null_ward." + Normalize(zone.id, "zone_" + i),
                        title = string.IsNullOrWhiteSpace(zone.title) ? "Null Ward Encounter" : zone.title,
                        kind = EncounterContractKind.Teaching,
                        authority_component = "NullWardEncounterDirector",
                        enemy_count = zone.enemies != null ? zone.enemies.Length : 0,
                        wave_count = 1,
                        recommended_mastery = 0,
                        supports_replay = true,
                        competitive_candidate = false,
                        ranked_eligible = false,
                        neural_contract = "optional_transform_only",
                    });
                }
            }

            result.Add(new EncounterContract
            {
                id = "menagerie.crucible",
                title = "Menagerie Crucible",
                kind = EncounterContractKind.Arena,
                authority_component = "ArenaMenagerieDirector + JourneyEnemyController",
                enemy_count = 10,
                wave_count = 3,
                recommended_mastery = 0,
                supports_replay = true,
                competitive_candidate = true,
                ranked_eligible = false,
                neural_contract = "conventional_actions; optional_transform_only",
            });
            result.Add(new EncounterContract
            {
                id = "boss.lord_malatract",
                title = "Lord Malatract",
                kind = EncounterContractKind.Boss,
                authority_component = "FracturedSignalDirector + FracturedSignalMeleeDirector",
                enemy_count = 1,
                wave_count = 1,
                recommended_mastery = 1,
                supports_replay = true,
                competitive_candidate = true,
                ranked_eligible = false,
                neural_contract = "conventional_actions; accepted_bci_transform_only",
            });
            return result.ToArray();
        }

        private static void BuildStoryBeacons(
            Transform parent,
            Transform guardian,
            WorldStateLedger ledger,
            WorldSignalBus bus)
        {
            Story(parent, guardian, ledger, bus, "prism_bastion", "PRISM BASTION", new Vector3(0f, 1f, -59f),
                "Aetheria did not fall. It kept thinking after its people stopped agreeing on what the city was for.");
            Story(parent, guardian, ledger, bus, "neon_causeway", "NEON CAUSEWAY", new Vector3(0f, 1f, -46f),
                "The conduits still carry intention. Every bridge is a sentence whose final word has been cut away.");
            Story(parent, guardian, ledger, bus, "broken_momentum", "MARKET OF BROKEN MOMENTUM", new Vector3(0f, 1f, -30f),
                "Here, motion became currency. The wealthy bought acceleration. Everyone else learned to dodge.");
            Story(parent, guardian, ledger, bus, "ruined_choir", "CHOIR OF RUINED TOWERS", new Vector3(0f, 1f, -11f),
                "The towers once synchronized the city. Now each broadcasts a different memory of the same disaster.");
            Story(parent, guardian, ledger, bus, "hall_gravitas", "HALL OF EXCESSIVE GRAVITAS", new Vector3(0f, 1f, 4f),
                "Authority was architecture here: tall enough to make obedience feel like weather.");
            Story(parent, guardian, ledger, bus, "menagerie_crucible", "MENAGERIE CRUCIBLE", new Vector3(5f, 1f, 14f),
                "The Crucible does not ask whether you are strong. It asks whether you can read ten kinds of danger without losing your rhythm.");
        }

        private static void Story(
            Transform parent,
            Transform guardian,
            WorldStateLedger ledger,
            WorldSignalBus bus,
            string id,
            string title,
            Vector3 position,
            string line)
        {
            GameObject go = new GameObject("StoryBeacon_" + id);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            WorldStoryBeaconV1 beacon = go.AddComponent<WorldStoryBeaconV1>();
            beacon.ConfigureRuntime(guardian, ledger, bus, id, title, line, 3.8f);
            EditorUtility.SetDirty(go);
            EditorUtility.SetDirty(beacon);
        }

        private static WorldQuestStepDefinition Step(string id, string title, params WorldQuestCondition[] conditions)
        {
            return new WorldQuestStepDefinition
            {
                id = id,
                title = title,
                conditions = conditions ?? Array.Empty<WorldQuestCondition>(),
            };
        }

        private static WorldQuestCondition Bool(string key)
        {
            return new WorldQuestCondition
            {
                state_key = key,
                kind = WorldQuestConditionKind.BoolEquals,
                bool_value = true,
            };
        }

        private static WorldQuestCondition IntAtLeast(string key, int value)
        {
            return new WorldQuestCondition
            {
                state_key = key,
                kind = WorldQuestConditionKind.IntAtLeast,
                int_value = value,
            };
        }

        private static WorldQuestRewardDefinition Resonance(int amount)
            => new WorldQuestRewardDefinition { kind = WorldRewardKind.Resonance, amount = amount };

        private static WorldQuestRewardDefinition Mastery(int amount)
            => new WorldQuestRewardDefinition { kind = WorldRewardKind.Mastery, amount = amount };

        private static WorldQuestRewardDefinition Unlock(string id)
            => new WorldQuestRewardDefinition { kind = WorldRewardKind.Unlock, id = id, amount = 1 };

        private static string Normalize(string value, string fallback)
            => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim().ToLowerInvariant();
    }
}
#endif
