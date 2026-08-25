# MINDFORGE: The First Guardian

### A BCI action game where your hands fight and your visual attention commands a living soul companion.

> **Target:** BR41N.IO Designers' Hackathon at IEEE SMC 2026  
> **Category:** Your Gaming Project / BCI Gaming  
> **Event:** October 4–5, 2026  
> **Engine target:** Unity 2022.3 LTS  
> **Primary BCI:** g.tec Unicorn Hybrid Black  
> **Primary neural paradigm:** two-target SSVEP / visual evoked potential selection

## The idea

**Mindforge** is being rebuilt around one interaction that is simple enough to explain in seconds and deep enough to master:

> A magical **Soul Wisp** floats beside the player. When an enemy appears, the Wisp splits into a **blue Sight aura** and a **green Guard aura** that orbit the enemy. The player continues fighting normally while visually attending to either moving aura. EEG detects which frequency-coded visual target the player's visual system is responding to.

### Blue → Neural Sight

Temporarily amplify damage.

### Green → Neural Guard

Temporarily accelerate healing.

The bonuses persist for a few seconds, so skilled players can repeatedly switch attention while moving, dodging, and attacking to keep both states optimally refreshed.

The game does **not** attempt to read thoughts such as “I want damage” or “I want healing.” It decodes a narrower signal: **which temporally coded visual target is producing the stronger steady-state visual evoked response in posterior EEG?** The game maps that decoded target into the fantasy of a soul companion obeying visual attention.

---

# Why this is a better BCI game

A conventional controller is already excellent at precise, millisecond-scale actions. EEG is not. Mindforge therefore does not replace movement, dodging, attacking, or parrying with slower neural commands.

```text
CONTROLLER / KEYBOARD
movement · aim · attacks · dodge · timing

              +

EEG / VISUAL ATTENTION
which magical aura should empower me now?
```

The player must decide **when it is worth moving visual attention away from the fight** to refresh an offensive or restorative neural state. That tradeoff is the game.

---

# The Soul Wisp

The Soul Wisp is not a HUD widget. It is a persistent character.

## Outside combat

It floats near the Guardian like a small balloon-like extension of the player's soul with spring-follow movement, gentle drift, particles, and subtle signal-quality behavior.

## During combat

The Wisp races toward the engaged enemy and bifurcates into two halves that orbit approximately opposite one another.

### 🔷 Blue aura — Neural Sight

Initial gameplay tuning:

```text
1.65× outgoing damage
3.4 s duration
```

A blue selection does not attack automatically. It makes the player's next few seconds of ordinary physical combat more powerful.

### 🟢 Green aura — Neural Guard

Initial gameplay tuning:

```text
+4.2 HP/s regeneration
3.4 s duration
```

Guard does not dodge or parry automatically. It lets the player recover while continuing to fight.

### Independent timers

Sight and Guard have independent expiration timers. A strong player can focus blue, exploit the damage window, switch to green, overlap healing with the remaining Sight duration, return to combat, then refresh whichever resource matters next.

The BCI becomes a genuine combat resource rather than a one-time gimmick.

---

# What is happening neurologically?

## Steady-state visual evoked potentials

The blue and green auras are visually modulated at different temporal frequencies.

| Target | Initial frequency | Gameplay identity |
|---|---:|---|
| Blue | 10 Hz | Neural Sight |
| Green | 12 Hz | Neural Guard |

When the player looks at one target, periodic visual stimulation can produce frequency-specific activity measurable in posterior EEG. The decoder compares the EEG with reference signals corresponding to the two target frequencies and harmonics.

With the Unicorn Hybrid Black, posterior channels **Pz, PO7, Oz, and PO8** are particularly relevant to this visual paradigm.

The current decoder uses a two-target **filter-bank canonical correlation analysis (FBCCA)** pipeline.

```text
BLUE 10 Hz aura       GREEN 12 Hz aura
       \                  /
          VISUAL ATTENTION
                 ↓
       posterior EEG window
                 ↓
        signal-quality gate
                 ↓
       filter-bank processing
                 ↓
      CCA score @ 10 / 12 Hz
                 ↓
        score + winner margin
                 ↓
      temporal dwell requirement
                 ↓
      ┌──────────┴──────────┐
      │                     │
 AURA_SELECTED          ABSTAIN
      │
      ├── sight → damage buff
      └── guard → healing buff
```

### Scientific boundary

The system does **not** claim that 10 Hz means aggression, 12 Hz means healing, or that it has decoded abstract damage/healing intent. The frequency identifies the **visual target**. The game assigns the meaning of that target.

---

# Why the old abstract Resonance mechanic is gone

Earlier plans included a continuous “Resonance” variable built from generic neural confidence. That is no longer a primary BCI mechanic because its neurological interpretation was too vague.

The Dual Aura system has a concrete answer to “what did the EEG decode?”

> **A calibrated visual target selection.**

The player knows what they are doing, the decoder knows what it is identifying, and a judge can see the causal chain.

---

# Why moving auras are plausible

Visual BCI targets are often stationary, but research has demonstrated VEP/P300 selection with moving targets and SSVEP paradigms with superimposed motion. Motion can reduce SSVEP performance as it becomes faster, so Mindforge makes **the animation obey the neuroscience**.

Initial orbit speed:

```text
0.92 rad/s ≈ 0.146 Hz ≈ one orbit every 6.8 seconds
```

The local particles can be rich, but the coded luminance component remains controlled. The auras are camera-facing to reduce occlusion, apparent size changes, and excessive gaze travel. Exact movement speed will be selected from physical sessions.

---

# Calibration is part of the story

The player awakens the Soul Wisp instead of beginning on a sterile settings page.

1. **Link** — check usable EEG channels while the Wisp appears beside the Guardian.
2. **Sight** — look directly at the blue aura while labeled EEG is recorded.
3. **Guard** — repeat for green.
4. **Alternation** — randomized blue/green prompts build target score distributions and teach switching.
5. **Moving validation** — both auras orbit a training construct at combat speed while prompted selections are measured.
6. **Session qualification** — fit conservative score/margin thresholds and decide whether the session is usable.

If the BCI is not reliable, Mindforge does not pretend otherwise. It can repeat a short block, inspect electrode quality, test another predeclared frequency pair, or continue Controller-Only.

---

# The decoder

The executable implementation lives in `neuro/mindforge_neuro/`.

For each target frequency `f`, it constructs sine/cosine reference signals across three harmonics, applies a small filter bank, computes canonical correlations against the two target references, combines scores, gates on signal quality and winner margin, then requires stable evidence across consecutive windows.

Current initial configuration:

```text
sampling rate        250 Hz
window               1.25 s
blue target          10 Hz
green target         12 Hz
harmonics            3
filter bank          6–35 Hz, 14–35 Hz
stable dwell         2 accepted windows
refresh              2.25 s
short refractory     0.35 s
```

These are engineering starting values to validate, not claims about achieved human performance.

---

# Uncertainty is simple: do nothing

If EEG evidence is ambiguous, contaminated, stale, or low quality, the decoder emits:

```text
ABSTAIN
```

No aura changes. No wrong “brain button.” No invented intent. An already active buff simply continues until its timer expires.

---

# Derived-event boundary

Unity never receives raw EEG. The neuroscience process emits small derived events such as:

```json
{
  "schema": "mindforge.neural_event.v1",
  "seq": 147,
  "monotonic_ns": 3829472394723,
  "event": "AURA_SELECTED",
  "target": "sight",
  "confidence": 0.91,
  "quality": 0.94,
  "paradigm": "ssvep_fbcca",
  "model_id": "participant-01-session-03",
  "artifact": false,
  "reason": null
}
```

This makes the same game testable with synthetic events, derived-event replay, recorded EEG through the live decoder, or physical Unicorn EEG.

---

# Game loop

1. **Enter the Forge** — meet and calibrate the Soul Wisp.
2. **Learn ordinary combat** — movement, attacks, dodge and enemy telegraphs before BCI pressure.
3. **Learn Sight** — a successful blue selection visibly sends energy into the Guardian and attacks become stronger.
4. **Learn Guard** — the player takes controlled damage, selects green, and sees health return.
5. **Combined encounter** — prompts disappear and the player begins choosing switches independently.
6. **Boss: The Fractured Signal** — pressure, attrition, interference, then a mastery phase where optimal play overlaps Sight and Guard while maintaining skilled physical combat.

---

# First playable implementation

## `web_demo/`

An immediately playable game-feel vertical slice.

```text
WASD      move
Space     fire
Shift     dash
Q         simulated blue/Sight evidence
E         simulated green/Guard evidence
```

It renders the persistent Wisp, dual orbiting targets, 10/12 Hz visual modulation, evidence accumulation, overlapping buffs, projectiles, dodging, and a multi-phase Fractured Signal encounter. It also exposes a `mindforge-neural-event` browser event bridge.

**Browser stimulus timing is not qualified as an EEG experiment.** It exists to test game feel before physical hardware integration.

## `unity/`

Competition-target C# components include:

- `UdpNeuralReceiver`
- `NeuralEvent`
- `SoulWispController`
- `VepAuraStimulus`
- `AuraBuffController`
- `DualAuraCombatDirector`
- `AuraAwarePlayerStats`
- development-only `SimulatedAuraInput`

The neural bridge listens on UDP port `19742` and accepts only derived events.

---

# Repository architecture

```text
mindforge/
├── README.md
├── pyproject.toml
├── docs/
│   ├── CURRENT_PLATFORM_AUDIT.md
│   ├── HACKATHON_REIMPLEMENTATION_PLAN.md
│   ├── DUAL_AURA_VEP_DESIGN.md
│   └── EXPERIMENT_PROTOCOL.md
├── neuro/mindforge_neuro/
│   ├── config.py
│   ├── quality.py
│   ├── ssvep.py
│   ├── calibration.py
│   ├── events.py
│   └── runtime.py
├── tests/test_ssvep.py
├── web_demo/
│   ├── index.html
│   ├── styles.css
│   └── game.js
└── unity/Assets/Mindforge/
    ├── NeuralBridge/
    ├── SoulWisp/
    └── Combat/
```

---

# Signal quality and artifacts

This is a movement-heavy game, so the runtime treats artifact handling primarily as **authority gating**, not magical signal cleaning. It rejects obvious unusable windows and the physical qualification campaign will stress blinks, jaw/facial EMG, head turns, simultaneous movement, electrode degradation, and Bluetooth interruption.

If the signal is uncertain, gameplay receives `ABSTAIN`.

---

# Unicorn Hybrid Black target

The primary headset provides eight EEG channels sampled at 250 Hz and supports dry/wet hybrid electrodes. The commonly used montage is:

```text
Fz
C3  Cz  C4
Pz
PO7 Oz PO8
```

This is convenient for Mindforge because it includes posterior coverage around the visual cortex. The Unicorn ecosystem also supports Python/.NET/Unity and LSL/UDP integration routes.

---

# Display timing matters

A frequency written in Unity code is not automatically the frequency physically emitted by a display. Before competition we must validate monitor refresh, variable-refresh state, frame pacing, luminance timing, target phase stability and dropped frames.

The final competition display should be measured with a **photodiode or equivalent physical timing method**.

---

# Accessibility and comfort

Color is never the only target cue.

**Sight:** blue, triangle/three-ray glyph, sharper particles, higher audio motif.  
**Guard:** green, ring/cross glyph, rounded pulse, lower audio motif.

Visual periodic stimulation is not appropriate for every participant. Mindforge must provide a warning, immediate opt-out, Controller-Only play, reduced-motion options, non-color-only information, and no gameplay penalty for BCI fallback.

---

# Scientific validation plan

We will qualify increasingly realistic conditions:

```text
stationary targets
       ↓
moving targets
       ↓
moving targets + player movement
       ↓
full combat
```

Measure per-target accuracy, accepted-decision precision, false-switch rate, abstention rate, decision-time median/p95, score margin, usable posterior channels, target-switch rate, Sight uptime, Guard uptime, overlap time, and game completion.

The question is not merely “can SSVEP classify two frequencies?” It is **can a player use this reliably while actually playing Mindforge?**

---

# Testing status

The initial decoder package includes deterministic synthetic tests for:

- 10 Hz Sight classification;
- 12 Hz Guard classification;
- obvious artifact rejection;
- calibration threshold fitting;
- dwell-gated selection emission.

These establish software behavior only, not observed human EEG accuracy.

---

# Privacy

1. Raw EEG remains local by default.
2. Unity receives only derived events.
3. Raw recording requires explicit consent.
4. No medical or psychological profiling.
5. No cloud service is required for the competition loop.
6. Participant stop always overrides gameplay.

---

# What success looks like at BR41N.IO

A judge should see the Wisp split around a boss, watch the player keep dodging, see 10 Hz evidence rise as they attend blue, then see blue energy snap back into the Guardian and attacks hit harder. After taking damage, the player attends green while repositioning, Guard activates, health returns, and for a brief period both buffs overlap. The player exploits that overlap to finish the boss.

Then the telemetry view shows exactly why each neural selection was accepted or rejected.

That is visible neural causality without pretending to read thoughts.

---

# Near-term implementation gates

- [x] Define the Dual Aura game mechanic
- [x] Replace abstract Resonance with explicit visual-target decoding
- [x] Implement two-target FBCCA decoder core
- [x] Implement conservative signal-quality gate
- [x] Implement session calibration thresholds
- [x] Implement dwell/refractory event runtime
- [x] Implement derived NeuralEvent contract
- [x] Implement browser game-feel prototype
- [x] Implement Unity neural receiver and Soul Wisp components
- [x] Add deterministic Python tests
- [ ] Connect physical Unicorn Hybrid Black acquisition
- [ ] Synchronize Unity stimulus markers with acquisition
- [ ] Validate 10/12 Hz codebook on physical display
- [ ] Run stationary human calibration
- [ ] Run moving-orb human calibration
- [ ] Run movement + BCI qualification
- [ ] Tune buff duration against measured decision time
- [ ] Complete Fractured Signal art/audio pass
- [ ] Run multi-participant playtesting
- [ ] Freeze competition build

---

# One-sentence pitch

> **Mindforge is a BCI action game where a magical soul companion splits into two moving visual-evoked-potential auras around enemies, letting players continuously allocate neural attention between offensive power and healing while their hands remain responsible for real combat.**

---

# Disclaimer

Mindforge is a research and entertainment project. It is not a medical device and does not diagnose, monitor, prevent, or treat any health condition. Its EEG features are operational BCI control signals tied to explicitly defined visual paradigms, not measurements of personality, intelligence, emotion, or mental health.

## BR41N.IO 2026

Mindforge targets the **BR41N.IO Designers' Hackathon at IEEE SMC 2026**, October 4–5, 2026, in the custom BCI gaming category.

**Your hands wield the Guardian. Your eyes guide the Wisp. Your visual cortex closes the loop.**
