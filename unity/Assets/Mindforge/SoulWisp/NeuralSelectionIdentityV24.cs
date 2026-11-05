using System;
using Mindforge.Neural;

namespace Mindforge.SoulWisp
{
    /// <summary>
    /// Pure, fail-closed identity/provenance precondition for a UDP neural selection.
    /// This is not proof of intention, trusted acquisition or physical stimulus timing.
    /// Editor keyboard simulation is evaluated through a separate local-only path.
    /// </summary>
    public static class NeuralSelectionIdentityV24
    {
        public static bool IsBoundToGameSession(NeuralEvent evt, string gameSessionId)
        {
            if (evt == null || !evt.IsV2 || !evt.IsSelection || evt.artifact ||
                !evt.has_evidence || evt.authority_ttl_ms <= 0 ||
                evt.seq <= 0 || evt.stimulus_epoch < 0 ||
                string.IsNullOrWhiteSpace(evt.model_id) ||
                string.IsNullOrWhiteSpace(gameSessionId) ||
                string.IsNullOrWhiteSpace(evt.session_id))
                return false;

            // A network producer may not claim the Unity-only keyboard simulation bypass.
            if (string.Equals(evt.source_mode, "unity_editor_resonance_sim", StringComparison.Ordinal))
                return false;

            return string.Equals(evt.session_id, gameSessionId, StringComparison.Ordinal);
        }
    }
}
