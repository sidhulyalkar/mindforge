# Mindforge SSVEP Gaze-Confound Qualification

## Scientific question

Mindforge currently frequency-tags Sight and Guard with simultaneously visible SSVEP stimuli. The central scientific risk is that a frequency classifier can detect a stimulus because it is physically stimulating the retina even when it is not the player's intended target. Foveation, retinal eccentricity, covert attention, stimulus size, spatial separation, and individual frequency-response differences can all change SSVEP magnitude.

Therefore the operational question is not simply:

> Which stimulus frequency has the larger EEG score?

It is:

> After controlling for where the eyes are pointing and the retinal geometry of both stimuli, is there enough neural evidence to infer the player's intended target with an acceptable false-activation rate?

Until this is demonstrated, a raw 10 Hz versus 12 Hz winner must not be interpreted as a validated intention classifier.

## Current Mindforge risk

The present design uses:

- Sight: 10 Hz;
- Guard: 12 Hz;
- both stimuli visible simultaneously;
- 1.25 s FBCCA windows with harmonics;
- posterior channels Pz/PO7/Oz/PO8;
- raw target-score and target-margin thresholds;
- world-space stimulus placement around the combat target.

World-space offsets do not guarantee fixed retinal eccentricity, angular stimulus size, or angular separation. Camera distance, FOV, target distance, and camera motion can therefore alter the measured SSVEP even if the player's intention is unchanged.

## Three latent variables that must remain separate

### 1. Retinal stimulation
A flickering object can evoke an SSVEP while it is in peripheral vision.

### 2. Overt attention / gaze
Direct fixation generally increases the response, but this combines retinal eccentricity and attention.

### 3. Covert attention
A player can attend to a peripheral target without directly looking at it, and this can also modulate SSVEP amplitude and posterior topography.

A frequency peak alone cannot uniquely identify which of these mechanisms produced it.

## Scientific decision

### Preferred V0.12 architecture: eye tracking as measurement/gating, EEG as command evidence

Pupil gaze should initially be treated as a nuisance-variable measurement and safety gate, not as command authority.

For target k:

1. Eye tracking estimates angular eccentricity from gaze to each coded stimulus.
2. The EEG decoder estimates target-specific neural evidence.
3. A command is accepted only when gaze geometry is valid, EEG evidence exceeds a calibrated target-specific threshold, target margin is sufficient, signal quality is acceptable, and the state persists for the required dwell.
4. Gaze alone never issues Sight or Guard.
5. EEG evidence without valid gaze geometry initially abstains rather than guessing.

This preserves a meaningful BCI claim: the eye tracker tells the decoder what visual stimulation reached the retina; EEG remains necessary for the command.

## Why this experiment matters

Pupil Labs can answer whether the BCI is genuinely adding information beyond gaze.

We should compare:

- gaze-only prediction;
- EEG-only prediction;
- EEG conditioned on gaze/eccentricity;
- fused gaze + EEG;
- EEG after regressing out gaze/eccentricity effects.

If EEG adds no measurable predictive value over gaze, the orb mechanic is not yet a meaningful BCI mechanic. If EEG retains predictive value after the gaze variables are controlled, we have much stronger evidence that the system is capturing selective attention rather than merely foveation.

## Required calibration experiment

Use Pupil gaze and EEG simultaneously. For both Sight and Guard, collect randomized trials under at least these conditions.

### A. Overt target attention
- fixate Sight, attend Sight;
- fixate Guard, attend Guard.

### B. Peripheral/covert attention
- fixate a center point, covertly attend Sight;
- fixate a center point, covertly attend Guard.

### C. Gaze-attention dissociation
- fixate Sight while covertly attending Guard;
- fixate Guard while covertly attending Sight.

These trials are particularly important because they explicitly break the correlation between gaze and intended target.

### D. Ignore / idle
- fixate center and ignore both;
- free-view scene without an intention to select either command.

### E. Gameplay transfer
- natural camera movement;
- target motion;
- combat visual clutter;
- natural gaze shifts;
- no cued command labels except explicit player confirmations used only for ground truth.

## Retinal geometry must be logged

For every EEG window log:

- gaze position;
- screen position of Sight and Guard;
- angular eccentricity of each target from gaze;
- angular target diameter;
- angular separation between the two targets;
- target visibility/occlusion;
- display refresh rate and measured stimulus phase/timing;
- camera FOV;
- target distance;
- stimulus luminance state;
- EEG quality metrics;
- intended target label from the experimental cue or explicit confirmation.

Do not infer angular geometry from Unity world distance alone.

## Initial display geometry

For qualification, temporarily decouple the coded targets from enemy world-space offsets.

Use screen-stabilized or gaze-relative coded targets with controlled visual angle so the experiment can answer a clean question. Initial parameter sweep:

- target diameter: 2, 3, and 4 degrees;
- target-center separation: 6, 10, and 14 degrees;
- target eccentricity from central fixation: 0, 3, 6, and 10 degrees where practical.

The final game can later map the validated geometry back onto world-space art.

## Decoder comparison

The present FBCCA decoder is a useful baseline, not the scientific endpoint.

Compare at least:

1. raw FBCCA score winner;
2. subject-calibrated, target-normalized FBCCA;
3. TRCA or individual-template CCA;
4. a classifier using posterior spatial/topographic features in addition to frequency evidence;
5. EEG + gaze geometry fusion;
6. EEG after conditioning/regressing on gaze eccentricity.

### Target normalization

Do not compare raw 10 Hz and 12 Hz scores as though their baselines are exchangeable. Learn a per-subject, per-target distribution from unattended and attended conditions and express evidence relative to that target's own baseline.

A useful operational score is conceptually:

`neural_evidence_k = z(score_k | subject, target k, gaze geometry) - z(non_target_score | matched geometry)`

The exact model should be selected from calibration data rather than assumed.

## Features worth testing

- FBCCA fundamental + harmonics;
- subject-specific template correlation;
- TRCA spatial filters;
- PO7/Oz/PO8 topographic balance;
- contralateral posterior features for covert attention;
- alpha-band lateralization;
- optional N2pc/P3 features if the stimulus paradigm contains discrete events;
- gaze eccentricity and angular target size as covariates, never silently as labels.

## Runtime state machine

The production decoder should contain an explicit idle/abstain state.

Example acceptance logic:

1. quality gate passes;
2. stimulus timing verified;
3. gaze sample fresh;
4. gaze-target geometry within the calibrated operating envelope;
5. target neural evidence above its calibrated threshold;
6. target-versus-alternative evidence margin above threshold;
7. evidence persists across N windows;
8. otherwise ABSTAIN.

The false-activation rate is more important than maximizing forced-choice accuracy in an action game.

## Key evaluation metrics

Report separately:

- balanced accuracy;
- target-wise sensitivity/specificity;
- false activations per minute during idle gameplay;
- abstention rate;
- time to valid selection;
- calibration-versus-gameplay domain shift;
- performance by gaze eccentricity bin;
- performance by angular target separation;
- EEG-only versus gaze-only versus fused performance;
- incremental information contributed by EEG after gaze is known.

### Make-or-break gate

Do not promote the BCI mechanic based only on cued, forced-choice accuracy.

The key promotion criterion is:

> In naturalistic gameplay, EEG must add reliable target-intention information beyond measured gaze/retinal geometry while maintaining an acceptably low false-activation rate.

If this fails, keep gaze for targeting and redesign the neural mechanic around a signal that is not confounded with spatial fixation.

## Three possible outcomes

### Outcome 1: overt gaze dominates
If gaze explains almost all target classification and EEG adds little after controlling for eccentricity, treat the current orb SSVEP implementation as a gaze-confirmation interface rather than a neural intention interface.

### Outcome 2: EEG adds robust attention information
If EEG significantly improves classification after gaze geometry is controlled, retain the hybrid orb mechanic. Eye tracking can remain a safety/nuisance channel while EEG owns the command decision.

### Outcome 3: covert attention is sufficiently decodable
If dissociation trials show robust covert-target decoding, consider a gaze-independent alternate mode using fixed central fixation and peripheral targets. This is a different interaction paradigm and should not be conflated with overt gaze selection.

## Additional timing qualification

`VepAuraStimulus` currently evaluates a continuous sine from Unity realtime in `LateUpdate`. Actual display output is quantized by frame presentation and can jitter with frame pacing. Before any neurophysiological claim, measure both frequencies with the existing photodiode path and require known frame-locked phase/frequency error bounds for every supported refresh mode.

10 Hz and 12 Hz should also be treated as provisional. Individual SSVEP response varies with frequency and 10 Hz overlaps endogenous alpha activity. A subject-specific frequency calibration should compare candidate pairs that are compatible with the measured display refresh rate.

## Recommended development order

1. Add synchronized Pupil + EEG + Unity geometry logging.
2. Replace world-space qualification stimuli with fixed visual-angle stimuli.
3. Run the overt/covert/dissociation/idle calibration matrix.
4. Quantify gaze-only, EEG-only, conditioned EEG, and fused performance.
5. Select decoder and thresholds from held-out trials.
6. Re-run under natural gameplay.
7. Only then decide whether production Mindforge requires eye tracking, can support EEG-only selection, or should move BCI authority to a different neural mechanic.
