# Hardware Qualification Boundary

Mindforge treats hardware support as observed only after physical acquisition is verified on the actual machine.

Verify physical Unicorn stream identity, channel order, nominal rate, units, timestamps and reconnect behavior. Never infer these values from Phantom data.

Software frame cadence is only a preflight signal. Physically measure both coded targets under idle and combat load before declaring the display path qualified.

Qualification proceeds:

```text
physical stream
→ stationary targets
→ moving targets
→ controller movement
→ light combat
→ full combat
```

Record selections, abstentions, false selections, timing, quality/artifact state, movement condition and failures at every stage.

Hardware loss never blocks controller combat, and `PARTICIPANT_STOP` always dominates.
