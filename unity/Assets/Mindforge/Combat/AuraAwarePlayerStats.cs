using UnityEngine;
using Mindforge.SoulWisp;

namespace Mindforge.Combat
{
    public sealed class AuraAwarePlayerStats : MonoBehaviour
    {
        [SerializeField] private AuraBuffController buffs;
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float baseDamage = 18f;
        public float Health { get; private set; }
        public float MaxHealth => maxHealth;
        public float CurrentDamage => baseDamage * (buffs != null ? buffs.DamageMultiplier : 1f);
        private void Awake() => Health = maxHealth;
        private void Update()
        {
            if (buffs != null && buffs.GuardActive)
                Health = Mathf.Min(maxHealth, Health + buffs.HealingPerSecond * Time.deltaTime);
        }
        public void TakeDamage(float amount) => Health = Mathf.Max(0f, Health - Mathf.Max(0f, amount));
    }
}
