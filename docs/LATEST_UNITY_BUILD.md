# Canonical Unity build

## One development target

For ordinary Mindforge development there is exactly one supported Unity entry point:

**Mindforge → Latest → PLAY LATEST (BCI Simulation)**

This rebuilds and opens the canonical integrated scene, then enters Play Mode.

The current product label is **V0.24 White Cathedral + World Reformation**. The scene asset remains `Assets/Mindforge/Scenes/MindforgeDemoV11.unity` because V0.11 is the clean systems/traversal assembler version, not the complete-game product version.

`MindforgeLatestEditorMenu.BuildCanonical(...)` now has six deterministic authoring stages:

1. `MindforgeDemoV11Builder.BuildDemoScene(...)` creates the authoritative systems and traversal kernel.
2. `WorldSoulV20Builder.ApplyOpenScene()` creates continuous terrain/material/world grammar.
3. `WorldCohesionV21Builder.ApplyOpenScene()` applies the arena correction and earlier local cohesion work.
4. `WorldIntegrityV22Builder.ApplyOpenScene()` normalizes structural render state and seals the broad cavern/world envelope.
5. `WorldFoundationV23Builder.ApplyOpenScene()` reconciles visual geometry with collision, makes generated terrain explorable and fixes the inward cavern/foundation shell.
6. `WorldCathedralV24Builder.ApplyOpenScene()` removes obsolete foreground clutter and imposes the canonical white-cathedral material, module, floor, lighting and architectural grammar.

Runtime then composes the maintained Guardian/combat presentation, Fractured Signal movement/scheduler, manual-Wisp intermission and SSVEP/telemetry systems.

## What V0.24 changes

V0.23 solved physical trust. V0.24 solves **visual and architectural trust**.

The latest playtest still looked like a stack of individually reasonable passes: dark blockout surfaces, procedural foreground scatter, market boxes, skyline masses, later foundations, rocks, columns and cavern parts all competed for attention. V0.24 stops treating that as a polish problem and reformulates the world as one architectural system.

The canonical foreground is now a **white cathedral carved into a darker cavern**:

- pale ivory and white marble dominate playable architecture;
- one pale-floor material owns all canonical route surfaces;
- cool dark stone is restricted to geology, backing and recessed foundations;
- bronze/gold are restrained trim rather than random accents;
- cyan remains guidance/sanctum energy;
- magenta remains Fractured Signal corruption;
- old scatter/facade/skyline/stall layers that conflict with this grammar are disabled rather than buried under more detail;
- the route is recomposed as narthex → nave → cloister/transept → choir ascent → corrupted apse;
- repeated columns, pointed arches, buttresses, wall panels, aisle inlays and vault ribs create a measured architectural cadence;
- V0.24 floor skins remain collider-free and align to V0.11/V0.23 physical authority;
- the boss apse is built outside the widened arena so architecture frames the fight without shrinking it;
- lighting is lifted with a warmer key, brighter ambient fill, cleaner fog and fixed point lights, with no flicker or neural-state modulation.

See `docs/WORLD_CATHEDRAL_V24.md` for the complete art direction, module grammar and playtest gate.

## What earlier stages still own

V0.24 is a re-art/composition stage, not a replacement for lower-level world correctness.

- V0.20 still owns deterministic landforms and generated world surfaces.
- V0.21 still owns the enlarged/flattened first-boss movement shell.
- V0.22 still owns opaque structural normalization, cavern/perimeter containment and boss-duel stability.
- V0.23 still owns the repaired ascent foundation, route seam protection, explorable generated terrain and shared render/collision cavern topology.
- V0.24 owns the final foreground aesthetic grammar, modular cathedral composition and cleanup of obsolete decorative layers.

## Canonical composition

The canonical build combines:

- clean V0.11 systems/traversal authority;
- V0.20 deterministic terrain and world-surface generation;
- V0.21 boss-arena geometry correction;
- V0.22 structural opacity, cavern envelope and stable boss-duel behavior;
- V0.23 floor/collision reconciliation, terrain collision and inward cavern shell;
- V0.24 white-cathedral material palette and deterministic generated stone textures;
- V0.24 semantic modular building kit (`FloorSkin`, `Column`, `PointedArch`, `Buttress`, `WallPanel`, `LumenSconce`, etc.);
- V0.24 processional spine and explicit threshold language;
- V0.24 narthex, nave, cloister, choir and Fractured Signal apse composition;
- V0.24 static lighting and fail-closed structural-role validation;
- current Guardian responsive movement, double jump, hover, air dash and physical sword/guard authority;
- current Wisp/SSVEP/display-timing/persistence/telemetry systems.

## Graphics engineering policy

World authoring uses public codebases as engineering references, not an asset landfill. V0.20's deterministic noise remains adapted from MIT-licensed `SebLague/Procedural-Landmass-Generation`; the mesh-recipe workflow remains informed by MIT-licensed `aadebdeb/ProceduralMesh`; V0.23 uses MIT-licensed `SebLague/Procedural-Cave-Generation` as a reference for shared visible/physical cave topology.

V0.24 does not import a new environment framework or copied cathedral art. It builds a project-authored cathedral kit on top of Mindforge's existing production triplanar shader and deterministic noise utilities. Generated V0.24 textures/materials live under ignored `Assets/Mindforge/Generated/V24`.

When borrowing from public projects:

- confirm the upstream license before adapting code or logic;
- record the upstream and usage in `third_party/manifest.json` where applicable;
- include required license notices when source is actually vendored or substantially adapted;
- prefer narrow techniques over importing a competing world-authority framework;
- do not copy another game's character identity, level art or visual signature;
- preserve the SSVEP visual-control boundary.

## Authority boundary

The V0.24 cathedral stage is deterministic editor authoring. It adds no gameplay scheduler, damage logic, input path, persistence authority or neural consumer.

Its processional floor skins are presentation only. Canonical V0.11/V0.23 floor colliders remain the physical authority. Structural columns added close to the ordinary route may own conservative static collision; boss-apartment architecture is placed outside the established combat ring and remains presentation framing.

V0.24 lighting is static. There is no `Update`, `LateUpdate`, `FixedUpdate`, flicker, pulsing or neural-state-driven modulation.

`FracturedSignalDuelStabilityV22` remains the boss-stability layer. V0.24 does not touch boss scheduling or Wisp/neural pause authority.

## Latest menu

`Mindforge → Latest` intentionally contains only:

- **PLAY LATEST (BCI Simulation)**: rebuild V0.11, apply V0.20 → V0.21 → V0.22 → V0.23 → V0.24, open and play in controller BCI simulation.
- **Rebuild Latest Integrated Scene**: perform the same deterministic build without Play Mode.
- **Open Latest Integrated Scene**: open the canonical scene and upgrade missing world layers in order.
- **Validate Latest Readiness**: run the maintained readiness audit. It is software/scene evidence, not physical SSVEP qualification.
- **Build Neural-Hardware Variant**: build the same world with controller-only qualification disabled for real neural-service/hardware testing.

## Manual Wisp and first-boss contract

Holding `V` remains the deliberate Wisp listening ritual. V0.24 does not alter that ceasefire, the neural-link safety owner, or combat scheduling.

Outside Wisp/neural safety, the boss fight remains the V0.22/V0.23 fight mechanically. V0.24 only changes the architecture surrounding it.

## Legacy policy

Historical V0.5-V0.10 showcase/build commands and the old V0.11 menu are implementation history, not supported development entry points. Their Unity menu entries live only under:

**Mindforge → Legacy**

Do not compose a new release by manually running historical `Apply ...` commands. If the canonical latest scene needs an older capability, the latest assembler must call the smallest required implementation explicitly and deterministically.

There should never again be multiple equally plausible "latest" builders.

## V0.24 playtest flow

1. Pull the intended branch or `main` and allow Unity to compile/import.
2. Run **Mindforge → Latest → PLAY LATEST (BCI Simulation)**.
3. At spawn, confirm the foreground immediately reads as pale cathedral architecture rather than dark blockout/procedural scatter.
4. Walk narthex → nave → cloister and inspect the center aisle. Floors should share one visual language and district thresholds should read as deliberate bands.
5. Inspect the Market from several camera angles. Old stall boxes, noisy scatter, facade clutter and skyline blocks should no longer dominate the view.
6. Re-test the Choir ascent with jump, double-jump, hover and air dash. The single canonical ramp must remain visually and physically coherent.
7. Run along columns and walls. Repeated modules should create architectural rhythm without introducing awkward snags or hiding the route.
8. Enter the Fractured Signal chamber. Pale apse architecture should sit outside the movement bowl; magenta corruption should read as invasion/contrast rather than the whole environment.
9. Look up and toward the cavern boundaries. Dark geology should frame the white cathedral rather than swallow it.
10. Test sword, dodge, guard, parry, Wisp and neural simulation flows for regression.
11. Run **Mindforge → Latest → Validate Latest Readiness** and inspect the Console for V0.24 structural validation failures.

For real BCI testing, use **Build Neural-Hardware Variant** and the live neural service on a physically qualified display. Software readiness still does not substitute for photodiode timing or real EEG qualification.
