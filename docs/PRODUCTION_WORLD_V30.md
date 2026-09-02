# V0.30 Production Combat World

V0.30 turns the Dragon Souls chassis from a useful combat dependency into the default world-production foundation for Mindforge.

## Canonical world source

The source is the pinned Dragon Souls `Assets/Levels/Scenes/MainGameScene.unity`, not the smaller gameplay test scene and not the historical procedural Mindforge cathedral. The builder copies that complete scene to:

`Assets/Mindforge/Scenes/MindforgeWorldV30.unity`

The source scene is never edited in place. Rebuilding with refresh deletes only the Mindforge-owned copy and recreates it from the pinned upstream source.

This matters because the full scene already carries authored modular architecture, terrain, baked lighting, occlusion data, baked NavMesh, collision, enemy placement, cameras, bonfires/progression and the dragon boss pipeline. V0.30 inherits those systems instead of attempting to recreate them.

## Presentation-only authority

The builder adds one new root, `Mindforge_Production_World_V30`. Everything below this root is presentation-only. It may contain lights, post-processing and visual identity components. It must contain zero colliders, rigidbodies or character controllers.

V0.30 does not move, rescale, delete or replace inherited gameplay geometry. It does not alter player movement, sword hit authority, enemy AI, health, damage, NavMesh agents, respawn logic, bonfires or boss behavior.

The first presentation pass is deliberately restrained:

- preserve the upstream skybox and fog model while shifting the atmosphere toward Mindforge's cool neural-stone palette;
- add ACES tonemapping, modest bloom, restrained contrast and a light vignette;
- tint only single-material static environment MeshRenderers whose hierarchy clearly identifies them as walls, terrain, ruins, rocks, pillars, paths, stairs or related level architecture;
- preserve original textures and source materials by using `MaterialPropertyBlock` at runtime instead of duplicating or mutating third-party assets;
- add two subtle spawn-region fill lights and two boss-region neural/corruption lights;
- add visual-only enemy presentation components to the existing standard `EnemyStateMachine` roots while leaving every combat state untouched;
- keep the existing V0.29 Aetherblade and dragon presentation layers intact.

## World-space contract

The production target remains:

- primary combat hall clear width >= 14 m;
- ordinary traversal corridor clear width >= 8 m;
- decorative shoulder exclusion >= 2 m on primary paths;
- boss arena clear diameter >= 32 m;
- every visually solid major wall/floor/column must correspond to intentional collision;
- no floating dressing in the gameplay-camera corridor;
- no invisible major architecture;
- no random scatter as a substitute for authored environment composition.

V0.30 does not claim the inherited Dragon Souls level already satisfies every one of those measurements. The point of this tranche is to establish a complete functioning world first while freezing collision/navigation authority. Once local play confirms the baseline, individual regions can be widened and rebuilt deliberately inside the Mindforge-owned copy with NavMesh rebakes and explicit traversal tests.

### Measured traversal audit

`MindforgeWorldGeometryAuditV30` provides a read-only ruler before any region is edited. It first anchors the player and dragon to nearby points on the inherited baked NavMesh, which makes the route test robust even when the visual dragon transform is offset from traversable ground. It then calculates that anchored route, samples it every 2 m, and casts horizontal collision probes to measure real left/right clearance. Samples below the 8 m ordinary-corridor target are counted as choke points, and the report records the exact position of the narrowest sample.

The same audit casts 24 radial probes around the dragon's NavMesh anchor and requires at least a 16 m clear radius for the 32 m boss-arena target. Its probe range is 20 m, so the acceptance target is actually observable rather than mathematically capped below the requirement. The report records both the minimum radius and the limiting probe angle.

It also reports large collider objects with no renderer on the collider object or its children as **review candidates** for the invisible-obstacle problem. Those candidates are intentionally not an automatic failure because some authored boundary/collision helpers are legitimate.

The geometry audit never moves scene transforms, adds physics, rebakes navigation or edits colliders. Its purpose is to tell the next regional rebuild exactly where to look.

### Qualification export

`MindforgeWorldQualificationExporterV30` combines the native readiness and geometry audits into one local JSON report. It records Unity version, active scene, Play Mode state, readiness pass/fail/deferred counts, failed check IDs, NavMesh anchoring, path status, narrowest-path position, boss clearance and invisible-collider candidate count.

Reports are written to `MindforgeReports/` at the root of the materialized Dragon Souls Unity project, outside `Assets`. They are local evidence only and are not copied into the tracked Mindforge overlay.

## Unity workflow

Use Unity `2021.3.20f1` for the materialized Dragon Souls project.

1. Run `bash tools/bootstrap_dragonsouls_chassis.sh` from the Mindforge repository root.
2. Open `external/DragonSouls-Unity3D/ThirdPersonCombat` in Unity 2021.3.20f1.
3. Run `Mindforge -> World V0.30 -> Build + Open Production World`.
4. Before Play Mode, run `Mindforge -> World V0.30 -> Audit Traversal Geometry` and note the narrowest-path position, minimum width and boss radius.
5. Run `Mindforge -> World V0.30 -> PLAY PRODUCTION WORLD`.
6. Test ordinary traversal, target lock, attacks, dodge, heal, sword throw/recall, bonfire/progression flow, multiple enemy encounters and dragon entry.
7. While in Play Mode run `Mindforge -> World V0.30 -> Audit Production World` and then `Audit Traversal Geometry` again.
8. Run `Mindforge -> World V0.30 -> Export Qualification Report` and share the generated `v30-world-qualification-*.json` together with a short gameplay capture.

The runtime audit must observe a baked NavMesh, one player, one authoritative sword, Cinemachine cameras, standard enemies, bonfire progression, the boss pipeline, the V0.30 presentation root and at least one enemy identity component.

## What comes next

After native V0.30 play is observed, the next work should be regional rather than global. Pick one visually important path from spawn to a meaningful encounter, use the measured choke coordinates to identify where combat space actually fails, widen only those places, replace weak set dressing with licensed/known-safe authored modules, rebake NavMesh, and qualify that region before moving to the next. This prevents another world-wide pile of loosely connected visual patches.
