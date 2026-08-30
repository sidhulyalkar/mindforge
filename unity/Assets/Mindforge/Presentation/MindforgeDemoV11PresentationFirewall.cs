using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mindforge.Presentation
{
    /// <summary>
    /// Keeps the V0.11 review scene visually single-owner even when historical
    /// RuntimeInitializeOnLoad bootstraps execute later in Unity's startup sequence.
    /// This component never disables gameplay, locomotion, damage, BCI receivers,
    /// telemetry, target lock or the Physical Arsenal. It suppresses presentation-only
    /// layers that would otherwise stack a second HUD/camera/avatar/VFX language.
    /// </summary>
    public sealed class MindforgeDemoV11PresentationFirewall : MonoBehaviour
    {
        private static readonly HashSet<string> SuppressedPresentationTypes = new HashSet<string>(StringComparer.Ordinal)
        {
            "ArenaMenagerieHud",
            "GroundedCombatHud",
            "CombatStateHud",
            "NullWardHud",
            "FirstJourneyHud",
            "PlayerAgencyGuide",
            "GuardianEquipmentMenu",
            "NullWardArtOverrideInstaller",
            "AetherbladeVisualPolishV2",
            "ShowcaseRuntimeInstaller",
            "ShowcasePostProcessing",
            "CinematicRuntimeMaterialOverride",
            "CinematicArtOverrideInstaller",
            "GuardianAvatarPresentation",
            "GuardianMotionPolish",
            "GuardianPresentationHierarchyBinder",
            "GuardianLocomotionVfx",
            "GuardianAnimatorBridge",
            "CinematicArmamentVfxPolish",
            "ProductionGuardianV09",
            "ProductionHudV09",
            "FracturedSignalAvatar",
            "FracturedSignalMotionPolish",
            "FracturedSignalAnimatorBridge",
            "FracturedSignalMeleePresentation",
            "ShowcasePreviewBootstrap",
            "ControllerOnlyQualificationBootstrap",
        };

        private float _nextSweep;
        private bool _logged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            MindforgeDemoV11Marker marker = UnityEngine.Object.FindObjectOfType<MindforgeDemoV11Marker>(true);
            if (marker == null || marker.GetComponent<MindforgeDemoV11PresentationFirewall>() != null) return;
            marker.gameObject.AddComponent<MindforgeDemoV11PresentationFirewall>();
        }

        private void OnEnable()
        {
            Sweep();
            _nextSweep = Time.unscaledTime + 0.25f;
        }

        private void Update()
        {
            if (Time.unscaledTime < _nextSweep) return;
            _nextSweep = Time.unscaledTime + 0.50f;
            Sweep();
        }

        private void Sweep()
        {
            int disabled = 0;
            MonoBehaviour[] behaviours = Resources.FindObjectsOfTypeAll<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour == null || behaviour == this || !behaviour.gameObject.scene.IsValid()) continue;
                if (!SuppressedPresentationTypes.Contains(behaviour.GetType().Name)) continue;
                if (!behaviour.enabled) continue;
                behaviour.enabled = false;
                disabled++;
            }

            GameObject competitionHud = GameObject.Find("CompetitionHUD");
            if (competitionHud != null && competitionHud.activeSelf)
            {
                competitionHud.SetActive(false);
                disabled++;
            }

            GameObject legacyShowcase = GameObject.Find("MindforgeShowcaseRuntime");
            if (legacyShowcase != null && legacyShowcase.activeSelf)
            {
                legacyShowcase.SetActive(false);
                disabled++;
            }

            if (!_logged)
            {
                _logged = true;
                Debug.Log($"[Mindforge:V11] Presentation firewall active; suppressed legacy presentation layers={disabled}. Gameplay and BCI authority untouched.");
            }
        }
    }
}
