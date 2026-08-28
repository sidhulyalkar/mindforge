#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Mindforge.Qualification;

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
            Debug.Log("[Mindforge:Showcase] Game view focused. WASD moves; mouse/arrows orbit camera; T locks target; Space dodges.");
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
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                "[Mindforge:Showcase] Cinematic scene ready. Arena V3 is the final environment layer: " +
                "midnight/indigo ritual floor, structured pillars, cyan/teal channels, copper trim, " +
                "ruins, braziers and reflection lighting are authored before Play Mode. " +
                "Runtime installs the third-person behind-Guardian camera and conventional target lock.");
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
