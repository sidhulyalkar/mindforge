using UnityEngine;
using Mindforge.Presentation;

namespace Mindforge.Combat
{
    /// <summary>
    /// Procedural placeholder rig for the competition slice. Gameplay collision uses
    /// the same bounded resonance scales, while production meshes/animations can later
    /// replace these transforms without changing combat authority.
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

            Color sightColor = palette != null ? palette.sightTarget : new Color(0.20f, 0.55f, 1f);
            Color guardColor = palette != null ? palette.guardTarget : new Color(0.18f, 1f, 0.52f);
            ApplyRenderer(swordRenderer, _swordBlock, sightColor, sight, attacking ? 1f : 0.62f);
            ApplyRenderer(shieldRenderer, _shieldBlock, guardColor, guard, guarding ? 1f : 0.34f);

            if (swordTrail != null)
            {
                swordTrail.emitting = attacking;
                float finisher = attacking && comboStep >= 3 ? 1.30f : 1f;
                swordTrail.widthMultiplier = Mathf.Lerp(0.05f, 0.18f, sight) * finisher;
                Color trail = Color.Lerp(new Color(sightColor.r, sightColor.g, sightColor.b, 0.18f), sightColor, sight);
                swordTrail.startColor = trail;
                swordTrail.endColor = new Color(trail.r, trail.g, trail.b, 0f);
            }

            if (swordLight != null)
            {
                swordLight.color = sightColor;
                float finisher = attacking && comboStep >= 3 ? 1.35f : 1f;
                swordLight.intensity = (attacking ? Mathf.Lerp(0.35f, 3.1f, sight) : Mathf.Lerp(0.08f, 0.8f, sight)) * finisher;
            }
            if (shieldLight != null)
            {
                shieldLight.color = guardColor;
                shieldLight.intensity = guarding ? Mathf.Lerp(0.3f, 3.5f, guard) : Mathf.Lerp(0.04f, 0.5f, guard);
                shieldLight.range = Mathf.Lerp(1.4f, 3.8f, guard);
            }
        }

        private void ApplyRenderer(Renderer renderer, MaterialPropertyBlock block, Color color, float resonance, float stateWeight)
        {
            if (renderer == null || block == null) return;
            float glow = Mathf.Lerp(0.45f, maxEmissionMultiplier, Mathf.Clamp01(resonance)) * Mathf.Clamp01(stateWeight);
            renderer.GetPropertyBlock(block);
            block.SetColor(BaseColor, Color.Lerp(Color.white * 0.55f, color, Mathf.Clamp01(resonance * 0.8f)));
            block.SetColor(ColorProperty, Color.Lerp(Color.white * 0.55f, color, Mathf.Clamp01(resonance * 0.8f)));
            block.SetColor(EmissionColor, color * glow);
            renderer.SetPropertyBlock(block);
        }
    }
}
