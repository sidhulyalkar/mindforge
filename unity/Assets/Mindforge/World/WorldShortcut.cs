using System;
using UnityEngine;
using Mindforge.Journey;
using Mindforge.Telemetry;

namespace Mindforge.World
{
    /// <summary>
    /// Conventional world interaction that permanently opens one traversal shortcut for
    /// the current run. It has no neural, combat, targeting or calibration authority.
    /// </summary>
    public sealed class WorldShortcut : MonoBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] private Transform interactionPoint;
        [SerializeField] private JourneyGate gate;
        [SerializeField] private KeyCode interactKey = KeyCode.G;
        [SerializeField] private float interactionRadius = 2.2f;
        [SerializeField] private string shortcutId = "memory_forge_market_loop";
        [SerializeField] private UdpGameMarkerSender markers;

        private bool _unlocked;
        private GUIStyle _promptStyle;

        public event Action<string> Unlocked;
        public bool IsUnlocked => _unlocked;
        public string ShortcutId => shortcutId;

        public void ConfigureRuntime(
            Transform guardian,
            Transform point,
            JourneyGate shortcutGate,
            string id,
            UdpGameMarkerSender markerSender = null)
        {
            player = guardian;
            interactionPoint = point;
            gate = shortcutGate;
            shortcutId = string.IsNullOrWhiteSpace(id) ? "shortcut" : id;
            markers = markerSender;
        }

        private void Start()
        {
            gate?.SetOpen(_unlocked, true);
        }

        private void Update()
        {
            if (_unlocked || !PlayerInRange()) return;
            if (Input.GetKeyDown(interactKey)) Unlock();
        }

        public bool Unlock()
        {
            if (_unlocked) return false;
            _unlocked = true;
            gate?.SetOpen(true);
            markers?.Emit("SHORTCUT_UNLOCKED", "world", target: shortcutId, reason: "CONVENTIONAL_INTERACTION");
            Unlocked?.Invoke(shortcutId);
            return true;
        }

        private bool PlayerInRange()
        {
            if (player == null || interactionPoint == null) return false;
            Vector3 delta = Vector3.ProjectOnPlane(player.position - interactionPoint.position, Vector3.up);
            float radius = Mathf.Max(0.5f, interactionRadius);
            return delta.sqrMagnitude <= radius * radius;
        }

        private void OnGUI()
        {
            if (_unlocked || !PlayerInRange()) return;
            if (_promptStyle == null)
            {
                _promptStyle = new GUIStyle(GUI.skin.box)
                {
                    fontSize = 13,
                    alignment = TextAnchor.MiddleCenter,
                };
            }
            const float width = 310f;
            GUI.Label(
                new Rect((Screen.width - width) * 0.5f, Screen.height - 92f, width, 34f),
                $"{interactKey}  OPEN MEMORY CONDUIT",
                _promptStyle);
        }
    }
}
