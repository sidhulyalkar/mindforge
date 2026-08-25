using UnityEngine;

namespace Mindforge.Combat
{
    /// <summary>
    /// Phase-II/III pressure node. Echoes widen physical positioning demands while
    /// the Wisp remains in the central action-gaze corridor. Destroying one rewards
    /// Flux, turning attention split into a strategic resource decision.
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

        private Transform _boss;
        private Transform _player;
        private FluxMeter _playerFlux;
        private float _phase;
        private float _nextFire;

        public void Initialize(Transform boss, Transform player, FluxMeter playerFlux, float phase)
        {
            _boss = boss;
            _player = player;
            _playerFlux = playerFlux;
            _phase = phase;
            _nextFire = Time.time + fireInterval * 0.65f;
        }

        private void OnEnable()
        {
            if (vitals != null) vitals.Died += OnDied;
        }

        private void OnDisable()
        {
            if (vitals != null) vitals.Died -= OnDied;
        }

        private void Update()
        {
            if (_boss == null || vitals == null || !vitals.IsAlive) return;

            _phase += orbitAngularSpeed * Time.deltaTime;
            Vector3 center = _boss.position;
            Vector3 offset = new Vector3(Mathf.Cos(_phase), 0.35f + Mathf.Sin(_phase * 1.7f) * 0.18f, Mathf.Sin(_phase)) * orbitRadius;
            transform.position = center + offset;
            transform.Rotate(Vector3.up, 70f * Time.deltaTime, Space.World);

            if (vitals.Poise != null && vitals.Poise.Broken) return;
            if (_player == null || projectilePrefab == null || Time.time < _nextFire) return;

            _nextFire = Time.time + fireInterval;
            Vector3 origin = projectileOrigin != null ? projectileOrigin.position : transform.position;
            Vector3 direction = (_player.position - origin).normalized;
            MindforgeProjectile p = Instantiate(projectilePrefab, origin, Quaternion.LookRotation(direction));
            p.Configure(CombatTeam.Enemy, direction * projectileSpeed, projectileDamage, 0f);
        }

        private void OnDied()
        {
            _playerFlux?.Award(fluxReward, "Echo Shatter");
            Destroy(gameObject, 0.05f);
        }
    }
}
