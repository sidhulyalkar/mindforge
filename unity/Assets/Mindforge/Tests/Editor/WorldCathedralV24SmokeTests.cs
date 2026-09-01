#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;
using Mindforge.Editor;
using Mindforge.World;

namespace Mindforge.Tests.Editor
{
    public sealed class WorldCathedralV24SmokeTests
    {
        [Test]
        public void V24CathedralRole_IsPureSemanticMarker()
        {
            GameObject go = new GameObject("V24RoleSmoke");
            try
            {
                CathedralRoleV24 role = go.AddComponent<CathedralRoleV24>();
                role.Configure(CathedralRoleV24.StructuralRole.StructuralSupport);
                Assert.AreEqual(CathedralRoleV24.StructuralRole.StructuralSupport, role.Role);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void V24CathedralModuleKit_CanConstructSemanticGeometry()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Assert.IsNotNull(shader);
            Material material = new Material(shader);
            GameObject parentObject = new GameObject("V24ModuleSmokeRoot");
            try
            {
                Transform column = CathedralModuleLibraryV24.Column(
                    "SmokeColumn",
                    parentObject.transform,
                    Vector3.zero,
                    new Vector3(0.5f, 3f, 0.5f),
                    material,
                    material,
                    false);

                CathedralRoleV24 role = column.GetComponentInChildren<CathedralRoleV24>(true);
                Assert.IsNotNull(role);
                Assert.AreEqual(CathedralRoleV24.StructuralRole.StructuralSupport, role.Role);
                Assert.Greater(column.GetComponentsInChildren<Renderer>(true).Length, 0);
            }
            finally
            {
                Object.DestroyImmediate(parentObject);
                Object.DestroyImmediate(material);
            }
        }
    }
}
#endif
