using System;
using UnityEngine;

namespace Mindforge.World
{
    [Serializable]
    internal sealed class PersistentShortcutStateV06 { public bool unlocked; }

    /// <summary>
    /// Context/persistence adapter for WorldShortcut. The shortcut remains the concrete
    /// gate/telemetry authority; this adapter contributes only stable identity, E routing and
    /// rest/restart restoration.
    /// </summary>
    public sealed class WorldShortcutInteractionV06 : PersistentWorldInteractionV06
    {
        [SerializeField] private WorldShortcut shortcut;
        [SerializeField] private string displayName = "Memory Conduit";

        public override string PersistenceType => "mindforge.shortcut.v1";
        public override string InteractionId => "shortcut." + StableWorldId + ".open";
        public override string Prompt => "Open " + displayName;
        public override Transform Anchor => shortcut != null ? shortcut.InteractionPoint : transform;
        public override float Radius => shortcut != null ? shortcut.InteractionRadius : 2.2f;
        public override int Priority => 27;

        public void ConfigureRuntime(
            string id,
            string label,
            WorldShortcut target,
            WorldStateLedger world,
            WorldSignalBus bus)
        {
            ConfigureIdentity(id, world, bus);
            displayName = string.IsNullOrWhiteSpace(label) ? "Shortcut" : label.Trim();
            shortcut = target;
            if (shortcut != null) shortcut.SetExternalInteractionOwned(true);
        }

        protected override void OnEnable()
        {
            if (shortcut == null) shortcut = GetComponent<WorldShortcut>();
            if (shortcut != null) shortcut.SetExternalInteractionOwned(true);
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            if (shortcut != null) shortcut.SetExternalInteractionOwned(false);
            base.OnDisable();
        }

        public override bool CanInteract(Transform actor)
            => shortcut != null && shortcut.CanUnlockNow && !string.IsNullOrEmpty(StableWorldId);

        public override bool TryInteract(Transform actor)
        {
            if (!CanInteract(actor) || !shortcut.Unlock()) return false;
            ledger?.SetBool("world.shortcut." + StableWorldId + ".unlocked", true, "persistent_shortcut_opened");
            signals?.Publish(
                WorldSignalKind.Milestone,
                "shortcut.opened",
                subject: StableWorldId,
                stateKey: "world.shortcut." + StableWorldId + ".unlocked",
                intValue: 1,
                reason: "v06_context_shortcut");
            return true;
        }

        public override string CapturePersistentState()
            => JsonUtility.ToJson(new PersistentShortcutStateV06 { unlocked = shortcut != null && shortcut.IsUnlocked });

        public override void RestorePersistentState(string stateJson)
        {
            PersistentShortcutStateV06 state = string.IsNullOrWhiteSpace(stateJson)
                ? new PersistentShortcutStateV06()
                : JsonUtility.FromJson<PersistentShortcutStateV06>(stateJson);
            bool unlocked = state != null && state.unlocked;
            shortcut?.RestoreUnlocked(unlocked, true);
            ledger?.SetBool("world.shortcut." + StableWorldId + ".unlocked", unlocked, "persistent_shortcut_restore");
        }
    }
}
