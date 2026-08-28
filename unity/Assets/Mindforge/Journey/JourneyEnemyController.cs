using System;
using UnityEngine;
using Mindforge.Combat;
using Mindforge.Enemies;

namespace Mindforge.Journey
{
    public enum JourneyEnemyArchetype
    {
        Hollow = 0,
        Shardcaster = 1,
        SignalWarden = 2,
        NullSentry = 3,
        ChromePenitent = 4,
    }

    public enum JourneyEnemyAttackKind
    {
        None = 0,
        Melee = 1,
        Projectile = 2,
        Burst = 3,
        Retreat = 4,
    }

    /// <summary>
    /// Reusable fixed-tick enemy authority for teaching encounters and the Null Ward.
    /// Attacks are data, filtered by cooldown/range/facing/LOS and selected by a stable
    /// deterministic PRNG. Presentation listens to events and never owns gameplay.
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
        [SerializeField] private float leashRange = 16f;
        [SerializeField] private float moveSpeed = 3.25f;
        [SerializeField] private float turnSharpness = 12f;
        [SerializeField] private float desiredDistance = 1.75f;
        [SerializeField] private float retreatDistance = 1.15f;
        [SerializeField] private float strafeStrength = 0.18f;

        [Header("Deterministic attack brain · 120 Hz")]
        [SerializeField, Min(1)] private int decisionCadenceTicks = 10;
        [SerializeField, Min(1)] private int firstAttackDelayTicks = 78;
        [SerializeField] private uint deterministicSeed;
        [SerializeField] private EnemyAttackDefinition[] attackDefinitions;

        [Header("Lifecycle / rewards")]
        [SerializeField] private bool destroyOnDeath = true;
        [SerializeField] private float defeatFluxReward = 0.15f;

        private readonly int[] _eligibleAttackIndices = new int[16];
        private readonly RaycastHit[] _losHits = new RaycastHit[12];
        private bool _armed;
        private bool _externalPaused;
        private long _pauseStartedTick;
        private Vector3 _home;
        private Vector3 _spawnPosition;
        private Quaternion _spawnRotation;
        private Vector3 _desiredMove;
        private long _nextDecisionTick;
        private long _attackResolveTick;
        private long _recoverUntilTick;
        private JourneyEnemyAttackKind _pendingAttack;
        private int _pendingAttackIndex = -1;
        private Vector3 _lockedAttackDirection;
        private long[] _attackCooldownUntil = Array.Empty<long>();
        private uint _rngState;
        private bool _deathHandled;
        private bool _defeatedDormant;
        private Collider[] _colliders = Array.Empty<Collider>();
        private bool[] _colliderDefaults = Array.Empty<bool>();
        private bool _bodyDefaultKinematic;

        public event Action<JourneyEnemyController> Defeated;
        public event Action<JourneyEnemyController> Reconstructed;
        public event Action<JourneyEnemyAttackKind, float> AttackTelegraphed;
        public event Action<JourneyEnemyAttackKind> AttackResolved;
        public event Action<bool> ArmedChanged;
        public event Action<EnemyAttackDefinition> AttackSelected;

        public JourneyEnemyArchetype Archetype => archetype;
        public CombatantVitals Vitals => vitals;
        public bool Armed => _armed;
        public bool ExternalPaused => _externalPaused;
        public bool CheckpointResettable => !destroyOnDeath;
        public bool DefeatedDormant => _defeatedDormant;
        public JourneyEnemyAttackKind PendingAttack => _pendingAttack;
        public bool IsAlive => vitals != null && vitals.IsAlive;
        public EnemyAttackDefinition CurrentAttackDefinition
            => _pendingAttackIndex >= 0 && attackDefinitions != null && _pendingAttackIndex < attackDefinitions.Length
                ? attackDefinitions[_pendingAttackIndex]
                : null;
        public string CurrentAttackId => CurrentAttackDefinition != null ? CurrentAttackDefinition.Id : string.Empty;
        public uint DeterministicSeed => deterministicSeed;

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
            ResolveDependencies();
            ConfigureBody();
            CaptureLifecycleDefaults();
            ApplyArchetypeDefaults();
            InitializeDeterminism();
        }

        private void OnEnable()
        {
            ResolveDependencies();
            ConfigureBody();
            CaptureLifecycleDefaults();
            ApplyArchetypeDefaults();
            InitializeDeterminism();
            if (vitals != null)
            {
                vitals.Died -= OnDied;
                vitals.Died += OnDied;
            }
            _home = transform.position;
            _desiredMove = Vector3.zero;
            _pendingAttack = JourneyEnemyAttackKind.None;
            _pendingAttackIndex = -1;
            if (!_defeatedDormant) _deathHandled = false;
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
            InitializeDeterminism();
            ConfigureBody();
            CaptureLifecycleDefaults();
        }

        /// <summary>
        /// Null Ward ordinary enemies use a persistent dormant-death lifecycle so the
        /// Memory Forge can reconstruct the exact authored encounter from the same seed.
        /// Legacy journey enemies retain destroy-on-death by default.
        /// </summary>
        public void ConfigureCheckpointLifecycle(bool checkpointResettable)
        {
            destroyOnDeath = !checkpointResettable;
            _spawnPosition = transform.position;
            _spawnRotation = transform.rotation;
            CaptureLifecycleDefaults(true);
        }

        public void ResetForCheckpoint()
        {
            if (destroyOnDeath || vitals == null) return;

            Disarm();
            _externalPaused = false;
            _pauseStartedTick = 0;
            _deathHandled = false;
            _defeatedDormant = false;
            _pendingAttack = JourneyEnemyAttackKind.None;
            _pendingAttackIndex = -1;
            _desiredMove = Vector3.zero;
            _lockedAttackDirection = Vector3.zero;

            transform.SetPositionAndRotation(_spawnPosition, _spawnRotation);
            _home = _spawnPosition;
            vitals.ResetForCheckpoint(true);

            ApplyArchetypeDefaults();
            RebuildCooldownState();
            _rngState = deterministicSeed != 0u ? deterministicSeed : 0x6D2B79F5u;
            _nextDecisionTick = FixedTick + Mathf.Max(1, firstAttackDelayTicks);
            _attackResolveTick = long.MaxValue / 4;
            _recoverUntilTick = FixedTick;

            if (body != null)
            {
                body.isKinematic = _bodyDefaultKinematic;
                body.position = _spawnPosition;
                body.rotation = _spawnRotation;
                body.velocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.WakeUp();
            }
            RestoreColliders();
            Physics.SyncTransforms();
            Reconstructed?.Invoke(this);
        }

        public void Arm()
        {
            ResolveDependencies();
            if (!IsAlive || _defeatedDormant) return;
            _armed = true;
            _pendingAttack = JourneyEnemyAttackKind.None;
            _pendingAttackIndex = -1;
            _recoverUntilTick = FixedTick + 18;
            _nextDecisionTick = FixedTick + Mathf.Max(1, firstAttackDelayTicks);
            _home = transform.position;
            ArmedChanged?.Invoke(true);
        }

        public void Disarm()
        {
            _armed = false;
            _desiredMove = Vector3.zero;
            _pendingAttack = JourneyEnemyAttackKind.None;
            _pendingAttackIndex = -1;
            if (body != null) body.velocity = Vector3.zero;
            ArmedChanged?.Invoke(false);
        }

        public void SetExternalPause(bool paused)
        {
            if (_externalPaused == paused) return;
            _externalPaused = paused;
            if (paused)
            {
                _pauseStartedTick = FixedTick;
                _desiredMove = Vector3.zero;
            }
            else
            {
                long shift = Math.Max(0L, FixedTick - _pauseStartedTick);
                _nextDecisionTick += shift;
                _attackResolveTick += shift;
                _recoverUntilTick += shift;
                for (int i = 0; i < _attackCooldownUntil.Length; i++)
                    _attackCooldownUntil[i] += shift;
            }
        }

        private void FixedUpdate()
        {
            _desiredMove = Vector3.zero;
            if (!_armed || _externalPaused || _defeatedDormant || !IsAlive || player == null || playerVitals == null || !playerVitals.IsAlive)
                return;
            if (vitals.Poise != null && vitals.Poise.Broken) return;

            // PhysicalArsenalBootstrap may add GuardianSwordShieldController after the
            // editor-authored journey is serialized. Resolve it lazily before attacks.
            if (playerDefense == null) ResolveDependencies();

            Vector3 toPlayer = Planar(player.position - transform.position);
            float distance = toPlayer.magnitude;

            if (_pendingAttack != JourneyEnemyAttackKind.None)
            {
                TrackPendingAttack(toPlayer);
                if (FixedTick >= _attackResolveTick) ResolvePendingAttack();
                FaceDirection(_lockedAttackDirection);
                return;
            }

            if (FixedTick < _recoverUntilTick)
            {
                FaceDirection(toPlayer);
                return;
            }

            if (distance > Mathf.Max(detectionRange, leashRange) &&
                Planar(transform.position - _home).magnitude > leashRange)
            {
                _desiredMove = Planar(_home - transform.position).normalized;
                ApplyMovement();
                FaceDirection(_desiredMove);
                return;
            }
            if (distance > detectionRange) return;

            if (FixedTick >= _nextDecisionTick)
            {
                int attackIndex = ChooseAttack(distance, toPlayer);
                _nextDecisionTick = FixedTick + Mathf.Max(1, decisionCadenceTicks);
                if (attackIndex >= 0)
                {
                    BeginAttack(attackIndex, toPlayer);
                    return;
                }
            }

            _desiredMove = ChooseMovement(toPlayer, distance);
            ApplyMovement();
            FaceDirection(toPlayer);
        }

        private int ChooseAttack(float distance, Vector3 toPlayer)
        {
            EnsureAttackProfile();
            if (attackDefinitions == null || attackDefinitions.Length == 0) return -1;

            float facingAngle = toPlayer.sqrMagnitude > 0.001f
                ? Vector3.Angle(transform.forward, toPlayer.normalized)
                : 0f;
            int eligibleCount = 0;
            int totalWeight = 0;

            for (int i = 0; i < attackDefinitions.Length && eligibleCount < _eligibleAttackIndices.Length; i++)
            {
                EnemyAttackDefinition attack = attackDefinitions[i];
                if (attack == null) continue;
                if (i < _attackCooldownUntil.Length && FixedTick < _attackCooldownUntil[i]) continue;
                if (!attack.RangeValid(distance)) continue;
                if (!attack.FacingValid(facingAngle)) continue;
                if (attack.RequiresLineOfSight && !HasLineOfSight()) continue;

                _eligibleAttackIndices[eligibleCount++] = i;
                totalWeight += attack.Weight;
            }

            if (eligibleCount == 0 || totalWeight <= 0) return -1;
            int roll = NextInt(totalWeight);
            int accumulated = 0;
            for (int i = 0; i < eligibleCount; i++)
            {
                int index = _eligibleAttackIndices[i];
                accumulated += attackDefinitions[index].Weight;
                if (roll < accumulated) return index;
            }
            return _eligibleAttackIndices[eligibleCount - 1];
        }

        private void BeginAttack(int attackIndex, Vector3 toPlayer)
        {
            EnsureAttackProfile();
            if (attackIndex < 0 || attackDefinitions == null || attackIndex >= attackDefinitions.Length) return;
            EnemyAttackDefinition attack = attackDefinitions[attackIndex];
            if (attack == null) return;

            _pendingAttackIndex = attackIndex;
            _pendingAttack = ToJourneyKind(attack.Type);
            _desiredMove = Vector3.zero;
            _lockedAttackDirection = toPlayer;
            if (_lockedAttackDirection.sqrMagnitude < 0.001f) _lockedAttackDirection = transform.forward;
            _lockedAttackDirection.Normalize();
            _attackResolveTick = FixedTick + attack.TelegraphTicks;
            if (attackIndex < _attackCooldownUntil.Length)
                _attackCooldownUntil[attackIndex] = FixedTick + attack.CooldownTicks;

            AttackSelected?.Invoke(attack);
            AttackTelegraphed?.Invoke(_pendingAttack, attack.TelegraphTicks * Time.fixedDeltaTime);
        }

        private void TrackPendingAttack(Vector3 toPlayer)
        {
            EnemyAttackDefinition attack = CurrentAttackDefinition;
            if (attack == null || attack.TrackingStrength <= 0f || toPlayer.sqrMagnitude < 0.001f) return;
            Vector3 desired = toPlayer.normalized;
            float t = Mathf.Clamp01(attack.TrackingStrength * 8f * Time.fixedDeltaTime);
            _lockedAttackDirection = Vector3.Slerp(_lockedAttackDirection, desired, t).normalized;
        }

        private void ResolvePendingAttack()
        {
            EnemyAttackDefinition attack = CurrentAttackDefinition;
            JourneyEnemyAttackKind kind = _pendingAttack;
            _pendingAttack = JourneyEnemyAttackKind.None;
            if (!_armed || _externalPaused || _defeatedDormant || !IsAlive || attack == null)
            {
                _pendingAttackIndex = -1;
                return;
            }

            if (attack.Type == EnemyAttackType.Melee) ResolveMelee(attack);
            else if (attack.Type == EnemyAttackType.Retreat) ResolveRetreat(attack);
            else ResolveProjectile(attack);

            _recoverUntilTick = FixedTick + attack.ActiveTicks + attack.RecoveryTicks;
            _nextDecisionTick = _recoverUntilTick + Mathf.Max(1, decisionCadenceTicks);
            AttackResolved?.Invoke(kind);
            _pendingAttackIndex = -1;
        }

        private void ResolveMelee(EnemyAttackDefinition attack)
        {
            ResolveDependencies();
            if (player == null || playerVitals == null || !playerVitals.IsAlive) return;
            Vector3 delta = Planar(player.position - transform.position);
            float distance = delta.magnitude;
            if (!attack.RangeValid(distance) || distance <= 0.001f) return;
            if (Vector3.Angle(_lockedAttackDirection, delta.normalized) > attack.MaximumFacingAngle * 0.5f) return;
            if (playerMotor != null && playerMotor.IsInvulnerable) return;

            GuardStrikeResult result = playerDefense != null
                ? playerDefense.TryResolveIncomingStrike(
                    attack.Damage,
                    attack.PoiseDamage,
                    transform.position,
                    player.position + Vector3.up * 0.8f,
                    attack.Heavy)
                : GuardStrikeResult.NotGuarded;

            if (result == GuardStrikeResult.Blocked ||
                result == GuardStrikeResult.PerfectGuard ||
                result == GuardStrikeResult.GuardBroken)
                return;

            playerVitals.ReceiveDamage(new DamagePacket(
                attack.Damage,
                attack.PoiseDamage,
                delta.normalized * attack.Knockback,
                player.position + Vector3.up * 0.8f,
                CombatTeam.Enemy,
                attack.Heavy));
        }

        private void ResolveProjectile(EnemyAttackDefinition attack)
        {
            if (player == null || projectilePrefab == null) return;
            Vector3 origin = projectileOrigin != null
                ? projectileOrigin.position
                : transform.position + Vector3.up * 0.75f;
            int count = attack.ProjectileCount;
            float spread = attack.ProjectileSpreadDegrees;

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
                    direction * attack.ProjectileSpeed,
                    attack.Damage,
                    attack.PoiseDamage);
            }
        }

        private void ResolveRetreat(EnemyAttackDefinition attack)
        {
            if (body == null) return;
            Vector3 away = -_lockedAttackDirection;
            away.y = 0f;
            if (away.sqrMagnitude < 0.001f) away = -transform.forward;
            body.MovePosition(body.position + away.normalized * Mathf.Max(0.5f, attack.Knockback));
        }

        private Vector3 ChooseMovement(Vector3 toPlayer, float distance)
        {
            if (toPlayer.sqrMagnitude < 0.001f) return Vector3.zero;
            Vector3 forward = toPlayer.normalized;
            float phase = FixedTick * 0.014f + (deterministicSeed % 997u) * 0.013f;
            Vector3 tangent = Vector3.Cross(Vector3.up, forward) * Mathf.Sin(phase);

            bool ranged = archetype == JourneyEnemyArchetype.Shardcaster || archetype == JourneyEnemyArchetype.NullSentry;
            if (ranged)
            {
                if (distance < retreatDistance) return (-forward + tangent * 0.42f).normalized;
                if (distance > desiredDistance) return (forward + tangent * 0.22f).normalized;
                return tangent.normalized;
            }

            if (distance > desiredDistance) return (forward + tangent * strafeStrength).normalized;
            if (distance < retreatDistance) return (-forward + tangent * 0.22f).normalized;
            return tangent * strafeStrength;
        }

        private void ApplyMovement()
        {
            if (body == null || _desiredMove.sqrMagnitude <= 0.001f) return;
            Vector3 next = body.position + _desiredMove.normalized * moveSpeed * Time.fixedDeltaTime;
            body.MovePosition(next);
        }

        private void FaceDirection(Vector3 direction)
        {
            if (body == null) return;
            Vector3 face = Planar(direction);
            if (face.sqrMagnitude <= 0.001f) return;
            Quaternion desired = Quaternion.LookRotation(face.normalized, Vector3.up);
            float t = 1f - Mathf.Exp(-Mathf.Max(0.1f, turnSharpness) * Time.fixedDeltaTime);
            body.MoveRotation(Quaternion.Slerp(body.rotation, desired, t));
        }

        private bool HasLineOfSight()
        {
            if (player == null) return false;
            Vector3 origin = projectileOrigin != null
                ? projectileOrigin.position
                : transform.position + Vector3.up * 0.9f;
            Vector3 target = player.position + Vector3.up * 0.85f;
            Vector3 delta = target - origin;
            float distance = delta.magnitude;
            if (distance <= 0.05f) return true;

            int count = Physics.RaycastNonAlloc(
                origin,
                delta / distance,
                _losHits,
                distance,
                ~0,
                QueryTriggerInteraction.Ignore);

            Transform nearest = null;
            float nearestDistance = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                Transform hit = _losHits[i].transform;
                if (hit == null || hit == transform || hit.IsChildOf(transform)) continue;
                if (_losHits[i].distance >= nearestDistance) continue;
                nearestDistance = _losHits[i].distance;
                nearest = hit;
            }

            if (nearest == null) return true;
            return nearest == player || nearest.IsChildOf(player) || player.IsChildOf(nearest);
        }

        private void OnDied()
        {
            if (_deathHandled) return;
            _deathHandled = true;
            _armed = false;
            _desiredMove = Vector3.zero;
            _pendingAttack = JourneyEnemyAttackKind.None;
            _pendingAttackIndex = -1;
            if (body != null)
            {
                body.velocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
            if (playerFlux != null && defeatFluxReward > 0f)
                playerFlux.Award(defeatFluxReward, archetype + " defeated");
            Defeated?.Invoke(this);

            if (destroyOnDeath)
            {
                Destroy(gameObject, 0.35f);
                return;
            }

            _defeatedDormant = true;
            SetCollidersEnabled(false);
            if (body != null) body.isKinematic = true;
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

        private void CaptureLifecycleDefaults(bool force = false)
        {
            if (!force && _colliders.Length > 0) return;
            _spawnPosition = transform.position;
            _spawnRotation = transform.rotation;
            _bodyDefaultKinematic = body != null && body.isKinematic;
            _colliders = GetComponentsInChildren<Collider>(true);
            _colliderDefaults = new bool[_colliders.Length];
            for (int i = 0; i < _colliders.Length; i++)
                _colliderDefaults[i] = _colliders[i] != null && _colliders[i].enabled;
        }

        private void SetCollidersEnabled(bool enabled)
        {
            for (int i = 0; i < _colliders.Length; i++)
                if (_colliders[i] != null) _colliders[i].enabled = enabled;
        }

        private void RestoreColliders()
        {
            for (int i = 0; i < _colliders.Length; i++)
                if (_colliders[i] != null) _colliders[i].enabled = i < _colliderDefaults.Length && _colliderDefaults[i];
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
                    firstAttackDelayTicks = 86;
                    defeatFluxReward = 0.12f;
                    attackDefinitions = new[]
                    {
                        EnemyAttackDefinition.Create("hollow_slash", EnemyAttackType.Melee, 0.35f, 2.05f, 82f, 10, 122, 67, 2, 94, 0.12f, 9f, 7f, 1.4f, 0f, 1, 0f, false, false, "hollow_slash"),
                    };
                    break;

                case JourneyEnemyArchetype.Shardcaster:
                    detectionRange = 13.5f;
                    moveSpeed = 2.75f;
                    desiredDistance = 6.4f;
                    retreatDistance = 4.15f;
                    strafeStrength = 0.42f;
                    firstAttackDelayTicks = 108;
                    defeatFluxReward = 0.16f;
                    attackDefinitions = new[]
                    {
                        EnemyAttackDefinition.Create("shard_bolt", EnemyAttackType.Projectile, 2.5f, 13.5f, 100f, 10, 150, 86, 1, 82, 0.72f, 7f, 3f, 0f, 10.5f, 1, 0f, true, false, "shard_bolt"),
                    };
                    break;

                case JourneyEnemyArchetype.SignalWarden:
                    detectionRange = 14.5f;
                    moveSpeed = 3.05f;
                    desiredDistance = 2.05f;
                    retreatDistance = 1.25f;
                    strafeStrength = 0.28f;
                    firstAttackDelayTicks = 96;
                    defeatFluxReward = 0.55f;
                    attackDefinitions = new[]
                    {
                        EnemyAttackDefinition.Create("warden_cleave", EnemyAttackType.Melee, 0.45f, 2.45f, 94f, 7, 128, 60, 2, 74, 0.22f, 15f, 15f, 2.4f, 0f, 1, 0f, false, true, "warden_cleave"),
                        EnemyAttackDefinition.Create("warden_burst", EnemyAttackType.Burst, 2.0f, 14.5f, 105f, 4, 180, 79, 1, 70, 0.68f, 8.5f, 5f, 0f, 12f, 3, 18f, true, false, "warden_burst"),
                    };
                    break;

                case JourneyEnemyArchetype.NullSentry:
                    detectionRange = 15f;
                    moveSpeed = 2.85f;
                    desiredDistance = 7.0f;
                    retreatDistance = 3.6f;
                    strafeStrength = 0.48f;
                    firstAttackDelayTicks = 90;
                    defeatFluxReward = 0.18f;
                    attackDefinitions = new[]
                    {
                        EnemyAttackDefinition.Create("sentry_tracking_bolt", EnemyAttackType.Projectile, 3.2f, 15f, 100f, 6, 156, 66, 1, 58, 0.78f, 7.5f, 3f, 0f, 10.8f, 1, 0f, true, false, "sentry_tracking_bolt"),
                        EnemyAttackDefinition.Create("sentry_fan_burst", EnemyAttackType.Burst, 4.0f, 13f, 105f, 4, 210, 72, 1, 72, 0.46f, 5.5f, 2.5f, 0f, 9.8f, 3, 24f, true, false, "sentry_fan_burst"),
                        EnemyAttackDefinition.Create("sentry_retreat_pulse", EnemyAttackType.Retreat, 0.0f, 3.2f, 145f, 8, 240, 24, 1, 30, 0f, 0f, 0f, 2.8f, 0f, 1, 0f, false, false, "sentry_retreat_pulse"),
                    };
                    break;

                case JourneyEnemyArchetype.ChromePenitent:
                    detectionRange = 11.5f;
                    moveSpeed = 3.30f;
                    desiredDistance = 1.85f;
                    retreatDistance = 0.95f;
                    strafeStrength = 0.34f;
                    firstAttackDelayTicks = 82;
                    defeatFluxReward = 0.22f;
                    attackDefinitions = new[]
                    {
                        EnemyAttackDefinition.Create("penitent_fast_slash", EnemyAttackType.Melee, 0.35f, 2.10f, 82f, 7, 96, 45, 2, 60, 0.20f, 9.5f, 7f, 1.5f, 0f, 1, 0f, false, false, "penitent_fast_slash"),
                        EnemyAttackDefinition.Create("penitent_delayed_overhead", EnemyAttackType.Melee, 0.45f, 2.35f, 62f, 4, 180, 72, 2, 96, 0.34f, 14f, 15f, 2.5f, 0f, 1, 0f, false, true, "penitent_delayed_overhead"),
                        EnemyAttackDefinition.Create("penitent_sweep", EnemyAttackType.Melee, 0.55f, 2.55f, 118f, 5, 160, 56, 2, 78, 0.16f, 11f, 10f, 1.9f, 0f, 1, 0f, false, false, "penitent_sweep"),
                    };
                    break;
            }
            RebuildCooldownState();
        }

        private void EnsureAttackProfile()
        {
            if (attackDefinitions == null || attackDefinitions.Length == 0)
                ApplyArchetypeDefaults();
            if (_attackCooldownUntil == null || _attackCooldownUntil.Length != attackDefinitions.Length)
                RebuildCooldownState();
        }

        private void RebuildCooldownState()
        {
            int count = attackDefinitions != null ? attackDefinitions.Length : 0;
            _attackCooldownUntil = new long[count];
        }

        private void InitializeDeterminism()
        {
            if (deterministicSeed == 0u) deterministicSeed = ComputeStableSeed();
            _rngState = deterministicSeed != 0u ? deterministicSeed : 0x6D2B79F5u;
            EnsureAttackProfile();
        }

        private uint ComputeStableSeed()
        {
            unchecked
            {
                uint hash = 2166136261u;
                string key = gameObject.name + "|" + archetype;
                for (int i = 0; i < key.Length; i++)
                {
                    hash ^= key[i];
                    hash *= 16777619u;
                }
                hash ^= (uint)Mathf.RoundToInt(transform.position.x * 10f);
                hash *= 16777619u;
                hash ^= (uint)Mathf.RoundToInt(transform.position.z * 10f);
                hash *= 16777619u;
                return hash == 0u ? 1u : hash;
            }
        }

        private int NextInt(int exclusiveMax)
        {
            if (exclusiveMax <= 1) return 0;
            unchecked
            {
                _rngState = _rngState * 1664525u + 1013904223u;
            }
            return (int)(_rngState % (uint)exclusiveMax);
        }

        private static JourneyEnemyAttackKind ToJourneyKind(EnemyAttackType type)
        {
            switch (type)
            {
                case EnemyAttackType.Melee: return JourneyEnemyAttackKind.Melee;
                case EnemyAttackType.Projectile: return JourneyEnemyAttackKind.Projectile;
                case EnemyAttackType.Burst: return JourneyEnemyAttackKind.Burst;
                case EnemyAttackType.Retreat: return JourneyEnemyAttackKind.Retreat;
                default: return JourneyEnemyAttackKind.None;
            }
        }

        private static Vector3 Planar(Vector3 value)
        {
            value.y = 0f;
            return value;
        }
    }
}
