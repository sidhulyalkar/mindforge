using Mindforge.Calibration;
using Mindforge.Combat;
using Mindforge.SoulWisp;
using UnityEngine;

namespace Mindforge.Presentation
{
    /// <summary>
    /// Narrow post-resolver for the V0.17 gameplay camera.
    ///
    /// It does not choose orbit, FOV, target lock or normal camera distance. It activates only
    /// when the locked target's rendered body intersects the camera-to-Guardian sight corridor,
    /// then makes a small lateral/upward displacement around that real renderer envelope.
    /// Neural visual fields disable the correction entirely.
    /// </summary>
    [DefaultExecutionOrder(645)]
    [RequireComponent(typeof(Camera))]
    public sealed class MindforgeActorOcclusionGuardV28 : MonoBehaviour
    {
        [SerializeField] private float corridorPadding = 0.42f;
        [SerializeField] private float maximumLateralCorrection = 1.55f;
        [SerializeField] private float maximumLiftCorrection = 0.58f;
        [SerializeField] private float correctionSharpness = 18f;

        private Camera _camera;
        private GuardianCombatInput _guardianInput;
        private GuardianTargetLock _targetLock;
        private AwakeningCalibrationDirector _calibration;
        private SoulWispController _wisp;
        private readonly Vector3[] _corners = new Vector3[8];

        public bool Correcting { get; private set; }

        private void Awake() => Resolve();
        private void OnEnable() => Resolve();

        private void Resolve()
        {
            if (_camera == null) _camera = GetComponent<Camera>();
            if (_guardianInput == null) _guardianInput = FindObjectOfType<GuardianCombatInput>(true);
            if (_guardianInput != null && _targetLock == null) _targetLock = _guardianInput.GetComponent<GuardianTargetLock>();
            if (_calibration == null) _calibration = FindObjectOfType<AwakeningCalibrationDirector>(true);
            if (_wisp == null) _wisp = FindObjectOfType<SoulWispController>(true);
        }

        private void LateUpdate()
        {
            Resolve();
            Correcting = false;
            if (_camera == null || _guardianInput == null || _targetLock == null) return;
            if (!_targetLock.Locked || _targetLock.Target == null || NeuralVisualFieldActive()) return;

            if (!TryRenderBounds(_targetLock.Target, out Bounds targetBounds)) return;

            Vector3 cameraPosition = transform.position;
            Vector3 guardianPoint = _guardianInput.transform.position + Vector3.up * 1.18f;
            Vector3 targetCenter = targetBounds.center;
            float horizontalRadius = Mathf.Max(targetBounds.extents.x, targetBounds.extents.z) + Mathf.Max(0.1f, corridorPadding);
            float verticalRadius = Mathf.Max(0.75f, targetBounds.extents.y * 0.82f);

            Vector3 segment = guardianPoint - cameraPosition;
            float segmentLengthSq = segment.sqrMagnitude;
            if (segmentLengthSq < 0.01f) return;
            float t = Mathf.Clamp01(Vector3.Dot(targetCenter - cameraPosition, segment) / segmentLengthSq);
            Vector3 closest = cameraPosition + segment * t;
            Vector3 delta = closest - targetCenter;
            Vector2 planar = new Vector2(delta.x, delta.z);

            bool cameraInside = targetBounds.SqrDistance(cameraPosition) < 0.0001f;
            bool corridorBlocked = t > 0.08f && t < 0.92f && planar.magnitude < horizontalRadius && Mathf.Abs(delta.y) < verticalRadius;
            if (!cameraInside && !corridorBlocked) return;

            Vector3 targetToGuardian = Vector3.ProjectOnPlane(guardianPoint - targetCenter, Vector3.up);
            if (targetToGuardian.sqrMagnitude < 0.01f) targetToGuardian = Vector3.forward;
            targetToGuardian.Normalize();
            Vector3 side = Vector3.Cross(Vector3.up, targetToGuardian).normalized;
            if (Vector3.Dot(cameraPosition - guardianPoint, side) < 0f) side = -side;

            float penetration = cameraInside
                ? 1f
                : Mathf.Clamp01((horizontalRadius - planar.magnitude) / Mathf.Max(0.2f, horizontalRadius));
            float lateral = Mathf.Lerp(0.38f, maximumLateralCorrection, penetration);
            float lift = Mathf.Lerp(0.10f, maximumLiftCorrection, penetration);
            Vector3 desired = cameraPosition + side * lateral + Vector3.up * lift;

            // Never let the guard pull the camera closer to the target than the current frame.
            Vector3 before = cameraPosition - targetCenter;
            Vector3 after = desired - targetCenter;
            float minimum = Mathf.Max(before.magnitude, horizontalRadius + 0.20f);
            if (after.magnitude < minimum && after.sqrMagnitude > 0.001f)
                desired = targetCenter + after.normalized * minimum;

            float dt = Mathf.Min(0.05f, Time.unscaledDeltaTime);
            float response = 1f - Mathf.Exp(-Mathf.Max(1f, correctionSharpness) * dt);
            transform.position = Vector3.Lerp(cameraPosition, desired, response);
            Correcting = true;
        }

        private bool TryRenderBounds(Transform target, out Bounds bounds)
        {
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
            bool has = false;
            bounds = default;
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
                if (renderer is ParticleSystemRenderer || renderer is TrailRenderer || renderer is LineRenderer) continue;
                Bounds b = renderer.bounds;
                if (b.size.sqrMagnitude < 0.0001f) continue;
                if (!has)
                {
                    bounds = b;
                    has = true;
                }
                else bounds.Encapsulate(b);
            }
            return has;
        }

        private bool NeuralVisualFieldActive()
        {
            return (_calibration != null && _calibration.CalibrationInProgress) ||
                   (_wisp != null && (_wisp.CalibrationStimuliActive || _wisp.ResonanceWindowActive));
        }
    }
}
