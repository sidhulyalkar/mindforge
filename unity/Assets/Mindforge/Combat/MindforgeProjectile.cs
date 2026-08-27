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
        private string _neuralPayoffKind;
        private float _neuralBonusDamage;
        private bool _consumed;

        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorProperty = Shader.PropertyToID("_Color");
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        public CombatTeam Team => team;
        public bool IsHostileToGuardian => team == CombatTeam.Enemy;
        public Rigidbody Body => _body;
        public bool Captured => _captured;
        public bool ExternalPaused => _externalPaused;
        public float Damage => Mathf.Max(0f, damage);
        public float Speed => _body != null ? _body.velocity.magnitude : 0f;

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

        public void Configure(
            CombatTeam newTeam,
            Vector3 velocity,
            float newDamage,
            float newPoise,
            int newPierce = 0,
            string neuralPayoffKind = null,
            float neuralBonusDamage = 0f)
        {
            team = newTeam;
            damage = newDamage;
            poiseDamage = newPoise;
            pierce = newPierce;
            _reflected = false;
            _consumed = false;
            _neuralPayoffKind = neuralPayoffKind;
            _neuralBonusDamage = Mathf.Clamp(neuralBonusDamage, 0f, Mathf.Max(0f, newDamage));
            _body.velocity = velocity;
            ApplyVisualIdentity();
        }

        public void ReflectTowards(
            Transform target,
            float speed,
            float newDamage,
            float newPoise,
            int extraPierce = 0,
            string neuralPayoffKind = null,
            float neuralBonusDamage = 0f)
        {
            if (target == null || _consumed) return;
            ReleaseFromCapture();
            team = CombatTeam.Guardian;
            damage = newDamage;
            poiseDamage = newPoise;
            pierce = Mathf.Max(pierce, extraPierce);
            _reflected = true;
            _neuralPayoffKind = neuralPayoffKind;
            _neuralBonusDamage = Mathf.Clamp(neuralBonusDamage, 0f, Mathf.Max(0f, newDamage));
            Vector3 direction = (target.position - transform.position).normalized;
            _body.velocity = direction * speed;
            _previousPosition = transform.position;
            ApplyVisualIdentity();
        }

        public void ConsumeByShield()
        {
            if (_consumed) return;
            _consumed = true;
            if (_collider != null) _collider.enabled = false;
            if (_body != null)
            {
                _body.velocity = Vector3.zero;
                _body.angularVelocity = Vector3.zero;
                _body.isKinematic = true;
            }
            Destroy(gameObject);
        }

        public void SetExternalPause(bool paused)
        {
            if (_externalPaused == paused) return;
            _externalPaused = paused;

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
            if (_captured || _externalPaused || _consumed) return;
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
            if (_externalPaused || !_captured || _captureAnchor == null || _consumed) return;
            _capturePhase += Time.unscaledDeltaTime * 4.5f;
            float radius = 0.65f + 0.16f * Mathf.Sin(_capturePhase * 1.7f);
            transform.position = _captureAnchor.position + new Vector3(Mathf.Cos(_capturePhase), 0.45f, Mathf.Sin(_capturePhase)) * radius;
        }

        private void FixedUpdate()
        {
            if (_externalPaused || _captured || _consumed) return;
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
            if (_externalPaused || _captured || _consumed || other == null) return;

            // A raised shield is a physical collision surface, not an invulnerability
            // flag on the Guardian. It gets first authority over the impact it caught.
            GuardianShieldHitbox shield = other.GetComponentInParent<GuardianShieldHitbox>();
            if (shield != null && shield.TryResolveProjectile(this, point)) return;

            CombatantVitals receiver = other.GetComponentInParent<CombatantVitals>();
            if (receiver == null || !receiver.IsAlive || receiver.Team == team) return;
            Vector3 impulse = _body.velocity.sqrMagnitude > 0.01f ? _body.velocity.normalized * 1.5f : Vector3.zero;
            receiver.ReceiveDamage(new DamagePacket(
                damage,
                poiseDamage,
                impulse,
                point,
                team,
                poiseDamage >= 15f,
                _neuralPayoffKind,
                _neuralBonusDamage));
            if (pierce > 0) pierce--; else Destroy(gameObject);
        }
    }
}
