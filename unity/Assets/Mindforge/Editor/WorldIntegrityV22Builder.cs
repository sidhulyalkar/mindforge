#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mindforge.Editor
{
    /// <summary>
    /// Final editor-authored world-integrity layer for V0.22.
    ///
    /// V0.20 owns landscape/material grammar and V0.21 owns the first-boss arena correction.
    /// V0.22 closes the remaining assembled-demo failure modes: stale transparent structural
    /// materials, visible void through floor seams, an open cavern top, disconnected side walls,
    /// and escape routes into un-authored space.
    ///
    /// Visible additions are static. The only new collision is the high cavern roof plus distant
    /// perimeter safety shell, deliberately outside ordinary route traversal.
    /// </summary>
    public static class WorldIntegrityV22Builder
    {
        public const string RootName = "Mindforge_World_Integrity_V22";
        public const float CavernMinX = -52f;
        public const float CavernMaxX = 52f;
        public const float CavernMinZ = -66f;
        public const float CavernMaxZ = 176f;
        private const int Seed = 22022;
        private const string GeneratedMaterialRoot = "Assets/Mindforge/Generated/V22/Materials";

        public static bool PresentInOpenScene()
            => EditorSceneLookup.FindIncludingInactive(RootName) != null;

        public static void ApplyOpenScene()
        {
            GameObject canonical = EditorSceneLookup.FindIncludingInactive(MindforgeDemoV11Builder.RootName);
            if (canonical == null)
                throw new UnityEditor.Build.BuildFailedException(
                    $"V0.22 requires canonical world '{MindforgeDemoV11Builder.RootName}' in the open scene.");
            if (!WorldSoulV20Builder.PresentInOpenScene() || !WorldCohesionV21Builder.PresentInOpenScene())
                throw new UnityEditor.Build.BuildFailedException(
                    "V0.22 must compose after V0.20 World Soul and V0.21 Arena + Patina.");

            Apply(canonical.transform);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                "[Mindforge:V22] World Integrity authored: opaque structural render state, continuous underlay, " +
                "sealed cavern vault/backing walls, distant safety envelope and integrated boss chamber crown.");
        }

        public static void Apply(Transform canonicalRoot)
        {
            if (canonicalRoot == null) throw new ArgumentNullException(nameof(canonicalRoot));

            Transform previous = canonicalRoot.Find(RootName);
            if (previous != null) UnityEngine.Object.DestroyImmediate(previous.gameObject);

            WorldSoulMaterialLibraryV20.Palette palette = WorldSoulMaterialLibraryV20.Ensure();
            NormalizeStructuralRenderState(canonicalRoot, palette);

            Transform root = Node(RootName, canonicalRoot);
            Material vaultBasalt = CloneOpaqueMaterial("V22_VaultBasalt", palette.Basalt, true);
            Material deepEarth = CloneOpaqueMaterial("V22_DeepEarth", palette.Earth, false);
            Material coolLumen = EnsureOpaqueGlowMaterial("V22_CoolLumen", new Color(0.08f, 0.34f, 0.52f), 1.35f);
            Material warmLumen = EnsureOpaqueGlowMaterial("V22_WarmLumen", new Color(0.68f, 0.22f, 0.055f), 1.10f);

            BuildContinuousGroundUnderlay(root, palette, deepEarth);
            BuildCavernEnvelope(root, palette, vaultBasalt);
            BuildBossChamberCrown(root, palette, coolLumen);
            BuildRouteLuminanceAnchors(root, palette, warmLumen, coolLumen);
            ConfigureStaticRenderers(root);
        }

        private static void NormalizeStructuralRenderState(
            Transform canonicalRoot,
            WorldSoulMaterialLibraryV20.Palette palette)
        {
            // World water is intentionally opaque for this stylized cavern. Surface identity comes
            // from color/normal/smoothness rather than depth-sorted transparency, which removes
            // shoreline holes and prevents ground from drawing through water in the wrong order.
            ForceOpaque(palette.Limestone);
            ForceOpaque(palette.Basalt);
            ForceOpaque(palette.WornStone);
            ForceOpaque(palette.Earth);
            ForceOpaque(palette.Moss);
            ForceOpaque(palette.Bark);
            ForceOpaque(palette.Foliage);
            ForceOpaque(palette.Water);
            ForceOpaque(palette.EmberStone);

            Renderer[] renderers = canonicalRoot.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null) continue;
                if (renderer.GetComponentInParent<Mindforge.Combat.CombatantVitals>() != null) continue;
                if (!LooksStructural(renderer.gameObject.name)) continue;

                Material[] materials = renderer.sharedMaterials;
                bool changed = false;
                for (int m = 0; m < materials.Length; m++)
                {
                    Material material = materials[m];
                    if (material == null || IsSemanticTransparentSurface(renderer.gameObject.name, material.name)) continue;
                    ForceOpaque(material);
                    changed = true;
                }
                if (changed) renderer.sharedMaterials = materials;
            }
        }

        private static bool LooksStructural(string name)
            => ContainsAny(name,
                "Floor", "Ground", "Road", "Ramp", "Platform", "Dais", "Terrain", "Landmass",
                "Highlands", "Rock", "Wall", "Arch", "Column", "Roof", "Facade", "Tower", "Cliff",
                "Crater", "Bank", "Rubble", "Stall", "Plinth", "Causeway", "Sanctum", "Market", "Ascent");

        private static bool IsSemanticTransparentSurface(string objectName, string materialName)
            => ContainsAny(objectName, "Glass", "Window", "SignalOrb", "Wisp", "Telegraph", "Vep", "Stimulus") ||
               ContainsAny(materialName, "Glass", "Signal", "Wisp", "Telegraph", "Vep", "Stimulus");

        private static void ForceOpaque(Material material)
        {
            if (material == null) return;
            if (material.HasProperty("_BaseColor"))
            {
                Color c = material.GetColor("_BaseColor");
                c.a = 1f;
                material.SetColor("_BaseColor", c);
            }
            else if (material.HasProperty("_Color"))
            {
                Color c = material.GetColor("_Color");
                c.a = 1f;
                material.SetColor("_Color", c);
            }

            if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 0f);
            if (material.HasProperty("_Blend")) material.SetFloat("_Blend", 0f);
            if (material.HasProperty("_SrcBlend")) material.SetFloat("_SrcBlend", (float)BlendMode.One);
            if (material.HasProperty("_DstBlend")) material.SetFloat("_DstBlend", (float)BlendMode.Zero);
            if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 1f);
            if (material.HasProperty("_AlphaClip")) material.SetFloat("_AlphaClip", 0f);
            if (material.HasProperty("_QueueOffset")) material.SetFloat("_QueueOffset", 0f);

            material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.DisableKeyword("_ALPHATEST_ON");
            material.SetOverrideTag("RenderType", "Opaque");
            material.renderQueue = (int)RenderQueue.Geometry;
            material.SetShaderPassEnabled("ShadowCaster", true);
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
        }

        private static void BuildContinuousGroundUnderlay(
            Transform root,
            WorldSoulMaterialLibraryV20.Palette palette,
            Material deepEarth)
        {
            Transform group = Node("V22_Continuous_Ground_Underlay", root);
            Block("LowerRouteUnderlay", group, new Vector3(0f, -1.10f, 10f),
                new Vector3(31f, 1.8f, 88f), deepEarth, Vector3.zero, false);
            Block("AscentUnderlay", group, new Vector3(0f, 1.20f, 70f),
                new Vector3(31f, 1.45f, 35f), palette.Basalt, new Vector3(6.5f, 0f, 0f), false);
            Block("BossPlateauUnderlay", group, new Vector3(0f, 2.78f, 101f),
                new Vector3(43f, 1.9f, 35f), palette.Basalt, Vector3.zero, false);
            Block("NorthContinuationUnderlay", group, new Vector3(0f, 2.05f, 143f),
                new Vector3(45f, 2.2f, 52f), palette.Earth, Vector3.zero, false);
        }

        private static void BuildCavernEnvelope(
            Transform root,
            WorldSoulMaterialLibraryV20.Palette palette,
            Material vaultBasalt)
        {
            Transform group = Node("V22_Cavern_Vault", root);
            Mesh vault = WorldSoulMeshLibraryV20.TerrainPatch(
                "V22_CavernVaultSurface", CavernMinX, CavernMaxX, CavernMinZ, CavernMaxZ, 28, 72, CavernHeight);

            GameObject roof = new GameObject("CavernVaultUnderside");
            roof.transform.SetParent(group, false);
            roof.AddComponent<MeshFilter>().sharedMesh = vault;
            MeshRenderer roofRenderer = roof.AddComponent<MeshRenderer>();
            roofRenderer.sharedMaterial = vaultBasalt;
            roofRenderer.shadowCastingMode = ShadowCastingMode.On;
            roofRenderer.receiveShadows = true;
            MeshCollider roofCollider = roof.AddComponent<MeshCollider>();
            roofCollider.sharedMesh = vault;
            roofCollider.convex = false;

            // Continuous backing volumes close long sightlines. Large irregular rock shoulders
            // sit in front of them, so the player gets a cavern silhouette rather than four boxes.
            Block("WestCavernBackwall", group, new Vector3(-50.7f, 8.2f, 55f),
                new Vector3(5.5f, 20.5f, 242f), palette.Basalt, Vector3.zero, false);
            Block("EastCavernBackwall", group, new Vector3(50.7f, 8.2f, 55f),
                new Vector3(5.5f, 20.5f, 242f), palette.Basalt, Vector3.zero, false);
            Block("SouthCavernBackwall", group, new Vector3(0f, 8.2f, -63.4f),
                new Vector3(102f, 20.5f, 5.5f), palette.Basalt, Vector3.zero, false);
            Block("NorthCavernBackwall", group, new Vector3(0f, 9.5f, 172.8f),
                new Vector3(102f, 23f, 5.5f), palette.Basalt, Vector3.zero, false);

            Transform transition = Node("V22_Vault_Wall_Transitions", group);
            for (int sideIndex = 0; sideIndex < 2; sideIndex++)
            {
                float side = sideIndex == 0 ? -1f : 1f;
                for (int i = 0; i < 13; i++)
                {
                    float z = -52f + i * 18f;
                    float x = side * Mathf.Lerp(44f, 48f, WorldSoulNoiseV20.Hash01(Seed, sideIndex * 100 + i));
                    float y = Mathf.Lerp(6.2f, 10.8f, WorldSoulNoiseV20.Hash01(Seed ^ 0x2211, sideIndex * 100 + i));
                    MeshObject($"WallShoulder_{sideIndex}_{i:00}", transition,
                        WorldSoulMeshLibraryV20.RockVariant(i + sideIndex * 7),
                        i % 4 == 0 ? palette.WornStone : palette.Basalt,
                        new Vector3(x, y, z), new Vector3(7.2f, 8.4f, 11.8f),
                        new Vector3(side * 6f, WorldSoulNoiseV20.Hash01(Seed, 500 + i) * 360f, side * 5f));
                }
            }

            float[] ribZ = { -24f, 2f, 29f, 55f, 81f, 119f, 151f };
            for (int i = 0; i < ribZ.Length; i++)
            {
                float z = ribZ[i];
                float y = Mathf.Min(17.5f, CavernHeight(0f, z) - 4.5f);
                MeshObject($"VaultRib_{i:00}", transition, ProductionMeshLibraryV09.PointedArch(),
                    i % 2 == 0 ? palette.Limestone : palette.WornStone,
                    new Vector3(0f, y, z), new Vector3(8.4f, 6.8f, 1.7f), Vector3.zero);
            }

            // Distant invisible safety collision prevents falling into or jumping out of the
            // un-authored exterior. It never replaces local route collision.
            Transform safety = Node("V22_Traversal_Envelope", root);
            Boundary("WestBoundary", safety, new Vector3(-53.5f, 10f, 55f), new Vector3(3f, 32f, 246f));
            Boundary("EastBoundary", safety, new Vector3(53.5f, 10f, 55f), new Vector3(3f, 32f, 246f));
            Boundary("SouthBoundary", safety, new Vector3(0f, 10f, -68.5f), new Vector3(110f, 32f, 3f));
            Boundary("NorthBoundary", safety, new Vector3(0f, 11f, 178.5f), new Vector3(110f, 34f, 3f));
        }

        private static float CavernHeight(float x, float z)
        {
            float side = Mathf.Clamp01(Mathf.Abs(x) / 52f);
            float vault = Mathf.Lerp(27.5f, 13.8f, Mathf.SmoothStep(0f, 1f, side));
            float bossLift = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(70f, 88f, z)) *
                             (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(116f, 136f, z))) * 5.8f;
            float broad = WorldSoulNoiseV20.Fbm(x + 41f, z - 23f, Seed, 4, 31f, 0.53f, 2.0f) * 1.55f;
            float ridge = WorldSoulNoiseV20.Ridge(x - 17f, z + 9f, Seed ^ 0x7771, 19f) * 1.15f;
            return vault + bossLift + broad + ridge;
        }

        private static void BuildBossChamberCrown(
            Transform root,
            WorldSoulMaterialLibraryV20.Palette palette,
            Material coolLumen)
        {
            Transform group = Node("V22_Fractured_Signal_Chamber", root);
            const float centerZ = 94f;
            for (int i = 0; i < 10; i++)
            {
                float angle = i / 10f * Mathf.PI * 2f;
                float radius = 23.4f + WorldSoulNoiseV20.SignedHash(Seed, 1400 + i) * 1.3f;
                Vector3 p = new Vector3(Mathf.Sin(angle) * radius, 11.6f, centerZ + Mathf.Cos(angle) * radius);
                MeshObject($"ChamberButtress_{i:00}", group,
                    i % 2 == 0 ? ProductionMeshLibraryV09.CathedralSpire() : WorldSoulMeshLibraryV20.RockVariant(i),
                    i % 3 == 0 ? palette.WornStone : palette.Basalt,
                    p, new Vector3(2.4f, 5.8f, 2.4f), new Vector3(0f, angle * Mathf.Rad2Deg, 0f));
            }

            for (int i = 0; i < 8; i++)
            {
                float angle = i / 8f * Mathf.PI * 2f;
                Vector3 dir = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
                BlockBetween($"ChamberCrownRib_{i:00}", group,
                    new Vector3(dir.x * 9f, 20.2f, centerZ + dir.z * 9f),
                    new Vector3(dir.x * 21.5f, 15.0f, centerZ + dir.z * 21.5f),
                    0.48f, palette.WornStone, 0.55f);
            }

            for (int i = 0; i < 6; i++)
            {
                float angle = i / 6f * Mathf.PI * 2f;
                Vector3 p = new Vector3(Mathf.Sin(angle) * 18.7f, 10.2f, centerZ + Mathf.Cos(angle) * 18.7f);
                Block($"ChamberLumen_{i:00}", group, p, new Vector3(0.18f, 0.72f, 0.18f),
                    coolLumen, Vector3.zero, false);
            }
        }

        private static void BuildRouteLuminanceAnchors(
            Transform root,
            WorldSoulMaterialLibraryV20.Palette palette,
            Material warmLumen,
            Material coolLumen)
        {
            Transform group = Node("V22_Route_Luminance_Anchors", root);
            Vector3[] anchors =
            {
                new Vector3(-8.9f, 2.5f, -18f), new Vector3(8.9f, 2.5f, -7f),
                new Vector3(-10.0f, 2.1f, 15f), new Vector3(10.0f, 2.1f, 29f),
                new Vector3(-9.8f, 4.7f, 51f), new Vector3(9.8f, 5.8f, 70f),
            };
            for (int i = 0; i < anchors.Length; i++)
            {
                Material glow = i < 2 ? warmLumen : coolLumen;
                Block($"RouteLumen_{i:00}", group, anchors[i], new Vector3(0.13f, 0.58f, 0.13f),
                    glow, Vector3.zero, false);
                MeshObject($"RouteLumenHousing_{i:00}", group, WorldSoulMeshLibraryV20.RockVariant(i + 2),
                    i % 2 == 0 ? palette.WornStone : palette.Basalt,
                    anchors[i] + new Vector3(0f, -0.62f, 0f), new Vector3(0.54f, 0.34f, 0.54f), Vector3.zero);
            }
        }

        private static Material CloneOpaqueMaterial(string name, Material source, bool doubleSided)
        {
            EnsureFolder(GeneratedMaterialRoot);
            string path = GeneratedMaterialRoot + "/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                if (source == null) throw new InvalidOperationException($"V0.22 cannot clone missing material for {name}.");
                material = new Material(source) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            else if (source != null && material.shader != source.shader)
            {
                material.shader = source.shader;
            }
            ForceOpaque(material);
            if (material.HasProperty("_Cull"))
                material.SetFloat("_Cull", doubleSided ? (float)CullMode.Off : (float)CullMode.Back);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material EnsureOpaqueGlowMaterial(string name, Color color, float intensity)
        {
            EnsureFolder(GeneratedMaterialRoot);
            string path = GeneratedMaterialRoot + "/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                if (shader == null) throw new InvalidOperationException("V0.22 requires a lit shader for static luminance anchors.");
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", new Color(color.r, color.g, color.b, 1f));
            else if (material.HasProperty("_Color")) material.SetColor("_Color", new Color(color.r, color.g, color.b, 1f));
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * Mathf.Max(0f, intensity));
            }
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.30f);
            ForceOpaque(material);
            return material;
        }

        private static Transform Node(string name, Transform parent)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        private static Transform Block(
            string name, Transform parent, Vector3 position, Vector3 scale,
            Material material, Vector3 euler, bool keepCollider)
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

        private static void MeshObject(
            string name, Transform parent, Mesh mesh, Material material,
            Vector3 position, Vector3 scale, Vector3 euler)
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

        private static void BlockBetween(
            string name, Transform parent, Vector3 start, Vector3 end,
            float width, Material material, float thickness)
        {
            Vector3 delta = end - start;
            if (delta.magnitude < 0.01f) return;
            Transform block = Block(name, parent, (start + end) * 0.5f,
                new Vector3(width, thickness, delta.magnitude), material, Vector3.zero, false);
            block.rotation = Quaternion.LookRotation(delta.normalized, Vector3.up);
        }

        private static void Boundary(string name, Transform parent, Vector3 position, Vector3 size)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            BoxCollider collider = go.AddComponent<BoxCollider>();
            collider.size = size;
        }

        private static bool ContainsAny(string source, params string[] needles)
        {
            if (string.IsNullOrEmpty(source)) return false;
            for (int i = 0; i < needles.Length; i++)
                if (source.IndexOf(needles[i], StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
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

        private static void EnsureFolder(string fullPath)
        {
            string[] parts = fullPath.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
#endif