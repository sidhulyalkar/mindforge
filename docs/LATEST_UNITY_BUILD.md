# Canonical Unity build

## One development target

For ordinary Mindforge development there is exactly one supported Unity entry point:

**Mindforge → Latest → PLAY LATEST (BCI Simulation)**

This rebuilds and opens the canonical integrated scene, then enters Play Mode.

The current product label is **V0.26 Production Geometry + Cathedral Depth**. The scene asset remains `Assets/Mindforge/Scenes/MindforgeDemoV11.unity` because V0.11 is the clean systems/traversal assembler version, not the complete-game product version.

`MindforgeLatestEditorMenu.BuildCanonical(...)` has eight deterministic stages:

1. `MindforgeDemoV11Builder.BuildDemoScene(...)` creates authoritative gameplay systems and traversal.
2. `WorldSoulV20Builder.ApplyOpenScene()` creates continuous terrain/material/world grammar.
3. `WorldCohesionV21Builder.ApplyOpenScene()` corrects the first-boss bowl and local cohesion.
4. `WorldIntegrityV22Builder.ApplyOpenScene()` normalizes structural render state and seals the cavern/world envelope.
5. `WorldFoundationV23Builder.ApplyOpenScene()` reconciles visible geometry with collision and makes generated terrain physically trustworthy.
6. `WorldCathedralV24Builder.ApplyOpenScene()` imposes the white-cathedral palette, modular architecture and final foreground world grammar.
7. `SensoryFidelityV25Builder.ApplyOpenScene()` promotes the pinned high-fidelity URP configuration, SSAO/screen-space shadows, ACES/post, data-cathedral inlays and maintained sensory presentation.
8. `WorldRenderingV26Builder.ApplyOpenScene()` replaces remaining primitive structural render meshes, adds tapered buttress silhouettes, recessed wall depth, continuous vault webs, cavern material separation and tri-light environmental depth.

Runtime then composes the maintained Guardian/combat presentation, Fractured Signal movement/scheduler, manual-Wisp intermission and SSVEP/telemetry systems.

## What V0.26 changes

V0.25 removed major runtime presentation conflicts and made the intended cathedral visible. V0.26 addresses what still looked unfinished once that conflict was gone: much of the architecture still resolved to raw cube silhouettes, the transverse ribs lacked a continuous roof surface, and flat ambient fill compressed pale architecture and dark cavern into nearly the same value range.

V0.26 therefore improves the actual rendered world instead of adding another post-processing layer:

- semantic V0.24 structural cubes receive deterministic chamfered render meshes;
- walkable floor skins and mystic/data accents are excluded from that replacement;
- stacked-box `Foot + Body + Crown` buttresses are visually replaced by tapered shells plus restrained finials;
- narthex/nave wall panels gain pointed recessed niches and sills;
- the five established cathedral vault stations are connected by four inward-facing Gothic vault webs;
- three longitudinal crown ribs prevent the ceiling from reading as one smooth tent;
- deep cavern/backwall surfaces and distant outer terrain receive separate material response from the pale cathedral;
- ambient lighting switches from V0.25's diagnostic flat fill to sky/equator/ground tri-light depth;
- fog resolves toward a deeper blue-slate distance color;
- cathedral shadow reach is extended to at least 68 m for long nave/cloister views.

See `docs/WORLD_RENDERING_V26.md` for implementation details and the focused playtest gate.

## World-stage ownership

The stack is intentionally layered rather than mutually authoritative:

- **V0.11**: gameplay systems and canonical traversal surfaces.
- **V0.20**: deterministic outer landforms and world surfaces.
- **V0.21**: first-boss bowl geometry correction and local cohesion.
- **V0.22**: opaque structural normalization, cavern/perimeter containment and boss-duel stability.
- **V0.23**: floor/collision reconciliation, terrain collision and inward cavern/foundation shell.
- **V0.24**: white-cathedral palette, modular architecture and foreground composition.
- **V0.25**: render fidelity and runtime sensory presentation.
- **V0.26**: static production render geometry and environmental depth only.

V0.26 does not become a second collision or gameplay authority. Existing V0.11/V0.23 surfaces remain physically authoritative.

## Rendering architecture

The canonical build now combines:

- the clean V0.11 gameplay/traversal kernel;
- V0.20 deterministic terrain;
- V0.21 arena correction;
- V0.22 cavern containment and duel stability;
- V0.23 truthful visible/physical floor and terrain topology;
- V0.24 cathedral materials and semantic module grammar;
- V0.25 HDR, depth/normals, four-cascade shadows, SSAO, screen-space shadows and restrained ACES/bloom/color response;
- V0.26 chamfered structural geometry, tapered supports, wall recesses and continuous vault surfaces;
- V0.26 deep-cavern/distant-terrain material separation and tri-light ambience;
- maintained responsive Guardian movement, double jump, hover, air dash and sword/guard authority;
- maintained Wisp/SSVEP/display-timing/persistence/telemetry systems.

## Graphics engineering policy

World authoring uses public codebases as engineering references, not an asset landfill. V0.20's deterministic noise remains adapted from MIT-licensed `SebLague/Procedural-Landmass-Generation`; the mesh-recipe workflow remains informed by MIT-licensed `aadebdeb/ProceduralMesh`; V0.23 records MIT-licensed `SebLague/Procedural-Cave-Generation` as a reference for visible/physical cave topology.

V0.24-V0.26 do not import another game's cathedral art or a competing environment framework. Generated materials, meshes and profiles live under ignored `Assets/Mindforge/Generated` paths.

When borrowing from public projects:

- confirm the upstream license before adapting code or logic;
- record upstream usage when applicable;
- include required license notices when source is actually vendored or substantially adapted;
- prefer narrow techniques over importing a competing world-authority framework;
- do not copy another game's character identity, level art or visual signature;
- preserve the SSVEP visual-control boundary.

## Authority boundary

V0.26 is editor-authored static presentation.

It does not create colliders, Rigidbody components, damage, boss cadence, locomotion requests, Flux, target authority, persistence state, Wisp state or neural evidence. It also has no `Update`, `LateUpdate` or `FixedUpdate` loop.

V0.25 remains the runtime sensory-presentation owner. Its Fractured Signal motion, diegetic prompts, camera impact and audio still suppress through calibration/Wisp resonance windows. V0.26 adds no temporal stimuli at all.

Software readiness still does not substitute for photodiode/display timing or real EEG qualification.

## Latest menu

`Mindforge → Latest` intentionally contains only:

- **PLAY LATEST (BCI Simulation)**: rebuild V0.11 and apply V0.20 → V0.21 → V0.22 → V0.23 → V0.24 → V0.25 → V0.26, then open and play in controller BCI simulation.
- **Rebuild Latest Integrated Scene**: perform the same deterministic build without Play Mode.
- **Open Latest Integrated Scene**: open the canonical scene and upgrade missing layers in order.
- **Validate Latest Readiness**: run the maintained readiness audit. It is software/scene evidence, not physical SSVEP qualification.
- **Build Neural-Hardware Variant**: build the same world with controller-only qualification disabled for real neural-service/hardware testing.

## Manual Wisp and first-boss contract

Holding `V` remains the deliberate Wisp listening ritual. V0.26 does not alter that ceasefire, neural-link safety owner, target policy or combat scheduler.

## Legacy policy

Historical V0.5-V0.10 showcase/build commands and the old V0.11 menu are implementation history, not supported development entry points. Their Unity menu entries live only under:

**Mindforge → Legacy**

Do not compose a new release by manually running historical `Apply ...` commands. If the canonical latest scene needs an older capability, the latest assembler must call the smallest required implementation explicitly and deterministically.

There should never again be multiple equally plausible "latest" builders.

## V0.26 playtest flow

1. Pull the intended branch and allow Unity to compile/import generated V0.26 mesh/material assets.
2. Run **Mindforge → Latest → PLAY LATEST (BCI Simulation)**.
3. Walk slowly through the Causeway nave and inspect column bases, capitals, walls and trim from oblique angles. They should catch beveled highlights rather than terminate in razor-edged cubes.
4. Orbit close to narthex and nave wall panels. Pointed frames should sit in front of visibly recessed darker surfaces.
5. Inspect Cloister, Choir and apse buttresses. Their dominant silhouette should taper upward instead of reading as three stacked blocks.
6. Look upward from nave/cloister. Transverse ribs should belong to a continuous roof surface, with three longitudinal ribs breaking the ceiling mass.
7. Look down the long route. White architecture should remain readable against a darker cavern shell and distinct distant terrain.
8. Re-test the Choir ascent, Causeway/Market seam, outer terrain and boss bowl with jump, double jump, hover and air dash. V0.26 must not change collision behaviour.
9. Start calibration/Wisp resonance and verify V0.25 neural-window suppression remains unchanged.
10. Run **Mindforge → Latest → Validate Latest Readiness** and inspect the Console.

If this still reads as prototype art, the next visual tranche should target the Guardian/hero props and bespoke region-specific facade modules rather than another global post pass.
