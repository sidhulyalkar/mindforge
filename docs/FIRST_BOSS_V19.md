# Mindforge V0.19 — The Fractured Signal as a first boss

The first boss should teach the player what kind of game Mindforge is. It should not be a stationary projectile dispenser with a larger health bar.

The intended encounter sentence is:

> **move → read → commit → recover → move → choose when to listen**

The Guardian owns immediate physical execution. The Wisp opens a short listening ritual. The boss must be readable enough that those two modes complement one another rather than competing for the player's eyes.

## Recording-driven diagnosis

The August 31 `PLAY LATEST (BCI Simulation)` capture shows a mechanically functional but visually anonymous boss. The authoritative black cylinder dominates the silhouette while floating magenta bars read as construction debris rather than anatomy. The boss occupies roughly one location, rotates presentation pieces around itself and attacks often enough that the encounter reads closer to a turret/bullet field than a first duel.

V0.19 changes the boss without changing the underlying health, damage or neural-selection authority.

## Encounter design

### Phase 1 — learn the creature

- preferred range: **4.35 m**
- movement: **1.75 m/s**
- scheduler interval after a committed pattern: **2.15 s**
- telegraph: **0.76 s**
- base radial count: **7**
- identity: patient orbit, close enough for the existing fourth-pattern cleave to matter

The point is not difficulty. The player should learn that the creature moves, plants before committing, has a dangerous blade side and leaves a real punish/reposition window afterward.

### Phase 2 — fracture the spacing

- preferred range: **5.10 m**
- movement: **2.15 m/s**
- scheduler interval: **1.78 s**
- telegraph: **0.66 s**
- maximum echoes: **2**

The boss opens the distance and makes its ranged/echo vocabulary relevant. Pressure increases through changing geometry rather than simply doubling fire rate.

### Phase 3 — hunt

- preferred range: **4.20 m**
- movement: **2.55 m/s**
- scheduler interval: **1.48 s**
- telegraph: **0.58 s**

The final phase closes again and becomes more predatory. It should feel urgent because the boss pursues and commits faster, not because the arena is permanently filled with projectiles.

## Movement contract

`FracturedSignalFirstBossV19` moves the existing authoritative kinematic boss body. It does not create another collider or enemy state machine.

The boss:

- approaches when outside its preferred band;
- backs away when crowded;
- circles inside the band and periodically changes orbit side;
- remains leashed to the authored arena center;
- probes collision before moving;
- turns toward the Guardian;
- roots itself for the complete telegraph and post-attack recovery;
- never moves while externally paused, poise-broken, or inside an SSVEP visual field.

The original `FracturedSignalDirector` remains the sole attack scheduler.

## Manual Wisp is a combat intermission

The current capture contains an important piece of game feel that should survive: holding `V` temporarily makes the sword fight disappear.

V0.19 makes that explicit through `WispCombatIntermissionV19`.

When a manual Wisp window arms:

- the boss attack scheduler pauses;
- existing projectiles freeze and stop colliding;
- new Guardian combat actions are disabled;
- ordinary movement and jump remain available;
- target selection and neural evidence are unchanged.

Combat returns only after `WindowEnded`.

The bridge remembers which authorities it personally paused. If the neural-link contingency independently enters `Degraded` or `ParticipantStopped`, the end of a Wisp window **must not** re-enable combat. The safety system remains authoritative.

This is intentionally a two-sided ceasefire, not a free time-stop damage exploit.

## Character direction — a broken signal knight

The Fractured Signal should look like something that once had purpose.

V0.19 replaces the cylinder-and-bars read with a render-only body built from:

- a faceted red **heart/core**;
- a dark **mask** with one bright fracture scar;
- split chest armor;
- asymmetric floating shoulders;
- articulated upper arms and forearms;
- one long **fracture blade** that makes the melee side legible;
- an open off-hand;
- ragged lower plates rather than legs;
- an asymmetric crown;
- a broken violet halo.

The silhouette should read at combat distance before the player can inspect the materials.

The existing boss Rigidbody, collider, vitals and schedulers remain untouched. Every V0.19 body part is renderer/mesh presentation only.

## Open-source graphics references

V0.19 deliberately borrows **technique vocabulary, not someone else's character identity**.

### `aadebdeb/ProceduralMesh` — MIT

Repository: `https://github.com/aadebdeb/ProceduralMesh`

Used as a design reference for the runtime mesh-first workflow. `OpenSourceMeshPrimitivesV19` is a Mindforge-authored implementation of the small set of geometry we need: a faceted icosahedral core, torus halo and tapered fracture shard. No package or character asset is vendored.

### `daniel-ilett/dissolve-urp` — MIT

Repository: `https://github.com/daniel-ilett/dissolve-urp`

A useful reference for a later boss-local URP fracture/dissolve transition. It is **not copied into V0.19 yet** because the first requirement is a native-Unity-compiled character and because any time-varying material behavior must be explicitly frozen around SSVEP evidence windows.

### `knowercoder/UsefulShaders` — MIT

Repository: `https://github.com/knowercoder/UsefulShaders`

Reference library for URP outline, vertex-animation and dissolve techniques. These are candidates for a later authored material pass after the V0.19 geometry/encounter is qualified.

Full-screen glitch/post effects are intentionally excluded from the first-boss neural interaction. A screen-space glitch would modulate a huge portion of the visual field and can contaminate the SSVEP measurement.

## SSVEP presentation invariant

The boss may be expressive before and after neural evidence, but it becomes a **stable background object** during calibration or an armed Wisp resonance field.

`FracturedSignalCharacterV19` therefore snaps into a neutral listening pose when the neural visual field begins and then freezes:

- idle bob;
- limb sway;
- halo rotation;
- skirt flutter;
- attack charge/release animation;
- event-driven emission changes.

`FracturedSignalFirstBossV19` independently stops root locomotion over the same field.

The Sight/Guard coded cores remain the intended time-varying retinal targets.

## Local promotion checklist

Before V0.19 leaves draft status, run **Mindforge → Latest → PLAY LATEST (BCI Simulation)** and verify:

1. The old black cylinder and magenta bar-cloud no longer dominate the boss silhouette.
2. The boss actually translates around the arena and turns toward the Guardian.
3. Phase 1 feels like a duel with breathing room rather than a projectile sprinkler.
4. Telegraphs root the boss for the entire tell and release.
5. The fracture blade/body pose makes a committed attack readable even without staring at the HUD.
6. Holding `V` stops both sword attacks and boss attacks/projectiles while preserving ordinary movement.
7. Releasing/resolving the Wisp does not resume combat early; combat returns after the Wisp window ends.
8. A degraded neural link remains paused after a Wisp window ends.
9. Boss root motion and character animation stay visually frozen throughout the neural visual field.
10. The Console contains no new exception/warning from V0.19 systems.

Static/software CI is not sufficient evidence for these visual/runtime claims. The current repository Unity workflow still skips its actual Editor test step when Unity license credentials are unavailable.
