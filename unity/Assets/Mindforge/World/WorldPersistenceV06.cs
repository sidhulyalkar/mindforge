using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Mindforge.World
{
    [Serializable]
    public sealed class InventoryStackV06
    {
        public string item_id;
        public int count;
    }

    [Serializable]
    public sealed class EquipmentBindingV06
    {
        public string slot;
        public string item_id;
    }

    [Serializable]
    public sealed class PlayerInventorySnapshotV06
    {
        public string schema = "mindforge.inventory.v1";
        public List<InventoryStackV06> stacks = new List<InventoryStackV06>();
        public List<EquipmentBindingV06> equipped = new List<EquipmentBindingV06>();
        public List<string> reward_receipts = new List<string>();
        public List<string> discovered_regions = new List<string>();
    }

    /// <summary>
    /// Small semantic inventory/equipment authority for V0.6 world content.
    /// Reward receipts are first-class and persisted so a stable world pickup can never
    /// duplicate its reward after Forge rest, scene reload or application restart.
    /// </summary>
    [DefaultExecutionOrder(-745)]
    public sealed class PlayerInventoryV06 : MonoBehaviour
    {
        [SerializeField] private List<InventoryStackV06> stacks = new List<InventoryStackV06>();
        [SerializeField] private List<EquipmentBindingV06> equipped = new List<EquipmentBindingV06>();
        [SerializeField] private List<string> rewardReceipts = new List<string>();
        [SerializeField] private List<string> discoveredRegions = new List<string>();
        [SerializeField] private WorldSignalBus signals;

        public IReadOnlyList<InventoryStackV06> Stacks => stacks;
        public IReadOnlyList<EquipmentBindingV06> Equipped => equipped;
        public IReadOnlyList<string> DiscoveredRegions => discoveredRegions;

        private void Awake()
        {
            if (signals == null) signals = GetComponent<WorldSignalBus>();
            Normalize();
        }

        public void ConfigureRuntime(WorldSignalBus bus)
        {
            signals = bus;
            Normalize();
        }

        public bool HasReceipt(string receiptId)
            => !string.IsNullOrWhiteSpace(receiptId) && rewardReceipts.Contains(NormalizeId(receiptId));

        public int Count(string itemId)
        {
            string id = NormalizeId(itemId);
            for (int i = 0; i < stacks.Count; i++)
                if (stacks[i] != null && stacks[i].item_id == id) return Mathf.Max(0, stacks[i].count);
            return 0;
        }

        public bool Grant(string itemId, int quantity, string rewardReceipt = null)
        {
            string id = NormalizeId(itemId);
            string receipt = NormalizeId(rewardReceipt);
            if (string.IsNullOrEmpty(id) || quantity <= 0) return false;
            if (!string.IsNullOrEmpty(receipt) && rewardReceipts.Contains(receipt)) return false;

            InventoryStackV06 stack = null;
            for (int i = 0; i < stacks.Count; i++)
            {
                if (stacks[i] != null && stacks[i].item_id == id)
                {
                    stack = stacks[i];
                    break;
                }
            }
            if (stack == null)
            {
                stack = new InventoryStackV06 { item_id = id, count = 0 };
                stacks.Add(stack);
            }
            stack.count += quantity;
            if (!string.IsNullOrEmpty(receipt)) rewardReceipts.Add(receipt);
            Normalize();

            signals?.Publish(
                WorldSignalKind.RewardGranted,
                "inventory.granted",
                subject: id,
                stringValue: receipt,
                intValue: quantity,
                reason: "v06_world_reward");
            return true;
        }

        public bool TryEquip(string slot, string itemId)
        {
            string normalizedSlot = NormalizeId(slot);
            string id = NormalizeId(itemId);
            if (string.IsNullOrEmpty(normalizedSlot) || Count(id) <= 0) return false;

            EquipmentBindingV06 binding = null;
            for (int i = 0; i < equipped.Count; i++)
            {
                if (equipped[i] != null && equipped[i].slot == normalizedSlot)
                {
                    binding = equipped[i];
                    break;
                }
            }
            if (binding == null)
            {
                binding = new EquipmentBindingV06 { slot = normalizedSlot };
                equipped.Add(binding);
            }
            if (binding.item_id == id) return false;
            binding.item_id = id;
            Normalize();
            signals?.Publish(
                WorldSignalKind.ProgressionChanged,
                "inventory.equipped",
                subject: normalizedSlot,
                stringValue: id,
                reason: "v06_equipment_binding");
            return true;
        }

        public bool DiscoverRegion(string regionId)
        {
            string id = NormalizeId(regionId);
            if (string.IsNullOrEmpty(id) || discoveredRegions.Contains(id)) return false;
            discoveredRegions.Add(id);
            discoveredRegions.Sort(StringComparer.Ordinal);
            signals?.Publish(
                WorldSignalKind.RegionEntered,
                "region.discovered",
                subject: id,
                stringValue: id,
                reason: "v06_region_discovery");
            return true;
        }

        public PlayerInventorySnapshotV06 CaptureSnapshot()
        {
            Normalize();
            PlayerInventorySnapshotV06 snapshot = new PlayerInventorySnapshotV06();
            for (int i = 0; i < stacks.Count; i++)
                snapshot.stacks.Add(new InventoryStackV06 { item_id = stacks[i].item_id, count = stacks[i].count });
            for (int i = 0; i < equipped.Count; i++)
                snapshot.equipped.Add(new EquipmentBindingV06 { slot = equipped[i].slot, item_id = equipped[i].item_id });
            snapshot.reward_receipts.AddRange(rewardReceipts);
            snapshot.discovered_regions.AddRange(discoveredRegions);
            return snapshot;
        }

        public void RestoreSnapshot(PlayerInventorySnapshotV06 snapshot)
        {
            stacks.Clear();
            equipped.Clear();
            rewardReceipts.Clear();
            discoveredRegions.Clear();
            if (snapshot != null)
            {
                if (snapshot.stacks != null)
                    for (int i = 0; i < snapshot.stacks.Count; i++)
                        if (snapshot.stacks[i] != null)
                            stacks.Add(new InventoryStackV06 { item_id = snapshot.stacks[i].item_id, count = snapshot.stacks[i].count });
                if (snapshot.equipped != null)
                    for (int i = 0; i < snapshot.equipped.Count; i++)
                        if (snapshot.equipped[i] != null)
                            equipped.Add(new EquipmentBindingV06 { slot = snapshot.equipped[i].slot, item_id = snapshot.equipped[i].item_id });
                if (snapshot.reward_receipts != null) rewardReceipts.AddRange(snapshot.reward_receipts);
                if (snapshot.discovered_regions != null) discoveredRegions.AddRange(snapshot.discovered_regions);
            }
            Normalize();
        }

        private void Normalize()
        {
            Dictionary<string, int> merged = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < stacks.Count; i++)
            {
                InventoryStackV06 stack = stacks[i];
                if (stack == null) continue;
                string id = NormalizeId(stack.item_id);
                if (string.IsNullOrEmpty(id) || stack.count <= 0) continue;
                merged.TryGetValue(id, out int count);
                merged[id] = count + stack.count;
            }
            stacks.Clear();
            foreach (KeyValuePair<string, int> pair in merged)
                stacks.Add(new InventoryStackV06 { item_id = pair.Key, count = pair.Value });
            stacks.Sort((a, b) => string.CompareOrdinal(a.item_id, b.item_id));

            HashSet<string> receipts = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < rewardReceipts.Count; i++)
            {
                string id = NormalizeId(rewardReceipts[i]);
                if (!string.IsNullOrEmpty(id)) receipts.Add(id);
            }
            rewardReceipts.Clear();
            rewardReceipts.AddRange(receipts);
            rewardReceipts.Sort(StringComparer.Ordinal);

            HashSet<string> regions = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < discoveredRegions.Count; i++)
            {
                string id = NormalizeId(discoveredRegions[i]);
                if (!string.IsNullOrEmpty(id)) regions.Add(id);
            }
            discoveredRegions.Clear();
            discoveredRegions.AddRange(regions);
            discoveredRegions.Sort(StringComparer.Ordinal);

            for (int i = equipped.Count - 1; i >= 0; i--)
            {
                if (equipped[i] == null)
                {
                    equipped.RemoveAt(i);
                    continue;
                }
                equipped[i].slot = NormalizeId(equipped[i].slot);
                equipped[i].item_id = NormalizeId(equipped[i].item_id);
                if (string.IsNullOrEmpty(equipped[i].slot) || string.IsNullOrEmpty(equipped[i].item_id)) equipped.RemoveAt(i);
            }
            equipped.Sort((a, b) => string.CompareOrdinal(a.slot, b.slot));
        }

        internal static string NormalizeId(string value)
            => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant().Replace(' ', '_');
    }

    [Serializable]
    public sealed class WorldPersistentRecordV06
    {
        public string stable_world_id;
        public string type;
        public string state_json;
    }

    public interface IWorldPersistentAdapterV06
    {
        string StableWorldId { get; }
        string PersistenceType { get; }
        string CapturePersistentState();
        void RestorePersistentState(string stateJson);
    }

    [Serializable]
    public sealed class PlayerProfileSaveEnvelopeV2
    {
        public string schema = "mindforge.player_profile.v2";
        public string generated_utc;
        public PlayerProgressionSnapshot progression = new PlayerProgressionSnapshot();
        public PlayerInventorySnapshotV06 inventory = new PlayerInventorySnapshotV06();
        public List<WorldStateEntry> durable_world_facts = new List<WorldStateEntry>();
        public List<WorldPersistentRecordV06> physical_world_records = new List<WorldPersistentRecordV06>();
    }

    /// <summary>
    /// V0.6 single profile architecture. Semantic story/profile facts and progression are
    /// persisted together with inventory and only those physical world states that expose an
    /// explicit restore adapter. No arbitrary encounter truth is serialized from the ledger.
    /// </summary>
    [DefaultExecutionOrder(-700)]
    public sealed class PlayerProfileSaveV06 : MonoBehaviour
    {
        public const string ProfileSchema = "mindforge.player_profile.v2";

        [SerializeField] private WorldStateLedger world;
        [SerializeField] private PlayerProgressionLedger progression;
        [SerializeField] private PlayerInventoryV06 inventory;
        [SerializeField] private MemoryForgeCheckpoint checkpoint;
        [SerializeField] private WorldSignalBus signals;
        [SerializeField] private bool loadOnStart = true;
        [SerializeField] private bool saveOnQuit = true;
        [SerializeField] private string fileName = "profile-v2.json";

        private bool _loadedOnce;
        private bool _restoring;

        public string LastStatus { get; private set; } = "NOT_LOADED";
        public string LastSavedPath { get; private set; }

        public void ConfigureRuntime(
            WorldStateLedger worldState,
            PlayerProgressionLedger playerProgression,
            PlayerInventoryV06 playerInventory,
            MemoryForgeCheckpoint memoryForge,
            WorldSignalBus signalBus)
        {
            Unsubscribe();
            world = worldState;
            progression = playerProgression;
            inventory = playerInventory;
            checkpoint = memoryForge;
            signals = signalBus;
            Subscribe();
        }

        private void OnEnable()
        {
            Resolve();
            Subscribe();
        }

        private void Start()
        {
            Resolve();
            if (loadOnStart && !_loadedOnce) LoadNow();
        }

        private void OnDisable() => Unsubscribe();
        private void OnApplicationQuit() { if (saveOnQuit) SaveNow(); }

        private void Subscribe()
        {
            if (checkpoint != null)
            {
                checkpoint.Rested -= OnForgeRested;
                checkpoint.Rested += OnForgeRested;
            }
            if (signals != null)
            {
                signals.SignalPublished -= OnWorldSignal;
                signals.SignalPublished += OnWorldSignal;
            }
        }

        private void Unsubscribe()
        {
            if (checkpoint != null) checkpoint.Rested -= OnForgeRested;
            if (signals != null) signals.SignalPublished -= OnWorldSignal;
        }

        private void OnForgeRested() => SaveNow();

        private void OnWorldSignal(WorldSignal signal)
        {
            if (_restoring || signal == null) return;
            if (signal.kind == WorldSignalKind.EncounterStarted || signal.kind == WorldSignalKind.EncounterCleared)
                SaveNow();
        }

        public bool SaveNow()
        {
            Resolve();
            if (world == null || progression == null || inventory == null)
                return Fail("PROFILE_V06_SAVE_MISSING_FOUNDATION");

            try
            {
                PlayerProfileSaveEnvelopeV2 envelope = new PlayerProfileSaveEnvelopeV2
                {
                    schema = ProfileSchema,
                    generated_utc = DateTime.UtcNow.ToString("O"),
                    progression = progression.CaptureSnapshot(),
                    inventory = inventory.CaptureSnapshot(),
                };

                IReadOnlyList<WorldStateEntry> facts = world.Entries;
                for (int i = 0; i < facts.Count; i++)
                {
                    WorldStateEntry entry = facts[i];
                    if (entry != null && IsDurableSemanticFact(entry.key)) envelope.durable_world_facts.Add(entry.Copy());
                }
                envelope.durable_world_facts.Sort((a, b) => string.CompareOrdinal(a.key, b.key));

                List<IWorldPersistentAdapterV06> adapters = FindAdapters();
                HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
                for (int i = 0; i < adapters.Count; i++)
                {
                    IWorldPersistentAdapterV06 adapter = adapters[i];
                    string id = PlayerInventoryV06.NormalizeId(adapter.StableWorldId);
                    if (string.IsNullOrEmpty(id) || !ids.Add(id))
                    {
                        Debug.LogError("[Mindforge:SaveV06] Missing or duplicate stable world id: " + id);
                        continue;
                    }
                    envelope.physical_world_records.Add(new WorldPersistentRecordV06
                    {
                        stable_world_id = id,
                        type = adapter.PersistenceType ?? string.Empty,
                        state_json = adapter.CapturePersistentState() ?? string.Empty,
                    });
                }
                envelope.physical_world_records.Sort((a, b) => string.CompareOrdinal(a.stable_world_id, b.stable_world_id));

                string path = ResolvePath();
                string directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
                string temp = path + ".tmp";
                File.WriteAllText(temp, JsonUtility.ToJson(envelope, true));
                CommitTempFile(temp, path);
                LastSavedPath = path;
                LastStatus = "PROFILE_V06_SAVED";
                signals?.Publish(
                    WorldSignalKind.Milestone,
                    "profile.v06.saved",
                    subject: "player_profile",
                    intValue: envelope.physical_world_records.Count,
                    reason: "stable_world_adapter_save");
                return true;
            }
            catch (Exception ex)
            {
                return Fail("PROFILE_V06_SAVE_FAILED:" + ex.GetType().Name);
            }
        }

        public bool LoadNow()
        {
            Resolve();
            _loadedOnce = true;
            if (world == null || progression == null || inventory == null)
                return Fail("PROFILE_V06_LOAD_MISSING_FOUNDATION");

            string path = ResolvePath();
            if (!File.Exists(path)) return TryMigrateLegacyV05();

            try
            {
                PlayerProfileSaveEnvelopeV2 envelope = JsonUtility.FromJson<PlayerProfileSaveEnvelopeV2>(File.ReadAllText(path));
                if (envelope == null || envelope.schema != ProfileSchema)
                    return Fail("PROFILE_V06_SCHEMA_MISMATCH");

                _restoring = true;
                progression.RestoreSnapshot(envelope.progression ?? new PlayerProgressionSnapshot());
                inventory.RestoreSnapshot(envelope.inventory ?? new PlayerInventorySnapshotV06());
                if (envelope.durable_world_facts != null)
                    for (int i = 0; i < envelope.durable_world_facts.Count; i++) ApplyDurableFact(envelope.durable_world_facts[i]);

                Dictionary<string, WorldPersistentRecordV06> records = new Dictionary<string, WorldPersistentRecordV06>(StringComparer.Ordinal);
                if (envelope.physical_world_records != null)
                {
                    for (int i = 0; i < envelope.physical_world_records.Count; i++)
                    {
                        WorldPersistentRecordV06 record = envelope.physical_world_records[i];
                        if (record == null) continue;
                        string id = PlayerInventoryV06.NormalizeId(record.stable_world_id);
                        if (!string.IsNullOrEmpty(id)) records[id] = record;
                    }
                }
                List<IWorldPersistentAdapterV06> adapters = FindAdapters();
                for (int i = 0; i < adapters.Count; i++)
                {
                    IWorldPersistentAdapterV06 adapter = adapters[i];
                    string id = PlayerInventoryV06.NormalizeId(adapter.StableWorldId);
                    if (records.TryGetValue(id, out WorldPersistentRecordV06 record) &&
                        string.Equals(record.type ?? string.Empty, adapter.PersistenceType ?? string.Empty, StringComparison.Ordinal))
                        adapter.RestorePersistentState(record.state_json);
                }
                _restoring = false;

                LastSavedPath = path;
                LastStatus = "PROFILE_V06_LOADED";
                signals?.Publish(
                    WorldSignalKind.Milestone,
                    "profile.v06.loaded",
                    subject: "player_profile",
                    intValue: records.Count,
                    reason: "stable_world_adapter_restore");
                return true;
            }
            catch (Exception ex)
            {
                _restoring = false;
                return Fail("PROFILE_V06_LOAD_FAILED:" + ex.GetType().Name);
            }
        }

        private bool TryMigrateLegacyV05()
        {
            string legacyPath = Path.Combine(Application.persistentDataPath, "mindforge", "profile-v1.json");
            if (!File.Exists(legacyPath))
            {
                LastStatus = "NO_PROFILE_V06";
                return false;
            }
            try
            {
                PlayerProfileSaveEnvelopeV1 legacy = JsonUtility.FromJson<PlayerProfileSaveEnvelopeV1>(File.ReadAllText(legacyPath));
                if (legacy == null || legacy.schema != PlayerProfileSaveV05.ProfileSchema)
                    return Fail("PROFILE_V06_LEGACY_SCHEMA_MISMATCH");
                progression.RestoreSnapshot(legacy.progression ?? new PlayerProgressionSnapshot());
                if (legacy.durable_world_facts != null)
                    for (int i = 0; i < legacy.durable_world_facts.Count; i++) ApplyDurableFact(legacy.durable_world_facts[i]);
                inventory.RestoreSnapshot(new PlayerInventorySnapshotV06());
                LastStatus = "PROFILE_V06_MIGRATED_V05";
                SaveNow();
                return true;
            }
            catch (Exception ex)
            {
                return Fail("PROFILE_V06_LEGACY_MIGRATION_FAILED:" + ex.GetType().Name);
            }
        }

        private static List<IWorldPersistentAdapterV06> FindAdapters()
        {
            MonoBehaviour[] behaviours = FindObjectsOfType<MonoBehaviour>(true);
            List<IWorldPersistentAdapterV06> adapters = new List<IWorldPersistentAdapterV06>();
            for (int i = 0; i < behaviours.Length; i++)
                if (behaviours[i] is IWorldPersistentAdapterV06 adapter) adapters.Add(adapter);
            adapters.Sort((a, b) => string.CompareOrdinal(a.StableWorldId, b.StableWorldId));
            return adapters;
        }

        private void ApplyDurableFact(WorldStateEntry entry)
        {
            if (entry == null || world == null || !IsDurableSemanticFact(entry.key)) return;
            switch (entry.type)
            {
                case WorldStateValueType.Bool: world.SetBool(entry.key, entry.bool_value, "profile_v06_restore"); break;
                case WorldStateValueType.Int: world.SetInt(entry.key, entry.int_value, "profile_v06_restore"); break;
                case WorldStateValueType.Float: world.SetFloat(entry.key, entry.float_value, "profile_v06_restore"); break;
                case WorldStateValueType.String: world.SetString(entry.key, entry.string_value, "profile_v06_restore"); break;
            }
        }

        private static bool IsDurableSemanticFact(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return false;
            string normalized = key.Trim().ToLowerInvariant();
            return normalized.StartsWith("story.", StringComparison.Ordinal) ||
                   normalized.StartsWith("profile.", StringComparison.Ordinal);
        }

        private string ResolvePath()
        {
            string safe = string.IsNullOrWhiteSpace(fileName) ? "profile-v2.json" : fileName.Trim();
            return Path.Combine(Application.persistentDataPath, "mindforge", safe);
        }

        private static void CommitTempFile(string temp, string path)
        {
            if (!File.Exists(path))
            {
                File.Move(temp, path);
                return;
            }
            string backup = path + ".bak";
            try
            {
                File.Replace(temp, path, backup, true);
                if (File.Exists(backup)) File.Delete(backup);
            }
            catch (PlatformNotSupportedException)
            {
                File.Copy(temp, path, true);
                File.Delete(temp);
            }
            catch (IOException)
            {
                File.Copy(temp, path, true);
                File.Delete(temp);
            }
        }

        private bool Fail(string status)
        {
            LastStatus = status ?? "PROFILE_V06_FAILURE";
            Debug.LogWarning("[Mindforge:SaveV06] " + LastStatus);
            return false;
        }

        private void Resolve()
        {
            if (world == null) world = GetComponent<WorldStateLedger>();
            if (progression == null) progression = GetComponent<PlayerProgressionLedger>();
            if (inventory == null) inventory = GetComponent<PlayerInventoryV06>();
            if (checkpoint == null) checkpoint = FindObjectOfType<MemoryForgeCheckpoint>(true);
            if (signals == null) signals = GetComponent<WorldSignalBus>();
        }
    }
}
