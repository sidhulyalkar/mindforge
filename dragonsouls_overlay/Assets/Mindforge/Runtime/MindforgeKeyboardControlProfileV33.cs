using System;
using System.Collections.Generic;
using System.Reflection;
using Cinemachine;
using Inputs;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Mindforge.Chassis
{
    /// <summary>
    /// Binding-only desktop/laptop control profile for native Mindforge development.
    ///
    /// The pinned Dragon Souls controller asset already provides WASD movement, but its
    /// camera and combat bindings are predominantly gamepad-oriented. This adapter adds
    /// a small keyboard profile to the live InputAction instances without invoking combat
    /// events, moving the player, changing Cinemachine priorities, or editing upstream
    /// Controller.inputactions/Controllers.cs on disk.
    ///
    /// Contract:
    ///   WASD       -> movement
    ///   Arrow keys -> camera/look
    ///   Space      -> light attack
    ///
    /// Existing gamepad/mouse bindings are preserved. Space is removed from the upstream
    /// Jump keyboard binding so one key press has one gameplay meaning in the profile.
    /// </summary>
    [DefaultExecutionOrder(1160)]
    [DisallowMultipleComponent]
    public sealed class MindforgeKeyboardControlProfileV33 : MonoBehaviour
    {
        public const string MovementContract = "WASD";
        public const string CameraContract = "ARROW_KEYS";
        public const string AttackContract = "SPACE_LIGHT_ATTACK";

        private const string ControlsFieldName = "_controls";
        private const string SpacePath = "<Keyboard>/space";
        private const string UpArrowPath = "<Keyboard>/upArrow";
        private const string DownArrowPath = "<Keyboard>/downArrow";
        private const string LeftArrowPath = "<Keyboard>/leftArrow";
        private const string RightArrowPath = "<Keyboard>/rightArrow";
        private const string WPath = "<Keyboard>/w";
        private const string SPath = "<Keyboard>/s";
        private const string APath = "<Keyboard>/a";
        private const string DPath = "<Keyboard>/d";

        [SerializeField] private float retrySeconds = 0.25f;
        [SerializeField] private float retryWindowSeconds = 3f;

        private float _startedAt;
        private float _nextRetryAt;
        private bool _loggedSuccess;

        public bool Installed { get; private set; }
        public bool WasdMovementBound { get; private set; }
        public bool ArrowCameraBound { get; private set; }
        public bool SpaceAttackBound { get; private set; }
        public bool UpstreamJumpSpaceRemoved { get; private set; }
        public int CinemachineCameraActionCount { get; private set; }
        public string Status { get; private set; } = "unresolved";

        private void Awake()
        {
            _startedAt = Time.unscaledTime;
            TryInstall();
        }

        private void Start()
        {
            if (!Installed) TryInstall();
        }

        private void LateUpdate()
        {
            if (Installed) return;
            if (Time.unscaledTime - _startedAt > retryWindowSeconds) return;
            if (Time.unscaledTime < _nextRetryAt) return;

            _nextRetryAt = Time.unscaledTime + retrySeconds;
            TryInstall();
        }

        private void TryInstall()
        {
            InputReader reader = FindObjectOfType<InputReader>(true);
            if (reader == null)
            {
                Status = "input_reader_unresolved";
                return;
            }

            FieldInfo controlsField = typeof(InputReader).GetField(
                ControlsFieldName,
                BindingFlags.Instance | BindingFlags.NonPublic
            );
            if (controlsField == null)
            {
                Status = "pinned_input_contract_drift:_controls_missing";
                return;
            }

            Controllers controls = controlsField.GetValue(reader) as Controllers;
            if (controls == null)
            {
                Status = "controllers_instance_unresolved";
                return;
            }

            Controllers.PlayerActions player = controls.Player;
            InputActionMap playerMap = player.Get();
            bool playerMapWasEnabled = playerMap.enabled;
            if (playerMapWasEnabled) playerMap.Disable();

            try
            {
                int removedJumpBindings = RemoveBindingsForPath(player.Jump, SpacePath);
                UpstreamJumpSpaceRemoved = removedJumpBindings > 0 || !HasDirectBinding(player.Jump, SpacePath);

                SpaceAttackBound = EnsureDirectBinding(player.LightAttack, SpacePath);
                WasdMovementBound = EnsureDirectionalComposite(
                    player.Move,
                    WPath,
                    SPath,
                    APath,
                    DPath
                );
                bool readerCameraBound = EnsureDirectionalComposite(
                    player.Camera,
                    UpArrowPath,
                    DownArrowPath,
                    LeftArrowPath,
                    RightArrowPath
                );

                CinemachineCameraActionCount = InstallCinemachineArrowBindings();
                ArrowCameraBound = readerCameraBound && CinemachineCameraActionCount > 0;
            }
            finally
            {
                if (playerMapWasEnabled) playerMap.Enable();
            }

            Installed =
                WasdMovementBound &&
                ArrowCameraBound &&
                SpaceAttackBound &&
                UpstreamJumpSpaceRemoved;

            Status = Installed
                ? $"ready; cinemachine_actions={CinemachineCameraActionCount}"
                : $"incomplete; wasd={WasdMovementBound} arrows={ArrowCameraBound} " +
                  $"space_attack={SpaceAttackBound} jump_space_removed={UpstreamJumpSpaceRemoved} " +
                  $"cinemachine_actions={CinemachineCameraActionCount}";

            if (Installed && !_loggedSuccess)
            {
                _loggedSuccess = true;
                Debug.Log(
                    "[Mindforge:V33:INPUT] Laptop keyboard profile ready: " +
                    "WASD=move, Arrow Keys=view, Space=light attack. " +
                    $"Cinemachine actions patched={CinemachineCameraActionCount}; " +
                    "existing gamepad/mouse bindings preserved."
                );
            }
        }

        private static int InstallCinemachineArrowBindings()
        {
            CinemachineInputProvider[] providers = FindObjectsOfType<CinemachineInputProvider>(true);
            HashSet<Guid> visitedActions = new HashSet<Guid>();
            int resolved = 0;

            for (int i = 0; i < providers.Length; i++)
            {
                CinemachineInputProvider provider = providers[i];
                if (provider == null || provider.XYAxis == null || provider.XYAxis.action == null)
                    continue;

                InputAction action = provider.XYAxis.action;
                if (!visitedActions.Add(action.id)) continue;

                bool wasEnabled = action.enabled;
                if (wasEnabled) action.Disable();
                try
                {
                    if (EnsureDirectionalComposite(
                        action,
                        UpArrowPath,
                        DownArrowPath,
                        LeftArrowPath,
                        RightArrowPath
                    ))
                    {
                        resolved++;
                    }
                }
                finally
                {
                    if (wasEnabled) action.Enable();
                }
            }

            return resolved;
        }

        private static bool EnsureDirectBinding(InputAction action, string path)
        {
            if (action == null) return false;
            if (!HasDirectBinding(action, path)) action.AddBinding(path);
            return HasDirectBinding(action, path);
        }

        private static bool HasDirectBinding(InputAction action, string path)
        {
            if (action == null) return false;
            for (int i = 0; i < action.bindings.Count; i++)
            {
                InputBinding binding = action.bindings[i];
                if (binding.isComposite || binding.isPartOfComposite) continue;
                if (PathEquals(binding.path, path)) return true;
            }
            return false;
        }

        private static bool EnsureDirectionalComposite(
            InputAction action,
            string up,
            string down,
            string left,
            string right
        )
        {
            if (action == null) return false;
            if (HasCompositeParts(action, up, down, left, right)) return true;

            action.AddCompositeBinding("2DVector")
                .With("Up", up)
                .With("Down", down)
                .With("Left", left)
                .With("Right", right);

            return HasCompositeParts(action, up, down, left, right);
        }

        private static bool HasCompositeParts(
            InputAction action,
            string up,
            string down,
            string left,
            string right
        )
        {
            bool hasUp = false;
            bool hasDown = false;
            bool hasLeft = false;
            bool hasRight = false;

            for (int i = 0; i < action.bindings.Count; i++)
            {
                InputBinding binding = action.bindings[i];
                if (!binding.isPartOfComposite) continue;

                hasUp |= PathEquals(binding.path, up);
                hasDown |= PathEquals(binding.path, down);
                hasLeft |= PathEquals(binding.path, left);
                hasRight |= PathEquals(binding.path, right);
            }

            return hasUp && hasDown && hasLeft && hasRight;
        }

        private static int RemoveBindingsForPath(InputAction action, string path)
        {
            if (action == null) return 0;
            int removed = 0;

            for (int i = action.bindings.Count - 1; i >= 0; i--)
            {
                InputBinding binding = action.bindings[i];
                if (binding.isComposite || binding.isPartOfComposite) continue;
                if (!PathEquals(binding.path, path)) continue;

                action.ChangeBinding(i).Erase();
                removed++;
            }

            return removed;
        }

        private static bool PathEquals(string a, string b)
        {
            return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        }
    }
}
