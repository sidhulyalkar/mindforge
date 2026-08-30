using System;
using UnityEngine;
using Mindforge.Journey;

namespace Mindforge.World
{
    [Serializable]
    internal sealed class PersistentGateStateV1
    {
        public bool open;
    }

    /// <summary>
    /// Persistent one-way world gate. JourneyGate remains the concrete geometry/collision
    /// authority; this component owns the interaction + persistence adapter for one stable id.
    /// </summary>
    public sealed class PersistentWorldGateV1 : WorldInteractionSourceV1, IWorldSaveAuthorityV1
    {
        public const string Schema = "mindforge.world_gate.v1";

        [SerializeField] private string stableId = "gate.unknown";
        [SerializeField] private string prompt = "Open Gate";
        [SerializeField] private JourneyGate gate;
        [SerializeField] private WorldStateLedger ledger;
        [SerializeField] private WorldSignalBus signals;
        [SerializeField, Min(0.5f)] private float interactionRadius = 3.1f;
        [SerializeField] private int interactionPriority = 20;

        public string AuthorityId => Normalize(stableId, "gate.unknown");
        public string AuthoritySchema => Schema;
        public int RestoreOrder => 100;

        public override string InteractionId => AuthorityId + ".open";
        public override string Prompt => prompt ?? "Open Gate";
        public override Transform Anchor => transform;
        public override float Radius => Mathf.Max(0.5f, interactionRadius);
        public override int Priority => interactionPriority;

        public bool Open => gate != null && gate.Open;

        public void ConfigureRuntime(
            string id,
            JourneyGate journeyGate,
            WorldStateLedger worldState,
            WorldSignalBus signalBus,
            string interactionPrompt = null)
        {
            stableId = Normalize(id, stableId);
            gate = journeyGate;
            ledger = worldState;
            signals = signalBus;
            if (!string.IsNullOrWhiteSpace(interactionPrompt)) prompt = interactionPrompt.Trim();
            SyncSemanticState("gate_configured");
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            Resolve();
        }

        public override bool CanInteract(Transform actor)
        {
            Resolve();
            return gate != null && !gate.Open;
        }

        public override bool TryInteract(Transform actor)
        {
            if (!CanInteract(actor)) return false;
            gate.SetOpen(true, false);
            SyncSemanticState("conventional_gate_interaction");
            signals?.Publish(
                WorldSignalKind.Interaction,
                "world.gate.opened",
                subject: AuthorityId,
                stateKey: StateKey,
                intValue: 1,
                floatValue: 1f,
                reason: "conventional_context_action");
            return true;
        }

        public WorldAuthoritySnapshotV1 CaptureSafeBoundary()
        {
            Resolve();
            PersistentGateStateV1 payload = new PersistentGateStateV1 { open = gate != null && gate.Open };
            return new WorldAuthoritySnapshotV1
            {
                authority_id = AuthorityId,
                authority_schema = Schema,
                payload_json = JsonUtility.ToJson(payload),
            };
        }

        public bool CanRestore(WorldAuthoritySnapshotV1 snapshot)
            => snapshot != null &&
               string.Equals(Normalize(snapshot.authority_id, string.Empty), AuthorityId, StringComparison.Ordinal) &&
               string.Equals(Normalize(snapshot.authority_schema, string.Empty), Schema, StringComparison.Ordinal) &&
               !string.IsNullOrWhiteSpace(snapshot.payload_json);

        public void RestoreSafeBoundary(WorldAuthoritySnapshotV1 snapshot)
        {
            if (!CanRestore(snapshot)) throw new InvalidOperationException("Incompatible gate snapshot: " + AuthorityId);
            Resolve();
            PersistentGateStateV1 payload = JsonUtility.FromJson<PersistentGateStateV1>(snapshot.payload_json);
            if (payload == null) throw new InvalidOperationException("Malformed gate snapshot: " + AuthorityId);
            gate?.SetOpen(payload.open, true);
            SyncSemanticState("world_save_restore");
        }

        public void ResetToSafeDefault()
        {
            Resolve();
            gate?.SetOpen(false, true);
            SyncSemanticState("world_save_safe_default");
        }

        private string StateKey => "world." + AuthorityId + ".open";

        private void SyncSemanticState(string reason)
        {
            if (ledger == null) return;
            ledger.SetBool(StateKey, gate != null && gate.Open, reason);
        }

        private void Resolve()
        {
            if (gate == null) gate = GetComponent<JourneyGate>();
            if (ledger == null) ledger = FindObjectOfType<WorldStateLedger>(true);
            if (signals == null) signals = FindObjectOfType<WorldSignalBus>(true);
        }

        private static string Normalize(string value, string fallback)
            => string.IsNullOrWhiteSpace(value) ? (fallback ?? string.Empty) : value.Trim().ToLowerInvariant();
    }

    [Serializable]
    internal sealed class PersistentPickupStateV1
    {
        public bool collected;
    }

    /// <summary>
    /// One-shot persistent world pickup. PlayerProgressionLedger owns the actual reward and
    /// receipt; this adapter owns only world presence/collection state for a stable content id.
    /// Receipt reconciliation makes profile/world-file write ordering duplication-safe.
    /// </summary>
    public sealed class PersistentWorldPickupV1 : WorldInteractionSourceV1, IWorldSaveAuthorityV1
    {
        public const string Schema = "mindforge.world_pickup.v1";

        [SerializeField] private string stableId = "pickup.unknown";
        [SerializeField] private string prompt = "Collect Resonance";
        [SerializeField] private Transform presentationRoot;
        [SerializeField] private PlayerProgressionLedger progression;
        [SerializeField] private WorldStateLedger ledger;
        [SerializeField] private WorldSignalBus signals;
        [SerializeField] private WorldRewardKind rewardKind = WorldRewardKind.Resonance;
        [SerializeField] private int rewardAmount = 25;
        [SerializeField] private string unlockId;
        [SerializeField, Min(0.5f)] private float interactionRadius = 2.6f;
        [SerializeField] private int interactionPriority = 15;
        [SerializeField] private bool collected;

        public string AuthorityId => Normalize(stableId, "pickup.unknown");
        public string AuthoritySchema => Schema;
        public int RestoreOrder => 200;

        public override string InteractionId => AuthorityId + ".collect";
        public override string Prompt => prompt ?? "Collect";
        public override Transform Anchor => presentationRoot != null ? presentationRoot : transform;
        public override float Radius => Mathf.Max(0.5f, interactionRadius);
        public override int Priority => interactionPriority;

        public bool Collected => collected;
        private string ReceiptId => "world_pickup:" + AuthorityId;
        private string StateKey => "world." + AuthorityId + ".collected";

        public void ConfigureRuntime(
            string id,
            Transform visuals,
            PlayerProgressionLedger playerProgression,
            WorldStateLedger worldState,
            WorldSignalBus signalBus,
            WorldRewardKind kind,
            int amount,
            string semanticUnlockId = null,
            string interactionPrompt = null)
        {
            stableId = Normalize(id, stableId);
            presentationRoot = visuals;
            progression = playerProgression;
            ledger = worldState;
            signals = signalBus;
            rewardKind = kind;
            rewardAmount = Mathf.Max(0, amount);
            unlockId = semanticUnlockId ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(interactionPrompt)) prompt = interactionPrompt.Trim();
            ReconcileWithProgression(false, "pickup_configured");
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            Resolve();
            ReconcileWithProgression(false, "pickup_enabled");
        }

        public override bool CanInteract(Transform actor)
        {
            Resolve();
            return !collected && progression != null;
        }

        public override bool TryInteract(Transform actor)
        {
            if (!CanInteract(actor)) return false;
            bool receiptWasNew = progression.TryClaimRewardReceipt(ReceiptId);
            if (receiptWasNew) progression.Grant(BuildReward(), "world_pickup:" + AuthorityId);
            SetCollected(true, "conventional_pickup_interaction");
            signals?.Publish(
                WorldSignalKind.RewardGranted,
                "world.pickup.collected",
                subject: AuthorityId,
                stateKey: StateKey,
                stringValue: rewardKind == WorldRewardKind.Unlock ? Normalize(unlockId, string.Empty) : string.Empty,
                intValue: rewardAmount,
                floatValue: rewardAmount,
                reason: receiptWasNew ? "world_pickup_reward" : "receipt_already_claimed");
            return true;
        }

        public WorldAuthoritySnapshotV1 CaptureSafeBoundary()
        {
            Resolve();
            ReconcileWithProgression(false, "pickup_capture");
            return new WorldAuthoritySnapshotV1
            {
                authority_id = AuthorityId,
                authority_schema = Schema,
                payload_json = JsonUtility.ToJson(new PersistentPickupStateV1 { collected = collected }),
            };
        }

        public bool CanRestore(WorldAuthoritySnapshotV1 snapshot)
            => snapshot != null &&
               string.Equals(Normalize(snapshot.authority_id, string.Empty), AuthorityId, StringComparison.Ordinal) &&
               string.Equals(Normalize(snapshot.authority_schema, string.Empty), Schema, StringComparison.Ordinal) &&
               !string.IsNullOrWhiteSpace(snapshot.payload_json);

        public void RestoreSafeBoundary(WorldAuthoritySnapshotV1 snapshot)
        {
            if (!CanRestore(snapshot)) throw new InvalidOperationException("Incompatible pickup snapshot: " + AuthorityId);
            Resolve();
            PersistentPickupStateV1 payload = JsonUtility.FromJson<PersistentPickupStateV1>(snapshot.payload_json);
            if (payload == null) throw new InvalidOperationException("Malformed pickup snapshot: " + AuthorityId);

            bool profileAlreadyPaid = progression != null && progression.HasRewardReceipt(ReceiptId);
            if (payload.collected && progression != null && !profileAlreadyPaid)
            {
                // World file may have been committed after its matching profile write was
                // interrupted. Reconcile once through the durable receipt, never by raw add.
                if (progression.TryClaimRewardReceipt(ReceiptId))
                    progression.Grant(BuildReward(), "world_pickup_restore_reconcile:" + AuthorityId);
            }
            SetCollected(payload.collected || profileAlreadyPaid, "world_save_restore");
        }

        public void ResetToSafeDefault()
        {
            Resolve();
            bool alreadyPaid = progression != null && progression.HasRewardReceipt(ReceiptId);
            SetCollected(alreadyPaid, "world_save_safe_default");
        }

        private WorldQuestRewardDefinition BuildReward()
        {
            return new WorldQuestRewardDefinition
            {
                kind = rewardKind,
                amount = Mathf.Max(0, rewardAmount),
                id = rewardKind == WorldRewardKind.Unlock ? Normalize(unlockId, string.Empty) : string.Empty,
            };
        }

        private void ReconcileWithProgression(bool allowGrant, string reason)
        {
            Resolve();
            if (progression == null) return;
            bool paid = progression.HasRewardReceipt(ReceiptId);
            if (paid)
            {
                SetCollected(true, reason);
                return;
            }
            if (allowGrant && collected && progression.TryClaimRewardReceipt(ReceiptId))
                progression.Grant(BuildReward(), reason);
            ApplyPresentation();
        }

        private void SetCollected(bool value, string reason)
        {
            collected = value;
            ApplyPresentation();
            ledger?.SetBool(StateKey, collected, reason);
        }

        private void ApplyPresentation()
        {
            if (presentationRoot != null) presentationRoot.gameObject.SetActive(!collected);
        }

        private void Resolve()
        {
            if (progression == null) progression = FindObjectOfType<PlayerProgressionLedger>(true);
            if (ledger == null) ledger = FindObjectOfType<WorldStateLedger>(true);
            if (signals == null) signals = FindObjectOfType<WorldSignalBus>(true);
        }

        private static string Normalize(string value, string fallback)
            => string.IsNullOrWhiteSpace(value) ? (fallback ?? string.Empty) : value.Trim().ToLowerInvariant();
    }
}
