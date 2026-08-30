# Neural Gothic World V0.7

## Purpose

V0.7 is the first world-art tranche that treats generated space as a place rather than merely as valid topology.

The V0.6 Neural Cloister already has the difficult systems properties: deterministic socket-constrained generation, bounded vertical variation, stable persistent objects, one contextual interaction authority, and one save envelope. Its visual fallback, however, is still deliberately primitive: floors, box walls, occasional ribs, and isolated signal cores. That is enough to prove generation but not enough to make the annex memorable.

V0.7 closes that gap without giving presentation any new gameplay authority.

## Current-state critique

### What is already strong

- **Grounded World V1/V2 owns safe geometry.** The world is collision-backed, enclosed, and contains authored vertical routes rather than visual-only platforms.
- **V0.6 generation is deterministic.** A socket and height grammar can expand negative space while fixed landmarks remain authoritative.
- **Interaction semantics are coherent.** Bike, Forge, shortcut, loot, shrine, and NPC offers share the contextual interaction router rather than each inventing a key prompt.
- **Persistence is explicit.** Stable world IDs and restore adapters keep physical truth separate from arbitrary semantic facts.
- **The BCI boundary is unusually disciplined.** Presentation, post-processing, replay, evidence, and coded neural stimuli already have clear authority lines.
- **The art direction is specific.** Aetheria has a useful contrast language: monumental dark architecture, sparse hard-light circuitry, readable combat silhouettes, and a bright compact player.

### What is holding the visuals back

- **Builder sediment.** The scene has accumulated many sequential V1/V2/V3 editor passes. They work, but another large coordinate-heavy builder would make the scene harder to reason about and harder to replace with final art.
- **Primitive repetition.** Procedural cells currently prove topology more strongly than identity. More cells do not automatically create more place.
- **Material sameness.** Deterministic procedural PBR materials are a strong source-only fallback, but broad reuse can flatten district identity if geometry and lighting do not carry more of the visual hierarchy.
- **Landmark hierarchy is uneven.** The authored spine has memorable named districts; generated space needs an equally legible threshold, internal rhythm, and destination silhouette.
- **Decorative density can become its own failure mode.** A BCI game cannot solve visual quality by turning every surface into emission. Combat danger, target lock, and coded stimuli must remain brighter and more legible than ambient world circuitry.

## V0.7 architectural rule

**Generate gameplay truth first. Decorate it second. Never let decoration repair or redefine traversal.**

`NeuralGothicWorldKitV07` therefore:

- reads the generated cell transforms and deterministic tile identity;
- never changes the WFC observation;
- never creates a gameplay collider;
- never writes a stable world ID or persistent fact;
- never reads input;
- never owns an interaction offer;
- never runs per-frame animation;
- never drives a coded neural stimulus;
- can be deleted wholesale without changing whether the level is completable.

That last property is the simplest test for whether presentation has stolen authority.

## Scene language introduced in tranche 1

### Pointed thresholds

Selected generated seams receive narrow neural-gothic arches. They establish rhythm between cells and make the procedural grammar read as intentional architecture rather than a tiled maze.

### Flying buttresses

Sealed edges gain paired diagonal buttresses with restrained secondary signal traces. These create depth, parallax, and vertical cadence while staying outside gameplay collision truth.

### Route circuitry

Every open socket receives a thin floor trace. Its purpose is subconscious continuity, not navigation-by-neon. It must stay below combat, target-lock, and neural-target luminance in the frame hierarchy.

### Oculi and spires

High, non-colliding circular motifs and occasional fins break the repeated rectangular silhouette. They live above player-height gameplay and are deterministic from cell identity.

### Cloister Crown

A three-spire crown and suspended oculus occupy the far-east skyline, opposite the authored western threshold. It gives the annex a visual destination before the player understands its internal layout.

## BCI and readability constraints

The world-art hierarchy remains:

1. immediate player/enemy contact and danger;
2. coded neural target when active;
3. character silhouette and lock-on state;
4. district landmark;
5. route/decorative hard-light;
6. optional ambient rhythmic detail.

V0.7 intentionally ships with **no ambient animation**. Any later bass-reactive or environmental motion must be a separate presentation component, must remain outside collision/attack/input/state authority, and must have an explicit BCI-safe operating envelope. Coded 10/12 Hz or future stimulus frequencies are not an artistic palette.

## Acceptance test for a good frame

Without reading HUD text, a capture should answer:

- Where can the player move next?
- What is currently dangerous?
- Which silhouette is the player and which is the enemy?
- Where is the Aetherblade?
- What landmark gives the current district orientation?

If an oculus, route line, bloom halo, or decorative signal makes one of those answers harder, remove or dim it.

## Performance contract

The first pass favors deterministic primitive construction because it keeps source control reproducible and avoids importing unverified asset packs into gameplay-critical scenes. Before increasing density materially, profile the completed scene and consolidate repeated decorative geometry through prefab/module reuse, static batching, or combined meshes as appropriate.

Performance should be measured on the finished gameplay frame, not in an empty annex. Stable frame pacing is part of BCI presentation quality.

## What should come next

### V0.7.1: district module profiles

Replace more coordinate-heavy one-off dressing with a small library of reusable district modules and explicit visual profiles. The same module should be placeable in authored and generated space without inheriting gameplay authority.

### V0.7.2: Fractured Signal heroic pass

Spend disproportionate art time on the encounter the player and jury are most likely to remember. Improve arena landmark framing, boss silhouette, phase staging, impact readability, danger telegraphs, and darkness around high-energy attacks before expanding the campaign again.

### V0.7.3: final-art replacement seams

Allow authored prefab modules and upgraded materials to replace source-only primitives without changing sockets, world IDs, interactions, traversal surfaces, or persistence contracts.

### V0.7.4: environmental life

Add low-frequency, presentation-only machinery, banners, speaker membranes, distant traffic/particles, and other ambient motion only after the static scene is readable. Environmental motion must be independently disableable for calibrated BCI runs.

### V0.7.5: visual qualification

Capture the same route at representative camera angles and validate:

- landmark recognition within a few seconds;
- route readability without HUD dependence;
- enemy silhouette separation at combat distance;
- no decorative collision or ghost blockers;
- no new interaction prompts;
- no coded-stimulus contamination;
- acceptable CPU/GPU frame pacing and draw-call/object-count budget;
- persistence and reward idempotence unchanged after Forge rest and restart.

## Scope discipline

The next milestone is not “make the map infinite.” It is **make one complete slice feel authored, coherent, and worth remembering**, then make the architecture cheap enough to repeat. The generator is infrastructure. The game still wins or loses on the quality of the player's authored journey through it.
