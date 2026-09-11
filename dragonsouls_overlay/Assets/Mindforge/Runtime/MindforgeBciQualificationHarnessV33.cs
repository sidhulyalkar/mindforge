using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Mindforge.Chassis
{
    /// <summary>
    /// Deterministic controller-only native qualification for the BCI semantic seam.
    /// F8 drives Sight, Guard, timeout abstention and participant-pause abort through
    /// the same window/bridge/receptor path used by the decoder, then writes a receipt.
    /// This is explicitly not EEG evidence and not physical display-timing evidence.
    /// </summary>
    [DefaultExecutionOrder(1250)]
    [DisallowMultipleComponent]
    public sealed class MindforgeBciQualificationHarnessV33 : MonoBehaviour
    {
        public const string ReceiptSchema = "mindforge.bci_b0_receipt.v1";

        [Serializable]
        private sealed class B0Receipt
        {
            public string schema;
            public string generated_utc;
            public string source_commit;
            public bool provenance_available;
            public bool source_clean;
            public bool worktree_dirty;
            public bool overlay_applied;
            public string upstream_commit;
            public string unity_version;
            public string runtime_platform;
            public string mode;
            public string source_mode;
            public bool sight_selection_resolved;
            public bool sight_receptor_observed;
            public bool guard_selection_resolved;
            public bool guard_receptor_observed;
            public bool timeout_abstention_observed;
            public bool participant_pause_abort_observed;
            public bool functional_pass;
            public bool passed;
            public bool eeg_observed;
            public bool physical_display_timing_observed;
            public string failure;
        }

        private MindforgeNativeProvenanceV33 _provenance;
        private MindforgeBciStimulusV33 _stimulus;
        private MindforgeNeuralWindowControllerV33 _windows;
        private MindforgeNeuralIntentBridgeV33 _bridge;
        private MindforgeSightReceptorV33 _sight;
        private MindforgeGuardReceptorV33 _guard;
        private Coroutine _run;
        private int _windowEndSerial;
        private string _lastWindowEndReason;

        public bool Running => _run != null;
        public bool HasRun { get; private set; }
        public bool LastFunctionalPass { get; private set; }
        public bool LastPassed { get; private set; }
        public string LastFailure { get; private set; }
        public string LastReceiptPath { get; private set; }
        public string StatusLabel => !HasRun ? "UNRUN" : LastPassed ? "PASS" : LastFunctionalPass ? "FUNCTIONAL" : "FAIL";

        private void Start()
        {
            ResolveDependencies();
            BindWindowEvents();
        }

        private void OnDestroy()
        {
            if (_windows != null) _windows.WindowEnded -= HandleWindowEnded;
        }

        private void Update()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.f8Key.wasPressedThisFrame && !Running)
                RunB0();
#endif
        }

        public bool RunB0()
        {
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
            LastFailure = "qualification_disabled_in_release_build";
            return false;
#else
            if (Running) return false;
            ResolveDependencies();
            BindWindowEvents();
            _run = StartCoroutine(RunSequence());
            return true;
#endif
        }

        private IEnumerator RunSequence()
        {
            HasRun = false;
            LastFunctionalPass = false;
            LastPassed = false;
            LastFailure = null;
            LastReceiptPath = null;

            B0Receipt receipt = NewReceipt();
            if (_stimulus == null || _windows == null || _bridge == null || _sight == null || _guard == null)
            {
                receipt.failure = "b0_dependencies_missing";
                Finish(receipt);
                yield break;
            }

            if (!_stimulus.Installed)
            {
                receipt.failure = "stimulus_not_installed";
                Finish(receipt);
                yield break;
            }

            _stimulus.SetParticipantPaused(false);
            if (_windows.IsListening) _windows.Abort("b0_preflight_reset");
            yield return null;

            int sightBefore = _sight.ActivationCount;
            bool sightOpened = _windows.OpenWindow("b0_sight", requireCalibration: false);
            bool sightAccepted = sightOpened && _bridge.InjectControllerSimulation(MindforgeIntentV29.Sight, 1f);
            receipt.sight_selection_resolved = sightAccepted && !_windows.IsListening;
            receipt.sight_receptor_observed = _sight.ActivationCount > sightBefore;

            yield return new WaitForSecondsRealtime(0.85f);

            int guardBefore = _guard.ActivationCount;
            bool guardOpened = _windows.OpenWindow("b0_guard", requireCalibration: false);
            bool guardAccepted = guardOpened && _bridge.InjectControllerSimulation(MindforgeIntentV29.Guard, 1f);
            receipt.guard_selection_resolved = guardAccepted && !_windows.IsListening;
            receipt.guard_receptor_observed = _guard.ActivationCount > guardBefore && _guard.GuardActive;

            yield return new WaitForSecondsRealtime(0.85f);

            int timeoutSerial = _windowEndSerial;
            bool timeoutOpened = _windows.OpenWindow("b0_timeout", requireCalibration: false);
            if (timeoutOpened)
            {
                double deadline = Time.realtimeSinceStartupAsDouble + 4.25;
                while (_windows.IsListening && Time.realtimeSinceStartupAsDouble < deadline)
                    yield return null;
            }
            receipt.timeout_abstention_observed = timeoutOpened &&
                                                   _windowEndSerial > timeoutSerial &&
                                                   string.Equals(_lastWindowEndReason, "timeout_abstain", StringComparison.Ordinal);

            yield return new WaitForSecondsRealtime(0.85f);

            int pauseSerial = _windowEndSerial;
            bool pauseOpened = _windows.OpenWindow("b0_participant_pause", requireCalibration: false);
            if (pauseOpened)
            {
                _stimulus.SetParticipantPaused(true);
                yield return null;
                yield return null;
            }
            receipt.participant_pause_abort_observed = pauseOpened &&
                                                        !_windows.IsListening &&
                                                        _windowEndSerial > pauseSerial &&
                                                        string.Equals(_lastWindowEndReason, "participant_paused", StringComparison.Ordinal);
            _stimulus.SetParticipantPaused(false);

            receipt.functional_pass =
                receipt.sight_selection_resolved &&
                receipt.sight_receptor_observed &&
                receipt.guard_selection_resolved &&
                receipt.guard_receptor_observed &&
                receipt.timeout_abstention_observed &&
                receipt.participant_pause_abort_observed;
            receipt.passed = receipt.functional_pass && receipt.provenance_available && receipt.source_clean;

            if (!receipt.functional_pass)
                receipt.failure = BuildFunctionalFailure(receipt);
            else if (!receipt.provenance_available)
                receipt.failure = "functional_pass_but_provenance_missing";
            else if (!receipt.source_clean)
                receipt.failure = "functional_pass_but_source_not_clean";

            Finish(receipt);
        }

        private B0Receipt NewReceipt()
        {
            ResolveDependencies();
            return new B0Receipt
            {
                schema = ReceiptSchema,
                generated_utc = DateTime.UtcNow.ToString("o"),
                source_commit = _provenance != null ? _provenance.SourceCommit : null,
                provenance_available = _provenance != null && _provenance.IsAvailable,
                source_clean = _provenance != null && _provenance.IsCleanSource,
                worktree_dirty = _provenance != null && _provenance.WorktreeDirty,
                overlay_applied = _provenance != null && _provenance.OverlayApplied,
                upstream_commit = _provenance != null ? _provenance.UpstreamCommit : null,
                unity_version = Application.unityVersion,
                runtime_platform = Application.platform.ToString(),
                mode = "controller_only",
                source_mode = "simulated_decision",
                eeg_observed = false,
                physical_display_timing_observed = false,
            };
        }

        private static string BuildFunctionalFailure(B0Receipt receipt)
        {
            if (!receipt.sight_selection_resolved) return "sight_selection_not_resolved";
            if (!receipt.sight_receptor_observed) return "sight_receptor_not_observed";
            if (!receipt.guard_selection_resolved) return "guard_selection_not_resolved";
            if (!receipt.guard_receptor_observed) return "guard_receptor_not_observed";
            if (!receipt.timeout_abstention_observed) return "timeout_abstention_not_observed";
            if (!receipt.participant_pause_abort_observed) return "participant_pause_abort_not_observed";
            return "unknown_b0_failure";
        }

        private void Finish(B0Receipt receipt)
        {
            receipt.generated_utc = DateTime.UtcNow.ToString("o");
            LastFunctionalPass = receipt.functional_pass;
            LastPassed = receipt.passed;
            LastFailure = receipt.failure;
            HasRun = true;

            try
            {
                string directory = Path.Combine(Application.persistentDataPath, "mindforge-bci");
                Directory.CreateDirectory(directory);
                string stamp = DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffZ");
                LastReceiptPath = Path.Combine(directory, "b0-native-" + stamp + ".json");
                File.WriteAllText(LastReceiptPath, JsonUtility.ToJson(receipt, true) + Environment.NewLine);
            }
            catch (Exception ex)
            {
                LastPassed = false;
                LastFailure = "receipt_write_failed:" + ex.GetType().Name;
            }

            string result = LastPassed ? "PASS" : LastFunctionalPass ? "FUNCTIONAL_ONLY" : "FAIL";
            string message = $"[Mindforge:V33:B0] {result} source={receipt.source_commit ?? "unknown"} " +
                             $"clean={receipt.source_clean} receipt={LastReceiptPath ?? "unwritten"}";
            if (LastPassed) Debug.Log(message);
            else Debug.LogWarning(message + " reason=" + (LastFailure ?? "unknown"));
            _run = null;
        }

        private void HandleWindowEnded(long epoch, string reason)
        {
            _windowEndSerial++;
            _lastWindowEndReason = reason;
        }

        private void ResolveDependencies()
        {
            if (_provenance == null) _provenance = GetComponent<MindforgeNativeProvenanceV33>();
            if (_stimulus == null) _stimulus = GetComponent<MindforgeBciStimulusV33>();
            if (_windows == null) _windows = GetComponent<MindforgeNeuralWindowControllerV33>();
            if (_bridge == null) _bridge = GetComponent<MindforgeNeuralIntentBridgeV33>();
            if (_sight == null) _sight = GetComponent<MindforgeSightReceptorV33>();
            if (_guard == null) _guard = GetComponent<MindforgeGuardReceptorV33>();
        }

        private void BindWindowEvents()
        {
            if (_windows == null) return;
            _windows.WindowEnded -= HandleWindowEnded;
            _windows.WindowEnded += HandleWindowEnded;
        }
    }
}
