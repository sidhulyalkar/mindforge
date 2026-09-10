using UnityEngine;
using UnityEngine.InputSystem;

namespace Mindforge.Chassis
{
    /// <summary>
    /// Explicit development-only semantic injector. N opens a local causal window,
    /// then 1/2 inject Sight/Guard through the same intent bridge used by decoder
    /// output. It never bypasses the window or writes gameplay state directly.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MindforgeBciDevInputV33 : MonoBehaviour
    {
        private MindforgeNeuralWindowControllerV33 _windows;
        private MindforgeNeuralIntentBridgeV33 _bridge;

        private void Start()
        {
            _windows = GetComponent<MindforgeNeuralWindowControllerV33>();
            _bridge = GetComponent<MindforgeNeuralIntentBridgeV33>();
        }

        private void Update()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || _windows == null || _bridge == null || !_windows.IsListening) return;

            if (keyboard.digit1Key.wasPressedThisFrame)
                _bridge.InjectControllerSimulation(MindforgeIntentV29.Sight, 1f);
            else if (keyboard.digit2Key.wasPressedThisFrame)
                _bridge.InjectControllerSimulation(MindforgeIntentV29.Guard, 1f);
#endif
        }
    }
}
