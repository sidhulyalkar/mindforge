using System;
using System.Collections.Generic;
using UnityEngine;
using Mindforge.Calibration;
using Mindforge.Combat;
using Mindforge.Journey;
using Mindforge.Neural;
using Mindforge.Telemetry;

namespace Mindforge.World
{
    public enum OpeningExperiencePhaseV08
    {
        Arrival = 0,
        Calibration = 1,
        Practice = 2,
        WorldReveal = 3,
        FirstEncounter = 4,
        Released = 5,
    }

    /// <summary>
    /// Pacing authority for the first 10-15 minutes. It does not resolve combat, movement,
    /// BCI selections or world interactions. It only exposes phase-level accessibility and a
    /// bounded projectile-speed assist consumed by existing enemy projectile authority.
    ///
    /// The opening is intentionally low-pressure: arrival -> calibration -> practice ->
    /// world reveal -> first encounter. Later combat remains mechanically identical, but
    /// V0.8 keeps a modest global projectile readability assist for the current build.
    /// </summary>
    [DefaultExecutionOrder(-680)]
    public sealed class OpeningExperienceDirectorV08 : MonoBehaviour
    {
        [SerializeField] private OpeningExperiencePhaseV08 phase = OpeningExperiencePhaseV08.Arrival;
        [SerializeField, Range(0.35f, 1f)] private float arrivalProjectileScale = 0.60f;
        [SerializeField, Range(0.35f, 1f)] private float calibrationProjectileScale = 0.60f;
        [SerializeField, Range(0.35f, 1f)] private float practiceProjectileScale = 0.66f;
        [SerializeField, Range(0.35f, 1f)] private float revealProjectileScale = 0.70f;
        [SerializeField, Range(0.35f, 1f)] private float firstEncounterProjectileScale = 0.74f;
        [SerializeField, Range(0.35f, 1f)] private float releasedProjectileScale = 0.82f;
        [SerializeField] private WorldStateLedger ledger;
        [SerializeField] private WorldSignalBus signals;
        [SerializeField] private UdpGameMarkerSender markers;

        private static OpeningExperienceDirectorV08 _active;

        public OpeningExperiencePhaseV08 Phase => phase;
        public static OpeningExperienceDirectorV08 Active => _active;
        public static float EnemyProjectileSpeedScale
            => _active != null ? _active.ResolveProjectileScale() : 1f;

        public event Action<OpeningExperiencePhaseV08> PhaseChanged;

        private void Awake()
        {
            _active = this;
            Resolve();
            PublishPhase("opening_v08_awake");
        }

        private void OnEnable()
        {
            _active = this;
            Resolve();
        }

        private void OnDisable()
        {
            if (_active == this) _active = null;
        }

        public bool AdvanceTo(OpeningExperiencePhaseV08 next, string reason)
        {
            if ((int)next <= (int)phase) return false;
            phase = next;
            PublishPhase(string.IsNullOrWhiteSpace(reason) ? "opening_v08_progress" : reason);
            PhaseChanged?.Invoke(phase);
            return true;
        }

        public void RestorePhase(OpeningExperiencePhaseV08 restored)
        {
            phase = restored;
            PublishPhase("opening_v08_restore");
        }

        private float ResolveProjectileScale()
        {
            switch (phase)
            {
                case OpeningExperiencePhaseV08.Arrival: return arrivalProjectileScale;
                case OpeningExperiencePhaseV08.Calibration: return calibrationProjectileScale;
                case OpeningExperiencePhaseV08.Practice: return practiceProjectileScale;
                case OpeningExperiencePhaseV08.WorldReveal: return revealProjectileScale;
                case OpeningExperiencePhaseV08.FirstEncounter: return firstEncounterProjectileScale;
                default: return releasedProjectileScale;
            }
        }

        private void PublishPhase(string reason)
        {
            Resolve();
            string value = phase.ToString().ToLowerInvariant();
            ledger?.SetString("profile.opening.v08.phase", value, reason);
            signals?.Publish(
                WorldSignalKind.ProgressionChanged,
                "opening.v08.phase",
                subject: value,
                stringValue: value,
                intValue: (int)phase,
                reason: reason);
            markers?.Emit(
                "OPENING_PHASE",
                "journey",
                target: value,
                reason: reason,
                value: (int)phase);
        }

        private void Resolve()
        {
            if (ledger == null) ledger = FindObjectOfType<WorldStateLedger>(true);
            if (signals == null) signals = FindObjectOfType<WorldSignalBus>(true);
            if (markers == null) markers = FindObjectOfType<UdpGameMarkerSender>(true);
        }
    }

    /// <summary>Spatial phase transition. Only the Guardian may advance the opening.</summary>
    public sealed class OpeningPhaseTriggerV08 : MonoBehaviour
    {
        [SerializeField] private OpeningExperienceDirectorV08 director;
        [SerializeField] private OpeningExperiencePhaseV08 targetPhase = OpeningExperiencePhaseV08.Practice;
        [SerializeField] private string reason = "opening_v08_trigger";
        [SerializeField] private bool oneShot = true;
        private bool _used;

        public void ConfigureRuntime(OpeningExperienceDirectorV08 value, OpeningExperiencePhaseV08 phase, string why)
        {
            director = value;
            targetPhase = phase;
            reason = why;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_used && oneShot) return;
            if (other == null || other.GetComponentInParent<GuardianMotor>() == null) return;
            if (director == null) director = FindObjectOfType<OpeningExperienceDirectorV08>(true);
            if (director == null) return;
            if (director.AdvanceTo(targetPhase, reason)) _used = true;
        }
    }

    /// <summary>
    /// Owns the sanctum threshold calibration requirement. A genuine Python-accepted
    /// calibration opens immediately. Controller-only preview never forges neural success;
    /// instead the player may inspect a small number of resonance stations to continue.
    /// </summary>
    public sealed class SanctumCalibrationSequenceV08 : MonoBehaviour
    {
        [SerializeField] private OpeningExperienceDirectorV08 opening;
        [SerializeField] private AwakeningCalibrationDirector neuralCalibration;
        [SerializeField] private JourneyGate thresholdGate;
        [SerializeField, Min(1)] private int previewStationsRequired = 2;
        [SerializeField] private WorldStateLedger ledger;
        [SerializeField] private WorldSignalBus signals;
        [SerializeField] private UdpGameMarkerSender markers;

        private readonly HashSet<string> _previewStations = new HashSet<string>(StringComparer.Ordinal);
        private bool _unlocked;

        public bool ThresholdUnlocked => _unlocked;
        public int PreviewStationsVisited => _previewStations.Count;
        public int PreviewStationsRequired => Mathf.Max(1, previewStationsRequired);
        public bool NeuralCalibrationAccepted => neuralCalibration != null && neuralCalibration.CalibrationReady;

        public void ConfigureRuntime(
            OpeningExperienceDirectorV08 director,
            AwakeningCalibrationDirector calibration,
            JourneyGate gate,
            int requiredPreviewStations,
            WorldStateLedger world,
            WorldSignalBus bus,
            UdpGameMarkerSender markerSender)
        {
            opening = director;
            neuralCalibration = calibration;
            thresholdGate = gate;
            previewStationsRequired = Mathf.Max(1, requiredPreviewStations);
            ledger = world;
            signals = bus;
            markers = markerSender;
            Subscribe();
            thresholdGate?.SetOpen(false, true);
        }

        private void OnEnable()
        {
            Resolve();
            Subscribe();
            thresholdGate?.SetOpen(_unlocked, true);
        }

        private void OnDisable() => Unsubscribe();

        public bool MarkPreviewStation(string stableId, float nominalFrequencyHz)
        {
            string id = PlayerInventoryV06.NormalizeId(stableId);
            if (string.IsNullOrEmpty(id) || !_previewStations.Add(id)) return false;

            opening?.AdvanceTo(OpeningExperiencePhaseV08.Calibration, "sanctum_resonance_station");
            ledger?.SetBool("profile.opening.calibration_preview." + id, true, "controller_preview_station");
            markers?.Emit(
                "CALIBRATION_PREVIEW_STATION",
                "calibration",
                target: id,
                reason: "VISUAL_PREVIEW_NOT_NEURAL_EVIDENCE",
                value: nominalFrequencyHz);

            signals?.Publish(
                WorldSignalKind.ProgressionChanged,
                "calibration.preview.station",
                subject: id,
                floatValue: nominalFrequencyHz,
                intValue: _previewStations.Count,
                reason: "visual_preview_only");

            if (_previewStations.Count >= PreviewStationsRequired)
                UnlockThreshold(false, "controller_preview_complete");
            return true;
        }

        private void OnCalibrationStageChanged(string stage)
        {
            if (string.Equals(stage, "ready", StringComparison.OrdinalIgnoreCase))
                UnlockThreshold(true, "participant_calibration_accepted");
        }

        private void UnlockThreshold(bool neuralAccepted, string reason)
        {
            if (_unlocked) return;
            _unlocked = true;
            thresholdGate?.SetOpen(true);
            ledger?.SetBool("profile.opening.sanctum_threshold_unlocked", true, reason);
            ledger?.SetBool("profile.opening.calibration_neural_accepted", neuralAccepted, reason);
            signals?.Publish(
                WorldSignalKind.Milestone,
                "sanctum.threshold.unlocked",
                subject: "sanctum_threshold",
                intValue: neuralAccepted ? 2 : 1,
                reason: reason);
            markers?.Emit(
                "SANCTUM_THRESHOLD_UNLOCKED",
                "journey",
                target: "sanctum_threshold",
                reason: neuralAccepted ? "NEURAL_CALIBRATION_ACCEPTED" : "CONTROLLER_PREVIEW_COMPLETE");
        }

        private void Subscribe()
        {
            if (neuralCalibration == null) return;
            neuralCalibration.CalibrationStageChanged -= OnCalibrationStageChanged;
            neuralCalibration.CalibrationStageChanged += OnCalibrationStageChanged;
            if (neuralCalibration.CalibrationReady) UnlockThreshold(true, "participant_calibration_already_ready");
        }

        private void Unsubscribe()
        {
            if (neuralCalibration != null)
                neuralCalibration.CalibrationStageChanged -= OnCalibrationStageChanged;
        }

        private void Resolve()
        {
            if (opening == null) opening = FindObjectOfType<OpeningExperienceDirectorV08>(true);
            if (neuralCalibration == null) neuralCalibration = FindObjectOfType<AwakeningCalibrationDirector>(true);
            if (ledger == null) ledger = FindObjectOfType<WorldStateLedger>(true);
            if (signals == null) signals = FindObjectOfType<WorldSignalBus>(true);
            if (markers == null) markers = FindObjectOfType<UdpGameMarkerSender>(true);
        }
    }

    /// <summary>
    /// One contextual E offer for a resonance station. The short local flicker is explicitly a
    /// controller-preview visualization and must never be interpreted as calibrated SSVEP
    /// evidence. Qualified stimulation remains owned by the existing calibration protocol.
    /// </summary>
    public sealed class SanctumCalibrationOrbV08 : WorldInteractionSourceV1
    {
        [SerializeField] private string stableWorldId = "sanctum.resonance.01";
        [SerializeField] private float nominalFrequencyHz = 10f;
        [SerializeField] private SanctumCalibrationSequenceV08 sequence;
        [SerializeField] private Renderer orbRenderer;
        [SerializeField] private Color restingColor = new Color(0.08f, 0.72f, 1f, 1f);
        [SerializeField] private float previewSeconds = 3f;

        private MaterialPropertyBlock _block;
        private float _previewUntil;
        private bool _visited;
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        public override string InteractionId => "calibration." + PlayerInventoryV06.NormalizeId(stableWorldId) + ".preview";
        public override string Prompt => _visited
            ? $"Review {nominalFrequencyHz:0.#} Hz Resonance"
            : $"Inspect {nominalFrequencyHz:0.#} Hz Resonance";
        public override float Radius => 3.4f;
        public override int Priority => 21;

        public void ConfigureRuntime(
            string id,
            float frequencyHz,
            SanctumCalibrationSequenceV08 calibrationSequence,
            Renderer renderer)
        {
            stableWorldId = PlayerInventoryV06.NormalizeId(id);
            nominalFrequencyHz = Mathf.Clamp(frequencyHz, 4f, 30f);
            sequence = calibrationSequence;
            orbRenderer = renderer;
            EnsureBlock();
            ApplyVisual(restingColor * 1.35f);
        }

        public override bool CanInteract(Transform actor) => sequence != null && !string.IsNullOrEmpty(stableWorldId);

        public override bool TryInteract(Transform actor)
        {
            if (!CanInteract(actor)) return false;
            bool first = sequence.MarkPreviewStation(stableWorldId, nominalFrequencyHz);
            _visited |= first;
            _previewUntil = Time.unscaledTime + Mathf.Max(0.5f, previewSeconds);
            return true;
        }

        private void Awake()
        {
            if (orbRenderer == null) orbRenderer = GetComponentInChildren<Renderer>(true);
            EnsureBlock();
        }

        private void Update()
        {
            if (orbRenderer == null) return;
            if (Time.unscaledTime >= _previewUntil)
            {
                ApplyVisual(restingColor * (_visited ? 1.65f : 1.15f));
                return;
            }

            // Visual preview only. Do not use this render-frame flicker as scientific timing.
            double phase = Time.realtimeSinceStartupAsDouble * nominalFrequencyHz * Math.PI * 2.0;
            float pulse = Math.Sin(phase) >= 0.0 ? 2.8f : 0.20f;
            ApplyVisual(restingColor * pulse);
        }

        private void EnsureBlock()
        {
            if (_block == null) _block = new MaterialPropertyBlock();
        }

        private void ApplyVisual(Color color)
        {
            if (orbRenderer == null) return;
            EnsureBlock();
            orbRenderer.GetPropertyBlock(_block);
            _block.SetColor(BaseColor, color);
            _block.SetColor(EmissionColor, color * 1.5f);
            orbRenderer.SetPropertyBlock(_block);
        }
    }

    [Serializable]
    public sealed class CalibrationCandidateV08
    {
        public float stimulus_hz;
        public float confidence;
        public float quality;
        public int rank;
    }

    /// <summary>
    /// Stores participant-specific DERIVED calibration evidence only. It listens to optional
    /// v2 neural-event calibration fields and persists selected frequencies/quality through
    /// profile.* world facts. No raw EEG or sample arrays cross into Unity.
    /// </summary>
    public sealed class ParticipantCalibrationProfileV08 : MonoBehaviour
    {
        [SerializeField] private UdpNeuralReceiver receiver;
        [SerializeField] private WorldStateLedger ledger;
        [SerializeField] private List<CalibrationCandidateV08> candidates = new List<CalibrationCandidateV08>();

        public IReadOnlyList<CalibrationCandidateV08> Candidates => candidates;
        public float SelectedSightHz { get; private set; }
        public float SelectedGuardHz { get; private set; }

        public void ConfigureRuntime(UdpNeuralReceiver neuralReceiver, WorldStateLedger world)
        {
            Unsubscribe();
            receiver = neuralReceiver;
            ledger = world;
            Subscribe();
        }

        private void OnEnable()
        {
            if (receiver == null) receiver = FindObjectOfType<UdpNeuralReceiver>(true);
            if (ledger == null) ledger = FindObjectOfType<WorldStateLedger>(true);
            Subscribe();
        }

        private void OnDisable() => Unsubscribe();

        private void Subscribe()
        {
            if (receiver == null) return;
            receiver.EventReceived -= OnNeuralEvent;
            receiver.EventReceived += OnNeuralEvent;
        }

        private void Unsubscribe()
        {
            if (receiver != null) receiver.EventReceived -= OnNeuralEvent;
        }

        private void OnNeuralEvent(NeuralEvent evt)
        {
            if (evt == null) return;
            if (evt.IsCalibrationCandidateScore && evt.stimulus_hz > 0f)
            {
                UpsertCandidate(evt.stimulus_hz, evt.confidence, evt.quality, evt.candidate_rank);
                return;
            }
            if (!evt.IsCalibrationReady) return;

            if (evt.selected_sight_hz > 0f)
            {
                SelectedSightHz = evt.selected_sight_hz;
                ledger?.SetFloat("profile.bci.selected_sight_hz", SelectedSightHz, "participant_calibration_ready");
            }
            if (evt.selected_guard_hz > 0f)
            {
                SelectedGuardHz = evt.selected_guard_hz;
                ledger?.SetFloat("profile.bci.selected_guard_hz", SelectedGuardHz, "participant_calibration_ready");
            }
            ledger?.SetFloat("profile.bci.calibration_confidence", evt.confidence, "participant_calibration_ready");
            ledger?.SetFloat("profile.bci.calibration_quality", evt.quality, "participant_calibration_ready");
            ledger?.SetString("profile.bci.calibration_id", evt.calibration_id ?? string.Empty, "participant_calibration_ready");
        }

        private void UpsertCandidate(float hz, float confidence, float quality, int rank)
        {
            for (int i = 0; i < candidates.Count; i++)
            {
                if (Mathf.Abs(candidates[i].stimulus_hz - hz) > 0.01f) continue;
                candidates[i].confidence = confidence;
                candidates[i].quality = quality;
                candidates[i].rank = rank;
                return;
            }
            candidates.Add(new CalibrationCandidateV08
            {
                stimulus_hz = hz,
                confidence = confidence,
                quality = quality,
                rank = rank,
            });
            candidates.Sort((a, b) => a.rank != b.rank ? a.rank.CompareTo(b.rank) : b.confidence.CompareTo(a.confidence));
        }
    }
}
