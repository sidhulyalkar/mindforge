using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mindforge.World
{
    public enum WorldStateValueType
    {
        Bool = 0,
        Int = 1,
        Float = 2,
        String = 3,
    }

    [Serializable]
    public sealed class WorldStateEntry
    {
        public string key;
        public WorldStateValueType type;
        public bool bool_value;
        public int int_value;
        public float float_value;
        public string string_value;

        public WorldStateEntry Copy()
        {
            return new WorldStateEntry
            {
                key = key,
                type = type,
                bool_value = bool_value,
                int_value = int_value,
                float_value = float_value,
                string_value = string_value,
            };
        }
    }

    [Serializable]
    public sealed class WorldStateSnapshot
    {
        public string schema = "mindforge.world_state.v1";
        public List<WorldStateEntry> entries = new List<WorldStateEntry>();
    }

    /// <summary>
    /// Semantic fact ledger. It stores explicit world facts but owns no physical gameplay.
    /// Snapshot capture/restore is memory-only so persistence format and platform storage can
    /// be decided later without coupling quests or encounters to PlayerPrefs/filesystem APIs.
    /// Restore always notifies derived state consumers exactly once through SnapshotRestored;
    /// optional per-key semantic signals remain a separate observability choice.
    /// </summary>
    [DefaultExecutionOrder(-810)]
    public sealed class WorldStateLedger : MonoBehaviour
    {
        [SerializeField] private WorldSignalBus signals;
        [SerializeField] private List<WorldStateEntry> entries = new List<WorldStateEntry>();

        private readonly Dictionary<string, WorldStateEntry> _index =
            new Dictionary<string, WorldStateEntry>(StringComparer.Ordinal);

        public event Action<string, WorldStateEntry, WorldStateEntry> StateChanged;
        public event Action SnapshotRestored;
        public IReadOnlyList<WorldStateEntry> Entries => entries;

        private void Awake()
        {
            if (signals == null) signals = GetComponent<WorldSignalBus>();
            Reindex();
        }

        public void ConfigureRuntime(WorldSignalBus bus)
        {
            signals = bus;
            Reindex();
        }

        public bool SetBool(string key, bool value, string reason = null)
            => Upsert(new WorldStateEntry { key = NormalizeKey(key), type = WorldStateValueType.Bool, bool_value = value }, reason);

        public bool SetInt(string key, int value, string reason = null)
            => Upsert(new WorldStateEntry { key = NormalizeKey(key), type = WorldStateValueType.Int, int_value = value }, reason);

        public bool SetFloat(string key, float value, string reason = null)
            => Upsert(new WorldStateEntry { key = NormalizeKey(key), type = WorldStateValueType.Float, float_value = value }, reason);

        public bool SetString(string key, string value, string reason = null)
            => Upsert(new WorldStateEntry { key = NormalizeKey(key), type = WorldStateValueType.String, string_value = value ?? string.Empty }, reason);

        public bool TryGetBool(string key, out bool value)
        {
            value = false;
            if (!TryGet(key, WorldStateValueType.Bool, out WorldStateEntry entry)) return false;
            value = entry.bool_value;
            return true;
        }

        public bool TryGetInt(string key, out int value)
        {
            value = 0;
            if (!TryGet(key, WorldStateValueType.Int, out WorldStateEntry entry)) return false;
            value = entry.int_value;
            return true;
        }

        public bool TryGetFloat(string key, out float value)
        {
            value = 0f;
            if (!TryGet(key, WorldStateValueType.Float, out WorldStateEntry entry)) return false;
            value = entry.float_value;
            return true;
        }

        public bool TryGetString(string key, out string value)
        {
            value = string.Empty;
            if (!TryGet(key, WorldStateValueType.String, out WorldStateEntry entry)) return false;
            value = entry.string_value ?? string.Empty;
            return true;
        }

        public WorldStateSnapshot CaptureSnapshot()
        {
            WorldStateSnapshot snapshot = new WorldStateSnapshot();
            for (int i = 0; i < entries.Count; i++)
            {
                WorldStateEntry entry = entries[i];
                if (entry != null) snapshot.entries.Add(entry.Copy());
            }
            snapshot.entries.Sort((a, b) => string.CompareOrdinal(a.key, b.key));
            return snapshot;
        }

        public void RestoreSnapshot(WorldStateSnapshot snapshot, bool emitSignals = false)
        {
            entries.Clear();
            _index.Clear();

            if (snapshot != null && snapshot.entries != null)
            {
                for (int i = 0; i < snapshot.entries.Count; i++)
                {
                    WorldStateEntry source = snapshot.entries[i];
                    if (source == null || string.IsNullOrWhiteSpace(source.key)) continue;
                    WorldStateEntry copy = source.Copy();
                    copy.key = NormalizeKey(copy.key);
                    if (_index.ContainsKey(copy.key)) continue;
                    entries.Add(copy);
                    _index[copy.key] = copy;
                    if (emitSignals) Publish(copy, "snapshot_restore");
                }
                entries.Sort((a, b) => string.CompareOrdinal(a.key, b.key));
            }

            SnapshotRestored?.Invoke();
        }

        private bool Upsert(WorldStateEntry next, string reason)
        {
            if (next == null || string.IsNullOrWhiteSpace(next.key)) return false;
            next.key = NormalizeKey(next.key);

            WorldStateEntry before = null;
            if (_index.TryGetValue(next.key, out WorldStateEntry existing))
            {
                before = existing.Copy();
                if (Equivalent(existing, next)) return false;
                int index = entries.IndexOf(existing);
                WorldStateEntry replacement = next.Copy();
                if (index >= 0) entries[index] = replacement;
                _index[next.key] = replacement;
                next = replacement;
            }
            else
            {
                WorldStateEntry inserted = next.Copy();
                entries.Add(inserted);
                _index[inserted.key] = inserted;
                next = inserted;
            }

            WorldStateEntry after = next.Copy();
            StateChanged?.Invoke(next.key, before, after);
            Publish(after, reason);
            return true;
        }

        private void Publish(WorldStateEntry entry, string reason)
        {
            if (signals == null) signals = GetComponent<WorldSignalBus>();
            if (signals == null || entry == null) return;

            signals.Publish(
                WorldSignalKind.StateChanged,
                "state.changed",
                subject: entry.key,
                stateKey: entry.key,
                stringValue: entry.type == WorldStateValueType.String ? entry.string_value : entry.type == WorldStateValueType.Bool ? (entry.bool_value ? "true" : "false") : string.Empty,
                intValue: entry.type == WorldStateValueType.Int ? entry.int_value : entry.type == WorldStateValueType.Bool ? (entry.bool_value ? 1 : 0) : 0,
                floatValue: entry.type == WorldStateValueType.Float ? entry.float_value : 0f,
                reason: reason ?? entry.type.ToString());
        }

        private bool TryGet(string key, WorldStateValueType type, out WorldStateEntry entry)
        {
            entry = null;
            if (string.IsNullOrWhiteSpace(key)) return false;
            return _index.TryGetValue(NormalizeKey(key), out entry) && entry != null && entry.type == type;
        }

        private void Reindex()
        {
            _index.Clear();
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                WorldStateEntry entry = entries[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.key))
                {
                    entries.RemoveAt(i);
                    continue;
                }
                entry.key = NormalizeKey(entry.key);
                if (_index.ContainsKey(entry.key))
                {
                    entries.RemoveAt(i);
                    continue;
                }
                _index[entry.key] = entry;
            }
            entries.Sort((a, b) => string.CompareOrdinal(a.key, b.key));
        }

        private static bool Equivalent(WorldStateEntry a, WorldStateEntry b)
        {
            if (a == null || b == null || a.type != b.type) return false;
            switch (a.type)
            {
                case WorldStateValueType.Bool: return a.bool_value == b.bool_value;
                case WorldStateValueType.Int: return a.int_value == b.int_value;
                case WorldStateValueType.Float: return Mathf.Approximately(a.float_value, b.float_value);
                default: return string.Equals(a.string_value ?? string.Empty, b.string_value ?? string.Empty, StringComparison.Ordinal);
            }
        }

        private static string NormalizeKey(string key)
            => string.IsNullOrWhiteSpace(key) ? string.Empty : key.Trim().ToLowerInvariant();
    }
}
