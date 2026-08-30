using System;
using UnityEngine;
using Mindforge.Journey;

namespace Mindforge.World
{
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
}
