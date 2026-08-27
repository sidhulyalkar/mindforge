# Mindforge BCI Game Platform

Mindforge is both a game and a reference architecture for building BCI games without turning the game engine into an EEG laboratory.

The invariant is simple:

> **Gameplay consumes derived neural authority. It never consumes raw EEG.**

The second invariant is equally important:

> **Every substituted layer declares its provenance. Simulation is a development tool, not human evidence.**

## Closed-loop architecture

```text
                         MINDFORGE UNITY
              ┌──────────────────────────────┐
 controller ─►│ responsive combat            │
 NeuralEvent ►│ neural authority             │
              │ presentation / VEP stimuli   │
              └──────────────┬───────────────┘
                             │ GameMarker
                             ▼

                      MINDFORGE NEURO
              ┌──────────────────────────────┐
 EEG ────────►│ acquisition                  │
              │ quality / artifact authority │
              │ FBCCA                        │
              │ dwell / refractory policy    │
              └──────────────┬───────────────┘
                             │ NeuralEvent
                             ▼

                           neurOS
              ┌──────────────────────────────┐
              │ synthetic participant worlds │
              │ Unicorn-like sensor models   │
              │ replay / transport faults    │
              │ measured display evidence    │
              │ qualification                │
              └──────────────────────────────┘
```

Unity and Python communicate through two intentionally asymmetric contracts:

- `NeuralEvent v2`: Python -> Unity. Derived evidence and bounded authority only.
- `GameMarker v1`: Unity -> external tools. Presentation and gameplay facts only.

Neither contract contains raw EEG.

## Interchangeable realities

The game should be developable at six evidence levels without changing game logic.

| Level | Source mode | What is substituted | Purpose |
|---|---|---|---|
| S0 | `manual` | neural authority | fastest mechanic/UI development |
| S1 | `simulated_decision` | decoder output | authority/error/UX stress testing |
| S2 | `decision_replay` | decoder output from a recorded tape | exact gameplay reproduction |
| S3 | `eeg_replay` | participant + acquisition | rerun the production decoder on recorded EEG |
| S4 | `synthetic_eeg` | participant/physiology/sensor world | neurOS closed-loop falsification |
| S5 | `live` | nothing | physical participant + headset evidence |

Legacy `simulation` and `replay` provenance remains accepted so existing qualification artifacts do not become unreadable.

### What these levels do not mean

S1 success does not validate the decoder.

S2 success does not validate EEG processing.

S4 success does not validate human physiology.

Only the level actually exercised may be claimed.

## One game session, separate evidence identities

All modern Unity evidence surfaces use the same process-lifetime `session_id` from `MindforgeSessionContext`. This makes the durable Unity session envelope, outbound `GameMarker` stream, and returned `NeuralEvent` stream exactly joinable by ID rather than by approximate timestamps.

Calibration is a separate dimension:

```text
session_id       = this Unity game run
calibration_id   = the calibration profile/epoch that authorized decoding
model_id         = decoder implementation/profile identity
source_mode      = which causal layer was substituted
```

A calibration marker must never silently change the meaning of `session_id`.

Legacy `mindforge.calibration_marker.v1` captures used their old `session_id` field as a calibration identifier. The Python adapter preserves those recordings by promoting that value into `calibration_id` when read.

## NeuralEvent v2

The versioned schema lives at `contracts/neural_event.v2.schema.json`.

Important fields beyond v1 include:

```text
session_id
calibration_id
source_sample_start
source_sample_end
decoder_time_ns
authority_ttl_ms
```

`authority_ttl_ms` is evaluated using the Unity process's local packet receive age. Python and Unity monotonic clocks are deliberately never subtracted from each other because they do not share an epoch.

An expired selection may still appear in judge-facing evidence, but it cannot mutate gameplay.

Unity accepts v1 and v2 so existing recordings remain useful.

## GameMarker v1

The inverse schema lives at `contracts/game_marker.v1.schema.json`.

The runtime publishes events such as:

```text
PHASE_DASH
PULSE_SHOT
RIFT_CLEAVE
COUNTER_PULSE
COUNTER_REFLECT
GRAVITY_BLOOM_CHARGE
GRAVITY_BLOOM_RELEASE
TWIN_ECLIPSE_CHARGE
TWIN_ECLIPSE_RELEASE
NEURAL_BUFF_APPLIED
CONCORD_ESTABLISHED
BOSS_PHASE
SIGNAL_BREAK
FLUX_CHANGED
BCI_DEGRADED
BCI_RECOVERED
VICTORY
DEFEAT
```

Markers carry Unity realtime, game time, rendered frame, fixed tick, game-session identity, optional calibration identity, and relevant semantic context.

Calibration begin/end markers are promoted into this same contract in newly bootstrapped scenes. The Python parser also accepts the previous `mindforge.calibration_marker.v1` schema so older captures remain valid.

### Two GameMarker lanes

UDP is datagram delivery, not a pub/sub bus. Two processes binding the same port must not be relied upon to receive independent copies. Mindforge therefore mirrors outbound markers deliberately:

```text
Unity GameMarker
      ├── UDP 19743  primary processing lane
      │              calibration / active decoder consumer
      │
      └── UDP 19745  passive observer lane
                     recorder / qualification / developer console
```

The mirror contains the same typed marker and sequence number. Passive logging can therefore run during calibration without stealing packets from the decoder.

The outbound path remains non-authoritative. Failure of either lane may remove evidence, but it must never pause combat, invent neural authority, or change a result.

## Automatic Unity installation

`MindforgePlatformBootstrap` installs the non-authoritative outbound telemetry path after a scene loads.

It is idempotent. Existing generated scenes do not need to be hand-edited, and future scenes receive the same contract without depending on fragile serialized references.

`MindforgeGameMarkerBridge` discovers the combat objects, subscribes to their semantic events, and publishes `GameMarker` records through `UdpGameMarkerSender`.

## Development harness

### Decision simulation

No headset, EEG process or neurOS instance is required:

```bash
python tools/mindforge_dev.py decision \
  --script sight:3,guard:3,abstain:1,lost:1,recovered:1 \
  --hz 4 \
  --output-tape experiments/tapes/dev.jsonl
```

Every emitted event declares:

```text
source_mode = simulated_decision
```

The simulator is seeded and deterministic so regressions can reproduce the same score/quality sequence.

### Decision replay

```bash
python tools/mindforge_dev.py replay experiments/tapes/dev.jsonl --speed 1.0
```

Replayed events receive fresh sequence/timing identity and declare:

```text
source_mode = decision_replay
```

The gameplay path is otherwise the production UDP receiver and authority implementation.

### Observe Unity without contending with calibration

```bash
python tools/mindforge_dev.py marker-log \
  --output experiments/markers/unity.jsonl
```

The command listens to the passive observer mirror on UDP 19745 by default. The active calibration/decoder path remains on UDP 19743.

## Causal trace

The target evidence chain is:

```text
requested stimulus
      ↓
Unity stimulus phase
      ↓
software presentation marker
      ↓
physical luminance
      ↓
photodiode observation
      ↓
participant visual response
      ↓
EEG
      ↓
headset / acquisition
      ↓
quality + FBCCA + dwell
      ↓
NeuralEvent
      ↓
Unity authority
      ↓
GameMarker consequence
```

A future qualification bundle should make each available edge explicit rather than collapsing the entire chain into a single claim such as “10 Hz worked.”

## Development rule for neural mechanics

A proposed BCI mechanic belongs in Mindforge only when it passes all four tests:

1. **BCI uniqueness:** neural input adds something more meaningful than another controller binding.
2. **Temporal compatibility:** the action tolerates evidence accumulation on the order of seconds.
3. **Error compatibility:** abstention/delay does not make the game unfair.
4. **Attention compatibility:** attending to the stimulus does not hide lethal information.

This is why movement, aiming, dashing and frame-critical counter timing remain conventional input while Sight and Guard alter slower strategic state.

## Promotion ladder

```text
P0  Python contract/unit tests
P1  Unity compile + generated scene gate
P2  controller-only full encounter
P3  simulated_decision -> NeuralEvent -> Unity
P4  decision replay reproduction
P5  neurOS synthetic EEG -> production decoder -> Unity
P6  render/network fault rehearsal
P7  measured physical display timing
P8  real Unicorn acquisition metadata/units
P9  stationary Sight vs Guard
P10 moving selection
P11 selection while player moves
P12 light combat
P13 full Fractured Signal encounter
```

Promotion is monotonic. A higher-looking demo must not erase a failed lower evidence gate.

## North star

Mindforge should be useful to game developers before they own a headset, useful to BCI engineers before the game art is final, and inspectable by researchers after a session is over.

The game remains the thing a participant wants to play.

The platform makes clear why the BCI was allowed to do what it did.
