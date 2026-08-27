# Phantom Unicorn Laboratory

## Purpose

Mindforge uses **neurOS** as its adversarial neuro-simulation and experiment layer while keeping the competition runtime small and hardware-agnostic.

The lab exists to answer a harder question than “does FBCCA detect a clean sine wave?”

> Can Mindforge remain trustworthy and fun when its EEG source is weak, contaminated, changing, delayed, or temporarily unusable?

Synthetic results are never human evidence. Their value is finding failures before a live participant finds them on stage.

---

# Canonical boundary

```text
                    FAST LAB
neurOS SyntheticEEGGenerator
            │
            └──────────────→ Mindforge FBCCA / quality tests

                 CLOSED LOOP
neurOS Phantom Unicorn
            ↓
      LSL UnicornMock
            ↓
Mindforge UnicornLslSource
            ↓
   1.25 s sliding windows
            ↓
 quality / artifact authority gate
            ↓
 posterior FBCCA
 Pz / PO7 / Oz / PO8
            ↓
 score + margin + dwell
            ↓
 NeuralEvent v1
            ↓ UDP :19742
          Unity
            ↓
Sight / Guard / Concord / combat
```

When physical hardware arrives, only the leftmost source changes:

```text
Unicorn Suite LSL
       ↓
Mindforge UnicornLslSource
       ↓
SAME decoder
       ↓
SAME NeuralEvent
       ↓
SAME Unity game
```

Raw EEG does not enter Unity in either mode.

---

# Why neurOS instead of a Mindforge-only mock?

neurOS already has canonical source/driver contracts, timing semantics, LSL infrastructure, recording/replay qualification, and evidence provenance. The new `SyntheticEEGGenerator` and `SyntheticEEGDriver` are therefore useful beyond this game.

Mindforge should consume neurOS as an **experiment dependency**, not pull the full neurOS platform into the shipped game.

This prevents two bad outcomes:

1. the competition build becoming operationally dependent on a large research monorepo;
2. the Phantom simulator becoming a game-specific toy that cannot be independently tested or reused.

---

# Synthetic participant model

The neurOS Phantom source currently provides:

- 250 Hz, 8-channel Unicorn-like montage;
- stateful colored / 1/f-like background activity;
- endogenous alpha close to the SSVEP band;
- posterior target-frequency SSVEP;
- first harmonic;
- continuously tunable response strength;
- per-channel contact gain;
- blink transients;
- jaw EMG;
- controller/hand EMG;
- motion drift;
- saturation;
- channel dropout;
- deterministic seeds.

This is a controlled nuisance generator, **not a digital human brain**.

---

# First simulator finding

The initial Phantom stress pass immediately falsified the old Mindforge signal-quality assumption.

The previous gate reliably detected saturation, but several synthetic blink, jaw-EMG, controller-EMG and movement windows remained nominally “high quality” and therefore retained authority to reach FBCCA.

The revised gate adds conservative authority checks for:

- common-mode transients;
- extreme temporal derivatives;
- broad high-frequency central-channel energy;
- saturation;
- flat/disconnected channels;
- extreme variance.

Default fail-closed reasons now include:

```text
SATURATION
COMMON_MODE_TRANSIENT
FAST_TRANSIENT
EMG_SUSPECTED
TOO_FEW_CHANNELS
NONFINITE
```

These names describe engineering suspicion, not physiological diagnosis.

Thresholds are provisional until physical Unicorn sessions measure real distributions.

---

# Decoder channel policy

The full montage remains available to the quality gate, but the default SSVEP decoder uses:

```text
Pz / PO7 / Oz / PO8
```

This separates two roles:

```text
all channels      → “is this window trustworthy enough to act?”
posterior channels → “which visual target has stronger SSVEP evidence?”
```

The channel set remains configurable for physical experiments.

---

# Scenario matrix

`tools/run_phantom_lab.py` evaluates deterministic families such as:

| Scenario | What it tests | Desired behavior |
|---|---|---|
| Strong Sight | happy path | reliable Sight evidence |
| Strong Guard | happy path | reliable Guard evidence |
| Weak responder | low SNR | more abstention, few false switches |
| 10 Hz alpha / no target | endogenous-confound pressure | avoid false Sight authority |
| Blink | transient | abstain |
| Jaw EMG | broadband contamination | abstain |
| Controller EMG | game-specific motor contamination | abstain or reduce authority |
| Motion | headset/player movement | abstain |
| Oz contact loss | imperfect montage | degrade gracefully |

Future scenario sweeps should also include:

- mixed windows during a Sight→Guard gaze switch;
- response-amplitude distributions across synthetic participants;
- moving-target SSVEP attenuation;
- packet loss and chunk jitter;
- LSL disconnect/reconnect;
- duplicated/stale data;
- display marker jitter;
- variable decoder hop size;
- simultaneous artifact + weak response.

---

# BCI cadence derived from the lab

## 1. Sight and Guard are sticky macro-buffs

A neural choice should create several seconds of opportunity. It should never be used for frame-perfect parry authority.

## 2. Concord has a grace period

True overlap of Sight and Guard triggers **Concord**, but Concord remains active for an initial 4.5 s grace window even if one primary aura expires.

This lets the neural system create a strategic state and then hands execute:

```text
Guard → attend Sight → Concord acquired
                  ↓
             eyes return
             to combat
                  ↓
           Phase Dash / Flux
                  ↓
           Gravity Bloom
                  ↓
           Twin Eclipse
```

The grace period is gameplay tuning and will be adjusted against measured human switch latency.

## 3. Signal Break is neural rest

When the boss loses poise, combat creates a ~2.6 s punish phase and both VEP targets hold a steady luminance rather than continuing periodic modulation.

The stimulus phase clock still advances in real time, so the visual code resumes phase-consistently after the rest.

This makes neural fatigue part of encounter pacing:

```text
ATTENTION PHASE
      ↓
combat pressure
      ↓
poise break
      ↓
VEP REST + physical punish
      ↓
attention phase resumes
```

## 4. Aura position follows the action corridor

The two targets orbit a camera-facing anchor between Guardian and boss, biased toward the threat, instead of an arbitrary HUD corner or a wide orbit around the boss.

This is intended to reduce gaze excursion while keeping the targets diegetically attached to combat.

The exact anchor/radius/orbit speed are experimental parameters.

---

# Make the invisible visible

Every decoder window can now carry spectator evidence in the backwards-compatible `mindforge.neural_event.v1` envelope:

```json
{
  "event": "ABSTAIN",
  "has_evidence": true,
  "sight_score": 0.41,
  "guard_score": 0.37,
  "margin": 0.04,
  "quality": 0.91,
  "source_mode": "simulation"
}
```

Unity's `NeuralEvidenceHud` can render:

- live Sight score;
- live Guard score;
- winner margin;
- quality;
- accepted target or abstention reason;
- `SIMULATION`, `LIVE`, or `REPLAY` provenance.

The audience should be able to see evidence shift before the game accepts a state.

---

# Running the lab

## Fast deterministic scenarios

Install the neurOS Phantom branch into the research environment, then:

```bash
python tools/run_phantom_lab.py --windows 32 --json phantom-report.json
```

## End-to-end LSL

Terminal A, neurOS:

```bash
python examples/mindforge_phantom_unicorn.py
```

Terminal B, Mindforge:

```bash
python tools/run_lsl_decoder.py \
  --stream-name UnicornMock \
  --source-mode simulation
```

Run Unity and listen on UDP `19742`.

The same Mindforge decoder tool can later target the physical Unicorn LSL stream with `--source-mode live` and the verified unit/channel configuration.

---

# What synthetic success can and cannot establish

## It can establish

- software correctness;
- fail-closed behavior;
- deterministic decoder behavior;
- source-swap architecture;
- game behavior under long/short neural delays;
- graceful abstention;
- telemetry and spectator UI correctness;
- recovery from injected transport faults;
- whether combat pacing tolerates plausible BCI latency ranges.

## It cannot establish

- human SSVEP accuracy;
- actual participant comfort;
- real motion artifact distributions;
- real moving-target performance;
- emitted monitor frequency fidelity;
- actual decision latency;
- generalization across people.

Those remain physical qualification gates.

---

# Competition objective

By the time the real Unicorn is connected, we want the question to be almost entirely physiological:

> “How strong and reliable is this participant's visual response under this game?”

not:

> “Why did the socket disconnect, why did Unity parse the wrong schema, why did an EMG burst trigger Sight, or why does the boss require a 200 ms neural action?”

That is the role of the Phantom Unicorn lab.
