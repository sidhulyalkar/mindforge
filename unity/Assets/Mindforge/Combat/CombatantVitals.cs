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

            float before = Health;
            float requestedDamage = Mathf.Max(0f, packet.Damage);
            Health = Mathf.Max(0f, Health - requestedDamage);
            float actualDamage = Mathf.Max(0f, before - Health);

            // Realized neural bonus is counterfactual incremental direct damage, not
            // requested bonus. If the non-neural baseline would already have removed
            // the remaining HP, the incremental realized contribution is zero.
            float requestedBonus = Mathf.Clamp(packet.NeuralBonusDamage, 0f, requestedDamage);
            float baselineDamage = Mathf.Max(0f, requestedDamage - requestedBonus);
            float baselineActual = Mathf.Min(before, baselineDamage);
            float realizedBonus = Mathf.Max(0f, actualDamage - baselineActual);

            if (body != null && packet.Impulse.sqrMagnitude > 0.001f)
                body.AddForce(packet.Impulse, ForceMode.VelocityChange);
            poise?.Apply(packet.PoiseDamage);

            Damaged?.Invoke(new DamagePacket(
                actualDamage,
                packet.PoiseDamage,
                packet.Impulse,
                packet.Point,
                packet.SourceTeam,
                packet.Heavy,
                packet.NeuralPayoffKind,
                realizedBonus));

            if (Health <= 0f) Died?.Invoke();
        }

        /// <summary>
        /// Returns the health that was actually restored after max-health clipping.
        /// Callers that do not need attribution may ignore the return value.
        /// </summary>
        public float Heal(float amount)
        {
            if (!IsAlive || amount <= 0f) return 0f;
            float before = Health;
            Health = Mathf.Min(maxHealth, Health + amount);
            return Mathf.Max(0f, Health - before);
        }
    }
}
