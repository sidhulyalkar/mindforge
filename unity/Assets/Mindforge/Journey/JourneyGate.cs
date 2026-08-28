using UnityEngine;

namespace Mindforge.Journey
{
    /// <summary>
    /// Animated encounter seal. The gate is gameplay geometry, but its animation is
    /// deterministic and contains no combat/neural authority. Closing enables blockers
    /// immediately; opening retracts the visual seal and then disables collision.
    /// </summary>
    public sealed class JourneyGate : MonoBehaviour
    {
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Collider[] blockers;
        [SerializeField] private Vector3 openLocalOffset = new Vector3(0f, -4.6f, 0f);
        [SerializeField] private float transitionSharpness = 9.5f;

        private Vector3 _closedLocalPosition;
        private bool _open;
        private bool _initialized;

        public bool Open => _open;

        public void ConfigureRuntime(Transform visuals, Collider[] gateBlockers)
        {
            visualRoot = visuals;
            blockers = gateBlockers;
            CaptureClosedPose();
        }

        private void Awake() => CaptureClosedPose();

        public void SetOpen(bool open, bool immediate = false)
        {
            CaptureClosedPose();
            _open = open;
            if (!open) SetCollision(true);

            if (immediate && visualRoot != null)
            {
                visualRoot.localPosition = _closedLocalPosition + (open ? openLocalOffset : Vector3.zero);
                SetCollision(!open);
            }
        }

        private void Update()
        {
            if (!_initialized || visualRoot == null) return;
            Vector3 target = _closedLocalPosition + (_open ? openLocalOffset : Vector3.zero);
            float t = 1f - Mathf.Exp(-Mathf.Max(0.1f, transitionSharpness) * Time.unscaledDeltaTime);
            visualRoot.localPosition = Vector3.Lerp(visualRoot.localPosition, target, t);

            if (_open)
            {
                float remaining = (visualRoot.localPosition - target).sqrMagnitude;
                if (remaining < 0.08f) SetCollision(false);
            }
        }

        private void CaptureClosedPose()
        {
            if (_initialized) return;
            if (visualRoot == null) visualRoot = transform;
            _closedLocalPosition = visualRoot.localPosition;
            _initialized = true;
        }

        private void SetCollision(bool enabled)
        {
            if (blockers == null) return;
            for (int i = 0; i < blockers.Length; i++)
                if (blockers[i] != null) blockers[i].enabled = enabled;
        }
    }
}
