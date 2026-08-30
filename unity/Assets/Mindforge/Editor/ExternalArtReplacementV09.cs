#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Mindforge.Editor
{
    /// <summary>
    /// Optional local art replacement pass. The committed V0.9 scene remains reproducible
    /// without binary art, but if the developer has lawfully obtained production packs under
    /// Assets/Mindforge/LocalArt this pass can automatically replace obvious generated
    /// columns/arches/spires/trees with the best discovered source model at matching scale.
    /// The fallback renderer remains in the hierarchy but is hidden, so removing LocalArt and
    /// rebuilding returns to deterministic source-only presentation.
    /// </summary>
    public static class ExternalArtReplacementV09
    {
        private const string ReplacementMarker = "__ExternalV09";

        // V0.10 uses explicit recipe -> asset bindings. It may temporarily suppress this V0.9
        // filename heuristic while invoking the inherited production hook, so two replacement
        // authorities can never race in the same Foundry compile. Standalone V0.9 behavior is
        // unchanged because this flag defaults false.
        public static bool SuppressAutomaticReplacement { get; set; }

        [MenuItem("Mindforge/Art/Apply Local Production Art Replacements", priority = 21)]
        public static int ApplyOpenScene()
        {
            if (SuppressAutomaticReplacement) return 0;

            GameObject root = EditorSceneLookup.FindIncludingInactive(ProductionArtV09Builder.RootName);
            if (root == null) return 0;

            bool haveColumns = ExternalArtDropV09.FindCandidates(ExternalArtDropV09.Role.Column).Count > 0;
            bool haveArches = ExternalArtDropV09.FindCandidates(ExternalArtDropV09.Role.Arch).Count > 0;
            bool haveSpires = ExternalArtDropV09.FindCandidates(ExternalArtDropV09.Role.Spire).Count > 0;
            bool haveTrees = ExternalArtDropV09.FindCandidates(ExternalArtDropV09.Role.Tree).Count > 0;
            if (!haveColumns && !haveArches && !haveSpires && !haveTrees) return 0;

            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            List<Transform> targets = new List<Transform>();
            for (int i = 0; i < transforms.Length; i++)
                if (transforms[i] != null && Classify(transforms[i].name, haveColumns, haveArches, haveSpires, haveTrees).HasValue)
                    targets.Add(transforms[i]);

            int replaced = 0;
            for (int i = 0; i < targets.Count; i++)
            {
                Transform target = targets[i];
                if (target == null || target.name.EndsWith(ReplacementMarker, StringComparison.Ordinal)) continue;
                if (target.parent == null || target.parent.Find(target.name + ReplacementMarker) != null) continue;
                ExternalArtDropV09.Role? role = Classify(target.name, haveColumns, haveArches, haveSpires, haveTrees);
                if (!role.HasValue) continue;

                Bounds? bounds = CalculateBounds(target);
                if (!bounds.HasValue || bounds.Value.size.sqrMagnitude < 0.0001f) continue;
                Vector3 targetSize = bounds.Value.size;

                GameObject replacement = ExternalArtDropV09.TryInstantiateBest(
                    role.Value,
                    target.parent,
                    target.name + ReplacementMarker,
                    target.localPosition,
                    targetSize,
                    target.localEulerAngles);
                if (replacement == null) continue;

                Renderer[] old = target.GetComponentsInChildren<Renderer>(true);
                for (int r = 0; r < old.Length; r++)
                    if (old[r] != null) old[r].enabled = false;
                replaced++;
            }

            if (replaced > 0)
            {
                EditorUtility.SetDirty(root);
                Debug.Log($"[Mindforge:V09:ExternalArt] Replaced {replaced} generated production motifs with local licensed source art. Gameplay collision remains on Mindforge authority objects.");
            }
            return replaced;
        }

        private static ExternalArtDropV09.Role? Classify(string name, bool columns, bool arches, bool spires, bool trees)
        {
            if (string.IsNullOrEmpty(name) || name.EndsWith(ReplacementMarker, StringComparison.Ordinal)) return null;
            if (trees && name.IndexOf("Tree", StringComparison.OrdinalIgnoreCase) >= 0 &&
                (name.IndexOf("Trunk", StringComparison.OrdinalIgnoreCase) < 0 && name.IndexOf("Canopy", StringComparison.OrdinalIgnoreCase) < 0))
                return ExternalArtDropV09.Role.Tree;
            if (arches && name.IndexOf("Arch", StringComparison.OrdinalIgnoreCase) >= 0)
                return ExternalArtDropV09.Role.Arch;
            if (spires && name.IndexOf("Spire", StringComparison.OrdinalIgnoreCase) >= 0)
                return ExternalArtDropV09.Role.Spire;
            if (columns && name.IndexOf("Column", StringComparison.OrdinalIgnoreCase) >= 0)
                return ExternalArtDropV09.Role.Column;
            return null;
        }

        private static Bounds? CalculateBounds(Transform root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            bool found = false;
            Bounds bounds = default;
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || !renderer.enabled) continue;
                if (!found)
                {
                    bounds = renderer.bounds;
                    found = true;
                }
                else bounds.Encapsulate(renderer.bounds);
            }
            return found ? bounds : (Bounds?)null;
        }
    }
}
#endif
