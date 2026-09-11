using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Mindforge.Chassis
{
    /// <summary>
    /// Passive evidence correlator between derived gaze and the production BCI ceremony.
    /// It never gates the decoder or publishes gameplay intent. Instead it summarizes
    /// where gaze fell during calibration and causal neural windows so later analysis can
    /// distinguish neural failure from obvious visual-attention failure.
    ///
    /// Only aggregate metrics are persisted. Individual gaze coordinates are not written.
    /// </summary>
    [DefaultExecutionOrder(1250)]
    [DisallowMultipleComponent]
    public sealed class MindforgeGazeBciEvidenceV34 : MonoBehaviour
    {
        [Serializable]
        private sealed class EvidenceRecord
        {
            public string schema = "mindforge.gaze_bci_evidence.v1";
            public string session_id;
            public string calibration_id;
            public string layout_id;
            public string phase;
            public string expected_target;
            public string selected_target;
            public string neural_source_mode;
            public string gaze_source_mode;
            public long stimulus_epoch = -1;
            public int usable_samples;
            public float sight_occupancy;
            public float guard_occupancy;
            public float off_target_fraction;
            public float selected_target_occupancy;
            public float selected_target_distance_p50;
            public float selected_target_distance_p90;
            public float duration_ms;
            public string outcome;
            public string generated_utc;
        }

        private sealed class ActiveEvidence
        {
            public string phase;
            public MindforgeIntentV29 expected = MindforgeIntentV29.None;
            public MindforgeIntentV29 selected = MindforgeIntentV29.None;
            public string neuralSource = "unobserved";
            public long epoch = -1;
            public double startedAt;
            public int usable;
            public int sightInside;
            public int guardInside;
            public readonly List<float> sightDistance = new List<float>(256);
            public readonly List<float> guardDistance = new List<float>(256);
        }

        [SerializeField] private float fallbackAoiRadius = 0.06f;

        private MindforgeUdpGazeReceiverV34 _gaze;
        private MindforgeAdaptiveStimulusLayoutV34 _layout;
        private MindforgeBciCalibrationDirectorV33 _calibration;
        private MindforgeNeuralWindowControllerV33 _windows;
        private MindforgeNeuralIntentBridgeV33 _bridge;
        private MindforgeBciMarkerSenderV33 _markers;
        private MindforgeBciStimulusV33 _stimulus;
        private Camera _camera;
        private Transform _sightTarget;
        private Transform _guardTarget;
        private ActiveEvidence _active;
        private string _latestGazeSource = "unobserved";
        private string _logPath;

        public string LogPath => _logPath;
        public int RecordCount { get; private set; }
        public bool TargetGeometryReady => ResolveTargetGeometry();

        private void Start()
        {
            ResolveDependencies();
            ResolveTargetGeometry();
            Bind();
            PrepareLog();
        }

        private void OnDestroy()
        {
            Unbind();
            if (_active != null) FinalizeActive("component_destroyed");
        }

        private void Bind()
        {
            if (_gaze != null) _gaze.SampleReceived += HandleGaze;
            if (_calibration != null) _calibration.StateChanged += HandleCalibrationState;
            if (_windows != null)
            {
                _windows.WindowOpened += HandleWindowOpened;
                _windows.WindowResolved += HandleWindowResolved;
                _windows.WindowEnded += HandleWindowEnded;
            }
            if (_bridge != null) _bridge.SelectionAccepted += HandleSelectionAccepted;
        }

        private void Unbind()
        {
            if (_gaze != null) _gaze.SampleReceived -= HandleGaze;
            if (_calibration != null) _calibration.StateChanged -= HandleCalibrationState;
            if (_windows != null)
            {
                _windows.WindowOpened -= HandleWindowOpened;
                _windows.WindowResolved -= HandleWindowResolved;
                _windows.WindowEnded -= HandleWindowEnded;
            }
            if (_bridge != null) _bridge.SelectionAccepted -= HandleSelectionAccepted;
        }

        private void HandleCalibrationState(MindforgeBciCalibrationDirectorV33.CalibrationState state)
        {
            if (_active != null && _active.phase.StartsWith("calibration_", StringComparison.Ordinal))
                FinalizeActive("stage_transition");

            if (state == MindforgeBciCalibrationDirectorV33.CalibrationState.Sight)
                Begin("calibration_sight", MindforgeIntentV29.Sight, -1);
            else if (state == MindforgeBciCalibrationDirectorV33.CalibrationState.Guard)
                Begin("calibration_guard", MindforgeIntentV29.Guard, -1);
        }

        private void HandleWindowOpened(long epoch, string reason)
        {
            if (_active != null) FinalizeActive("superseded");
            Begin("neural_window", MindforgeIntentV29.None, epoch);
        }

        private void HandleWindowResolved(long epoch, MindforgeIntentV29 intent)
        {
            if (_active == null || _active.epoch != epoch) return;
            _active.selected = intent;
        }

        private void HandleSelectionAccepted(MindforgeIntentEventV29 evt, long epoch)
        {
            if (_active == null || _active.epoch != epoch) return;
            _active.selected = evt.Intent;
            _active.neuralSource = evt.Source;
            FinalizeActive("accepted");
        }

        private void HandleWindowEnded(long epoch, string reason)
        {
            if (_active == null || _active.epoch != epoch) return;
            FinalizeActive(string.IsNullOrEmpty(reason) ? "ended" : reason);
        }

        private void Begin(string phase, MindforgeIntentV29 expected, long epoch)
        {
            _active = new ActiveEvidence
            {
                phase = phase,
                expected = expected,
                epoch = epoch,
                startedAt = Time.realtimeSinceStartupAsDouble,
            };
        }

        private void HandleGaze(MindforgeGazeEventV34 sample)
        {
            if (_active == null || sample == null || !sample.IsUsable(0.55f)) return;
            if (!ResolveTargetGeometry()) return;

            _latestGazeSource = string.IsNullOrEmpty(sample.source_mode) ? "unknown" : sample.source_mode;
            Vector2 gazePoint = sample.UnityViewportPoint;
            Vector2 sight = ViewportPoint(_sightTarget);
            Vector2 guard = ViewportPoint(_guardTarget);
            float sightDistance = Vector2.Distance(gazePoint, sight);
            float guardDistance = Vector2.Distance(gazePoint, guard);
            float radius = _layout != null && _layout.Frozen
                ? _layout.RecommendedAoiRadius
                : fallbackAoiRadius;

            _active.usable++;
            _active.sightDistance.Add(sightDistance);
            _active.guardDistance.Add(guardDistance);
            if (sightDistance <= radius) _active.sightInside++;
            if (guardDistance <= radius) _active.guardInside++;
        }

        private Vector2 ViewportPoint(Transform target)
        {
            Vector3 projected = _camera.WorldToViewportPoint(target.position);
            return new Vector2(projected.x, projected.y);
        }

        private bool ResolveTargetGeometry()
        {
            if (_camera == null) _camera = Camera.main;
            if (_camera == null) return false;
            if (_sightTarget != null && _guardTarget != null) return true;

            Transform root = _camera.transform.Find("Mindforge_BCI_Targets_V33");
            if (root == null) return false;
            _sightTarget = root.Find("SightTarget");
            _guardTarget = root.Find("GuardTarget");
            return _sightTarget != null && _guardTarget != null;
        }

        private void FinalizeActive(string outcome)
        {
            ActiveEvidence active = _active;
            _active = null;
            if (active == null) return;

            int n = active.usable;
            float sightOccupancy = n > 0 ? active.sightInside / (float)n : 0f;
            float guardOccupancy = n > 0 ? active.guardInside / (float)n : 0f;
            int anyInside = Math.Min(n, active.sightInside + active.guardInside);
            float offTarget = n > 0 ? 1f - anyInside / (float)n : 1f;

            MindforgeIntentV29 comparison = active.selected != MindforgeIntentV29.None
                ? active.selected
                : active.expected;
            List<float> selectedDistances = comparison == MindforgeIntentV29.Sight
                ? active.sightDistance
                : comparison == MindforgeIntentV29.Guard
                    ? active.guardDistance
                    : null;
            float selectedOccupancy = comparison == MindforgeIntentV29.Sight
                ? sightOccupancy
                : comparison == MindforgeIntentV29.Guard
                    ? guardOccupancy
                    : 0f;

            EvidenceRecord record = new EvidenceRecord
            {
                session_id = _markers != null ? _markers.SessionId : null,
                calibration_id = _calibration != null ? _calibration.CalibrationId : null,
                layout_id = _stimulus != null ? _stimulus.LayoutId : null,
                phase = active.phase,
                expected_target = IntentLabel(active.expected),
                selected_target = IntentLabel(active.selected),
                neural_source_mode = active.neuralSource,
                gaze_source_mode = _latestGazeSource,
                stimulus_epoch = active.epoch,
                usable_samples = n,
                sight_occupancy = sightOccupancy,
                guard_occupancy = guardOccupancy,
                off_target_fraction = Mathf.Clamp01(offTarget),
                selected_target_occupancy = selectedOccupancy,
                selected_target_distance_p50 = selectedDistances != null ? Percentile(selectedDistances, 0.50f) : 0f,
                selected_target_distance_p90 = selectedDistances != null ? Percentile(selectedDistances, 0.90f) : 0f,
                duration_ms = (float)Math.Max(0.0, (Time.realtimeSinceStartupAsDouble - active.startedAt) * 1000.0),
                outcome = string.IsNullOrEmpty(outcome) ? "unknown" : outcome,
                generated_utc = DateTime.UtcNow.ToString("o"),
            };
            Append(record);
        }

        private void PrepareLog()
        {
            try
            {
                string directory = Path.Combine(Application.persistentDataPath, "mindforge-bci");
                Directory.CreateDirectory(directory);
                string session = _markers != null && !string.IsNullOrEmpty(_markers.SessionId)
                    ? _markers.SessionId
                    : Guid.NewGuid().ToString("N");
                _logPath = Path.Combine(directory, "v34-gaze-bci-" + session + ".jsonl");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Mindforge:V34:Gaze] evidence path failed: {ex.Message}");
            }
        }

        private void Append(EvidenceRecord record)
        {
            if (string.IsNullOrEmpty(_logPath)) return;
            try
            {
                File.AppendAllText(_logPath, JsonUtility.ToJson(record) + Environment.NewLine);
                RecordCount++;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Mindforge:V34:Gaze] evidence write failed: {ex.Message}");
            }
        }

        private static string IntentLabel(MindforgeIntentV29 intent)
        {
            return intent == MindforgeIntentV29.None ? null : intent.ToString().ToLowerInvariant();
        }

        private static float Percentile(List<float> values, float percentile)
        {
            if (values == null || values.Count == 0) return 0f;
            List<float> sorted = new List<float>(values);
            sorted.Sort();
            float position = Mathf.Clamp01(percentile) * (sorted.Count - 1);
            int lower = Mathf.FloorToInt(position);
            int upper = Mathf.CeilToInt(position);
            if (lower == upper) return sorted[lower];
            return Mathf.Lerp(sorted[lower], sorted[upper], position - lower);
        }

        private void ResolveDependencies()
        {
            if (_gaze == null) _gaze = GetComponent<MindforgeUdpGazeReceiverV34>();
            if (_layout == null) _layout = GetComponent<MindforgeAdaptiveStimulusLayoutV34>();
            if (_calibration == null) _calibration = GetComponent<MindforgeBciCalibrationDirectorV33>();
            if (_windows == null) _windows = GetComponent<MindforgeNeuralWindowControllerV33>();
            if (_bridge == null) _bridge = GetComponent<MindforgeNeuralIntentBridgeV33>();
            if (_markers == null) _markers = GetComponent<MindforgeBciMarkerSenderV33>();
            if (_stimulus == null) _stimulus = GetComponent<MindforgeBciStimulusV33>();
        }
    }
}
