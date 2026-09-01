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
    ///
    /// Current V0.24/V0.25 cathedral scenes already own their horizon/enclosure and use the
    /// V0.17 collision-aware camera. In that path the historical cube skyline and renderer-only
    /// occlusion ghost are retired rather than stacked over the modern world authority.
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
                Destroy(gameObject);
                yield break;
            }

            LegacyMaterialHierarchyV16 materialHierarchy = gameObject.AddComponent<LegacyMaterialHierarchyV16>();
            materialHierarchy.Configure(calibration, wisp);

            bool modernCathedralAuthority =
                FindSceneObject("Mindforge_Sensory_Fidelity_V25") != null ||
                FindSceneObject("Mindforge_White_Cathedral_V24") != null;

            if (!modernCathedralAuthority)
            {
                CameraOcclusionGhostV16 occlusion = gameObject.AddComponent<CameraOcclusionGhostV16>();
                occlusion.Configure(camera, guardianVitals.transform, calibration, wisp);

                WorldDepthBackdropV16 backdrop = gameObject.AddComponent<WorldDepthBackdropV16>();
                backdrop.Configure(guardianVitals.transform, calibration, wisp);
            }

            CombatSilhouetteV16 silhouettes = gameObject.AddComponent<CombatSilhouetteV16>();
            silhouettes.Configure(guardianVitals.transform, targetLock, calibration, wisp);

            Debug.Log(modernCathedralAuthority
                ? "[Mindforge:V16] Current cathedral authority detected: legacy material safeguards + combat silhouettes retained; obsolete runtime cube skyline and renderer-only occlusion ghost retired."
                : "[Mindforge:V16] Legacy visual identity installed: material hierarchy, camera occlusion readability, layered horizon depth and combat silhouettes.");
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
