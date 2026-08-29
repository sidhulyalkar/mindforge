# Arena Menagerie V1

Arena Menagerie V1 turns the controller-only showcase into a readable ten-identity combat demo while preserving Mindforge's core authority rule:

> Hands own precision. The brain owns transformation.

The Menagerie is not a second combat framework. Every ordinary enemy still uses `JourneyEnemyController` and `EnemyAttackDefinition`. The new layer adds authored role profiles, original Neural-Gothic silhouettes, signature telegraph geometry, a deterministic 3/3/4 arena scheduler, and a richer Aetherblade presentation.

## Inspiration boundary

The roster borrows design lessons, not content, from games with strong enemy languages such as Tunic, Hollow Knight and Elden Ring:

- recognize an enemy from silhouette before reading UI,
- give each creature one or two memorable questions,
- contrast fast and delayed attack rhythms,
- make anticipation and recovery part of the fight,
- use mixed compositions to create new decisions from familiar roles,
- keep dangerous attacks visually honest.

No names, character designs, animations, meshes, story elements or signature attacks are copied. The resulting enemies are original Mindforge creatures built around fractured signal geometry, neural machinery, cathedral forms and anomalous synthetic life.

## The Menagerie Crucible

The dedicated Crucible is centered near `(5, 0, 18)` inside the existing continuous collision-backed world basin. It is intentionally separate from the Fractured Signal boss arena.

The arena contains:

- a broad collision-backed central disc,
- ten perimeter landing/combat platforms,
- paired inner/outer signal rings,
- ten luminous identity beacons,
- four tall framing pylons,
- enough open center space for camera rotation and dodge reads,
- no intentional void exposure.

The arena wakes when the Guardian enters its trigger radius. The ten enemies appear in deterministic waves of **3 → 3 → 4**. This preserves the spectacle of a ten-role roster without asking the player to parse ten simultaneous telegraphs.

## Roster

### 1. Rift Hollow

**Shape:** low knife-crawler with forward wedge body and twin lateral blades.

**Lesson:** target switching under rush pressure.

**Attacks:**
- `hollow_snap` — fast short-range bite/slash rhythm.
- `hollow_hook` — slower, wider punish for panic backpedaling.

The Hollow should be visually subordinate to larger threats while remaining immediately recognizable beneath projectile lanes.

### 2. Shardsinger

**Shape:** floating tuning-fork obelisk with bow-like transverse geometry and a bright lens.

**Lesson:** respect committed ranged lanes while changing elevation.

**Attacks:**
- `shardsinger_lance` — narrow precision shot with a clear tracking-lock transition.
- `shardsinger_chord` — limited fan that punishes one-dimensional strafing.

### 3. Signal Warden

**Shape:** broad cathedral-gate torso flanked by two towers and crowned with signal horns.

**Lesson:** isolate the anchor before greedily attacking smaller enemies around it.

**Attacks:**
- `warden_judgement` — heavy close-range commitment with long punish recovery.
- `warden_triune` — three-shot ranged answer that prevents permanent hover safety.

### 4. Null Sentry

**Shape:** compact diamond hover chassis with fins, gun keel and horizontal visor.

**Lesson:** read the exact moment a tracking attack becomes committed.

**Attacks:**
- `sentry_lockbolt` — high-tracking shot that visibly locks before release.
- `sentry_fan` — broader three-shot space check.
- `sentry_breakaway` — retreat pulse when crowded.

### 5. Chrome Penitent

**Shape:** asymmetric armored executioner with oversized cleaver side and compressed visor.

**Lesson:** do not dodge on animation start alone; read attack rhythm.

**Attacks:**
- `penitent_quick` — fast slash.
- `penitent_bell` — conspicuously delayed heavy.
- `penitent_sweep` — broad lateral answer.

### 6. Rift Stalker

**Shape:** mantis-like synthetic predator with blade legs, forward spine and luminous mandibles.

**Lesson:** sidestep a narrow committed threat rather than always rolling backward.

**Attacks:**
- `stalker_pounce` — long-reach, early-commit lunge threat. Its telegraph is rendered as a narrow charge lane rather than a generic melee sector.
- `stalker_falsebeat` — slower heavy beat that catches players who memorize the first timing.

The current V1 gameplay remains within the ordinary authoritative melee vocabulary. The lane is an honest visualization of the long, narrow strike envelope, not a second presentation-owned hitbox.

### 7. Choir Drone

**Shape:** floating spherical cage between two tuning forks, crossed by luminous halo rails.

**Lesson:** move through gaps in patterned projectile pressure rather than fleeing the entire arena.

**Attacks:**
- `choir_tone` — slower single note/bolt.
- `choir_crescendo` — five-shot 120-degree fan with explicit spoke telegraph.
- `choir_recoil` — short retreat when collapsed on.

### 8. Prism Maw

**Shape:** squat rotating carapace with four jaw planes opening around a central prism-eye.

**Lesson:** recognize cone ownership and escape sideways or vertically before release.

**Attacks:**
- `prism_maw_cone` — five-projectile cone with wedge-shaped danger preview.
- `prism_maw_needle` — fast single projectile that punishes standing safely just outside the cone.

### 9. Veil Reaper

**Shape:** tall narrow executioner on split lower stems with twin scythe structures and a dark hood mass.

**Lesson:** timing contrast under intimidation.

**Attacks:**
- `reaper_whisper` — extremely fast light cut.
- `reaper_toll` — long delayed heavy with an explicit central doom-axis marker.
- `reaper_horizon` — wide sweep.

The Reaper is designed to make premature dodge habits visible without relying on invisible delay tricks.

### 10. Orbit Seraph

**Shape:** central machine-orb wrapped in orthogonal halos and four detached-looking orbit blades.

**Lesson:** read global-looking patterns without losing track of the actual gaps.

**Attacks:**
- `seraph_horizon` — five-shot 180-degree fan rendered as strong spokes plus a boundary arc.
- `seraph_verdict` — narrow surgical follow-up shot.

The Seraph should feel like a small spatial puzzle, not a projectile firehose.

## Telegraph language

`JourneyEnemyIntentVfx` remains presentation-only. It reads fixed-tick authority from `JourneyEnemyController`:

- `AttackTelegraphProgress01`
- `AttackTrackingLocked`
- `RecoveryProgress01`
- selected `EnemyAttackDefinition`

The general grammar is:

1. **Anticipation:** thin, breathing threat geometry may track the Guardian.
2. **Commit:** geometry brightens/thickens when gameplay tracking locks.
3. **Contact:** authoritative attack resolves.
4. **Recovery:** threat geometry collapses into a fading cyan punish ring.

Menagerie signatures add shape identity:

- Stalker pounce → narrow lane,
- Prism Maw cone → wedge,
- Choir crescendo → spokes + boundary arc,
- Orbit Seraph horizon → broad spoke fan,
- Reaper toll → heavy arc + central execution axis.

These shapes do not create hitboxes and never decide whether damage occurs.

## Variant lifecycle contract

`JourneyEnemyController.OnEnable()` intentionally reapplies base archetype defaults. That is useful for normal authored enemies but would erase Menagerie specialization whenever a wave activates.

`ArenaMenagerieRoleProfile` therefore:

1. snapshots the already-authored custom locomotion and attack definitions before first deactivation,
2. lets normal Unity activation occur,
3. reapplies that stored configuration,
4. rebuilds the controller cooldown array,
5. returns control to `JourneyEnemyController`,
6. only then allows the wave scheduler to call `Arm()`.

The profile has no `FixedUpdate`, damage resolution, input handling or neural dependency.

## Aetherblade Visual Polish V2

The Aetherblade now has a layered energy treatment around the existing physical blade authority:

- bright existing inner core,
- translucent cyan outer bloom shell,
- hotter luminous tip cap,
- four emitter vents around the hilt,
- attack-sensitive emitter light,
- existing primary trail tuned for active contact,
- separate short-lived wide afterimage trail,
- bounded visual breathing driven by attack state and accepted Sight resonance.

The afterimage uses its own `AetherbladeAfterimageTipV2` child. It never renames or replaces the authoritative `SwordEnergyTip` transform.

The polish component may read:

- `IsAttacking`,
- `IsAttackActive`,
- `AttackProgress`,
- accepted `SightResonance`.

It may not modify:

- sword reach,
- damage,
- parry authority,
- attack timing,
- target lock,
- movement,
- input,
- VEP stimulus timing,
- EEG evidence.

## Compact roster visibility

`ArenaMenagerieHud` appears only after the Crucible begins. It occupies a small top-center strip and shows:

- wave number,
- active creature names,
- a short quiet-state message between waves,
- clear-state confirmation.

It intentionally does not add ten health cards or expose internal attack clocks. The primary identification system remains silhouette + threat geometry.

## BCI boundary

Arena Menagerie V1 does not change the neural authority boundary.

Accepted neural state may still transform bounded properties of a player-commanded Aetherblade action through the existing aura/resonance systems. Neural evidence cannot:

- move the Guardian,
- jump or hover,
- dodge or air dash,
- lock a target,
- swing the blade,
- select an enemy attack,
- spawn a Menagerie wave.

Stable Sight/Guard coded targets must remain isolated from these presentation effects.

## Unity qualification matrix

Run:

`Mindforge → Showcase → Build + Play Cinematic Showcase`

Then verify:

### World / arena

- Menagerie Crucible appears inside the safe basin without intersecting the boss arena or main route.
- Central disc and perimeter platforms have ordinary 3D collision.
- Camera can orbit across the arena without repeated pillar occlusion.
- No intentional route exposes the void.

### Waves

- Entering the Crucible activates exactly three enemies.
- Clearing wave 1 creates a brief quiet beat before wave 2.
- Wave 2 contains exactly three enemies.
- Wave 3 contains exactly four enemies.
- Cleared/deactivated enemies do not reappear unexpectedly.

### Role persistence

- Rift Stalker retains its fast close-pressure profile after activation.
- Choir Drone retains broad ranged spacing and its five-shot crescendo.
- Prism Maw retains its cone + needle pair.
- Veil Reaper retains fast/delayed/sweep timing contrast.
- Orbit Seraph retains its wide horizon fan.
- No specialized role silently reverts to its base Hollow/Sentry/Shardcaster/Penitent defaults.

### Silhouettes

At medium distance and with emission mentally ignored, confirm that each of the ten can be distinguished by outer contour alone.

Especially check:

- Stalker reads low and insectoid,
- Choir reads floating/forked,
- Maw reads squat and jaw-dominant,
- Reaper reads tall and bifurcated,
- Seraph reads circular/orbital.

No silhouette primitive should create a gameplay collider.

### Attack readability

- Stalker pounce has a narrow lane.
- Prism Maw cone has a visible wedge.
- Choir crescendo shows discrete spokes.
- Seraph horizon clearly communicates its very broad fan.
- Reaper toll is visually distinct from the fast Reaper strike.
- aim-lock brightening corresponds to the actual point the attack stops tracking.
- recovery rings are visible but subordinate to incoming danger.

### Aetherblade

- white/hot inner blade remains visibly brighter than the cyan bloom shell,
- outer shell is translucent rather than an opaque blue tube,
- tip cap reads as a hot energy termination rather than a ball attached to the blade,
- primary trail remains narrow enough to see the target,
- afterimage trail adds speed without covering enemy telegraphs,
- Sight resonance increases visual energy without turning the weapon into a screen-filling bloom source,
- `SwordEnergyTip` remains intact in the hierarchy.

### BCI / coded targets

- Sight and Guard targets remain stable and legible.
- Menagerie emission and Aetherblade trails do not visually mask coded luminance.
- no presentation code changes VEP timing.

### Console

- no compile errors,
- no missing material errors,
- no reflection field/method failures,
- no duplicate component spam,
- no lifecycle exceptions when waves activate.

## What V1 deliberately does not solve

This pass creates a strong procedural/demo roster but does not pretend procedural primitives are final production character art. The next art ceiling remains authored meshes, rigs, animation and audio.

The correct next production step after gameplay qualification is not another ten enemy concepts. It is to choose the 3–5 strongest silhouettes from this roster and replace their blockout geometry with proper modeled/rigged characters while keeping these exact gameplay contracts intact.
