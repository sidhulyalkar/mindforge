# Canonical Unity build

## One development target

For ordinary Mindforge development there is exactly one supported Unity entry point:

**Mindforge → Latest → PLAY LATEST (BCI Simulation)**

This rebuilds and opens the canonical integrated scene, then enters Play Mode.

The current product label is **V0.22 World Integrity + Boss Duel**. The scene asset remains `Assets/Mindforge/Scenes/MindforgeDemoV11.unity` because V0.11 is the clean systems/traversal assembler version, not the complete-game product version.

`MindforgeLatestEditorMenu.BuildCanonical(...)` has four deterministic authoring stages:

1. `MindforgeDemoV11Builder.BuildDemoScene(...)` creates the authoritative systems and traversal kernel.
2. `WorldSoulV20Builder.ApplyOpenScene()` creates continuous terrain, material, ecology and far-field grammar.
3. `WorldCohesionV21Builder.ApplyOpenScene()` performs the recording-driven arena correction and local patina/facade/foreground pass.
4. `WorldIntegrityV22Builder.ApplyOpenScene()` normalizes structural render state, closes visual seams and authors the complete cavern/world envelope.

Runtime then composes the maintained Guardian/combat presentation, V0.19 Fractured Signal movement and scheduler, V0.21 spacing adapter, V0.22 duel-stability layer, manual-Wisp intermission and SSVEP/telemetry systems.

## What V0.22 changes

V0.21 proved that enlarging one arena is not enough if the surrounding game still exposes implementation seams. The follow-up playtest reported ghosted/transparent world surfaces, an incomplete top/cavern, exploration into visibly unfinished areas and a boss fight that could still stop or feel unreliable.

V0.22 fixes those as system boundaries:

- structural materials are explicitly reset to opaque, depth-writing render state rather than trusting serialized state from reused Unity material assets;
- ordinary World Soul water is opaque stylized water to eliminate depth-sorted shoreline holes;
- broad underlay geometry closes floor seams without changing walkable collision;
- a continuous cavern vault, backing walls and rock shoulders close the route into one place;
- high ceiling/perimeter safety collision prevents aerial escape or falling into un-authored infinity;
- the boss chamber receives architecture that connects it to the same cavern shell;
- the Fractured Signal leash expands from V0.21's 9 m to 14.2 m so the enlarged 18.3 m arena is actually used;
- projectile/echo density is reduced while melee telegraphs become clearer;
- a trigger-only combat hull improves sword contact reliability;
- exceptional stall recovery handles wall-lock/pathological immobility without taking over normal locomotion;
- stale external pause is only cleared when Wisp and neural-safety owners are demonstrably inactive.

See `docs/WORLD_INTEGRITY_V22.md` for the detailed diagnosis and playtest gate.

## Canonical composition

The canonical build combines:

- clean V0.11 systems/traversal authority;
- V0.20 deterministic landforms, generated triplanar surfaces, ecology and far city;
- V0.21 enlarged/flattened first-boss arena and local environmental cohesion;
- V0.22 opaque structural render-state normalization and ground underlay;
- V0.22 cavern roof, continuous backing walls, roof-to-world geology and distant safety envelope;
- current Guardian responsive movement, double jump, hover, air dash and physical sword/guard authority;
- V0.19 Fractured Signal locomotion plus V0.21/V0.22 one-time profile composition;
- V0.22 trigger-only boss sword-contact hull and exceptional stall recovery;
- the V0.19 two-sided manual-Wisp combat intermission;
- synchronized SSVEP epoch/decoder and display-timing contracts;
- neural-quiet calibration/presentation rules;
- current directed intro/gameplay camera, HUD, persistence and telemetry.

## Graphics engineering policy

World authoring uses public codebases as engineering references, not an asset landfill. V0.20's deterministic noise remains adapted from MIT-licensed `SebLague/Procedural-Landmass-Generation`; the procedural mesh workflow remains informed by MIT-licensed `aadebdeb/ProceduralMesh`; `keijiro/NoiseShader` remains reference-only until gameplay-camera evidence demonstrates that runtime GPU microvariation is a higher-value bottleneck than composition, material correctness or module quality.

Generated V0.20 textures/materials/meshes remain under ignored `Assets/Mindforge/Generated/V20`; V0.21 and V0.22 generated local materials remain under their ignored generated directories.

When borrowing from public projects:

- confirm the upstream license before adapting code;
- record source/license where adaptation is owned;
- prefer adapting a narrow technique over importing a framework;
- do not copy another game's character identity or art direction;
- do not add runtime complexity where editor-authored results are sufficient;
- preserve the SSVEP visual-control boundary.

## Authority boundary

V0.20 scenery, V0.21 patina/facades/ecology and V0.22 visible cavern/world geometry are static editor-authored presentation. None runs periodic visual animation or consumes neural state.

V0.22 adds only distant world-envelope collision: the high cavern roof and far perimeter safety shell. The ordinary route remains governed by existing V0.11 traversal collision.

`FracturedSignalDuelStabilityV22` does not create a second ordinary movement or attack scheduler. V0.19 remains the locomotion owner and `FracturedSignalDirector` remains the scheduler. V0.22 changes their profile once and intervenes only for impossible boundary state, sustained post-commit locomotion stall, or a provably stale external pause.

The Wisp intermission and neural-link safety stop remain higher-authority pause owners and are never cleared by V0.22 while active.

## Latest menu

`Mindforge → Latest` intentionally contains only:

- **PLAY LATEST (BCI Simulation)**: rebuild V0.11, apply V0.20 → V0.21 → V0.22, open and play in controller BCI simulation.
- **Rebuild Latest Integrated Scene**: perform the same deterministic build without Play Mode.
- **Open Latest Integrated Scene**: open the canonical scene and upgrade missing world layers in order.
- **Validate Latest Readiness**: run the maintained readiness audit. It is software/scene evidence, not physical SSVEP qualification.
- **Build Neural-Hardware Variant**: build the same world with controller-only qualification disabled for real neural-service/hardware testing.

## Manual Wisp and first-boss contract

Holding `V` remains a deliberate listening ritual. When a Wisp window arms, boss attacks and existing hostile projectiles pause and Guardian combat commands are suspended while ordinary locomotion remains available. V0.22 explicitly detects this owner and will not repair that pause.

Outside Wisp/neural safety, the ordinary sword fight should remain continuously live after encounter entry. The boss should use most of the chamber, recover from pathological wall stalls, provide reliable sword contact and present fewer simultaneous projectile/echo distractions.

## Legacy policy

Historical V0.5-V0.10 showcase/build commands and the old V0.11 menu are implementation history, not supported development entry points. Their Unity menu entries live only under:

**Mindforge → Legacy**

Do not compose a new release by manually running historical `Apply ...` commands. If the canonical latest scene needs an older capability, the latest assembler must call the smallest required implementation explicitly and deterministically.

There should never again be multiple equally plausible "latest" builders.

## V0.22 playtest flow

1. Pull the intended branch or `main` and allow Unity to compile/import.
2. Run **Mindforge → Latest → PLAY LATEST (BCI Simulation)**.
3. Traverse the full route and inspect every floor/rock/wall seam for transparency, ghosting, incorrect depth order or visible void.
4. Jump, double-jump, hover and air-dash near world edges. The cavern must remain visually closed and the player must not escape the roof/perimeter.
5. Look upward and sideways throughout the route. The ceiling, backing walls, irregular shoulders and ribs should read as one cavern rather than isolated roof props.
6. Enter the Fractured Signal chamber and orbit through every quadrant. The boss should use substantially more lateral room than V0.21.
7. Repeatedly attack at plausible sword reach and verify hits register consistently without the new hull physically blocking movement.
8. Test dodge, jump, guard, projectile sword-parry and all three boss phases. Telegraphs should be readable and projectile/echo density should not drown out melee.
9. Hold `V`, verify the deliberate ceasefire, then end the Wisp window and verify combat resumes. No non-Wisp/non-safety stall should persist after encounter entry.
10. Run **Mindforge → Latest → Validate Latest Readiness** and inspect the Console for V0.22 authoring/profile/recovery warnings.

For real BCI testing, use **Build Neural-Hardware Variant** and the live neural service on a physically qualified display. Software readiness still does not substitute for photodiode timing or real EEG qualification.
