using System;
using System.IO;
using UnityEngine;

namespace Mindforge.Chassis
{
    /// <summary>
    /// Reads the bootstrap-sealed Mindforge source identity from the materialized
    /// Dragon Souls project. Native qualification may execute without this record,
    /// but promotion evidence is clean only when it exists and the source worktree
    /// was clean when the overlay was applied.
    /// </summary>
    [DefaultExecutionOrder(900)]
    [DisallowMultipleComponent]
    public sealed class MindforgeNativeProvenanceV33 : MonoBehaviour
    {
        public const string Schema = "mindforge.overlay_provenance.v1";

        [Serializable]
        private sealed class OverlayRecord
        {
            public string schema;
            public string mindforge_source_commit;
            public bool mindforge_worktree_dirty;
            public bool overlay_applied;
            public string upstream_source_commit;
            public string unity_version;
            public string overlay_source_path;
            public string generated_utc;
        }

        private OverlayRecord _record;

        public bool IsAvailable { get; private set; }
        public bool IsCleanSource => IsAvailable && _record != null && _record.overlay_applied &&
                                     !_record.mindforge_worktree_dirty &&
                                     !string.IsNullOrEmpty(_record.mindforge_source_commit);
        public bool WorktreeDirty => _record != null && _record.mindforge_worktree_dirty;
        public bool OverlayApplied => _record != null && _record.overlay_applied;
        public string SourceCommit => _record != null ? _record.mindforge_source_commit : null;
        public string UpstreamCommit => _record != null ? _record.upstream_source_commit : null;
        public string RecordedUnityVersion => _record != null ? _record.unity_version : null;
        public string RecordPath { get; private set; }
        public string LoadError { get; private set; }
        public string ShortCommit
        {
            get
            {
                string value = SourceCommit;
                if (string.IsNullOrEmpty(value)) return "unknown";
                return value.Length <= 8 ? value : value.Substring(0, 8);
            }
        }

        private void Awake()
        {
            Reload();
        }

        public void Reload()
        {
            IsAvailable = false;
            LoadError = null;
            _record = null;
            RecordPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".mindforge_overlay.json"));

            try
            {
                if (!File.Exists(RecordPath))
                {
                    LoadError = "overlay_provenance_missing";
                    return;
                }

                string raw = File.ReadAllText(RecordPath);
                OverlayRecord parsed = JsonUtility.FromJson<OverlayRecord>(raw);
                if (parsed == null || !string.Equals(parsed.schema, Schema, StringComparison.Ordinal))
                {
                    LoadError = "overlay_provenance_schema_invalid";
                    return;
                }
                if (string.IsNullOrEmpty(parsed.mindforge_source_commit))
                {
                    LoadError = "overlay_source_commit_missing";
                    return;
                }

                _record = parsed;
                IsAvailable = true;
            }
            catch (Exception ex)
            {
                LoadError = "overlay_provenance_read_failed:" + ex.GetType().Name;
            }
        }
    }
}
