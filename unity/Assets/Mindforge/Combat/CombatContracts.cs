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

        public DamagePacket(float damage, float poiseDamage, Vector3 impulse, Vector3 point, CombatTeam sourceTeam, bool heavy)
        {
            Damage = damage;
            PoiseDamage = poiseDamage;
            Impulse = impulse;
            Point = point;
            SourceTeam = sourceTeam;
            Heavy = heavy;
        }
    }

    public interface IDamageReceiver
    {
        CombatTeam Team { get; }
        bool IsAlive { get; }
        void ReceiveDamage(DamagePacket packet);
    }
}
