using UnityEngine;

namespace Mindforge.Chassis
{
    /// <summary>
    /// Installs the V0.35 master tutorial on the V0.34 adaptive BCI scene.
    /// V0.31 remains combat authority and V0.33/V0.34 remain neural authority.
    /// V0.35 contributes only player-facing orchestration, evidence and presentation.
    /// </summary>
    [DefaultExecutionOrder(1350)]
    [DisallowMultipleComponent]
    public sealed class MindforgeMasterTutorialRuntimeV35 : MonoBehaviour
    {
        public const string ProductVersion = "V0.35 Master Combat + BCI Tutorial";

        public bool Installed { get; private set; }

        private void Start()
        {
            if (GetComponent<MindforgeBciIntegrationRuntimeV33>() == null ||
                GetComponent<MindforgeAdaptiveBciRuntimeV34>() == null)
            {
                Debug.LogError("[Mindforge:V35] V0.33/V0.34 authority stack missing; master tutorial refused to install.");
                enabled = false;
                return;
            }

            if (GetComponent<MindforgeCombatTutorialV35>() == null)
                gameObject.AddComponent<MindforgeCombatTutorialV35>();

            Installed = true;
            Debug.Log(
                "[Mindforge:V35] Master tutorial installed. Combat remains Dragon Souls-authoritative; " +
                "BCI remains V0.33/V0.34-authoritative; V0.35 observes and sequences evidence only."
            );
        }
    }
}
