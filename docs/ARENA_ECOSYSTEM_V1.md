# Arena Ecosystem V1

Arena Ecosystem V1 turns the Null Ward from a sparse sequence of two ordinary enemy types into a layered combat space built around the Guardian's full movement and defense vocabulary.

The design constraint remains unchanged:

> Hands own movement, targeting and combat decisions. EEG can transform bounded equipment state after accepted neural evidence, but it never creates enemy actions or player commands.

## Combat design goal

Enemy variety is useful only when each unit asks a different question. HP/color swaps do not count as new archetypes.

The current ordinary roster therefore uses one fixed-tick `JourneyEnemyController` authority with distinct data-driven attack profiles, geometry, scale and encounter roles:

| Enemy | Read | Pressure | Intended answers |
| --- | --- | --- | --- |
| Rift Hollow | tiny, low, forward knife-hound | fast close-range collapse | quick sword target switch, ground/air dash interception, jump over the low threat |
| Null Sentry | tall narrow sensor predator | tracking bolt, fan burst, retreat pulse | close distance, reflect/parry shots, move vertically rather than circle forever |
| Chrome Penitent | wide armored bruiser | fast slash, delayed heavy overhead, sweep | read timing, perfect guard, flank/space, use height against grounded melee |
| Shardsinger / Shardcaster | floating open obelisk | aimed 3D bolt while kiting | Pulse, reflected projectile, double-jump/air-dash pursuit, prioritize caster |
| Signal Warden | huge cathedral-guard mass | heavy cleave plus 3-shot ranged burst | change range/elevation, punish recovery, avoid face-tanking the elite |
| Aether Needle | narrow elevated Shardcaster variant | high-lane 3D ranged pressure | explicitly use aerial offense, reflected fire or precision ranged pressure |
| Fractured Echo | orbiting fracture node | recurring 3D projectile pressure | priority targeting, projectile defense, Flux opportunity |

No parallel arena-only combat scheduler is introduced for these ordinary enemies. Selection, cooldowns, telegraph ticks, resolution, movement and damage remain in `JourneyEnemyController.FixedUpdate()` and `EnemyAttackDefinition`.

## Encounter progression

### Synapse Causeway

Existing Null Sentries are joined by two Rift Hollows.

The Sentries create long-lane projectile pressure while the Hollows close distance. The player must switch targets, intercept a rusher, or reflect ranged pressure through the melee scrum rather than solving the room with circular strafing.

### Null Market

The existing Chrome Penitent and Fractured Echo are joined by an elevated Shardsinger.

This is a target-priority problem:

- Penitent controls immediate floor space.
- Shardsinger controls vertical/ranged space.
- Echo taxes attention and creates parry/Flux opportunities.

The player decides which source of pressure to collapse first.

### Fracture Court

A new required encounter is inserted before the Protocol Veil:

- one large Signal Warden on the floor;
- one elevated Aether Needle on the high lane.

The Warden mixes cleave and burst pressure while the Needle contests aerial safety. The intended solution is to change planes, isolate threats and collapse one envelope rather than trade into both simultaneously.

Clearing Causeway, Market and Fracture Court opens the existing Protocol Veil and preserves the Signal Cathedral / Fractured Signal boss flow.

## Readability

### Silhouette V3

`NullWardEnemySilhouetteV3Builder` replaces the generic visible capsule for every ordinary archetype with collider-free presentation geometry.

Shape is deliberately readable without emission:

- Hollow: low body, lateral knife limbs, rear spike.
- Sentry: narrow keel, crown and sensor fins.
- Penitent: broad shoulders, armor block and cleaver mass.
- Shardcaster: open vertical obelisk with orbit blades.
- Aether Needle: extremely narrow tall fork.
- Warden: broad chest, twin weapon pylons and cathedral crown.

### Honest hit volumes

`NullWardEnemyColliderProfileBuilder` tunes only the existing authoritative root `CapsuleCollider` for the newly populated ecosystem. Presentation primitives remain collider-free.

This prevents the large Warden from wearing a tiny hidden capsule and prevents the Hollow/Needle from inheriting an oversized generic target.

### Intent geometry

`JourneyEnemyIntentVfx` listens to the already-selected `EnemyAttackDefinition` and draws non-authoritative spatial intent:

- melee: actual forward arc derived from maximum range and facing angle;
- projectile: projected attack lane;
- burst: visible fan;
- retreat: inward/escape ring.

It does not select attacks, resolve hits, alter cooldowns, move actors or touch neural state.

## Arena fullness / graphics

The graphics pass avoids filling the combat floor with colliders or adding indiscriminate glow. It adds depth at three scales.

### Near scale

- Memory Forge reconstruction cradles and suspended signal fragments;
- Market archive desks, crates, signage and damaged directory hardware;
- Fracture Court pylons, crowns and fracture halo.

### Mid scale

- Causeway side towers, hanging conduits and floating plates;
- Market overhead cable network;
- Cathedral external buttresses and suspended service buses.

### Far scale

- floating cathedral fracture blades;
- fourteen low-detail distant towers beyond the Arena V3 combat radius.

Generated dressing primitives have their colliders removed immediately. Realtime accent lights do not cast shadows. Existing shared cinematic materials are reused rather than creating per-object materials.

## One-click build order

`Mindforge -> Showcase -> Build + Play Cinematic Showcase` now deterministically runs:

1. Arena Environment V3
2. base Null Ward world
3. Arena Ecosystem V1 gameplay population
4. honest enemy collider profiles
5. Enemy Silhouette V3
6. existing Null Ward Visual Infrastructure V2
7. Arena Set Dressing V3
8. traversal playability layer
9. competition gate validation
10. presentation budget audit
11. Play Mode

This order matters. Gameplay actors exist before presentation is layered onto them, and qualification runs after the complete scene is assembled.

## BCI boundary

Arena Ecosystem V1 does not modify:

- `VepAuraStimulus` frequency or luminance;
- accepted Sight/Guard state;
- decoder evidence;
- calibration state;
- coded gaze-target timing;
- the Wisp-to-coded-target separation.

Enemy telegraphs and environment graphics are ordinary game presentation. They must still be checked in real calibrated play for visual competition with the 10/12 Hz gaze targets, but they never become neural evidence or stimulus authority.

## Unity qualification checklist

Static source/software gates cannot replace this playtest.

### Combat

- Causeway: Sentries remain dangerous while Hollows close; no enemy spawns inside geometry.
- Market: Shardsinger is reachable with double jump/air dash and target-lock aim remains sensible at elevation.
- Fracture Court: Warden + Needle create two distinct threat envelopes rather than unavoidable overlap.
- Every enemy can be sword-hit where its visible body suggests.
- No enemy can damage the Guardian through obviously false vertical melee contact.
- Projectile parry/perfect guard remain readable in mixed groups.
- Target cycling works with 3+ enemies and never selects inactive future-room actors.
- Memory Forge reconstruction restores every new encounter deterministically.
- Protocol Veil requires all intended required zones.

### Movement / camera

- Double jump, hover and air dash remain comfortable among taller set dressing.
- Camera collision does not catch on collider-free decoration.
- Aether Needle and Shardsinger can be fought without camera pitch becoming disorienting.
- Wisp movement does not repeatedly cross target/telegraph sight lines.

### Presentation

- Hollow, Sentry, Penitent, Shardcaster/Needle and Warden are distinguishable by silhouette before reading color.
- Telegraph arcs/fans read as intent rather than extra HUD noise.
- Fracture Court has a clear combat center.
- Market remains navigable despite higher visual density.
- distant arena towers add depth without pulling visual salience away from boss/player threats.
- no visible generic capsule bodies remain on ordinary enemies.

### Performance

Review `experiments/reports/presentation-budget-latest.json` after the editor build and `presentation-runtime-latest.json` from the controller-only development play session.

Do not call the graphics pass performance-qualified until target Unity evidence confirms acceptable main-thread p95, batches/draw calls, triangles and GC behavior. Static batching flags and collider-free primitives are preparation, not a performance claim.

### BCI

After controller-only visual/feel qualification, re-run physical 10/12 Hz stimulus timing and gaze/readability validation. Added architecture, enemy emission and telegraphs must not make the coded targets harder to acquire or alter their physical display timing.
