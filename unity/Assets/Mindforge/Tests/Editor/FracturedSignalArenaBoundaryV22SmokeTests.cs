#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using Mindforge.Combat;
using NUnit.Framework;
using UnityEngine;

namespace Mindforge.Tests.Editor
{
    public sealed class FracturedSignalArenaBoundaryV22SmokeTests
    {
        [Test]
        public void V22ArenaBoundary_CanBeConstructedByUnity()
        {
            GameObject host = null;
            try
            {
                host = new GameObject("Mindforge_UnityLifecycleSmoke_V22_ArenaBoundary");
                host.SetActive(false);
                host.AddComponent<CombatantVitals>();
                host.AddComponent<FracturedSignalDirector>();
                FracturedSignalArenaBoundaryV22 component = null;
                Assert.DoesNotThrow(() => component = host.AddComponent<FracturedSignalArenaBoundaryV22>());
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
