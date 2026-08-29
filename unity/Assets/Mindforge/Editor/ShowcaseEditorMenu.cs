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

            // Starting Play Mode from a menu can leave keyboard focus on the Scene,
            // Console or Inspector window. Register before entering play so the Game
            // view explicitly receives third-person keyboard/mouse input on laptops.
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
            Debug.Log("[Mindforge:Showcase] Game view focused. WASD moves; mouse/arrows orbit; Space jumps twice and holds hover while descending; Shift dashes on ground or in air; T locks; F/LMB swords; RMB/E guards; X/MMB fires Pulse.");
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

                // Keep the qualified boss arena at its existing world-space origin.
                // The Null Ward extends backward along -Z and rejoins the arena at the
                // Signal Cathedral threshold without translating Arena V3 rune geometry.
                ArenaEnvironmentV3Builder.BuildOpenScene();
                NullWardSceneBuilder.BuildOpenScene();

                // Gameplay population comes before visual layers so every newly-authored
                // enemy receives honest collision, silhouette and intent presentation.
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
                "[Mindforge:Showcase] Full Null Ward arena ecosystem ready. The controller-only path now runs from " +
                "the Memory Forge through a populated Synapse Causeway, mixed-pressure Null Market, the eastern " +
                "maintenance loop with its persistent shortcut, and Fracture Court before the Signal Cathedral " +
                "and existing Fractured Signal boss arena. Five ordinary enemy archetypes plus the elevated " +
                "Aether Needle variant use distinct collider-free silhouettes, honest root hit volumes and " +
                "geometric intent telegraphs. Layered near/mid/far set dressing extends the Ward and boss-arena " +
                "skyline without adding gameplay colliders. A presentation-budget report is emitted before Play " +
                "Mode; controller-only runtime performance evidence is emitted during play. Runtime retains the " +
                "tighter third-person camera, conventional multi-target lock, double jump, hold-Space hover, " +
                "ground/air Shift dash, pooled effects and stable Wisp gaze anchors.");
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