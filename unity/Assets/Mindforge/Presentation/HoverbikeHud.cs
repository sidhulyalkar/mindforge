using UnityEngine;
using Mindforge.Traversal;

namespace Mindforge.Presentation
{
    /// <summary>
    /// Compact presentation-only mounted status. V0.5 intentionally renders no interaction
    /// prompt here: GuardianInteractionRouterV1 is the single player-facing owner of E offers.
    /// This HUD reads mounted state/speed only and never samples input, changes movement,
    /// spends resources, or touches combat/neural state.
    /// </summary>
    public sealed class HoverbikeHud : MonoBehaviour
    {
        [SerializeField] private GuardianHoverbikeController bike;

        private GUIStyle _statusStyle;

        private void Awake()
        {
            if (bike == null) bike = GetComponent<GuardianHoverbikeController>();
        }

        private void OnGUI()
        {
            if (bike == null || !bike.Mounted) return;
            EnsureStyles();

            float width = Mathf.Min(300f, Screen.width * 0.32f);
            Rect box = new Rect(Screen.width - width - 18f, Screen.height - 78f, width, 38f);
            GUI.Box(box, GUIContent.none);
            string boost = bike.Boosting ? " · BOOST" : string.Empty;
            GUI.Label(
                new Rect(box.x + 12f, box.y + 4f, box.width - 24f, 30f),
                $"PRISM HOVERBIKE · {bike.HorizontalSpeed:0.0} m/s{boost}",
                _statusStyle);
        }

        private void EnsureStyles()
        {
            if (_statusStyle != null) return;
            _statusStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.88f, 0.96f, 1f) },
            };
        }
    }
}
