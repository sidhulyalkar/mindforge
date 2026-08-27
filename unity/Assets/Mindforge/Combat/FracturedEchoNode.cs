using System;
using UnityEngine;

namespace Mindforge.Combat
{
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

        private Transform _boss;
        private Transform _player;
        private FluxMeter _playerFlux;
        private float _phase;
        private float _nextFire;
        private bool _externalPaused;
        private bool _shattered;

        /// <summary>
        /// Semantic lifecycle evidence only. Consumers may observe that the player
        /// destroyed an Echo, but the event never changes combat authority.
        /// </summary>
        public event Action Shattered;

        public void Initialize(Transform boss, Transform player, FluxMeter playerFlux, float phase)
        {
            _boss = boss;
            _player = player;
            _playerFlux = playerFlux;
            _phase = phase;
            _nextFire = Time.time + fireInterval * 0.65f;
        }

        public void SetExternalPause(bool paused)
        {
            _externalPaused = paused;
            if (!paused) _nextFire = Mathf.Max(_nextFire, Time.time + 0.35f);
        }

        private void OnEnable() { if (vitals != null) vitals.Died += OnDied; }
        private void OnDisable() { if (vitals != null) vitals.Died -= OnDied; }

        private void Update()
        {
            if (_boss == null || vitals == null || !vitals.IsAlive) return;
            _phase += orbitAngularSpeed * Time.deltaTime;
            Vector3 center = _boss.position;
            Vector3 offset = new Vector3(Mathf.Cos(_phase), 0.35f + Mathf.Sin(_phase * 1.7f) * 0.18f, Mathf.Sin(_phase)) * orbitRadius;
            transform.position = center + offset;
            transform.Rotate(Vector3.up, 70f * Time.deltaTime, Space.World);

            if (_externalPaused || (vitals.Poise != null && vitals.Poise.Broken)) return;
            if (_player == null || projectilePrefab == null || Time.time < _nextFire) return;
            _nextFire = Time.time + fireInterval;
            Vector3 origin = projectileOrigin != null ? projectileOrigin.position : transform.position;
            Vector3 direction = (_player.position - origin).normalized;
            MindforgeProjectile p = Instantiate(projectilePrefab, origin, Quaternion.LookRotation(direction));
            p.Configure(CombatTeam.Enemy, direction * projectileSpeed, projectileDamage, 0f);
        }

        private void OnDied()
        {
            if (_shattered) return;
            _shattered = true;
            Shattered?.Invoke();
            _playerFlux?.Award(fluxReward, "Echo Shatter");
            Destroy(gameObject, 0.05f);
        }
    }
}
