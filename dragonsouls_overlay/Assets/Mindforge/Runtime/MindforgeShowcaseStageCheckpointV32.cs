using States;
using UnityEngine;

namespace Mindforge.Chassis
{
    /// <summary>
    /// Non-physical spatial observer used by the showcase builder. It reports when
    /// the player comes within a bounded radius of an authored route checkpoint.
    /// No Collider, Rigidbody or gameplay-state mutation is required.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MindforgeShowcaseStageCheckpointV32 : MonoBehaviour
    {
        [SerializeField] private MindforgeShowcaseStageV32 stage;
        [SerializeField] private float activationRadius = 5.5f;
        [SerializeField] private float maximumVerticalDelta = 5f;

        private MindforgeShowcaseFlowV32 _flow;
        private PlayerStateMachine _player;
        private bool _observed;

        public MindforgeShowcaseStageV32 Stage => stage;
        public bool Observed => _observed;
        public float ActivationRadius => activationRadius;

        public void Configure(MindforgeShowcaseStageV32 configuredStage, float radius = 5.5f)
        {
            stage = configuredStage;
            activationRadius = Mathf.Max(1f, radius);
        }

        private void Start()
        {
            _flow = FindObjectOfType<MindforgeShowcaseFlowV32>(true);
            _player = FindObjectOfType<PlayerStateMachine>(true);
        }

        private void Update()
        {
            if (_observed) return;
            if (_flow == null) _flow = FindObjectOfType<MindforgeShowcaseFlowV32>(true);
            if (_player == null) _player = FindObjectOfType<PlayerStateMachine>(true);
            if (_flow == null || _player == null) return;

            Vector3 delta = _player.transform.position - transform.position;
            if (Mathf.Abs(delta.y) > maximumVerticalDelta) return;
            delta.y = 0f;
            if (delta.sqrMagnitude > activationRadius * activationRadius) return;

            _observed = true;
            _flow.ObserveStageArrival(stage);
        }
    }
}
