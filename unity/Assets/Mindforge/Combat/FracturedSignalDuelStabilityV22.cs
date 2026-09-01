using System.Reflection;
using Mindforge.Neural;
using Mindforge.SoulWisp;
using UnityEngine;

namespace Mindforge.Combat
{
    /// <summary>
    /// V0.22 stability/combat-contact layer for the first Fractured Signal encounter.
    ///
    /// FracturedSignalFirstBossV19 remains the normal locomotion owner and
    /// FracturedSignalDirector remains the attack scheduler. This component only applies a
    /// one-time profile, adds a trigger-only sword contact hull, and performs exceptional
    /// recovery when the boss is provably outside the authored chamber or stationary after an
    /// attack commitment long enough to qualify as a locomotion stall.
    ///
    /// A stale external pause is repaired only after encounter entry and only when neither the
    /// Wisp intermission nor neural-link safety system owns the pause.
    /// </summary>
    [DefaultExecutionOrder(-92)]
    [RequireComponent(typeof(FracturedSignalDirector))]
    [RequireComponent(typeof(CombatantVitals))]
    public sealed class FracturedSignalDuelStabilityV22 : MonoBehaviour
    {
        private const float ArenaCenterZ = 94f;

        [Header("Playable chamber")]
        [SerializeField] private float playableHalfX = 14.8f;
        [SerializeField] private float playableHalfZ = 13.9f;
        [SerializeField] private float encounterReleaseZ = 82f;
        [SerializeField] private float pauseRepairDelay = 0.45f;

        [Header("Stall recovery")]
        [SerializeField] private float stallWindowSeconds = 0.85f;
        [SerializeField] private float stallDistanceEpsilon = 0.055f;
        [SerializeField] private float recoveryNudge = 0.95f;
        [SerializeField] private float hardBoundaryMargin = 1.0f;

        private readonly Collider[] _overlap = new Collider[24];
        private FracturedSignalDirector _director;
        private FracturedSignalFirstBossV19 _movement;
        private FracturedSignalMeleeDirector _melee;
        private CombatantVitals _vitals;
        private Rigidbody _body;
        private GuardianCombatInput _guardianInput;
        private Transform _player;
        private WispCombatIntermissionV19 _wispIntermission;
        private NeuralLinkContingency _linkContingency;
        private float _homeY;
        private Vector3 _lastPosition;
        private float _stallSeconds;
        private float _pauseRepairSeconds;
        private float _commitUntil;
        private int _stallRecoveries;
        private bool _profileAttempted;

        public int StallRecoveries => _stallRecoveries;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            FracturedSignalDirector[] bosses = FindObjectsOfType<FracturedSignalDirector>(true);
            for (int i = 0; i < bosses.Length; i++)
            {
                FracturedSignalDirector boss = bosses[i];
                if (boss != null && boss.GetComponent<FracturedSignalDuelStabilityV22>() == null)
                    boss.gameObject.AddComponent<FracturedSignalDuelStabilityV22>();
            }
        }

        private void Awake()
        {
            Resolve();
            _homeY = transform.position.y;
            _lastPosition = transform.position;
        }

        private void Start()
        {
            Resolve();
            ApplyProfiles();
            EnsureCombatHull();
            _homeY = transform.position.y;
            _lastPosition = transform.position;
        }

        private void OnEnable()
        {
            Resolve();
            if (_director != null)
            {
                _director.AttackTelegraphed += OnAttackTelegraphed;
                _director.AttackFired += OnAttackFired;
            }
        }

        private void OnDisable()
        {
            if (_director != null)
            {
                _director.AttackTelegraphed -= OnAttackTelegraphed;
                _director.AttackFired -= OnAttackFired;
            }
            _stallSeconds = 0f;
            _pauseRepairSeconds = 0f;
        }

        private void FixedUpdate()
        {
            Resolve();
            ApplyProfiles();
            if (_director == null || _vitals == null || !_vitals.IsAlive) return;

            RepairImpossibleBoundaryState();
            RepairStaleExternalPause();
            RecoverFromLocomotionStall();
            _lastPosition = transform.position;
        }

        private void Resolve()
        {
            if (_director == null) _director = GetComponent<FracturedSignalDirector>();
            if (_movement == null) _movement = GetComponent<FracturedSignalFirstBossV19>();
            if (_melee == null) _melee = GetComponent<FracturedSignalMeleeDirector>();
            if (_vitals == null) _vitals = GetComponent<CombatantVitals>();
            if (_body == null) _body = GetComponent<Rigidbody>();
            if (_guardianInput == null) _guardianInput = FindObjectOfType<GuardianCombatInput>(true);
            if (_guardianInput != null && _player == null) _player = _guardianInput.transform;
            if (_wispIntermission == null) _wispIntermission = FindObjectOfType<WispCombatIntermissionV19>(true);
            if (_linkContingency == null) _linkContingency = FindObjectOfType<NeuralLinkContingency>(true);
        }

        private void ApplyProfiles()
        {
            if (_profileAttempted) return;
            if (_movement == null || _director == null)
            {
                Resolve();
                if (_movement == null || _director == null) return;
            }
            _profileAttempted = true;

            bool movementOk =
                CanSet<FracturedSignalFirstBossV19, float>("phaseOnePreferredDistance") &
                CanSet<FracturedSignalFirstBossV19, float>("phaseTwoPreferredDistance") &
                CanSet<FracturedSignalFirstBossV19, float>("phaseThreePreferredDistance") &
                CanSet<FracturedSignalFirstBossV19, float>("distanceBand") &
                CanSet<FracturedSignalFirstBossV19, float>("phaseOneMoveSpeed") &
                CanSet<FracturedSignalFirstBossV19, float>("phaseTwoMoveSpeed") &
                CanSet<FracturedSignalFirstBossV19, float>("phaseThreeMoveSpeed") &
                CanSet<FracturedSignalFirstBossV19, float>("retreatMultiplier") &
                CanSet<FracturedSignalFirstBossV19, float>("orbitBias") &
                CanSet<FracturedSignalFirstBossV19, float>("turnSharpness") &
                CanSet<FracturedSignalFirstBossV19, float>("homeLeashRadius") &
                CanSet<FracturedSignalFirstBossV19, float>("collisionProbeRadius") &
                CanSet<FracturedSignalFirstBossV19, float>("postAttackRecovery") &
                CanSet<FracturedSignalFirstBossV19, float>("orbitSideHoldSeconds");

            bool cadenceOk =
                CanSet<FracturedSignalDirector, float>("phaseOneInterval") &
                CanSet<FracturedSignalDirector, float>("phaseTwoInterval") &
                CanSet<FracturedSignalDirector, float>("phaseThreeInterval") &
                CanSet<FracturedSignalDirector, float>("phaseOneTelegraph") &
                CanSet<FracturedSignalDirector, float>("phaseTwoTelegraph") &
                CanSet<FracturedSignalDirector, float>("phaseThreeTelegraph") &
                CanSet<FracturedSignalDirector, int>("radialCount") &
                CanSet<FracturedSignalDirector, int>("maxEchoes");

            if (!movementOk || !cadenceOk)
            {
                Debug.LogError("[Mindforge:BossV22] Boss field contract changed; V0.22 duel profile applied nothing.");
                return;
            }

            // V0.21 built an 18.3 m wall ring but left a 9 m leash. V0.22 lets the boss use
            // most of the chamber while retaining a safe buffer from the wall shell.
            Set(_movement, "phaseOnePreferredDistance", 5.55f);
            Set(_movement, "phaseTwoPreferredDistance", 6.20f);
            Set(_movement, "phaseThreePreferredDistance", 5.45f);
            Set(_movement, "distanceBand", 1.05f);
            Set(_movement, "phaseOneMoveSpeed", 1.88f);
            Set(_movement, "phaseTwoMoveSpeed", 2.24f);
            Set(_movement, "phaseThreeMoveSpeed", 2.58f);
            Set(_movement, "retreatMultiplier", 0.92f);
            Set(_movement, "orbitBias", 0.86f);
            Set(_movement, "turnSharpness", 8.5f);
            Set(_movement, "homeLeashRadius", 14.2f);
            Set(_movement, "collisionProbeRadius", 0.58f);
            Set(_movement, "postAttackRecovery", 0.42f);
            Set(_movement, "orbitSideHoldSeconds", 1.55f);

            // First encounter teaches sword/dodge/spacing. It should not become projectile soup.
            Set(_director, "phaseOneInterval", 2.35f);
            Set(_director, "phaseTwoInterval", 2.02f);
            Set(_director, "phaseThreeInterval", 1.72f);
            Set(_director, "phaseOneTelegraph", 0.86f);
            Set(_director, "phaseTwoTelegraph", 0.76f);
            Set(_director, "phaseThreeTelegraph", 0.66f);
            Set(_director, "radialCount", 6);
            Set(_director, "maxEchoes", 1);

            ApplyMeleeProfile();
            Debug.Log(
                "[Mindforge:BossV22] Duel profile applied: full chamber leash, lower projectile clutter, " +
                "clearer melee tells, reliable sword hull and post-commit stall recovery.");
        }

        private void ApplyMeleeProfile()
        {
            if (_melee == null) return;
            bool fieldsAvailable =
                CanSet<FracturedSignalMeleeDirector, float>("engageDistance") &
                CanSet<FracturedSignalMeleeDirector, float>("cleaveRange") &
                CanSet<FracturedSignalMeleeDirector, float>("cleaveArcDegrees") &
                CanSet<FracturedSignalMeleeDirector, float>("cleaveTelegraphPhaseOne") &
                CanSet<FracturedSignalMeleeDirector, float>("cleaveTelegraphPhaseTwo") &
                CanSet<FracturedSignalMeleeDirector, float>("cleaveTelegraphPhaseThree") &
                CanSet<FracturedSignalMeleeDirector, float>("slamRadius") &
                CanSet<FracturedSignalMeleeDirector, float>("slamTelegraphPhaseTwo") &
                CanSet<FracturedSignalMeleeDirector, float>("slamTelegraphPhaseThree");
            if (!fieldsAvailable)
            {
                Debug.LogWarning("[Mindforge:BossV22] Melee field contract changed; retained existing melee tuning.");
                return;
            }

            Set(_melee, "engageDistance", 5.65f);
            Set(_melee, "cleaveRange", 3.55f);
            Set(_melee, "cleaveArcDegrees", 126f);
            Set(_melee, "cleaveTelegraphPhaseOne", 0.90f);
            Set(_melee, "cleaveTelegraphPhaseTwo", 0.78f);
            Set(_melee, "cleaveTelegraphPhaseThree", 0.68f);
            Set(_melee, "slamRadius", 2.85f);
            Set(_melee, "slamTelegraphPhaseTwo", 0.96f);
            Set(_melee, "slamTelegraphPhaseThree", 0.82f);
        }

        private void EnsureCombatHull()
        {
            if (transform.Find("V22_BossCombatHull") != null) return;

            GameObject hull = new GameObject("V22_BossCombatHull");
            hull.layer = gameObject.layer;
            hull.transform.SetParent(transform, false);
            CapsuleCollider collider = hull.AddComponent<CapsuleCollider>();
            collider.isTrigger = true;
            collider.direction = 1;
            collider.center = new Vector3(0f, 1.48f, 0f);
            collider.radius = 1.08f;
            collider.height = 3.15f;
        }

        private void OnAttackTelegraphed(string pattern, int count, bool heavy)
        {
            _commitUntil = Mathf.Max(_commitUntil, Time.unscaledTime + (heavy ? 1.18f : 1.02f));
            _stallSeconds = 0f;
        }

        private void OnAttackFired(string pattern, int count, bool heavy)
        {
            _commitUntil = Mathf.Max(_commitUntil, Time.unscaledTime + (heavy ? 0.68f : 0.52f));
            _stallSeconds = 0f;
        }

        private void RepairImpossibleBoundaryState()
        {
            Vector3 p = transform.position;
            Vector2 normalized = new Vector2(
                p.x / Mathf.Max(1f, playableHalfX),
                (p.z - ArenaCenterZ) / Mathf.Max(1f, playableHalfZ));
            float hardLimit = 1f + Mathf.Max(0.05f, hardBoundaryMargin) / Mathf.Min(playableHalfX, playableHalfZ);
            bool outside = normalized.sqrMagnitude > hardLimit * hardLimit;
            bool badY = p.y < _homeY - 2.0f || p.y > _homeY + 3.0f;
            if (!outside && !badY) return;

            Vector2 dir = normalized.sqrMagnitude > 0.001f ? normalized.normalized : Vector2.up;
            Vector3 repaired = new Vector3(
                dir.x * playableHalfX * 0.82f,
                _homeY,
                ArenaCenterZ + dir.y * playableHalfZ * 0.82f);
            MoveExceptional(repaired);
            KickOrbitRecovery();
            _stallRecoveries++;
            Debug.LogWarning("[Mindforge:BossV22] Recovered Fractured Signal from outside the authored combat chamber.");
        }

        private void RepairStaleExternalPause()
        {
            if (_player == null || _director == null || !_director.ExternalPaused || _player.position.z < encounterReleaseZ)
            {
                _pauseRepairSeconds = 0f;
                return;
            }

            bool wispOwnsPause = _wispIntermission != null && _wispIntermission.Active;
            bool safetyOwnsPause = _linkContingency != null &&
                                   (_linkContingency.Degraded || _linkContingency.ParticipantStopped);
            if (wispOwnsPause || safetyOwnsPause)
            {
                _pauseRepairSeconds = 0f;
                return;
            }

            _pauseRepairSeconds += Time.fixedDeltaTime;
            if (_pauseRepairSeconds < Mathf.Max(0.2f, pauseRepairDelay)) return;

            _director.SetExternalPause(false);
            if (_guardianInput != null && !_guardianInput.CombatActionsEnabled)
                _guardianInput.SetCombatActionsEnabled(true);
            _pauseRepairSeconds = 0f;
            Debug.LogWarning(
                "[Mindforge:BossV22] Cleared stale boss pause after encounter entry; no Wisp or neural-safety owner remained.");
        }

        private void RecoverFromLocomotionStall()
        {
            if (_movement == null || !_movement.enabled || _player == null || _director == null) return;
            if (_director.ExternalPaused || (_vitals.Poise != null && _vitals.Poise.Broken))
            {
                _stallSeconds = 0f;
                return;
            }
            if (Time.unscaledTime < _commitUntil)
            {
                _stallSeconds = 0f;
                return;
            }

            Vector3 flatDelta = Vector3.ProjectOnPlane(transform.position - _lastPosition, Vector3.up);
            Vector3 toPlayer = Vector3.ProjectOnPlane(_player.position - transform.position, Vector3.up);
            if (toPlayer.magnitude < 2.4f || flatDelta.magnitude > Mathf.Max(0.01f, stallDistanceEpsilon))
            {
                _stallSeconds = 0f;
                return;
            }

            _stallSeconds += Time.fixedDeltaTime;
            if (_stallSeconds < Mathf.Max(0.35f, stallWindowSeconds)) return;
            _stallSeconds = 0f;

            KickOrbitRecovery();
            Vector3 inward = new Vector3(-transform.position.x, 0f, ArenaCenterZ - transform.position.z);
            if (inward.sqrMagnitude < 0.001f) inward = -transform.right;
            inward.Normalize();
            Vector3 tangent = Vector3.Cross(Vector3.up, inward).normalized;

            Vector3 candidateA = transform.position +
                                 (inward * 0.72f + tangent * 0.69f).normalized * recoveryNudge;
            candidateA.y = _homeY;
            Vector3 candidateB = transform.position +
                                 (inward * 0.72f - tangent * 0.69f).normalized * recoveryNudge;
            candidateB.y = _homeY;

            if (CandidateClear(candidateA)) MoveExceptional(candidateA);
            else if (CandidateClear(candidateB)) MoveExceptional(candidateB);

            _stallRecoveries++;
            Debug.LogWarning("[Mindforge:BossV22] Recovered a post-commit locomotion stall and reversed orbit preference.");
        }

        private bool CandidateClear(Vector3 candidate)
        {
            int count = Physics.OverlapSphereNonAlloc(
                candidate + Vector3.up * 1.05f,
                0.54f,
                _overlap,
                ~0,
                QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                Collider hit = _overlap[i];
                if (hit == null) continue;
                Transform t = hit.transform;
                if (t == transform || t.IsChildOf(transform) || transform.IsChildOf(t)) continue;
                if (hit.GetComponentInParent<MindforgeProjectile>() != null) continue;
                if (hit.GetComponentInParent<FracturedEchoNode>() != null) continue;
                return false;
            }
            return true;
        }

        private void MoveExceptional(Vector3 position)
        {
            if (_body != null && _body.isKinematic) _body.MovePosition(position);
            else transform.position = position;
        }

        private void KickOrbitRecovery()
        {
            if (_movement == null) return;
            FieldInfo orbit = typeof(FracturedSignalFirstBossV19).GetField(
                "_orbitSide", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo nextSwap = typeof(FracturedSignalFirstBossV19).GetField(
                "_nextOrbitSwap", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo holdUntil = typeof(FracturedSignalFirstBossV19).GetField(
                "_holdUntil", BindingFlags.Instance | BindingFlags.NonPublic);

            if (orbit != null && orbit.FieldType == typeof(float))
            {
                float current = (float)orbit.GetValue(_movement);
                orbit.SetValue(_movement, current == 0f ? -1f : -current);
            }
            if (nextSwap != null && nextSwap.FieldType == typeof(float))
                nextSwap.SetValue(_movement, Time.unscaledTime + 0.75f);
            if (holdUntil != null && holdUntil.FieldType == typeof(float) && Time.unscaledTime >= _commitUntil)
                holdUntil.SetValue(_movement, Time.unscaledTime);
        }

        private static bool CanSet<TOwner, TValue>(string fieldName)
        {
            FieldInfo field = typeof(TOwner).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            return field != null && field.FieldType == typeof(TValue);
        }

        private static void Set<TOwner, TValue>(TOwner owner, string fieldName, TValue value)
        {
            FieldInfo field = typeof(TOwner).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(owner, value);
        }
    }
}