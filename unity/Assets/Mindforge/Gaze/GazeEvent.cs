using System;
using UnityEngine;

namespace Mindforge.Gaze
{
    /// <summary>
    /// Gameplay-facing gaze sample. This is intentionally a derived spatial signal,
    /// not a transport for eye images, pupil video, or vendor-specific biometric data.
    /// </summary>
    [Serializable]
    public sealed class GazeEvent
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
        public bool IsTopLeftOrigin => coordinate_origin == "top_left";

        public bool IsFinite =>
            !float.IsNaN(x) && !float.IsInfinity(x) &&
            !float.IsNaN(y) && !float.IsInfinity(y) &&
            !float.IsNaN(confidence) && !float.IsInfinity(confidence);

        public bool IsInsideSurface => x >= 0f && x <= 1f && y >= 0f && y <= 1f;

        public Vector2 UnityViewportPoint
        {
            get
            {
                float viewportY = IsTopLeftOrigin ? 1f - y : y;
                return new Vector2(x, viewportY);
            }
        }

        public bool IsUsable(float minimumConfidence)
        {
            return HasSupportedSchema &&
                   IsFinite &&
                   IsInsideSurface &&
                   worn &&
                   confidence >= Mathf.Clamp01(minimumConfidence) &&
                   (coordinate_origin == "top_left" || coordinate_origin == "bottom_left");
        }
    }
}
