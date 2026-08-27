using UnityEngine;

namespace Mindforge.Combat
{
    /// <summary>Competition runtime timing policy. VEP timing remains realtime-based.</summary>
    public sealed class CombatBootstrap : MonoBehaviour
    {
        [SerializeField] private int targetFixedHz = 120;
        [SerializeField] private int targetRenderHz = 120;
        [SerializeField] private bool requestVSync = true;
        [SerializeField] private Rigidbody[] continuousBodies;

        private void Awake()
        {
            targetFixedHz = Mathf.Clamp(targetFixedHz, 50, 240);
            targetRenderHz = Mathf.Clamp(targetRenderHz, 60, 240);
            Time.fixedDeltaTime = 1f / targetFixedHz;
            Application.targetFrameRate = targetRenderHz;
            Application.runInBackground = true;
            if (requestVSync) QualitySettings.vSyncCount = 1;
            foreach (Rigidbody body in continuousBodies)
            {
                if (body == null) continue;
                body.interpolation = RigidbodyInterpolation.Interpolate;
                body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            }
        }
    }
}
