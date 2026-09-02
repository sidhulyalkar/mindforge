using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mindforge.Chassis
{
    public enum MindforgeRegionIdV32
    {
        Sanctum,
        NeuralCloister,
        FractureCaverns,
        MemoryGardens,
        SignalFoundry,
        AbyssalArchive,
    }

    public enum MindforgeChunkKindV32
    {
        Entry,
        Hub,
        Corridor,
        Vertical,
        ArenaSmall,
        ArenaMedium,
        Boss,
        Vista,
        Puzzle,
        Shrine,
        Secret,
        Transition,
    }

    public enum MindforgeSocketKindV32
    {
        Exit,
        Enemy,
        Loot,
        Shrine,
        Landmark,
        LightingKey,
        LightingFill,
        AudioZone,
    }

    [Serializable]
    public sealed class MindforgeRegionDefinitionV32
    {
        public MindforgeRegionIdV32 id;
        public string displayName;
        public Color ambientColor;
        public Color neuralAccent;
        public Color corruptionAccent;
        public MindforgeChunkKindV32[] requiredChunkKinds;
    }

    /// <summary>
    /// Stable world-scale vocabulary shared by V0.32 showcase tooling and future
    /// additive region builders. Macro topology remains authored; deterministic
    /// dressing is allowed only downstream of these semantic chunks/sockets.
    /// </summary>
    public static class MindforgeWorldGrammarV32
    {
        public const int GrammarVersion = 1;
        public const float MinimumGeneralCorridorWidth = 8f;
        public const float MinimumCombatHallWidth = 14f;
        public const float MinimumBossArenaDiameter = 32f;

        private static readonly MindforgeRegionDefinitionV32[] Regions =
        {
            new MindforgeRegionDefinitionV32
            {
                id = MindforgeRegionIdV32.Sanctum,
                displayName = "Sanctum Reliquary",
                ambientColor = new Color(0.66f, 0.72f, 0.78f),
                neuralAccent = new Color(0.16f, 0.90f, 1.00f),
                corruptionAccent = new Color(0.72f, 0.18f, 0.52f),
                requiredChunkKinds = new[]
                {
                    MindforgeChunkKindV32.Entry,
                    MindforgeChunkKindV32.Hub,
                    MindforgeChunkKindV32.Puzzle,
                    MindforgeChunkKindV32.ArenaMedium,
                    MindforgeChunkKindV32.Boss,
                    MindforgeChunkKindV32.Vista,
                },
            },
            new MindforgeRegionDefinitionV32
            {
                id = MindforgeRegionIdV32.NeuralCloister,
                displayName = "Neural Cloister",
                ambientColor = new Color(0.42f, 0.48f, 0.54f),
                neuralAccent = new Color(0.18f, 0.88f, 1.00f),
                corruptionAccent = new Color(0.80f, 0.24f, 0.56f),
                requiredChunkKinds = new[]
                {
                    MindforgeChunkKindV32.Corridor,
                    MindforgeChunkKindV32.Puzzle,
                    MindforgeChunkKindV32.Shrine,
                    MindforgeChunkKindV32.Secret,
                    MindforgeChunkKindV32.Transition,
                },
            },
            new MindforgeRegionDefinitionV32
            {
                id = MindforgeRegionIdV32.FractureCaverns,
                displayName = "Fracture Caverns",
                ambientColor = new Color(0.10f, 0.12f, 0.16f),
                neuralAccent = new Color(0.14f, 0.72f, 0.94f),
                corruptionAccent = new Color(0.92f, 0.16f, 0.58f),
                requiredChunkKinds = new[]
                {
                    MindforgeChunkKindV32.Transition,
                    MindforgeChunkKindV32.Vertical,
                    MindforgeChunkKindV32.ArenaSmall,
                    MindforgeChunkKindV32.Secret,
                    MindforgeChunkKindV32.Vista,
                },
            },
            new MindforgeRegionDefinitionV32
            {
                id = MindforgeRegionIdV32.MemoryGardens,
                displayName = "Memory Gardens",
                ambientColor = new Color(0.24f, 0.34f, 0.38f),
                neuralAccent = new Color(0.20f, 0.96f, 0.82f),
                corruptionAccent = new Color(0.80f, 0.44f, 0.18f),
                requiredChunkKinds = new[]
                {
                    MindforgeChunkKindV32.Entry,
                    MindforgeChunkKindV32.Hub,
                    MindforgeChunkKindV32.Vertical,
                    MindforgeChunkKindV32.Puzzle,
                    MindforgeChunkKindV32.Secret,
                    MindforgeChunkKindV32.Vista,
                },
            },
            new MindforgeRegionDefinitionV32
            {
                id = MindforgeRegionIdV32.SignalFoundry,
                displayName = "Signal Foundry",
                ambientColor = new Color(0.19f, 0.20f, 0.23f),
                neuralAccent = new Color(0.18f, 0.82f, 1.00f),
                corruptionAccent = new Color(0.95f, 0.34f, 0.15f),
                requiredChunkKinds = new[]
                {
                    MindforgeChunkKindV32.Entry,
                    MindforgeChunkKindV32.Corridor,
                    MindforgeChunkKindV32.ArenaMedium,
                    MindforgeChunkKindV32.Shrine,
                    MindforgeChunkKindV32.Transition,
                },
            },
            new MindforgeRegionDefinitionV32
            {
                id = MindforgeRegionIdV32.AbyssalArchive,
                displayName = "Abyssal Archive",
                ambientColor = new Color(0.08f, 0.09f, 0.16f),
                neuralAccent = new Color(0.35f, 0.44f, 1.00f),
                corruptionAccent = new Color(0.62f, 0.18f, 0.78f),
                requiredChunkKinds = new[]
                {
                    MindforgeChunkKindV32.Entry,
                    MindforgeChunkKindV32.Hub,
                    MindforgeChunkKindV32.Puzzle,
                    MindforgeChunkKindV32.Boss,
                    MindforgeChunkKindV32.Vista,
                },
            },
        };

        public static IReadOnlyList<MindforgeRegionDefinitionV32> AllRegions => Regions;

        public static MindforgeRegionDefinitionV32 GetRegion(MindforgeRegionIdV32 id)
        {
            for (int i = 0; i < Regions.Length; i++)
                if (Regions[i].id == id) return Regions[i];
            return null;
        }
    }
}
