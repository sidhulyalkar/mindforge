using UnityEngine;

namespace Mindforge.Presentation
{
    /// <summary>
    /// Slow mechanical motion for the production resonance apparatus. This deliberately never
    /// modulates luminance or stimulus timing: calibrated/preview flicker remains exclusively on
    /// the existing SanctumCalibrationOrbV08 renderer and scientific timing path.
    /// </summary>
    public sealed class ProductionCalibrationPresentationV09 : MonoBehaviour
    {
        [SerializeField] private Transform phaseRingA;
        [SerializeField] private Transform phaseRingB;
        [SerializeField, Range(1f, 30f)] private float ringASpeedDeg = 8f;
        [SerializeField, Range(1f, 30f)] private float ringBSpeedDeg = 5f;

        public void ConfigureRuntime(Transform a, Transform b)
        {
            phaseRingA = a;
            phaseRingB = b;
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (phaseRingA != null)
                phaseRingA.Rotate(Vector3.up, ringASpeedDeg * dt, Space.Self);
            if (phaseRingB != null)
                phaseRingB.Rotate(Vector3.right, -ringBSpeedDeg * dt, Space.Self);
        }
    }
}
