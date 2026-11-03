#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using NUnit.Framework;
using UnityEngine;
using Mindforge.LivingWorld;

namespace Mindforge.Tests.Editor
{
    /// <summary>Pure mapping and default-no-authority lifecycle gate tests.</summary>
    public sealed class LivingWorldV31IntegrationTests
    {
        [Test]
        public void MapFeatureIsNeutralWhenIntensityZeroAndBoundedWhenEnabled()
        {
            Assert.That(LivingWorldExperienceV31.MapFeature(0f, 0f), Is.EqualTo(.5f).Within(.00001f));
            Assert.That(LivingWorldExperienceV31.MapFeature(1f, 0f), Is.EqualTo(.5f).Within(.00001f));
            Assert.That(LivingWorldExperienceV31.MapFeature(0f, .3f), Is.EqualTo(.35f).Within(.00001f));
            Assert.That(LivingWorldExperienceV31.MapFeature(1f, .3f), Is.EqualTo(.65f).Within(.00001f));
            Assert.That(LivingWorldExperienceV31.MapFeature(5f, 8f), Is.EqualTo(.65f).Within(.00001f));
            Assert.That(LivingWorldExperienceV31.MapFeature(-5f, -8f), Is.EqualTo(.5f).Within(.00001f));
        }

        [Test]
        public void UnconfiguredV31CannotBindNetworkOrEnableVisuals()
        {
            GameObject host = new GameObject("LivingWorld_V31_Test");
            try
            {
                host.SetActive(false); // no runtime or active scene bootstrap.
                LivingWorldExperienceV31 v31 = host.AddComponent<LivingWorldExperienceV31>();
                Assert.That(v31.EnabledByPlayer, Is.False);
                Assert.DoesNotThrow(() => v31.SetOptIn(true, save: false));
                Assert.That(v31.EnabledByPlayer, Is.False);
                Assert.That(v31.EffectiveIntensity, Is.EqualTo(0f));
                Assert.That(v31.DecorativeSurfaceCount, Is.EqualTo(6));
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }
    }
}
#endif
