using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Mindforge.World
{
    [Serializable]
    public sealed class PlayerProfileSaveEnvelopeV1
    {
        public string schema = "mindforge.player_profile.v1";
        public string generated_utc;
        public PlayerProgressionSnapshot progression = new PlayerProgressionSnapshot();
        public List<WorldStateEntry> durable_world_facts = new List<WorldStateEntry>();
    }

    /// <summary>
    /// Safe persistent profile layer for V0.5.
    ///
    /// Only durable player progression and explicitly non-physical semantic facts are written
    /// to disk. Encounter completion, boss state, checkpoints and other facts that require a
    /// matching physical-authority restore adapter are intentionally excluded. This prevents
    /// a save from claiming a world state that the concrete scene cannot reconstruct.
    /// </summary>
    [DefaultExecutionOrder(-730)]
    public sealed class PlayerProfileSaveV05 : MonoBehaviour
    {
        [SerializeField] private WorldStateLedger world;
        [SerializeField] private PlayerProgressionLedger progression;
        [SerializeField] private MemoryForgeCheckpoint checkpoint;
        [SerializeField] private WorldSignalBus signals;
        [SerializeField] private bool loadOnStart = true;
        [SerializeField] private bool saveOnQuit = true;
        [SerializeField] private string fileName = "profile-v1.json";

        private bool _loadedOnce;

        public event Action<string> ProfileSaved;
        public event Action<string> ProfileLoaded;
        public event Action<string> ProfileSaveFailed;

        public string LastSavedPath { get; private set; }
        public string LastStatus { get; private set; } = "NOT_LOADED";

        public void ConfigureRuntime(
            WorldStateLedger worldState,
            PlayerProgressionLedger playerProgression,
            MemoryForgeCheckpoint memoryForge,
            WorldSignalBus signalBus)
        {
            Unsubscribe();
            world = worldState;
            progression = playerProgression;
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
            if (loadOnStart && !_loadedOnce)
                LoadNow();
        }

        private void OnDisable() => Unsubscribe();

        private void OnApplicationQuit()
        {
            if (saveOnQuit) SaveNow();
        }

        private void Subscribe()
        {
            if (checkpoint == null) return;
            checkpoint.Rested -= OnCheckpointRested;
            checkpoint.Rested += OnCheckpointRested;
        }

        private void Unsubscribe()
        {
            if (checkpoint != null) checkpoint.Rested -= OnCheckpointRested;
        }

        private void OnCheckpointRested() => SaveNow();

        public bool SaveNow()
        {
            Resolve();
            if (world == null || progression == null)
                return Fail("PROFILE_SAVE_MISSING_FOUNDATION");

            try
            {
                PlayerProfileSaveEnvelopeV1 envelope = new PlayerProfileSaveEnvelopeV1
                {
                    generated_utc = DateTime.UtcNow.ToString("O"),
                    progression = progression.CaptureSnapshot(),
                };

                IReadOnlyList<WorldStateEntry> entries = world.Entries;
                for (int i = 0; i < entries.Count; i++)
                {
                    WorldStateEntry entry = entries[i];
                    if (entry == null || !IsDurableNonPhysicalFact(entry.key)) continue;
                    envelope.durable_world_facts.Add(entry.Copy());
                }
                envelope.durable_world_facts.Sort((a, b) => string.CompareOrdinal(a.key, b.key));

                string path = ResolvePath();
                string directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
                string temp = path + ".tmp";
                File.WriteAllText(temp, JsonUtility.ToJson(envelope, true));
                if (File.Exists(path)) File.Delete(path);
                File.Move(temp, path);

                LastSavedPath = path;
                LastStatus = "PROFILE_SAVED";
                ProfileSaved?.Invoke(path);
                signals?.Publish(
                    WorldSignalKind.Milestone,
                    "profile.saved",
                    subject: "player_profile",
                    intValue: envelope.durable_world_facts.Count,
                    reason: "checkpoint_safe_persistence");
                Debug.Log($"[Mindforge:Save] Profile saved: {path}");
                return true;
            }
            catch (Exception ex)
            {
                return Fail("PROFILE_SAVE_FAILED:" + ex.GetType().Name);
            }
        }

        public bool LoadNow()
        {
            Resolve();
            _loadedOnce = true;
            if (world == null || progression == null)
                return Fail("PROFILE_LOAD_MISSING_FOUNDATION");

            string path = ResolvePath();
            if (!File.Exists(path))
            {
                LastStatus = "NO_PROFILE";
                return false;
            }

            try
            {
                PlayerProfileSaveEnvelopeV1 envelope = JsonUtility.FromJson<PlayerProfileSaveEnvelopeV1>(File.ReadAllText(path));
                if (envelope == null || envelope.schema != "mindforge.player_profile.v1")
                    return Fail("PROFILE_LOAD_SCHEMA_MISMATCH");

                progression.RestoreSnapshot(envelope.progression ?? new PlayerProgressionSnapshot());
                if (envelope.durable_world_facts != null)
                {
                    for (int i = 0; i < envelope.durable_world_facts.Count; i++)
                        ApplyDurableFact(envelope.durable_world_facts[i]);
                }

                LastSavedPath = path;
                LastStatus = "PROFILE_LOADED";
                ProfileLoaded?.Invoke(path);
                signals?.Publish(
                    WorldSignalKind.Milestone,
                    "profile.loaded",
                    subject: "player_profile",
                    intValue: envelope.durable_world_facts != null ? envelope.durable_world_facts.Count : 0,
                    reason: "non_physical_profile_restore");
                Debug.Log($"[Mindforge:Save] Profile loaded: {path}");
                return true;
            }
            catch (Exception ex)
            {
                return Fail("PROFILE_LOAD_FAILED:" + ex.GetType().Name);
            }
        }

        private void ApplyDurableFact(WorldStateEntry entry)
        {
            if (entry == null || !IsDurableNonPhysicalFact(entry.key) || world == null) return;
            switch (entry.type)
            {
                case WorldStateValueType.Bool: world.SetBool(entry.key, entry.bool_value, "profile_restore"); break;
                case WorldStateValueType.Int: world.SetInt(entry.key, entry.int_value, "profile_restore"); break;
                case WorldStateValueType.Float: world.SetFloat(entry.key, entry.float_value, "profile_restore"); break;
                case WorldStateValueType.String: world.SetString(entry.key, entry.string_value, "profile_restore"); break;
            }
        }

        private static bool IsDurableNonPhysicalFact(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return false;
            string normalized = key.Trim().ToLowerInvariant();
            return normalized.StartsWith("story.", StringComparison.Ordinal) ||
                   normalized.StartsWith("profile.", StringComparison.Ordinal);
        }

        private bool Fail(string status)
        {
            LastStatus = status ?? "PROFILE_FAILURE";
            ProfileSaveFailed?.Invoke(LastStatus);
            Debug.LogWarning("[Mindforge:Save] " + LastStatus);
            return false;
        }

        private string ResolvePath()
        {
            string safeName = string.IsNullOrWhiteSpace(fileName) ? "profile-v1.json" : fileName.Trim();
            return Path.Combine(Application.persistentDataPath, "mindforge", safeName);
        }

        private void Resolve()
        {
            if (world == null) world = GetComponent<WorldStateLedger>();
            if (progression == null) progression = GetComponent<PlayerProgressionLedger>();
            if (checkpoint == null) checkpoint = FindObjectOfType<MemoryForgeCheckpoint>(true);
            if (signals == null) signals = GetComponent<WorldSignalBus>();
        }
    }
}
