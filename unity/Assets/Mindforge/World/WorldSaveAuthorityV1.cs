using System;

namespace Mindforge.World
{
    [Serializable]
    public sealed class WorldAuthoritySnapshotV1
    {
        public string authority_id;
        public string authority_schema;
        public string payload_json;
    }

    /// <summary>
    /// Explicit safe-boundary persistence contract for concrete physical world authorities.
    /// Implementations own capture/restore of their own physical state; the coordinator only
    /// orders and transports snapshots. No implementation may infer BCI state from a save.
    /// </summary>
    public interface IWorldSaveAuthorityV1
    {
        string AuthorityId { get; }
        string AuthoritySchema { get; }
        int RestoreOrder { get; }

        WorldAuthoritySnapshotV1 CaptureSafeBoundary();
        bool CanRestore(WorldAuthoritySnapshotV1 snapshot);
        void RestoreSafeBoundary(WorldAuthoritySnapshotV1 snapshot);
        void ResetToSafeDefault();
    }
}
