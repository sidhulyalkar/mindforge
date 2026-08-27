# Mindforge Development Roadmap

This roadmap is ordered by **evidence and player value**, not feature count.

The project already has enough BCI vocabulary. The fastest route to a competition-winning and field-useful system is to make the existing two-class design extraordinarily playable, observable, reproducible and physically qualified.

## Product thesis

Mindforge should optimize four properties at once:

1. **Fun without EEG.** The controller-only game must be worth playing on its own.
2. **BCI-native value.** Sight and Guard must change strategy in a way that feels meaningfully different from another face button.
3. **Graceful uncertainty.** Delay, abstention, artifact rejection and link loss must remain legible and fair.
4. **Inspectable evidence.** Every technical claim must point to an artifact from the causal layer that actually supports it.

The design rule remains:

> **Hands own precision. The brain owns transformation.**

## Phase A — promote the platform through reality

### P0 — software contracts

Automated in GitHub Actions.

Evidence:

- JUnit XML;
- `mindforge.software_gate.v1`;
- exact Git SHA;
- uploaded CI artifact.

Pass condition: tests > 0, failures = 0, errors = 0.

### P1 — clean-checkout Unity assembly

Run:

```bash
python tools/run_unity_gate.py
```

This must use the exact editor pinned by `unity/ProjectSettings/ProjectVersion.txt`.

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

A committed pre-generated scene is not accepted as a substitute.

Pass condition:

- Unity exits 0;
- `CompetitionGateValidator` passes;
- observed editor version equals the pinned version exactly;
- the Gate 1 report was regenerated during the current invocation.

### P2 — controller-only encounter

P2 is now explicitly **BCI-free** rather than relying on a fake calibration source.

Start capture:

```bash
python tools/mindforge_playtest.py --require-terminal
```

Then press Play in the Unity Editor and press **F8**.

The development-only controller qualification bootstrap:

- disables `UdpNeuralReceiver`;
- disables `DualAuraCombatDirector`;
- disarms `NeuralLinkContingency`;
- opens the real competition arena;
- keeps a persistent `P2 CONTROLLER-ONLY · BCI DISABLED` label visible;
- emits `QUALIFICATION_MODE / CONTROLLER_ONLY_NO_BCI`.

The bootstrap is excluded from non-development player builds.

Each P2 run should produce:

```text
markers.jsonl
encounter.json
capture.json
```

`capture.json` binds the evidence to one Unity session, records the stop reason and Git head when available, and hashes the marker stream. Cross-session marker contamination is rejected.

The encounter report tracks:

- terminal outcome and duration;
- Pulse Shot / Cleave use;
- cleave hit rate;
- Counter Pulse attempts and reflects;
- near misses and dashes;
- player/boss damage pressure;
- Signal Break cadence;
- Gravity Bloom and Twin Eclipse use;
- Concord and neural payoff markers if present;
- BCI degradation if a non-P2 run is analyzed;
- diagnostic flags for suspicious fight patterns.

It does **not** generate a synthetic fun score.

Target experience:

- meaningful control learned in roughly 60–90 s or less;
- complete competition encounter roughly 4–6 minutes;
- readable boss escalation rather than raw projectile density;
- at least one memorable threat→resource→weapon conversion moment;
- player can explain why they were hit.

These are design targets, not physiological claims.

### P3 — simulated decision end-to-end

Use `simulated_decision` through the production NeuralEvent receiver **and the real Awakening handshake**:

```bash
python tools/mindforge_dev.py decision --calibrate \
  --script sight:3,abstain:1,guard:3,lost:1,recovered:1,sight:3,guard:3 \
  --hz 4 \
  --output-tape experiments/tapes/p3-neural.jsonl
```

Stress:

- delayed selection;
- abstention;
- contradictory sequences;
- link loss / recovery;
- authority TTL expiry;
- repeated Sight/Guard cadence;
- Concord timing;
- status readability during combat.

A P3 pass means every state has a fair, understandable gameplay consequence. It does not validate EEG.

### P4 — deterministic replay reproduction

Conventional input is captured on the authoritative fixed simulation using `mindforge.guardian_input_tape.v1`.

Development Player recording:

```text
-mindforgeInputMode record
```

Replay:

```text
-mindforgeInputMode replay -mindforgeInputTape <path>
```

Replay exhaustion returns neutral commands and never falls back to live input.

Neural decision replay uses:

```bash
python tools/mindforge_dev.py replay experiments/tapes/p3-neural.jsonl --speed 1.0
```

Compare semantic Unity consequences:

```bash
python tools/mindforge_qualify.py compare-markers \
  experiments/markers/reference.jsonl \
  experiments/markers/replay.jsonl \
  --output experiments/reports/replay-comparison.json \
  --enforce
```

Timestamps, transport sequences and session IDs are ignored. Gameplay semantics are not. Exact semantic equality is the gate; similarity is diagnostic only.

### P5 — neurOS synthetic EEG closed loop

Replace the decision fixture with the simulated participant/sensor world:

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

Use P5 to attack assumptions with:

- weak responders;
- alpha overlap;
- blink contamination;
- controller/movement artifact;
- dropped chunks;
- jitter;
- stale packets;
- channel loss;
- source silence;
- recovery.

P5 validates integration robustness under a declared synthetic world. It does not validate human SSVEP physiology.

## Phase B — make the game excellent

P2 should start this phase immediately after P1. P3–P5 should continue as regression/adversarial lanes while most iteration time moves toward game quality.

### B1 — Guardian feel

Priorities:

- acceleration, stopping and movement readability;
- dash trajectory and invulnerability communication;
- aim assistance that helps without feeling magnetic;
- shot cadence and impact confirmation;
- cleave range readability;
- Counter Pulse timing feedback;
- controller rumble only where it cannot contaminate EEG accumulation.

### B2 — Fractured Signal choreography

Treat the boss as a teacher, not a particle emitter.

- Phase I teaches threat grammars individually.
- Phase II combines learned grammars and introduces Echo nodes as spatial priorities.
- Phase III creates controlled overload by composition, not merely projectile count.
- Signal Break is punctuation: relief, damage opportunity and visual reset.

### B3 — Soul Wisp readability

The Wisp should preserve three layers:

```text
coded VEP core       scientific stimulus
feedback shell       quality / evidence state
fantasy body         character / emotion / payoff
```

Without reading the judge HUD, a player should understand:

- which aura is available;
- whether evidence is accumulating;
- whether a selection was accepted;
- whether the system abstained;
- whether the neural link is degraded;
- whether Concord is active/in grace;
- whether Twin Eclipse is ready.

### B4 — Concord and Twin Eclipse payoff

This is likely Mindforge's signature loop and deserves disproportionate polish.

```text
attention investment
    ↓
Sight + Guard overlap
    ↓
CONCORD
    ↓
eyes return to combat
    ↓
manual skill builds Flux
    ↓
full Flux
    ↓
TWIN ECLIPSE
```

The BCI creates the strategic condition. The player's hands earn the spectacular payoff.

## Phase C — calibration becomes part of the game

Awakening should simultaneously:

- teach Sight;
- teach Guard;
- acquire labelled EEG;
- estimate decoder thresholds;
- communicate signal quality;
- establish the Soul Wisp narratively;
- make retry understandable without blaming the participant.

Distinguish at least:

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

## Phase D — physical qualification

Only after P1–P5 are repeatable should scarce physical/headset time dominate development.

### P6 — fault rehearsal

Deliberately induce render stalls, UDP silence, old packets, queue pressure, decoder abstention bursts and application focus changes. Neural loss must never create unfair combat authority.

### P7 — measured display

Use photodiode evidence to measure the actually emitted luminance sequence:

- observed frequency;
- frequency error;
- transition jitter;
- contrast;
- dropped/irregular transitions.

Software phase calculations are not physical display claims.

### P8 — real Unicorn acquisition

Verify on the competition machine:

- stream identity;
- channel order;
- sample rate;
- physical units/scaling;
- timestamps;
- drop behavior;
- reconnect behavior.

### P9–P13 — human progression

Advance monotonically:

```text
stationary discrimination
 → moving selection
 → selection while player moves
 → light combat
 → full encounter
```

Do not jump directly to the boss because stationary calibration looked promising.

## Metrics that matter

### Game metrics

- time to first meaningful action;
- encounter completion rate;
- encounter duration;
- damage sources;
- counter success;
- near-miss conversion;
- Signal Break cadence;
- Gravity Bloom / Twin Eclipse usage;
- deaths during attention shifts.

### Neural-system metrics

- accepted selections/minute;
- abstention fraction;
- artifact-suspicion fraction;
- selection latency;
- wrong-target rate where ground truth exists;
- stale authority drops;
- link degradation duration;
- calibration retries;
- source mode and model/calibration identity.

### Human-experience questions

- Did the BCI feel useful or merely novel?
- Did you understand when it was uncertain?
- Did looking at the Wisp make the fight unfair?
- Did Sight and Guard create meaningful decisions?
- Did Twin Eclipse feel earned?
- Would you play another round?

The last question has teeth.

## What not to build yet

Until the reference loop is deeply validated, avoid:

- a third SSVEP target;
- motor-imagery locomotion;
- P300 menus;
- emotion classification;
- foundation-model inference in the control path;
- VR-specific dependencies;
- online multiplayer;
- generalized plugin systems that slow competition development.

## Immediate queue

1. run P1 on the pinned Unity editor and repair compile/serialization defects without weakening the gate;
2. run the first P2 bundle with `mindforge_playtest.py --require-terminal` and the explicit F8 controller-only mode;
3. iterate movement, telegraphs, counter feel, hit confirmation, boss pacing, camera, audio and Twin Eclipse presentation from repeated P2 sessions;
4. run P3 against the improved fight and repair any unfair attention/uncertainty interactions;
5. record conventional input plus neural authority and make P4 semantic replay exact;
6. connect neurOS Phantom through the same observable loop for P5;
7. only then begin P6–P13 physical/human qualification;
8. preserve the majority of remaining competition time for presentation, onboarding and repeated human playtesting rather than expanding the BCI vocabulary.

The north star is not “the most complicated BCI game.”

It is **the clearest demonstration of how to give uncertain neural evidence meaningful authority inside a game without sacrificing playability, scientific honesty or debuggability.**
