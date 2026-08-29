# Aetheria Scene Implementation Plan

## Layering rule

Aetheria is an authored layer over the already-qualified Grounded World shell. The existing basin/perimeter remain the no-void safety topology. New scene work must not replace that contract.

Build order target:

1. ArenaEnvironmentV3
2. NullWardScene
3. GroundedWorldV1 safety shell
4. GroundedWorldCompositionV2
5. GroundedWorldTuningV1
6. Arena ecosystem + Menagerie gameplay population
7. honest collider passes
8. enemy silhouette passes
9. AetheriaWorldV1 story/landmark/hoverbike layer
10. visual infrastructure and set dressing
11. traversal playability validation
12. software/presentation gates

Aetheria visuals may decorate reachable architecture but may not create invisible traversal blockers. Any reachable platform requires real collision; any decorative signal strip, hologram, banner, halo, cable, skyline structure, or bike ornament is collider-free.

## Spatial mapping

The current world already has useful district coordinates. Re-theme rather than discard:

| Existing district | Aetheria identity | Approx. Z |
|---|---|---:|
| Memory Forge | Prism Bastion | -56 |
| Synapse Causeway | Neon Causeway | -44 |
| Null Market | Market of Broken Momentum | -29 |
| Fracture Court / tower | Ruined Choir lower ascent | -18 |
| Cathedral | Choir of Ruined Towers | -10 |
| Arena outer ring | Hall of Excessive Gravitas | +6 |
| Menagerie Crucible | Cyber-Mythic combat showcase | +18 |

## Landmark language

### Prism Bastion

- paired hard-light castle crowns outside the central lane;
- cyan/magenta Prism banners;
- bass pylons and luminous buttresses;
- bright guild identity close to the player, darker megastructure in the distance.

### Neon Causeway

- widened visual lane framing suitable for hoverbike speed;
- hard-light rail strips outside the collision lane;
- repeating bridge ribs for speed parallax;
- first parked Prism hoverbike on a safe side bay;
- no bike requirement for progression.

### Market of Broken Momentum

- RGB salvage stacks;
- broken momentum-drive rings;
- large speaker shrine / Bass-Golem foreshadowing;
- clutter kept outside core roll and bike lanes.

### Choir of Ruined Towers

- tall tuning-fork towers and suspended signal bells;
- layered vertical silhouettes;
- route-readable cyan landing pockets;
- violet hostile architecture farther away.

### Hall of Excessive Gravitas

- deliberately severe symmetry;
- black/obsidian columns with restrained violet signal;
- Malatract holographic crown/monolith at vista end;
- keep combat floor uncluttered.

### Menagerie Crucible

Retain existing 3/3/4 wave grammar. Aetheria framing may add faction banners and audience-like signal pylons but must not alter spawn positions or enemy combat authority in V1.

## Narrative presentation

Use a presentation-only `AetheriaNarrativeDirector` that reads Guardian world position and displays one-time area cards / serious Malatract transmissions.

It must not:

- move the player;
- lock input;
- pause combat;
- spawn enemies;
- alter BCI evidence;
- alter VEP luminance;
- gate progression.

Narrative cards should be brief enough to coexist with action. Malatract's lines are severe and self-important; humor remains primarily physical.

## Hoverbike station

First bike station should sit off the Causeway lane around the transition from Bastion to Causeway. Interaction radius should be generous enough to discover naturally while not triggering accidentally.

The parked bike is visual-only except for its interaction component. Mounted movement continues to use the Guardian Rigidbody, so world collision and safety recovery remain valid.

## Capture checklist

A Unity capture must prove:

- no new exposed world edge;
- Causeway can be traversed both on foot and mounted;
- mounted speed does not outrun camera collision or world streaming assumptions;
- bike visual does not clip through Guardian excessively;
- dismount cannot place player outside the safety shell;
- landmark silhouettes remain legible from the more distant diorama camera;
- enemy telegraphs outrank banners, neon rails, bike trails, and narrative UI;
- Sight/Guard coded targets remain perceptually distinct.