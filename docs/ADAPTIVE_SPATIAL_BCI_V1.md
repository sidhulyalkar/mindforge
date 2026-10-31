# Adaptive Spatial EEG v1: research-only sensor selection

## Scope

This experiment determines which **scalp electrode group** gives the best *held-out* discriminatory performance for a specified, labeled neural task. It does **not** localize a cortical region, read open-ended thoughts, or establish that a classifier is detecting internally imagined speech. The current simple model uses time-binned 4–30 Hz log bandpower and a train-only standardized class-centroid baseline. It is intentionally weak enough to serve as an auditable baseline, not marketed as a foundation model.

**Mindforge's existing SSVEP Sight/Guard decoder remains unchanged** and continues to use posterior visual electrodes. Do not connect this research module to the live `NeuralEvent v2` authority path or build a gameplay action on its exploratory score.

### Candidate groups

- `central`: sensorimotor-related montage candidates (e.g. C3, Cz, C4)
- `frontocentral`: frontal and central montage candidates
- `lateral_frontotemporal`: lateral frontotemporal sensors, if physically present
- `posterior_control`: posterior sensors as an *experimental negative comparator*, not an assumed speech source
- `all_scalp`: benchmark using all physically available electrodes

The present eight-channel Unicorn-like montage (`Fz,C3,Cz,C4,Pz,PO7,Oz,PO8`) lacks dedicated lateral frontotemporal electrodes. **This system cannot manufacture that coverage in software.** A research montage with additional frontotemporal coverage and facial/neck EMG would help distinguish speech-related scalp activity from movement artifacts. Channel labels and actual electrode locations must be verified from hardware metadata.

## Running the first offline evaluation

Record separately acquired sessions (not randomly partitioned trials) of time-aligned, annotated EEG epochs, units in microvolts. The last two unique session IDs in the supplied row order are reserved for validation and **one untouched holdout**; preceding sessions are training. The last session must represent a genuinely new acquisition session, ideally a different day. Each partition requires at least three trials per class. Counterbalance trial prompts, timing, eye behavior, and instructions across classes.

Input an `.npz` file containing:

```python
np.savez_compressed("sessions.npz",
    eeg_uv=epochs,                 # float, (trial, channel, sample), microvolts
    labels=np.asarray(labels),     # class labels, e.g. "wall" / "bridge"
    session_ids=np.asarray(ids),   # actual independently acquired session IDs
    channel_names=np.asarray(names),
    sample_rate_hz=np.asarray(250.0))
```

From the Mindforge repository root:

```bash
python -m pip install -e '.[test]'
python tools/mindforge_spatial_select.py sessions.npz --task imagined_speech \
  --output experiments/reports/imagined-speech-spatial-v1.json
python -m pytest -q tests/test_adaptive_spatial.py
```

The output stores electrode names, chosen group, candidate validation accuracy, final holdout accuracy, class labels, a basic high-frequency confound proxy, and warnings. It excludes raw EEG. The baseline uses balanced accuracy, not class-majority accuracy. Model selection uses **training+validation only**; the holdout is never used to choose a group. Do not repeatedly adjust groups or thresholds after examining holdout scores. Use a new holdout session if you modify the system.

## Artifact and validity controls

The 35–60 Hz predictive proxy is a crude warning, not a validated EMG detector. Its output gates research pilot eligibility but cannot prove the absence of jaw, tongue, facial, eye, head, or motion confounds. The next study must add synchronized facial EMG and ideally gaze/IMU, compare actual imagined speech with mouthing, overt speech, silent reading, and idle conditions, include randomized cues, and use a prerecorded/blinded held-out protocol.

A successful synthetic test only establishes correct software behavior. A positive held-out EEG score does not demonstrate imagined-speech decoding, and the `passes_offline_screen` field is **not** an instruction to switch on automatic gameplay. Human research requires consent, secure local data handling, session-level evaluation, explicit abstention and stop conditions.

## Follow-on platform integration

1. **Passive Living World**: create a separate non-authoritative `NeuralFeatureFrame` stream containing smoothed spectral estimates, signal quality, and provenance; never interpret an alpha/beta ratio as a universal emotional state. Map normalized features to a bounded color palette, wind, particle density, or generative audio parameter. Provide opt-out and photosensitivity controls.
2. **Active abilities**: maintain Mindforge's SSVEP `NeuralEvent v2` authority, including session/calibration identity, dwell, TTL, and abstention. Imagined-speech classes remain research-only until independent human tests meet task-specific false activation, latency, and calibration standards.
3. **World building**: use an explicit `BuildIntent` preview/confirm interface, validating snapping, collision, resource cost, undo, and persistent IDs before any committed geometry changes. EEG selects among constrained choices. Procedural generation fills in deterministic details; it does not guess arbitrary mental images.
4. **Game mods**: expose read-only passive features and explicit gated commands via per-game capability adapters, never through unsandboxed neural input or direct raw EEG ingestion in game processes.

All three systems can share the common acquisition, provenance, recording, and study framework while retaining distinct authority boundaries.
