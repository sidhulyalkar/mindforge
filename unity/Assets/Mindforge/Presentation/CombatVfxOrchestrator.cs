using UnityEngine;
using Mindforge.Combat;

namespace Mindforge.Presentation
{
    /// <summary>
    /// Semantic presentation layer for combat consequences. It subscribes to already-
    /// authoritative events and turns them into distinct impact languages; it cannot
    /// deal damage, spend stamina, move actors, apply neural state or award Flux.
    /// </summary>
    public sealed class CombatVfxOrchestrator : MonoBehaviour
    {
        [SerializeField] private GuardianSwordShieldController physical;
        [SerializeField] private GuardianCombatInput input;
        [SerializeField] private CombatantVitals playerVitals;
        [SerializeField] private CombatantVitals bossVitals;
        [SerializeField] private FracturedSignalDirector bossDirector;
        [SerializeField] private CombatPresentationDirector presentation;

        private Material _particleMaterial;
        private Material _ringMaterial;
        private bool _subscribed;
        private float _resolveAfter;

        private static readonly Color SwordColor = new Color(0.18f, 0.56f, 1f);
        private static readonly Color ShieldColor = new Color(0.12f, 1f, 0.48f);
        private static readonly Color PerfectColor = new Color(0.62f, 0.34f, 1f);
        private static readonly Color EnemyColor = new Color(1f, 0.12f, 0.26f);
        private static readonly Color HeavyColor = new Color(1f, 0.42f, 0.10f);

        private void OnEnable()
        {
            Resolve();
            Subscribe();
        }

        private void OnDisable() => Unsubscribe();

        private void Update()
        {
            if (_subscribed || Time.unscaledTime < _resolveAfter) return;
            _resolveAfter = Time.unscaledTime + 0.20f;
            Resolve();
            Subscribe();
        }

        private void Resolve()
        {
            if (physical == null) physical = FindObjectOfType<GuardianSwordShieldController>(true);
            if (input == null) input = FindObjectOfType<GuardianCombatInput>(true);
            if (bossDirector == null) bossDirector = FindObjectOfType<FracturedSignalDirector>(true);
            if (presentation == null) presentation = FindObjectOfType<CombatPresentationDirector>(true);

            CombatantVitals[] all = FindObjectsOfType<CombatantVitals>(true);
            foreach (CombatantVitals candidate in all)
            {
                if (candidate == null) continue;
                if (candidate.Team == CombatTeam.Guardian && playerVitals == null) playerVitals = candidate;
                if (candidate.Team == CombatTeam.Enemy && candidate.GetComponent<FracturedSignalDirector>() != null)
                    bossVitals = candidate;
            }
        }

        private void Subscribe()
        {
            if (physical == null || bossDirector == null || playerVitals == null || bossVitals == null) return;
            Unsubscribe();

            physical.SwordHit += OnSwordHit;
            physical.ShieldBlocked += OnShieldBlock;
            physical.PerfectGuard += OnPerfectGuard;
            physical.GuardBroken += OnGuardBroken;
            playerVitals.Damaged += OnPlayerDamaged;
            bossVitals.Damaged += OnBossDamaged;
            bossDirector.PhaseChanged += OnPhaseChanged;
            bossDirector.AttackFired += OnBossAttackFired;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (physical != null)
            {
                physical.SwordHit -= OnSwordHit;
                physical.ShieldBlocked -= OnShieldBlock;
                physical.PerfectGuard -= OnPerfectGuard;
                physical.GuardBroken -= OnGuardBroken;
            }
            if (playerVitals != null) playerVitals.Damaged -= OnPlayerDamaged;
            if (bossVitals != null) bossVitals.Damaged -= OnBossDamaged;
            if (bossDirector != null)
            {
                bossDirector.PhaseChanged -= OnPhaseChanged;
                bossDirector.AttackFired -= OnBossAttackFired;
            }
            _subscribed = false;
        }

        private void OnSwordHit(float damage, float neuralBonus)
        {
            Vector3 direction = input != null ? input.CurrentAimDirection : Vector3.forward;
            Vector3 position = bossVitals != null && bossVitals.IsAlive
                ? bossVitals.transform.position + Vector3.up * 0.55f
                : physical.transform.position + direction * 1.7f + Vector3.up * 0.55f;
            Color color = neuralBonus > 0.001f ? Color.Lerp(SwordColor, Color.white, 0.28f) : SwordColor;
            SpawnBurst(position, color, neuralBonus > 0f ? 28 : 19, neuralBonus > 0f ? 4.8f : 3.6f, 0.12f);
            SpawnRing(position, Vector3.up, color, 0.35f, neuralBonus > 0f ? 1.75f : 1.15f, 0.28f, 0.050f);
            presentation?.CleaveImpact(direction);
        }

        private void OnShieldBlock(float incoming, float chip)
        {
            Vector3 direction = input != null ? input.CurrentAimDirection : physical.transform.forward;
            Vector3 position = physical.transform.position + Vector3.up * 0.58f + direction.normalized * 0.70f;
            SpawnBurst(position, ShieldColor, 15, 2.8f, 0.08f);
            SpawnRing(position, direction, ShieldColor, 0.28f, 1.05f, 0.23f, 0.045f);
            presentation?.CounterImpact(-direction);
        }

        private void OnPerfectGuard()
        {
            Vector3 direction = input != null ? input.CurrentAimDirection : physical.transform.forward;
            Vector3 position = physical.transform.position + Vector3.up * 0.62f + direction.normalized * 0.72f;
            SpawnBurst(position, PerfectColor, 42, 6.2f, 0.10f);
            SpawnRing(position, direction, PerfectColor, 0.30f, 2.45f, 0.42f, 0.085f);
            SpawnRing(position, Vector3.up, ShieldColor, 0.25f, 1.75f, 0.34f, 0.055f);
            presentation?.CounterImpact(-direction * 1.4f);
        }

        private void OnGuardBroken()
        {
            Vector3 position = physical.transform.position + Vector3.up * 0.55f;
            SpawnBurst(position, EnemyColor, 34, 5.0f, 0.11f);
            SpawnRing(position, Vector3.up, EnemyColor, 0.40f, 1.55f, 0.36f, 0.075f);
        }

        private void OnPlayerDamaged(DamagePacket packet)
        {
            if (packet.Damage <= 0f) return;
            Color color = packet.Heavy ? HeavyColor : EnemyColor;
            Vector3 position = packet.Point == Vector3.zero
                ? playerVitals.transform.position + Vector3.up * 0.55f
                : packet.Point;
            SpawnBurst(position, color, packet.Heavy ? 28 : 14, packet.Heavy ? 5.4f : 3.2f, packet.Heavy ? 0.12f : 0.075f);
        }

        private void OnBossDamaged(DamagePacket packet)
        {
            if (packet.Damage <= 0f) return;
            Vector3 position = packet.Point == Vector3.zero
                ? bossVitals.transform.position + Vector3.up * 0.6f
                : packet.Point;
            Color color = packet.NeuralBonusDamage > 0.001f ? SwordColor : new Color(0.92f, 0.80f, 1f);
            SpawnBurst(position, color, packet.Heavy ? 24 : 10, packet.Heavy ? 4.5f : 2.8f, packet.Heavy ? 0.11f : 0.065f);
        }

        private void OnPhaseChanged(int phase)
        {
            Vector3 position = bossDirector.transform.position + Vector3.up * 0.35f;
            Color color = phase >= 3 ? HeavyColor : PerfectColor;
            SpawnRing(position, Vector3.up, color, 0.8f, 6.2f, 0.95f, 0.12f);
            SpawnBurst(position, color, 48, 5.8f, 0.14f);
        }

        private void OnBossAttackFired(string pattern, int count, bool heavy)
        {
            if (!heavy) return;
            Vector3 position = bossDirector.transform.position + Vector3.up * 0.42f;
            SpawnRing(position, Vector3.up, HeavyColor, 0.55f, 2.7f, 0.34f, 0.075f);
        }

        private void SpawnBurst(Vector3 position, Color color, int count, float speed, float size)
        {
            GameObject go = new GameObject("MindforgeImpactBurst");
            go.transform.position = position;
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = ps.main;
            main.duration = 0.35f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.16f, 0.38f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.55f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.55f, size * 1.25f);
            main.startColor = new ParticleSystem.MinMaxGradient(color, Color.Lerp(color, Color.white, 0.35f));
            main.maxParticles = 80;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.Destroy;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)Mathf.Clamp(count, 1, 72)) });

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.14f;

            ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = ParticleMaterial();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            ps.Play();
        }

        private void SpawnRing(Vector3 position, Vector3 normal, Color color, float startRadius, float endRadius, float lifetime, float width)
        {
            GameObject go = new GameObject("MindforgeImpactRing");
            go.transform.position = position;
            Vector3 n = normal.sqrMagnitude > 0.01f ? normal.normalized : Vector3.up;
            go.transform.rotation = Quaternion.FromToRotation(Vector3.up, n);
            LineRenderer line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = RingMaterial();
            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = 48;
            line.widthMultiplier = width;
            ShowcaseTransientRing ring = go.AddComponent<ShowcaseTransientRing>();
            ring.Configure(line, color, startRadius, endRadius, lifetime);
        }

        private Material ParticleMaterial()
        {
            if (_particleMaterial != null) return _particleMaterial;
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ??
                            Shader.Find("Particles/Standard Unlit") ??
                            Shader.Find("Sprites/Default");
            _particleMaterial = new Material(shader) { name = "MindforgeImpactParticleMaterial" };
            return _particleMaterial;
        }

        private Material RingMaterial()
        {
            if (_ringMaterial != null) return _ringMaterial;
            Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit");
            _ringMaterial = new Material(shader) { name = "MindforgeImpactRingMaterial" };
            return _ringMaterial;
        }

        private void OnDestroy()
        {
            if (_particleMaterial != null) Destroy(_particleMaterial);
            if (_ringMaterial != null) Destroy(_ringMaterial);
        }
    }

    public sealed class ShowcaseTransientRing : MonoBehaviour
    {
        private LineRenderer _line;
        private Color _color;
        private float _startRadius;
        private float _endRadius;
        private float _lifetime;
        private float _age;
        private float _baseWidth;

        public void Configure(LineRenderer line, Color color, float startRadius, float endRadius, float lifetime)
        {
            _line = line;
            _color = color;
            _startRadius = Mathf.Max(0.01f, startRadius);
            _endRadius = Mathf.Max(_startRadius, endRadius);
            _lifetime = Mathf.Max(0.05f, lifetime);
            _baseWidth = line != null ? line.widthMultiplier : 0.05f;
            Draw(_startRadius, 1f);
        }

        private void Update()
        {
            _age += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(_age / _lifetime);
            float eased = 1f - Mathf.Pow(1f - t, 2f);
            Draw(Mathf.Lerp(_startRadius, _endRadius, eased), 1f - t);
            if (_age >= _lifetime) Destroy(gameObject);
        }

        private void Draw(float radius, float alpha)
        {
            if (_line == null) return;
            for (int i = 0; i < _line.positionCount; i++)
            {
                float a = i / (float)_line.positionCount * Mathf.PI * 2f;
                _line.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius));
            }
            Color faded = new Color(_color.r, _color.g, _color.b, Mathf.Clamp01(alpha));
            _line.startColor = faded;
            _line.endColor = faded;
            _line.widthMultiplier = _baseWidth * Mathf.Lerp(1f, 0.20f, 1f - alpha);
        }
    }
}
