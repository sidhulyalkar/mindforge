using System.Reflection;
using Mindforge.SoulWisp;
using UnityEngine;

namespace Mindforge.Combat
{
    /// <summary>
    /// First-boss locomotion/cadence layer for The Fractured Signal.
    ///
    /// The legacy FracturedSignalDirector remains the sole attack scheduler. This layer only
    /// retunes its serialized cadence and moves the existing authoritative kinematic body
    /// between attack commitments so the encounter reads as a creature rather than a turret.
    ///
    /// Movement freezes whenever the boss is externally paused, poise-broken, attacking, or a
    /// Wisp calibration/resonance visual field is active. Neural evidence never chooses movement.
    /// </summary>
    [DefaultExecutionOrder(-95)]
    [RequireComponent(typeof(FracturedSignalDirector))]
    [RequireComponent(typeof(CombatantVitals))]
    public sealed class FracturedSignalFirstBossV19 : MonoBehaviour
    {
        [Header("First-encounter cadence")]
        [SerializeField] private float phaseOneInterval = 2.15f;
        [SerializeField] private float phaseTwoInterval = 1.78f;
        [SerializeField] private float phaseThreeInterval = 1.48f;
        [SerializeField] private float phaseOneTelegraph = 0.76f;
        [SerializeField] private float phaseTwoTelegraph = 0.66f;
        [SerializeField] private float phaseThreeTelegraph = 0.58f;
        [SerializeField] private int radialCount = 7;
        [SerializeField] private int maxEchoes = 2;

        [Header("Duel movement")]
        [SerializeField] private float phaseOnePreferredDistance = 4.35f;
        [SerializeField] private float phaseTwoPreferredDistance = 5.10f;
        [SerializeField] private float phaseThreePreferredDistance = 4.20f;
        [SerializeField] private float distanceBand = 0.72f;
        [SerializeField] private float phaseOneMoveSpeed = 1.75f;
        [SerializeField] private float phaseTwoMoveSpeed = 2.15f;
        [SerializeField] private float phaseThreeMoveSpeed = 2.55f;
        [SerializeField] private float retreatMultiplier = 0.72f;
        [SerializeField] private float orbitBias = 0.72f;
        [SerializeField] private float turnSharpness = 6.5f;
        [SerializeField] private float homeLeashRadius = 5.4f;
        [SerializeField] private float collisionProbeRadius = 0.95f;
        [SerializeField] private float postAttackRecovery = 0.62f;
        [SerializeField] private float orbitSideHoldSeconds = 3.2f;

        private readonly Collider[] _overlap = new Collider[20];
        private FracturedSignalDirector _director;
        private CombatantVitals _vitals;
        private Rigidbody _body;
        private Transform _player;
        private SoulWispController _wisp;
        private Vector3 _home;
        private float _holdUntil;
        private float _nextOrbitSwap;
        private float _orbitSide = 1f;
        private bool _cadenceApplied;

        public bool MovementActive { get; private set; }
        public float CurrentMoveSpeed { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            FracturedSignalDirector[] bosses = FindObjectsOfType<FracturedSignalDirector>(true);
            for (int i = 0; i < bosses.Length; i++)
            {
                FracturedSignalDirector boss = bosses[i];
                if (boss != null && boss.GetComponent<FracturedSignalFirstBossV19>() == null)
                    boss.gameObject.AddComponent<FracturedSignalFirstBossV19>();
            }
        }

        private void Awake()
        {
            _director = GetComponent<FracturedSignalDirector>();
            _vitals = GetComponent<CombatantVitals>();
            _body = GetComponent<Rigidbody>();
            _home = transform.position;
            ResolveRuntimeReferences();
            ApplyCadenceProfile();
        }

        private void OnEnable()
        {
            if (_director == null) _director = GetComponent<FracturedSignalDirector>();
            if (_director != null)
            {
                _director.AttackTelegraphed += OnAttackTelegraphed;
                _director.AttackFired += OnAttackFired;
            }
            _home = transform.position;
            _nextOrbitSwap = Time.unscaledTime + Mathf.Max(1.2f, orbitSideHoldSeconds);
        }

        private void OnDisable()
        {
            if (_director != null)
            {
                _director.AttackTelegraphed -= OnAttackTelegraphed;
                _director.AttackFired -= OnAttackFired;
            }
            MovementActive = false;
            CurrentMoveSpeed = 0f;
        }

        private void FixedUpdate()
        {
            ResolveRuntimeReferences();
            ApplyCadenceProfile();

            if (!MovementAuthorityAvailable())
            {
                MovementActive = false;
                CurrentMoveSpeed = 0f;
                return;
            }

            if (Time.unscaledTime < _holdUntil)
            {
                FacePlayer();
                MovementActive = false;
                CurrentMoveSpeed = 0f;
                return;
            }

            if (Time.unscaledTime >= _nextOrbitSwap)
            {
                _orbitSide *= -1f;
                _nextOrbitSwap = Time.unscaledTime + Mathf.Max(1.2f, orbitSideHoldSeconds);
            }

            Vector3 toPlayer = Vector3.ProjectOnPlane(_player.position - transform.position, Vector3.up);
            float distance = toPlayer.magnitude;
            if (distance < 0.001f) return;
            Vector3 toward = toPlayer / distance;
            Vector3 lateral = Vector3.Cross(Vector3.up, toward) * _orbitSide;

            float preferred = PreferredDistance();
            float band = Mathf.Max(0.25f, distanceBand);
            Vector3 desiredDirection;
            float speed = MoveSpeed();

            if (distance > preferred + band)
            {
                desiredDirection = Vector3.Slerp(toward, lateral, 0.24f).normalized;
            }
            else if (distance < preferred - band)
            {
                desiredDirection = Vector3.Slerp(-toward, lateral, 0.24f).normalized;
                speed *= Mathf.Clamp(retreatMultiplier, 0.3f, 1f);
            }
            else
            {
                desiredDirection = Vector3.Slerp(lateral, toward, Mathf.Clamp01(1f - orbitBias)).normalized;
                speed *= 0.78f;
            }

            Vector3 candidate = transform.position + desiredDirection * speed * Time.fixedDeltaTime;
            candidate.y = _home.y;

            Vector3 fromHome = Vector3.ProjectOnPlane(candidate - _home, Vector3.up);
            float leash = Mathf.Max(2f, homeLeashRadius);
            if (fromHome.magnitude > leash)
            {
                Vector3 homeward = Vector3.ProjectOnPlane(_home - transform.position, Vector3.up);
                if (homeward.sqrMagnitude > 0.001f)
                    candidate = transform.position + homeward.normalized * speed * Time.fixedDeltaTime;
            }

            if (!PositionClear(candidate))
            {
                Vector3 alternate = transform.position - lateral * speed * Time.fixedDeltaTime;
                alternate.y = _home.y;
                if (PositionClear(alternate)) candidate = alternate;
                else candidate = transform.position;
            }

            MovementActive = Vector3.Distance(candidate, transform.position) > 0.0005f;
            CurrentMoveSpeed = MovementActive ? speed : 0f;
            if (_body != null && _body.isKinematic) _body.MovePosition(candidate);
            else transform.position = candidate;

            FacePlayer();
        }

        private void FacePlayer()
        {
            if (_player == null) return;
            Vector3 direction = Vector3.ProjectOnPlane(_player.position - transform.position, Vector3.up);
            if (direction.sqrMagnitude < 0.001f) return;
            Quaternion desired = Quaternion.LookRotation(direction.normalized, Vector3.up);
            float response = 1f - Mathf.Exp(-Mathf.Max(0.1f, turnSharpness) * Time.fixedDeltaTime);
            Quaternion rotation = Quaternion.Slerp(transform.rotation, desired, response);
            if (_body != null && _body.isKinematic) _body.MoveRotation(rotation);
            else transform.rotation = rotation;
        }

        private bool MovementAuthorityAvailable()
        {
            if (_director == null || _vitals == null || !_vitals.IsAlive || _player == null) return false;
            if (_director.ExternalPaused) return false;
            if (_vitals.Poise != null && _vitals.Poise.Broken) return false;
            if (NeuralVisualFieldActive()) return false;
            return true;
        }

        private bool NeuralVisualFieldActive()
        {
            if (_wisp == null) _wisp = FindObjectOfType<SoulWispController>(true);
            return _wisp != null && (_wisp.CalibrationStimuliActive || _wisp.ResonanceWindowActive);
        }

        private bool PositionClear(Vector3 candidate)
        {
            int count = Physics.OverlapSphereNonAlloc(
                candidate + Vector3.up * 1.05f,
                Mathf.Max(0.35f, collisionProbeRadius),
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

        private float PreferredDistance()
        {
            int phase = _director != null ? _director.Phase : 1;
            return phase <= 1 ? phaseOnePreferredDistance : phase == 2 ? phaseTwoPreferredDistance : phaseThreePreferredDistance;
        }

        private float MoveSpeed()
        {
            int phase = _director != null ? _director.Phase : 1;
            return Mathf.Max(0.2f, phase <= 1 ? phaseOneMoveSpeed : phase == 2 ? phaseTwoMoveSpeed : phaseThreeMoveSpeed);
        }

        private float TelegraphDuration()
        {
            int phase = _director != null ? _director.Phase : 1;
            return phase <= 1 ? phaseOneTelegraph : phase == 2 ? phaseTwoTelegraph : phaseThreeTelegraph;
        }

        private void OnAttackTelegraphed(string pattern, int count, bool heavy)
        {
            float commitment = Mathf.Max(0.20f, TelegraphDuration()) + 0.08f + (heavy ? 0.10f : 0f);
            _holdUntil = Mathf.Max(_holdUntil, Time.unscaledTime + commitment);
        }

        private void OnAttackFired(string pattern, int count, bool heavy)
        {
            float recovery = Mathf.Max(0.20f, postAttackRecovery) + (heavy ? 0.18f : 0f);
            _holdUntil = Mathf.Max(_holdUntil, Time.unscaledTime + recovery);
        }

        private void ResolveRuntimeReferences()
        {
            if (_director == null) _director = GetComponent<FracturedSignalDirector>();
            if (_vitals == null) _vitals = GetComponent<CombatantVitals>();
            if (_body == null) _body = GetComponent<Rigidbody>();
            if (_player == null)
            {
                GuardianCombatInput input = FindObjectOfType<GuardianCombatInput>(true);
                if (input != null) _player = input.transform;
            }
            if (_wisp == null) _wisp = FindObjectOfType<SoulWispController>(true);
        }

        private void ApplyCadenceProfile()
        {
            if (_cadenceApplied || _director == null) return;
            bool ok =
                SetPrivate("phaseOneInterval", phaseOneInterval) &
                SetPrivate("phaseTwoInterval", phaseTwoInterval) &
                SetPrivate("phaseThreeInterval", phaseThreeInterval) &
                SetPrivate("phaseOneTelegraph", phaseOneTelegraph) &
                SetPrivate("phaseTwoTelegraph", phaseTwoTelegraph) &
                SetPrivate("phaseThreeTelegraph", phaseThreeTelegraph) &
                SetPrivate("radialCount", radialCount) &
                SetPrivate("maxEchoes", maxEchoes);

            _cadenceApplied = ok;
            if (!ok)
                Debug.LogError("[Mindforge:BossV19] FracturedSignalDirector cadence fields changed; V19 profile refused partial application.");
        }

        private bool SetPrivate<T>(string fieldName, T value)
        {
            FieldInfo field = typeof(FracturedSignalDirector).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null || !field.FieldType.IsAssignableFrom(typeof(T))) return false;
            field.SetValue(_director, value);
            return true;
        }
    }
}
