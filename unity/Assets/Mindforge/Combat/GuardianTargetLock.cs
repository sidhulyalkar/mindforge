using System;
using UnityEngine;

namespace Mindforge.Combat
{
    /// <summary>
    /// Conventional player-owned target lock for third-person combat.
    /// This component is deliberately neural-agnostic: T toggles lock state and the
    /// current target is exposed to camera, movement-facing and combat aim consumers.
    /// EEG never creates, changes or confirms a target lock.
    /// </summary>
    public sealed class GuardianTargetLock : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private KeyCode toggleKey = KeyCode.T;
        [SerializeField] private float lockRange = 28f;
        [SerializeField] private float breakRange = 34f;
        [SerializeField] private float targetAimHeight = 0.9f;

        private bool _locked;

        public event Action<bool> LockChanged;

        public bool Locked => _locked && TargetAvailable();
        public Transform Target => Locked ? target : null;
        public KeyCode ToggleKey => toggleKey;

        public void Configure(Transform combatTarget)
        {
            target = combatTarget;
            if (_locked && !TargetAvailable()) SetLocked(false);
        }

        public void SetLocked(bool locked)
        {
            bool desired = locked && CanAcquire();
            if (_locked == desired) return;
            _locked = desired;
            LockChanged?.Invoke(_locked);
            Debug.Log($"[Mindforge:TargetLock] {(_locked ? "LOCKED" : "UNLOCKED")} by conventional player input.");
        }

        public Vector3 AimPoint
        {
            get
            {
                Transform t = Target;
                return t != null ? t.position + Vector3.up * targetAimHeight : transform.position + transform.forward * 6f;
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
            if (Input.GetKeyDown(toggleKey)) SetLocked(!_locked);

            if (_locked && (!TargetAvailable() || HorizontalDistanceToTarget() > Mathf.Max(lockRange, breakRange)))
                SetLocked(false);
        }

        private bool CanAcquire()
        {
            return TargetAvailable() && HorizontalDistanceToTarget() <= Mathf.Max(1f, lockRange);
        }

        private bool TargetAvailable()
        {
            return target != null && target.gameObject.activeInHierarchy;
        }

        private float HorizontalDistanceToTarget()
        {
            if (target == null) return float.PositiveInfinity;
            Vector3 a = Vector3.ProjectOnPlane(transform.position, Vector3.up);
            Vector3 b = Vector3.ProjectOnPlane(target.position, Vector3.up);
            return Vector3.Distance(a, b);
        }
    }
}
