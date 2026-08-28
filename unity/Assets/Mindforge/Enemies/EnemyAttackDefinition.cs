using System;
using UnityEngine;

namespace Mindforge.Enemies
{
    public enum EnemyAttackType
    {
        Melee = 0,
        Projectile = 1,
        Burst = 2,
        Retreat = 3,
    }

    /// <summary>
    /// Gameplay definition for one enemy attack. The definition is authoritative data;
    /// presentationId only selects animation/VFX/audio after gameplay has chosen it.
    /// </summary>
    [Serializable]
    public sealed class EnemyAttackDefinition
    {
        [SerializeField] private string id = "attack";
        [SerializeField] private EnemyAttackType type = EnemyAttackType.Melee;
        [SerializeField, Min(0f)] private float minimumRange;
        [SerializeField, Min(0.05f)] private float maximumRange = 2f;
        [SerializeField, Range(0f, 180f)] private float maximumFacingAngle = 80f;
        [SerializeField, Min(1)] private int weight = 1;
        [SerializeField, Min(0)] private int cooldownTicks = 120;
        [SerializeField, Min(1)] private int telegraphTicks = 48;
        [SerializeField, Min(0)] private int activeTicks = 1;
        [SerializeField, Min(1)] private int recoveryTicks = 60;
        [SerializeField, Range(0f, 1f)] private float trackingStrength;
        [SerializeField, Min(0f)] private float damage = 8f;
        [SerializeField, Min(0f)] private float poiseDamage = 5f;
        [SerializeField, Min(0f)] private float knockback = 1.4f;
        [SerializeField, Min(0f)] private float projectileSpeed = 10f;
        [SerializeField, Min(1)] private int projectileCount = 1;
        [SerializeField, Min(0f)] private float projectileSpreadDegrees;
        [SerializeField] private bool requiresLineOfSight;
        [SerializeField] private bool heavy;
        [SerializeField] private string presentationId = "attack";

        public string Id => string.IsNullOrWhiteSpace(id) ? "attack" : id;
        public EnemyAttackType Type => type;
        public float MinimumRange => Mathf.Max(0f, minimumRange);
        public float MaximumRange => Mathf.Max(MinimumRange + 0.05f, maximumRange);
        public float MaximumFacingAngle => Mathf.Clamp(maximumFacingAngle, 0f, 180f);
        public int Weight => Mathf.Max(1, weight);
        public int CooldownTicks => Mathf.Max(0, cooldownTicks);
        public int TelegraphTicks => Mathf.Max(1, telegraphTicks);
        public int ActiveTicks => Mathf.Max(0, activeTicks);
        public int RecoveryTicks => Mathf.Max(1, recoveryTicks);
        public float TrackingStrength => Mathf.Clamp01(trackingStrength);
        public float Damage => Mathf.Max(0f, damage);
        public float PoiseDamage => Mathf.Max(0f, poiseDamage);
        public float Knockback => Mathf.Max(0f, knockback);
        public float ProjectileSpeed => Mathf.Max(0f, projectileSpeed);
        public int ProjectileCount => Mathf.Max(1, projectileCount);
        public float ProjectileSpreadDegrees => Mathf.Max(0f, projectileSpreadDegrees);
        public bool RequiresLineOfSight => requiresLineOfSight;
        public bool Heavy => heavy;
        public string PresentationId => string.IsNullOrWhiteSpace(presentationId) ? Id : presentationId;

        public bool RangeValid(float distance)
            => distance >= MinimumRange && distance <= MaximumRange;

        public bool FacingValid(float angle)
            => angle <= MaximumFacingAngle;

        public static EnemyAttackDefinition Create(
            string attackId,
            EnemyAttackType attackType,
            float minRange,
            float maxRange,
            float facingAngle,
            int selectionWeight,
            int cooldown,
            int telegraph,
            int active,
            int recovery,
            float tracking,
            float attackDamage,
            float attackPoise,
            float attackKnockback,
            float shotSpeed,
            int shotCount,
            float shotSpread,
            bool los,
            bool isHeavy,
            string presentation)
        {
            return new EnemyAttackDefinition
            {
                id = attackId,
                type = attackType,
                minimumRange = minRange,
                maximumRange = maxRange,
                maximumFacingAngle = facingAngle,
                weight = selectionWeight,
                cooldownTicks = cooldown,
                telegraphTicks = telegraph,
                activeTicks = active,
                recoveryTicks = recovery,
                trackingStrength = tracking,
                damage = attackDamage,
                poiseDamage = attackPoise,
                knockback = attackKnockback,
                projectileSpeed = shotSpeed,
                projectileCount = shotCount,
                projectileSpreadDegrees = shotSpread,
                requiresLineOfSight = los,
                heavy = isHeavy,
                presentationId = presentation,
            };
        }
    }
}
