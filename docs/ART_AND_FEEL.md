# Art, Animation, Camera, and Combat Feel Plan

## Goal

Mindforge should look authored rather than procedurally noisy. Visual fidelity comes from coherent motion, lighting hierarchy, impact timing, and material language more than raw polygon count.

## Rendering target

Unity 2022.3 LTS with URP is the recommended competition target for predictable performance and fast iteration.

Target display performance:

- stable 120 Hz presentation if the competition display supports it;
- otherwise a locked, physically measured refresh mode compatible with the final VEP codebook;
- no dynamic-resolution or variable-refresh behavior that changes stimulus timing without measurement.

## Guardian motion

Use layered animation rather than translating a capsule:

- locomotion root direction follows velocity;
- upper body follows aim direction;
- dash compresses silhouette before release;
- Rift Cleave has anticipation, active sweep, recoil, and recovery;
- Counter Pulse has almost no anticipation but a strong reflected-energy response;
- Gravity Bloom pulls cloth/particles inward before the release.

## Soul Wisp motion

The Wisp should feel alive without making the BCI stimulus unstable.

Separate transforms:

1. **navigation root** handles player follow and enemy orbit;
2. **visual shell** handles bob/squash/particles;
3. **stimulus renderer** preserves measured luminance modulation.

Never implement decorative animation by multiplying the same material brightness value used for the VEP code. Position/shape motion and VEP luminance must remain separable.

## Enemy animation

The Fractured Signal should telegraph with body deformation before HUD indicators are needed:

- needle fan: shoulders/spines converge toward player;
- radial petals: body opens symmetrically;
- lance: core compresses into a bright line;
- Echo call: fragments detach visibly;
- vortex: outer rings rotate in opposite directions.

## Impact stack

A satisfying heavy hit combines:

1. collision event;
2. 30-70 ms combat hit-stop;
3. target recoil/impulse;
4. camera kick;
5. one sharp transient;
6. one low-frequency body sound;
7. impact flash at contact point;
8. particles following the collision normal;
9. damage/poise state change;
10. optional time-scale ease back.

Do not use huge camera shake on every shot. Contrast creates impact.

## Camera

Third-person Unity target:

- spring-damped follow;
- combat lock framing that includes player, boss, and both aura targets;
- short impulse shake independent of base camera;
- collision-aware camera boom;
- FOV kick on dash;
- subtle FOV compression on Gravity Bloom capture;
- no camera effect that causes the two aura stimuli to repeatedly leave the useful visual field.

## Lighting

Use a restrained base exposure so VFX can bloom without washing out target modulation.

- player key light: cool white;
- Sight contribution: directional blue;
- Guard contribution: soft green bounce;
- enemy: violet/magenta corruption;
- arena architecture: mostly neutral, low saturation.

## Post processing

Use sparingly:

- bloom for energy only;
- vignette for arena depth;
- subtle chromatic split on Signal Break / Twin Eclipse only;
- no global motion blur on the VEP aura targets;
- avoid temporal effects that smear luminance modulation.

## UI

Keep the center of screen clear.

Persistent HUD:

- health;
- Flux;
- Sight remaining;
- Guard remaining;
- boss health;
- boss poise.

Contextual HUD:

- `CONCORD` only during overlap;
- `SIGNAL BREAK` only while staggered;
- neural evidence details primarily on the optional science/telemetry view.

## Audio identities

### Sight

Higher spectral center, sharper transients, harmonics that increase attack brightness.

### Guard

Lower, rounder tonal layer with soft restorative pulses.

### Concord

Do not merely play both sounds simultaneously. Introduce a third *musical arrangement* caused by the overlap, while making clear that no third neural class exists.

### Enemy

Unstable pitch relations and phase-like motion. As the boss loses health, rhythmic structure becomes less coherent.

## Performance budget

The visual design must protect BCI timing.

Before adding an effect, test:

- CPU frame time;
- GPU frame time;
- dropped frames;
- actual stimulus luminance timing;
- whether target visibility is preserved under the effect.

A beautiful effect that corrupts target timing is a gameplay bug.
