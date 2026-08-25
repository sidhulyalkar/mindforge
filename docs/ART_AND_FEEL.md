# Art, Animation, Camera, and Cognitive-Combat Feel Plan

## Goal

Mindforge should feel authored, readable, and physically satisfying while treating visual attention as a scarce gameplay resource.

The visual target is not "maximum particles." It is **maximum legibility per effect**.

The hierarchy is:

```text
lethal telegraph
    > BCI target core
    > Guardian silhouette / immediate combat state
    > ability geometry
    > impact feedback
    > ambience / decoration
```

If an effect breaks this ordering, it is a bug even if it looks beautiful in isolation.

---

## Rendering target

Unity 2022.3 LTS + URP is the competition target.

Presentation requirements:

- stable 120 Hz presentation when the measured display path supports it;
- otherwise a locked physically measured refresh mode compatible with the final codebook;
- no uncontrolled dynamic resolution or variable refresh during qualified stimulus blocks;
- VEP phase uses realtime/unscaled time, never combat time;
- `DisplayTimingMonitor` is a software warning only, never evidence of physical luminance timing;
- final qualification still requires a photodiode or equivalent physical timing measurement under combat GPU load.

---

# 1. Visual language authority

## Reserved neural colors

The exact Sight blue and Guard green are **reserved** for:

1. the coded Sight/Guard stimulus cores;
2. their non-coded aura shells;
3. the immediate acceptance tether/transfer when a neural selection is applied.

They must not become generic combat colors.

`CombatVisualPalette` is the Unity authority for these values.

### Combat palette

- Guardian projectiles: ivory / cool white;
- hostile normal projectiles: crimson / hot magenta;
- hostile heavy attacks: orange-red;
- reflected enemy projectiles: violet;
- Concord/Twin Eclipse: magenta-white fusion, not another blue/green stimulus.

Sight may change weapon speed, damage, geometry, sound, trail length, pierce, and animation, but ordinary player ordnance should not become the exact coded Sight blue.

Guard may change shield geometry and restorative behavior, but generic healing particles should not flood the arena with the exact coded Guard green while the player is trying to identify the actual target.

## Shape language

Shape communicates category before detail.

- BCI targets: smooth, spherical, soft-edged, continuous curves;
- Guardian: clean directional silhouette;
- hostile projectiles: angular, sharp, diamond/needle/shard forms;
- Echo nodes: fractured polygonal forms;
- telegraphs: simple large geometry that resolves before decorative detail.

Never use a smooth glowing blue/green sphere as an enemy projectile.

---

# 2. The action-gaze corridor

The two neural targets should stay near the locus of combat rather than in distant HUD corners.

`SoulWispController` anchors the aura pair between Guardian and threat, biased toward the target, then performs a slow camera-facing orbit around that anchor.

Tunable experiment variables:

```text
anchorTowardTarget
orbitRadius
orbitVerticalAmplitude
orbitAngularSpeedRadians
auraScale
```

These are neuroscience parameters as much as art parameters.

If human sessions show that a prettier/wider orbit hurts selection reliability, the orbit gets smaller or slower.

Persistent health, Flux, aura timers, and poise should also be placed close enough to the action corridor that the player does not repeatedly saccade into screen corners.

---

# 3. Coded core vs diegetic feedback shell

Every aura is conceptually two separate render layers.

```text
AURA ROOT
├── coded stimulus core
└── non-coded feedback shell / tether / particles
```

## Coded stimulus core

Owned by `VepAuraStimulus`.

It may change because of:

- the declared target frequency;
- the measured luminance waveform;
- an explicit visual-rest state such as Signal Break.

It must **not** react to:

- FBCCA score;
- confidence;
- classifier margin;
- signal quality;
- ABSTAIN;
- damage;
- Flux;
- camera shake;
- combat hit-stop.

Feeding classifier evidence back into the coded luminance would amplitude-modulate the signal that produced that evidence and could introduce sidebands or self-referential decoder behavior.

## Non-coded feedback shell

Owned by `NeuralAuraFeedback`.

It may communicate evidence through slow/non-periodic:

- shell scale;
- particle density;
- tether coherence;
- desaturation;
- small irregular jitter during artifacts/offline states;
- low-volume evidence audio.

This gives the player and judges diegetic feedback without corrupting the stimulus core.

---

# 4. Spectator proof

`NeuralEvidenceHud` is primarily a judge/spectator instrument.

It displays:

- Sight FBCCA score;
- Guard FBCCA score;
- winner margin;
- quality;
- accepted/abstained state;
- simulation/live/replay provenance;
- receiver queue/backpressure health.

The HUD subscribes to the newest coalesced **evidence** stream, while gameplay consumes the receiver's bounded **authority** stream.

That distinction lets a judge watch neural evidence evolve even when a render stall has caused multiple decoder windows to arrive together.

---

# 5. Combat impact hierarchy

Impact feedback is intentionally asymmetric.

Initial target values:

```text
light impact       20 ms
Counter Pulse      20 ms
Rift Cleave        55 ms
Signal Break       80 ms
Twin Eclipse      120 ms
```

Twin Eclipse earns the enormous freeze precisely because most actions do not.

`HitStopController` owns one extendable realtime freeze window. Nested hits must never recursively capture an already-zero `Time.timeScale`.

The VEP clock continues on realtime while combat is frozen.

---

# 6. Directional camera response

Random omnidirectional shake is not the primary camera language.

`CombatPresentationDirector` uses a dedicated camera child/pivot so combat feedback layers on top of normal follow/lock-on tracking.

### Rift Cleave

Displace slightly in the strike direction, then return on a tight spring.

### Counter Pulse

Short, precise directional kick. Minimal shake.

### Gravity Bloom

Compress FOV during capture.

### Twin Eclipse release

Sharp FOV expansion plus the largest transient ambience dim in the game.

No camera effect may cause the coded aura cores to leave the useful visual field during a qualified selection block.

---

# 7. Dimming without corrupting stimuli

When a rare payoff needs emphasis, do not add another screen-filling effect first.

Reduce competing information.

`CombatPresentationDirector` can dim **ambient lights and opt-in ambience shaders** by up to roughly 40% during major moments.

The VEP core materials must ignore `_MindforgeAmbientDim`.

This is especially useful for:

- Concord acquisition;
- Gravity Bloom charge;
- Twin Eclipse release;
- Signal Break.

Do not implement dimming as a fullscreen overlay that also changes the luminance of the coded targets.

---

# 8. Haptic policy

The attractive idea of a rumble that rises while the BCI "locks on" is **not P0**.

Controller vibration during EEG accumulation can create hand/arm movement and EMG contamination. The BCI would then be changing the noise environment while attempting to decode itself.

`NeuralHapticFeedback` therefore provides short **post-decision** haptic echoes only:

- accepted Sight;
- accepted Guard;
- Concord acquisition.

Continuous evidence-driven rumble remains an experiment that must earn promotion through physical EEG testing.

---

# 9. Cognitive pacing of The Fractured Signal

The fight should alternate neural demand and physical release.

## Phase I — Learn the rhythm

- slow aimed fans;
- clean radial waves;
- generous attack intervals;
- obvious Counter Pulse opportunities;
- safe time to learn Sight/Guard refresh cadence.

## Phase II — Attention split

- Echo nodes widen physical positioning requirements;
- player chooses boss pressure vs Echo destruction/Flux;
- gaze-corridor design is stressed without immediately maximizing projectile density.

## Phase III — Controlled overload

- homing/curved projectiles;
- denser crossfire;
- higher value for near-miss Flux, counters, and Gravity Bloom;
- BCI remains a sticky strategic resource rather than a twitch requirement.

## Signal Break — catharsis and visual rest

For approximately 2.6 seconds:

- boss attacks stop;
- boss remains vulnerable;
- VEP modulation is held at steady luminance;
- the real stimulus phase clock continues underneath;
- ambience dims;
- high-frequency combat audio is low-pass filtered;
- a simple bass/heartbeat cue can replace the normal combat noise;
- player performs the physical punish.

The rest period is part of the neuroscience design, not just dramatic pacing.

---

# 10. Guardian and enemy animation

## Guardian

Use layered animation rather than translating a capsule:

- locomotion root follows velocity;
- upper body follows aim;
- dash compresses silhouette before release;
- Rift Cleave has anticipation, active sweep, recoil, recovery;
- Counter Pulse has minimal anticipation and a strong reflected-energy response;
- Gravity Bloom pulls cloth/particles inward before release.

## Fractured Signal

Telegraph using body deformation before UI symbols:

- aimed fan: spines converge toward player;
- radial pattern: body opens symmetrically;
- lance: core compresses into a line;
- Echo call: fragments visibly detach;
- vortex: outer rings rotate in opposite directions.

---

# 11. Audio identities

### Sight

Higher spectral center and sharper transients after a successful selection.

### Guard

Lower, rounder restorative layer.

### Concord

A new musical arrangement created by overlap, while remaining explicitly **not a third neural class**.

### Signal Break

Drop high-frequency combat density, low-pass the mix, expose bass/heartbeat and impact body.

### Enemy

Unstable pitch relationships and fractured rhythmic structures that become less coherent through the fight.

---

# 12. Qualification budget

Before promoting any visual effect, test:

- CPU frame time;
- GPU frame time;
- dropped/long frames;
- `DisplayTimingMonitor` warnings;
- physical stimulus luminance with photodiode;
- target visibility under the effect;
- classification under stationary attention;
- classification while moving;
- classification during controller use;
- classification during full combat.

A beautiful effect that corrupts target timing, changes target identity, or forces unnecessary eye travel is a gameplay bug.
