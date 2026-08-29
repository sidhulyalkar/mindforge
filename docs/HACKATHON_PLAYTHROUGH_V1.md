# Hackathon Playthrough V1

This tranche turns the qualified Aetheria V2 systems into one coherent judge-facing playthrough while preserving the authority boundaries needed for a much larger game later.

## Product goal

A judge should be able to launch one scene and immediately experience:

1. a readable Prism Bastion arrival;
2. a cathedral-scale Neon Causeway with optional Prism hoverbike traversal;
3. a denser Market of Broken Momentum;
4. the vertical silhouette of the Ruined Choir;
5. the Hall of Excessive Gravitas approach;
6. a large ten-enemy Menagerie Crucible encounter;
7. the Lord Malatract confrontation after the ordinary-enemy combat grammar has been demonstrated.

The slice should feel authored rather than generated, but it must remain honest about what is gameplay authority and what is presentation.

## World graphics contract

`HackathonPlaythroughV1Builder` is a deterministic editor composition pass layered on top of the existing collision-qualified Grounded World and Aetheria scene.

It adds only collider-free world-detail geometry:

- Prism Bastion arrival buttresses, path inlays and hero arch;
- nine Causeway mega-ribs plus overbeams and parallax under-fins;
- a Broken Momentum bazaar with ten stall shells and a central momentum core;
- eight enlarged Choir spines/forks and hanging resonance bells;
- layered Crucible terraces, twelve banner masts, three wave beacons and a victory crown;
- six pairs of Gravitas blade monoliths and a final lintel;
- twenty-eight distant Aetheria skyline spires.

The pass does not replace the collision basin, safety walls, traversal tiles or camera collision.

## First large combat encounter

The Menagerie keeps exactly ten authored enemy identities and the same `JourneyEnemyController` combat authority.

Hackathon staging reorders the encounter into **3 / 4 / 3**:

### Wave 1 · teach the triangle

- Scrap Goblin: close pressure
- Shardsinger: ranged lane commitment
- Bass Golem: heavy anchor

### Wave 2 · force target-priority decisions

- Chrome Penitent: timing contrast
- Rift Stalker: fast pounce pressure
- Choir Drone: hovering lane denial
- Aero Gargoyle: vertical dive threat

### Wave 3 · exam

- Prism Maw: cone zoning
- Veil Reaper: execution timing
- Orbit Seraph: aerial/orbital pressure

The scheduler still only decides when authored enemies are active. Attack choice, movement, hit resolution, projectile contact, poise and death stay in `JourneyEnemyController` and related combat authorities.

`HackathonEncounterPresentationV1` listens to wave events to animate three authored beacons and a victory crown. It never starts or clears waves itself.

## Enemy art contract

`HackathonEnemyPresentationV1` adds a second-pass silhouette hook to all ten identities. The details are intentionally readable from the elevated gameplay camera rather than relying on tiny textures.

Examples include tuning-fork crowns, speaker stacks, scythe legs, orbital nodes, jaw plates, executioner geometry and outer wing blades.

The layer is collider-free and may only read existing intent/recovery state. It cannot select attacks, move bodies, apply damage, modify vitals or read neural evidence.

## Guardian art contract

`PrismSquirePresentationV2` keeps the readable V1 silhouette but adds:

- layered breastplate and waist guard;
- asymmetric shoulder fins;
- a small aether reactor ring;
- segmented half-cape;
- visor/cheek detail;
- knee plates and signal accents.

Motion is downstream of existing Guardian state. Speed, dash, airborne, attack, mounted and Flux state may change presentation transforms only.

## Larger-game progression seam

`HackathonPlaythroughDirectorV1` converts Guardian world position plus Menagerie completion into a monotonic stage enum:

`Arrival → Causeway → BrokenMomentum → RuinedChoir → Gravitas → Crucible → Aftermath`

This is deliberately a **signal**, not a quest god-object. It does not spawn enemies, block doors, save checkpoints, grant items, move the player or touch BCI state.

Future quest, dialogue, analytics, spectator and esports systems can subscribe to `StageChanged` without coupling themselves to scene-coordinate checks.

## Why this scales beyond the hackathon

The intended larger-game architecture is multiplication, not replacement:

- world regions reuse the authored composition grammar;
- encounter directors schedule existing enemy authorities rather than duplicating AI;
- presentation identities stay separate from combat definitions;
- progression emits stable semantic stages/events;
- BCI remains a bounded transformation layer rather than a second control scheme.

That is the path from one vertical slice to regions, dungeons, factions, bosses, cooperative/competitive rule sets and eventually spectator-grade esports telemetry.

## Unity qualification

Before promotion of this tranche to `main`, verify in Unity 2022.3.62f3:

1. clean import/compile;
2. no decorative Hackathon geometry creates collision snags;
3. Causeway visibility/camera collision remains comfortable on foot and at hoverbike boost speed;
4. the 3/4/3 encounter activates the intended identities in the intended order;
5. all ten enemies remain physically honest and target-lockable;
6. hero/enemy V2 detail remains readable without hiding attack telegraphs;
7. presentation budget remains acceptable;
8. no new 10/12 Hz periodic luminance or stimulus-like flicker is introduced;
9. physical VEP timing/salience is re-qualified after the denser visual pass.
