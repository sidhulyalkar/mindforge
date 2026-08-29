using UnityEngine;
using UnityEngine.Rendering;
using Mindforge.Enemies;
using Mindforge.Journey;

namespace Mindforge.Presentation
{
    /// <summary>
    /// Presentation-only geometric intent language for ordinary enemies. The authoritative
    /// JourneyEnemyController still chooses, tracks, commits, times and resolves every attack.
    /// This layer reads that fixed-tick phase truth and turns it into spatial ground arcs,
    /// projectile lanes, a visible aim-lock transition and a brief recovery window.
    /// </summary>
    public sealed class JourneyEnemyIntentVfx : MonoBehaviour
    {
        [SerializeField] private JourneyEnemyController controller;
        [SerializeField] private float lineWidth = 0.032f;
        [SerializeField] private float groundOffset = 0.055f;
        [SerializeField] private float maximumPreviewRange = 8.0f;
        [SerializeField] private float committedWidthMultiplier = 1.65f;
        [SerializeField] private float recoveryRingRadius = 0.78f;

        private LineRenderer _outline;
        private readonly LineRenderer[] _rays = new LineRenderer[5];
        private EnemyAttackDefinition _attack;
        private bool _recoveryGeometry;
        private bool _subscribed;

        private static readonly Color Melee = new Color(1.00f, 0.25f, 0.07f, 0.90f);
        private static readonly Color Projectile = new Color(1.00f, 0.08f, 0.42f, 0.92f);
        private static readonly Color Burst = new Color(0.82f, 0.18f, 1.00f, 0.94f);
        private static readonly Color Retreat = new Color(0.95f, 0.52f, 0.15f, 0.80f);
        private static readonly Color Recovery = new Color(0.42f, 0.88f, 1.00f, 0.48f);

        private void Awake()
        {
            if (controller == null) controller = GetComponent<JourneyEnemyController>();
            BuildVisuals();
            HideAll();
        }

        private void OnEnable()
        {
            if (controller == null) controller = GetComponent<JourneyEnemyController>();
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
            _attack = null;
            _recoveryGeometry = false;
            HideAll();
        }

        private void Subscribe()
        {
            if (_subscribed || controller == null) return;
            controller.AttackSelected += OnAttackSelected;
            controller.AttackResolved += OnAttackResolved;
            controller.ArmedChanged += OnArmedChanged;
            controller.Defeated += OnDefeated;
            controller.Reconstructed += OnReconstructed;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed || controller == null) return;
            controller.AttackSelected -= OnAttackSelected;
            controller.AttackResolved -= OnAttackResolved;
            controller.ArmedChanged -= OnArmedChanged;
            controller.Defeated -= OnDefeated;
            controller.Reconstructed -= OnReconstructed;
            _subscribed = false;
        }

        private void BuildVisuals()
        {
            if (_outline != null) return;
            Material material = null;
            Transform core = transform.Find("Visuals/Core");
            Renderer coreRenderer = core != null ? core.GetComponent<Renderer>() : null;
            if (coreRenderer != null) material = coreRenderer.sharedMaterial;

            GameObject root = new GameObject("IntentTelegraphV2");
            root.transform.SetParent(transform, false);

            _outline = CreateLine("IntentOutline", root.transform, material);
            for (int i = 0; i < _rays.Length; i++)
                _rays[i] = CreateLine($"IntentRay_{i:00}", root.transform, material);
        }

        private LineRenderer CreateLine(string name, Transform parent, Material material)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            LineRenderer line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.useWorldSpace = false;
            line.loop = false;
            line.widthMultiplier = Mathf.Max(0.008f, lineWidth);
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.alignment = LineAlignment.TransformZ;
            line.textureMode = LineTextureMode.Stretch;
            line.positionCount = 0;
            return line;
        }

        private void OnAttackSelected(EnemyAttackDefinition attack)
        {
            if (attack == null) return;
            if (_outline == null) BuildVisuals();
            _attack = attack;
            _recoveryGeometry = false;
            DrawAttackShape(attack);
            SetWidths(Mathf.Max(0.008f, lineWidth));
            SetVisible(true);
        }

        private void OnAttackResolved(JourneyEnemyAttackKind kind)
        {
            _attack = null;
            _recoveryGeometry = controller != null && controller.IsRecovering;
            if (_recoveryGeometry)
            {
                DrawRecoveryRing();
                SetWidths(Mathf.Max(0.008f, lineWidth) * 1.15f);
                SetVisible(true);
            }
            else
            {
                HideAll();
            }
        }

        private void OnArmedChanged(bool armed)
        {
            if (!armed)
            {
                _attack = null;
                _recoveryGeometry = false;
                HideAll();
            }
        }

        private void OnDefeated(JourneyEnemyController enemy)
        {
            _attack = null;
            _recoveryGeometry = false;
            HideAll();
        }

        private void OnReconstructed(JourneyEnemyController enemy)
        {
            _attack = null;
            _recoveryGeometry = false;
            HideAll();
        }

        private void Update()
        {
            if (controller == null)
            {
                HideAll();
                return;
            }

            if (_attack != null && controller.PendingAttack != JourneyEnemyAttackKind.None)
            {
                _recoveryGeometry = false;
                float phase = controller.AttackTelegraphProgress01;
                bool committed = controller.AttackTrackingLocked;
                Color baseColor = ColorFor(_attack);

                // Progress itself comes from fixed-tick gameplay. Unscaled time is used only
                // for a small emissive breath, so pause/hit-stop cannot move the warning clock.
                float breath = 0.86f + 0.14f * Mathf.Sin(Time.unscaledTime * (committed ? 13f : 7f));
                float commitStart = Mathf.Clamp01(_attack.TrackingLock01);
                float commit01 = committed
                    ? Mathf.InverseLerp(commitStart, 1f, phase)
                    : 0f;
                Color animated = Color.Lerp(baseColor, Color.white, committed ? 0.34f + commit01 * 0.46f : phase * 0.16f);
                animated.a = Mathf.Clamp01(baseColor.a * breath * (0.72f + phase * 0.28f));
                ApplyColor(_outline, animated);
                for (int i = 0; i < _rays.Length; i++) ApplyColor(_rays[i], animated);

                float width = Mathf.Max(0.008f, lineWidth) *
                              Mathf.Lerp(1f, Mathf.Max(1f, committedWidthMultiplier), commit01);
                SetWidths(width);
                SetVisible(true);
                return;
            }

            if (controller.IsRecovering)
            {
                if (!_recoveryGeometry)
                {
                    _recoveryGeometry = true;
                    DrawRecoveryRing();
                }

                float recovery01 = controller.RecoveryProgress01;
                Color color = Recovery;
                color.a *= 1f - recovery01;
                ApplyColor(_outline, color);
                for (int i = 0; i < _rays.Length; i++) ApplyColor(_rays[i], color);
                SetWidths(Mathf.Max(0.008f, lineWidth) * Mathf.Lerp(1.20f, 0.62f, recovery01));
                SetVisible(true);
                return;
            }

            _attack = null;
            _recoveryGeometry = false;
            HideAll();
        }

        private void DrawAttackShape(EnemyAttackDefinition attack)
        {
            ClearGeometry();
            switch (attack.Type)
            {
                case EnemyAttackType.Melee:
                    DrawMeleeArc(attack);
                    break;
                case EnemyAttackType.Projectile:
                    DrawProjectileFan(attack, 1);
                    break;
                case EnemyAttackType.Burst:
                    DrawProjectileFan(attack, Mathf.Clamp(attack.ProjectileCount, 2, _rays.Length));
                    break;
                case EnemyAttackType.Retreat:
                    DrawRetreatRing();
                    break;
            }
        }

        private void DrawMeleeArc(EnemyAttackDefinition attack)
        {
            float radius = Mathf.Clamp(attack.MaximumRange, 0.6f, Mathf.Max(0.6f, maximumPreviewRange));
            float arc = Mathf.Clamp(attack.MaximumFacingAngle, 24f, 155f);
            const int points = 25;
            _outline.positionCount = points + 2;
            _outline.SetPosition(0, new Vector3(0f, groundOffset, 0f));
            for (int i = 0; i < points; i++)
            {
                float angle = Mathf.Lerp(-arc * 0.5f, arc * 0.5f, i / (float)(points - 1)) * Mathf.Deg2Rad;
                _outline.SetPosition(i + 1, new Vector3(Mathf.Sin(angle) * radius, groundOffset, Mathf.Cos(angle) * radius));
            }
            _outline.SetPosition(points + 1, new Vector3(0f, groundOffset, 0f));
        }

        private void DrawProjectileFan(EnemyAttackDefinition attack, int requestedRayCount)
        {
            int count = Mathf.Clamp(requestedRayCount, 1, _rays.Length);
            float range = Mathf.Clamp(attack.MaximumRange, 1.5f, Mathf.Max(1.5f, maximumPreviewRange));
            float spread = attack.Type == EnemyAttackType.Burst
                ? Mathf.Max(attack.ProjectileSpreadDegrees, 12f)
                : 0f;

            for (int i = 0; i < count; i++)
            {
                float centered = count <= 1 ? 0f : i - (count - 1) * 0.5f;
                float angle = count <= 1 ? 0f : centered * spread / Mathf.Max(1f, count - 1);
                float radians = angle * Mathf.Deg2Rad;
                Vector3 direction = new Vector3(Mathf.Sin(radians), 0f, Mathf.Cos(radians));
                LineRenderer ray = _rays[i];
                ray.positionCount = 2;
                ray.SetPosition(0, new Vector3(0f, 0.28f, 0.18f));
                ray.SetPosition(1, direction * range + Vector3.up * 0.28f);
            }

            if (count == 1)
            {
                _outline.positionCount = 2;
                _outline.SetPosition(0, new Vector3(-0.32f, 0.28f, range));
                _outline.SetPosition(1, new Vector3(0.32f, 0.28f, range));
            }
        }

        private void DrawRetreatRing()
        {
            const int points = 33;
            const float radius = 1.15f;
            _outline.loop = true;
            _outline.positionCount = points;
            for (int i = 0; i < points; i++)
            {
                float angle = i / (float)points * Mathf.PI * 2f;
                _outline.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, groundOffset, Mathf.Sin(angle) * radius));
            }
        }

        private void DrawRecoveryRing()
        {
            ClearGeometry();
            const int points = 33;
            float radius = Mathf.Max(0.35f, recoveryRingRadius);
            _outline.loop = true;
            _outline.positionCount = points;
            for (int i = 0; i < points; i++)
            {
                float angle = i / (float)points * Mathf.PI * 2f;
                _outline.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, groundOffset, Mathf.Sin(angle) * radius));
            }
        }

        private void ClearGeometry()
        {
            if (_outline != null)
            {
                _outline.loop = false;
                _outline.positionCount = 0;
            }
            for (int i = 0; i < _rays.Length; i++)
                if (_rays[i] != null) _rays[i].positionCount = 0;
        }

        private void SetVisible(bool visible)
        {
            if (_outline != null) _outline.enabled = visible && _outline.positionCount > 0;
            for (int i = 0; i < _rays.Length; i++)
                if (_rays[i] != null) _rays[i].enabled = visible && _rays[i].positionCount > 0;
        }

        private void HideAll()
        {
            if (_outline != null) _outline.enabled = false;
            for (int i = 0; i < _rays.Length; i++)
                if (_rays[i] != null) _rays[i].enabled = false;
        }

        private void SetWidths(float width)
        {
            if (_outline != null) _outline.widthMultiplier = width;
            for (int i = 0; i < _rays.Length; i++)
                if (_rays[i] != null) _rays[i].widthMultiplier = width;
        }

        private static Color ColorFor(EnemyAttackDefinition attack)
        {
            if (attack == null) return Projectile;
            if (attack.Heavy) return new Color(1.00f, 0.50f, 0.08f, 0.96f);
            switch (attack.Type)
            {
                case EnemyAttackType.Melee: return Melee;
                case EnemyAttackType.Burst: return Burst;
                case EnemyAttackType.Retreat: return Retreat;
                default: return Projectile;
            }
        }

        private static void ApplyColor(LineRenderer line, Color color)
        {
            if (line == null) return;
            line.startColor = color;
            line.endColor = new Color(color.r, color.g, color.b, color.a * 0.18f);
        }
    }
}
