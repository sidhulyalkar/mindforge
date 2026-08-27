using System;
using UnityEngine;

namespace Mindforge.Combat
{
    public sealed class CombatantVitals : MonoBehaviour, IDamageReceiver
    {
        [SerializeField] private CombatTeam team = CombatTeam.Enemy;
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private PoiseSystem poise;
        [SerializeField] private Rigidbody body;

        public CombatTeam Team => team;
        public bool IsAlive => Health > 0f;
        public float Health { get; private set; }
        public float MaxHealth => maxHealth;
        public PoiseSystem Poise => poise;

        public event Action<DamagePacket> Damaged;
        public event Action Died;

        private void Awake() => Health = maxHealth;

        public void ReceiveDamage(DamagePacket packet)
        {
            if (!IsAlive || packet.SourceTeam == team) return;
            Health = Mathf.Max(0f, Health - Mathf.Max(0f, packet.Damage));
            if (body != null && packet.Impulse.sqrMagnitude > 0.001f)
                body.AddForce(packet.Impulse, ForceMode.VelocityChange);
            poise?.Apply(packet.PoiseDamage);
            Damaged?.Invoke(packet);
            if (Health <= 0f) Died?.Invoke();
        }

        public void Heal(float amount)
        {
            if (!IsAlive || amount <= 0f) return;
            Health = Mathf.Min(maxHealth, Health + amount);
        }
    }
}
