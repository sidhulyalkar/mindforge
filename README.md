# MINDFORGE: The First Guardian

### A BCI action game where your hands fight and your visual attention commands a living soul companion.

> **Target:** BR41N.IO Designers' Hackathon at IEEE SMC 2026  
> **Category:** Your Gaming Project / BCI Gaming  
> **Event:** October 4–5, 2026  
> **Engine target:** Unity 2022.3 LTS  
> **Primary BCI:** g.tec Unicorn Hybrid Black  
> **Primary neural paradigm:** two-target SSVEP / visual evoked potential selection

## The idea

**Mindforge** is built around one rule:

> **Hands own precision. The brain owns transformation.**

The player controls movement, aiming, dashing, shooting, cleaving, counters and projectile manipulation conventionally. A living **Soul Wisp** adds a slower strategic layer through two temporally coded visual targets:

- **Sight / blue / 10 Hz:** temporarily amplifies offensive capability.
- **Guard / green / 12 Hz:** temporarily accelerates recovery.

The game does not claim to read abstract thoughts such as “damage” or “heal.” It asks the narrower, testable question:

> **Which coded visual target is producing the stronger steady-state visual evoked response in posterior EEG?**

The fantasy meaning is assigned by the game.

## Neural Counterplay

```text
enemy attack
   ↓
THREAT
   ↓
near miss / counter / capture
   ↓
RESOURCE
   ↓
Flux / reflected projectile / poise pressure
   ↓
WEAPON
```

The manual combat verbs are:

- **Pulse Shot**: mobile ranged pressure;
- **Rift Cleave**: short-range poise damage and knockback;
- **Phase Dash**: repositioning and near-miss Flux harvesting;
- **Counter Pulse**: a 180 ms projectile reflection window;
- **Gravity Bloom**: consume full Flux, capture hostile projectiles, fire them back.

Sight and Guard remain the **only two BCI target classes**.

If both independently timed buffs genuinely overlap, **Concord** is established and remains available for a forgiving 4.5 s grace window. Full Flux + Concord + Gravity Bloom becomes **Twin Eclipse**.

```text
Guard accepted
      ↓
Sight accepted while Guard remains
      ↓
CONCORD
      ↓
eyes return to combat
      ↓
dash / counter / build Flux
      ↓
Gravity Bloom
      ↓
TWIN ECLIPSE
```

## Defensive neural authority

Initial engineering configuration:

```text
sampling rate          250 Hz
analysis window        1.25 s
Sight target           10 Hz
Guard target           12 Hz
harmonics              3
filter-bank CCA        FBCCA
posterior decoder      Pz / PO7 / Oz / PO8
quality authority      full 8-channel montage
stable dwell           2 accepted windows
```

The default Unicorn-like montage is:

```text
Fz C3 Cz C4 Pz PO7 Oz PO8
```

The quality layer conservatively detects obvious engineering failure signatures such as saturation, disconnected channels, common-mode transients, extreme derivatives and broad high-frequency contamination. These are engineering suspicion flags, not physiological diagnoses.

Suspicious or ambiguous evidence yields `ABSTAIN`. No guessed brain button is emitted.

## A BCI game platform, not a headset-bound Unity demo

Mindforge now treats the neural loop as two versioned contracts:

```text
                 NeuralEvent v2
Python / neurOS ───────────────► Unity

                  GameMarker v1
Python / neurOS ◄─────────────── Unity
```

`NeuralEvent` contains only derived neural evidence/authority. `GameMarker` contains only presentation/gameplay facts. **Raw EEG never crosses into Unity.**

The detailed architecture, schemas, simulation hierarchy and promotion rules live in [`docs/BCI_GAME_PLATFORM.md`](docs/BCI_GAME_PLATFORM.md).

### NeuralEvent v2

The v2 contract adds explicit provenance and freshness fields:

```text
session_id
calibration_id
source_sample_start
source_sample_end
decoder_time_ns
authority_ttl_ms
```

Unity accepts both v1 and v2. A v2 selection whose local receive age exceeds `authority_ttl_ms` may still appear as evidence, but it cannot change gameplay.

Python and Unity monotonic clocks are never subtracted from each other because independent process monotonic clocks do not share an epoch.

### GameMarker v1

Unity publishes semantically meaningful events including:

```text
PHASE_DASH
PULSE_SHOT
RIFT_CLEAVE
COUNTER_PULSE
COUNTER_REFLECT
GRAVITY_BLOOM_CHARGE / RELEASE
TWIN_ECLIPSE_CHARGE / RELEASE
NEURAL_BUFF_APPLIED
CONCORD_ESTABLISHED
BOSS_PHASE
SIGNAL_BREAK
FLUX_CHANGED
BCI_DEGRADED / BCI_RECOVERED
VICTORY / DEFEAT
```

Markers include session identity, Unity realtime, game time, rendered frame and fixed tick. The transport is non-authoritative: losing a recorder is allowed to lose evidence, never to alter the fight.

## Develop the game without wearing electrodes

The same Unity authority path can be driven by increasingly realistic sources:

| Level | Source | Purpose |
|---|---|---|
| S0 | `manual` | mechanic and UI development |
| S1 | `simulated_decision` | error/authority/game-feel testing |
| S2 | `decision_replay` | exact gameplay reproduction |
| S3 | `eeg_replay` | production decoder on recorded EEG |
| S4 | `synthetic_eeg` | neurOS participant/sensor/fault simulation |
| S5 | `live` | physical participant + headset |

These labels are evidence boundaries. S1 is not synthetic EEG. S4 is not human evidence.

### Decision-level development

```bash
python tools/mindforge_dev.py decision \
  --script sight:3,guard:3,abstain:1,lost:1,recovered:1 \
  --hz 4 \
  --output-tape experiments/tapes/dev.jsonl
```

### Reproduce an exact authority trace

```bash
python tools/mindforge_dev.py replay experiments/tapes/dev.jsonl --speed 1.0
```

### Observe Unity's side of the loop

```bash
python tools/mindforge_dev.py marker-log \
  --output experiments/markers/unity.jsonl
```

## Two clocks: combat crunch without corrupting SSVEP

Combat uses a 120 Hz fixed simulation target while the visual stimulus uses real/unscaled time.

```text
light impact       20 ms
Counter Pulse      20 ms
Rift Cleave        55 ms
Signal Break       80 ms
Twin Eclipse      120 ms
```

`HitStopController` owns one extendable realtime freeze window. The VEP phase clock continues through combat freezes.

## Coded VEP core vs feedback shell

Each neural aura is deliberately split:

```text
Aura Root
├── coded VEP core
└── non-coded feedback shell / tether / particles
```

The coded core owns only declared frequency/luminance behavior and explicit visual rest. It does **not** react to classifier score, margin, quality, damage, Flux, camera shake or hit-stop.

The shell may communicate signal state using slower, non-periodic visual changes. This avoids feeding decoder output back into the amplitude of the stimulus that produced the EEG evidence.

## Haptic policy

Continuous rumble while evidence is accumulating is excluded. Short haptic echoes occur **after** accepted Sight, accepted Guard or Concord so controller vibration is not intentionally injected into the measurement window.

## The Fractured Signal

The competition encounter is built around readable escalation.

**Phase I: Warm-up.** Predictable aimed fans and radial patterns teach movement, counters and aura refresh cadence.

**Phase II: Attention split.** Fractured Echo nodes add pressure and Flux opportunities while the Wisp remains near the action-gaze corridor.

**Phase III: Controlled overload.** Crossfire and heavy attacks increase the value of near misses, Counter Pulse, Gravity Bloom and pre-established Concord without abandoning telegraph readability.

**Signal Break.** Poise collapse creates roughly 2.6 s of relief. Boss attacks pause, VEP modulation rests at steady luminance, the underlying phase clock continues, the arena dims and the player receives a physical punish window.

## Make the invisible visible

`NeuralEvidenceHud` shows Sight score, Guard score, winner margin, quality, accepted/abstained state, source provenance, UDP queue depth, stale-packet drops and backpressure drops.

The evidence HUD follows newest evidence while gameplay follows bounded authority. A render stall therefore cannot hide the distinction between “the decoder observed this” and “the game was allowed to act on this.”

## neurOS: the Phantom Unicorn laboratory

Mindforge uses neurOS as simulation, perturbation, replay and qualification infrastructure rather than as a frame-by-frame game dependency.

```text
neurOS synthetic participant / EEG
        ↓
LSL UnicornMock
        ↓
Mindforge quality + FBCCA + dwell
        ↓
NeuralEvent v2
        ↓
Unity Neural Counterplay
        ↓
GameMarker v1
        ↓
qualification / replay evidence
```

Phantom can model deterministic Unicorn-like EEG, endogenous alpha, target-frequency posterior SSVEPs, weak responders, blinks, jaw/controller/movement contamination, channel degradation, saturation, dropout, LSL jitter, dropped chunks, source silence and recovery.

Synthetic success exists to falsify assumptions before real sessions. It is not human physiological evidence.

Useful tools:

```bash
python tools/run_phantom_lab.py --windows 32 --json phantom-report.json

python tools/run_phantom_cadence.py \
  --calibration-gain 1.0 \
  --combat-gains 1.0,0.8,0.65 \
  --switch-seconds 3.25 \
  --buff-seconds 3.6,4.5,5.25 \
  --grace-seconds 3.0,4.5,6.0 \
  --json cadence.json

python tools/run_lsl_decoder.py --stream-name UnicornMock --source-mode simulation
```

## Qualification ladder

```text
Q0  Python contracts/tests
 ↓
Q1  Unity 2022.3 imports + compiles + scene references work
 ↓
Q2  controller-only full encounter
 ↓
Q3  simulated_decision → NeuralEvent → Unity
 ↓
Q4  decision replay reproduction
 ↓
Q5  Phantom Unicorn → production decoder → Unity
 ↓
Q6  forced render/network fault rehearsal
 ↓
Q7  measured physical display timing
 ↓
Q8  real Unicorn acquisition metadata/units
 ↓
Q9  stationary Sight vs Guard
 ↓
Q10 moving selection
 ↓
Q11 selection while player moves
 ↓
Q12 light combat
 ↓
Q13 full Fractured Signal encounter
```

See [`docs/UNITY_SCENE_WIRING.md`](docs/UNITY_SCENE_WIRING.md), [`docs/PHANTOM_UNICORN_LAB.md`](docs/PHANTOM_UNICORN_LAB.md), and [`docs/BCI_GAME_PLATFORM.md`](docs/BCI_GAME_PLATFORM.md).

## Not claimed yet

We do **not yet claim** an observed successful Unity Editor/Player compile of this new platform branch, measured physical 10/12 Hz luminance timing, verified physical Unicorn metadata/units on the competition machine, human SSVEP performance, human full-combat BCI performance, or final production art/audio.

Those remain evidence gates, not TODOs to wave away.

## North star

Mindforge should not be remembered as a game controlled badly by EEG.

It should demonstrate a different possibility:

> **A fast physical action game can remain responsive and expressive while neural attention controls a slower strategic layer that ordinary input does not replicate.**

The game should be useful to designers before they own a headset, useful to BCI engineers before the art is final, and inspectable by researchers after a session is over.

The hands fight the enemy. The Soul Wisp turns visual attention into power. The platform makes clear why the BCI was allowed to do what it did.
