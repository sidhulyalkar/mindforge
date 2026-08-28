using System;
using UnityEngine;
using Mindforge.Combat;

namespace Mindforge.Journey
{
    public enum JourneyEnemyArchetype
    {
        Hollow = 0,
        Shardcaster = 1,
        SignalWarden = 2,
    }

    public enum JourneyEnemyAttackKind
    {
        None = 0,
        Melee = 1,
        Projectile = 2,
        Burst = 3,
    }

    /// <summary>
    /// Reusable enemy authority for the first journey. The three archetypes share one
    /// readable state machine: approach/space -> telegraph -> resolve -> recovery.
    /// Presentation listens to events and never owns damage or movement authority.
    /// </summary>
    [RequireComponent(typeof(CombatantVitals), typeof(Rigidbody))]
    public sealed class JourneyEnemyController : MonoBehaviour
    {
        [SerializeField] private JourneyEnemyArchetype archetype;
        [SerializeField] private CombatantVitals vitals;
        [SerializeField] private Rigidbody body;
        [SerializeField] private Transform player;
        [SerializeField] private CombatantVitals playerVitals;
        [SerializeField] private GuardianMotor playerMotor;
        [SerializeField] private GuardianSwordShieldController playerDefense;
        [SerializeField] private MindforgeProjectile projectilePrefab;
        [SerializeField] private Transform projectileOrigin;
        [SerializeField] private FluxMeter playerFlux;

        [Header("Perception / locomotion")]
        [SerializeField] private float detectionRange = 13.5f;
        [SerializeField] private float leashRange = 15f;
        [SerializeField] private float moveSpeed = 3.25f;
        [SerializeField] private float turnSharpness = 12f;
        [SerializeField] private float desiredDistance = 1.75f;
        [SerializeField] private float retreatDistance = 1.15f;
        [SerializeField] private float strafeStrength = 0.18f;

        [Header("Attack cadence")]
        [SerializeField] private float firstAttackDelay = 0.65f;
        [SerializeField] private float attackInterval = 1.45f;
        [SerializeField] private float meleeWindup = 0.52f;
        [SerializeField] private float meleeRecovery = 0.72f;
        [SerializeField] private float meleeRange = 2.15f;
        [SerializeField] private float meleeArcDegrees = 82f;
        [SerializeField] private float meleeDamage = 10f;
        [SerializeField] private float meleePoise = 8f;

        [Header("Projectile pressure")]
        [SerializeField] private float projectileWindup = 0.68f;
        [SerializeField] private float projectileRecovery = 0.72f;
        [SerializeField] private float projectileSpeed = 10f;
        [SerializeField] private float projectileDamage = 7f;
        [SerializeField] private float projectilePoise = 3f;
        [SerializeField] private int burstCount = 1;
        [SerializeField] private float burstSpreadDegrees = 8f;

        [Header("Rewards")]
        [SerializeField] private float defeatFluxReward = 0.15f;

        private bool _armed;
        private bool _externalPaused;
        private float _pauseStartedAt;
        private Vector3 _home;
        private Vector3 _desiredMove;
        private float _nextAttackAt;
        private float _attackResolveAt;
        private float _recoverUntil;
        private JourneyEnemyAttackKind _pendingAttack;
        private Vector3 _lockedAttackDirection;
        private int _attackSequence;
        private bool _deathHandled;

        public event Action<JourneyEnemyController> Defeated;
        public event Action<JourneyEnemyAttackKind, float> AttackTelegraphed;
        public event Action<JourneyEnemyAttackKind> AttackResolved;
        public event Action<bool> ArmedChanged;

        public JourneyEnemyArchetype Archetype => archetype;
        public CombatantVitals Vitals => vitals;
        public bool Armed => _armed;
        public bool ExternalPaused => _externalPaused;
        public JourneyEnemyAttackKind PendingAttack => _pendingAttack;
        public bool IsAlive => vitals != null && vitals.IsAlive;

        private void Awake()
        {
            ResolveDependencies();
            ConfigureBody();
            ApplyArchetypeDefaults();
        }

        private void OnEnable()
        {
            ResolveDependencies();
            ConfigureBody();
            if (vitals != null)
            {
                vitals.Died -= OnDied;
                vitals.Died += OnDied;
            }
            _home = transform.position;
            _desiredMove = Vector3.zero;
            _pendingAttack = JourneyEnemyAttackKind.None;
            _deathHandled = false;
        }

        private void OnDisable()
        {
            if (vitals != null) vitals.Died -= OnDied;
        }

        public void ConfigureRuntime(
            JourneyEnemyArchetype enemyArchetype,
            Transform guardian,
            CombatantVitals guardianVitals,
            GuardianMotor guardianMotor,
            GuardianSwordShieldController guardianDefense,
            MindforgeProjectile projectile,
            Transform shotOrigin,
            FluxMeter guardianFlux)
        {
            archetype = enemyArchetype;
            player = guardian;
            playerVitals = guardianVitals;
            playerMotor = guardianMotor;
            playerDefense = guardianDefense;
            projectilePrefab = projectile;
            projectileOrigin = shotOrigin;
            playerFlux = guardianFlux;
            ResolveDependencies();
            ApplyArchetypeDefaults();
            ConfigureBody();
        }

        public void Arm()
        {
            ResolveDependencies();
            if (!IsAlive) return;
            _armed = true;
            _pendingAttack = JourneyEnemyAttackKind.None;
            _recoverUntil = Time.time + 0.15f;
            _nextAttackAt = Time.time + Mathf.Max(0.15f, firstAttackDelay);
            _home = transform.position;
            ArmedChanged?.Invoke(true);
        }

        public void Disarm()
        {
            _armed = false;
            _desiredMove = Vector3.zero;
            _pendingAttack = JourneyEnemyAttackKind.None;
            if (body != null) body.velocity = Vector3.zero;
            ArmedChanged?.Invoke(false);
        }

        public void SetExternalPause(bool paused)
        {
            if (_externalPaused == paused) return;
            _externalPaused = paused;
            if (paused)
            {
                _pauseStartedAt = Time.time;
                _desiredMove = Vector3.zero;
            }
            else
            {
                float shift = Mathf.Max(0f, Time.time - _pauseStartedAt);
                _nextAttackAt += shift;
                _attackResolveAt += shift;
                _recoverUntil += shift;
            }
        }

        private void Update()
        {
            _desiredMove = Vector3.zero;
            if (!_armed || _externalPaused || !IsAlive || player == null || playerVitals == null || !playerVitals.IsAlive)
                return;
            if (vitals.Poise != null && vitals.Poise.Broken) return;

            // PhysicalArsenalBootstrap may add GuardianSwordShieldController after the
            // editor-authored journey is serialized. Resolve it lazily before attacks.
            if (playerDefense == null) ResolveDependencies();

            Vector3 toPlayer = Planar(player.position - transform.position);
            float distance = toPlayer.magnitude;
            if (distance > Mathf.Max(detectionRange, leashRange) &&
                Planar(transform.position - _home).magnitude > leashRange)
            {
                _desiredMove = Planar(_home - transform.position).normalized;
                return;
            }
            if (distance > detectionRange) return;

            if (_pendingAttack != JourneyEnemyAttackKind.None)
            {
                if (Time.time >= _attackResolveAt) ResolvePendingAttack();
                return;
            }
            if (Time.time < _recoverUntil) return;

            if (Time.time >= _nextAttackAt && ShouldAttack(distance))
            {
                BeginAttack(ChooseAttack(distance));
                return;
            }

            _desiredMove = ChooseMovement(toPlayer, distance);
        }

        private void FixedUpdate()
        {
            if (body == null || !_armed || _externalPaused || !IsAlive) return;

            if (_desiredMove.sqrMagnitude > 0.001f)
            {
                Vector3 next = body.position + _desiredMove.normalized * moveSpeed * Time.fixedDeltaTime;
                body.MovePosition(next);
            }

            if (player != null)
            {
                Vector3 face = Planar(player.position - transform.position);
                if (face.sqrMagnitude > 0.001f)
                {
                    Quaternion desired = Quaternion.LookRotation(face.normalized, Vector3.up);
                    float t = 1f - Mathf.Exp(-Mathf.Max(0.1f, turnSharpness) * Time.fixedDeltaTime);
                    body.MoveRotation(Quaternion.Slerp(body.rotation, desired, t));
                }
            }
        }

        private Vector3 ChooseMovement(Vector3 toPlayer, float distance)
        {
            if (toPlayer.sqrMagnitude < 0.001f) return Vector3.zero;
            Vector3 forward = toPlayer.normalized;
            Vector3 tangent = Vector3.Cross(Vector3.up, forward) * Mathf.Sin(Time.time * 1.7f + GetInstanceID() * 0.013f);

            if (archetype == JourneyEnemyArchetype.Shardcaster)
            {
                if (distance < retreatDistance) return (-forward + tangent * 0.42f).normalized;
                if (distance > desiredDistance) return (forward + tangent * 0.22f).normalized;
                return tangent.normalized;
            }

            if (distance > desiredDistance) return (forward + tangent * strafeStrength).normalized;
            if (distance < retreatDistance) return (-forward + tangent * 0.22f).normalized;
            return tangent * strafeStrength;
        }

        private bool ShouldAttack(float distance)
        {
            if (archetype == JourneyEnemyArchetype.Hollow) return distance <= meleeRange;
            if (archetype == JourneyEnemyArchetype.Shardcaster) return distance <= detectionRange;
            return distance <= detectionRange;
        }

        private JourneyEnemyAttackKind ChooseAttack(float distance)
        {
            _attackSequence++;
            if (archetype == JourneyEnemyArchetype.Hollow) return JourneyEnemyAttackKind.Melee;
            if (archetype == JourneyEnemyArchetype.Shardcaster) return JourneyEnemyAttackKind.Projectile;

            if (distance <= meleeRange * 1.08f && (_attackSequence % 3 != 0))
                return JourneyEnemyAttackKind.Melee;
            return JourneyEnemyAttackKind.Burst;
        }

        private void BeginAttack(JourneyEnemyAttackKind kind)
        {
            if (kind == JourneyEnemyAttackKind.None || player == null) return;
            _pendingAttack = kind;
            _desiredMove = Vector3.zero;
            _lockedAttackDirection = Planar(player.position - transform.position);
            if (_lockedAttackDirection.sqrMagnitude < 0.001f) _lockedAttackDirection = transform.forward;
            _lockedAttackDirection.Normalize();

            float windup = kind == JourneyEnemyAttackKind.Melee ? meleeWindup : projectileWindup;
            _attackResolveAt = Time.time + Mathf.Max(0.08f, windup);
            AttackTelegraphed?.Invoke(kind, Mathf.Max(0.08f, windup));
        }

        private void ResolvePendingAttack()
        {
            JourneyEnemyAttackKind kind = _pendingAttack;
            _pendingAttack = JourneyEnemyAttackKind.None;
            if (!_armed || _externalPaused || !IsAlive) return;

            if (kind == JourneyEnemyAttackKind.Melee) ResolveMelee();
            else ResolveProjectile(kind == JourneyEnemyAttackKind.Burst);

            float recovery = kind == JourneyEnemyAttackKind.Melee ? meleeRecovery : projectileRecovery;
            _recoverUntil = Time.time + Mathf.Max(0.1f, recovery);
            _nextAttackAt = _recoverUntil + Mathf.Max(0.1f, attackInterval);
            AttackResolved?.Invoke(kind);
        }

        private void ResolveMelee()
        {
            ResolveDependencies();
            if (player == null || playerVitals == null || !playerVitals.IsAlive) return;
            Vector3 delta = Planar(player.position - transform.position);
            float distance = delta.magnitude;
            if (distance > meleeRange || distance <= 0.001f) return;
            if (Vector3.Angle(_lockedAttackDirection, delta.normalized) > meleeArcDegrees * 0.5f) return;
            if (playerMotor != null && playerMotor.IsInvulnerable) return;

            GuardStrikeResult result = playerDefense != null
                ? playerDefense.TryResolveIncomingStrike(
                    meleeDamage,
                    meleePoise,
                    transform.position,
                    player.position + Vector3.up * 0.8f,
                    archetype == JourneyEnemyArchetype.SignalWarden)
                : GuardStrikeResult.NotGuarded;

            if (result == GuardStrikeResult.Blocked ||
                result == GuardStrikeResult.PerfectGuard ||
                result == GuardStrikeResult.GuardBroken)
                return;

            playerVitals.ReceiveDamage(new DamagePacket(
                meleeDamage,
                meleePoise,
                delta.normalized * (archetype == JourneyEnemyArchetype.SignalWarden ? 2.4f : 1.4f),
                player.position + Vector3.up * 0.8f,
                CombatTeam.Enemy,
                archetype == JourneyEnemyArchetype.SignalWarden));
        }

        private void ResolveProjectile(bool burst)
        {
            if (player == null || projectilePrefab == null) return;
            Vector3 origin = projectileOrigin != null
                ? projectileOrigin.position
                : transform.position + Vector3.up * 0.75f;
            int count = burst ? Mathf.Max(2, burstCount) : 1;
            float spread = burst ? Mathf.Max(0f, burstSpreadDegrees) : 0f;

            for (int i = 0; i < count; i++)
            {
                float centered = count <= 1 ? 0f : i - (count - 1) * 0.5f;
                float angle = count <= 1 ? 0f : centered * spread / Mathf.Max(1f, count - 1);
                Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * _lockedAttackDirection;
                direction = direction.normalized;
                MindforgeProjectile p = Instantiate(
                    projectilePrefab,
                    origin,
                    Quaternion.LookRotation(direction, Vector3.up));
                p.Configure(
                    CombatTeam.Enemy,
                    direction * projectileSpeed,
                    projectileDamage,
                    projectilePoise);
            }
        }

        private void OnDied()
        {
            if (_deathHandled) return;
            _deathHandled = true;
            _armed = false;
            _desiredMove = Vector3.zero;
            _pendingAttack = JourneyEnemyAttackKind.None;
            if (body != null)
            {
                body.velocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
            if (playerFlux != null && defeatFluxReward > 0f)
                playerFlux.Award(defeatFluxReward, archetype + " defeated");
            Defeated?.Invoke(this);
            Destroy(gameObject, 0.35f);
        }

        private void ResolveDependencies()
        {
            if (vitals == null) vitals = GetComponent<CombatantVitals>();
            if (body == null) body = GetComponent<Rigidbody>();
            if (player == null) return;
            if (playerVitals == null) playerVitals = player.GetComponent<CombatantVitals>();
            if (playerMotor == null) playerMotor = player.GetComponent<GuardianMotor>();
            if (playerDefense == null) playerDefense = player.GetComponent<GuardianSwordShieldController>();
            if (playerFlux == null) playerFlux = player.GetComponent<FluxMeter>();
        }

        private void ConfigureBody()
        {
            if (body == null) return;
            body.useGravity = false;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.constraints = RigidbodyConstraints.FreezePositionY |
                               RigidbodyConstraints.FreezeRotationX |
                               RigidbodyConstraints.FreezeRotationZ;
        }

        private void ApplyArchetypeDefaults()
        {
            switch (archetype)
            {
                case JourneyEnemyArchetype.Hollow:
                    detectionRange = 10.5f;
                    moveSpeed = 3.35f;
                    desiredDistance = 1.72f;
                    retreatDistance = 1.0f;
                    strafeStrength = 0.16f;
                    firstAttackDelay = 0.72f;
                    attackInterval = 1.10f;
                    meleeWindup = 0.56f;
                    meleeRecovery = 0.78f;
                    meleeRange = 2.05f;
                    meleeArcDegrees = 80f;
                    meleeDamage = 9f;
                    meleePoise = 7f;
                    defeatFluxReward = 0.12f;
                    break;

                case JourneyEnemyArchetype.Shardcaster:
                    detectionRange = 13.5f;
                    moveSpeed = 2.75f;
                    desiredDistance = 6.4f;
                    retreatDistance = 4.15f;
                    strafeStrength = 0.42f;
                    firstAttackDelay = 0.90f;
                    attackInterval = 1.22f;
                    projectileWindup = 0.72f;
                    projectileRecovery = 0.68f;
                    projectileSpeed = 10.5f;
                    projectileDamage = 7f;
                    projectilePoise = 3f;
                    burstCount = 1;
                    defeatFluxReward = 0.16f;
                    break;

                case JourneyEnemyArchetype.SignalWarden:
                    detectionRange = 14.5f;
                    moveSpeed = 3.05f;
                    desiredDistance = 2.05f;
                    retreatDistance = 1.25f;
                    strafeStrength = 0.28f;
                    firstAttackDelay = 0.80f;
                    attackInterval = 0.90f;
                    meleeWindup = 0.50f;
                    meleeRecovery = 0.62f;
                    meleeRange = 2.45f;
                    meleeArcDegrees = 92f;
                    meleeDamage = 15f;
                    meleePoise = 15f;
                    projectileWindup = 0.66f;
                    projectileRecovery = 0.58f;
                    projectileSpeed = 12f;
                    projectileDamage = 8.5f;
                    projectilePoise = 5f;
                    burstCount = 3;
                    burstSpreadDegrees = 18f;
                    defeatFluxReward = 0.55f;
                    break;
            }
        }

        private static Vector3 Planar(Vector3 value)
        {
            value.y = 0f;
            return value;
        }
    }
}
