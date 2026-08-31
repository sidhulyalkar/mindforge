#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using NUnit.Framework;
using UnityEngine;
using Mindforge.Combat;
using Mindforge.Presentation;
using Mindforge.Telemetry;

namespace Mindforge.Tests.Editor
{
    /// <summary>
    /// Native Unity lifecycle smoke tests. These deliberately exercise component construction
    /// inside Unity rather than trying to infer engine lifecycle behavior from source text.
    ///
    /// Keep this suite small and structural: it is the tripwire for constructor/deserialization
    /// mistakes, while gameplay/BCI semantics remain covered by the existing repository contracts
    /// and the canonical readiness audit.
    /// </summary>
    public sealed class MindforgeUnityLifecycleSmokeTests
    {
        [Test]
        public void LegacyMaterialHierarchyV16_CanBeConstructedByUnity()
        {
            GameObject host = null;
            try
            {
                host = new GameObject("Mindforge_UnityLifecycleSmoke_V16");
                LegacyMaterialHierarchyV16 component = null;
                Assert.DoesNotThrow(() => component = host.AddComponent<LegacyMaterialHierarchyV16>());
                Assert.That(component, Is.Not.Null);
            }
            finally
            {
                if (host != null) Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void PresentationPropertyBlockConstruction_IsDeferredUntilAwake()
        {
            GameObject host = null;
            try
            {
                host = new GameObject("Mindforge_UnityLifecycleSmoke_PropertyBlock");
                LegacyMaterialHierarchyV16 component = host.AddComponent<LegacyMaterialHierarchyV16>();
                Assert.That(component, Is.Not.Null,
                    "Unity must be able to deserialize/construct V16 without native CreateImpl calls from field initialization.");
            }
            finally
            {
                if (host != null) Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void V18EncounterAssist_CanBeConstructedByUnity()
        {
            GameObject host = null;
            try
            {
                host = new GameObject("Mindforge_UnityLifecycleSmoke_V18_TargetAssist");
                EncounterTargetAssistV18 component = null;
                Assert.DoesNotThrow(() => component = host.AddComponent<EncounterTargetAssistV18>());
                Assert.That(component, Is.Not.Null);
            }
            finally
            {
                if (host != null) Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void V18SsvepFocusBackdrop_CanBeConstructedByUnity()
        {
            GameObject host = null;
            try
            {
                host = new GameObject("Mindforge_UnityLifecycleSmoke_V18_FocusBackdrop");
                SsvepFocusBackdropV18 component = null;
                Assert.DoesNotThrow(() => component = host.AddComponent<SsvepFocusBackdropV18>());
                Assert.That(component, Is.Not.Null);
            }
            finally
            {
                if (host != null) Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void V18SsvepDatasetTelemetry_CanBeConstructedByUnity()
        {
            GameObject host = null;
            try
            {
                host = new GameObject("Mindforge_UnityLifecycleSmoke_V18_DatasetTelemetry");
                SsvepDatasetTelemetryV18 component = null;
                Assert.DoesNotThrow(() => component = host.AddComponent<SsvepDatasetTelemetryV18>());
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
