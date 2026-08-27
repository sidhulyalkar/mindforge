# No-Headset Development Sources

Mindforge's development ladder is useful only if each source can enter the same generated competition scene without hidden Unity bypasses.

The rule is:

> **Development sources substitute a declared causal layer. They do not skip the game's authority boundaries.**

## Shared Awakening handshake

The competition scene starts with combat locked. `AwakeningCalibrationDirector` waits for:

```text
CALIBRATION_SERVICE_READY
        ↓
baseline begin/end
        ↓
Sight begin/end
        ↓
Guard begin/end
        ↓
CALIBRATION_READY
        ↓
combat unlocked
```

Live/synthetic EEG uses the real calibrated decoder to satisfy this protocol.

S0/S1/P4 use `DevelopmentCalibrationFixture`. It listens to the same Unity `GameMarker` calibration stream and emits the same NeuralEvent status classes, but every event carries development provenance and reasons containing `NO_EEG`.

It accepts only:

```text
manual
simulated_decision
decision_replay
```

Attempting to use it with `live` is an error.

The fixture requires the complete ordered baseline → Sight → Guard protocol. Out-of-order markers produce `CALIBRATION_FAILED` rather than silently opening the arena.

## S0 — manual neural authority

S0 is designed for game/UI work before any decoder is involved.

Run the Python authority service:

```bash
python tools/mindforge_dev.py manual-service
```

Launch the Unity player with:

```text
-mindforgeManualBCI
```

Then hold:

```text
Q  → Sight
E  → Guard
```

### Why manual intent uses a separate port

Unity does **not** emit NeuralEvents for Q/E directly.

Instead it emits:

```text
mindforge.manual_intent.v1
```

on development-only UDP 19746.

The Python `manual-service` owns:

- Awakening calibration status;
- a single monotonic NeuralEvent sequence;
- manual selection conversion;
- idle liveness;
- game/calibration provenance.

It emits the one authoritative NeuralEvent stream on UDP 19742.

This avoids a subtle but serious problem where a Python calibration fixture and a Unity manual sender could each generate independent `seq` counters into the same ordered receiver.

The resulting path is:

```text
Q/E in Unity
   ↓
manual intent :19746       non-authoritative dev input
   ↓
Python manual-service
   ↓
NeuralEvent v2 :19742      authoritative derived-event boundary
   ↓
UdpNeuralReceiver
   ↓
normal freshness / sequencing / contingency / Aura authority
```

### S0 liveness

A healthy player may spend many seconds fighting without pressing Q or E. That must not look like a dead BCI stream.

`manual-service` therefore emits safe periodic:

```text
BCI_HEARTBEAT
reason = MANUAL_DEV_IDLE
source_mode = manual
authority_ttl_ms = 0
has_evidence = false
```

A heartbeat maintains transport liveness but grants no aura authority and is not presented by the judge HUD as a classifier abstention. The HUD preserves the most recent meaningful evidence display while updating source provenance.

## S1 — simulated decision source

Run:

```bash
python tools/mindforge_dev.py decision --calibrate \
  --script sight:3,guard:3,abstain:1,lost:1,recovered:1 \
  --hz 4
```

The tool first completes Awakening using:

```text
source_mode = simulated_decision
```

Then the deterministic `DecisionSimulator` continues from the fixture's sequence number and reuses the exact Unity game-session and calibration IDs.

This means the transition:

```text
calibration status → first simulated selection
```

is one ordered NeuralEvent stream rather than two loosely related producers.

S1 validates gameplay authority/error handling only. It is **not** EEG simulation.

## P4 — decision replay through fresh Awakening

Run:

```bash
python tools/mindforge_dev.py replay experiments/tapes/reference.jsonl --calibrate
```

A fresh development calibration is created for the new Unity run, then replay events are rebound to:

- the new game `session_id`;
- the new `calibration_id`;
- sequence numbers following the calibration fixture;
- `source_mode = decision_replay`.

The recorded target/score/authority content remains the replay input.

Combine this with `GuardianInputTape` to reproduce both conventional and neural command streams.

## Ports

```text
19742  NeuralEvent authority into Unity
19743  primary GameMarker calibration/processing lane
19745  passive GameMarker observer lane
19746  manual development intent, non-authoritative
```

Only 19742 carries derived neural authority into the game.

## What not to claim

A successful S0/S1/P4 run proves increasingly strong game-system properties, but none of them prove:

- physical display timing;
- EEG acquisition quality;
- SSVEP discriminability;
- human calibration accuracy;
- human BCI combat performance.

Those remain later promotion gates.
