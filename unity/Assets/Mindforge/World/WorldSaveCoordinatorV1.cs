using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Mindforge.World
{
    [Serializable]
    public sealed class WorldSafeSaveEnvelopeV1
    {
        public const string Schema = "mindforge.world_safe_save.v1";

        public string schema = Schema;
        public string content_revision = "aetheria.v06";
        public string generated_utc;
        public string safe_boundary_id = "checkpoint.memory_forge";
        public List<WorldAuthoritySnapshotV1> authorities = new List<WorldAuthoritySnapshotV1>();
    }

    /// <summary>
    /// Coordinates safe-boundary physical world persistence without becoming scene authority.
    /// Each IWorldSaveAuthorityV1 captures/restores its own state. The coordinator validates a
    /// complete restore plan before mutating anything, resets to safe defaults, then restores
    /// in deterministic order. Unknown saved ids are tolerated for removed content; duplicate
    /// current ids or incompatible known schemas abort the physical restore rather than leave
    /// a half-restored world.
    /// </summary>
    [DefaultExecutionOrder(1500)]
    public sealed class WorldSaveCoordinatorV1 : MonoBehaviour
    {
        [SerializeField] private MemoryForgeCheckpoint checkpoint;
        [SerializeField] private PlayerProfileSaveV05 profile;
        [SerializeField] private WorldSignalBus signals;
        [SerializeField] private bool loadOnStart = true;
        [SerializeField] private bool saveOnQuit = false;
        [SerializeField] private string contentRevision = "aetheria.v06";
        [SerializeField] private string fileName = "world-safe-v1.json";

        private bool _loadedOnce;

        public event Action<string> WorldSaved;
        public event Action<string> WorldLoaded;
        public event Action<string> WorldSaveFailed;

        public string LastPath { get; private set; }
        public string LastStatus { get; private set; } = "NOT_LOADED";

        public void ConfigureRuntime(
            MemoryForgeCheckpoint memoryForge,
            PlayerProfileSaveV05 playerProfile,
            WorldSignalBus signalBus,
            string revision = null)
        {
            Unsubscribe();
            checkpoint = memoryForge;
            profile = playerProfile;
            signals = signalBus;
            if (!string.IsNullOrWhiteSpace(revision)) contentRevision = revision.Trim();
            Subscribe();
        }

        private void OnEnable()
        {
            Resolve();
            Subscribe();
        }

        private IEnumerator Start()
        {
            // Let scene authoring/runtime installers finish their own Awake/OnEnable/Start
            // initialization before physical adapters are asked to restore serialized state.
            yield return null;
            Resolve();
            if (loadOnStart && !_loadedOnce) LoadLatest();
        }

        private void OnDisable() => Unsubscribe();

        private void OnApplicationQuit()
        {
            // Safe world state is normally persisted only at explicit Memory Forge rest.
            // Optional quit saving is available for development, but stays disabled by default.
            if (saveOnQuit && checkpoint != null && checkpoint.CanRestNow)
                SaveAtSafeBoundary("checkpoint.memory_forge");
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

        private void OnCheckpointRested()
            => SaveAtSafeBoundary("checkpoint.memory_forge");

        public bool SaveAtSafeBoundary(string boundaryId)
        {
            Resolve();
            if (checkpoint == null || !checkpoint.Active || checkpoint.RespawnPending)
                return Fail("WORLD_SAVE_NOT_AT_SAFE_BOUNDARY");

            // PlayerProfileSaveV05 remains the durable player-progression authority. Save it
            // first. Cross-file pickup reward receipts make a crash between the two writes
            // duplication-safe when the physical world is later restored.
            profile?.SaveNow();

            if (!TryDiscoverAuthorities(out List<IWorldSaveAuthorityV1> authorities, out string discoveryError))
                return Fail(discoveryError);

            try
            {
                WorldSafeSaveEnvelopeV1 envelope = new WorldSafeSaveEnvelopeV1
                {
                    content_revision = string.IsNullOrWhiteSpace(contentRevision) ? "aetheria.v06" : contentRevision.Trim(),
                    generated_utc = DateTime.UtcNow.ToString("O"),
                    safe_boundary_id = Normalize(boundaryId, "checkpoint.memory_forge"),
                };

                for (int i = 0; i < authorities.Count; i++)
                {
                    IWorldSaveAuthorityV1 authority = authorities[i];
                    WorldAuthoritySnapshotV1 snapshot = authority.CaptureSafeBoundary();
                    if (snapshot == null)
                        return Fail("WORLD_SAVE_NULL_SNAPSHOT:" + authority.AuthorityId);
                    snapshot.authority_id = Normalize(snapshot.authority_id, authority.AuthorityId);
                    snapshot.authority_schema = Normalize(snapshot.authority_schema, authority.AuthoritySchema);
                    snapshot.payload_json = snapshot.payload_json ?? string.Empty;
                    envelope.authorities.Add(snapshot);
                }
                envelope.authorities.Sort((a, b) => string.CompareOrdinal(a.authority_id, b.authority_id));

                string path = ResolvePath();
                WriteAtomicWithBackup(path, JsonUtility.ToJson(envelope, true));
                LastPath = path;
                LastStatus = "WORLD_SAVED";
                WorldSaved?.Invoke(path);
                signals?.Publish(
                    WorldSignalKind.Checkpoint,
                    "world.save.completed",
                    subject: envelope.safe_boundary_id,
                    stringValue: envelope.content_revision,
                    intValue: envelope.authorities.Count,
                    reason: "safe_boundary_physical_snapshot");
                Debug.Log($"[Mindforge:WorldSave] Saved {envelope.authorities.Count} authorities: {path}");
                return true;
            }
            catch (Exception ex)
            {
                return Fail("WORLD_SAVE_FAILED:" + ex.GetType().Name);
            }
        }

        public bool LoadLatest()
        {
            Resolve();
            _loadedOnce = true;
            string path = ResolvePath();
            if (!File.Exists(path))
            {
                LastStatus = "NO_WORLD_SAVE";
                return false;
            }

            // Progression/reward receipts must be present before physical pickups reconcile.
            profile?.LoadNow();

            if (!TryDiscoverAuthorities(out List<IWorldSaveAuthorityV1> authorities, out string discoveryError))
                return Fail(discoveryError);

            try
            {
                WorldSafeSaveEnvelopeV1 envelope = JsonUtility.FromJson<WorldSafeSaveEnvelopeV1>(File.ReadAllText(path));
                if (envelope == null || !string.Equals(envelope.schema, WorldSafeSaveEnvelopeV1.Schema, StringComparison.Ordinal))
                    return Fail("WORLD_LOAD_SCHEMA_MISMATCH");

                Dictionary<string, IWorldSaveAuthorityV1> current = new Dictionary<string, IWorldSaveAuthorityV1>(StringComparer.Ordinal);
                for (int i = 0; i < authorities.Count; i++) current.Add(authorities[i].AuthorityId, authorities[i]);

                Dictionary<string, WorldAuthoritySnapshotV1> saved = new Dictionary<string, WorldAuthoritySnapshotV1>(StringComparer.Ordinal);
                if (envelope.authorities != null)
                {
                    for (int i = 0; i < envelope.authorities.Count; i++)
                    {
                        WorldAuthoritySnapshotV1 snapshot = envelope.authorities[i];
                        if (snapshot == null) continue;
                        string id = Normalize(snapshot.authority_id, string.Empty);
                        if (string.IsNullOrEmpty(id)) return Fail("WORLD_LOAD_EMPTY_AUTHORITY_ID");
                        if (saved.ContainsKey(id)) return Fail("WORLD_LOAD_DUPLICATE_SAVED_ID:" + id);
                        saved.Add(id, snapshot);
                    }
                }

                // Validate every known snapshot before mutating any physical state.
                foreach (KeyValuePair<string, WorldAuthoritySnapshotV1> pair in saved)
                {
                    if (!current.TryGetValue(pair.Key, out IWorldSaveAuthorityV1 authority)) continue;
                    if (!authority.CanRestore(pair.Value))
                    {
                        ResetAll(authorities);
                        return Fail("WORLD_LOAD_INCOMPATIBLE_AUTHORITY:" + pair.Key);
                    }
                }

                ResetAll(authorities);
                List<IWorldSaveAuthorityV1> restorePlan = new List<IWorldSaveAuthorityV1>();
                for (int i = 0; i < authorities.Count; i++)
                    if (saved.ContainsKey(authorities[i].AuthorityId)) restorePlan.Add(authorities[i]);
                restorePlan.Sort(CompareRestoreOrder);

                try
                {
                    for (int i = 0; i < restorePlan.Count; i++)
                    {
                        IWorldSaveAuthorityV1 authority = restorePlan[i];
                        authority.RestoreSafeBoundary(saved[authority.AuthorityId]);
                    }
                }
                catch
                {
                    ResetAll(authorities);
                    throw;
                }

                LastPath = path;
                LastStatus = "WORLD_LOADED";
                WorldLoaded?.Invoke(path);
                signals?.Publish(
                    WorldSignalKind.Checkpoint,
                    "world.load.completed",
                    subject: Normalize(envelope.safe_boundary_id, "checkpoint.memory_forge"),
                    stringValue: envelope.content_revision ?? string.Empty,
                    intValue: restorePlan.Count,
                    reason: "validated_safe_boundary_restore");
                Debug.Log($"[Mindforge:WorldSave] Restored {restorePlan.Count} known authorities from {path}");
                return true;
            }
            catch (Exception ex)
            {
                ResetAll(authorities);
                return Fail("WORLD_LOAD_FAILED:" + ex.GetType().Name);
            }
        }

        private static int CompareRestoreOrder(IWorldSaveAuthorityV1 a, IWorldSaveAuthorityV1 b)
        {
            int order = a.RestoreOrder.CompareTo(b.RestoreOrder);
            return order != 0 ? order : string.CompareOrdinal(a.AuthorityId, b.AuthorityId);
        }

        private static void ResetAll(List<IWorldSaveAuthorityV1> authorities)
        {
            if (authorities == null) return;
            List<IWorldSaveAuthorityV1> ordered = new List<IWorldSaveAuthorityV1>(authorities);
            ordered.Sort(CompareRestoreOrder);
            for (int i = 0; i < ordered.Count; i++)
            {
                try { ordered[i].ResetToSafeDefault(); }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[Mindforge:WorldSave] Safe reset failed for {ordered[i].AuthorityId}: {ex.GetType().Name}");
                }
            }
        }

        private static bool TryDiscoverAuthorities(out List<IWorldSaveAuthorityV1> result, out string error)
        {
            result = new List<IWorldSaveAuthorityV1>();
            error = null;
            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            MonoBehaviour[] all = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (!(all[i] is IWorldSaveAuthorityV1 authority)) continue;
                string id = Normalize(authority.AuthorityId, string.Empty);
                if (string.IsNullOrEmpty(id))
                {
                    error = "WORLD_SAVE_EMPTY_CURRENT_AUTHORITY_ID";
                    return false;
                }
                if (!ids.Add(id))
                {
                    error = "WORLD_SAVE_DUPLICATE_CURRENT_ID:" + id;
                    return false;
                }
                result.Add(authority);
            }
            result.Sort((a, b) => string.CompareOrdinal(a.AuthorityId, b.AuthorityId));
            return true;
        }

        private static void WriteAtomicWithBackup(string path, string json)
        {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            string temp = path + ".tmp";
            string backup = path + ".bak";
            File.WriteAllText(temp, json ?? string.Empty);

            if (!File.Exists(path))
            {
                File.Move(temp, path);
                return;
            }

            try
            {
                File.Replace(temp, path, backup, true);
            }
            catch (PlatformNotSupportedException)
            {
                File.Copy(path, backup, true);
                File.Copy(temp, path, true);
                File.Delete(temp);
            }
            catch (IOException)
            {
                File.Copy(path, backup, true);
                File.Copy(temp, path, true);
                File.Delete(temp);
            }
        }

        private bool Fail(string status)
        {
            LastStatus = status ?? "WORLD_SAVE_FAILURE";
            WorldSaveFailed?.Invoke(LastStatus);
            Debug.LogWarning("[Mindforge:WorldSave] " + LastStatus);
            return false;
        }

        private string ResolvePath()
        {
            string safeName = string.IsNullOrWhiteSpace(fileName) ? "world-safe-v1.json" : fileName.Trim();
            return Path.Combine(Application.persistentDataPath, "mindforge", safeName);
        }

        private void Resolve()
        {
            if (checkpoint == null) checkpoint = FindObjectOfType<MemoryForgeCheckpoint>(true);
            if (profile == null) profile = FindObjectOfType<PlayerProfileSaveV05>(true);
            if (signals == null) signals = FindObjectOfType<WorldSignalBus>(true);
        }

        private static string Normalize(string value, string fallback)
            => string.IsNullOrWhiteSpace(value) ? (fallback ?? string.Empty) : value.Trim().ToLowerInvariant();
    }
}
