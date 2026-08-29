using UnityEngine;
using Mindforge.Traversal;

namespace Mindforge.Presentation
{
    /// <summary>
    /// Compact presentation-only mount prompt/status. It reads mounted state but never
    /// samples input, changes movement, spends resources, or touches combat/neural state.
    /// </summary>
    public sealed class HoverbikeHud : MonoBehaviour
    {
        [SerializeField] private GuardianHoverbikeController bike;

        private GUIStyle _promptStyle;
        private GUIStyle _statusStyle;

        private void Awake()
        {
            if (bike == null) bike = GetComponent<GuardianHoverbikeController>();
        }

        private void OnGUI()
        {
            if (bike == null) return;
            EnsureStyles();

            if (bike.Mounted)
            {
                float width = Mathf.Min(430f, Screen.width * 0.44f);
                Rect box = new Rect((Screen.width - width) * 0.5f, Screen.height - 82f, width, 46f);
                GUI.Box(box, GUIContent.none);
                string boost = bike.Boosting ? " · BOOST" : string.Empty;
                GUI.Label(
                    new Rect(box.x + 12f, box.y + 7f, box.width - 24f, 32f),
                    $"PRISM HOVERBIKE · {bike.HorizontalSpeed:0.0} m/s{boost} · E DISMOUNT",
                    _statusStyle);
                return;
            }

            if (!bike.CanMountNearby) return;
            float promptWidth = Mathf.Min(360f, Screen.width * 0.42f);
            Rect prompt = new Rect((Screen.width - promptWidth) * 0.5f, Screen.height - 76f, promptWidth, 38f);
            GUI.Box(prompt, GUIContent.none);
            GUI.Label(
                new Rect(prompt.x + 10f, prompt.y + 4f, prompt.width - 20f, 30f),
                "E · MOUNT PRISM HOVERBIKE",
                _promptStyle);
        }

        private void EnsureStyles()
        {
            if (_promptStyle != null) return;
            _promptStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.76f, 0.96f, 1f) },
            };
            _statusStyle = new GUIStyle(_promptStyle)
            {
                fontSize = 13,
                normal = { textColor = new Color(0.88f, 0.96f, 1f) },
            };
        }
    }
}
