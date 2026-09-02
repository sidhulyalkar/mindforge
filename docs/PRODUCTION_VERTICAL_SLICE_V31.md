# V0.31 Production Vertical Slice

V0.31 is the first tranche that treats the Mac-qualified Dragon Souls build as a real production chassis rather than a temporary combat test.

The starting point is the post-merge V0.30 fix head `6bd027ac123d134a8dd0915bd9f819a5221dcc9d`, which compiled and ran natively in Unity 2021.3.20f1 on the user's Mac after the `PlayerStateMachine` namespace fix.

## Product objective

The slice should stop reading as a grassy student-project arena with oversized black blockers and overlapping red enemies. The immediate target is a professional-looking, mechanically stable five-to-ten-minute combat route that can be iterated visually without rewriting the underlying action game.

The intended long-form route remains:

`Memory Forge Sanctuary -> Cathedral Processional Hall -> Corrupted Courtyard -> Neural Cloister -> Descent Cavern -> Fractured Signal Arena`

V0.31 does not claim all six authored regions are finished. It creates the production systems and first deterministic route framing needed to build them safely.

## Chassis authority

Dragon Souls keeps ownership of:

- player state machine and locomotion;
- target/free combat states;
- sword hit and throw/recall authority;
- health, death, healing and stamina;
- enemy attack states and CharacterController motion;
- bonfire/rest/progression loops;
- boss activation, behavior and rewards;
- the inherited baked NavMesh and full-world scene graph.

Mindforge V0.31 adds downstream presentation and approach-spacing behavior around those systems.

## Production camera

`MindforgeProductionCameraV31` retunes the existing Cinemachine camera graph instead of creating a second camera.

Normal traversal becomes lower and closer, with the player framed slightly off-center rather than as a small figure near the bottom of a distant tactical view. Free-look orbit radii are reduced, the near clip plane is tightened, damping/dead zones are reduced, and Cinemachine collision gets a larger camera radius plus faster recovery.

Target framing adapts to combat context:

- base target FOV: 50 degrees;
- three or more enemies within 8.5 m: widen toward 55 degrees;
- dragon within 20 m: widen toward at least 57 degrees.

This is presentation only. It does not move the player, enemies or target transforms.

## Enemy formation and silhouette protection

The original chase state repeatedly sends every standard enemy NavMeshAgent to the player's exact position. That behavior contributed directly to the red-enemy pile seen in the native recording.

`MindforgeEnemyFormationV31` leaves the upstream chase/attack state machine intact but gives active chase agents stable approach slots around the player:

- ordinary melee: ~3.45 m ring;
- heavy: ~4.15 m;
- ranged: ~6.25 m;
- caster: ~7.10 m;
- twelve deterministic angular slots;
- high-quality NavMesh obstacle avoidance with varied priority;
- once an enemy gets inside ~2.45 m, V0.31 releases the destination override so normal attack behavior owns close combat.

It never calls CharacterController.Move, changes attack state, changes damage or teleports actors.

## Combat punctuation

`MindforgeCombatFeedbackV31` subscribes to the existing `Combat.Health` events. Damage produces a short bounded renderer flash and compact neural spark burst at the existing hit point. Death produces a larger visual burst.

It does not deal damage, change hitstop/timeScale, apply force or alter animation state. Those are future tuning surfaces once native feel is observed.

## HUD

`MindforgeHudPresentationV31` restyles inherited gameplay Sliders and high-value HUD labels while leaving existing UI scripts in charge of values and visibility.

The palette is:

- health: restrained magenta/red;
- stamina: cyan/teal;
- boss: corruption magenta;
- neutral meters/text: cold desaturated blue-white;
- backgrounds: near-black blue with high transparency.

## World look

`MindforgeVerticalSliceRuntimeV31` reduces the visual dominance of the original grassy terrain rather than deleting functioning terrain geometry:

- terrain detail density is capped at 0.22;
- detail draw distance is capped at 18 m;
- tree distance is capped at 190 m;
- atmospheric fog shifts to deep blue-grey;
- ACES, stronger contrast, lower saturation and cool white balance establish foreground/midground/background separation;
- existing V0.30 and upstream materials remain intact rather than being globally replaced.

## Authored route boundaries

`MindforgeVerticalSliceBuilderV31` never edits the Mac-working V0.30 scene in place.

It reconstructs V0.30 first and copies that scene to:

`Assets/Mindforge/Scenes/MindforgeVerticalSliceV31.unity`

The builder then calculates the actual inherited baked NavMesh path from the player to the dragon and places only five deterministic paired boundary stations along that path.

World-space invariants:

- protected combat half-width: 7 m, therefore 14 m clear route width;
- boss exclusion radius: 20 m;
- solid-module hard cap: 12;
- no random placement;
- no Unity primitive scenery;
- every boundary must be an authored prefab with an enabled, non-trigger, physically meaningful Collider;
- each solid module is grounded using its real renderer/collider bounds and a downward world-collision query;
- the inner edge of every new boundary is measured after grounding and must remain outside the protected route.

The preferred early-route module is the locally available `Metal_Wall_With_Pillars.prefab`. If its inherited collider graph cannot prove a usable boundary, the builder automatically destroys that instance and falls back to the known-good `Rock_Wall.prefab`, which carries a visible mesh and matching non-trigger MeshCollider.

This tranche intentionally does not rebake navigation. New solid stations are kept outside the 14 m protected lane so the inherited route stays authoritative until native testing shows where actual regional geometry surgery is warranted.

## Third-party art boundary

The Inguz Media Studio prefabs referenced above already exist inside the pinned, git-ignored Dragon Souls checkout. V0.31 references them **only as local upstream prototype assets**.

Mindforge does not copy those art files into its tracked overlay and does not infer redistribution permission from Dragon Souls' MIT repository license. Before a public distributable build, each retained third-party art family must have its original license independently verified or be replaced with audited CC0/CC-BY/MIT-compatible/project-authored art.

The architectural system is intentionally prefab-agnostic so replacing the prototype modules later does not require rewriting traversal logic.

## Native Unity workflow

Use Unity `2021.3.20f1`.

From the Mindforge repository root:

```bash
git fetch origin
git checkout feat/v31-production-vertical-slice
git pull --ff-only origin feat/v31-production-vertical-slice
bash tools/bootstrap_dragonsouls_chassis.sh
```

Do not pass `--refresh`; the existing Dragon Souls checkout can be reused.

Back in Unity, allow scripts to recompile, then run:

1. `Mindforge -> World V0.31 -> Build + Open Vertical Slice`
2. clear the Console and confirm zero compile errors;
3. `Mindforge -> World V0.31 -> PLAY VERTICAL SLICE`
4. test ordinary traversal and camera orbit;
5. engage one enemy, then three or more enemies;
6. verify enemies approach from distinct lanes instead of occupying one body-sized point;
7. verify sword hits produce compact flashes/sparks but damage behavior remains unchanged;
8. verify HUD values still update correctly;
9. traverse past multiple authored boundary stations and inspect ground contact/collision from both sides;
10. run `Mindforge -> World V0.31 -> Audit Vertical Slice` while in Play Mode.

The most valuable capture is 2-4 minutes showing traversal camera, a 3+ enemy fight, one death/respawn, wall/collider inspection and the boss approach.

## Native acceptance gate

V0.31 should not merge merely because Python source contracts pass. Native Unity must prove:

- zero compile errors in 2021.3.20f1;
- no second/competing camera;
- player remains visible and meaningfully larger in frame;
- crowd FOV does not oscillate unpleasantly;
- enemy formation improves readability without breaking attacks or pathfinding;
- no new boundary floats above or sinks badly below terrain;
- no new solid boundary intrudes into the 14 m protected lane;
- visible solid modules have matching collision and no obvious invisible extension;
- grass/detail reduction improves architectural readability without turning terrain into a barren LOD artifact;
- hit sparks do not obscure targets;
- HUD remains functional;
- bonfire, death/respawn, sword throw/recall and dragon encounter still work.

After that evidence, V0.31.x can begin true regional geometry replacement and NavMesh rebakes rather than merely framing the inherited route.
