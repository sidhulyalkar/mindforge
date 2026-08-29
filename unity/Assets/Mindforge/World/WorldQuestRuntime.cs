using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mindforge.World
{
    public enum WorldQuestConditionKind
    {
        BoolEquals = 0,
        IntAtLeast = 1,
        FloatAtLeast = 2,
        StringEquals = 3,
    }

    [Serializable]
    public sealed class WorldQuestCondition
    {
        public string state_key;
        public WorldQuestConditionKind kind;
        public bool bool_value = true;
        public int int_value;
        public float float_value;
        public string string_value;
    }

    [Serializable]
    public sealed class WorldQuestDefinition
    {
        public string id;
        public string title;
        public WorldQuestCondition[] conditions = Array.Empty<WorldQuestCondition>();
    }

    [Serializable]
    public sealed class WorldQuestProgress
    {
        public string id;
        public int satisfied;
        public int total;
        public bool completed;
    }

    /// <summary>
    /// Read-only quest/progression evaluator over semantic world state. It never executes
    /// rewards or manipulates scene objects. Concrete reward and story systems may subscribe
    /// to its semantic completion events later without making quest evaluation a god object.
    /// </summary>
    [DefaultExecutionOrder(-780)]
    public sealed class WorldQuestRuntime : MonoBehaviour
    {
        [SerializeField] private WorldStateLedger ledger;
        [SerializeField] private WorldSignalBus signals;
        [SerializeField] private WorldQuestDefinition[] definitions = Array.Empty<WorldQuestDefinition>();
        [SerializeField] private List<WorldQuestProgress> progress = new List<WorldQuestProgress>();

        private readonly Dictionary<string, WorldQuestProgress> _index =
            new Dictionary<string, WorldQuestProgress>(StringComparer.Ordinal);

        public event Action<string, int, int> QuestAdvanced;
        public event Action<string> QuestCompleted;
        public IReadOnlyList<WorldQuestProgress> Progress => progress;

        private void Awake()
        {
            Resolve();
            RebuildProgress();
        }

        private void OnEnable()
        {
            Resolve();
            if (ledger != null) ledger.StateChanged += OnStateChanged;
            EvaluateAll(false);
        }

        private void OnDisable()
        {
            if (ledger != null) ledger.StateChanged -= OnStateChanged;
        }

        public void ConfigureRuntime(
            WorldStateLedger stateLedger,
            WorldSignalBus signalBus,
            WorldQuestDefinition[] questDefinitions)
        {
            if (ledger != null) ledger.StateChanged -= OnStateChanged;
            ledger = stateLedger;
            signals = signalBus;
            definitions = questDefinitions ?? Array.Empty<WorldQuestDefinition>();
            RebuildProgress();
            if (isActiveAndEnabled && ledger != null) ledger.StateChanged += OnStateChanged;
            EvaluateAll(false);
        }

        public bool IsComplete(string questId)
        {
            return !string.IsNullOrWhiteSpace(questId) &&
                   _index.TryGetValue(questId.Trim(), out WorldQuestProgress value) &&
                   value != null && value.completed;
        }

        private void OnStateChanged(string key, WorldStateEntry before, WorldStateEntry after)
        {
            EvaluateAll(true);
        }

        private void EvaluateAll(bool emit)
        {
            if (ledger == null || definitions == null) return;
            for (int i = 0; i < definitions.Length; i++)
                Evaluate(definitions[i], emit);
        }

        private void Evaluate(WorldQuestDefinition definition, bool emit)
        {
            if (definition == null || string.IsNullOrWhiteSpace(definition.id)) return;
            string id = definition.id.Trim();
            if (!_index.TryGetValue(id, out WorldQuestProgress state) || state == null) return;
            if (state.completed) return;

            WorldQuestCondition[] conditions = definition.conditions ?? Array.Empty<WorldQuestCondition>();
            int satisfied = 0;
            for (int i = 0; i < conditions.Length; i++)
            {
                if (Satisfied(conditions[i])) satisfied++;
            }

            int before = state.satisfied;
            state.total = conditions.Length;
            state.satisfied = satisfied;
            bool complete = conditions.Length > 0 && satisfied >= conditions.Length;

            if (emit && satisfied != before)
            {
                QuestAdvanced?.Invoke(id, satisfied, state.total);
                signals?.Publish(
                    WorldSignalKind.QuestAdvanced,
                    "quest.advanced",
                    subject: id,
                    intValue: satisfied,
                    floatValue: state.total > 0 ? satisfied / (float)state.total : 0f,
                    reason: definition.title);
            }

            if (!complete) return;
            state.completed = true;
            if (emit)
            {
                QuestCompleted?.Invoke(id);
                signals?.Publish(
                    WorldSignalKind.QuestCompleted,
                    "quest.completed",
                    subject: id,
                    intValue: state.total,
                    floatValue: 1f,
                    reason: definition.title);
            }
        }

        private bool Satisfied(WorldQuestCondition condition)
        {
            if (condition == null || string.IsNullOrWhiteSpace(condition.state_key) || ledger == null) return false;
            string key = condition.state_key;
            switch (condition.kind)
            {
                case WorldQuestConditionKind.BoolEquals:
                    return ledger.TryGetBool(key, out bool b) && b == condition.bool_value;
                case WorldQuestConditionKind.IntAtLeast:
                    return ledger.TryGetInt(key, out int i) && i >= condition.int_value;
                case WorldQuestConditionKind.FloatAtLeast:
                    return ledger.TryGetFloat(key, out float f) && f >= condition.float_value;
                case WorldQuestConditionKind.StringEquals:
                    return ledger.TryGetString(key, out string s) &&
                           string.Equals(s, condition.string_value ?? string.Empty, StringComparison.Ordinal);
                default:
                    return false;
            }
        }

        private void RebuildProgress()
        {
            progress.Clear();
            _index.Clear();
            if (definitions == null) return;

            for (int i = 0; i < definitions.Length; i++)
            {
                WorldQuestDefinition definition = definitions[i];
                if (definition == null || string.IsNullOrWhiteSpace(definition.id)) continue;
                string id = definition.id.Trim();
                if (_index.ContainsKey(id)) continue;
                WorldQuestProgress state = new WorldQuestProgress
                {
                    id = id,
                    total = definition.conditions != null ? definition.conditions.Length : 0,
                };
                progress.Add(state);
                _index[id] = state;
            }
        }

        private void Resolve()
        {
            if (ledger == null) ledger = GetComponent<WorldStateLedger>();
            if (signals == null) signals = GetComponent<WorldSignalBus>();
        }
    }
}
