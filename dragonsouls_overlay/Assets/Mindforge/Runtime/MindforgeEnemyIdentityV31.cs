using UnityEngine;
using UnityEngine.Rendering;

namespace Mindforge.Chassis
{
    /// <summary>
    /// Replaces the staging-game saturated enemy look with a restrained role-readable
    /// Mindforge palette while preserving the inherited meshes, rigs and materials.
    /// This component owns renderer property blocks only.
    /// </summary>
    [DefaultExecutionOrder(790)]
    [DisallowMultipleComponent]
    public sealed class MindforgeEnemyIdentityV31 : MonoBehaviour
    {
        public enum Archetype
        {
            NeuralHusk,
            Sentinel,
            SignalCaster,
            SignalRanger,
            CathedralBrute,
            CorruptedBeast,
            BoneRemnant,
        }

        [SerializeField, Range(0f, 1f)] private float desaturation = 0.76f;
        [SerializeField, Range(0f, 1f)] private float identityBlend = 0.62f;
        [SerializeField, Range(0f, 1f)] private float emissionStrength = 0.18f;

        public Archetype Role { get; private set; }
        public bool Installed { get; private set; }
        public int MaterialSlotsRethemed { get; private set; }

        private void Start()
        {
            Role = ResolveRole(gameObject.name);
            Color identity = RoleColor(Role);
            Color signal = RoleSignalColor(Role);
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            int changed = 0;

            for (int r = 0; r < renderers.Length; r++)
            {
                Renderer renderer = renderers[r];
                if (renderer == null || renderer is ParticleSystemRenderer || renderer is TrailRenderer) continue;
                if (LooksLikeUiOrIndicator(renderer.transform)) continue;

                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
                Material[] materials = renderer.sharedMaterials;
                if (materials == null) continue;

                for (int m = 0; m < materials.Length; m++)
                {
                    Material material = materials[m];
                    if (material == null) continue;
                    string colorProperty = material.HasProperty("_BaseColor") ? "_BaseColor" :
                        material.HasProperty("_Color") ? "_Color" : null;
                    if (colorProperty == null) continue;

                    Color original = material.GetColor(colorProperty);
                    float luminance = Mathf.Clamp01(original.r * 0.2126f + original.g * 0.7152f + original.b * 0.0722f);
                    Color grey = new Color(luminance, luminance, luminance, original.a);
                    Color desaturated = Color.Lerp(original, grey, desaturation);
                    Color adjusted = Color.Lerp(desaturated, identity, identityBlend);
                    adjusted.a = original.a;

                    MaterialPropertyBlock block = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(block, m);
                    block.SetColor(colorProperty, adjusted);
                    if (material.HasProperty("_EmissionColor"))
                        block.SetColor("_EmissionColor", signal * emissionStrength);
                    renderer.SetPropertyBlock(block, m);
                    changed++;
                }
            }

            MaterialSlotsRethemed = changed;
            Installed = true;
        }

        private static Archetype ResolveRole(string objectName)
        {
            string n = objectName.ToLowerInvariant();
            if (ContainsAny(n, "mage", "wizard", "caster", "sorcer")) return Archetype.SignalCaster;
            if (ContainsAny(n, "archer", "range", "bow")) return Archetype.SignalRanger;
            if (ContainsAny(n, "minotaur", "heavy", "brute", "great")) return Archetype.CathedralBrute;
            if (ContainsAny(n, "bear", "wolf", "hound", "beast", "slime")) return Archetype.CorruptedBeast;
            if (ContainsAny(n, "skeleton", "bone")) return Archetype.BoneRemnant;
            if (ContainsAny(n, "knight", "shield", "sentinel")) return Archetype.Sentinel;
            return Archetype.NeuralHusk;
        }

        private static Color RoleColor(Archetype role)
        {
            switch (role)
            {
                case Archetype.Sentinel: return new Color(0.31f, 0.34f, 0.40f, 1f);
                case Archetype.SignalCaster: return new Color(0.31f, 0.23f, 0.39f, 1f);
                case Archetype.SignalRanger: return new Color(0.27f, 0.37f, 0.40f, 1f);
                case Archetype.CathedralBrute: return new Color(0.25f, 0.24f, 0.29f, 1f);
                case Archetype.CorruptedBeast: return new Color(0.30f, 0.27f, 0.31f, 1f);
                case Archetype.BoneRemnant: return new Color(0.53f, 0.53f, 0.49f, 1f);
                default: return new Color(0.34f, 0.38f, 0.43f, 1f);
            }
        }

        private static Color RoleSignalColor(Archetype role)
        {
            switch (role)
            {
                case Archetype.SignalCaster: return new Color(0.90f, 0.20f, 0.78f, 1f);
                case Archetype.CathedralBrute: return new Color(0.68f, 0.20f, 0.46f, 1f);
                case Archetype.CorruptedBeast: return new Color(0.74f, 0.18f, 0.58f, 1f);
                default: return new Color(0.30f, 0.82f, 1.00f, 1f);
            }
        }

        private static bool LooksLikeUiOrIndicator(Transform transform)
        {
            Transform current = transform;
            for (int depth = 0; current != null && depth < 5; depth++, current = current.parent)
            {
                string n = current.name.ToLowerInvariant();
                if (n.Contains("healthbar") || n.Contains("health bar") || n.Contains("targetindicator") ||
                    n.Contains("target indicator") || n.Contains("lockon") || n.Contains("lock on"))
                    return true;
            }
            return false;
        }

        private static bool ContainsAny(string value, params string[] tokens)
        {
            for (int i = 0; i < tokens.Length; i++)
                if (value.Contains(tokens[i])) return true;
            return false;
        }
    }
}
