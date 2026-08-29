using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mindforge.World
{
    public enum EncounterContractKind
    {
        Teaching = 0,
        Arena = 1,
        Boss = 2,
        WorldEvent = 3,
    }

    [Serializable]
    public sealed class EncounterContract
    {
        public string id;
        public string title;
        public EncounterContractKind kind;
        public string authority_component;
        public int enemy_count;
        public int wave_count = 1;
        public int recommended_mastery;
        public bool supports_replay = true;
        public bool competitive_candidate;
        public bool ranked_eligible;
        public string neural_contract = "optional_transform_only";
    }

    /// <summary>
    /// Stable metadata registry for encounters. Contracts describe a fight for UI, QA,
    /// replay, tuning and future tournament services; they never schedule or resolve combat.
    /// ranked_eligible is intentionally independent of competitive_candidate so an encounter
    /// can be designed for competition without claiming it has passed runtime qualification.
    /// </summary>
    [DefaultExecutionOrder(-768)]
    public sealed class EncounterContractRegistry : MonoBehaviour
    {
        [SerializeField] private EncounterContract[] contracts = Array.Empty<EncounterContract>();
        private readonly Dictionary<string, EncounterContract> _index =
            new Dictionary<string, EncounterContract>(StringComparer.Ordinal);

        public IReadOnlyList<EncounterContract> Contracts => contracts;

        private void Awake() => Reindex();

        public void ConfigureRuntime(EncounterContract[] authoredContracts)
        {
            contracts = authoredContracts ?? Array.Empty<EncounterContract>();
            Array.Sort(contracts, CompareContracts);
            Reindex();
        }

        public EncounterContract Get(string id)
        {
            string normalized = Normalize(id);
            return !string.IsNullOrEmpty(normalized) && _index.TryGetValue(normalized, out EncounterContract value)
                ? value
                : null;
        }

        public bool Contains(string id) => Get(id) != null;

        private void Reindex()
        {
            _index.Clear();
            if (contracts == null) return;
            for (int i = 0; i < contracts.Length; i++)
            {
                EncounterContract contract = contracts[i];
                if (contract == null) continue;
                contract.id = Normalize(contract.id);
                if (string.IsNullOrEmpty(contract.id) || _index.ContainsKey(contract.id)) continue;
                contract.wave_count = Mathf.Max(1, contract.wave_count);
                contract.enemy_count = Mathf.Max(0, contract.enemy_count);
                contract.recommended_mastery = Mathf.Max(0, contract.recommended_mastery);
                _index[contract.id] = contract;
            }
        }

        private static int CompareContracts(EncounterContract a, EncounterContract b)
        {
            if (ReferenceEquals(a, b)) return 0;
            if (a == null) return 1;
            if (b == null) return -1;
            return string.CompareOrdinal(Normalize(a.id), Normalize(b.id));
        }

        private static string Normalize(string value)
            => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();
    }
}
