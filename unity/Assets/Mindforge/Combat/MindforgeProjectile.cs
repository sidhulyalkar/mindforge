using UnityEngine;
using Mindforge.Presentation;

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

        [Header("Visual language")]
        [SerializeField] private CombatVisualPalette palette;
        [SerializeField] private Renderer visualRenderer;
        [SerializeField] private TrailRenderer trailRenderer;

        private Rigidbody _body;
        private Collider _collider;
        private Vector3 _previousPosition;
        private bool _captured;
        private bool _reflected;
        private bool _externalPaused;
        private bool _pauseWasKinematic;
        private bool _pauseColliderEnabled;
        private Vector3 _pauseVelocity;
        private Vector3 _pauseAngularVelocity;
        private Transform _captureAnchor;
        private float _capturePhase;
        private MaterialPropertyBlock _visualBlock;

        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorProperty = Shader.PropertyToID("_Color");
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        public CombatTeam Team => team;
        public bool IsHostileToGuardian => team == CombatTeam.Enemy;
        public Rigidbody Body => _body;
        public bool Captured => _captured;
        public bool ExternalPaused => _externalPaused;

        private void Awake()
        {
            _body = GetComponent<Rigidbody>();
            _collider = GetComponent<Collider>();
            _body.interpolation = RigidbodyInterpolation.Interpolate;
            _body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            _previousPosition = transform.position;
            _visualBlock = new MaterialPropertyBlock();
            if (visualRenderer == null) visualRenderer = GetComponentInChildren<Renderer>();
            if (trailRenderer == null) trailRenderer = GetComponentInChildren<TrailRenderer>();
            ApplyVisualIdentity();
            Destroy(gameObject, lifetime);
        }

        public void Configure(CombatTeam newTeam, Vector3 velocity, float newDamage, float newPoise, int newPierce = 0)
        {
            team = newTeam;
            damage = newDamage;
            poiseDamage = newPoise;
            pierce = newPierce;
            _reflected = false;
            _body.velocity = velocity;
            ApplyVisualIdentity();
        }

        public void ReflectTowards(Transform target, float speed, float newDamage, float newPoise, int extraPierce = 0)
        {
            if (target == null) return;
            ReleaseFromCapture();
            team = CombatTeam.Guardian;
            damage = newDamage;
            poiseDamage = newPoise;
            pierce = Mathf.Max(pierce, extraPierce);
            _reflected = true;
            Vector3 direction = (target.position - transform.position).normalized;
            _body.velocity = direction * speed;
            ApplyVisualIdentity();
        }

        public void SetExternalPause(bool paused)
        {
            if (_externalPaused == paused) return;
            _externalPaused = paused;

            // Captured projectiles are already kinematic/non-colliding. Their orbit
            // simply stops while externally paused and resumes with the Bloom.
            if (_captured) return;

            if (paused)
            {
                _pauseWasKinematic = _body.isKinematic;
                _pauseColliderEnabled = _collider.enabled;
                _pauseVelocity = _body.velocity;
                _pauseAngularVelocity = _body.angularVelocity;
                _body.isKinematic = true;
                _collider.enabled = false;
            }
            else
            {
                _body.isKinematic = _pauseWasKinematic;
                _collider.enabled = _pauseColliderEnabled;
                if (!_body.isKinematic)
                {
                    _body.velocity = _pauseVelocity;
                    _body.angularVelocity = _pauseAngularVelocity;
                }
                _previousPosition = transform.position;
            }
        }

        private void ApplyVisualIdentity()
        {
            Color color;
            if (team == CombatTeam.Enemy)
            {
                bool heavy = damage >= 14f || poiseDamage >= 15f;
                color = palette != null
                    ? (heavy ? palette.hostileHeavy : palette.hostilePrimary)
                    : (heavy ? new Color(1f, 0.42f, 0.12f) : new Color(1f, 0.18f, 0.34f));
            }
            else if (_reflected)
            {
                color = palette != null ? palette.reflected : new Color(0.73f, 0.38f, 1f);
            }
            else
            {
                color = palette != null ? palette.guardianPrimary : new Color(0.94f, 0.95f, 1f);
            }

            if (visualRenderer != null)
            {
                visualRenderer.GetPropertyBlock(_visualBlock);
                _visualBlock.SetColor(BaseColor, color);
                _visualBlock.SetColor(ColorProperty, color);
                _visualBlock.SetColor(EmissionColor, color * 1.8f);
                visualRenderer.SetPropertyBlock(_visualBlock);
            }

            if (trailRenderer != null)
            {
                trailRenderer.startColor = color;
                trailRenderer.endColor = new Color(color.r, color.g, color.b, 0f);
            }
        }

        public void Capture(Transform anchor, float phase)
        {
            if (_captured || _externalPaused) return;
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
            _collider.enabled = !_externalPaused;
            _previousPosition = transform.position;
        }

        private void Update()
        {
            if (_externalPaused || !_captured || _captureAnchor == null) return;
            _capturePhase += Time.unscaledDeltaTime * 4.5f;
            float radius = 0.65f + 0.16f * Mathf.Sin(_capturePhase * 1.7f);
            transform.position = _captureAnchor.position + new Vector3(Mathf.Cos(_capturePhase), 0.45f, Mathf.Sin(_capturePhase)) * radius;
        }

        private void FixedUpdate()
        {
            if (_externalPaused || _captured) return;
            Vector3 current = transform.position;
            Vector3 delta = current - _previousPosition;
            float distance = delta.magnitude;
            if (distance > 0.001f && Physics.SphereCast(_previousPosition, 0.08f, delta / distance, out RaycastHit hit, distance, hitMask, QueryTriggerInteraction.Collide))
                TryHit(hit.collider, hit.point);
            _previousPosition = current;
        }

        private void OnTriggerEnter(Collider other) => TryHit(other, transform.position);

        private void TryHit(Collider other, Vector3 point)
        {
            if (_externalPaused || _captured || other == null) return;
            CombatantVitals receiver = other.GetComponentInParent<CombatantVitals>();
            if (receiver == null || !receiver.IsAlive || receiver.Team == team) return;
            Vector3 impulse = _body.velocity.sqrMagnitude > 0.01f ? _body.velocity.normalized * 1.5f : Vector3.zero;
            receiver.ReceiveDamage(new DamagePacket(damage, poiseDamage, impulse, point, team, poiseDamage >= 15f));
            if (pierce > 0) pierce--; else Destroy(gameObject);
        }
    }
}
