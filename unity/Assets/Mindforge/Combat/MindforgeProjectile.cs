using UnityEngine;

namespace Mindforge.Combat
{
    [RequireComponent(typeof(Rigidbody), typeof(Collider))]
    public sealed class MindforgeProjectile : MonoBehaviour
    {
        [SerializeField] private CombatTeam team = CombatTeam.Enemy;
        [SerializeField] private float damage = 10f;
        [SerializeField] private float poiseDamage = 0f;
        [SerializeField] private int pierce;
        [SerializeField] private float lifetime = 5f;
        [SerializeField] private LayerMask hitMask = ~0;

        private Rigidbody _body;
        private Collider _collider;
        private Vector3 _previousPosition;
        private bool _captured;
        private Transform _captureAnchor;
        private float _capturePhase;

        public CombatTeam Team => team;
        public bool IsHostileToGuardian => team == CombatTeam.Enemy;
        public Rigidbody Body => _body;
        public bool Captured => _captured;

        private void Awake()
        {
            _body = GetComponent<Rigidbody>();
            _collider = GetComponent<Collider>();
            _body.interpolation = RigidbodyInterpolation.Interpolate;
            _body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            _previousPosition = transform.position;
            Destroy(gameObject, lifetime);
        }

        public void Configure(CombatTeam newTeam, Vector3 velocity, float newDamage, float newPoise, int newPierce = 0)
        {
            team = newTeam; damage = newDamage; poiseDamage = newPoise; pierce = newPierce;
            _body.velocity = velocity;
        }

        public void ReflectTowards(Transform target, float speed, float newDamage, float newPoise, int extraPierce = 0)
        {
            if (target == null) return;
            ReleaseFromCapture();
            team = CombatTeam.Guardian;
            damage = newDamage;
            poiseDamage = newPoise;
            pierce = Mathf.Max(pierce, extraPierce);
            Vector3 direction = (target.position - transform.position).normalized;
            _body.velocity = direction * speed;
        }

        public void Capture(Transform anchor, float phase)
        {
            if (_captured) return;
            _captured = true;
            _captureAnchor = anchor;
            _capturePhase = phase;
            _body.isKinematic = true;
            _collider.enabled = false;
        }

        public void ReleaseFromCapture()
        {
            if (!_captured) return;
            _captured = false;
            _captureAnchor = null;
            _body.isKinematic = false;
            _collider.enabled = true;
            _previousPosition = transform.position;
        }

        private void Update()
        {
            if (!_captured || _captureAnchor == null) return;
            _capturePhase += Time.unscaledDeltaTime * 4.5f;
            float radius = 0.65f + 0.16f * Mathf.Sin(_capturePhase * 1.7f);
            transform.position = _captureAnchor.position + new Vector3(Mathf.Cos(_capturePhase), 0.45f, Mathf.Sin(_capturePhase)) * radius;
        }

        private void FixedUpdate()
        {
            if (_captured) return;
            Vector3 current = transform.position;
            Vector3 delta = current - _previousPosition;
            float distance = delta.magnitude;
            if (distance > 0.001f && Physics.SphereCast(_previousPosition, 0.08f, delta / distance, out RaycastHit hit, distance, hitMask, QueryTriggerInteraction.Collide))
            {
                TryHit(hit.collider, hit.point);
            }
            _previousPosition = current;
        }

        private void OnTriggerEnter(Collider other) => TryHit(other, transform.position);

        private void TryHit(Collider other, Vector3 point)
        {
            if (_captured || other == null) return;
            CombatantVitals receiver = other.GetComponentInParent<CombatantVitals>();
            if (receiver == null || !receiver.IsAlive || receiver.Team == team) return;
            Vector3 impulse = _body.velocity.sqrMagnitude > 0.01f ? _body.velocity.normalized * 1.5f : Vector3.zero;
            receiver.ReceiveDamage(new DamagePacket(damage, poiseDamage, impulse, point, team, poiseDamage >= 15f));
            if (pierce > 0) pierce--; else Destroy(gameObject);
        }
    }
}
