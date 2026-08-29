using UnityEngine;
using UnityEngine.Rendering;
using Mindforge.Combat;

namespace Mindforge.Presentation
{
    /// <summary>
    /// Presentation-only second-stage treatment for the runtime Aetherblade. The physical
    /// sword controller still owns attack/contact/parry authority. This component reads
    /// accepted combat/resonance state only to shape emission, light and trail intensity.
    /// It never changes reach, damage, action timing, target lock or neural evidence.
    /// </summary>
    public sealed class AetherbladeVisualPolishV2 : MonoBehaviour
    {
        private const string VisualRootName = "AetherbladeVisualPolishV2";
        private const string EnergyVisualRootName = "AetherbladeEnergyVisualsV2";
        private const string AfterimageTipName = "AetherbladeAfterimageTipV2";

        private GuardianSwordShieldController _combat;
        private Transform _energyScale;
        private Renderer _outerBloom;
        private Renderer _tipGlow;
        private Renderer[] _vents;
        private TrailRenderer _afterTrail;
        private TrailRenderer _primaryTrail;
        private Light _tipLight;
        private Light _emitterLight;
        private MaterialPropertyBlock _block;
        private Vector3 _outerBaseScale;
        private Vector3 _tipBaseScale;
        private bool _built;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            GuardianCombatInput input = Object.FindObjectOfType<GuardianCombatInput>(true);
            if (input == null || input.GetComponent<AetherbladeVisualPolishV2>() != null) return;
            input.gameObject.AddComponent<AetherbladeVisualPolishV2>();
        }

        private void Awake()
        {
            _combat = GetComponent<GuardianSwordShieldController>();
            _block = new MaterialPropertyBlock();
        }

        private void LateUpdate()
        {
            if (!_built && !TryBuild()) return;
            if (_combat == null) _combat = GetComponent<GuardianSwordShieldController>();

            bool attacking = _combat != null && _combat.IsAttacking;
            bool active = _combat != null && _combat.IsAttackActive;
            float attackProgress = _combat != null ? _combat.AttackProgress : 0f;
            float sight = _combat != null ? Mathf.Clamp01(_combat.SightResonance) : 0f;
            float pulse = 0.94f + 0.06f * Mathf.Sin(Time.unscaledTime * 19f + attackProgress * 7f);
            float attackEnergy = attacking ? (active ? 1f : 0.55f) : 0f;

            if (_outerBloom != null)
            {
                float width = pulse * (1f + sight * 0.22f + attackEnergy * 0.16f);
                _outerBloom.transform.localScale = new Vector3(
                    _outerBaseScale.x * width,
                    _outerBaseScale.y * (1f + sight * 0.03f),
                    _outerBaseScale.z * width);
                ApplyEmission(_outerBloom, new Color(0.10f, 0.62f, 1f), 2.2f + sight * 4.0f + attackEnergy * 2.3f, 0.20f + attackEnergy * 0.13f);
            }

            if (_tipGlow != null)
            {
                float swell = 1f + sight * 0.16f + attackEnergy * 0.20f;
                _tipGlow.transform.localScale = _tipBaseScale * swell;
                ApplyEmission(_tipGlow, new Color(0.52f, 0.96f, 1f), 4.2f + sight * 4.5f + attackEnergy * 3.0f, 0.46f);
            }

            if (_vents != null)
            {
                for (int i = 0; i < _vents.Length; i++)
                    if (_vents[i] != null)
                        ApplyEmission(_vents[i], new Color(0.16f, 0.78f, 1f), 2.1f + attackEnergy * 3.8f + sight * 2.2f, 0.78f);
            }

            if (_afterTrail != null) _afterTrail.emitting = attacking;
            if (_primaryTrail != null)
            {
                _primaryTrail.time = active ? 0.20f : 0.145f;
                _primaryTrail.startWidth = 0.115f + sight * 0.045f + attackEnergy * 0.035f;
                _primaryTrail.endWidth = 0.012f;
            }

            if (_tipLight != null)
            {
                _tipLight.intensity = 0.48f + sight * 0.38f + attackEnergy * 0.42f;
                _tipLight.range = 2.8f + sight * 0.9f + attackEnergy * 0.6f;
            }
            if (_emitterLight != null)
            {
                _emitterLight.intensity = 0.10f + sight * 0.16f + attackEnergy * 0.32f;
                _emitterLight.range = 1.25f + attackEnergy * 0.65f;
            }
        }

        private bool TryBuild()
        {
            Transform arsenal = transform.Find("PhysicalArsenalRig");
            Transform sword = arsenal != null ? arsenal.Find("SwordRoot") : null;
            _energyScale = sword != null ? sword.Find("AetherbladeEnergyScale") : null;
            if (_energyScale == null) return false;

            Transform existing = sword.Find(VisualRootName);
            if (existing != null) Destroy(existing.gameObject);
            Transform existingEnergy = _energyScale.Find(EnergyVisualRootName);
            if (existingEnergy != null) Destroy(existingEnergy.gameObject);

            GameObject root = new GameObject(VisualRootName);
            root.transform.SetParent(sword, false);
            GameObject energyRoot = new GameObject(EnergyVisualRootName);
            energyRoot.transform.SetParent(_energyScale, false);

            Material bloomMaterial = CreateTransparentEnergyMaterial(
                "AetherbladeOuterBloomV2",
                new Color(0.08f, 0.58f, 1f, 0.20f),
                new Color(0.10f, 0.68f, 1f) * 3.2f);
            Material hotMaterial = CreateTransparentEnergyMaterial(
                "AetherbladeTipHotV2",
                new Color(0.62f, 0.96f, 1f, 0.48f),
                new Color(0.62f, 0.98f, 1f) * 5.5f);
            Material trailMaterial = CreateTrailMaterial();

            GameObject bloom = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bloom.name = "AetherbladeOuterBloom";
            bloom.transform.SetParent(energyRoot.transform, false);
            bloom.transform.localPosition = new Vector3(0f, 0f, 1.02f);
            bloom.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            bloom.transform.localScale = new Vector3(0.132f, 0.815f, 0.132f);
            DisableCollider(bloom);
            _outerBloom = bloom.GetComponent<Renderer>();
            if (_outerBloom != null)
            {
                _outerBloom.sharedMaterial = bloomMaterial;
                _outerBloom.shadowCastingMode = ShadowCastingMode.Off;
                _outerBloom.receiveShadows = false;
            }
            _outerBaseScale = bloom.transform.localScale;

            GameObject tip = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            tip.name = "AetherbladeTipCapV2";
            tip.transform.SetParent(energyRoot.transform, false);
            tip.transform.localPosition = new Vector3(0f, 0f, 1.84f);
            tip.transform.localScale = Vector3.one * 0.155f;
            DisableCollider(tip);
            _tipGlow = tip.GetComponent<Renderer>();
            if (_tipGlow != null)
            {
                _tipGlow.sharedMaterial = hotMaterial;
                _tipGlow.shadowCastingMode = ShadowCastingMode.Off;
                _tipGlow.receiveShadows = false;
            }
            _tipBaseScale = tip.transform.localScale;

            _vents = new Renderer[4];
            for (int i = 0; i < 4; i++)
            {
                float angle = i * Mathf.PI * 0.5f;
                GameObject vent = GameObject.CreatePrimitive(PrimitiveType.Cube);
                vent.name = $"AetherbladeEmitterVent_{i:00}";
                vent.transform.SetParent(root.transform, false);
                vent.transform.localPosition = new Vector3(Mathf.Cos(angle) * 0.13f, Mathf.Sin(angle) * 0.13f, 0.18f);
                vent.transform.localRotation = Quaternion.Euler(0f, 0f, -angle * Mathf.Rad2Deg);
                vent.transform.localScale = new Vector3(0.065f, 0.025f, 0.12f);
                DisableCollider(vent);
                Renderer renderer = vent.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.sharedMaterial = hotMaterial;
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                }
                _vents[i] = renderer;
            }

            Transform tipAnchor = _energyScale.Find("SwordEnergyTip");
            if (tipAnchor != null)
            {
                _primaryTrail = tipAnchor.GetComponent<TrailRenderer>();
                _tipLight = tipAnchor.GetComponent<Light>();

                Transform oldAfterimage = tipAnchor.Find(AfterimageTipName);
                if (oldAfterimage != null) Destroy(oldAfterimage.gameObject);
                GameObject afterimageTip = new GameObject(AfterimageTipName);
                afterimageTip.transform.SetParent(tipAnchor, false);
                _afterTrail = afterimageTip.AddComponent<TrailRenderer>();
                _afterTrail.sharedMaterial = trailMaterial;
                _afterTrail.time = 0.085f;
                _afterTrail.minVertexDistance = 0.018f;
                _afterTrail.startWidth = 0.24f;
                _afterTrail.endWidth = 0.015f;
                _afterTrail.startColor = new Color(0.22f, 0.80f, 1f, 0.48f);
                _afterTrail.endColor = new Color(0.08f, 0.34f, 1f, 0f);
                _afterTrail.emitting = false;
                _afterTrail.shadowCastingMode = ShadowCastingMode.Off;
                _afterTrail.receiveShadows = false;
            }

            GameObject emitterLightGo = new GameObject("AetherbladeEmitterLightV2");
            emitterLightGo.transform.SetParent(root.transform, false);
            emitterLightGo.transform.localPosition = new Vector3(0f, 0f, 0.18f);
            _emitterLight = emitterLightGo.AddComponent<Light>();
            _emitterLight.type = LightType.Point;
            _emitterLight.color = new Color(0.22f, 0.82f, 1f);
            _emitterLight.range = 1.4f;
            _emitterLight.intensity = 0.12f;
            _emitterLight.shadows = LightShadows.None;

            _combat = GetComponent<GuardianSwordShieldController>();
            _built = true;
            return true;
        }

        private void ApplyEmission(Renderer renderer, Color color, float intensity, float alpha)
        {
            if (renderer == null) return;
            renderer.GetPropertyBlock(_block);
            Color baseColor = new Color(color.r, color.g, color.b, Mathf.Clamp01(alpha));
            if (renderer.sharedMaterial != null && renderer.sharedMaterial.HasProperty("_BaseColor"))
                _block.SetColor("_BaseColor", baseColor);
            else _block.SetColor("_Color", baseColor);
            _block.SetColor("_EmissionColor", color * Mathf.Max(0f, intensity));
            renderer.SetPropertyBlock(_block);
        }

        private static Material CreateTransparentEnergyMaterial(string name, Color color, Color emission)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material material = new Material(shader) { name = name };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            else if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission);
            }
            if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
            if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;
            return material;
        }

        private static Material CreateTrailMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ??
                            Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            Material material = new Material(shader) { name = "AetherbladeAfterimageV2" };
            Color color = new Color(0.18f, 0.72f, 1f, 0.50f);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            else if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            material.renderQueue = (int)RenderQueue.Transparent;
            return material;
        }

        private static void DisableCollider(GameObject go)
        {
            Collider collider = go != null ? go.GetComponent<Collider>() : null;
            if (collider != null) collider.enabled = false;
        }
    }
}
