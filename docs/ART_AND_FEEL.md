# Art, Animation, Camera, and Cognitive-Combat Feel Plan

Mindforge should maximize **legibility per effect**, not particle count.

Visual priority:

```text
lethal telegraph
 > BCI target core
 > Guardian / immediate state
 > ability geometry
 > impact feedback
 > ambience
```

## Reserved color authority

Exact Sight blue and Guard green are reserved for the two neural targets, their non-coded shells, and immediate selection transfer. They are not generic combat colors.

```text
Sight target       blue
Guard target       green
hostile normal     crimson/magenta
hostile heavy      orange-red
Guardian ordnance  ivory
reflected          violet
Twin Eclipse       magenta-white/violet
```

Enemy projectiles should be angular. Neural targets should be smooth and soft-edged.

## Action-gaze corridor

`SoulWispController` keeps both neural targets near the player-threat locus rather than in a distant HUD corner. Orbit radius, speed, anchor bias and scale are experimental neuroscience variables as much as art variables.

## Coded core vs feedback shell

```text
Aura Root
├── VEP core        <- frequency/luminance only
└── feedback shell  <- evidence/quality/offline feedback
```

The VEP core must not react to FBCCA score, confidence, margin, quality, damage, Flux, camera shake or combat hit-stop.

The feedback shell may use slow/non-periodic scale, particles, tether coherence, desaturation, irregular artifact/offline jitter and subtle audio.

This prevents classifier output from amplitude-modulating the stimulus that generated the EEG evidence.

## Impact hierarchy

```text
light impact       20 ms
Counter Pulse      20 ms
Rift Cleave        55 ms
Signal Break       80 ms
Twin Eclipse      120 ms
```

`HitStopController` owns one extendable real-time freeze window. The VEP clock continues on realtime/unscaled time.

## Directional camera response

Use a dedicated `ImpactPivot` below the normal follow/lock-on rig.

- Rift Cleave: strike-direction displacement, spring return.
- Counter Pulse: short precise kick.
- Gravity Bloom: FOV compression during capture.
- Twin Eclipse: strongest FOV expansion on release.

No camera effect may repeatedly push the VEP cores outside the useful visual field.

## Dimming rule

Major payoffs should often reduce competing information rather than add more effects.

`CombatPresentationDirector` can dim opt-in environment lights/ambience. VEP materials must ignore `_MindforgeAmbientDim`.

Do not use a fullscreen dim layer that also changes coded target luminance.

## Haptics

Continuous evidence-driven rumble during EEG accumulation is excluded from P0 because controller vibration may add movement/EMG contamination.

Use short post-decision haptic echoes for accepted Sight, accepted Guard and Concord.

## Cognitive pacing

### Phase I
Predictable fan/radial rhythms and generous telegraphs teach the physical grammar and neural refresh cadence.

### Phase II
Echo nodes widen physical positioning demands and reward Flux, stressing attention split without maximum projectile density.

### Phase III
Crossfire intensifies and increases the value of counters, near misses, Gravity Bloom and pre-established Concord. Harder does not mean unreadable.

### Signal Break
For about 2.6 s, boss scheduling pauses, the boss stays vulnerable, VEP modulation holds steady while real phase continues, ambience dims, and the combat mix may low-pass into a bass/heartbeat cue. This is both catharsis and visual rest.

## Qualification

Before promoting an effect, test CPU/GPU frame time, long frames, `DisplayTimingMonitor`, target visibility, photodiode timing, stationary classification, moving-target classification, controller movement and full combat.

A beautiful effect that corrupts target timing or forces unnecessary eye travel is a gameplay bug.
