#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Mindforge.Qualification;
using Mindforge.EditorTools;

namespace Mindforge.Editor
{
    /// <summary>
    /// Human-facing entry points for visual/gameplay validation. The Build + Play
    /// command is intentionally controller-only; calibrated BCI validation remains a
    /// separate workflow with its own evidence requirements.
    /// </summary>
    public static class ShowcaseEditorMenu
    {
        [MenuItem("Mindforge/Showcase/Build + Play Cinematic Showcase", priority = 1)]
        public static void BuildAndPlay()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[Mindforge:Showcase] Stop Play Mode before rebuilding the showcase.");
                return;
            }

            BuildScene();
            EditorPrefs.SetBool(ShowcasePreviewBootstrap.EditorPreferenceKey, true);
            Selection.activeGameObject = GameObject.Find("Guardian");

            EditorApplication.playModeStateChanged -= FocusGameViewWhenPlayStarts;
            EditorApplication.playModeStateChanged += FocusGameViewWhenPlayStarts;
            EditorApplication.delayCall += () =>
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode)
                    EditorApplication.isPlaying = true;
            };
        }

        private static void FocusGameViewWhenPlayStarts(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode) return;
            EditorApplication.playModeStateChanged -= FocusGameViewWhenPlayStarts;
            EditorApplication.delayCall += FocusGameView;
        }

        private static void FocusGameView()
        {
            Type gameViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
            if (gameViewType == null) return;
            EditorWindow gameView = EditorWindow.GetWindow(gameViewType, false, "Game", true);
            gameView?.Focus();
            Debug.Log(
                "[Mindforge:Showcase] Game view focused. WASD moves; mouse/arrows orbit; Space jumps twice and holds hover; " +
                "Shift/RMB dodge-rolls on ground and air-dashes aloft; T locks; F/LMB swings/parries with the Aetherblade.");
        }

        [MenuItem("Mindforge/Showcase/Build + Play Combat Showcase", priority = 2)]
        public static void BuildAndPlayLegacyAlias() => BuildAndPlay();

        [MenuItem("Mindforge/Showcase/Rebuild Cinematic Showcase Scene", priority = 3)]
        public static void BuildScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[Mindforge:Showcase] Stop Play Mode before rebuilding the showcase.");
                return;
            }

            CinematicFidelityConfigurator.Configure();
            CinematicMaterialAuthoring.EnsureAuthored();
            CompetitionSceneAssembler.BuildCompetitionScene();

            GameObject arena = EditorSceneLookup.FindIncludingInactive("Fractured_Signal_Arena");
            if (arena == null)
                throw new UnityEditor.Build.BuildFailedException(
                    "Mindforge showcase assembly did not create Fractured_Signal_Arena.");

            bool arenaWasActive = arena.activeSelf;
            try
            {
                if (!arenaWasActive) arena.SetActive(true);
                ShowcaseSceneDecorator.DecorateOpenScene();
                CinematicSceneDetailer.EnhanceOpenScene();

                ArenaEnvironmentV3Builder.BuildOpenScene();
                NullWardSceneBuilder.BuildOpenScene();

                // World topology comes before population/presentation. V1 owns the continuous
                // basin and perimeter; V2 composes district-specific collision-backed landmarks
                // and vertical routes from that safe shell; later passes populate and dress it.
                GroundedWorldV1Builder.ApplyOpenScene();
                GroundedWorldCompositionV2Builder.ApplyOpenScene();
                GroundedWorldTuningV1.ApplyOpenScene();
                NullWardArenaEcosystemBuilder.ApplyOpenScene();
                NullWardEnemyColliderProfileBuilder.ApplyOpenScene();
                NullWardEnemySilhouetteV3Builder.ApplyOpenScene();
                NullWardVisualInfrastructureBuilder.ApplyOpenScene();
                NullWardArenaSetDressingV3Builder.ApplyOpenScene();
                NullWardTraversalPlayabilityBuilder.ApplyOpenScene();
            }
            finally
            {
                if (arena != null && !arenaWasActive)
                {
                    arena.SetActive(false);
                    EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                    EditorSceneManager.SaveOpenScenes();
                }
            }

            CompetitionGateValidator.ValidateAndWrite(false);
            PresentationBudgetAudit.Run();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                "[Mindforge:Showcase] Grounded World V2 ready inside a continuous collision-backed basin with a tall enclosing wall. " +
                "The district-specific Forge keep, Causeway rib gallery, Market court, Fracture tower, Cathedral ascent and Arena ring " +
                "create distinct vertical silhouettes, landing pockets and shortcut routes. No reachable route intentionally exposes " +
                "the void. The combat core is energy-blade + endurance dodge roll + jump/double-jump/hover/air-dash; " +
                "Pulse fire and the physical shield are retired from the normal control surface.");
            Debug.Log(
                "[Mindforge:Showcase] Composition retained: Memory Forge → Synapse Causeway → Null Market → Fracture Court → Cathedral. " +
                "Five ordinary enemy roles remain in the encounter grammar; the persistent shortcut, geometric intent telegraphs, " +
                "stable VEP targets and Layered near/mid/far set dressing remain downstream of deterministic gameplay authority.");
        }

        [MenuItem("Mindforge/Showcase/Rebuild Showcase Scene", priority = 4)]
        public static void BuildSceneLegacyAlias() => BuildScene();

        [MenuItem("Mindforge/Showcase/Open Showcase Scene", priority = 5)]
        public static void OpenScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            EditorSceneManager.OpenScene(CompetitionSceneAssembler.ScenePath, OpenSceneMode.Single);
            Selection.activeGameObject = GameObject.Find("Guardian");
        }
    }
}
#endif