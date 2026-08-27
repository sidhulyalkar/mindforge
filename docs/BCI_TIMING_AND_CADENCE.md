# BCI Timing and Combat Cadence

Mindforge has two clocks with different jobs.

```text
COMBAT CLOCK                      STIMULUS CLOCK
120 Hz fixed simulation           real / unscaled time
movement                           10 Hz Sight code
collision                          12 Hz Guard code
poise                              stimulus phase
hit-stop allowed                   never paused by hit-stop
```

This separation is a core competition feature. A heavy parry can freeze game simulation for impact without changing the intended visual-stimulus frequency.

## Display frame pacing

The target frequency is only physically useful if the display emits the expected temporal pattern. `DisplayTimingMonitor` therefore watches software render cadence and records:

- observed refresh rate;
- long-frame/drop fraction;
- whether the configured expected refresh is being sustained.

This is a software guard, **not physical stimulus validation**.

Competition qualification still requires measurement of emitted luminance timing on the intended monitor with a photodiode or equivalent high-speed physical method.

Recommended display qualification:

1. select the actual competition monitor;
2. disable/characterize variable refresh behavior;
3. test intended 60 or 120 Hz operating mode;
4. record Sight target luminance timing;
5. record Guard target luminance timing;
6. verify frequencies, phase stability, dropped frames and harmonics;
7. repeat while the full combat scene is under GPU load;
8. repeat across hit-stop and Signal Break transitions.

## Why macro-buffs are required

The EEG evidence window is measured in seconds while Counter Pulse is measured in milliseconds. These systems should never compete for the same authority.

Mindforge therefore uses:

```text
EEG       → strategic state
controller → physical execution
```

The first FBCCA evidence window is currently 1.25 s and the decoder uses multi-window dwell. Actual human selection time will depend on hop size, participant SNR, target motion, artifacts and calibrated thresholds.

Sight and Guard must therefore remain long enough that the player can:

1. acquire a neural state;
2. return visual attention to the battlefield;
3. physically exploit it.

## Cadence sweep

`tools/run_phantom_cadence.py` uses neurOS Phantom EEG to compare candidate gameplay timing under controlled attenuation between calibration and combat.

Example:

```bash
python tools/run_phantom_cadence.py \
  --calibration-gain 1.0 \
  --combat-gains 1.0,0.8,0.65 \
  --switch-seconds 3.25 \
  --buff-seconds 3.6,4.5,5.25 \
  --grace-seconds 3.0,4.5,6.0 \
  --json cadence.json
```

It reports:

- accepted neural events;
- stale selections after an intended attention switch;
- median and p95 synthetic switch latency;
- Sight uptime;
- Guard uptime;
- Concord uptime.

The important experimental variable is not only absolute response strength. It is the **drop from calibration response strength to combat response strength**.

A player may calibrate beautifully while stationary and then lose SSVEP amplitude when:

- the targets move;
- they make frequent saccades;
- controller tension rises;
- visual competition increases;
- head/face movement increases;
- fatigue accumulates.

The cadence sweep is designed to make that degradation visible before we pick final buff durations.

## Current gameplay defaults

The current implementation should be treated as an experiment candidate, not a final truth:

```text
Sight             3.6 s
Guard             3.6 s
Concord grace     4.5 s after true overlap
Signal Break rest 2.6 s
```

If physical moving-target sessions show longer switching latency, prefer increasing macro-buff duration or reducing visual pressure before lowering decoder reliability thresholds.

## Design priority

When the neuroscience and action game disagree, preserve both by changing the encounter cadence rather than forcing one system to impersonate the other.

For example:

```text
attention opportunity
      ↓
neural state acquired
      ↓
physical pressure
      ↓
poise collapse
      ↓
Signal Break + VEP rest
      ↓
physical punish
      ↓
new attention opportunity
```

That rhythm is more defensible and more legible than permanent high-intensity flicker during nonstop bullet pressure.
