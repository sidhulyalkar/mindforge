using System.Collections;
using UnityEngine;
using Mindforge.Combat;
using Mindforge.Presentation;

namespace Mindforge.World
{
    /// <summary>
    /// Binds GuardianWorldSafety to the clean V0.11 collision envelope after all global
    /// runtime installers have had a chance to create their components.
    /// </summary>
    public sealed class MindforgeDemoV11SafetyProfile : MonoBehaviour
    {
        private static readonly Vector2 DemoXBounds = new Vector2(-14.5f, 14.5f);
        private static readonly Vector2 DemoZBounds = new Vector2(-25.5f, 108.5f);
        private const float DemoRecoveryHeight = -4.0f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            MindforgeDemoV11Marker marker = Object.FindObjectOfType<MindforgeDemoV11Marker>(true);
            if (marker == null || marker.GetComponent<MindforgeDemoV11SafetyProfile>() != null) return;
            marker.gameObject.AddComponent<MindforgeDemoV11SafetyProfile>();
        }

        private IEnumerator Start()
        {
            GuardianMotor motor = null;
            GuardianWorldSafety safety = null;
            for (int frame = 0; frame < 120; frame++)
            {
                if (motor == null) motor = Object.FindObjectOfType<GuardianMotor>(true);
                if (motor != null && safety == null) safety = motor.GetComponent<GuardianWorldSafety>();
                if (motor != null && safety != null) break;
                yield return null;
            }

            if (safety == null)
            {
                Debug.LogError("[Mindforge:V11] GuardianWorldSafety was not available for the demo profile.");
                yield break;
            }

            safety.ConfigureBounds(DemoXBounds, DemoZBounds, DemoRecoveryHeight);
            Debug.Log(
                $"[Mindforge:V11] World safety rebound to visible demo envelope " +
                $"x=[{DemoXBounds.x:0.0},{DemoXBounds.y:0.0}] z=[{DemoZBounds.x:0.0},{DemoZBounds.y:0.0}].");
        }
    }
}
