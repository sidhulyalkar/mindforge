# Hardware Qualification Boundary

Mindforge treats hardware support as observed only after physical acquisition is verified on the actual machine.

## Before live labeling

Verify the physical Unicorn stream's identity, channel count/order, nominal rate, units, timestamp behavior, reconnect semantics and any auxiliary channels. Do not infer physical scale or ordering from the Phantom source.

## Display

Software frame cadence is a preflight signal only. Physically measure the two coded targets under idle and combat load before declaring the stimulus codebook qualified.

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

Record accepted selections, abstentions, false selections, timing, quality/artifact state, movement condition and failures at each stage.

## Failure behavior

Hardware loss never blocks controller combat. Existing buffs decay normally unless the experiment protocol explicitly chooses a stricter fail-close behavior. `PARTICIPANT_STOP` always dominates.
