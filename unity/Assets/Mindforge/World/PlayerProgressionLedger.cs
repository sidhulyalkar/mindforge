using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mindforge.World
{
    public enum WorldRewardKind
    {
        Resonance = 0,
        Mastery = 1,
        Unlock = 2,
    }

    [Serializable]
    public sealed class WorldQuestRewardDefinition
    {
        public WorldRewardKind kind;
        public string id;
        public int amount = 1;
    }

    [Serializable]
    public sealed class PlayerProgressionSnapshot
    {
        public string schema = "mindforge.player_progression.v1";
        public int resonance;
        public int mastery;
        public List<string> unlocks = new List<string>();
        public List<string> reward_receipts = new List<string>();
    }

    /// <summary>
    /// Narrow authority for durable player progression. It owns currencies, semantic unlocks
    /// and idempotent reward receipts only. It never moves the Guardian, schedules encounters,
    /// changes damage, opens gates, or reads neural state. Gameplay systems may later consume
    /// unlock flags through explicit adapters rather than giving progression scene authority.
    /// </summary>
    [DefaultExecutionOrder(-775)]
    public sealed class PlayerProgressionLedger : MonoBehaviour
    {
        [SerializeField] private WorldSignalBus signals;
        [SerializeField, Min(0)] private int resonance;
        [SerializeField, Min(0)] private int mastery;
        [SerializeField] private List<string> unlocks = new List<string>();
        [SerializeField] private List<string> rewardReceipts = new List<string>();

        private readonly HashSet<string> _unlockIndex = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> _receiptIndex = new HashSet<string>(StringComparer.Ordinal);

        public event Action<string, int, int, string> CurrencyChanged;
        public event Action<string, string> Unlocked;
        public event Action SnapshotRestored;

        public int Resonance => resonance;
        public int Mastery => mastery;
        public IReadOnlyList<string> Unlocks => unlocks;
        public IReadOnlyList<string> RewardReceipts => rewardReceipts;

        private void Awake()
        {
            if (signals == null) signals = GetComponent<WorldSignalBus>();
            Reindex();
        }

        public void ConfigureRuntime(WorldSignalBus bus)
        {
            signals = bus;
            Reindex();
        }

        public bool Grant(WorldQuestRewardDefinition reward, string source)
        {
            if (reward == null) return false;
            switch (reward.kind)
            {
                case WorldRewardKind.Resonance:
                    return AddResonance(Mathf.Max(0, reward.amount), source);
                case WorldRewardKind.Mastery:
                    return AddMastery(Mathf.Max(0, reward.amount), source);
                case WorldRewardKind.Unlock:
                    return Unlock(reward.id, source);
                default:
                    return false;
            }
        }

        public bool TryClaimRewardReceipt(string questId)
        {
            string id = Normalize(questId);
            if (string.IsNullOrEmpty(id) || !_receiptIndex.Add(id)) return false;
            rewardReceipts.Add(id);
            rewardReceipts.Sort(StringComparer.Ordinal);
            return true;
        }

        public bool HasRewardReceipt(string questId)
        {
            string id = Normalize(questId);
            return !string.IsNullOrEmpty(id) && _receiptIndex.Contains(id);
        }

        public bool AddResonance(int amount, string reason = null)
        {
            amount = Mathf.Max(0, amount);
            if (amount == 0) return false;
            int before = resonance;
            resonance = checked(resonance + amount);
            CurrencyChanged?.Invoke("resonance", before, resonance, reason ?? string.Empty);
            PublishCurrency("resonance", before, resonance, reason);
            return true;
        }

        public bool AddMastery(int amount, string reason = null)
        {
            amount = Mathf.Max(0, amount);
            if (amount == 0) return false;
            int before = mastery;
            mastery = checked(mastery + amount);
            CurrencyChanged?.Invoke("mastery", before, mastery, reason ?? string.Empty);
            PublishCurrency("mastery", before, mastery, reason);
            return true;
        }

        public bool Unlock(string id, string reason = null)
        {
            string normalized = Normalize(id);
            if (string.IsNullOrEmpty(normalized) || !_unlockIndex.Add(normalized)) return false;
            unlocks.Add(normalized);
            unlocks.Sort(StringComparer.Ordinal);
            Unlocked?.Invoke(normalized, reason ?? string.Empty);
            signals?.Publish(
                WorldSignalKind.ProgressionChanged,
                "progression.unlocked",
                subject: normalized,
                stringValue: normalized,
                intValue: 1,
                floatValue: 1f,
                reason: reason ?? "unlock");
            return true;
        }

        public bool HasUnlock(string id)
        {
            string normalized = Normalize(id);
            return !string.IsNullOrEmpty(normalized) && _unlockIndex.Contains(normalized);
        }

        public PlayerProgressionSnapshot CaptureSnapshot()
        {
            PlayerProgressionSnapshot snapshot = new PlayerProgressionSnapshot
            {
                resonance = resonance,
                mastery = mastery,
            };
            CopyNormalized(unlocks, snapshot.unlocks);
            CopyNormalized(rewardReceipts, snapshot.reward_receipts);
            return snapshot;
        }

        public void RestoreSnapshot(PlayerProgressionSnapshot snapshot)
        {
            resonance = snapshot != null ? Mathf.Max(0, snapshot.resonance) : 0;
            mastery = snapshot != null ? Mathf.Max(0, snapshot.mastery) : 0;
            unlocks.Clear();
            rewardReceipts.Clear();
            _unlockIndex.Clear();
            _receiptIndex.Clear();

            if (snapshot != null)
            {
                RestoreNormalized(snapshot.unlocks, unlocks, _unlockIndex);
                RestoreNormalized(snapshot.reward_receipts, rewardReceipts, _receiptIndex);
            }
            SnapshotRestored?.Invoke();
        }

        private void PublishCurrency(string id, int before, int after, string reason)
        {
            signals?.Publish(
                WorldSignalKind.ProgressionChanged,
                "progression.currency",
                subject: id,
                stateKey: "progression." + id,
                intValue: after,
                floatValue: after,
                reason: (reason ?? string.Empty) + ":" + before + "->" + after);
        }

        private void Reindex()
        {
            ReindexList(unlocks, _unlockIndex);
            ReindexList(rewardReceipts, _receiptIndex);
        }

        private static void ReindexList(List<string> values, HashSet<string> index)
        {
            index.Clear();
            for (int i = values.Count - 1; i >= 0; i--)
            {
                string id = Normalize(values[i]);
                if (string.IsNullOrEmpty(id) || !index.Add(id))
                {
                    values.RemoveAt(i);
                    continue;
                }
                values[i] = id;
            }
            values.Sort(StringComparer.Ordinal);
        }

        private static void CopyNormalized(List<string> source, List<string> destination)
        {
            destination.Clear();
            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < source.Count; i++)
            {
                string id = Normalize(source[i]);
                if (!string.IsNullOrEmpty(id) && seen.Add(id)) destination.Add(id);
            }
            destination.Sort(StringComparer.Ordinal);
        }

        private static void RestoreNormalized(List<string> source, List<string> destination, HashSet<string> index)
        {
            if (source == null) return;
            for (int i = 0; i < source.Count; i++)
            {
                string id = Normalize(source[i]);
                if (string.IsNullOrEmpty(id) || !index.Add(id)) continue;
                destination.Add(id);
            }
            destination.Sort(StringComparer.Ordinal);
        }

        private static string Normalize(string value)
            => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();
    }
}
