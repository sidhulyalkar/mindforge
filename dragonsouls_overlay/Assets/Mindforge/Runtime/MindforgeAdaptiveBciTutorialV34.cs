using System;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Mindforge.Chassis
{
    /// <summary>
    /// First-run tutorial and participant-profiling state machine. It treats gaze as an
    /// independent measurement channel, freezes presentation geometry before neural
    /// calibration, and then trains Sight/Guard through the production semantic seam.
    /// The tutorial never feeds gaze into the EEG classifier or grants gaze gameplay
    /// authority. Failure is non-destructive: ordinary game controls remain available.
    /// </summary>
    [DefaultExecutionOrder(1240)]
    [DisallowMultipleComponent]
    public sealed class MindforgeAdaptiveBciTutorialV34 : MonoBehaviour
    {
        public enum TutorialStage
        {
            Idle = 0,
            Controls = 1,
            GazeHealth = 2,
            GazeGrid = 3,
            FreeExplore = 4,
            GazeSwitch = 5,
            LayoutFreeze = 6,
            WaitingForNeuralService = 7,
            Calibration = 8,
            SightPractice = 9,
            GuardPractice = 10,
            MovementStress = 11,
            Complete = 12,
            Partial = 13,
        }

        [Serializable]
        private sealed class TutorialReceipt
        {
            public string schema = "mindforge.tutorial_receipt.v1";
            public string source_commit;
            public bool clean_source;
            public string unity_version;
            public string session_id;
            public string tutorial_status;
            public string gaze_source_mode;
            public MindforgeGazeProfilerV34.ProfileSnapshot gaze_profile;
            public string stimulus_layout_id;
            public bool stimulus_layout_frozen;
            public bool default_layout_used;
            public float recommended_aoi_radius;
            public int recommended_acquisition_lead_ms;
            public string calibration_id;
            public string calibration_state;
            public bool sight_practice_pass;
            public bool guard_practice_pass;
            public int sight_attempts;
            public int guard_attempts;
            public int practice_abstentions;
            public int practice_mismatches;
            public bool movement_stress_observed;
            public string movement_stress_outcome;
            public string neural_source_mode;
            public bool raw_eeg_in_unity = false;
            public bool physical_display_timing_observed = false;
            public string bci_session_log;
            public string generated_utc;
        }

        [Header("Tutorial pacing")]
        [SerializeField] private bool autoStart;
        [SerializeField] private float controlsMinimumSeconds = 6f;
        [SerializeField] private float gazeHealthTimeoutSeconds = 4.5f;
        [SerializeField] private float gridPromptTimeoutSeconds = 1.6f;
        [SerializeField] private float freeExploreSeconds = 8f;
        [SerializeField] private float switchPromptTimeoutSeconds = 1.25f;
        [SerializeField] private float movementStressTimeoutSeconds = 10f;
        [SerializeField] private int maximumPracticeAttempts = 4;

        private static readonly Vector2[] GridTargets =
        {
            new Vector2(0.15f, 0.82f), new Vector2(0.50f, 0.82f), new Vector2(0.85f, 0.82f),
            new Vector2(0.15f, 0.50f), new Vector2(0.50f, 0.50f), new Vector2(0.85f, 0.50f),
            new Vector2(0.15f, 0.18f), new Vector2(0.50f, 0.18f), new Vector2(0.85f, 0.18f),
        };

        private static readonly Vector2[] SwitchTargets =
        {
            new Vector2(0.24f, 0.58f), new Vector2(0.76f, 0.58f),
            new Vector2(0.24f, 0.42f), new Vector2(0.76f, 0.42f),
            new Vector2(0.50f, 0.72f), new Vector2(0.50f, 0.28f),
        };

        private MindforgeUdpGazeReceiverV34 _gazeReceiver;
        private MindforgeGazeProfilerV34 _gazeProfiler;
        private MindforgeAdaptiveStimulusLayoutV34 _layout;
        private MindforgeBciStimulusV33 _stimulus;
        private MindforgeBciCalibrationDirectorV33 _calibration;
        private MindforgeNeuralWindowControllerV33 _windows;
        private MindforgeNeuralIntentBridgeV33 _bridge;
        private MindforgeBciMarkerSenderV33 _markers;
        private MindforgeNativeProvenanceV33 _provenance;
        private MindforgeBciSessionLoggerV33 _sessionLogger;
        private MindforgeDisplayTimingMonitorV33 _timing;

        private float _stageEnteredAt;
        private int _promptIndex;
        private bool _gazeObserved;
        private bool _stagePromptStarted;
        private bool _sightPass;
        private bool _guardPass;
        private int _sightAttempts;
        private int _guardAttempts;
        private int _abstentions;
        private int _mismatches;
        private bool _movementWindowOpened;
        private bool _movementStressObserved;
        private string _movementStressOutcome = "unobserved";
        private string _lastNeuralSource = "unobserved";
        private string _receiptPath;
        private bool _receiptWritten;
        private bool _started;

        public event Action<TutorialStage> StageChanged;

        public TutorialStage Stage { get; private set; } = TutorialStage.Idle;
        public bool Started => _started;
        public bool Finished => Stage == TutorialStage.Complete || Stage == TutorialStage.Partial;
        public string ReceiptPath => _receiptPath;
        public string StatusLabel => Stage.ToString().ToUpperInvariant();

        private void Start()
        {
            ResolveDependencies();
            Bind();
            if (_stimulus != null) _stimulus.SetVisible(false);
            if (autoStart) BeginTutorial();
        }

        private void OnDestroy()
        {
            Unbind();
        }

        private void Update()
        {
            ResolveDependencies();
            Keyboard keyboard = Keyboard.current;

            if (!_started)
            {
                if (keyboard != null && keyboard.f9Key.wasPressedThisFrame) BeginTutorial();
                return;
            }

            switch (Stage)
            {
                case TutorialStage.Controls:
                    if (TimeInStage >= controlsMinimumSeconds && keyboard != null && keyboard.enterKey.wasPressedThisFrame)
                        EnterStage(TutorialStage.GazeHealth, "controls_acknowledged");
                    break;
                case TutorialStage.GazeHealth:
                    UpdateGazeHealth();
                    break;
                case TutorialStage.GazeGrid:
                    UpdatePromptSequence(GridTargets, TutorialStage.FreeExplore, gridPromptTimeoutSeconds, "grid");
                    break;
                case TutorialStage.FreeExplore:
                    if (TimeInStage >= freeExploreSeconds)
                        EnterStage(TutorialStage.GazeSwitch, "free_explore_complete");
                    break;
                case TutorialStage.GazeSwitch:
                    UpdatePromptSequence(SwitchTargets, TutorialStage.LayoutFreeze, switchPromptTimeoutSeconds, "switch");
                    break;
                case TutorialStage.LayoutFreeze:
                    FreezeLayoutAndContinue();
                    break;
                case TutorialStage.WaitingForNeuralService:
                    TryBeginNeuralCalibration();
                    break;
                case TutorialStage.Calibration:
                    UpdateCalibration();
                    break;
                case TutorialStage.SightPractice:
                    UpdatePractice(MindforgeIntentV29.Sight);
                    break;
                case TutorialStage.GuardPractice:
                    UpdatePractice(MindforgeIntentV29.Guard);
                    break;
                case TutorialStage.MovementStress:
                    UpdateMovementStress(keyboard);
                    break;
            }
        }

        public void BeginTutorial()
        {
            if (_started && !Finished) return;
            ResolveDependencies();
            _started = true;
            _receiptWritten = false;
            _sightPass = false;
            _guardPass = false;
            _sightAttempts = 0;
            _guardAttempts = 0;
            _abstentions = 0;
            _mismatches = 0;
            _movementWindowOpened = false;
            _movementStressObserved = false;
            _movementStressOutcome = "unobserved";
            _lastNeuralSource = "unobserved";
            _stimulus?.SetVisible(false);
            EnterStage(TutorialStage.Controls, "tutorial_started");
        }

        private float TimeInStage => Time.unscaledTime - _stageEnteredAt;

        private void EnterStage(TutorialStage next, string reason)
        {
            if (Stage == next) return;
            if (_gazeProfiler != null && _gazeProfiler.PromptActive)
                _gazeProfiler.CompletePrompt(Stage.ToString().ToLowerInvariant() + "_ended");

            _markers?.SendTutorialStage(Stage.ToString().ToLowerInvariant(), "end", reason);
            Stage = next;
            _stageEnteredAt = Time.unscaledTime;
            _promptIndex = 0;
            _stagePromptStarted = false;
            StageChanged?.Invoke(Stage);
            _markers?.SendTutorialStage(Stage.ToString().ToLowerInvariant(), "begin", reason);
            Debug.Log($"[Mindforge:V34:TUTORIAL] {Stage} ({reason}).");

            if (Stage == TutorialStage.Complete || Stage == TutorialStage.Partial)
                WriteReceipt();
        }

        private void UpdateGazeHealth()
        {
            if (!_stagePromptStarted)
            {
                _gazeProfiler?.BeginPrompt(new Vector2(0.50f, 0.50f), 0.07f);
                _stagePromptStarted = true;
            }

            if (_gazeProfiler != null && _gazeProfiler.CurrentPromptStable)
            {
                _gazeProfiler.CompletePrompt("gaze_health_center");
                _gazeObserved = true;
                EnterStage(TutorialStage.GazeGrid, "gaze_center_stable");
                return;
            }

            if (TimeInStage < gazeHealthTimeoutSeconds) return;
            if (_gazeProfiler != null && _gazeProfiler.PromptActive)
                _gazeProfiler.CompletePrompt("gaze_health_timeout");
            _gazeObserved = _gazeReceiver != null && _gazeReceiver.IsConnected && _gazeProfiler != null && _gazeProfiler.UsableSamples > 0;
            EnterStage(_gazeObserved ? TutorialStage.GazeGrid : TutorialStage.LayoutFreeze,
                _gazeObserved ? "gaze_present_but_unstable" : "gaze_unobserved_use_default");
        }

        private void UpdatePromptSequence(Vector2[] targets, TutorialStage next, float timeout, string labelPrefix)
        {
            if (!_gazeObserved || _gazeProfiler == null)
            {
                EnterStage(next, "gaze_unavailable_skip_sequence");
                return;
            }
            if (_promptIndex >= targets.Length)
            {
                EnterStage(next, labelPrefix + "_complete");
                return;
            }

            if (!_stagePromptStarted)
            {
                float radius = _layout != null ? _layout.RecommendedAoiRadius : 0.06f;
                _gazeProfiler.BeginPrompt(targets[_promptIndex], radius);
                _stagePromptStarted = true;
                _stageEnteredAt = Time.unscaledTime;
            }

            bool done = _gazeProfiler.CurrentPromptStable || TimeInStage >= timeout;
            if (!done) return;

            _gazeProfiler.CompletePrompt(labelPrefix + "_" + _promptIndex);
            _promptIndex++;
            _stagePromptStarted = false;
            _stageEnteredAt = Time.unscaledTime;
        }

        private void FreezeLayoutAndContinue()
        {
            if (_layout == null || _stimulus == null)
            {
                EnterStage(TutorialStage.Partial, "layout_dependencies_missing");
                return;
            }

            bool frozen = _gazeObserved ? _layout.FreezeFromProfile() : _layout.FreezeDefault("gaze_unobserved");
            if (!frozen && !_layout.Frozen)
            {
                EnterStage(TutorialStage.Partial, "layout_freeze_failed");
                return;
            }

            _stimulus.SetVisible(true);
            EnterStage(TutorialStage.WaitingForNeuralService, "layout_frozen:" + _layout.LayoutId);
        }

        private void TryBeginNeuralCalibration()
        {
            if (_calibration == null) return;
            if (!_calibration.ServiceReady) return;
            if (_timing != null && !_timing.TimingHealthy) return;

            if (_calibration.BeginCalibration())
                EnterStage(TutorialStage.Calibration, "neural_calibration_started");
        }

        private void UpdateCalibration()
        {
            if (_calibration == null)
            {
                EnterStage(TutorialStage.Partial, "calibration_director_missing");
                return;
            }
            if (_calibration.IsCalibrated)
            {
                EnterStage(TutorialStage.SightPractice, "calibration_accepted");
                return;
            }
            if (_calibration.State == MindforgeBciCalibrationDirectorV33.CalibrationState.Failed)
                EnterStage(TutorialStage.Partial, "calibration_failed");
        }

        private void UpdatePractice(MindforgeIntentV29 expected)
        {
            if (_windows == null || _calibration == null || !_calibration.IsCalibrated) return;
            int attempts = expected == MindforgeIntentV29.Sight ? _sightAttempts : _guardAttempts;
            if (attempts >= maximumPracticeAttempts)
            {
                if (expected == MindforgeIntentV29.Sight)
                    EnterStage(TutorialStage.GuardPractice, "sight_practice_exhausted");
                else
                    EnterStage(TutorialStage.MovementStress, "guard_practice_exhausted");
                return;
            }

            if (!_windows.IsListening && TimeInStage >= 0.55f)
            {
                if (_windows.OpenWindow("tutorial_" + expected.ToString().ToLowerInvariant(), requireCalibration: true))
                {
                    if (expected == MindforgeIntentV29.Sight) _sightAttempts++;
                    else _guardAttempts++;
                    _stageEnteredAt = Time.unscaledTime;
                }
            }
        }

        private void UpdateMovementStress(Keyboard keyboard)
        {
            if (_windows == null || _calibration == null || !_calibration.IsCalibrated)
            {
                EnterStage(TutorialStage.Partial, "movement_stress_without_calibration");
                return;
            }

            if (!_movementWindowOpened && keyboard != null)
            {
                bool movementHeld = keyboard.wKey.isPressed || keyboard.aKey.isPressed || keyboard.sKey.isPressed || keyboard.dKey.isPressed;
                bool cameraHeld = keyboard.leftArrowKey.isPressed || keyboard.rightArrowKey.isPressed ||
                                  keyboard.upArrowKey.isPressed || keyboard.downArrowKey.isPressed;
                if ((movementHeld || cameraHeld) && !_windows.IsListening)
                {
                    _movementWindowOpened = _windows.OpenWindow("tutorial_movement_stress", requireCalibration: true);
                    if (_movementWindowOpened) _stageEnteredAt = Time.unscaledTime;
                }
            }

            if (_movementStressObserved)
            {
                FinishTutorial();
                return;
            }
            if (TimeInStage >= movementStressTimeoutSeconds)
            {
                _movementStressOutcome = _movementWindowOpened ? "window_no_outcome" : "input_not_observed";
                FinishTutorial();
            }
        }

        private void FinishTutorial()
        {
            bool complete = _calibration != null && _calibration.IsCalibrated && _sightPass && _guardPass;
            EnterStage(complete ? TutorialStage.Complete : TutorialStage.Partial,
                complete ? "tutorial_complete" : "tutorial_partial");
        }

        private void HandleSelectionAccepted(MindforgeIntentEventV29 evt, long epoch)
        {
            _lastNeuralSource = evt.Source;

            if (Stage == TutorialStage.SightPractice)
            {
                if (evt.Intent == MindforgeIntentV29.Sight)
                {
                    _sightPass = true;
                    EnterStage(TutorialStage.GuardPractice, "sight_demonstrated");
                }
                else
                {
                    _mismatches++;
                    _stageEnteredAt = Time.unscaledTime;
                }
                return;
            }

            if (Stage == TutorialStage.GuardPractice)
            {
                if (evt.Intent == MindforgeIntentV29.Guard)
                {
                    _guardPass = true;
                    EnterStage(TutorialStage.MovementStress, "guard_demonstrated");
                }
                else
                {
                    _mismatches++;
                    _stageEnteredAt = Time.unscaledTime;
                }
                return;
            }

            if (Stage == TutorialStage.MovementStress)
            {
                _movementStressObserved = true;
                _movementStressOutcome = "resolved_" + evt.Intent.ToString().ToLowerInvariant();
            }
        }

        private void HandleWindowEnded(long epoch, string reason)
        {
            if (Stage == TutorialStage.SightPractice || Stage == TutorialStage.GuardPractice)
            {
                _abstentions++;
                _stageEnteredAt = Time.unscaledTime;
            }
            else if (Stage == TutorialStage.MovementStress && _movementWindowOpened)
            {
                _movementStressObserved = true;
                _movementStressOutcome = string.IsNullOrEmpty(reason) ? "ended" : reason;
            }
        }

        private void WriteReceipt()
        {
            if (_receiptWritten) return;
            _receiptWritten = true;
            ResolveDependencies();

            TutorialReceipt receipt = new TutorialReceipt
            {
                source_commit = _provenance != null ? _provenance.SourceCommit : null,
                clean_source = _provenance != null && _provenance.IsCleanSource,
                unity_version = Application.unityVersion,
                session_id = _markers != null ? _markers.SessionId : null,
                tutorial_status = Stage.ToString().ToLowerInvariant(),
                gaze_source_mode = _gazeProfiler != null ? _gazeProfiler.SourceMode : "unobserved",
                gaze_profile = _gazeProfiler != null ? _gazeProfiler.Snapshot() : new MindforgeGazeProfilerV34.ProfileSnapshot(),
                stimulus_layout_id = _layout != null ? _layout.LayoutId : (_stimulus != null ? _stimulus.LayoutId : null),
                stimulus_layout_frozen = _layout != null ? _layout.Frozen : (_stimulus != null && _stimulus.LayoutFrozen),
                default_layout_used = _layout != null && _layout.UsedDefaultLayout,
                recommended_aoi_radius = _layout != null ? _layout.RecommendedAoiRadius : 0.06f,
                recommended_acquisition_lead_ms = _layout != null ? _layout.RecommendedAcquisitionLeadMs : 350,
                calibration_id = _calibration != null ? _calibration.CalibrationId : null,
                calibration_state = _calibration != null ? _calibration.State.ToString() : "Unknown",
                sight_practice_pass = _sightPass,
                guard_practice_pass = _guardPass,
                sight_attempts = _sightAttempts,
                guard_attempts = _guardAttempts,
                practice_abstentions = _abstentions,
                practice_mismatches = _mismatches,
                movement_stress_observed = _movementStressObserved,
                movement_stress_outcome = _movementStressOutcome,
                neural_source_mode = _lastNeuralSource,
                bci_session_log = _sessionLogger != null ? _sessionLogger.LogPath : null,
                generated_utc = DateTime.UtcNow.ToString("o"),
            };

            try
            {
                string directory = Path.Combine(Application.persistentDataPath, "mindforge-bci");
                Directory.CreateDirectory(directory);
                string session = string.IsNullOrEmpty(receipt.session_id) ? Guid.NewGuid().ToString("N") : receipt.session_id;
                _receiptPath = Path.Combine(directory, "v34-tutorial-" + session + ".json");
                File.WriteAllText(_receiptPath, JsonUtility.ToJson(receipt, true) + Environment.NewLine);
                Debug.Log($"[Mindforge:V34:TUTORIAL] receipt={_receiptPath} status={receipt.tutorial_status} layout={receipt.stimulus_layout_id}.");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Mindforge:V34:TUTORIAL] receipt write failed: {ex.Message}");
            }
        }

        private void ResolveDependencies()
        {
            if (_gazeReceiver == null) _gazeReceiver = GetComponent<MindforgeUdpGazeReceiverV34>();
            if (_gazeProfiler == null) _gazeProfiler = GetComponent<MindforgeGazeProfilerV34>();
            if (_layout == null) _layout = GetComponent<MindforgeAdaptiveStimulusLayoutV34>();
            if (_stimulus == null) _stimulus = GetComponent<MindforgeBciStimulusV33>();
            if (_calibration == null) _calibration = GetComponent<MindforgeBciCalibrationDirectorV33>();
            if (_windows == null) _windows = GetComponent<MindforgeNeuralWindowControllerV33>();
            if (_bridge == null) _bridge = GetComponent<MindforgeNeuralIntentBridgeV33>();
            if (_markers == null) _markers = GetComponent<MindforgeBciMarkerSenderV33>();
            if (_provenance == null) _provenance = GetComponent<MindforgeNativeProvenanceV33>();
            if (_sessionLogger == null) _sessionLogger = GetComponent<MindforgeBciSessionLoggerV33>();
            if (_timing == null) _timing = GetComponent<MindforgeDisplayTimingMonitorV33>();
        }

        private void Bind()
        {
            if (_bridge != null) _bridge.SelectionAccepted += HandleSelectionAccepted;
            if (_windows != null) _windows.WindowEnded += HandleWindowEnded;
        }

        private void Unbind()
        {
            if (_bridge != null) _bridge.SelectionAccepted -= HandleSelectionAccepted;
            if (_windows != null) _windows.WindowEnded -= HandleWindowEnded;
        }

        private string StageInstruction()
        {
            switch (Stage)
            {
                case TutorialStage.Controls: return "WASD move • Arrow keys view • Space attack. Practice, then press Enter.";
                case TutorialStage.GazeHealth: return "Look at the center signal and hold your gaze naturally.";
                case TutorialStage.GazeGrid: return "Follow each signal with your eyes. No button press needed.";
                case TutorialStage.FreeExplore: return "Look around naturally. We are measuring interface behavior, not grading you.";
                case TutorialStage.GazeSwitch: return "Follow the moving signal as it changes position.";
                case TutorialStage.LayoutFreeze: return "Freezing this session's visual geometry before neural calibration.";
                case TutorialStage.WaitingForNeuralService: return "Waiting for the neural decoder service. Ordinary controls remain active.";
                case TutorialStage.Calibration: return "Calibration: rest, then focus Sight, then Guard as prompted.";
                case TutorialStage.SightPractice: return "Focus SIGHT when the neural window opens.";
                case TutorialStage.GuardPractice: return "Focus GUARD when the neural window opens.";
                case TutorialStage.MovementStress: return "Hold WASD or an Arrow key, then focus either neural target once.";
                case TutorialStage.Complete: return "Attunement complete. Your session evidence has been sealed.";
                case TutorialStage.Partial: return "Tutorial finished in safe partial mode. Ordinary gameplay remains available.";
                default: return "Press F9 to begin adaptive BCI onboarding.";
            }
        }

        private bool HasGazeTarget(out Vector2 target)
        {
            target = Vector2.zero;
            if (Stage == TutorialStage.GazeHealth)
            {
                target = new Vector2(0.50f, 0.50f);
                return true;
            }
            if (Stage == TutorialStage.GazeGrid && _promptIndex < GridTargets.Length)
            {
                target = GridTargets[_promptIndex];
                return true;
            }
            if (Stage == TutorialStage.GazeSwitch && _promptIndex < SwitchTargets.Length)
            {
                target = SwitchTargets[_promptIndex];
                return true;
            }
            return false;
        }

        private void OnGUI()
        {
            if (!_started && Stage == TutorialStage.Idle) return;

            float width = Mathf.Min(720f, Screen.width - 40f);
            Rect panel = new Rect(20f, 20f, width, 84f);
            GUI.Box(panel, "");
            GUI.Label(new Rect(panel.x + 14f, panel.y + 10f, panel.width - 28f, 24f),
                "MINDFORGE ATTUNEMENT  •  " + StatusLabel);
            GUI.Label(new Rect(panel.x + 14f, panel.y + 38f, panel.width - 28f, 38f), StageInstruction());

            Vector2 target;
            if (!HasGazeTarget(out target)) return;
            float size = 24f;
            float x = target.x * Screen.width - size * 0.5f;
            float y = (1f - target.y) * Screen.height - size * 0.5f;
            Color previous = GUI.color;
            GUI.color = _gazeProfiler != null && _gazeProfiler.CurrentPromptStable
                ? new Color(0.35f, 1f, 0.55f, 0.95f)
                : new Color(0.25f, 0.90f, 1f, 0.95f);
            GUI.DrawTexture(new Rect(x, y, size, size), Texture2D.whiteTexture);
            GUI.color = previous;
        }
    }
}
