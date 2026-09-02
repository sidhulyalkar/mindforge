#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Mindforge.Combat;
using Mindforge.Presentation;
using Mindforge.World;

namespace Mindforge.Editor
{
    /// <summary>
    /// V0.28 professional creature + world staging pass.
    ///
    /// This stage replaces the procedural V0.27 boss proxy with pinned CC0 authored anatomy,
    /// derives a trigger-only sword-contact envelope from that rendered body, installs a bounded
    /// camera occlusion guard, and adds sparse CC0 cathedral props from deterministic sockets.
    /// Existing movement, attack, damage, traversal and neural owners remain authoritative.
    /// </summary>
    public static class ProfessionalEncounterV28Builder
    {
        public const string RootName = "Mindforge_Professional_Encounter_V28";
        public const string WorldStagingName = "V28_Socketed_World_Staging";
        public const string CombatEnvelopeName = "V28_BeastCombatEnvelope";
        private const float TargetCreatureLength = 3.70f;
        private const float RouteClearHalfWidth = 3.15f;
        private const float BossClearRadius = 14.4f;
        private const float ArenaCenterZ = 94f;

        private static readonly List<Transform> StagedProps = new List<Transform>(48);

        public static bool PresentInOpenScene()
            => EditorSceneLookup.FindIncludingInactive(RootName) != null;

        public static void ApplyOpenScene()
        {
            GameObject canonical = EditorSceneLookup.FindIncludingInactive(MindforgeDemoV11Builder.RootName);
            if (canonical == null)
                throw new UnityEditor.Build.BuildFailedException(
                    $"V0.28 requires canonical world '{MindforgeDemoV11Builder.RootName}'.");
            if (!CombatEmbodimentV27Builder.PresentInOpenScene())
                throw new UnityEditor.Build.BuildFailedException(
                    "V0.28 must compose after V0.27 Guardian Embodiment + Fractured Beast.");

            PublicAssetAcquisitionV28.EnsureAll();
            Apply(canonical.transform);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                "[Mindforge:V28] Professional encounter authored: CC0 rigged creature, render-derived sword hurt envelope, " +
                "minimum-separation support, bounded actor occlusion guard and socketed cathedral staging installed.");
        }

        public static void Apply(Transform canonicalRoot)
        {
            if (canonicalRoot == null) throw new ArgumentNullException(nameof(canonicalRoot));
            Transform previous = canonicalRoot.Find(RootName);
            if (previous != null) UnityEngine.Object.DestroyImmediate(previous.gameObject);

            StagedProps.Clear();
            CathedralMaterialLibraryV24.Palette palette = CathedralMaterialLibraryV24.Ensure();
            Transform root = CathedralModuleLibraryV24.Node(RootName, canonicalRoot);
            Transform staging = CathedralModuleLibraryV24.Node(WorldStagingName, root);

            FracturedSignalDirector boss = FindSceneObject<FracturedSignalDirector>();
            if (boss == null)
                throw new UnityEditor.Build.BuildFailedException("V0.28 could not resolve the Fractured Signal boss.");

            BuildAuthoredCreature(boss.transform, palette);
            InstallActorOcclusionGuard();
            BuildSocketedWorldStaging(canonicalRoot, staging, palette);
            ReduceLegacyArenaNoise(canonicalRoot);
            Validate(root, boss.transform, staging);
        }

        private static void BuildAuthoredCreature(Transform boss, CathedralMaterialLibraryV24.Palette palette)
        {
            Transform previous = boss.Find(FracturedSignalCreaturePresentationV28.RootName);
            if (previous != null) UnityEngine.Object.DestroyImmediate(previous.gameObject);
            Transform oldEnvelope = boss.Find(CombatEnvelopeName);
            if (oldEnvelope != null) UnityEngine.Object.DestroyImmediate(oldEnvelope.gameObject);

            GameObject creatureAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PublicAssetAcquisitionV28.RhinoPath);
            if (creatureAsset == null)
                throw new UnityEditor.Build.BuildFailedException(
                    $"V0.28 could not load imported Gobkit creature at {PublicAssetAcquisitionV28.RhinoPath}. " +
                    "Confirm UnityGLTF release/2.20.0 resolved and rebuild Latest.");

            Transform creatureRoot = new GameObject(FracturedSignalCreaturePresentationV28.RootName).transform;
            creatureRoot.SetParent(boss, false);

            GameObject modelObject = UnityEngine.Object.Instantiate(creatureAsset, creatureRoot);
            modelObject.name = FracturedSignalCreaturePresentationV28.ModelName;
            Transform model = modelObject.transform;
            model.localPosition = Vector3.zero;
            model.localRotation = Quaternion.identity;
            model.localScale = Vector3.one;

            NormalizeCreature(model, boss);
            ConfigureCreatureRenderers(model);

            AnimationClip idle = FindClip(PublicAssetAcquisitionV28.RhinoPath, "idle");
            AnimationClip walk = FindClip(PublicAssetAcquisitionV28.RhinoPath, "walk");
            AnimationClip attack = FindClip(PublicAssetAcquisitionV28.RhinoPath, "attack");
            AnimationClip dead = FindClip(PublicAssetAcquisitionV28.RhinoPath, "dead");
            if (idle == null || walk == null || attack == null)
                throw new UnityEditor.Build.BuildFailedException(
                    $"V0.28 Rhino animation contract incomplete: idle={idle != null}, walk={walk != null}, attack={attack != null}, dead={dead != null}.");

            FracturedSignalCreaturePresentationV28 presentation = boss.GetComponent<FracturedSignalCreaturePresentationV28>();
            if (presentation == null) presentation = boss.gameObject.AddComponent<FracturedSignalCreaturePresentationV28>();
            presentation.Configure(model, idle, walk, attack, dead);

            DisableRetiredBossPresentation(boss);
            Bounds localBounds = ComputeLocalRenderBounds(boss, model);
            BuildCombatEnvelope(boss, localBounds);
        }

        private static void NormalizeCreature(Transform model, Transform boss)
        {
            Bounds bounds = ComputeWorldRenderBounds(model);
            if (bounds.size.sqrMagnitude < 0.001f)
                throw new UnityEditor.Build.BuildFailedException("V0.28 imported creature has no usable renderer bounds.");

            // Make the authored quadruped's major horizontal axis the encounter forward axis.
            if (bounds.size.x > bounds.size.z * 1.08f)
            {
                model.localRotation = Quaternion.Euler(0f, 90f, 0f);
                bounds = ComputeWorldRenderBounds(model);
            }

            float horizontalLength = Mathf.Max(bounds.size.x, bounds.size.z);
            float scale = TargetCreatureLength / Mathf.Max(0.1f, horizontalLength);
            model.localScale = model.localScale * scale;
            bounds = ComputeWorldRenderBounds(model);

            // Ground the actual rendered feet/belly against the boss pivot floor rather than
            // assuming an arbitrary source-model origin.
            float groundDelta = boss.position.y + 0.025f - bounds.min.y;
            model.position += Vector3.up * groundDelta;
            bounds = ComputeWorldRenderBounds(model);

            float finalLength = Mathf.Max(bounds.size.x, bounds.size.z);
            if (finalLength < 3.25f || finalLength > 4.15f)
                throw new UnityEditor.Build.BuildFailedException($"V0.28 creature normalization produced implausible length {finalLength:0.00}m.");
            if (bounds.size.y < 1.05f || bounds.size.y > 2.75f)
                throw new UnityEditor.Build.BuildFailedException($"V0.28 creature normalization produced implausible height {bounds.size.y:0.00}m.");
        }

        private static void ConfigureCreatureRenderers(Transform model)
        {
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                throw new UnityEditor.Build.BuildFailedException("V0.28 imported creature contains no renderers.");

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null) continue;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
                renderer.lightProbeUsage = LightProbeUsage.BlendProbes;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.BlendProbes;
            }
        }

        private static void BuildCombatEnvelope(Transform boss, Bounds b)
        {
            Transform envelope = new GameObject(CombatEnvelopeName).transform;
            envelope.SetParent(boss, false);
            envelope.gameObject.layer = boss.gameObject.layer;

            float length = Mathf.Max(1f, b.size.z);
            float width = Mathf.Max(0.8f, b.size.x);
            float height = Mathf.Max(0.9f, b.size.y);
            float zMin = b.min.z;
            float zMax = b.max.z;
            float y = b.center.y;

            HurtBox("V28_Hurt_Head", envelope,
                new Vector3(b.center.x, y + height * 0.07f, zMax - length * 0.13f),
                new Vector3(width * 0.78f, height * 0.70f, length * 0.27f), boss.gameObject.layer);
            HurtBox("V28_Hurt_Chest", envelope,
                new Vector3(b.center.x, y + height * 0.02f, b.center.z + length * 0.20f),
                new Vector3(width * 0.94f, height * 0.84f, length * 0.34f), boss.gameObject.layer);
            HurtBox("V28_Hurt_Midbody", envelope,
                new Vector3(b.center.x, y - height * 0.02f, b.center.z - length * 0.08f),
                new Vector3(width * 0.98f, height * 0.80f, length * 0.38f), boss.gameObject.layer);
            HurtBox("V28_Hurt_Rear", envelope,
                new Vector3(b.center.x, y - height * 0.04f, zMin + length * 0.15f),
                new Vector3(width * 0.82f, height * 0.70f, length * 0.28f), boss.gameObject.layer);

            Transform oldHull = FindDeep(boss, "V22_BossCombatHull");
            if (oldHull != null)
            {
                Collider[] old = oldHull.GetComponentsInChildren<Collider>(true);
                for (int i = 0; i < old.Length; i++) old[i].enabled = false;
            }
        }

        private static void HurtBox(string name, Transform parent, Vector3 center, Vector3 size, int layer)
        {
            GameObject go = new GameObject(name);
            go.layer = layer;
            go.transform.SetParent(parent, false);
            BoxCollider collider = go.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.center = center;
            collider.size = new Vector3(
                Mathf.Max(0.45f, size.x),
                Mathf.Max(0.55f, size.y),
                Mathf.Max(0.55f, size.z));
        }

        private static void DisableRetiredBossPresentation(Transform boss)
        {
            FracturedSignalBeastV27 v27 = boss.GetComponent<FracturedSignalBeastV27>();
            if (v27 != null) v27.enabled = false;
            FracturedSignalCharacterV19 v19 = boss.GetComponent<FracturedSignalCharacterV19>();
            if (v19 != null) v19.enabled = false;

            string[] names =
            {
                FracturedSignalBeastV27.RootName,
                FracturedSignalCharacterV19.RootName,
                "V11BossVisual",
                "FracturedSignalShowcaseAvatar",
                "FracturedSignalThreatSilhouette",
            };
            for (int i = 0; i < names.Length; i++)
            {
                Transform child = boss.Find(names[i]);
                if (child != null) child.gameObject.SetActive(false);
            }
        }

        private static void InstallActorOcclusionGuard()
        {
            Camera camera = FindSceneObject<Camera>();
            if (camera == null)
                throw new UnityEditor.Build.BuildFailedException("V0.28 could not resolve the canonical camera.");
            if (camera.GetComponent<MindforgeActorOcclusionGuardV28>() == null)
                camera.gameObject.AddComponent<MindforgeActorOcclusionGuardV28>();
        }

        private static void BuildSocketedWorldStaging(
            Transform canonicalRoot,
            Transform staging,
            CathedralMaterialLibraryV24.Palette palette)
        {
            Transform sanctum = CathedralModuleLibraryV24.Node("V28_Sanctum_Dressing", staging);
            Transform nave = CathedralModuleLibraryV24.Node("V28_Nave_Dressing", staging);
            Transform cloister = CathedralModuleLibraryV24.Node("V28_Cloister_Dressing", staging);

            // Narthex / Memory Forge: two readable side chapels, never the centre lane.
            PlaceGroundProp(canonicalRoot, "SanctumFloor", PublicAssetAcquisitionV28.TablePath,
                "V28_Sanctum_RelicTable_L", sanctum, new Vector3(-5.7f, 0f, 2.0f), 12f, 0.82f, palette.CoolShadowStone);
            PlaceGroundProp(canonicalRoot, "SanctumFloor", PublicAssetAcquisitionV28.TablePath,
                "V28_Sanctum_RelicTable_R", sanctum, new Vector3(5.7f, 0f, 2.0f), -12f, 0.82f, palette.CoolShadowStone);
            PlaceGroundProp(canonicalRoot, "SanctumFloor", PublicAssetAcquisitionV28.ChairPath,
                "V28_Sanctum_Chair_L", sanctum, new Vector3(-6.3f, 0f, -1.2f), 18f, 0.90f, palette.CoolShadowStone);
            PlaceGroundProp(canonicalRoot, "SanctumFloor", PublicAssetAcquisitionV28.ChairPath,
                "V28_Sanctum_Chair_R", sanctum, new Vector3(6.3f, 0f, -1.2f), -18f, 0.90f, palette.CoolShadowStone);

            // Processional nave: repeated wall rhythm at 8 m, centre aisle remains completely open.
            float[] naveZ = { -10f, -2f, 6f, 14f };
            for (int i = 0; i < naveZ.Length; i++)
            {
                PlaceWallProp(canonicalRoot, "CausewayRoad", PublicAssetAcquisitionV28.TorchPath,
                    $"V28_Nave_Torch_L_{i:00}", nave, new Vector3(-6.9f, 2.35f, naveZ[i]), 90f, 0.88f, palette.Bronze);
                PlaceWallProp(canonicalRoot, "CausewayRoad", PublicAssetAcquisitionV28.TorchPath,
                    $"V28_Nave_Torch_R_{i:00}", nave, new Vector3(6.9f, 2.35f, naveZ[i]), -90f, 0.88f, palette.Bronze);
            }
            float[] bannerZ = { -6f, 10f };
            for (int i = 0; i < bannerZ.Length; i++)
            {
                PlaceWallProp(canonicalRoot, "CausewayRoad", PublicAssetAcquisitionV28.BannerPath,
                    $"V28_Nave_Banner_L_{i:00}", nave, new Vector3(-7.25f, 3.35f, bannerZ[i]), 90f, 1.10f, palette.IvoryStone);
                PlaceWallProp(canonicalRoot, "CausewayRoad", PublicAssetAcquisitionV28.BannerPath,
                    $"V28_Nave_Banner_R_{i:00}", nave, new Vector3(7.25f, 3.35f, bannerZ[i]), -90f, 1.10f, palette.IvoryStone);
            }

            // Market / cloister: side-alcove relic furniture. The traversal cross remains empty.
            PlaceGroundProp(canonicalRoot, "MarketFloor", PublicAssetAcquisitionV28.TablePath,
                "V28_Cloister_RelicTable_L", cloister, new Vector3(-6.4f, 0f, -3.2f), 6f, 0.88f, palette.CoolShadowStone);
            PlaceGroundProp(canonicalRoot, "MarketFloor", PublicAssetAcquisitionV28.TablePath,
                "V28_Cloister_RelicTable_R", cloister, new Vector3(6.4f, 0f, 3.2f), -174f, 0.88f, palette.CoolShadowStone);
            PlaceGroundProp(canonicalRoot, "MarketFloor", PublicAssetAcquisitionV28.ChestPath,
                "V28_Cloister_Reliquary", cloister, new Vector3(7.2f, 0f, -5.6f), -12f, 0.78f, palette.SacredGold);
            PlaceGroundProp(canonicalRoot, "MarketFloor", PublicAssetAcquisitionV28.ChairPath,
                "V28_Cloister_Chair", cloister, new Vector3(-7.1f, 0f, 5.4f), 168f, 0.88f, palette.CoolShadowStone);
        }

        private static void PlaceGroundProp(
            Transform canonicalRoot,
            string anchorName,
            string assetPath,
            string name,
            Transform parent,
            Vector3 offset,
            float yaw,
            float scale,
            Material material)
        {
            Transform anchor = FindDeep(canonicalRoot, anchorName);
            if (anchor == null) throw new UnityEditor.Build.BuildFailedException($"V0.28 staging anchor missing: {anchorName}");
            float floorY = SurfaceTopY(anchor);
            Vector3 position = anchor.position + new Vector3(offset.x, 0f, offset.z);
            position.y = floorY;
            Transform prop = InstantiateProp(assetPath, name, parent, position, Quaternion.Euler(0f, yaw, 0f), scale, material);
            GroundImportedProp(prop, floorY);
        }

        private static void PlaceWallProp(
            Transform canonicalRoot,
            string anchorName,
            string assetPath,
            string name,
            Transform parent,
            Vector3 offset,
            float yaw,
            float scale,
            Material material)
        {
            Transform anchor = FindDeep(canonicalRoot, anchorName);
            if (anchor == null) throw new UnityEditor.Build.BuildFailedException($"V0.28 staging anchor missing: {anchorName}");
            float floorY = SurfaceTopY(anchor);
            Vector3 position = anchor.position + new Vector3(offset.x, offset.y, offset.z);
            position.y = floorY + offset.y;
            InstantiateProp(assetPath, name, parent, position, Quaternion.Euler(0f, yaw, 0f), scale, material);
        }

        private static Transform InstantiateProp(
            string assetPath,
            string name,
            Transform parent,
            Vector3 position,
            Quaternion rotation,
            float scale,
            Material material)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab == null) throw new UnityEditor.Build.BuildFailedException($"V0.28 staging asset failed to import: {assetPath}");
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

        private static void GroundImportedProp(Transform prop, float floorY)
        {
            Bounds bounds = ComputeWorldRenderBounds(prop);
            if (bounds.size.sqrMagnitude < 0.0001f) return;
            prop.position += Vector3.up * (floorY - bounds.min.y + 0.006f);
        }

        private static void ReduceLegacyArenaNoise(Transform canonicalRoot)
        {
            Transform rites = FindDeep(canonicalRoot, "V27_RiteFloor");
            if (rites != null)
            {
                for (int i = 0; i < rites.childCount; i++)
                {
                    Transform child = rites.GetChild(i);
                    if (child != null && child.name.StartsWith("V27_SignalRiteAxis_", StringComparison.Ordinal))
                    {
                        int index = ParseTrailingIndex(child.name);
                        if (index >= 0 && index % 2 == 1) child.gameObject.SetActive(false);
                    }
                }
            }
        }

        private static int ParseTrailingIndex(string name)
        {
            int underscore = name.LastIndexOf('_');
            if (underscore < 0 || underscore + 1 >= name.Length) return -1;
            return int.TryParse(name.Substring(underscore + 1), out int value) ? value : -1;
        }

        private static AnimationClip FindClip(string path, string token)
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            AnimationClip fallback = null;
            for (int i = 0; i < assets.Length; i++)
            {
                AnimationClip clip = assets[i] as AnimationClip;
                if (clip == null || clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase)) continue;
                if (fallback == null) fallback = clip;
                if (clip.name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0) return clip;
            }
            return fallback != null && string.Equals(token, "idle", StringComparison.OrdinalIgnoreCase) ? fallback : null;
        }

        private static Bounds ComputeWorldRenderBounds(Transform root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            bool has = false;
            Bounds bounds = default;
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || renderer.bounds.size.sqrMagnitude < 0.000001f) continue;
                if (!has)
                {
                    bounds = renderer.bounds;
                    has = true;
                }
                else bounds.Encapsulate(renderer.bounds);
            }
            return bounds;
        }

        private static Bounds ComputeLocalRenderBounds(Transform localSpace, Transform root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            bool has = false;
            Bounds bounds = default;
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null) continue;
                Bounds b = renderer.bounds;
                for (int x = -1; x <= 1; x += 2)
                for (int y = -1; y <= 1; y += 2)
                for (int z = -1; z <= 1; z += 2)
                {
                    Vector3 corner = b.center + Vector3.Scale(b.extents, new Vector3(x, y, z));
                    Vector3 local = localSpace.InverseTransformPoint(corner);
                    if (!has)
                    {
                        bounds = new Bounds(local, Vector3.zero);
                        has = true;
                    }
                    else bounds.Encapsulate(local);
                }
            }
            if (!has) throw new UnityEditor.Build.BuildFailedException("V0.28 could not derive local creature render bounds.");
            return bounds;
        }

        private static float SurfaceTopY(Transform root)
        {
            float top = root.position.y;
            bool observed = false;
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null) continue;
                top = observed ? Mathf.Max(top, renderer.bounds.max.y) : renderer.bounds.max.y;
                observed = true;
            }
            Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider collider = colliders[i];
                if (collider == null) continue;
                top = observed ? Mathf.Max(top, collider.bounds.max.y) : collider.bounds.max.y;
                observed = true;
            }
            return top;
        }

        private static void Validate(Transform root, Transform boss, Transform staging)
        {
            FracturedSignalCreaturePresentationV28 creature = boss.GetComponent<FracturedSignalCreaturePresentationV28>();
            if (creature == null || !creature.Configured)
                throw new UnityEditor.Build.BuildFailedException("V0.28 authored creature presentation is not configured.");

            Transform envelope = boss.Find(CombatEnvelopeName);
            if (envelope == null)
                throw new UnityEditor.Build.BuildFailedException("V0.28 beast combat envelope missing.");
            BoxCollider[] hurt = envelope.GetComponentsInChildren<BoxCollider>(true);
            if (hurt.Length != 4)
                throw new UnityEditor.Build.BuildFailedException($"V0.28 expected 4 anatomical hurt boxes, found {hurt.Length}.");
            for (int i = 0; i < hurt.Length; i++)
                if (!hurt[i].isTrigger) throw new UnityEditor.Build.BuildFailedException($"V0.28 hurt box must be trigger-only: {hurt[i].name}");
            if (envelope.GetComponentsInChildren<Rigidbody>(true).Length != 0)
                throw new UnityEditor.Build.BuildFailedException("V0.28 hurt envelope must not introduce a Rigidbody.");

            FracturedSignalFirstBossV19 movement = boss.GetComponent<FracturedSignalFirstBossV19>();
            if (movement == null || movement.MinimumSeparationDistance < 2.4f)
                throw new UnityEditor.Build.BuildFailedException("V0.28 boss minimum-separation contract is missing or too small.");

            if (FindSceneObject<MindforgeActorOcclusionGuardV28>() == null)
                throw new UnityEditor.Build.BuildFailedException("V0.28 actor occlusion guard missing from canonical camera.");

            if (staging.GetComponentsInChildren<Collider>(true).Length != 0 || staging.GetComponentsInChildren<Rigidbody>(true).Length != 0)
                throw new UnityEditor.Build.BuildFailedException("V0.28 decorative staging must remain collision-free.");
            if (StagedProps.Count < 16)
                throw new UnityEditor.Build.BuildFailedException($"V0.28 expected at least 16 staged props, found {StagedProps.Count}.");

            for (int i = 0; i < StagedProps.Count; i++)
            {
                Transform prop = StagedProps[i];
                Vector3 p = prop.position;
                if (p.z > -28f && p.z < 78f && Mathf.Abs(p.x) < RouteClearHalfWidth)
                    throw new UnityEditor.Build.BuildFailedException($"V0.28 prop violates processional clearance: {prop.name} at {p}");
                Vector2 arenaDelta = new Vector2(p.x, p.z - ArenaCenterZ);
                if (arenaDelta.magnitude < BossClearRadius)
                    throw new UnityEditor.Build.BuildFailedException($"V0.28 prop violates boss clear radius: {prop.name} at {p}");

                for (int j = i + 1; j < StagedProps.Count; j++)
                {
                    Vector3 q = StagedProps[j].position;
                    Vector2 delta = new Vector2(p.x - q.x, p.z - q.z);
                    if (delta.magnitude < 0.72f && Mathf.Abs(p.y - q.y) < 1.2f)
                        throw new UnityEditor.Build.BuildFailedException(
                            $"V0.28 staged props overlap excessively: {prop.name} and {StagedProps[j].name}.");
                }
            }
        }

        private static T FindSceneObject<T>() where T : Component
        {
            T[] all = Resources.FindObjectsOfTypeAll<T>();
            for (int i = 0; i < all.Length; i++)
            {
                T item = all[i];
                if (item != null && item.gameObject.scene.IsValid()) return item;
            }
            return null;
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root == null) return null;
            if (string.Equals(root.name, name, StringComparison.Ordinal)) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindDeep(root.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }
    }
}
#endif
