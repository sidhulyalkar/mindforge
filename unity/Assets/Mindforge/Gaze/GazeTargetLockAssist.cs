using System.Collections.Generic;
using Mindforge.Combat;
using UnityEngine;

namespace Mindforge.Gaze
{
    /// <summary>
    /// Applies gaze only as a target preference after the player has pressed the existing
    /// target-lock key. GuardianTargetLock still owns lock authority and all attack timing.
    /// This deliberately prevents the eye-tracking "Midas touch" failure mode.
    /// </summary>
    [DefaultExecutionOrder(10000)]
    public sealed class GazeTargetLockAssist : MonoBehaviour
    {
        [SerializeField] private GuardianTargetLock targetLock;
        [SerializeField] private GazeAttentionRouter attention;
        [SerializeField] private int maximumCyclesPerConfirmation = 24;
        [SerializeField] private bool logConfirmedPreference;

        public void Configure(GuardianTargetLock playerTargetLock, GazeAttentionRouter router)
        {
            targetLock = playerTargetLock;
            attention = router;
        }

        private void Update()
        {
            if (targetLock == null) targetLock = GetComponent<GuardianTargetLock>();
            if (attention == null) attention = FindObjectOfType<GazeAttentionRouter>();
            if (targetLock == null || attention == null) return;

            // GuardianTargetLock runs at default execution order. At this point a T press has
            // either created a player-owned lock or released an existing one. We only refine
            // the newly confirmed lock; gaze never synthesizes that confirmation itself.
            if (!Input.GetKeyDown(targetLock.ToggleKey) || !targetLock.Locked) return;
            if (!attention.TryGetStableEnemy(out Transform desired) || desired == null) return;
            if (targetLock.Target == desired) return;

            HashSet<Transform> visited = new HashSet<Transform>();
            int limit = Mathf.Clamp(maximumCyclesPerConfirmation, 1, 64);
            for (int i = 0; i < limit && targetLock.Locked; i++)
            {
                Transform before = targetLock.Target;
                if (before == null || !visited.Add(before)) break;
                if (!targetLock.Cycle(1)) break;
                if (targetLock.Target == desired)
                {
                    if (logConfirmedPreference)
                        Debug.Log($"[Mindforge:Gaze] Player-confirmed gaze target -> {desired.name}");
                    return;
                }
            }
        }
    }
}
