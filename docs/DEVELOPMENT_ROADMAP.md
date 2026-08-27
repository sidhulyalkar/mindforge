# Mindforge Development Roadmap

This roadmap is intentionally ordered by **evidence and player value**, not by feature count.

The project already has enough BCI vocabulary. The fastest route to a competition-winning and field-useful system is to make the existing two-class design extraordinarily playable, observable, reproducible and physically qualified.

## Product thesis

Mindforge should optimize four properties at once:

1. **Fun without EEG.** The controller-only game must be worth playing on its own.
2. **BCI-native value.** Sight and Guard must change strategy in a way that feels meaningfully different from another face button.
3. **Graceful uncertainty.** Delay, abstention, artifact rejection and link loss must remain legible and fair.
4. **Inspectable evidence.** Every technical claim must point to an artifact from the causal layer that actually supports it.

The design rule remains:

> **Hands own precision. The brain owns transformation.**

## Development sequence

### Phase A — Promote the platform through reality

Goal: turn architecture into observed evidence before adding presentation complexity.

#### P0 — software contracts

Automated in GitHub Actions.

Evidence:

- JUnit XML;
- `mindforge.software_gate.v1`;
- exact Git SHA;
- uploaded CI artifact.

Pass condition: tests > 0, failures = 0, errors = 0.

#### P1 — clean-checkout Unity assembly

Run:

```bash
python tools/run_unity_gate.py
```

This must use the exact editor pinned by `unity/ProjectSettings/ProjectVersion.txt`.

The qualification path is intentionally cold-start:

```text
clean checkout
   ↓
Unity import + compile
   ↓
project configuration
   ↓
competition scene generation
   ↓
serialized-reference validation
   ↓
Gate 1 JSON + editor log
```

A committed pre-generated scene is not accepted as a substitute for this test.

Pass condition:

- Unity process exits 0;
- `CompetitionGateValidator` passes;
- observed Unity version equals the pinned version exactly;
- the report was regenerated during the current invocation.

#### P2 — controller-only encounter

No neural source is needed.

The first real game-quality milestone is one complete Fractured Signal fight that a new player can understand and finish using conventional input.

Record at minimum:

- completion / defeat;
- encounter duration;
- damage taken;
- successful counters;
- near misses;
- Gravity Bloom uses;
- Signal Break count;
- Twin Eclipse opportunities, even if manually triggered for game-feel evaluation.

Target experience:

- onboarding to meaningful control in under ~60–90 s;
- a full competition encounter in roughly 4–6 minutes;
- readable boss escalation rather than raw projectile density;
- at least one memorable resource-conversion moment before the finale.

These are design targets, not physiological claims.

#### P3 — simulated decision end-to-end

Use `simulated_decision` through the production NeuralEvent receiver.

The purpose is not decoder validation. It is to stress:

- delayed selection;
- abstention;
- contradictory sequences;
- link loss / recovery;
- authority TTL expiry;
- repeated Sight/Guard cadence;
- Concord timing;
- BCI status readability while the player is busy.

A recommended adversarial script is:

```text
Sight → abstain → Guard → stale selection → lost → recovered → Sight → Guard
```

A P3 pass means every state has a fair, understandable gameplay consequence.

#### P4 — replay reproduction

Capture a reference GameMarker stream, replay the same neural decision tape, and compare semantic consequences:

```bash
python tools/mindforge_qualify.py compare-markers \
  experiments/markers/reference.jsonl \
  experiments/markers/replay.jsonl \
  --output experiments/reports/replay-comparison.json \
  --enforce
```

Timestamps, sequence IDs and session IDs are ignored. Gameplay semantics are not.

An exact match is the gate. Similarity is diagnostic only and cannot create a pass.

#### P5 — neurOS synthetic EEG closed loop

Now replace the decision fixture with a simulated participant/sensor world:

```text
neurOS participant
 → synthetic EEG
 → Unicorn-like acquisition
 → Mindforge quality
 → FBCCA
 → dwell
 → NeuralEvent
 → Unity
 → GameMarker
```

Important: P5 validates integration robustness under a declared synthetic world. It does not validate human SSVEP physiology.

Use P5 to attack assumptions:

- weak responder;
- alpha overlap;
- blink contamination;
- controller/movement artifact;
- dropped chunks;
- jitter;
- stale packets;
- channel loss;
- source silence;
- recovery.

### Phase B — make the game excellent

Once P1–P5 are repeatable, most development time should move into game feel.

#### B1 — Guardian feel

Priorities:

- movement acceleration / stopping readability;
- dash trajectory and invulnerability communication;
- aim assistance that helps without feeling magnetic;
- shot cadence and impact confirmation;
- cleave range readability;
- Counter Pulse timing feedback;
- controller rumble only where it cannot contaminate EEG evidence accumulation.

The player should be able to understand why they were hit.

#### B2 — Fractured Signal choreography

Treat the boss as a teacher, not a particle emitter.

Phase I should teach one threat grammar at a time.

Phase II should combine already-learned grammars and introduce Echo nodes as spatial priorities.

Phase III should create controlled overload through combinations, not by simply increasing projectile count.

Signal Break should function as punctuation: relief, damage opportunity and visual reset.

#### B3 — Soul Wisp readability

The Wisp needs three visually distinct layers:

```text
coded VEP core       scientific stimulus
feedback shell       quality / evidence state
fantasy body         character / emotion / payoff
```

The coded core must remain boringly controlled. The fantasy shell can be beautiful.

A player should understand, without reading a debug HUD:

- which aura is currently available;
- whether evidence is accumulating;
- whether a selection was accepted;
- whether the system abstained;
- whether the neural link is degraded;
- whether Concord is active or in grace;
- whether Twin Eclipse is ready.

#### B4 — Concord and Twin Eclipse payoff

This is likely Mindforge's signature loop and should receive disproportionate polish.

Desired rhythm:

```text
attention investment
    ↓
Sight + Guard overlap
    ↓
CONCORD established
    ↓
eyes return fully to combat
    ↓
manual skill builds Flux
    ↓
full Flux
    ↓
TWIN ECLIPSE
```

The BCI should create the strategic condition. The player's hands should earn the spectacular payoff.

### Phase C — calibration as part of the game

Calibration should stop feeling like a diagnostics screen pasted in front of a game.

The Awakening should simultaneously:

- teach Sight;
- teach Guard;
- acquire labeled EEG;
- estimate decoder thresholds;
- communicate signal quality;
- establish the Soul Wisp narratively;
- make failure/retry understandable without blaming the participant.

Calibration UX should distinguish:

```text
SERVICE NOT READY
SIGNAL QUALITY ISSUE
COLLECTING BASELINE
ATTEND SIGHT
ATTEND GUARD
CALIBRATION ACCEPTED
CALIBRATION NEEDS RETRY
```

Never collapse all failure modes into “BCI failed.”

### Phase D — physical qualification

Only after the software loop is stable should we spend scarce human/headset time.

#### P6 — fault rehearsal

Deliberately induce:

- render stalls;
- UDP silence;
- old packets;
- queue pressure;
- decoder abstention bursts;
- application focus changes.

The participant must never receive unfair damage because the neural subsystem vanished.

#### P7 — measured display

Use photodiode evidence to measure the actually emitted luminance sequence.

Required claims should be narrow:

- observed frequency;
- frequency error;
- transition jitter;
- contrast;
- dropped/irregular transitions.

Do not promote software phase calculations into physical display claims.

#### P8 — real Unicorn acquisition

Verify on the actual competition machine:

- stream identity;
- channel order;
- sample rate;
- physical units / scaling;
- timestamps;
- drop behavior;
- reconnect behavior.

#### P9–P13 — human progression

Advance monotonically:

```text
stationary discrimination
 → moving selection
 → selection while player moves
 → light combat
 → full encounter
```

Do not jump directly to the boss fight because a stationary calibration looked promising.

## Metrics that matter

### Game metrics

Track:

- time to first meaningful action;
- encounter completion rate;
- median encounter duration;
- damage sources;
- counter success;
- near-miss conversion;
- Signal Break cadence;
- Gravity Bloom / Twin Eclipse usage;
- deaths that occur during attention shifts.

### Neural-system metrics

Track:

- accepted selections/minute;
- abstention fraction;
- artifact-suspicion fraction;
- selection latency;
- wrong-target rate where ground truth exists;
- stale authority drops;
- link degradation duration;
- calibration retries;
- source mode and calibration/model identity.

### Human-experience metrics

Ask simple questions after sessions:

- Did the BCI feel useful or merely novel?
- Did you understand when it was uncertain?
- Did looking at the Wisp make the fight unfair?
- Did Sight and Guard create meaningful decisions?
- Did Twin Eclipse feel earned?
- Would you choose to play another round?

The final question is brutal and valuable.

## What not to build yet

Until the current loop is deeply validated, avoid:

- a third SSVEP target;
- motor imagery locomotion;
- P300 menus;
- emotion classification;
- foundation-model inference in the control path;
- VR-specific dependencies;
- online multiplayer;
- generalized plugin systems that slow competition development.

These can become future platform extensions after the reference loop is proven.

## How Mindforge becomes a field example

The project should eventually ship five things together:

1. **A genuinely good playable game.**
2. **Versioned BCI/game contracts.**
3. **A no-headset development ladder.**
4. **Reproducible qualification artifacts.**
5. **A public failure/claim boundary that distinguishes simulation, hardware and human evidence.**

That combination is more valuable than having the largest model zoo.

## Immediate development queue

In order:

1. run the new clean-checkout Unity Gate 1 locally on the pinned editor;
2. repair any compile/serialization failures without weakening the gate;
3. complete one controller-only encounter and tune obvious game-feel failures;
4. record a P3 simulated-decision session with outbound GameMarkers;
5. add deterministic player-input capture/replay so P4 can become fully automatic;
6. connect neurOS Phantom through the same evidence bundle for P5;
7. only then begin physical display and Unicorn sessions;
8. move the majority of remaining competition time into presentation, onboarding, boss feel, audio and repeated human playtesting.

The north star is not “the most complicated BCI game.”

It is **the clearest demonstration of how to give uncertain neural evidence meaningful authority inside a game without sacrificing playability, scientific honesty or debuggability.**
