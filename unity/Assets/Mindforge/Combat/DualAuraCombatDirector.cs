using UnityEngine;
using Mindforge.Neural;
using Mindforge.SoulWisp;

namespace Mindforge.Combat
{
    /// <summary>
    /// BCI modifies macro buffs only. It never moves, attacks, aims, dodges, or parries.
    /// AURA_SELECTED additionally has no standing authority: a fresh selection must arrive
    /// inside an explicitly player-armed WispResonanceWindow. PARTICIPANT_STOP dominates all.
    /// </summary>
    public sealed class DualAuraCombatDirector : MonoBehaviour
    {
        [SerializeField] private UdpNeuralReceiver neuralReceiver;
        [SerializeField] private AuraBuffController buffs;
        [SerializeField] private WispResonanceWindow resonanceWindow;
        [SerializeField] private bool controllerOnlyFallback = true;

        public bool BciOnline { get; private set; }
        public bool ParticipantStopped { get; private set; }
        public WispResonanceWindow ResonanceWindow => resonanceWindow;

        private void OnEnable()
        {
            ResolveResonanceWindow();
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

        public void BindResonanceWindow(WispResonanceWindow window)
        {
            if (window != null) resonanceWindow = window;
        }

        private void ResolveResonanceWindow()
        {
            if (resonanceWindow == null)
                resonanceWindow = Object.FindObjectOfType<WispResonanceWindow>(true);
        }

        private void OnNeuralEvent(NeuralEvent evt)
        {
            if (evt == null || ParticipantStopped) return;

            if (evt.IsParticipantStop)
            {
                ParticipantStopped = true;
                BciOnline = false;
                buffs?.ClearAll();
                ResolveResonanceWindow();
                resonanceWindow?.AbortForLinkLoss("PARTICIPANT_STOP");
                return;
            }

            if (evt.IsLost)
            {
                BciOnline = false;
                if (!controllerOnlyFallback) buffs?.ClearAll();
                ResolveResonanceWindow();
                resonanceWindow?.AbortForLinkLoss("BCI_LOST");
                return;
            }

            if (evt.IsRecovered)
            {
                BciOnline = true;
                return;
            }

            ResolveResonanceWindow();

            if (evt.IsAbstain)
            {
                resonanceWindow?.ObserveAbstain(evt);
                return;
            }

            if (!evt.IsSelection) return;

            // Fail closed. If the neutral player-armed decision gate is unavailable or
            // closed, a decoder selection cannot become gameplay state.
            if (resonanceWindow == null || !resonanceWindow.CanAcceptSelection(evt)) return;

            bool applied = buffs != null && buffs.TryApply(evt);
            if (applied)
                resonanceWindow.MarkResolved(evt.Target);
        }

        private void OnConnectionChanged(bool connected)
        {
            if (ParticipantStopped) return;
            BciOnline = connected;
            if (!connected)
            {
                if (!controllerOnlyFallback) buffs?.ClearAll();
                ResolveResonanceWindow();
                resonanceWindow?.AbortForLinkLoss("BCI_LINK_STALE");
            }
        }
    }
}
