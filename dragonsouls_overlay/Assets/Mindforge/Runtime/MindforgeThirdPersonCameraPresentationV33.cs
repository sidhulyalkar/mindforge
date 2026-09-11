using System;
using System.Collections.Generic;
using Cinemachine;
using PlayerController;
using UnityEngine;

namespace Mindforge.Chassis
{
    /// <summary>
    /// Presentation-only third-person camera correction for the V0.33 playthrough.
    /// Dragon Souls keeps all camera switching/brain authority. Mindforge only widens
    /// the inherited FreeLook/Target-state composition enough to keep the whole player
    /// readable while the BCI targets are on screen.
    ///
    /// Aim and bonfire cameras are intentionally excluded so aiming precision and
    /// authored cinematic framing remain upstream-authoritative.
    /// </summary>
    [DefaultExecutionOrder(1180)]
    [DisallowMultipleComponent]
    public sealed class MindforgeThirdPersonCameraPresentationV33 : MonoBehaviour
    {
        public const float PreferredFieldOfView = 55f;
        public const float MinimumFollowDistance = 5.6f;

        [SerializeField, Range(50f, 62f)] private float preferredFieldOfView = PreferredFieldOfView;
        [SerializeField, Range(4.5f, 7.0f)] private float minimumFollowDistance = MinimumFollowDistance;
        [SerializeField] private float retrySeconds = 0.25f;
        [SerializeField] private float retryWindowSeconds = 3f;

        private float _startedAt;
        private float _nextRetryAt;

        public bool Installed { get; private set; }
        public int CandidateCameraCount { get; private set; }
        public int AdjustedCameraCount { get; private set; }
        public string CameraSummary { get; private set; } = "unresolved";

        private void Awake()
        {
            _startedAt = Time.unscaledTime;
            ApplyComposition();
        }

        private void Start()
        {
            if (!Installed) ApplyComposition();
        }

        private void LateUpdate()
        {
            if (Installed) return;
            if (Time.unscaledTime - _startedAt > retryWindowSeconds) return;
            if (Time.unscaledTime < _nextRetryAt) return;
            _nextRetryAt = Time.unscaledTime + retrySeconds;
            ApplyComposition();
        }

        private void ApplyComposition()
        {
            PlayerStateMachine player = FindObjectOfType<PlayerStateMachine>(true);
            if (player == null)
            {
                CameraSummary = "player unresolved";
                return;
            }

            CinemachineVirtualCamera[] cameras = FindObjectsOfType<CinemachineVirtualCamera>(true);
            int candidates = 0;
            int adjusted = 0;
            List<string> names = new List<string>();

            for (int i = 0; i < cameras.Length; i++)
            {
                CinemachineVirtualCamera camera = cameras[i];
                if (camera == null || !IsGameplayCamera(camera, player.transform)) continue;

                candidates++;
                names.Add(camera.name);
                bool changed = false;

                LensSettings lens = camera.m_Lens;
                float targetFov = Mathf.Max(lens.FieldOfView, preferredFieldOfView);
                if (!Mathf.Approximately(lens.FieldOfView, targetFov))
                {
                    lens.FieldOfView = targetFov;
                    camera.m_Lens = lens;
                    changed = true;
                }

                CinemachineTransposer transposer = camera.GetCinemachineComponent<CinemachineTransposer>();
                if (transposer != null)
                {
                    Vector3 offset = transposer.m_FollowOffset;
                    float resolvedZ = Mathf.Min(offset.z, -minimumFollowDistance);
                    if (!Mathf.Approximately(offset.z, resolvedZ))
                    {
                        offset.z = resolvedZ;
                        transposer.m_FollowOffset = offset;
                        changed = true;
                    }
                }

                if (changed) adjusted++;
            }

            CandidateCameraCount = candidates;
            AdjustedCameraCount = adjusted;
            Installed = candidates > 0;
            CameraSummary = candidates == 0 ? "no player gameplay cameras resolved" : string.Join(", ", names);

            if (Installed)
            {
                Debug.Log(
                    $"[Mindforge:V33:CAMERA] Third-person framing ready. candidates={candidates}, " +
                    $"adjusted={adjusted}, minFov={preferredFieldOfView:F1}, minDistance={minimumFollowDistance:F1}m, " +
                    $"cameras=[{CameraSummary}]. Aim/bonfire framing remains inherited."
                );
            }
        }

        private static bool IsGameplayCamera(CinemachineVirtualCamera camera, Transform playerRoot)
        {
            if (camera == null || playerRoot == null) return false;

            string name = camera.name ?? string.Empty;
            if (name.IndexOf("Aim", StringComparison.OrdinalIgnoreCase) >= 0) return false;
            if (name.IndexOf("Bonfire", StringComparison.OrdinalIgnoreCase) >= 0) return false;

            bool knownGameplayRole =
                name.IndexOf("FreeLook", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("TargetState", StringComparison.OrdinalIgnoreCase) >= 0;
            if (!knownGameplayRole) return false;

            return IsPlayerTarget(camera.Follow, playerRoot) || IsPlayerTarget(camera.LookAt, playerRoot);
        }

        private static bool IsPlayerTarget(Transform target, Transform playerRoot)
        {
            return target != null && (target == playerRoot || target.IsChildOf(playerRoot));
        }
    }
}
