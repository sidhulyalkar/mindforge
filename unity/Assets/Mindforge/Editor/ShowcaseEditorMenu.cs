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
        // These concise summaries are intentionally stable: older regression contracts inspect
        // them to ensure later showcase passes do not silently erase previously qualified scope.
        private const string InheritedShowcaseContract =
            "Five ordinary enemy roles remain in the journey grammar; the Menagerie Crucible adds five specialized variants for a ten-identity roster and a 3/4/3 hackathon encounter. " +
            "Scrap Goblin, Bass Golem and Aero Gargoyle remain story-facing identities over existing roles; Lord Malatract still resolves through the existing Fractured Signal projectile/melee scheduler. " +
            "The persistent shortcut and geometric intent telegraphs remain intact. Layered near/mid/far set dressing remains downstream of deterministic gameplay authority. " +
            "All ten Menagerie enemies receive unique close/mid-distance silhouette detail. V0.7 is presentation-only: topology, E routing, persistence, combat and BCI remain untouched.";

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
                "Shift/RMB evades on foot and boosts while mounted; T locks and mouse wheel cycles targets; F/LMB swings/parries; " +
                "E is the single contextual world action for ride/dismount/reconstruct/open/take/talk/commune/inspect resonance; Q/C/R are advanced skills; " +
                "Tab opens kit + controls + objective + persistent world state.");
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

                GroundedWorldV1Builder.ApplyOpenScene();
                GroundedWorldCompositionV2Builder.ApplyOpenScene();
                GroundedWorldTuningV1.ApplyOpenScene();
                NullWardArenaEcosystemBuilder.ApplyOpenScene();
                ArenaMenagerieV1Builder.ApplyOpenScene();
                ArenaMenagerieColliderV1Builder.ApplyOpenScene();
                NullWardEnemyColliderProfileBuilder.ApplyOpenScene();
                NullWardEnemySilhouetteV3Builder.ApplyOpenScene();
                ArenaMenagerieSilhouetteV1Builder.ApplyOpenScene();
                AetheriaHordeBossV1Builder.ApplyOpenScene();
                AetheriaWorldV1Builder.ApplyOpenScene();
                AetheriaStateOfArtV2Builder.ApplyOpenScene();
                HackathonPlaythroughV1Builder.ApplyOpenScene();
                AetheriaDynamicMountSafetyBuilder.ApplyOpenScene();
                NullWardVisualInfrastructureBuilder.ApplyOpenScene();
                NullWardArenaSetDressingV3Builder.ApplyOpenScene();
                NullWardTraversalPlayabilityBuilder.ApplyOpenScene();

                GameFoundationV1Builder.ApplyOpenScene();
                UxInteractionSaveV05Builder.ApplyOpenScene();
                WorldV06Builder.ApplyOpenScene();
                WorldV07Builder.ApplyOpenScene();

                // V0.8 is an opening-experience recomposition. It runs after world art so it
                // can deliberately replace cramped legacy opening presentation/collision while
                // preserving semantic/persistence/input/neural authorities built above.
                SanctumOnboardingV08Builder.ApplyOpenScene();
                SanctumHeroV08Builder.ApplyOpenScene();
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
            Debug.Log("[Mindforge:Showcase] " + InheritedShowcaseContract);
            Debug.Log(
                "[Mindforge:Showcase] Grounded World V2 ready inside a continuous collision-backed basin with a tall enclosing wall. " +
                "The district-specific Forge keep, Causeway rib gallery, Market court, Fracture tower, Cathedral ascent and Arena ring remain the " +
                "core authored route; the Menagerie Crucible adds a dedicated combat district with distinct vertical silhouettes, landing pockets and " +
                "combat spaces. No reachable route intentionally exposes the void. The combat core is energy-blade + endurance dodge roll + " +
                "jump/double-jump/hover/air-dash; Pulse fire and the physical shield are retired from the normal control surface.");
            Debug.Log(
                "[Mindforge:Showcase] Composition retained: Memory Forge → Synapse Causeway → Null Market → Fracture Court → Cathedral. " +
                "Persistent shortcut, geometric intent telegraphs and stable VEP targets remain intact; V0.8 changes the first route's physical spacing and pacing, not its semantic identity.");
            Debug.Log(
                "[Mindforge:Showcase] Aetheria identity layer ready: Prism Bastion → Neon Causeway → Market of Broken Momentum → " +
                "Choir of Ruined Towers → Hall of Excessive Gravitas → Menagerie Crucible. Two optional Prism hoverbikes use the existing Guardian " +
                "Rigidbody as mounted authority; contextual E requests ride/dismount while mounted F/LMB still routes through the authoritative Aetherblade controller.");
            Debug.Log(
                "[Mindforge:Showcase] Cyber-Mythic Horde remains available beyond onboarding. V0.8 removes low Rift Hollow rushers from the first Causeway encounter " +
                "so the opening roster begins with suspended Sentries and upright/hovering threats rather than floor-crawling pressure.");
            Debug.Log(
                "[Mindforge:Showcase] Aetheria V2 polish retained: the fixed-tick conventional-input tape crosses foot/mount mode; " +
                "mounted camera composition uses physical distance/look-ahead while keeping FOV fixed; kinetic bike motion, procedural audio and " +
                "Malatract phase staging remain read-only presentation consumers.");
            Debug.Log(
                "[Mindforge:Showcase] Hackathon Playthrough systems remain downstream of gameplay authority. V0.8 intentionally gives the first minutes " +
                "more negative space and delays density rather than applying the late-game 3/4/3 combat rhythm immediately.");
            Debug.Log(
                "[Mindforge:Showcase] Game Foundation V1 ready: final authored gameplay publishes typed semantic facts into ordered prerequisite quests; " +
                "idempotent Resonance/Mastery/unlock rewards, six durable story discoveries, encounter contracts and passive run splits sit downstream " +
                "of gameplay authority. Competitive candidates remain explicitly NOT ranked-qualified until Unity/runtime/BCI evidence says otherwise.");
            Debug.Log(
                "[Mindforge:Showcase] UX + Interaction + Save V0.5 contracts retained: one canonical control profile drives gameplay labels; E is one contextual action; " +
                "arrows remain camera-only while mouse wheel cycles locked targets; V5 input tapes record context separately from legacy mount edges.");
            Debug.Log(
                "[Mindforge:Showcase] Persistent World V0.6 ready: profile-v2 is the sole active disk writer; progression, inventory/equipment, reward receipts, " +
                "regions and explicit physical restore adapters share one save. Memory Conduit, loot, shrine and Archivist all reuse contextual E and stable world IDs.");
            Debug.Log(
                "[Mindforge:Showcase] Neural-Gothic World V0.7 remains the scalable background art system. Its solved-cell detail stays presentation-only and " +
                "V0.8 selectively supersedes the dark opening hero props with a bright sanctum palette.");
            Debug.Log(
                "[Mindforge:Showcase] Sanctum Onboarding V0.8 ready: ~30m-wide initiation hall, 12m threshold, broad terrace/courts, gardens/water, distant cathedral-city reveal, " +
                "bright Memory Forge altar on the existing checkpoint, three resonance preview stations, two-station controller fallback, Python-accepted calibration shortcut, " +
                "participant-specific derived frequency ranking, no Causeway Rift Hollows, and hostile projectile readability scaling from 60% during onboarding to 82% after release. " +
                "Controller preview is never neural evidence; profile-v2 restores opening phase/threshold state.");
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
