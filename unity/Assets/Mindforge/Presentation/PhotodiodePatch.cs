using UnityEngine;
using UnityEngine.UI;
using Mindforge.SoulWisp;

namespace Mindforge.Presentation
{
    /// <summary>
    /// Qualification-only photodiode patch. It mirrors the Sight VEP core's high/low
    /// phase and holds low during stimulus rest. The patch verifies presentation
    /// timing/phase edges, not the aura's exact emitted luminance amplitude.
    ///
    /// During human sessions this square must be physically occluded by the
    /// photodiode or disabled, otherwise it becomes an additional 10 Hz stimulus.
    /// </summary>
    public sealed class PhotodiodePatch : MonoBehaviour
    {
        [SerializeField] private VepAuraStimulus sightStimulus;
        [SerializeField] private Image patch;
        [SerializeField] private KeyCode toggleKey = KeyCode.F10;
        [SerializeField] private bool visibleAtStartup;

        public bool Visible { get; private set; }

        private void Awake() => SetVisible(visibleAtStartup);

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey)) SetVisible(!Visible);
            if (!Visible || patch == null || sightStimulus == null) return;
            patch.color = sightStimulus.IsHighPhase ? Color.white : Color.black;
        }

        public void SetVisible(bool visible)
        {
            Visible = visible;
            if (patch != null) patch.gameObject.SetActive(visible);
        }
    }
}
