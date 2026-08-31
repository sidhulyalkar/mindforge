# Mindforge V0.14 — Low-Latency SSVEP Game Architecture

Status: design/qualification branch. This document does **not** authorize a claim that the current Unity build has already produced validated physical SSVEP stimulation or real-time neural control.

## Scientific product claim we are designing toward

Mindforge should claim only what the instrumentation can support:

> Mindforge uses intentionally triggered, frequency-tagged visual targets and posterior EEG to select between strategic combat transformations. The decoder may abstain when evidence is weak.

Do **not** claim that the system reads intention independently of gaze, that peripheral flicker is fully removed by filtering, or that a nominal Unity frequency is equivalent to a physically verified display frequency.

## Current architecture: what is already good

The current Wisp interaction already has several strong design decisions:

- neural authority is explicitly armed by the player rather than continuously active;
- normal movement, attack, dodge, parry, camera and target-lock controls retain authority;
- the coded cores are hidden outside a resonance window;
- the coded cores are camera-relative during resonance instead of inheriting world-space target distance;
- current coded geometry is 3 degrees diameter and 10 degrees center separation;
- Sight and Guard are nominally 10 Hz and 12 Hz;
- the display qualification layer requests VSync and 120 Hz and exposes a photodiode qualification path;
- the decoder uses the posterior Unicorn channels Pz/PO7/Oz/PO8 for target classification while signal-quality logic can inspect all eight channels.

The 3 degree / 10 degree geometry is a scientifically reasonable starting point because classic SSVEP stimulus-specificity work found better classification when targets subtended at least about 2 degrees and were separated by more than about 5 degrees.

## Critical current gap 1 — Unity and Python do not yet share one evidence epoch

The current Unity resonance window allows 1.25 s of Listening.

The current Python decoder configuration also uses a 1.25 s EEG window, a 0.25 s live hop, and `dwell_windows = 2`.

If the EEG buffer is correctly reset at physical stimulus onset, the second full 1.25 s window cannot exist until roughly 1.50 s after onset. Therefore the current contracts cannot simultaneously provide:

1. a scientifically clean post-stimulus EEG window,
2. two accepted dwell windows,
3. and a Unity listening window that closes at 1.25 s.

If a selection arrives earlier, it can only have used EEG that began before the coded stimulus epoch or another stale/overlapping timing assumption. Sequence gating in Unity prevents replay of an old *event*, but does not prove that the EEG samples used to generate a new event came from the current resonance window.

This is the most important V0.14 issue.

### V0.14 authority rule

The neural process must consume Unity's `NEURAL_WINDOW_LISTENING` marker and create an epoch-scoped EEG accumulator. No neural selection may be emitted for epoch N from samples preceding epoch N's physical stimulus onset.

Every selection event should carry the same `stimulus_epoch` / window identifier that opened the evidence accumulator.

## Critical current gap 2 — nominal luminance timing is not physical display timing

`VepAuraStimulus` currently computes luminance from `Time.realtimeSinceStartupAsDouble` in `LateUpdate`.

That defines a nominal software waveform, but the retina receives discrete display frames after compositor/GPU/display latency. A physically valid SSVEP claim therefore requires:

- frame-locked stimulus phase;
- VSync;
- known display refresh;
- dropped-frame detection;
- frame and epoch logging;
- and a photodiode measurement on the final display path.

At 120 Hz the current pair is especially convenient:

- 10 Hz = 12 display frames per cycle;
- 12 Hz = 10 display frames per cycle.

At 60 Hz they are also integral:

- 10 Hz = 6 frames per cycle;
- 12 Hz = 5 frames per cycle.

That makes 10/12 Hz a good **display-compatible baseline**, but not a universal participant-optimal pair.

### V0.14 stimulus rule

Stimulus phase must advance from the presented frame index and qualified refresh rate rather than from arbitrary render-loop wall time. Any dropped frame is logged as a stimulus-quality defect. Photodiode edges remain the physical authority.

## Critical current gap 3 — CPU work is not the true latency bottleneck, but we still waste work

The online problem is tiny computationally: eight channels at 250 Hz, with only four posterior channels used for two-target classification. The dominant latency is the amount of post-stimulus neural evidence required, not UDP JSON or matrix arithmetic.

However the current FBCCA path redesigns Butterworth filters on every call and applies zero-phase `sosfiltfilt` independently to every overlapping window.

V0.14 should:

- precompute filter coefficients;
- filter incoming EEG once using streaming causal filter state for the real-time path;
- keep raw/unfiltered rolling data separately for artifact checks when required;
- precompute/cached sine-cosine references for each supported evidence length;
- avoid recomputing identical overlapping preprocessing;
- benchmark decoder wall time separately from physiological decision latency.

A sub-10-ms decoder is sufficient. Chasing microseconds while waiting 500-1000 ms for cortical evidence is the wrong optimization target.

## Decoder hierarchy

### Stage A — zero/low calibration fallback

Use FBCCA as the immediate fallback because it requires no participant-specific spatial template.

Do not interpret raw 10 Hz and 12 Hz CCA magnitudes as exchangeable physiological probabilities. Calibration must learn target-specific score distributions because endogenous alpha, anatomy and electrode contact can make one nominal frequency systematically stronger.

### Stage B — participant-calibrated primary decoder

Once the player has supplied repeated labeled Wisp trials, switch the primary decoder to filter-bank ensemble TRCA (or an equivalently validated individual-template method) while retaining FBCCA as an independent fallback/diagnostic.

TRCA is attractive for Mindforge because it learns participant-specific spatial filters that maximize reproducibility of time-locked SSVEP activity and has repeatedly supported high-speed online SSVEP systems, including dry-electrode systems.

Do not make a deep network the first production decoder. Deep methods are a research lane for ultra-short windows and transfer learning, but they add model/version/calibration/generalization complexity before the timing and physiology contracts are qualified.

## Dynamic stopping instead of two-window dwell

The current two-window dwell rule should not be used inside a bounded triggered resonance window.

Use one cumulative evidence epoch with checkpoints. Initial qualification schedule:

| checkpoint | usable EEG after visual-latency guard | purpose |
| --- | ---: | --- |
| A | 0.55 s | earliest high-confidence resolution |
| B | 0.75 s | expected common resolution |
| C | 1.00 s | conservative fallback |
| D | 1.25 s | maximum evidence before abstain |

The decoder checks the same epoch at increasing lengths. It resolves as soon as target-specific calibrated evidence exceeds a high-precision threshold. Otherwise it waits for the next checkpoint.

Dynamic-stopping research has shown that adaptive evidence duration can improve SSVEP information-transfer rate relative to fixed windows. Mindforge should optimize it for **wrong-command avoidance**, not headline ITR.

## Visual response latency

Published refresh-rate SSVEP experiments report an approximately constant visual-cortical latency on the order of 128-135 ms. Mindforge should therefore not treat the first rendered stimulus frame as instant cortical evidence.

Initial design:

- 80-100 ms neutral core settle;
- coded frame 0 starts the physical stimulus epoch;
- ignore approximately the first 120-140 ms when constructing phase-sensitive participant templates;
- begin dynamic evidence checks after enough post-latency samples have accumulated.

This yields a realistic target interaction budget of roughly 0.7-0.9 s for many confident trials, while allowing difficult trials to extend toward 1.4-1.5 s before abstaining.

## Overt attention is the V0.14 game contract

Peripheral targets still evoke SSVEP. There is no general signal-processing operation that erases all peripheral retinal drive and leaves a pure intention signal.

Research consistently finds stronger and more reliable SSVEP modulation when the player directly looks at the attended target than when attention is shifted covertly while fixation remains elsewhere. Retinal eccentricity also changes the SSVEP response.

Therefore the first production interaction should explicitly teach:

> Hold Channel Wisp and look directly at the Wisp transformation you want until the Wisp resolves.

That is a valid SSVEP BCI interaction. The EEG measures frequency-tagged occipital response and decides the target; the trigger key only opens the decision interval.

Covert-attention selection remains an experimental mode that must earn promotion through separate qualification.

## Preventing an eye-movement shortcut from masquerading as SSVEP

Qualification must counterbalance physical side.

During calibration and scientific validation, Sight/Guard positions should swap left/right across trials while frequency identity remains tracked. If a model only succeeds when 'Sight means look left', it has learned an ocular/spatial shortcut rather than a frequency-specific target response.

Required ablations:

- frequency decoding with side counterbalanced;
- posterior-only channels versus all channels;
- classifier performance after removing or regressing strongly gaze/EOG-predictive features where eye data are available;
- public-data EEG-only versus EEG+gaze analyses;
- sham/no-command windows.

Production UI may later keep stable semantic positions if that improves usability, but scientific qualification must break the side-label correlation.

## Frequency policy

10/12 Hz remains the baseline pair because it is refresh-compatible at 60/120 Hz and does not have a low-order harmonic collision within the current three-harmonic reference bank.

It must not become a sacred constant.

At 120 Hz, useful integer-frame candidate frequencies include values such as 8, 10, 12, 15, 20, 24 and 30 Hz. Participant calibration should rank candidates using:

- held-out balanced accuracy;
- accepted-command precision;
- target-specific score separation;
- harmonic-collision constraints;
- signal quality;
- fatigue/comfort;
- and physically measured display fidelity.

The chosen pair must fit the decoder evidence band and the actual monitor refresh.

## Target-specific normalized evidence

The production decision should eventually use something conceptually closer to:

`evidence(target) = normalized_target_response - normalized_competitor_response`

where normalization is conditioned on participant, frequency and qualified stimulus geometry.

A practical implementation can learn per-target calibration distributions and convert each checkpoint's FBCCA/TRCA features into calibrated log-likelihood or z-score evidence. This addresses the observed problem where one participant/frequency can have a systematically higher raw response than the other.

Do not label the current monotonic `confidence` field as a posterior probability until probability calibration has actually been performed.

## Wisp combat design

### Normal combat

No coded visual stimulation. The Wisp remains a fantasy companion. All frame-critical combat remains conventional controller input.

### Channel

The player creates a tactical opening and holds Channel Wisp. The two cores appear in stable camera-relative geometry. The game does not pause.

### Resolve

The first checkpoint that crosses the participant-specific high-precision neural threshold resolves Sight or Guard. A strong trial should feel almost immediate after the Wisp visibly 'locks in'.

### Abstain

If no checkpoint reaches the precision gate, the Wisp destabilizes and returns no transformation. No resource is spent. This is a normal game outcome, not an error screen.

### Sight

Sight should create an offensive information advantage: weak-point exposure, increased poise exploitation, increased reach/readability or hidden traversal/combat information. It never attacks automatically.

### Guard

Guard should create a defensive information/timing advantage: widened manually executed counter timing, clearer dangerous telegraphs, projectile-counter assistance or recovery after a successful manually executed defense. It never auto-parries.

### Higher-order grammar

Keep the neural classifier two-class for now. Complex strategy can emerge from sequences rather than more simultaneous visual targets, e.g. Sight -> Guard = Concord. This is preferable to adding 4-8 frequencies before two-target reliability is proven.

## End-to-end latency budget

Target budget after optimization:

| stage | target |
| --- | ---: |
| neutral settle | 80-100 ms |
| visual cortical latency guard | ~130 ms |
| first useful evidence | 450-600 ms |
| FBCCA/TRCA compute | <10 ms target |
| local transport | <10 ms target |
| Unity application | next rendered frame |
| common button-to-resolution | ~0.7-0.9 s target |
| difficult-trial maximum | ~1.4-1.5 s, then abstain |

These are design targets, not measured guarantees.

## Promotion gates before we may say 'Mindforge reliably elicits and decodes SSVEP'

### Display gate

On the exact monitor/GPU/display mode used for the claim:

- VSync confirmed;
- observed refresh compatible with the configured stimulus pair;
- no systematic cycle-count error;
- photodiode verifies the intended frequencies and phase schedule;
- frame drops during resonance are below the qualification threshold;
- Unity-marker-to-photon delay is measured and stable.

### Forced-choice EEG gate

With real EEG and counterbalanced target side:

- accepted-command precision >= 95% target;
- balanced forced-choice accuracy >= 90% target;
- accepted fraction >= 70% by the maximum checkpoint target;
- median neural resolution <= 0.85 s after coded onset target;
- p95 neural resolution <= 1.25 s after coded onset target;
- zero gameplay actions from events outside the currently armed epoch;
- target-frequency bias characterized and normalized.

These are product promotion targets, not literature constants.

### Idle/abstain gate

Even though gameplay authority is triggered, test sham/no-command and natural combat windows. Wrong accepted actions are much more costly than abstentions.

### Natural-game gate

Repeat the same metrics during real movement, combat animation, camera motion, particles and enemies. Laboratory success does not automatically transfer to a moving game.

## Public-data program before hardware

Continue using public data for algorithm and confound selection:

1. Guttmann-Flury simultaneous SSVEP + eye tracking for peripheral leakage and frequency asymmetry.
2. Overt/covert/no-attention SSVEP data for the attention-mode and idle classifier.
3. EEGEyeNet-style datasets for ocular nuisance analysis.
4. Wide-frequency/fatigue datasets for candidate-frequency ranking and comfort.
5. Standard Benchmark/BETA-style SSVEP datasets for short-window FBCCA/TRCA comparisons.

Public data can qualify architecture and model choices. It cannot replace the final display + participant + headset measurement.

## V0.14 implementation order

1. Add an epoch-aware neural marker receiver and make `NEURAL_WINDOW_LISTENING` reset the evidence accumulator.
2. Carry `stimulus_epoch` through every neural event and reject mismatched epochs in Unity.
3. Replace two-window dwell with dynamic cumulative checkpoints.
4. Precompute filter banks/reference templates and add a streaming preprocessing path.
5. Add participant target-specific evidence normalization.
6. Implement filter-bank ensemble TRCA as a calibrated primary decoder with FBCCA fallback.
7. Make VEP phase frame-indexed and refresh-aware.
8. Extend display qualification to explicitly validate configured pair compatibility and edge/cycle counts.
9. Add counterbalanced-side calibration trials and sham/no-command trials.
10. Benchmark short-window performance on public datasets with trial/session-grouped splits.
11. Only then run real EEG forced-choice qualification.
12. Promote the neural authority path to production only after the display, forced-choice, abstain and natural-game gates pass.

## Bottom line

The current Mindforge concept is scientifically plausible and several interaction choices are already strong, especially triggered authority and stable retinal geometry. It is **not yet valid to state that the generated Unity setup has been proven to elicit and decode the intended SSVEP reliably in real time**.

The biggest missing piece is not a more expensive classifier. It is a single synchronized evidence epoch connecting the first physically presented coded frame to the exact EEG samples used for the eventual Unity action.

Once that is fixed, participant-calibrated TRCA + dynamic stopping is the most promising low-computation path for a fast game, while FBCCA remains the robust zero-calibration fallback.
