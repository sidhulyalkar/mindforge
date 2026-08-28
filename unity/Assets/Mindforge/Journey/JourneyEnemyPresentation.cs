using UnityEngine;

namespace Mindforge.Journey
{
    /// <summary>
    /// Presentation-only companion for JourneyEnemyController. It visualizes intent,
    /// timing, recovery and death without issuing damage, movement, targeting or neural
    /// commands.
    /// </summary>
    public sealed class JourneyEnemyPresentation : MonoBehaviour
    {
        [SerializeField] private JourneyEnemyController controller;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Transform core;
        [SerializeField] private Transform telegraphRing;
        [SerializeField] private Renderer coreRenderer;
        [SerializeField] private Light coreLight;

        [Header("Motion")]
        [SerializeField] private float idleBobAmplitude = 0.055f;
        [SerializeField] private float idleBobSpeed = 2.1f;
        [SerializeField] private float coreSpinDegreesPerSecond = 55f;
        [SerializeField] private float telegraphMaxScale = 1.55f;
        [SerializeField] private float resolvedFlashSeconds = 0.10f;

        [Header("Telegraph colors")]
        [SerializeField] private Color idleColor = new Color(0.82f, 0.10f, 0.24f);
        [SerializeField] private Color meleeColor = new Color(1.00f, 0.28f, 0.08f);
        [SerializeField] private Color projectileColor = new Color(0.95f, 0.10f, 0.48f);
        [SerializeField] private Color wardenColor = new Color(0.82f, 0.18f, 0.95f);

        private Vector3 _visualBaseLocalPosition;
        private Vector3 _ringBaseScale;
        private float _telegraphStartedAt;
        private float _telegraphUntil;
        private float _flashUntil;
        private bool _dying;
        private float _deathStartedAt;
        private JourneyEnemyAttackKind _telegraphKind;
        private MaterialPropertyBlock _block;

        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorProperty = Shader.PropertyToID("_Color");
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        public void ConfigureRuntime(
            JourneyEnemyController enemy,
            Transform visuals,
            Transform enemyCore,
            Transform ring,
            Renderer renderer,
            Light light)
        {
            controller = enemy;
            visualRoot = visuals;
            core = enemyCore;
            telegraphRing = ring;
            coreRenderer = renderer;
            coreLight = light;
            CaptureBases();
        }

        private void Awake()
        {
            _block = new MaterialPropertyBlock();
            if (controller == null) controller = GetComponent<JourneyEnemyController>();
            CaptureBases();
            ApplyColor(IdleForArchetype(), 1.15f);
            if (telegraphRing != null) telegraphRing.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            Subscribe();
            CaptureBases();
        }

        private void OnDisable() => Unsubscribe();

        private void Subscribe()
        {
            if (controller == null) return;
            controller.AttackTelegraphed -= OnAttackTelegraphed;
            controller.AttackResolved -= OnAttackResolved;
            controller.Defeated -= OnDefeated;
            controller.AttackTelegraphed += OnAttackTelegraphed;
            controller.AttackResolved += OnAttackResolved;
            controller.Defeated += OnDefeated;
        }

        private void Unsubscribe()
        {
            if (controller == null) return;
            controller.AttackTelegraphed -= OnAttackTelegraphed;
            controller.AttackResolved -= OnAttackResolved;
            controller.Defeated -= OnDefeated;
        }

        private void Update()
        {
            float now = Time.time;
            if (_dying)
            {
                float t = Mathf.Clamp01((now - _deathStartedAt) / 0.32f);
                if (visualRoot != null)
                    visualRoot.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 0.08f, t * t);
                if (coreLight != null) coreLight.intensity = Mathf.Lerp(coreLight.intensity, 0f, t);
                return;
            }

            if (visualRoot != null)
            {
                Vector3 bob = Vector3.up * Mathf.Sin(Time.time * idleBobSpeed + GetInstanceID() * 0.01f) * idleBobAmplitude;
                visualRoot.localPosition = _visualBaseLocalPosition + bob;
            }
            if (core != null)
                core.Rotate(Vector3.up, coreSpinDegreesPerSecond * Time.deltaTime, Space.Self);

            bool telegraphing = now < _telegraphUntil;
            if (telegraphRing != null)
            {
                if (telegraphRing.gameObject.activeSelf != telegraphing)
                    telegraphRing.gameObject.SetActive(telegraphing);
                if (telegraphing)
                {
                    float t = Mathf.InverseLerp(_telegraphStartedAt, Mathf.Max(_telegraphStartedAt + 0.01f, _telegraphUntil), now);
                    float scale = Mathf.Lerp(0.45f, telegraphMaxScale, Mathf.SmoothStep(0f, 1f, t));
                    telegraphRing.localScale = _ringBaseScale * scale;
                    telegraphRing.Rotate(Vector3.up, (120f + t * 180f) * Time.deltaTime, Space.Self);
                }
            }

            if (telegraphing)
            {
                float t = Mathf.InverseLerp(_telegraphStartedAt, Mathf.Max(_telegraphStartedAt + 0.01f, _telegraphUntil), now);
                Color color = ColorFor(_telegraphKind);
                ApplyColor(color, Mathf.Lerp(1.4f, 4.2f, t));
            }
            else if (now < _flashUntil)
            {
                ApplyColor(Color.white, 5.2f);
            }
            else
            {
                ApplyColor(IdleForArchetype(), 1.15f);
            }
        }

        private void OnAttackTelegraphed(JourneyEnemyAttackKind kind, float duration)
        {
            _telegraphKind = kind;
            _telegraphStartedAt = Time.time;
            _telegraphUntil = Time.time + Mathf.Max(0.05f, duration);
            if (telegraphRing != null)
            {
                telegraphRing.localScale = _ringBaseScale * 0.45f;
                telegraphRing.gameObject.SetActive(true);
            }
        }

        private void OnAttackResolved(JourneyEnemyAttackKind kind)
        {
            _telegraphUntil = -1f;
            _flashUntil = Time.time + Mathf.Max(0.02f, resolvedFlashSeconds);
            if (telegraphRing != null) telegraphRing.gameObject.SetActive(false);
        }

        private void OnDefeated(JourneyEnemyController enemy)
        {
            _dying = true;
            _deathStartedAt = Time.time;
            _telegraphUntil = -1f;
            if (telegraphRing != null) telegraphRing.gameObject.SetActive(false);
        }

        private void CaptureBases()
        {
            if (visualRoot != null) _visualBaseLocalPosition = visualRoot.localPosition;
            if (telegraphRing != null)
            {
                _ringBaseScale = telegraphRing.localScale;
                if (_ringBaseScale.sqrMagnitude < 0.001f) _ringBaseScale = Vector3.one;
            }
        }

        private Color IdleForArchetype()
        {
            return controller != null && controller.Archetype == JourneyEnemyArchetype.SignalWarden
                ? wardenColor
                : idleColor;
        }

        private Color ColorFor(JourneyEnemyAttackKind kind)
        {
            if (controller != null && controller.Archetype == JourneyEnemyArchetype.SignalWarden)
                return wardenColor;
            return kind == JourneyEnemyAttackKind.Melee ? meleeColor : projectileColor;
        }

        private void ApplyColor(Color color, float emission)
        {
            if (_block == null) _block = new MaterialPropertyBlock();
            if (coreRenderer != null)
            {
                coreRenderer.GetPropertyBlock(_block);
                _block.SetColor(BaseColor, color * 0.55f);
                _block.SetColor(ColorProperty, color * 0.55f);
                _block.SetColor(EmissionColor, color * Mathf.Max(0f, emission));
                coreRenderer.SetPropertyBlock(_block);
            }
            if (coreLight != null)
            {
                coreLight.color = color;
                coreLight.intensity = Mathf.Clamp(emission * 0.65f, 0.4f, 4.5f);
            }
        }
    }
}
