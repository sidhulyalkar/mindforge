using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mindforge.Chassis
{
    [DisallowMultipleComponent]
    public sealed class MindforgeWorldSocketV32 : MonoBehaviour
    {
        [SerializeField] private string stableId;
        [SerializeField] private MindforgeSocketKindV32 kind;
        [SerializeField] private string compatibilityTag;
        [SerializeField] private float clearanceRadius = 2f;

        public string StableId => stableId;
        public MindforgeSocketKindV32 Kind => kind;
        public string CompatibilityTag => compatibilityTag;
        public float ClearanceRadius => clearanceRadius;

        public void Configure(string id, MindforgeSocketKindV32 socketKind, string tag, float clearance)
        {
            stableId = id;
            kind = socketKind;
            compatibilityTag = tag ?? string.Empty;
            clearanceRadius = Mathf.Max(0.25f, clearance);
        }

        public bool IsValid(out string reason)
        {
            if (string.IsNullOrWhiteSpace(stableId))
            {
                reason = "socket stableId is empty";
                return false;
            }
            if (clearanceRadius < 0.25f)
            {
                reason = "socket clearance is below 0.25 m";
                return false;
            }
            reason = string.Empty;
            return true;
        }
    }

    [DisallowMultipleComponent]
    public sealed class MindforgeChunkDescriptorV32 : MonoBehaviour
    {
        [SerializeField] private string stableId;
        [SerializeField] private MindforgeRegionIdV32 region;
        [SerializeField] private MindforgeChunkKindV32 kind;
        [SerializeField] private float minimumClearWidth = MindforgeWorldGrammarV32.MinimumGeneralCorridorWidth;
        [SerializeField] private bool supportsCombat;
        [SerializeField] private bool supportsPersistence = true;

        public string StableId => stableId;
        public MindforgeRegionIdV32 Region => region;
        public MindforgeChunkKindV32 Kind => kind;
        public float MinimumClearWidth => minimumClearWidth;
        public bool SupportsCombat => supportsCombat;
        public bool SupportsPersistence => supportsPersistence;

        public void Configure(
            string id,
            MindforgeRegionIdV32 regionId,
            MindforgeChunkKindV32 chunkKind,
            bool combat,
            float clearWidth)
        {
            stableId = id;
            region = regionId;
            kind = chunkKind;
            supportsCombat = combat;
            minimumClearWidth = Mathf.Max(
                combat ? MindforgeWorldGrammarV32.MinimumCombatHallWidth : MindforgeWorldGrammarV32.MinimumGeneralCorridorWidth,
                clearWidth);
        }

        public IReadOnlyList<MindforgeWorldSocketV32> GetSockets()
        {
            return GetComponentsInChildren<MindforgeWorldSocketV32>(true);
        }

        public bool IsValid(out string reason)
        {
            if (string.IsNullOrWhiteSpace(stableId))
            {
                reason = "chunk stableId is empty";
                return false;
            }

            float requiredWidth = supportsCombat
                ? MindforgeWorldGrammarV32.MinimumCombatHallWidth
                : MindforgeWorldGrammarV32.MinimumGeneralCorridorWidth;
            if (minimumClearWidth < requiredWidth)
            {
                reason = $"chunk clear width {minimumClearWidth:F2} m is below required {requiredWidth:F2} m";
                return false;
            }

            MindforgeWorldSocketV32[] sockets = GetComponentsInChildren<MindforgeWorldSocketV32>(true);
            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < sockets.Length; i++)
            {
                string socketReason;
                if (!sockets[i].IsValid(out socketReason))
                {
                    reason = $"socket {i} invalid: {socketReason}";
                    return false;
                }
                if (!ids.Add(sockets[i].StableId))
                {
                    reason = $"duplicate socket stableId: {sockets[i].StableId}";
                    return false;
                }
            }

            reason = string.Empty;
            return true;
        }
    }
}
