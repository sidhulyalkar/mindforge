#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Mindforge.Telemetry;
using Mindforge.World;

namespace Mindforge.Editor
{
    /// <summary>
    /// Installs the semantic large-game foundation over existing concrete gameplay systems.
    /// No authority is moved out of encounter/movement/combat/checkpoint systems here.
    /// </summary>
    public static class GameFoundationV1Builder
    {
        public const string RootName = "Mindforge_GameFoundation_V1";
        public const string Revision = "GAME_FOUNDATION_V1";

        [MenuItem("Mindforge/Showcase/Apply Game Foundation V1", priority = 32)]
        public static void ApplyOpenScene()
        {
            HackathonPlaythroughDirectorV1 playthrough = UnityEngine.Object.FindObjectOfType<HackathonPlaythroughDirectorV1>(true);
            ArenaMenagerieDirector menagerie = UnityEngine.Object.FindObjectOfType<ArenaMenagerieDirector>(true);
            NullWardEncounterDirector nullWard = UnityEngine.Object.FindObjectOfType<NullWardEncounterDirector>(true);
            MemoryForgeCheckpoint checkpoint = UnityEngine.Object.FindObjectOfType<MemoryForgeCheckpoint>(true);
            UdpGameMarkerSender markers = UnityEngine.Object.FindObjectOfType<UdpGameMarkerSender>(true);

            if (playthrough == null || menagerie == null || nullWard == null || checkpoint == null)
                throw new InvalidOperationException("Game Foundation V1 requires the Hackathon playthrough, Menagerie, Null Ward and Memory Forge checkpoint.");

            GameObject previous = EditorSceneLookup.FindIncludingInactive(RootName);
            if (previous != null) UnityEngine.Object.DestroyImmediate(previous);

            GameObject root = new GameObject(RootName);
            WorldSignalBus bus = root.AddComponent<WorldSignalBus>();
            WorldStateLedger ledger = root.AddComponent<WorldStateLedger>();
            WorldQuestRuntime quests = root.AddComponent<WorldQuestRuntime>();
            HackathonWorldSemanticBridgeV1 bridge = root.AddComponent<HackathonWorldSemanticBridgeV1>();
            WorldSignalTelemetryAdapter telemetry = root.AddComponent<WorldSignalTelemetryAdapter>();

            ledger.ConfigureRuntime(bus);
            quests.ConfigureRuntime(ledger, bus, BuildQuestDefinitions());
            bridge.ConfigureRuntime(bus, ledger, playthrough, menagerie, nullWard, checkpoint);
            telemetry.ConfigureRuntime(bus, markers);

            EditorUtility.SetDirty(root);
            EditorUtility.SetDirty(bus);
            EditorUtility.SetDirty(ledger);
            EditorUtility.SetDirty(quests);
            EditorUtility.SetDirty(bridge);
            EditorUtility.SetDirty(telemetry);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[Mindforge:FoundationV1] Semantic foundation installed: WorldSignalBus -> typed WorldStateLedger -> read-only WorldQuestRuntime -> " +
                "passive observer telemetry. Existing Menagerie, Null Ward, checkpoint, Guardian and BCI systems remain concrete authorities.");
        }

        private static WorldQuestDefinition[] BuildQuestDefinitions()
        {
            return new[]
            {
                new WorldQuestDefinition
                {
                    id = "hackathon.combat_exam",
                    title = "Survive the Menagerie Crucible",
                    conditions = new[]
                    {
                        Bool("encounter.menagerie.complete"),
                    },
                },
                new WorldQuestDefinition
                {
                    id = "hackathon.reconnect_null_ward",
                    title = "Reconnect the Null Ward",
                    conditions = new[]
                    {
                        Bool("region.causeway.entered"),
                        Bool("encounter.menagerie.complete"),
                        Bool("world.null_ward.complete"),
                    },
                },
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
    }
}
#endif
