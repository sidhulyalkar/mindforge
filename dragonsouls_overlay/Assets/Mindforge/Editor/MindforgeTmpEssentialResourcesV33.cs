#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Mindforge.Chassis.Editor
{
    /// <summary>
    /// Repairs the one project-level dependency TextMesh Pro does not install through
    /// UPM alone: its Essential Resources. Dragon Souls uses TMP and Mindforge creates
    /// runtime TMP labels, so a materialized chassis without TMP Settings otherwise
    /// throws from TMP_Settings.defaultStyleSheet before gameplay qualification begins.
    ///
    /// The repair is local to the materialized Unity project. It does not vendor or
    /// commit package resources into the Mindforge overlay.
    /// </summary>
    [InitializeOnLoad]
    public static class MindforgeTmpEssentialResourcesV33
    {
        private const string TmpSettingsResource = "TMP Settings";
        private const string SessionAttemptKey = "Mindforge.V33.TmpEssentialResourcesAttempted";

        static MindforgeTmpEssentialResourcesV33()
        {
            EditorApplication.delayCall += AutoRepairIfMissing;
        }

        [MenuItem("Mindforge/Chassis/Repair TextMesh Pro Essential Resources", priority = 22)]
        public static void RepairFromMenu()
        {
            SessionState.EraseBool(SessionAttemptKey);
            EnsureEssentialResources(logWhenHealthy: true);
        }

        [MenuItem("Mindforge/Chassis/Repair TextMesh Pro Essential Resources", true)]
        private static bool ValidateRepairFromMenu()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        private static void AutoRepairIfMissing()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            EnsureEssentialResources(logWhenHealthy: false);
        }

        private static void EnsureEssentialResources(bool logWhenHealthy)
        {
            if (Resources.Load<TMP_Settings>(TmpSettingsResource) != null)
            {
                if (logWhenHealthy)
                    Debug.Log("[Mindforge:TMP] Essential Resources already present.");
                return;
            }

            if (SessionState.GetBool(SessionAttemptKey, false))
            {
                if (logWhenHealthy)
                {
                    Debug.LogError(
                        "[Mindforge:TMP] TMP Settings are still missing after an import attempt. " +
                        "Use Window > TextMeshPro > Import TMP Essential Resources, then rerun the Mindforge repair audit."
                    );
                }
                return;
            }

            SessionState.SetBool(SessionAttemptKey, true);
            Debug.LogWarning(
                "[Mindforge:TMP] TMP Settings are missing. Importing the package's Essential Resources before native play."
            );

            try
            {
                TMP_PackageUtilities.ImportProjectResourcesMenu();
            }
            catch (System.Exception exc)
            {
                Debug.LogError(
                    "[Mindforge:TMP] Automatic Essential Resources import failed: " + exc.Message +
                    ". Use Window > TextMeshPro > Import TMP Essential Resources and rerun."
                );
                return;
            }

            EditorApplication.delayCall += VerifyAfterImport;
        }

        private static void VerifyAfterImport()
        {
            bool ready = Resources.Load<TMP_Settings>(TmpSettingsResource) != null;
            if (ready)
            {
                Debug.Log("[Mindforge:TMP] Essential Resources repair PASS; TMP Settings resolved.");
            }
            else
            {
                Debug.LogError(
                    "[Mindforge:TMP] Essential Resources import completed but TMP Settings still do not resolve. " +
                    "Do not qualify native gameplay until Window > TextMeshPro > Import TMP Essential Resources succeeds."
                );
            }
        }
    }
}
#endif
