# Canonical Unity build

## One development target

For ordinary Mindforge development there is exactly one supported Unity entry point:

**Mindforge → Latest → PLAY LATEST (BCI Simulation)**

This rebuilds and opens the canonical integrated scene, then enters Play Mode.

The current product label is **V0.21 Arena + Patina**. The scene asset remains `Assets/Mindforge/Scenes/MindforgeDemoV11.unity` because V0.11 is the version of the clean systems/traversal assembler, not the version of the complete game. Product-version churn must not force serialized-scene renames.

`MindforgeLatestEditorMenu.BuildCanonical(...)` has three deterministic authoring stages:

1. `MindforgeDemoV11Builder.BuildDemoScene(...)` creates the authoritative systems and traversal kernel.
2. `WorldSoulV20Builder.ApplyOpenScene()` creates the continuous terrain, material, ecology and far-field world layer.
3. `WorldCohesionV21Builder.ApplyOpenScene()` performs the recording-driven arena correction and static patina/facade/foreground pass.

The resulting game also receives maintained runtime layers after scene load, including current Guardian/combat presentation, the V0.19 Fractured Signal boss redesign, the V0.21 arena-mobility tuning adapter, the manual-Wisp combat intermission and current SSVEP/telemetry systems.

## What V0.21 changes

V0.20 successfully replaced much of the platform/blockout feeling with continuous landform and real surface breakup. The August 31 gameplay capture exposed the next layer of truth: the Fractured Signal arena still had the dimensions and movement economics of a corridor, while many world seams still looked freshly assembled.

V0.21 therefore prioritizes structure over object count.

The first-boss floor grows from 25 x 24 m to 36 x 34 m, the enclosing ring moves from 13 m to 18.3 m, and the old raised inner dais becomes a nearly flush visual medallion with no collider. The V0.20 crater dressing moves outward with the shell. A fail-closed V0.21 adapter expands the V0.19 boss leash from 5.4 m to 9.0 m and retunes spacing/orbit behavior so the creature can actually exploit the room it has been given.

The graphics pass then attacks procedural tells where they are most visible: wet/mossy contact zones, masonry-ground erosion, fracture/soot scars, close-camera ferns, near-city facade depth and pitched roofs, and stronger landmark framing.

See `docs/WORLD_COHESION_V21.md` for the recording diagnosis and exact playtest gate.

## Canonical composition

The canonical build now combines:

- the clean V0.11 district/world and systems assembler;
- V0.20 deterministic landforms, generated triplanar surfaces, natural set dressing and far city;
- V0.21 enlarged boss arena and flat duel center;
- V0.21 static material transitions, erosion/fracture patina and close-camera ecology;
- V0.21 near-city facades, windows and roof silhouettes;
- current Guardian movement and combat authority;
- the V0.19 Fractured Signal locomotion/attack owner plus V0.21 spacing retune;
- the V0.19 two-sided manual-Wisp combat intermission;
- synchronized SSVEP epoch/decoder and display-timing contracts;
- neural-quiet calibration/presentation rules;
- the current directed intro/gameplay camera and HUD;
- current persistence, telemetry and BCI interfaces.

## Graphics engineering policy

World authoring uses public codebases as engineering references, not an asset landfill. V0.20's deterministic noise remains adapted from MIT-licensed `SebLague/Procedural-Landmass-Generation`; the procedural mesh workflow remains informed by MIT-licensed `aadebdeb/ProceduralMesh`; `keijiro/NoiseShader` remains reference-only until gameplay-camera evidence justifies a runtime GPU microvariation path.

Generated V0.20 textures/materials/meshes remain under ignored `Assets/Mindforge/Generated/V20`. V0.21's tiny generated glow materials live under ignored `Assets/Mindforge/Generated/V21`.

When borrowing from public projects:

- confirm the upstream license before adapting code;
- record source/license where the adaptation is owned;
- prefer adapting a narrow technique over importing a framework;
- do not copy another game's character identity or art direction;
- do not add runtime complexity where editor-authored results are sufficient;
- preserve the SSVEP visual-control boundary.

## Authority boundary

V0.20 scenery and V0.21 patina/facade/ecology scenery are static presentation. They add no periodic runtime animation, weather particles or stimulus-linked environmental lighting.

The deliberate V0.21 collision change is narrow: it edits the existing V0.11 Fractured Signal arena floor/wall shell and removes the inner dais collider. It does not create a competing traversal system. All newly added V0.21 decorative geometry is collider-free.

`FracturedSignalArenaMobilityV21` does not move the boss itself. It validates and retunes the maintained V0.19 movement owner's private spacing fields once at startup. It has no `FixedUpdate`, Rigidbody movement, attack scheduling, damage or neural authority.

During calibration or an armed Wisp neural visual field, the maintained V0.19 movement and presentation owners continue freezing boss motion. V0.21 does not weaken that invariant.

## Latest menu

`Mindforge → Latest` intentionally contains only:

- **PLAY LATEST (BCI Simulation)**: rebuild V0.11, apply V0.20 and V0.21, open, then play with controller BCI simulation.
- **Rebuild Latest Integrated Scene**: perform the same deterministic build without Play Mode.
- **Open Latest Integrated Scene**: open the canonical scene and upgrade missing world layers in order.
- **Validate Latest Readiness**: run the maintained readiness audit. It is software/scene evidence, not physical SSVEP qualification.
- **Build Neural-Hardware Variant**: build the same world with controller-only qualification disabled for real neural-service/hardware testing.

## Manual Wisp and first-boss contract

Holding `V` remains a deliberate listening ritual. When a Wisp window arms, boss attacks and existing hostile projectiles pause and Guardian combat commands are suspended while ordinary locomotion remains available. V0.21 does not change that BCI contract.

The ordinary sword fight should nevertheless feel much less physically stalled because the arena no longer funnels both actors onto a raised 9 x 9 m center and the boss has a movement envelope proportionate to the new combat bowl.

## Legacy policy

Historical V0.5-V0.10 showcase/build commands and the old V0.11 menu are implementation history, not supported development entry points. Their Unity menu entries live only under:

**Mindforge → Legacy**

Do not compose a new release by manually running historical `Apply ...` commands. If the canonical latest scene needs an older capability, the latest assembler must call the smallest required implementation explicitly and deterministically.

There should never again be multiple equally plausible "latest" builders.

## V0.21 playtest flow

1. Pull the intended branch or `main` and allow Unity to compile/import.
2. Run **Mindforge → Latest → PLAY LATEST (BCI Simulation)**.
3. Traverse the existing route and verify V0.20 terrain/material generation is reused from cache rather than rebuilt without a recipe revision.
4. Enter the Fractured Signal arena and orbit the boss. The whole 36 x 34 m bowl should feel usable and the visual medallion must not catch movement.
5. Test every arena quadrant with target lock, dodge rolls and backward movement. The boss should use significantly more lateral space and recover from wall-facing orbit choices faster.
6. Confirm the exterior crater, broken arches and spires frame the arena from outside the movement bowl rather than forming misleading interior obstacles.
7. Inspect Causeway canal seams, Sanctum/Market wall feet and the Ascent geology transition from the gameplay camera.
8. Inspect close ferns and near-Market facades/roofs. They should improve near/mid-distance credibility without obscuring traversal.
9. Hold `V` during combat and verify the existing Wisp ceasefire, coded-core behavior and neural visual-field freeze remain unchanged.
10. Run **Mindforge → Latest → Validate Latest Readiness**, then inspect the Console for V0.21 authoring or boss-profile errors.

For real BCI testing, use **Build Neural-Hardware Variant** and the live neural service on a physically qualified display. Software readiness still does not substitute for photodiode timing or real EEG qualification.
