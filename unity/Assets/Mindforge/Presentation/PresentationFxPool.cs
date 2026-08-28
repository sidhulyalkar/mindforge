using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mindforge.Presentation
{
    /// <summary>
    /// Bounded pool for short-lived presentation effects.
    ///
    /// Combat authority never depends on whether an effect is available. If the pool is
    /// saturated the presentation layer drops the optional effect instead of allocating
    /// another GameObject during a combat spike.
    /// </summary>
    public sealed class PresentationFxPool : MonoBehaviour
    {
        [SerializeField] private int burstPrewarm = 10;
        [SerializeField] private int ringPrewarm = 8;
        [SerializeField] private int maximumBursts = 24;
        [SerializeField] private int maximumRings = 18;

        private readonly Stack<PooledParticleBurst> _availableBursts = new Stack<PooledParticleBurst>(24);
        private readonly Stack<PooledTransientRing> _availableRings = new Stack<PooledTransientRing>(18);
        private int _createdBursts;
        private int _createdRings;
        private Material _particleMaterial;
        private Material _ringMaterial;

        public static PresentationFxPool Instance { get; private set; }

        public static PresentationFxPool GetOrCreate()
        {
            if (Instance != null) return Instance;
            PresentationFxPool existing = FindObjectOfType<PresentationFxPool>(true);
            if (existing != null) return existing;
            return new GameObject("MindforgePresentationFxPool")
                .AddComponent<PresentationFxPool>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            Prewarm();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (_particleMaterial != null) Destroy(_particleMaterial);
            if (_ringMaterial != null) Destroy(_ringMaterial);
        }

        public void EmitBurst(Vector3 position, Color color, int count, float speed, float size)
        {
            int scaledCount = Mathf.Clamp(
                Mathf.RoundToInt(count * PresentationQualityGovernor.FxDensity),
                1,
                96);
            PooledParticleBurst burst = AcquireBurst();
            if (burst == null) return;
            burst.Play(position, color, scaledCount, speed, size);
        }

        public void EmitRing(
            Vector3 position,
            Vector3 normal,
            Color color,
            float startRadius,
            float endRadius,
            float lifetime,
            float width)
        {
            PooledTransientRing ring = AcquireRing();
            if (ring == null) return;
            ring.Play(
                position,
                normal,
                color,
                startRadius,
                endRadius,
                lifetime,
                width,
                PresentationQualityGovernor.PreferredRingSegments);
        }

        internal void Release(PooledParticleBurst burst)
        {
            if (burst == null) return;
            burst.Deactivate();
            _availableBursts.Push(burst);
        }

        internal void Release(PooledTransientRing ring)
        {
            if (ring == null) return;
            ring.Deactivate();
            _availableRings.Push(ring);
        }

        private void Prewarm()
        {
            int bursts = Mathf.Clamp(burstPrewarm, 0, Mathf.Max(1, maximumBursts));
            int rings = Mathf.Clamp(ringPrewarm, 0, Mathf.Max(1, maximumRings));
            for (int i = 0; i < bursts; i++)
            {
                PooledParticleBurst burst = CreateBurst();
                if (burst != null) _availableBursts.Push(burst);
            }
            for (int i = 0; i < rings; i++)
            {
                PooledTransientRing ring = CreateRing();
                if (ring != null) _availableRings.Push(ring);
            }
        }

        private PooledParticleBurst AcquireBurst()
        {
            if (_availableBursts.Count > 0) return _availableBursts.Pop();
            if (_createdBursts >= Mathf.Max(1, maximumBursts)) return null;
            return CreateBurst();
        }

        private PooledTransientRing AcquireRing()
        {
            if (_availableRings.Count > 0) return _availableRings.Pop();
            if (_createdRings >= Mathf.Max(1, maximumRings)) return null;
            return CreateRing();
        }

        private PooledParticleBurst CreateBurst()
        {
            GameObject go = new GameObject("MindforgeImpactBurst_Pooled");
            go.transform.SetParent(transform, false);
            go.SetActive(false);

            ParticleSystem particleSystem = go.AddComponent<ParticleSystem>();
            ParticleSystemRenderer particleRenderer = go.GetComponent<ParticleSystemRenderer>();
            PooledParticleBurst burst = go.AddComponent<PooledParticleBurst>();
            burst.Initialize(this, particleSystem, particleRenderer, ParticleMaterial());
            _createdBursts++;
            return burst;
        }

        private PooledTransientRing CreateRing()
        {
            GameObject go = new GameObject("MindforgeImpactRing_Pooled");
            go.transform.SetParent(transform, false);
            go.SetActive(false);

            LineRenderer line = go.AddComponent<LineRenderer>();
            PooledTransientRing ring = go.AddComponent<PooledTransientRing>();
            ring.Initialize(this, line, RingMaterial());
            _createdRings++;
            return ring;
        }

        private Material ParticleMaterial()
        {
            if (_particleMaterial != null) return _particleMaterial;
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ??
                            Shader.Find("Particles/Standard Unlit") ??
                            Shader.Find("Sprites/Default");
            _particleMaterial = new Material(shader) { name = "MindforgeImpactParticleMaterial_Pooled" };
            return _particleMaterial;
        }

        private Material RingMaterial()
        {
            if (_ringMaterial != null) return _ringMaterial;
            Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit");
            _ringMaterial = new Material(shader) { name = "MindforgeImpactRingMaterial_Pooled" };
            return _ringMaterial;
        }
    }

    public sealed class PooledParticleBurst : MonoBehaviour
    {
        private PresentationFxPool _owner;
        private ParticleSystem _particles;
        private bool _leased;

        public void Initialize(
            PresentationFxPool owner,
            ParticleSystem particles,
            ParticleSystemRenderer particleRenderer,
            Material material)
        {
            _owner = owner;
            _particles = particles;

            ParticleSystem.MainModule main = particles.main;
            main.duration = 0.35f;
            main.loop = false;
            main.playOnAwake = false;
            main.maxParticles = 96;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.useUnscaledTime = true;
            main.stopAction = ParticleSystemStopAction.Callback;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.enabled = false;

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.14f;

            particleRenderer.sharedMaterial = material;
            particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            particleRenderer.shadowCastingMode = ShadowCastingMode.Off;
            particleRenderer.receiveShadows = false;
        }

        public void Play(Vector3 position, Color color, int count, float speed, float size)
        {
            _leased = true;
            transform.position = position;
            gameObject.SetActive(true);
            _particles.Clear(true);

            ParticleSystem.MainModule main = _particles.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.16f, 0.38f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.55f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.55f, size * 1.25f);
            main.startColor = new ParticleSystem.MinMaxGradient(color, Color.Lerp(color, Color.white, 0.35f));

            _particles.Emit(Mathf.Clamp(count, 1, 96));
            _particles.Play(false);
        }

        private void OnParticleSystemStopped()
        {
            if (!_leased || _owner == null) return;
            _leased = false;
            _owner.Release(this);
        }

        internal void Deactivate()
        {
            _leased = false;
            if (_particles != null)
                _particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            gameObject.SetActive(false);
        }
    }

    public sealed class PooledTransientRing : MonoBehaviour
    {
        private PresentationFxPool _owner;
        private LineRenderer _line;
        private Color _color;
        private float _startRadius;
        private float _endRadius;
        private float _lifetime;
        private float _age;
        private float _baseWidth;
        private bool _leased;

        public void Initialize(PresentationFxPool owner, LineRenderer line, Material material)
        {
            _owner = owner;
            _line = line;
            line.sharedMaterial = material;
            line.useWorldSpace = false;
            line.loop = true;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.textureMode = LineTextureMode.Stretch;
        }

        public void Play(
            Vector3 position,
            Vector3 normal,
            Color color,
            float startRadius,
            float endRadius,
            float lifetime,
            float width,
            int segments)
        {
            _leased = true;
            _age = 0f;
            _color = color;
            _startRadius = Mathf.Max(0.01f, startRadius);
            _endRadius = Mathf.Max(_startRadius, endRadius);
            _lifetime = Mathf.Max(0.05f, lifetime);
            _baseWidth = Mathf.Max(0.002f, width);

            transform.position = position;
            Vector3 n = normal.sqrMagnitude > 0.01f ? normal.normalized : Vector3.up;
            transform.rotation = Quaternion.FromToRotation(Vector3.up, n);
            gameObject.SetActive(true);

            _line.positionCount = Mathf.Clamp(segments, 16, 64);
            Draw(_startRadius, 1f);
        }

        private void Update()
        {
            if (!_leased) return;
            _age += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(_age / _lifetime);
            float eased = 1f - Mathf.Pow(1f - t, 2f);
            Draw(Mathf.Lerp(_startRadius, _endRadius, eased), 1f - t);
            if (_age >= _lifetime && _owner != null)
            {
                _leased = false;
                _owner.Release(this);
            }
        }

        private void Draw(float radius, float alpha)
        {
            if (_line == null) return;
            int count = _line.positionCount;
            for (int i = 0; i < count; i++)
            {
                float angle = i / (float)count * Mathf.PI * 2f;
                _line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
            }

            Color faded = new Color(_color.r, _color.g, _color.b, Mathf.Clamp01(alpha));
            _line.startColor = faded;
            _line.endColor = faded;
            _line.widthMultiplier = _baseWidth * Mathf.Lerp(0.20f, 1f, alpha);
        }

        internal void Deactivate()
        {
            _leased = false;
            gameObject.SetActive(false);
        }
    }
}
