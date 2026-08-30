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
                "Shift/RMB evades on foot and boosts while mounted; T locks and mouse wheel cycles targets; F/LMB swings/parries; " +
                "E is the single contextual world action for ride/dismount/reconstruct/open/take/talk/commune; Q/C/R are advanced skills; " +
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

                // World topology comes before population/presentation. V1 owns the continuous
                // basin and perimeter; V2 composes district-specific collision-backed landmarks
                // and vertical routes from that safe shell; later passes populate and dress it.
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

                // Semantic systems bind only after the final authored gameplay/world graph exists.
                GameFoundationV1Builder.ApplyOpenScene();

                // Player-facing control and one contextual E router bind against the final scene.
                UxInteractionSaveV05Builder.ApplyOpenScene();

                // V0.6 replaces the active V0.5 disk writer with profile-v2, then adds only
                // restorable persistent physical truth plus bounded procedural world expansion.
                WorldV06Builder.ApplyOpenScene();

                // V0.7 is presentation-only: it decorates solved cells and adds long-range
                // silhouette anchors after topology, interaction and persistence are complete.
                WorldV07Builder.ApplyOpenScene();

                // Readability polish remains presentation-only: shared openings receive pointed
                // crowns, and only V0.7 decorative lights are scaled down in calibrated BCI mode.
                WorldV07ReadabilityPolishBuilder.ApplyOpenScene();
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
                "The district-specific Forge keep, Causeway rib gallery, Market court, Fracture tower, Cathedral ascent and Arena ring remain the " +
                "core authored route; the Menagerie Crucible adds a dedicated combat district with distinct vertical silhouettes, landing pockets and " +
                "combat spaces. No reachable route intentionally exposes the void. The combat core is energy-blade + endurance dodge roll + " +
                "jump/double-jump/hover/air-dash; Pulse fire and the physical shield are retired from the normal control surface.");
            Debug.Log(
                "[Mindforge:Showcase] Composition retained: Memory Forge → Synapse Causeway → Null Market → Fracture Court → Cathedral. " +
                "Five ordinary enemy roles remain in the journey grammar; the Menagerie Crucible adds five specialized variants for a ten-identity " +
                "3/4/3 hackathon encounter. The persistent shortcut, geometric intent telegraphs and stable VEP targets remain intact. " +
                "Layered near/mid/far set dressing remains downstream of deterministic gameplay authority.");
            Debug.Log(
                "[Mindforge:Showcase] Aetheria identity layer ready: Prism Bastion → Neon Causeway → Market of Broken Momentum → " +
                "Choir of Ruined Towers → Hall of Excessive Gravitas → Menagerie Crucible. Two optional Prism hoverbikes use the existing Guardian " +
                "Rigidbody as mounted authority; contextual E requests ride/dismount while mounted F/LMB still routes through the authoritative Aetherblade controller.");
            Debug.Log(
                "[Mindforge:Showcase] Cyber-Mythic Horde ready: Scrap Goblin, Bass Golem and Aero Gargoyle are story-facing identities over existing " +
                "Menagerie roles; Stalker and Gargoyle committed advances resolve through JourneyEnemyController. Lord Malatract is a serious presentation " +
                "layer over the existing Fractured Signal projectile/melee scheduler, not a second boss authority.");
            Debug.Log(
                "[Mindforge:Showcase] Aetheria V2 polish retained: the fixed-tick conventional-input tape crosses foot/mount mode; " +
                "mounted camera composition uses physical distance/look-ahead while keeping FOV fixed; kinetic bike motion, procedural audio and " +
                "Malatract phase staging remain read-only presentation consumers.");
            Debug.Log(
                "[Mindforge:Showcase] Hackathon Playthrough V1 ready: every major Aetheria district receives a denser near/mid/far visual layer; " +
                "all ten Menagerie enemies receive unique close/mid-distance silhouette detail; the Guardian receives Prism Squire V2 armor; " +
                "the Crucible is restaged as 3/4/3 with wave beacons and victory crown; monotonic playthrough state is exposed for future quests/story.");
            Debug.Log(
                "[Mindforge:Showcase] Game Foundation V1 ready: final authored gameplay publishes typed semantic facts into ordered prerequisite quests; " +
                "idempotent Resonance/Mastery/unlock rewards, six durable story discoveries, encounter contracts and passive run splits sit downstream " +
                "of gameplay authority. Competitive candidates remain explicitly NOT ranked-qualified until Unity/runtime/BCI evidence says otherwise.");
            Debug.Log(
                "[Mindforge:Showcase] UX + Interaction + Save V0.5 contracts retained: one canonical control profile drives gameplay labels; E is one contextual action; " +
                "arrows remain camera-only while mouse wheel cycles locked targets; V5 input tapes record context separately from legacy mount edges.");
            Debug.Log(
                "[Mindforge:Showcase] Persistent World V0.6 ready: profile-v2 is the sole active disk writer; progression, inventory/equipment, reward receipts, " +
                "regions and explicit physical restore adapters share one save. Memory Conduit, loot, shrine and Archivist all reuse contextual E and stable world IDs. " +
                "The Neural Cloister is deterministically generated from a small socket/height grammar inside the existing collision basin; authored route landmarks remain fixed.");
            Debug.Log(
                "[Mindforge:Showcase] Neural-Gothic World V0.7 ready: solved procedural cells now receive deterministic local architectural detail with no new colliders; " +
                "Cloister gate/spire/well, Memory Loom, Market Reliquary, Cathedral Relay and distant skyline anchors establish long/mid/near visual hierarchy. " +
                "A small PBR palette and bounded point-light rhythm replace indiscriminate effect stacking; topology, E routing, persistence, combat and BCI remain untouched.");
            Debug.Log(
                "[Mindforge:Showcase] V0.7 readability polish ready: shared generated seams carry pointed arch crowns, while the six decorative world lights retain " +
                "full authored intensity only in controller-only qualification and reduce their luminance contribution during calibrated/live BCI presentation.");
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
