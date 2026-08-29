using UnityEngine;

namespace Mindforge.World
{
    /// <summary>
    /// Narrow adapter from semantic quest completion to progression rewards. Reward receipts
    /// make reconciliation idempotent across scene rebuilds and future save restores. This
    /// component never touches movement, combat, encounter scheduling, gates or neural state.
    /// </summary>
    [DefaultExecutionOrder(-770)]
    public sealed class WorldQuestRewardRuntime : MonoBehaviour
    {
        [SerializeField] private WorldQuestRuntime quests;
        [SerializeField] private PlayerProgressionLedger progression;
        [SerializeField] private WorldSignalBus signals;

        private bool _subscribed;

        public void ConfigureRuntime(
            WorldQuestRuntime questRuntime,
            PlayerProgressionLedger progressionLedger,
            WorldSignalBus signalBus)
        {
            Unsubscribe();
            quests = questRuntime;
            progression = progressionLedger;
            signals = signalBus;
            Subscribe();
            ReconcileCompletedQuests();
        }

        private void Awake() => Resolve();

        private void OnEnable()
        {
            Resolve();
            Subscribe();
            ReconcileCompletedQuests();
        }

        private void OnDisable() => Unsubscribe();

        private void Resolve()
        {
            if (quests == null) quests = GetComponent<WorldQuestRuntime>();
            if (progression == null) progression = GetComponent<PlayerProgressionLedger>();
            if (signals == null) signals = GetComponent<WorldSignalBus>();
        }

        private void Subscribe()
        {
            if (_subscribed) return;
            Resolve();
            if (quests != null) quests.QuestCompleted += OnQuestCompleted;
            if (progression != null) progression.SnapshotRestored += ReconcileCompletedQuests;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            if (quests != null) quests.QuestCompleted -= OnQuestCompleted;
            if (progression != null) progression.SnapshotRestored -= ReconcileCompletedQuests;
            _subscribed = false;
        }

        private void OnQuestCompleted(string questId) => GrantQuestRewards(questId);

        private void ReconcileCompletedQuests()
        {
            if (quests == null || progression == null || quests.Progress == null) return;
            for (int i = 0; i < quests.Progress.Count; i++)
            {
                WorldQuestProgress state = quests.Progress[i];
                if (state != null && state.completed) GrantQuestRewards(state.id);
            }
        }

        private void GrantQuestRewards(string questId)
        {
            if (quests == null || progression == null || string.IsNullOrWhiteSpace(questId)) return;
            WorldQuestDefinition definition = quests.GetDefinition(questId);
            if (definition == null || !progression.TryClaimRewardReceipt(questId)) return;

            WorldQuestRewardDefinition[] rewards = definition.rewards ?? System.Array.Empty<WorldQuestRewardDefinition>();
            int granted = 0;
            for (int i = 0; i < rewards.Length; i++)
            {
                if (progression.Grant(rewards[i], "quest:" + questId)) granted++;
            }

            signals?.Publish(
                WorldSignalKind.RewardGranted,
                "quest.reward_granted",
                subject: questId,
                intValue: granted,
                floatValue: rewards.Length > 0 ? granted / (float)rewards.Length : 1f,
                reason: definition.title);
        }
    }
}
