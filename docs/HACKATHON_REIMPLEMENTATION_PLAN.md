# Mindforge BR41N.IO 2026 Reimplementation Plan

## Mission

Create an 8–12 minute BCI-native action-game experience that is scientifically defensible, visually spectacular, immediately understandable, physically reliable, and rehearsed enough to survive a live jury demonstration.

The goal is not maximum feature count.

The goal is one interaction no conventional game can reproduce cleanly with an extra button.

---

# Product thesis

## The hands own precision. The brain owns transformation.

Controller/keyboard:

- movement
- dodge
- attacks
- counter timing
- aim

BCI:

- reveals hidden state
- chooses/stabilizes a neural channel
- prepares opportunities
- accumulates Resonance

BCI never owns a frame-perfect attack.

---

# Vertical slice

## 1. Awakening / calibration

Duration target: 2–5 minutes including hardware preparation.

The Guardian learns the participant's neural signature while the player learns the world.

Required:

- channel quality
- task instructions
- short baseline
- paradigm trials
- model validation
- threshold selection
- practice selection
- clear Live BCI / Controller-Only decision

No fake “ready” state.

## 2. Hall of Echoes tutorial

Duration target: 2 minutes.

Teach physical combat first.

Then show the first BCI-native interaction:

- several Echo Sigils appear
- participant attends to one
- decoder accumulates evidence
- one stabilizes
- player physically attacks the revealed vulnerability

The entire game grammar should be understood here.

## 3. The Fractured Signal boss

Duration target: 5–7 minutes.

### Phase A — Echo

Neural Sight reveals the real boss copy.

### Phase B — Prediction

BCI selects/stabilizes one future timeline; controller skill handles the resulting attack pattern.

### Phase C — Interference

The game makes abstention meaningful. Low-quality evidence does nothing rather than creating a false command.

### Phase D — Resonance

Successful BCI interactions accumulate into a spectacular audiovisual final state. Controller execution lands the final attack.

## 4. Evidence epilogue

Duration target: 30–60 seconds.

Show:

- paradigm
- calibration duration
- accepted neural decisions
- abstentions
- false activations if ground truth is available
- mean/median confidence
- processing overhead
- signal loss events
- boss result

The judge sees both the game and the science.

---

# Architecture

```text
Unicorn
  ↓
neuro/acquisition
  ↓
neuro/signal
  ↓
neuro/paradigms
  ↓
neuro/inference
  ↓
shared NeuralEvent protocol
  ↓
unity/NeuralBridge
  ↓
Boss / world / HUD / audio
```

## Hard invariants

1. Unity never receives raw EEG.
2. Every neural event is timestamped and sequenced.
3. Every event carries confidence and quality.
4. Decoder can always emit `ABSTAIN`.
5. `PARTICIPANT_STOP` dominates every state.
6. Controller-Only remains playable.
7. A hardware fault cannot own or block the combat loop.
8. Replay and Live BCI use the same event schema.

---

# NeuralEvent v1

```json
{
  "schema": "mindforge.neural_event.v1",
  "seq": 147,
  "monotonic_ns": 3829472394723,
  "event": "ATTUNE_TARGET",
  "target": "echo_03",
  "value": null,
  "confidence": 0.91,
  "quality": 0.94,
  "paradigm": "ssvep_fbcca",
  "model_id": "participant-01-session-03",
  "artifact": false,
  "reason": null
}
```

`ABSTAIN` is a normal event, not an exception.

---

# Paradigm tournament

We should not decide the competition decoder by taste.

Build three experiments behind the same event API.

## Candidate A — SSVEP / FBCCA

### Pros

- strong target-selection mapping
- relatively short calibration
- natural multi-target mechanic
- straightforward confidence score

### Risks

- flicker burden
- refresh-rate/frequency design
- photosensitivity concerns
- potential visual interference with action combat

### First gameplay use

Neural Sight during deliberately slowed/contained attunement windows.

## Candidate B — P300

### Pros

- excellent narrative mapping to attended target
- less continuous flicker
- established BCI paradigm

### Risks

- requires repeated stimulus sequences
- selection latency
- stimulus synchronization must be excellent

### First gameplay use

Echo Sigil selection and timeline collapse.

## Candidate C — motor imagery

### Pros

- feels like active self-generated neural control
- strong scientific story

### Risks

- high subject variability
- calibration burden
- novice performance can be poor

### First gameplay use

Neural Guard preparation only.

### Gate

MI does not become competition-critical unless real participants demonstrate sufficiently stable control.

---

# Empirical selection score

For each paradigm, collect:

- setup minutes
- calibration minutes
- online accuracy
- false activation rate
- abstention rate
- median decision time
- p95 software overhead after decision window
- artifact sensitivity
- participant success fraction
- self-reported comfort
- game comprehension

Create a weighted score where reliability and player understanding outweigh raw information-transfer rate.

---

# Game systems to build

## Player

Minimal action set:

- move
- dash
- light attack
- shatter/heavy
- counter
- interact

One primary weapon.

Do not migrate five weapons until the core fight is already excellent.

## Boss

One authoritative state machine with:

- physical state
- neural opportunity state
- vulnerability state
- resonance state
- presentation state

Neural state may reveal/change opportunities but cannot directly mutate player inputs.

## Resonance

Resonance is the bridge between classifier uncertainty and game feel.

Potential update rule:

```text
if artifact or quality < q_min:
    delta = 0
elif confidence < c_min:
    delta = 0
else:
    delta = gain * calibrated_confidence * task_success
```

Decay slowly between interactions so one lucky decision cannot dominate the final state.

## Telemetry

Every run produces one append-only local session record containing:

- timestamps
- stimulus markers
- derived events
- quality summaries
- game-state transitions
- controller inputs needed for deterministic replay

Raw EEG recording is separately consented and separately stored.

---

# Scientific implementation

## Acquisition

Target Unicorn configuration:

- 8 EEG channels
- nominal 250 Hz
- explicit channel mapping
- timestamp preservation
- local ring buffer

Required states:

- disconnected
- connecting
- connected
- usable
- uncertain
- stale
- lost
- recovering

## Signal processing

Paradigm-specific pipelines, but shared infrastructure for:

- notch 50/60 Hz
- band-pass
- causal online path
- filter delay accounting
- clipping/saturation detection
- flat channel detection
- packet/stale detection
- high-frequency EMG proxy
- optional ocular rejection/down-weighting

Do not over-promise artifact “removal.” Prefer gating and evidence weighting.

## Calibration

Use within-session calibration first.

Do not introduce long-term personalization before the live single-session system is reliable.

## Confidence

Raw classifier probability is not automatically calibrated confidence.

Where possible:

- use held-out calibration trials
- estimate score distributions
- tune thresholds from calibration data
- measure expected calibration error / reliability
- record margins

## Latency

Track separately:

- acquisition delay
- window duration
- preprocessing compute
- inference compute
- transport
- Unity consumption
- visual response

Never call the task window itself “system latency.”

---

# Human study plan

The hackathon is not a clinical study, but disciplined usability testing matters.

## Pilot stages

### Developer pilot

Goal: correctness.

### Familiar-user pilot

Goal: calibration and decoder tuning.

### Naive-player pilot

Goal: tutorial and game understanding.

### Jury rehearsal

Goal: complete experience under time pressure.

## Minimum useful sample before competition

Aim for at least 8–12 distinct adult participants if hardware access permits.

This is not a powered scientific efficacy trial. It is a robustness/usability campaign.

## Record

- successful calibration yes/no
- primary paradigm result
- time-to-first-success
- completion
- number of abstentions
- obvious false activations
- perceived agency
- frustration
- comfort
- “what did the BCI do?” free response

The free response is critical. If participants describe the mechanic incorrectly, the game is communicating poorly.

---

# Reliability campaign

Test the ugly cases intentionally.

## Software fault injection

- 0.5/2/5/10% packet loss
- jitter
- duplicated events
- reordering
- stale frames
- decoder timeout
- bad sequence number
- malformed event

## Signal stress

- blink
- jaw clench
- eyebrow movement
- head turn
- electrode degradation
- noisy room
- device reconnection

## Demo stress

- launch from clean boot
- no internet
- controller unplug/replug
- headset lost mid-boss
- failed calibration
- low-quality participant
- projector/monitor refresh mismatch

Every one needs a graceful known path.

---

# Presentation choreography

## 0:00–0:30 — pitch

“Your hands control the Guardian. Your brain reveals the reality it can fight.”

## 0:30–2:30 — calibration montage

Show the system learning the participant.

## 2:30–4:00 — first neural causal moment

Select an Echo Sigil. Make the world change dramatically.

## 4:00–8:00 — boss

Show physical skill + BCI cooperation.

## 8:00–9:00 — Resonance finale

Maximum audiovisual payoff.

## 9:00–10:00 — science

One screen:

- signals
- paradigm
- confidence
- abstentions
- latency breakdown
- result

Then demonstrate one failure mode deliberately if time permits.

---

# Schedule to October 4, 2026

## Aug 24–28 — Salvage and design freeze

- recover v7.8
- classify KEEP/SIMPLIFY/REWRITE/ARCHIVE
- define new repo structure
- freeze NeuralEvent v1
- choose boss mechanic spec
- build migration manifest

Exit gate: new codebase can build from a minimal competition core.

## Aug 29–Sep 4 — Controller vertical slice

- player movement/combat
- Fractured Signal state machine
- synthetic neural events
- Resonance
- tutorial
- deterministic replay

Exit gate: game is fun enough to test without EEG.

## Sep 5–11 — Real Unicorn + paradigm prototypes

- physical acquisition
- timing markers
- SSVEP prototype
- P300 prototype
- signal quality
- recording

Exit gate: real EEG causes a visible Unity event.

## Sep 12–18 — Paradigm tournament

- multiple participants
- compare P300/SSVEP
- MI pilot only if worthwhile
- select primary
- tune confidence/abstention

Exit gate: one paradigm is frozen for competition.

## Sep 19–25 — Closed-loop game integration

- production calibration ritual
- boss neural phases
- fallback
- telemetry
- first naive-player sessions

Exit gate: unfamiliar player can complete the full experience.

## Sep 26–30 — Competition polish

- final art direction
- VFX
- audio
- accessibility
- setup UX
- crash/fault pass
- scientific summary

Exit gate: candidate build.

## Oct 1–3 — Freeze and rehearse

No new systems.

- clean-machine installs
- repeated full runs
- backup replay mode
- presentation timing
- hardware packing checklist
- evidence snapshot

Exit gate: demo survives repeated rehearsals.

---

# Definition of done

Mindforge is competition-ready when:

1. a naive participant can explain the neural mechanic correctly;
2. real Unicorn data controls at least one central game mechanic;
3. the system abstains rather than hallucinating low-confidence commands;
4. controller combat remains satisfying without BCI;
5. the BCI enables a game-state transformation that an ordinary extra button would not capture well;
6. the same session can be replayed deterministically from derived events;
7. physical signal loss is graceful;
8. the final Resonance moment is visually and sonically memorable;
9. scientific claims are backed by observed measurements;
10. the full demo fits comfortably inside a jury presentation.

---

# North-star rule

Whenever a feature competes with:

- better real EEG reliability,
- better game feel,
- clearer onboarding,
- stronger audiovisual payoff,
- better live-demo robustness,

**the feature loses.**

That is how the v7.8 platform becomes a winning game rather than an ever-growing architecture.
