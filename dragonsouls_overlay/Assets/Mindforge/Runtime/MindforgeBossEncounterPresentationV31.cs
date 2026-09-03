using Combat;
using States;
using UnityEngine;

namespace Mindforge.Chassis
{
    /// <summary>
    /// Phase-aware presentation layered on the existing Nightmare Dragon boss.
    /// Health events and authored particle playback drive visual escalation only;
    /// behavior tree, animation events, projectiles, damage and movement stay upstream.
    /// </summary>
    [DefaultExecutionOrder(810)]
    [DisallowMultipleComponent]
    public sealed class MindforgeBossEncounterPresentationV31 : MonoBehaviour
    {
        [SerializeField] private Color neuralColor = new Color(0.22f, 0.82f, 1.00f, 1f);
        [SerializeField] private Color corruptionColor = new Color(0.88f, 0.16f, 0.64f, 1f);
        [SerializeField] private float activeDistance = 42f;
        [SerializeField] private float phaseOneIntensity = 0.55f;
        [SerializeField] private float phaseTwoIntensity = 0.95f;
        [SerializeField] private float phaseThreeIntensity = 1.45f;

        private Health _health;
        private PlayerStateMachine _player;
        private Renderer[] _renderers;
        private ParticleSystem[] _particles;
        private Light _signalCore;
        private float _healthFraction = 1f;
        private float _damagePulse;

        public bool Installed { get; private set; }
        public int Phase { get; private set; } = 1;
        public int AuthoredParticlesRethemed { get; private set; }

        private void Start()
        {
            _health = GetComponent<Health>();
            if (_health == null) _health = GetComponentInParent<Health>();
            if (_health == null) _health = GetComponentInChildren<Health>(true);
            _player = FindObjectOfType<PlayerStateMachine>();
            _renderers = GetComponentsInChildren<Renderer>(true);
            _particles = GetComponentsInChildren<ParticleSystem>(true);

            if (_health != null)
            {
                _health.OnHealthUpdated += HandleHealthUpdated;
                _health.OnDead += HandleDead;
            }

            RethemeAuthoredParticles();
            BuildSignalCore();
            Installed = _renderers != null && _renderers.Length > 0;
        }

        private void OnDestroy()
        {
            if (_health != null)
            {
                _health.OnHealthUpdated -= HandleHealthUpdated;
                _health.OnDead -= HandleDead;
            }
        }

        private void LateUpdate()
        {
            if (!Installed) return;
            if (_damagePulse > 0f)
                _damagePulse = Mathf.MoveTowards(_damagePulse, 0f, Time.unscaledDeltaTime * 3.6f);

            float proximity = 1f;
            if (_player != null)
            {
                Vector3 delta = _player.transform.position - transform.position;
                delta.y = 0f;
                proximity = 1f - Mathf.Clamp01(delta.magnitude / activeDistance);
            }

            float phaseIntensity = Phase == 1 ? phaseOneIntensity : Phase == 2 ? phaseTwoIntensity : phaseThreeIntensity;
            float attackActivity = AuthoredAttackActivity();
            float pulse = 0.82f + 0.18f * Mathf.Sin(Time.unscaledTime * (2.1f + Phase * 0.45f));
            float resolved = phaseIntensity * Mathf.Lerp(0.35f, 1f, proximity) * pulse + attackActivity * 0.55f + _damagePulse;

            if (_signalCore != null)
            {
                _signalCore.intensity = resolved;
                _signalCore.range = 8.5f + Phase * 1.6f + attackActivity * 2.2f;
                _signalCore.color = Color.Lerp(neuralColor, corruptionColor, Mathf.Clamp01((1f - _healthFraction) * 1.15f));
            }
            ApplySignalEmission(resolved);
        }

        private void HandleHealthUpdated(int remaining, int damage)
        {
            if (_health == null || _health.maxHealth <= 0) return;
            _healthFraction = Mathf.Clamp01(remaining / (float)_health.maxHealth);
            Phase = _healthFraction > 0.66f ? 1 : _healthFraction > 0.34f ? 2 : 3;
            if (damage > 0) _damagePulse = Mathf.Clamp(0.45f + damage / 80f, 0.45f, 1.25f);
        }

        private void HandleDead()
        {
            _healthFraction = 0f;
            Phase = 3;
            _damagePulse = 1.8f;
        }

        private void RethemeAuthoredParticles()
        {
            if (_particles == null) return;
            int changed = 0;
            for (int i = 0; i < _particles.Length; i++)
            {
                ParticleSystem system = _particles[i];
                if (system == null) continue;
                string n = system.name.ToLowerInvariant();
                if (!ContainsAny(n, "fire", "flame", "breath", "magic", "glow")) continue;

                ParticleSystem.MainModule main = system.main;
                main.startColor = new ParticleSystem.MinMaxGradient(corruptionColor, neuralColor);
                changed++;
            }
            AuthoredParticlesRethemed = changed;
        }

        private void BuildSignalCore()
        {
            Transform existing = transform.Find("Mindforge_Boss_SignalCore_V31");
            if (existing != null)
            {
                _signalCore = existing.GetComponent<Light>();
                return;
            }

            GameObject go = new GameObject("Mindforge_Boss_SignalCore_V31");
            go.transform.SetParent(transform, false);
            Renderer anchor = LargestRenderer();
            if (anchor != null)
            {
                Vector3 world = anchor.bounds.center + Vector3.up * Mathf.Min(1.2f, anchor.bounds.extents.y * 0.25f);
                go.transform.position = world;
            }
            else go.transform.localPosition = new Vector3(0f, 2.2f, 0f);

            _signalCore = go.AddComponent<Light>();
            _signalCore.type = LightType.Point;
            _signalCore.color = neuralColor;
            _signalCore.intensity = phaseOneIntensity;
            _signalCore.range = 10f;
            _signalCore.shadows = LightShadows.None;
            _signalCore.renderMode = LightRenderMode.Auto;
        }

        private float AuthoredAttackActivity()
        {
            if (_particles == null) return 0f;
            for (int i = 0; i < _particles.Length; i++)
            {
                ParticleSystem system = _particles[i];
                if (system == null || !system.isPlaying) continue;
                string n = system.name.ToLowerInvariant();
                if (ContainsAny(n, "fire", "flame", "breath", "magic")) return 1f;
            }
            return 0f;
        }

        private void ApplySignalEmission(float intensity)
        {
            if (_renderers == null) return;
            Color signal = Color.Lerp(neuralColor, corruptionColor, Mathf.Clamp01((1f - _healthFraction) * 1.2f));
            for (int r = 0; r < _renderers.Length; r++)
            {
                Renderer renderer = _renderers[r];
                if (renderer == null || renderer is ParticleSystemRenderer || renderer is TrailRenderer) continue;
                Material[] materials = renderer.sharedMaterials;
                for (int m = 0; m < materials.Length; m++)
                {
                    Material material = materials[m];
                    if (material == null || !material.HasProperty("_EmissionColor")) continue;
                    string semantic = (renderer.name + " " + material.name).ToLowerInvariant();
                    if (!ContainsAny(semantic, "eye", "mouth", "fire", "flame", "magic", "glow", "crystal")) continue;
                    MaterialPropertyBlock block = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(block, m);
                    block.SetColor("_EmissionColor", signal * Mathf.Clamp(intensity, 0.15f, 2.4f));
                    renderer.SetPropertyBlock(block, m);
                }
            }
        }

        private Renderer LargestRenderer()
        {
            Renderer best = null;
            float volume = -1f;
            if (_renderers == null) return null;
            for (int i = 0; i < _renderers.Length; i++)
            {
                Renderer renderer = _renderers[i];
                if (renderer == null || renderer is ParticleSystemRenderer || renderer is TrailRenderer) continue;
                Vector3 size = renderer.bounds.size;
                float candidate = size.x * size.y * size.z;
                if (candidate <= volume) continue;
                volume = candidate;
                best = renderer;
            }
            return best;
        }

        private static bool ContainsAny(string value, params string[] tokens)
        {
            for (int i = 0; i < tokens.Length; i++)
                if (value.Contains(tokens[i])) return true;
            return false;
        }
    }
}
