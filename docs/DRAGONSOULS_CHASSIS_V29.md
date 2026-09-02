# V0.29 — Dragon Souls playable chassis

## Decision

Mindforge V0.29 stops treating the current procedural Unity scene as the only route to a playable combat demo.

The public project **btuhany/DragonSouls-Unity3D** becomes a pinned, local, complete third-person action-game chassis. Mindforge's existing Unity project remains the neuroscience/world-vision/reference implementation, but new production gameplay work should first prove itself in the Dragon Souls chassis when the equivalent system already works there.

This is a speed and quality decision, not a change to the Mindforge concept.

## Why this chassis

The pinned upstream game already contains the systems that have consumed a disproportionate amount of Mindforge iteration time:

- third-person locomotion and camera behavior;
- target-based sword combat;
- dodge roll, sprint, healing and stamina;
- sword throw/recall and unarmed fallback;
- enemy targeting;
- player state-machine architecture;
- behavior-tree AI tooling;
- multiple distinct enemy archetypes;
- a large dragon boss encounter;
- enemy respawn;
- bonfire/rest/fast-travel loops;
- souls/progression;
- reusable-object pooling;
- authored character/enemy animation integration;
- a complete main game scene and main menu.

The goal is to inherit those proven interaction loops and spend Mindforge effort on the parts that actually make Mindforge unique: world identity, neural interaction, encounter design, character identity, and signal-driven mechanics.

## Pinned upstream contract

Repository:

`https://github.com/btuhany/DragonSouls-Unity3D`

Commit:

`f54824255517801d5d3443848e1e4275d8d5066d`

Unity project:

`ThirdPersonCombat`

Known upstream editor:

`2021.3.20f1`

The first qualification run **must not upgrade Unity, URP, Cinemachine, Input System, or serialized project data**. We first establish that the pinned game still plays on the user's machine. Modernization comes after that baseline is captured.

## One-command bootstrap

From the Mindforge repository root:

```bash
bash tools/bootstrap_dragonsouls_chassis.sh
```

This materializes the exact upstream commit into:

`external/DragonSouls-Unity3D/ThirdPersonCombat`

The external checkout is intentionally git-ignored.

The bootstrap verifies:

1. exact upstream commit;
2. upstream MIT license notice exists;
3. Unity editor version is exactly `2021.3.20f1`;
4. the tracked Mindforge overlay is copied only beneath `Assets/Mindforge`;
5. a local provenance record is written.

Open the resulting project with Unity 2021.3.20f1 and run:

**Mindforge → Chassis → PLAY MAIN GAME**

## Authority migration map

### Adopt as the initial production chassis

These upstream concepts become the default starting point for the V0.29 production track:

| Need | Dragon Souls foundation | Mindforge treatment |
| --- | --- | --- |
| locomotion | player state machine + free/target states | retune speed/acceleration; preserve stable animation coupling |
| lock-on combat | target combat states | adapt input semantics and target presentation |
| dodge | roll state | retune toward Mindforge dodge/air-capability vision |
| sword combat | sword free/target states | replace sword presentation with Aetherblade/lightsaber identity |
| stamina | existing movement/combat economy | map to Guardian endurance semantics |
| health/death | existing player health/death loop | restyle HUD/checkpoint meaning |
| enemy AI | state machine + behavior trees | use as encounter framework, author new Mindforge behaviors |
| boss framework | dragon boss + boss trigger/manager | use as boss-production template, not final creature identity |
| pooling | projectile/effect pools | keep and extend for Mindforge combat FX |
| rest/respawn | bonfire/checkpoint loop | reinterpret as Memory Forge |

### Preserve from Mindforge

The following remain Mindforge-owned and are ported into the chassis behind adapters rather than replaced by generic Souls systems:

- neural decision semantics and evidence boundaries;
- Sight / Guard / Concord interaction language;
- calibration and Wisp semantics;
- no-uncontrolled-stimulus constraints during neural evidence windows;
- deterministic neural replay/qualification tooling;
- Mindforge world/lore identity;
- Aetherblade visual identity;
- BCI simulation vs physical-hardware qualification separation.

`MindforgeIntentBusV29` is the first chassis-safe seam for this work. It has zero movement/combat authority in the initial tranche.

## World-production strategy

The Dragon Souls map is a **functional staging world**, not the final Mindforge world.

We should exploit its working scale, camera, NavMesh, enemy spacing, terrain readability and combat lanes while replacing the visual world in large coherent chunks.

The order is:

1. **prove baseline playability** with untouched upstream layout;
2. **make the player read professionally** using the upstream animated character pipeline and an Aetherblade replacement;
3. **create one production-quality Mindforge combat arena** inside a broad, correctly scaled section of the existing level;
4. **replace surrounding environment with a coherent realistic cathedral/cavern kit**, preserving generous traversal widths and definite boundaries;
5. **replace enemy art one archetype at a time** while retaining proven AI contracts;
6. **build a signature Mindforge boss** by adapting the dragon boss behavior-tree/phase machinery to a newly licensed or project-authored creature;
7. only then expand the final world outward.

This avoids another cycle where a huge procedural world exists before one polished combat room does.

## Spacing and geometry rules

V0.29 adopts environment-scale rules before art replacement:

- primary combat hall clear width: **>= 14 m**;
- ordinary traversal corridor clear width: **>= 8 m**;
- no decorative prop inside a **2.0 m** shoulder zone around primary paths;
- boss arena clear diameter: **>= 32 m** before peripheral scenery;
- camera collision geometry must use explicit static colliders rather than invisible presentation tricks;
- every visually solid wall/floor/column intended as a boundary must have an intentional matching collision owner;
- no floating decorative geometry in the gameplay-camera corridor;
- no renderer-only object may masquerade as a traversal boundary;
- no invisible collision may define major architecture without a matching visible surface;
- environment dressing follows wall/alcove sockets, not random scatter.

These values are starting contracts and should be adjusted from actual gameplay recordings, not silently eroded by later decorators.

## Boss/enemy direction

Do not copy the upstream dragon as the final Mindforge boss.

Instead, copy the *production machinery*:

- authored skeletal animation;
- boss-sized collision and hurt volumes;
- phase-aware AI;
- behavior-tree sequencing;
- telegraph/attack separation;
- projectile pooling;
- arena trigger/health presentation;
- death/reward lifecycle.

Then build the Mindforge boss as a distinct animalistic corrupted organism using art with explicit redistribution rights. The same principle applies to standard enemies: keep reliable AI/combat integration, replace silhouette/material/lore.

## Licensing boundary

The Dragon Souls repository itself is MIT, which gives a clear basis for using and adapting the upstream-authored software.

However, its README states that the game combines many external free assets and separately attributed music/sound sources. Therefore Mindforge does **not** infer that every binary model, animation, texture, sound, or Asset Store package in the repository is MIT merely because it is stored in the MIT repository.

Policy:

- the complete upstream checkout stays under git-ignored `external/`;
- upstream-authored source code may be adapted with the MIT notice retained;
- a third-party asset moves into distributable Mindforge only after its original license is identified and recorded;
- ambiguous assets remain local placeholders and are replaced by audited CC0/CC-BY/MIT-compatible/project-authored alternatives;
- music/audio attribution is handled independently from code licensing.

## V0.29 promotion gates

### P0 — repository contracts

- bootstrap pins immutable upstream commit;
- overlay cannot write outside `Assets/Mindforge`;
- license/provenance records exist;
- Python/shell source contracts pass.

### P1 — untouched chassis boots

On the M1 Max MacBook Pro with Unity 2021.3.20f1:

- project imports without source modifications;
- MainGameScene opens;
- player can move, target, attack, roll, heal and die;
- at least one normal enemy can be defeated;
- boss encounter can be entered;
- no pink/missing material catastrophe dominates the baseline.

### P2 — Mindforge presentation prototype

- Aetherblade presentation replaces ordinary sword presentation;
- one Mindforge HUD treatment is active;
- one production arena section has Mindforge material/lighting identity;
- baseline combat remains intact.

### P3 — Mindforge encounter prototype

- one enemy archetype is re-skinned/re-authored;
- one Mindforge boss prototype runs on the proven boss/behavior infrastructure;
- BCI simulation can publish Sight/Guard/Concord intent without violating combat ownership.

## What not to do

- do not merge the two Unity projects asset-by-asset;
- do not upgrade Dragon Souls to Unity 2022/Unity 6 before baseline qualification;
- do not import all upstream art into public Mindforge and assume the repo MIT license covers it;
- do not rewrite proven camera/combat systems merely to preserve current Mindforge implementation history;
- do not expand world size before one polished, readable combat slice exists;
- do not let generated/procedural geometry remain the primary visual language for characters or signature bosses.

V0.29 is successful when we can play a competent complete action game immediately and then replace its identity layer by layer without losing functionality.
