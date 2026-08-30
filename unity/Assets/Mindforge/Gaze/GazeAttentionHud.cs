using UnityEngine;

namespace Mindforge.Gaze
{
    /// <summary>
    /// Development-only evidence surface for hardware bring-up. It intentionally renders
    /// attention state without issuing gameplay commands.
    /// </summary>
    public sealed class GazeAttentionHud : MonoBehaviour
    {
        [SerializeField] private GazeAttentionRouter attention;
        [SerializeField] private UdpGazeReceiver receiver;
        [SerializeField] private bool showInReleaseBuilds;

        private GUIStyle _labelStyle;

        private void Awake()
        {
            if (attention == null) attention = FindObjectOfType<GazeAttentionRouter>();
            if (receiver == null) receiver = FindObjectOfType<UdpGazeReceiver>();
        }

        private void OnGUI()
        {
            if (!showInReleaseBuilds && !Application.isEditor && !Debug.isDebugBuild) return;
            if (attention == null || receiver == null || !receiver.IsConnected || !attention.IsFresh) return;

            if (_labelStyle == null)
            {
                _labelStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.UpperRight,
                    fontSize = 12
                };
            }

            Transform target = attention.SuggestedCombatTarget;
            string targetName = target != null ? target.name : "scan";
            string fixation = attention.LastFixation ? "FIX" : "GAZE";
            GUI.Label(
                new Rect(Mathf.Max(8f, Screen.width - 330f), 8f, 320f, 24f),
                $"{fixation} · {attention.LastSourceMode} · {targetName} · {attention.LastConfidence:0.00}",
                _labelStyle);

            Vector2 p = attention.LastViewportPoint;
            float px = p.x * Screen.width;
            float py = (1f - p.y) * Screen.height;
            GUI.Box(new Rect(px - 2f, py - 8f, 4f, 16f), GUIContent.none);
            GUI.Box(new Rect(px - 8f, py - 2f, 16f, 4f), GUIContent.none);
        }
    }
}
