#if UNITY_INCLUDE_TESTS
using NUnit.Framework;

namespace LuckySeven.Tests
{
    public sealed class LuckyBrainGateTests
    {
        private static NeuralEventV2 E(string eventName, long seq, string source = "live")
        {
            return new NeuralEventV2 {
                schema="mindforge.neural_event.v2", @event=eventName,
                source_mode=source, session_id="s1", calibration_id="c1",
                seq=seq, target="sight", confidence=0.92f, quality=0.91f,
                has_evidence=true, artifact=false,
                source_sample_start=100, source_sample_end=200, authority_ttl_ms=800
            };
        }

        [Test] public void NoCalibrationCannotGrantWind()
        {
            var g = new LuckyBrainGate();
            Assert.AreEqual(AnimalSense.None, g.Observe(E("AURA_SELECTED", 1), 5));
        }

        [Test] public void FreshLiveCalibrationAllowsExactlyTwoSenses()
        {
            var g = new LuckyBrainGate();
            g.Observe(E("CALIBRATION_READY", 1), 5);
            Assert.AreEqual(AnimalSense.Wind, g.Observe(E("AURA_SELECTED", 2), 5));
            var root = E("AURA_SELECTED", 3); root.target = "guard";
            Assert.AreEqual(AnimalSense.Root, g.Observe(root, 5));
            Assert.AreEqual(AnimalSense.None, g.Observe(root, 5), "No duplicate authority");
        }

        [Test] public void RejectsReplaysStaleArtifactAndLowQuality()
        {
            var g = new LuckyBrainGate();
            g.Observe(E("CALIBRATION_READY", 1), 5);
            Assert.AreEqual(AnimalSense.None, g.Observe(E("AURA_SELECTED", 2, "simulated_decision"), 2));
            Assert.AreEqual(AnimalSense.None, g.Observe(E("AURA_SELECTED", 3), 900));
            var artifact = E("AURA_SELECTED", 4); artifact.artifact = true;
            Assert.AreEqual(AnimalSense.None, g.Observe(artifact, 4));
            var badQuality = E("AURA_SELECTED", 5); badQuality.quality = 0.2f;
            Assert.AreEqual(AnimalSense.None, g.Observe(badQuality, 4));
        }

        [Test] public void SignalLossDisarmsNewSelections()
        {
            var g = new LuckyBrainGate();
            g.Observe(E("CALIBRATION_READY", 1), 1);
            g.Observe(E("BCI_LOST", 2), 1);
            Assert.IsFalse(g.Ready);
            Assert.AreEqual(AnimalSense.None, g.Observe(E("AURA_SELECTED", 3), 1));
        }
    }
}
