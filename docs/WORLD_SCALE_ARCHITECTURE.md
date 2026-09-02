# Mindforge World Scale Architecture

Mindforge should feel enormous without becoming one monolithic Unity scene or a procedural landscape generator.

The world hierarchy is:

`WORLD -> REGION -> CHUNK -> SOCKET`

## World graph

The first six semantic regions are defined in `MindforgeWorldGrammarV32`:

- Sanctum Reliquary
- Neural Cloister
- Fracture Caverns
- Memory Gardens
- Signal Foundry
- Abyssal Archive

The V0.32 showcase is primarily Sanctum + Neural Cloister, ending with a vista that previews later regions.

## Chunk vocabulary

All regions compose from a deliberately small chunk vocabulary:

- Entry
- Hub
- Corridor
- Vertical
- ArenaSmall
- ArenaMedium
- Boss
- Vista
- Puzzle
- Shrine
- Secret
- Transition

A chunk is a meaningful playable space, not a decorative prefab.

A production chunk should eventually own:

- stable world ID;
- region ID;
- chunk kind;
- traversal floor and visible enclosure;
- ceiling / roof where appropriate;
- exit sockets;
- encounter sockets;
- loot sockets;
- shrine sockets;
- landmark sockets;
- lighting anchors;
- audio-zone metadata;
- NavMesh surface/data;
- persistence namespace;
- performance budget metadata.

## Socket vocabulary

The stable semantic socket kinds are:

- Exit
- Enemy
- Loot
- Shrine
- Landmark
- LightingKey
- LightingFill
- AudioZone

Sockets are authored. Dressing may vary downstream, but gameplay-critical topology is never produced by arbitrary `Random.Range` placement.

## Composition ratio

Target:

- 70% authored macro-layout;
- 30% deterministic dressing.

Authored macro-layout decides:

- traversal topology;
- combat space dimensions;
- vertical movement;
- landmarks;
- major sight lines;
- boss approaches;
- puzzle topology;
- shortcuts and gates;
- safe recovery spaces.

Deterministic dressing may vary:

- rubble;
- banners;
- torches;
- vegetation;
- small furniture;
- ambient particles;
- minor decals;
- non-gameplay prop arrangements.

## Scale contracts

`MindforgeWorldGrammarV32` currently defines:

- minimum general corridor width: 8 m;
- minimum combat hall width: 14 m;
- minimum boss arena diameter: 32 m.

These are minimum clear traversable dimensions, not center-to-center prefab spacing.

## Boundary contract

Every visually solid large boundary must have corresponding usable non-trigger collision unless it is explicitly decorative and unreachable.

Do not create:

- invisible blocking walls with no visual surface;
- one-sided floors that disappear from camera angles;
- floating wall modules;
- walls whose collider lives tens of meters away from the renderer;
- giant primitive blockers used to disguise incomplete world edges.

The V0.31 `AlignBoundaryBoundsToLane` fix exists because imported prefab roots cannot be assumed to coincide with visible geometry. Future chunk placement must use actual renderer/collider bounds or explicit authored sockets.

## Additive-scene target architecture

The long-term world should load region chunks additively rather than keeping every region active in one scene.

Suggested hierarchy:

- persistent bootstrap scene
  - player
  - cameras
  - global UI
  - BCI/neurOS bridge
  - save/progression services
  - audio services
- region root scene
  - regional lighting
  - fog / volume
  - region-specific materials
  - global vista proxies
- active chunk scenes
  - geometry
  - NavMesh
  - encounters
  - local audio
  - local interactables

The world graph decides which chunks must be loaded around the player. Distant vista geometry should be lightweight proxies, not live gameplay chunks.

## Stable IDs and persistence

Anything whose state matters after leaving/reloading a region needs a stable authored ID:

- gate
- shortcut
- shrine
- pickup
- chest
- boss
- one-time reward
- encounter boundary
- region discovery
- lore object

Runtime instance IDs or hierarchy paths are not persistence identifiers.

## Region identity

### Sanctum Reliquary

Dominant mass: pale stone / silver.
Accent: cyan neural light.
Threat: restrained magenta corruption.
Composition: symmetry, processional axes, sacred negative space.

### Neural Cloister

Dominant mass: weathered pale stone.
Accent: cyan signal traces.
Threat: corruption beginning to intrude.
Composition: repeated bays, side chambers, hidden Sight information.

### Fracture Caverns

Dominant mass: dark basalt / broken architecture.
Accent: cool neural remnants.
Threat: stronger magenta corruption.
Composition: vertical shafts, broken bridges, deep negative space.

### Memory Gardens

Dominant mass: ruins + living material.
Accent: cyan/gold memory traces.
Composition: more open vertical space, vegetation, hidden routes.

### Signal Foundry

Dominant mass: ceramic / metal / monumental machinery.
Accent: directed energy conduits.
Composition: mechanical rhythm, strong directional flow.

### Abyssal Archive

Dominant mass: dark monumental architecture.
Accent: deep blue/violet memory structures.
Composition: enormous negative spaces, sparse geometry, distant scale.

## Performance budgets

Initial target hardware:

- M1 Max MacBook Pro
- RTX 3070 Ti-class PC

Target showcase performance: stable 60 FPS.

Per chunk, track:

- renderer count;
- triangle count;
- shadow casters;
- real-time lights;
- particle count;
- NavMesh agents;
- physics contacts;
- CPU frame time;
- GPU frame time;
- GC allocations.

Do not optimize empty scenes aggressively. Measure the real showcase first, then establish budgets from observed frame cost.
