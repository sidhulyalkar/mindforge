using UnityEngine;
using Mindforge.Presentation;

namespace Mindforge.Combat
{
    /// <summary>
    /// Procedural visual rig for the competition slice. Gameplay collision uses the
    /// same bounded resonance scales, while production meshes/animations can replace
    /// these transforms without changing combat authority.
    /// </summary>
    public sealed class GuardianSwordShieldRig : MonoBehaviour
    {
        [SerializeField] private Transform swordRoot;
        [SerializeField] private Transform swordBlade;
        [SerializeField] private Renderer swordRenderer;
        [SerializeField] private TrailRenderer swordTrail;
        [SerializeField] private Light swordLight;
        [SerializeField] private Transform shieldRoot;
        [SerializeField] private Renderer shieldRenderer;
        [SerializeField] private Light shieldLight;
        [SerializeField] private CombatVisualPalette palette;

        [Header("Presentation")]
        [SerializeField] private float maxSwordLengthBonus = 0.42f;
        [SerializeField] private float maxSwordWidthBonus = 0.18f;
        [SerializeField] private float maxEmissionMultiplier = 4.2f;
        [SerializeField] private float shieldForwardOffset = 0.78f;

        private Vector3 _swordBaseScale = Vector3.one;
        private Vector3 _shieldBaseScale = Vector3.one;
        private MaterialPropertyBlock _swordBlock;
        private MaterialPropertyBlock _shieldBlock;
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorProperty = Shader.PropertyToID("_Color");

        private void Awake()
        {
            _swordBlock = new MaterialPropertyBlock();
            _shieldBlock = new MaterialPropertyBlock();
            CaptureBaseScales();
        }

        public void ConfigureRuntime(
            Transform newSwordRoot,
            Transform newSwordBlade,
            Renderer newSwordRenderer,
            TrailRenderer newSwordTrail,
            Light newSwordLight,
            Transform newShieldRoot,
            Renderer newShieldRenderer,
            Light newShieldLight,
            CombatVisualPalette visualPalette = null)
        {
            swordRoot = newSwordRoot;
            swordBlade = newSwordBlade;
            swordRenderer = newSwordRenderer;
            swordTrail = newSwordTrail;
            swordLight = newSwordLight;
            shieldRoot = newShieldRoot;
            shieldRenderer = newShieldRenderer;
            shieldLight = newShieldLight;
            if (visualPalette != null) palette = visualPalette;
            CaptureBaseScales();
        }

        private void CaptureBaseScales()
        {
            if (swordBlade != null) _swordBaseScale = swordBlade.localScale;
            if (shieldRoot != null) _shieldBaseScale = shieldRoot.localScale;
        }

        public void SetCombatState(
            bool guarding,
            bool attacking,
            float attackProgress,
            Vector3 aimDirection,
            float sightResonance,
            float guardResonance,
            float guardCoverageScale,
            int comboStep = 1)
        {
            Vector3 aim = Vector3.ProjectOnPlane(aimDirection, Vector3.up);
            if (aim.sqrMagnitude < 0.01f) aim = transform.forward;
            aim.Normalize();
            Quaternion facing = Quaternion.LookRotation(aim, Vector3.up);

            float sight = Mathf.Clamp01(sightResonance);
            float guard = Mathf.Clamp01(guardResonance);

            if (swordRoot != null)
            {
                float yaw;
                float pitch;
                if (!attacking)
                {
                    yaw = 22f;
                    pitch = -12f;
                }
                else
                {
                    float eased = Mathf.SmoothStep(0f, 1f, attackProgress);
                    if (comboStep == 2)
                        yaw = Mathf.Lerp(72f, -72f, eased);
                    else if (comboStep >= 3)
                        yaw = Mathf.Lerp(-86f, 92f, eased);
                    else
                        yaw = Mathf.Lerp(-68f, 72f, eased);
                    pitch = comboStep >= 3
                        ? Mathf.Lerp(-18f, 15f, attackProgress)
                        : Mathf.Lerp(-7f, 8f, attackProgress);
                }
                swordRoot.rotation = facing * Quaternion.Euler(pitch, yaw, 0f);
            }

            if (swordBlade != null)
            {
                Vector3 scale = _swordBaseScale;
                scale.z *= 1f + maxSwordLengthBonus * sight;
                scale.x *= 1f + maxSwordWidthBonus * sight;
                if (attacking && comboStep >= 3) scale.x *= 1.08f;
                swordBlade.localScale = scale;
            }

            if (shieldRoot != null)
            {
                shieldRoot.gameObject.SetActive(true);
                shieldRoot.rotation = facing;
                shieldRoot.localScale = _shieldBaseScale * Mathf.Max(0.25f, guardCoverageScale);
                Vector3 localForward = transform.InverseTransformDirection(aim);
                shieldRoot.localPosition = localForward.normalized * shieldForwardOffset + Vector3.up * 0.54f + Vector3.left * 0.28f;
            }

            Color sightColor = palette != null ? palette.sightTarget : new Color(0.18f, 0.62f, 1f);
            Color sightHot = Color.Lerp(sightColor, new Color(0.55f, 0.96f, 1f), 0.62f);
            Color guardColor = palette != null ? palette.guardTarget : new Color(0.18f, 1f, 0.52f);
            ApplySwordRenderer(swordRenderer, _swordBlock, sightColor, sightHot, sight, attacking ? 1f : 0.50f);
            ApplyShieldRenderer(shieldRenderer, _shieldBlock, guardColor, guard, guarding ? 1f : 0.30f);

            if (swordTrail != null)
            {
                swordTrail.emitting = attacking;
                float finisher = attacking && comboStep >= 3 ? 1.30f : 1f;
                swordTrail.widthMultiplier = Mathf.Lerp(0.045f, 0.19f, sight) * finisher;
                Color trail = Color.Lerp(new Color(0.10f, 0.38f, 0.82f, 0.34f), sightHot, Mathf.Lerp(0.22f, 1f, sight));
                swordTrail.startColor = trail;
                swordTrail.endColor = new Color(trail.r, trail.g, trail.b, 0f);
                swordTrail.time = Mathf.Lerp(0.14f, 0.27f, sight) * (comboStep >= 3 ? 1.15f : 1f);
            }

            if (swordLight != null)
            {
                swordLight.color = Color.Lerp(new Color(0.12f, 0.34f, 0.78f), sightHot, sight);
                float finisher = attacking && comboStep >= 3 ? 1.35f : 1f;
                swordLight.intensity = (attacking ? Mathf.Lerp(0.55f, 3.4f, sight) : Mathf.Lerp(0.14f, 0.95f, sight)) * finisher;
                swordLight.range = Mathf.Lerp(2.0f, 3.4f, sight);
            }
            if (shieldLight != null)
            {
                shieldLight.color = guardColor;
                shieldLight.intensity = guarding ? Mathf.Lerp(0.3f, 3.5f, guard) : Mathf.Lerp(0.04f, 0.5f, guard);
                shieldLight.range = Mathf.Lerp(1.4f, 3.8f, guard);
            }
        }

        private void ApplySwordRenderer(Renderer renderer, MaterialPropertyBlock block, Color sightColor, Color sightHot, float resonance, float stateWeight)
        {
            if (renderer == null || block == null) return;
            float r = Mathf.Clamp01(resonance);
            Color forged = new Color(0.022f, 0.045f, 0.085f);
            Color chargedSteel = Color.Lerp(new Color(0.06f, 0.19f, 0.38f), sightColor, 0.52f);
            Color surface = Color.Lerp(forged, chargedSteel, Mathf.SmoothStep(0f, 1f, r));
            float glow = Mathf.Lerp(0.16f, maxEmissionMultiplier, r) * Mathf.Lerp(0.55f, 1f, Mathf.Clamp01(stateWeight));
            renderer.GetPropertyBlock(block);
            block.SetColor(BaseColor, surface);
            block.SetColor(ColorProperty, surface);
            block.SetColor(EmissionColor, Color.Lerp(sightColor, sightHot, r) * glow);
            renderer.SetPropertyBlock(block);
        }

        private void ApplyShieldRenderer(Renderer renderer, MaterialPropertyBlock block, Color color, float resonance, float stateWeight)
        {
            if (renderer == null || block == null) return;
            float r = Mathf.Clamp01(resonance);
            float glow = Mathf.Lerp(0.20f, maxEmissionMultiplier, r) * Mathf.Clamp01(stateWeight);
            Color baseShield = Color.Lerp(new Color(0.055f, 0.16f, 0.12f), color * 0.52f, r);
            renderer.GetPropertyBlock(block);
            block.SetColor(BaseColor, baseShield);
            block.SetColor(ColorProperty, baseShield);
            block.SetColor(EmissionColor, color * glow);
            renderer.SetPropertyBlock(block);
        }
    }
}
