# Mindforge: Neural Game Platform v1–v8

## Architecture invariant

Raw EEG stays outside the game. Keep independent authority lanes:
- Research EEG and reproducible offline evidence.
- Passive, read-only, continuously varying features controlling bounded audiovisual presentation.
- Explicit calibrated active decisions with uncertainty, TTL, abstention and participant-stop.
- Legal world-edit transactions with preview, confirmation, collision constraints, undo and persistence.

Existing Sight/Guard SSVEP remains the reference combat path. Simulation never counts as real human evidence.

| Version | Scope | Release gate |
| --- | --- | --- |
| v1 — Spatial selection | Offline task-specific scalp electrode group baseline | Train/validation/untouched holdout separation; artifact warning; no live command |
| v2 — Calibration Lab (this PR) | Standalone scalp map, candidate scores, session counts, provenance and confounds | Privacy and rendering tests; synthetic reproducibility; no Unity changes |
| v3 — Living World | Passive-only NeuralFeatureFrame v1, Unity shader/atmosphere adapter | Quality gating, slow interpolation, disconnect safe fallback, photosensitivity controls, gameplay parity |
| v4 — Multimodal decoder study | SSVEP, motor imagery, imagined speech, BCI Essentials adapters | Controlled independent real-session evaluation including facial EMG, EOG/gaze and idle |
| v5 — Neural Constructor | Wall/bridge/bloom using SSVEP selection and deterministic procedural geometry | Placement preview, explicit confirmation, collision/nav, resource checks, undo, stable saves |
| v6 — Portable mods | Semantic action contracts and explicit supported game APIs | Real independent mod installation, safe fallback and compatibility evidence for each game |
| v7 — Human qualification | Participant-specific calibration and cross-day stability | Preplanned real EEG studies with accuracy, command coverage, false activations/min and latency |
| v8 — Generative world | Contextual AI suggestions and procedural sandbox | Deterministic validity, permissions, reproducibility, bounded operations and rollback |

## Project priorities

Do not let new neural functionality delay the existing P1 Unity compile and P2 controller-only encounter. Gameplay readability and signal evidence are independent qualification gates. Neither electrode selection nor spectral power is equivalent to brain source localization, emotions, or imagined speech. Neither simulation nor a green Python CI run validates real hardware.

For v3: (1) define a versioned passive-only feature frame with session/source identity, units, normalized value, signal quality and expiry; (2) use synthetic/replayed EEG first; (3) opt-in atmospheric rendering with clamps, low-pass smoothing and no rapid flicker; (4) verify no changes to coded SSVEP stimulus phase or to combat/physics; (5) fail safely on low-quality/stale data; (6) test on actual Unity builds with replay and accessibility settings.

For v4: imagined speech remains offline research until independent session performance and explicit facial/jaw/neck EMG controls justify further study. For v5 and v8: EEG may select a constrained building intention, but game geometry must be confirmed and validated before mutation.

Source of truth: this document is a design and acceptance plan, not proof of shipped v3–v8 functionality.
