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

## Unity workflow

Use Unity `2021.3.20f1` for the materialized Dragon Souls project.

1. Run `bash tools/bootstrap_dragonsouls_chassis.sh` from the Mindforge repository root.
2. Open `external/DragonSouls-Unity3D/ThirdPersonCombat` in Unity 2021.3.20f1.
3. Run `Mindforge -> World V0.30 -> Build + Open Production World`.
4. Run `Mindforge -> World V0.30 -> PLAY PRODUCTION WORLD`.
5. Test ordinary traversal, target lock, attacks, dodge, heal, sword throw/recall, bonfire/progression flow, multiple enemy encounters and dragon entry.
6. While in Play Mode run `Mindforge -> World V0.30 -> Audit Production World`.

The runtime audit must observe a baked NavMesh, one player, one authoritative sword, Cinemachine cameras, standard enemies, the boss pipeline, the V0.30 presentation root and at least one enemy identity component.

## What comes next

After native V0.30 play is observed, the next work should be regional rather than global. Pick one visually important path from spawn to a meaningful encounter, measure it, widen only the places that constrain combat, replace weak set dressing with licensed/known-safe authored modules, rebake NavMesh, and qualify that region before moving to the next. This prevents another world-wide pile of loosely connected visual patches.
