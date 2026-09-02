#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;
using Mindforge.Editor;

namespace Mindforge.Tests.Editor
{
    public sealed class WorldRenderingV26SmokeTests
    {
        [Test]
        public void V26ChamferedBlock_HasProductionEdgeGeometry()
        {
            Mesh mesh = ProductionGeometryV26.BuildTransientChamferedBlock();
            try
            {
                Assert.IsNotNull(mesh);
                Assert.Greater(mesh.vertexCount, 24);
                Assert.Greater(mesh.triangles.Length, 36);
                Assert.That(mesh.bounds.size.x, Is.EqualTo(1f).Within(0.001f));
                Assert.That(mesh.bounds.size.y, Is.EqualTo(1f).Within(0.001f));
                Assert.That(mesh.bounds.size.z, Is.EqualTo(1f).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(mesh);
            }
        }

        [Test]
        public void V26VaultWeb_FacesIntoGameplaySpace()
        {
            Mesh mesh = ProductionGeometryV26.BuildTransientVaultWeb();
            try
            {
                Assert.IsNotNull(mesh);
                Assert.IsNotNull(mesh.normals);
                Assert.Greater(mesh.normals.Length, 0);
                Vector3 centre = mesh.normals[mesh.normals.Length / 2];
                Assert.Less(centre.y, -0.35f);
                Assert.Greater(mesh.bounds.size.y, 0.90f);
            }
            finally
            {
                Object.DestroyImmediate(mesh);
            }
        }

        [Test]
        public void V26TaperedButtress_IsNotABoxPrimitive()
        {
            Mesh mesh = ProductionGeometryV26.BuildTransientTaperedButtress();
            try
            {
                Assert.IsNotNull(mesh);
                Assert.Greater(mesh.vertexCount, 8);
                Assert.AreEqual("V26_TaperedButtress", mesh.name);
                Assert.That(mesh.bounds.size.y, Is.EqualTo(1f).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(mesh);
            }
        }
    }
}
#endif
