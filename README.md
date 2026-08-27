# MINDFORGE: The First Guardian

### A BCI action game where your hands fight and your visual attention commands a living soul companion.

> **Target:** BR41N.IO Designers' Hackathon at IEEE SMC 2026  
> **Category:** Your Gaming Project / BCI Gaming  
> **Event:** October 4–5, 2026  
> **Engine target:** Unity 2022.3 LTS  
> **Primary BCI:** g.tec Unicorn Hybrid Black  
> **Primary neural paradigm:** two-target SSVEP / visual evoked potential selection

## The pitch

**Mindforge** is built around one rule:

> **Hands own precision. The brain owns transformation.**

The player controls a fast action character conventionally: movement, aiming, dashing, shooting, cleaving, parrying, and projectile manipulation remain physical/controller skills. A living **Soul Wisp** adds a second strategic axis. During combat it splits into two frequency-coded aura targets positioned in the action-gaze corridor between the Guardian and the enemy.

- **Blue / Neural Sight / 10 Hz:** temporarily amplifies offensive capability.
- **Green / Neural Guard / 12 Hz:** temporarily accelerates recovery.

The player allocates visual attention between these moving targets while the fight continues. EEG does not replace the controller. It changes what the player's physical combat can become.

The system does **not** claim to read abstract thoughts such as "damage" or "heal." It decodes the narrower and testable question:

> **Which temporally coded visual target is producing the stronger steady-state visual evoked response in posterior EEG?**

The game assigns fantasy meaning to that target.

---

# Neural Counterplay

Mindforge's combat ecosystem is designed around converting danger into opportunity.

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

The core manual verbs are:

- **Pulse Shot** — mobile ranged pressure;
- **Rift Cleave** — short-range poise damage and knockback;
- **Phase Dash** — high-speed repositioning and near-miss Flux harvesting;
- **Counter Pulse** — 180 ms projectile reflection window;
- **Gravity Bloom** — consume full Flux to capture hostile projectiles and fire them back.

Sight and Guard remain the **only two BCI target classes**.

If both independently timed buffs genuinely overlap, Mindforge establishes **Concord**. Concord then remains available for a generous **4.5 s grace window**, letting the player return their eyes to the battlefield and execute a physical sequence rather than asking a slow BCI to perform frame-critical timing.

Full Flux + Concord + Gravity Bloom becomes **Twin Eclipse**.

```text
Guard accepted
      ↓
Sight accepted while Guard remains
      ↓
CONCORD established
      ↓
eyes return to fight
      ↓
dash / counter / build Flux
      ↓
Gravity Bloom
      ↓
TWIN ECLIPSE
```

---

# Defensive neural authority

Mindforge treats uncertainty as loss of authority, not an invitation to guess.

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

The quality layer conservatively detects obvious engineering failure signatures such as saturation, disconnected channels, common-mode transients, extreme temporal derivatives, and broad high-frequency contamination. These are engineering suspicion flags, not physiological diagnoses.

Suspicious or ambiguous evidence yields:

```text
ABSTAIN
```

No guessed brain button is emitted.

---

# Source-independent neural pipeline

```text
EEG source
   ↓
Python acquisition / quality / FBCCA / dwell
   ↓
NeuralEvent v1
   ↓
UDP 127.0.0.1:19742
   ↓
Unity
```

Unity never receives raw EEG.

Every judge-facing run identifies its provenance as:

```text
SIMULATION
REPLAY
LIVE
```

---

# Thread-safe, bounded Unity neural transport

The UDP socket runs on a dedicated background thread.

A heavy render stall must not turn delayed neural packets into a command avalanche, so Unity now:

- stores arrivals in a bounded concurrent queue;
- timestamps receipt using a Unity-process `Stopwatch` clock;
- discards old non-critical arrivals;
- limits how much backlog is drained per frame;
- separates newest **evidence** from gameplay **authority**;
- applies at most one ordinary authority state per frame;
- preserves `PARTICIPANT_STOP` as the dominant control event.

Python `monotonic_ns` remains useful for source provenance/order, but Unity does not subtract it from its own clock because independent process monotonic clocks do not share an epoch.

See [`docs/NEURAL_EVENT_TRANSPORT.md`](docs/NEURAL_EVENT_TRANSPORT.md).

---

# Two clocks: combat crunch without corrupting SSVEP

Combat uses a 120 Hz fixed simulation target while the visual stimulus uses real/unscaled time.

Initial impact hierarchy:

```text
light impact       20 ms
Counter Pulse      20 ms
Rift Cleave        55 ms
Signal Break       80 ms
Twin Eclipse      120 ms
```

`HitStopController` owns one extendable realtime freeze window. Nested impacts therefore cannot recapture an already-zero time scale and accidentally leave the game nearly frozen.

The VEP phase clock continues through combat freezes.

---

# Visual hierarchy is neuro-engineering

The exact Sight blue and Guard green are reserved for the two neural targets and their immediate acceptance feedback.

```text
BCI Sight       blue
BCI Guard       green
hostile normal  crimson / magenta
hostile heavy   orange-red
Guardian fire   ivory
reflected fire  violet
Concord payoff  magenta-white / violet
```

Shape reinforces category:

- neural targets: smooth, spherical, soft-edged;
- hostile projectiles: angular, needle, shard, diamond;
- Echo nodes: fractured polygonal forms.

Priority:

```text
lethal telegraph
 > BCI target core
 > Guardian / immediate state
 > ability geometry
 > impact decoration
 > ambience
```

---

# Coded VEP core vs diegetic feedback shell

Each aura is two render layers:

```text
Aura Root
├── coded VEP core
└── non-coded feedback shell / tether / particles
```

The coded core owns only declared frequency/luminance behavior and explicit visual rest. It does **not** react to classifier score, margin, quality, combat damage, Flux, camera shake, or hit-stop.

The feedback shell may communicate signal state with slow/non-periodic scale changes, particle density, desaturation, tether coherence, irregular artifact/offline jitter, and subtle audio.

This avoids feeding the decoder's result back into the amplitude of the stimulus that produced the EEG evidence.

---

# Haptic policy

Continuous rumble while FBCCA evidence is accumulating is deliberately excluded from P0 because controller vibration may add movement/EMG contamination during measurement.

Short haptic echoes occur **after** accepted Sight, accepted Guard, or Concord.

---

# The Fractured Signal encounter

## Phase I — Warm-up

Predictable aimed fans and radial patterns teach movement, counters, and aura refresh cadence with strong hostile-colored telegraphs.

## Phase II — Attention split

Fractured Echo nodes orbit the boss and add secondary pressure. Destroying one rewards Flux, creating a reason to reroute physical attention while the Wisp remains near the central gaze corridor.

## Phase III — Controlled overload

Crossfire and heavy attacks intensify, increasing the value of near misses, Counter Pulse, Gravity Bloom, and pre-established Concord. Harder does not mean unreadable: every attack family retains explicit telegraph language.

## Signal Break — catharsis and neural rest

Poise collapse creates roughly 2.6 s of relief:

```text
boss attacks pause
boss remains vulnerable
VEP modulation holds steady luminance
real VEP phase continues underneath
ambient scene dims
combat audio can low-pass
physical punish window opens
```

Signal Break is combat reward, tension-release rhythm, and visual-fatigue management at the same time.

---

# Presentation

The Unity presentation layer includes hooks for directional Rift Cleave / Counter camera displacement, FOV compression during Gravity Bloom capture, FOV snap on release, environment-only dimming for major payoffs, Signal Break low-pass/bass-pulse sensory rest, and 120 ms Twin Eclipse impact contrast.

Environment dimming is opt-in. The coded VEP materials intentionally ignore the presentation dim global.

---

# Make the invisible visible

`NeuralEvidenceHud` shows judges Sight score, Guard score, winner margin, quality, accepted/abstained state, simulation/replay/live provenance, UDP queue depth, stale packet drops, and backpressure drops.

The HUD follows the newest evidence stream while gameplay follows bounded authority, so a judge can see what the decoder is currently observing even after a render stall.

---

# neurOS: the Phantom Unicorn laboratory

Mindforge uses neurOS as simulation, perturbation, replay and qualification infrastructure rather than as a frame-by-frame game dependency.

The Phantom source can model deterministic Unicorn-like EEG with colored background activity, endogenous alpha, target-frequency posterior SSVEPs, weak responders, blinks, jaw/controller/movement contamination, channel degradation, saturation, dropout, LSL jitter, dropped chunks, source silence, and recovery.

```text
neurOS Phantom EEG
        ↓
LSL UnicornMock
        ↓
Mindforge Python FBCCA
        ↓
NeuralEvent
        ↓
Unity Neural Counterplay
```

Synthetic success is not human physiological evidence. It exists to falsify assumptions before real sessions.

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

python tools/run_lsl_decoder.py \
  --stream-name UnicornMock \
  --source-mode simulation
```

---

# Qualification ladder

```text
Q0  Unity 2022.3 imports + compiles + scene references work
 ↓
Q1  controller-only full encounter
 ↓
Q2  Phantom Unicorn full LSL → Python → UDP → Unity route
 ↓
Q3  forced render/network fault rehearsal
 ↓
Q4  physical display timing
 ↓
Q5  real Unicorn acquisition
 ↓
Q6  stationary Sight vs Guard
 ↓
Q7  moving Sight vs Guard
 ↓
Q8  target selection while player moves
 ↓
Q9  light combat
 ↓
Q10 full Fractured Signal encounter
```

See [`docs/UNITY_SCENE_WIRING.md`](docs/UNITY_SCENE_WIRING.md) for the concrete scene/prefab wiring checklist.

## Not claimed yet

We do **not yet claim** an observed successful Unity Editor/Player compile of the complete new scene, verified serialized production scene/prefab wiring, measured physical 10/12 Hz luminance timing, verified physical Unicorn metadata/units on the competition machine, human SSVEP performance, human full-combat BCI performance, or final production art/audio.

Those are the remaining evidence gates.

---

# North star

Mindforge should not be remembered as a game controlled badly by EEG.

It should demonstrate a different possibility:

> **A fast physical action game can remain responsive and expressive while neural attention controls a slower strategic layer that ordinary input does not replicate.**

The hands fight the enemy.

The Soul Wisp turns visual attention into power.

And the game is deliberately engineered to know when the BCI should say **nothing at all**.
