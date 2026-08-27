using UnityEngine;
using UnityEngine.UI;
using Mindforge.SoulWisp;

namespace Mindforge.Presentation
{
    /// <summary>
    /// Qualification-only photodiode patch. F10 toggles visibility. F11 switches
    /// between the Sight (10 Hz) and Guard (12 Hz) VEP phase clocks.
    ///
    /// During human sessions this square must be physically occluded by the diode or
    /// disabled, otherwise it becomes an additional peripheral SSVEP stimulus.
    /// </summary>
    public sealed class PhotodiodePatch : MonoBehaviour
    {
        public enum StimulusSource { Sight = 0, Guard = 1 }

        [SerializeField] private VepAuraStimulus sightStimulus;
        [SerializeField] private VepAuraStimulus guardStimulus;
        [SerializeField] private Image patch;
        [SerializeField] private KeyCode toggleKey = KeyCode.F10;
        [SerializeField] private KeyCode switchSourceKey = KeyCode.F11;
        [SerializeField] private bool visibleAtStartup;
        [SerializeField] private StimulusSource source = StimulusSource.Sight;

        public bool Visible { get; private set; }
        public StimulusSource Source => source;
        public VepAuraStimulus ActiveStimulus => source == StimulusSource.Guard ? guardStimulus : sightStimulus;
        public float ActiveFrequencyHz => ActiveStimulus != null ? ActiveStimulus.FrequencyHz : 0f;

        private void Awake() => SetVisible(visibleAtStartup);

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey)) SetVisible(!Visible);
            if (Input.GetKeyDown(switchSourceKey))
                source = source == StimulusSource.Sight ? StimulusSource.Guard : StimulusSource.Sight;

            VepAuraStimulus stimulus = ActiveStimulus;
            if (!Visible || patch == null || stimulus == null) return;
            patch.color = stimulus.IsHighPhase ? Color.white : Color.black;
        }

        public void SelectSight() => source = StimulusSource.Sight;
        public void SelectGuard() => source = StimulusSource.Guard;

        public void SetVisible(bool visible)
        {
            Visible = visible;
            if (patch != null) patch.gameObject.SetActive(visible);
        }
    }
}
