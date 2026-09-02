# Mindforge V0.32 Showcase Vertical Slice

V0.32 turns the Mac-working Dragon Souls chassis into one deliberately paced Mindforge chapter rather than expanding the game in every direction at once.

The target is a 10–15 minute portfolio-quality run with this rhythm:

`QUIET -> DISCOVERY -> LEARNING -> FIRST THREAT -> NEURAL REVEAL -> TRAVERSAL -> ELITE TEST -> BOSS -> WORLD REVEAL`

## Current implementation

The branch `feat/v32-showcase-intro` already provides:

- V0.31 signed world-bounds route-placement fix;
- `MindforgeShowcaseFlowV32`, the single chapter progression observer;
- `MindforgeShowcaseStageCheckpointV32`, collider-free route checkpoints;
- `MindforgeShowcaseIntroBuilderV32`, which copies the V0.31 scene into `Assets/Mindforge/Scenes/MindforgeShowcaseIntroV32.unity`;
- nine deterministic route checkpoints from Memory Forge to boss entry;
- BCI orb hidden during awakening and revealed at the BCI beat;
- sword swing/hit milestone integration through the read-only V0.31 combat assurance component;
- `MindforgeEncounterLibraryV32` authored encounter recipes;
- `MindforgeWorldGrammarV32` region/chunk/socket scale vocabulary;
- `MindforgeShowcaseReadinessV32` native audit.

The V0.32 flow never opens a sword hitbox, changes damage, moves the player, or changes Dragon Souls combat state.

## Chapter beats

| Beat | Target time | Player learns / experiences | Implementation owner |
| --- | ---: | --- | --- |
| Awakening | 0:00–1:00 | move, camera, world scale | V0.32 flow + environment |
| Memory Forge | 1:00–2:00 | objective, Aetherblade significance | presentation + forge interaction |
| Blade Training | 2:00–3:30 | LMB combo, RMB heavy | inherited combat + tutorial presentation |
| First Encounter | 3:30–5:00 | target lock, roll, two-role pressure | encounter recipe |
| BCI Reveal | 5:00–6:00 | Sight/Guard/Concord orb | BCI presentation |
| Sight Puzzle | 6:00–7:30 | neural information has gameplay meaning | semantic intent adapter |
| Traversal | 7:30–9:00 | vertical exploration + optional secret | authored chunk composition |
| Elite Encounter | 9:00–10:30 | combine combat systems under pressure | encounter recipe |
| Boss Approach | 10:30–11:30 | decompression + visual framing | environment/audio/presentation |
| Boss Fight | 11:30–14:30 | mastery | inherited boss chassis + Mindforge mechanics |
| World Reveal | 14:30–15:00 | reward + future-region vista | progression/world graph |

## Stage semantics

The chapter flow is monotonic. Route arrival may advance presentation stage, but combat evidence is tracked separately as milestones. This avoids pretending the player learned a mechanic merely because they walked past a coordinate.

Tracked milestones currently include:

- first animation-driven sword window;
- first real sword damage contact;
- BCI orb reveal;
- future target-lock combat evidence;
- future dodge evidence;
- future boss entry;
- future boss defeat.

## Spatial targets

V0.32 inherits the scale contracts introduced by the production-world work:

- ordinary corridor: at least 8 m clear width;
- combat hall: at least 14 m clear width;
- boss arena: at least 32 m clear diameter;
- no rendered solid boundary without corresponding usable collision;
- no random primitive scenery;
- no semantic checkpoint collider or hidden blocking geometry.

## Intro environment target

The opening should read as an enclosed neural cathedral / cavern reliquary, not an exposed field.

Required composition:

- complete roof or cavern ceiling;
- visible floor/wall/ceiling junctions;
- strong distant Memory Forge landmark;
- sparse foreground occluders;
- readable midground traversal corridor;
- distant architectural silhouette;
- white/grey stone as the dominant mass;
- cyan as neural information;
- magenta reserved for corruption/threat;
- gold used sparingly for sacred/structural accents.

## First encounter recipe

`showcase.first_real_encounter` contains:

- one Remnant melee pressure role;
- one Ranger support role;
- at most one simultaneous committed attacker;
- at least 3.2 m intended player breathing room;
- delayed support activation rather than instant dogpile.

The recipe does not spawn anything itself. A later socket resolver maps roles to qualified local Dragon Souls enemy rigs.

## Elite encounter recipe

`showcase.elite_encounter` contains:

- Brute anchor;
- Stalker flanker;
- Resonant second-wave controller;
- optional Ranger reinforcement;
- at most two simultaneous committed attackers;
- at least 3.6 m intended player breathing room.

## BCI reveal

The V0.31 orb remains the visual substrate:

- Sight: requested 8 Hz;
- Guard: requested 10 Hz;
- Concord: requested 12 Hz;
- reduced-contrast default;
- B toggles temporal modulation only.

V0.32 hides the orb during Awakening and reveals it on reaching `BciReveal`.

Requested frequencies remain simulation targets, not measured optical-frequency claims.

## Promotion order

1. Native V0.31 rebuild must clear the boundary-placement failure.
2. V0.32 must compile in Unity 2021.3.20f1.
3. Showcase scene must build with nine collider-free checkpoints.
4. Player, sword, combat controller, enemy population, boss and NavMesh must remain present.
5. At least one sword swing and one real sword hit must be observed in Play Mode.
6. BCI orb must be hidden before BCI Reveal and visible after it.
7. Only then should encounter spawning, Sight puzzle authority, boss reward and region transitions become progression-critical.

This sequencing keeps the showcase chapter buildable at every step instead of becoming a long-lived broken branch.
