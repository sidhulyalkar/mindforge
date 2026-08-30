using System;
using UnityEngine;
using Mindforge.Journey;

namespace Mindforge.World
{
    /// <summary>
    /// Restores semantic V0.8 onboarding facts after profile-v2 has loaded. The actual gate
    /// remains JourneyGate authority; this adapter only reconciles its already-persisted
    /// profile fact with physical presentation at startup.
    /// </summary>
    [DefaultExecutionOrder(-620)]
    public sealed class OpeningExperiencePersistenceV08 : MonoBehaviour
    {
        [SerializeField] private WorldStateLedger ledger;
        [SerializeField] private OpeningExperienceDirectorV08 opening;
        [SerializeField] private JourneyGate sanctumThreshold;

        public void ConfigureRuntime(
            WorldStateLedger world,
            OpeningExperienceDirectorV08 director,
            JourneyGate threshold)
        {
            ledger = world;
            opening = director;
            sanctumThreshold = threshold;
        }

        private void Start()
        {
            if (ledger == null) ledger = FindObjectOfType<WorldStateLedger>(true);
            if (opening == null) opening = FindObjectOfType<OpeningExperienceDirectorV08>(true);

            if (ledger != null &&
                ledger.TryGetString("profile.opening.v08.phase", out string phaseText) &&
                Enum.TryParse(phaseText, true, out OpeningExperiencePhaseV08 restoredPhase))
                opening?.RestorePhase(restoredPhase);

            if (ledger != null &&
                ledger.TryGetBool("profile.opening.sanctum_threshold_unlocked", out bool unlocked) &&
                unlocked)
                sanctumThreshold?.SetOpen(true, true);
        }
    }
}
