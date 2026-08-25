# Dual Aura SSVEP Qualification Protocol

## Purpose

Qualify the Dual Aura mechanic as an observed physical BCI system before BR41N.IO 2026. This protocol is for engineering/usability evidence, not clinical inference.

## Primary questions

1. Can participants reliably distinguish the moving 10 Hz Sight target from the moving 12 Hz Guard target using the Unicorn Hybrid Black?
2. How quickly can a participant switch targets while simultaneously operating ordinary game controls?
3. How often does the system abstain?
4. How often does it make an obvious false switch?
5. Does orb motion materially degrade classification versus stationary targets?
6. Does the mechanic remain enjoyable when real decision latency is used?

## Conditions

Run each participant through:

1. stationary Sight;
2. stationary Guard;
3. randomized stationary two-target selection;
4. randomized moving two-target selection;
5. moving selection + WASD/controller movement;
6. moving selection + full combat.

This progression lets us identify where performance degrades.

## Initial session structure

### Signal setup

- verify all channels finite;
- inspect posterior channels;
- wait for signals to stabilize;
- prefer wet/gel recording if movement destabilizes dry contacts;
- record exact display refresh configuration.

### Calibration

- 8–12 prompted trials per aura;
- ~2 s attention periods initially;
- inter-trial rest;
- randomized target order;
- collect stimulus timestamps and raw EEG only with explicit consent.

### Online validation

At least 20 prompted switches with moving auras.

Record truth target, decoder selection, score margin, quality, time to accepted selection, abstentions, artifacts, orb position, and player movement state.

## Required metrics

### Neural

- per-target accuracy;
- balanced accuracy;
- false-switch rate;
- abstention rate;
- accepted-decision precision;
- decision-time median and p95;
- score distributions;
- calibration threshold;
- usable posterior channels.

### Game

- damage dealt per minute;
- health recovered per minute;
- aura uptime;
- overlap time;
- switches per minute;
- boss completion;
- damage taken while visually attending to an aura.

### Human factors

Ask whether the player understood each aura, whether the game reacted when expected, whether switching felt strategic or distracting, whether modulation was uncomfortable, and whether they would choose the BCI layer over Controller-Only.

## Acceptance gates before competition

### Gate A — individual usability

A participant should not enter the public live-BCI demo path unless their own calibration and short online validation exceed the session's predeclared reliability threshold.

### Gate B — multi-user robustness

The mechanic should work on multiple independent adult participants rather than only the developer.

### Gate C — moving-target parity

Moving-target performance must be close enough to stationary performance that the orbit remains justified. If motion causes a large reliability loss, reduce orbit speed/radius before changing the decoder.

### Gate D — combat compatibility

False switches during full combat must remain rare. Prefer additional abstention over aggressive switching.

### Gate E — physical timing

Measure actual display modulation timing with photodiode/high-speed capture on the intended demo display. Do not infer stimulus fidelity from Unity timestamps alone.

## Frequency/codebook optimization

10 Hz and 12 Hz are defaults. If performance is weak, test a small predeclared codebook rather than endlessly tuning on one participant.

Evaluate SSVEP amplitude at Oz/PO7/PO8, harmonic collisions, display refresh fidelity, comfort, separation score, and moving-target performance.

## Motion stress test

Compare at minimum:

- stationary;
- ~0.10 Hz orbit;
- ~0.15 Hz orbit;
- ~0.20 Hz orbit.

Keep target size and luminance modulation constant. If performance falls with motion, animation is the first variable to simplify.

## Demo truth policy

A competition build must visibly identify whether it is running:

- LIVE BCI;
- RECORDED EEG REPLAY;
- DERIVED-EVENT REPLAY;
- SIMULATION.

No path may visually masquerade as another.
