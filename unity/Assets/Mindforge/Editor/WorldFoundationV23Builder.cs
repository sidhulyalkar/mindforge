#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mindforge.Editor
{
    /// <summary>
    /// V0.23 reconciles visible world geometry with the collision world after the recording-driven
    /// V0.22 pass. It fixes the ascent fake-floor overlap, closes route collider seams, makes
    /// obvious solid scenery participate in contact/camera collision, and replaces the cavern
    /// ceiling with inward-facing topology so the shell is authored from the player's side.
    ///
    /// Public technique references:
    /// - SebLague/Procedural-Cave-Generation (MIT): render shell and physical shell share topology.
    /// - aadebdeb/ProceduralMesh (MIT): deterministic editor-generated mesh assets.
    ///
    /// No runtime animation, combat, neural, input, persistence or scheduler authority is added.
    /// </summary>
    public static class WorldFoundationV23Builder
    {
        public const string RootName = "Mindforge_World_Foundation_V23";
        public const float AscentSlopeDegrees = -8.1f;
        private const int VaultSeed = 22022;

        public static bool PresentInOpenScene()
            => EditorSceneLookup.FindIncludingInactive(RootName) != null;

        public static void ApplyOpenScene()
        {
            GameObject canonical = EditorSceneLookup.FindIncludingInactive(MindforgeDemoV11Builder.RootName);
            if (canonical == null)
                throw new UnityEditor.Build.BuildFailedException(
                    $"V0.23 requires canonical world '{MindforgeDemoV11Builder.RootName}' in the open scene.");
            if (!WorldSoulV20Builder.PresentInOpenScene() ||
                !WorldCohesionV21Builder.PresentInOpenScene() ||
                !WorldIntegrityV22Builder.PresentInOpenScene())
            {
                throw new UnityEditor.Build.BuildFailedException(
                    "V0.23 must compose after V0.20 World Soul, V0.21 Arena + Patina and V0.22 World Integrity.");
            }

            Apply(canonical.transform);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                "[Mindforge:V23] World Foundation authored: ascent visual/collision reconciliation, " +
                "continuous route seam guards, solid-scenery contact proxies, inward cavern ceiling and upper end seals.");
        }

        public static void Apply(Transform canonicalRoot)
        {
            if (canonicalRoot == null) throw new ArgumentNullException(nameof(canonicalRoot));

            Transform previous = canonicalRoot.Find(RootName);
            if (previous != null) UnityEngine.Object.DestroyImmediate(previous.gameObject);

            WorldSoulMaterialLibraryV20.Palette palette = WorldSoulMaterialLibraryV20.Ensure();
            Transform root = Node(RootName, canonicalRoot);

            RepairAscentVisualAuthority(canonicalRoot, root, palette);
            BuildRouteSeamGuards(root);
            AddStructuralContactProxies(canonicalRoot);
            RebuildInwardCavernCeiling(canonicalRoot);
            BuildUpperCavernSeals(root, palette);
            BuildRouteFoundations(root, palette);
            ConfigureStaticRenderers(root);
            ValidateFoundation(canonicalRoot, root);
        }

        private static void RepairAscentVisualAuthority(
            Transform canonicalRoot,
            Transform root,
            WorldSoulMaterialLibraryV20.Palette palette)
        {
            Transform underlayRoot = Require(
                canonicalRoot,
                WorldIntegrityV22Builder.RootName + "/V22_Continuous_Ground_Underlay");
            Transform stale = underlayRoot.Find("AscentUnderlay");
            if (stale == null)
                throw new UnityEditor.Build.BuildFailedException(
                    "V0.23 expected the V0.22 AscentUnderlay so it can repair the known recording-visible fake-floor overlap.");

            // V0.22 tilted this visual-only slab +6.5 degrees while the canonical collision ramp
            // tilts -8.1 degrees. The two solids cross through each other, so the player can look
            // as though they jump through a floor even when Rigidbody collision is correct.
            UnityEngine.Object.DestroyImmediate(stale.gameObject);

            Transform repair = Node("V23_Ascent_Visual_Reconciliation", root);
            Block(
                "AscentFoundationSkin",
                repair,
                new Vector3(0f, 1.28f, 71.3f),
                new Vector3(11.55f, 0.70f, 28.35f),
                palette.Basalt,
                new Vector3(AscentSlopeDegrees, 0f, 0f),
                false);

            // Narrow lower shoulders make the ramp read as masonry embedded in geology instead
            // of a floating plank. They sit outside the canonical traversal lane.
            for (int sideIndex = 0; sideIndex < 2; sideIndex++)
            {
                float side = sideIndex == 0 ? -1f : 1f;
                Block(
                    $"AscentFoundationShoulder_{sideIndex}",
                    repair,
                    new Vector3(side * 5.55f, 0.88f, 71.3f),
                    new Vector3(1.25f, 0.95f, 28.4f),
                    sideIndex == 0 ? palette.WornStone : palette.Basalt,
                    new Vector3(AscentSlopeDegrees, 0f, 0f),
                    false);
            }
        }

        private static void BuildRouteSeamGuards(Transform root)
        {
            Transform guards = Node("V23_Collision_Reconciliation", root);

            // Canonical visible floors remain the normal contact surface. These thin colliders sit
            // just underneath them and bridge only the tiny assembler seams, including the real
            // one-metre gap between CausewayRoad (ending at z=32) and MarketFloor (starting at z=33).
            CollisionBlock(
                "LowerRouteSeamGuard",
                guards,
                new Vector3(0f, -0.18f, 16.5f),
                new Vector3(8.15f, 0.16f, 81.4f),
                Vector3.zero);

            // A second guard follows the exact V0.11 ramp slope. It is below the authoritative
            // ramp collider and cannot become a higher phantom floor.
            CollisionBlock(
                "AscentSeamGuard",
                guards,
                new Vector3(0f, 1.34f, 71.3f),
                new Vector3(10.10f, 0.18f, 28.15f),
                new Vector3(AscentSlopeDegrees, 0f, 0f));

            // The arena floor was widened in V0.21. A very thin catcher immediately below its
            // underside prevents any wall/floor junction crack from becoming an escape chute.
            CollisionBlock(
                "BossArenaSeamGuard",
                guards,
                new Vector3(0f, 3.25f, 94f),
                new Vector3(35.6f, 0.16f, 33.6f),
                Vector3.zero);
        }

        private static void AddStructuralContactProxies(Transform canonicalRoot)
        {
            MeshFilter[] filters = canonicalRoot.GetComponentsInChildren<MeshFilter>(true);
            int added = 0;
            for (int i = 0; i < filters.Length; i++)
            {
                MeshFilter filter = filters[i];
                if (filter == null || filter.sharedMesh == null) continue;
                GameObject go = filter.gameObject;
                if (go.GetComponent<Collider>() != null) continue;
                if (!ShouldReceiveContactProxy(go.name)) continue;

                Bounds bounds = filter.sharedMesh.bounds;
                if (bounds.size.sqrMagnitude < 0.0001f) continue;

                BoxCollider proxy = go.AddComponent<BoxCollider>();
                proxy.center = bounds.center;
                Vector3 size = bounds.size;
                // Slightly inset proxies make rocks/columns feel solid without catching the
                // Guardian on tiny silhouette corners. The Guardian low-friction material owns
                // actual movement feel.
                proxy.size = new Vector3(
                    Mathf.Max(0.05f, size.x * 0.72f),
                    Mathf.Max(0.05f, size.y * 0.90f),
                    Mathf.Max(0.05f, size.z * 0.72f));
                added++;
            }

            if (added < 8)
                throw new UnityEditor.Build.BuildFailedException(
                    $"V0.23 expected to reconcile obvious solid scenery, but only added {added} contact proxies.");
        }

        private static bool ShouldReceiveContactProxy(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            return StartsWithAny(
                name,
                "SanctumColumn_",
                "CausewayPylon",
                "MarketColumn_",
                "AscentColumn",
                "FractureSpire_",
                "FieldRock_",
                "CausewayBankRock_",
                "CausewayWetStone_",
                "AscentToe_",
                "CraterRock_",
                "WallShoulder_",
                "ChamberButtress_");
        }

        private static void RebuildInwardCavernCeiling(Transform canonicalRoot)
        {
            Transform roof = Require(
                canonicalRoot,
                WorldIntegrityV22Builder.RootName + "/V22_Cavern_Vault/CavernVaultUnderside");
            MeshFilter filter = roof.GetComponent<MeshFilter>();
            MeshCollider collider = roof.GetComponent<MeshCollider>();
            if (filter == null || collider == null)
                throw new UnityEditor.Build.BuildFailedException(
                    "V0.23 requires the V0.22 cavern roof to own both MeshFilter and MeshCollider.");

            Mesh inward = WorldFoundationMeshLibraryV23.InwardTerrainPatch(
                "V23_CavernVaultInterior",
                WorldIntegrityV22Builder.CavernMinX,
                WorldIntegrityV22Builder.CavernMaxX,
                WorldIntegrityV22Builder.CavernMinZ,
                WorldIntegrityV22Builder.CavernMaxZ,
                28,
                72,
                CavernHeight);

            filter.sharedMesh = inward;
            collider.sharedMesh = null;
            collider.sharedMesh = inward;
        }

        private static float CavernHeight(float x, float z)
        {
            float side = Mathf.Clamp01(Mathf.Abs(x) / 52f);
            float vault = Mathf.Lerp(27.5f, 13.8f, Mathf.SmoothStep(0f, 1f, side));
            float bossLift = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(70f, 88f, z)) *
                             (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(116f, 136f, z))) * 5.8f;
            float broad = WorldSoulNoiseV20.Fbm(x + 41f, z - 23f, VaultSeed, 4, 31f, 0.53f, 2.0f) * 1.55f;
            float ridge = WorldSoulNoiseV20.Ridge(x - 17f, z + 9f, VaultSeed ^ 0x7771, 19f) * 1.15f;
            return vault + bossLift + broad + ridge;
        }

        private static void BuildUpperCavernSeals(
            Transform root,
            WorldSoulMaterialLibraryV20.Palette palette)
        {
            Transform seals = Node("V23_Cavern_Upper_End_Seals", root);

            // V0.22 side walls overlap the low roof edges, but the north/south back walls stop
            // several metres below the high centre of the vault. These upper seals close those
            // camera-visible sky wedges. Irregular rock masses in front hide the rectangular backing.
            Block(
                "SouthUpperBacking",
                seals,
                new Vector3(0f, 23.1f, -63.4f),
                new Vector3(102f, 10.8f, 5.5f),
                palette.Basalt,
                Vector3.zero,
                false);
            Block(
                "NorthUpperBacking",
                seals,
                new Vector3(0f, 23.7f, 172.8f),
                new Vector3(102f, 11.6f, 5.5f),
                palette.Basalt,
                Vector3.zero,
                false);

            for (int endIndex = 0; endIndex < 2; endIndex++)
            {
                float z = endIndex == 0 ? -60.9f : 170.2f;
                for (int i = 0; i < 9; i++)
                {
                    float x = -43f + i * 10.75f;
                    float y = 20.0f + Mathf.Abs(4 - i) * 0.45f;
                    MeshObject(
                        $"UpperSealRock_{endIndex}_{i:00}",
                        seals,
                        WorldSoulMeshLibraryV20.RockVariant(i + endIndex * 3),
                        i % 3 == 0 ? palette.WornStone : palette.Basalt,
                        new Vector3(x, y, z),
                        new Vector3(7.3f, 6.2f + (i % 2) * 1.6f, 4.2f),
                        new Vector3(endIndex == 0 ? -8f : 8f, i * 31f, (i - 4) * 1.7f));
                }
            }
        }

        private static void BuildRouteFoundations(
            Transform root,
            WorldSoulMaterialLibraryV20.Palette palette)
        {
            Transform foundations = Node("V23_Route_Foundations", root);

            // Retaining edges give the central path weight and hide the old blockout silhouette
            // when the camera looks over a ledge. They remain below/outside normal traversal.
            Block(
                "CausewayRetainerL",
                foundations,
                new Vector3(-4.72f, -0.62f, 15f),
                new Vector3(0.92f, 1.22f, 34.2f),
                palette.Basalt,
                Vector3.zero,
                false);
            Block(
                "CausewayRetainerR",
                foundations,
                new Vector3(4.72f, -0.62f, 15f),
                new Vector3(0.92f, 1.22f, 34.2f),
                palette.Basalt,
                Vector3.zero,
                false);
            Block(
                "MarketRetainerL",
                foundations,
                new Vector3(-9.62f, -0.77f, 45f),
                new Vector3(1.05f, 1.48f, 24.2f),
                palette.Earth,
                Vector3.zero,
                false);
            Block(
                "MarketRetainerR",
                foundations,
                new Vector3(9.62f, -0.77f, 45f),
                new Vector3(1.05f, 1.48f, 24.2f),
                palette.Earth,
                Vector3.zero,
                false);

            for (int i = 0; i < 5; i++)
            {
                float z = 61.5f + i * 5.2f;
                float routeY = Mathf.Lerp(
                    0.65f,
                    3.15f,
                    Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(61.5f, 82.3f, z)));
                for (int sideIndex = 0; sideIndex < 2; sideIndex++)
                {
                    float side = sideIndex == 0 ? -1f : 1f;
                    MeshObject(
                        $"AscentFoundationRock_{sideIndex}_{i:00}",
                        foundations,
                        WorldSoulMeshLibraryV20.RockVariant(i + sideIndex),
                        i % 2 == 0 ? palette.Basalt : palette.WornStone,
                        new Vector3(side * 5.85f, routeY - 0.45f, z),
                        new Vector3(1.35f, 1.05f, 2.15f),
                        new Vector3(side * 6f, i * 47f, side * 5f));
                }
            }
        }

        private static void ValidateFoundation(Transform canonicalRoot, Transform root)
        {
            if (canonicalRoot.Find(
                    WorldIntegrityV22Builder.RootName + "/V22_Continuous_Ground_Underlay/AscentUnderlay") != null)
            {
                throw new UnityEditor.Build.BuildFailedException(
                    "V0.23 validation failed: the crossing V0.22 AscentUnderlay is still present.");
            }

            Transform roof = Require(
                canonicalRoot,
                WorldIntegrityV22Builder.RootName + "/V22_Cavern_Vault/CavernVaultUnderside");
            Mesh mesh = roof.GetComponent<MeshFilter>()?.sharedMesh;
            if (mesh == null || mesh.normals == null || mesh.normals.Length == 0)
                throw new UnityEditor.Build.BuildFailedException("V0.23 cavern ceiling has no authored normals.");

            int centre = mesh.normals.Length / 2;
            if (mesh.normals[centre].y > -0.20f)
                throw new UnityEditor.Build.BuildFailedException(
                    $"V0.23 cavern ceiling is not inward-facing; centre normal={mesh.normals[centre]}.");

            Transform guards = root.Find("V23_Collision_Reconciliation");
            if (guards == null || guards.GetComponentsInChildren<BoxCollider>(true).Length != 3)
                throw new UnityEditor.Build.BuildFailedException(
                    "V0.23 collision reconciliation requires exactly three seam-guard colliders.");
        }

        private static Transform Require(Transform root, string path)
        {
            Transform found = root.Find(path);
            if (found == null)
                throw new UnityEditor.Build.BuildFailedException($"V0.23 missing required authored object: {path}");
            return found;
        }

        private static bool StartsWithAny(string source, params string[] prefixes)
        {
            for (int i = 0; i < prefixes.Length; i++)
                if (source.StartsWith(prefixes[i], StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private static Transform Node(string name, Transform parent)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        private static Transform Block(
            string name,
            Transform parent,
            Vector3 position,
            Vector3 scale,
            Material material,
            Vector3 euler,
            bool keepCollider)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = scale;
            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = material;
            Collider collider = go.GetComponent<Collider>();
            if (!keepCollider && collider != null) UnityEngine.Object.DestroyImmediate(collider);
            return go.transform;
        }

        private static void CollisionBlock(
            string name,
            Transform parent,
            Vector3 position,
            Vector3 size,
            Vector3 euler)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localRotation = Quaternion.Euler(euler);
            BoxCollider collider = go.AddComponent<BoxCollider>();
            collider.size = size;
        }

        private static void MeshObject(
            string name,
            Transform parent,
            Mesh mesh,
            Material material,
            Vector3 position,
            Vector3 scale,
            Vector3 euler)
        {
            if (mesh == null) return;
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = scale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
        }

        private static void ConfigureStaticRenderers(Transform root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null) continue;
                renderer.receiveShadows = true;
                renderer.lightProbeUsage = LightProbeUsage.BlendProbes;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.BlendProbes;
            }
        }
    }
}
#endif