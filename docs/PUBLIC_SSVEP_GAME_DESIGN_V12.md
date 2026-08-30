# Mindforge V0.12 — Public-data SSVEP game-design qualification

## Decision

Do **not** buy eye-tracking hardware to answer a question that public data can already falsify.

Mindforge should first qualify the orb mechanic against public EEG/eye-tracking datasets, then use hardware only for the final device/display/player transfer test.

The preferred game architecture to test is no longer an always-listening pair of continuously flashing world-space orbs. It is a **player-armed, short, two-choice resonance window**:

1. ordinary movement/combat remains conventional;
2. the player holds/presses a neutral *Channel Wisp* input that means only “I want to make a neural choice now”;
3. Sight and Guard coded targets enter controlled stimulus geometry for a bounded interval;
4. EEG chooses Sight or Guard;
5. the decoder may stop early when evidence is sufficiently strong;
6. ambiguous evidence returns `ABSTAIN` and spends nothing;
7. releasing/cancelling the channel immediately closes the window;
8. a refractory/rest interval prevents rapid repeated flicker.

The conventional arming input **must never encode which neural command is desired**. It solves the asynchronous zero-class problem without turning the BCI into a button-controlled menu.

## Why this is the best first design

An always-on SSVEP control loop asks the decoder to solve two hard problems simultaneously:

- *is the player currently intending any neural action?*
- *if so, which tagged target is intended?*

The first is the zero-class problem and is especially damaging in an action game because a false activation has an immediate gameplay cost. A short player-armed decision window converts the first question into explicit game context while leaving the actual Sight/Guard choice neural.

It also reduces peripheral flicker exposure, visual fatigue, accidental attention capture and the amount of time that world/camera motion can distort retinal geometry.

## Public evidence ladder

### P-SSVEP-1 — Guttmann-Flury 2025 multimodal SSVEP

Primary question: **how much non-target frequency evidence survives while the participant fixates another simultaneously flickering target?**

Publication: `10.1038/s41597-025-04861-9`
Data: `10.7303/syn64005218`

Facts from the original publication:

- 31 participants;
- simultaneous EEG, Tobii eye tracking and high-speed eye video;
- four checkerboards in monitor quadrants;
- nominal frequencies 10, 13, 12 and 11 Hz by quadrant;
- participants are cued to fixate one target while all tagged targets are visible.

The original publication/raw annotations are authoritative. A 2026 re-host currently exposes conflicting frequency metadata, so automated dataset ingestion must validate event labels rather than trust catalog text.

#### Mindforge analyses

1. Restrict to the 10 Hz and 12 Hz classes to mirror the current Sight/Guard baseline.
2. Compute current FBCCA scores for **both** targets on every window.
3. Re-run with:
   - full posterior montage;
   - Pz/PO7/Oz/PO8;
   - Unicorn-like Fz/C3/Cz/C4/Pz/PO7/Oz/PO8 where channel mapping permits.
4. Calculate:
   - forced-choice accuracy;
   - accepted accuracy after abstention;
   - target/non-target score ratio;
   - non-target leakage while gaze is on the correct target;
   - performance as a function of measured gaze eccentricity;
   - subject-wise variance;
   - 0.5/0.75/1.0/1.25/1.5 s window curves.
5. Repeat with subject-normalized target scores so a naturally strong 10 Hz response cannot dominate simply because it is 10 Hz.

This dataset can tell us whether the current 10/12 pair is plausible and how large the peripheral-response problem is. It cannot prove covert intention because gaze and instructed target are intentionally aligned.

### P-SSVEP-2 — İşcan 2026 overt/covert SSVEP

Publication: `10.1371/journal.pone.0345793`
Data: `10.5281/zenodo.19081765`

- 20 participants;
- 16 posterior/parietal channels;
- four simultaneously presented circles;
- 4.6, 6.43, 8.03 and 10.7 Hz;
- each experimental task lasts 30 seconds;
- Tasks 2–5: central fixation plus covert attention to top/bottom/right/left respectively;
- Tasks 6–9: overt gaze plus attention to the corresponding target;
- Task 1: spontaneous activity with no stimuli;
- Task 10: spontaneous/no-command activity while the stimuli remain present.

Task 10 is particularly valuable for Mindforge because it approximates the dangerous production state: **coded visual stimulation is physically present but the user intends no command**. It provides a direct public-data stress test for idle false activations instead of inferring zero-class behavior from forced-choice trials.

This is our cheapest way to test whether a *gaze-independent* Mindforge mode deserves continued investment and whether an always-listening mode is safe enough to consider at all.

#### Mindforge analyses

- compare overt vs covert target classification using identical decoder families;
- use left/right targets as the closest analogue to the two-orb game geometry;
- quantify the loss in SNR/accuracy/latency from overt to covert attention;
- slide non-overlapping candidate decision windows through Task 10 and measure false activations per minute;
- use Task 1 to distinguish stimulus-driven idle evidence from ordinary spontaneous EEG;
- test FBCCA vs template/TRCA-style subject-specific decoding where task structure permits;
- test whether adding alpha lateralization/topographic features improves covert decoding;
- report every participant rather than hiding non-responders behind a cohort mean.

If covert performance is weak or highly participant-dependent, the production game should not require gaze-independent attention. That is not a scientific failure; it is a product-design result.

### P-SSVEP-3 — retinal eccentricity evidence

Li et al. 2021: `10.3389/fnins.2021.746146`

The study reports covert tasks across approximately 0.75°–13.9° retinal eccentricity plus overt and no-attention conditions. Use it as a design prior for the geometry sweep and, if raw data are obtained, as a direct validation set.

Mindforge should log and reason in **degrees of visual angle**, not Unity units.

### P-SSVEP-4 — EEGEyeNet ocular nuisance benchmark

EEGEyeNet contains simultaneous EEG/eye tracking from 356 participants. It does not contain Mindforge SSVEP trials, but it is valuable for asking a different question:

> How much eye position and eye movement can be inferred from the same EEG channels we plan to call “neural control”?

Run gaze-prediction probes on full EEG and a Unicorn-like montage. Then remove/attenuate ocular-predictive components and measure how much SSVEP performance changes on the SSVEP datasets.

A large collapse after removing ocular information is a warning that apparent control may be partially eye-movement decoding.

### P-SSVEP-5 — fatigue/frequency-band evidence

Han et al. 2024: `10.1109/TNSRE.2024.3380635`, public data `10.5281/zenodo.10507229`.

Use this to compare frequency bands and time-on-task degradation before declaring 10/12 Hz permanent. Frequency selection is participant- and display-dependent.

## Decoder ladder

### D0 — current baseline

Current Mindforge FBCCA is retained as the reference implementation. Do not discard a simple baseline before proving the replacement.

### D1 — participant-normalized FBCCA

For each frequency learn unattended/target distributions per participant. Compare normalized evidence rather than raw correlations.

Conceptually:

```text
E_k = standardized(target score k | subject, retinal geometry)
      - standardized(non-target score k | matched nuisance state)
```

The exact normalization must be fitted without test-subject leakage.

### D2 — subject templates / TRCA

Compare subject-specific template CCA and TRCA/eTRCA where enough calibration trials exist. These models are candidates for faster windows, not automatic replacements.

### D3 — multimodal covert-attention features

Only if public covert data support the idea, evaluate:

- SSVEP fundamental/harmonics;
- posterior topography;
- PO7/Oz/PO8 lateral balance;
- alpha lateralization;
- optional event-related P3/N2pc features when the stimulus protocol contains suitable events.

### D4 — dynamic stopping

Do not force every neural decision to last 1.25 s.

Evaluate incrementally longer windows and stop only when the calibrated risk of an incorrect command is low enough. Easy trials should finish earlier; weak trials should collect more evidence or abstain.

## Gameplay risk objective

Optimize for the game, not merely academic ITR.

Suggested ordering of costs:

```text
wrong neural command  >>  idle false activation  >>  latency  >  abstention
```

The repository implementation uses this principle in `mindforge_neuro.gaze_confound.gameplay_loss`.

A player will tolerate “signal unclear, try again.” They will quickly stop trusting a mechanic that burns an ability they did not choose.

## Stimulus design

### 1. Separate fantasy art from coded stimulus authority

The Wisp, aura, enemy and particles may move freely as presentation.

The frequency-coded component should obey a stricter contract:

- stable angular size;
- stable left/right visual separation;
- known visibility/occlusion;
- measured luminance timing;
- no confidence-driven amplitude modulation;
- no camera-dependent shrinking below the qualified operating envelope.

The coded core can be visually embedded inside the diegetic orb without inheriting arbitrary world-space geometry.

### 2. Start with controlled screen-relative geometry

Qualification sweep:

- coded diameter: 2°, 3°, 4°;
- center separation: 6°, 10°, 14°;
- eccentricity from fixation: 0°, 3°, 6°, 10° where the paradigm allows it.

Only map the winning region back into 3D presentation after the classifier is characterized.

### 3. Treat 10/12 Hz as a baseline, not canon

10 and 12 Hz are useful because they are already implemented and are present in the Guttmann-Flury public experiment. They are **not** presumed universally optimal.

Avoid frequency pairs with problematic harmonic overlap. Rank candidate pairs per participant, then require actual display timing qualification.

### 4. Evaluate friendlier visual encodings

Low-frequency high-contrast luminance flicker is strong but intrusive. Candidate follow-up stimuli include:

- ON/OFF grid/checkerboard modulation at moderated contrast;
- sinusoidal rather than harsh square-wave luminance modulation;
- textured/Gabor-like coded regions as an experimental comfort candidate;
- higher-frequency/flicker-reduced modes if performance survives.

A visually beautiful orb that is physiologically unreliable is not useful, and a perfect laboratory checkerboard that makes the game unpleasant is not useful either. Optimize both axes.

## Recommended gameplay loop

### Normal combat

Sight/Guard orbs are present as non-coded fantasy elements. No continuous flicker authority is active.

### Channel Wisp

Player intentionally arms a neural choice. Suggested first prototype:

- input begins a maximum ~1.5 s resonance window;
- two coded cores stabilize into validated left/right geometry;
- combat may slow modestly if playtesting shows this improves readability, but should not fully pause by default;
- EEG begins evidence accumulation;
- dynamic stopping accepts only when risk gates pass;
- otherwise the window ends in `ABSTAIN`;
- selection applies the existing Sight/Guard strategic transformation;
- refractory rest follows.

### Why the arming input is allowed

It communicates **when** the user wants to use the BCI, not **what** they want. The neural signal still supplies the command identity.

This is analogous to opening a spell wheel without using the controller to pick the spell.

## Promotion gates

### Gate A — public-data feasibility

Across held-out subjects and a Unicorn-like montage where possible:

- accepted command accuracy >= 90%;
- useful command coverage >= 55%;
- quantify performance at <=1.5 s, not only long laboratory windows;
- no-attention false activation low enough to support the proposed runtime state machine;
- report every subject, including weak/non-responder subjects.

These are product engineering gates, not claims of universal physiological thresholds.

### Gate B — gaze-confound understanding

On multimodal data:

- report gaze-only prediction;
- report EEG-only prediction;
- report EEG performance stratified by gaze/eccentricity;
- report trials where gaze and target evidence disagree;
- do not claim “intention beyond gaze” unless the experiment actually dissociates those variables.

### Gate C — covert mode

Promote a gaze-independent/covert mode only if held-out performance and latency remain gameplay-usable. Otherwise make overt visual attention the honest interaction contract.

### Gate D — device transfer

Only after A–C justify continuation:

- acquire/borrow EEG hardware;
- photodiode-qualify the actual display;
- validate Unicorn channel metadata/units;
- run stationary calibration;
- run camera motion;
- run light combat;
- run full encounter;
- compare public-data predicted operating envelope against the real player.

Pupil hardware is optional unless coarse gaze measurement proves insufficient for the final scientific question.

## What would make us redesign the mechanic

Redesign rather than rationalize if:

- the Unicorn-like montage loses most of the public-dataset discrimination;
- accepted accuracy requires windows that feel too slow for the game;
- no-attention false activations remain high without an explicit arm state;
- overt performance is good but covert performance is too weak for the desired fantasy;
- performance depends overwhelmingly on gaze/ocular information when the product claim requires neural attention beyond gaze;
- visual comfort requires stimulus changes that destroy decoding robustness.

## Current product recommendation

Until public benchmarks say otherwise:

**Build Mindforge as a conventional action game with a deliberately invoked, short SSVEP strategic-choice mechanic.**

- Hands: movement, attack, dodge, parry, interaction, *when to open the neural channel*.
- EEG: *which strategic neural transformation is selected*.
- Gaze: experimental measurement/gating only; never required command authority by default.

This is narrower than an always-reading “mind control” fantasy, but much more likely to become a BCI mechanic that is reliable enough to be fun and scientifically defensible.
