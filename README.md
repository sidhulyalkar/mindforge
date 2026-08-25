# MINDFORGE: The First Guardian

### A brain-computer-interface action game where your hands fight, but your brain changes what is possible.

> **Target:** BR41N.IO Designers' Hackathon at IEEE SMC 2026  
> **Category:** Your Gaming Project / BCI Gaming  
> **Event:** October 4–5, 2026  
> **Status:** Competition reimplementation in progress  
> **Engine target:** Unity 2022.3 LTS  
> **Primary BCI target:** g.tec Unicorn Hybrid Black / Unicorn Unity Interface

**Mindforge** is an experimental BCI-native action game built around one design question:

> **What can a videogame become when neural activity is not used as a worse replacement for a button, but as a new layer of perception, preparation, and world interaction?**

The player controls a Guardian through a fast, readable action-combat system using a conventional controller or keyboard. EEG is deliberately assigned to slower, uncertainty-tolerant mechanics that are difficult or impossible to express with an ordinary controller: revealing hidden structure, attuning to neural channels, stabilizing a defensive field, and building a powerful state called **Resonance**.

The goal is not to make the player “press attack with their brain.” The goal is to create a game in which the player can point to a moment and say:

> **“That happened because the game understood something measurable about what my brain was doing.”**

Mindforge is being rebuilt for the **[BR41N.IO Designers' Hackathon at IEEE SMC 2026](https://www.gtec.at/hackathon/ieee-smc-2026/)**, where custom BCI gaming projects can use a Unicorn Hybrid Black or another BCI system with Unity.

---

## Why Mindforge exists

BCI games often inherit an awkward assumption from conventional interface design: if a joystick can move left, perhaps EEG should also move left.

That is rarely where EEG is strongest.

A thumb can generate a precise button press in milliseconds. Non-invasive scalp EEG is noisy, subject-dependent, artifact-prone, temporally smeared, and often requires evidence accumulation. Forcing it into the role of a low-latency controller can make the BCI feel frustrating rather than magical.

Mindforge starts from the opposite premise:

### The hands own precision. The brain owns transformation.

The conventional input layer handles:

- movement,
- dodge timing,
- attack execution,
- aim and positioning,
- parry/counter timing,
- weapon choice.

The BCI layer handles:

- **neural perception:** attending to a stimulus to reveal or select hidden information,
- **attunement:** accumulating evidence toward a world state rather than issuing an instantaneous command,
- **preparation:** enabling or strengthening a future action without owning its final timing,
- **resonance:** sustained, confidence-weighted neural interaction that changes the arena, audiovisual state, or combat opportunity.

This separation is the core design philosophy of the project.

---

# The game

## Core fantasy

You awaken inside the **Mindforge**, an ancient machine-world built by the Guardians to convert thought into physical law.

Something has fractured its signal.

The Forge can still sense neural activity, but it can no longer distinguish intention from noise. Its defensive Guardians have become unstable. Memories have split into contradictory copies. Doors open to the wrong thoughts. Attacks arrive from possible futures that do not yet exist.

You are linked to the **First Guardian**, the last stable combat frame inside the Forge.

Your hands control the Guardian.

Your neural activity determines which version of reality the Guardian can perceive.

The player is therefore fighting on two coupled layers:

1. **Physical combat** — movement, spacing, attacks, dodges, counters.
2. **Neural combat** — perception, selection, uncertainty, attunement, resonance.

The story and the BCI are intentionally the same idea. Calibration is not a settings screen pasted onto the game. It is the process through which the First Guardian learns the player's neural signature.

---

## The competition experience

The hackathon build is intentionally designed as a polished **8–12 minute vertical slice**, not a sprawling unfinished campaign.

A complete jury playthrough should have four acts.

### Act I — Awakening

The Guardian comes online in darkness.

Eight neural channels appear as thin threads of light around the player. The world asks the player to perform short calibration tasks while the Forge visibly learns signal quality and discriminability.

The calibration sequence doubles as the opening cinematic.

Instead of:

> “Collecting training data: trial 7/20.”

The game communicates:

> “The Forge is learning how to hear you.”

The player sees honest signal-quality feedback and can continue in Controller-Only mode if a usable BCI model cannot be established.

### Act II — The Hall of Echoes

The player learns physical combat against simple enemies.

Then a Guardian creates several visually competing **Echo Sigils**. Only one corresponds to the real vulnerability.

The player attends to a chosen stimulus. The BCI accumulates evidence. The selected sigil stabilizes and the false copies dissolve.

The player then physically attacks the exposed vulnerability.

This teaches the central grammar:

> **Brain: discover the opportunity.**  
> **Hands: exploit it.**

### Act III — The Fractured Signal

The main boss can no longer be understood through ordinary vision alone.

It attacks with overlapping possible futures. During specific windows, several neural channels appear around the arena. The player uses the BCI to identify or select the channel they want to stabilize.

A successful neural interaction does not automatically damage the boss. Instead it changes the rules of the next few seconds:

- a hidden weak point becomes visible,
- a false attack disappears,
- the counter window becomes legible,
- a dangerous projectile becomes reflectable,
- an environmental relay becomes physically real.

The controller still determines whether the player succeeds.

### Act IV — Resonance

The boss enters a final state where conventional damage is insufficient.

Every successful BCI interaction has been building **Resonance**, a visible quantity represented by the entire arena becoming increasingly coherent:

- musical layers synchronize,
- fractured geometry aligns,
- particles begin moving in phase,
- the Guardian's weapon develops harmonics,
- environmental colors converge,
- neural confidence becomes visible without exposing raw EEG.

At full Resonance, the player earns one final physical attack sequence.

The decisive blow is therefore neither “brain-controlled” nor “controller-controlled.”

It is **closed-loop cooperation between neural interpretation and player skill**.

---

# The three neural mechanics

Mindforge does not assume that one EEG feature universally means “focus,” “calm,” or “intention.” Every BCI mechanic is tied to a defined paradigm, individual calibration, confidence estimate, and abstention policy.

## 1. Neural Sight — evoked-response selection

**Gameplay role:** reveal which object, timeline, rune, weak point, or environmental relay the player is attending to.

**Preferred hackathon implementation:** P300/visual evoked potential or SSVEP/FBCCA, depending on measured subject performance and Unicorn integration constraints.

Example interaction:

Four corrupted symbols orbit a boss. They are visually similar but flicker or flash according to an experimental stimulus schedule. The player attends to one symbol. The BCI estimates the attended target and gradually stabilizes it.

The mechanic is deliberately used for **selection and perception**, not frame-perfect combat.

### Candidate P300 pipeline

1. timestamp stimulus onset,
2. acquire 8-channel EEG,
3. band-pass and notch-filter,
4. extract stimulus-locked epochs,
5. reject or down-weight contaminated epochs,
6. optional xDAWN spatial filtering,
7. classify target/non-target response with regularized LDA or equivalent,
8. aggregate evidence across flashes,
9. emit a selection only when posterior confidence is sufficient,
10. otherwise abstain.

### Candidate SSVEP pipeline

1. present spatially separated targets at carefully selected frequencies/phases,
2. acquire occipital/parietal EEG,
3. filter into SSVEP bands and harmonics,
4. compute CCA or filter-bank CCA correlation against reference signals,
5. estimate confidence/margin,
6. require temporal persistence,
7. emit an attended-target event or abstain.

The final paradigm will be chosen empirically based on setup time, accuracy, comfort, visual burden, latency, and robustness across participants.

---

## 2. Neural Guard — preparation rather than timing

**Gameplay role:** the BCI prepares a defensive opportunity; the player's hands execute it.

A boss telegraphs a large incoming attack. During the preparation phase, the BCI can charge or stabilize the Guardian's defensive field. If sufficient evidence is accumulated, the player receives a stronger or more forgiving opportunity to counter the attack.

The neural system **never performs the parry for the player**.

That matters because scalp EEG is poorly suited to owning a 50–200 ms skill event. The controller remains the authoritative source for exact timing.

Possible qualified inputs include:

- motor imagery classification,
- an evoked selection indicating which defensive channel to prepare,
- an individually calibrated sustained neural-control feature.

The implementation used in competition will be whichever can be demonstrated reliably with measured evidence.

---

## 3. Resonance — uncertainty becomes game feel

**Gameplay role:** turn probabilistic BCI evidence into a continuous, emotionally legible game state.

Instead of treating every classifier window as a binary command, Mindforge accumulates calibrated confidence over time.

Conceptually:

```text
neural evidence
      ↓
confidence + signal quality
      ↓
artifact / uncertainty gate
      ↓
confidence-weighted accumulation
      ↓
RESONANCE
      ↓
world, music, VFX, combat opportunity
```

This is one of the project's most important design ideas.

A noisy classifier does not have to produce a frustrating wrong button press. Low-confidence evidence can simply contribute little or nothing. Strong, stable evidence makes the world visibly converge.

BCI uncertainty becomes part of the game's visual language.

---

# Scientific significance

Mindforge is a videogame, not a medical device, diagnostic system, cognitive assessment, or treatment.

Its scientific value is as an experimental platform for **closed-loop human-computer interaction with non-invasive neural signals**.

The project asks several concrete research and engineering questions.

## 1. Can BCI mechanics be designed around the temporal characteristics of EEG rather than against them?

We compare mechanics that require instantaneous commands with mechanics that allow evidence accumulation, preparation, and delayed execution.

The hypothesis is that BCI will feel more reliable and more meaningful when the game assigns it to interactions compatible with its signal properties.

## 2. Can uncertainty be surfaced without breaking immersion?

Most classifiers produce probabilities, margins, or confidence scores, but games often collapse them into a hidden binary action.

Mindforge treats uncertainty as a first-class quantity. The game may:

- abstain,
- accumulate evidence,
- delay commitment,
- request re-attunement,
- reduce visual certainty,
- fall back to Controller-Only play.

## 3. Can calibration become part of the player's experience?

BCI calibration is typically treated as pre-game friction.

Mindforge explores calibration as onboarding, narrative, and skill learning. The player and system learn one another simultaneously.

We measure whether this reduces perceived setup burden while preserving scientific clarity.

## 4. Can the BCI add agency without taking agency away?

Every competition build must remain playable without EEG.

That is not because the BCI is unimportant. It is because a neural interface should add a meaningful dimension rather than make the player hostage to classifier errors.

We can therefore compare:

- Controller-Only performance,
- Controller + BCI performance,
- subjective agency,
- perceived responsiveness,
- error attribution,
- cognitive workload,
- BCI abandonment rate.

## 5. Can a game make neural signals understandable to a non-expert audience?

The jury should not need a signal-processing lecture before the mechanic makes sense.

Mindforge aims to create a direct visual correspondence between:

```text
stimulus / task
→ measured EEG evidence
→ confidence
→ game-state transformation
```

The scientific interpretation remains available in an optional telemetry panel, while the game communicates the same process through animation, sound, and world behavior.

---

# Scientific honesty and claims policy

Mindforge follows a strict evidence boundary.

We will **not** claim that:

- alpha power universally measures “focus,”
- a classifier is reading thoughts,
- motor imagery means literal intended movement,
- a neural score measures intelligence, attention, emotion, or mental health without validation,
- simulated hardware performance is observed physical performance,
- a software test proves real-world EEG reliability.

We **will** report:

- the exact paradigm,
- electrode montage,
- sampling rate,
- preprocessing,
- feature extraction,
- model class,
- calibration duration,
- validation protocol,
- decision thresholds,
- abstention behavior,
- selection accuracy,
- false activation rate,
- signal-quality exclusions,
- processing latency,
- end-to-end interaction latency.

A beautiful demo is more impressive when the claims are trustworthy.

---

# Hardware and signal pipeline

## Primary device target

The competition implementation targets the **g.tec Unicorn Hybrid Black**, an 8-channel wearable EEG system commonly used in BR41N.IO projects.

The runtime architecture is designed so that device-specific acquisition remains isolated from the game.

```text
┌──────────────────────────────┐
│ Unicorn Hybrid Black         │
│ 8-channel EEG                │
└──────────────┬───────────────┘
               │ timestamped samples
               ▼
┌──────────────────────────────┐
│ Acquisition Adapter          │
│ SDK / Unicorn / optional LSL │
└──────────────┬───────────────┘
               ▼
┌──────────────────────────────┐
│ Signal Quality + Ring Buffer │
│ packet gaps / stale frames   │
└──────────────┬───────────────┘
               ▼
┌──────────────────────────────┐
│ Preprocessing                │
│ notch / band-pass / epochs   │
└──────────────┬───────────────┘
               ▼
┌──────────────────────────────┐
│ Artifact + Quality Gate      │
│ blink / EMG / saturation     │
└──────────────┬───────────────┘
               ▼
┌──────────────────────────────┐
│ Paradigm Decoder             │
│ P300 / SSVEP / MI            │
└──────────────┬───────────────┘
               ▼
┌──────────────────────────────┐
│ Confidence + Abstention      │
│ calibration / dwell / margin │
└──────────────┬───────────────┘
               ▼
┌──────────────────────────────┐
│ Derived Neural Event API     │
│ NO RAW EEG IN GAMEPLAY       │
└──────────────┬───────────────┘
               ▼
┌──────────────────────────────┐
│ Unity Gameplay Runtime       │
│ perception / guard / state   │
└──────────────────────────────┘
```

The Unity game should never need to know whether a selection came from CCA, LDA, a future decoder, or a replay fixture.

It receives a small derived event contract such as:

```json
{
  "event": "ATTUNE_TARGET",
  "target": "echo_03",
  "confidence": 0.91,
  "quality": 0.94,
  "source": "ssvep_fbcca",
  "timestamp_ms": 1724531234567
}
```

Raw EEG remains on the acquisition/decoding side of the boundary unless explicitly recorded for a consented research session.

---

# Preprocessing and artifact strategy

The exact production pipeline will be frozen only after real Unicorn recordings, but the intended architecture includes:

- device-clock timestamps preserved at acquisition,
- configurable mains notch at 50/60 Hz,
- paradigm-specific band-pass filtering,
- causal filtering for online inference where appropriate,
- explicit filter-delay accounting,
- sliding or stimulus-locked windows,
- channel-quality estimation,
- amplitude/saturation checks,
- high-frequency EMG contamination metrics,
- eye-blink / ocular artifact flags where distinguishable,
- stale-frame detection,
- packet-gap detection,
- decoder-specific confidence,
- temporal dwell / persistence rules,
- abstention when evidence is insufficient.

The primary goal of artifact handling is not to create an impossibly “clean” EEG stream. It is to prevent obvious non-neural contamination from silently becoming authoritative gameplay.

---

# Calibration

Calibration is personalized and paradigm-specific.

## Proposed competition calibration budget

Target total setup experience: **approximately 2–5 minutes**, including basic signal-quality checks and model calibration.

That is a target to be measured on physical hardware, not a current claim.

### Calibration stages

1. **Contact check** — show electrode quality and obvious bad channels.
2. **Baseline recording** — short eyes-open/rest segment for signal characterization.
3. **Paradigm trials** — collect labeled P300, SSVEP, or MI data.
4. **Fast cross-validation** — estimate whether a usable model exists.
5. **Threshold selection** — tune confidence and abstention for this participant.
6. **Practice interaction** — player learns how the game responds.
7. **Qualification decision** — Live BCI or Controller-Only fallback.

Calibration quality should be visible rather than hidden behind a single “Ready” indicator.

---

# Decoding strategy

The project intentionally supports multiple decoding strategies because the best hackathon mechanic should be chosen based on observed reliability rather than ideology.

## Primary candidate: evoked-response selection

Likely first choice for the competition vertical slice because it naturally maps to deliberate target selection and can tolerate evidence accumulation.

Candidate algorithms:

- P300 + xDAWN + regularized LDA,
- P300 + Riemannian covariance features,
- SSVEP + CCA,
- filter-bank CCA,
- compact subject-calibrated discriminative models.

## Secondary candidate: motor imagery

Potential use for Neural Guard or a slow binary attunement action.

Candidate algorithms:

- CSP + shrinkage LDA,
- filter-bank CSP,
- Riemannian covariance / tangent-space classification,
- compact EEGNet-style model only if data volume and latency justify it.

Motor imagery is considered a **qualified extension**, not a required dependency for the winning demo. If a participant cannot establish stable MI control quickly, the game must not pretend otherwise.

## Continuous neural control

Continuous features may be explored for Resonance, but any semantic label must be conservative.

Rather than calling a normalized band-power value “focus,” the runtime should expose an operational quantity such as:

```text
participant-calibrated spectral control score
```

and define exactly how it is computed.

---

# Confidence, abstention, and failure behavior

A central Mindforge principle is:

> **The system is allowed to say “I do not know.”**

A neural event should only become authoritative when:

- signal quality is acceptable,
- the relevant channels are available,
- the model margin/confidence passes threshold,
- temporal persistence is satisfied,
- no high-priority artifact or stale-data condition is active.

Otherwise the decoder emits `ABSTAIN`.

Examples:

```text
ATTUNE_TARGET target=echo_02 confidence=.93
NEURAL_GUARD confidence=.84
RESONANCE_DELTA value=.12 confidence=.78
ABSTAIN reason=LOW_MARGIN
ABSTAIN reason=ARTIFACT
BCI_LOST reason=STALE_STREAM
BCI_RECOVERED
```

This creates deterministic, testable behavior in Unity.

---

# Game mechanics in detail

## Physical combat

The combat layer inherits several strong principles from the earlier Mindforge prototype lineage:

- deterministic combat authority,
- world-space collision independent of render resolution,
- clear attack telegraphs,
- controller-owned counter timing,
- no hidden aim assistance from neural input,
- readable vulnerability windows,
- accessibility settings that do not alter collision truth.

The competition version will reduce the old prototype's breadth and focus on a smaller, more polished combat grammar.

### Player actions

- move,
- dash,
- light attack,
- heavy / shatter attack,
- counter,
- interact,
- optional weapon stance change.

The player should understand the physical controls in under one minute.

## Neural perception windows

Bosses intentionally create moments where the relevant information is ambiguous in the ordinary rendered world.

The BCI resolves **information**, not dexterity.

Examples:

- determine which weak point is real,
- identify the safe route through a field,
- choose which attack timeline to collapse,
- reveal the provenance of an incoming signal,
- activate one of several resonant relays.

## Neural preparation windows

The player receives advance warning that a large event is coming. Neural interaction can prepare an advantage before the fast physical action occurs.

This creates a natural division between slower EEG inference and faster motor skill.

## Closed-loop adaptation

Mindforge may adapt presentation based on measured signal reliability, but it should not secretly make the game easier because it believes a player is “bad.”

Allowed adaptations include:

- longer evidence-collection windows,
- fewer simultaneous BCI targets,
- stronger stimulus separation,
- clearer signal-quality prompts,
- more conservative confidence thresholds,
- optional practice trials.

These adaptations must be explainable and logged.

---

# Boss concept: The Fractured Signal

The hackathon vertical slice centers on a single memorable boss rather than many shallow enemies.

## Phase 1 — Echo

The boss duplicates itself into several visually plausible copies.

The player uses Neural Sight to stabilize the real signal. A successful selection exposes a vulnerability that must be attacked physically.

**BCI teaches:** attended-target decoding.

## Phase 2 — Prediction

The boss projects several future attacks at once.

The player attends to one neural relay to collapse the field into a single readable timeline. The chosen pathway changes the physical attack pattern.

**BCI teaches:** a brain-selected decision can alter game state without directly issuing a reflex action.

## Phase 3 — Interference

The arena becomes noisy and visually unstable. Artifacts and low-confidence periods are represented diegetically as unstable Forge interference, but the game never fabricates a neural action from them.

The player learns that sometimes the right response is to wait for a cleaner signal.

**BCI teaches:** uncertainty and abstention.

## Phase 4 — Resonance

Accumulated successful interactions synchronize the arena. The player performs a final controller-driven sequence while the neural layer maintains the world state that makes the attack possible.

**BCI teaches:** the interface is cooperative rather than substitutive.

---

# Visual and audio philosophy

The aesthetic should make the neural system visible without becoming a medical dashboard.

## World language

- dark architectural voids,
- luminous signal paths,
- geometric Guardian silhouettes,
- fractured holographic matter,
- volumetric neural fields,
- high-contrast vulnerability colors,
- coherent spatial telegraphs.

The world should look less like a literal brain and more like a machine that was designed to think.

## Neural visualization

Raw waveforms belong in optional diagnostics, not the center of the HUD.

The primary player feedback is environmental:

- confidence sharpens geometry,
- uncertainty introduces phase jitter,
- successful evidence accumulation synchronizes motion,
- artifact rejection briefly distorts a signal without triggering an action,
- Resonance harmonizes music and environment.

## Audio

Sound is part of the closed loop.

Neural evidence should gradually alter:

- harmonic density,
- rhythmic coherence,
- stereo width,
- environmental pulse,
- weapon timbre.

The final Resonance state should be audibly different even with eyes closed.

---

# Accessibility

BCI must never become an accessibility trap.

The target build includes:

- complete Controller-Only mode,
- keyboard support,
- remappable controls,
- reduced motion,
- high contrast,
- non-color-only telegraphs,
- captions,
- adjustable stimulus intensity where paradigm permits,
- safe stimulus-frequency selection,
- immediate BCI opt-out,
- automatic fallback on device loss,
- no gameplay penalty for choosing Controller-Only.

Where flashing/periodic stimuli are used, the game must provide appropriate warnings and use conservative stimulus design. A non-flicker fallback paradigm should remain available for participants who cannot or should not use an SSVEP-style interface.

---

# Privacy and ethics

EEG is sensitive biometric data and should not be treated like ordinary game telemetry.

Mindforge therefore follows these rules:

1. **Raw EEG is local by default.**
2. **Gameplay receives derived events, not raw samples.**
3. **Recording requires explicit participant consent.**
4. **Research exports are opt-in and separately labeled.**
5. **No diagnosis or mental-state profiling.**
6. **No cloud upload required for the competition demo.**
7. **No hidden online adaptation of a participant model.**
8. **Participant stop/opt-out always overrides gameplay.**

The competition demo should be able to run completely offline after installation.

---

# Measuring whether it actually works

A winning BCI game should produce evidence, not only spectacle.

The live qualification campaign will measure at least:

## Signal / decoder metrics

- usable-channel count,
- signal-quality score,
- calibration duration,
- cross-validation accuracy,
- online selection accuracy,
- information-transfer rate where appropriate,
- false activation rate,
- abstention rate,
- artifact-trigger rate,
- confidence calibration.

## Timing metrics

These must be separated rather than collapsed into one misleading latency number:

- acquisition buffering latency,
- preprocessing latency,
- inference latency,
- game-bridge latency,
- **processing overhead after a decision window closes**,
- full stimulus-to-selection time,
- controller input-to-simulation latency,
- BCI-loss-to-controller-fallback latency.

For an ERP/SSVEP system, a 1-second evidence window is not the same thing as 1 second of software latency.

## Gameplay metrics

- tutorial completion,
- boss completion,
- time-to-understand first BCI mechanic,
- neural attempts per successful interaction,
- controller-only completion,
- retries,
- missed physical execution after successful BCI preparation,
- BCI abandonment / fallback rate.

## Human factors

Short post-session measures can capture:

- perceived agency,
- perceived control,
- mental effort,
- frustration,
- immersion,
- novelty,
- trust in neural feedback,
- whether the player understood what the BCI actually measured.

---

# Competition success criteria

The hackathon build is considered ready only when the following are demonstrated on physical hardware.

### P0 — Must work

- Unity build launches reliably.
- Unicorn acquisition is real, not simulated.
- calibration completes on multiple participants.
- at least one neural mechanic works end-to-end.
- BCI failure cannot freeze or corrupt gameplay.
- Controller-Only fallback works without restart.
- raw EEG does not cross the gameplay boundary.
- the jury can understand the mechanic quickly.

### P1 — Must feel good

- physical combat is responsive,
- BCI success produces a large and unmistakable payoff,
- false activations are rare enough not to destroy trust,
- abstention is visually understandable,
- calibration feels like part of the game,
- the full experience fits comfortably inside a live presentation slot.

### P2 — Podium differentiators

- multiple participants demonstrate reproducible control,
- the game automatically generates a post-run scientific summary,
- a live telemetry view shows why each neural decision was accepted or rejected,
- the final boss uses BCI in a way that cannot be recreated by simply mapping another controller button,
- the project can replay an entire session deterministically from derived neural events,
- the architecture is reusable for future BCI games.

---

# Planned repository architecture

The new competition-focused implementation will be organized around clear ownership boundaries rather than the broad platform surface of the historical prototype.

```text
mindforge/
├── README.md
├── LICENSE
├── docs/
│   ├── GAME_DESIGN.md
│   ├── SCIENCE.md
│   ├── HACKATHON_PLAN.md
│   ├── HARDWARE_QUALIFICATION.md
│   ├── EXPERIMENT_PROTOCOL.md
│   └── ARCHITECTURE.md
│
├── unity/                         # player-visible game
│   ├── Assets/Mindforge/
│   │   ├── Combat/
│   │   ├── Guardians/
│   │   ├── Boss/
│   │   ├── NeuralBridge/
│   │   ├── Calibration/
│   │   ├── UI/
│   │   ├── Audio/
│   │   └── Telemetry/
│   └── ProjectSettings/
│
├── neuro/                         # EEG authority
│   ├── acquisition/
│   │   ├── base.py
│   │   ├── unicorn.py
│   │   ├── replay.py
│   │   └── synthetic.py
│   ├── signal/
│   │   ├── filters.py
│   │   ├── quality.py
│   │   └── artifacts.py
│   ├── paradigms/
│   │   ├── p300/
│   │   ├── ssvep/
│   │   └── motor_imagery/
│   ├── calibration/
│   ├── inference/
│   ├── bridge/
│   └── telemetry/
│
├── shared/
│   ├── schemas/
│   └── protocol/
│
├── experiments/                   # reproducible offline analysis
│   ├── recordings/
│   ├── notebooks/
│   └── reports/
│
├── tools/
│   ├── run_simulation.py
│   ├── replay_session.py
│   ├── qualification.py
│   └── export_demo_report.py
│
├── tests/
│   ├── unit/
│   ├── integration/
│   ├── replay/
│   └── hardware_contract/
│
└── legacy/                         # selectively preserved v7.8 lineage
```

---

# Runtime contract

The most important architectural boundary is:

```text
EEG system  →  DERIVED NEURAL EVENTS  →  game
```

not:

```text
EEG samples → game code → arbitrary interpretation
```

The game is therefore testable with four interchangeable sources:

1. **Synthetic** — deterministic development events.
2. **Replay** — previously recorded neural events.
3. **Offline EEG replay** — real EEG run through the live decoder.
4. **Live Unicorn** — physical hardware.

The same Unity build should behave identically given the same derived-event sequence.

---

# Development strategy

## Phase 0 — Recover and simplify the historical prototype

The previous Mindforge lineage reached v7.8 and accumulated extensive deterministic combat, accessibility, evidence, creator-tooling, and hardware-abstraction work.

The new repository will not blindly copy that platform.

We will inventory every component and classify it as:

- **KEEP** — directly supports the competition experience,
- **SIMPLIFY** — good idea, overbuilt implementation,
- **REWRITE** — concept survives but architecture changes,
- **ARCHIVE** — valuable research history, not competition-critical,
- **DELETE** — complexity without current value.

Our priority is now a real player experience and real EEG evidence.

## Phase 1 — Controller-first vertical slice

Build the complete boss encounter with synthetic BCI events.

Nothing about the game should require real EEG before the game itself is fun.

Deliverables:

- movement,
- combat,
- boss state machine,
- Neural Sight windows,
- Neural Guard windows,
- Resonance system,
- calibration presentation,
- accessibility,
- telemetry,
- deterministic replay.

## Phase 2 — Real acquisition

Implement and qualify Unicorn Hybrid Black acquisition.

Deliverables:

- discovery/connect/disconnect,
- sample timestamps,
- channel mapping,
- ring buffer,
- stale detection,
- quality metrics,
- local recording,
- device-state UI.

## Phase 3 — Paradigm tournament

Do not guess which BCI paradigm will be best.

Run P300, SSVEP, and optionally MI prototypes through the same evaluation harness.

Compare:

- calibration time,
- selection accuracy,
- latency,
- comfort,
- artifact sensitivity,
- participant variance,
- gameplay comprehensibility.

Select the primary competition mechanic based on measured results.

## Phase 4 — Closed-loop integration

Replace synthetic events with live derived events while keeping the same gameplay contract.

Validate:

- confidence behavior,
- abstention,
- signal loss,
- reconnect,
- mid-boss fallback,
- replay determinism.

## Phase 5 — Human playtesting

Test with people who did not build the game.

A new participant should be able to answer:

1. What am I trying to do physically?
2. What am I doing with the BCI?
3. How can I tell whether it worked?
4. Why did the game abstain?
5. What did the BCI let me do that a normal game could not?

If those answers are unclear, the design is not finished.

## Phase 6 — Competition polish

Freeze features.

Spend the remaining time on:

- onboarding,
- art direction,
- animation,
- sound,
- telegraph readability,
- crash resistance,
- quick device setup,
- presentation choreography,
- scientific results,
- backup demo paths.

---

# Testing philosophy

Mindforge should be testable even when the headset is not present.

## Deterministic tests

- neural event serialization,
- state transitions,
- confidence thresholds,
- abstention,
- Counter boundaries,
- boss phase transitions,
- BCI loss/recovery,
- replay equivalence.

## Fault injection

Simulate:

- packet loss,
- sample jitter,
- stale frames,
- channel dropout,
- amplifier disconnect,
- low-quality windows,
- high-amplitude artifacts,
- decoder uncertainty,
- delayed neural events,
- duplicated events.

## Physical qualification

Software fault injection does not replace hardware testing.

Before competition, we must record observed behavior for:

- headset connection,
- electrode setup,
- calibration,
- multiple users,
- movement artifacts,
- eye blinks,
- jaw/face EMG,
- disconnect/reconnect,
- Windows sleep/resume if relevant,
- long-session stability.

---

# What we are preserving from Mindforge v7.8

The historical prototype contains several ideas worth retaining:

- deterministic gameplay authority,
- controller-complete combat,
- raw-EEG isolation,
- confidence-aware neural assistance,
- explicit hardware-truth boundaries,
- fault injection,
- evidence exports,
- replayability,
- accessibility-first behavior,
- conservative claims.

It also accumulated a very broad platform surface: creator SDKs, packaging systems, federation, cooperative research, large campaign infrastructure, and extensive evidence machinery.

Those systems are not automatically part of the hackathon game.

**The 2026 rebuild optimizes for depth, reliability, scientific meaning, and one unforgettable BCI interaction.**

---

# What winning looks like

We cannot guarantee a competition result.

We can design for the qualities that make a project difficult to ignore.

A winning Mindforge demo should have five moments:

### 1. Immediate understanding

Within the first minute, the jury understands:

> “The controller controls the Guardian. The EEG reveals or stabilizes things the Guardian otherwise cannot perceive.”

### 2. Visible neural causality

The participant attends to a target.

The telemetry shows rising evidence.

The game world visibly changes.

There is no ambiguity about what caused what.

### 3. Scientific credibility

We can show the actual decoder, confidence, calibration, artifact rejection, and measured results without undermining the fantasy.

### 4. A spectacular payoff

A successful neural interaction must not produce a tiny UI icon.

It should change the room.

### 5. Grace under failure

Blink hard. Move. Lose signal. Disconnect the headset.

The game does not hallucinate commands or collapse.

It abstains, explains, and continues.

That reliability is part of the spectacle.

---

# The one-sentence pitch

> **Mindforge is a BCI action game where conventional controls execute skill, while EEG lets the player perceive and stabilize hidden realities, turning neural uncertainty into an interactive combat mechanic rather than a noisy replacement for a button.**

---

# Current status

This repository is the new canonical home for the BR41N.IO 2026 reimplementation.

The historical Mindforge v7.8 source has been recovered and is being audited for selective migration. The competition version will intentionally be smaller in surface area and much deeper in:

- player experience,
- BCI integration,
- scientific validity,
- physical hardware evidence,
- audiovisual quality,
- live-demo reliability.

Until physical qualification is complete, any hardware performance values in plans or tests are **targets**, not observed results.

---

# Near-term milestones

- [ ] Import and audit the v7.8 source lineage
- [ ] Define the competition-focused architecture
- [ ] Build the controller-first Fractured Signal vertical slice
- [ ] Implement the derived neural-event protocol
- [ ] Implement Unicorn Hybrid Black acquisition
- [ ] Build P300 prototype
- [ ] Build SSVEP/FBCCA prototype
- [ ] Evaluate MI only if calibration results justify it
- [ ] Run real multi-participant BCI qualification
- [ ] Integrate the winning paradigm into the boss encounter
- [ ] Complete audiovisual Resonance pass
- [ ] Run external usability playtests
- [ ] Produce scientific results summary
- [ ] Produce competition demo build and backup replay build
- [ ] Present at BR41N.IO 2026

---

# Collaboration

Mindforge sits at the intersection of:

- neuroscience,
- EEG signal processing,
- machine learning,
- game design,
- Unity engineering,
- audio/visual design,
- human-computer interaction,
- accessibility,
- experimental methodology.

That interdisciplinarity is intentional. Great BCI interaction design is not only a classifier problem and not only a game-design problem. It is the shape created where the two constraints meet.

---

# Disclaimer

Mindforge is a research and entertainment project. It is **not** a medical device and is not intended to diagnose, monitor, prevent, or treat any disease or health condition. Neural features used for gameplay are operational control signals defined by the project's calibration and decoding procedures; they should not be interpreted as clinical or psychological assessments.

---

## BR41N.IO 2026

The BR41N.IO Designers' Hackathon is scheduled for **October 4–5, 2026** during IEEE SMC 2026. The official Gaming category explicitly includes a **“Your Gaming Project”** track for teams using a Unicorn Hybrid Black or another BCI system with Unity.

Official event page: [gtec.at/hackathon/ieee-smc-2026](https://www.gtec.at/hackathon/ieee-smc-2026/)

---

**Build the Guardian with your hands. Teach the Forge how to hear you.**
