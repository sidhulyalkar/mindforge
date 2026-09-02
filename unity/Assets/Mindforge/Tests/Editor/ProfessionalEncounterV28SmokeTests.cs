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
        public void V28NormalizedCacheSha256_IsDeterministic()
        {
            byte[] bytes = System.Text.Encoding.ASCII.GetBytes("mindforge-v28");
            Assert.AreEqual(
                "a0656d180c39a3fffd67767cf4315e47cf51fc4c52e65594db9b3f750e6c0278",
                PublicAssetAcquisitionV28.ComputeSha256(bytes));
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
        public void V28WorldDetail_UsesSameProtectedNegativeSpaceAsEncounterStage()
        {
            Assert.AreEqual(3.15f, ProfessionalWorldDetailV28Builder.RouteClearHalfWidth, 0.0001f);
            Assert.AreEqual(14.4f, ProfessionalWorldDetailV28Builder.BossClearRadius, 0.0001f);
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
