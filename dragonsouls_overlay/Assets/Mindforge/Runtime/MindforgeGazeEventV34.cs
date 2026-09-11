using System;
using UnityEngine;

namespace Mindforge.Chassis
{
    /// <summary>
    /// Derived screen-space gaze sample used by the adaptive tutorial.
    /// This boundary intentionally carries no eye images, scene video, pupil data,
    /// or other vendor-specific biometric payloads.
    /// </summary>
    [Serializable]
    public sealed class MindforgeGazeEventV34
    {
        public string schema = "mindforge.gaze_event.v1";
        public long seq;
        public string source_mode = "";
        public long timestamp_ns;
        [Range(0f, 1f)] public float x;
        [Range(0f, 1f)] public float y;
        [Range(0f, 1f)] public float confidence = 1f;
        public bool fixation;
        public bool worn = true;
        public string coordinate_origin = "top_left";
        public string surface = "screen";

        public bool HasSupportedSchema => schema == "mindforge.gaze_event.v1";
        public bool IsFinite =>
            !float.IsNaN(x) && !float.IsInfinity(x) &&
            !float.IsNaN(y) && !float.IsInfinity(y) &&
            !float.IsNaN(confidence) && !float.IsInfinity(confidence);
        public bool IsInsideSurface => x >= 0f && x <= 1f && y >= 0f && y <= 1f;
        public bool HasSupportedOrigin => coordinate_origin == "top_left" || coordinate_origin == "bottom_left";

        public Vector2 UnityViewportPoint
        {
            get
            {
                float viewportY = coordinate_origin == "top_left" ? 1f - y : y;
                return new Vector2(x, viewportY);
            }
        }

        public bool IsUsable(float minimumConfidence)
        {
            return HasSupportedSchema && IsFinite && IsInsideSurface && HasSupportedOrigin &&
                   worn && confidence >= Mathf.Clamp01(minimumConfidence);
        }
    }
}
