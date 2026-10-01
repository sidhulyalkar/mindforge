using System;
using Cinemachine;
using States;
using UnityEngine;

namespace Mindforge.Chassis
{
    /// <summary>
    /// V0.35 presentation authority for ordinary third-person gameplay.
    ///
    /// The inherited Dragon Souls CameraController still owns camera switching and
    /// player/target authority. This component only retunes FreeLook/Target cameras,
    /// applies state-aware FOV, collision smoothing and sprint recentering, then
    /// measures the rendered player's real viewport bounds as a readability signal.
    ///
    /// Aim and bonfire framing are deliberately left alone.
    /// </summary>
    [DefaultExecutionOrder(1480)]
    [DisallowMultipleComponent]
    public sealed class MindforgeCharacterReadabilityCameraV35 : MonoBehaviour
    {
        public const string ProductVersion = "V0.35 Character Readability Camera";

        [Header("Free-roam composition")]
        [SerializeField, Range(48f, 62f)] private float freeLookFov = 52f;
        [SerializeField, Range(48f, 64f)] private float sprintFov = 55f;
        [SerializeField, Range(48f, 66f)] private float rollFov = 56f;
        [SerializeField] private float topHeight = 2.80f;
        [SerializeField] private float topRadius = 4.45f;
        [SerializeField] private float middleHeight = 1.50f;
        [SerializeField] private float middleRadius = 4.75f;
        [SerializeField] private float bottomHeight = 0.65f;
        [SerializeField] private float bottomRadius = 4.35f;
        [SerializeField, Range(0.35f, 0.65f)] private float freeLookScreenX = 0.50f;
        [SerializeField, Range(0.42f, 0.68f)] private float freeLookScreenY = 0.56f;
        [SerializeField] private float trackedBodyOffsetY = 1.30f;

        [Header("Target-combat composition")]
        [SerializeField, Range(50f, 68f)] private float targetFov = 56f;
        [SerializeField, Range(52f, 72f)] private float crowdedTargetFov = 59f;
        [SerializeField, Range(54f, 74f)] private float bossTargetFov = 62f;
        [SerializeField, Range(0.35f, 0.65f)] private float targetScreenX = 0.50f;
        [SerializeField, Range(0.42f, 0.68f)] private float targetScreenY = 0.54f;
        [SerializeField] private float minimumTargetFollowDistance = 5.6f;
        [SerializeField] private float crowdRadius = 9.0f;
        [SerializeField] private int crowdedEnemyThreshold = 3;
        [SerializeField] private float bossAwarenessDistance = 24f;

        [Header("Motion readability")]
        [SerializeField] private float fovResponseDegreesPerSecond = 12f;
        [SerializeField] private float composerDamping = 0.28f;
        [SerializeField] private float manualLookGraceSeconds = 0.70f;
        [SerializeField] private float sprintRecenterSeconds = 0.90f;
        [SerializeField] private float sprintRecenterWaitSeconds = 0.10f;
        [SerializeField] private float manualLookThreshold = 0.035f;

        [Header("Occlusion recovery")]
        [SerializeField] private float cameraCollisionRadius = 0.32f;
        [SerializeField] private float minimumCameraTargetDistance = 0.55f;
        [SerializeField] private float occlusionSmoothingSeconds = 0.10f;
        [SerializeField] private float occlusionDamping = 0.18f;
        [SerializeField] private float occlusionDampingWhenHidden = 0.07f;

        [Header("Screen-space guardrails")]
        [SerializeField, Range(0.01f, 0.20f)] private float horizontalSafeMargin = 0.055f;
        [SerializeField, Range(0.01f, 0.20f)] private float verticalSafeMargin = 0.060f;
        [SerializeField, Range(0f, 10f)] private float maximumFramingAssistFov = 4.0f;
        [SerializeField] private float framingAssistInSpeed = 16f;
        [SerializeField] private float framingAssistOutSpeed = 4f;
        [SerializeField] private float warningHoldSeconds = 0.45f;

        [Header("Resolution")]
        [SerializeField] private float retrySeconds = 0.25f;
        [SerializeField] private float retryWindowSeconds = 5f;
        [SerializeField] private float enemyRefreshSeconds = 0.75f;

        private PlayerStateMachine _player;
        private CameraController _controller;
        private CinemachineBrain _brain;
        private CinemachineFreeLook _freeLook;
        private CinemachineVirtualCamera _targetCam;
        private Camera _mainCamera;
        private SkinnedMeshRenderer[] _characterRenderers;
        private EnemyStateMachine[] _enemies;
        private EnemyNightmareDragonController _dragon;

        private float _startedAt;
        private float _nextRetryAt;
        private float _nextEnemyRefresh;
        private float _lastManualLookAt = float.NegativeInfinity;
        private float _framingAssistFov;
        private float _violationStartedAt = -1f;
        private float _nextWarningAt;
        private bool _staticConfigured;

        public bool Installed { get; private set; }
        public bool StaticCompositionConfigured => _staticConfigured;
        public string ActiveMode { get; private set; } = "unresolved";
        public Rect CharacterViewportBounds { get; private set; }
        public bool CharacterBoundsMeasured { get; private set; }
        public bool CharacterInsideSafeFrame { get; private set; }
        public int FramingViolationFrames { get; private set; }
        public int PersistentFramingWarnings { get; private set; }
        public float CharacterViewportHeight => CharacterBoundsMeasured ? CharacterViewportBounds.height : 0f;
        public float CurrentFreeLookFov => _freeLook != null ? _freeLook.m_Lens.FieldOfView : 0f;
        public float CurrentTargetFov => _targetCam != null ? _targetCam.m_Lens.FieldOfView : 0f;
        public float CurrentFramingAssistFov => _framingAssistFov;

        private void Awake()
        {
            _startedAt = Time.unscaledTime;
            ResolveAndConfigure();
        }

        private void Start()
        {
            ResolveAndConfigure();
        }

        private void LateUpdate()
        {
            if (!Installed)
            {
                if (Time.unscaledTime - _startedAt > retryWindowSeconds) return;
                if (Time.unscaledTime < _nextRetryAt) return;
                _nextRetryAt = Time.unscaledTime + retrySeconds;
                ResolveAndConfigure();
                return;
            }

            if (Time.unscaledTime >= _nextEnemyRefresh)
                RefreshEnemyCache();

            TrackManualCameraIntent();
            ActiveMode = ResolveActiveMode();

            bool presentationMode =
                string.Equals(ActiveMode, "free", StringComparison.Ordinal) ||
                string.Equals(ActiveMode, "target", StringComparison.Ordinal);

            bool blending = _brain != null && _brain.IsBlending;
            bool safe = true;
            Rect bounds;
            if (presentationMode && !blending && TryMeasureCharacterViewport(out bounds))
            {
                CharacterViewportBounds = bounds;
                CharacterBoundsMeasured = true;
                safe = IsInsideSafeFrame(bounds);
                CharacterInsideSafeFrame = safe;
                UpdateFramingGuardrail(safe);
            }
            else
            {
                CharacterBoundsMeasured = false;
                CharacterInsideSafeFrame = true;
                UpdateFramingGuardrail(true);
            }

            UpdateFreeLookPresentation();
            UpdateTargetPresentation();
            UpdateSprintRecentering();
        }

        private void ResolveAndConfigure()
        {
            if (_player == null) _player = FindObjectOfType<PlayerStateMachine>(true);
            if (_player == null || _player.cameraController == null)
            {
                Installed = false;
                return;
            }

            _controller = _player.cameraController;
            _brain = _controller._cinemachineBrain;
            _freeLook = _controller._cinemachineFreeLookCam;
            _targetCam = _controller._cinemachineTargetCam;
            _mainCamera = Camera.main;

            if (_freeLook == null || _targetCam == null || _mainCamera == null)
            {
                Installed = false;
                return;
            }

            _characterRenderers = _player.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            _dragon = FindObjectOfType<EnemyNightmareDragonController>(true);
            RefreshEnemyCache();
            ApplyStaticComposition();

            Installed = true;
            Debug.Log(
                "[Mindforge:V35:CAMERA] Character readability camera ready. " +
                $"free={freeLookFov:F1}° target={targetFov:F1}° crowd={crowdedTargetFov:F1}° boss={bossTargetFov:F1}°; " +
                $"freeScreen=({freeLookScreenX:F2},{freeLookScreenY:F2}) targetScreen=({targetScreenX:F2},{targetScreenY:F2}). " +
                "Aim/bonfire cameras remain inherited."
            );
        }

        private void ApplyStaticComposition()
        {
            ConfigureBrain();
            ConfigureFreeLook();
            ConfigureTargetCamera();
            _staticConfigured = true;
        }

        private void ConfigureBrain()
        {
            if (_brain == null) return;
            _brain.m_DefaultBlend =
                new CinemachineBlendDefinition(CinemachineBlendDefinition.Style.EaseInOut, 0.20f);
        }

        private void ConfigureFreeLook()
        {
            if (_freeLook == null) return;

            LensSettings lens = _freeLook.m_Lens;
            lens.FieldOfView = Mathf.Max(lens.FieldOfView, freeLookFov);
            lens.NearClipPlane = Mathf.Min(lens.NearClipPlane, 0.08f);
            _freeLook.m_Lens = lens;

            if (_freeLook.m_Orbits != null && _freeLook.m_Orbits.Length >= 3)
            {
                _freeLook.m_Orbits[0].m_Height = topHeight;
                _freeLook.m_Orbits[0].m_Radius = topRadius;
                _freeLook.m_Orbits[1].m_Height = middleHeight;
                _freeLook.m_Orbits[1].m_Radius = middleRadius;
                _freeLook.m_Orbits[2].m_Height = bottomHeight;
                _freeLook.m_Orbits[2].m_Radius = bottomRadius;
            }

            _freeLook.m_YAxis.Value = Mathf.Clamp01(_freeLook.m_YAxis.Value);
            _freeLook.m_RecenterToTargetHeading.m_WaitTime = sprintRecenterWaitSeconds;
            _freeLook.m_RecenterToTargetHeading.m_RecenteringTime = sprintRecenterSeconds;
            _freeLook.m_RecenterToTargetHeading.m_enabled = false;

            ConfigureCollider(_freeLook);

            for (int i = 0; i < 3; i++)
            {
                CinemachineVirtualCamera rig = _freeLook.GetRig(i);
                if (rig == null) continue;

                CinemachineComposer composer = rig.GetCinemachineComponent<CinemachineComposer>();
                if (composer != null)
                {
                    composer.m_TrackedObjectOffset = new Vector3(0f, trackedBodyOffsetY, 0f);
                    composer.m_ScreenX = freeLookScreenX;
                    composer.m_ScreenY = freeLookScreenY;
                    composer.m_DeadZoneWidth = 0.025f;
                    composer.m_DeadZoneHeight = 0.030f;
                    composer.m_SoftZoneWidth = 0.82f;
                    composer.m_SoftZoneHeight = 0.76f;
                    composer.m_HorizontalDamping = composerDamping;
                    composer.m_VerticalDamping = composerDamping;
                    composer.m_LookaheadTime = 0f;
                }

                ConfigureCollider(rig);
            }
        }

        private void ConfigureTargetCamera()
        {
            if (_targetCam == null) return;

            LensSettings lens = _targetCam.m_Lens;
            lens.FieldOfView = Mathf.Max(lens.FieldOfView, targetFov);
            lens.NearClipPlane = Mathf.Min(lens.NearClipPlane, 0.08f);
            _targetCam.m_Lens = lens;

            CinemachineComposer composer = _targetCam.GetCinemachineComponent<CinemachineComposer>();
            if (composer != null)
            {
                composer.m_ScreenX = targetScreenX;
                composer.m_ScreenY = targetScreenY;
                composer.m_DeadZoneWidth = 0.015f;
                composer.m_DeadZoneHeight = 0.020f;
                composer.m_SoftZoneWidth = 0.84f;
                composer.m_SoftZoneHeight = 0.78f;
                composer.m_HorizontalDamping = composerDamping;
                composer.m_VerticalDamping = composerDamping;
            }

            CinemachineTransposer transposer = _targetCam.GetCinemachineComponent<CinemachineTransposer>();
            if (transposer != null)
            {
                Vector3 offset = transposer.m_FollowOffset;
                offset.z = Mathf.Min(offset.z, -minimumTargetFollowDistance);
                transposer.m_FollowOffset = offset;
            }

            ConfigureCollider(_targetCam);
        }

        private void UpdateFreeLookPresentation()
        {
            if (_freeLook == null) return;

            float desired = freeLookFov;
            if (_player != null && _player.isRoll)
                desired = Mathf.Max(desired, rollFov);
            else if (_player != null && (_player.isSprinting || _player.isSprintHolding))
                desired = Mathf.Max(desired, sprintFov);

            desired += _framingAssistFov;
            LensSettings lens = _freeLook.m_Lens;
            lens.FieldOfView = Mathf.MoveTowards(
                lens.FieldOfView,
                desired,
                fovResponseDegreesPerSecond * Time.unscaledDeltaTime
            );
            _freeLook.m_Lens = lens;
        }

        private void UpdateTargetPresentation()
        {
            if (_targetCam == null) return;

            float desired = targetFov;
            if (CountNearbyEnemies() >= crowdedEnemyThreshold)
                desired = Mathf.Max(desired, crowdedTargetFov);

            if (_dragon != null && _player != null && _dragon.gameObject.activeInHierarchy)
            {
                Vector3 delta = _dragon.transform.position - _player.transform.position;
                delta.y = 0f;
                if (delta.sqrMagnitude <= bossAwarenessDistance * bossAwarenessDistance)
                    desired = Mathf.Max(desired, bossTargetFov);
            }

            if (string.Equals(ActiveMode, "target", StringComparison.Ordinal))
                desired += _framingAssistFov;

            LensSettings lens = _targetCam.m_Lens;
            lens.FieldOfView = Mathf.MoveTowards(
                lens.FieldOfView,
                desired,
                fovResponseDegreesPerSecond * Time.unscaledDeltaTime
            );
            _targetCam.m_Lens = lens;
        }

        private void TrackManualCameraIntent()
        {
            if (_player == null || _player.InputReader == null) return;
            if (_player.InputReader.CameraMovementOn2DAxis.sqrMagnitude >= manualLookThreshold * manualLookThreshold)
                _lastManualLookAt = Time.unscaledTime;
        }

        private void UpdateSprintRecentering()
        {
            if (_freeLook == null || _player == null) return;

            bool sprinting = _player.isSprinting || _player.isSprintHolding;
            bool moving = _player.movement != null && _player.movement.Velocity.sqrMagnitude > 0.25f;
            bool manualRecently = Time.unscaledTime - _lastManualLookAt < manualLookGraceSeconds;
            bool allowed =
                sprinting &&
                moving &&
                !manualRecently &&
                string.Equals(ActiveMode, "free", StringComparison.Ordinal);

            _freeLook.m_RecenterToTargetHeading.m_enabled = allowed;
        }

        private string ResolveActiveMode()
        {
            if (_controller == null || _controller._cinemachineStateDrivenCam == null)
                return "unresolved";

            ICinemachineCamera live = _controller._cinemachineStateDrivenCam.LiveChild;
            if (live == null) return "unresolved";

            if (_controller._cinemachineAimCam != null &&
                live.Priority == _controller._cinemachineAimCam.Priority)
                return "aim";
            if (_controller._cinemachineTargetCam != null &&
                live.Priority == _controller._cinemachineTargetCam.Priority)
                return "target";

            string name = live.Name ?? string.Empty;
            if (name.IndexOf("Bonfire", StringComparison.OrdinalIgnoreCase) >= 0)
                return "bonfire";
            if (name.IndexOf("FreeLook", StringComparison.OrdinalIgnoreCase) >= 0)
                return "free";

            return "other";
        }

        private void UpdateFramingGuardrail(bool safe)
        {
            float desiredAssist = safe ? 0f : maximumFramingAssistFov;
            float speed = desiredAssist > _framingAssistFov
                ? framingAssistInSpeed
                : framingAssistOutSpeed;

            _framingAssistFov = Mathf.MoveTowards(
                _framingAssistFov,
                desiredAssist,
                speed * Time.unscaledDeltaTime
            );

            if (safe)
            {
                _violationStartedAt = -1f;
                return;
            }

            FramingViolationFrames++;
            if (_violationStartedAt < 0f)
                _violationStartedAt = Time.unscaledTime;

            if (Time.unscaledTime - _violationStartedAt < warningHoldSeconds ||
                Time.unscaledTime < _nextWarningAt)
                return;

            _nextWarningAt = Time.unscaledTime + 2.0f;
            PersistentFramingWarnings++;
            Debug.LogWarning(
                "[Mindforge:V35:CAMERA] Persistent character framing pressure. " +
                $"mode={ActiveMode} viewport={FormatRect(CharacterViewportBounds)} " +
                $"assist={_framingAssistFov:F1}°."
            );
        }

        private bool TryMeasureCharacterViewport(out Rect viewport)
        {
            viewport = default;
            if (_mainCamera == null) _mainCamera = Camera.main;
            if (_mainCamera == null || _characterRenderers == null || _characterRenderers.Length == 0)
                return false;

            bool hasBounds = false;
            Bounds worldBounds = default;
            for (int i = 0; i < _characterRenderers.Length; i++)
            {
                SkinnedMeshRenderer renderer = _characterRenderers[i];
                if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                    continue;

                if (!hasBounds)
                {
                    worldBounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    worldBounds.Encapsulate(renderer.bounds);
                }
            }

            if (!hasBounds) return false;

            Vector3 min = worldBounds.min;
            Vector3 max = worldBounds.max;
            Vector3[] corners =
            {
                new Vector3(min.x, min.y, min.z),
                new Vector3(min.x, min.y, max.z),
                new Vector3(min.x, max.y, min.z),
                new Vector3(min.x, max.y, max.z),
                new Vector3(max.x, min.y, min.z),
                new Vector3(max.x, min.y, max.z),
                new Vector3(max.x, max.y, min.z),
                new Vector3(max.x, max.y, max.z),
            };

            float minX = float.PositiveInfinity;
            float maxX = float.NegativeInfinity;
            float minY = float.PositiveInfinity;
            float maxY = float.NegativeInfinity;

            for (int i = 0; i < corners.Length; i++)
            {
                Vector3 v = _mainCamera.WorldToViewportPoint(corners[i]);
                if (v.z <= 0.01f) return false;
                minX = Mathf.Min(minX, v.x);
                maxX = Mathf.Max(maxX, v.x);
                minY = Mathf.Min(minY, v.y);
                maxY = Mathf.Max(maxY, v.y);
            }

            viewport = Rect.MinMaxRect(minX, minY, maxX, maxY);
            return true;
        }

        private bool IsInsideSafeFrame(Rect viewport)
        {
            return viewport.xMin >= horizontalSafeMargin &&
                   viewport.xMax <= 1f - horizontalSafeMargin &&
                   viewport.yMin >= verticalSafeMargin &&
                   viewport.yMax <= 1f - verticalSafeMargin;
        }

        private void ConfigureCollider(CinemachineVirtualCameraBase camera)
        {
            if (camera == null) return;
            CinemachineCollider collider = camera.GetComponent<CinemachineCollider>();
            if (collider == null) return;

            collider.m_CameraRadius = Mathf.Max(collider.m_CameraRadius, cameraCollisionRadius);
            collider.m_MinimumDistanceFromTarget =
                Mathf.Max(collider.m_MinimumDistanceFromTarget, minimumCameraTargetDistance);
            collider.m_SmoothingTime = occlusionSmoothingSeconds;
            collider.m_Damping = occlusionDamping;
            collider.m_DampingWhenOccluded = occlusionDampingWhenHidden;
            collider.m_MaximumEffort = Mathf.Max(collider.m_MaximumEffort, 6);
        }

        private void RefreshEnemyCache()
        {
            _enemies = FindObjectsOfType<EnemyStateMachine>(true);
            _nextEnemyRefresh = Time.unscaledTime + enemyRefreshSeconds;
        }

        private int CountNearbyEnemies()
        {
            if (_player == null || _enemies == null) return 0;

            int count = 0;
            float radiusSq = crowdRadius * crowdRadius;
            Vector3 playerPosition = _player.transform.position;
            for (int i = 0; i < _enemies.Length; i++)
            {
                EnemyStateMachine enemy = _enemies[i];
                if (enemy == null || !enemy.gameObject.activeInHierarchy || enemy.isDead) continue;

                Vector3 delta = enemy.transform.position - playerPosition;
                delta.y = 0f;
                if (delta.sqrMagnitude <= radiusSq) count++;
            }

            return count;
        }

        private static string FormatRect(Rect rect)
        {
            return $"[{rect.xMin:F2},{rect.yMin:F2}]→[{rect.xMax:F2},{rect.yMax:F2}]";
        }
    }
}
