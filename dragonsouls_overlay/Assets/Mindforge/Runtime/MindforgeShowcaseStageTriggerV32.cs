using States;
using UnityEngine;

namespace Mindforge.Chassis
{
    /// <summary>
    /// Non-blocking spatial observer used by the showcase builder. The trigger only
    /// reports that the player reached a chapter beat. It never teleports, damages,
    /// blocks, or changes the Dragon Souls state machine.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MindforgeShowcaseStageTriggerV32 : MonoBehaviour
    {
        [SerializeField] private MindforgeShowcaseStageV32 stage;
        [SerializeField] private bool fireOnce = true;

        private MindforgeShowcaseFlowV32 _flow;
        private bool _fired;

        public MindforgeShowcaseStageV32 Stage => stage;
        public bool Fired => _fired;

        public void Configure(MindforgeShowcaseStageV32 configuredStage)
        {
            stage = configuredStage;
        }

        private void Awake()
        {
            _flow = FindObjectOfType<MindforgeShowcaseFlowV32>(true);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (fireOnce && _fired) return;
            PlayerStateMachine player = other.GetComponentInParent<PlayerStateMachine>();
            if (player == null) return;
            if (_flow == null) _flow = FindObjectOfType<MindforgeShowcaseFlowV32>(true);
            if (_flow == null) return;

            _fired = true;
            _flow.ObserveStageArrival(stage);
        }
    }
}
