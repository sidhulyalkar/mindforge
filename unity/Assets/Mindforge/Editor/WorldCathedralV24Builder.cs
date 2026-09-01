#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Mindforge.Combat;
using Mindforge.World;

namespace Mindforge.Editor
{
    /// <summary>
    /// V0.24 is a deliberate re-art and architectural-composition pass.
    ///
    /// Earlier tranches made the world continuous and physically trustworthy; this pass removes
    /// the remaining "assembled from unrelated pieces" look by imposing one cathedral grammar:
    /// pale processional floors, ivory load-bearing structure, cool recessed foundations,
    /// restrained bronze/gold trim and signal colour only where it has semantic meaning.
    ///
    /// V0.24 does not replace V0.23 collision authority. It reuses the proven route/foundation,
    /// adds only edge-safe structural collision, and treats its floor geometry as presentation
    /// skins aligned to authoritative surfaces.
    /// </summary>
    public static class WorldCathedralV24Builder
    {
        public const string RootName = "Mindforge_White_Cathedral_V24";
        private const float ArenaCenterZ = 94f;
        private const float ArenaFloorY = 4.095f;

        public static bool PresentInOpenScene()
            => EditorSceneLookup.FindIncludingInactive(RootName) != null;

        public static void ApplyOpenScene()
        {
            GameObject canonical = EditorSceneLookup.FindIncludingInactive(MindforgeDemoV11Builder.RootName);
            if (canonical == null)
                throw new UnityEditor.Build.BuildFailedException(
                    $"V0.24 requires canonical world '{MindforgeDemoV11Builder.RootName}' in the open scene.");
            if (!WorldSoulV20Builder.PresentInOpenScene() ||
                !WorldCohesionV21Builder.PresentInOpenScene() ||
                !WorldIntegrityV22Builder.PresentInOpenScene() ||
                !WorldFoundationV23Builder.PresentInOpenScene())
            {
                throw new UnityEditor.Build.BuildFailedException(
                    "V0.24 must compose after V0.20 World Soul, V0.21 Arena + Patina, V0.22 World Integrity and V0.23 World Foundation.");
            }

            Apply(canonical.transform);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                "[Mindforge:V24] White Cathedral authored: legacy foreground clutter suppressed, " +
                "cathedral palette normalized, processional spine rebuilt, nave/cloister/choir/apse composed, " +
                "static lighting lifted and structural-role validation passed.");
        }

        public static void Apply(Transform canonicalRoot)
        {
            if (canonicalRoot == null) throw new ArgumentNullException(nameof(canonicalRoot));

            Transform previous = canonicalRoot.Find(RootName);
            if (previous != null) UnityEngine.Object.DestroyImmediate(previous.gameObject);

            CathedralMaterialLibraryV24.Palette palette = CathedralMaterialLibraryV24.Ensure();

            SuppressLegacyForegroundClutter(canonicalRoot);
            RethemeStructuralWorld(canonicalRoot, palette);

            Transform root = CathedralModuleLibraryV24.Node(RootName, canonicalRoot);
            BuildProcessionalSpine(canonicalRoot, root, palette);
            BuildSanctumNarthex(root, palette);
            BuildCausewayNave(root, palette);
            BuildMarketCloister(root, palette);
            BuildChoirAscent(canonicalRoot, root, palette);
            BuildFracturedSignalApse(root, palette);
            BuildVaultRhythm(root, palette);
            BuildStaticCathedralLighting(root);
            ConfigureRenderers(root);
            ValidateCathedral(canonicalRoot, root, palette);
        }

        private static void SuppressLegacyForegroundClutter(Transform canonicalRoot)
        {
            // These layers were useful while discovering the route, but their random scatter,
            // blocky facades and one-off props are exactly what make the current build read as a
            // pile of passes. Preserve far terrain, cavern enclosure, V23 foundations and boss
            // fracture history; remove only noisy foreground grammar that V0.24 replaces.
            string[] paths =
            {
                WorldSoulV20Builder.RootName + "/WorldSoul_Natural_Rock",
                WorldSoulV20Builder.RootName + "/WorldSoul_Sanctum_Grove",
                WorldSoulV20Builder.RootName + "/WorldSoul_Causeway_Banks",
                WorldSoulV20Builder.RootName + "/WorldSoul_Market_Ruins",
                WorldSoulV20Builder.RootName + "/WorldSoul_Ascent_Geology",
                WorldCohesionV21Builder.RootName + "/V21_Surface_Transitions",
                WorldCohesionV21Builder.RootName + "/V21_Foreground_Ecology",
                WorldCohesionV21Builder.RootName + "/V21_Near_City_Facades",
                WorldCohesionV21Builder.RootName + "/V21_Landmark_Composition",
                WorldIntegrityV22Builder.RootName + "/V22_Route_Luminance_Anchors",
                "V11_Skyline",
            };

            for (int i = 0; i < paths.Length; i++)
            {
                Transform target = canonicalRoot.Find(paths[i]);
                if (target != null) target.gameObject.SetActive(false);
            }

            Transform market = FindDeep(canonicalRoot, "V11_Market_of_Broken_Momentum");
            if (market != null)
            {
                DisableChildrenByPrefix(market, "MarketStall_", "MarketGarden_");
            }
        }

        private static void RethemeStructuralWorld(
            Transform canonicalRoot,
            CathedralMaterialLibraryV24.Palette palette)
        {
            Renderer[] renderers = canonicalRoot.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || !renderer.gameObject.activeInHierarchy) continue;
                if (renderer.GetComponentInParent<CombatantVitals>() != null) continue;
                if (IsSemanticSignal(renderer.gameObject.name, renderer.sharedMaterial != null ? renderer.sharedMaterial.name : string.Empty))
                    continue;

                string n = renderer.gameObject.name;
                Material replacement = null;

                if (ContainsAny(n, "Floor", "Road", "Ramp", "Platform", "Perch", "Dais", "Threshold", "Transition"))
                    replacement = palette.PaleFloor;
                else if (ContainsAny(n, "Gold"))
                    replacement = palette.SacredGold;
                else if (ContainsAny(n, "Column", "Arch", "Crown", "Spire", "Buttress", "Rib", "Facade"))
                    replacement = ContainsAny(n, "FractureSpire") ? palette.WhiteMarble : palette.IvoryStone;
                else if (ContainsAny(n, "Fracture", "Ember"))
                    replacement = palette.FractureDark;
                else if (ContainsAny(n, "Retainer", "Foundation", "Underlay", "Backing", "Backwall", "Boundary"))
                    replacement = palette.CoolShadowStone;
                else if (ContainsAny(n, "Terrain", "Landmass", "Highlands", "Rock", "Crater", "Earth"))
                    replacement = palette.CoolShadowStone;
                else if (ContainsAny(n, "Wall", "Sanctum", "Market", "Ascent", "Causeway"))
                    replacement = palette.IvoryStone;

                if (replacement != null) renderer.sharedMaterial = replacement;
                renderer.receiveShadows = true;
                renderer.shadowCastingMode = ShadowCastingMode.On;
            }
        }

        private static void BuildProcessionalSpine(
            Transform canonicalRoot,
            Transform root,
            CathedralMaterialLibraryV24.Palette palette)
        {
            Transform spine = CathedralModuleLibraryV24.Node("V24_Processional_Spine", root);

            // Horizontal skins are intentionally wafer-thin and collider-free. V0.11/V0.23 remain
            // the physical floor authority; these pieces establish one continuous visual language.
            CathedralModuleLibraryV24.FloorSkin("SanctumAisle", spine,
                new Vector3(0f, 0.018f, -13f), new Vector3(4.2f, 0.035f, 21.2f), palette.WhiteMarble);
            CathedralModuleLibraryV24.FloorSkin("CausewayAisle", spine,
                new Vector3(0f, 0.018f, 15f), new Vector3(3.8f, 0.035f, 33.4f), palette.WhiteMarble);
            CathedralModuleLibraryV24.FloorSkin("MarketAisle", spine,
                new Vector3(0f, 0.020f, 45f), new Vector3(5.2f, 0.040f, 23.0f), palette.WhiteMarble);
            CathedralModuleLibraryV24.FloorSkin("MarketTransept", spine,
                new Vector3(0f, 0.022f, 49f), new Vector3(18.0f, 0.042f, 3.2f), palette.WhiteMarble);

            AddAisleEdgePair(spine, "Sanctum", -13f, 21.0f, 2.45f, 0.024f, palette.Bronze);
            AddAisleEdgePair(spine, "Causeway", 15f, 33.0f, 2.15f, 0.024f, palette.Bronze);
            AddAisleEdgePair(spine, "Market", 45f, 22.7f, 2.85f, 0.026f, palette.SacredGold);

            // Explicit threshold bands make district changes look designed rather than like
            // independently placed floor rectangles meeting by accident.
            float[] thresholds = { -2.0f, 32.5f, 57.6f, 84.1f };
            for (int i = 0; i < thresholds.Length; i++)
            {
                CathedralModuleLibraryV24.Trim($"Threshold_{i:00}", spine,
                    new Vector3(0f, i == 3 ? 4.10f : 0.030f, thresholds[i]),
                    new Vector3(i == 2 ? 9.8f : 7.4f, 0.055f, 0.30f),
                    i == 3 ? palette.SacredGold : palette.Bronze);
            }

            Transform ramp = FindDeep(canonicalRoot, "AscentRamp");
            if (ramp == null)
                throw new UnityEditor.Build.BuildFailedException("V0.24 requires canonical AscentRamp.");

            Vector3 rampTop = ramp.position + ramp.up * (ramp.localScale.y * 0.5f + 0.020f);
            Transform rampAisle = CathedralModuleLibraryV24.FloorSkin("ChoirRampAisle", spine,
                Vector3.zero, new Vector3(3.9f, 0.040f, 25.8f), palette.WhiteMarble);
            rampAisle.position = rampTop;
            rampAisle.rotation = ramp.rotation;
            CathedralRoleV24 rampRole = rampAisle.GetComponent<CathedralRoleV24>();
            if (rampRole != null) rampRole.Configure(CathedralRoleV24.StructuralRole.WalkableFloor);
        }

        private static void BuildSanctumNarthex(
            Transform root,
            CathedralMaterialLibraryV24.Palette palette)
        {
            Transform zone = CathedralModuleLibraryV24.Node("V24_Sanctum_Narthex", root);
            float[] z = { -20f, -13f, -6f };
            for (int i = 0; i < z.Length; i++)
            {
                for (int sideIndex = 0; sideIndex < 2; sideIndex++)
                {
                    float side = sideIndex == 0 ? -1f : 1f;
                    CathedralModuleLibraryV24.Column($"CathedralColumn_Sanctum_{sideIndex}_{i}", zone,
                        new Vector3(side * 7.25f, 3.05f, z[i]), new Vector3(0.78f, 6.10f, 0.78f),
                        palette.IvoryStone, palette.WhiteMarble, true);
                    CathedralModuleLibraryV24.LumenSconce($"SanctumSconce_{sideIndex}_{i}", zone,
                        new Vector3(side * 9.30f, 2.55f, z[i]), palette.Bronze, palette.LumenCyan,
                        new Vector3(0f, side < 0f ? 90f : -90f, 0f));
                }

                CathedralModuleLibraryV24.PointedArch($"CathedralArch_Sanctum_{i}", zone,
                    new Vector3(0f, 5.15f, z[i]), new Vector3(5.9f, 5.8f, 1.0f), palette.WhiteMarble);
            }

            for (int sideIndex = 0; sideIndex < 2; sideIndex++)
            {
                float side = sideIndex == 0 ? -1f : 1f;
                CathedralModuleLibraryV24.WallPanel($"SanctumWallPanel_{sideIndex}", zone,
                    new Vector3(side * 9.58f, 2.45f, -13f), new Vector3(17.8f, 4.9f, 0.18f),
                    palette.IvoryStone, palette.CoolShadowStone,
                    new Vector3(0f, side < 0f ? 90f : -90f, 0f));
            }
        }

        private static void BuildCausewayNave(
            Transform root,
            CathedralMaterialLibraryV24.Palette palette)
        {
            Transform zone = CathedralModuleLibraryV24.Node("V24_Causeway_Nave", root);
            float[] z = { 2f, 8f, 14f, 20f, 26f, 31.5f };
            for (int i = 0; i < z.Length; i++)
            {
                for (int sideIndex = 0; sideIndex < 2; sideIndex++)
                {
                    float side = sideIndex == 0 ? -1f : 1f;
                    CathedralModuleLibraryV24.Column($"CathedralColumn_Nave_{sideIndex}_{i}", zone,
                        new Vector3(side * 4.35f, 2.75f, z[i]), new Vector3(0.60f, 5.50f, 0.60f),
                        palette.IvoryStone, palette.WhiteMarble, true);
                    if (i < z.Length - 1)
                    {
                        float panelZ = (z[i] + z[i + 1]) * 0.5f;
                        CathedralModuleLibraryV24.WallPanel($"NaveWallPanel_{sideIndex}_{i}", zone,
                            new Vector3(side * 4.83f, 1.70f, panelZ), new Vector3(5.0f, 3.15f, 0.14f),
                            palette.IvoryStone, palette.CoolShadowStone,
                            new Vector3(0f, side < 0f ? 90f : -90f, 0f));
                    }
                }

                CathedralModuleLibraryV24.PointedArch($"CathedralArch_Nave_{i}", zone,
                    new Vector3(0f, 4.80f, z[i]), new Vector3(4.8f, 5.0f, 0.88f), palette.WhiteMarble);
            }

            for (int i = 0; i < 4; i++)
            {
                float zLamp = 5f + i * 8f;
                CathedralModuleLibraryV24.LumenSconce($"NaveLumenL_{i}", zone,
                    new Vector3(-4.72f, 2.25f, zLamp), palette.Bronze, palette.LumenCyan, new Vector3(0f, 90f, 0f));
                CathedralModuleLibraryV24.LumenSconce($"NaveLumenR_{i}", zone,
                    new Vector3(4.72f, 2.25f, zLamp + 4f), palette.Bronze, palette.LumenCyan, new Vector3(0f, -90f, 0f));
            }
        }

        private static void BuildMarketCloister(
            Transform root,
            CathedralMaterialLibraryV24.Palette palette)
        {
            Transform zone = CathedralModuleLibraryV24.Node("V24_Market_Cloister", root);
            float[] z = { 36.5f, 42.5f, 48.5f, 54.5f };
            for (int i = 0; i < z.Length; i++)
            {
                for (int sideIndex = 0; sideIndex < 2; sideIndex++)
                {
                    float side = sideIndex == 0 ? -1f : 1f;
                    CathedralModuleLibraryV24.Column($"CathedralColumn_Cloister_{sideIndex}_{i}", zone,
                        new Vector3(side * 8.55f, 3.0f, z[i]), new Vector3(0.70f, 6.0f, 0.70f),
                        palette.IvoryStone, palette.WhiteMarble, true);
                    CathedralModuleLibraryV24.Buttress($"CloisterButtress_{sideIndex}_{i}", zone,
                        new Vector3(side * 10.0f, 2.15f, z[i]), new Vector3(0.80f, 4.3f, 1.35f),
                        palette.IvoryStone, palette.WhiteMarble,
                        new Vector3(0f, side < 0f ? 90f : -90f, 0f), false);
                }

                CathedralModuleLibraryV24.PointedArch($"CathedralArch_Cloister_{i}", zone,
                    new Vector3(0f, 5.35f, z[i]), new Vector3(6.6f, 5.9f, 1.0f), palette.WhiteMarble);
            }

            CathedralModuleLibraryV24.Trim("CloisterMedallionLong", zone,
                new Vector3(0f, 0.050f, 49f), new Vector3(11.0f, 0.055f, 0.22f), palette.SacredGold);
            CathedralModuleLibraryV24.Trim("CloisterMedallionCross", zone,
                new Vector3(0f, 0.052f, 49f), new Vector3(0.22f, 0.058f, 9.0f), palette.SacredGold);
        }

        private static void BuildChoirAscent(
            Transform canonicalRoot,
            Transform root,
            CathedralMaterialLibraryV24.Palette palette)
        {
            Transform zone = CathedralModuleLibraryV24.Node("V24_Choir_Ascent", root);
            float[] z = { 61f, 67f, 73f, 79f, 84f };
            for (int i = 0; i < z.Length; i++)
            {
                float routeY = RouteElevation(z[i]);
                CathedralModuleLibraryV24.PointedArch($"CathedralArch_Choir_{i}", zone,
                    new Vector3(0f, routeY + 4.8f, z[i]), new Vector3(5.2f, 5.2f, 0.90f), palette.WhiteMarble,
                    new Vector3(WorldFoundationV23Builder.AscentSlopeDegrees, 0f, 0f));

                for (int sideIndex = 0; sideIndex < 2; sideIndex++)
                {
                    float side = sideIndex == 0 ? -1f : 1f;
                    CathedralModuleLibraryV24.Buttress($"ChoirButtress_{sideIndex}_{i}", zone,
                        new Vector3(side * 6.65f, routeY + 1.65f, z[i]), new Vector3(0.85f, 3.3f, 1.3f),
                        palette.IvoryStone, palette.WhiteMarble,
                        new Vector3(WorldFoundationV23Builder.AscentSlopeDegrees, 0f, side < 0f ? -2f : 2f), false);
                }
            }

            Transform ramp = FindDeep(canonicalRoot, "AscentRamp");
            if (ramp != null)
            {
                Vector3 normal = ramp.up;
                for (int sideIndex = 0; sideIndex < 2; sideIndex++)
                {
                    float side = sideIndex == 0 ? -1f : 1f;
                    Transform rail = CathedralModuleLibraryV24.Trim($"ChoirRampTrim_{sideIndex}", zone,
                        Vector3.zero, new Vector3(0.16f, 0.075f, 25.8f), palette.SacredGold);
                    rail.position = ramp.position + ramp.right * (side * 4.65f) + normal * (ramp.localScale.y * 0.5f + 0.05f);
                    rail.rotation = ramp.rotation;
                }
            }
        }

        private static void BuildFracturedSignalApse(
            Transform root,
            CathedralMaterialLibraryV24.Palette palette)
        {
            Transform zone = CathedralModuleLibraryV24.Node("V24_Fractured_Signal_Apse", root);

            // The fight remains open. Architecture is placed just outside the widened V0.21 floor
            // and wall ring, so the boss/player movement contract is unchanged while the chamber
            // gains a legible sacred shell.
            const int columns = 10;
            const float radius = 19.4f;
            for (int i = 0; i < columns; i++)
            {
                float angle = i / (float)columns * Mathf.PI * 2f;
                Vector3 radial = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
                Vector3 p = new Vector3(radial.x * radius, ArenaFloorY + 3.2f, ArenaCenterZ + radial.z * radius);
                CathedralModuleLibraryV24.Column($"CathedralColumn_Apse_{i:00}", zone, p,
                    new Vector3(0.78f, 6.4f, 0.78f), palette.IvoryStone, palette.WhiteMarble, false);
                CathedralModuleLibraryV24.Buttress($"ApseButtress_{i:00}", zone,
                    new Vector3(radial.x * 21.2f, ArenaFloorY + 2.4f, ArenaCenterZ + radial.z * 21.2f),
                    new Vector3(0.90f, 4.8f, 1.6f), palette.CoolShadowStone, palette.IvoryStone,
                    new Vector3(0f, angle * Mathf.Rad2Deg, 0f), false);
            }

            // Pale sanctum ring surrounding the fracture medallion.
            const int ringSegments = 14;
            for (int i = 0; i < ringSegments; i++)
            {
                float angle = i / (float)ringSegments * Mathf.PI * 2f;
                float radiusFloor = 9.1f;
                Vector3 p = new Vector3(Mathf.Sin(angle) * radiusFloor, ArenaFloorY + 0.018f,
                    ArenaCenterZ + Mathf.Cos(angle) * radiusFloor);
                CathedralModuleLibraryV24.FloorSkin($"ApseFloorRing_{i:00}", zone, p,
                    new Vector3(2.05f, 0.035f, 4.0f), i % 2 == 0 ? palette.WhiteMarble : palette.PaleFloor,
                    new Vector3(0f, angle * Mathf.Rad2Deg, 0f));
            }

            // North apse triptych gives the boss a memorable architectural backdrop.
            float[] archX = { -8.0f, 0f, 8.0f };
            for (int i = 0; i < archX.Length; i++)
            {
                CathedralModuleLibraryV24.PointedArch($"BossApseArch_{i}", zone,
                    new Vector3(archX[i], 10.4f, 113.1f), new Vector3(4.0f, 7.3f, 1.15f),
                    i == 1 ? palette.WhiteMarble : palette.IvoryStone);
                CathedralModuleLibraryV24.LumenSconce($"BossApseLumen_{i}", zone,
                    new Vector3(archX[i], 8.0f, 112.4f), palette.Bronze,
                    i == 1 ? palette.SignalMagenta : palette.LumenCyan);
            }

            CathedralModuleLibraryV24.Trim("BossFractureAxis", zone,
                new Vector3(0f, ArenaFloorY + 0.065f, 94f), new Vector3(0.16f, 0.065f, 15.0f), palette.SignalMagenta);
        }

        private static void BuildVaultRhythm(
            Transform root,
            CathedralMaterialLibraryV24.Palette palette)
        {
            Transform zone = CathedralModuleLibraryV24.Node("V24_Vault_Rhythm", root);
            float[] z = { -2f, 33f, 58f, 84f, 112f };
            float[] y = { 8.2f, 8.6f, 9.8f, 12.1f, 13.0f };
            for (int i = 0; i < z.Length; i++)
            {
                CathedralModuleLibraryV24.PointedArch($"CathedralVaultRib_{i:00}", zone,
                    new Vector3(0f, y[i], z[i]), new Vector3(8.2f, 7.0f, 1.20f),
                    i == z.Length - 1 ? palette.CoolShadowStone : palette.IvoryStone);
            }
        }

        private static void BuildStaticCathedralLighting(Transform root)
        {
            Transform lights = CathedralModuleLibraryV24.Node("V24_Static_Lighting", root);
            Vector3[] positions =
            {
                new Vector3(-6.8f, 4.8f, -14f), new Vector3(6.8f, 4.8f, 12f),
                new Vector3(-8.0f, 5.6f, 45f), new Vector3(8.0f, 7.2f, 72f),
                new Vector3(-10.5f, 9.0f, 97f), new Vector3(10.5f, 9.0f, 97f),
            };
            Color[] colors =
            {
                new Color(1.0f, 0.90f, 0.72f), new Color(0.64f, 0.86f, 1.0f),
                new Color(1.0f, 0.88f, 0.70f), new Color(0.60f, 0.84f, 1.0f),
                new Color(0.92f, 0.42f, 0.82f), new Color(0.34f, 0.72f, 0.94f),
            };

            for (int i = 0; i < positions.Length; i++)
            {
                GameObject go = new GameObject($"CathedralStaticLight_{i:00}");
                go.transform.SetParent(lights, false);
                go.transform.position = positions[i];
                Light light = go.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = colors[i];
                light.intensity = i < 4 ? 0.78f : 0.62f;
                light.range = i < 4 ? 13f : 15f;
                light.shadows = LightShadows.None;
            }

            Light key = GameObject.Find("KeyLight")?.GetComponent<Light>();
            if (key != null)
            {
                key.color = new Color(1.0f, 0.95f, 0.86f);
                key.intensity = 1.28f;
                key.shadows = LightShadows.Soft;
                key.transform.rotation = Quaternion.Euler(52f, -32f, 0f);
            }

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.29f, 0.31f, 0.34f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.10f, 0.12f, 0.15f);
            RenderSettings.fogStartDistance = 78f;
            RenderSettings.fogEndDistance = 225f;
        }

        private static void AddAisleEdgePair(
            Transform parent,
            string prefix,
            float z,
            float length,
            float halfWidth,
            float height,
            Material material)
        {
            CathedralModuleLibraryV24.Trim(prefix + "EdgeL", parent,
                new Vector3(-halfWidth, 0.035f, z), new Vector3(0.08f, height, length), material);
            CathedralModuleLibraryV24.Trim(prefix + "EdgeR", parent,
                new Vector3(halfWidth, 0.035f, z), new Vector3(0.08f, height, length), material);
        }

        private static float RouteElevation(float z)
        {
            if (z <= 54f) return 0f;
            if (z >= 86f) return 3.65f;
            return Mathf.Lerp(0f, 3.65f, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(54f, 86f, z)));
        }

        private static void ValidateCathedral(
            Transform canonicalRoot,
            Transform root,
            CathedralMaterialLibraryV24.Palette palette)
        {
            string[] requiredFloors = { "SanctumFloor", "CausewayRoad", "MarketFloor", "AscentRamp", "FractureFloor" };
            for (int i = 0; i < requiredFloors.Length; i++)
            {
                Transform floor = FindDeep(canonicalRoot, requiredFloors[i]);
                if (floor == null)
                    throw new UnityEditor.Build.BuildFailedException($"V0.24 missing canonical floor {requiredFloors[i]}.");
                Renderer renderer = floor.GetComponent<Renderer>();
                Collider collider = floor.GetComponent<Collider>();
                if (renderer == null || collider == null)
                    throw new UnityEditor.Build.BuildFailedException(
                        $"V0.24 floor contract requires visible collision owner {requiredFloors[i]}.");
                if (renderer.sharedMaterial != palette.PaleFloor)
                    throw new UnityEditor.Build.BuildFailedException(
                        $"V0.24 floor {requiredFloors[i]} escaped the canonical pale-floor material.");
            }

            CathedralRoleV24[] roles = root.GetComponentsInChildren<CathedralRoleV24>(true);
            int floors = 0;
            int supports = 0;
            int mystic = 0;
            for (int i = 0; i < roles.Length; i++)
            {
                if (roles[i] == null) continue;
                switch (roles[i].Role)
                {
                    case CathedralRoleV24.StructuralRole.WalkableFloor: floors++; break;
                    case CathedralRoleV24.StructuralRole.StructuralSupport: supports++; break;
                    case CathedralRoleV24.StructuralRole.MysticAccent: mystic++; break;
                }
            }
            if (floors < 12 || supports < 45 || mystic < 8)
                throw new UnityEditor.Build.BuildFailedException(
                    $"V0.24 cathedral kit under-populated: floors={floors}, supports={supports}, mystic={mystic}.");

            Renderer[] authored = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < authored.Length; i++)
            {
                Renderer renderer = authored[i];
                if (renderer == null) continue;
                if (renderer.GetComponent<CathedralRoleV24>() == null && renderer.GetComponentInParent<CathedralRoleV24>() == null)
                    throw new UnityEditor.Build.BuildFailedException(
                        $"V0.24 renderer '{renderer.name}' has no cathedral structural role.");
            }

            string[] suppressed =
            {
                WorldSoulV20Builder.RootName + "/WorldSoul_Natural_Rock",
                WorldCohesionV21Builder.RootName + "/V21_Surface_Transitions",
                WorldCohesionV21Builder.RootName + "/V21_Foreground_Ecology",
                "V11_Skyline",
            };
            for (int i = 0; i < suppressed.Length; i++)
            {
                Transform target = canonicalRoot.Find(suppressed[i]);
                if (target != null && target.gameObject.activeSelf)
                    throw new UnityEditor.Build.BuildFailedException(
                        $"V0.24 cleanup contract failed: noisy legacy layer still active at {suppressed[i]}.");
            }

            if (FindDeep(canonicalRoot, "AscentUnderlay") != null)
                throw new UnityEditor.Build.BuildFailedException(
                    "V0.24 requires the V0.23 removal of the contradictory +6.5 degree AscentUnderlay.");
        }

        private static void ConfigureRenderers(Transform root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null) continue;
                renderer.receiveShadows = true;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.lightProbeUsage = LightProbeUsage.BlendProbes;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.BlendProbes;
            }
        }

        private static bool IsSemanticSignal(string objectName, string materialName)
            => ContainsAny(objectName, "MemoryForgeCore", "SignalOrb", "Wisp", "Vep", "Stimulus", "Telegraph", "Aether") ||
               ContainsAny(materialName, "Signal", "Wisp", "Vep", "Stimulus", "Telegraph", "Aether");

        private static void DisableChildrenByPrefix(Transform root, params string[] prefixes)
        {
            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child == null) continue;
                for (int p = 0; p < prefixes.Length; p++)
                {
                    if (child.name.StartsWith(prefixes[p], StringComparison.Ordinal))
                    {
                        child.gameObject.SetActive(false);
                        break;
                    }
                }
            }
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root == null) return null;
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindDeep(root.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }

        private static bool ContainsAny(string source, params string[] needles)
        {
            if (string.IsNullOrEmpty(source)) return false;
            for (int i = 0; i < needles.Length; i++)
                if (source.IndexOf(needles[i], StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }
    }
}
#endif
