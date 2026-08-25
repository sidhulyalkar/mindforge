# Hardware Qualification Boundary

Mindforge treats hardware support as observed only after physical acquisition is verified on the actual machine.

## Before live labeling

Verify physical Unicorn stream identity, channel count/order, nominal rate, units, timestamps, reconnect semantics, and auxiliary channels. Never infer physical scale/order from Phantom data.

## Display

Software cadence is only a preflight signal. Physically measure both coded targets under idle and combat load before declaring the stimulus codebook qualified.

## Closed-loop ladder

```text
physical stream
  ↓
stationary targets
  ↓
moving targets
  ↓
controller movement
  ↓
light combat
  ↓
full combat
```

Record accepted selections, abstentions, false selections, timing, quality/artifact state, movement condition, and failures at every stage.

## Failure behavior

Hardware loss never blocks controller combat. Existing buffs decay normally unless the experimental protocol explicitly chooses stricter fail-close behavior. `PARTICIPANT_STOP` always dominates.
