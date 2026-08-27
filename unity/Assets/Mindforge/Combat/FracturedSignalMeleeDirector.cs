using System;
using System.Collections;
using UnityEngine;

namespace Mindforge.Combat
{
    /// <summary>
    /// Close-range authority for The Fractured Signal. The primary boss scheduler
    /// invokes these patterns instead of running a second independent attack loop, so
    /// melee pressure cannot silently stack with an unrelated projectile pattern.
    /// </summary>
    public sealed class FracturedSignalMeleeDirector : MonoBehaviour
    {
        [SerializeField] private FracturedSignalDirector bossDirector;
        [SerializeField] private CombatantVitals bossVitals;
        [SerializeField] private Transform player;
        [SerializeField] private CombatantVitals playerVitals;
        [SerializeField] private GuardianSwordShieldController playerGuard;
        [SerializeField] private GuardianMotor playerMotor;

        [Header("Engagement")]
        [SerializeField] private float engageDistance = 5.0f;

        [Header("Fracture cleave")]
        [SerializeField] private float cleaveRange = 3.65f;
        [SerializeField] private float cleaveArcDegrees = 138f;
        [SerializeField] private float cleaveDamage = 22f;
        [SerializeField] private float cleavePoise = 18f;
        [SerializeField] private float cleaveTelegraphPhaseOne = 0.76f;
        [SerializeField] private float cleaveTelegraphPhaseTwo = 0.64f;
        [SerializeField] private float cleaveTelegraphPhaseThree = 0.54f;

        [Header("Fracture slam")]
        [SerializeField] private float slamRadius = 3.05f;
        [SerializeField] private float slamDamage = 29f;
        [SerializeField] private float slamPoise = 29f;
        [SerializeField] private float slamTelegraphPhaseTwo = 0.82f;
        [SerializeField] private float slamTelegraphPhaseThree = 0.67f;

        public event Action<string, Vector3, float, float, bool> MeleeTelegraphed;
        public event Action<string, string, float> MeleeResolved;

        public bool CanEngage
        {
            get
            {
                if (player == null || bossVitals == null || !bossVitals.IsAlive) return false;
                Vector3 delta = Vector3.ProjectOnPlane(player.position - transform.position, Vector3.up);
                return delta.sqrMagnitude <= engageDistance * engageDistance;
            }
        }

        private void Awake() => Resolve();

        public void ConfigureRuntime(
            FracturedSignalDirector owner,
            Transform playerTransform,
            GuardianSwordShieldController guard,
            GuardianMotor motor)
        {
            bossDirector = owner;
            bossVitals = owner != null ? owner.GetComponent<CombatantVitals>() : GetComponent<CombatantVitals>();
            player = playerTransform;
            playerGuard = guard;
            playerMotor = motor;
            playerVitals = player != null ? player.GetComponent<CombatantVitals>() : null;
        }

        private void Resolve()
        {
            if (bossDirector == null) bossDirector = GetComponent<FracturedSignalDirector>();
            if (bossVitals == null) bossVitals = GetComponent<CombatantVitals>();
            if (playerGuard == null) playerGuard = FindObjectOfType<GuardianSwordShieldController>(true);
            if (playerGuard != null)
            {
                if (player == null) player = playerGuard.transform;
                if (playerVitals == null) playerVitals = playerGuard.GetComponent<CombatantVitals>();
                if (playerMotor == null) playerMotor = playerGuard.GetComponent<GuardianMotor>();
            }
        }

        public IEnumerator ExecuteCleave(int phase, bool heavy = false)
        {
            Resolve();
            if (!CanExecute()) yield break;

            Vector3 direction = FlatDirectionToPlayer();
            float telegraph = phase <= 1 ? cleaveTelegraphPhaseOne : phase == 2 ? cleaveTelegraphPhaseTwo : cleaveTelegraphPhaseThree;
            float arc = cleaveArcDegrees + (heavy ? 12f : 0f);
            float range = cleaveRange + (phase >= 3 ? 0.25f : 0f);
            float damage = cleaveDamage * (phase <= 1 ? 0.90f : phase == 2 ? 1f : 1.12f) * (heavy ? 1.16f : 1f);
            float poise = cleavePoise * (heavy ? 1.25f : 1f);

            MeleeTelegraphed?.Invoke("CLEAVE", direction, range, arc, heavy);
            yield return WaitTelegraph(telegraph);
            if (!CanExecute()) yield break;

            string outcome = ResolveCleave(direction, range, arc, damage, poise, heavy);
            MeleeResolved?.Invoke("CLEAVE", outcome, damage);
        }

        public IEnumerator ExecuteSlam(int phase, bool heavy = true)
        {
            Resolve();
            if (!CanExecute()) yield break;

            float telegraph = phase >= 3 ? slamTelegraphPhaseThree : slamTelegraphPhaseTwo;
            float radius = slamRadius + (phase >= 3 ? 0.30f : 0f);
            float damage = slamDamage * (phase >= 3 ? 1.10f : 1f) * (heavy ? 1f : 0.86f);
            float poise = slamPoise * (phase >= 3 ? 1.08f : 1f);
            Vector3 direction = FlatDirectionToPlayer();

            MeleeTelegraphed?.Invoke("SLAM", direction, radius, 360f, heavy);
            yield return WaitTelegraph(telegraph);
            if (!CanExecute()) yield break;

            string outcome = ResolveSlam(radius, damage, poise, heavy);
            MeleeResolved?.Invoke("SLAM", outcome, damage);
        }

        private IEnumerator WaitTelegraph(float seconds)
        {
            float elapsed = 0f;
            float duration = Mathf.Max(0.08f, seconds);
            while (elapsed < duration)
            {
                if (!CanExecute()) yield break;
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        private string ResolveCleave(Vector3 lockedDirection, float range, float arc, float damage, float poise, bool heavy)
        {
            Vector3 delta = Vector3.ProjectOnPlane(player.position - transform.position, Vector3.up);
            float distance = delta.magnitude;
            if (distance > range || distance < 0.001f) return "SPACED";
            if (Vector3.Angle(lockedDirection, delta.normalized) > arc * 0.5f) return "SIDESTEPPED";
            return ResolveContact(damage, poise, heavy, player.position + Vector3.up * 0.52f);
        }

        private string ResolveSlam(float radius, float damage, float poise, bool heavy)
        {
            Vector3 delta = Vector3.ProjectOnPlane(player.position - transform.position, Vector3.up);
            if (delta.magnitude > radius) return "SPACED";
            return ResolveContact(damage, poise, heavy, player.position + Vector3.up * 0.42f);
        }

        private string ResolveContact(float damage, float poise, bool heavy, Vector3 hitPoint)
        {
            if (playerMotor != null && playerMotor.IsInvulnerable)
                return "DODGED";

            if (playerGuard != null)
            {
                GuardStrikeResult guard = playerGuard.TryResolveIncomingStrike(
                    damage,
                    poise,
                    transform.position,
                    hitPoint,
                    heavy);
                if (guard == GuardStrikeResult.Blocked) return "BLOCKED";
                if (guard == GuardStrikeResult.PerfectGuard) return "PERFECT_GUARD";
                if (guard == GuardStrikeResult.GuardBroken) return "GUARD_BROKEN";
                if (guard == GuardStrikeResult.OutsideCoverage) return ApplyDirectHit(damage, poise, heavy, hitPoint, "FLANKED");
            }

            return ApplyDirectHit(damage, poise, heavy, hitPoint, "HIT");
        }

        private string ApplyDirectHit(float damage, float poise, bool heavy, Vector3 point, string outcome)
        {
            if (playerVitals == null || !playerVitals.IsAlive) return "NO_TARGET";
            Vector3 impulse = FlatDirectionToPlayer() * (heavy ? 4.8f : 3.2f);
            playerVitals.ReceiveDamage(new DamagePacket(
                damage,
                poise,
                impulse,
                point,
                CombatTeam.Enemy,
                heavy));
            return outcome;
        }

        private Vector3 FlatDirectionToPlayer()
        {
            if (player == null) return transform.forward;
            Vector3 direction = Vector3.ProjectOnPlane(player.position - transform.position, Vector3.up);
            if (direction.sqrMagnitude < 0.001f) direction = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
            if (direction.sqrMagnitude < 0.001f) direction = Vector3.forward;
            return direction.normalized;
        }

        private bool CanExecute()
        {
            if (player == null || playerVitals == null || !playerVitals.IsAlive || bossVitals == null || !bossVitals.IsAlive)
                return false;
            if (bossDirector != null && bossDirector.ExternalPaused) return false;
            if (bossVitals.Poise != null && bossVitals.Poise.Broken) return false;
            return true;
        }
    }
}
