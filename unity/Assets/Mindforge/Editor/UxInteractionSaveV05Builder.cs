#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Mindforge.Combat;
using Mindforge.Traversal;
using Mindforge.World;

namespace Mindforge.Editor
{
    /// <summary>
    /// Final player-facing authoring pass for V0.5. It installs one canonical control
    /// profile, one context interaction router, the Memory Forge interaction adapter and
    /// safe player-profile persistence after Game Foundation V1 has bound the final world.
    /// </summary>
    public static class UxInteractionSaveV05Builder
    {
        public const string RootName = "Mindforge_UX_Interaction_Save_V05";
        public const string Revision = "UX_INTERACTION_SAVE_V05";

        [MenuItem("Mindforge/Showcase/Apply UX + Interaction + Save V0.5", priority = 33)]
        public static void ApplyOpenScene()
        {
            GameObject guardian = EditorSceneLookup.FindIncludingInactive("Guardian");
            MemoryForgeCheckpoint checkpoint = UnityEngine.Object.FindObjectOfType<MemoryForgeCheckpoint>(true);
            WorldSignalBus bus = UnityEngine.Object.FindObjectOfType<WorldSignalBus>(true);
            WorldStateLedger ledger = UnityEngine.Object.FindObjectOfType<WorldStateLedger>(true);
            PlayerProgressionLedger progression = UnityEngine.Object.FindObjectOfType<PlayerProgressionLedger>(true);

            if (guardian == null || checkpoint == null || bus == null || ledger == null || progression == null)
                throw new InvalidOperationException(
                    "UX V0.5 requires Guardian, Memory Forge and the complete Game Foundation V1 semantic/progression stack.");

            GameObject previous = EditorSceneLookup.FindIncludingInactive(RootName);
            if (previous != null) UnityEngine.Object.DestroyImmediate(previous);

            GameObject root = new GameObject(RootName);
            GuardianControlProfileV1 controls = root.AddComponent<GuardianControlProfileV1>();

            GuardianInteractionRouterV1 router = guardian.GetComponent<GuardianInteractionRouterV1>();
            if (router == null) router = guardian.AddComponent<GuardianInteractionRouterV1>();

            MemoryForgeInteractionV1 forgeInteraction = checkpoint.GetComponent<MemoryForgeInteractionV1>();
            if (forgeInteraction == null) forgeInteraction = checkpoint.gameObject.AddComponent<MemoryForgeInteractionV1>();
            forgeInteraction.ConfigureRuntime(checkpoint);
            checkpoint.SetExternalInteractionOwned(true);

            GameObject foundationRoot = bus.gameObject;
            PlayerProfileSaveV05 profile = foundationRoot.GetComponent<PlayerProfileSaveV05>();
            if (profile == null) profile = foundationRoot.AddComponent<PlayerProfileSaveV05>();
            profile.ConfigureRuntime(ledger, progression, checkpoint, bus);

            GuardianHoverbikeController bike = guardian.GetComponent<GuardianHoverbikeController>();
            bike?.SetContextInteractionOwned(true);

            EditorUtility.SetDirty(root);
            EditorUtility.SetDirty(controls);
            EditorUtility.SetDirty(guardian);
            EditorUtility.SetDirty(router);
            EditorUtility.SetDirty(checkpoint);
            EditorUtility.SetDirty(forgeInteraction);
            EditorUtility.SetDirty(profile);
            if (bike != null) EditorUtility.SetDirty(bike);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[Mindforge:UXV05] Canonical controls + contextual E interaction + V5 context replay + safe player profile persistence ready. " +
                "E now owns ride/dismount/reconstruct and future world interactions; physical action authority remains in each concrete system. " +
                "Profile persistence stores progression/reward receipts and non-physical story/profile facts only; encounter/boss resume remains intentionally excluded.");
        }
    }
}
#endif
