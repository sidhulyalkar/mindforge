#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mindforge.Editor
{
    /// <summary>
    /// Sparse second-pass world dressing for V0.28.
    ///
    /// This stage deliberately reuses the already pinned/verified public-art cache. It fills the
    /// two places that still read under-authored after the first V0.28 pass: the long choir/ascent
    /// and the far side of the Fractured Signal apse. Everything remains outside the protected
    /// processional lane and boss-duel radius, and imported dressing is stripped of all physics.
    /// </summary>
    public static class ProfessionalWorldDetailV28Builder
    {
        public const string RootName = "Mindforge_Professional_World_Detail_V28";
        public const float RouteClearHalfWidth = 3.15f;
        public const float BossClearRadius = 14.4f;
        private const float BossCenterZ = 94f;

        private static readonly List<Transform> StagedProps = new List<Transform>(32);

        public static bool PresentInOpenScene()
            => EditorSceneLookup.FindIncludingInactive(RootName) != null;

        public static void ApplyOpenScene()
        {
            GameObject canonical = EditorSceneLookup.FindIncludingInactive(MindforgeDemoV11Builder.RootName);
            if (canonical == null)
                throw new UnityEditor.Build.BuildFailedException(
                    $"V0.28 world detail requires canonical world '{MindforgeDemoV11Builder.RootName}'.");
            if (!ProfessionalEncounterV28Builder.PresentInOpenScene())
                throw new UnityEditor.Build.BuildFailedException(
                    "V0.28 world detail must compose after ProfessionalEncounterV28Builder.");

            Apply(canonical.transform);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                "[Mindforge:V28] Professional world detail authored: choir side rhythm and distant apse reliquary framing installed with protected negative space.");
        }

        public static void Apply(Transform canonicalRoot)
        {
            if (canonicalRoot == null) throw new ArgumentNullException(nameof(canonicalRoot));
            Transform previous = canonicalRoot.Find(RootName);
            if (previous != null) UnityEngine.Object.DestroyImmediate(previous.gameObject);

            PublicAssetAcquisitionV28.EnsureAll();
            CathedralMaterialLibraryV24.Palette palette = CathedralMaterialLibraryV24.Ensure();
            Transform root = CathedralModuleLibraryV24.Node(RootName, canonicalRoot);
            StagedProps.Clear();

            BuildChoirAscent(root, palette);
            BuildDistantApse(root, palette);
            Validate(root);
        }

        private static void BuildChoirAscent(Transform root, CathedralMaterialLibraryV24.Palette palette)
        {
            Transform choir = CathedralModuleLibraryV24.Node("V28_Choir_Ascent_Detail", root);

            // The ascent is long enough that it previously read as a transition corridor rather
            // than a destination. Side-wall rhythm supplies scale without occupying the center.
            float[] torchZ = { 59.5f, 65.5f, 71.5f, 77.5f, 82.0f };
            for (int i = 0; i < torchZ.Length; i++)
            {
                float z = torchZ[i];
                float y = RouteElevation(z) + 2.55f;
                PlaceAbsolute(PublicAssetAcquisitionV28.TorchPath,
                    $"V28_Choir_Torch_L_{i:00}", choir,
                    new Vector3(-7.15f, y, z), Quaternion.Euler(0f, 90f, 0f), 0.86f, palette.Bronze);
                PlaceAbsolute(PublicAssetAcquisitionV28.TorchPath,
                    $"V28_Choir_Torch_R_{i:00}", choir,
                    new Vector3(7.15f, y, z), Quaternion.Euler(0f, -90f, 0f), 0.86f, palette.Bronze);
            }

            float[] bannerZ = { 62.5f, 74.5f, 81.5f };
            for (int i = 0; i < bannerZ.Length; i++)
            {
                float z = bannerZ[i];
                float y = RouteElevation(z) + 3.75f;
                PlaceAbsolute(PublicAssetAcquisitionV28.BannerPath,
                    $"V28_Choir_Banner_L_{i:00}", choir,
                    new Vector3(-7.42f, y, z), Quaternion.Euler(0f, 90f, 0f), 1.02f, palette.IvoryStone);
                PlaceAbsolute(PublicAssetAcquisitionV28.BannerPath,
                    $"V28_Choir_Banner_R_{i:00}", choir,
                    new Vector3(7.42f, y, z), Quaternion.Euler(0f, -90f, 0f), 1.02f, palette.IvoryStone);
            }

            // Two small prayer seats at the lower and upper choir landings. They are intentionally
            // farther from the route than the wall lights so they read as usable alcoves.
            float[] seatZ = { 61.0f, 79.5f };
            for (int i = 0; i < seatZ.Length; i++)
            {
                float z = seatZ[i];
                float floorY = RouteElevation(z);
                Transform left = PlaceAbsolute(PublicAssetAcquisitionV28.ChairPath,
                    $"V28_Choir_Seat_L_{i:00}", choir,
                    new Vector3(-6.55f, floorY + 0.10f, z), Quaternion.Euler(0f, 18f, 0f), 0.82f, palette.CoolShadowStone);
                GroundToY(left, floorY);
                Transform right = PlaceAbsolute(PublicAssetAcquisitionV28.ChairPath,
                    $"V28_Choir_Seat_R_{i:00}", choir,
                    new Vector3(6.55f, floorY + 0.10f, z), Quaternion.Euler(0f, -18f, 0f), 0.82f, palette.CoolShadowStone);
                GroundToY(right, floorY);
            }
        }

        private static void BuildDistantApse(Transform root, CathedralMaterialLibraryV24.Palette palette)
        {
            Transform apse = CathedralModuleLibraryV24.Node("V28_Distant_Apse_Detail", root);
            const float floorY = 4.095f;

            // The north framing sits safely beyond the duel radius. It gives the camera a deeper
            // terminus when looking through/around the creature without putting furniture in the fight.
            Vector3 leftTable = new Vector3(-8.4f, floorY, BossCenterZ + 17.2f);
            Vector3 rightTable = new Vector3(8.4f, floorY, BossCenterZ + 17.2f);
            Transform l = PlaceAbsolute(PublicAssetAcquisitionV28.TablePath,
                "V28_Apse_Reliquary_Table_L", apse, leftTable, Quaternion.Euler(0f, 12f, 0f), 0.88f, palette.CoolShadowStone);
            GroundToY(l, floorY);
            Transform r = PlaceAbsolute(PublicAssetAcquisitionV28.TablePath,
                "V28_Apse_Reliquary_Table_R", apse, rightTable, Quaternion.Euler(0f, -12f, 0f), 0.88f, palette.CoolShadowStone);
            GroundToY(r, floorY);

            Transform chestL = PlaceAbsolute(PublicAssetAcquisitionV28.ChestPath,
                "V28_Apse_Reliquary_L", apse,
                new Vector3(-8.4f, floorY + 0.8f, BossCenterZ + 17.2f), Quaternion.Euler(0f, 8f, 0f), 0.58f, palette.SacredGold);
            GroundToY(chestL, floorY + 0.82f);
            Transform chestR = PlaceAbsolute(PublicAssetAcquisitionV28.ChestPath,
                "V28_Apse_Reliquary_R", apse,
                new Vector3(8.4f, floorY + 0.8f, BossCenterZ + 17.2f), Quaternion.Euler(0f, -8f, 0f), 0.58f, palette.SacredGold);
            GroundToY(chestR, floorY + 0.82f);

            for (int side = -1; side <= 1; side += 2)
            {
                PlaceAbsolute(PublicAssetAcquisitionV28.BannerPath,
                    side < 0 ? "V28_Apse_Banner_L" : "V28_Apse_Banner_R", apse,
                    new Vector3(side * 10.6f, floorY + 6.8f, BossCenterZ + 17.8f),
                    Quaternion.Euler(0f, side < 0 ? 18f : -18f, 0f), 1.18f, palette.IvoryStone);
                PlaceAbsolute(PublicAssetAcquisitionV28.TorchPath,
                    side < 0 ? "V28_Apse_Torch_L" : "V28_Apse_Torch_R", apse,
                    new Vector3(side * 11.2f, floorY + 4.0f, BossCenterZ + 16.8f),
                    Quaternion.Euler(0f, side < 0 ? 35f : -35f, 0f), 0.94f, palette.Bronze);
            }
        }

        private static Transform PlaceAbsolute(
            string assetPath,
            string name,
            Transform parent,
            Vector3 position,
            Quaternion rotation,
            float scale,
            Material material)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab == null)
                throw new UnityEditor.Build.BuildFailedException($"V0.28 detail asset failed to import: {assetPath}");

            GameObject go = UnityEngine.Object.Instantiate(prefab, parent);
            go.name = name;
            go.transform.position = position;
            go.transform.rotation = rotation;
            go.transform.localScale = Vector3.one * scale;

            Collider[] colliders = go.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++) UnityEngine.Object.DestroyImmediate(colliders[i]);
            Rigidbody[] bodies = go.GetComponentsInChildren<Rigidbody>(true);
            for (int i = 0; i < bodies.Length; i++) UnityEngine.Object.DestroyImmediate(bodies[i]);

            Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null) continue;
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }

            StagedProps.Add(go.transform);
            return go.transform;
        }

        private static void GroundToY(Transform prop, float targetY)
        {
            if (!TryBounds(prop, out Bounds bounds)) return;
            prop.position += Vector3.up * (targetY - bounds.min.y + 0.006f);
        }

        private static float RouteElevation(float z)
        {
            if (z <= 54f) return 0f;
            if (z >= 86f) return 3.65f;
            return Mathf.Lerp(0f, 3.65f, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(54f, 86f, z)));
        }

        private static bool TryBounds(Transform root, out Bounds bounds)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            bool has = false;
            bounds = default;
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || !renderer.enabled) continue;
                if (!has)
                {
                    bounds = renderer.bounds;
                    has = true;
                }
                else bounds.Encapsulate(renderer.bounds);
            }
            return has;
        }

        private static void Validate(Transform root)
        {
            if (root.GetComponentsInChildren<Collider>(true).Length != 0)
                throw new UnityEditor.Build.BuildFailedException("V0.28 professional world detail must remain collider-free.");
            if (root.GetComponentsInChildren<Rigidbody>(true).Length != 0)
                throw new UnityEditor.Build.BuildFailedException("V0.28 professional world detail must remain Rigidbody-free.");
            if (StagedProps.Count < 20 || StagedProps.Count > 28)
                throw new UnityEditor.Build.BuildFailedException($"V0.28 expected 20-28 sparse detail props, found {StagedProps.Count}.");

            for (int i = 0; i < StagedProps.Count; i++)
            {
                Transform prop = StagedProps[i];
                if (prop == null) continue;
                Vector3 p = prop.position;
                bool inAscentBand = p.z >= 54f && p.z <= 86.5f;
                if (inAscentBand && Mathf.Abs(p.x) < RouteClearHalfWidth)
                    throw new UnityEditor.Build.BuildFailedException($"V0.28 detail prop violates ascent clearance: {prop.name} at {p}.");

                float bossDistance = new Vector2(p.x, p.z - BossCenterZ).magnitude;
                if (p.z > 86.5f && bossDistance < BossClearRadius)
                    throw new UnityEditor.Build.BuildFailedException($"V0.28 detail prop violates boss clear radius: {prop.name} at {p}.");
            }
        }
    }
}
#endif
