using System;
using UnityEngine;

namespace Mindforge.Chassis
{
    /// <summary>
    /// The first compatibility seam between the working Dragon Souls chassis and
    /// Mindforge's later BCI semantics. It intentionally owns no combat, movement,
    /// damage, camera, or animation authority yet.
    /// </summary>
    public enum MindforgeIntentV29
    {
        None = 0,
        Sight = 1,
        Guard = 2,
        Concord = 3,
    }

    public readonly struct MindforgeIntentEventV29
    {
        public readonly MindforgeIntentV29 Intent;
        public readonly float Confidence;
        public readonly double Timestamp;
        public readonly string Source;

        public MindforgeIntentEventV29(
            MindforgeIntentV29 intent,
            float confidence,
            double timestamp,
            string source)
        {
            Intent = intent;
            Confidence = Mathf.Clamp01(confidence);
            Timestamp = timestamp;
            Source = string.IsNullOrEmpty(source) ? "unknown" : source;
        }
    }

    public static class MindforgeIntentBusV29
    {
        public static event Action<MindforgeIntentEventV29> IntentPublished;

        public static MindforgeIntentEventV29 Last { get; private set; }

        public static void Publish(
            MindforgeIntentV29 intent,
            float confidence,
            double timestamp,
            string source)
        {
            Last = new MindforgeIntentEventV29(intent, confidence, timestamp, source);
            IntentPublished?.Invoke(Last);
        }

        public static void PublishControllerSimulation(MindforgeIntentV29 intent, float confidence = 1f)
        {
            Publish(intent, confidence, Time.unscaledTimeAsDouble, "controller_simulation");
        }
    }
}
