using System;
using System.Collections.Generic;
using UnityEngine;
using Mindforge.Combat;
using Mindforge.Journey;

namespace Mindforge.World
{
    public abstract class PersistentWorldInteractionV06 : WorldInteractionSourceV1, IWorldPersistentAdapterV06
    {
        [SerializeField] protected string stableWorldId;
        [SerializeField] protected WorldStateLedger ledger;
        [SerializeField] protected WorldSignalBus signals;

        public string StableWorldId => PlayerInventoryV06.NormalizeId(stableWorldId);
        public abstract string PersistenceType { get; }
        public abstract string CapturePersistentState();
        public abstract void RestorePersistentState(string stateJson);

        protected void ConfigureIdentity(string id, WorldStateLedger world, WorldSignalBus bus)
        {
            stableWorldId = PlayerInventoryV06.NormalizeId(id);
            ledger = world;
            signals = bus;
        }

        protected void ResolveFoundation()
        {
            if (ledger == null) ledger = FindObjectOfType<WorldStateLedger>(true);
            if (signals == null) signals = FindObjectOfType<WorldSignalBus>(true);
        }

        protected override void OnEnable()
        {
            ResolveFoundation();
            if (string.IsNullOrEmpty(StableWorldId))
                Debug.LogError("[Mindforge:WorldV06] Persistent interaction missing stable world id on " + name);
            base.OnEnable();
        }
    }

    [Serializable]
    internal sealed class PersistentGateStateV06 { public bool open; }

    /// <summary>Stable, rest/restart-safe shortcut or progression gate using the shared E router.</summary>
    public sealed class PersistentGateInteractionV06 : PersistentWorldInteractionV06
    {
        [SerializeField] private JourneyGate gate;
        [SerializeField] private string displayName = "Shortcut";
        [SerializeField] private string requiredBoolFact;
        [SerializeField] private bool requiredBoolValue = true;

        public override string PersistenceType => "mindforge.gate.v1";
        public override string InteractionId => "gate." + StableWorldId + ".open";
        public override string Prompt => "Open " + (string.IsNullOrWhiteSpace(displayName) ? "Gate" : displayName.Trim());
        public override float Radius => 3.2f;
        public override int Priority => 28;

        public void ConfigureRuntime(
            string id,
            JourneyGate targetGate,
            string label,
            WorldStateLedger world,
            WorldSignalBus bus)
        {
            ConfigureIdentity(id, world, bus);
            gate = targetGate;
            displayName = label;
        }

        public override bool CanInteract(Transform actor)
        {
            ResolveFoundation();
            if (gate == null || gate.Open || string.IsNullOrEmpty(StableWorldId)) return false;
            if (string.IsNullOrWhiteSpace(requiredBoolFact)) return true;
            return ledger != null && ledger.TryGetBool(requiredBoolFact, out bool value) && value == requiredBoolValue;
        }

        public override bool TryInteract(Transform actor)
        {
            if (!CanInteract(actor)) return false;
            gate.SetOpen(true);
            ledger?.SetBool("world.gate." + StableWorldId + ".open", true, "persistent_gate_opened");
            signals?.Publish(
                WorldSignalKind.Milestone,
                "gate.opened",
                subject: StableWorldId,
                stateKey: "world.gate." + StableWorldId + ".open",
                intValue: 1,
                reason: "v06_persistent_gate");
            return true;
        }

        public override string CapturePersistentState()
            => JsonUtility.ToJson(new PersistentGateStateV06 { open = gate != null && gate.Open });

        public override void RestorePersistentState(string stateJson)
        {
            PersistentGateStateV06 state = string.IsNullOrWhiteSpace(stateJson)
                ? new PersistentGateStateV06()
                : JsonUtility.FromJson<PersistentGateStateV06>(stateJson);
            bool open = state != null && state.open;
            if (gate != null) gate.SetOpen(open, true);
            ledger?.SetBool("world.gate." + StableWorldId + ".open", open, "persistent_gate_restore");
        }
    }

    [Serializable]
    internal sealed class PersistentPickupStateV06 { public bool claimed; }

    /// <summary>Loot source with stable identity and persisted reward receipt.</summary>
    public sealed class PersistentPickupInteractionV06 : PersistentWorldInteractionV06
    {
        [SerializeField] private PlayerInventoryV06 inventory;
        [SerializeField] private string itemId = "memory_shard";
        [SerializeField] private string displayName = "Memory Shard";
        [SerializeField, Min(1)] private int quantity = 1;
        [SerializeField] private string autoEquipSlot;
        [SerializeField] private Renderer[] renderers;
        [SerializeField] private Collider[] colliders;
        private bool _claimed;

        public override string PersistenceType => "mindforge.pickup.v1";
        public override string InteractionId => "loot." + StableWorldId + ".take";
        public override string Prompt => "Take " + (string.IsNullOrWhiteSpace(displayName) ? itemId : displayName.Trim());
        public override float Radius => 2.6f;
        public override int Priority => 22;
        private string Receipt => "world_pickup:" + StableWorldId;

        public void ConfigureRuntime(
            string id,
            string item,
            string label,
            int count,
            PlayerInventoryV06 playerInventory,
            WorldStateLedger world,
            WorldSignalBus bus,
            string equipSlot = null)
        {
            ConfigureIdentity(id, world, bus);
            itemId = PlayerInventoryV06.NormalizeId(item);
            displayName = label;
            quantity = Mathf.Max(1, count);
            inventory = playerInventory;
            autoEquipSlot = equipSlot;
            ResolveVisuals();
            ReconcileReceipt();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (inventory == null) inventory = FindObjectOfType<PlayerInventoryV06>(true);
            ResolveVisuals();
            ReconcileReceipt();
        }

        public override bool CanInteract(Transform actor)
        {
            if (inventory == null) inventory = FindObjectOfType<PlayerInventoryV06>(true);
            ReconcileReceipt();
            return !_claimed && inventory != null && !string.IsNullOrEmpty(StableWorldId) && !string.IsNullOrEmpty(itemId);
        }

        public override bool TryInteract(Transform actor)
        {
            if (!CanInteract(actor)) return false;
            if (!inventory.Grant(itemId, quantity, Receipt)) return false;
            if (!string.IsNullOrWhiteSpace(autoEquipSlot)) inventory.TryEquip(autoEquipSlot, itemId);
            _claimed = true;
            ApplyVisibility();
            ledger?.SetBool("world.pickup." + StableWorldId + ".claimed", true, "persistent_pickup_claimed");
            return true;
        }

        public override string CapturePersistentState()
        {
            ReconcileReceipt();
            return JsonUtility.ToJson(new PersistentPickupStateV06 { claimed = _claimed });
        }

        public override void RestorePersistentState(string stateJson)
        {
            PersistentPickupStateV06 state = string.IsNullOrWhiteSpace(stateJson)
                ? new PersistentPickupStateV06()
                : JsonUtility.FromJson<PersistentPickupStateV06>(stateJson);
            _claimed = state != null && state.claimed;
            ReconcileReceipt();
            ApplyVisibility();
            ledger?.SetBool("world.pickup." + StableWorldId + ".claimed", _claimed, "persistent_pickup_restore");
        }

        private void ReconcileReceipt()
        {
            if (inventory != null && inventory.HasReceipt(Receipt)) _claimed = true;
            ApplyVisibility();
        }

        private void ResolveVisuals()
        {
            if (renderers == null || renderers.Length == 0) renderers = GetComponentsInChildren<Renderer>(true);
            if (colliders == null || colliders.Length == 0) colliders = GetComponentsInChildren<Collider>(true);
        }

        private void ApplyVisibility()
        {
            if (renderers != null)
                for (int i = 0; i < renderers.Length; i++) if (renderers[i] != null) renderers[i].enabled = !_claimed;
            if (colliders != null)
                for (int i = 0; i < colliders.Length; i++) if (colliders[i] != null) colliders[i].enabled = !_claimed;
        }
    }

    [Serializable]
    internal sealed class ShrineStateV06 { public bool visited; }

    /// <summary>Persistent shrine discovery/save anchor. It does not duplicate Memory Forge healing authority.</summary>
    public sealed class PersistentShrineInteractionV06 : PersistentWorldInteractionV06
    {
        [SerializeField] private PlayerInventoryV06 inventory;
        [SerializeField] private PlayerProfileSaveV06 persistence;
        [SerializeField] private string displayName = "Signal Shrine";
        [SerializeField] private string regionId;
        private bool _visited;

        public override string PersistenceType => "mindforge.shrine.v1";
        public override string InteractionId => "shrine." + StableWorldId + ".commune";
        public override string Prompt => _visited ? "Commune with " + displayName : "Discover " + displayName;
        public override float Radius => 2.8f;
        public override int Priority => 24;

        public void ConfigureRuntime(
            string id,
            string label,
            string region,
            PlayerInventoryV06 playerInventory,
            PlayerProfileSaveV06 save,
            WorldStateLedger world,
            WorldSignalBus bus)
        {
            ConfigureIdentity(id, world, bus);
            displayName = label;
            regionId = PlayerInventoryV06.NormalizeId(region);
            inventory = playerInventory;
            persistence = save;
        }

        public override bool CanInteract(Transform actor) => !string.IsNullOrEmpty(StableWorldId);

        public override bool TryInteract(Transform actor)
        {
            ResolveFoundation();
            if (inventory == null) inventory = FindObjectOfType<PlayerInventoryV06>(true);
            if (persistence == null) persistence = FindObjectOfType<PlayerProfileSaveV06>(true);
            _visited = true;
            if (inventory != null && !string.IsNullOrEmpty(regionId)) inventory.DiscoverRegion(regionId);
            ledger?.SetBool("world.shrine." + StableWorldId + ".visited", true, "persistent_shrine_visit");
            signals?.Publish(WorldSignalKind.Checkpoint, "shrine.visited", StableWorldId, reason: "v06_shrine");
            persistence?.SaveNow();
            return true;
        }

        public override string CapturePersistentState()
            => JsonUtility.ToJson(new ShrineStateV06 { visited = _visited });

        public override void RestorePersistentState(string stateJson)
        {
            ShrineStateV06 state = string.IsNullOrWhiteSpace(stateJson) ? new ShrineStateV06() : JsonUtility.FromJson<ShrineStateV06>(stateJson);
            _visited = state != null && state.visited;
            ledger?.SetBool("world.shrine." + StableWorldId + ".visited", _visited, "persistent_shrine_restore");
        }
    }

    /// <summary>Trigger-only region discovery. Discovery lives in the persisted inventory/profile snapshot.</summary>
    public sealed class RegionDiscoveryV06 : MonoBehaviour
    {
        [SerializeField] private string regionId;
        [SerializeField] private PlayerInventoryV06 inventory;
        [SerializeField] private WorldStateLedger ledger;

        public void ConfigureRuntime(string id, PlayerInventoryV06 playerInventory, WorldStateLedger world)
        {
            regionId = PlayerInventoryV06.NormalizeId(id);
            inventory = playerInventory;
            ledger = world;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other == null || other.GetComponentInParent<GuardianMotor>() == null) return;
            if (inventory == null) inventory = FindObjectOfType<PlayerInventoryV06>(true);
            if (ledger == null) ledger = FindObjectOfType<WorldStateLedger>(true);
            if (inventory == null || !inventory.DiscoverRegion(regionId)) return;
            ledger?.SetBool("profile.region." + PlayerInventoryV06.NormalizeId(regionId) + ".discovered", true, "region_trigger");
        }
    }

    [Serializable]
    public sealed class DialogueChoiceV06
    {
        public string label;
        public string next_node_id;
        public string required_bool_fact;
        public bool required_bool_value = true;
        public string set_bool_fact;
        public bool set_bool_value = true;
    }

    [Serializable]
    public sealed class DialogueNodeV06
    {
        public string id;
        public string speaker;
        [TextArea(2, 6)] public string text;
        public string next_node_id;
        public string set_bool_fact;
        public bool set_bool_value = true;
        public List<DialogueChoiceV06> choices = new List<DialogueChoiceV06>();
    }

    [CreateAssetMenu(menuName = "Mindforge/World/Dialogue Graph V0.6", fileName = "DialogueGraphV06")]
    public sealed class DialogueGraphV06 : ScriptableObject
    {
        public string graph_id;
        public string start_node_id = "start";
        public List<DialogueNodeV06> nodes = new List<DialogueNodeV06>();

        public DialogueNodeV06 Find(string id)
        {
            string key = PlayerInventoryV06.NormalizeId(id);
            for (int i = 0; i < nodes.Count; i++)
                if (nodes[i] != null && PlayerInventoryV06.NormalizeId(nodes[i].id) == key) return nodes[i];
            return null;
        }
    }

    /// <summary>
    /// One scene-local dialogue presenter. It never samples E; the existing contextual router
    /// continues to own E and calls NpcDialogueInteractionV06.TryInteract to advance.
    /// </summary>
    public sealed class DialogueSessionV06 : MonoBehaviour
    {
        private NpcDialogueInteractionV06 _owner;
        private DialogueGraphV06 _graph;
        private DialogueNodeV06 _node;
        private WorldStateLedger _ledger;
        private GUIStyle _speaker;
        private GUIStyle _body;
        private GUIStyle _choice;

        public bool Active => _owner != null && _graph != null && _node != null;
        public bool IsOwnedBy(NpcDialogueInteractionV06 owner) => Active && _owner == owner;

        public static DialogueSessionV06 ResolveOrCreate()
        {
            DialogueSessionV06 session = FindObjectOfType<DialogueSessionV06>(true);
            if (session != null) return session;
            return new GameObject("MindforgeDialogueSessionV06").AddComponent<DialogueSessionV06>();
        }

        public bool Begin(NpcDialogueInteractionV06 owner, DialogueGraphV06 graph, WorldStateLedger ledger)
        {
            if (owner == null || graph == null) return false;
            DialogueNodeV06 start = graph.Find(graph.start_node_id);
            if (start == null) return false;
            _owner = owner;
            _graph = graph;
            _ledger = ledger;
            Enter(start);
            return true;
        }

        public bool AdvanceDefault()
        {
            if (!Active) return false;
            List<DialogueChoiceV06> valid = ValidChoices();
            if (valid.Count > 0) return SelectChoice(valid[0]);
            if (string.IsNullOrWhiteSpace(_node.next_node_id))
            {
                Close();
                return true;
            }
            DialogueNodeV06 next = _graph.Find(_node.next_node_id);
            if (next == null)
            {
                Close();
                return true;
            }
            Enter(next);
            return true;
        }

        private bool SelectChoice(DialogueChoiceV06 choice)
        {
            if (choice == null) return false;
            if (!string.IsNullOrWhiteSpace(choice.set_bool_fact))
                _ledger?.SetBool(choice.set_bool_fact, choice.set_bool_value, "dialogue_choice");
            DialogueNodeV06 next = _graph.Find(choice.next_node_id);
            if (next == null)
            {
                Close();
                return true;
            }
            Enter(next);
            return true;
        }

        private void Enter(DialogueNodeV06 node)
        {
            _node = node;
            if (_node != null && !string.IsNullOrWhiteSpace(_node.set_bool_fact))
                _ledger?.SetBool(_node.set_bool_fact, _node.set_bool_value, "dialogue_node");
        }

        private List<DialogueChoiceV06> ValidChoices()
        {
            List<DialogueChoiceV06> valid = new List<DialogueChoiceV06>();
            if (_node == null || _node.choices == null) return valid;
            for (int i = 0; i < _node.choices.Count; i++)
            {
                DialogueChoiceV06 choice = _node.choices[i];
                if (choice == null) continue;
                if (string.IsNullOrWhiteSpace(choice.required_bool_fact))
                {
                    valid.Add(choice);
                    continue;
                }
                if (_ledger != null && _ledger.TryGetBool(choice.required_bool_fact, out bool value) && value == choice.required_bool_value)
                    valid.Add(choice);
            }
            return valid;
        }

        private void Close()
        {
            _owner = null;
            _graph = null;
            _node = null;
        }

        private void OnGUI()
        {
            if (!Active) return;
            EnsureStyles();
            float width = Mathf.Min(760f, Screen.width - 48f);
            Rect panel = new Rect((Screen.width - width) * 0.5f, Screen.height - 270f, width, 190f);
            Color before = GUI.color;
            GUI.color = new Color(0.025f, 0.032f, 0.050f, 0.97f);
            GUI.DrawTexture(panel, Texture2D.whiteTexture);
            GUI.color = before;
            GUI.Label(new Rect(panel.x + 20f, panel.y + 14f, panel.width - 40f, 24f), _node.speaker ?? string.Empty, _speaker);
            GUI.Label(new Rect(panel.x + 20f, panel.y + 42f, panel.width - 40f, 72f), _node.text ?? string.Empty, _body);

            List<DialogueChoiceV06> choices = ValidChoices();
            if (choices.Count == 0)
            {
                GUI.Label(new Rect(panel.x + 20f, panel.yMax - 48f, panel.width - 40f, 24f), "E  CONTINUE", _choice);
                return;
            }
            float x = panel.x + 20f;
            float y = panel.yMax - 62f;
            float choiceWidth = (panel.width - 40f) / Mathf.Max(1, choices.Count);
            for (int i = 0; i < choices.Count; i++)
            {
                DialogueChoiceV06 choice = choices[i];
                if (GUI.Button(new Rect(x + i * choiceWidth, y, choiceWidth - 8f, 34f), choice.label ?? "Continue"))
                    SelectChoice(choice);
            }
        }

        private void EnsureStyles()
        {
            if (_speaker == null)
            {
                _speaker = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold };
                _speaker.normal.textColor = new Color(0.30f, 0.88f, 1f, 1f);
            }
            if (_body == null)
            {
                _body = new GUIStyle(GUI.skin.label) { fontSize = 15, wordWrap = true };
                _body.normal.textColor = new Color(0.94f, 0.97f, 1f, 1f);
            }
            if (_choice == null)
            {
                _choice = new GUIStyle(GUI.skin.label) { fontSize = 12, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleRight };
                _choice.normal.textColor = new Color(0.66f, 0.78f, 0.92f, 1f);
            }
        }
    }

    public sealed class NpcDialogueInteractionV06 : WorldInteractionSourceV1
    {
        [SerializeField] private string stableWorldId;
        [SerializeField] private string displayName = "Wanderer";
        [SerializeField] private DialogueGraphV06 dialogue;
        [SerializeField] private WorldStateLedger ledger;
        [SerializeField] private DialogueSessionV06 session;

        public string StableWorldId => PlayerInventoryV06.NormalizeId(stableWorldId);
        public override string InteractionId => "npc." + StableWorldId + ".talk";
        public override string Prompt => session != null && session.IsOwnedBy(this) ? "Continue conversation" : "Speak with " + displayName;
        public override float Radius => 3.4f;
        public override int Priority => 18;

        public void ConfigureRuntime(string id, string label, DialogueGraphV06 graph, WorldStateLedger world)
        {
            stableWorldId = id;
            displayName = label;
            dialogue = graph;
            ledger = world;
            session = DialogueSessionV06.ResolveOrCreate();
        }

        public override bool CanInteract(Transform actor) => dialogue != null && !string.IsNullOrEmpty(StableWorldId);

        public override bool TryInteract(Transform actor)
        {
            if (!CanInteract(actor)) return false;
            if (ledger == null) ledger = FindObjectOfType<WorldStateLedger>(true);
            if (session == null) session = DialogueSessionV06.ResolveOrCreate();
            if (session.IsOwnedBy(this)) return session.AdvanceDefault();
            return session.Begin(this, dialogue, ledger);
        }
    }
}
