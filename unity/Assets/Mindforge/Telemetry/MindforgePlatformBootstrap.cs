using UnityEngine;

namespace Mindforge.Telemetry
{
    /// <summary>
    /// Installs the non-authoritative GameMarker transport in any playable scene.
    /// Idempotent by design so generated competition scenes and future hand-authored
    /// scenes share the same observable contract without serialized wiring drift.
    /// </summary>
    public static class MindforgePlatformBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            UdpGameMarkerSender sender = Object.FindObjectOfType<UdpGameMarkerSender>();
            MindforgeGameMarkerBridge bridge = Object.FindObjectOfType<MindforgeGameMarkerBridge>();
            if (sender != null && bridge != null) return;

            GameObject root = GameObject.Find("MindforgeBCIPlatform");
            if (root == null)
                root = new GameObject("MindforgeBCIPlatform");

            if (sender == null) sender = root.AddComponent<UdpGameMarkerSender>();
            if (bridge == null) bridge = root.AddComponent<MindforgeGameMarkerBridge>();
        }
    }
}
