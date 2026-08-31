using System;
using System.Collections;
using System.Net.Sockets;
using System.Text;
using Mindforge.Combat;
using Mindforge.Journey;
using Mindforge.Presentation;
using Mindforge.SoulWisp;
using UnityEngine;

namespace Mindforge.Telemetry
{
    /// <summary>
    /// Observer-only rendering context for future SSVEP datasets.
    ///
    /// This stream records what Unity actually presented around a neural interval: coded-core
    /// screen coordinates, measured angular geometry, target context, fixed-FOV camera motion,
    /// display-timing state and lock provenance. It contains no raw EEG and no hidden decoder
    /// state. External acquisition joins it to EEG by game session + stimulus epoch + time.
    ///
    /// Failure is intentionally silent and non-authoritative. A recorder can disappear without
    /// affecting gameplay, target lock, stimulus phase, neural selection or combat timing.
    /// </summary>
    [DefaultExecutionOrder(940)]
    public sealed class SsvepDatasetTelemetryV18 : MonoBehaviour
    {
        public const string RootName = "Mindforge_SsvepDatasetTelemetry_V18";
        public const string SchemaV1 = "mindforge.ssvep_observation.v1";

        [SerializeField] private string host = "127.0.0.1";
        [SerializeField] private int observerPort = 19746;
        [SerializeField, Range(2f, 60f)] private float sampleRateHz = 20f;
        [SerializeField] private bool logSendFailures;

        [SerializeField] private SoulWispController wisp;
        [SerializeField] private WispResonanceWindow resonance;
        [SerializeField] private GuardianTargetLock targetLock;
        [SerializeField] private DisplayTimingMonitor displayTiming;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private SsvepFocusBackdropV18 focusBackdrop;

        private UdpClient _client;
        private VepAuraStimulus _sightStimulus;
        private VepAuraStimulus _guardStimulus;
        private double _nextSampleAt;
        private long _seq;
        private Vector3 _lastCameraPosition;
        private Quaternion _lastCameraRotation;
        private double _lastCameraSampleAt;
        private bool _hasCameraSample;

        [Serializable]
        private sealed class Observation
        {
            public string schema = SchemaV1;
            public long seq;
            public string session_id;
            public double unity_realtime_s;
            public float game_time_s;
            public int frame;
            public long stimulus_epoch;
            public string mode;
            public string neural_state;
            public bool coded_active;

            public string target_name;
            public string target_kind;
            public bool target_locked;
            public string target_lock_reason;
            public float target_distance_m;
            public float target_viewport_x;
            public float target_viewport_y;
            public float target_viewport_z;

            public float sight_frequency_hz;
            public float guard_frequency_hz;
            public float qualified_refresh_hz;
            public int sight_phase_start_frame;
            public int guard_phase_start_frame;

            public float sight_viewport_x;
            public float sight_viewport_y;
            public float sight_viewport_z;
            public float guard_viewport_x;
            public float guard_viewport_y;
            public float guard_viewport_z;
            public bool sight_visible;
            public bool guard_visible;

            public float actual_separation_deg;
            public float sight_actual_diameter_deg;
            public float guard_actual_diameter_deg;
            public bool focus_backdrop_active;

            public float camera_fov_deg;
            public float camera_aspect;
            public float camera_speed_m_s;
            public float camera_angular_speed_deg_s;
            public int screen_width_px;
            public int screen_height_px;

            public float display_expected_refresh_hz;
            public float display_observed_refresh_hz;
            public bool display_has_measurement;
            public bool display_timing_healthy;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (FindObjectOfType<MindforgeDemoV11Marker>(true) == null) return;
            if (FindObjectOfType<SsvepDatasetTelemetryV18>(true) != null) return;
            new GameObject(RootName).AddComponent<SsvepDatasetTelemetryV18>();
        }

        private void Awake()
        {
            _client = new UdpClient();
        }

        private IEnumerator Start()
        {
            for (int frame = 0; frame < 300; frame++)
            {
                Resolve();
                if (wisp != null && targetCamera != null && _sightStimulus != null && _guardStimulus != null)
                    yield break;
                yield return null;
            }

            Debug.LogWarning("[Mindforge:Dataset] SSVEP observation stream could not resolve the full stimulus pair; disabled.");
            enabled = false;
        }

        private void Resolve()
        {
            if (wisp == null) wisp = FindObjectOfType<SoulWispController>(true);
            if (wisp != null && resonance == null) resonance = wisp.GetComponent<WispResonanceWindow>();
            if (targetLock == null)
            {
                GuardianCombatInput input = FindObjectOfType<GuardianCombatInput>(true);
                if (input != null) targetLock = input.GetComponent<GuardianTargetLock>();
            }
            if (displayTiming == null) displayTiming = FindObjectOfType<DisplayTimingMonitor>(true);
            if (targetCamera == null) targetCamera = Camera.main;
            if (focusBackdrop == null) focusBackdrop = FindObjectOfType<SsvepFocusBackdropV18>(true);
            ResolveStimulusPair();
        }

        private void ResolveStimulusPair()
        {
            if (_sightStimulus != null && _guardStimulus != null) return;
            VepAuraStimulus[] all = FindObjectsOfType<VepAuraStimulus>(true);
            VepAuraStimulus low = null;
            VepAuraStimulus high = null;
            for (int i = 0; i < all.Length; i++)
            {
                VepAuraStimulus stimulus = all[i];
                if (stimulus == null) continue;
                string objectName = stimulus.gameObject.name ?? string.Empty;
                if (objectName.IndexOf("Sight", StringComparison.OrdinalIgnoreCase) >= 0)
                    _sightStimulus = stimulus;
                else if (objectName.IndexOf("Guard", StringComparison.OrdinalIgnoreCase) >= 0)
                    _guardStimulus = stimulus;

                if (low == null || stimulus.FrequencyHz < low.FrequencyHz) low = stimulus;
                if (high == null || stimulus.FrequencyHz > high.FrequencyHz) high = stimulus;
            }

            // Current canonical semantics are 10 Hz Sight / 12 Hz Guard. Name matching is
            // authoritative, while frequency ordering is a backwards-compatible fallback for
            // older generated scenes whose stimulus GameObjects lacked semantic names.
            if (_sightStimulus == null) _sightStimulus = low;
            if (_guardStimulus == null) _guardStimulus = high != _sightStimulus ? high : null;
        }

        private void LateUpdate()
        {
            if (wisp == null || targetCamera == null || _sightStimulus == null || _guardStimulus == null)
            {
                Resolve();
                return;
            }

            bool visualInterval = wisp.CalibrationStimuliActive || wisp.ResonanceWindowActive;
            if (!visualInterval)
            {
                _hasCameraSample = false;
                return;
            }

            double now = Time.realtimeSinceStartupAsDouble;
            double period = 1.0 / Mathf.Max(2f, sampleRateHz);
            if (now < _nextSampleAt) return;
            _nextSampleAt = now + period;

            EmitObservation(now);
        }

        private void EmitObservation(double now)
        {
            if (_client == null) _client = new UdpClient();

            Transform target = targetLock != null && targetLock.Locked ? targetLock.Target : wisp.CurrentTarget;
            Vector3 sightViewport = targetCamera.WorldToViewportPoint(_sightStimulus.transform.position);
            Vector3 guardViewport = targetCamera.WorldToViewportPoint(_guardStimulus.transform.position);
            Vector3 targetViewport = target != null
                ? targetCamera.WorldToViewportPoint(target.position + Vector3.up * 0.9f)
                : new Vector3(-1f, -1f, -1f);

            float cameraSpeed = 0f;
            float cameraAngularSpeed = 0f;
            if (_hasCameraSample)
            {
                float dt = Mathf.Max(0.0001f, (float)(now - _lastCameraSampleAt));
                cameraSpeed = Vector3.Distance(targetCamera.transform.position, _lastCameraPosition) / dt;
                cameraAngularSpeed = Quaternion.Angle(targetCamera.transform.rotation, _lastCameraRotation) / dt;
            }
            _lastCameraPosition = targetCamera.transform.position;
            _lastCameraRotation = targetCamera.transform.rotation;
            _lastCameraSampleAt = now;
            _hasCameraSample = true;

            Vector3 sightDirection = _sightStimulus.transform.position - targetCamera.transform.position;
            Vector3 guardDirection = _guardStimulus.transform.position - targetCamera.transform.position;
            float separation = sightDirection.sqrMagnitude > 0.0001f && guardDirection.sqrMagnitude > 0.0001f
                ? Vector3.Angle(sightDirection, guardDirection)
                : 0f;

            Observation observation = new Observation
            {
                seq = ++_seq,
                session_id = MindforgeSessionContext.GameSessionId,
                unity_realtime_s = now,
                game_time_s = Time.time,
                frame = Time.frameCount,
                stimulus_epoch = wisp.CalibrationStimuliActive || resonance == null ? -1 : resonance.WindowId,
                mode = wisp.CalibrationStimuliActive ? "calibration" : "gameplay",
                neural_state = resonance != null ? resonance.State.ToString().ToLowerInvariant() : "unavailable",
                coded_active = _sightStimulus.CodedActive && _guardStimulus.CodedActive,

                target_name = target != null ? target.name : string.Empty,
                target_kind = DescribeTarget(target),
                target_locked = targetLock != null && targetLock.Locked,
                target_lock_reason = targetLock != null ? targetLock.LastChangeReason : string.Empty,
                target_distance_m = target != null && wisp != null
                    ? Vector3.Distance(wisp.transform.position, target.position)
                    : -1f,
                target_viewport_x = targetViewport.x,
                target_viewport_y = targetViewport.y,
                target_viewport_z = targetViewport.z,

                sight_frequency_hz = _sightStimulus.FrequencyHz,
                guard_frequency_hz = _guardStimulus.FrequencyHz,
                qualified_refresh_hz = _sightStimulus.QualifiedRefreshHz,
                sight_phase_start_frame = _sightStimulus.SessionStartFrame,
                guard_phase_start_frame = _guardStimulus.SessionStartFrame,

                sight_viewport_x = sightViewport.x,
                sight_viewport_y = sightViewport.y,
                sight_viewport_z = sightViewport.z,
                guard_viewport_x = guardViewport.x,
                guard_viewport_y = guardViewport.y,
                guard_viewport_z = guardViewport.z,
                sight_visible = IsPresented(_sightStimulus, sightViewport),
                guard_visible = IsPresented(_guardStimulus, guardViewport),

                actual_separation_deg = separation,
                sight_actual_diameter_deg = AngularDiameterDeg(_sightStimulus, targetCamera),
                guard_actual_diameter_deg = AngularDiameterDeg(_guardStimulus, targetCamera),
                focus_backdrop_active = focusBackdrop != null && focusBackdrop.Active,

                camera_fov_deg = targetCamera.fieldOfView,
                camera_aspect = targetCamera.aspect,
                camera_speed_m_s = cameraSpeed,
                camera_angular_speed_deg_s = cameraAngularSpeed,
                screen_width_px = Screen.width,
                screen_height_px = Screen.height,

                display_expected_refresh_hz = displayTiming != null ? displayTiming.ExpectedRefreshHz : 0f,
                display_observed_refresh_hz = displayTiming != null ? displayTiming.ObservedRefreshHz : 0f,
                display_has_measurement = displayTiming != null && displayTiming.HasMeasurement,
                display_timing_healthy = displayTiming != null && displayTiming.TimingHealthy,
            };

            byte[] bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(observation));
            try
            {
                _client.Send(bytes, bytes.Length, host, observerPort);
            }
            catch (SocketException ex)
            {
                if (logSendFailures)
                    Debug.LogWarning($"[Mindforge:Dataset] SSVEP observer send failed: {ex.SocketErrorCode}");
            }
        }

        private static bool IsPresented(VepAuraStimulus stimulus, Vector3 viewport)
        {
            if (stimulus == null || !stimulus.gameObject.activeInHierarchy || viewport.z <= 0f) return false;
            if (viewport.x < 0f || viewport.x > 1f || viewport.y < 0f || viewport.y > 1f) return false;
            Renderer renderer = stimulus.GetComponent<Renderer>();
            if (renderer == null) renderer = stimulus.GetComponentInChildren<Renderer>(true);
            return renderer == null || renderer.enabled;
        }

        private static float AngularDiameterDeg(VepAuraStimulus stimulus, Camera camera)
        {
            if (stimulus == null || camera == null) return 0f;
            Renderer renderer = stimulus.GetComponent<Renderer>();
            if (renderer == null) renderer = stimulus.GetComponentInChildren<Renderer>(true);
            float worldDiameter;
            if (renderer != null)
            {
                Vector3 size = renderer.bounds.size;
                worldDiameter = Mathf.Max(size.x, size.y, size.z);
            }
            else
            {
                Vector3 scale = stimulus.transform.lossyScale;
                worldDiameter = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
            }

            float distance = Vector3.Distance(camera.transform.position, stimulus.transform.position);
            if (distance <= 0.001f || worldDiameter <= 0f) return 0f;
            return 2f * Mathf.Atan2(worldDiameter * 0.5f, distance) * Mathf.Rad2Deg;
        }

        private static string DescribeTarget(Transform target)
        {
            if (target == null) return "none";
            FracturedSignalDirector boss = target.GetComponentInParent<FracturedSignalDirector>();
            if (boss != null) return "boss:fractured_signal";
            JourneyEnemyController journey = target.GetComponentInParent<JourneyEnemyController>();
            if (journey != null) return "journey:" + journey.Archetype.ToString().ToLowerInvariant();
            CombatantVitals vitals = target.GetComponentInParent<CombatantVitals>();
            return vitals != null && vitals.Team == CombatTeam.Enemy ? "enemy" : "other";
        }

        private void OnDestroy()
        {
            _client?.Close();
            _client = null;
        }
    }
}
