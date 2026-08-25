# MINDFORGE: The First Guardian

### A BCI action game where your hands fight, your visual attention reallocates power, and enemy attacks can become your weapons.

> **Target:** BR41N.IO Designers' Hackathon at IEEE SMC 2026  
> **Event:** October 4–5, 2026  
> **Category:** Your Gaming Project / BCI Gaming  
> **Engine target:** Unity 2022.3 LTS  
> **Primary hardware:** g.tec Unicorn Hybrid Black  
> **Primary paradigm:** two-target SSVEP / visual evoked potential selection

---

## The pitch

**Mindforge** is built around a simple rule:

> **The hands own precision. The brain owns strategic transformation.**

A magical companion called the **Soul Wisp** floats beside the Guardian. In combat it bifurcates into two frequency-coded visual targets positioned in the gaze corridor between the player and the active threat.

- **Blue / Neural Sight / 10 Hz** temporarily amplifies offense.
- **Green / Neural Guard / 12 Hz** temporarily enables recovery.

The player continues moving, aiming, dashing, attacking and parrying with ordinary controls while choosing where to allocate visual attention.

Mindforge does **not** claim to decode abstract “damage intent,” “healing intent,” emotion, or concentration. It asks a narrower BCI question:

> Which of two temporally coded visual targets is producing the stronger steady-state visual evoked response in posterior EEG?

The game assigns the fantasy meaning to the decoded target.

---

# Neural Counterplay

Mindforge is no longer a shooter with an EEG button attached. Its combat system is designed around **conversion**.

```text
THREAT
  ↓
near miss / parry / capture
  ↓
RESOURCE
  ↓
physical opening
  ↓
neural state changes its value
```

A hostile projectile can:

1. hit the player;
2. be grazed during a high-speed dash to generate **Flux**;
3. be reflected with a **Counter Pulse**;
4. be captured inside **Gravity Bloom**;
5. be fired back as part of **Twin Eclipse**.

The neural layer creates another conversion:

```text
VISUAL ATTENTION
        ↓
posterior VEP evidence
        ↓
Sight / Guard
        ↓
new combat opportunity
```

The interesting game lives where those two systems intersect.

---

# The Soul Wisp

## Exploration

The unsplit Wisp behaves like a floating extension of the Guardian: spring-follow movement, gentle drift, particles and subtle connection-state feedback.

## Combat

When a threat is engaged, the Wisp splits into two camera-facing auras.

Rather than orbiting a detached HUD corner, the pair occupies an **action-centered gaze corridor** between Guardian and enemy, biased toward the threat. This reduces unnecessary eye travel while preserving the fiction that the Wisp is binding the enemy.

### Sight

Initial game candidate:

```text
10 Hz visual code
~3.6 s primary buff
~1.58× outgoing damage
```

Sight never attacks for the player. It makes physical execution more valuable.

### Guard

Initial game candidate:

```text
12 Hz visual code
~3.6 s primary buff
regenerative recovery
```

Guard never dodges or parries automatically.

### Concord

Sight and Guard are the **only neural target classes**.

If their independently timed buffs genuinely overlap, Mindforge creates an emergent gameplay state called **Concord**.

Concord currently remains available for a **4.5 s grace window** after true overlap. This is deliberate: the BCI establishes a strategic state, then the player can return their eyes to the battlefield and execute with their hands.

```text
Guard acquired
      ↓
attend Sight
      ↓
true overlap
      ↓
CONCORD
      ↓
eyes return to combat
      ↓
Phase Dash / Flux / Gravity Bloom
      ↓
TWIN ECLIPSE
```

Concord is not a third classifier output.

---

# Combat abilities

## Pulse Shot

Fast mobile pressure. Sight improves its offensive payoff.

## Rift Cleave

A committed close-range strike with heavy poise damage, knockback and impact feedback. It is especially valuable during Signal Break.

## Phase Dash

Momentum-driven high-speed movement. Skillfully passing close to hostile projectiles generates Flux.

## Counter Pulse

A short precision reflection window, currently around **180 ms**. Successful counters change projectile ownership, damage boss poise and generate Flux.

The neural system never owns this timing.

## Gravity Bloom

At full Flux, the Guardian creates a temporary field that captures nearby hostile projectiles and then releases them toward the enemy.

## Twin Eclipse

When Gravity Bloom is executed during Concord, the encounter reaches its highest-value combined neural/physical state: the boss's own projectile topology becomes the player's offensive burst.

---

# The Fractured Signal

The competition boss is designed around a repeating cognitive rhythm rather than permanent maximal pressure.

### Phase I — Pressure

Clear projectile grammar. Learn the relationship between Sight, Guard and physical combat.

### Phase II — Attrition

More crossfire and resource pressure. Guard decisions begin to matter.

### Phase III — Interference

The player must decide when it is safe to visually attend a Wisp target while movement, parry and projectile-management demands continue.

### Signal Break — visual rest + punish

Boss poise collapse creates an approximately **2.6 s vulnerability window**.

During this state:

- the boss stops attacking;
- the player receives a physical punish opportunity;
- periodic Wisp modulation is held at a steady luminance;
- the VEP phase clock continues in real time;
- modulation resumes phase-consistently afterward.

Signal Break is therefore both a combat reward and an intentional visual-cortex rest period.

---

# Two clocks, two responsibilities

One of Mindforge's central engineering ideas is that combat feel must not corrupt visual-stimulus timing.

```text
COMBAT                              VISUAL BCI
120 Hz fixed simulation             unscaled real-time clock
movement                             10 Hz Sight phase
collision                            12 Hz Guard phase
poise                                stimulus modulation
hit-stop allowed                     never paused by hit-stop
```

A successful parry or heavy Cleave may temporarily freeze scaled game simulation for impact.

The visual target clock does not freeze.

This lets Mindforge retain action-game impact without intentionally changing its SSVEP code every time the player lands a satisfying hit.

---

# What happens neurologically?

The initial visual codes are:

| Aura | Frequency | Gameplay mapping |
|---|---:|---|
| Sight | 10 Hz | offense |
| Guard | 12 Hz | recovery |

The current pipeline uses **filter-bank canonical correlation analysis (FBCCA)**.

```text
visual target
     ↓
8-channel EEG @ 250 Hz
     ↓
quality / artifact authority gate
     ↓
posterior decoder subset
Pz / PO7 / Oz / PO8
     ↓
filter-bank processing
     ↓
CCA against 10 / 12 Hz + harmonics
     ↓
Sight score / Guard score
     ↓
absolute score + margin
     ↓
multi-window dwell
     ↓
AURA_SELECTED or ABSTAIN
```

The full montage remains useful for deciding whether a window is trustworthy, while the default classifier focuses on posterior channels most relevant to the visual paradigm.

Current engineering defaults:

```text
sample rate          250 Hz
analysis window      1.25 s
hop target           ~0.25 s
Sight                10 Hz
Guard                12 Hz
harmonics            3
filter bank          6–35 Hz, 14–35 Hz
decode channels      Pz / PO7 / Oz / PO8
dwell                2 accepted windows
refresh              2.25 s
refractory           0.35 s
```

These are experiment candidates, not claims of measured human performance.

---

# Uncertainty has one authority rule

If the signal is ambiguous or suspicious:

```text
ABSTAIN
```

No wrong “brain button.”

No guessed intent.

An existing macro-state may continue according to its normal timer, but the uncertain EEG window cannot invent a new one.

The authority gate currently checks for obvious failure modes including:

- non-finite data;
- flat/disconnected channels;
- saturation;
- extreme variance;
- large common-mode transients;
- extreme derivatives;
- broad high-frequency central-channel contamination.

Names such as `EMG_SUSPECTED` are engineering flags, not physiological diagnoses.

The thresholds were hardened using synthetic stress cases and **must be retuned/validated on physical Unicorn data**.

---

# Make the invisible visible

A live BCI demonstration should not look like “a person stared at something and then magic happened.”

Mindforge's derived event envelope can carry:

```json
{
  "schema": "mindforge.neural_event.v1",
  "event": "ABSTAIN",
  "target": null,
  "sight_score": 0.41,
  "guard_score": 0.37,
  "margin": 0.04,
  "quality": 0.91,
  "source_mode": "simulation"
}
```

The Unity `NeuralEvidenceHud` is designed to show judges:

- Sight evidence;
- Guard evidence;
- score margin;
- signal quality;
- accepted target or abstention reason;
- run provenance: `SIMULATION`, `LIVE`, or `REPLAY`.

The audience should see evidence move **before** the game accepts a neural state.

---

# neurOS + Phantom Unicorn

Mindforge now uses **neurOS** as a neuro-simulation and experiment layer.

The reusable simulator lives in neurOS rather than being buried inside the game.

neurOS PR **#39** adds:

- a protocol-grade `SyntheticEEGGenerator`;
- a normal neurOS `SyntheticEEGDriver`;
- Unicorn-like 8-channel / 250 Hz configuration;
- colored background activity;
- endogenous alpha;
- posterior SSVEP + harmonic injection;
- weak/strong responder control;
- per-channel contact control;
- blink, jaw, controller, motion, saturation and dropout stressors;
- deterministic seeds;
- LSL publication as `UnicornMock`;
- transport loss, delivery jitter and deliberate stream silence.

This simulator is deliberately **not a physiological digital twin**.

Its job is to break software assumptions before a participant does.

## Canonical source swap

```text
PHANTOM
neurOS synthetic EEG
       ↓
LSL UnicornMock
       ↓
Mindforge decoder
       ↓
NeuralEvent
       ↓
Unity

PHYSICAL
Unicorn Suite LSL
       ↓
SAME Mindforge decoder
       ↓
SAME NeuralEvent
       ↓
SAME Unity game
```

Raw EEG does not enter Unity in either path.

---

# Running the Phantom lab

## Fast deterministic stress matrix

```bash
python tools/run_phantom_lab.py \
  --windows 32 \
  --json phantom-report.json
```

This evaluates strong/weak responses, alpha collision, artifact cases and posterior-channel loss.

## Combat-cadence sweep

```bash
python tools/run_phantom_cadence.py \
  --calibration-gain 1.0 \
  --combat-gains 1.0,0.8,0.65 \
  --switch-seconds 3.25 \
  --buff-seconds 3.6,4.5,5.25 \
  --grace-seconds 3.0,4.5,6.0 \
  --json cadence.json
```

This intentionally separates **calibration SSVEP strength** from **combat SSVEP strength**.

That lets us ask what happens if moving targets, saccades, controller tension or visual competition attenuate the response after a beautiful stationary calibration.

## End-to-end LSL simulation

In neurOS:

```bash
python examples/mindforge_phantom_unicorn.py
```

In Mindforge:

```bash
python tools/run_lsl_decoder.py \
  --stream-name UnicornMock \
  --source-mode simulation
```

Unity listens for derived events on UDP port `19742`.

The same decoder tool is intended to target the verified physical Unicorn LSL source with `--source-mode live`.

---

# Display timing

A number written in a Unity script is not proof that a monitor emitted that frequency.

`DisplayTimingMonitor` provides a software-side guard for:

- observed frame cadence;
- long-frame/drop fraction;
- expected refresh-rate health.

That is still insufficient for competition qualification.

The intended display must be physically measured with a **photodiode or equivalent high-speed timing method**, including while the full combat scene is under load and across hit-stop/rest transitions.

---

# Calibration ritual

Calibration is part of the fiction: **the Forge learns how to hear the player**.

1. verify channel readiness;
2. attend Sight during labeled trials;
3. attend Guard during labeled trials;
4. randomized alternation;
5. fit session-specific score/margin thresholds;
6. validate stationary targets;
7. validate moving targets;
8. validate while operating normal controls;
9. qualify or fall back transparently.

A participant who cannot achieve a reliable session does not get a fake `LIVE BCI` label.

---

# Physical qualification ladder

```text
Q0  emitted display timing
 ↓
Q1  physical Unicorn acquisition + units/channel identity
 ↓
Q2  stationary Sight / Guard
 ↓
Q3  moving gaze-corridor targets
 ↓
Q4  targets + controller movement
 ↓
Q5  full Neural Counterplay combat
 ↓
Q6  multiple independent participants
 ↓
competition candidate
```

For each stage measure at least:

- calibration duration;
- accepted-decision precision;
- false-switch rate;
- abstention rate;
- median/p95 selection time;
- per-target performance;
- signal-loss events;
- movement/artifact sensitivity;
- participant comfort;
- free-response understanding of what the BCI actually did.

---

# Repository map

```text
mindforge/
├── README.md
├── docs/
│   ├── COMBAT_ECOSYSTEM.md
│   ├── ART_AND_FEEL.md
│   ├── DUAL_AURA_VEP_DESIGN.md
│   ├── PHANTOM_UNICORN_LAB.md
│   ├── BCI_TIMING_AND_CADENCE.md
│   ├── EXPERIMENT_PROTOCOL.md
│   ├── SCIENTIFIC_REFERENCES.md
│   └── HACKATHON_REIMPLEMENTATION_PLAN.md
├── neuro/mindforge_neuro/
│   ├── acquisition.py
│   ├── calibration.py
│   ├── config.py
│   ├── events.py
│   ├── quality.py
│   ├── runtime.py
│   └── ssvep.py
├── tools/
│   ├── run_lsl_decoder.py
│   ├── run_phantom_lab.py
│   ├── run_phantom_cadence.py
│   └── synthetic_bci_demo.py
├── tests/
├── web_demo/
└── unity/Assets/Mindforge/
    ├── Combat/
    ├── NeuralBridge/
    └── SoulWisp/
```

---

# Playable browser prototype

The browser build exists for rapid **game-feel** testing, not physiological stimulus qualification.

```text
WASD     move
mouse    aim
Space    Pulse Shot
F        Rift Cleave
Shift    Phase Dash
R        Counter Pulse
X        Gravity Bloom
Q        simulated Sight evidence
E        simulated Guard evidence
```

The competition implementation target remains Unity.

---

# Current evidence status

## Established by software tests / synthetic stress

- decoder distinguishes synthetic 10/12 Hz targets;
- posterior-channel decode path works;
- ambiguous decisions can abstain;
- saturation/transient/high-frequency stress loses authority;
- dwell/refractory event behavior works;
- continuous evidence fields propagate through `NeuralEvent`;
- source provenance is explicit;
- sliding-window behavior is deterministic;
- the neurOS Phantom source passes neurOS CI and driver-contract checks;
- browser combat source passes automated syntax checks.

## Not yet established

- observed human SSVEP accuracy;
- observed human decision time;
- actual moving-target human reliability;
- actual controller-motion contamination distribution;
- physical display-code fidelity;
- physical Unicorn end-to-end performance;
- multi-user robustness;
- final Unity art/audio quality.

Those are the next gates, not footnotes.

---

# Privacy and scientific boundaries

1. Raw EEG remains local by default.
2. Unity receives derived events only.
3. Raw recording requires explicit consent.
4. Simulation, replay and live modes are visibly distinct.
5. No medical, psychological or personality inference.
6. `PARTICIPANT_STOP` dominates gameplay authority.
7. Controller-Only remains a valid fallback.

Mindforge is a research and entertainment project, not a medical device.

---

# What winning looks like

A judge should be able to watch one uninterrupted causal sequence:

```text
player fights physically
        ↓
Sight / Guard evidence bars move
        ↓
decoder accepts one target
        ↓
Wisp visibly transfers power
        ↓
player creates Concord
        ↓
eyes return to the fight
        ↓
near-miss dash / counter generates Flux
        ↓
Gravity Bloom captures a hostile barrage
        ↓
Twin Eclipse returns it
        ↓
boss poise collapses
        ↓
Signal Break suppresses VEP modulation
        ↓
player physically punishes the opening
```

Then the same screen can show whether that run was `LIVE`, `SIMULATION`, or `REPLAY` and why each neural decision was accepted or rejected.

That is the project:

> **a fast action game whose physical skill remains immediate, while a slower and probabilistic BCI creates a distinct strategic axis that the game is explicitly paced around.**

---

## Key docs

- `docs/COMBAT_ECOSYSTEM.md` — Neural Counterplay combat design
- `docs/DUAL_AURA_VEP_DESIGN.md` — visual BCI mechanic
- `docs/PHANTOM_UNICORN_LAB.md` — neurOS simulation architecture
- `docs/BCI_TIMING_AND_CADENCE.md` — clock separation and timing sweeps
- `docs/EXPERIMENT_PROTOCOL.md` — physical qualification
- `docs/ART_AND_FEEL.md` — visual/readability direction
- `docs/SCIENTIFIC_REFERENCES.md` — scientific/hardware sources

**Your hands wield the Guardian. Your attention guides the Wisp. The game respects the difference.**
