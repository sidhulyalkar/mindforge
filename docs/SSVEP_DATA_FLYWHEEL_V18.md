# Mindforge V0.18 — SSVEP data flywheel

## Product decision

Mindforge should become a game that is pleasant to play **and** an instrument that produces scientifically interpretable neural-control sessions.

Those goals reinforce each other when the retinal interface is controlled. They conflict when game spectacle is allowed to silently change the stimulus or when telemetry is treated as gameplay authority.

V0.18 therefore separates three layers:

1. **gameplay/presentation** — combat, camera, encounter framing and fantasy art;
2. **retinal experiment surface** — the two bounded coded cores plus a static local contrast field;
3. **observer evidence** — immutable facts describing what was actually rendered and what the game actually did.

Raw EEG remains outside Unity.

## Recording contract

A future real-participant capture should preserve these canonical source streams before any feature engineering:

- raw EEG samples with device timestamps and channel metadata;
- Unity game markers (`mindforge.game_marker.v1`);
- rendered SSVEP observations (`mindforge.ssvep_observation.v1`);
- gaze/eye events when available;
- display/photodiode evidence when available;
- external session metadata: pseudonymous participant, device, display, software revision and protocol.

The source streams are canonical evidence. Filtered EEG, spectrograms, CCA features, learned embeddings and training tensors are **derived artifacts** and must be reproducible from them.

`tools/record_ssvep_session.py` records the two non-EEG Unity observer lanes now. It intentionally does not invent EEG or intention labels during controller-only simulation.

## Identity and joins

Use separate identifiers for separate facts:

- `participant_id`: external pseudonymous research identity. Never a real name or email.
- `session_id`: one Unity game process/session.
- `trial_id`: one instructed experimental trial when the protocol supplies it.
- `stimulus_epoch`: one player-armed gameplay resonance window.
- timestamp: synchronized continuous alignment inside an epoch.

For gameplay, the minimum leakage-safe grouping unit is `(participant_id, session_id, stimulus_epoch)`.

Never randomly split overlapping windows from the same epoch across train and validation/test sets.

Calibration currently reports `stimulus_epoch=-1`; calibration samples must not enter train/test splitting until they have a real calibration/trial identifier from the experiment controller.

## What Unity records during an SSVEP interval

The V0.18 observation stream samples the rendered context at 20 Hz and records:

- Sight/Guard frequency and shared phase-start frames;
- coded-active state;
- actual viewport position of both cores;
- actual rendered angular diameter of both cores;
- actual angular separation between cores;
- core visibility;
- fixed camera FOV/aspect and screen resolution;
- camera translational and angular speed;
- local focus-backdrop state;
- target identity/type/distance/screen position;
- target-lock provenance, including encounter-assisted locks;
- expected and observed display refresh and timing-health state;
- Unity session, frame, realtime and stimulus epoch.

This matters because the requested design geometry is not enough. Training data should preserve what was actually presented after camera, transform, rendering and resolution effects.

## Graphics contract for neural windows

The game may be visually rich outside an SSVEP interval. Inside the interval, physiology-facing visuals obey a stricter contract:

- camera FOV remains fixed;
- camera orbit and target-driven yaw do not change during the evidence interval;
- coded cores remain camera-relative and angularly defined;
- only the coded cores carry periodic luminance modulation;
- a static, non-emissive circular backing field reduces local-background contrast variance;
- target indicators and presentation effects must not become competing periodic signals;
- decoder confidence must never amplitude-modulate the coded stimulus;
- if motion/visibility/timing leaves the qualified envelope, the decoder should abstain rather than relabel the interval as valid.

The current baseline remains 3° coded diameter, 10° center separation and 10/12 Hz at a 120 Hz software refresh contract. These are experiment parameters, not permanent game lore.

## Encounter framing contract

Bosses and selected high-information enemies may receive **conventional encounter-assisted target lock** so the player does not begin a critical fight facing away from its subject.

V0.18 automatically considers:

- Fractured Signal bosses;
- Signal Wardens;
- Chrome Penitents;
- Null Sentries.

Hollows and Shardcasters remain manual targets by default. Player `T` input remains authoritative, and an explicit manual unlock suppresses automatic reacquisition for a grace interval.

Encounter targeting is frozen throughout calibration/resonance. It consumes no EEG or gaze evidence and cannot resolve a neural selection.

## Public-data ladder before hardware scale-up

Use public data to falsify weak designs before collecting expensive private sessions:

1. Guttmann-Flury multimodal SSVEP: simultaneous EEG + eye tracking, including the current 10/12 Hz pair. Quantify peripheral leakage and Unicorn-like montage performance.
2. overt/covert/no-command SSVEP data: quantify covert-attention loss and false activations while stimuli are present but no command is intended.
3. EEGEyeNet: train nuisance probes that estimate eye position/movement from EEG and test whether proposed neural features collapse when ocular information is controlled.
4. additional SSVEP datasets: frequency robustness, fatigue, spatial overlap and subject variability.

Public datasets should be adapted into the same conceptual trial schema without erasing their original labels/provenance.

## Storage recommendation

For real EEG collection:

- preserve EEG and channel/device metadata in a BIDS/MNE-BIDS-compatible raw derivative structure where practical;
- preserve Unity observations and markers as append-only JSONL during acquisition;
- convert validated tabular streams to Parquet for cohort-scale analytics;
- store model-ready arrays/tensors as versioned derivatives with source hashes and transformation manifests;
- never commit large participant data into the game repository.

A model artifact should be traceable back to participant/session/trial/epoch source identifiers plus dataset and code revisions.

## Label hierarchy

Not all labels have equal scientific authority.

**Strongest initial labels**

- instructed calibration target with counterbalanced left/right placement;
- experimental overt/covert target labels from public datasets;
- explicitly marked no-command/idle trials.

**Useful but weaker gameplay labels**

- accepted Sight/Guard decoder result;
- player subsequent behavior/outcome;
- gaze-congruent target where gaze is independently measured.

An accepted decoder command is not automatically ground-truth intention. Editor keyboard simulation is synthetic and must always be excluded from physiological model training.

## Split hierarchy

Evaluation should become stricter as the dataset grows:

1. group by trial/epoch inside a session;
2. hold out whole sessions;
3. hold out participants (`GroupKFold`/LOSO style);
4. hold out device/display configurations for transfer testing;
5. maintain a frozen external public-dataset test set when license/protocol permits.

Never report random-window accuracy as evidence of subject-generalizable BCI performance.

## Model ladder

Do not jump directly to a large neural network because more parameters look more advanced.

The recommended ladder is:

1. current FBCCA baseline;
2. participant/frequency/geometry-normalized FBCCA;
3. subject templates and TRCA/eTRCA;
4. compact learned EEG models (EEGNet/temporal CNN/Braindecode-style architectures) once cohort size supports them;
5. multimodal nuisance-conditioned models using gaze/render context;
6. larger pretrained or self-supervised EEG encoders only when the dataset scale and held-out transfer benchmark justify them.

Every learned model competes against the simple baseline on accepted accuracy, false activations/minute, coverage, latency, calibration burden and cross-participant transfer.

## Training sample construction

A future dataset builder should materialize samples only when source evidence permits it. At minimum, retain:

```text
participant/session/trial/stimulus_epoch
EEG window + exact timestamps
channel montage + quality/artifact masks
candidate frequencies + stimulus phase
rendered core geometry
camera motion
screen/display timing state
target/encounter context
gaze context when available
label source + label confidence
selection/abstain outcome
source dataset + source revision
```

Geometry/context are covariates and quality controls, not shortcuts that are allowed to reveal the target label by construction.

## Promotion criteria

Before scaling participant collection, require evidence that:

- the 8-channel/Unicorn-like montage retains usable discrimination;
- accepted command accuracy is high enough for play with conservative abstention;
- no-command false activations are acceptably low;
- useful decisions can occur inside the game latency budget;
- the model retains information after controlling gaze/ocular nuisance when that scientific claim matters;
- actual rendered geometry stays inside the intended envelope during gameplay;
- target/camera assistance improves readability without changing neural evidence mid-window.

Only then is a larger private Mindforge dataset worth collecting.

## Immediate V0.18 test

Run `PLAY LATEST (BCI Simulation)` and verify:

1. the boss becomes locked only as the player approaches the encounter, not from the opening route;
2. selected priority enemies can auto-lock when the player has no lock;
3. pressing `T` to unlock produces a visible grace period with no immediate relock;
4. no automatic target change occurs while the Wisp is priming/listening;
5. the blue/green coded cores receive dark static backing discs only during calibration/resonance;
6. core position/diameter/separation remain stable while listening;
7. `tools/record_ssvep_session.py` records game markers and SSVEP observations without affecting play;
8. editor simulation remains visibly/provenance-labelled synthetic evidence.
