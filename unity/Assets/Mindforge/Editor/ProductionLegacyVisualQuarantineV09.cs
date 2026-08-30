#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace Mindforge.Editor
{
    /// <summary>
    /// Removes superseded prototype scenery from the rendered frame without weakening the
    /// collision-backed world. Several earlier passes were intentionally additive and
    /// collider-free; stacking all of them at once is what produced the dense beam/sphere
    /// thicket visible in the August 30 capture. V0.9 keeps their source and gameplay seams,
    /// but hides presentation that has been replaced by the production composition.
    /// </summary>
    public static class ProductionLegacyVisualQuarantineV09
    {
        private static readonly string[] SemanticKeepTokens =
        {
            "Signal", "Intent", "Target", "Vep", "VEP", "Gate", "Shortcut",
            "Landing", "Stair", "Ramp", "Bridge", "Conduit", "Threshold",
        };

        public static int ApplyOpenScene()
        {
            int hidden = 0;
            GameObject ward = EditorSceneLookup.FindIncludingInactive(NullWardSceneBuilder.RootName);
            GameObject arena = EditorSceneLookup.FindIncludingInactive("Fractured_Signal_Arena");

            if (ward != null)
            {
                hidden += QuarantineChild(ward.transform, NullWardVisualInfrastructureBuilder.DetailRootName);
                hidden += QuarantineChild(ward.transform, NullWardArenaSetDressingV3Builder.WardRootName);
            }
            if (arena != null)
                hidden += QuarantineChild(arena.transform, NullWardArenaSetDressingV3Builder.ArenaBackdropRootName);

            GameObject composition = EditorSceneLookup.FindIncludingInactive(GroundedWorldCompositionV2Builder.RootName);
            if (composition != null)
                hidden += HideColliderFreeCompositionDecor(composition.transform);

            if (hidden > 0)
                Debug.Log($"[Mindforge:V09:Art] Quarantined {hidden} superseded legacy renderers/roots while preserving collision and semantic telegraphs.");
            return hidden;
        }

        private static int QuarantineChild(Transform parent, string childName)
        {
            if (parent == null || string.IsNullOrWhiteSpace(childName)) return 0;
            Transform root = parent.Find(childName);
            if (root == null) return 0;

            Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
            bool ownsEnabledCollision = false;
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null && colliders[i].enabled)
                {
                    ownsEnabledCollision = true;
                    break;
                }
            }

            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (!ownsEnabledCollision)
            {
                int count = EnabledRendererCount(renderers);
                root.gameObject.SetActive(false);
                EditorUtility.SetDirty(root.gameObject);
                return count;
            }

            // Defensive fallback. The named roots are currently documented collider-free,
            // but if an older builder evolves later, never hide the entire collision owner.
            int hidden = 0;
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || !renderer.enabled) continue;
                if (HasEnabledCollider(renderer.gameObject)) continue;
                renderer.enabled = false;
                EditorUtility.SetDirty(renderer);
                hidden++;
            }
            return hidden;
        }

        private static int HideColliderFreeCompositionDecor(Transform root)
        {
            int hidden = 0;
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || !renderer.enabled) continue;
                if (HasEnabledCollider(renderer.gameObject)) continue;
                if (ShouldKeepSemanticRenderer(renderer.gameObject.name)) continue;

                renderer.enabled = false;
                EditorUtility.SetDirty(renderer);
                hidden++;
            }
            return hidden;
        }

        private static bool ShouldKeepSemanticRenderer(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            for (int i = 0; i < SemanticKeepTokens.Length; i++)
                if (name.IndexOf(SemanticKeepTokens[i], StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            return false;
        }

        private static bool HasEnabledCollider(GameObject go)
        {
            if (go == null) return false;
            Collider collider = go.GetComponent<Collider>();
            return collider != null && collider.enabled;
        }

        private static int EnabledRendererCount(Renderer[] renderers)
        {
            int count = 0;
            if (renderers == null) return count;
            for (int i = 0; i < renderers.Length; i++)
                if (renderers[i] != null && renderers[i].enabled) count++;
            return count;
        }
    }
}
#endif
