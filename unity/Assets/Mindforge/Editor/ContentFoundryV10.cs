#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Mindforge.Editor
{
    /// <summary>
    /// Recipe-aware V0.10 presentation compiler. It accelerates art iteration while the
    /// canonical ShowcaseEditorMenu full rebuild remains the promotion authority.
    /// </summary>
    public static class ContentFoundryV10
    {
        public const string RecipeSchema = "mindforge.content_asset_recipe.v1";
        public const string BindingSchema = "mindforge.local_asset_bindings.v1";
        public const string ReplacementMarker = "__FoundryV10__";
        private const int MaxReplacementsPerRecipe = 64;

        [Serializable] private sealed class Recipe
        {
            public string schema;
            public string asset_id;
            public string semantic_role;
            public string[] districts;
            public Source source;
            public Geometry geometry;
            public Render render;
            public UnityTarget unity;
            public Authority authority;
            public Quality quality;
        }
        [Serializable] private sealed class Source { public string kind, tool, tool_version, license, redistribution_policy; }
        [Serializable] private sealed class Geometry
        {
            public float[] target_size_m;
            public string forward_axis, up_axis, pivot_policy;
            public int max_triangles, max_submeshes;
        }
        [Serializable] private sealed class Render
        {
            public string material_family;
            public int max_materials, texture_max_px;
            public float[] lod_ratios;
            public bool cast_shadows, receive_shadows;
        }
        [Serializable] private sealed class UnityTarget { public string[] target_tokens; public string fallback_symbol; }
        [Serializable] private sealed class Authority { public bool gameplay, collision, bci; }
        [Serializable] private sealed class Quality
        {
            public float minimum_score;
            public bool require_finite_normals, require_nonzero_bounds, reject_magenta_material;
        }
        [Serializable] private sealed class Bindings { public string schema; public Binding[] bindings; }
        [Serializable] private sealed class Binding { public string asset_id, unity_asset_path, expected_sha256; }
        [Serializable] private sealed class Cache
        {
            public string schema = "mindforge.content_foundry_unity_cache.v1";
            public string fingerprint, generated_utc;
            public int recipe_count, bound_asset_count;
        }
        private sealed class LoadedRecipe { public string Path, Raw; public Recipe Value; }

        private static string RepoRoot => Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
        private static string RecipesRoot => Path.Combine(RepoRoot, "content", "recipes");
        private static string BindingsPath => Path.Combine(RepoRoot, "content", "local_asset_bindings.v1.json");
        private static string CachePath => Path.Combine(Application.dataPath, "..", "Library", "MindforgeContentFoundry", "v10-cache.json");
        private static string ReportPath => Path.Combine(RepoRoot, "experiments", "reports", "content-foundry-unity-latest.json");

        [MenuItem("Mindforge/Content Foundry/Validate Recipes V1", priority = 10)]
        public static void ValidateRecipesMenu()
        {
            List<LoadedRecipe> recipes = LoadRecipes();
            Bindings bindings = LoadBindings(recipes);
            Debug.Log($"[Mindforge:Foundry] PASS recipes={recipes.Count}, explicit bindings={bindings.bindings.Length}.");
        }

        [MenuItem("Mindforge/Content Foundry/Compile Production Art Incremental", priority = 11)]
        public static void CompileProductionArtIncremental()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new UnityEditor.Build.BuildFailedException("Stop Play Mode before running Content Foundry.");

            List<LoadedRecipe> recipes = LoadRecipes();
            Bindings bindings = LoadBindings(recipes);
            string fingerprint = Fingerprint(recipes, bindings);
            Cache previous = ReadCache();
            GameObject production = EditorSceneLookup.FindIncludingInactive(ProductionArtV09Builder.RootName);

            if (previous != null && previous.fingerprint == fingerprint && production != null)
            {
                Debug.Log($"[Mindforge:Foundry] cache HIT {fingerprint.Substring(0, 12)}. Canonical full-rebuild qualification is unchanged.");
                return;
            }

            // V0.9 still exposes an exploratory filename heuristic. Suppress it only while
            // Foundry invokes the inherited production hook so explicit recipe bindings are the
            // sole local-art replacement authority in this path. Standalone V0.9 stays unchanged.
            bool legacySuppression = ExternalArtReplacementV09.SuppressAutomaticReplacement;
            ExternalArtReplacementV09.SuppressAutomaticReplacement = true;
            try
            {
                ProductionArtAutoHookV09.ApplyNow();
            }
            finally
            {
                ExternalArtReplacementV09.SuppressAutomaticReplacement = legacySuppression;
            }

            production = EditorSceneLookup.FindIncludingInactive(ProductionArtV09Builder.RootName);
            if (production == null)
                throw new UnityEditor.Build.BuildFailedException("Foundry requires the V0.8 reference-fidelity scene before production compilation.");

            RemoveExistingFoundryReplacements(production.transform);
            int replacements = ApplyBindings(recipes, bindings, production.transform);
            ValidateReplacementAuthority(production.transform);
            PresentationBudgetAudit.Run();

            Cache next = new Cache
            {
                fingerprint = fingerprint,
                recipe_count = recipes.Count,
                bound_asset_count = bindings.bindings.Length,
                generated_utc = DateTime.UtcNow.ToString("o"),
            };
            WriteCache(next);
            WriteReport(next, replacements);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            Debug.Log($"[Mindforge:Foundry] PASS recipes={recipes.Count}, bindings={bindings.bindings.Length}, replacements={replacements}, fp={fingerprint.Substring(0, 12)}. Iteration evidence only.");
        }

        [MenuItem("Mindforge/Content Foundry/Clear Incremental Cache", priority = 12)]
        public static void ClearIncrementalCache()
        {
            if (File.Exists(CachePath)) File.Delete(CachePath);
            Debug.Log("[Mindforge:Foundry] incremental cache cleared.");
        }

        private static List<LoadedRecipe> LoadRecipes()
        {
            if (!Directory.Exists(RecipesRoot)) throw new UnityEditor.Build.BuildFailedException("Missing recipe directory: " + RecipesRoot);
            string[] paths = Directory.GetFiles(RecipesRoot, "*.json", SearchOption.AllDirectories);
            Array.Sort(paths, StringComparer.Ordinal);
            if (paths.Length == 0) throw new UnityEditor.Build.BuildFailedException("Foundry requires at least one recipe.");

            List<LoadedRecipe> result = new List<LoadedRecipe>();
            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < paths.Length; i++)
            {
                string raw = File.ReadAllText(paths[i], Encoding.UTF8);
                Recipe recipe = JsonUtility.FromJson<Recipe>(raw);
                ValidateRecipe(paths[i], recipe);
                if (!ids.Add(recipe.asset_id)) throw new UnityEditor.Build.BuildFailedException("Duplicate asset_id: " + recipe.asset_id);
                result.Add(new LoadedRecipe { Path = paths[i], Raw = raw, Value = recipe });
            }
            return result;
        }

        private static void ValidateRecipe(string path, Recipe recipe)
        {
            if (recipe == null || recipe.schema != RecipeSchema) throw new UnityEditor.Build.BuildFailedException("Invalid recipe schema: " + path);
            if (string.IsNullOrWhiteSpace(recipe.asset_id) || !recipe.asset_id.StartsWith("mf_", StringComparison.Ordinal)) throw new UnityEditor.Build.BuildFailedException("Invalid asset_id: " + path);
            if (recipe.authority == null || recipe.authority.gameplay || recipe.authority.collision || recipe.authority.bci) throw new UnityEditor.Build.BuildFailedException("Foundry asset attempted to own authority: " + recipe.asset_id);
            if (recipe.geometry == null || recipe.geometry.target_size_m == null || recipe.geometry.target_size_m.Length != 3 || recipe.geometry.max_triangles < 12 || recipe.geometry.max_submeshes < 1) throw new UnityEditor.Build.BuildFailedException("Invalid geometry budget: " + recipe.asset_id);
            if (recipe.render == null || recipe.render.max_materials < 1 || recipe.render.max_materials > 8) throw new UnityEditor.Build.BuildFailedException("Invalid render budget: " + recipe.asset_id);
            if (recipe.unity == null || recipe.unity.target_tokens == null || recipe.unity.target_tokens.Length == 0 || string.IsNullOrWhiteSpace(recipe.unity.fallback_symbol)) throw new UnityEditor.Build.BuildFailedException("Missing Unity targeting/fallback: " + recipe.asset_id);
            if (recipe.quality == null || !recipe.quality.require_finite_normals || !recipe.quality.require_nonzero_bounds || !recipe.quality.reject_magenta_material) throw new UnityEditor.Build.BuildFailedException("Foundry quality firewall disabled: " + recipe.asset_id);
        }

        private static Bindings LoadBindings(List<LoadedRecipe> recipes)
        {
            Bindings value = File.Exists(BindingsPath)
                ? JsonUtility.FromJson<Bindings>(File.ReadAllText(BindingsPath, Encoding.UTF8))
                : new Bindings { schema = BindingSchema, bindings = Array.Empty<Binding>() };
            if (value == null || value.schema != BindingSchema) throw new UnityEditor.Build.BuildFailedException("Invalid local binding manifest.");
            if (value.bindings == null) value.bindings = Array.Empty<Binding>();

            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < recipes.Count; i++) ids.Add(recipes[i].Value.asset_id);
            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < value.bindings.Length; i++)
            {
                Binding binding = value.bindings[i];
                if (binding == null || !ids.Contains(binding.asset_id) || !seen.Add(binding.asset_id)) throw new UnityEditor.Build.BuildFailedException("Unknown/duplicate Foundry binding.");
                if (string.IsNullOrWhiteSpace(binding.unity_asset_path) || !binding.unity_asset_path.StartsWith("Assets/Mindforge/LocalArt/", StringComparison.Ordinal) || binding.unity_asset_path.Contains("..")) throw new UnityEditor.Build.BuildFailedException("Binding escaped LocalArt: " + binding.asset_id);
                ValidateExpectedHash(binding);
            }
            return value;
        }

        private static void ValidateExpectedHash(Binding binding)
        {
            if (string.IsNullOrWhiteSpace(binding.expected_sha256)) return;
            string relative = binding.unity_asset_path.Substring("Assets/".Length).Replace('/', Path.DirectorySeparatorChar);
            string absolute = Path.Combine(Application.dataPath, relative);
            if (!File.Exists(absolute)) throw new UnityEditor.Build.BuildFailedException("Bound LocalArt source is missing: " + binding.unity_asset_path);
            using (SHA256 sha = SHA256.Create())
            using (FileStream stream = File.OpenRead(absolute))
            {
                byte[] bytes = sha.ComputeHash(stream);
                StringBuilder actual = new StringBuilder(bytes.Length * 2);
                for (int i = 0; i < bytes.Length; i++) actual.Append(bytes[i].ToString("x2"));
                if (!string.Equals(actual.ToString(), binding.expected_sha256, StringComparison.OrdinalIgnoreCase)) throw new UnityEditor.Build.BuildFailedException("Bound LocalArt SHA-256 mismatch: " + binding.asset_id);
            }
        }

        private static int ApplyBindings(List<LoadedRecipe> recipes, Bindings bindings, Transform production)
        {
            Dictionary<string, Recipe> byId = new Dictionary<string, Recipe>(StringComparer.Ordinal);
            for (int i = 0; i < recipes.Count; i++) byId[recipes[i].Value.asset_id] = recipes[i].Value;
            int total = 0;
            for (int i = 0; i < bindings.bindings.Length; i++)
            {
                Binding binding = bindings.bindings[i];
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(binding.unity_asset_path);
                if (prefab == null) throw new UnityEditor.Build.BuildFailedException("LocalArt binding is not importable: " + binding.unity_asset_path);
                total += ReplaceTargets(byId[binding.asset_id], prefab, production);
            }
            return total;
        }

        private static int ReplaceTargets(Recipe recipe, GameObject prefab, Transform root)
        {
            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            List<Transform> targets = new List<Transform>();
            for (int i = 0; i < all.Length; i++)
            {
                Transform candidate = all[i];
                if (candidate == null || candidate.name.IndexOf(ReplacementMarker, StringComparison.Ordinal) >= 0) continue;
                if (!Matches(candidate.name, recipe.unity.target_tokens)) continue;
                if (candidate.GetComponentsInChildren<Renderer>(true).Length == 0) continue;
                targets.Add(candidate);
            }
            if (targets.Count > MaxReplacementsPerRecipe) throw new UnityEditor.Build.BuildFailedException($"{recipe.asset_id} matched {targets.Count} targets; refine target_tokens.");

            int count = 0;
            for (int i = 0; i < targets.Count; i++)
            {
                Transform target = targets[i];
                Bounds? targetBounds = BoundsOf(target);
                if (!targetBounds.HasValue) continue;
                GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                if (instance == null) instance = UnityEngine.Object.Instantiate(prefab);
                if (instance == null) continue;
                instance.name = target.name + ReplacementMarker + recipe.asset_id;
                instance.transform.SetParent(target.parent, false);
                instance.transform.localPosition = target.localPosition;
                instance.transform.localRotation = target.localRotation;
                StripAuthority(instance);
                Fit(instance, targetBounds.Value.size);
                ValidateAssetBudget(instance, recipe);
                Renderer[] old = target.GetComponentsInChildren<Renderer>(true);
                for (int r = 0; r < old.Length; r++) if (old[r] != null) old[r].enabled = false;
                count++;
            }
            return count;
        }

        private static bool Matches(string name, string[] tokens)
        {
            for (int i = 0; i < tokens.Length; i++) if (!string.IsNullOrWhiteSpace(tokens[i]) && name.IndexOf(tokens[i], StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }

        private static void StripAuthority(GameObject root)
        {
            Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++) if (colliders[i] != null) colliders[i].enabled = false;
            Rigidbody[] bodies = root.GetComponentsInChildren<Rigidbody>(true);
            for (int i = 0; i < bodies.Length; i++) if (bodies[i] != null) UnityEngine.Object.DestroyImmediate(bodies[i]);
            MonoBehaviour[] behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++) if (behaviours[i] != null) behaviours[i].enabled = false;
            Light[] lights = root.GetComponentsInChildren<Light>(true);
            for (int i = 0; i < lights.Length; i++) if (lights[i] != null) lights[i].enabled = false;
            Camera[] cameras = root.GetComponentsInChildren<Camera>(true);
            for (int i = 0; i < cameras.Length; i++) if (cameras[i] != null) cameras[i].enabled = false;
            AudioListener[] listeners = root.GetComponentsInChildren<AudioListener>(true);
            for (int i = 0; i < listeners.Length; i++) if (listeners[i] != null) listeners[i].enabled = false;
        }

        private static void ValidateAssetBudget(GameObject root, Recipe recipe)
        {
            int triangles = 0;
            int submeshes = 0;
            HashSet<Material> materials = new HashSet<Material>();
            MeshFilter[] filters = root.GetComponentsInChildren<MeshFilter>(true);
            for (int i = 0; i < filters.Length; i++)
            {
                Mesh mesh = filters[i] != null ? filters[i].sharedMesh : null;
                if (mesh == null) continue;
                triangles += mesh.triangles.Length / 3;
                submeshes += mesh.subMeshCount;
            }
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                Material[] shared = renderers[i].sharedMaterials;
                for (int m = 0; m < shared.Length; m++)
                {
                    Material material = shared[m];
                    if (material == null || material.shader == null || ShaderUtil.ShaderHasError(material.shader)) throw new UnityEditor.Build.BuildFailedException("Foundry replacement has missing/failing material: " + recipe.asset_id);
                    materials.Add(material);
                }
            }
            if (triangles > recipe.geometry.max_triangles) throw new UnityEditor.Build.BuildFailedException($"Triangle budget exceeded for {recipe.asset_id}: {triangles}/{recipe.geometry.max_triangles}");
            if (submeshes > recipe.geometry.max_submeshes) throw new UnityEditor.Build.BuildFailedException($"Submesh budget exceeded for {recipe.asset_id}: {submeshes}/{recipe.geometry.max_submeshes}");
            if (materials.Count > recipe.render.max_materials) throw new UnityEditor.Build.BuildFailedException($"Material budget exceeded for {recipe.asset_id}: {materials.Count}/{recipe.render.max_materials}");
            Bounds? bounds = BoundsOf(root.transform);
            if (!bounds.HasValue || bounds.Value.size.sqrMagnitude < 0.0001f) throw new UnityEditor.Build.BuildFailedException("Foundry replacement has degenerate bounds: " + recipe.asset_id);
        }

        private static void RemoveExistingFoundryReplacements(Transform root)
        {
            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            List<GameObject> remove = new List<GameObject>();
            for (int i = 0; i < all.Length; i++) if (all[i] != null && all[i].name.IndexOf(ReplacementMarker, StringComparison.Ordinal) >= 0) remove.Add(all[i].gameObject);
            for (int i = 0; i < remove.Count; i++) if (remove[i] != null) UnityEngine.Object.DestroyImmediate(remove[i]);
        }

        private static void ValidateReplacementAuthority(Transform root)
        {
            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                Transform item = all[i];
                if (item == null || item.name.IndexOf(ReplacementMarker, StringComparison.Ordinal) < 0) continue;
                Collider[] colliders = item.GetComponentsInChildren<Collider>(true);
                for (int c = 0; c < colliders.Length; c++) if (colliders[c] != null && colliders[c].enabled) throw new UnityEditor.Build.BuildFailedException("Foundry replacement retained enabled collision: " + item.name);
                if (item.GetComponentsInChildren<Rigidbody>(true).Length != 0) throw new UnityEditor.Build.BuildFailedException("Foundry replacement retained Rigidbody: " + item.name);
                Light[] lights = item.GetComponentsInChildren<Light>(true);
                for (int l = 0; l < lights.Length; l++) if (lights[l] != null && lights[l].enabled) throw new UnityEditor.Build.BuildFailedException("Foundry replacement retained active light: " + item.name);
            }
        }

        private static Bounds? BoundsOf(Transform root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            bool found = false;
            Bounds bounds = default;
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || !renderer.enabled) continue;
                if (!found) { bounds = renderer.bounds; found = true; } else bounds.Encapsulate(renderer.bounds);
            }
            return found ? bounds : (Bounds?)null;
        }

        private static void Fit(GameObject instance, Vector3 targetSize)
        {
            Bounds? value = BoundsOf(instance.transform);
            if (!value.HasValue) return;
            Vector3 current = value.Value.size;
            float sx = current.x > 0.001f ? targetSize.x / current.x : 1f;
            float sy = current.y > 0.001f ? targetSize.y / current.y : 1f;
            float sz = current.z > 0.001f ? targetSize.z / current.z : 1f;
            instance.transform.localScale *= Mathf.Clamp(Mathf.Min(sx, Mathf.Min(sy, sz)), 0.02f, 100f);
        }

        private static string Fingerprint(List<LoadedRecipe> recipes, Bindings bindings)
        {
            StringBuilder text = new StringBuilder("mindforge.content_foundry.v10\n");
            for (int i = 0; i < recipes.Count; i++) text.Append(recipes[i].Path).Append('\n').Append(recipes[i].Raw).Append('\n');
            text.Append(JsonUtility.ToJson(bindings));
            using (SHA256 sha = SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(text.ToString()));
                StringBuilder hex = new StringBuilder(bytes.Length * 2);
                for (int i = 0; i < bytes.Length; i++) hex.Append(bytes[i].ToString("x2"));
                return hex.ToString();
            }
        }

        private static Cache ReadCache()
        {
            if (!File.Exists(CachePath)) return null;
            try { return JsonUtility.FromJson<Cache>(File.ReadAllText(CachePath, Encoding.UTF8)); } catch { return null; }
        }

        private static void WriteCache(Cache state)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(CachePath));
            File.WriteAllText(CachePath, JsonUtility.ToJson(state, true) + "\n", Encoding.UTF8);
        }

        private static void WriteReport(Cache state, int replacements)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
            string json = "{\n" +
                          "  \"schema\": \"mindforge.content_foundry_unity_report.v1\",\n" +
                          $"  \"generated_utc\": \"{state.generated_utc}\",\n" +
                          $"  \"fingerprint\": \"{state.fingerprint}\",\n" +
                          $"  \"recipe_count\": {state.recipe_count},\n" +
                          $"  \"bound_asset_count\": {state.bound_asset_count},\n" +
                          $"  \"replacement_count\": {replacements},\n" +
                          "  \"canonical_promotion_evidence\": false,\n" +
                          "  \"authority\": {\"gameplay\": false, \"collision\": false, \"bci\": false}\n" +
                          "}\n";
            File.WriteAllText(ReportPath, json, Encoding.UTF8);
        }
    }
}
#endif
