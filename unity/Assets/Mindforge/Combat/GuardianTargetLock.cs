using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mindforge.Combat
{
    /// <summary>
    /// Conventional player-owned target lock for third-person combat.
    ///
    /// T acquires/releases a useful enemy near the camera center. While locked,
    /// left/right arrow keys or the mouse wheel cycle conventional targets. EEG never
    /// creates, changes, confirms, cycles, or releases target lock.
    /// </summary>
    public sealed class GuardianTargetLock : MonoBehaviour
    {
        [SerializeField] private Transform fallbackTarget;
        [SerializeField] private KeyCode toggleKey = KeyCode.T;
        [SerializeField] private float lockRange = 28f;
        [SerializeField] private float breakRange = 34f;
        [SerializeField] private float targetAimHeight = 0.9f;
        [SerializeField, Range(25f, 170f)] private float maximumAcquireAngle = 105f;
        [SerializeField] private float distanceScoreWeight = 0.42f;
        [SerializeField] private float angleScoreWeight = 0.58f;
        [SerializeField] private bool requireLineOfSight = true;
        [SerializeField] private bool cycleWithArrowKeys = true;
        [SerializeField] private bool cycleWithMouseWheel = true;

        private bool _locked;
        private Transform _lockedTarget;

        public event Action<bool> LockChanged;
        public event Action<Transform> TargetChanged;

        public bool Locked => _locked && TargetAvailable(_lockedTarget);
        public Transform Target => Locked ? _lockedTarget : null;
        public Transform FallbackTarget => fallbackTarget;
        public KeyCode ToggleKey => toggleKey;

        public void Configure(Transform combatTarget)
        {
            fallbackTarget = combatTarget;
            if (_locked && !TargetAvailable(_lockedTarget))
                ReacquireOrUnlock();
        }

        public void SetLocked(bool locked)
        {
            if (!locked)
            {
                SetState(false, null);
                return;
            }

            Transform candidate = TargetAvailable(_lockedTarget) ? _lockedTarget : AcquireBestTarget();
            if (candidate == null)
            {
                SetState(false, null);
                return;
            }

            SetState(true, candidate);
        }

        public bool AcquireBest()
        {
            Transform candidate = AcquireBestTarget();
            if (candidate == null) return false;
            SetState(true, candidate);
            return true;
        }

        public bool Cycle(int direction)
        {
            if (!Locked || direction == 0) return false;
            List<Transform> candidates = CollectCandidates(Mathf.Max(lockRange, breakRange));
            if (candidates.Count <= 1) return false;

            Vector3 currentDirection = HorizontalDirectionTo(_lockedTarget);
            if (currentDirection.sqrMagnitude < 0.001f) return false;

            Transform selected = null;
            float selectedAngle = direction > 0 ? float.PositiveInfinity : float.NegativeInfinity;
            Transform wrapped = null;
            float wrappedAngle = direction > 0 ? float.NegativeInfinity : float.PositiveInfinity;

            for (int i = 0; i < candidates.Count; i++)
            {
                Transform candidate = candidates[i];
                if (candidate == null || candidate == _lockedTarget) continue;
                Vector3 candidateDirection = HorizontalDirectionTo(candidate);
                if (candidateDirection.sqrMagnitude < 0.001f) continue;

                float signed = Vector3.SignedAngle(currentDirection, candidateDirection, Vector3.up);
                if (direction > 0)
                {
                    if (signed > 1f && signed < selectedAngle)
                    {
                        selectedAngle = signed;
                        selected = candidate;
                    }
                    if (signed < wrappedAngle)
                    {
                        wrappedAngle = signed;
                        wrapped = candidate;
                    }
                }
                else
                {
                    if (signed < -1f && signed > selectedAngle)
                    {
                        selectedAngle = signed;
                        selected = candidate;
                    }
                    if (signed > wrappedAngle)
                    {
                        wrappedAngle = signed;
                        wrapped = candidate;
                    }
                }
            }

            if (selected == null) selected = wrapped;
            if (selected == null || selected == _lockedTarget) return false;
            SetState(true, selected);
            return true;
        }

        public Vector3 AimPoint
        {
            get
            {
                Transform t = Target;
                return t != null
                    ? t.position + Vector3.up * targetAimHeight
                    : transform.position + transform.forward * 6f;
            }
        }

        public Vector3 DirectionFrom(Vector3 origin)
        {
            Vector3 delta = AimPoint - origin;
            delta.y = 0f;
            if (delta.sqrMagnitude < 0.0001f) return transform.forward;
            return delta.normalized;
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                if (Locked) SetLocked(false);
                else AcquireBest();
            }

            if (Locked)
            {
                if (cycleWithArrowKeys)
                {
                    if (Input.GetKeyDown(KeyCode.LeftArrow)) Cycle(-1);
                    if (Input.GetKeyDown(KeyCode.RightArrow)) Cycle(1);
                }

                if (cycleWithMouseWheel)
                {
                    float wheel = Input.mouseScrollDelta.y;
                    if (wheel > 0.25f) Cycle(1);
                    else if (wheel < -0.25f) Cycle(-1);
                }
            }

            if (_locked && (!TargetAvailable(_lockedTarget) ||
                            HorizontalDistanceTo(_lockedTarget) > Mathf.Max(lockRange, breakRange)))
                ReacquireOrUnlock();
        }

        private void ReacquireOrUnlock()
        {
            Transform next = AcquireBestTarget(_lockedTarget);
            if (next != null) SetState(true, next);
            else SetState(false, null);
        }

        private Transform AcquireBestTarget(Transform excluded = null)
        {
            List<Transform> candidates = CollectCandidates(Mathf.Max(1f, lockRange));
            Camera camera = Camera.main;
            Vector3 referenceForward = camera != null
                ? Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up)
                : Vector3.ProjectOnPlane(transform.forward, Vector3.up);
            if (referenceForward.sqrMagnitude < 0.001f) referenceForward = Vector3.forward;
            referenceForward.Normalize();

            Transform best = null;
            float bestScore = float.PositiveInfinity;
            float maxAngle = Mathf.Max(5f, maximumAcquireAngle);
            float range = Mathf.Max(1f, lockRange);

            for (int i = 0; i < candidates.Count; i++)
            {
                Transform candidate = candidates[i];
                if (candidate == null || candidate == excluded) continue;
                Vector3 direction = HorizontalDirectionTo(candidate);
                if (direction.sqrMagnitude < 0.001f) continue;
                float angle = Vector3.Angle(referenceForward, direction);
                if (angle > maxAngle) continue;
                if (requireLineOfSight && !HasLineOfSight(candidate)) continue;

                float distance01 = Mathf.Clamp01(HorizontalDistanceTo(candidate) / range);
                float angle01 = Mathf.Clamp01(angle / maxAngle);
                float score = distance01 * Mathf.Max(0f, distanceScoreWeight) +
                              angle01 * Mathf.Max(0f, angleScoreWeight);
                if (score >= bestScore) continue;
                bestScore = score;
                best = candidate;
            }

            if (best == null && excluded == null && TargetAvailable(fallbackTarget) &&
                HorizontalDistanceTo(fallbackTarget) <= range &&
                (!requireLineOfSight || HasLineOfSight(fallbackTarget)))
                best = fallbackTarget;

            return best;
        }

        private List<Transform> CollectCandidates(float range)
        {
            List<Transform> candidates = new List<Transform>(16);
            float maxDistance = Mathf.Max(1f, range);
            CombatantVitals[] all = FindObjectsOfType<CombatantVitals>(true);
            for (int i = 0; i < all.Length; i++)
            {
                CombatantVitals vitals = all[i];
                if (vitals == null || vitals.Team != CombatTeam.Enemy || !vitals.IsAlive ||
                    !vitals.gameObject.activeInHierarchy)
                    continue;
                if (HorizontalDistanceTo(vitals.transform) > maxDistance) continue;
                candidates.Add(vitals.transform);
            }

            if (TargetAvailable(fallbackTarget) && !candidates.Contains(fallbackTarget) &&
                HorizontalDistanceTo(fallbackTarget) <= maxDistance)
                candidates.Add(fallbackTarget);

            return candidates;
        }

        private bool HasLineOfSight(Transform candidate)
        {
            if (candidate == null) return false;
            Camera camera = Camera.main;
            if (camera == null) return true;

            Vector3 origin = camera.transform.position;
            Vector3 point = candidate.position + Vector3.up * targetAimHeight;
            Vector3 delta = point - origin;
            float distance = delta.magnitude;
            if (distance <= 0.05f) return true;

            RaycastHit[] hits = Physics.RaycastAll(
                origin,
                delta / distance,
                distance,
                ~0,
                QueryTriggerInteraction.Ignore);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            for (int i = 0; i < hits.Length; i++)
            {
                Transform hit = hits[i].transform;
                if (hit == null) continue;
                if (hit == transform || hit.IsChildOf(transform)) continue;
                if (hit == candidate || hit.IsChildOf(candidate) || candidate.IsChildOf(hit)) return true;
                return false;
            }
            return true;
        }

        private void SetState(bool locked, Transform candidate)
        {
            bool beforeLocked = Locked;
            Transform beforeTarget = _lockedTarget;

            _locked = locked && TargetAvailable(candidate);
            _lockedTarget = _locked ? candidate : null;

            if (beforeTarget != _lockedTarget)
            {
                TargetChanged?.Invoke(_lockedTarget);
                Debug.Log($"[Mindforge:TargetLock] Target -> {(_lockedTarget != null ? _lockedTarget.name : "NONE")} by conventional player input.");
            }

            bool afterLocked = Locked;
            if (beforeLocked != afterLocked)
            {
                LockChanged?.Invoke(afterLocked);
                Debug.Log($"[Mindforge:TargetLock] {(afterLocked ? "LOCKED" : "UNLOCKED")} by conventional player input.");
            }
        }

        private bool TargetAvailable(Transform candidate)
        {
            if (candidate == null || !candidate.gameObject.activeInHierarchy) return false;
            CombatantVitals vitals = candidate.GetComponentInParent<CombatantVitals>();
            if (vitals == null) vitals = candidate.GetComponent<CombatantVitals>();
            return vitals == null || (vitals.Team == CombatTeam.Enemy && vitals.IsAlive);
        }

        private Vector3 HorizontalDirectionTo(Transform candidate)
        {
            if (candidate == null) return Vector3.zero;
            Vector3 delta = candidate.position - transform.position;
            delta.y = 0f;
            return delta.sqrMagnitude > 0.001f ? delta.normalized : Vector3.zero;
        }

        private float HorizontalDistanceTo(Transform candidate)
        {
            if (candidate == null) return float.PositiveInfinity;
            Vector3 a = Vector3.ProjectOnPlane(transform.position, Vector3.up);
            Vector3 b = Vector3.ProjectOnPlane(candidate.position, Vector3.up);
            return Vector3.Distance(a, b);
        }
    }
}
