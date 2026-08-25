using UnityEngine;

namespace Mindforge.Combat
{
    [CreateAssetMenu(menuName = "Mindforge/Combat Tuning", fileName = "MindforgeCombatTuning")]
    public sealed class CombatTuning : ScriptableObject
    {
        [Header("Movement")]
        public float acceleration = 42f;
        public float maxSpeed = 7.2f;
        public float drag = 10f;
        public float dashSpeed = 21f;
        public float dashDuration = 0.145f;
        public float dashCooldown = 0.78f;

        [Header("Pulse Shot")]
        public float shotCooldown = 0.185f;
        public float shotSpeed = 19.5f;
        public float shotDamage = 13f;
        public float shotPoise = 5f;
        public float sightShotSpeed = 24f;
        public float sightShotDamage = 19f;

        [Header("Rift Cleave")]
        public float cleaveCooldown = 0.62f;
        public float cleaveRange = 2.9f;
        [Range(10f, 180f)] public float cleaveArcDegrees = 66f;
        public float cleaveDamage = 29f;
        public float cleavePoise = 28f;
        public float cleaveImpulse = 6.5f;

        [Header("Counter Pulse")]
        public float counterCooldown = 0.78f;
        public float counterWindow = 0.18f;
        public float counterRadius = 1.9f;
        public float reflectedDamage = 30f;
        public float reflectedPoise = 21f;
        public float counterFlux = 0.52f;

        [Header("Flux")]
        public float maxFlux = 3f;
        public float nearMissFlux = 0.18f;
        public float auraSwitchFlux = 0.13f;
        public float poiseBreakFlux = 0.50f;

        [Header("Gravity Bloom")]
        public float bloomCooldown = 3.8f;
        public float bloomDuration = 0.82f;
        public float bloomRadius = 6.5f;
        public float bloomPull = 22f;
        public float bloomReleaseSpeed = 20f;
        public float concordRadiusMultiplier = 1.22f;
        public float concordDamageMultiplier = 1.45f;

        [Header("Hit Stop")]
        public float lightHitStop = 0.025f;
        public float heavyHitStop = 0.055f;
        public float parryHitStop = 0.055f;
        public float poiseBreakHitStop = 0.075f;
    }
}
