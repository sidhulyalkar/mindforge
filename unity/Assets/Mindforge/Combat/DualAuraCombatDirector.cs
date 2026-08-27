using UnityEngine;
using Mindforge.Neural;
using Mindforge.SoulWisp;

namespace Mindforge.Combat
{
    /// <summary>
    /// BCI modifies macro buffs only. It never moves, attacks, aims, dodges, or
    /// parries for the player. PARTICIPANT_STOP dominates every other event.
    /// </summary>
    public sealed class DualAuraCombatDirector : MonoBehaviour
    {
        [SerializeField] private UdpNeuralReceiver neuralReceiver;
        [SerializeField] private AuraBuffController buffs;
        [SerializeField] private bool controllerOnlyFallback = true;

        public bool BciOnline { get; private set; }
        public bool ParticipantStopped { get; private set; }

        private void OnEnable()
        {
            if (neuralReceiver == null) return;
            neuralReceiver.EventReceived += OnNeuralEvent;
            neuralReceiver.ConnectionStateChanged += OnConnectionChanged;
            BciOnline = neuralReceiver.IsConnected;
        }

        private void OnDisable()
        {
            if (neuralReceiver == null) return;
            neuralReceiver.EventReceived -= OnNeuralEvent;
            neuralReceiver.ConnectionStateChanged -= OnConnectionChanged;
        }

        private void OnNeuralEvent(NeuralEvent evt)
        {
            if (evt == null || ParticipantStopped) return;

            if (evt.IsParticipantStop)
            {
                ParticipantStopped = true;
                BciOnline = false;
                buffs?.ClearAll();
                return;
            }

            if (evt.IsLost)
            {
                BciOnline = false;
                if (!controllerOnlyFallback) buffs?.ClearAll();
                return;
            }

            if (evt.IsRecovered)
            {
                BciOnline = true;
                return;
            }

            if (evt.IsSelection) buffs?.TryApply(evt);
        }

        private void OnConnectionChanged(bool connected)
        {
            if (ParticipantStopped) return;
            BciOnline = connected;
            if (!connected && !controllerOnlyFallback) buffs?.ClearAll();
        }
    }
}
