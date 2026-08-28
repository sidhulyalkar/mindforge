#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mindforge.Editor
{
    /// <summary>
    /// Adds a small optional traversal playground to the Null Ward without changing
    /// encounter/world authority. The maintenance shortcut gains jumpable half-lane
    /// obstacles and raised pads while retaining a continuous ground bypass.
    ///
    /// This is intentionally authored as a replaceable editor layer so jump feel can be
    /// tuned independently from the qualified NullWard encounter topology.
    /// </summary>
    public static class NullWardTraversalPlayabilityBuilder
    {
        public const string RootName = "Mindforge_NullWard_TraversalPlayability_V1";
        private const float GuardianGroundedSpawnY = 0.72f;

        public static void ApplyOpenScene()
        {
            GameObject ward = EditorSceneLookup.FindIncludingInactive(NullWardSceneBuilder.RootName);
            if (ward == null)
                throw new InvalidOperationException("Null Ward traversal pass requires the Null Ward scene root.");

            GameObject old = EditorSceneLookup.FindIncludingInactive(RootName);
            if (old != null) UnityEngine.Object.DestroyImmediate(old);

            CinematicMaterialAuthoring.EnsureAuthored();
            Material basalt = RequireMaterial("ArenaBasalt");
            Material metal = RequireMaterial("GuardianMetal");
            Material viridian = RequireMaterial("WispVerdant");
            Material cyan = RequireMaterial("AetherCyan");

            // The original planar prototype authored the built-in Guardian capsule at
            // y=0.5 while FreezePositionY hid a small floor overlap. Once vertical motion
            // is real, give entry/respawn a deterministic physical clearance instead of
            // relying on one frame of Rigidbody depenetration.
            NormalizeMarkerHeight(ward.transform, "NullWard_WorldStart", GuardianGroundedSpawnY);
            NormalizeMarkerHeight(ward.transform, "MemoryForge_Respawn", GuardianGroundedSpawnY);

            GameObject root = new GameObject(RootName);
            root.transform.SetParent(ward.transform, false);
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;

            BuildMaintenanceJumpLine(root.transform, basalt, metal, viridian, cyan);
            BuildMarketPracticePlinths(root.transform, basalt, metal, cyan);

            EditorUtility.SetDirty(root);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log(
                "[Mindforge:Traversal] Added optional jumpable maintenance obstacles and landing pads. " +
                "The primary Null Ward route remains ground-completable.");
        }

        private static void BuildMaintenanceJumpLine(
            Transform parent,
            Material basalt,
            Material metal,
            Material viridian,
            Material cyan)
        {
            GameObject zone = new GameObject("Maintenance_JumpLine");
            zone.transform.SetParent(parent, false);

            // Half-lane signal blocks create a readable jump rhythm but always leave the
            // opposite side of the 5 m maintenance run open as a no-jump bypass.
            CreateJumpBlock(zone.transform, "SignalBlock_A", new Vector3(8.10f, 0.05f, -48.0f), new Vector3(1.65f, 0.70f, 1.15f), metal, viridian);
            CreateJumpBlock(zone.transform, "SignalBlock_B", new Vector3(9.95f, 0.10f, -43.0f), new Vector3(1.55f, 0.80f, 1.20f), basalt, cyan);
            CreateJumpBlock(zone.transform, "SignalBlock_C", new Vector3(8.05f, 0.16f, -38.2f), new Vector3(1.70f, 0.92f, 1.18f), metal, viridian);

            // A pair of low raised pads gives the jump somewhere to land rather than
            // making every obstacle a simple hurdle. They remain broad enough for the
            // forgiving air-control envelope and low enough to walk around safely.
            CreateLandingPad(zone.transform, "LandingPad_A", new Vector3(9.65f, -0.02f, -45.6f), new Vector3(1.65f, 0.55f, 2.05f), basalt, cyan);
            CreateLandingPad(zone.transform, "LandingPad_B", new Vector3(8.35f, 0.05f, -40.6f), new Vector3(1.75f, 0.68f, 2.10f), basalt, viridian);

            // Narrow luminous rails make the optional route legible at a glance without
            // requiring floating tutorial text or another permanent HUD widget.
            CreateRail(zone.transform, "JumpLine_Guide_Cyan", new Vector3(8.35f, -0.24f, -50.0f), new Vector3(8.35f, -0.24f, -35.5f), cyan);
            CreateRail(zone.transform, "JumpLine_Guide_Green", new Vector3(9.65f, -0.24f, -50.0f), new Vector3(9.65f, -0.24f, -35.5f), viridian);
        }

        private static void BuildMarketPracticePlinths(
            Transform parent,
            Material basalt,
            Material metal,
            Material cyan)
        {
            GameObject zone = new GameObject("Market_TraversalPlinths");
            zone.transform.SetParent(parent, false);

            // These sit on the quiet edge of the Market and do not alter encounter
            // blockers. They give players a harmless place to feel short/held jumps.
            CreateLandingPad(zone.transform, "PracticePlinth_Low", new Vector3(-8.2f, -0.08f, -27.7f), new Vector3(1.45f, 0.42f, 1.45f), basalt, cyan);
            CreateLandingPad(zone.transform, "PracticePlinth_High", new Vector3(-8.2f, 0.13f, -25.7f), new Vector3(1.45f, 0.84f, 1.45f), metal, cyan);
        }

        private static void CreateJumpBlock(
            Transform parent,
            string name,
            Vector3 localPosition,
            Vector3 size,
            Material body,
            Material signal)
        {
            GameObject block = Primitive(name, parent, localPosition, size, body, true);
            BoxCollider collider = block.GetComponent<BoxCollider>();
            if (collider != null) collider.sharedMaterial = null;

            Primitive(
                name + "_Signal",
                parent,
                localPosition + Vector3.up * (size.y * 0.52f + 0.035f),
                new Vector3(size.x * 0.76f, 0.055f, size.z * 0.78f),
                signal,
                false);
        }

        private static void CreateLandingPad(
            Transform parent,
            string name,
            Vector3 localPosition,
            Vector3 size,
            Material body,
            Material signal)
        {
            Primitive(name, parent, localPosition, size, body, true);
            Primitive(
                name + "_Edge",
                parent,
                localPosition + Vector3.up * (size.y * 0.52f + 0.025f),
                new Vector3(size.x * 0.88f, 0.045f, size.z * 0.88f),
                signal,
                false);
        }

        private static GameObject Primitive(
            string name,
            Transform parent,
            Vector3 localPosition,
            Vector3 size,
            Material material,
            bool collider)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = size;

            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = collider ? ShadowCastingMode.On : ShadowCastingMode.Off;
                renderer.receiveShadows = collider;
            }

            if (!collider)
            {
                Collider c = go.GetComponent<Collider>();
                if (c != null) UnityEngine.Object.DestroyImmediate(c);
            }
            else
            {
                GameObjectUtility.SetStaticEditorFlags(
                    go,
                    StaticEditorFlags.BatchingStatic |
                    StaticEditorFlags.OccluderStatic |
                    StaticEditorFlags.OccludeeStatic |
                    StaticEditorFlags.ReflectionProbeStatic);
            }

            return go;
        }

        private static void CreateRail(
            Transform parent,
            string name,
            Vector3 start,
            Vector3 end,
            Material material)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            LineRenderer line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.useWorldSpace = false;
            line.positionCount = 2;
            line.SetPosition(0, start);
            line.SetPosition(1, end);
            line.widthMultiplier = 0.035f;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
        }

        private static void NormalizeMarkerHeight(Transform root, string markerName, float localY)
        {
            Transform marker = FindRecursive(root, markerName);
            if (marker == null)
                throw new InvalidOperationException($"Traversal pass could not find required Null Ward marker {markerName}.");
            Vector3 local = marker.localPosition;
            local.y = localY;
            marker.localPosition = local;
            EditorUtility.SetDirty(marker);
        }

        private static Transform FindRecursive(Transform root, string name)
        {
            if (root == null) return null;
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindRecursive(root.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }

        private static Material RequireMaterial(string name)
        {
            Material material = CinematicMaterialAuthoring.Load(name);
            if (material == null)
                throw new InvalidOperationException($"Missing shared cinematic material {name}.");
            return material;
        }
    }
}
#endif
