# Deterministic Guardian Input Replay

Mindforge's P4 replay gate needs both sides of a play session to be reproducible:

```text
conventional player commands + neural decision tape
                    ↓
             fixed simulation
                    ↓
            semantic GameMarkers
```

Replaying only NeuralEvents is not enough. If a human has to manually reproduce movement, counters and attacks, differences in the resulting GameMarker stream cannot be attributed cleanly to the neural path or the game simulation.

## Design rule

Device input is sampled during Unity's render `Update`, but **gameplay commands are applied on the authoritative fixed simulation tick**.

`GuardianCombatInput` therefore has two jobs:

1. sample/latch device state in `Update`;
2. construct and apply one complete `GuardianCommandFrame` in `FixedUpdate`.

The command frame contains:

```text
tick
move_x / move_y
aim_x / aim_y / aim_z
fire_held
cleave_down
counter_down
dash_down
bloom_down
```

One-shot button edges remain latched until the next fixed tick and are then cleared. A render frame cannot cause the same key-down to be consumed by multiple simulation ticks.

## Tape contract

Recordings use:

```text
mindforge.guardian_input_tape.v1
```

The envelope records:

- game `session_id`;
- generation time;
- fixed simulation frequency;
- ordered command frames.

The first implementation uses Unity JSON rather than a binary format because the qualification priority is auditability and portability, not storage density.

## Record

Launch a Unity player with:

```text
-mindforgeInputMode record
```

Optionally choose the output path:

```text
-mindforgeInputMode record \
-mindforgeInputTape /path/to/guardian-input.json
```

During the fight, commands accumulate **in memory**. The recorder does not write a file every simulation tick. The tape is written on application quit or by an explicit save call.

Without an explicit path, recordings are written beneath Unity's `Application.persistentDataPath` in `mindforge_input_tapes/`.

## Replay

Launch with:

```text
-mindforgeInputMode replay \
-mindforgeInputTape /path/to/guardian-input.json
```

Replay consumes one recorded command for each fixed simulation tick.

The safety behavior is intentional:

> **If the tape ends, replay returns neutral commands. It never falls back to the keyboard/controller.**

Falling back to live input would make an apparently successful replay partly human-controlled and invalidate the evidence boundary.

## Neural-link authority still wins

Recorded input is not privileged input.

`NeuralLinkContingency` can still disable combat actions. During degradation, movement remains available by the existing fairness policy, but recorded shots, cleaves, counters, dashes and Gravity Bloom requests cannot bypass `CombatActionsEnabled`.

That means replay exercises the same authority model as a live session.

## P4 procedure

A complete P4 reproduction should eventually run:

```text
reference Guardian input tape
          +
reference NeuralEvent decision tape
          ↓
     fresh Unity process
          ↓
passive GameMarker observer
          ↓
semantic marker comparison
```

Compare with:

```bash
python tools/mindforge_qualify.py compare-markers \
  experiments/markers/reference.jsonl \
  experiments/markers/replay.jsonl \
  --output experiments/reports/replay-comparison.json \
  --enforce
```

Sequence IDs, session IDs and timestamps are intentionally ignored. The semantic gameplay consequence sequence must match exactly.

## What this does not claim

The command tape does not make Unity bitwise deterministic across arbitrary machines, physics versions, render configurations or changed game code.

It gives us a much stronger question:

> Given the same fixed-tick conventional commands and the same neural decision tape on the same qualified build, do we observe the same semantic game consequences?

If not, P4 fails and we investigate the first divergent GameMarker.

## Next step

Once P1 produces an observed player build, add a `mindforge_qualify.py session-replay` orchestrator that launches:

1. the Unity player in Guardian replay mode;
2. the passive GameMarker recorder;
3. the NeuralEvent decision replay;
4. the exact semantic comparator;
5. the promotion-manifest writer.

That will turn P4 from a multi-terminal development ritual into one qualification command.
