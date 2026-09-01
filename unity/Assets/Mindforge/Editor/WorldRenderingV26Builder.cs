#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Mindforge.World;

namespace Mindforge.Editor
{
    /// <summary>
    /// V0.26 production-geometry and depth pass.
    ///
    /// V0.23 remains collision authority, V0.24 remains architectural-layout authority and
    /// V0.25 remains sensory/post authority. V0.26 improves what those structures actually look
    /// like from the gameplay camera: primitive cube render meshes become chamfered production
    /// geometry, stacked-box buttresses become tapered silhouettes, wall panels gain recessed
    /// Gothic depth, transverse ribs receive continuous vault webs, and cavern/terrain materials
    /// regain depth separation from the white cathedral.
    ///
    /// This pass creates no gameplay colliders, attacks, movement state, neural evidence or
    /// temporal stimulus behaviour.
    /// </summary>
    public static class WorldRenderingV26Builder
    {
        public const string RootName = "Mindforge_Production_World_Rendering_V26";
        public const string GeneratedRoot = "Assets/Mindforge/Generated/V26";
        public const string MaterialRoot = GeneratedRoot + "/Materials";
        public const string DeepCavernMaterialPath = MaterialRoot + "/V26_DeepCavern.mat";
        public const string DistantStoneMaterialPath = MaterialRoot + "/V26_DistantStone.mat";
        public const string VaultPlasterMaterialPath = MaterialRoot + "/V26_VaultPlaster.mat";

        private static int _primitiveUpgrades;
        private static int _buttressShells;
        private static int _wallNiches;
        private static int _cavernDepthAssignments;
        private static int _terrainDepthAssignments;

        public static bool PresentInOpenScene()
            => EditorSceneLookup.FindIncludingInactive(RootName) != null;

        public static void ApplyOpenScene()
        {
            GameObject canonical = EditorSceneLookup.FindIncludingInactive(MindforgeDemoV11Builder.RootName);
            if (canonical == null)
                throw new UnityEditor.Build.BuildFailedException(
                    $"V0.26 requires canonical world '{MindforgeDemoV11Builder.RootName}'.");
            if (!WorldCathedralV24Builder.PresentInOpenScene() || !SensoryFidelityV25Builder.PresentInOpenScene())
                throw new UnityEditor.Build.BuildFailedException(
                    "V0.26 must compose after V0.24 White Cathedral and V0.25 Sensory Fidelity.");

            Apply(canonical.transform);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                $"[Mindforge:V26] Production world rendering authored: {_primitiveUpgrades} cube render meshes upgraded, " +
                $"{_buttressShells} buttress silhouettes replaced, {_wallNiches} recessed wall niches added, " +
                "continuous vault webs installed, cavern depth separated and tri-light ambience configured.");
        }

        public static void Apply(Transform canonicalRoot)
        {
            if (canonicalRoot == null) throw new ArgumentNullException(nameof(canonicalRoot));

            Transform previous = canonicalRoot.Find(RootName);
            if (previous != null) UnityEngine.Object.DestroyImmediate(previous.gameObject);

            _primitiveUpgrades = 0;
            _buttressShells = 0;
            _wallNiches = 0;
            _cavernDepthAssignments = 0;
            _terrainDepthAssignments = 0;

            EnsureFolder(MaterialRoot);
            CathedralMaterialLibraryV24.Palette palette = CathedralMaterialLibraryV24.Ensure();
            Material deepCavern = EnsureVariant(
                DeepCavernMaterialPath,
                palette.CoolShadowStone,
                new Color(0.19f, 0.225f, 0.265f, 1f),
                0.24f,
                0.04f);
            Material distantStone = EnsureVariant(
                DistantStoneMaterialPath,
                palette.CoolShadowStone,
                new Color(0.285f, 0.315f, 0.345f, 1f),
                0.20f,
                0.02f);
            Material vaultPlaster = EnsureVariant(
                VaultPlasterMaterialPath,
                palette.IvoryStone,
                new Color(0.78f, 0.79f, 0.785f, 1f),
                0.32f,
                0.02f);

            Transform root = CathedralModuleLibraryV24.Node(RootName, canonicalRoot);
            Transform cathedral = canonicalRoot.Find(WorldCathedralV24Builder.RootName);
            if (cathedral == null)
                throw new UnityEditor.Build.BuildFailedException("V0.26 could not resolve the V0.24 cathedral root.");

            UpgradePrimitiveCathedral(cathedral);
            BuildButtressSilhouettes(cathedral, root, palette);
            BuildWallNicheDepth(cathedral, root, palette);
            BuildContinuousVaultWebs(root, palette, vaultPlaster, deepCavern);
            ApplyWorldDepthMaterials(canonicalRoot, deepCavern, distantStone);
            ConfigureAtmosphericDepth();
            ExtendCathedralShadowReach();
            ConfigureRenderers(root);
            Validate(canonicalRoot, cathedral, root);
        }

        private static void UpgradePrimitiveCathedral(Transform cathedral)
        {
            Mesh production = ProductionGeometryV26.ChamferedBlock();
            MeshFilter[] filters = cathedral.GetComponentsInChildren<MeshFilter>(true);
            for (int i = 0; i < filters.Length; i++)
            {
                MeshFilter filter = filters[i];
                if (filter == null || filter.sharedMesh == null) continue;
                CathedralRoleV24 role = filter.GetComponent<CathedralRoleV24>();
                if (role == null) continue;
                if (role.Role == CathedralRoleV24.StructuralRole.WalkableFloor ||
                    role.Role == CathedralRoleV24.StructuralRole.MysticAccent)
                    continue;

                if (filter.sharedMesh == production)
                {
                    _primitiveUpgrades++;
                    continue;
                }

                if (!IsUnityCube(filter.sharedMesh)) continue;
                filter.sharedMesh = production;
                EditorUtility.SetDirty(filter);
                _primitiveUpgrades++;
            }
        }

        private static void BuildButtressSilhouettes(
            Transform cathedral,
            Transform root,
            CathedralMaterialLibraryV24.Palette palette)
        {
            Transform shells = CathedralModuleLibraryV24.Node("V26_Tapered_Buttresses", root);
            Transform[] all = cathedral.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                Transform source = all[i];
                if (source == null || source.name.IndexOf("Buttress", StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                Transform foot = source.Find("Foot");
                Transform body = source.Find("Body");
                Transform crown = source.Find("Crown");
                if (foot == null || body == null || crown == null) continue;

                float minY = float.PositiveInfinity;
                float maxY = float.NegativeInfinity;
                float width = 0f;
                float depth = 0f;
                Transform[] parts = { foot, body, crown };
                Material material = palette.IvoryStone;
                Material accent = palette.WhiteMarble;
                for (int p = 0; p < parts.Length; p++)
                {
                    Transform part = parts[p];
                    Vector3 s = part.localScale;
                    minY = Mathf.Min(minY, part.localPosition.y - s.y * 0.5f);
                    maxY = Mathf.Max(maxY, part.localPosition.y + s.y * 0.5f);
                    width = Mathf.Max(width, Mathf.Abs(s.x));
                    depth = Mathf.Max(depth, Mathf.Abs(s.z));
                    Renderer renderer = part.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        if (part == body && renderer.sharedMaterial != null) material = renderer.sharedMaterial;
                        if (part == crown && renderer.sharedMaterial != null) accent = renderer.sharedMaterial;
                        renderer.enabled = false;
                        EditorUtility.SetDirty(renderer);
                    }
                }

                float height = Mathf.Max(0.4f, maxY - minY);
                float centreY = (minY + maxY) * 0.5f;
                Vector3 shellPosition = source.TransformPoint(new Vector3(0f, centreY, 0f));
                CreateMeshPart(
                    $"V26_ButtressShell_{source.name}",
                    shells,
                    ProductionGeometryV26.TaperedButtress(),
                    shellPosition,
                    source.rotation,
                    new Vector3(Mathf.Max(0.20f, width), height, Mathf.Max(0.24f, depth)),
                    material,
                    CathedralRoleV24.StructuralRole.StructuralSupport);

                Vector3 finialPosition = source.TransformPoint(new Vector3(0f, maxY + height * 0.015f, 0f));
                CreateMeshPart(
                    $"V26_ButtressFinial_{source.name}",
                    shells,
                    ProductionMeshLibraryV09.CathedralSpire(),
                    finialPosition,
                    source.rotation,
                    new Vector3(width * 0.32f, height * 0.20f, depth * 0.32f),
                    accent,
                    CathedralRoleV24.StructuralRole.DecorativePatina);
                _buttressShells++;
            }
        }

        private static void BuildWallNicheDepth(
            Transform cathedral,
            Transform root,
            CathedralMaterialLibraryV24.Palette palette)
        {
            Transform niches = CathedralModuleLibraryV24.Node("V26_Recessed_Wall_Niches", root);
            Transform[] all = cathedral.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                Transform wall = all[i];
                if (wall == null || wall.name.IndexOf("WallPanel", StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                Transform panel = wall.Find("Panel");
                if (panel == null) continue;
                Vector3 panelScale = panel.localScale;
                float width = Mathf.Abs(panelScale.x);
                float height = Mathf.Abs(panelScale.y);
                float depth = Mathf.Abs(panelScale.z);
                int count = width > 8f ? 3 : 1;

                for (int n = 0; n < count; n++)
                {
                    float normalized = count == 1 ? 0f : (n / (float)(count - 1) - 0.5f) * 2f;
                    float x = normalized * width * 0.31f;
                    Vector3 localPosition = new Vector3(x, -height * 0.38f, -depth * 0.73f);
                    Vector3 worldPosition = wall.TransformPoint(localPosition);
                    float archX = width / (count * 2.72f);
                    float archY = height * 0.62f;
                    CreateMeshPart(
                        $"V26_NicheArch_{wall.name}_{n:00}",
                        niches,
                        ProductionMeshLibraryV09.PointedArch(),
                        worldPosition,
                        wall.rotation,
                        new Vector3(archX, archY, Mathf.Max(0.70f, depth * 4.0f)),
                        palette.WhiteMarble,
                        CathedralRoleV24.StructuralRole.DecorativePatina);

                    Vector3 sillLocal = new Vector3(x, -height * 0.34f, -depth * 0.88f);
                    Vector3 sillWorld = wall.TransformPoint(sillLocal);
                    CreateMeshPart(
                        $"V26_NicheSill_{wall.name}_{n:00}",
                        niches,
                        ProductionGeometryV26.ChamferedBlock(),
                        sillWorld,
                        wall.rotation,
                        new Vector3(archX * 1.75f, Mathf.Max(0.06f, height * 0.035f), Mathf.Max(0.08f, depth * 0.72f)),
                        palette.CoolShadowStone,
                        CathedralRoleV24.StructuralRole.DecorativePatina);
                    _wallNiches++;
                }
            }
        }

        private static void BuildContinuousVaultWebs(
            Transform root,
            CathedralMaterialLibraryV24.Palette palette,
            Material vaultPlaster,
            Material deepCavern)
        {
            Transform vaults = CathedralModuleLibraryV24.Node("V26_Continuous_Vault_Webs", root);
            float[] z = { -2f, 33f, 58f, 84f, 112f };
            float[] y = { 8.2f, 8.6f, 9.8f, 12.1f, 13.0f };

            for (int i = 0; i < z.Length - 1; i++)
            {
                float dz = z[i + 1] - z[i];
                float dy = y[i + 1] - y[i];
                float length = Mathf.Sqrt(dz * dz + dy * dy);
                float pitch = -Mathf.Atan2(dy, Mathf.Max(0.001f, dz)) * Mathf.Rad2Deg;
                Vector3 position = new Vector3(0f, (y[i] + y[i + 1]) * 0.5f + 0.18f, (z[i] + z[i + 1]) * 0.5f);
                Material material = i == z.Length - 2 ? deepCavern : vaultPlaster;
                Transform web = CreateMeshPart(
                    $"V26_VaultWeb_{i:00}",
                    vaults,
                    ProductionGeometryV26.VaultWeb(),
                    position,
                    Quaternion.Euler(pitch, 0f, 0f),
                    new Vector3(15.4f, i >= 2 ? 7.8f : 7.2f, length + 0.45f),
                    material,
                    CathedralRoleV24.StructuralRole.VaultCeiling);
                Renderer renderer = web.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.shadowCastingMode = ShadowCastingMode.On;
                    renderer.receiveShadows = true;
                }
            }

            // Longitudinal crown ribs stop the new web from reading as one smooth tent.
            float[] ribX = { -3.85f, 0f, 3.85f };
            for (int i = 0; i < ribX.Length; i++)
            {
                Transform rib = CreateMeshPart(
                    $"V26_LongitudinalVaultRib_{i:00}",
                    vaults,
                    ProductionGeometryV26.ChamferedBlock(),
                    new Vector3(ribX[i], 13.55f, 51.5f),
                    Quaternion.identity,
                    new Vector3(0.16f, 0.18f, 105.0f),
                    i == 1 ? palette.WhiteMarble : palette.IvoryStone,
                    CathedralRoleV24.StructuralRole.StructuralSupport);
                Renderer renderer = rib.GetComponent<Renderer>();
                if (renderer != null) renderer.receiveShadows = true;
            }
        }

        private static void ApplyWorldDepthMaterials(
            Transform canonicalRoot,
            Material deepCavern,
            Material distantStone)
        {
            Transform integrity = canonicalRoot.Find(WorldIntegrityV22Builder.RootName);
            Transform foundation = canonicalRoot.Find(WorldFoundationV23Builder.RootName);
            Transform soul = canonicalRoot.Find(WorldSoulV20Builder.RootName);

            if (integrity != null)
            {
                Renderer[] renderers = integrity.GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < renderers.Length; i++)
                {
                    Renderer renderer = renderers[i];
                    if (renderer == null) continue;
                    string name = renderer.gameObject.name;
                    if (!ContainsAny(name, "CavernVault", "CavernBackwall", "Backwall", "UpperBacking")) continue;
                    renderer.sharedMaterial = deepCavern;
                    renderer.receiveShadows = true;
                    _cavernDepthAssignments++;
                }
            }

            if (foundation != null)
            {
                Renderer[] renderers = foundation.GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < renderers.Length; i++)
                {
                    Renderer renderer = renderers[i];
                    if (renderer == null) continue;
                    string name = renderer.gameObject.name;
                    if (!ContainsAny(name, "UpperSealRock", "UpperBacking")) continue;
                    renderer.sharedMaterial = deepCavern;
                    renderer.receiveShadows = true;
                    _cavernDepthAssignments++;
                }
            }

            if (soul != null)
            {
                Renderer[] renderers = soul.GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < renderers.Length; i++)
                {
                    Renderer renderer = renderers[i];
                    if (renderer == null) continue;
                    string name = renderer.gameObject.name;
                    if (!ContainsAny(name, "Landmass", "Highlands", "Terrain")) continue;
                    renderer.sharedMaterial = distantStone;
                    renderer.receiveShadows = true;
                    _terrainDepthAssignments++;
                }
            }
        }

        private static void ConfigureAtmosphericDepth()
        {
            // V0.25 intentionally used flat fill to expose the new white world. Once the geometry
            // is established, tri-light ambience restores vertical depth without making the route dark.
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.50f, 0.515f, 0.545f, 1f);
            RenderSettings.ambientEquatorColor = new Color(0.325f, 0.355f, 0.395f, 1f);
            RenderSettings.ambientGroundColor = new Color(0.155f, 0.175f, 0.205f, 1f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.245f, 0.275f, 0.315f, 1f);
            RenderSettings.fogStartDistance = 84f;
            RenderSettings.fogEndDistance = 238f;
            RenderSettings.reflectionIntensity = 0.86f;
        }

        private static void ExtendCathedralShadowReach()
        {
            UniversalRenderPipelineAsset pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(
                CompetitionProjectConfigurator.PipelineAssetPath);
            if (pipeline != null)
            {
                pipeline.shadowDistance = Mathf.Max(pipeline.shadowDistance, 68f);
                EditorUtility.SetDirty(pipeline);
            }
            QualitySettings.shadowDistance = Mathf.Max(QualitySettings.shadowDistance, 68f);
            QualitySettings.lodBias = Mathf.Max(QualitySettings.lodBias, 2.20f);
        }

        private static Material EnsureVariant(
            string path,
            Material source,
            Color tint,
            float smoothness,
            float metallic)
        {
            if (source == null)
                throw new UnityEditor.Build.BuildFailedException($"V0.26 material source missing for {path}.");

            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(source) { name = System.IO.Path.GetFileNameWithoutExtension(path) };
                AssetDatabase.CreateAsset(material, path);
            }
            else if (material.shader != source.shader)
            {
                material.shader = source.shader;
            }

            material.CopyPropertiesFromMaterial(source);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", tint);
            if (material.HasProperty("_Color")) material.SetColor("_Color", tint);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Transform CreateMeshPart(
            string name,
            Transform parent,
            Mesh mesh,
            Vector3 worldPosition,
            Quaternion worldRotation,
            Vector3 scale,
            Material material,
            CathedralRoleV24.StructuralRole role)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = worldPosition;
            go.transform.rotation = worldRotation;
            go.transform.localScale = scale;

            MeshFilter filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            MeshRenderer renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
            renderer.allowOcclusionWhenDynamic = true;
            CathedralRoleV24 marker = go.AddComponent<CathedralRoleV24>();
            marker.Configure(role);
            return go.transform;
        }

        private static void ConfigureRenderers(Transform root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null) continue;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
                renderer.allowOcclusionWhenDynamic = true;
            }
        }

        private static void Validate(Transform canonicalRoot, Transform cathedral, Transform root)
        {
            if (_primitiveUpgrades < 24)
                throw new UnityEditor.Build.BuildFailedException(
                    $"V0.26 expected at least 24 visible cube-mesh upgrades; observed {_primitiveUpgrades}.");
            if (_buttressShells < 10)
                throw new UnityEditor.Build.BuildFailedException(
                    $"V0.26 expected at least 10 tapered buttress shells; observed {_buttressShells}.");
            if (_wallNiches < 8)
                throw new UnityEditor.Build.BuildFailedException(
                    $"V0.26 expected at least 8 recessed wall niches; observed {_wallNiches}.");
            if (_cavernDepthAssignments < 4)
                throw new UnityEditor.Build.BuildFailedException(
                    $"V0.26 cavern depth material did not reach the enclosing shell; assignments={_cavernDepthAssignments}.");
            if (_terrainDepthAssignments < 4)
                throw new UnityEditor.Build.BuildFailedException(
                    $"V0.26 distant stone material did not reach all outer terrain; assignments={_terrainDepthAssignments}.");

            Transform vaults = root.Find("V26_Continuous_Vault_Webs");
            if (vaults == null || vaults.GetComponentsInChildren<MeshRenderer>(true).Length < 7)
                throw new UnityEditor.Build.BuildFailedException("V0.26 continuous vault web/rib composition is incomplete.");

            Mesh vaultMesh = ProductionGeometryV26.VaultWeb();
            if (vaultMesh == null || vaultMesh.normals == null || vaultMesh.normals.Length == 0 ||
                vaultMesh.normals[vaultMesh.normals.Length / 2].y > -0.35f)
                throw new UnityEditor.Build.BuildFailedException("V0.26 vault web must face inward/downward toward gameplay space.");

            MeshFilter[] filters = cathedral.GetComponentsInChildren<MeshFilter>(true);
            for (int i = 0; i < filters.Length; i++)
            {
                MeshFilter filter = filters[i];
                CathedralRoleV24 role = filter != null ? filter.GetComponent<CathedralRoleV24>() : null;
                if (filter == null || filter.sharedMesh == null || role == null) continue;
                if (role.Role == CathedralRoleV24.StructuralRole.StructuralSupport ||
                    role.Role == CathedralRoleV24.StructuralRole.BoundaryWall ||
                    role.Role == CathedralRoleV24.StructuralRole.RetainingSubstructure)
                {
                    if (IsUnityCube(filter.sharedMesh))
                        throw new UnityEditor.Build.BuildFailedException(
                            $"V0.26 left production-visible primitive cube mesh on '{filter.name}'.");
                }
            }

            if (root.GetComponentsInChildren<Collider>(true).Length != 0)
                throw new UnityEditor.Build.BuildFailedException("V0.26 production rendering root must remain collider-free.");
            if (root.GetComponentsInChildren<Rigidbody>(true).Length != 0)
                throw new UnityEditor.Build.BuildFailedException("V0.26 production rendering root must not create Rigidbody authority.");
            if (RenderSettings.ambientMode != AmbientMode.Trilight)
                throw new UnityEditor.Build.BuildFailedException("V0.26 tri-light ambient depth configuration was not applied.");
        }

        private static bool IsUnityCube(Mesh mesh)
            => mesh != null && string.Equals(mesh.name, "Cube", StringComparison.OrdinalIgnoreCase);

        private static bool ContainsAny(string value, params string[] tokens)
        {
            if (string.IsNullOrEmpty(value)) return false;
            for (int i = 0; i < tokens.Length; i++)
                if (value.IndexOf(tokens[i], StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            string parent = folder.Substring(0, folder.LastIndexOf('/'));
            string leaf = folder.Substring(folder.LastIndexOf('/') + 1);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
#endif
