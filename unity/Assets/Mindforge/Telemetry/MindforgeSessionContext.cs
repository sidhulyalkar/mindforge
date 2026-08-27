using System;
using UnityEngine;

namespace Mindforge.Telemetry
{
    /// <summary>
    /// Identity for one Unity runtime/play session. Every local evidence surface uses
    /// this ID so GameMarker logs and durable session envelopes can be joined exactly
    /// rather than by approximate timestamps.
    ///
    /// SubsystemRegistration intentionally resets the identity on every Play entry,
    /// including Editor configurations where domain reload is disabled. A standalone
    /// player still receives one identity for its runtime process.
    /// </summary>
    public static class MindforgeSessionContext
    {
        private static DateTime _started;
        private static string _id;

        static MindforgeSessionContext()
        {
            ResetIdentity();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForRuntime()
        {
            ResetIdentity();
        }

        private static void ResetIdentity()
        {
            _started = DateTime.UtcNow;
            _id = $"{_started:yyyyMMddTHHmmssfffZ}-{Guid.NewGuid():N}";
        }

        public static string GameSessionId => _id;
        public static string StartedUtc => _started.ToString("O");
    }
}
