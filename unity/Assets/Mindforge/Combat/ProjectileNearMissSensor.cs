using System.Collections.Generic;
using UnityEngine;

namespace Mindforge.Combat
{
    [RequireComponent(typeof(SphereCollider))]
    public sealed class ProjectileNearMissSensor : MonoBehaviour
    {
        [SerializeField] private GuardianMotor motor;
        [SerializeField] private FluxMeter flux;
        [SerializeField] private CombatTuning tuning;
        [SerializeField] private float minSpeed = 4.5f;

        private readonly HashSet<int> _inside = new HashSet<int>();

        private void Awake()
        {
            SphereCollider c = GetComponent<SphereCollider>();
            c.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            MindforgeProjectile p = other.GetComponentInParent<MindforgeProjectile>();
            if (p != null && p.IsHostileToGuardian) _inside.Add(p.GetInstanceID());
        }

        private void OnTriggerExit(Collider other)
        {
            MindforgeProjectile p = other.GetComponentInParent<MindforgeProjectile>();
            if (p == null || !_inside.Remove(p.GetInstanceID()) || !p.IsHostileToGuardian) return;
            if (motor != null && motor.Velocity.magnitude >= minSpeed)
                flux?.Award(tuning != null ? tuning.nearMissFlux : 0.18f, "Thread the Needle");
        }
    }
}
