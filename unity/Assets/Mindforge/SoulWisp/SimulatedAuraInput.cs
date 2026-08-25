using UnityEngine;
using Mindforge.Neural;

namespace Mindforge.SoulWisp
{
    /// Development-only stand-in for a decoder. Never enable in observed-hardware builds.
    public sealed class SimulatedAuraInput : MonoBehaviour
    {
        [SerializeField] private AuraBuffController buffs;
        [SerializeField] private float secondsToSelection = 1.2f;
        private float _sight, _guard;
        private long _seq;

        private void Update()
        {
            bool sight = Input.GetKey(KeyCode.Q), guard = Input.GetKey(KeyCode.E);
            float decay = Time.unscaledDeltaTime * 0.8f;
            _sight = sight && !guard ? _sight + Time.unscaledDeltaTime : Mathf.Max(0, _sight - decay);
            _guard = guard && !sight ? _guard + Time.unscaledDeltaTime : Mathf.Max(0, _guard - decay);
            if (_sight >= secondsToSelection) { Emit("sight"); _sight = 0; }
            if (_guard >= secondsToSelection) { Emit("guard"); _guard = 0; }
        }

        private void Emit(string target)
        {
            _seq++;
            buffs?.TryApply(new NeuralEvent {
                schema = "mindforge.neural_event.v1", seq = _seq, @event = "AURA_SELECTED",
                target = target, confidence = 0.90f, quality = 0.92f,
                paradigm = "simulation", model_id = "keyboard-fixture"
            });
        }
    }
}
