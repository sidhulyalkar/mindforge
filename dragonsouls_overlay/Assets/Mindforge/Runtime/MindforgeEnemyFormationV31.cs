using States;
using UnityEngine;
using UnityEngine.AI;

namespace Mindforge.Chassis
{
    /// <summary>
    /// Adds stable approach lanes to Dragon Souls' existing chase navigation so
    /// enemies do not all request the player's exact center point. Attack states,
    /// damage, animation and CharacterController locomotion remain upstream-owned.
    /// </summary>
    [DefaultExecutionOrder(720)]
    [DisallowMultipleComponent]
    public sealed class MindforgeEnemyFormationV31 : MonoBehaviour
    {
        [SerializeField] private float meleeRingRadius = 3.45f;
        [SerializeField] private float heavyRingRadius = 4.15f;
        [SerializeField] private float rangedRingRadius = 6.25f;
        [SerializeField] private float casterRingRadius = 7.10f;
        [SerializeField] private float navMeshSampleRadius = 2.6f;
        [SerializeField] private float closeCombatReleaseRadius = 2.45f;

        private EnemyStateMachine _enemy;
        private NavMeshAgent _agent;
        private PlayerStateMachine _player;
        private float _slotAngle;
        private float _ringRadius;
        private int _slotIndex;

        public bool Installed { get; private set; }
        public int SlotIndex => _slotIndex;
        public float RingRadius => _ringRadius;

        private void Start()
        {
            _enemy = GetComponent<EnemyStateMachine>();
            _player = FindObjectOfType<PlayerStateMachine>();
            if (_enemy == null || _player == null || _enemy.navmeshAgent == null)
            {
                enabled = false;
                return;
            }

            _agent = _enemy.navmeshAgent;
            int hash = Animator.StringToHash(gameObject.name + ":" + transform.GetSiblingIndex());
            int positive = hash & 0x7fffffff;
            _slotIndex = positive % 12;
            _slotAngle = (_slotIndex / 12f) * 360f;
            _ringRadius = ResolveRingRadius(gameObject.name);

            _agent.radius = Mathf.Max(_agent.radius, 0.62f);
            _agent.stoppingDistance = Mathf.Max(_agent.stoppingDistance, 1.25f);
            _agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
            _agent.avoidancePriority = 32 + (positive % 48);
            Installed = true;
        }

        private void LateUpdate()
        {
            if (!Installed || _enemy == null || _enemy.isDead || _player == null || _agent == null) return;
            if (!_agent.enabled || !_agent.isOnNavMesh || _agent.isStopped) return;

            Vector3 toPlayer = _player.transform.position - transform.position;
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude <= closeCombatReleaseRadius * closeCombatReleaseRadius) return;

            // Rotate the formation slowly with the player's facing so approach lanes
            // remain readable instead of becoming a static world-space pinwheel.
            float playerYaw = _player.transform.eulerAngles.y;
            Quaternion slotRotation = Quaternion.Euler(0f, playerYaw + _slotAngle, 0f);
            Vector3 desired = _player.transform.position + slotRotation * (Vector3.forward * _ringRadius);

            NavMeshHit hit;
            if (!NavMesh.SamplePosition(desired, out hit, navMeshSampleRadius, _agent.areaMask)) return;

            // Do not continually churn almost-identical paths.
            Vector3 delta = _agent.destination - hit.position;
            delta.y = 0f;
            if (delta.sqrMagnitude > 0.36f)
                _agent.destination = hit.position;
        }

        private float ResolveRingRadius(string objectName)
        {
            string n = objectName.ToLowerInvariant();
            if (ContainsAny(n, "mage", "wizard", "caster", "sorcer")) return casterRingRadius;
            if (ContainsAny(n, "archer", "range", "bow")) return rangedRingRadius;
            if (ContainsAny(n, "heavy", "knight", "brute", "great")) return heavyRingRadius;
            return meleeRingRadius;
        }

        private static bool ContainsAny(string value, params string[] tokens)
        {
            for (int i = 0; i < tokens.Length; i++)
                if (value.Contains(tokens[i])) return true;
            return false;
        }
    }
}
