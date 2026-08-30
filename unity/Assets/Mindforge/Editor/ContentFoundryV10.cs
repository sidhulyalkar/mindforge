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
    /// V0.10 presentation content compiler.
    ///
    /// This is deliberately downstream of Mindforge gameplay, collision, persistence and BCI
    /// authority. The canonical ShowcaseEditorMenu full rebuild remains the promotion gate.
    /// This compiler exists to make repeated production-art iteration cheap and deterministic.
    /// </summary>
    public static class ContentFoundryV10
    {
        public const string RecipeSchema = "mindforge.content_asset_recipe.v1";
        public const string BindingSchema = "mindforge.local_asset_bindings.v1";
        public const string ReplacementMarker = "__FoundryV10__";
        private const int MaxReplacementsPerRecipe = 64;

        [Serializable]
        private sealed class RecipeEnvelope
        {
            public string schema;
            public string asset_id;
            public string semantic_role;
            public string[] districts;
            public SourceSpec source;
            public GeometrySpec geometry;
            public RenderSpec render;
            public UnitySpec unity;
            public AuthoritySpec authority;
            public QualitySpec quality;
        }

        [Serializable] private sealed class SourceSpec
        {
            public string kind;
            public string tool;
            public string tool_version;
            public string license;
            public string redistribution_policy;
        }

        [Serializable] private sealed class GeometrySpec
        {
            public float[] target_size_m;
            public string forward_axis;
            public string up_axis;
            public string pivot_policy;
            public int max_triangles;
            public int max_submeshes;
        }

        [Serializable] private sealed class RenderSpec
        {
            public string material_family;
            public int max_materials;
            public int texture_max_px;
            public float[] lod_ratios;
            public bool cast_shadows;
            public bool receive_shadows;
        }

        [Serializable] private sealed class UnitySpec
        {
            public string[] target_tokens;
            public string fallback_symbol;
        }

        [Serializable] private sealed class AuthoritySpec
        {
            public bool gameplay;
            public bool collision;
            public bool bci;
        }

        [Serializable] private sealed class QualitySpec
        {
            public float minimum_score;
            public bool require_finite_normals;
            public bool require_nonzero_bounds;
            public bool reject_magenta_material;
        }

        [Serializable] private sealed class BindingEnvelope
        {
            public string schema;
            public Binding[] bindings;
        }

        [Serializable] private sealed class Binding
        {
            public string asset_id;
            public string unity_asset_path;
            public string expected_sha256;
        }

        [Serializable] private sealed class CacheState
        {
            public string schema = "mindforge.content_foundry_unity_cache.v1";
            public string fingerprint;
            public int recipe_count;
            public int bound_asset_count;
            public string generated_utc;
        }

        private sealed class LoadedRecipe
        {
            public string Path;
            public string Raw;
            public RecipeEnvelope Recipe;
        }

        private static string RepoRoot => Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
        private static string RecipesRoot => Path.Combine(RepoRoot, "content", "recipes");
        private static string BindingsPath => Path.Combine(RepoRoot, "content", "local_asset_bindings.v1.json");
        private static string CachePath => Path.Combine(Application.dataPath, "..", "Library", "MindforgeContentFoundry", "v10-cache.json");
        private static string ReportPath => Path.Combine(RepoRoot, "experiments", "reports", "content-foundry-unity-latest.json");

        [MenuItem("Mindforge/Content Foundry/Validate Recipes V1", priority = 10)]
        public static void ValidateRecipesMenu()
        {
            List<LoadedRecipe> recipes = LoadAndValidateRecipes();
            BindingEnvelope bindings = LoadAndValidateBindings(recipes);
            Debug.Log($"[Mindforge:Foundry] Recipe validation PASS: recipes={recipes.Count}, explicit local bindings={bindings.bindings.Length}.");
        }

        [MenuItem("Mindforge/Content Foundry/Compile Production Art Incremental", priority = 11)]
        public static void CompileProductionArtIncremental()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new UnityEditor.Build.BuildFailedException("Stop Play Mode before running the Content Foundry compiler.");

            List<LoadedRecipe> recipes = LoadAndValidateRecipes();
            BindingEnvelope bindings = LoadAndValidateBindings(recipes);
            string fingerprint = ComputeFingerprint(recipes, bindings);
            CacheState previous = ReadCache();

            GameObject production = EditorSceneLookup.FindIncludingInactive(ProductionArtV09Builder.RootName);
            bool sceneHasCompiledBase = production != null;
            if (previous != null && previous.fingerprint == fingerprint && sceneHasCompiledBase)
            {
                Debug.Log($"[Mindforge:Foundry] Incremental production-art stage cache HIT {fingerprint.Substring(0, 12)}. Canonical full-rebuild qualification remains unchanged.");
                return;
            }

            ProductionArtAutoHookV09.ApplyNow();
            production = EditorSceneLookup.FindIncludingInactive(ProductionArtV09Builder.RootName);
            if (production == null)
                throw new UnityEditor.Build.BuildFailedException("Content Foundry requires a V0.8 reference-fidelity scene before production-art compilation.");

            int replacements = ApplyExplicitBindings(recipes, bindings, production);
            ValidateCompiledPresentation(production);
            PresentationBudgetAudit.Run();

            CacheState next = new CacheState
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

            Debug.Log(
                $"[Mindforge:Foundry] Incremental production-art compile PASS: recipes={recipes.Count}, bound assets={bindings.bindings.Length}, " +
                $"replacements={replacements}, fingerprint={fingerprint.Substring(0, 12)}. This is iteration evidence, not canonical Unity promotion evidence.");
        }

        [MenuItem("Mindforge/Content Foundry/Clear Incremental Cache", priority = 12)]
        public static void ClearIncrementalCache()
        {
            if (File.Exists(CachePath)) File.Delete(CachePath);
            Debug.Log("[Mindforge:Foundry] Incremental cache cleared. The next Foundry compile will rebuild production presentation.");
        }

        private static List<LoadedRecipe> LoadAndValidateRecipes()
        {
            if (!Directory.Exists(RecipesRoot))
                throw new UnityEditor.Build.BuildFailedException("Content Foundry recipe directory is missing: " + RecipesRoot);

            string[] paths = Directory.GetFiles(RecipesRoot, "*.json", SearchOption.AllDirectories);
            Array.Sort(paths, StringComparer.Ordinal);
            if (paths.Length == 0)
                throw new UnityEditor.Build.BuildFailedException("Content Foundry requires at least one recipe.");

            List<LoadedRecipe> result = new List<LoadedRecipe>();
            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < paths.Length; i++)
            {
                string raw = File.ReadAllText(paths[i], Encoding.UTF8);
                RecipeEnvelope recipe = JsonUtility.FromJson<RecipeEnvelope>(raw);
                ValidateRecipe(paths[i], recipe);
                if (!ids.Add(recipe.asset_id))
                    throw new UnityEditor.Build.BuildFailedException("Duplicate Content Foundry asset_id: " + recipe.asset_id);
                result.Add(new LoadedRecipe { Path = paths[i], Raw = raw, Recipe = recipe });
            }
            return result;
        }

        private static void ValidateRecipe(string path, RecipeEnvelope recipe)
        {
            if (recipe == null || recipe.schema != RecipeSchema)
                throw new UnityEditor.Build.BuildFailedException("Invalid Content Foundry recipe schema: " + path);
            if (string.IsNullOrWhiteSpace(recipe.asset_id) || !recipe.asset_id.StartsWith("mf_", StringComparison.Ordinal))
                throw new UnityEditor.Build.BuildFailedException("Invalid Content Foundry asset_id: " + path);
            if (recipe.authority == null || recipe.authority.gameplay || recipe.authority.collision || recipe.authority.bci)
                throw new UnityEditor.Build.BuildFailedException("Content Foundry assets may not own gameplay, collision or BCI authority: " + recipe.asset_id);
            if (recipe.geometry == null || recipe.geometry.target_size_m == null || recipe.geometry.target_size_m.Length != 3 || recipe.geometry.max_triangles < 12)
                throw new UnityEditor.Build.BuildFailedException("Invalid geometry budget: " + recipe.asset_id);
            if (recipe.render == null || recipe.render.max_materials < 1 || recipe.render.max_materials > 8)
                throw new UnityEditor.Build.BuildFailedException("Invalid render budget: " + recipe.asset_id);
            if (recipe.unity == null || recipe.unity.target_tokens == null || recipe.unity.target_tokens.Length == 0 || string.IsNullOrWhiteSpace(recipe.unity.fallback_symbol))
                throw new UnityEditor.Build.BuildFailedException("Recipe lacks deterministic Unity targeting/fallback metadata: " + recipe.asset_id);
            if (recipe.quality == null || !recipe.quality.require_finite_normals || !recipe.quality.require_nonzero_bounds || !recipe.quality.reject_magenta_material)
                throw new UnityEditor.Build.BuildFailedException("Content Foundry quality firewalls may not be disabled: " + recipe.asset_id);
        }

        private static BindingEnvelope LoadAndValidateBindings(List<LoadedRecipe> recipes)
        {
            BindingEnvelope value;
            if (!File.Exists(BindingsPath))
                value = new BindingEnvelope { schema = BindingSchema, bindings = Array.Empty<Binding>() };
            else
                value = JsonUtility.FromJson<BindingEnvelope>(File.ReadAllText(BindingsPath, Encoding.UTF8));

            if (value == null || value.schema != BindingSchema)
                throw new UnityEditor.Build.BuildFailedException("Invalid Content Foundry local binding manifest.");
            if (value.bindings == null) value.bindings = Array.Empty<Binding>();

            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < recipes.Count; i++) ids.Add(recipes[i].Recipe.asset_id);
            HashSet<string> bound = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < value.bindings.Length; i++)
            {
                Binding binding = value.bindings[i];
                if (binding == null || !ids.Contains(binding.asset_id) || !bound.Add(binding.asset_id))
                    throw new UnityEditor.Build.BuildFailedException("Content Foundry binding is missing, duplicated or references an unknown recipe.");
                if (string.IsNullOrWhiteSpace(binding.unity_asset_path) ||
                    !binding.unity_asset_path.StartsWith("Assets/Mindforge/LocalArt/", StringComparison.Ordinal) ||
                    binding.unity_asset_path.Contains(".."))
                    throw new UnityEditor.Build.BuildFailedException("Local art binding escaped Assets/Mindforge/LocalArt/: " + binding.asset_id);
            }
            return value;
        }

        private static int ApplyExplicitBindings(List<LoadedRecipe> recipes, BindingEnvelope bindings, GameObject production)
        {
            if (bindings.bindings.Length == 0) return 0;
            Dictionary<string, RecipeEnvelope> byId = new Dictionary<string, RecipeEnvelope>(StringComparer.Ordinal);
            for (int i = 0; i < recipes.Count; i++) byId[recipes[i].Recipe.asset_id] = recipes[i].Recipe;

            int total = 0;
            for (int i = 0; i < bindings.bindings.Length; i++)
            {
                Binding binding = bindings.bindings[i];
                RecipeEnvelope recipe = byId[binding.asset_id];
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(binding.unity_asset_path);
                if (prefab == null)
                    throw new UnityEditor.Build.BuildFailedException("Bound local production asset is not importable as a GameObject: " + binding.unity_asset_path);
                total += ReplaceTargets(recipe, prefab, production.transform);
            }
            return total;
        }

        private static int ReplaceTargets(RecipeEnvelope recipe, GameObject prefab, Transform root)
        {
            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            List<Transform> targets = new List<Transform>();
            for (int i = 0; i < all.Length; i++)
            {
                Transform candidate = all[i];
                if (candidate == null || candidate.name.Contains(ReplacementMarker)) continue;
                if (!MatchesAnyToken(candidate.name, recipe.unity.target_tokens)) continue;
                if (candidate.GetComponentsInChildren<Renderer>(true).Length == 0) continue;
                targets.Add(candidate);
            }
            if (targets.Count > MaxReplacementsPerRecipe)
                throw new UnityEditor.Build.BuildFailedException($"Recipe {recipe.asset_id} matched {targets.Count} targets; cap is {MaxReplacementsPerRecipe}. Refine target_tokens.");

            int replaced = 0;
            for (int i = 0; i < targets.Count; i++)
            {
                Transform target = targets[i];
                string replacementName = target.name + ReplacementMarker + recipe.asset_id;
                if (target.parent != null && target.parent.Find(replacementName) != null) continue;
                Bounds? bounds = CalculateEnabledBounds(target);
                if (!bounds.HasValue || bounds.Value.size.sqrMagnitude < 0.0001f) continue;

                GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                if (instance == null) instance = UnityEngine.Object.Instantiate(prefab);
                if (instance == null) continue;
                instance.name = replacementName;
                instance.transform.SetParent(target.parent, false);
                instance.transform.localPosition = target.localPosition;
                instance.transform.localRotation = target.localRotation;
                StripAuthority(instance);
                FitToBounds(instance, bounds.Value.size);

                Renderer[] old = target.GetComponentsInChildren<Renderer>(true);
                for (int r = 0; r < old.Length; r++)
                    if (old[r] != null) old[r].enabled = false;
                replaced++;
            }
            return replaced;
        }

        private static bool MatchesAnyToken(string name, string[] tokens)
        {
            for (int i = 0; i < tokens.Length; i++)
                if (!string.IsNullOrWhiteSpace(tokens[i]) && name.IndexOf(tokens[i], StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
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

        private static Bounds? CalculateEnabledBounds(Transform root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            bool found = false;
            Bounds bounds = default;
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || !renderer.enabled) continue;
                if (!found) { bounds = renderer.bounds; found = true; }
                else bounds.Encapsulate(renderer.bounds);
            }
            return found ? bounds : (Bounds?)null;
        }

        private static void FitToBounds(GameObject instance, Vector3 targetSize)
        {
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return;
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) if (renderers[i] != null) bounds.Encapsulate(renderers[i].bounds);
            Vector3 current = bounds.size;
            float sx = current.x > 0.001f ? targetSize.x / current.x : 1f;
            float sy = current.y > 0.001f ? targetSize.y / current.y : 1f;
            float sz = current.z > 0.001f ? targetSize.z / current.z : 1f;
            instance.transform.localScale *= Mathf.Clamp(Mathf.Min(sx, Mathf.Min(sy, sz)), 0.02f, 100f);
        }

        private static void ValidateCompiledPresentation(GameObject production)
        {
            Transform[] all = production.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                Transform item = all[i];
                if (item == null || !item.name.Contains(ReplacementMarker)) continue;
                Collider[] colliders = item.GetComponentsInChildren<Collider>(true);
                for (int c = 0; c < colliders.Length; c++)
                    if (colliders[c] != null && colliders[c].enabled)
                        throw new UnityEditor.Build.BuildFailedException("Content Foundry replacement retained enabled collision: " + item.name);
                if (item.GetComponentsInChildren<Rigidbody>(true).Length != 0)
                    throw new UnityEditor.Build.BuildFailedException("Content Foundry replacement retained Rigidbody authority: " + item.name);
                Light[] lights = item.GetComponentsInChildren<Light>(true);
                for (int l = 0; l < lights.Length; l++)
                    if (lights[l] != null && lights[l].enabled)
                        throw new UnityEditor.Build.BuildFailedException("Content Foundry replacement retained active light authority: " + item.name);
            }
        }

        private static string ComputeFingerprint(List<LoadedRecipe> recipes, BindingEnvelope bindings)
        {
            StringBuilder builder = new StringBuilder("mindforge.content_foundry.v10\n");
            for (int i = 0; i < recipes.Count; i++)
            {
                builder.Append(recipes[i].Path).Append('\n');
                builder.Append(recipes[i].Raw).Append('\n');
            }
            builder.Append(JsonUtility.ToJson(bindings));
            using (SHA256 sha = SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(builder.ToString()));
                StringBuilder hex = new StringBuilder(bytes.Length * 2);
                for (int i = 0; i < bytes.Length; i++) hex.Append(bytes[i].ToString("x2"));
                return hex.ToString();
            }
        }

        private static CacheState ReadCache()
        {
            if (!File.Exists(CachePath)) return null;
            try { return JsonUtility.FromJson<CacheState>(File.ReadAllText(CachePath, Encoding.UTF8)); }
            catch { return null; }
        }

        private static void WriteCache(CacheState state)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(CachePath));
            File.WriteAllText(CachePath, JsonUtility.ToJson(state, true) + "\n", Encoding.UTF8);
        }

        private static void WriteReport(CacheState state, int replacements)
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
