#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using NUnit.Framework;
using UnityEngine;
using Mindforge.Combat;

namespace Mindforge.Tests.Editor
{
    public sealed class FracturedSignalDuelStabilityV22SmokeTests
    {
        [Test]
        public void V22DuelStability_CanBeConstructedByUnity()
        {
            GameObject host = null;
            try
            {
                host = new GameObject("Mindforge_UnityLifecycleSmoke_V22_DuelStability");
                host.SetActive(false);
                host.AddComponent<CombatantVitals>();
                host.AddComponent<FracturedSignalDirector>();
                host.AddComponent<FracturedSignalFirstBossV19>();
                FracturedSignalDuelStabilityV22 component = null;
                Assert.DoesNotThrow(() => component = host.AddComponent<FracturedSignalDuelStabilityV22>());
                Assert.That(component, Is.Not.Null);
            }
            finally
            {
                if (host != null) Object.DestroyImmediate(host);
            }
        }
    }
}
#endif