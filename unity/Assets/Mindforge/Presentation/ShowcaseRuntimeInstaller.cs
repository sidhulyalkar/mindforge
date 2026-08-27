using System.Collections;
using UnityEngine;
using Mindforge.Combat;

namespace Mindforge.Presentation
{
    /// <summary>
    /// Composes the visual showcase around the already-authoritative competition scene.
    /// All installed components are presentation-only and tolerate runtime bootstrap
    /// ordering with the physical arsenal.
    /// </summary>
    public sealed class ShowcaseRuntimeInstaller : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
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
            if (guardian.GetComponent<GuardianAvatarPresentation>() == null)
                guardian.AddComponent<GuardianAvatarPresentation>();
            if (guardian.GetComponent<GuardianMotionPolish>() == null)
                guardian.AddComponent<GuardianMotionPolish>();
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
            cameraRig.Configure(guardian.transform, boss.transform, guardian.GetComponent<GuardianMotor>(), camera);

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
                "[Mindforge:Showcase] Animation/graphics v3 installed: additive Guardian weight and recoil, " +
                "production Animator contracts, armament afterimages/motes, grounded locomotion particles, " +
                "Fractured Signal secondary motion, truthful telegraphs, tactical camera, semantic VFX, " +
                "cinematic URP, PBR rebinding and optional authored-art overrides.");
        }
    }
}
