using System.Collections;
using UnityEngine;
using Mindforge.Combat;

namespace Mindforge.Presentation
{
    /// <summary>
    /// Composes the legacy visual showcase around the already-authoritative competition scene.
    /// V0.11 scenes carry MindforgeDemoV11Marker and intentionally bypass this historical
    /// presentation stack so camera, HUD, character shell and VFX have one owner.
    /// </summary>
    public sealed class ShowcaseRuntimeInstaller : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (FindObjectOfType<MindforgeDemoV11Marker>(true) != null) return;
            if (FindObjectOfType<ShowcaseRuntimeInstaller>(true) != null) return;
            new GameObject("MindforgeShowcaseRuntime").AddComponent<ShowcaseRuntimeInstaller>();
        }

        private IEnumerator Start()
        {
            GuardianCombatInput input = null;
            FracturedSignalDirector boss = null;
            Camera camera = null;

            for (int frame = 0; frame < 120; frame++)
            {
                if (input == null) input = FindObjectOfType<GuardianCombatInput>(true);
                if (boss == null) boss = FindObjectOfType<FracturedSignalDirector>(true);
                if (camera == null) camera = Camera.main;
                if (input != null && boss != null && camera != null) break;
                yield return null;
            }

            if (input == null || boss == null || camera == null)
            {
                Debug.LogError("[Mindforge:Showcase] Missing Guardian, Fractured Signal, or gameplay camera; presentation stack not installed.");
                yield break;
            }

            GameObject guardian = input.gameObject;
            GuardianTargetLock targetLock = guardian.GetComponent<GuardianTargetLock>();
            if (targetLock == null) targetLock = guardian.AddComponent<GuardianTargetLock>();
            targetLock.Configure(boss.transform);

            if (guardian.GetComponent<GuardianAvatarPresentation>() == null)
                guardian.AddComponent<GuardianAvatarPresentation>();
            if (guardian.GetComponent<GuardianMotionPolish>() == null)
                guardian.AddComponent<GuardianMotionPolish>();
            if (guardian.GetComponent<GuardianPresentationHierarchyBinder>() == null)
                guardian.AddComponent<GuardianPresentationHierarchyBinder>();
            if (guardian.GetComponent<GuardianLocomotionVfx>() == null)
                guardian.AddComponent<GuardianLocomotionVfx>();
            if (guardian.GetComponent<GuardianAnimatorBridge>() == null)
                guardian.AddComponent<GuardianAnimatorBridge>();
            if (guardian.GetComponent<CinematicArmamentVfxPolish>() == null)
                guardian.AddComponent<CinematicArmamentVfxPolish>();

            if (boss.GetComponent<FracturedSignalAvatar>() == null)
                boss.gameObject.AddComponent<FracturedSignalAvatar>();
            if (boss.GetComponent<FracturedSignalMotionPolish>() == null)
                boss.gameObject.AddComponent<FracturedSignalMotionPolish>();
            if (boss.GetComponent<FracturedSignalAnimatorBridge>() == null)
                boss.gameObject.AddComponent<FracturedSignalAnimatorBridge>();

            FracturedSignalMeleeDirector melee = null;
            for (int frame = 0; frame < 60 && melee == null; frame++)
            {
                melee = boss.GetComponent<FracturedSignalMeleeDirector>();
                if (melee == null) yield return null;
            }
            if (melee != null && gameObject.GetComponent<FracturedSignalMeleePresentation>() == null)
                gameObject.AddComponent<FracturedSignalMeleePresentation>();

            CombatPresentationDirector presentation = FindObjectOfType<CombatPresentationDirector>(true);
            GameObject cameraRigObject = presentation != null ? presentation.gameObject : camera.transform.root.gameObject;
            ShowcaseCameraRig cameraRig = cameraRigObject.GetComponent<ShowcaseCameraRig>();
            if (cameraRig == null) cameraRig = cameraRigObject.AddComponent<ShowcaseCameraRig>();
            cameraRig.Configure(
                guardian.transform,
                boss.transform,
                guardian.GetComponent<GuardianMotor>(),
                targetLock,
                camera);

            if (GetComponent<ArenaVisibilityDirector>() == null)
                gameObject.AddComponent<ArenaVisibilityDirector>();
            if (GetComponent<CombatVfxOrchestrator>() == null)
                gameObject.AddComponent<CombatVfxOrchestrator>();
            ShowcasePostProcessing post = GetComponent<ShowcasePostProcessing>();
            if (post == null) post = gameObject.AddComponent<ShowcasePostProcessing>();
            post.Configure(camera, presentation);

            if (GetComponent<CinematicRuntimeMaterialOverride>() == null)
                gameObject.AddComponent<CinematicRuntimeMaterialOverride>();
            if (GetComponent<CinematicArtOverrideInstaller>() == null)
                gameObject.AddComponent<CinematicArtOverrideInstaller>();

            Debug.Log(
                "[Mindforge:Showcase] Third-person ARPG presentation installed: behind-Guardian orbit camera, " +
                "mouse/trackpad + arrow orbit, conventional T target lock, camera-relative WASD, " +
                "readable Arena V3 lighting, coherent Guardian/armament presentation, truthful telegraphs, " +
                "cinematic URP and PBR rebinding. Target lock is conventional player state; EEG cannot create it.");
        }
    }
}
