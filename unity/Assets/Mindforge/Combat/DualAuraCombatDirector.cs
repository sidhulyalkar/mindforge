using System;
using UnityEngine;
using Mindforge.Telemetry;
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
        // Observer only. Correlation IDs ride on existing GameMarker trial_id.
        [SerializeField] private UdpGameMarkerSender evidenceMarkerSender;

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

        // All observed stages share a stable identity within the current Unity session.
        // The seq/epoch pair is not a human intention label or an independent sensor.
        private static string SelectionAuditId(NeuralEvent evt)
        {
            return "selection-v24:" + MindforgeSessionContext.GameSessionId +
                ":" + evt.stimulus_epoch + ":" + evt.seq;
        }

        private void EmitSelectionEvidence(string stage, NeuralEvent evt, string trialId, string reason = null)
        {
            // Observability must never become gameplay authority or throw into combat.
            try
            {
                if (evidenceMarkerSender == null)
                    evidenceMarkerSender = Object.FindObjectOfType<UdpGameMarkerSender>(true);
                if (evidenceMarkerSender == null) return;
                string target = evt.Target == AuraTarget.Sight ? "sight" :
                    evt.Target == AuraTarget.Guard ? "guard" : null;
                evidenceMarkerSender.Emit(stage, "neural_evidence",
                    target: target, reason: reason,
                    stimulusEpoch: evt.stimulus_epoch, trialId: trialId);
            }
            catch (Exception)
            {
                // Missing or damaged records must block downstream qualification.
            }
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

            string trialId = SelectionAuditId(evt);
            EmitSelectionEvidence("NEURAL_SELECTION_OBSERVED", evt, trialId);

            // Fail closed. No decoder output can bypass the active player-armed window.
            if (resonanceWindow == null || !resonanceWindow.CanAcceptSelection(evt))
            {
                EmitSelectionEvidence("NEURAL_SELECTION_DENIED", evt, trialId, "WINDOW_OR_PROVENANCE_GATE");
                return;
            }
            if (buffs == null)
            {
                EmitSelectionEvidence("NEURAL_SELECTION_DENIED", evt, trialId, "BUFF_CONTROLLER_UNAVAILABLE");
                return;
            }

            bool applied = buffs.TryApply(evt);
            if (!applied)
            {
                EmitSelectionEvidence("NEURAL_SELECTION_DENIED", evt, trialId, "BUFF_REJECTED");
                return;
            }

            // Acceptance is recorded only after TryApply returns true. An observed state
            // change is recorded separately and must not be inferred from acceptance.
            EmitSelectionEvidence("NEURAL_SELECTION_ACCEPTED", evt, trialId);
            bool effectObserved = evt.Target == AuraTarget.Sight ? buffs.SightActive : buffs.GuardActive;
            if (effectObserved)
                EmitSelectionEvidence("NEURAL_EFFECT_OBSERVED", evt, trialId);
            else
                EmitSelectionEvidence("NEURAL_EFFECT_UNVERIFIED", evt, trialId, "BUFF_STATE_NOT_ACTIVE");
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
