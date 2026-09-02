using System;
using System.Collections.Generic;

namespace Mindforge.Chassis
{
    public enum MindforgeEnemyRoleV32
    {
        Remnant,
        Warden,
        Ranger,
        Stalker,
        Resonant,
        Brute,
    }

    [Serializable]
    public sealed class MindforgeEncounterSlotV32
    {
        public MindforgeEnemyRoleV32 role;
        public int wave;
        public float preferredRangeMeters;
        public float activationDelaySeconds;
        public bool optional;
    }

    [Serializable]
    public sealed class MindforgeEncounterRecipeV32
    {
        public string id;
        public string displayName;
        public int maximumSimultaneousAttackers;
        public float minimumPlayerBreathingRoomMeters;
        public MindforgeEncounterSlotV32[] slots;
    }

    /// <summary>
    /// Authored combat composition vocabulary. Recipes describe roles, waves and
    /// spacing only. They do not spawn prefabs or override the inherited enemy AI.
    /// A later socket resolver maps these semantic roles onto qualified local rigs.
    /// </summary>
    public static class MindforgeEncounterLibraryV32
    {
        public static readonly MindforgeEncounterRecipeV32 FirstRealEncounter = new MindforgeEncounterRecipeV32
        {
            id = "showcase.first_real_encounter",
            displayName = "First Fracture",
            maximumSimultaneousAttackers = 1,
            minimumPlayerBreathingRoomMeters = 3.2f,
            slots = new[]
            {
                new MindforgeEncounterSlotV32
                {
                    role = MindforgeEnemyRoleV32.Remnant,
                    wave = 1,
                    preferredRangeMeters = 3.4f,
                    activationDelaySeconds = 0f,
                },
                new MindforgeEncounterSlotV32
                {
                    role = MindforgeEnemyRoleV32.Ranger,
                    wave = 1,
                    preferredRangeMeters = 7.0f,
                    activationDelaySeconds = 1.8f,
                },
            },
        };

        public static readonly MindforgeEncounterRecipeV32 EliteEncounter = new MindforgeEncounterRecipeV32
        {
            id = "showcase.elite_encounter",
            displayName = "Broken Choir",
            maximumSimultaneousAttackers = 2,
            minimumPlayerBreathingRoomMeters = 3.6f,
            slots = new[]
            {
                new MindforgeEncounterSlotV32
                {
                    role = MindforgeEnemyRoleV32.Brute,
                    wave = 1,
                    preferredRangeMeters = 4.2f,
                    activationDelaySeconds = 0f,
                },
                new MindforgeEncounterSlotV32
                {
                    role = MindforgeEnemyRoleV32.Stalker,
                    wave = 1,
                    preferredRangeMeters = 4.8f,
                    activationDelaySeconds = 0.9f,
                },
                new MindforgeEncounterSlotV32
                {
                    role = MindforgeEnemyRoleV32.Resonant,
                    wave = 2,
                    preferredRangeMeters = 7.4f,
                    activationDelaySeconds = 2.0f,
                },
                new MindforgeEncounterSlotV32
                {
                    role = MindforgeEnemyRoleV32.Ranger,
                    wave = 2,
                    preferredRangeMeters = 7.8f,
                    activationDelaySeconds = 3.2f,
                    optional = true,
                },
            },
        };

        private static readonly MindforgeEncounterRecipeV32[] All =
        {
            FirstRealEncounter,
            EliteEncounter,
        };

        public static IReadOnlyList<MindforgeEncounterRecipeV32> Recipes => All;

        public static MindforgeEncounterRecipeV32 Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            for (int i = 0; i < All.Length; i++)
                if (string.Equals(All[i].id, id, StringComparison.Ordinal)) return All[i];
            return null;
        }
    }
}
