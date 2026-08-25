# Mindforge BR41N.IO 2026 Reimplementation Plan

## Frozen competition thesis

Mindforge is now centered on **one primary BCI interaction**:

> A persistent Soul Wisp splits into blue **Neural Sight** and green **Neural Guard** VEP auras around the active enemy. Visual attention selects which aura temporarily empowers the Guardian while normal controls retain movement, attacks, dodge, aim, and timing.

The primary competition decoder is **two-target SSVEP**, initially 10 Hz vs 12 Hz, with filter-bank CCA and session-specific acceptance thresholds.

P300 and motor imagery are no longer parallel P0 development tracks. They may remain research/fallback experiments only if the primary SSVEP path fails physical qualification.

---

# Why this design wins attention

It has an immediate explanation:

```text
look blue  → hit harder
look green → heal faster
hands      → still play the game
```

But the expert loop is deeper because Sight and Guard have independent timers. Players can deliberately create overlap windows while deciding whether visual attention can safely leave the combat action.

The jury can see an unbroken causal chain from target fixation to posterior VEP evidence to decoder selection to a visible aura transfer and changed combat statistics.

---

# Competition vertical slice

## 1. Wisp awakening and calibration

Target: 2–5 minutes including signal setup.

- electrode/channel quality;
- stationary Sight trials;
- stationary Guard trials;
- randomized alternation;
- moving-orb validation;
- session score/margin thresholds;
- clear LIVE BCI vs fallback state.

## 2. Physical combat tutorial

Teach movement, attack and dodge before the BCI must be managed.

## 3. Sight tutorial

One enemy. Prompt blue. On accepted selection, blue energy visibly returns from the enemy-orbiting aura to the Guardian and damage amplification begins.

## 4. Guard tutorial

Damage the player in a controlled way. Prompt green. On accepted selection, green energy returns and regeneration becomes obvious.

## 5. Unprompted combined encounter

The player chooses when to switch. This establishes that the neural layer is strategic, not a scripted tutorial trigger.

## 6. Boss: The Fractured Signal

### Phase I — Pressure
Sight is rewarding and easy to maintain.

### Phase II — Attrition
Chip damage introduces Guard decisions.

### Phase III — Interference
Combat pressure makes gaze allocation costly. Uncertain EEG abstains rather than switching incorrectly.

### Phase IV — Mastery
Optimal play refreshes both timers and exploits overlap while maintaining controller skill.

## 7. Evidence epilogue

Show the run's real paradigm, calibration result, accepted selections, abstentions, false switches if prompted ground truth exists, decision times, signal losses, and game outcome.

---

# Software architecture

```text
Unicorn Hybrid Black
        ↓
Unicorn Suite LSL / qualified acquisition
        ↓
SlidingWindowBuffer
        ↓
signal quality / artifact gate
        ↓
FBCCA 10 Hz vs 12 Hz
        ↓
score + winner margin
        ↓
dwell / refresh / refractory governor
        ↓
NeuralEvent v1
        ↓ UDP 19742
Unity
        ↓
AuraBuffController
        ↓
Sight / Guard gameplay state
```

## Hard invariants

1. Unity never receives raw EEG.
2. Controller owns frame-critical combat authority.
3. Neural target identity is not inferred from gameplay context.
4. Every accepted event contains target, quality, confidence/control score, model ID, sequence, and monotonic timestamp.
5. `ABSTAIN` is expected behavior.
6. Duplicate/out-of-order neural events are ignored.
7. Stream loss cannot freeze combat.
8. Simulation/replay/live modes are visibly distinct.
9. Physical stimulus timing is measured, not assumed.

---

# Implementation state

## Implemented now

- two-target FBCCA core;
- session calibration thresholds;
- basic signal-quality gate;
- dwell/refractory selection runtime;
- NeuralEvent schema;
- UDP Unity receiver;
- Soul Wisp follow/orbit behavior;
- 10/12 Hz sampled-sine aura renderer;
- independent Sight/Guard buff timers;
- playable web game-feel prototype;
- synthetic end-to-end UDP fixture;
- optional Unicorn LSL acquisition adapter;
- deterministic tests.

## Not yet observed

- physical Unicorn stream integration on the competition machine;
- emitted 10/12 Hz display timing;
- human calibration accuracy;
- moving-target human accuracy;
- full-combat false-switch rate;
- measured selection latency;
- comfort across participants;
- polished Unity scene/art/audio.

These are the P0 evidence gaps.

---

# Physical qualification ladder

## Q0 — display

- lock intended refresh configuration;
- disable problematic variable refresh behavior if needed;
- photodiode/high-speed measurement of both target codes;
- verify dropped-frame behavior.

## Q1 — acquisition

- connect Unicorn LSL stream;
- verify 8 EEG channels and ~250 Hz nominal rate;
- verify units/channel ordering;
- validate stale-stream and reconnect behavior.

## Q2 — stationary SSVEP

- 8–12 trials per target;
- fit session thresholds;
- measure accepted precision, abstention, false switches, decision time.

## Q3 — moving auras

Compare stationary, ~0.10 Hz, ~0.15 Hz, and ~0.20 Hz orbit conditions.

If motion hurts badly, simplify animation before adding decoder complexity.

## Q4 — movement

Repeat while player uses movement controls.

Stress blink, jaw/face EMG, head motion, and dry-vs-wet contact stability.

## Q5 — full combat

Measure whether selection quality survives the actual game.

This is the real release gate.

---

# Tuning rules

## Neural parameters

Do not tune gameplay and classifier thresholds on the same outcome metric.

Classifier tuning optimizes accepted-decision reliability and abstention. Gameplay tuning optimizes fun and strategic tradeoffs using the resulting measured decision time.

## Buff duration

Current 3.4 s durations are placeholders. After real decision-time measurements, set durations so:

- a novice can experience a clear payoff;
- a skilled player can create some overlap;
- maintaining both requires intentional attention switching;
- the optimal strategy is not to stare permanently at one aura.

## Orbit

Current angular speed: 0.92 rad/s (~0.146 Hz). Slow it before compromising decoding reliability.

---

# Human playtest campaign

Aim for multiple independent adult participants before competition.

For each participant record:

- setup/calibration time;
- session qualification result;
- stationary accuracy;
- moving accuracy;
- movement accuracy;
- full-combat accepted precision;
- abstention and false-switch rates;
- median/p95 decision time;
- visual comfort;
- tutorial comprehension;
- boss completion;
- free response: “What did the BCI do?”

The free response is a product metric. If the player cannot explain the causal mechanic, the design is not communicating clearly enough.

---

# Competition choreography

## Opening

“The Guardian is controlled by my hands. The creature beside me is my Soul Wisp. When combat starts, it becomes two visual BCI targets.”

## First causal moment

Focus blue. Show decoder evidence. Sight activates. Immediately demonstrate increased damage.

## Second causal moment

Take damage. Focus green. Guard activates. Show recovery.

## Mastery moment

Switch quickly enough to overlap both states while dodging boss attacks.

## Science screen

Show exact paradigm and observed session metrics.

## Reliability moment

If presentation time permits, deliberately look away or generate an ambiguous interval and show `ABSTAIN` rather than a false switch.

---

# Schedule

## Aug 24–28

- Dual Aura architecture and prototype ✅
- decoder/runtime/tests ✅
- physical acquisition route scaffold ✅
- Unity component core ✅

## Aug 29–Sep 4

- create complete Unity scene/prefabs;
- finish controller-first boss loop;
- wire Wisp targeting and combat stats;
- implement calibration scene/state machine;
- add session telemetry/replay.

## Sep 5–11

- obtain/connect physical Unicorn;
- qualify LSL stream;
- measure monitor timing;
- run first stationary and moving sessions.

## Sep 12–18

- multi-user SSVEP qualification;
- select final codebook/orbit speed;
- tune thresholds;
- tune Sight/Guard duration from measured selection timing.

## Sep 19–25

- full closed-loop combat sessions;
- external player tutorial testing;
- art/VFX/audio production;
- reliability and fallback campaign.

## Sep 26–30

- freeze mechanics;
- polish onboarding and boss readability;
- finalize evidence summary;
- clean-machine installation rehearsals.

## Oct 1–3

No new systems.

Repeated full demo rehearsals, hardware checklist, backup replay build, and final competition package.

---

# Definition of done

Mindforge is competition-ready when:

1. real Unicorn EEG selects the two moving auras;
2. multiple participants can calibrate successfully;
3. false switches stay low enough that players trust the system;
4. uncertainty causes abstention;
5. movement does not destroy decoder usability;
6. controller combat is satisfying on its own;
7. Sight/Guard switching creates a real mastery curve;
8. the Soul Wisp is visually memorable;
9. every scientific claim shown to judges has observed evidence;
10. the full experience survives repeated live rehearsals.

## North-star rule

If a new feature competes with better physical EEG reliability, better game feel, clearer onboarding, stronger Soul Wisp presentation, or greater demo robustness, **the new feature loses**.
