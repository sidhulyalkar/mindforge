#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using NUnit.Framework;
using Mindforge.Neural;
using Mindforge.SoulWisp;

namespace Mindforge.Tests.Editor
{
    public sealed class NeuralSelectionIdentityV24Tests
    {
        private static NeuralEvent Valid()
        {
            return new NeuralEvent
            {
                schema = NeuralEvent.SchemaV2,
                seq = 41,
                @event = "AURA_SELECTED",
                target = "sight",
                session_id = "game-alpha",
                model_id = "calibration-1",
                stimulus_epoch = 7,
                evidence_ms = 750,
                has_evidence = true,
                artifact = false,
                authority_ttl_ms = 250,
                source_mode = "live",
            };
        }

        [Test] public void SameSessionValidEvidence_IsBound()
        {
            Assert.IsTrue(NeuralSelectionIdentityV24.IsBoundToGameSession(Valid(), "game-alpha"));
        }

        [Test] public void DifferentSession_IsRejected()
        {
            Assert.IsFalse(NeuralSelectionIdentityV24.IsBoundToGameSession(Valid(), "game-beta"));
        }

        [Test] public void MissingSession_IsRejected()
        {
            NeuralEvent evt = Valid();
            evt.session_id = null;
            Assert.IsFalse(NeuralSelectionIdentityV24.IsBoundToGameSession(evt, "game-alpha"));
        }

        [Test] public void NoEvidence_IsRejected()
        {
            NeuralEvent evt = Valid();
            evt.has_evidence = false;
            Assert.IsFalse(NeuralSelectionIdentityV24.IsBoundToGameSession(evt, "game-alpha"));
        }

        [Test] public void ZeroAuthorityTtl_IsRejected()
        {
            NeuralEvent evt = Valid();
            evt.authority_ttl_ms = 0;
            Assert.IsFalse(NeuralSelectionIdentityV24.IsBoundToGameSession(evt, "game-alpha"));
        }

        [Test] public void NetworkEditorModeSpoof_IsRejected()
        {
            NeuralEvent evt = Valid();
            evt.source_mode = "unity_editor_resonance_sim";
            Assert.IsFalse(NeuralSelectionIdentityV24.IsBoundToGameSession(evt, "game-alpha"));
        }

        [Test] public void ArtifactOrLegacySchema_IsRejected()
        {
            NeuralEvent evt = Valid();
            evt.artifact = true;
            Assert.IsFalse(NeuralSelectionIdentityV24.IsBoundToGameSession(evt, "game-alpha"));
            evt.artifact = false;
            evt.schema = NeuralEvent.SchemaV1;
            Assert.IsFalse(NeuralSelectionIdentityV24.IsBoundToGameSession(evt, "game-alpha"));
        }

        [Test] public void RepeatedOrMissingProvenance_IsRejected()
        {
            NeuralEvent evt = Valid();
            evt.seq = 0;
            Assert.IsFalse(NeuralSelectionIdentityV24.IsBoundToGameSession(evt, "game-alpha"));
            evt.seq = 41;
            evt.model_id = "";
            Assert.IsFalse(NeuralSelectionIdentityV24.IsBoundToGameSession(evt, "game-alpha"));
        }
    }
}
#endif
