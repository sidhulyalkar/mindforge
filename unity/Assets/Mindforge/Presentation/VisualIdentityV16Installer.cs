using System.Collections;
using UnityEngine;
using Mindforge.Calibration;
using Mindforge.Combat;
using Mindforge.SoulWisp;

namespace Mindforge.Presentation
{
    /// <summary>
    /// Runtime composition root for the V0.16 recording-driven visual identity pass.
    ///
    /// V0.16 is deliberately presentation-only. It reads authoritative Guardian, camera,
    /// target-lock and neural-window state, then installs removable visual helpers. It never
    /// moves the player, changes collision, creates damage, creates neural evidence, changes
    /// VEP timing, or owns target selection.
    /// </summary>
    [DefaultExecutionOrder(-55)]
    public sealed class VisualIdentityV16Installer : MonoBehaviour
    {
        public const string RootName = "Mindforge_VisualIdentity_V16";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (FindObjectOfType<VisualIdentityV16Installer>(true) != null) return;
            new GameObject(RootName).AddComponent<VisualIdentityV16Installer>();
        }

        private IEnumerator Start()
        {
            Camera camera = null;
            CombatantVitals guardianVitals = null;
            GuardianTargetLock targetLock = null;
            SoulWispController wisp = null;
            AwakeningCalibrationDirector calibration = null;

            for (int frame = 0; frame < 150; frame++)
            {
                if (camera == null) camera = Camera.main;
                if (guardianVitals == null) guardianVitals = FindGuardianVitals();
                if (guardianVitals != null && targetLock == null)
                    targetLock = guardianVitals.GetComponent<GuardianTargetLock>();
                if (wisp == null) wisp = FindObjectOfType<SoulWispController>(true);
                if (calibration == null) calibration = FindObjectOfType<AwakeningCalibrationDirector>(true);

                if (camera != null && guardianVitals != null) break;
                yield return null;
            }

            if (camera == null || guardianVitals == null)
            {
                Destroy(gameObject);
                yield break;
            }

            if (!HasMindforgePresentation())
            {
                // Do not install this visual tranche into isolated test scenes that happen
                // to contain a CombatantVitals component.
                Destroy(gameObject);
                yield break;
            }

            LegacyMaterialHierarchyV16 materialHierarchy = gameObject.AddComponent<LegacyMaterialHierarchyV16>();
            materialHierarchy.Configure(calibration, wisp);

            CameraOcclusionGhostV16 occlusion = gameObject.AddComponent<CameraOcclusionGhostV16>();
            occlusion.Configure(camera, guardianVitals.transform, calibration, wisp);

            WorldDepthBackdropV16 backdrop = gameObject.AddComponent<WorldDepthBackdropV16>();
            backdrop.Configure(guardianVitals.transform, calibration, wisp);

            CombatSilhouetteV16 silhouettes = gameObject.AddComponent<CombatSilhouetteV16>();
            silhouettes.Configure(guardianVitals.transform, targetLock, calibration, wisp);

            Debug.Log(
                "[Mindforge:V16] Recording-driven visual identity installed: material hierarchy, " +
                "camera-safe visual ghosting, layered horizon depth, and combat silhouettes. " +
                "All layers remain presentation-only and freeze visibility changes during neural evidence windows.");
        }

        private static CombatantVitals FindGuardianVitals()
        {
            CombatantVitals[] all = FindObjectsOfType<CombatantVitals>(true);
            for (int i = 0; i < all.Length; i++)
                if (all[i] != null && all[i].Team == CombatTeam.Guardian)
                    return all[i];
            return null;
        }

        private static bool HasMindforgePresentation()
        {
            return FindSceneObject("Mindforge_AetheriaWorld_V1") != null ||
                   FindSceneObject("Mindforge_GroundedWorld_V1") != null ||
                   FindSceneObject("Mindforge_Demo_Environment_V15") != null ||
                   FindObjectOfType<MindforgeDemoV11Marker>(true) != null ||
                   FindObjectOfType<ProductionHudV09>(true) != null;
        }

        internal static GameObject FindSceneObject(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            GameObject[] roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            for (int r = 0; r < roots.Length; r++)
            {
                Transform[] transforms = roots[r].GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < transforms.Length; i++)
                    if (transforms[i] != null && transforms[i].name == name)
                        return transforms[i].gameObject;
            }
            return null;
        }
    }
}
