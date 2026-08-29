using System;
using UnityEngine;

namespace Mindforge.Combat
{
    /// <summary>
    /// Authoritative fixed-tick timing and tuning for one Guardian light-chain step.
    /// Animation/VFX consume the resulting state; they never define these windows.
    /// </summary>
    [Serializable]
    public sealed class AttackDefinition
    {
        [SerializeField] private string id = "light_1";
        [SerializeField, Min(1)] private int startupTicks = 14;
        [SerializeField, Min(1)] private int activeTicks = 24;
        [SerializeField, Min(0)] private int recoveryTicks = 16;
        [SerializeField, Min(0)] private int comboBufferOpenTick = 23;
        [SerializeField, Min(0)] private int comboBufferCloseTick = 36;
        [SerializeField, Range(0.15f, 1f)] private float movementMultiplier = 0.82f;
        [SerializeField, Range(0.10f, 1f)] private float turnMultiplier = 0.82f;
        [SerializeField, Min(0f)] private float damageMultiplier = 1f;
        [SerializeField, Min(0f)] private float poiseMultiplier = 1f;
        [SerializeField, Min(0f)] private float reachMultiplier = 1f;
        [SerializeField, Min(0f)] private float sweepMultiplier = 1f;
        [SerializeField, Min(0f)] private float knockbackMultiplier = 1f;
        [SerializeField] private bool reverseSweep;
        [SerializeField] private bool heavy;
        [SerializeField] private string presentationId = "guardian_light_1";

        public string Id => id;
        public int StartupTicks => Mathf.Max(1, startupTicks);
        public int ActiveTicks => Mathf.Max(1, activeTicks);
        public int RecoveryTicks => Mathf.Max(0, recoveryTicks);
        public int ActiveStartTick => StartupTicks;
        public int ActiveEndTick => StartupTicks + ActiveTicks;
        public int CommitmentTicks => ActiveEndTick;
        public int TotalTicks => CommitmentTicks + RecoveryTicks;
        public int ComboBufferOpenTick => Mathf.Clamp(comboBufferOpenTick, 0, Mathf.Max(0, CommitmentTicks - 1));
        public int ComboBufferCloseTick => Mathf.Clamp(comboBufferCloseTick, ComboBufferOpenTick, Mathf.Max(ComboBufferOpenTick, CommitmentTicks - 1));
        public float MovementMultiplier => Mathf.Clamp(movementMultiplier, 0.15f, 1f);
        public float TurnMultiplier => Mathf.Clamp(turnMultiplier, 0.10f, 1f);
        public float DamageMultiplier => Mathf.Max(0f, damageMultiplier);
        public float PoiseMultiplier => Mathf.Max(0f, poiseMultiplier);
        public float ReachMultiplier => Mathf.Max(0.1f, reachMultiplier);
        public float SweepMultiplier => Mathf.Max(0.1f, sweepMultiplier);
        public float KnockbackMultiplier => Mathf.Max(0.1f, knockbackMultiplier);
        public bool ReverseSweep => reverseSweep;
        public bool Heavy => heavy;
        public string PresentationId => string.IsNullOrWhiteSpace(presentationId) ? id : presentationId;

        public bool IsActive(long elapsedTicks)
            => elapsedTicks >= ActiveStartTick && elapsedTicks < ActiveEndTick;

        public bool IsCommitted(long elapsedTicks)
            => elapsedTicks >= 0 && elapsedTicks < CommitmentTicks;

        public bool InRecovery(long elapsedTicks)
            => elapsedTicks >= CommitmentTicks && elapsedTicks < TotalTicks;

        public bool ComboBufferOpen(long elapsedTicks)
            => elapsedTicks >= ComboBufferOpenTick && elapsedTicks <= ComboBufferCloseTick;

        public float AttackProgress(long elapsedTicks)
            => CommitmentTicks <= 0 ? 1f : Mathf.Clamp01(elapsedTicks / (float)CommitmentTicks);

        public float ActiveProgress(long elapsedTicks)
            => ActiveTicks <= 0 ? 1f : Mathf.Clamp01((elapsedTicks - ActiveStartTick) / (float)ActiveTicks);

        public static AttackDefinition Create(
            string attackId,
            int startup,
            int active,
            int recovery,
            int bufferOpen,
            int bufferClose,
            float movement,
            float turn,
            float damage,
            float poise,
            float reach,
            float sweep,
            float knockback,
            bool reverse,
            bool isHeavy,
            string presentation)
        {
            return new AttackDefinition
            {
                id = attackId,
                startupTicks = startup,
                activeTicks = active,
                recoveryTicks = recovery,
                comboBufferOpenTick = bufferOpen,
                comboBufferCloseTick = bufferClose,
                movementMultiplier = movement,
                turnMultiplier = turn,
                damageMultiplier = damage,
                poiseMultiplier = poise,
                reachMultiplier = reach,
                sweepMultiplier = sweep,
                knockbackMultiplier = knockback,
                reverseSweep = reverse,
                heavy = isHeavy,
                presentationId = presentation,
            };
        }

        public static AttackDefinition[] CreateDefaultLightChain()
        {
            // 120 Hz authoritative simulation. Step one is a fast checking cut, step two
            // reverses the sweep while retaining mobility, and step three is the deliberate
            // high-poise finisher. The rhythm is compact without allowing animation/VFX to
            // invent contact or cancel windows.
            return new[]
            {
                // ~0.35 s total. Quick opener with broad steering and slightly conservative damage.
                Create("aetherblade_light_1", 10, 19, 13, 18, 27, 0.90f, 0.92f, 0.90f, 0.88f, 1.03f, 1.02f, 0.92f, false, false, "guardian_light_1"),
                // ~0.38 s total. Reverse cut reaches a little farther and keeps the combo moving.
                Create("aetherblade_light_2", 11, 21, 14, 20, 30, 0.86f, 0.86f, 1.00f, 1.00f, 1.07f, 1.10f, 1.00f, true, false, "guardian_light_2"),
                // ~0.52 s total. This is the commitment: longer recovery, larger poise/knockback payoff.
                Create("aetherblade_light_3", 15, 25, 22, 25, 37, 0.72f, 0.68f, 1.32f, 1.62f, 1.14f, 1.20f, 1.40f, false, true, "guardian_light_3"),
            };
        }
    }
}
