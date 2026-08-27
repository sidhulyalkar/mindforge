using System;
using UnityEngine;

namespace Mindforge.Combat
{
    public enum WeaponArchetype { Sword = 0, Greatsword = 1, Spear = 2, Axe = 3, Hammer = 4 }
    public enum ShieldArchetype { Buckler = 0, Kite = 1, Tower = 2 }
    public enum ArmorWeightClass { Light = 0, Medium = 1, Heavy = 2 }
    public enum EquipLoadClass { Light = 0, Medium = 1, Heavy = 2, Overloaded = 3 }

    [Serializable]
    public sealed class WeaponSpec
    {
        public string id = "aetherblade_longsword";
        public string displayName = "Aetherblade Longsword";
        public WeaponArchetype archetype = WeaponArchetype.Sword;
        [Min(0.1f)] public float massKg = 3.2f;
        [Min(0.25f)] public float reachMeters = 2.15f;
        [Min(0.05f)] public float lightAttackSeconds = 0.42f;
        [Range(10f, 220f)] public float sweepDegrees = 132f;
        [Min(0f)] public float baseDamage = 24f;
        [Min(0f)] public float poiseDamage = 18f;
        [Min(0f)] public float staminaCost = 18f;
        [Min(0f)] public float bladeRadius = 0.16f;
    }

    [Serializable]
    public sealed class ShieldSpec
    {
        public string id = "verdant_ward_kite";
        public string displayName = "Verdant Ward Shield";
        public ShieldArchetype archetype = ShieldArchetype.Kite;
        [Min(0.1f)] public float massKg = 7.4f;
        [Range(0f, 1f)] public float baseDamageAbsorption = 0.78f;
        [Range(0.05f, 2f)] public float stability = 0.82f;
        [Min(0f)] public float guardStaminaScale = 1.25f;
        [Min(0f)] public float baseCoverageScale = 1f;
        [Min(0f)] public float perfectGuardWindowSeconds = 0.18f;
    }

    [Serializable]
    public sealed class ArmorSpec
    {
        public string id = "warden_weave";
        public string displayName = "Warden Weave";
        public ArmorWeightClass weightClass = ArmorWeightClass.Medium;
        [Min(0.1f)] public float massKg = 16.0f;
        [Min(1f)] public float equipCapacityKg = 52f;
        [Range(0f, 1f)] public float physicalMitigation = 0.10f;
        [Min(0f)] public float poiseBonus = 10f;
    }

    /// <summary>
    /// Data-first equipment contract for the Guardian. The first competition build
    /// ships one coherent sword/shield/armor kit, while the same contract is intended
    /// to back a later inventory/build UI with multiple item families.
    ///
    /// Mass is not cosmetic: total equipped mass defines a load class which feeds
    /// movement, roll and stamina behavior. Weapon and shield mass are also consumed
    /// by their respective impact/guard calculations.
    /// </summary>
    public sealed class GuardianEquipmentLoadout : MonoBehaviour
    {
        [SerializeField] private WeaponSpec mainHand = new WeaponSpec();
        [SerializeField] private ShieldSpec offHand = new ShieldSpec();
        [SerializeField] private ArmorSpec armor = new ArmorSpec();

        public WeaponSpec MainHand => mainHand;
        public ShieldSpec OffHand => offHand;
        public ArmorSpec Armor => armor;

        public float TotalMassKg =>
            Mathf.Max(0f, mainHand?.massKg ?? 0f) +
            Mathf.Max(0f, offHand?.massKg ?? 0f) +
            Mathf.Max(0f, armor?.massKg ?? 0f);

        public float EquipCapacityKg => Mathf.Max(1f, armor?.equipCapacityKg ?? 52f);
        public float LoadRatio => TotalMassKg / EquipCapacityKg;

        public EquipLoadClass LoadClass
        {
            get
            {
                float ratio = LoadRatio;
                if (ratio < 0.40f) return EquipLoadClass.Light;
                if (ratio < 0.70f) return EquipLoadClass.Medium;
                if (ratio < 0.90f) return EquipLoadClass.Heavy;
                return EquipLoadClass.Overloaded;
            }
        }

        public float MoveSpeedMultiplier
        {
            get
            {
                switch (LoadClass)
                {
                    case EquipLoadClass.Light: return 1.07f;
                    case EquipLoadClass.Medium: return 1.00f;
                    case EquipLoadClass.Heavy: return 0.88f;
                    default: return 0.70f;
                }
            }
        }

        public float RollSpeedMultiplier
        {
            get
            {
                switch (LoadClass)
                {
                    case EquipLoadClass.Light: return 1.10f;
                    case EquipLoadClass.Medium: return 1.00f;
                    case EquipLoadClass.Heavy: return 0.86f;
                    default: return 0.66f;
                }
            }
        }

        public float RollDurationMultiplier
        {
            get
            {
                switch (LoadClass)
                {
                    case EquipLoadClass.Light: return 0.92f;
                    case EquipLoadClass.Medium: return 1.00f;
                    case EquipLoadClass.Heavy: return 1.16f;
                    default: return 1.35f;
                }
            }
        }

        public float RollStaminaMultiplier
        {
            get
            {
                switch (LoadClass)
                {
                    case EquipLoadClass.Light: return 0.86f;
                    case EquipLoadClass.Medium: return 1.00f;
                    case EquipLoadClass.Heavy: return 1.22f;
                    default: return 1.55f;
                }
            }
        }
    }
}
