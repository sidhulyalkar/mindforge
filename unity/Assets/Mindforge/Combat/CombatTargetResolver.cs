using UnityEngine;

namespace Mindforge.Combat
{
    /// <summary>
    /// Shared conventional target resolution for physical combat systems.
    /// Target lock is always player-owned. This helper only lets existing combat
    /// actions resolve the target the player has already chosen, with a serialized
    /// fallback for boss-only/legacy scenes.
    /// </summary>
    public static class CombatTargetResolver
    {
        public static Transform Resolve(GuardianTargetLock targetLock, Transform fallback)
        {
            Transform locked = targetLock != null ? targetLock.Target : null;
            if (IsAliveEnemy(locked)) return locked;
            return IsAliveEnemy(fallback) ? fallback : null;
        }

        public static bool IsAliveEnemy(Transform candidate)
        {
            if (candidate == null || !candidate.gameObject.activeInHierarchy) return false;
            CombatantVitals vitals = candidate.GetComponentInParent<CombatantVitals>();
            if (vitals == null) vitals = candidate.GetComponent<CombatantVitals>();
            return vitals == null || (vitals.Team == CombatTeam.Enemy && vitals.IsAlive);
        }

        public static CombatantVitals FindEnemyNear(Vector3 position, float radius)
        {
            float maxSqr = Mathf.Max(0.1f, radius) * Mathf.Max(0.1f, radius);
            CombatantVitals best = null;
            float bestSqr = maxSqr;
            CombatantVitals[] all = Object.FindObjectsOfType<CombatantVitals>(true);
            for (int i = 0; i < all.Length; i++)
            {
                CombatantVitals candidate = all[i];
                if (candidate == null || candidate.Team != CombatTeam.Enemy || !candidate.IsAlive ||
                    !candidate.gameObject.activeInHierarchy)
                    continue;

                Vector3 delta = candidate.transform.position - position;
                delta.y = 0f;
                float sqr = delta.sqrMagnitude;
                if (sqr >= bestSqr) continue;
                bestSqr = sqr;
                best = candidate;
            }
            return best;
        }
    }
}
