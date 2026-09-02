using System.Reflection;
using Inputs;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Mindforge.Chassis
{
    /// <summary>
    /// Completes the pinned Dragon Souls Mouse+Keyboard scheme at runtime without
    /// modifying the upstream Controller.inputactions asset.
    ///
    /// The upstream InputReader owns a generated Controllers instance privately.
    /// V0.31 resolves that one instance, adds missing desktop bindings to its existing
    /// InputActions, then immediately returns authority to InputReader. All resulting
    /// actions still flow through the original InputReader events and player states.
    /// This component never invokes attacks, changes state, moves the player, or deals
    /// damage directly.
    /// </summary>
    [DefaultExecutionOrder(-120)]
    [DisallowMultipleComponent]
    public sealed class MindforgeDesktopCombatBindingsV31 : MonoBehaviour
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        private InputReader _inputReader;
        private global::Controllers _controls;

        public bool Installed { get; private set; }
        public bool DesktopCombatReady { get; private set; }
        public int BindingsAdded { get; private set; }
        public string BindingSummary =>
            "LMB light | RMB heavy | MMB target | Q aim | R recall | X sheath | Shift sprint | Alt roll | H heal | E forge | Esc pause";

        private void Start()
        {
            _inputReader = FindObjectOfType<InputReader>();
            if (_inputReader == null)
            {
                Debug.LogError("[Mindforge:V31] Desktop combat bindings could not find the inherited InputReader.");
                enabled = false;
                return;
            }

            FieldInfo controlsField = typeof(InputReader).GetField("_controls", PrivateInstance);
            if (controlsField == null)
            {
                Debug.LogError("[Mindforge:V31] Pinned InputReader no longer exposes the expected private _controls field.");
                enabled = false;
                return;
            }

            _controls = controlsField.GetValue(_inputReader) as global::Controllers;
            if (_controls == null)
            {
                Debug.LogError("[Mindforge:V31] Could not resolve the inherited generated Controllers instance.");
                enabled = false;
                return;
            }

            global::Controllers.PlayerActions player = _controls.Player;
            bool wasEnabled = player.enabled;
            if (wasEnabled) player.Disable();
            try
            {
                AddBinding(player.Camera, "<Mouse>/delta", "scaleVector2(x=0.035,y=0.035)");
                AddBinding(player.Target, "<Mouse>/middleButton");
                AddBinding(player.Sprint, "<Keyboard>/leftShift");
                AddBinding(player.LightAttack, "<Mouse>/leftButton");
                AddBinding(player.HeavyAttack, "<Mouse>/rightButton");
                AddBinding(player.SheathSword, "<Keyboard>/x");
                AddBinding(player.Aim, "<Keyboard>/q");
                AddBinding(player.WeaponReturn, "<Keyboard>/r");
                AddBinding(player.Roll, "<Keyboard>/leftAlt");
                AddBinding(player.Heal, "<Keyboard>/h");
                AddBinding(player.LightBonfire, "<Keyboard>/e");
                AddBinding(player.Pause, "<Keyboard>/escape");
            }
            finally
            {
                if (wasEnabled) player.Enable();
            }

            DesktopCombatReady = HasBinding(player.LightAttack, "<Mouse>/leftButton") &&
                HasBinding(player.HeavyAttack, "<Mouse>/rightButton") &&
                HasBinding(player.Camera, "<Mouse>/delta") &&
                HasBinding(player.Target, "<Mouse>/middleButton") &&
                HasBinding(player.Roll, "<Keyboard>/leftAlt") &&
                HasBinding(player.WeaponReturn, "<Keyboard>/r");
            Installed = true;

            Debug.Log(
                "[Mindforge:V31] Desktop combat input adapter installed. " + BindingSummary
            );
        }

        private void AddBinding(InputAction action, string path, string processors = null)
        {
            if (action == null || HasBinding(action, path)) return;
            InputActionSetupExtensions.BindingSyntax syntax = action.AddBinding(path);
            if (!string.IsNullOrEmpty(processors))
                syntax.WithProcessors(processors);
            BindingsAdded++;
        }

        private static bool HasBinding(InputAction action, string path)
        {
            if (action == null) return false;
            for (int i = 0; i < action.bindings.Count; i++)
            {
                InputBinding binding = action.bindings[i];
                if (string.Equals(binding.path, path, System.StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }
    }
}
