#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;
using Mindforge.Editor;
using Mindforge.Presentation;

namespace Mindforge.Tests.Editor
{
    public sealed class ProfessionalEncounterV28SmokeTests
    {
        [Test]
        public void V28GitBlobHash_MatchesGitObjectContract()
        {
            byte[] bytes = System.Text.Encoding.ASCII.GetBytes("hello");
            Assert.AreEqual(
                "b6fc4c620b67d95f953a5c1c1230aaab5db5a1b0",
                PublicAssetAcquisitionV28.ComputeGitBlobSha1(bytes));
        }

        [Test]
        public void V28ActorOcclusionGuard_CanBeConstructedByUnity()
        {
            GameObject go = new GameObject("V28_CameraSmoke");
            try
            {
                go.AddComponent<Camera>();
                MindforgeActorOcclusionGuardV28 guard = go.AddComponent<MindforgeActorOcclusionGuardV28>();
                Assert.IsNotNull(guard);
                Assert.IsFalse(guard.Correcting);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void V28PresentationTypes_AreRuntimeComponentsNotEditorStubs()
        {
            Assert.IsTrue(typeof(MonoBehaviour).IsAssignableFrom(typeof(FracturedSignalCreaturePresentationV28)));
            Assert.IsTrue(typeof(MonoBehaviour).IsAssignableFrom(typeof(MindforgeActorOcclusionGuardV28)));
        }
    }
}
#endif
