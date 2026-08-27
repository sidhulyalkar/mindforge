using System;

namespace Mindforge.Telemetry
{
    /// <summary>
    /// Process-lifetime identity for one Unity game session. Every local evidence
    /// surface uses this ID so GameMarker logs and the durable session envelope can
    /// be joined exactly rather than by approximate wall-clock timestamps.
    /// Calibration/decoder identities remain separate provenance dimensions.
    /// </summary>
    public static class MindforgeSessionContext
    {
        private static readonly DateTime Started = DateTime.UtcNow;
        private static readonly string Id = Started.ToString("yyyyMMddTHHmmssfffZ");

        public static string GameSessionId => Id;
        public static string StartedUtc => Started.ToString("O");
    }
}
