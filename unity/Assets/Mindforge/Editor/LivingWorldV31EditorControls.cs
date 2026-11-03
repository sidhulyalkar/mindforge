#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Mindforge.LivingWorld;

namespace Mindforge.Editor
{
    /// <summary>Explicit preview controls for authored V11 game route only.</summary>
    public static class LivingWorldV31EditorControls
    {
        // Must match LivingWorldExperienceV31 preference keys.
        private const string EnablePreference = "mindforge.living_world.v31.enabled";

        [MenuItem("Mindforge/Living World/V3.1/Enable Opt-in Preview")]
        public static void Enable()
        {
            PlayerPrefs.SetInt(EnablePreference, 1);
            PlayerPrefs.Save();
            LivingWorldExperienceV31 experience = Object.FindObjectOfType<LivingWorldExperienceV31>(true);
            if (EditorApplication.isPlaying && experience != null)
                experience.SetOptIn(true);
            Debug.Log("[LivingWorld:V31] Explicitly opted into passive scenery; no EEG claims.");
        }

        [MenuItem("Mindforge/Living World/V3.1/Disable and Restore Baseline")]
        public static void Disable()
        {
            PlayerPrefs.SetInt(EnablePreference, 0);
            PlayerPrefs.Save();
            LivingWorldExperienceV31 experience = Object.FindObjectOfType<LivingWorldExperienceV31>(true);
            if (EditorApplication.isPlaying && experience != null)
                experience.SetOptIn(false);
            Debug.Log("[LivingWorld:V31] Passive visual integration disabled.");
        }

        [MenuItem("Mindforge/Living World/V3.1/Enable Opt-in Preview", true)]
        private static bool ValidateEnable()
        {
            return PlayerPrefs.GetInt(EnablePreference, 0) == 0;
        }

        [MenuItem("Mindforge/Living World/V3.1/Disable and Restore Baseline", true)]
        private static bool ValidateDisable()
        {
            return PlayerPrefs.GetInt(EnablePreference, 0) == 1;
        }
    }
}
#endif
