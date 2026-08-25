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

# Soul Wisp and the action-gaze corridor

Outside combat, the Wisp floats beside the Guardian like a small balloon-like extension of the player's soul.

During combat it moves into a camera-facing corridor between player and threat. Sight and Guard orbit slowly around that anchor rather than living in distant HUD corners.

Initial movement parameters are deliberately conservative and remain experimental:

```text
anchor toward target      ~0.78
orbit angular speed       ~0.78 rad/s
small camera-facing orbit
```

The animation must obey the neuroscience. If human testing shows that a wider/faster orbit reduces SSVEP reliability, the orbit gets smaller or slower.

---

# What happens neurologically

Initial target codebook:

| Target | Frequency | Gameplay identity |
|---|---:|---|
| Blue | 10 Hz | Neural Sight |
| Green | 12 Hz | Neural Guard |

Current engineering configuration:

```text
sampling rate          250 Hz
analysis window        1.25 s
harmonics              3
filter-bank CCA        FBCCA
posterior decoder      Pz / PO7 / Oz / PO8
quality authority      full 8-channel montage
stable dwell           2 accepted windows
refresh                2.25 s
short refractory       0.35 s
```

The default Unicorn-like montage is:

```text
Fz C3 Cz C4 Pz PO7 Oz PO8
```

For each EEG window, the pipeline:

```text
EEG
 ↓
quality / artifact authority
 ↓
posterior channel selection
 ↓
filter bank
 ↓
CCA evidence @ 10 Hz and 12 Hz + harmonics
 ↓
score + winner margin
 ↓
dwell / refractory governor
 ↓
AURA_SELECTED or ABSTAIN
```

If evidence is ambiguous, contaminated, stale, or otherwise untrustworthy, the correct command is:

```text
ABSTAIN
```

No guessed brain button is emitted.

---

# Defensive neural authority

Mindforge treats uncertainty as loss of authority, not an invitation to guess.

The current quality layer conservatively detects obvious engineering failure signatures such as:

- saturated channels;
- disconnected / flat channels;
- extreme variance;
- common-mode transients;
- extreme temporal derivatives;
- broad high-frequency contamination consistent with possible EMG.

These are **engineering suspicion flags**, not medical or physiological diagnoses.

Suspicious evidence yields `ABSTAIN`.

---

# Derived-event boundary

Unity never receives raw EEG.

```text
EEG source
   ↓
Python acquisition / FBCCA / quality / dwell
   ↓
NeuralEvent v1
   ↓
UDP 127.0.0.1:19742
   ↓
Unity
```

A derived event can include:

```json
{
  "schema": "mindforge.neural_event.v1",
  "seq": 147,
  "event": "AURA_SELECTED",
  "target": "sight",
  "confidence": 0.91,
  "quality": 0.94,
  "paradigm": "ssvep_fbcca",
  "source_mode": "live",
  "sight_score": 0.73,
  "guard_score": 0.28,
  "margin": 0.45
}
```

Every judge-facing run explicitly identifies its source as:

```text
SIMULATION
REPLAY
LIVE
```

A simulation run can never silently masquerade as physical EEG evidence.

---

# Thread-safe, bounded Unity neural transport

Unity's UDP receiver runs network I/O on a dedicated background thread and pushes datagrams into a bounded concurrent queue.

A render stall or heavy Twin Eclipse effect must **not** turn delayed UDP packets into a burst of conflicting neural state changes.

The receiver therefore separates:

### Evidence stream

Newest decoder evidence for:

- spectator HUD;
- non-coded aura feedback;
- telemetry.

### Gameplay authority stream

Bounded state changes for gameplay/governance.

At most one ordinary neural authority event is applied per Unity frame, while `PARTICIPANT_STOP` remains dominant.

Old non-critical packets are discarded using a receive timestamp measured inside the Unity process.

Python `monotonic_ns` is retained for provenance/order, but Unity does **not** subtract it from its own monotonic clock because independent process monotonic clocks do not share an epoch.

See [`docs/NEURAL_EVENT_TRANSPORT.md`](docs/NEURAL_EVENT_TRANSPORT.md).

---

# Two clocks: satisfying combat without corrupting VEP timing

Combat runs around a 120 Hz fixed simulation target.

The visual stimulus uses **real/unscaled time**.

That distinction allows heavy impact feedback without changing the declared visual frequency.

Initial hit-stop hierarchy:

```text
light impact       20 ms
Counter Pulse      20 ms
Rift Cleave        55 ms
Signal Break       80 ms
Twin Eclipse      120 ms
```

`HitStopController` uses one extendable real-time freeze window, so nested impacts cannot accidentally capture an already-zero `Time.timeScale` and leave the game nearly frozen.

The VEP phase clock continues through every combat freeze.

---

# Visual hierarchy is part of the neuroscience

The exact Sight blue and Guard green are reserved for the neural targets and their immediate acceptance feedback.

Combat uses a separate language:

```text
BCI Sight       blue
BCI Guard       green
hostile normal  crimson / magenta
hostile heavy   orange-red
Guardian fire   ivory
reflected fire  violet
Concord payoff  magenta-white fusion
```

Shape reinforces the distinction:

- neural targets: smooth / spherical / soft-edged;
- hostile projectiles: angular / needle / shard / diamond;
- Echo nodes: fractured polygonal forms.

A blue glowing enemy projectile would be a visual-design bug because it competes with the Sight target.

---

# Coded core vs diegetic feedback shell

Each neural aura is intentionally split into two visual layers:

```text
Aura Root
├── coded VEP core
└── non-coded feedback shell / tether / particles
```

The **coded core** owns only the measured 10/12 Hz luminance behavior and explicit visual-rest state.

It does **not** listen to classifier confidence, margin, quality, damage, Flux, or hit-stop.

The **feedback shell** can respond to neural evidence through slow/non-periodic:

- scale;
- particle density;
- tether coherence;
- desaturation;
- irregular artifact/offline jitter;
- subtle evidence audio.

This avoids a self-referential decoder→stimulus feedback loop that could amplitude-modulate the very signal being decoded.

---

# Haptic policy

A rising controller rumble while FBCCA is accumulating evidence is deliberately **not** part of the P0 design.

Controller vibration may add hand/arm movement and EMG contamination during the neural measurement itself.

Instead, short haptic echoes occur **after**:

- accepted Sight;
- accepted Guard;
- Concord acquisition.

The player feels successful neural control without modifying the noise environment while the decoder is trying to measure it.

---

# The Fractured Signal

The boss encounter is paced around cognitive load rather than a flat fire-rate ramp.

## Phase I — Warm-up

Predictable aimed fans and radial patterns teach:

- movement;
- Phase Dash;
- Counter Pulse;
- aura refresh cadence.

Every attack has a clear hostile-colored telegraph.

## Phase II — Attention split

**Echo nodes** orbit the boss and create secondary pressure.

Destroying one rewards Flux, giving the player a reason to leave pure boss DPS while the Wisp remains near the central action corridor.

## Phase III — Controlled overload

Crossfire becomes denser and more aggressive, increasing the value of:

- near misses;
- Counter Pulse;
- Flux;
- Gravity Bloom;
- pre-established Concord.

The fight becomes physically intense without asking the BCI to become a twitch controller.

## Signal Break — catharsis and neural rest

Boss poise collapse creates approximately **2.6 seconds** of sensory relief:

```text
boss attacks stop
boss stays vulnerable
VEP modulation holds steady luminance
real VEP phase clock continues
ambient scene dims
high-frequency combat audio can low-pass
physical punish window opens
```

The rest period is simultaneously game feel, encounter rhythm, and visual-fatigue management.

---

# Presentation and "make the invisible visible"

`NeuralEvidenceHud` shows judges the system's live evidence before gameplay accepts a state:

```text
Sight score
Guard score
winner margin
quality
ABSTAIN reason
SIMULATION / REPLAY / LIVE
UDP queue depth / stale drops / backpressure drops
```

This turns a BCI demo from "someone stared at something and the game changed" into an observable causal chain.

---

# neurOS: the Phantom Unicorn laboratory

Mindforge uses **neurOS** as a development and qualification laboratory rather than as a frame-by-frame game dependency.

The reusable Phantom source lives in the corresponding neurOS work and can simulate:

- deterministic Unicorn-like 8-channel EEG;
- colored / 1/f-like background activity;
- endogenous alpha;
- posterior target-frequency SSVEP responses;
- weak and strong responders;
- blink contamination;
- jaw / controller / movement contamination;
- channel contact degradation;
- saturation;
- dropout;
- LSL jitter;
- dropped chunks;
- temporary source silence and recovery.

The intended rehearsal path is:

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

Synthetic success is **not** human BCI evidence. Its purpose is to falsify software and design assumptions before exhausting a real participant.

---

# Simulation layers

## Fast decoder stress

```bash
python tools/run_phantom_lab.py --windows 32 --json phantom-report.json
```

Useful for hostile deterministic signal scenarios.

## Cadence sweeps

```bash
python tools/run_phantom_cadence.py \
  --calibration-gain 1.0 \
  --combat-gains 1.0,0.8,0.65 \
  --switch-seconds 3.25 \
  --buff-seconds 3.6,4.5,5.25 \
  --grace-seconds 3.0,4.5,6.0 \
  --json cadence.json
```

This explicitly separates **calibration response strength** from **combat response strength** and measures switch latency, accepted events, stale selections, aura uptime, and Concord availability.

## Full transport route

```bash
python tools/run_lsl_decoder.py \
  --stream-name UnicornMock \
  --source-mode simulation
```

A verified physical LSL source can replace `UnicornMock` without changing the downstream gameplay boundary.

---

# Display timing

`DisplayTimingMonitor` watches Unity's software frame cadence and warns if the expected display rhythm is unhealthy.

That is useful, but it is **not proof of physical luminance timing**.

The competition build still requires physical measurement, ideally with a photodiode, under:

- idle stimulus;
- full boss rendering load;
- Counter Pulse;
- Signal Break transition;
- Twin Eclipse;
- post-rest resume.

A beautiful effect that corrupts the emitted target timing is a gameplay bug.

---

# Project structure

```text
mindforge/
├── README.md
├── docs/
│   ├── DUAL_AURA_VEP_DESIGN.md
│   ├── COMBAT_ECOSYSTEM.md
│   ├── ART_AND_FEEL.md
│   ├── EXPERIMENT_PROTOCOL.md
│   ├── NEURAL_EVENT_TRANSPORT.md
│   ├── UNITY_SCENE_WIRING.md
│   └── ...
├── neuro/
│   └── mindforge_neuro/
├── tools/
│   ├── run_phantom_lab.py
│   ├── run_phantom_cadence.py
│   └── run_lsl_decoder.py
├── unity/
│   └── Assets/Mindforge/
│       ├── NeuralBridge/
│       ├── SoulWisp/
│       ├── Combat/
│       └── Presentation/
├── web_demo/
└── tests/
```

---

# Qualification ladder

The software architecture is intentionally not the final evidence claim.

Promotion ladder:

```text
Q0  Unity Editor imports + compiles + scene wiring works
 ↓
Q1  Phantom Unicorn full transport loop
 ↓
Q2  physical display timing measurement
 ↓
Q3  real Unicorn acquisition
 ↓
Q4  stationary Sight vs Guard
 ↓
Q5  moving Sight vs Guard
 ↓
Q6  target selection while player moves
 ↓
Q7  light combat
 ↓
Q8  full Fractured Signal encounter
```

At every physical stage, measure:

- target truth;
- Sight/Guard scores;
- accepted selections;
- abstentions;
- false switches;
- decision timing;
- signal quality / artifact flags;
- movement/combat state;
- display state;
- connection failures.

---

# What is currently implemented

The competition branch currently includes:

- two-target SSVEP FBCCA;
- session calibration and abstention;
- posterior decoding / full-montage quality authority;
- derived-event UDP boundary;
- bounded threaded Unity event intake;
- continuous spectator evidence;
- sticky Concord;
- visual-rest Signal Break;
- reserved visual palette;
- coded-core / feedback-shell separation;
- post-decision haptics;
- directional camera/FOV/ambient presentation hooks;
- Neural Counterplay combat systems;
- cognitively paced Fractured Signal boss;
- Echo nodes and hostile telegraphs;
- Phantom-lab and cadence tooling;
- browser combat prototype;
- architecture regression tests.

Automated repository checks cover the Python decoder/runtime, browser JavaScript syntax, and source-level Unity architecture contracts.

## Not claimed yet

The project does **not yet claim**:

- an observed successful Unity Editor/Player compile of this complete new scene;
- serialized production scene/prefab wiring;
- measured physical 10/12 Hz display timing;
- verified live Unicorn metadata/units on the competition machine;
- human SSVEP performance;
- human full-combat BCI performance;
- final production art/audio.

Those are the next qualification gates.

---

# North star

Mindforge should not be remembered as a game controlled badly by EEG.

It should demonstrate a different design possibility:

> **A fast physical action game can remain responsive and skillful while neural attention controls a slower strategic layer that ordinary input does not replicate.**

The hands fight the enemy.

The Soul Wisp turns visual attention into power.

And the game is deliberately built to know when the brain-computer interface should say **nothing at all**.
