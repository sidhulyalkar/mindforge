using System;
using UnityEngine;

namespace Mindforge.Combat
{
    /// <summary>
    /// Fractured Echo gameplay authority shared by boss phases and the Null Ward tutorial.
    /// Orbit position and firing cadence are fixed-tick so replay semantics do not depend
    /// on rendered frame rate. Cosmetic spin remains presentation-only.
    /// </summary>
    public sealed class FracturedEchoNode : MonoBehaviour
    {
        [SerializeField] private CombatantVitals vitals;
        [SerializeField] private MindforgeProjectile projectilePrefab;
        [SerializeField] private Transform projectileOrigin;
        [SerializeField] private float orbitRadius = 4.4f;
        [SerializeField] private float orbitAngularSpeed = 0.58f;
        [SerializeField] private float fireInterval = 1.6f;
        [SerializeField] private float projectileSpeed = 11.5f;
        [SerializeField] private float projectileDamage = 8f;
        [SerializeField] private float fluxReward = 0.35f;
        [SerializeField] private bool destroyOnShatter = true;

        private Transform _anchor;
        private Transform _player;
        private FluxMeter _playerFlux;
        private float _phase;
        private float _initialPhase;
        private long _nextFireTick;
        private bool _externalPaused;
        private bool _shattered;
        private Collider[] _colliders = Array.Empty<Collider>();
        private bool[] _colliderDefaults = Array.Empty<bool>();
        private Renderer[] _renderers = Array.Empty<Renderer>();
        private bool[] _rendererDefaults = Array.Empty<bool>();

        public event Action Shattered;
        public event Action Reconstructed;
        public CombatantVitals Vitals => vitals;
        public bool CheckpointResettable => !destroyOnShatter;

        private long FixedTick
        {
            get
            {
                float dt = Mathf.Max(0.0001f, Time.fixedDeltaTime);
                return (long)Math.Round(Time.fixedTime / dt);
            }
        }

        private void Awake()
        {
            if (vitals == null) vitals = GetComponent<CombatantVitals>();
            CapturePresentationDefaults();
        }

        public void Initialize(Transform anchor, Transform player, FluxMeter playerFlux, float phase)
        {
            _anchor = anchor;
            _player = player;
            _playerFlux = playerFlux;
            _phase = phase;
            _initialPhase = phase;
            _nextFireTick = FixedTick + Mathf.Max(1, Mathf.RoundToInt(SecondsToTicks(fireInterval) * 0.65f));
        }

        public void ConfigureWorldEcho(
            Transform anchor,
            Transform player,
            FluxMeter playerFlux,
            float phase,
            float worldOrbitRadius = 1.15f)
        {
            orbitRadius = Mathf.Max(0f, worldOrbitRadius);
            destroyOnShatter = false;
            Initialize(anchor, player, playerFlux, phase);
            CapturePresentationDefaults(true);
        }

        public void SetExternalPause(bool paused)
        {
            _externalPaused = paused;
            if (!paused)
                _nextFireTick = Math.Max(_nextFireTick, FixedTick + SecondsToTicks(0.35f));
        }

        public void ResetForCheckpoint()
        {
            if (destroyOnShatter || vitals == null) return;
            _shattered = false;
            _externalPaused = false;
            _phase = _initialPhase;
            vitals.ResetForCheckpoint(true);
            RestorePresentationDefaults();
            PositionFromPhase();
            _nextFireTick = FixedTick + Mathf.Max(1, Mathf.RoundToInt(SecondsToTicks(fireInterval) * 0.65f));
            Physics.SyncTransforms();
            Reconstructed?.Invoke();
        }

        private void OnEnable()
        {
            if (vitals == null) vitals = GetComponent<CombatantVitals>();
            if (vitals != null)
            {
                vitals.Died -= OnDied;
                vitals.Died += OnDied;
            }
            CapturePresentationDefaults();
        }

        private void OnDisable()
        {
            if (vitals != null) vitals.Died -= OnDied;
        }

        private void FixedUpdate()
        {
            if (_anchor == null || vitals == null || !vitals.IsAlive || _shattered) return;
            _phase += orbitAngularSpeed * Time.fixedDeltaTime;
            PositionFromPhase();

            if (_externalPaused || (vitals.Poise != null && vitals.Poise.Broken)) return;
            if (_player == null || projectilePrefab == null || FixedTick < _nextFireTick) return;
            _nextFireTick = FixedTick + SecondsToTicks(fireInterval);
            Vector3 origin = projectileOrigin != null ? projectileOrigin.position : transform.position;
            Vector3 direction = (_player.position - origin).normalized;
            MindforgeProjectile p = Instantiate(projectilePrefab, origin, Quaternion.LookRotation(direction));
            p.Configure(CombatTeam.Enemy, direction * projectileSpeed, projectileDamage, 0f);
        }

        private void Update()
        {
            // Cosmetic only. Gameplay position and firing live in FixedUpdate above.
            if (!_shattered) transform.Rotate(Vector3.up, 70f * Time.deltaTime, Space.World);
        }

        private void PositionFromPhase()
        {
            if (_anchor == null) return;
            Vector3 center = _anchor.position;
            Vector3 offset = new Vector3(
                Mathf.Cos(_phase) * orbitRadius,
                0.35f + Mathf.Sin(_phase * 1.7f) * 0.18f,
                Mathf.Sin(_phase) * orbitRadius);
            transform.position = center + offset;
        }

        private void OnDied()
        {
            if (_shattered) return;
            _shattered = true;
            Shattered?.Invoke();
            _playerFlux?.Award(fluxReward, "Echo Shatter");

            if (destroyOnShatter)
            {
                Destroy(gameObject, 0.05f);
                return;
            }

            SetPresentationEnabled(false);
        }

        private void CapturePresentationDefaults(bool force = false)
        {
            if (!force && _colliders.Length > 0) return;
            _colliders = GetComponentsInChildren<Collider>(true);
            _colliderDefaults = new bool[_colliders.Length];
            for (int i = 0; i < _colliders.Length; i++)
                _colliderDefaults[i] = _colliders[i] != null && _colliders[i].enabled;

            _renderers = GetComponentsInChildren<Renderer>(true);
            _rendererDefaults = new bool[_renderers.Length];
            for (int i = 0; i < _renderers.Length; i++)
                _rendererDefaults[i] = _renderers[i] != null && _renderers[i].enabled;
        }

        private void SetPresentationEnabled(bool enabled)
        {
            for (int i = 0; i < _colliders.Length; i++)
                if (_colliders[i] != null) _colliders[i].enabled = enabled;
            for (int i = 0; i < _renderers.Length; i++)
                if (_renderers[i] != null) _renderers[i].enabled = enabled;
        }

        private void RestorePresentationDefaults()
        {
            for (int i = 0; i < _colliders.Length; i++)
                if (_colliders[i] != null) _colliders[i].enabled = i < _colliderDefaults.Length && _colliderDefaults[i];
            for (int i = 0; i < _renderers.Length; i++)
                if (_renderers[i] != null) _renderers[i].enabled = i < _rendererDefaults.Length && _rendererDefaults[i];
        }

        private static int SecondsToTicks(float seconds)
        {
            float dt = Mathf.Max(0.0001f, Time.fixedDeltaTime);
            return Mathf.Max(1, Mathf.CeilToInt(Mathf.Max(0f, seconds) / dt));
        }
    }
}
