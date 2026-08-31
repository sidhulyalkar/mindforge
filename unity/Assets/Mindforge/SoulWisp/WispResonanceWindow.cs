using System;
using System.Collections;
using UnityEngine;
using Mindforge.Combat;
using Mindforge.Neural;
using Mindforge.Telemetry;

namespace Mindforge.SoulWisp
{
    public enum WispResonanceState
    {
        Idle = 0,
        Priming = 1,
        Listening = 2,
        Resolved = 3,
        Abstained = 4,
        Cooldown = 5,
    }

    /// <summary>
    /// Player-armed decision window for the first research-grounded Mindforge BCI loop.
    ///
    /// V answers WHEN the player wants a neural decision. It never says WHICH decision.
    /// During Listening, only a fresh derived NeuralEvent may resolve Sight or Guard.
    /// Ordinary movement remains player-owned; the Wisp never zeroes movement input. Instead,
    /// an independent read-only motion qualifier tells this state machine whether the visual/
    /// EEG interval is stable enough to use. Camera, attack, evade, target lock and interaction
    /// remain conventional authorities, and this class never depends on the locomotion motor.
    /// </summary>
    [DefaultExecutionOrder(-120)]
    [RequireComponent(typeof(SoulWispController))]
    public sealed class WispResonanceWindow : MonoBehaviour
    {
        [SerializeField] private SoulWispController wisp;
        [SerializeField] private GuardianControlProfileV1 controls;
        [SerializeField] private UdpNeuralReceiver neuralReceiver;
        [SerializeField] private UdpGameMarkerSender markerSender;
        [SerializeField] private DisplayTimingMonitor displayTiming;
        [SerializeField] private WispMotionQualification motionQualification;

        [Header("Decision timing")]
        [Tooltip("Neutral cores settle before modulation starts. This is presentation time, not decoder evidence.")]
        [SerializeField] private float settleSeconds = 0.09f;
        [Tooltip("Maximum coded decision duration. Dynamic stopping may resolve earlier.")]
        [SerializeField] private float listeningSeconds = 1.50f;
        [SerializeField] private float outcomeHoldSeconds = 0.34f;
        [SerializeField] private float cooldownSeconds = 0.72f;

        [Header("Authority")]
        [SerializeField] private bool requireCombatTarget = true;
        [SerializeField] private bool requireHoldThroughDecision = true;
        [Tooltip("Selections with less post-onset EEG than this never gain gameplay authority.")]
        [SerializeField] private int minimumEvidenceMs = 450;
        [SerializeField] private bool requireHealthyDisplayTimingForNeuralAuthority = true;

#if UNITY_EDITOR
        [Header("Editor-only gameplay simulation")]
        [Tooltip("1=Sight, 2=Guard, 0=Abstain while Listening. Never compiled as production neural authority.")]
        [SerializeField] private bool editorKeyboardSimulation = true;
        [SerializeField] private AuraBuffController editorBuffs;
#endif

        private double _stateEnteredAt;
        private long _minimumSelectionSeq = -1;
        private long _windowId;
        private string _lastOutcome = string.Empty;
        private AuraTarget _lastResolvedTarget = AuraTarget.None;

        public WispResonanceState State { get; private set; } = WispResonanceState.Idle;
        public bool SelectionAuthorityOpen => State == WispResonanceState.Listening;
        public long WindowId => _windowId;
        public long MinimumSelectionSequence => _minimumSelectionSeq;
        public string LastOutcome => _lastOutcome;
        public AuraTarget LastResolvedTarget => _lastResolvedTarget;
        public bool MotionQualifiedForArm => motionQualification != null && motionQualification.MotionQualifiedForArm;
        public string MotionBlockReason => motionQualification != null
            ? motionQualification.ArmBlockReason
            : "MOTION_STATE_UNAVAILABLE";
        public bool CanArm => State == WispResonanceState.Idle &&
                              wisp != null &&
                              (!requireCombatTarget || wisp.InCombat) &&
                              !wisp.StimuliResting &&
                              MotionQualifiedForArm;

        public float StateProgress
        {
            get
            {
                float duration = DurationFor(State);
                if (duration <= 0f) return 0f;
                return Mathf.Clamp01((float)((Now - _stateEnteredAt) / duration));
            }
        }

        public float ListeningRemaining => State == WispResonanceState.Listening
            ? Mathf.Max(0f, listeningSeconds - (float)(Now - _stateEnteredAt))
            : 0f;

        public event Action<long> WindowArmed;
        public event Action<long> ListeningStarted;
        public event Action<long, AuraTarget> WindowResolved;
        public event Action<long, string> WindowAbstained;
        public event Action<long> WindowEnded;

        private static double Now => Time.realtimeSinceStartupAsDouble;

        private void Awake()
        {
            ResolveReferences();
            Enter(WispResonanceState.Idle);
        }

        private void OnEnable() => ResolveReferences();

        private void OnDisable()
        {
            if (wisp != null) wisp.EndResonanceWindow();
            State = WispResonanceState.Idle;
            _minimumSelectionSeq = -1;
        }

        public void Bind(UdpNeuralReceiver receiver, GuardianControlProfileV1 profile)
        {
            if (receiver != null) neuralReceiver = receiver;
            if (profile != null) controls = profile;
        }

        private void ResolveReferences()
        {
            if (wisp == null) wisp = GetComponent<SoulWispController>();
            if (controls == null) controls = GuardianControlProfileV1.ResolveOrCreate();
            if (neuralReceiver == null) neuralReceiver = FindObjectOfType<UdpNeuralReceiver>(true);
            if (markerSender == null) markerSender = FindObjectOfType<UdpGameMarkerSender>(true);
            if (displayTiming == null) displayTiming = FindObjectOfType<DisplayTimingMonitor>(true);
            if (motionQualification == null) motionQualification = GetComponent<WispMotionQualification>();
            if (motionQualification == null) motionQualification = gameObject.AddComponent<WispMotionQualification>();
#if UNITY_EDITOR
            if (editorBuffs == null) editorBuffs = FindObjectOfType<AuraBuffController>(true);
#endif
        }

        private void Update()
        {
            if (wisp == null || controls == null || motionQualification == null) ResolveReferences();

            if (State == WispResonanceState.Idle)
            {
                if (controls != null && controls.Pressed(GuardianControlAction.ChannelWisp))
                    TryArm();
                return;
            }

            if (State == WispResonanceState.Priming || State == WispResonanceState.Listening)
            {
                if (requireHoldThroughDecision && controls != null && !controls.Held(GuardianControlAction.ChannelWisp))
                {
                    Abstain("PLAYER_RELEASED");
                    return;
                }
                if (requireCombatTarget && (wisp == null || !wisp.InCombat))
                {
                    Abstain("TARGET_LOST");
                    return;
                }
                if (motionQualification == null || motionQualification.TryGetEvidenceInstability(out string motionReason))
                {
                    Abstain(string.IsNullOrEmpty(motionReason) ? "MOTION_STATE_UNAVAILABLE" : motionReason);
                    return;
                }
            }

            switch (State)
            {
                case WispResonanceState.Priming:
                    if (Now - _stateEnteredAt >= Mathf.Max(0.02f, settleSeconds))
                        BeginListening();
                    break;
                case WispResonanceState.Listening:
#if UNITY_EDITOR
                    HandleEditorSimulation();
                    if (State != WispResonanceState.Listening) break;
#endif
                    if (Now - _stateEnteredAt >= Mathf.Max(0.15f, listeningSeconds))
                        Abstain("TIMEOUT");
                    break;
                case WispResonanceState.Resolved:
                case WispResonanceState.Abstained:
                    if (Now - _stateEnteredAt >= Mathf.Max(0.05f, outcomeHoldSeconds))
                        Enter(WispResonanceState.Cooldown);
                    break;
                case WispResonanceState.Cooldown:
                    if (Now - _stateEnteredAt >= Mathf.Max(0.05f, cooldownSeconds))
                    {
                        long ended = _windowId;
                        Enter(WispResonanceState.Idle);
                        WindowEnded?.Invoke(ended);
                        markerSender?.Emit("NEURAL_WINDOW_ENDED", "neural_window", stimulusEpoch: ended);
                    }
                    break;
            }
        }

        public bool TryArm()
        {
            if (!CanArm) return false;
            if (!wisp.PrepareResonanceWindow()) return false;

            _windowId++;
            _lastOutcome = string.Empty;
            _lastResolvedTarget = AuraTarget.None;
            _minimumSelectionSeq = -1;
            Enter(WispResonanceState.Priming);
            WindowArmed?.Invoke(_windowId);
            markerSender?.Emit(
                "NEURAL_WINDOW_ARMED",
                "neural_window",
                value: listeningSeconds,
                stimulusEpoch: _windowId);
            return true;
        }

        private void BeginListening()
        {
            if (wisp == null || !wisp.BeginCodedResonance())
            {
                Abstain("STIMULUS_UNAVAILABLE");
                return;
            }

            _minimumSelectionSeq = neuralReceiver != null ? neuralReceiver.LastSeenSequence : -1;
            Enter(WispResonanceState.Listening);
            ListeningStarted?.Invoke(_windowId);
            markerSender?.Emit(
                "NEURAL_WINDOW_LISTENING",
                "neural_window",
                value: listeningSeconds,
                stimulusEpoch: _windowId);
        }

        /// <summary>
        /// Pure authority check used by DualAuraCombatDirector. A selection that was already
        /// visible to Unity before this listening epoch can never be replayed into the window.
        /// </summary>
        public bool CanAcceptSelection(NeuralEvent evt)
        {
            if (State != WispResonanceState.Listening || evt == null || !evt.IsSelection) return false;
            if (evt.Target == AuraTarget.None || evt.artifact || !evt.IsV2) return false;
            if (evt.seq <= _minimumSelectionSeq) return false;
            if (evt.stimulus_epoch != _windowId) return false;
            if (evt.evidence_ms < Mathf.Max(0, minimumEvidenceMs)) return false;
            if (motionQualification == null || !motionQualification.EvidenceQualified) return false;
#if UNITY_EDITOR
            bool editorSimulation = string.Equals(evt.source_mode, "unity_editor_resonance_sim", StringComparison.Ordinal);
#else
            bool editorSimulation = false;
#endif
            if (!editorSimulation && requireHealthyDisplayTimingForNeuralAuthority &&
                (displayTiming == null || !displayTiming.HasMeasurement || !displayTiming.TimingHealthy))
                return false;
            return true;
        }

        /// <summary>Called only after AuraBuffController itself accepted the event.</summary>
        public void MarkResolved(AuraTarget target)
        {
            if (State != WispResonanceState.Listening || target == AuraTarget.None) return;
            _lastResolvedTarget = target;
            _lastOutcome = target == AuraTarget.Sight ? "SIGHT" : "GUARD";
            wisp?.EndResonanceWindow();
            Enter(WispResonanceState.Resolved);
            WindowResolved?.Invoke(_windowId, target);
            markerSender?.Emit(
                "NEURAL_WINDOW_RESOLVED",
                "neural_window",
                target: target == AuraTarget.Sight ? "sight" : "guard",
                stimulusEpoch: _windowId);
        }

        public void ObserveAbstain(NeuralEvent evt)
        {
            if (State != WispResonanceState.Listening || evt == null || !evt.IsAbstain) return;
            if (evt.seq <= _minimumSelectionSeq || !evt.IsV2) return;
            if (evt.stimulus_epoch != _windowId) return;
            Abstain(string.IsNullOrEmpty(evt.reason) ? "DECODER_ABSTAIN" : evt.reason);
        }

        public void AbortForLinkLoss(string reason)
        {
            if (State == WispResonanceState.Priming || State == WispResonanceState.Listening)
                Abstain(string.IsNullOrEmpty(reason) ? "BCI_LINK_LOST" : reason);
        }

        private void Abstain(string reason)
        {
            if (State != WispResonanceState.Priming && State != WispResonanceState.Listening) return;
            _lastOutcome = string.IsNullOrEmpty(reason) ? "ABSTAIN" : reason;
            _lastResolvedTarget = AuraTarget.None;
            wisp?.EndResonanceWindow();
            Enter(WispResonanceState.Abstained);
            WindowAbstained?.Invoke(_windowId, _lastOutcome);
            markerSender?.Emit(
                "NEURAL_WINDOW_ABSTAINED",
                "neural_window",
                reason: _lastOutcome,
                stimulusEpoch: _windowId);
        }

        private void Enter(WispResonanceState state)
        {
            State = state;
            _stateEnteredAt = Now;
            if (state == WispResonanceState.Idle)
                _minimumSelectionSeq = -1;
        }

        private float DurationFor(WispResonanceState state)
        {
            switch (state)
            {
                case WispResonanceState.Priming: return Mathf.Max(0.02f, settleSeconds);
                case WispResonanceState.Listening: return Mathf.Max(0.15f, listeningSeconds);
                case WispResonanceState.Resolved:
                case WispResonanceState.Abstained: return Mathf.Max(0.05f, outcomeHoldSeconds);
                case WispResonanceState.Cooldown: return Mathf.Max(0.05f, cooldownSeconds);
                default: return 0f;
            }
        }

#if UNITY_EDITOR
        private void HandleEditorSimulation()
        {
            if (!editorKeyboardSimulation) return;
            if (Input.GetKeyDown(KeyCode.Alpha0))
            {
                Abstain("EDITOR_SIM_ABSTAIN");
                return;
            }
            AuraTarget target = AuraTarget.None;
            if (Input.GetKeyDown(KeyCode.Alpha1)) target = AuraTarget.Sight;
            else if (Input.GetKeyDown(KeyCode.Alpha2)) target = AuraTarget.Guard;
            if (target == AuraTarget.None || editorBuffs == null) return;

            NeuralEvent simulated = new NeuralEvent
            {
                schema = NeuralEvent.SchemaV2,
                seq = Math.Max(_minimumSelectionSeq + 1, neuralReceiver != null ? neuralReceiver.LastSeenSequence + 1 : 1),
                @event = "AURA_SELECTED",
                target = target == AuraTarget.Sight ? "sight" : "guard",
                confidence = 1f,
                quality = 1f,
                paradigm = "EDITOR_GAMEPLAY_SIM",
                model_id = "unity_editor",
                artifact = false,
                has_evidence = true,
                margin = 1f,
                source_mode = "unity_editor_resonance_sim",
                authority_ttl_ms = 250,
                stimulus_epoch = _windowId,
                evidence_ms = 750,
            };

            if (CanAcceptSelection(simulated) && editorBuffs.TryApply(simulated))
                MarkResolved(target);
        }
#endif
    }

    /// <summary>
    /// Merge-friendly bootstrap so existing scenes gain the state machine without serialized
    /// scene surgery. It binds the motion-quality sensor and combat authority gate once the
    /// Wisp and aura director exist.
    /// </summary>
    public sealed class WispResonanceBootstrap : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (FindObjectOfType<WispResonanceBootstrap>(true) != null) return;
            new GameObject("MindforgeWispResonanceBootstrap").AddComponent<WispResonanceBootstrap>();
        }

        private IEnumerator Start()
        {
            for (int frame = 0; frame < 300; frame++)
            {
                SoulWispController wisp = FindObjectOfType<SoulWispController>(true);
                DualAuraCombatDirector director = FindObjectOfType<DualAuraCombatDirector>(true);
                if (wisp != null && director != null)
                {
                    if (wisp.GetComponent<WispMotionQualification>() == null)
                        wisp.gameObject.AddComponent<WispMotionQualification>();
                    WispResonanceWindow window = wisp.GetComponent<WispResonanceWindow>();
                    if (window == null) window = wisp.gameObject.AddComponent<WispResonanceWindow>();
                    director.BindResonanceWindow(window);
                    if (wisp.GetComponent<WispResonanceHud>() == null)
                        wisp.gameObject.AddComponent<WispResonanceHud>();
                    Destroy(gameObject);
                    yield break;
                }
                yield return null;
            }

            Debug.LogWarning("[Mindforge:Wisp] Resonance bootstrap could not bind a Wisp and DualAuraCombatDirector.");
            Destroy(gameObject);
        }
    }
}
