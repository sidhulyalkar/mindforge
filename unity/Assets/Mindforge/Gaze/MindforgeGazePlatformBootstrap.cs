using Mindforge.Combat;
using UnityEngine;

namespace Mindforge.Gaze
{
    /// <summary>
    /// Installs the optional gaze-attention lane in playable scenes without changing scene
    /// serialization. The lane is inert unless loopback GazeEvent samples are present.
    /// </summary>
    public static class MindforgeGazePlatformBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            UdpGazeReceiver receiver = Object.FindObjectOfType<UdpGazeReceiver>();
            GazeAttentionRouter router = Object.FindObjectOfType<GazeAttentionRouter>();
            GazeAttentionHud hud = Object.FindObjectOfType<GazeAttentionHud>();

            GameObject root = GameObject.Find("MindforgeGazePlatform");
            if (root == null) root = new GameObject("MindforgeGazePlatform");

            if (receiver == null) receiver = root.AddComponent<UdpGazeReceiver>();
            if (router == null) router = root.AddComponent<GazeAttentionRouter>();
            router.Bind(receiver);
            if (hud == null) root.AddComponent<GazeAttentionHud>();

            GuardianTargetLock[] locks = Object.FindObjectsOfType<GuardianTargetLock>(true);
            for (int i = 0; i < locks.Length; i++)
            {
                GuardianTargetLock targetLock = locks[i];
                if (targetLock == null) continue;
                GazeTargetLockAssist assist = targetLock.GetComponent<GazeTargetLockAssist>();
                if (assist == null) assist = targetLock.gameObject.AddComponent<GazeTargetLockAssist>();
                assist.Configure(targetLock, router);
            }
        }
    }
}
