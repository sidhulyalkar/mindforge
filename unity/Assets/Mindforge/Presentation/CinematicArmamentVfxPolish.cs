using UnityEngine;
using UnityEngine.Rendering;
using Mindforge.Combat;

namespace Mindforge.Presentation
{
    /// <summary>
    /// High-frequency visual polish for the Aetherblade and Verdant Ward. It derives
    /// only from already-authoritative attack/guard state and never changes reach,
    /// collision, damage, stamina or neural evidence.
    /// </summary>
    public sealed class CinematicArmamentVfxPolish : MonoBehaviour
    {
        [SerializeField] private GuardianSwordShieldController combat;

        private TrailRenderer _primaryTrail;
        private TrailRenderer _afterimage;
        private LineRenderer _shieldOutline;
        private ParticleSystem _bladeMotes;
        private ParticleSystem _shieldMotes;
        private Material _trailMaterial;
        private Material _particleMaterial;
        private float _blockPulse;
        private float _perfectPulse;
        private float _breakPulse;

        private void Awake()
        {
            if (combat == null) combat = GetComponent<GuardianSwordShieldController>();
        }

        private void Start()
        {
            BindRig();
            BuildMotes();
            Subscribe();
        }

        private void OnDestroy()
        {
            Unsubscribe();
            if (_trailMaterial != null) Destroy(_trailMaterial);
            if (_particleMaterial != null) Destroy(_particleMaterial);
        }

        private void Subscribe()
        {
            if (combat == null) return;
            combat.ShieldBlocked += OnBlocked;
            combat.PerfectGuard += OnPerfectGuard;
            combat.GuardBroken += OnGuardBroken;
        }

        private void Unsubscribe()
        {
            if (combat == null) return;
            combat.ShieldBlocked -= OnBlocked;
            combat.PerfectGuard -= OnPerfectGuard;
            combat.GuardBroken -= OnGuardBroken;
        }

        private void BindRig()
        {
            Transform arsenal = transform.Find("PhysicalArsenalRig");
            if (arsenal == null) return;
            Transform swordRoot = arsenal.Find("SwordRoot");
            Transform shieldRoot = arsenal.Find("ShieldRoot");
            Transform tip = swordRoot != null ? swordRoot.Find("SwordEnergyTip") : null;
            if (tip != null)
            {
                _primaryTrail = tip.GetComponent<TrailRenderer>();
                if (_primaryTrail != null)
                {
                    _primaryTrail.time = 0.16f;
                    _primaryTrail.minVertexDistance = 0.018f;
                    _primaryTrail.alignment = LineAlignment.TransformZ;
                    _primaryTrail.textureMode = LineTextureMode.Stretch;
                    _primaryTrail.numCornerVertices = 3;
                    _primaryTrail.numCapVertices = 2;
                }
                GameObject after = new GameObject("AetherbladeAfterimage");
                after.transform.SetParent(tip, false);
                _afterimage = after.AddComponent<TrailRenderer>();
                _trailMaterial = BuildTrailMaterial();
                _afterimage.sharedMaterial = _trailMaterial;
                _afterimage.time = 0.32f;
                _afterimage.minVertexDistance = 0.024f;
                _afterimage.alignment = LineAlignment.TransformZ;
                _afterimage.textureMode = LineTextureMode.Stretch;
                _afterimage.widthMultiplier = 0.065f;
                _afterimage.numCornerVertices = 4;
                _afterimage.numCapVertices = 3;
                _afterimage.emitting = false;
                _afterimage.shadowCastingMode = ShadowCastingMode.Off;
                _afterimage.receiveShadows = false;
            }
            if (shieldRoot != null)
            {
                Transform outline = shieldRoot.Find("VerdantWardOutline");
                if (outline != null) _shieldOutline = outline.GetComponent<LineRenderer>();
            }
        }

        private void BuildMotes()
        {
            Transform arsenal = transform.Find("PhysicalArsenalRig");
            if (arsenal == null) return;
            Transform swordRoot = arsenal.Find("SwordRoot");
            Transform shieldRoot = arsenal.Find("ShieldRoot");
            _particleMaterial = BuildParticleMaterial();
            if (swordRoot != null) _bladeMotes = BuildParticleSystem("AetherbladeMotes", swordRoot, new Color(0.18f, 0.58f, 1f));
            if (shieldRoot != null) _shieldMotes = BuildParticleSystem("VerdantWardMotes", shieldRoot, new Color(0.18f, 1f, 0.52f));
        }

        private void Update()
        {
            if (combat == null) return;
            float dt = Mathf.Min(0.05f, Time.unscaledDeltaTime);
            _blockPulse = Mathf.Lerp(_blockPulse, 0f, 1f - Mathf.Exp(-8f * dt));
            _perfectPulse = Mathf.Lerp(_perfectPulse, 0f, 1f - Mathf.Exp(-5.5f * dt));
            _breakPulse = Mathf.Lerp(_breakPulse, 0f, 1f - Mathf.Exp(-4.5f * dt));

            float sight = Mathf.Clamp01(combat.SightResonance);
            float guard = Mathf.Clamp01(combat.GuardResonance);
            bool attacking = combat.IsAttacking;
            bool guarding = combat.IsGuarding;
            int combo = Mathf.Max(1, combat.ComboStep);

            if (_primaryTrail != null)
            {
                _primaryTrail.time = attacking ? (combo >= 3 ? 0.24f : 0.16f) : 0.10f;
                _primaryTrail.widthMultiplier = Mathf.Lerp(0.055f, combo >= 3 ? 0.22f : 0.17f, sight);
            }
            if (_afterimage != null)
            {
                _afterimage.emitting = attacking && combat.AttackProgress > 0.15f && combat.AttackProgress < 0.80f;
                _afterimage.widthMultiplier = Mathf.Lerp(0.035f, combo >= 3 ? 0.12f : 0.085f, sight);
                Color c = Color.Lerp(new Color(0.20f, 0.40f, 0.88f), new Color(0.34f, 0.78f, 1f), sight);
                _afterimage.startColor = new Color(c.r, c.g, c.b, Mathf.Lerp(0.22f, 0.72f, sight));
                _afterimage.endColor = new Color(c.r, c.g, c.b, 0f);
            }

            SetEmission(_bladeMotes, attacking ? Mathf.Lerp(3f, 18f, sight) * (combo >= 3 ? 1.5f : 1f) : 0f);
            SetEmission(_shieldMotes, guarding ? Mathf.Lerp(2f, 14f, guard) + _perfectPulse * 30f : 0f);

            if (_shieldOutline != null)
            {
                float pulse = _blockPulse * 0.035f + _perfectPulse * 0.075f + _breakPulse * 0.055f;
                _shieldOutline.widthMultiplier = Mathf.Lerp(0.028f, 0.050f, guard) + pulse;
                Color baseColor = _breakPulse > 0.15f
                    ? Color.Lerp(new Color(0.18f, 1f, 0.52f), new Color(1f, 0.16f, 0.08f), _breakPulse)
                    : Color.Lerp(new Color(0.12f, 0.45f, 0.28f), new Color(0.28f, 1f, 0.60f), guard + _perfectPulse * 0.35f);
                _shieldOutline.startColor = baseColor;
                _shieldOutline.endColor = baseColor;
            }
        }

        private void OnBlocked(float incoming, float chip)
        {
            _blockPulse = Mathf.Clamp01(0.45f + incoming / 45f);
            EmitBurst(_shieldMotes, 8, 0.16f, 0.36f);
        }

        private void OnPerfectGuard()
        {
            _perfectPulse = 1f;
            EmitBurst(_shieldMotes, 26, 0.24f, 0.52f);
        }

        private void OnGuardBroken()
        {
            _breakPulse = 1f;
            EmitBurst(_shieldMotes, 20, 0.30f, 0.62f);
        }

        private ParticleSystem BuildParticleSystem(string name, Transform parent, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = ps.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 128;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.46f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.018f, 0.055f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.08f, 0.35f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(color.r, color.g, color.b, 0.35f), color);
            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 0f;
            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.12f;
            ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = _particleMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            ps.Play();
            return ps;
        }

        private static void SetEmission(ParticleSystem ps, float rate)
        {
            if (ps == null) return;
            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = Mathf.Max(0f, rate);
        }

        private static void EmitBurst(ParticleSystem ps, int count, float size, float speed)
        {
            if (ps == null) return;
            for (int i = 0; i < count; i++)
            {
                ParticleSystem.EmitParams emit = new ParticleSystem.EmitParams();
                emit.velocity = Random.onUnitSphere * Random.Range(speed * 0.4f, speed);
                emit.startSize = Random.Range(size * 0.35f, size);
                emit.startLifetime = Random.Range(0.16f, 0.48f);
                ps.Emit(emit, 1);
            }
        }

        private static Material BuildTrailMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default") ?? Shader.Find("Standard");
            return shader != null ? new Material(shader) { name = "MindforgeCinematicBladeAfterimage" } : null;
        }

        private static Material BuildParticleMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit");
            return shader != null ? new Material(shader) { name = "MindforgeCinematicArmamentParticles" } : null;
        }
    }
}
