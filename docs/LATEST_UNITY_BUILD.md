# Canonical Unity build

## One development target

For ordinary Mindforge development there is exactly one supported Unity entry point:

**Mindforge → Latest → PLAY LATEST (BCI Simulation)**

This rebuilds and opens the canonical integrated scene, then enters Play Mode.

The current product label is **V0.23 World Foundation + Coherence**. The scene asset remains `Assets/Mindforge/Scenes/MindforgeDemoV11.unity` because V0.11 is the clean systems/traversal assembler version, not the complete-game product version.

`MindforgeLatestEditorMenu.BuildCanonical(...)` has five deterministic authoring stages:

1. `MindforgeDemoV11Builder.BuildDemoScene(...)` creates the authoritative systems and traversal kernel.
2. `WorldSoulV20Builder.ApplyOpenScene()` creates continuous terrain, material, ecology and far-field grammar.
3. `WorldCohesionV21Builder.ApplyOpenScene()` performs the recording-driven arena correction and local patina/facade/foreground pass.
4. `WorldIntegrityV22Builder.ApplyOpenScene()` normalizes structural render state, closes visual seams and authors the broad cavern/world envelope.
5. `WorldFoundationV23Builder.ApplyOpenScene()` reconciles visual geometry with collision, replaces the cavern ceiling with inward-facing topology, seals the high vault ends and gives the route believable foundations.

Runtime then composes the maintained Guardian/combat presentation, V0.19 Fractured Signal movement and scheduler, V0.21 spacing adapter, V0.22 duel-stability layer, manual-Wisp intermission and SSVEP/telemetry systems.

## What V0.23 changes

The V0.22 playtest showed an important distinction: a world can be visually sealed while still feeling physically untrustworthy. The new recording exposed a concrete example. The canonical Choir Tower ascent collider is rotated **-8.1 degrees**, while V0.22 placed a broad visual-only `AscentUnderlay` at **+6.5 degrees**. Those two surfaces cross, making correct Rigidbody traversal look like the Guardian is jumping through a floor.

V0.23 treats this as a world-foundation problem rather than another decoration pass:

- the crossing V0.22 ascent underlay is removed and replaced with a visual foundation skin aligned to the exact canonical ramp slope;
- three recessed collision seam guards sit below the normal route surfaces, including the real one-metre gap between `CausewayRoad` ending at z=32 and `MarketFloor` beginning at z=33;
- large visually solid procedural rocks, columns, spires and buttresses receive conservative inset contact proxies so the Guardian and existing camera collision agree with what the player sees;
- the cavern ceiling is regenerated with inward/downward triangle winding and normals rather than relying on a double-sided material to display outward-facing terrain topology from below;
- the corrected cavern mesh is shared by render and roof collision so those boundaries cannot drift apart;
- high north/south backing seals close the remaining gap between the tall center of the vault and V0.22 end walls;
- causeway, market and ascent retaining/foundation geometry makes the route feel embedded in the cavern rather than suspended inside it.

See `docs/WORLD_FOUNDATION_V23.md` for the exact recording diagnosis, geometry contract, public-code provenance and playtest gate.

## What V0.22 still owns

V0.23 composes after V0.22 rather than replacing it. V0.22 still owns:

- explicit opaque/depth-writing structural material normalization;
- opaque stylized world water that avoids depth-sorted shoreline holes;
- broad visual underlay away from the repaired ascent;
- the main cavern vault placement, backing walls, irregular shoulders and distant traversal envelope;
- the expanded Fractured Signal leash and movement profile;
- lower projectile/echo density and clearer melee telegraphs;
- the trigger-only boss sword-contact hull;
- exceptional boss stall recovery that preserves Wisp and neural-safety pause authority.

## Canonical composition

The canonical build combines:

- clean V0.11 systems/traversal authority;
- V0.20 deterministic landforms, generated triplanar surfaces, ecology and far city;
- V0.21 enlarged/flattened first-boss arena and local environmental cohesion;
- V0.22 opaque structural render-state normalization, broad ground backing and world envelope;
- V0.23 route geometry/collision reconciliation and inward cavern shell correction;
- V0.23 upper cavern seals and below-route foundation composition;
- current Guardian responsive movement, double jump, hover, air dash and physical sword/guard authority;
- V0.19 Fractured Signal locomotion plus V0.21/V0.22 one-time profile composition;
- V0.22 trigger-only boss sword-contact hull and exceptional stall recovery;
- the V0.19 two-sided manual-Wisp combat intermission;
- synchronized SSVEP epoch/decoder and display-timing contracts;
- neural-quiet calibration/presentation rules;
- current directed intro/gameplay camera, HUD, persistence and telemetry.

## Graphics engineering policy

World authoring uses public codebases as engineering references, not an asset landfill. V0.20's deterministic noise remains adapted from MIT-licensed `SebLague/Procedural-Landmass-Generation`; the procedural mesh workflow remains informed by MIT-licensed `aadebdeb/ProceduralMesh`; V0.23 additionally uses MIT-licensed `SebLague/Procedural-Cave-Generation` as a reference for keeping visible cave boundaries and physical cave boundaries derived from shared generated topology. `keijiro/NoiseShader` remains reference-only until gameplay-camera evidence demonstrates that runtime GPU microvariation is a higher-value bottleneck than composition, material correctness or module quality.

V0.23 does **not** copy SebLague's cave script, scene, art or marching-squares implementation. Mindforge already has a deterministic height-field cavern, so importing an independent cave generator would create a competing world authority. The project-authored V0.23 mesh recipe borrows the narrower and more useful engineering idea: one generated interior topology should define both what the player sees and what the roof collider represents.

Generated V0.20 textures/materials/meshes remain under ignored `Assets/Mindforge/Generated/V20`; V0.21 and V0.22 generated local materials remain under their ignored generated directories; V0.23 inward meshes are generated locally under ignored `Assets/Mindforge/Generated/V23`.

When borrowing from public projects:

- confirm the upstream license before adapting code or logic;
- record the upstream and usage in `third_party/manifest.json`;
- include the required license notice whenever source is actually vendored or substantially adapted;
- prefer adapting a narrow technique over importing a framework when Mindforge already owns the surrounding authority;
- do not copy another game's character identity, level art or visual signature;
- do not add runtime complexity where deterministic editor-authored results are sufficient;
- preserve the SSVEP visual-control boundary.

## Authority boundary

V0.20 scenery and V0.21 patina/facades/ecology remain static presentation. V0.22 owns the distant cavern envelope collision and V0.23 adds only static physical reconciliation required to make visible solidity truthful.

V0.23's three route seam guards are deliberately recessed beneath canonical traversal surfaces. They are safety catchers, not replacement floors. V0.23 also adds conservative contact proxies to selected large foreground solids. Small foliage, patina, fracture marks and atmospheric detail remain non-blocking.

The V0.23 cavern roof continues to use the same V0.22 spatial recipe, but its mesh is regenerated with inward-facing winding and assigned to both renderer and `MeshCollider`.

`FracturedSignalDuelStabilityV22` still does not create a second ordinary movement or attack scheduler. V0.19 remains the locomotion owner and `FracturedSignalDirector` remains the scheduler. The Wisp intermission and neural-link safety stop remain higher-authority pause owners and are never cleared while active.

## Latest menu

`Mindforge → Latest` intentionally contains only:

- **PLAY LATEST (BCI Simulation)**: rebuild V0.11, apply V0.20 → V0.21 → V0.22 → V0.23, open and play in controller BCI simulation.
- **Rebuild Latest Integrated Scene**: perform the same deterministic build without Play Mode.
- **Open Latest Integrated Scene**: open the canonical scene and upgrade missing world layers in order.
- **Validate Latest Readiness**: run the maintained readiness audit. It is software/scene evidence, not physical SSVEP qualification.
- **Build Neural-Hardware Variant**: build the same world with controller-only qualification disabled for real neural-service/hardware testing.

## Manual Wisp and first-boss contract

Holding `V` remains a deliberate listening ritual. When a Wisp window arms, boss attacks and existing hostile projectiles pause and Guardian combat commands are suspended while ordinary locomotion remains available. V0.22 explicitly detects this owner and will not repair that pause; V0.23 does not touch this runtime authority at all.

Outside Wisp/neural safety, the ordinary sword fight should remain continuously live after encounter entry. The boss should use most of the chamber, recover from pathological wall stalls, provide reliable sword contact and present fewer simultaneous projectile/echo distractions.

## Legacy policy

Historical V0.5-V0.10 showcase/build commands and the old V0.11 menu are implementation history, not supported development entry points. Their Unity menu entries live only under:

**Mindforge → Legacy**

Do not compose a new release by manually running historical `Apply ...` commands. If the canonical latest scene needs an older capability, the latest assembler must call the smallest required implementation explicitly and deterministically.

There should never again be multiple equally plausible "latest" builders.

## V0.23 playtest flow

1. Pull the intended branch or `main` and allow Unity to compile/import.
2. Run **Mindforge → Latest → PLAY LATEST (BCI Simulation)**.
3. Revisit the Choir Tower ascent first. Jump, double-jump, hover and air-dash across the area where the recording showed the intersecting floor. There should be one readable ramp surface and no body-through-stone illusion.
4. Cross from Causeway to Market around z=32–33 from the center and both sides. The one-metre historical collision seam must no longer allow a drop while the recessed catcher must remain imperceptible during ordinary traversal.
5. Traverse every other floor, perch, threshold and boss edge. Seam guards must never feel like higher invisible platforms.
6. Run into major columns, foreground rocks, fracture spires and chamber buttresses. Large objects that look solid should now feel solid; small foliage and surface patina should remain non-blocking.
7. Rotate the camera aggressively around those same objects and walls. The existing gameplay-camera sphere cast should respect newly reconciled solid scenery rather than passing through it.
8. Look upward and toward both cavern ends throughout the route. The ceiling should light as an interior surface and the high north/south vault should remain closed without sky wedges.
9. Explore lateral edges using the full aerial kit. Existing V0.22 roof/perimeter containment must remain intact.
10. Enter the Fractured Signal chamber, use the entire arena, and test sword contact, dodge, jump, guard, projectile parry and all boss phases. V0.23 must not regress V0.22 combat behavior.
11. Hold `V`, verify the deliberate ceasefire, then end the Wisp window and verify combat resumes.
12. Run **Mindforge → Latest → Validate Latest Readiness** and inspect the Console for any V0.23 foundation validation failure.

For real BCI testing, use **Build Neural-Hardware Variant** and the live neural service on a physically qualified display. Software readiness still does not substitute for photodiode timing or real EEG qualification.
