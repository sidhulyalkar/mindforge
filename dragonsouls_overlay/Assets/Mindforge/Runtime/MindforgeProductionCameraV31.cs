using Cinemachine;
using States;
using UnityEngine;

namespace Mindforge.Chassis
{
    /// <summary>
    /// Retunes Dragon Souls' existing Cinemachine cameras for a closer, lower,
    /// more legible third-person action composition. It never creates a competing
    /// gameplay camera and never owns player/target movement.
    /// </summary>
    [DefaultExecutionOrder(650)]
    [DisallowMultipleComponent]
    public sealed class MindforgeProductionCameraV31 : MonoBehaviour
    {
        public const string ProductVersion = "V0.31 Production Vertical Slice";

        [Header("Free look")]
        [SerializeField] private float normalFov = 49f;
        [SerializeField] private float topHeight = 2.45f;
        [SerializeField] private float topRadius = 3.35f;
        [SerializeField] private float middleHeight = 1.25f;
        [SerializeField] private float middleRadius = 3.75f;
        [SerializeField] private float bottomHeight = 0.30f;
        [SerializeField] private float bottomRadius = 3.15f;

        [Header("Combat framing")]
        [SerializeField] private float targetFov = 50f;
        [SerializeField] private float crowdedTargetFov = 55f;
        [SerializeField] private float bossTargetFov = 57f;
        [SerializeField] private float crowdRadius = 8.5f;
        [SerializeField] private int crowdedEnemyThreshold = 3;
        [SerializeField] private float bossPullbackDistance = 20f;
        [SerializeField] private float fovResponse = 10f;

        private PlayerStateMachine _player;
        private CameraController _controller;
        private CinemachineFreeLook _freeLook;
        private CinemachineVirtualCamera _targetCam;
        private CinemachineVirtualCamera _aimCam;
        private EnemyStateMachine[] _enemies;
        private EnemyNightmareDragonController _dragon;
        private float _nextEnemyRefresh;

        public bool Installed { get; private set; }
        public float CurrentTargetFov => _targetCam == null ? 0f : _targetCam.m_Lens.FieldOfView;

        private void Start()
        {
            _player = FindObjectOfType<PlayerStateMachine>();
            if (_player == null || _player.cameraController == null)
            {
                Debug.LogWarning("[Mindforge:V31] Production camera could not resolve PlayerStateMachine/CameraController.");
                return;
            }

            _controller = _player.cameraController;
            _freeLook = _controller._cinemachineFreeLookCam;
            _targetCam = _controller._cinemachineTargetCam;
            _aimCam = _controller._cinemachineAimCam;
            _dragon = FindObjectOfType<EnemyNightmareDragonController>();

            ConfigureBrain();
            ConfigureFreeLook();
            ConfigureVirtualCamera(_targetCam, targetFov, 0.46f, 0.57f);
            ConfigureVirtualCamera(_aimCam, 51f, 0.43f, 0.56f);
            RefreshEnemyCache();
            Installed = _freeLook != null && _targetCam != null;
        }

        private void LateUpdate()
        {
            if (!Installed || _player == null || _targetCam == null) return;
            if (Time.unscaledTime >= _nextEnemyRefresh) RefreshEnemyCache();

            float desired = targetFov;
            int nearby = CountNearbyEnemies();
            if (nearby >= crowdedEnemyThreshold) desired = crowdedTargetFov;

            if (_dragon != null)
            {
                Vector3 delta = _dragon.transform.position - _player.transform.position;
                delta.y = 0f;
                if (delta.sqrMagnitude <= bossPullbackDistance * bossPullbackDistance)
                    desired = Mathf.Max(desired, bossTargetFov);
            }

            LensSettings lens = _targetCam.m_Lens;
            lens.FieldOfView = Mathf.MoveTowards(lens.FieldOfView, desired, fovResponse * Time.unscaledDeltaTime);
            _targetCam.m_Lens = lens;
        }

        private void ConfigureBrain()
        {
            if (_controller._cinemachineBrain == null) return;
            _controller._cinemachineBrain.m_DefaultBlend =
                new CinemachineBlendDefinition(CinemachineBlendDefinition.Style.EaseInOut, 0.18f);
        }

        private void ConfigureFreeLook()
        {
            if (_freeLook == null) return;

            LensSettings lens = _freeLook.m_Lens;
            lens.FieldOfView = normalFov;
            lens.NearClipPlane = 0.08f;
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

            _freeLook.m_YAxis.Value = 0.52f;
            _freeLook.m_RecenterToTargetHeading.m_WaitTime = 0.65f;
            _freeLook.m_RecenterToTargetHeading.m_RecenteringTime = 0.75f;

            for (int i = 0; i < 3; i++)
            {
                CinemachineVirtualCamera rig = _freeLook.GetRig(i);
                if (rig == null) continue;
                CinemachineComposer composer = rig.GetCinemachineComponent<CinemachineComposer>();
                if (composer != null)
                {
                    composer.m_TrackedObjectOffset = new Vector3(0f, 1.45f, 0f);
                    composer.m_ScreenX = 0.44f;
                    composer.m_ScreenY = 0.57f;
                    composer.m_DeadZoneWidth = 0.035f;
                    composer.m_DeadZoneHeight = 0.035f;
                    composer.m_SoftZoneWidth = 0.78f;
                    composer.m_SoftZoneHeight = 0.72f;
                    composer.m_HorizontalDamping = 0.42f;
                    composer.m_VerticalDamping = 0.42f;
                }
                ConfigureCollider(rig);
            }
        }

        private static void ConfigureVirtualCamera(CinemachineVirtualCamera camera, float fov, float screenX, float screenY)
        {
            if (camera == null) return;
            LensSettings lens = camera.m_Lens;
            lens.FieldOfView = fov;
            lens.NearClipPlane = 0.08f;
            camera.m_Lens = lens;

            CinemachineComposer composer = camera.GetCinemachineComponent<CinemachineComposer>();
            if (composer != null)
            {
                composer.m_ScreenX = screenX;
                composer.m_ScreenY = screenY;
                composer.m_DeadZoneWidth = 0.025f;
                composer.m_DeadZoneHeight = 0.025f;
                composer.m_SoftZoneWidth = 0.80f;
                composer.m_SoftZoneHeight = 0.74f;
                composer.m_HorizontalDamping = 0.40f;
                composer.m_VerticalDamping = 0.40f;
            }
            ConfigureCollider(camera);
        }

        private static void ConfigureCollider(CinemachineVirtualCameraBase camera)
        {
            if (camera == null) return;
            CinemachineCollider collider = camera.GetComponent<CinemachineCollider>();
            if (collider == null) return;
            collider.m_CameraRadius = Mathf.Max(collider.m_CameraRadius, 0.28f);
            collider.m_MinimumDistanceFromTarget = Mathf.Max(collider.m_MinimumDistanceFromTarget, 0.45f);
            collider.m_SmoothingTime = 0.08f;
            collider.m_Damping = 0.22f;
            collider.m_DampingWhenOccluded = 0.10f;
            collider.m_MaximumEffort = Mathf.Max(collider.m_MaximumEffort, 6);
        }

        private void RefreshEnemyCache()
        {
            _enemies = FindObjectsOfType<EnemyStateMachine>();
            _nextEnemyRefresh = Time.unscaledTime + 1.0f;
        }

        private int CountNearbyEnemies()
        {
            if (_enemies == null || _player == null) return 0;
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
    }
}
