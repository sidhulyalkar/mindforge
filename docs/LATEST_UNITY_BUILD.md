# Canonical Unity build

## One development target

For ordinary Mindforge development there is exactly one supported Unity entry point:

**Mindforge → Latest → PLAY LATEST (BCI Simulation)**

This rebuilds and opens the canonical integrated scene, then enters Play Mode.

The current product label is **V0.20 World Soul**. The scene asset remains `Assets/Mindforge/Scenes/MindforgeDemoV11.unity` because V0.11 is the version of the clean systems/traversal assembler, not the version of the complete game. Product-version churn must not force serialized-scene renames.

`MindforgeLatestEditorMenu.BuildCanonical(...)` has two deterministic authoring stages:

1. `MindforgeDemoV11Builder.BuildDemoScene(...)` creates the clean authoritative systems and traversal kernel.
2. `WorldSoulV20Builder.ApplyOpenScene()` authors one static presentation landscape onto that kernel and saves the same canonical scene.

The resulting game also receives the maintained runtime layers after scene load, including current Guardian/combat presentation, the V0.19 Fractured Signal boss redesign, the manual-Wisp combat intermission and the current SSVEP/telemetry systems.

## Why V0.20 is different from the historical showcase stack

V0.11 deliberately stopped composing the V0.5-V0.10 showcase decorator tower. That was necessary to regain one comprehensible world assembler, but the clean replacement still relied heavily on simple blocks and flat-color URP/Lit surfaces.

V0.20 does **not** resurrect those old decorators. It adds exactly one new canonical world-authoring layer. `WorldSoulV20Builder` owns terrain continuity, surface breakup, natural set dressing, environmental storytelling, far-field city silhouette and static atmosphere. It never owns traversal, collision, combat, persistence or neural evidence.

The canonical build now combines:

- the clean V0.11 district/world and systems assembler;
- V0.20 deterministic landforms and generated world-surface materials;
- V0.20 Sanctum grove, Causeway canal ecology, Market ruins, Ascent geology, Fracture crater and distant city;
- current Guardian movement and combat authority;
- the moving/readable V0.19 Fractured Signal first-boss layer;
- the V0.19 two-sided manual-Wisp combat intermission;
- the synchronized SSVEP epoch/decoder and display-timing contracts;
- neural-quiet calibration/presentation rules;
- the current directed intro/gameplay camera and HUD;
- current persistence, telemetry and BCI interfaces.

## Public graphics code policy

World Soul uses public codebases as engineering references, not as an indiscriminate asset dump.

`SebLague/Procedural-Landmass-Generation` is MIT-licensed and informs V0.20's deterministic multi-octave noise grammar. `aadebdeb/ProceduralMesh` is MIT-licensed and reinforces the repository's recipe-over-binary mesh workflow. `keijiro/NoiseShader` is MIT-licensed and has been evaluated as a future GPU surface-noise path, but V0.20 intentionally does not add it as a runtime dependency.

Generated V0.20 textures, materials and mesh assets live under `Assets/Mindforge/Generated/V20`, which is already ignored by source control. Source control stores the deterministic recipes. Unity regenerates the local assets.

When borrowing from public projects:

- confirm the upstream license before adapting code;
- record the source and license in the owning Mindforge file/doc;
- prefer adapting a small technique over importing a whole framework;
- do not copy another game's character identity or art direction;
- do not add a runtime dependency where an editor-authored result is sufficient;
- preserve the SSVEP visual-control boundary.

## World Soul authority boundary

V0.20 scenery is static presentation. The existing V0.11 route floors, walls and authored gameplay geometry remain the collision/navigation authority.

World Soul therefore does not add gameplay colliders or Rigidbodies. Its primitive helpers remove Unity's temporary primitive colliders immediately. Terrain, rocks, trees, ruins and far-city geometry are render-only. Existing actor renderers are excluded from world-surface retargeting through `CombatantVitals` ownership checks.

The world can be visually dense without becoming temporally noisy. V0.20 adds no per-frame `Update`, `LateUpdate` or `FixedUpdate` loop. It adds no wind animation, particle weather or periodic environmental flicker. Its locality lights are static and shadowless. This keeps neural evidence windows from inheriting a new uncontrolled temporal stimulus source.

## Latest menu

`Mindforge → Latest` intentionally contains only:

- **PLAY LATEST (BCI Simulation)**: normal day-to-day command. Rebuilds V0.11, applies World Soul, opens the scene and starts controller-only BCI simulation.
- **Rebuild Latest Integrated Scene**: same deterministic build without entering Play Mode.
- **Open Latest Integrated Scene**: opens the existing canonical scene. If the scene predates World Soul, it is upgraded once by applying V0.20.
- **Validate Latest Readiness**: runs the maintained readiness audit. This is software/scene evidence, not physical SSVEP qualification.
- **Build Neural-Hardware Variant**: builds the same V0.20 world with controller-only qualification disabled for real neural-service/hardware testing.

## Manual Wisp and first-boss contract

The V0.19 Fractured Signal remains the first-boss baseline inside V0.20. It moves, manages spacing, roots for attack commitment and recovery, and attacks at a deliberately slower first-encounter cadence than the old turret-like scheduler presentation.

Holding `V` remains a deliberate listening ritual. When the Wisp window arms, boss attacks and existing hostile projectiles pause and Guardian combat commands are suspended while ordinary locomotion remains available. The Wisp intermission never overrides neural-link degradation or participant-stop safety.

World Soul must never weaken that separation. During SSVEP evidence, maintained runtime presentation systems remain responsible for freezing relevant boss/camera temporal motion. V0.20 itself has no runtime ambient animation to freeze.

## Legacy policy

Historical V0.5-V0.10 showcase/build commands and the old V0.11 menu are implementation history, not supported development entry points. Their Unity menu entries live only under:

**Mindforge → Legacy**

Do not compose a new release by manually running historical `Apply ...` commands. If the canonical latest scene needs an older capability, the latest assembler must call the smallest required implementation explicitly and deterministically.

There should never again be multiple equally plausible "latest" builders.

## V0.20 playtest flow

1. Pull the intended branch or `main` and allow Unity to compile/import.
2. Run **Mindforge → Latest → PLAY LATEST (BCI Simulation)**.
3. Confirm the scene no longer ends visually at the edge of the traversable route. West/east landmass, south foreground and north highlands should make the districts feel embedded in terrain.
4. Inspect the Sanctum. It should read as an inhabited/reclaimed grove rather than a clean room made from primitives.
5. Walk the Causeway and Market. Canal vegetation, bank rocks, ruined colonnades, rubble and distant structures should create depth without changing collision.
6. Climb the Ascent. Geological masses should visually support the elevation change instead of leaving a ramp floating inside architecture.
7. Enter the Fractured Signal arena. The boss should remain readable against an exterior crater/ruin silhouette, and the broad southern approach must stay visually open.
8. Confirm distant buildings and horizon spires imply a larger ruined city without becoming navigable fake geometry.
9. Hold `V`. The sword fight must still disappear for the complete Wisp window. No V0.20 environmental object should begin, stop or visibly pulse because neural evidence started.
10. Check the Console for V0.20 authoring/runtime errors, then iterate on composition from the actual gameplay camera rather than Scene-view beauty shots.

For real BCI testing, use **Build Neural-Hardware Variant** and the live neural service on a physically qualified display. A software readiness report still does not substitute for photodiode timing or real EEG qualification.
