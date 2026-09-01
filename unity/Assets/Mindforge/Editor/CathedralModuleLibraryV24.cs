#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.Rendering;
using Mindforge.World;

namespace Mindforge.Editor
{
    /// <summary>
    /// Small deterministic cathedral kit used by V0.24. Modules carry semantic roles and use a
    /// narrow vocabulary instead of arbitrary one-off primitives scattered across zone builders.
    /// </summary>
    public static class CathedralModuleLibraryV24
    {
        public static Transform Node(string name, Transform parent)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        public static Transform FloorSkin(
            string name,
            Transform parent,
            Vector3 position,
            Vector3 scale,
            Material material,
            Vector3? euler = null)
            => Block(name, parent, position, scale, material,
                CathedralRoleV24.StructuralRole.WalkableFloor, euler ?? Vector3.zero, false);

        public static Transform Trim(
            string name,
            Transform parent,
            Vector3 position,
            Vector3 scale,
            Material material,
            Vector3? euler = null)
            => Block(name, parent, position, scale, material,
                CathedralRoleV24.StructuralRole.DecorativePatina, euler ?? Vector3.zero, false);

        public static Transform RetainingBlock(
            string name,
            Transform parent,
            Vector3 position,
            Vector3 scale,
            Material material,
            Vector3? euler = null,
            bool collider = false)
            => Block(name, parent, position, scale, material,
                CathedralRoleV24.StructuralRole.RetainingSubstructure, euler ?? Vector3.zero, collider);

        public static Transform BoundaryBlock(
            string name,
            Transform parent,
            Vector3 position,
            Vector3 scale,
            Material material,
            Vector3? euler = null,
            bool collider = false)
            => Block(name, parent, position, scale, material,
                CathedralRoleV24.StructuralRole.BoundaryWall, euler ?? Vector3.zero, collider);

        public static Transform Column(
            string name,
            Transform parent,
            Vector3 position,
            Vector3 scale,
            Material shaftMaterial,
            Material accentMaterial,
            bool collider = true)
        {
            Transform root = Node(name, parent);
            root.position = position;

            float baseHeight = Mathf.Max(0.12f, scale.y * 0.055f);
            float capHeight = Mathf.Max(0.10f, scale.y * 0.045f);
            float shaftHeight = Mathf.Max(0.2f, scale.y - baseHeight - capHeight);

            Block("Base", root, new Vector3(0f, -scale.y * 0.5f + baseHeight * 0.5f, 0f),
                new Vector3(scale.x * 1.28f, baseHeight, scale.z * 1.28f), accentMaterial,
                CathedralRoleV24.StructuralRole.StructuralSupport, Vector3.zero, false);

            MeshPart("Shaft", root, ProductionMeshLibraryV09.FlutedColumn(),
                new Vector3(0f, (baseHeight - capHeight) * 0.5f, 0f),
                new Vector3(scale.x, shaftHeight, scale.z), shaftMaterial,
                CathedralRoleV24.StructuralRole.StructuralSupport, collider, 0.72f);

            Block("Capital", root, new Vector3(0f, scale.y * 0.5f - capHeight * 0.5f, 0f),
                new Vector3(scale.x * 1.42f, capHeight, scale.z * 1.42f), accentMaterial,
                CathedralRoleV24.StructuralRole.StructuralSupport, Vector3.zero, false);
            return root;
        }

        public static Transform PointedArch(
            string name,
            Transform parent,
            Vector3 position,
            Vector3 scale,
            Material material,
            Vector3? euler = null)
        {
            return MeshPart(name, parent, ProductionMeshLibraryV09.PointedArch(), position, scale, material,
                CathedralRoleV24.StructuralRole.StructuralSupport, false, 0.75f, euler ?? Vector3.zero);
        }

        public static Transform Buttress(
            string name,
            Transform parent,
            Vector3 position,
            Vector3 scale,
            Material material,
            Material accent,
            Vector3? euler = null,
            bool collider = true)
        {
            Transform root = Node(name, parent);
            root.position = position;
            root.rotation = Quaternion.Euler(euler ?? Vector3.zero);

            Block("Foot", root, new Vector3(0f, -scale.y * 0.34f, 0f),
                new Vector3(scale.x * 1.18f, scale.y * 0.32f, scale.z * 1.20f), material,
                CathedralRoleV24.StructuralRole.StructuralSupport, Vector3.zero, collider);
            Block("Body", root, new Vector3(0f, scale.y * 0.02f, 0f),
                new Vector3(scale.x, scale.y * 0.58f, scale.z), material,
                CathedralRoleV24.StructuralRole.StructuralSupport, Vector3.zero, collider);
            Block("Crown", root, new Vector3(0f, scale.y * 0.35f, 0f),
                new Vector3(scale.x * 0.76f, scale.y * 0.17f, scale.z * 0.78f), accent,
                CathedralRoleV24.StructuralRole.StructuralSupport, Vector3.zero, false);
            return root;
        }

        public static Transform WallPanel(
            string name,
            Transform parent,
            Vector3 position,
            Vector3 scale,
            Material frame,
            Material inset,
            Vector3? euler = null)
        {
            Transform root = Node(name, parent);
            root.position = position;
            root.rotation = Quaternion.Euler(euler ?? Vector3.zero);
            Block("Panel", root, Vector3.zero, scale, frame,
                CathedralRoleV24.StructuralRole.BoundaryWall, Vector3.zero, false);
            Block("Inset", root, new Vector3(0f, scale.y * 0.06f, -scale.z * 0.52f),
                new Vector3(scale.x * 0.58f, scale.y * 0.62f, Mathf.Max(0.04f, scale.z * 0.08f)), inset,
                CathedralRoleV24.StructuralRole.DecorativePatina, Vector3.zero, false);
            return root;
        }

        public static Transform LumenSconce(
            string name,
            Transform parent,
            Vector3 position,
            Material housing,
            Material glow,
            Vector3? euler = null)
        {
            Transform root = Node(name, parent);
            root.position = position;
            root.rotation = Quaternion.Euler(euler ?? Vector3.zero);
            Block("Housing", root, Vector3.zero, new Vector3(0.30f, 0.52f, 0.24f), housing,
                CathedralRoleV24.StructuralRole.MysticAccent, Vector3.zero, false);
            Block("Core", root, new Vector3(0f, 0.08f, -0.16f), new Vector3(0.10f, 0.26f, 0.055f), glow,
                CathedralRoleV24.StructuralRole.MysticAccent, Vector3.zero, false);
            return root;
        }

        public static Transform BeamBetween(
            string name,
            Transform parent,
            Vector3 start,
            Vector3 end,
            float width,
            float thickness,
            Material material,
            CathedralRoleV24.StructuralRole role = CathedralRoleV24.StructuralRole.StructuralSupport)
        {
            Vector3 delta = end - start;
            if (delta.sqrMagnitude < 0.0001f) return Node(name, parent);
            Transform beam = Block(name, parent, (start + end) * 0.5f,
                new Vector3(width, thickness, delta.magnitude), material, role, Vector3.zero, false);
            beam.rotation = Quaternion.LookRotation(delta.normalized, Vector3.up);
            return beam;
        }

        public static Transform Block(
            string name,
            Transform parent,
            Vector3 position,
            Vector3 scale,
            Material material,
            CathedralRoleV24.StructuralRole role,
            Vector3 euler,
            bool collider)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = scale;
            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }
            Collider existing = go.GetComponent<Collider>();
            if (!collider && existing != null) UnityEngine.Object.DestroyImmediate(existing);
            CathedralRoleV24 marker = go.AddComponent<CathedralRoleV24>();
            marker.Configure(role);
            return go.transform;
        }

        private static Transform MeshPart(
            string name,
            Transform parent,
            Mesh mesh,
            Vector3 position,
            Vector3 scale,
            Material material,
            CathedralRoleV24.StructuralRole role,
            bool collider,
            float colliderInset,
            Vector3? euler = null)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localRotation = Quaternion.Euler(euler ?? Vector3.zero);
            go.transform.localScale = scale;
            MeshFilter filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            MeshRenderer renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;

            if (collider && mesh != null)
            {
                Bounds bounds = mesh.bounds;
                BoxCollider proxy = go.AddComponent<BoxCollider>();
                proxy.center = bounds.center;
                proxy.size = bounds.size * Mathf.Clamp(colliderInset, 0.45f, 1f);
            }

            CathedralRoleV24 marker = go.AddComponent<CathedralRoleV24>();
            marker.Configure(role);
            return go.transform;
        }
    }
}
#endif
