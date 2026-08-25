using UnityEngine;
using Mindforge.Neural;
using Mindforge.SoulWisp;

namespace Mindforge.Combat
{
    /// BCI modifies buffs only; it never moves, attacks, aims, or parries for the player.
    public sealed class DualAuraCombatDirector : MonoBehaviour
    {
        [SerializeField] private UdpNeuralReceiver neuralReceiver;
        [SerializeField] private AuraBuffController buffs;
        [SerializeField] private bool controllerOnlyFallback = true;
        public bool BciOnline { get; private set; }

        private void OnEnable()
        {
            if (neuralReceiver == null) return;
            neuralReceiver.EventReceived += OnNeuralEvent;
            neuralReceiver.ConnectionStateChanged += OnConnectionChanged;
        }
        private void OnDisable()
        {
            if (neuralReceiver == null) return;
            neuralReceiver.EventReceived -= OnNeuralEvent;
            neuralReceiver.ConnectionStateChanged -= OnConnectionChanged;
        }
        private void OnNeuralEvent(NeuralEvent evt)
        {
            if (evt == null) return;
            if (evt.@event == "PARTICIPANT_STOP") { buffs?.ClearAll(); enabled = false; return; }
            buffs?.TryApply(evt);
        }
        private void OnConnectionChanged(bool connected)
        {
            BciOnline = connected;
            if (!connected && !controllerOnlyFallback) buffs?.ClearAll();
        }
    }
}
