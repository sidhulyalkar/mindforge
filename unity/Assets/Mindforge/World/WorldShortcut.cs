using System;
using UnityEngine;
using Mindforge.Combat;
using Mindforge.Journey;
using Mindforge.Telemetry;

namespace Mindforge.World
{
    /// <summary>
    /// Conventional world interaction that permanently opens one traversal shortcut for
    /// the current run. V0.6 can delegate its input/prompt to the shared contextual router
    /// while this component remains the physical shortcut authority and telemetry source.
    /// </summary>
    public sealed class WorldShortcut : MonoBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] private GuardianSwordShieldController combatState;
        [SerializeField] private Transform interactionPoint;
        [SerializeField] private JourneyGate gate;
        [SerializeField] private KeyCode interactKey = KeyCode.G;
        [SerializeField] private float interactionRadius = 2.2f;
        [SerializeField] private string shortcutId = "memory_forge_market_loop";
        [SerializeField] private UdpGameMarkerSender markers;

        private bool _unlocked;
        private bool _externalInteractionOwned;
        private GUIStyle _promptStyle;

        public event Action<string> Unlocked;
        public bool IsUnlocked => _unlocked;
        public string ShortcutId => shortcutId;
        public bool ExternalInteractionOwned => _externalInteractionOwned;
        public Transform InteractionPoint => interactionPoint != null ? interactionPoint : transform;
        public float InteractionRadius => Mathf.Max(0.5f, interactionRadius);
        public bool CanUnlockNow => !_unlocked && PlayerInRange() && CanInteract();

        public void ConfigureRuntime(
            Transform guardian,
            Transform point,
            JourneyGate shortcutGate,
            string id,
            UdpGameMarkerSender markerSender = null)
        {
            player = guardian;
            combatState = guardian != null ? guardian.GetComponent<GuardianSwordShieldController>() : null;
            interactionPoint = point;
            gate = shortcutGate;
            shortcutId = string.IsNullOrWhiteSpace(id) ? "shortcut" : id;
            markers = markerSender;
        }

        public void SetExternalInteractionOwned(bool owned) => _externalInteractionOwned = owned;

        private void Start()
        {
            Resolve();
            gate?.SetOpen(_unlocked, true);
        }

        private void Update()
        {
            if (_externalInteractionOwned || !CanUnlockNow) return;
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

        public void RestoreUnlocked(bool unlocked, bool immediate = true)
        {
            _unlocked = unlocked;
            gate?.SetOpen(unlocked, immediate);
        }

        private bool PlayerInRange()
        {
            if (player == null || InteractionPoint == null) return false;
            Vector3 delta = Vector3.ProjectOnPlane(player.position - InteractionPoint.position, Vector3.up);
            float radius = InteractionRadius;
            return delta.sqrMagnitude <= radius * radius;
        }

        private bool CanInteract()
            => combatState == null || combatState.ActionState == GuardianActionState.Locomotion;

        private void Resolve()
        {
            if (player != null && combatState == null)
                combatState = player.GetComponent<GuardianSwordShieldController>();
        }

        private void OnGUI()
        {
            if (_externalInteractionOwned || !CanUnlockNow) return;
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
