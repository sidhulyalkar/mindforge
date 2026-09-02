using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mindforge.Chassis
{
    /// <summary>
    /// Presentation-only Mindforge treatment for Dragon Souls' fully authored
    /// Nightmare Dragon. V0.29 intentionally keeps the upstream skeleton,
    /// skinned meshes, animation controller, boss collider/hurt logic, behaviour
    /// tree, pools and attack-event transforms intact.
    ///
    /// No geometry is generated here. The component only applies per-renderer
    /// material property blocks and a restrained local key light, so the boss
    /// remains one coherent animal rather than another collection of floating
    /// procedural shapes.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MindforgeDragonBossPresentationV29 : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly int MetallicId = Shader.PropertyToID("_Metallic");
        private static readonly int SmoothnessId = Shader.PropertyToID("_Smoothness");
        private static readonly int GlossinessId = Shader.PropertyToID("_Glossiness");

        [Header("Mindforge palette")]
        [SerializeField] private Color bodyTint = new Color(0.47f, 0.54f, 0.58f, 1f);
        [SerializeField] private Color membraneTint = new Color(0.35f, 0.29f, 0.34f, 1f);
        [SerializeField] private Color corruptionTint = new Color(0.78f, 0.12f, 0.52f, 1f);
        [SerializeField] private Color neuralTint = new Color(0.18f, 0.88f, 1.0f, 1f);

        [Header("Surface response")]
        [SerializeField, Range(0f, 1f)] private float bodyMetallic = 0.04f;
        [SerializeField, Range(0f, 1f)] private float bodySmoothness = 0.31f;
        [SerializeField, Range(0f, 1f)] private float membraneSmoothness = 0.43f;
        [SerializeField] private float corruptionEmission = 2.2f;
        [SerializeField] private float neuralEmission = 1.35f;

        private Renderer[] _renderers;
        private MaterialPropertyBlock _block;
        private Light _localKey;
        private bool _installed;

        public bool Installed => _installed;
        public int RendererCount => _renderers != null ? _renderers.Length : 0;

        private void Awake()
        {
            Install();
        }

        private void OnEnable()
        {
            Install();
        }

        private void OnDisable()
        {
            ClearPropertyBlocks();
        }

        public void Install()
        {
            if (_installed) return;
            _renderers = GetComponentsInChildren<Renderer>(true);
            _block = new MaterialPropertyBlock();
            ApplySurfaceTreatment();
            InstallLocalKey();
            _installed = _renderers != null && _renderers.Length > 0;
        }

        private void ApplySurfaceTreatment()
        {
            if (_renderers == null) return;
            for (int r = 0; r < _renderers.Length; r++)
            {
                Renderer renderer = _renderers[r];
                if (renderer == null) continue;

                Material[] materials = renderer.sharedMaterials;
                for (int m = 0; m < materials.Length; m++)
                {
                    Material shared = materials[m];
                    if (shared == null) continue;

                    renderer.GetPropertyBlock(_block, m);
                    string semantic = (renderer.name + " " + shared.name).ToLowerInvariant();
                    Color original = ReadBaseColor(shared);
                    Color tint = ClassifyTint(semantic);
                    Color resolved = MultiplyPreservingValue(original, tint);

                    if (shared.HasProperty(BaseColorId)) _block.SetColor(BaseColorId, resolved);
                    if (shared.HasProperty(ColorId)) _block.SetColor(ColorId, resolved);

                    bool corruption = ContainsAny(semantic, "fire", "flame", "lava", "mouth", "eye", "magic", "glow");
                    bool neural = ContainsAny(semantic, "eye", "pupil", "magic", "crystal");
                    if (shared.HasProperty(EmissionColorId) && (corruption || neural))
                    {
                        Color emissionColor = neural ? neuralTint * neuralEmission : corruptionTint * corruptionEmission;
                        _block.SetColor(EmissionColorId, emissionColor);
                    }

                    bool membrane = ContainsAny(semantic, "wing", "membrane", "skin_wing");
                    if (shared.HasProperty(MetallicId)) _block.SetFloat(MetallicId, bodyMetallic);
                    if (shared.HasProperty(SmoothnessId))
                        _block.SetFloat(SmoothnessId, membrane ? membraneSmoothness : bodySmoothness);
                    if (shared.HasProperty(GlossinessId))
                        _block.SetFloat(GlossinessId, membrane ? membraneSmoothness : bodySmoothness);

                    renderer.SetPropertyBlock(_block, m);
                    _block.Clear();
                }
            }
        }

        private void InstallLocalKey()
        {
            Transform existing = transform.Find("Mindforge_Boss_LocalKey_V29");
            if (existing != null)
            {
                _localKey = existing.GetComponent<Light>();
                return;
            }

            GameObject lightObject = new GameObject("Mindforge_Boss_LocalKey_V29");
            lightObject.transform.SetParent(transform, false);
            lightObject.transform.localPosition = new Vector3(0f, 2.25f, 0.15f);
            _localKey = lightObject.AddComponent<Light>();
            _localKey.type = LightType.Point;
            _localKey.color = new Color(0.42f, 0.68f, 0.78f, 1f);
            _localKey.intensity = 0.62f;
            _localKey.range = 7.5f;
            _localKey.shadows = LightShadows.None;
            _localKey.renderMode = LightRenderMode.Auto;
        }

        private void ClearPropertyBlocks()
        {
            if (_renderers == null) return;
            for (int r = 0; r < _renderers.Length; r++)
            {
                Renderer renderer = _renderers[r];
                if (renderer == null) continue;
                Material[] materials = renderer.sharedMaterials;
                for (int m = 0; m < materials.Length; m++)
                    renderer.SetPropertyBlock(null, m);
            }
        }

        private Color ClassifyTint(string semantic)
        {
            if (ContainsAny(semantic, "wing", "membrane", "skin_wing")) return membraneTint;
            if (ContainsAny(semantic, "fire", "flame", "lava", "mouth")) return corruptionTint;
            if (ContainsAny(semantic, "eye", "pupil", "magic", "crystal")) return neuralTint;
            return bodyTint;
        }

        private static Color ReadBaseColor(Material material)
        {
            if (material.HasProperty(BaseColorId)) return material.GetColor(BaseColorId);
            if (material.HasProperty(ColorId)) return material.GetColor(ColorId);
            return Color.white;
        }

        private static Color MultiplyPreservingValue(Color original, Color tint)
        {
            // Preserve the authored texture/material value structure while steering
            // the palette. Avoid flattening every renderer into one neon color.
            float value = Mathf.Max(original.r, Mathf.Max(original.g, original.b));
            float floor = Mathf.Lerp(0.42f, 1f, Mathf.Clamp01(value));
            return new Color(
                Mathf.Clamp01(tint.r * floor),
                Mathf.Clamp01(tint.g * floor),
                Mathf.Clamp01(tint.b * floor),
                original.a);
        }

        private static bool ContainsAny(string value, params string[] terms)
        {
            for (int i = 0; i < terms.Length; i++)
                if (value.Contains(terms[i])) return true;
            return false;
        }
    }

    /// <summary>
    /// Finds the upstream authored Nightmare Dragon after scene load and installs
    /// only Mindforge presentation. The boss AI/controller remains upstream-owned.
    /// </summary>
    public static class MindforgeDragonBossInstallerV29
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallAfterSceneLoad()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            InstallInActiveScene();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            InstallInActiveScene();
        }

        private static void InstallInActiveScene()
        {
            EnemyNightmareDragonController[] dragons =
                UnityEngine.Object.FindObjectsOfType<EnemyNightmareDragonController>(true);
            for (int i = 0; i < dragons.Length; i++)
            {
                EnemyNightmareDragonController dragon = dragons[i];
                if (dragon == null) continue;
                if (dragon.GetComponent<MindforgeDragonBossPresentationV29>() == null)
                    dragon.gameObject.AddComponent<MindforgeDragonBossPresentationV29>();
            }
        }
    }
}
