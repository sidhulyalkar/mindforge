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
    public sealed class WorldQuestStepDefinition
    {
        public string id;
        public string title;
        [TextArea] public string description;
        public WorldQuestCondition[] conditions = Array.Empty<WorldQuestCondition>();
    }

    [Serializable]
    public sealed class WorldQuestDefinition
    {
        public string id;
        public string title;
        [TextArea] public string description;
        public int sort_order;
        public string[] prerequisite_ids = Array.Empty<string>();
        public WorldQuestStepDefinition[] steps = Array.Empty<WorldQuestStepDefinition>();
        public WorldQuestRewardDefinition[] rewards = Array.Empty<WorldQuestRewardDefinition>();
    }

    [Serializable]
    public sealed class WorldQuestProgress
    {
        public string id;
        public bool active;
        public int current_step;
        public int completed_steps;
        public int total_steps;
        public bool completed;
    }

    /// <summary>
    /// Ordered read-only quest evaluator over semantic world state. Quest evaluation never
    /// executes rewards, spawns content, opens gates, changes combat, or reads neural state.
    /// Progress is monotonic during a run: once a step is satisfied it does not regress when
    /// a transient world fact later changes. A full ConfigureRuntime/snapshot restore rebuilds
    /// derived progress from durable semantic facts and prerequisite completion.
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
        private readonly Dictionary<string, WorldQuestDefinition> _definitions =
            new Dictionary<string, WorldQuestDefinition>(StringComparer.Ordinal);

        public event Action<string> QuestActivated;
        public event Action<string, int, int> QuestAdvanced;
        public event Action<string> QuestCompleted;

        public IReadOnlyList<WorldQuestProgress> Progress => progress;
        public IReadOnlyList<WorldQuestDefinition> Definitions => definitions;

        private void Awake()
        {
            Resolve();
            RebuildProgress();
        }

        private void OnEnable()
        {
            Resolve();
            SubscribeLedger();
            RebuildFromWorld(false);
        }

        private void OnDisable() => UnsubscribeLedger();

        public void ConfigureRuntime(
            WorldStateLedger stateLedger,
            WorldSignalBus signalBus,
            WorldQuestDefinition[] questDefinitions)
        {
            UnsubscribeLedger();
            ledger = stateLedger;
            signals = signalBus;
            definitions = questDefinitions ?? Array.Empty<WorldQuestDefinition>();
            SortDefinitions();
            RebuildProgress();
            if (isActiveAndEnabled) SubscribeLedger();
            RebuildFromWorld(false);
        }

        public bool IsComplete(string questId)
        {
            string id = Normalize(questId);
            return !string.IsNullOrEmpty(id) &&
                   _index.TryGetValue(id, out WorldQuestProgress value) &&
                   value != null && value.completed;
        }

        public bool IsActive(string questId)
        {
            string id = Normalize(questId);
            return !string.IsNullOrEmpty(id) &&
                   _index.TryGetValue(id, out WorldQuestProgress value) &&
                   value != null && value.active && !value.completed;
        }

        public WorldQuestDefinition GetDefinition(string questId)
        {
            string id = Normalize(questId);
            return !string.IsNullOrEmpty(id) && _definitions.TryGetValue(id, out WorldQuestDefinition value)
                ? value
                : null;
        }

        public WorldQuestProgress GetProgress(string questId)
        {
            string id = Normalize(questId);
            return !string.IsNullOrEmpty(id) && _index.TryGetValue(id, out WorldQuestProgress value)
                ? value
                : null;
        }

        public WorldQuestDefinition GetPrimaryActiveQuest()
        {
            for (int i = 0; i < definitions.Length; i++)
            {
                WorldQuestDefinition definition = definitions[i];
                if (definition != null && IsActive(definition.id)) return definition;
            }
            return null;
        }

        public WorldQuestStepDefinition GetCurrentStep(string questId)
        {
            WorldQuestDefinition definition = GetDefinition(questId);
            WorldQuestProgress state = GetProgress(questId);
            if (definition == null || state == null || definition.steps == null || definition.steps.Length == 0) return null;
            int index = Mathf.Clamp(state.current_step, 0, definition.steps.Length - 1);
            return state.completed ? null : definition.steps[index];
        }

        private void OnStateChanged(string key, WorldStateEntry before, WorldStateEntry after)
            => EvaluateAll(true);

        private void OnSnapshotRestored()
            => RebuildFromWorld(false);

        private void RebuildFromWorld(bool emit)
        {
            RebuildProgress();
            EvaluateAll(emit);
        }

        private void EvaluateAll(bool emit)
        {
            if (ledger == null || definitions == null) return;

            // Bounded convergence handles prerequisite chains without requiring authored
            // definitions to be topologically sorted. Cycles simply never activate.
            int passes = Mathf.Max(1, definitions.Length + 1);
            for (int pass = 0; pass < passes; pass++)
            {
                bool changed = false;
                for (int i = 0; i < definitions.Length; i++)
                    changed |= Evaluate(definitions[i], emit);
                if (!changed) break;
            }
        }

        private bool Evaluate(WorldQuestDefinition definition, bool emit)
        {
            if (definition == null || string.IsNullOrWhiteSpace(definition.id)) return false;
            string id = Normalize(definition.id);
            if (!_index.TryGetValue(id, out WorldQuestProgress state) || state == null) return false;
            if (state.completed) return false;

            bool changed = false;
            if (!state.active)
            {
                if (!PrerequisitesSatisfied(definition)) return false;
                state.active = true;
                changed = true;
                if (emit)
                {
                    QuestActivated?.Invoke(id);
                    signals?.Publish(
                        WorldSignalKind.QuestActivated,
                        "quest.activated",
                        subject: id,
                        reason: definition.title);
                }
            }

            WorldQuestStepDefinition[] steps = definition.steps ?? Array.Empty<WorldQuestStepDefinition>();
            state.total_steps = steps.Length;
            if (steps.Length == 0) return changed;

            while (state.current_step < steps.Length && StepSatisfied(steps[state.current_step]))
            {
                state.completed_steps = state.current_step + 1;
                state.current_step++;
                changed = true;

                if (emit)
                {
                    QuestAdvanced?.Invoke(id, state.completed_steps, state.total_steps);
                    signals?.Publish(
                        WorldSignalKind.QuestAdvanced,
                        "quest.advanced",
                        subject: id,
                        stringValue: steps[state.completed_steps - 1] != null ? steps[state.completed_steps - 1].title : string.Empty,
                        intValue: state.completed_steps,
                        floatValue: state.total_steps > 0 ? state.completed_steps / (float)state.total_steps : 0f,
                        reason: definition.title);
                }
            }

            if (state.current_step < steps.Length) return changed;

            state.completed = true;
            state.active = false;
            changed = true;
            if (emit)
            {
                QuestCompleted?.Invoke(id);
                signals?.Publish(
                    WorldSignalKind.QuestCompleted,
                    "quest.completed",
                    subject: id,
                    intValue: state.total_steps,
                    floatValue: 1f,
                    reason: definition.title);
            }
            return changed;
        }

        private bool PrerequisitesSatisfied(WorldQuestDefinition definition)
        {
            string[] prerequisites = definition.prerequisite_ids ?? Array.Empty<string>();
            for (int i = 0; i < prerequisites.Length; i++)
            {
                string id = Normalize(prerequisites[i]);
                if (string.IsNullOrEmpty(id)) continue;
                if (!IsComplete(id)) return false;
            }
            return true;
        }

        private bool StepSatisfied(WorldQuestStepDefinition step)
        {
            if (step == null) return false;
            WorldQuestCondition[] conditions = step.conditions ?? Array.Empty<WorldQuestCondition>();
            if (conditions.Length == 0) return false;
            for (int i = 0; i < conditions.Length; i++)
            {
                if (!Satisfied(conditions[i])) return false;
            }
            return true;
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
            _definitions.Clear();
            if (definitions == null) return;

            for (int i = 0; i < definitions.Length; i++)
            {
                WorldQuestDefinition definition = definitions[i];
                if (definition == null || string.IsNullOrWhiteSpace(definition.id)) continue;
                string id = Normalize(definition.id);
                if (_index.ContainsKey(id)) continue;
                definition.id = id;
                WorldQuestProgress state = new WorldQuestProgress
                {
                    id = id,
                    total_steps = definition.steps != null ? definition.steps.Length : 0,
                };
                progress.Add(state);
                _index[id] = state;
                _definitions[id] = definition;
            }
        }

        private void SortDefinitions()
        {
            if (definitions == null) return;
            Array.Sort(definitions, (a, b) =>
            {
                if (ReferenceEquals(a, b)) return 0;
                if (a == null) return 1;
                if (b == null) return -1;
                int order = a.sort_order.CompareTo(b.sort_order);
                return order != 0 ? order : string.CompareOrdinal(Normalize(a.id), Normalize(b.id));
            });
        }

        private void Resolve()
        {
            if (ledger == null) ledger = GetComponent<WorldStateLedger>();
            if (signals == null) signals = GetComponent<WorldSignalBus>();
        }

        private void SubscribeLedger()
        {
            if (ledger == null) return;
            ledger.StateChanged -= OnStateChanged;
            ledger.StateChanged += OnStateChanged;
            ledger.SnapshotRestored -= OnSnapshotRestored;
            ledger.SnapshotRestored += OnSnapshotRestored;
        }

        private void UnsubscribeLedger()
        {
            if (ledger == null) return;
            ledger.StateChanged -= OnStateChanged;
            ledger.SnapshotRestored -= OnSnapshotRestored;
        }

        private static string Normalize(string value)
            => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();
    }
}
