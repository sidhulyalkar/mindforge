# Mindforge V0.15 EEG Demo Sequence

V0.15 turns the competition scene into a presentation-ready neural-combat demo without weakening the V0.14 causal SSVEP contract.

## Experience order

1. **Cinematic arrival**
   - dark neural-sanctum establishing shot;
   - slow non-authoritative camera move toward the Wisp;
   - `MINDFORGE / NEURAL COMBAT PROTOTYPE` title;
   - no coded 10/12 Hz stimulus is active.

2. **Interaction explanation**
   - `HOLD V TO OPEN A NEURAL WINDOW`;
   - `ATTEND BLUE: SIGHT`;
   - `ATTEND GREEN: GUARD`;
   - `UNCLEAR SIGNALS DO NOTHING`;
   - conventional controls remain explicitly separate from neural target selection.

3. **Neural quiet handoff**
   - intro camera moves to a fixed calibration pose;
   - decorative camera motion stops;
   - decorative ring motion and accent-light breathing freeze for neural evidence intervals;
   - the intro waits a rendered frame plus a short settle before calling `SetIntroReady(true)`;
   - intro readiness never creates `CalibrationReady`.

4. **EEG calibration**
   - wait for real headset service;
   - require measured healthy ~120 Hz display timing;
   - baseline first;
   - simultaneous Sight/Guard coded pair;
   - counterbalanced target side;
   - Python accepts or rejects participant-specific calibration;
   - ambiguous calibration fails closed.

5. **Arena reveal**
   - only after calibration is accepted (or explicitly labelled controller-only qualification);
   - short black transition hides the awakening/arena root swap;
   - the boss is externally paused and combat input is disabled during the camera reveal;
   - camera lands on the normal gameplay pose before hostile authority resumes.

6. **Combat**
   - `WASD` movement;
   - `SPACE` jump/hover;
   - `SHIFT/RMB` evade;
   - `F/LMB` blade;
   - `T` target;
   - hold `V` for a neural window;
   - `Q` cleave, `C` counter, `R` bloom;
   - decorative ambient motion freezes whenever calibration or an armed Wisp resonance window owns EEG evidence.

## Visual language

### Awakening

The room is a restrained neural sanctum rather than a placeholder box:

- obsidian circular plinth;
- pearl/ivory structural ribs;
- metallic-gold inlays;
- cyan/verdant signal pylons;
- vertical portal ring and Wisp dais;
- slow ornamental signal rings only outside EEG evidence windows.

### Guardian

The gameplay capsule remains the physical authority, while collider-free presentation children provide:

- armored torso and shoulders;
- bright visor/core;
- dark waist structure;
- attached cyan energy blade.

### Fractured Signal

The original boss collider/vitals remain authoritative. Presentation-only children create:

- bright hostile core;
- asymmetric fracture shards;
- violet/red orbit rings;
- surrounding ruined arena spires and fractured floor geometry.

## Research HUD

The raw evidence-style HUD is hidden by default for a clean demo. `F8` toggles it for headset testing. Qualification controls remain separate (`F9`, `F11`, `F12` where already defined).

## EEG test checklist

Before calling the build headset-ready on a machine:

1. Use the intended monitor and force/confirm the target refresh mode.
2. Let the intro complete. Calibration must not start during camera motion.
3. Confirm the display monitor reports a healthy measured cadence before calibration begins.
4. Measure Sight and Guard with the photodiode path on the actual display.
5. Verify no decorative object visibly pulses or rotates during baseline/coded trials.
6. Confirm both coded targets are simultaneously present and unobstructed.
7. Complete the counterbalanced participant calibration.
8. Record held-out forced-choice trials before natural combat.
9. Record false activations/minute and abstention rate during normal movement with no intended Wisp command.
10. Test `V` windows while moving and fighting. A stale or wrong-epoch neural event must never apply an aura.
11. Trigger a display timing degradation/fault. Real neural authority must fail closed.
12. Confirm the boss cannot damage the player during the post-calibration cinematic reveal.

## Scientific boundary

The upgraded graphics do not count as SSVEP validation. V0.15 intentionally keeps the visually rich layer outside the causal stimulus path. The only components allowed to define the neural code remain the qualified coded Wisp cores and their synchronized event/EEG epoch machinery.
