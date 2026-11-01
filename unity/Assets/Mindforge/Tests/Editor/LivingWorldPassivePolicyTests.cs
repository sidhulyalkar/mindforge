#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using NUnit.Framework;
using UnityEngine;
using Mindforge.LivingWorld;

namespace Mindforge.Tests.Editor
{
    public sealed class LivingWorldPassivePolicyTests
    {
        private static PassiveFeatureFrame Frame(long seq = 0, float value = 1f,
            string session = "session-a", float quality = 1f, bool artifact = false,
            int ttlMs = 500)
        {
            return new PassiveFeatureFrame
            {
                schema = PassiveFeatureFrame.Schema,
                seq = seq,
                source_session_id = session,
                source_mode = "simulated_feature",
                calibration_id = null,
                feature = PassiveFeatureFrame.Feature,
                unit = "ratio",
                value = value,
                quality = quality,
                artifact = artifact,
                passive_only = true,
                ttl_ms = ttlMs,
                sample_start = -1,
                sample_end = -1,
                producer_monotonic_ns = 1000
            };
        }

        [Test]
        public void PassiveJsonRejectsInvalidOrAuthorityLikeFrames()
        {
            var frame = Frame();
            string json = JsonUtility.ToJson(frame);
            Assert.That(PassiveFeatureFrame.TryParse(json, out PassiveFeatureFrame parsed), Is.True);
            Assert.That(parsed.Valid, Is.True);
            frame.passive_only = false;
            Assert.That(PassiveFeatureFrame.TryParse(JsonUtility.ToJson(frame), out _), Is.False);
            frame.passive_only = true;
            frame.value = float.NaN;
            Assert.That(frame.Valid, Is.False);
            frame.value = .5f;
            frame.artifact = true;
            Assert.That(frame.Valid, Is.True); // Artifact is rejected at policy, not parsing.
        }

        [Test]
        public void PolicyRejectsArtifactAndLowQuality()
        {
            var policy = new PassiveFeaturePolicy();
            Assert.That(policy.TryAccept(Frame(1, .9f, quality: .1f), 5.0, 0.0), Is.False);
            Assert.That(policy.TryAccept(Frame(2, .9f, artifact: true), 5.0, 0.0), Is.False);
            Assert.That(policy.AcceptedCount, Is.EqualTo(0));
            Assert.That(policy.Advance(.1f, 5.1), Is.EqualTo(.5f).Within(.001f));
        }

        [Test]
        public void PolicyRejectsStaleAndOutOfOrderAndPreventsSessionSteal()
        {
            var policy = new PassiveFeaturePolicy();
            Assert.That(policy.TryAccept(Frame(seq: 3), 10.0, .1), Is.True);
            Assert.That(policy.TryAccept(Frame(seq: 2), 10.1, .1), Is.False);
            Assert.That(policy.TryAccept(Frame(seq: 4, session: "other"), 10.1, .0), Is.False);
            Assert.That(policy.TryAccept(Frame(seq: 5), 10.1, .6), Is.False);
            Assert.That(policy.TryAccept(Frame(seq: 1, session: "other"), 11.0, .0), Is.True);
        }

        [Test]
        public void PolicyIsSlowClampedAndDecaysToNeutralAfterExpiry()
        {
            var policy = new PassiveFeaturePolicy { MaxDeltaPerSecond = .10f };
            Assert.That(policy.TryAccept(Frame(1, 1f, ttlMs: 200), 10., 0.), Is.True);
            float high = policy.Advance(.1f, 10.1);
            Assert.That(high, Is.GreaterThan(.5f));
            Assert.That(high, Is.LessThanOrEqualTo(.51f));
            float next = policy.Advance(.1f, 11.);
            Assert.That(next, Is.LessThan(high));
            for (int i = 0; i < 1000; i++) policy.Advance(.1f, 12. + i * .1);
            Assert.That(policy.CurrentValue, Is.EqualTo(.5f).Within(.001f));
        }
    }
}
#endif
