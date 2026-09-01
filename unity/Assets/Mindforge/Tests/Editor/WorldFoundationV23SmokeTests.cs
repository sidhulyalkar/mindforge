#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

namespace Mindforge.Tests.Editor
{
    public sealed class WorldFoundationV23SmokeTests
    {
        [Test]
        public void V23InwardPatch_FacesIntoTheCavern()
        {
            Mesh mesh = Mindforge.Editor.WorldFoundationMeshLibraryV23.BuildTransientInwardPatch(
                -2f,
                2f,
                -2f,
                2f,
                4,
                4,
                (x, z) => 3f + x * 0.01f + z * 0.01f);

            try
            {
                Assert.That(mesh, Is.Not.Null);
                Assert.That(mesh.vertexCount, Is.GreaterThan(0));
                Assert.That(mesh.triangles.Length, Is.GreaterThan(0));
                Assert.That(mesh.normals.Length, Is.EqualTo(mesh.vertexCount));
                Assert.That(mesh.normals[mesh.normals.Length / 2].y, Is.LessThan(-0.95f));
            }
            finally
            {
                Object.DestroyImmediate(mesh);
            }
        }
    }
}
#endif