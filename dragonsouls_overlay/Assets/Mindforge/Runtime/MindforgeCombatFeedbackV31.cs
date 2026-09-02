using Combat;
using States;
using UnityEngine;

namespace Mindforge.Chassis
{
    /// <summary>
    /// Visual-only hit punctuation layered on the existing Health events. Damage,
    /// invulnerability, knockback, animation and death remain Dragon Souls authority.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MindforgeCombatFeedbackV31 : MonoBehaviour
    {
        [SerializeField] private float flashDuration = 0.075f;
        [SerializeField] private int baseSparkCount = 7;
        [SerializeField] private int deathSparkCount = 22;

        private Health _health;
        private Renderer[] _renderers;
        private MaterialPropertyBlock[] _baselineBlocks;
        private ParticleSystem _sparks;
        private Material _sparkMaterial;
        private bool _isPlayer;
        private bool _flashActive;
        private float _flashUntil;

        public bool Installed { get; private set; }
        public int HitEventsObserved { get; private set; }

        private void Start()
        {
            _health = GetComponent<Health>();
            if (_health == null) _health = GetComponentInChildren<Health>(true);
            if (_health == null)
            {
                enabled = false;
                return;
            }

            _isPlayer = GetComponent<PlayerStateMachine>() != null;
            _renderers = GetComponentsInChildren<Renderer>(true);
            _baselineBlocks = new MaterialPropertyBlock[_renderers.Length];
            for (int i = 0; i < _renderers.Length; i++)
            {
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                _renderers[i].GetPropertyBlock(block);
                _baselineBlocks[i] = block;
            }

            BuildSparkSystem();
            _health.OnHealthUpdated += HandleHealthUpdated;
            _health.OnDead += HandleDead;
            Installed = true;
        }

        private void OnDestroy()
        {
            if (_health != null)
            {
                _health.OnHealthUpdated -= HandleHealthUpdated;
                _health.OnDead -= HandleDead;
            }
            if (_sparkMaterial != null) Destroy(_sparkMaterial);
        }

        private void LateUpdate()
        {
            if (_flashActive && Time.unscaledTime >= _flashUntil)
            {
                RestoreRendererBlocks();
                _flashActive = false;
            }
        }

        private void HandleHealthUpdated(int remainingHealth, int damage)
        {
            if (damage <= 0) return;
            HitEventsObserved++;
            Color flash = _isPlayer
                ? new Color(1.0f, 0.24f, 0.52f, 1f)
                : new Color(0.56f, 0.94f, 1.0f, 1f);
            ApplyFlash(flash);
            EmitSparks(ResolveHitPoint(), Mathf.Clamp(baseSparkCount + damage / 8, baseSparkCount, 16), flash);
        }

        private void HandleDead()
        {
            Color burst = _isPlayer
                ? new Color(0.95f, 0.20f, 0.48f, 1f)
                : new Color(0.38f, 0.78f, 1.0f, 1f);
            EmitSparks(ResolveHitPoint(), deathSparkCount, burst);
        }

        private void ApplyFlash(Color color)
        {
            _flashUntil = Time.unscaledTime + flashDuration;
            _flashActive = true;

            for (int i = 0; i < _renderers.Length; i++)
            {
                Renderer renderer = _renderers[i];
                if (renderer == null || !renderer.enabled) continue;
                Material material = renderer.sharedMaterial;
                if (material == null) continue;

                MaterialPropertyBlock block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                if (material.HasProperty("_EmissionColor"))
                    block.SetColor("_EmissionColor", color * 2.4f);
                else if (material.HasProperty("_BaseColor"))
                    block.SetColor("_BaseColor", color);
                else if (material.HasProperty("_Color"))
                    block.SetColor("_Color", color);
                else
                    continue;
                renderer.SetPropertyBlock(block);
            }
        }

        private void RestoreRendererBlocks()
        {
            if (_renderers == null || _baselineBlocks == null) return;
            int count = Mathf.Min(_renderers.Length, _baselineBlocks.Length);
            for (int i = 0; i < count; i++)
            {
                if (_renderers[i] != null)
                    _renderers[i].SetPropertyBlock(_baselineBlocks[i]);
            }
        }

        private Vector3 ResolveHitPoint()
        {
            if (_health != null)
            {
                Vector3 point = _health.EnterHitPosition;
                if (point.sqrMagnitude > 0.001f && (point - transform.position).sqrMagnitude < 625f)
                    return point;
            }

            if (_renderers != null)
            {
                for (int i = 0; i < _renderers.Length; i++)
                    if (_renderers[i] != null && _renderers[i].enabled)
                        return _renderers[i].bounds.center;
            }
            return transform.position + Vector3.up * 1.0f;
        }

        private void BuildSparkSystem()
        {
            GameObject go = new GameObject("Mindforge_HitSparks_V31");
            go.transform.SetParent(transform, false);
            _sparks = go.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = _sparks.main;
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.12f, 0.28f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.3f, 5.6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.075f);
            main.maxParticles = 64;
            main.gravityModifier = 0.12f;

            ParticleSystem.EmissionModule emission = _sparks.emission;
            emission.enabled = false;
            ParticleSystem.ShapeModule shape = _sparks.shape;
            shape.enabled = false;

            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                _sparkMaterial = new Material(shader);
                _sparkMaterial.name = "Mindforge_V31_Runtime_HitSpark";
                ParticleSystemRenderer particleRenderer = _sparks.GetComponent<ParticleSystemRenderer>();
                particleRenderer.material = _sparkMaterial;
                particleRenderer.renderMode = ParticleSystemRenderMode.Stretch;
                particleRenderer.velocityScale = 0.12f;
                particleRenderer.lengthScale = 2.1f;
            }
        }

        private void EmitSparks(Vector3 worldPosition, int count, Color color)
        {
            if (_sparks == null) return;
            ParticleSystem.EmitParams emit = new ParticleSystem.EmitParams();
            emit.position = worldPosition;
            emit.applyShapeToPosition = false;
            emit.startColor = color;
            for (int i = 0; i < count; i++)
            {
                Vector3 direction = Random.onUnitSphere;
                direction.y = Mathf.Abs(direction.y) * 0.65f;
                emit.velocity = direction * Random.Range(2.2f, 5.3f);
                emit.startSize = Random.Range(0.025f, 0.075f);
                emit.startLifetime = Random.Range(0.12f, 0.27f);
                _sparks.Emit(emit, 1);
            }
        }
    }
}
