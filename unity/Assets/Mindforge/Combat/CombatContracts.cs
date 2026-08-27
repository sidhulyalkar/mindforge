using UnityEngine;

namespace Mindforge.Combat
{
    public enum CombatTeam { Guardian = 0, Enemy = 1 }

    public readonly struct DamagePacket
    {
        public readonly float Damage;
        public readonly float PoiseDamage;
        public readonly Vector3 Impulse;
        public readonly Vector3 Point;
        public readonly CombatTeam SourceTeam;
        public readonly bool Heavy;

        // Conservative payoff attribution. This is carried with the actual damage
        // consequence rather than reconstructed later from overlapping aura timers.
        // NeuralBonusDamage is the incremental direct-damage amount above the same
        // action's non-neural baseline. It intentionally excludes harder-to-price
        // benefits such as projectile speed, pierce, range, positioning, and survival.
        public readonly string NeuralPayoffKind;
        public readonly float NeuralBonusDamage;

        public DamagePacket(
            float damage,
            float poiseDamage,
            Vector3 impulse,
            Vector3 point,
            CombatTeam sourceTeam,
            bool heavy,
            string neuralPayoffKind = null,
            float neuralBonusDamage = 0f)
        {
            Damage = damage;
            PoiseDamage = poiseDamage;
            Impulse = impulse;
            Point = point;
            SourceTeam = sourceTeam;
            Heavy = heavy;
            NeuralPayoffKind = neuralPayoffKind;
            NeuralBonusDamage = Mathf.Clamp(neuralBonusDamage, 0f, Mathf.Max(0f, damage));
        }
    }

    public interface IDamageReceiver
    {
        CombatTeam Team { get; }
        bool IsAlive { get; }
        void ReceiveDamage(DamagePacket packet);
    }
}
