using UnityEngine;

namespace Mindforge.Combat
{
    public sealed class CombatBootstrap : MonoBehaviour
    {
        [SerializeField] private int targetFixedHz = 120;
        [SerializeField] private Rigidbody[] continuousBodies;

        private void Awake()
        {
            targetFixedHz = Mathf.Clamp(targetFixedHz, 50, 240);
            Time.fixedDeltaTime = 1f / targetFixedHz;
            foreach (Rigidbody body in continuousBodies)
            {
                if (body == null) continue;
                body.interpolation = RigidbodyInterpolation.Interpolate;
                body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            }
        }
    }
}
