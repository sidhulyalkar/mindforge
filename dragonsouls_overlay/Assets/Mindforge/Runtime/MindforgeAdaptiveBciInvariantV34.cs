using UnityEngine;

namespace Mindforge.Chassis
{
    /// <summary>
    /// Runtime sentry for the V0.34 authority boundary. Before the participant-specific
    /// layout is frozen, the temporal BCI targets stay hidden and any impossible
    /// calibration-in-progress state is cancelled. This catches execution-order/domain-
    /// reload surprises without granting the tutorial new neural or gameplay authority.
    /// </summary>
    [DefaultExecutionOrder(20000)]
    [DisallowMultipleComponent]
    public sealed class MindforgeAdaptiveBciInvariantV34 : MonoBehaviour
    {
        private MindforgeAdaptiveBciTutorialV34 _tutorial;
        private MindforgeAdaptiveStimulusLayoutV34 _layout;
        private MindforgeBciStimulusV33 _stimulus;
        private MindforgeBciCalibrationDirectorV33 _calibration;
        private bool _reportedCalibrationViolation;

        public int ViolationCount { get; private set; }

        private void Start()
        {
            ResolveDependencies();
        }

        private void LateUpdate()
        {
            ResolveDependencies();
            if (_tutorial == null || !_tutorial.Started || _tutorial.Finished) return;

            bool preFreeze = _layout == null || !_layout.Frozen;
            if (preFreeze && _stimulus != null)
                _stimulus.SetVisible(false);

            if (preFreeze && _calibration != null && _calibration.InProgress)
            {
                _calibration.ResetCalibration();
                ViolationCount++;
                if (!_reportedCalibrationViolation)
                {
                    _reportedCalibrationViolation = true;
                    Debug.LogError(
                        "[Mindforge:V34] Invariant stopped neural calibration before stimulus-layout freeze."
                    );
                }
            }
        }

        private void ResolveDependencies()
        {
            if (_tutorial == null) _tutorial = GetComponent<MindforgeAdaptiveBciTutorialV34>();
            if (_layout == null) _layout = GetComponent<MindforgeAdaptiveStimulusLayoutV34>();
            if (_stimulus == null) _stimulus = GetComponent<MindforgeBciStimulusV33>();
            if (_calibration == null) _calibration = GetComponent<MindforgeBciCalibrationDirectorV33>();
        }
    }
}
