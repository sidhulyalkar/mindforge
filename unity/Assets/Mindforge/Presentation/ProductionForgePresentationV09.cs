using UnityEngine;

namespace Mindforge.Presentation
{
    /// <summary>
    /// Presentation-only mechanical drift for the Memory Forge's physical rings. It never owns
    /// checkpoint state, interaction routing, persistence, luminance coding or player actions.
    /// </summary>
    public sealed class ProductionForgePresentationV09 : MonoBehaviour
    {
        [SerializeField] private Transform outerRing;
        [SerializeField] private Transform innerRing;
        [SerializeField, Range(1f, 20f)] private float outerSpeedDeg = 6f;
        [SerializeField, Range(1f, 20f)] private float innerSpeedDeg = 9f;

        public void ConfigureRuntime(Transform outer, Transform inner)
        {
            outerRing = outer;
            innerRing = inner;
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (outerRing != null) outerRing.Rotate(Vector3.up, outerSpeedDeg * dt, Space.Self);
            if (innerRing != null) innerRing.Rotate(Vector3.right, -innerSpeedDeg * dt, Space.Self);
        }
    }
}
