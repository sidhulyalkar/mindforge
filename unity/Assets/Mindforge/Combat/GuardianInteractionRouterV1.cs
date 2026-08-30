using System;
using UnityEngine;
using Mindforge.Traversal;
using Mindforge.World;

namespace Mindforge.Combat
{
    /// <summary>
    /// One contextual conventional interaction surface for the Guardian.
    ///
    /// The router owns selection + the explicit E edge, not the gameplay behind an offer.
    /// Hoverbike mounting remains in GuardianHoverbikeController; checkpoint reconstruction
    /// remains in MemoryForgeCheckpoint; future doors/NPCs/loot remain in their own sources.
    ///
    /// V0.5 records context_down in tape V5. Pre-V5 replay tapes retain their historical
    /// mount_toggle_down semantics only when a bike mount/dismount offer is actually focused,
    /// preventing old E presses from acquiring newly-authored world interaction meanings.
    /// </summary>
    [DefaultExecutionOrder(-120)]
    public sealed class GuardianInteractionRouterV1 : MonoBehaviour
    {
        [SerializeField] private GuardianControlProfileV1 controls;
        [SerializeField] private GuardianInputTape inputTape;
        [SerializeField] private GuardianHoverbikeController hoverbike;
        [SerializeField] private GuardianSwordShieldController bladeCombat;
        [SerializeField] private WorldSignalBus signals;
        [SerializeField] private Camera cameraReference;
        [SerializeField, Min(1f)] private float discoveryRadius = 4.25f;

        private bool _interactLatched;
        private WorldInteractionSourceV1 _focusedSource;
        private string _focusedId = string.Empty;
        private string _focusedPrompt = string.Empty;
        private GUIStyle _promptStyle;
        private GUIStyle _keyStyle;
        private GUIStyle _hintStyle;

        public event Action<string> FocusChanged;
        public event Action<string> InteractionPerformed;

        public string FocusedInteractionId => _focusedId;
        public string FocusedPrompt => _focusedPrompt;
        public bool HasOffer => !string.IsNullOrEmpty(_focusedPrompt);

        private long FixedTick => GuardianInputTape.FixedTickNow;

        private void Awake()
        {
            Resolve();
            hoverbike?.SetContextInteractionOwned(true);
        }

        private void OnEnable()
        {
            Resolve();
            hoverbike?.SetContextInteractionOwned(true);
        }

        private void OnDisable()
        {
            _interactLatched = false;
            if (hoverbike != null) hoverbike.SetContextInteractionOwned(false);
        }

        private void Update()
        {
            Resolve();
            ResolveOffer();
            if (controls != null)
                _interactLatched |= controls.Pressed(GuardianControlAction.Interact);
        }

        private void FixedUpdate()
        {
            Resolve();
            GuardianCommandFrame live = new GuardianCommandFrame
            {
                tick = FixedTick,
                context_down = _interactLatched,
            };
            _interactLatched = false;

            int fixedHz = Mathf.Max(1, Mathf.RoundToInt(1f / Mathf.Max(0.0001f, Time.fixedDeltaTime)));
            GuardianCommandFrame command = inputTape != null ? inputTape.Resolve(live, fixedHz) : live;
            if (command == null) return;

            bool contextEdge = command.context_down;
            if (!contextEdge && inputTape != null && inputTape.IsLegacyPreContextReplay &&
                IsFocusedBikeInteraction())
                contextEdge = command.mount_toggle_down;

            if (contextEdge) ExecuteFocusedInteraction();
        }

        private bool IsFocusedBikeInteraction()
            => string.Equals(_focusedId, "vehicle.prism_hoverbike.mount", StringComparison.Ordinal) ||
               string.Equals(_focusedId, "vehicle.prism_hoverbike.dismount", StringComparison.Ordinal);

        private void ResolveOffer()
        {
            string nextId = string.Empty;
            string nextPrompt = string.Empty;
            WorldInteractionSourceV1 nextSource = null;

            // Dismount is the unambiguous context while riding. Parked bikes participate in
            // the same priority/distance/view-angle registry as every other world offer.
            if (hoverbike != null && hoverbike.Mounted)
            {
                nextId = "vehicle.prism_hoverbike.dismount";
                nextPrompt = "Dismount Prism Hoverbike";
            }
            else
            {
                nextSource = WorldInteractionSourceV1.FindBest(
                    transform,
                    cameraReference != null ? cameraReference : Camera.main,
                    Mathf.Max(1f, discoveryRadius),
                    out _);
                if (nextSource != null)
                {
                    nextId = nextSource.InteractionId ?? string.Empty;
                    nextPrompt = nextSource.Prompt ?? string.Empty;
                }
            }

            _focusedSource = nextSource;
            if (string.Equals(_focusedId, nextId, StringComparison.Ordinal) &&
                string.Equals(_focusedPrompt, nextPrompt, StringComparison.Ordinal))
                return;

            _focusedId = nextId;
            _focusedPrompt = nextPrompt;
            FocusChanged?.Invoke(_focusedId);
        }

        private void ExecuteFocusedInteraction()
        {
            ResolveOffer();
            if (bladeCombat != null && bladeCombat.ActionState != GuardianActionState.Locomotion)
                return;

            bool accepted = false;
            string id = _focusedId;

            if (hoverbike != null && hoverbike.Mounted)
            {
                hoverbike.RequestDismount(false);
                accepted = true;
                id = "vehicle.prism_hoverbike.dismount";
            }
            else if (_focusedSource != null)
            {
                accepted = _focusedSource.TryInteract(transform);
                id = _focusedSource.InteractionId;
            }

            if (!accepted || string.IsNullOrWhiteSpace(id)) return;
            InteractionPerformed?.Invoke(id);
            signals?.Publish(
                WorldSignalKind.Interaction,
                "interaction.performed",
                subject: id,
                stringValue: id,
                intValue: 1,
                floatValue: 1f,
                reason: "conventional_context_action");
            ResolveOffer();
        }

        private void OnGUI()
        {
            Resolve();
            ResolveOffer();
            if (!HasOffer) return;
            EnsureStyles();

            string key = controls != null ? controls.Label(GuardianControlAction.Interact) : "E";
            float width = Mathf.Min(430f, Screen.width - 40f);
            float height = 48f;
            float x = (Screen.width - width) * 0.5f;
            float y = Screen.height - 132f;

            Color before = GUI.color;
            GUI.color = new Color(0.025f, 0.035f, 0.055f, 0.92f);
            GUI.DrawTexture(new Rect(x, y, width, height), Texture2D.whiteTexture);
            GUI.color = new Color(0.20f, 0.78f, 1f, 0.92f);
            GUI.DrawTexture(new Rect(x, y, 4f, height), Texture2D.whiteTexture);
            GUI.color = before;

            GUI.Label(new Rect(x + 14f, y + 7f, 64f, 28f), key, _keyStyle);
            GUI.Label(new Rect(x + 78f, y + 5f, width - 94f, 24f), _focusedPrompt, _promptStyle);
            GUI.Label(new Rect(x + 78f, y + 26f, width - 94f, 16f), "CONTEXT ACTION", _hintStyle);
        }

        private void EnsureStyles()
        {
            if (_promptStyle == null)
            {
                _promptStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleLeft,
                    fontSize = 15,
                    fontStyle = FontStyle.Bold,
                };
                _promptStyle.normal.textColor = new Color(0.94f, 0.97f, 1f, 1f);
            }
            if (_keyStyle == null)
            {
                _keyStyle = new GUIStyle(_promptStyle)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 17,
                };
                _keyStyle.normal.textColor = new Color(0.30f, 0.88f, 1f, 1f);
            }
            if (_hintStyle == null)
            {
                _hintStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.UpperLeft,
                    fontSize = 9,
                    fontStyle = FontStyle.Bold,
                };
                _hintStyle.normal.textColor = new Color(0.54f, 0.64f, 0.76f, 0.95f);
            }
        }

        private void Resolve()
        {
            if (controls == null) controls = GuardianControlProfileV1.ResolveOrCreate();
            if (inputTape == null) inputTape = FindObjectOfType<GuardianInputTape>(true);
            if (hoverbike == null) hoverbike = GetComponent<GuardianHoverbikeController>();
            if (bladeCombat == null) bladeCombat = GetComponent<GuardianSwordShieldController>();
            if (signals == null) signals = FindObjectOfType<WorldSignalBus>(true);
            if (cameraReference == null) cameraReference = Camera.main;
            if (hoverbike != null && !hoverbike.ContextInteractionOwned)
                hoverbike.SetContextInteractionOwned(true);
        }
    }
}
