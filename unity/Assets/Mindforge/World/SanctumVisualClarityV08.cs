using UnityEngine;

namespace Mindforge.World
{
    /// <summary>
    /// Presentation-only clarity policy for the bright Sanctum. It never moves the camera,
    /// player or gameplay objects and never changes BCI timing. The policy only asks Unity
    /// to preserve distant architectural definition and conventional texture/shadow quality
    /// on desktop-class builds.
    /// </summary>
    [DefaultExecutionOrder(-620)]
    public sealed class SanctumVisualClarityV08 : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private float minimumFarClip = 420f;
        [SerializeField] private float maximumNearClip = 0.10f;
        [SerializeField] private float desktopShadowDistance = 85f;
        [SerializeField] private bool forceAnisotropicFiltering = true;

        public Camera TargetCamera => targetCamera;

        private void OnEnable()
        {
            Resolve();
            ApplyPresentationPolicy();
        }

        public void ConfigureRuntime(Camera camera)
        {
            targetCamera = camera;
            ApplyPresentationPolicy();
        }

        private void Resolve()
        {
            if (targetCamera == null) targetCamera = Camera.main;
            if (targetCamera == null) targetCamera = UnityEngine.Object.FindObjectOfType<Camera>(true);
        }

        private void ApplyPresentationPolicy()
        {
            if (targetCamera != null)
            {
                targetCamera.allowHDR = true;
                targetCamera.allowMSAA = true;
                targetCamera.useOcclusionCulling = true;
                targetCamera.farClipPlane = Mathf.Max(targetCamera.farClipPlane, minimumFarClip);
                targetCamera.nearClipPlane = Mathf.Min(targetCamera.nearClipPlane, maximumNearClip);
            }

            if (forceAnisotropicFiltering)
                QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;

            if (!Application.isMobilePlatform)
            {
                QualitySettings.shadowDistance = Mathf.Max(QualitySettings.shadowDistance, desktopShadowDistance);
                QualitySettings.shadowCascades = Mathf.Max(QualitySettings.shadowCascades, 4);
            }
        }
    }
}
